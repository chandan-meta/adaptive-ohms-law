using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OhmsLaw
{
    // The floating ASK WATTSON button and its chat panel: hold-to-talk, text, suggested questions, speech bubbles.
    public class AskWattsonUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        const float Width = 640f, Height = 1016f, MaxBubbles = 4;
        const string Hint = "Hold the mic to talk, type, or tap a question.";

        WattsonAgent agent;
        RectTransform askButton, panel, bubbles, chips;
        TMP_InputField input;
        TextMeshProUGUI status, lastBubble;
        Image micImage;
        GameObject stopButton;
        string lastRole, lastText = "";
        double resumeTime = -1;

        static ModuleFlow Flow => ModuleFlow.Instance;
        static SceneAgentContext Context => WattsonController.Active ? WattsonController.Active.agentContext : null;

        void Awake()
        {
            agent = Flow.Agent;
            BuildAskButton();
            BuildPanel();
            agent.BubbleChanged += OnBubble;
            agent.StateChanged += OnState;
            agent.Notice += message => status.text = message;
            agent.LearnerQuestion += ShowOptions;
            SceneManager.sceneLoaded += (_, _) => { resumeTime = -1; if (IsOpen) Close(); };
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
            panel = UI.Rect("Ask Panel", transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(Width + 60, 0), new Vector2(Width, Height));
            UI.Panel(panel, UI.Theme ? UI.Theme.panel : Color.black);

            var title = UI.Label(Row("Header", -16, 60), "ASK WATTSON", 34, accent, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;

            bubbles = Row("Bubbles", -88, 372);
            bubbles.gameObject.AddComponent<RectMask2D>();
            var layout = bubbles.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            chips = Row("Chips", -472, 284);
            status = UI.Label(Row("Status", -762, 40), Hint, 22, new Color(1, 1, 1, 0.7f), TextAlignmentOptions.MidlineLeft);

            var inputRow = Row("Input", -808, 96);
            var mic = UI.Button("Mic", inputRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(96, 96), "MIC", null, 24);
            micImage = mic.GetComponent<Image>();
            micImage.sprite = UI.Theme ? UI.Theme.circle : null;
            micImage.type = Image.Type.Simple;
            micImage.color = new Color(0.16f, 0.36f, 0.6f);
            var hold = mic.gameObject.AddComponent<EventTrigger>();
            CinematicControlsUI.On(hold, EventTriggerType.PointerDown, agent.StartListening);
            CinematicControlsUI.On(hold, EventTriggerType.PointerUp, agent.StopListening);

            var field = UI.Rect("Question", inputRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(108, 0), new Vector2(364, 88));
            UI.Panel(field, new Color(1, 1, 1, 0.12f));
            input = field.gameObject.AddComponent<TMP_InputField>();
            var area = UI.Stretch("Text Area", field);
            area.offsetMin = new Vector2(16, 8);
            area.offsetMax = new Vector2(-16, -8);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = UI.Label(area, "Type a question...", 28, new Color(1, 1, 1, 0.45f), TextAlignmentOptions.MidlineLeft);
            var text = UI.Label(area, "", 28, Color.white, TextAlignmentOptions.MidlineLeft);
            placeholder.textWrappingMode = text.textWrappingMode = TextWrappingModes.NoWrap;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 300;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.onSubmit.AddListener(Send);
            UI.Button("Send", inputRow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(116, 88), "SEND", () => Send(input.text), 26);

            var bottom = Row("Bottom", -912, 88);
            UI.Button("Back", bottom, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(420, 88), "Back to the case " + UI.Glyph("▶", ">"), Close, 30)
                .GetComponent<Image>().color = new Color(0.16f, 0.36f, 0.6f);
            stopButton = UI.Button("Stop", bottom, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(160, 88), "STOP", agent.Stop, 28).gameObject;
            stopButton.SetActive(false);
            panel.gameObject.SetActive(false);
        }

        RectTransform Row(string name, float y, float height) =>
            UI.Rect(name, panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(Width - 40, height));

        void Open()
        {
            IsOpen = true;
            // Coming back restarts the scripted line that was interrupted, if there was one.
            resumeTime = SubtitleTrack.CurrentLine ? SubtitleTrack.LastLineStart : -1;
            Flow.SetPaused(ModuleFlow.PauseReason.Ask, true);
            agent.Engage(true);
            if (SubtitleUI.Instance) SubtitleUI.Instance.RightInset = Width + 72;
            panel.gameObject.SetActive(true);
            status.text = Hint;
            ShowChips();
        }

        void Close()
        {
            IsOpen = false;
            agent.Engage(false);
            if (SubtitleUI.Instance) SubtitleUI.Instance.RightInset = 0;
            if (resumeTime >= 0) Flow.JumpTo(resumeTime);
            Flow.SetPaused(ModuleFlow.PauseReason.Ask, false);
        }

        void Send(string question)
        {
            if (string.IsNullOrWhiteSpace(question)) return;
            input.text = "";
            agent.Ask(question);
        }

        void ShowChips()
        {
            var faq = Context ? Context.faq : null;
            var questions = new List<string>();
            if (faq) foreach (var entry in faq.entries) if (questions.Count < 3) questions.Add(entry.question);
            FillChips(questions, index => agent.Ask(questions[index]));
        }

        // Deterministic multiple-choice from AskLearner replaces the chips until the student picks.
        void ShowOptions(string[] options, Action<int> pick)
        {
            if (!IsOpen) Open();
            FillChips(options, index => { ShowChips(); pick(index); });
        }

        void FillChips(IList<string> labels, Action<int> onPick)
        {
            foreach (Transform child in chips) Destroy(child.gameObject);
            for (int i = 0; i < labels.Count && i < 3; i++)
            {
                int index = i;
                var chip = UI.Button("Chip", chips, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -i * 98), new Vector2(Width - 40, 88), labels[i], () => onPick(index), 28);
                chip.GetComponent<Image>().color = new Color(1, 1, 1, 0.14f);
            }
        }

        void OnBubble(string role, string text)
        {
            // Streaming answers grow the newest bubble instead of adding one per chunk.
            if (lastBubble && role == lastRole && text.StartsWith(lastText))
            {
                lastBubble.text = lastText = text;
                LayoutRebuilder.ForceRebuildLayoutImmediate(bubbles);
                return;
            }
            bool user = role == "user";
            var bubble = new GameObject("Bubble", typeof(RectTransform)).GetComponent<RectTransform>();
            bubble.SetParent(bubbles, false);
            var tint = UI.Theme ? (user ? UI.Theme.voltage : UI.Theme.wattsonAccent) : Color.white;
            UI.Panel(bubble, new Color(tint.r, tint.g, tint.b, user ? 0.3f : 0.16f)).raycastTarget = false;
            var layout = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false; // otherwise each bubble claims a share of the empty space
            lastBubble = UI.Label(bubble, text, 26, Color.white, user ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);
            lastRole = role;
            lastText = text;
            if (bubbles.childCount > MaxBubbles) Destroy(bubbles.GetChild(0).gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubbles); // text height depends on the width the layout just gave it
        }

        void OnState(WattsonAgent.State state)
        {
            status.text = state == WattsonAgent.State.Listening ? "Listening... let go to send." :
                          state == WattsonAgent.State.Thinking ? "Wattson is thinking..." :
                          state == WattsonAgent.State.Talking ? "" :
                          agent.Offline ? "Wattson's radio is fuzzy. Tap a clue above!" : Hint;
            stopButton.SetActive(state == WattsonAgent.State.Thinking || state == WattsonAgent.State.Talking);
            micImage.color = state == WattsonAgent.State.Listening ? new Color(0.9f, 0.3f, 0.25f) : new Color(0.16f, 0.36f, 0.6f);
        }

        void Update()
        {
            var context = Context;
            askButton.gameObject.SetActive(!IsOpen && context && context.askWattsonEnabled);

            float target = IsOpen ? -28 : Width + 60;
            float x = Mathf.Lerp(panel.anchoredPosition.x, target, 1 - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            panel.anchoredPosition = new Vector2(x, 0);
            if (!IsOpen && x > Width + 40) panel.gameObject.SetActive(false);

            // Spacebar is push-to-talk on PC, unless the student is typing.
            var keyboard = Keyboard.current;
            if (!IsOpen || keyboard == null || input.isFocused) return;
            if (keyboard.spaceKey.wasPressedThisFrame) agent.StartListening();
            if (keyboard.spaceKey.wasReleasedThisFrame) agent.StopListening();
        }
    }
}
