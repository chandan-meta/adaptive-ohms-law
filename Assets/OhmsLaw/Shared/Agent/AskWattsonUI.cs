using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OhmsLaw
{
    // The Ask Wattson conversation, inside the lesson panel: the panel's header button opens it (the lesson pauses and the
    // panel's column gives way to the conversation) and closes it again. Collapsed, it shows this scene's latest exchange
    // (the explanation stays readable after the voice ends); HISTORY shows the whole session, scrollable. Every message
    // carries a label (YOU / WATTSON · LIVE AI / OFFLINE ANSWER / LAB CONTROLS), never colour alone, and the status line
    // says what Wattson is doing: listening, thinking, speaking.
    public class AskWattsonUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        const float Pad = 18f, Gap = 10f, HeaderHeight = 76f, InputHeight = 60f, AskLabel = 24f, Actions = 250f;   // Actions: History + Close
        const int MaxViews = 40;

        class View
        {
            public WattsonAgent.Message message;
            public GameObject root;
            public TextMeshProUGUI text;
            public int scene;
        }

        WattsonAgent agent;
        RectTransform home, content, suggestions;
        ScrollRect scroll;
        TMP_InputField input;
        TextMeshProUGUI status, historyLabel, micLabel, sendLabel;
        GameObject sendIcon;
        const float MicWidth = 192f;
        HudButton micHud;
        Image stateDot;
        readonly List<View> views = new List<View>();
        bool expanded, scrollToEnd, spaceTalk;   // spaceTalk: the Space bar (not the mic button) started the current hold
        int dirtyFrames, scene;   // scene bumps on every load: the collapsed view shows only this scene's latest exchange
        double resumeTime = -1;

        static ModuleFlow Flow => ModuleFlow.Instance;
        static LessonPanel Panel => LessonPanel.Instance;
        static SceneAgentContext Context => WattsonController.Active ? WattsonController.Active.agentContext : null;
        bool Busy => agent.Current == WattsonAgent.State.Thinking || agent.Current == WattsonAgent.State.Talking;

        public static AskWattsonUI Instance { get; private set; }

        // Play-test hook: open the conversation and ask, exactly as a learner would (the button, then the field).
        public static void Probe(string question)
        {
            if (!Instance) return;
            if (!IsOpen) Instance.Open();
            Instance.Send(question);
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        void Awake()
        {
            Instance = this;
            agent = Flow.Agent;
            home = Panel.Conversation;
            Build();
            Panel.AskButton.onClick.AddListener(Toggle);
            agent.MessageAdded += OnAdded;
            agent.MessageChanged += OnChanged;
            SceneManager.sceneLoaded += (_, _) => { resumeTime = -1; scene++; if (IsOpen) Close(); dirtyFrames = 2; };
        }

        // Design v3: a header (his face, his name, what he is doing in plain words, HISTORY, Close), the conversation,
        // the suggested questions as outlined chips, and a labelled input row (Hold to talk, the field, Ask). Every
        // button carries a word.
        void Build()
        {
            var spark = LessonPanel.Spark;

            var top = Band("Header", Pad, HeaderHeight, true);
            var face = UI.Rect("Face", top, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(48, 48));
            var ringImage = face.gameObject.AddComponent<Image>();
            ringImage.sprite = UI.Circle();
            ringImage.color = spark;
            ringImage.raycastTarget = false;
            var disc = UI.Rect("Disc", face, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42, 42));
            var discImage = disc.gameObject.AddComponent<Image>();
            discImage.sprite = UI.Circle();
            discImage.color = LessonPanel.Ink;
            discImage.raycastTarget = false;
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (UI.Theme && UI.Theme.wattsonFace) { var raw = UI.Stretch("Face", disc).gameObject.AddComponent<RawImage>(); raw.texture = UI.Theme.wattsonFace; raw.raycastTarget = false; }
            // His name, and under it the status: up to two lines at 16 px in the width History and Close leave.
            var name = UI.Rect("Name", top, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(62, 20), new Vector2(0, 28));
            name.anchorMax = new Vector2(1, 0.5f);
            name.sizeDelta = new Vector2(-62 - Actions, 28);
            LessonPanel.Text(name, "Wattson", 22f, LessonPanel.Paper, TextAlignmentOptions.MidlineLeft);
            var statusRow = UI.Rect("Status", top, new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(62, 4), new Vector2(0, 42));
            statusRow.anchorMax = new Vector2(1, 0.5f);
            statusRow.sizeDelta = new Vector2(-62 - Actions, 42);
            stateDot = UI.Rect("State", statusRow, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(0, -11), new Vector2(10, 10)).gameObject.AddComponent<Image>();
            stateDot.sprite = UI.Circle();
            stateDot.color = spark;
            stateDot.raycastTarget = false;
            var statusText = UI.Stretch("Text", statusRow);
            statusText.offsetMin = new Vector2(18, 0);
            status = LessonPanel.Text(statusText, "", 16f, LessonPanel.Muted, TextAlignmentOptions.TopLeft, false);
            status.lineSpacing = -8f;
            var actions = LessonPanel.Row("Actions", top, 8f, TextAnchor.MiddleRight);
            actions.anchorMin = actions.anchorMax = new Vector2(1, 0.5f);
            actions.pivot = new Vector2(1, 0.5f);
            actions.anchoredPosition = Vector2.zero;
            actions.sizeDelta = new Vector2(Actions, 48);
            historyLabel = LessonPanel.Pill("History", actions, "History", ToggleHistory, LessonPanel.Kind.Ghost, null, 48f, 16f).GetComponentInChildren<TextMeshProUGUI>();
            LessonPanel.Pill("Close", actions, "Close", Close, LessonPanel.Kind.Secondary, "x", 48f, 17f);

            // Conversation: a scrolling column of messages, then the suggested questions.
            var view = UI.Stretch("Conversation", home);
            view.offsetMin = new Vector2(Pad, Pad + InputHeight + AskLabel + Gap);
            view.offsetMax = new Vector2(-Pad, -(Pad + HeaderHeight + Gap));
            view.gameObject.AddComponent<Image>().color = Color.clear; // catches drags and the mouse wheel
            view.gameObject.AddComponent<RectMask2D>();
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.viewport = view;
            content = Column("Content", view, 10);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            suggestions = Column("Suggestions", content, 8);

            // Input: its label, then hold-to-talk, the text field, and ASK (STOP while Wattson is busy and nothing is typed).
            var label = Band("Ask Label", Pad + InputHeight + 4, AskLabel, false);
            LessonPanel.Eyebrow(label, "Or ask your own");
            var row = Band("Input", Pad, InputHeight, false);
            var mic = LessonPanel.Pill("Mic", row, "Hold to talk", null, LessonPanel.Kind.Secondary, "mic", InputHeight, 18f);
            var micRect = (RectTransform)mic.transform;
            micRect.anchorMin = micRect.anchorMax = new Vector2(0, 0.5f);
            micRect.pivot = new Vector2(0, 0.5f);
            micRect.anchoredPosition = Vector2.zero;
            micRect.sizeDelta = new Vector2(MicWidth, InputHeight);
            mic.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(16, 16, 0, 0);   // "Let go to send" fits
            micHud = mic.GetComponent<HudButton>();
            micHud.fillColor = UI.Alpha(spark, 0.14f);
            micHud.activeFill = spark;
            micHud.outlineColor = micHud.activeOutline = UI.Alpha(spark, 0.5f);
            micLabel = mic.GetComponentInChildren<TextMeshProUGUI>();
            var hold = mic.gameObject.AddComponent<EventTrigger>();
            LessonPanel.On(hold, EventTriggerType.PointerDown, agent.StartListening);
            LessonPanel.On(hold, EventTriggerType.PointerUp, agent.StopListening);

            var field = UI.Rect("Question", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(MicWidth + Gap, 0), new Vector2(0, InputHeight));
            field.anchorMax = new Vector2(1, 0.5f);
            field.sizeDelta = new Vector2(-(MicWidth + Gap) - (120 + Gap), InputHeight);
            UI.Panel(field, LessonPanel.Raised, true, InputHeight / 2f);
            UI.Outline(UI.Stretch("Edge", field), LessonPanel.ControlLine, InputHeight / 2f, 1.5f);
            input = field.gameObject.AddComponent<TMP_InputField>();
            var area = UI.Stretch("Text Area", field);
            area.offsetMin = new Vector2(22, 8);
            area.offsetMax = new Vector2(-18, -8);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = UI.Label(area, "Type a question", 20, LessonPanel.Muted, TextAlignmentOptions.MidlineLeft);
            var text = UI.Label(area, "", 20, LessonPanel.Paper, TextAlignmentOptions.MidlineLeft);
            placeholder.textWrappingMode = text.textWrappingMode = TextWrappingModes.NoWrap;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 300;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onSubmit.AddListener(Send);
            var send = LessonPanel.Pill("Send", row, "Ask", SendOrStop, LessonPanel.Kind.Primary, "send", InputHeight, 19f);
            var sendRect = (RectTransform)send.transform;
            sendRect.anchorMin = sendRect.anchorMax = new Vector2(1, 0.5f);
            sendRect.pivot = new Vector2(1, 0.5f);
            sendRect.anchoredPosition = Vector2.zero;
            sendRect.sizeDelta = new Vector2(120, InputHeight);
            sendLabel = send.GetComponentInChildren<TextMeshProUGUI>();
            sendIcon = send.transform.Find("Icon send").gameObject;
        }

        // A full-width strip at a fixed distance from the conversation area's top or bottom edge.
        RectTransform Band(string name, float distance, float height, bool fromTop)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(home, false);
            rect.anchorMin = new Vector2(0, fromTop ? 1 : 0);
            rect.anchorMax = new Vector2(1, fromTop ? 1 : 0);
            rect.pivot = new Vector2(0.5f, fromTop ? 1 : 0);
            rect.sizeDelta = new Vector2(-2 * Pad, height);
            rect.anchoredPosition = new Vector2(0, fromTop ? -distance : distance);
            return rect;
        }

        static RectTransform Column(string name, Transform parent, float spacing)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false; // otherwise each child claims a share of the empty space
            return rect;
        }

        void Open()
        {
            IsOpen = true;
            // Coming back restarts the scripted line that was interrupted, if there was one.
            resumeTime = SubtitleTrack.CurrentLine ? SubtitleTrack.LastLineStart : -1;
            Flow.SetPaused(ModuleFlow.PauseReason.Ask, true);
            WideShot();
            agent.Engage(true);
            Panel.ShowConversation(true);
            ShowSuggestions();
            dirtyFrames = 2;
        }

        // A rebuilt scene has lost the conversation shot CinematicPolish gives Wattson (the study's wide shot in Scene 7, the
        // bench's in the lab chapters): the same rule at run time, so an Ask during a close shot (Scene 7's "Study Aim",
        // 1 m from the wire) never parks him a hand's width from the lens. Scenes without such a camera keep the old spot.
        static void WideShot()
        {
            var wattson = WattsonController.Active;
            if (!wattson || wattson.conversationCamera) return;
            string wide = SceneManager.GetActiveScene().name.StartsWith("Scene07") ? "Cam Study Wide" : "Cam Bench Wide";
            foreach (var cam in FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (cam.name == wide) { wattson.conversationCamera = cam; return; }
        }

        void Close()
        {
            IsOpen = false;
            agent.Engage(false); // stops any answer: the story never plays over Wattson
            if (Panel) Panel.ShowConversation(false);
            if (resumeTime >= 0) Flow.JumpTo(resumeTime);
            Flow.SetPaused(ModuleFlow.PauseReason.Ask, false);
        }

        void ToggleHistory()
        {
            expanded = !expanded;
            dirtyFrames = 2;
        }

        void Send(string question)
        {
            if (string.IsNullOrWhiteSpace(question)) return;
            input.text = "";
            agent.Ask(question);
        }

        void SendOrStop()
        {
            if (!string.IsNullOrWhiteSpace(input.text)) Send(input.text);
            else if (Busy) agent.Stop();
        }

        // The scene's suggested questions (their saved answers are the offline fallback). None while an activity runs (some
        // answer it outright), none naming the ohm before it is taught (WattsonAgent.Offered).
        void ShowSuggestions()
        {
            foreach (Transform child in suggestions) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var faq = Context ? Context.faq : null;
            var questions = new List<string>();
            if (faq && faq.entries != null)
                foreach (var entry in faq.entries)
                    if (entry != null && !string.IsNullOrEmpty(entry.question) && questions.Count < 3 && WattsonAgent.Offered(entry.question, entry.answer))
                        questions.Add(entry.question);
            if (questions.Count == 0) return;
            LessonPanel.Eyebrow(suggestions, "Try asking");
            foreach (var question in questions)
            {
                var chip = LessonPanel.Pill("Chip", suggestions, question, () => agent.Ask(question), LessonPanel.Kind.Secondary, null, 52f, 18f);
                var words = chip.GetComponentInChildren<TextMeshProUGUI>();
                words.textWrappingMode = TextWrappingModes.Normal;
                words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                var line = chip.GetComponent<HorizontalLayoutGroup>();
                line.childAlignment = TextAnchor.MiddleLeft;
                line.padding = new RectOffset(20, 20, 8, 8);
                var size = chip.GetComponent<LayoutElement>();
                size.minHeight = 52f;
                size.preferredHeight = -1f;   // two-line questions grow the chip
                chip.GetComponent<HudButton>().fillColor = Color.clear;
            }
        }

        void OnAdded(WattsonAgent.Message message)
        {
            bool you = message.FromLearner;
            var bubble = new GameObject(you ? "You" : "Wattson", typeof(RectTransform)).GetComponent<RectTransform>();
            bubble.SetParent(content, false);
            UI.Panel(bubble, you ? new Color(1f, 1f, 1f, 0.13f) : LessonPanel.Raised, true, 18f).raycastTarget = false;
            if (!you) UI.Outline(UI.Stretch("Edge", bubble), LessonPanel.CardLine, 18f, 1.5f).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var layout = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 14, 14);
            layout.spacing = 6;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var align = you ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            if (!you)
            {
                var who = UI.Label(bubble, Label(message.speaker), 14, LessonPanel.Spark, align);
                who.fontStyle = FontStyles.Bold;
                who.characterSpacing = 6;
            }
            var text = UI.Label(bubble, message.text, 20, LessonPanel.Paper, align);
            text.fontStyle = FontStyles.Bold;
            views.Add(new View { message = message, root = bubble.gameObject, text = text, scene = scene });
            if (views.Count > MaxViews) { views[0].root.SetActive(false); Destroy(views[0].root); views.RemoveAt(0); }
            suggestions.SetAsLastSibling();
            dirtyFrames = 2;
        }

        void OnChanged(WattsonAgent.Message message)
        {
            foreach (var view in views)
                if (view.message == message) { view.text.text = message.text; dirtyFrames = 2; return; }
        }

        static string Label(WattsonAgent.Speaker speaker) =>
            speaker == WattsonAgent.Speaker.LabControls ? "WATTSON " + UI.Glyph("·", "-") + " LAB CONTROLS" : "WATTSON";

        // What Wattson is doing, in plain words (the technical detail lives in the F1 review overlay).
        string StatusText()
        {
            switch (agent.Current)
            {
                case WattsonAgent.State.Listening: return "Listening · let go to send";
                case WattsonAgent.State.Thinking: return "Thinking...";
                case WattsonAgent.State.Talking: return "Speaking · tap Stop to cut in";
                case WattsonAgent.State.Error: return "His radio is fuzzy. Try a suggested question.";
            }
            if (agent.Note.Length > 0) return agent.Note;
            if (agent.Offline || !agent.LiveAnswered) return "Answering from his case notes";
            if (agent.Resting) return "Resting a moment: too many questions";
            var rig = CircuitRig.Active;
            return rig && rig.controlsOpen ? "Ask anything, or tell me a voltage to set." : "Ask about this chapter";
        }

        void Update()
        {
            var context = Context;
            if (Panel && Panel.AskRoot) Panel.AskRoot.gameObject.SetActive((context && context.askWattsonEnabled || IsOpen) && !TitleScreen.IsOpen);
            if (!IsOpen) return;

            status.text = StatusText();
            bool listening = agent.Current == WattsonAgent.State.Listening;
            micLabel.text = listening ? "Let go to send" : "Hold to talk";
            micHud.active = listening;
            if (!FigmaSkin.Available)
                micLabel.color = listening ? LessonPanel.Ink : agent.VoiceInputDown ? LessonPanel.Muted : LessonPanel.Spark;
            bool stop = Busy && string.IsNullOrWhiteSpace(input.text);
            sendLabel.text = stop ? "Stop" : "Ask";
            sendIcon.SetActive(!stop);   // the arrow means send; Stop reads alone
            historyLabel.text = expanded ? "Latest" : "History";
            // The state dot: steady while idle, breathing while he listens, thinks or speaks; muted on an error.
            float breathe = ReducedMotion.On ? 1f : 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * (agent.Current == WattsonAgent.State.Thinking ? 8f : 4f));
            stateDot.color = agent.Current == WattsonAgent.State.Error ? LessonPanel.Muted
                : agent.Current == WattsonAgent.State.Idle ? UI.Alpha(LessonPanel.Spark, 0.6f)
                : UI.Alpha(LessonPanel.Spark, breathe);

            // Spacebar is push-to-talk on PC, unless the student is typing. Letting go always ends a hold Space started,
            // even if the text field took focus meanwhile (otherwise he stays Listening with the mic running).
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (spaceTalk && !keyboard.spaceKey.isPressed) { spaceTalk = false; agent.StopListening(); }
            if (!input.isFocused && keyboard.spaceKey.wasPressedThisFrame) { spaceTalk = true; agent.StartListening(); }
        }

        void LateUpdate()
        {
            if (!IsOpen || dirtyFrames <= 0) return;
            dirtyFrames--;
            // Collapsed: this scene's latest question and everything after it. Expanded: the whole session.
            int latest = -1;
            for (int i = views.Count - 1; i >= 0 && latest < 0; i--)
                if (views[i].scene == scene && views[i].message.FromLearner) latest = i;
            for (int i = 0; i < views.Count; i++) views[i].root.SetActive(expanded || (latest >= 0 && i >= latest));
            bool offlineLast = views.Count > 0 && views[views.Count - 1].message.speaker == WattsonAgent.Speaker.Offline;
            suggestions.gameObject.SetActive(expanded || latest < 0 || offlineLast || agent.Offline || agent.Resting);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content); // text heights depend on the width the layout gives them
            scrollToEnd = true;
            if (scrollToEnd) { scroll.verticalNormalizedPosition = 0; scrollToEnd = false; }
        }
    }
}
