using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Top-right playback controls: pause, replay line, subtitles, volume and (long-press) skip, always visible (HideAfter)
    // except while Ask Wattson is open; the reduced-motion toggle sits bottom-left so the row keeps the reference width
    // (world cards such as the wire counter are laid out against x >= 1094 px). Toggles say their state in words.
    public class CinematicControlsUI : MonoBehaviour
    {
        const float HideAfter = float.PositiveInfinity, SkipHold = 1f, ButtonWidth = 150f; // always visible, like the reference HUD

        CanvasGroup group;
        RectTransform volumePanel;
        Button replay;
        TextMeshProUGUI pauseLabel, subtitleLabel, skipLabel, motionLabel;
        float lastActivity, skipPressed = -1;

        void Awake()
        {
            var root = UI.Stretch("Controls", transform);
            group = root.gameObject.AddComponent<CanvasGroup>();

            // Reduced motion (ReducedMotion.On): fewer flashes, no camera shake, calmer particles, no sliding panels.
            motionLabel = Add(root, 0, "MOTION\nFULL", () => ReducedMotion.On = !ReducedMotion.On);
            motionLabel.fontSize = 22;
            motionLabel.transform.parent.name = "Reduced Motion";
            var motion = (RectTransform)motionLabel.transform.parent;
            motion.anchorMin = motion.anchorMax = motion.pivot = Vector2.zero; // bottom-left corner, clear of the subtitle box (x >= 210)
            motion.anchoredPosition = new Vector2(36, 36);
            pauseLabel = Add(root, 4, "PAUSE", TogglePause);
            // Not during a gate: its lines are spoken outside the Timeline, so a replay would only rewind the paused Timeline
            // to the last scripted line while the gate keeps running.
            replay = Add(root, 3, "REPLAY", () => { if (SceneDirector.Gate == null && SubtitleTrack.LastLineStart >= 0) ModuleFlow.Instance.JumpTo(SubtitleTrack.LastLineStart); })
                .GetComponentInParent<Button>();
            subtitleLabel = Add(root, 2, "CC ON", () => SubtitleUI.Enabled = !SubtitleUI.Enabled);
            Add(root, 1, "VOLUME", () => volumePanel.gameObject.SetActive(!volumePanel.gameObject.activeSelf));
            skipLabel = Add(root, 0, "SKIP", () => { if (ModuleFlow.Instance.debugMode) ModuleFlow.Instance.CompleteScene(); });
            var hold = skipLabel.transform.parent.gameObject.AddComponent<EventTrigger>();
            On(hold, EventTriggerType.PointerDown, () => skipPressed = Time.unscaledTime);
            On(hold, EventTriggerType.PointerUp, () => skipPressed = -1);

            volumePanel = UI.Rect("Volume", root, Vector2.one, Vector2.one, new Vector2(-36, -136), new Vector2(520, 300));
            UI.Panel(volumePanel, UI.Theme ? UI.Theme.panel : Color.black);
            string[] names = { "Wattson", "Effects", "Music" };
            for (int i = 0; i < 3; i++)
            {
                var kind = (AudioBus.Kind)i;
                var label = UI.Label(volumePanel, names[i], 28, Color.white, TextAlignmentOptions.MidlineLeft);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0, 1);
                label.rectTransform.anchoredPosition = new Vector2(28, -18 - i * 92);
                label.rectTransform.sizeDelta = new Vector2(150, UI.MinTarget);
                Slider(volumePanel, new Vector2(190, -18 - i * 92), AudioBus.Get(kind), value => AudioBus.Set(kind, value));
            }
            volumePanel.gameObject.SetActive(false);
        }

        static TextMeshProUGUI Add(Transform root, int slot, string text, UnityAction onClick)
        {
            var button = UI.Button(text, root, Vector2.one, Vector2.one, new Vector2(-36 - slot * (ButtonWidth + 10), -36), new Vector2(ButtonWidth, UI.MinTarget), text, onClick, 26);
            return button.GetComponentInChildren<TextMeshProUGUI>();
        }

        public static void On(EventTrigger trigger, EventTriggerType type, UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        static void Slider(Transform parent, Vector2 position, float value, UnityAction<float> onChange)
        {
            var rect = UI.Rect("Slider", parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(300, UI.MinTarget));
            var slider = rect.gameObject.AddComponent<Slider>();
            var track = UI.Stretch("Track", rect);
            track.offsetMin = new Vector2(0, 37);
            track.offsetMax = new Vector2(0, -37);
            UI.Panel(track, new Color(1, 1, 1, 0.25f));
            var fill = UI.Stretch("Fill", track);
            UI.Panel(fill, UI.Theme ? UI.Theme.wattsonAccent : Color.cyan);
            var handleArea = UI.Stretch("Handle Area", rect);
            handleArea.offsetMin = new Vector2(22, 0);
            handleArea.offsetMax = new Vector2(-22, 0);
            var handle = UI.Rect("Handle", handleArea, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 0));
            var knob = handle.gameObject.AddComponent<Image>();
            if (UI.Theme) knob.sprite = UI.Theme.circle;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.value = value;
            slider.onValueChanged.AddListener(onChange);
        }

        void TogglePause()
        {
            var flow = ModuleFlow.Instance;
            flow.SetPaused(ModuleFlow.PauseReason.User, !flow.IsPaused(ModuleFlow.PauseReason.User));
        }

        void Update()
        {
            var flow = ModuleFlow.Instance;
            bool userPaused = flow.IsPaused(ModuleFlow.PauseReason.User);
            pauseLabel.text = userPaused ? "PLAY" : "PAUSE";
            subtitleLabel.text = SubtitleUI.Enabled ? "CC ON" : "CC OFF";
            motionLabel.text = ReducedMotion.On ? "MOTION\nREDUCED" : "MOTION\nFULL";
            if (replay.interactable != (SceneDirector.Gate == null)) replay.interactable = SceneDirector.Gate == null;

            // Long-press guards Skip so a stray tap cannot jump the scene.
            skipLabel.text = skipPressed >= 0 ? "HOLD..." : "SKIP";
            if (skipPressed >= 0 && Time.unscaledTime - skipPressed > SkipHold) { skipPressed = -1; flow.CompleteScene(); }

            var pointer = Pointer.current;
            if (pointer != null && (pointer.press.isPressed || pointer.delta.ReadValue().sqrMagnitude > 0)) lastActivity = Time.unscaledTime;
            bool show = !AskWattsonUI.IsOpen && (userPaused || volumePanel.gameObject.activeSelf || Time.unscaledTime - lastActivity < HideAfter);
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1 : 0, Time.unscaledDeltaTime * 4f);
            group.blocksRaycasts = group.alpha > 0.5f;
        }
    }
}
