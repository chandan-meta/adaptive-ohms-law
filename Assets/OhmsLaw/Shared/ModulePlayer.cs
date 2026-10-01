using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Start screen: the scene list (04-13), Play all, jump to a scene, and the review toggles.
    public class ModulePlayer : MonoBehaviour
    {
        [System.Serializable] public struct Entry { public string scene, title; }
        public Entry[] entries;

        TextMeshProUGUI autoLabel, speedLabel, subtitleLabel;

        void Start()
        {
            var root = (RectTransform)transform;
            UI.Panel(root, new Color(0.05f, 0.07f, 0.1f), false);
            var title = UI.Label(UI.Rect("Title", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1600, 90)), "Ohm's Law — module player", 56, UI.Theme ? UI.Theme.wattsonAccent : Color.cyan);
            title.fontStyle = FontStyles.Bold;
            UI.Label(UI.Rect("Sub", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1600, 50)), "Scenes 1-3 and 14 onward are videos. Scenes 4-13 play here.", 26, new Color(1, 1, 1, 0.7f));

            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                int column = i / 5, row = i % 5;
                UI.Button(e.scene, root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(80 + column * 760, -200 - row * 104), new Vector2(720, UI.MinTarget),
                    e.scene.Substring(5, 2) + "   " + e.title, () => ModuleFlow.Instance.JumpToScene(e.scene), 30)
                    .GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            }

            var play = UI.Button("Play All", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(80, 80), new Vector2(420, 110), "▶  PLAY ALL (from Scene 4)", () => ModuleFlow.Instance.JumpToScene(entries[0].scene), 34);
            play.GetComponent<Image>().color = new Color(0.16f, 0.36f, 0.6f);
            autoLabel = Toggle(1, () => ReviewMode.autoAnswer = !ReviewMode.autoAnswer);
            speedLabel = Toggle(2, () => ReviewMode.timeScale = ReviewMode.timeScale > 1 ? 1 : 2);
            subtitleLabel = Toggle(3, () => SubtitleUI.Enabled = !SubtitleUI.Enabled);
            UI.Label(UI.Rect("Hint", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80, 40), new Vector2(900, 40)), "F1 or a three-finger tap opens the review overlay during a scene.", 22, new Color(1, 1, 1, 0.55f), TextAlignmentOptions.MidlineRight);
        }

        TextMeshProUGUI Toggle(int slot, UnityEngine.Events.UnityAction onClick) =>
            UI.Button("Toggle " + slot, transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(80 + slot * 440, 80), new Vector2(420, 110), "", onClick, 28).GetComponentInChildren<TextMeshProUGUI>();

        void Update()
        {
            autoLabel.text = "AUTO ANSWER: " + (ReviewMode.autoAnswer ? "ON" : "OFF");
            speedLabel.text = "SPEED: " + ReviewMode.timeScale + "×";
            subtitleLabel.text = "SUBTITLES: " + (SubtitleUI.Enabled ? "ON" : "OFF");
        }
    }
}
