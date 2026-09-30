using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    // Draws a quantitative grid and evidence line in normalized data coordinates.
    [RequireComponent(typeof(CanvasRenderer))]
    public class GraphGridGraphic : MaskableGraphic
    {
        public float maxVoltage=6f,maxCurrent=1.2f;
        public bool showTrend;
        public readonly List<Vector2> points=new List<Vector2>();
        public void Refresh(){SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;
            for(int i=0;i<=LessonExperimentData.ReadingCount;i++)
            {
                float x=(i*LessonExperimentData.CurrentStep)/maxCurrent;float y=i/maxVoltage;
                Line(vh,new Vector2(x,0),new Vector2(x,1),i==0?3:1,WarmLabTheme.Sage,rect);
                Line(vh,new Vector2(0,y),new Vector2(1,y),i==0?3:1,WarmLabTheme.Sage,rect);
            }
            if(showTrend)
            {
                var prev=Vector2.zero;
                foreach(var point in points){var p=new Vector2(point.x/maxCurrent,point.y/maxVoltage);Line(vh,prev,p,3,WarmLabTheme.Plum,rect);prev=p;}
            }
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color,Rect rect)
        {
            a=rect.min+Vector2.Scale(a,rect.size);b=rect.min+Vector2.Scale(b,rect.size);
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int start=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}

