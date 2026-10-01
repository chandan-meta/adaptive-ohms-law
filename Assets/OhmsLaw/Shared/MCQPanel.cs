using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Multiple-choice question on the module canvas: question text and 2-4 option buttons, each at least 88 px tall.
    // The caller decides what an answer means and writes it to LearnerSession.
    public class MCQPanel
    {
        const float Width = 1180f, Row = UI.MCQRow, Pad = 40f, Top = 36f, Gap = 24f;
        const float OptionHeight = 92f, Badge = 60f, HintBand = 60f;

        readonly RectTransform panel;
        readonly Image[] options;
        readonly TextMeshProUGUI question;
        readonly TextMeshProUGUI hint;
        public int Picked { get; private set; } = -1;

        public MCQPanel(string questionText, string[] optionTexts)
        {
            var root = ModuleFlow.Instance.transform.Find("SafeArea");
            panel = UI.Rect("MCQ Panel", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(Width, 100));
            UI.Panel(panel, UI.HudColor, true, 28f);
            panel.gameObject.AddComponent<UIFade>();

            // The question block takes the height its text needs (1-3 lines), the options follow below it.
            question = UI.Label(panel, questionText, UI.MCQQuestionFont, Color.white, TextAlignmentOptions.Top);
            float questionHeight = Mathf.Ceil(Mathf.Max(question.GetPreferredValues(questionText, Width - 2 * Pad, 0).y, UI.MCQQuestionFont * 1.4f));
            question.rectTransform.anchorMin = question.rectTransform.anchorMax = question.rectTransform.pivot = new Vector2(0.5f, 1);
            question.rectTransform.anchoredPosition = new Vector2(0, -Top);
            question.rectTransform.sizeDelta = new Vector2(Width - 2 * Pad, questionHeight);

            float optionsTop = Top + questionHeight + Gap;
            options = new Image[optionTexts.Length];
            for (int i = 0; i < optionTexts.Length; i++)
            {
                int index = i;
                Split(optionTexts[i], LearnerSession.Letter(i), out var letter, out var text);
                var button = UI.Button("Option " + LearnerSession.Letter(i), panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                    new Vector2(0, -(optionsTop + i * Row)), new Vector2(Width - 2 * Pad, OptionHeight), text, () => Picked = index, UI.MCQOptionFont);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.rectTransform.offsetMin = new Vector2(16 + Badge + 20, 6);
                options[i] = button.GetComponent<Image>();
                options[i].color = UI.RaisedColor;

                // The option letter in an accent disc at the left edge of the row.
                var badge = UI.Rect("Letter", button.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16 + Badge / 2, 0), new Vector2(Badge, Badge));
                var disc = badge.gameObject.AddComponent<Image>();
                disc.sprite = UI.Circle();
                disc.color = UI.AccentColor;
                disc.raycastTarget = false;
                UI.Label(badge, letter, UI.MCQOptionFont - 2, UI.Alpha(UI.PanelColor, 1f));
            }

            float height = optionsTop + (optionTexts.Length - 1) * Row + OptionHeight + HintBand + 8;
            panel.sizeDelta = new Vector2(Width, height);
            hint = UI.Label(panel, "", UI.MCQHintFont, UI.Theme ? UI.Theme.current : Color.yellow, TextAlignmentOptions.Bottom);
            hint.rectTransform.anchorMin = new Vector2(0, 0);
            hint.rectTransform.anchorMax = new Vector2(1, 0);
            hint.rectTransform.offsetMin = new Vector2(Pad, 12);
            hint.rectTransform.offsetMax = new Vector2(-Pad, HintBand);
        }

        // "A. Increase" -> badge "A", row "Increase"; options without a letter prefix ("0.2") keep their text and
        // take the badge letter from their index.
        static void Split(string option, string fallbackLetter, out string letter, out string text)
        {
            if (option.Length > 3 && char.IsLetter(option[0]) && char.IsUpper(option[0]) && option[1] == '.' && option[2] == ' ')
            {
                letter = option.Substring(0, 1);
                text = option.Substring(3).TrimStart();
            }
            else { letter = fallbackLetter; text = option; }
        }

        // Waits for a tap. With ReviewMode.autoAnswer the panel answers itself (autoPick) after the review delay.
        public IEnumerator WaitForPick(int autoPick)
        {
            Picked = -1;
            Interactable(true);
            float waited = 0;
            while (Picked < 0)
            {
                waited += Time.deltaTime;
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay) Picked = Mathf.Max(0, autoPick);
                yield return null;
            }
            Interactable(false);
        }

        void Interactable(bool on)
        {
            foreach (var option in options) option.GetComponent<Button>().interactable = on;
        }

        public void Mark(int index, Color color) { if (index >= 0 && index < options.Length) options[index].color = color; }

        public void ClearMarks()
        {
            foreach (var option in options) option.color = UI.RaisedColor;
        }

        public void Hint(string text) => hint.text = text;

        public IEnumerator Shake()
        {
            var home = panel.anchoredPosition;
            for (float t = 0; t < 0.45f; t += Time.deltaTime)
            {
                panel.anchoredPosition = home + Vector2.right * (Mathf.Sin(t * 60f) * 22f * (1 - t / 0.45f));
                yield return null;
            }
            panel.anchoredPosition = home;
        }

        // Fades out, then the panel destroys itself.
        public void Close() { if (panel) panel.GetComponent<UIFade>().Dismiss(); }
    }
}
