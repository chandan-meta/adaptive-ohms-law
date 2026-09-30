using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    // Keeps the same camera framing inside the reserved apparatus area on every aspect ratio.
    public class ExperimentPresentation : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] RectTransform viewport;
        [SerializeField] SimpleCircuitModel circuit;
        [SerializeField] Text source, step;
        [SerializeField] GameObject controls;
        [SerializeField] Button switchButton, recordButton;
        [SerializeField] Button[] voltageButtons;
        [SerializeField] RotaryDialGraphic voltageDial;
        public void SetRotary(RotaryDialGraphic dial)=>voltageDial=dial;
        Vector2 lastSize;
        LearningViewport viewportRenderer;
        void Awake() { gameObject.AddComponent<ViewportBackdrop>(); viewportRenderer=viewport.gameObject.AddComponent<LearningViewport>(); viewportRenderer.Configure(view); }
        public void Configure(Camera camera,RectTransform area,SimpleCircuitModel model,Text sourceText,Text stepText,GameObject controlPanel,Button close,Button record,Button[] volts)
        { view=camera; viewport=area; circuit=model; source=sourceText; step=stepText; controls=controlPanel; switchButton=close; recordButton=record; voltageButtons=volts; }
        void OnEnable() { if(circuit!=null) { circuit.StateChanged += RefreshSource; RefreshSource(circuit); } }
        void OnDisable() { if(circuit!=null)circuit.StateChanged -= RefreshSource; }
        void RefreshSource(SimpleCircuitModel model) { source.text = "SOURCE  " + model.SelectedVoltage.ToString("0.0") + " V    /    " + (model.IsSwitchClosed ? "CIRCUIT CLOSED" : "CIRCUIT OPEN"); }
        void LateUpdate()
        {
            var size=new Vector2(Screen.width,Screen.height); if(size==lastSize) return; lastSize=size;
            Canvas.ForceUpdateCanvases(); var corners=new Vector3[4]; viewport.GetWorldCorners(corners);
            viewportRenderer.Refresh();
            float aspect=view.aspect;
            var focus = new Vector3(-.5f,.35f,1.3f);
            float distance=Mathf.Max(4.9f, 5.2f/(Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f)*aspect));
            view.transform.position=focus + new Vector3(0,.65f,-1).normalized*distance;
            view.transform.LookAt(focus);
        }
        public void SetStage(bool collecting,string instruction,SupportLevel support,int nextVoltage=1)
        {
            controls.SetActive(collecting); step.text=instruction;
            if(voltageDial!=null)voltageDial.SetGuidance(collecting && circuit.IsSwitchClosed && support==SupportLevel.Supported);
            Highlight(switchButton, collecting && !circuit.IsSwitchClosed && support!=SupportLevel.Independent);
            Highlight(recordButton, collecting && circuit.IsSwitchClosed && support==SupportLevel.Supported);
            for(int i=0;i<voltageButtons.Length;i++) Highlight(voltageButtons[i],collecting && circuit.IsSwitchClosed && i+1==nextVoltage && support==SupportLevel.Supported);
        }
        static void Highlight(Button button,bool active)
        {
            var outline=button.GetComponent<Outline>(); if(outline!=null) outline.enabled=active;
        }
    }
}




