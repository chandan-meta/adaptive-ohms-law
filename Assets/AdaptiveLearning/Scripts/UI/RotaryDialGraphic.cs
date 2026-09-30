using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class RotaryDialGraphic : MaskableGraphic
    {
        float normalizedValue;
        bool guided;
        public void SetGuidance(bool value){if(guided==value)return;guided=value;SetVerticesDirty();}
        public void SetValue(float value){normalizedValue=Mathf.Clamp01(value);SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.49f;var center=rectTransform.rect.center;
            if(guided)Disc(vh,center,radius*1.035f,new Color(1,.72f,.29f,.8f));
            Disc(vh,center,radius,new Color(.025f,.07f,.10f,.97f));
            Disc(vh,center,radius*.71f,new Color(.22f,.31f,.35f));
            Disc(vh,center,radius*.65f,new Color(.065f,.13f,.17f));
            Disc(vh,center,radius*.57f,new Color(.10f,.19f,.23f));
            for(int i=0;i<=30;i++)
            {
                float radians=(135-i*9)*Mathf.Deg2Rad;var direction=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians));
                var tint=i/30f<=normalizedValue?WarmLabTheme.Amber:new Color(.34f,.47f,.5f);
                Segment(vh,center+direction*radius*(i%10==0?.77f:.83f),center+direction*radius*.94f,i%10==0?3:1.5f,tint);
            }
            float a=(135-normalizedValue*270)*Mathf.Deg2Rad;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            Segment(vh,center+d*radius*.34f,center+d*radius*.62f,5,new Color(1,.72f,.29f));
        }
        static void Disc(VertexHelper vh,Vector2 center,float radius,Color tint)
        {
            int index=vh.currentVertCount;vh.AddVert(center,tint,Vector2.zero);
            for(int i=0;i<=64;i++){float a=i*Mathf.PI*2/64;vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint,Vector2.zero);}
            for(int i=0;i<64;i++)vh.AddTriangle(index,index+i+2,index+i+1);
        }
        static void Segment(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int i=vh.currentVertCount;
            vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
