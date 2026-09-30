using UnityEngine;
namespace AdaptiveOhmsLaw
{
    public sealed class LabWireRoute : MonoBehaviour
    {
        public int a,b;
        public Transform[] points;
        public LineRenderer line;
        public void Show(){const int samples=12;line.positionCount=(points.Length-1)*samples+1;int output=0;
            for(int segment=0;segment<points.Length-1;segment++)for(int j=0;j<samples;j++){
                var p0=points[Mathf.Max(0,segment-1)].position;var p1=points[segment].position;var p2=points[segment+1].position;var p3=points[Mathf.Min(points.Length-1,segment+2)].position;float t=j/(float)samples;
                var p=.5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t);p.y=Mathf.Max(.06f,p.y);line.SetPosition(output++,p);
            }
            line.SetPosition(output,points[points.Length-1].position);line.enabled=true;}
    }
}

