using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    public sealed class MicrophoneIcon : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh){
            vh.Clear();var r=GetPixelAdjustedRect();float s=Mathf.Min(r.width,r.height);Vector2 c=r.center;
            System.Action<Vector2,Vector2,float> line=(a,b,w)=>{var n=new Vector2(-(b-a).y,(b-a).x).normalized*w*.5f;int i=vh.currentVertCount;vh.AddVert(c+a+n,color,Vector2.zero);vh.AddVert(c+b+n,color,Vector2.zero);vh.AddVert(c+b-n,color,Vector2.zero);vh.AddVert(c+a-n,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);};
            for(int k=0;k<20;k++){float a=k*Mathf.PI*2/20,b=(k+1)*Mathf.PI*2/20;line(new Vector2(Mathf.Cos(a)*.12f,Mathf.Sin(a)*.23f+.1f)*s,new Vector2(Mathf.Cos(b)*.12f,Mathf.Sin(b)*.23f+.1f)*s,.065f*s);}
            for(int k=0;k<16;k++){float a=Mathf.PI+k*Mathf.PI/16,b=Mathf.PI+(k+1)*Mathf.PI/16;line(new Vector2(Mathf.Cos(a)*.25f,Mathf.Sin(a)*.29f)*s,new Vector2(Mathf.Cos(b)*.25f,Mathf.Sin(b)*.29f)*s,.055f*s);}
            line(new Vector2(0,-.29f)*s,new Vector2(0,-.43f)*s,.055f*s);line(new Vector2(-.15f,-.43f)*s,new Vector2(.15f,-.43f)*s,.055f*s);
        }
    }
}
