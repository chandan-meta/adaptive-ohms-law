#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class ImplementedScenesSmokeValidation
    {
        static readonly string[] Scenes = { "Scene_06_AdaptivePrediction", "Scene_08_OurLabIntroduction", "Scene_09_BuildTheCircuit", "Scene_10_BecomeTheExperimenter", "Scene_11_TurnDataIntoGraph" };
        static readonly List<string> Results = new List<string>();
        static readonly List<string> Errors = new List<string>();
        static int index;
        static double next;
        static ImplementedScenesSmokeValidation()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool("ImplementedScenesSmoke", false) && EditorApplication.isPlaying &&
                    (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                {
                    // Unity 6000.3 can throw this editor-index startup exception in
                    // headless mode. It is outside lesson runtime and is reported
                    // separately; do not let it mask scene gameplay failures.
                    if ((stack ?? "").Contains("UnityEditor.Search.SearchDatabase")) return;
                    Errors.Add(message);
                }
            };
        }
        public static void Run()
        {
            index = 0; next = 0; Results.Clear(); Errors.Clear();
            SessionState.SetBool("ImplementedScenesSmoke", true);
            Scene07Validation.SetResolution(1920, 1080);
            EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/" + Scenes[0] + ".unity");
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool("ImplementedScenesSmoke", false) || !EditorApplication.isPlaying) return;
            if (next == 0) { next = EditorApplication.timeSinceStartup + 5; return; }
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (SceneManager.GetActiveScene().name != Scenes[index]) throw new Exception("Wrong active scene");
                if (Camera.main == null) throw new Exception("Missing learner camera");
                var missing = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true))
                    .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                if (missing != 0) throw new Exception("Missing scripts: " + missing);
                if (Errors.Count != 0) throw new Exception(string.Join("; ", Errors));
                Results.Add("PASS " + Scenes[index] + ": loads, camera present, no missing scripts or startup exceptions");
                File.WriteAllLines("Logs/Implemented-scenes-smoke.txt", Results);
                if (++index == Scenes.Length) { Finish(0); return; }
                SceneManager.LoadScene(Scenes[index]);
                next = EditorApplication.timeSinceStartup + 5;
            }
            catch (Exception e) { Results.Add("FAIL " + Scenes[index] + ": " + e.Message); Finish(1); }
        }
        static void Finish(int code)
        {
            Results.Add(code == 0 ? "COMPLETE — startup checks only" : "FAILED");
            File.WriteAllLines("Logs/Implemented-scenes-smoke.txt", Results);
            SessionState.SetBool("ImplementedScenesSmoke", false);
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
#endif
