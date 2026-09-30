#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class CinematicValidation
    {
        static CinematicValidation() => EditorApplication.playModeStateChanged += AddRunner;

        [MenuItem("Adaptive Learning/Cinematics/Validate Direction And Handoffs")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_12_DiscoverResistance.unity");
            Scene07Validation.SetResolution(1920, 1080);
            SessionState.SetBool("CinematicValidation", true);
            EditorApplication.isPlaying = true;
        }

        static void AddRunner(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("CinematicValidation", false);
                return;
            }
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CinematicValidation", false)) return;
            new GameObject("CinematicValidationRunner").AddComponent<CinematicValidationRunner>();
        }
    }
}
#endif
