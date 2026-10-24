using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OhmsLaw
{
    // A multiple-choice question in the lesson panel's MCQ block: the question (quantity words in their colours) and 2-4
    // option rows, each a full-width Raised button 64 px tall with its letter in a badge. Select() marks the pick
    // (electric-blue row, ink text) and steps the others back. The caller decides what an answer means and writes it to
    // LearnerSession; a detective challenge (SceneDirector.Gates.Challenge) tells the panel the verdict with Mark(), takes
    // a missed row out with Disable(), offers the rest again with Reopen() and shows the answer with Reveal() after a
    // second miss. A caller may instead hand `correctIndex` to the constructor and Select() grades the pick itself. Either
    // way the row turns success green or error red (ink text, a check or a cross), a chip says "Correct!" / "Not quite"
    // and fades after ChipSeconds, and a wrong row shakes. correctIndex -1 (the default) leaves the pick unmarked.
    public class MCQPanel
    {
        const float Badge = 40f, OptionFont = 22f, QuestionFont = 26f, HintFont = 17f, RowHeight = 64f, ChipHeight = 36f;
        public const float ChipSeconds = 1.5f;

        // Answer marks on the option rows (ink text on each): right, wrong, part right.
        public static readonly Color RightMark = LessonPanel.Success, WrongMark = LessonPanel.Error, PartMark = LessonPanel.Gold;

        readonly RectTransform block;
        readonly RectTransform panel;
        readonly Image[] options;
        readonly bool[] disabled;
        readonly TextMeshProUGUI question, hint;
        readonly int correctIndex;
        RectTransform chip;
        Image chipFill, chipIcon;
        TextMeshProUGUI chipText;
        public int Picked { get; private set; } = -1;

        // `eyebrow` names the card ("Detective challenge"); null keeps the old pair, "Quick check" for a "[QC]" question,
        // else "Your prediction".
        public MCQPanel(string questionText, string[] optionTexts, int correctIndex = -1, string eyebrow = null)
        {
            this.correctIndex = correctIndex;
            var lesson = LessonPanel.Instance;
            lesson.Clear("MCQ");
            block = lesson.Block("MCQ");
            panel = LessonPanel.Stack("MCQ Panel", block, 10f);

            // v5: a question tagged "[QC]" is a quick check, not a prediction (the tag never reaches the screen or the agent).
            bool quick = questionText != null && questionText.StartsWith("[QC]");
            if (quick) questionText = questionText.Substring(4).TrimStart();
            LessonPanel.Brackets(block, quick);   // the quick check is a title-bearing card; the prediction and the law tiles are not
            LessonPanel.Eyebrow(panel, eyebrow ?? (quick ? "Quick check" : "Your prediction")).gameObject.name = "Eyebrow";   // the agent reads "Label" as the question
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
                FigmaSkin.Apply(disc, "Icon-choice", false);
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

        // The pick: its row turns electric blue with ink text and an ink badge; the others step back (45 %). A right/wrong
        // mark only when the panel was given the correct option (else the caller's Mark(), or none).
        public void Select(int index)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (!options[i]) continue;
                var row = options[i].transform;
                bool chosen = i == index;
                if (row.TryGetComponent(out HudButton hud)) { hud.active = chosen; if (FigmaSkin.Available) hud.figmaState = chosen ? "Selected" : null; }
                if (row.TryGetComponent(out Button button)) button.interactable = true;   // no 36 % dim on the chosen row
                foreach (var label in row.GetComponentsInChildren<TextMeshProUGUI>()) label.color = FigmaSkin.Available ? LessonPanel.Paper : chosen ? (label.transform.parent.name == "Letter" ? LessonPanel.Paper : LessonPanel.Ink) : LessonPanel.Paper;
                var badge = row.Find("Letter");
                if (badge && badge.TryGetComponent(out Image disc)) disc.color = chosen ? LessonPanel.Ink : new Color(1f, 1f, 1f, 0.10f);
                if (!row.TryGetComponent(out CanvasGroup group)) group = row.gameObject.AddComponent<CanvasGroup>();
                group.alpha = chosen ? 1f : 0.45f;
                group.interactable = false;
            }
            if (correctIndex >= 0 && index >= 0) Mark(index, index == correctIndex ? RightMark : WrongMark);
        }

        // The verdict on a row: it takes the mark's colour with ink text, a chip says so ("Correct!" with a check, "Not
        // quite" with a cross, "Nearly" for a part mark) and fades after ChipSeconds, and a wrong row shakes (fades only
        // under reduced motion). The fill goes through the row's HudButton, which otherwise eases it back every frame.
        public void Mark(int index, Color color)
        {
            if (index < 0 || index >= options.Length || !options[index]) return;
            Fill(index, color);
            bool right = color == RightMark, part = color == PartMark;
            Chip(right ? "Correct!" : part ? "Nearly" : "Not quite", color, right ? "check" : "x");
            UiSounds.Play(right ? "correct" : part ? "done" : "wrong");   // SFX_Correct_Answer, or the gentle UI_Error; the gate's Done() of the same frame stays quiet
            if (!right && !part && !ReducedMotion.On && LessonPanel.Instance) LessonPanel.Instance.StartCoroutine(Shake());
        }

        void Fill(int index, Color color)
        {
            var row = options[index].transform;
            if (row.TryGetComponent(out HudButton hud))
            {
                hud.fillColor = hud.activeFill = color;
                hud.outlineColor = hud.activeOutline = color;
                hud.hoverFill = Color.clear;
                if (FigmaSkin.Available) hud.figmaState = color == RightMark ? "Correct" : color == WrongMark ? "Incorrect" : "Selected";
            }
            options[index].color = color;
            if (FigmaSkin.Available) FigmaSkin.Apply(options[index], color == RightMark ? "RaisedCorrect" : color == WrongMark ? "RaisedIncorrect" : "RaisedSelected");
            foreach (var label in row.GetComponentsInChildren<TextMeshProUGUI>()) label.color = FigmaSkin.Available ? LessonPanel.Paper : label.transform.parent.name == "Letter" ? LessonPanel.Paper : LessonPanel.Ink;
            var badge = row.Find("Letter");
            if (badge && badge.TryGetComponent(out Image disc)) disc.color = LessonPanel.Ink;
            if (FigmaSkin.Available && badge)
            {
                FigmaSkin.Apply(badge.GetComponent<Image>(), color == RightMark ? "Icon-choice-correct" : "Icon-choice-incorrect", false);
                foreach (var label in badge.GetComponentsInChildren<TextMeshProUGUI>()) label.gameObject.SetActive(false);
            }
        }

        // The verdict chip under the options: a hugging pill in the mark's colour, an icon and a word in ink.
        void Chip(string text, Color color, string icon)
        {
            if (!panel) return;
            if (!chip)
            {
                var holder = LessonPanel.Row("Chip", panel, 0f);
                LessonPanel.Fixed(holder, ChipHeight);
                chip = LessonPanel.Row("Pill", holder, 8f);
                chip.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(14, 16, 0, 0);
                LessonPanel.Fixed(chip, ChipHeight);
                chipFill = UI.Panel(chip, color, true, ChipHeight / 2f);
                chipFill.raycastTarget = false;
                chipIcon = LessonPanel.Icon(chip, icon, 16f, LessonPanel.Ink);
                chipText = LessonPanel.Text(chip, text, 16f, LessonPanel.Ink, TextAlignmentOptions.MidlineLeft);
                chipText.textWrappingMode = TextWrappingModes.NoWrap;
                chip.gameObject.AddComponent<ChipFade>();
            }
            chipFill.color = color;
            chipIcon.sprite = Icons.Get(icon);
            chipText.text = text;
            chip.GetComponent<ChipFade>().Restart();   // no sound of its own: Mark() plays the verdict
        }

        // A tried option keeps its mark and can no longer be picked (retries offer the others).
        public void Disable(int index, Color mark)
        {
            if (index < 0 || index >= options.Length || !options[index]) return;
            disabled[index] = true;
            Fill(index, mark);
            options[index].GetComponent<Button>().interactable = false;
        }

        // A retry: the rows not yet tried come back to full strength and take taps again (the tried ones keep their mark).
        public void Reopen()
        {
            Picked = -1;
            for (int i = 0; i < options.Length; i++)
            {
                if (!options[i] || disabled[i]) continue;
                var row = options[i].transform;
                if (row.TryGetComponent(out HudButton hud)) { hud.active = false; hud.figmaState = null; }
                foreach (var label in row.GetComponentsInChildren<TextMeshProUGUI>()) label.color = LessonPanel.Paper;
                var badge = row.Find("Letter");
                if (badge && badge.TryGetComponent(out Image disc)) disc.color = new Color(1f, 1f, 1f, 0.10f);
                if (FigmaSkin.Available && badge)
                {
                    FigmaSkin.Apply(badge.GetComponent<Image>(), "Icon-choice", false);
                    foreach (var label in badge.GetComponentsInChildren<TextMeshProUGUI>(true)) label.gameObject.SetActive(true);
                }
                if (row.TryGetComponent(out CanvasGroup group)) { group.alpha = 1f; group.interactable = true; }
                if (row.TryGetComponent(out Button button)) button.interactable = true;
            }
        }

        // The answer after two misses: the right row turns green with the chip's own word, no verdict sound.
        public void Reveal(int index)
        {
            if (index < 0 || index >= options.Length || !options[index]) return;
            Fill(index, RightMark);
            Chip("Here's the answer", RightMark, "check");
        }

        public void Hint(string text)
        {
            if (!hint) return;
            hint.text = text ?? "";
            hint.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        // A wrong pick: the panel shakes sideways for 0.45 s (nothing under reduced motion).
        public IEnumerator Shake()
        {
            if (!panel || ReducedMotion.On) yield break;
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

    // The verdict chip's life: shown at once, held for MCQPanel.ChipSeconds, then a 0.3 s fade (a cut under reduced
    // motion) and gone. Unscaled time: the Timeline is held at the gate.
    public class ChipFade : MonoBehaviour
    {
        const float FadeSeconds = 0.3f;
        CanvasGroup group;
        float t;

        public void Restart()
        {
            if (!group && !TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
            t = 0f;
            group.alpha = 1f;
            gameObject.SetActive(true);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t < MCQPanel.ChipSeconds) return;
            float alpha = ReducedMotion.On ? 0f : 1f - Mathf.Clamp01((t - MCQPanel.ChipSeconds) / FadeSeconds);
            group.alpha = alpha;
            if (alpha <= 0f) gameObject.SetActive(false);
        }
    }
}
