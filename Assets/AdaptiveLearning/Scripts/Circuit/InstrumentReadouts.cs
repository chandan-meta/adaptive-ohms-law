using UnityEngine;
namespace AdaptiveOhmsLaw
{
    public class InstrumentReadouts : MonoBehaviour
    {
        [SerializeField] SimpleCircuitModel circuit;
        [SerializeField] TextMesh source,current,voltage;
        [SerializeField] MeterDisplay ammeter,voltmeter;
        [SerializeField] Transform knob;
        [SerializeField] CircuitSwitchInteraction circuitSwitch;
        public void ConfigureVisuals(MeterDisplay currentMeter,MeterDisplay voltageMeter,Transform sourceKnob,CircuitSwitchInteraction switchVisual)
        {ammeter=currentMeter;voltmeter=voltageMeter;knob=sourceKnob;circuitSwitch=switchVisual;Refresh(circuit);}
        public void Configure(SimpleCircuitModel model,TextMesh supply,TextMesh ammeter,TextMesh voltmeter){circuit=model;source=supply;current=ammeter;voltage=voltmeter;}
        void OnEnable(){if(circuit!=null){circuit.StateChanged+=Refresh;Refresh(circuit);}}
        void OnDisable(){if(circuit!=null)circuit.StateChanged-=Refresh;}
        void Refresh(SimpleCircuitModel model)
        {
            source.text=model.SelectedVoltage.ToString("0.0")+" V";current.text=model.Current.ToString("0.00")+" A";voltage.text=model.ResistorVoltage.ToString("0.00")+" V";
            if(ammeter!=null)ammeter.SetReading(model.Current);if(voltmeter!=null)voltmeter.SetReading(model.ResistorVoltage);
            if(knob!=null)knob.localRotation=Quaternion.Euler(0,0,135-model.SelectedVoltage*90);
            if(circuitSwitch!=null)circuitSwitch.UpdateVisual(model.IsSwitchClosed);
        }
    }
}
