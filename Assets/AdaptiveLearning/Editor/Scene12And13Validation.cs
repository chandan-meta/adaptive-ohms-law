#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class Scene12And13Validation
    {
        static Scene12And13Validation()
        {
            EditorApplication.update += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (File.Exists("Logs/scene12-13-validate.req"))
                {
                    File.Delete("Logs/scene12-13-validate.req");
                    Run();
                }
            };

            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Scene12And13Validation", false))
                {
                    if (UnityEngine.Object.FindFirstObjectByType<Scene12And13ValidationRunner>() == null) new GameObject("Scene12And13ValidationRunner").AddComponent<Scene12And13ValidationRunner>();
                }
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool("Scene12And13Validation", false);
                }
            };
        }

        [MenuItem("Adaptive Learning/Validate Scenes 12 and 13")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            // Validate the authored scenes without overwriting manual model,
            // camera or cinematic edits with generated defaults.
            if (!File.Exists("Assets/AdaptiveLearning/Scenes/Scene_12_DiscoverResistance.unity") ||
                !File.Exists("Assets/AdaptiveLearning/Scenes/Scene_13_OhmsLaw.unity"))
                throw new InvalidOperationException("Create Scenes 12 and 13 before running validation.");

            Directory.CreateDirectory("Logs");
            EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_12_DiscoverResistance.unity");
            Scene07Validation.SetResolution(1920, 1080);
            SessionState.SetBool("Scene12And13Validation", true);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Adaptive Learning/Open Scene 12 Discover Resistance")]
        public static void Open12()
        {
            if (!File.Exists("Assets/AdaptiveLearning/Scenes/Scene_12_DiscoverResistance.unity"))
                Scene12And13Builder.BuildAll();
            EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_12_DiscoverResistance.unity");
        }

        [MenuItem("Adaptive Learning/Open Scene 13 Ohms Law")]
        public static void Open13()
        {
            if (!File.Exists("Assets/AdaptiveLearning/Scenes/Scene_13_OhmsLaw.unity"))
                Scene12And13Builder.BuildAll();
            EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_13_OhmsLaw.unity");
        }

        // Batch validation must exercise Play mode, not call runtime components in Edit mode.
        [MenuItem("Adaptive Learning/Validate Scenes 12 and 13 (Batch)")]
        public static void RunBatch() => Run();
    }
}
#endif

