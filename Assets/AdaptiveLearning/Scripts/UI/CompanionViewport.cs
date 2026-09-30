using UnityEngine;
namespace AdaptiveOhmsLaw
{
    public class CompanionViewport : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] RectTransform area;
        [SerializeField] Transform companion;
        [SerializeField] Transform apparatus;
        Vector2 size;
        public void Configure(Camera camera,RectTransform viewport,Transform target){view=camera;area=viewport;companion=target;}
        public void ConfigureStudy(Transform bench){apparatus=bench;FrameStudy(16f/9f);}
        void FrameStudy(float aspect)
        {
            view.fieldOfView=38;view.transform.SetPositionAndRotation(new Vector3(0,1,-8),Quaternion.identity);view.aspect=aspect;
            companion.position=view.ViewportToWorldPoint(new Vector3(.805f,.625f,9));
            companion.localScale=Vector3.one*.85f;
            companion.rotation=Quaternion.LookRotation(view.transform.position-companion.position,Vector3.up);
            if(Application.isPlaying)companion.GetComponent<WattsonController>()?.RebasePresentationHome();
            if(apparatus!=null){
                // Keep the reference circuit in the companion's column on every tablet ratio.
                float scale=.265f*18*Mathf.Tan(19*Mathf.Deg2Rad)*aspect/9;
                apparatus.localScale=Vector3.one*scale;apparatus.rotation=Quaternion.Euler(-20,0,0);
                apparatus.position=view.ViewportToWorldPoint(new Vector3(.805f,.375f,9))-apparatus.rotation*(new Vector3(0,0,1.2f)*scale);
            }
        }
        LearningViewport viewportRenderer;
        void Awake(){gameObject.AddComponent<ViewportBackdrop>();viewportRenderer=area.gameObject.AddComponent<LearningViewport>();viewportRenderer.Configure(view);}
        void LateUpdate()
        {
            if(size==new Vector2(Screen.width,Screen.height))return;size=new Vector2(Screen.width,Screen.height);
            Canvas.ForceUpdateCanvases();var corners=new Vector3[4];area.GetWorldCorners(corners);
            viewportRenderer.Refresh();
            FrameStudy(view.aspect);
        }
    }
}




