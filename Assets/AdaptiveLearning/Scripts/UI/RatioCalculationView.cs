using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public class RatioCalculationView : MonoBehaviour
    {
        [System.Serializable]
        public class TableRowUI
        {
            public Text voltageText;
            public Text currentText;
            public Text ratioText;
            public GameObject rowHighlight;
        }

        [SerializeField] TableRowUI[] rows;
        [SerializeField] Text sourceLabel;
        [SerializeField] Text promptText;
        [SerializeField] GameObject optionsGroup;
        [SerializeField] Button[] optionButtons;
        [SerializeField] Text[] optionTexts;

        [Header("Question UI")]
        [SerializeField] GameObject questionGroup;
        [SerializeField] Text questionPromptText;
        [SerializeField] Button[] questionButtons;
        [SerializeField] Text[] questionButtonTexts;
        [SerializeField] Text feedbackText;

        [Header("Reveal Banner")]
        [SerializeField] GameObject formulaRevealBanner;
        [SerializeField] Text formulaRevealText;

        public event Action<bool> CalculationAnswered;
        public event Action<bool> RatioObservationAnswered;
        public event Action RatioStageCompleted;

        List<ExperimentObservation> observations = new List<ExperimentObservation>();
        int currentStep = 0; // 0 = row 1 calc, 1 = row 2 calc, 2 = all rows revealed, 3 = question answered
        int mistakeCount = 0;

        void Awake()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
            }

            for (int i = 0; i < questionButtons.Length; i++)
            {
                int index = i;
                questionButtons[i].onClick.AddListener(() => OnQuestionSelected(index));
            }
        }

        public void Initialize(List<ExperimentObservation> data, bool isPracticeData)
        {
            observations = data != null && data.Count == 5 ? data : CreateDefaultData();
            currentStep = 0;
            mistakeCount = 0;

            if (sourceLabel != null)
                sourceLabel.text = isPracticeData ? "PRACTICE DATA  •  5.0 Ω Wire XY" : "YOUR MEASUREMENTS  •  5.0 Ω Wire XY";

            if (formulaRevealBanner != null)
                formulaRevealBanner.SetActive(false);
            if (questionGroup != null)
                questionGroup.SetActive(false);
            if (feedbackText != null)
                feedbackText.text = "";

            // Populate table rows
            for (int i = 0; i < rows.Length && i < observations.Count; i++)
            {
                if (rows[i].voltageText != null)
                    rows[i].voltageText.text = observations[i].voltage.ToString("0.0") + " V";
                if (rows[i].currentText != null)
                    rows[i].currentText.text = observations[i].current.ToString("0.00") + " A";
                if (rows[i].ratioText != null)
                    rows[i].ratioText.text = "—";
                if (rows[i].rowHighlight != null)
                    HighlightRow(i, false);
            }

            ShowStep0();
        }

        static List<ExperimentObservation> CreateDefaultData()
        {
            var list = new List<ExperimentObservation>();
            for (int i = 1; i <= 5; i++)
            {
                list.Add(new ExperimentObservation { voltage = i * 1.0f, current = i * 0.20f });
            }
            return list;
        }

        void HighlightRow(int index, bool highlighted)
        {
            // The row background contains its labels; hiding it also hides the measurements.
            var row = rows[index].rowHighlight;
            row.SetActive(true);
            var graphic = row.GetComponent<Image>();
            if (graphic != null) graphic.color = highlighted ? WarmLabTheme.Amber : WarmLabTheme.Paper;
        }

        void ShowStep0()
        {
            currentStep = 0;
            if (rows.Length > 0 && rows[0].rowHighlight != null)
                HighlightRow(0, true);

            if (promptText != null)
                promptText.text = "Calculate V ÷ I for reading 1:  1.0 V ÷ 0.20 A = ?";

            optionsGroup.SetActive(true);
            SetOptionTexts("2 Ω", "5 Ω", "10 Ω");
        }

        void ShowStep1()
        {
            currentStep = 1;
            if (rows.Length > 0 && rows[0].rowHighlight != null)
                HighlightRow(0, false);
            if (rows.Length > 1 && rows[1].rowHighlight != null)
                HighlightRow(1, true);

            if (promptText != null)
                promptText.text = "Calculate V ÷ I for reading 2:  2.0 V ÷ 0.40 A = ?";

            optionsGroup.SetActive(true);
            SetOptionTexts("5 Ω", "8 Ω", "0.5 Ω");
        }

        void SetOptionTexts(string opt0, string opt1, string opt2)
        {
            if (optionTexts.Length > 0 && optionTexts[0] != null) optionTexts[0].text = opt0;
            if (optionTexts.Length > 1 && optionTexts[1] != null) optionTexts[1].text = opt1;
            if (optionTexts.Length > 2 && optionTexts[2] != null) optionTexts[2].text = opt2;
        }

        void OnOptionSelected(int index)
        {
            if (currentStep == 0)
            {
                // Correct is index 1 ("5 Ω")
                bool correct = (index == 1);
                CalculationAnswered?.Invoke(correct);
                if (correct)
                {
                    if (rows[0].ratioText != null) rows[0].ratioText.text = "5.0 Ω";
                    if (feedbackText != null) feedbackText.text = "Correct! 1.0 V ÷ 0.20 A = 5 Ω.";
                    StartCoroutine(AdvanceAfterDelay(ShowStep1));
                }
                else
                {
                    mistakeCount++;
                    if (feedbackText != null) feedbackText.text = "1.0 ÷ 0.20 = 5. Try 5 Ω.";
                }
            }
            else if (currentStep == 1)
            {
                // Correct is index 0 ("5 Ω")
                bool correct = (index == 0);
                CalculationAnswered?.Invoke(correct);
                if (correct)
                {
                    if (rows[1].ratioText != null) rows[1].ratioText.text = "5.0 Ω";
                    if (feedbackText != null) feedbackText.text = "2.0 V ÷ 0.40 A = 5 Ω.";
                    optionsGroup.SetActive(false);
                    StartCoroutine(RevealRemainingRows());
                }
                else
                {
                    mistakeCount++;
                    if (feedbackText != null) feedbackText.text = "2.0 ÷ 0.40 = 5. Try 5 Ω.";
                }
            }
        }

        IEnumerator AdvanceAfterDelay(Action next)
        {
            yield return new WaitForSeconds(0.8f);
            next?.Invoke();
        }

        IEnumerator RevealRemainingRows()
        {
            if (promptText != null)
                promptText.text = "Revealing all V ÷ I ratios for wire XY...";

            yield return new WaitForSeconds(0.4f);
            for (int i = 2; i < rows.Length; i++)
            {
                if (rows[i].rowHighlight != null) HighlightRow(i, true);
                if (rows[i].ratioText != null) rows[i].ratioText.text = "5.0 Ω";
                yield return new WaitForSeconds(0.35f);
                if (rows[i].rowHighlight != null) HighlightRow(i, false);
            }

            yield return new WaitForSeconds(0.5f);
            ShowObservationQuestion();
        }

        void ShowObservationQuestion()
        {
            currentStep = 2;
            if (promptText != null) promptText.text = "";
            if (questionGroup != null) questionGroup.SetActive(true);
            if (questionPromptText != null) questionPromptText.text = "What do you notice about V divided by I?";

            if (questionButtonTexts.Length >= 3)
            {
                questionButtonTexts[0].text = "It stays the same";
                questionButtonTexts[1].text = "It increases each time";
                questionButtonTexts[2].text = "It decreases each time";
            }
        }

        void OnQuestionSelected(int index)
        {
            bool correct = (index == 0);
            RatioObservationAnswered?.Invoke(correct);

            if (correct)
            {
                currentStep = 3;
                if (questionGroup != null) questionGroup.SetActive(false);
                if (feedbackText != null)
                    feedbackText.text = "The value of V ÷ I is constant: 5 Ω for every reading!";

                if (formulaRevealBanner != null)
                {
                    formulaRevealBanner.SetActive(true);
                    if (formulaRevealText != null)
                        formulaRevealText.text = "RESISTANCE  •  R = V / I  =  5 Ω";
                }

                RatioStageCompleted?.Invoke();
            }
            else
            {
                mistakeCount++;
                if (feedbackText != null)
                    feedbackText.text = "Look at the V/I column: every single reading gives 5 Ω. Try again.";
                // Highlight rows 0 and 1 to help learner compare
                if (rows.Length > 0 && rows[0].rowHighlight != null) HighlightRow(0, true);
                if (rows.Length > 1 && rows[1].rowHighlight != null) HighlightRow(1, true);
            }
        }
    }
}

