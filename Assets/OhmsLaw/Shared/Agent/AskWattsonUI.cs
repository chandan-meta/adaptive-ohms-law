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

        const float Pad = 4f, Gap = 8f, StatusHeight = 48f, InputHeight = 80f, ButtonWidth = 118f;
        const int MaxViews = 40;
        static readonly Color MicIdle = new Color(0.16f, 0.36f, 0.6f), MicLive = new Color(0.85f, 0.28f, 0.22f), MicDim = new Color(0.3f, 0.33f, 0.38f);
        static readonly Color OfflineTint = new Color(1f, 0.72f, 0.3f), ControlsTint = new Color(0.5f, 0.88f, 0.58f);

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
        TextMeshProUGUI status, mode, historyLabel, micLabel, sendLabel;
        Image micImage, stateDot;
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

        void Awake()
        {
            Instance = this;
            agent = Flow.Agent;
            home = Panel.Conversation;
            Build();
            Panel.AskButton.onClick.AddListener(() => { if (IsOpen) Close(); else Open(); });
            agent.MessageAdded += OnAdded;
            agent.MessageChanged += OnChanged;
            SceneManager.sceneLoaded += (_, _) => { resumeTime = -1; scene++; if (IsOpen) Close(); dirtyFrames = 2; };
        }

        void Build()
        {
            var accent = UI.Theme ? UI.Theme.wattsonAccent : Color.cyan;

            // Status strip at the top: a state dot, the status text, the connection mode, HISTORY.
            var top = Band("Status", Pad, StatusHeight, true);
            stateDot = UI.Rect("State", top, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(18, 18)).gameObject.AddComponent<Image>();
            stateDot.sprite = UI.Circle();
            stateDot.color = accent;
            stateDot.raycastTarget = false;
            var statusRect = UI.Rect("Text", top, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(34, 0), new Vector2(0, StatusHeight));
            statusRect.anchorMax = new Vector2(1, 0.5f);
            statusRect.sizeDelta = new Vector2(-34 - 150, StatusHeight);
            status = Fit(UI.Label(statusRect, "", 20, new Color(1, 1, 1, 0.9f), TextAlignmentOptions.MidlineLeft));
            historyLabel = Fit(UI.Button("History", top, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(140, StatusHeight), "HISTORY", ToggleHistory, 20)
                .GetComponentInChildren<TextMeshProUGUI>());
            mode = Fit(UI.Label(Band("Mode", Pad + StatusHeight, 26, true), "", 16, OfflineTint, TextAlignmentOptions.MidlineLeft));
            mode.fontStyle = FontStyles.Bold;

            // Conversation: a scrolling column of labelled messages, then the suggested questions.
            var view = UI.Stretch("Conversation", home);
            view.offsetMin = new Vector2(Pad, Pad + InputHeight + Gap);
            view.offsetMax = new Vector2(-Pad, -(Pad + StatusHeight + 26 + Gap));
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

            // Input: hold-to-talk, the text field, and SEND (STOP while Wattson is busy and nothing is typed).
            var row = Band("Input", Pad, InputHeight, false);
            var mic = UI.Button("Mic", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(ButtonWidth, InputHeight), "HOLD\nTO TALK", null, 20);
            micImage = mic.GetComponent<Image>();
            micImage.color = MicIdle;
            micLabel = Fit(mic.GetComponentInChildren<TextMeshProUGUI>());
            var hold = mic.gameObject.AddComponent<EventTrigger>();
            LessonPanel.On(hold, EventTriggerType.PointerDown, agent.StartListening);
            LessonPanel.On(hold, EventTriggerType.PointerUp, agent.StopListening);

            var field = UI.Rect("Question", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(ButtonWidth + Gap, 0), new Vector2(0, InputHeight - 4));
            field.anchorMax = new Vector2(1, 0.5f);
            field.sizeDelta = new Vector2(-2 * (ButtonWidth + Gap), InputHeight - 4);
            UI.Panel(field, new Color(1, 1, 1, 0.12f));
            input = field.gameObject.AddComponent<TMP_InputField>();
            var area = UI.Stretch("Text Area", field);
            area.offsetMin = new Vector2(14, 8);
            area.offsetMax = new Vector2(-14, -8);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = UI.Label(area, "Type a question...", 22, new Color(1, 1, 1, 0.45f), TextAlignmentOptions.MidlineLeft);
            var text = UI.Label(area, "", 22, Color.white, TextAlignmentOptions.MidlineLeft);
            placeholder.textWrappingMode = text.textWrappingMode = TextWrappingModes.NoWrap;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 300;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onSubmit.AddListener(Send);
            sendLabel = Fit(UI.Button("Send", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(ButtonWidth, InputHeight), "SEND", SendOrStop, 22)
                .GetComponentInChildren<TextMeshProUGUI>());
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

        // Small labels shrink (down to 14 px) rather than spill out of their buttons or strip.
        static TextMeshProUGUI Fit(TextMeshProUGUI label)
        {
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = label.fontSize;
            return label;
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
            agent.Engage(true);
            Panel.ShowConversation(true);
            ShowSuggestions();
            dirtyFrames = 2;
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
            var heading = UI.Label(suggestions, "TRY ASKING", 17, new Color(1, 1, 1, 0.6f), TextAlignmentOptions.MidlineLeft);
            heading.fontStyle = FontStyles.Bold;
            heading.characterSpacing = 3;
            foreach (var question in questions)
            {
                var chip = UI.Button("Chip", suggestions, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, UI.MinTarget), question, () => agent.Ask(question), 22);
                chip.GetComponent<Image>().color = new Color(1, 1, 1, 0.12f);
                chip.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                var size = chip.gameObject.AddComponent<LayoutElement>();
                size.minHeight = size.preferredHeight = UI.MinTarget;
            }
        }

        void OnAdded(WattsonAgent.Message message)
        {
            bool you = message.FromLearner;
            Color tint = you ? UI.AccentColor :
                         message.speaker == WattsonAgent.Speaker.Offline ? OfflineTint :
                         message.speaker == WattsonAgent.Speaker.LabControls ? ControlsTint :
                         UI.Theme ? UI.Theme.wattsonAccent : Color.cyan;
            var bubble = new GameObject(you ? "You" : "Wattson", typeof(RectTransform)).GetComponent<RectTransform>();
            bubble.SetParent(content, false);
            UI.Panel(bubble, new Color(tint.r, tint.g, tint.b, you ? 0.24f : 0.14f), true, 16f).raycastTarget = false;
            var layout = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 12);
            layout.spacing = 2;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var align = you ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            var who = UI.Label(bubble, Label(message.speaker), 17, you ? new Color(1, 1, 1, 0.8f) : tint, align);
            who.fontStyle = FontStyles.Bold;
            who.characterSpacing = 3;
            var text = UI.Label(bubble, message.text, 24, Color.white, align);
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

        static string Label(WattsonAgent.Speaker speaker)
        {
            string dot = " " + UI.Glyph("·", "-") + " ";
            switch (speaker)
            {
                case WattsonAgent.Speaker.You: return "YOU";
                case WattsonAgent.Speaker.YouHeard: return "YOU (SPOKEN, AS HEARD)";
                case WattsonAgent.Speaker.Wattson: return "WATTSON" + dot + "LIVE AI";
                case WattsonAgent.Speaker.Offline: return "WATTSON" + dot + "OFFLINE ANSWER";
                default: return "WATTSON" + dot + "LAB CONTROLS";
            }
        }

        string StatusText()
        {
            switch (agent.Current)
            {
                case WattsonAgent.State.Listening: return "LISTENING: speak now, then let go to send.";
                case WattsonAgent.State.Thinking: return "THINKING: working on it...";
                case WattsonAgent.State.Talking: return "SPEAKING: tap STOP, or ask something new.";
                case WattsonAgent.State.Error: return "ERROR: " + agent.Problem;
            }
            if (agent.Note.Length > 0) return agent.Note;
            var rig = CircuitRig.Active;
            return rig && rig.controlsOpen ? "Ask anything, or tell me a voltage to set." : "HOLD TO TALK (or Space) and speak, or type a question.";
        }

        void Update()
        {
            var context = Context;
            if (Panel && Panel.AskRoot) Panel.AskRoot.gameObject.SetActive(context && context.askWattsonEnabled || IsOpen);
            if (!IsOpen) return;

            status.text = StatusText();
            // What is true: unreachable is OFFLINE, a rate limit is RESTING (the server is up; saved answers meanwhile).
            string chat = agent.Offline ? "OFFLINE MODE: saved answers only (the Wattson server needs its API keys)"
                : agent.Resting ? "RESTING: too many questions"
                : agent.LiveAnswered ? "LIVE AI"
                : "NOT CONNECTED YET: saved answers until the Wattson server answers (it needs its API keys)";
            string voice = agent.VoiceInputDown ? " · VOICE INPUT OFF" : "";
            mode.text = chat + voice;
            mode.color = agent.Offline || agent.Resting || !agent.LiveAnswered ? OfflineTint : ControlsTint;
            bool listening = agent.Current == WattsonAgent.State.Listening;
            micLabel.text = listening ? "LET GO\nTO SEND" : "HOLD\nTO TALK";
            micImage.color = listening ? MicLive : agent.VoiceInputDown ? MicDim : MicIdle;
            sendLabel.text = Busy && string.IsNullOrWhiteSpace(input.text) ? "STOP" : "SEND";
            historyLabel.text = expanded ? "LATEST ONLY" : "HISTORY";
            // The state dot: steady while idle, breathing while he listens, thinks or speaks.
            float breathe = ReducedMotion.On ? 1f : 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * (agent.Current == WattsonAgent.State.Thinking ? 8f : 4f));
            stateDot.color = agent.Current == WattsonAgent.State.Listening ? UI.Alpha(MicLive, breathe)
                : agent.Current == WattsonAgent.State.Error ? OfflineTint
                : agent.Current == WattsonAgent.State.Idle ? UI.Alpha(UI.Theme ? UI.Theme.wattsonAccent : Color.cyan, 0.6f)
                : UI.Alpha(UI.Theme ? UI.Theme.wattsonAccent : Color.cyan, breathe);

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
