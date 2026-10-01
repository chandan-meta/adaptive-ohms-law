using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace OhmsLaw.Editor
{
    // Hands-free play test: starts a scene (or the whole module) with ReviewMode.autoAnswer, logs every line, gate
    // and hand-off with its time, and fails on stalls, repeated lines, missing lines or console errors.
    // Report: Logs/Blockout/play-<scene>.txt
    public static class BlockoutPlayTest
    {
        const string Key = "ohm.blockout.play", SceneKey = "ohm.blockout.play.scene", SpeedKey = "ohm.blockout.play.speed", Report = "Logs/Blockout/play-report.txt";
        const float StallSeconds = 30f, MaxMinutes = 25f;

        static readonly List<string> spoken = new List<string>();
        static int shotIndex;
        static string currentScene = "", lastGate;
        static double lastTime = -1;
        static float lastProgress, started;
        static int errors;
        static bool hooked;

        [MenuItem("Ohm's Law/Play Test/Whole module (autoAnswer)")] static void Module() => Run("all");
        [MenuItem("Ohm's Law/Play Test/Current scene (autoAnswer)")] static void Current() => Run(SceneManager.GetActiveScene().name);

        // scene: a scene name, a scene id ("Scene05"), or "all" (Scene 4 to the end card). Append " 2x" for double speed.
        public static void Run(string request)
        {
            string[] words = request.Split(' ');
            string scene = words[0];
            float speed = words.Length > 1 && words[1].EndsWith("x") ? float.Parse(words[1].TrimEnd('x')) : 1f;
            if (scene == "all") scene = BlockoutAssets.SceneOrder[0];
            else if (!scene.Contains("_")) scene = BlockoutAssets.Builders().FirstOrDefault(b => b.SceneId == scene)?.SceneName ?? scene;
            string path = SceneBuilderBase.Root + "/Scenes/" + scene + ".unity";
            if (!File.Exists(path)) { Debug.LogError("Play test: scene not built: " + path); return; }
            // Play starts in the requested scene without touching whatever scene is open in the editor.
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            Directory.CreateDirectory("Logs/Blockout");
            File.WriteAllText(Report, $"Blockout play test from {scene} ({request}) {DateTime.Now}\n");
            SessionState.SetBool(Key, true);
            SessionState.SetString(SceneKey, scene);
            SessionState.SetFloat(SpeedKey, speed);
            EditorApplication.EnterPlaymode();
            EditorApplication.LockReloadAssemblies(); // script edits during the run must not reload the domain mid-play
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (!SessionState.GetBool(Key, false) || hooked) return;
            hooked = true;
            errors = 0;
            spoken.Clear();
            currentScene = "";
            lastTime = -1;
            Application.logMessageReceived += OnLog;
            SubtitleTrack.LineStarted += OnLine;
            EditorApplication.update += Tick;
        }

        static void Note(string text) => File.AppendAllText(Report, text + "\n");

        static void Check(string what, bool ok)
        {
            if (!ok) errors++;
            Note((ok ? "PASS  " : "FAIL  ") + what);
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            errors++;
            Note("ERROR " + message + "\n" + stack);
        }

        static void OnLine(DialogueLine line)
        {
            var director = ModuleFlow.Instance ? ModuleFlow.Instance.Director : null;
            string last = spoken.LastOrDefault();
            Check($"{(director ? director.time : 0),6:0.0}s  {line.id}  {(line.vo ? (BlockoutAssets.IsScratch(line.vo) ? "scratch" : "real") : "silent")}  \"{line.text}\"", last != line.id);
            spoken.Add(line.id);
            lastProgress = Time.realtimeSinceStartup;
            // A frame later so the subtitle is on screen in the picture.
            EditorApplication.delayCall += () => Shot(line.id);
        }

        // Game-view screenshot for the visual review: Logs/Blockout/shots/<scene>/<nn>_<what>.png
        static void Shot(string what)
        {
            if (!EditorApplication.isPlaying) return;
            string scene = SceneManager.GetActiveScene().name;
            Directory.CreateDirectory($"Logs/Blockout/shots/{scene}");
            ScreenCapture.CaptureScreenshot($"Logs/Blockout/shots/{scene}/{++shotIndex:00}_{what}.png");
        }

        static void Tick()
        {
            // Play mode stopped from the editor: close the test down so playModeStartScene does not stick.
            if (!EditorApplication.isPlaying) { if (started > 0) { Note("STOPPED  play mode exited before the end card"); Finish(); } return; }
            if (started == 0) { started = Time.realtimeSinceStartup; lastProgress = started; Application.runInBackground = true; }
            ReviewMode.autoAnswer = true;
            if (!Mathf.Approximately(ReviewMode.timeScale, SessionState.GetFloat(SpeedKey, 1f))) ReviewMode.timeScale = SessionState.GetFloat(SpeedKey, 1f);

            var flow = ModuleFlow.Instance;
            string scene = SceneManager.GetActiveScene().name;
            if (scene != currentScene)
            {
                // Lines spoken in the first frame of the new scene belong to it, not to the one just verified.
                var early = spoken.Where(id => id.StartsWith(scene.Substring(0, 7).Replace("Scene", "S"))).ToList();
                spoken.RemoveAll(early.Contains);
                if (currentScene != "") Verify(currentScene);
                spoken.AddRange(early);
                currentScene = scene;
                shotIndex = 0;
                Note($"\n=== {scene} ===");
                lastProgress = Time.realtimeSinceStartup;
                lastTime = -1;
            }
            if (SceneDirector.Gate != lastGate)
            {
                if (SceneDirector.Gate != null) { Note($"{(flow && flow.Director ? flow.Director.time : 0),6:0.0}s  GATE {SceneDirector.Gate} (autoAnswer)"); string g = SceneDirector.Gate; EditorApplication.delayCall += () => Shot("gate_" + g); }
                lastGate = SceneDirector.Gate;
                lastProgress = Time.realtimeSinceStartup;
            }
            if (flow && flow.Director && flow.Director.time != lastTime) { lastTime = flow.Director.time; lastProgress = Time.realtimeSinceStartup; }

            bool stalled = Time.realtimeSinceStartup - lastProgress > StallSeconds;
            var note = GameObject.Find("WhiteFade") ? GameObject.Find("WhiteFade").GetComponentInChildren<TMPro.TMP_Text>() : null;
            bool ended = note && (note.text.Contains("Scene 14") || note.text.Contains("not built yet") || note.text.Contains("End of module"));
            bool timedOut = Time.realtimeSinceStartup - started > MaxMinutes * 60;
            if (!stalled && !ended && !timedOut) return;

            if (ended) Verify(currentScene);
            Check("module reached the end card / hand-off without stalling", ended && !stalled);
            if (ended) Note("END  " + note.text);
            if (stalled) Note($"STALL  no progress for {StallSeconds} s in {scene} at {lastTime:0.0} s (gate: {lastGate ?? "none"})");
            Note(errors == 0 ? "RESULT PASS" : "RESULT FAIL (" + errors + ")");
            Finish();
        }

        // Every subtitle clip in the scene's Timeline must have been spoken, in order.
        static void Verify(string scene)
        {
            var builder = BlockoutAssets.Builders().FirstOrDefault(b => b.SceneName == scene);
            if (builder == null) return;
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(SceneBuilderBase.Root + "/" + builder.SceneId + "/Timeline/" + builder.SceneId + "_Timeline.playable");
            if (!timeline) return;
            var planned = timeline.GetOutputTracks().OfType<SubtitleTrack>().SelectMany(t => t.GetClips()).OrderBy(c => c.start).Select(c => ((SubtitleClip)c.asset).line.id).ToList();
            var heard = spoken.Where(planned.Contains).ToList();
            Check($"{scene}: all {planned.Count} Timeline lines spoken in order ({heard.Count} heard)", heard.SequenceEqual(planned));
            spoken.Clear();
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            SubtitleTrack.LineStarted -= OnLine;
            SessionState.SetBool(Key, false);
            hooked = false;
            started = 0;
            ReviewMode.timeScale = 1f;
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.UnlockReloadAssemblies();
            EditorApplication.ExitPlaymode();
        }
    }
}
