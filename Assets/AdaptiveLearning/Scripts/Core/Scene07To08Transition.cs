using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AdaptiveOhmsLaw
{
    public class Scene07To08Transition : MonoBehaviour
    {
        [SerializeField] Scene07FlowManager flow;
        [SerializeField] CanvasGroup fade;
        bool started;
        public void Configure(Scene07FlowManager source,CanvasGroup curtain){flow=source;fade=curtain;}
        void Awake(){flow.OnScene07Completed+=Go;}
        void Go()
        {
#if UNITY_EDITOR
            if(UnityEditor.SessionState.GetBool("Scene07Validation",false))return;
#endif
            if(started)return;started=true;StartCoroutine(Transition());
        }
        IEnumerator Transition()
        {
            fade.blocksRaycasts=true;yield return new WaitForSeconds(4);
            while(flow.IsSpeaking)yield return null;
            for(float t=0;t<1;t+=Time.deltaTime){fade.alpha=t;yield return null;}fade.alpha=1;
            yield return SceneManager.LoadSceneAsync("Scene_11_TurnDataIntoGraph");
        }
        void OnDestroy(){if(flow!=null)flow.OnScene07Completed-=Go;}
    }
}
