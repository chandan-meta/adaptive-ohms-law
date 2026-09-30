using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    // Captures one mouse/finger and releases reliably outside the pad or on dismissal.
    public sealed class LabOrbitJoystick : MaskableGraphic, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value { get; private set; }
        int? pointer;
        public void OnPointerDown(PointerEventData e)
        {
            if(pointer.HasValue||e.button!=PointerEventData.InputButton.Left)return;
            pointer=e.pointerId;OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if(pointer!=e.pointerId)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var point))
            {
                float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.32f;
                Value=Vector2.ClampMagnitude((point-rectTransform.rect.center)/radius,1);SetVerticesDirty();
            }
        }
        public void OnPointerUp(PointerEventData e){if(pointer==e.pointerId)ResetInput();}
        public void ResetInput(){pointer=null;Value=Vector2.zero;SetVerticesDirty();}
        protected override void OnDisable(){ResetInput();base.OnDisable();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();float size=Mathf.Min(r.width,r.height);var c=r.center;
            Disc(vh,c,size*.49f,WarmLabTheme.Paper);
            Disc(vh,c,size*.46f,WarmLabTheme.Sage);
            Disc(vh,c,size*.29f,WarmLabTheme.Sage);
            Disc(vh,c+Value*size*.28f,size*.17f,WarmLabTheme.Ivory);
        }
        static void Disc(VertexHelper vh,Vector2 center,float radius,Color tint)
        {
            int start=vh.currentVertCount;vh.AddVert(center,tint,Vector2.zero);
            for(int i=0;i<=40;i++){float angle=i*Mathf.PI*2/40;vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint,Vector2.zero);}
            for(int i=1;i<=40;i++)vh.AddTriangle(start,start+i,start+i+1);
        }
    }
}
