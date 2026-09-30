using System;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public class CircuitExperimentController : MonoBehaviour
    {
        [SerializeField] SimpleCircuitModel circuit;
        [SerializeField] MeterDisplay ammeter;
        [SerializeField] MeterDisplay voltmeter;
        [SerializeField] CircuitSwitchInteraction switchVisual;
        [SerializeField] ObservationTableUI observationTable;
        [SerializeField] ChargeDriftVisualization chargeDrift;
        [SerializeField] Button switchButton;
        [SerializeField] RotaryVoltageControl voltageKnob;
        public void SetKnob(RotaryVoltageControl knob) => voltageKnob=knob;
        [SerializeField] Button[] voltageButtons;
        [SerializeField] float[] voltageValues = new[] { 1f, 2f, 3f };
        [SerializeField] Button recordReadingButton;
        [SerializeField] Text switchButtonText;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip recordSound;

        public SimpleCircuitModel Circuit => circuit;
        public ObservationTableUI Table => observationTable;

        public event Action<float, float> ReadingRecorded;
        public event Action AllReadingsCompleted;
        bool interactionEnabled = true;
        public void SetInteraction(bool enabled)
        {
            interactionEnabled = enabled;
            if(voltageKnob!=null)voltageKnob.interactable=enabled;
            if (switchButton != null) switchButton.interactable = enabled;
            foreach (var button in voltageButtons) if (button != null) button.interactable = enabled;
            if (switchVisual != null) switchVisual.InteractionEnabled = enabled;
            UpdateRecordAvailability();
        }
        void UpdateRecordAvailability()
        {
            if (recordReadingButton == null || circuit == null || observationTable == null) return;
            recordReadingButton.interactable = interactionEnabled && observationTable.CanRecord(circuit.ResistorVoltage, circuit.IsSwitchClosed, out _);
        }

        void Start()
        {
            AttachListeners();
            UpdateAllDisplays();
        }

        public void AttachListeners()
        {
            if (circuit != null)
            {
                circuit.StateChanged -= OnCircuitChanged;
                circuit.StateChanged += OnCircuitChanged;
            }

            if (switchButton != null)
            {
                switchButton.onClick.RemoveListener(ToggleSwitch);
                switchButton.onClick.AddListener(ToggleSwitch);
            }

            if (switchVisual != null)
            {
                switchVisual.SwitchToggled -= OnSwitchToggledFromVisual;
                switchVisual.SwitchToggled += OnSwitchToggledFromVisual;
            }

            if (voltageButtons != null)
            {
                for (int i = 0; i < voltageButtons.Length; i++)
                {
                    int index = i;
                    voltageButtons[i].onClick.RemoveAllListeners();
                    voltageButtons[i].onClick.AddListener(() => SelectVoltageIndex(index));
                }
            }

            if (recordReadingButton != null)
            {
                recordReadingButton.onClick.RemoveAllListeners();
                recordReadingButton.onClick.AddListener(() => RecordCurrentReading());
            }

            if (observationTable != null)
            {
                observationTable.ObservationRecorded -= OnObservationRecordedFromTable;
                observationTable.ObservationRecorded += OnObservationRecordedFromTable;
                observationTable.AllObservationsComplete -= OnAllObservationsCompleteFromTable;
                observationTable.AllObservationsComplete += OnAllObservationsCompleteFromTable;
            }
        }

        void OnObservationRecordedFromTable(float v, float i) { UpdateRecordAvailability(); ReadingRecorded?.Invoke(v, i); }
        void OnAllObservationsCompleteFromTable() => AllReadingsCompleted?.Invoke();

        public void Configure(SimpleCircuitModel model, MeterDisplay currentMeter, MeterDisplay voltageMeter,
            CircuitSwitchInteraction knifeSwitch, ObservationTableUI table, ChargeDriftVisualization drift,
            Button toggleSwitchBtn, Text toggleSwitchText, Button[] vBtns, Button recordBtn, AudioSource audio = null, AudioClip recordClip = null)
        {
            circuit = model;
            ammeter = currentMeter;
            voltmeter = voltageMeter;
            switchVisual = knifeSwitch;
            observationTable = table;
            chargeDrift = drift;
            switchButton = toggleSwitchBtn;
            switchButtonText = toggleSwitchText;
            voltageButtons = vBtns;
            recordReadingButton = recordBtn;
            audioSource = audio;
            recordSound = recordClip;

            AttachListeners();
            UpdateAllDisplays();
        }

        public void SelectVoltageIndex(int index)
        {
            if (index >= 0 && index < voltageValues.Length)
                SelectVoltage(voltageValues[index]);
        }

        public void SelectVoltage(float v)
        {
            if (circuit == null || !interactionEnabled) return;
            circuit.SetVoltage(v);
            UpdateVoltageButtons();
        }

        public void ToggleSwitch()
        {
            if (circuit == null || !interactionEnabled) return;
            circuit.ToggleSwitch();
            if (switchVisual != null)
                switchVisual.UpdateVisual(circuit.IsSwitchClosed);
        }

        public void SetSwitch(bool closed)
        {
            if (circuit == null) return;
            circuit.SetSwitch(closed);
            if (switchVisual != null)
                switchVisual.UpdateVisual(closed);
        }

        void OnSwitchToggledFromVisual(bool closed)
        {
            UpdateSwitchButtonText();
            UpdateMeters();
            UpdateDrift();
        }

        void OnCircuitChanged(SimpleCircuitModel model)
        {
            UpdateAllDisplays();
        }

        public void UpdateAllDisplays()
        {
            UpdateSwitchButtonText();
            UpdateVoltageButtons();
            UpdateMeters();
            UpdateDrift();
            UpdateRecordAvailability();
        }

        void UpdateSwitchButtonText()
        {
            if (switchButtonText != null && circuit != null)
            {
                switchButtonText.text = circuit.IsSwitchClosed ? "OPEN SWITCH" : "CLOSE SWITCH";
            }
        }

        void UpdateVoltageButtons()
        {
            if (voltageButtons == null || circuit == null) return;
            for (int i = 0; i < voltageButtons.Length; i++)
            {
                if (i < voltageValues.Length && voltageButtons[i] != null)
                {
                    bool isSelected = Mathf.Abs(circuit.SelectedVoltage - voltageValues[i]) < 0.05f;
                    var img = voltageButtons[i].GetComponent<Image>();
                    if (img != null)
                        img.color = isSelected ? new Color(0.2f, 0.65f, 0.75f) : new Color(0.08f, 0.24f, 0.29f);
                }
            }
        }

        void UpdateMeters()
        {
            if (circuit == null) return;
            if (ammeter != null)
                ammeter.SetReading(circuit.Current);
            if (voltmeter != null)
                voltmeter.SetReading(circuit.ResistorVoltage);
        }

        void UpdateDrift()
        {
            if (chargeDrift == null || circuit == null) return;
            // Drifts only when switch is closed, with speed proportional to voltage/current
            chargeDrift.SetVoltage(circuit.IsSwitchClosed ? circuit.SelectedVoltage : 0f);
        }

        public bool RecordCurrentReading()
        {
            if (circuit == null || observationTable == null || !interactionEnabled) return false;
            float v = circuit.ResistorVoltage;
            float i = circuit.Current;
            bool success = observationTable.RecordCurrentReading(v, i, circuit.IsSwitchClosed);
            if (success && audioSource != null && recordSound != null)
                audioSource.PlayOneShot(recordSound);
            return success;
        }

        void OnDestroy()
        {
            if (circuit != null)
                circuit.StateChanged -= OnCircuitChanged;
            if (switchVisual != null)
                switchVisual.SwitchToggled -= OnSwitchToggledFromVisual;
        }
    }
}

