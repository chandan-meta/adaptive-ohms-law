using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OhmsLaw
{
    // Review HUD, toggled with F1 or a three-finger tap: scene, line, time, FPS and quick flow controls.
    public class FlowOverlay : MonoBehaviour
    {
        const float Font = 26f, ValueColumn = 150f; // readout column starts here (reference px into the text rect)

        RectTransform root;
        TextMeshProUGUI info, autoLabel, speedLabel;
        float fps = 60;

        void Awake()
        {
            root = UI.Rect("Flow Overlay", transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(560, 460));
            UI.Panel(root, new Color(0, 0, 0, 0.72f), true, 20f);
            info = UI.Label(root, "", Font, Color.white, TextAlignmentOptions.TopLeft);
            info.rectTransform.offsetMin = new Vector2(24, 0);
            info.rectTransform.offsetMax = new Vector2(-24, -20);
            info.lineSpacing = 8f;
            Button(0, 0, "NEXT SCENE", LessonPanel.Skip);
            // Not during a gate (CinematicControlsUI REPLAY): the Gate row shows why.
            Button(1, 0, "REPLAY LINE", () => { if (SceneDirector.Gate == null && SubtitleTrack.LastLineStart >= 0) ModuleFlow.Instance.JumpTo(SubtitleTrack.LastLineStart); });
            autoLabel = Button(0, 1, "", () => ReviewMode.autoAnswer = !ReviewMode.autoAnswer);
            speedLabel = Button(1, 1, "", () => ReviewMode.timeScale = ReviewMode.timeScale > 1 ? 1 : 2);
            root.gameObject.SetActive(false);
        }

        TextMeshProUGUI Button(int column, int row, string text, UnityEngine.Events.UnityAction onClick) =>
            UI.Button(text, root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20 + column * 270, 20 + row * 100), new Vector2(250, UI.MinTarget), text, onClick, Font)
                .GetComponentInChildren<TextMeshProUGUI>();

        // Two aligned columns: dim key, value tabbed to a fixed x; numeric values monospaced so time and FPS do not jitter.
        static string Row(string key, string value, bool mono = false) =>
            $"<alpha=#99>{key}<alpha=#FF><pos={ValueColumn}>" + (mono ? $"<mspace=0.6em>{value}</mspace>" : value);

        void Update()
        {
            var keyboard = Keyboard.current;
            var touch = Touchscreen.current;
            bool threeFingers = false;
            if (touch != null)
            {
                int down = 0;
                foreach (var t in touch.touches) if (t.press.isPressed) down++;
                threeFingers = down >= 3 && touch.touches[2].press.wasPressedThisFrame;
            }
            if ((keyboard != null && keyboard.f1Key.wasPressedThisFrame) || threeFingers) root.gameObject.SetActive(!root.gameObject.activeSelf);

            fps = Mathf.Lerp(fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
            if (!root.gameObject.activeSelf) return;
            var director = ModuleFlow.Instance.Director;
            var line = SceneDirector.Speaking ? SceneDirector.Speaking : SubtitleTrack.CurrentLine;
            info.text = Row("Scene", SceneManager.GetActiveScene().name) + "\n" +
                        Row("Line", line ? line.id : "-") + "\n" +
                        Row("Gate", SceneDirector.Gate ?? "-") + "\n" +
                        Row("Time", $"{(director ? director.time : 0):0.0} / {(director ? director.duration : 0):0.0} s", true) + "\n" +
                        Row("FPS", $"{fps:0}", true);
            autoLabel.text = ReviewMode.autoAnswer ? "AUTO ANSWER ON" : "AUTO ANSWER OFF";
            speedLabel.text = "SPEED " + ReviewMode.timeScale + "x";
        }
    }
}
