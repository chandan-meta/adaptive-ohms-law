using UnityEngine;
namespace AdaptiveOhmsLaw
{
    // A full-screen clear prevents stale UI pixels outside a letterboxed apparatus camera.
    public class ViewportBackdrop : MonoBehaviour
    {
        void Awake()
        {
            var go=new GameObject("Viewport background");go.transform.SetParent(transform,false);
            var camera=go.AddComponent<Camera>();camera.cullingMask=0;camera.depth=-100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.022f,.045f,.067f);camera.useOcclusionCulling=false;
        }
    }
}
