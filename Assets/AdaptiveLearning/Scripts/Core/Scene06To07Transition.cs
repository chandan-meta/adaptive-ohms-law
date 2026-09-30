using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdaptiveOhmsLaw
{
    public class Scene06To07Transition : MonoBehaviour
    {
        [SerializeField] SceneFlowManager flowManager;
        [SerializeField] CanvasGroup fadeCanvasGroup;
        [SerializeField] float delayBeforeFade = 3.5f;
        [SerializeField] float fadeDuration = 1.0f;
        [SerializeField] string targetScene = "Scene_08_OurLabIntroduction";

        bool transitioning;
        public void Configure(SceneFlowManager flow, CanvasGroup fade) { flowManager = flow; fadeCanvasGroup = fade; }

        void Awake()
        {
            if (flowManager == null)
                flowManager = GetComponent<SceneFlowManager>() ?? FindFirstObjectByType<SceneFlowManager>();

            if (flowManager != null)
                flowManager.OnScene06Completed += HandleScene06Completed;
        }

        void HandleScene06Completed()
        {
            if (transitioning) return;

#if UNITY_EDITOR
            // Do not disrupt automated Scene 06 validation runner in play mode
            if (UnityEditor.SessionState.GetBool("Scene06Validation", false))
                return;
#endif

            transitioning = true;
            StartCoroutine(TransitionRoutine());
        }

        IEnumerator TransitionRoutine()
        {
            yield return new WaitForSeconds(delayBeforeFade);
            while(flowManager.IsSpeaking)yield return null;

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.blocksRaycasts = true;
                for (float t = 0; t < fadeDuration; t += Time.deltaTime)
                {
                    fadeCanvasGroup.alpha = t / fadeDuration;
                    yield return null;
                }
                fadeCanvasGroup.alpha = 1f;
            }

            SceneManager.LoadScene(targetScene);
        }

        void OnDestroy()
        {
            if (flowManager != null)
                flowManager.OnScene06Completed -= HandleScene06Completed;
        }
    }
}

