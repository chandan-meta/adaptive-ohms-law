using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdaptiveOhmsLaw
{
    public class Scene12To13Transition : MonoBehaviour
    {
        [SerializeField] Scene12FlowManager flow;
        [SerializeField] CanvasGroup fade;
        bool started;

        public void Configure(Scene12FlowManager source, CanvasGroup curtain)
        {
            flow = source;
            fade = curtain;
        }

        void Awake()
        {
            if (flow != null)
                flow.OnScene12Completed += Go;
        }

        void Go()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("Scene12Validation", false)) return;
#endif
            if (started) return;
            started = true;
            StartCoroutine(Transition());
        }

        IEnumerator Transition()
        {
            if (fade != null) fade.blocksRaycasts = true;
            yield return new WaitForSeconds(1.5f);
            while (flow != null && flow.IsSpeaking) yield return null;

            if (fade != null)
            {
                for (float t = 0; t < 1f; t += Time.deltaTime)
                {
                    fade.alpha = t;
                    yield return null;
                }
                fade.alpha = 1f;
            }

            yield return SceneManager.LoadSceneAsync("Scene_13_OhmsLaw");
        }

        void OnDestroy()
        {
            if (flow != null)
                flow.OnScene12Completed -= Go;
        }
    }
}
