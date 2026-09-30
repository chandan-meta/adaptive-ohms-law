using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public class ObservationTableUI : MonoBehaviour
    {
        [SerializeField] Text[] rowVoltageTexts;
        [SerializeField] Text[] rowCurrentTexts;
        [SerializeField] Button recordButton;
        [SerializeField] Text statusFeedbackText;
        [SerializeField] Text progressText;

        readonly List<ExperimentObservation> observations = new List<ExperimentObservation>();
        public List<ExperimentObservation> Observations => observations;
        public int RecordedCount => observations.Count;
        public bool IsComplete => observations.Count >= LessonExperimentData.ReadingCount;

        public event Action<float, float> ObservationRecorded;
        public event Action AllObservationsComplete;

        public void Restore(IEnumerable<ExperimentObservation> saved)
        {
            observations.Clear();
            foreach (var entry in saved) if (entry != null && observations.Count < rowVoltageTexts.Length) observations.Add(entry);
            RefreshRows(); UpdateProgress();
        }
        void RefreshRows()
        {
            observations.Sort((a,b)=>a.voltage.CompareTo(b.voltage));
            for (int i=0;i<rowVoltageTexts.Length;i++)
            {
                rowVoltageTexts[i].text = i < observations.Count ? observations[i].voltage.ToString("0.0") + " V" : "—";
                rowCurrentTexts[i].text = i < observations.Count ? observations[i].current.ToString("0.00") + " A" : "—";
            }
        }

        public void Configure(Text[] vTexts, Text[] iTexts, Button recordBtn, Text feedback, Text progress)
        {
            rowVoltageTexts = vTexts;
            rowCurrentTexts = iTexts;
            recordButton = recordBtn;
            statusFeedbackText = feedback;
            progressText = progress;

            ClearTable();
        }

        public void ClearTable()
        {
            observations.Clear();
            for (int i = 0; i < rowVoltageTexts.Length; i++)
            {
                if (rowVoltageTexts[i] != null) rowVoltageTexts[i].text = "—";
                if (rowCurrentTexts[i] != null) rowCurrentTexts[i].text = "—";
            }
            UpdateProgress();
            SetFeedback("Close the circuit switch and turn the source knob to begin.");
        }

        public bool CanRecord(float voltage, bool switchClosed, out string reason)
        {
            if (!switchClosed)
            {
                reason = "Circuit switch is open. Close the switch first to measure current.";
                return false;
            }
            if (!float.IsFinite(voltage) || voltage < .99f || voltage > LessonExperimentData.ReadingCount + .01f || Mathf.Abs(voltage - Mathf.Round(voltage)) >= .01f)
            {
                reason = "Turn the knob to a whole volt from 1 V to 5 V to record.";
                return false;
            }
            if (observations.Exists(o => Mathf.Abs(o.voltage - voltage) < 0.05f))
            {
                reason = voltage.ToString("0.0") + " V is already recorded. Turn the knob to a different voltage.";
                return false;
            }
            if (IsComplete)
            {
                reason = "All five readings are already recorded.";
                return false;
            }
            reason = "";
            return true;
        }

        public bool RecordCurrentReading(float voltage, float current, bool switchClosed)
        {
            if (!CanRecord(voltage, switchClosed, out string reason))
            {
                SetFeedback(reason);
                return false;
            }

            int index = observations.Count;
            var obs = new ExperimentObservation
            {
                voltage = voltage,
                current = current,
                recordedUtc = DateTime.UtcNow.ToString("o")
            };
            observations.Add(obs);

            if (index < rowVoltageTexts.Length && rowVoltageTexts[index] != null)
                rowVoltageTexts[index].text = voltage.ToString("0.0") + " V";
            if (index < rowCurrentTexts.Length && rowCurrentTexts[index] != null)
                rowCurrentTexts[index].text = current.ToString("0.00") + " A";

            RefreshRows(); UpdateProgress();
            ObservationRecorded?.Invoke(voltage, current);

            if (IsComplete)
            {
                SetFeedback("All five observations recorded. Notice how current changes with voltage.");
                AllObservationsComplete?.Invoke();
            }
            else
            {
                SetFeedback("Recorded: " + voltage.ToString("0.0") + " V  |  " + current.ToString("0.00") + " A. Select another voltage.");
            }

            return true;
        }

        void OnRecordClicked()
        {
            // Note: CircuitExperimentController wires into this or triggers RecordCurrentReading with current circuit values.
        }

        public void SetFeedback(string text)
        {
            if (statusFeedbackText != null)
                statusFeedbackText.text = text;
        }

        public void UpdateProgress()
        {
            if (progressText != null)
                progressText.text = "READINGS RECORDED:  " + observations.Count + " / " + LessonExperimentData.ReadingCount;
            if (recordButton != null)
                recordButton.interactable = !IsComplete;
        }
    }
}

