using System;
using UnityEngine;

namespace AdaptiveOhmsLaw
{
    [Serializable]
    public class SimpleCircuitModel : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] float resistance = 10f;
        [SerializeField] float selectedVoltage = 1f;
        [SerializeField] bool isSwitchClosed;

        public event Action<SimpleCircuitModel> StateChanged;

        public float Resistance
        {
            get => resistance;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) return;
                resistance = Mathf.Max(0.01f, value);
                StateChanged?.Invoke(this);
            }
        }

        public float SelectedVoltage
        {
            get => selectedVoltage;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) return;
                selectedVoltage = Mathf.Max(0f, value);
                StateChanged?.Invoke(this);
            }
        }

        public bool IsSwitchClosed
        {
            get => isSwitchClosed;
            set
            {
                if (isSwitchClosed == value) return;
                isSwitchClosed = value;
                StateChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Voltage measured by the voltmeter connected in parallel across the resistor.
        /// When the switch is open, the current is 0, so the potential drop across the resistor is 0 V.
        /// </summary>
        public float ResistorVoltage => isSwitchClosed ? selectedVoltage : 0f;

        /// <summary>
        /// Source voltage setting.
        /// </summary>
        public float SourceVoltage => selectedVoltage;

        /// <summary>
        /// Current in amperes (I = V / R).
        /// When switch is open, the path is broken and current is 0 A.
        /// </summary>
        public float Current
        {
            get
            {
                if (!isSwitchClosed) return 0f;
                float r = Mathf.Max(0.001f, resistance);
                float i = selectedVoltage / r;
                return float.IsFinite(i) ? i : 0f;
            }
        }

        public void Configure(float fixedResistance, float initialVoltage = 1f, bool startClosed = false)
        {
            resistance = Mathf.Max(0.01f, fixedResistance);
            selectedVoltage = Mathf.Max(0f, initialVoltage);
            isSwitchClosed = startClosed;
            StateChanged?.Invoke(this);
        }

        public void SetVoltage(float voltage) => SelectedVoltage = voltage;
        public void SetSwitch(bool closed) => IsSwitchClosed = closed;
        public void ToggleSwitch() => IsSwitchClosed = !isSwitchClosed;
    }
}
