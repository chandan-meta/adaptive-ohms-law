#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public sealed class CinematicValidationRunner : MonoBehaviour
    {
        readonly List<string> results = new List<string>();
        readonly List<string> errors = new List<string>();

        void Awake() { DontDestroyOnLoad(gameObject); Application.logMessageReceived += Log; }
        void Log(string message, string stack, LogType type)
        {
            if ((stack ?? "").Contains("UnityEditor.Search.SearchDatabase")) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        }
        void Check(bool condition, string label)
        {
            results.Add((condition ? "PASS " : "FAIL ") + label);
            File.WriteAllLines("Logs/Cinematic-validation.txt", results);
            if (!condition)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(1);
                throw new Exception(label);
            }
        }
        IEnumerator Wait(Func<bool> condition, string label, float seconds = 10f)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Check(condition(), label);
        }
        IEnumerator Start()
        {
            yield return null;
            var director = FindFirstObjectByType<LessonCinematicDirector>();
            var dialogue = FindFirstObjectByType<DialoguePresenter>();
            var orbit = Camera.main.GetComponent<LabCameraOrbit>();
            var chat = FindFirstObjectByType<WattsonChatUI>();
            var flow = FindFirstObjectByType<Scene12FlowManager>();
            Check(director != null && dialogue != null && orbit != null && chat != null, "Cinematic services are present");
            Check(FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(t => t.name.StartsWith("Focus - ")) >= 5, "Editable apparatus focus targets are saved in the hierarchy");

            yield return Wait(() => orbit.IsReady, "Exploration camera is initialized");
            // Let the authored opening fade finish before interrupting the lesson.
            yield return new WaitForSecondsRealtime(1.5f);
            flow.StopAllCoroutines();
            dialogue.SkipCurrentLine(); director.SkipCurrentExplanation();
            yield return new WaitForSecondsRealtime(1.4f);
            orbit.ResetView();
            Vector3 home = Camera.main.transform.position;

            var companion = FindFirstObjectByType<WattsonController>();
            Vector3 companionHome = companion.transform.position;
            director.PresentSpokenText("The ammeter measures current in the series circuit.");
            yield return Wait(() => director.ShotActive, "Ammeter explanation starts an apparatus shot");
            Check(orbit.IsSuspended, "Manual camera control is suspended during a shot");
            yield return new WaitForSecondsRealtime(3f);
            yield return Wait(() => companion.IsAtMovementTarget, "Wattson reaches his explanation position");
            Check(Vector3.Distance(companionHome, companion.transform.position) > .1f, "Wattson moves beside the explained apparatus");
            Check(Vector3.Distance(home, Camera.main.transform.position) > .1f, "Camera moves smoothly toward the explained instrument");
            ScreenCapture.CaptureScreenshot("Logs/Cinematic-Ammeter.png");
            yield return null; yield return null;
            director.ReplayCurrentExplanation();
            Check(director.ShotActive, "Replay restarts the current explanation without lesson scoring");
            director.SkipCurrentExplanation();
            yield return Wait(() => !director.CameraTransitioning, "Skip returns the camera to exploration view", 5f);
            Check(!orbit.IsSuspended && Vector3.Distance(home, Camera.main.transform.position) < .05f, "Exploration controls and home composition are restored after skip");

            director.PresentSpokenText("Look closely at the nichrome wire and its resistance.");
            yield return Wait(() => director.ShotActive, "Wire explanation starts a close-up");
            chat.SetOpen(true);
            Check(!director.ShotActive && orbit.IsSuspended, "Opening Ask Wattson interrupts the shot and keeps manual camera paused");
            chat.SetOpen(false);
            Check(orbit.IsSuspended, "Camera stays locked while its interrupted shot returns home");
            yield return Wait(() => !director.CameraTransitioning, "Interrupted camera return completes", 5f);
            Check(!orbit.IsSuspended, "Closing chat restores exploration after the camera handoff");

            var graph = FindFirstObjectByType<ResistanceGraphView>(FindObjectsInactive.Include);
            graph.gameObject.SetActive(true);
            director.PresentSpokenText("Use the graph slope to find resistance.");
            yield return null;
            Check(!director.ShotActive, "Screen-space graph narration does not move the 3D camera toward UI coordinates");
            var graphCameraPosition = Camera.main.transform.position;
            director.PresentSpokenText("Current goes along the bottom. Potential difference goes up the side.");
            yield return new WaitForSecondsRealtime(.25f);
            Check(!director.ShotActive && Vector3.Distance(graphCameraPosition, Camera.main.transform.position) < .01f,
                "Instrument words in graph instructions preserve the graph composition");

            var skip = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.name == "SkipExplanation");
            var replay = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.name == "ReplayExplanation");
            Canvas.ForceUpdateCanvases();
            Check(((RectTransform)skip.transform).rect.height >= 44 && ((RectTransform)replay.transform).rect.height >= 44, "Skip and replay controls meet tablet touch height");
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Scene_11_TurnDataIntoGraph");
            yield return new WaitForSecondsRealtime(3);
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1440,1080) })
            {
                Type.GetType("AdaptiveOhmsLaw.Editor.Scene07Validation, Assembly-CSharp-Editor").GetMethod("SetResolution").Invoke(null, new object[] { size.x, size.y });
                yield return new WaitForSecondsRealtime(1);
                var graphCompanion = FindFirstObjectByType<WattsonController>();
                graphCompanion.ReturnToHome();
                yield return new WaitForSecondsRealtime(1);
                var position = Camera.main.WorldToViewportPoint(graphCompanion.transform.position);
                Check(position.x > .70f && position.x < .93f, "Graph companion stays beside evidence after narration and resize " + size);
                ScreenCapture.CaptureScreenshot("Logs/Warm-Graph-" + size.x + "x" + size.y + ".png");
                yield return null;
            }
            Check(errors.Count == 0, "No lesson runtime errors during cinematic interruption tests");
            results.Add("COMPLETE: camera focus, Wattson positioning hooks, skip, replay, chat interruption, graph safety and camera restoration verified.");
            File.WriteAllLines("Logs/Cinematic-validation.txt", results);
            UnityEditor.EditorApplication.isPlaying = false;
            if (Application.isBatchMode) UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.Exit(0);
        }
        void OnDestroy() => Application.logMessageReceived -= Log;
    }
}
#endif
