using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OhmsLaw
{
    // A multiple-choice question in the lesson panel's MCQ block: the question (quantity words in their colours) and 2-4
    // option rows, each a full-width Raised button 64 px tall with its letter in a badge. Select() marks the pick (paper
    // row, ink text) and steps the others back. The caller decides what an answer means and writes it to LearnerSession.
    public class MCQPanel
    {
        const float Badge = 40f, OptionFont = 22f, QuestionFont = 26f, HintFont = 17f, RowHeight = 64f;

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
            panel = LessonPanel.Stack("MCQ Panel", block, 10f);

            LessonPanel.Eyebrow(panel, "Your prediction").gameObject.name = "Eyebrow";   // the agent reads "Label" as the question
            question = LessonPanel.Text(panel, LessonPanel.Quantities(questionText), QuestionFont, LessonPanel.Paper);
            var gap = UI.Rect("Gap", panel, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            LessonPanel.Fixed(gap, 4f);

            options = new Image[optionTexts.Length];
            disabled = new bool[optionTexts.Length];
            for (int i = 0; i < optionTexts.Length; i++)
            {
                int index = i;
                Split(optionTexts[i], LearnerSession.Letter(i), out var letter, out var text);
                var button = LessonPanel.Pill("Option " + LearnerSession.Letter(i), panel, text, () => Picked = index, LessonPanel.Kind.Raised, null, RowHeight, OptionFont);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                label.gameObject.name = "Text";
                label.textWrappingMode = TextWrappingModes.Normal;
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                options[i] = button.GetComponent<Image>();
                // The option letter in a badge at the row's left.
                var badge = UI.Rect("Letter", button.transform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Badge, Badge));
                badge.SetAsFirstSibling();
                var size = badge.gameObject.AddComponent<LayoutElement>();
                size.preferredWidth = size.minWidth = size.preferredHeight = size.minHeight = Badge;
                var disc = badge.gameObject.AddComponent<Image>();
                disc.sprite = UI.Circle();
                disc.color = new Color(1f, 1f, 1f, 0.10f);
                disc.raycastTarget = false;
                var mark = UI.Label(badge, letter, 18f, LessonPanel.Paper);
                mark.fontStyle = FontStyles.Bold;
            }

            hint = LessonPanel.Text(panel, "", HintFont, LessonPanel.Spark, TextAlignmentOptions.TopLeft);
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

        // Waits for a tap, or the option's number key (1, 2, 3 ...; the letters would clash with the HUD's C for captions).
        // With ReviewMode.autoAnswer the panel answers itself (autoPick) after the review delay; the delay starts once
        // holdAuto (e.g. "the question is still being spoken") turns false, and never runs while Ask Wattson is open or the
        // learner pressed PAUSE (which also greys the options out). A panel swept away under the wait (the scene was
        // skipped) ends it with no pick.
        public IEnumerator WaitForPick(int autoPick, System.Func<bool> holdAuto = null)
        {
            Picked = -1;
            float waited = 0;
            while (Picked < 0)
            {
                if (!panel) yield break;
                bool paused = ModuleFlow.UserPaused;
                Interactable(!paused);
                var keys = Keyboard.current;
                if (keys != null && !paused && !AskWattsonUI.IsOpen)
                    for (int i = 0; i < options.Length && i < 9; i++)
                        if (!disabled[i] && options[i] && (keys[Key.Digit1 + i].wasPressedThisFrame || keys[Key.Numpad1 + i].wasPressedThisFrame))
                            options[i].GetComponent<Button>().onClick.Invoke(); // exactly as a tap: the pick and its click
                if ((holdAuto == null || !holdAuto()) && !AskWattsonUI.IsOpen && !paused) waited += Time.deltaTime;
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay) Picked = Mathf.Max(0, autoPick);
                yield return null;
            }
            Interactable(false);
        }

        void Interactable(bool on)
        {
            for (int i = 0; i < options.Length; i++)
                if (options[i] && options[i].TryGetComponent(out Button button)) button.interactable = on && !disabled[i];
        }

        // The pick: its row turns paper with ink text and an ink badge; the others step back (45 %). Never a right/wrong mark.
        public void Select(int index)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (!options[i]) continue;
                var row = options[i].transform;
                bool chosen = i == index;
                if (row.TryGetComponent(out HudButton hud)) hud.active = chosen;
                if (row.TryGetComponent(out Button button)) button.interactable = true;   // no 36 % dim on the chosen row
                foreach (var label in row.GetComponentsInChildren<TextMeshProUGUI>()) label.color = chosen ? (label.transform.parent.name == "Letter" ? LessonPanel.Paper : LessonPanel.Ink) : LessonPanel.Paper;
                var badge = row.Find("Letter");
                if (badge && badge.TryGetComponent(out Image disc)) disc.color = chosen ? LessonPanel.Ink : new Color(1f, 1f, 1f, 0.10f);
                if (!row.TryGetComponent(out CanvasGroup group)) group = row.gameObject.AddComponent<CanvasGroup>();
                group.alpha = chosen ? 1f : 0.45f;
                group.interactable = false;
            }
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
