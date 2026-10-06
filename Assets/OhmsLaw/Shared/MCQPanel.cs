using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // A multiple-choice question in the lesson panel's MCQ block: the question and 2-4 option rows, each a full-width
    // button at the panel's button height. The caller decides what an answer means and writes it to LearnerSession.
    public class MCQPanel
    {
        const float Badge = 48f, OptionFont = 26f, QuestionFont = 28f, HintFont = 22f;

        // Answer marks on the option tiles (white text stays readable on each).
        public static readonly Color RightMark = new Color(0.2f, 0.5f, 0.25f), WrongMark = new Color(0.5f, 0.2f, 0.2f), PartMark = new Color(0.55f, 0.42f, 0.12f);

        readonly RectTransform block;
        readonly RectTransform panel;
        readonly Image[] options;
        readonly bool[] disabled;
        readonly TextMeshProUGUI question, hint;
        public int Picked { get; private set; } = -1;

        public MCQPanel(string questionText, string[] optionTexts)
        {
            var lesson = LessonPanel.Instance;
            lesson.Clear("MCQ");
            block = lesson.Block("MCQ");
            panel = UI.Rect("MCQ Panel", block, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            UI.Panel(panel, UI.Alpha(Color.Lerp(UI.PanelColor, Color.white, 0.04f), 1f), true, 18f).raycastTarget = false;

            question = UI.Label(panel, questionText, QuestionFont, Color.white, TextAlignmentOptions.TopLeft);
            question.fontStyle = FontStyles.Bold;

            options = new Image[optionTexts.Length];
            disabled = new bool[optionTexts.Length];
            for (int i = 0; i < optionTexts.Length; i++)
            {
                int index = i;
                Split(optionTexts[i], LearnerSession.Letter(i), out var letter, out var text);
                var button = LessonPanel.Button("Option " + LearnerSession.Letter(i), panel, text, () => Picked = index, LessonPanel.ButtonHeight, OptionFont);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.rectTransform.offsetMin = new Vector2(16 + Badge + 14, 6);
                options[i] = button.GetComponent<Image>();
                options[i].color = UI.RaisedColor;
                // The option letter in an accent disc in the row's left gutter.
                var badge = UI.Rect("Letter", button.transform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16 + Badge / 2, 0), new Vector2(Badge, Badge));
                var disc = badge.gameObject.AddComponent<Image>();
                disc.sprite = UI.Circle();
                disc.color = UI.AccentColor;
                disc.raycastTarget = false;
                UI.Label(badge, letter, OptionFont - 2, Color.white);
            }

            hint = UI.Label(panel, "", HintFont, UI.Theme ? UI.Theme.current : Color.yellow, TextAlignmentOptions.TopLeft);
            hint.gameObject.SetActive(false);
            lesson.Show("MCQ", true);
            LessonPanel.Pop(panel);
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

        // Waits for a tap. With ReviewMode.autoAnswer the panel answers itself (autoPick) after the review delay; the delay
        // starts once holdAuto (e.g. "the question is still being spoken") turns false, and never runs while Ask Wattson is
        // open or the learner pressed PAUSE (which also greys the options out). A panel swept away under the wait (the scene
        // was skipped) ends it with no pick.
        public IEnumerator WaitForPick(int autoPick, System.Func<bool> holdAuto = null)
        {
            Picked = -1;
            float waited = 0;
            while (Picked < 0)
            {
                if (!panel) yield break;
                bool paused = ModuleFlow.UserPaused;
                Interactable(!paused);
                if ((holdAuto == null || !holdAuto()) && !AskWattsonUI.IsOpen && !paused) waited += Time.deltaTime;
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay) Picked = Mathf.Max(0, autoPick);
                yield return null;
            }
            Interactable(false);
        }

        void Interactable(bool on)
        {
            for (int i = 0; i < options.Length; i++) if (options[i]) options[i].GetComponent<Button>().interactable = on && !disabled[i];
        }

        public void Mark(int index, Color color) { if (index >= 0 && index < options.Length && options[index]) options[index].color = color; }

        // A tried option keeps its mark and can no longer be picked (retries offer the others).
        public void Disable(int index, Color mark)
        {
            if (index < 0 || index >= options.Length || !options[index]) return;
            disabled[index] = true;
            options[index].color = mark;
            options[index].GetComponent<Button>().interactable = false;
        }

        public void Hint(string text)
        {
            if (!hint) return;
            hint.text = text ?? "";
            hint.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public IEnumerator Shake()
        {
            if (!panel) yield break;
            var home = panel.anchoredPosition;
            for (float t = 0; t < 0.45f; t += Time.deltaTime)
            {
                if (!panel) yield break;
                panel.anchoredPosition = home + Vector2.right * (Mathf.Sin(t * 60f) * 14f * (1 - t / 0.45f));
                yield return null;
            }
            panel.anchoredPosition = home;
        }

        public void Close()
        {
            if (!panel) return;
            var lesson = LessonPanel.Instance;
            if (lesson) lesson.Clear("MCQ");
            else Object.Destroy(panel.gameObject);
        }
    }
}
