using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    // Relative angular movement avoids jumping when the learner first grabs the dial.
    public class RotaryVoltageControl : Selectable, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [SerializeField] CircuitExperimentController experiment;
        [SerializeField] RotaryDialGraphic face;
        [SerializeField] Text valueLabel;
        [SerializeField] Transform physicalKnob;
        [SerializeField, Min(0)] float physicalScaleMaximum;
        [SerializeField] float minimum=0, maximum=5, sweepDegrees=270, step=.1f, detentTolerance=.12f;
        float rawValue, lastAngle;
        int? activePointer;
        public float Value=>experiment.Circuit.SelectedVoltage;
        public void Configure(CircuitExperimentController controller,RotaryDialGraphic graphic,Text label,Transform model)
        {experiment=controller;face=graphic;valueLabel=label;physicalKnob=model;targetGraphic=face;}
        protected override void Start()
        {
            base.Start();experiment.Circuit.StateChanged+=Refresh;Refresh(experiment.Circuit);
        }
        void Refresh(SimpleCircuitModel model)
        {
            face.SetValue(Mathf.InverseLerp(minimum,maximum,model.SelectedVoltage));
            valueLabel.text=model.SelectedVoltage.ToString("0.0")+" V";
            if(physicalKnob!=null)physicalKnob.localRotation=Quaternion.Euler(0,0,135-model.SelectedVoltage/(physicalScaleMaximum>0?physicalScaleMaximum:maximum)*sweepDegrees);
        }
        float Angle(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.pressEventCamera,out var point);
            point-=((RectTransform)transform).rect.center;return Mathf.Atan2(point.y,point.x)*Mathf.Rad2Deg;
        }
        public override void OnPointerDown(PointerEventData data)
        {
            base.OnPointerDown(data);
            if(!IsInteractable()||data.button!=PointerEventData.InputButton.Left||activePointer.HasValue)return;
            activePointer=data.pointerId;lastAngle=Angle(data);rawValue=Value;
        }
        public void OnBeginDrag(PointerEventData data) { data.useDragThreshold=false; }
        public void OnDrag(PointerEventData data)
        {
            if(!IsInteractable()||activePointer!=data.pointerId)return;
            float angle=Angle(data);rawValue=Mathf.Clamp(rawValue-Mathf.DeltaAngle(lastAngle,angle)/sweepDegrees*(maximum-minimum),minimum,maximum);lastAngle=angle;Apply(rawValue);
        }
        void Apply(float value)
        {
            value=Mathf.Clamp(value,minimum,maximum);float detent=Mathf.Round(value);
            if(Mathf.Abs(value-detent)<=detentTolerance)value=detent;else value=Mathf.Round(value/step)*step;
            experiment.SelectVoltage(value);
        }
        public void OnEndDrag(PointerEventData data){if(activePointer==data.pointerId)activePointer=null;}
        public override void OnPointerUp(PointerEventData data){base.OnPointerUp(data);if(activePointer==data.pointerId)activePointer=null;}
        public override void OnDeselect(BaseEventData data){activePointer=null;base.OnDeselect(data);}
        void OnApplicationFocus(bool focused){if(!focused)activePointer=null;}
        void OnApplicationPause(bool paused){if(paused)activePointer=null;}
        public void OnScroll(PointerEventData data){if(IsInteractable())experiment.SelectVoltage(Mathf.Clamp(Value+Mathf.Sign(data.scrollDelta.y)*step,minimum,maximum));}
        public override void OnMove(AxisEventData data)
        {
            if(!IsInteractable())return;
            if(data.moveDir==MoveDirection.Left)experiment.SelectVoltage(Mathf.Max(minimum,Value-step));
            else if(data.moveDir==MoveDirection.Right)experiment.SelectVoltage(Mathf.Min(maximum,Value+step));
            else {base.OnMove(data);return;}
            data.Use();
        }
        protected override void OnDisable(){activePointer=null;base.OnDisable();}
        protected override void OnDestroy(){if(experiment!=null&&experiment.Circuit!=null)experiment.Circuit.StateChanged-=Refresh;base.OnDestroy();}
    }
}

