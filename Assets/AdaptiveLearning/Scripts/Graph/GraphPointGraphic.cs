using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    // A small visual marker with a full-size, touch-friendly RectTransform hit area.
    public class GraphPointGraphic : Image
    {
        bool marked;
        public void SetMarked(bool value){marked=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var center=rectTransform.rect.center;float radius=marked?7:6;
            for(int i=0;i<32;i++){
                float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;int start=vh.currentVertCount;
                var da=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var db=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                float inner=marked?0:radius-1.6f;
                vh.AddVert(center+da*inner,color,Vector2.zero);vh.AddVert(center+da*radius,color,Vector2.zero);
                vh.AddVert(center+db*radius,color,Vector2.zero);vh.AddVert(center+db*inner,color,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
