using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public class BenchCircuitChallenge : MonoBehaviour
    {
        [Header("Target & Model")]
        [SerializeField] float fixedResistance = 5.0f;
        [SerializeField] float targetCurrent = 0.60f;
        [SerializeField] float targetVoltage = 3.0f;
        [SerializeField] float tolerance = 0.05f;

        [Header("Controls")]
        [SerializeField] RotaryVoltageControl rotaryControl;
        [SerializeField] Button stepDownButton;
        [SerializeField] Button stepUpButton;
        [SerializeField] Button[] presetButtons;
        [SerializeField] Button confirmButton;
        [SerializeField] Button hintButton;
        [SerializeField] Button autoSetButton;

        [Header("Displays & Feedback")]
        [SerializeField] Text promptText;
        [SerializeField] Text voltmeterText;
        [SerializeField] Text ammeterText;
        [SerializeField] Text currentVoltageDisplay;
        [SerializeField] Text currentAmperageDisplay;
        [SerializeField] Text feedbackText;
        [SerializeField] GameObject successIndicator;

        public event Action<bool> ChallengeCompleted;
        public event Action<string> HintGiven;

        float currentVoltage = 1.0f;
        bool completed = false;
        bool isGuided = false;
        int hintLevel = 0;

        public float CurrentVoltage => currentVoltage;
        public float CurrentAmperage => currentVoltage / fixedResistance;
        public bool IsTargetMet => Mathf.Abs(currentVoltage - targetVoltage) <= tolerance &&
            Mathf.Abs(CurrentAmperage - targetCurrent) <= tolerance / fixedResistance;

        void Awake()
        {
            if (presetButtons == null || presetButtons.Length == 0 || System.Array.Exists(presetButtons, b => b == null))
            {
                var pList = new System.Collections.Generic.List<Button>();
                for (int p = 1; p <= 5; p++)
                {
                    var b = transform.Find("Preset" + p)?.GetComponent<Button>() ?? GameObject.Find("Preset" + p)?.GetComponent<Button>();
                    if (b != null) pList.Add(b);
                }
                if (pList.Count > 0) presetButtons = pList.ToArray();
            }
            if (stepDownButton == null) stepDownButton = transform.Find("StepDownBtn")?.GetComponent<Button>() ?? GameObject.Find("StepDownBtn")?.GetComponent<Button>();
            if (stepUpButton == null) stepUpButton = transform.Find("StepUpBtn")?.GetComponent<Button>() ?? GameObject.Find("StepUpBtn")?.GetComponent<Button>();
            if (confirmButton == null) confirmButton = transform.Find("ConfirmBtn")?.GetComponent<Button>() ?? GameObject.Find("ConfirmBtn")?.GetComponent<Button>();
            if (hintButton == null) hintButton = transform.Find("HintBtn")?.GetComponent<Button>() ?? GameObject.Find("HintBtn")?.GetComponent<Button>();
            if (autoSetButton == null) autoSetButton = transform.Find("AutoBtn")?.GetComponent<Button>() ?? GameObject.Find("AutoBtn")?.GetComponent<Button>();

            if (stepDownButton != null)
            {
                stepDownButton.onClick.RemoveAllListeners();
                stepDownButton.onClick.AddListener(() => AdjustVoltage(-0.1f));
            }
            if (stepUpButton != null)
            {
                stepUpButton.onClick.RemoveAllListeners();
                stepUpButton.onClick.AddListener(() => AdjustVoltage(0.1f));
            }

            if (presetButtons != null)
            {
                float[] presets = { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };
                for (int i = 0; i < presetButtons.Length && i < presets.Length; i++)
                {
                    float v = presets[i];
                    presetButtons[i].onClick.RemoveAllListeners();
                    presetButtons[i].onClick.AddListener(() => SetVoltage(v));
                }
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (hintButton != null)
            {
                hintButton.onClick.RemoveAllListeners();
                hintButton.onClick.AddListener(ProvideHint);
            }

            if (autoSetButton != null)
            {
                autoSetButton.onClick.RemoveAllListeners();
                autoSetButton.onClick.AddListener(AutoSetTarget);
                autoSetButton.gameObject.SetActive(false);
            }
        }

        public void Initialize()
        {
            completed = false;
            isGuided = false;
            hintLevel = 0;
            currentVoltage = 1.0f;

            if (successIndicator != null)
                successIndicator.SetActive(false);
            if (confirmButton != null)
                confirmButton.interactable = false;

            if (promptText != null)
                promptText.text = "CHALLENGE: Our wire has a resistance of 5 Ω. Set the voltage so the current becomes 0.60 amperes.";
            if (feedbackText != null)
                feedbackText.text = "Turn the source knob or use the step buttons to find the required voltage.";

            UpdateDisplays();
        }

        public void SetVoltage(float v)
        {
            if (completed) return;
            currentVoltage = Mathf.Clamp(Mathf.Round(v * 10f) / 10f, 0f, 5.0f);
            UpdateDisplays();
        }

        public void AdjustVoltage(float delta)
        {
            SetVoltage(currentVoltage + delta);
        }

        void UpdateDisplays()
        {
            float current = CurrentAmperage;

            if (currentVoltageDisplay != null)
                currentVoltageDisplay.text = currentVoltage.ToString("0.0") + " V";
            if (currentAmperageDisplay != null)
                currentAmperageDisplay.text = current.ToString("0.00") + " A";

            if (voltmeterText != null)
                voltmeterText.text = currentVoltage.ToString("0.00") + " V";
            if (ammeterText != null)
                ammeterText.text = current.ToString("0.00") + " A";

            bool isMatch = IsTargetMet;
            if (successIndicator != null) successIndicator.SetActive(isMatch);
            if (!isMatch && feedbackText != null)
                feedbackText.text = "Adjust the source to reach the target current.";

            if (confirmButton != null)
                confirmButton.interactable = isMatch && !completed;

            if (isMatch && !completed)
            {
                if (feedbackText != null)
                    feedbackText.text = "Target reached! At 3.0 V, current is exactly 0.60 A. Press CONFIRM SETTING.";
                if (successIndicator != null)
                    successIndicator.SetActive(true);
            }
        }

        void OnConfirmClicked()
        {
            if (completed) return;
            if (IsTargetMet)
            {
                completed = true;
                if (confirmButton != null) confirmButton.interactable = false;
                if (feedbackText != null)
                    feedbackText.text = "Confirmed! V = I × R = 0.60 A × 5 Ω = 3.0 V.";

                ChallengeCompleted?.Invoke(isGuided);
            }
        }

        public void ProvideHint()
        {
            hintLevel++;
            string hint = "";

            if (hintLevel == 1)
            {
                hint = "Hint 1: We know current I = 0.60 A and resistance R = 5.0 Ω. How are V, I, and R related?";
            }
            else if (hintLevel == 2)
            {
                hint = "Hint 2: Apply Ohm's Law: V = I × R.";
            }
            else
            {
                hint = "Hint 3: Multiply: V = 0.60 A × 5 Ω = 3.0 V. Set the voltage to 3.0 V.";
                if (autoSetButton != null)
                    autoSetButton.gameObject.SetActive(true);
            }

            if (feedbackText != null)
                feedbackText.text = hint;

            HintGiven?.Invoke(hint);
        }

        public void AutoSetTarget()
        {
            isGuided = true;
            SetVoltage(targetVoltage);
            if (autoSetButton != null)
                autoSetButton.gameObject.SetActive(false);
        }
    }
}
