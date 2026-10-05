using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OhmsLaw
{
    // The floating ASK WATTSON button and the conversation panel. The panel docks top-left (Wattson's side of the Boards
    // shots: the graph owns the right half of the board) and ends above the battery control when it is up, otherwise
    // above the subtitles, so the meters, graph and slider stay in view. Collapsed, it shows this scene's latest exchange
    // (the explanation stays readable after the voice ends); HISTORY shows the whole session, scrollable. Every message
    // carries a label (YOU / WATTSON · LIVE AI / OFFLINE ANSWER / LAB CONTROLS), never colour alone.
    public class AskWattsonUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        // Reference px (1920x1080). Touch targets stay at 88 px.
        const float Width = 520f, Margin = 24f, Top = 16f, Pad = 16f, Gap = 8f, Clearance = 16f;
        const float HeaderHeight = 88f, StatusHeight = 56f, InputHeight = 88f, ButtonWidth = 112f, MinConversation = 132f;
        const float Chrome = 2 * Pad + HeaderHeight + StatusHeight + InputHeight + 3 * Gap;
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
        RectTransform safe, askButton, panel, content, suggestions;
        ScrollRect scroll;
        TMP_InputField input;
        TextMeshProUGUI status, mode, historyLabel, micLabel, sendLabel;
        Image micImage;
        readonly List<View> views = new List<View>();
        bool expanded, scrollToEnd;
        int dirtyFrames, scene;   // scene bumps on every load: the collapsed view shows only this scene's latest exchange
        float contentHeight;
        double resumeTime = -1;

        static ModuleFlow Flow => ModuleFlow.Instance;
        static SceneAgentContext Context => WattsonController.Active ? WattsonController.Active.agentContext : null;
        bool Busy => agent.Current == WattsonAgent.State.Thinking || agent.Current == WattsonAgent.State.Talking;

        void Awake()
        {
            safe = (RectTransform)transform;
            agent = Flow.Agent;
            BuildAskButton();
            BuildPanel();
            agent.MessageAdded += OnAdded;
            agent.MessageChanged += OnChanged;
            SceneManager.sceneLoaded += (_, _) => { resumeTime = -1; scene++; if (IsOpen) Close(); dirtyFrames = 2; };
        }

        void BuildAskButton()
        {
            var accent = UI.Theme ? UI.Theme.wattsonAccent : Color.cyan;
            askButton = UI.Rect("Ask Wattson", transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 28), new Vector2(170, 190));
            var button = askButton.gameObject.AddComponent<Button>();
            button.onClick.AddListener(Open);

            // A magnifier: handle, ring, and Wattson's face in the lens.
            var handle = UI.Rect("Handle", askButton, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(52, -112), new Vector2(22, 60));
            handle.localRotation = Quaternion.Euler(0, 0, 45);
            UI.Panel(handle, accent).raycastTarget = false;
            var ring = UI.Rect("Ring", askButton, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-6, 0), new Vector2(140, 140));
            var ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = UI.Theme ? UI.Theme.circle : null;
            ringImage.color = accent;
            button.targetGraphic = ringImage;
            var lens = UI.Rect("Lens", ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));
            var lensImage = lens.gameObject.AddComponent<Image>();
            lensImage.sprite = ringImage.sprite;
            lensImage.color = new Color(0.08f, 0.12f, 0.2f);
            lens.gameObject.AddComponent<Mask>();
            if (UI.Theme && UI.Theme.wattsonFace)
            {
                var face = UI.Stretch("Face", lens).gameObject.AddComponent<RawImage>();
                face.texture = UI.Theme.wattsonFace;
                face.raycastTarget = false;
            }
            else UI.Label(lens, "?", 72, accent);

            var caption = UI.Label(askButton, "ASK WATTSON", 22, Color.white, TextAlignmentOptions.Bottom);
            caption.fontStyle = FontStyles.Bold;
        }

        void BuildPanel()
        {
            var accent = UI.Theme ? UI.Theme.wattsonAccent : Color.cyan;
            panel = UI.Rect("Ask Panel", transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-Width - 60, -Top), new Vector2(Width, Chrome + MinConversation));
            UI.Panel(panel, UI.HudColor, true, 24f);

            // Header: title and connection mode, HISTORY toggle, Back to the case.
            var header = Band("Header", Pad, HeaderHeight, true);
            var title = UI.Label(UI.Rect("Title", header, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(208, 46)), "ASK WATTSON", 28, accent, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            mode = UI.Label(UI.Rect("Mode", header, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -46), new Vector2(208, 42)), "", 16, OfflineTint, TextAlignmentOptions.TopLeft);
            mode.fontStyle = FontStyles.Bold;
            var back = UI.Button("Back", header, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(132, HeaderHeight), "BACK TO\nTHE CASE", Close, 22);
            back.GetComponent<Image>().color = MicIdle;
            Fit(back.GetComponentInChildren<TextMeshProUGUI>());
            historyLabel = Fit(UI.Button("History", header, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-140, 0), new Vector2(132, HeaderHeight), "HISTORY", ToggleHistory, 22)
                .GetComponentInChildren<TextMeshProUGUI>());

            // Conversation: a scrolling column of labelled messages, then the suggested questions.
            var view = UI.Stretch("Conversation", panel);
            view.offsetMin = new Vector2(Pad, Pad + InputHeight + Gap + StatusHeight + Gap);
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

            status = Fit(UI.Label(Band("Status", Pad + InputHeight + Gap, StatusHeight, false), "", 20, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.MidlineLeft));

            // Input: hold-to-talk, the text field, and SEND (STOP while Wattson is busy and nothing is typed).
            var row = Band("Input", Pad, InputHeight, false);
            var mic = UI.Button("Mic", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(ButtonWidth, InputHeight), "HOLD\nTO TALK", null, 20);
            micImage = mic.GetComponent<Image>();
            micImage.color = MicIdle;
            micLabel = Fit(mic.GetComponentInChildren<TextMeshProUGUI>());
            var hold = mic.gameObject.AddComponent<EventTrigger>();
            CinematicControlsUI.On(hold, EventTriggerType.PointerDown, agent.StartListening);
            CinematicControlsUI.On(hold, EventTriggerType.PointerUp, agent.StopListening);

            float fieldWidth = Width - 2 * Pad - 2 * (ButtonWidth + Gap);
            var field = UI.Rect("Question", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(ButtonWidth + Gap, 0), new Vector2(fieldWidth, 84));
            UI.Panel(field, new Color(1, 1, 1, 0.12f));
            input = field.gameObject.AddComponent<TMP_InputField>();
            var area = UI.Stretch("Text Area", field);
            area.offsetMin = new Vector2(14, 8);
            area.offsetMax = new Vector2(-14, -8);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = UI.Label(area, "Type a question...", 24, new Color(1, 1, 1, 0.45f), TextAlignmentOptions.MidlineLeft);
            var text = UI.Label(area, "", 24, Color.white, TextAlignmentOptions.MidlineLeft);
            placeholder.textWrappingMode = text.textWrappingMode = TextWrappingModes.NoWrap;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 300;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onSubmit.AddListener(Send);
            sendLabel = Fit(UI.Button("Send", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(ButtonWidth, InputHeight), "SEND", SendOrStop, 24)
                .GetComponentInChildren<TextMeshProUGUI>());
            panel.gameObject.SetActive(false);
        }

        // A full-width strip at a fixed distance from the panel's top or bottom edge.
        RectTransform Band(string name, float distance, float height, bool fromTop)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(panel, false);
            rect.anchorMin = new Vector2(0, fromTop ? 1 : 0);
            rect.anchorMax = new Vector2(1, fromTop ? 1 : 0);
            rect.pivot = new Vector2(0.5f, fromTop ? 1 : 0);
            rect.sizeDelta = new Vector2(-2 * Pad, height);
            rect.anchoredPosition = new Vector2(0, fromTop ? -distance : distance);
            return rect;
        }

        // Small labels shrink (down to 14 px) rather than spill out of their 88 px buttons or strip.
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
            panel.gameObject.SetActive(true);
            ShowSuggestions();
            dirtyFrames = 2;
        }

        void Close()
        {
            IsOpen = false;
            agent.Engage(false); // stops any answer: the story never plays over Wattson
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
                var chip = UI.Button("Chip", suggestions, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(Width - 2 * Pad, UI.MinTarget), question, () => agent.Ask(question), 22);
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

        // The panel's bottom edge: above the battery control while it is up, otherwise above the subtitles.
        float Bottom()
        {
            var control = safe.Find("Voltage Control") as RectTransform;
            if (control && control.gameObject.activeInHierarchy) return control.anchoredPosition.y + control.rect.height + Clearance;
            return UI.SubtitleTop + Clearance;
        }

        void Update()
        {
            var context = Context;
            askButton.gameObject.SetActive(!IsOpen && context && context.askWattsonEnabled);
            if (!panel.gameObject.activeSelf) return;

            float target = IsOpen ? Margin : -Width - 60;
            float x = ReducedMotion.On ? target : Mathf.Lerp(panel.anchoredPosition.x, target, 1 - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            panel.anchoredPosition = new Vector2(x, -Top);
            if (!IsOpen && x < -Width - 40) { panel.gameObject.SetActive(false); return; }

            status.text = StatusText();
            mode.text = agent.Offline && agent.VoiceInputDown ? "OFFLINE MODE\nVOICE INPUT OFF" :
                        agent.Offline ? "OFFLINE MODE" : agent.VoiceInputDown ? "VOICE INPUT OFF" : "";
            bool listening = agent.Current == WattsonAgent.State.Listening;
            micLabel.text = listening ? "LET GO\nTO SEND" : "HOLD\nTO TALK";
            micImage.color = listening ? MicLive : agent.VoiceInputDown ? MicDim : MicIdle;
            sendLabel.text = Busy && string.IsNullOrWhiteSpace(input.text) ? "STOP" : "SEND";
            historyLabel.text = expanded ? "LATEST\nONLY" : "HISTORY";

            // Spacebar is push-to-talk on PC, unless the student is typing.
            var keyboard = Keyboard.current;
            if (!IsOpen || keyboard == null || input.isFocused) return;
            if (keyboard.spaceKey.wasPressedThisFrame) agent.StartListening();
            if (keyboard.spaceKey.wasReleasedThisFrame) agent.StopListening();
        }

        void LateUpdate()
        {
            if (!panel.gameObject.activeSelf) return;
            if (dirtyFrames > 0)
            {
                dirtyFrames--;
                // Collapsed: this scene's latest question and everything after it. Expanded: the whole session.
                int latest = -1;
                for (int i = views.Count - 1; i >= 0 && latest < 0; i--)
                    if (views[i].scene == scene && views[i].message.FromLearner) latest = i;
                for (int i = 0; i < views.Count; i++) views[i].root.SetActive(expanded || (latest >= 0 && i >= latest));
                bool offlineLast = views.Count > 0 && views[views.Count - 1].message.speaker == WattsonAgent.Speaker.Offline;
                suggestions.gameObject.SetActive(expanded || latest < 0 || offlineLast || agent.Offline);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content); // text heights depend on the width the layout gives them
                contentHeight = LayoutUtility.GetPreferredHeight(content);
                scrollToEnd = true;
            }
            float available = Mathf.Max(Chrome + MinConversation, safe.rect.height - Top - Bottom());
            float height = expanded ? available : Mathf.Min(Chrome + Mathf.Max(MinConversation, contentHeight), available);
            if (!Mathf.Approximately(panel.sizeDelta.y, height)) panel.sizeDelta = new Vector2(Width, height);
            if (scrollToEnd) { scroll.verticalNormalizedPosition = 0; scrollToEnd = false; }
        }
    }
}
