#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class ExperimentEvidenceValidation
    {
        static ExperimentEvidenceValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ExperimentEvidenceValidation", false))
                    new GameObject("ExperimentEvidenceValidation").AddComponent<ExperimentEvidenceValidationRunner>();
                if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool("ExperimentEvidenceValidation", false);
            };
        }
        [MenuItem("Adaptive Learning/Validate Measurement Handoff 10 To 12")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_10_BecomeTheExperimenter.unity");
            Scene07Validation.SetResolution(1440, 1080);
            SessionState.SetBool("ExperimentEvidenceValidation", true);
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
