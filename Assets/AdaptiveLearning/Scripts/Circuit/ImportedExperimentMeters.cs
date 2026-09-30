using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    // Presentation only: the existing circuit owns current, voltage and assessment.
    public sealed class ImportedExperimentMeters : MonoBehaviour
    {
        [SerializeField] SimpleCircuitModel circuit;
        [SerializeField] LabMeterVisual ammeter, voltmeter;
        [SerializeField] Text currentHud, voltageHud;
        public void Configure(SimpleCircuitModel model, LabMeterVisual current, LabMeterVisual voltage)
        { circuit = model; ammeter = current; voltmeter = voltage; }
        public void BindReadouts(Text current, Text voltage) { currentHud = current; voltageHud = voltage; }
        void OnEnable() { if (circuit != null) { circuit.StateChanged += Refresh; Refresh(circuit); } }
        void OnDisable() { if (circuit != null) circuit.StateChanged -= Refresh; }
        void Refresh(SimpleCircuitModel model)
        {
            ammeter?.SetValue(model.Current); voltmeter?.SetValue(model.ResistorVoltage);
            if (currentHud != null) currentHud.text = model.Current.ToString("0.00") + " A";
            if (voltageHud != null) voltageHud.text = model.ResistorVoltage.ToString("0.00") + " V";
        }
    }
}
