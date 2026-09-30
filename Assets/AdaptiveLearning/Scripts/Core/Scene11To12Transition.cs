using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdaptiveOhmsLaw
{
    public class Scene11To12Transition : MonoBehaviour
    {
        [SerializeField] Scene08FlowManager flow;
        [SerializeField] CanvasGroup fade;
        bool started;

        public void Configure(Scene08FlowManager source, CanvasGroup curtain)
        {
            flow = source;
            fade = curtain;
        }

        void Awake()
        {
            if (flow == null) flow = FindFirstObjectByType<Scene08FlowManager>();
            if (flow != null)
                flow.OnScene08Completed += Go;
        }

        void Go()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("GraphReview", false) || UnityEditor.SessionState.GetBool("LessonContinuityValidation", false)) return;
#endif
            if (started) return;
            started = true;
            StartCoroutine(Transition());
        }

        IEnumerator Transition()
        {
            if (fade != null) fade.blocksRaycasts = true;
            yield return new WaitForSeconds(3.5f);

            if (fade != null)
            {
                for (float t = 0; t < 1f; t += Time.deltaTime)
                {
                    fade.alpha = t;
                    yield return null;
                }
                fade.alpha = 1f;
            }

            yield return SceneManager.LoadSceneAsync("Scene_12_DiscoverResistance");
        }

        void OnDestroy()
        {
            if (flow != null)
                flow.OnScene08Completed -= Go;
        }
    }
}
