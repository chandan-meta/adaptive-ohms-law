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
        // Screenshots wait for UI fades (subtitle box, MCQ panel: UIFade 0.25 s) so the picture shows them at full alpha.
        static readonly List<(float due, string what)> pending = new List<(float, string)>();
        const float LineShotDelay = 0.4f, GateShotDelay = 0.6f;

        // Extra review frames, `delay` Timeline seconds after an event: the first `what` in that scene's Timeline that comes
        // at or after the first `after` ("" = from the start). Events are the Timeline's cue commands as the builders write
        // them ("show Drift Arrow", "rig.voltage 2", "appear 0.7 AMMETER") and its camera shots as "shot <camera>". Timeline
        // time, so a gate's hold or a 2x run moves the frame with the beat. A rule whose events are missing shoots nothing.
        static readonly (string scene, string after, string what, float delay)[] Captures =
        {
            ("Scene05", "", "show Card Terminal Label +", 1.0f),     // both terminals lit and labelled
            ("Scene05", "", "show Drift Arrow", 1.0f),               // the drift confirmed after S05_Drift
            ("Scene08", "", "appear 0.7 AMMETER", 0.4f),             // the transformation: each swap mid-way (old out, new in)
            ("Scene08", "", "appear 0.7 cells / controllable DC source", 0.4f),
            ("Scene08", "", "appear 0.7 NICHROME WIRE — XY", 0.4f),
            ("Scene08", "", "appear 0.7 switch / key", 0.4f),
            ("Scene08", "", "glide 0.9 Bench", 0.5f),                // the desk becoming the bench
            ("Scene08", "", "look 2.8 classroom", 0.9f),             // the room sweep, twice
            ("Scene08", "", "look 2.8 classroom", 2.0f),
            ("Scene12", "", "rig.voltage 2", 2.5f),                  // inside the wire at 2 V: the counter at 4
            ("Scene12", "lab", "shot Bench Wide", 1.2f),             // back out of the wire, on the bench (2.0 V / 0.40 A)
            ("Scene13", "", "fx Title Ignite", 0.9f),                // OHM'S LAW lit mid-sweep
            ("Scene13", "", "shot Question", 1.1f),                  // the Question frame just before the AQ2 panel
            ("Scene13", "", "shot Apply", 1.5f),                     // the Apply frame before the gate: line, live point, strip, meters, V = IR
            ("Scene13", "", "shot Wattson", 1.4f),                   // the closing frame with Title Back / Law Back
        };
        static readonly List<(double due, string what)> captures = new List<(double, string)>();
        static PlayableDirector capturesFor;
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
            ModuleFlow.Leaving += OnLeaving;
            EditorApplication.update += Tick;
        }

        // The scene's last frame, before the white fade or the match-cut, so hand-offs can be compared.
        static void OnLeaving(string next) => Shot("exit");

        static void Later(string what, float seconds) => pending.Add((Time.realtimeSinceStartup + seconds, what));

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
            Later(line.id, LineShotDelay);
        }

        // Game-view screenshot for the visual review: Logs/Blockout/shots/<scene>/<nn>_<what>.png
        static void Shot(string what)
        {
            if (!EditorApplication.isPlaying) return;
            string scene = SceneManager.GetActiveScene().name;
            Directory.CreateDirectory($"Logs/Blockout/shots/{scene}");
            ScreenCapture.CaptureScreenshot($"Logs/Blockout/shots/{scene}/{++shotIndex:00}_{what}.png");
        }

        // The Captures rules for this director's Timeline, once per director (a new scene brings a new one). Only a
        // director in the active scene counts, so a scene being unloaded never plans for the next one.
        static void PlanCaptures(PlayableDirector director, string scene)
        {
            if (director == capturesFor || director.gameObject.scene.name != scene) return;
            capturesFor = director;
            captures.Clear();
            if (!(director.playableAsset is TimelineAsset timeline)) return;
            var events = new List<(double at, string name)>();
            if (timeline.markerTrack) events.AddRange(timeline.markerTrack.GetMarkers().OfType<Cue>().Select(cue => (cue.time, cue.command)));
            foreach (var track in timeline.GetOutputTracks().Where(t => t.name == "Cameras"))
                events.AddRange(track.GetClips().Select(clip => (clip.start, "shot " + clip.displayName)));
            events = events.OrderBy(e => e.at).ToList();
            foreach (var rule in Captures.Where(r => scene.StartsWith(r.scene + "_")))
            {
                double from = 0;
                if (rule.after != "")
                {
                    var anchor = events.FirstOrDefault(e => e.name == rule.after);
                    if (anchor.name == null) continue;
                    from = anchor.at;
                }
                var hit = events.FirstOrDefault(e => e.at >= from && e.name == rule.what);
                if (hit.name != null) captures.Add((hit.at + rule.delay, $"at_{Safe(rule.what)}+{rule.delay:0.0}s"));
            }
        }

        static string Safe(string name) => new string(name.Select(c => char.IsLetterOrDigit(c) || c == '.' || c == '+' || c == '-' ? c : '_').ToArray());

        static void Tick()
        {
            // Play mode stopped from the editor: close the test down so playModeStartScene does not stick.
            if (!EditorApplication.isPlaying) { if (started > 0) { Note("STOPPED  play mode exited before the end card"); Finish(); } return; }
            if (started == 0) { started = Time.realtimeSinceStartup; lastProgress = started; Application.runInBackground = true; SubtitleUI.Enabled = true; }
            foreach (var due in pending.Where(p => Time.realtimeSinceStartup >= p.due).ToList()) { Shot(due.what); pending.Remove(due); }
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
                pending.Clear(); // a shot due after the cut would land in the new scene's folder
                captures.Clear(); capturesFor = null; // planned again from the new scene's own Timeline
                if (Directory.Exists($"Logs/Blockout/shots/{scene}")) Directory.Delete($"Logs/Blockout/shots/{scene}", true); // no frames left over from a longer run
                Note($"\n=== {scene} ===");
                lastProgress = Time.realtimeSinceStartup;
                lastTime = -1;
            }
            if (SceneDirector.Gate != lastGate)
            {
                if (SceneDirector.Gate != null) { Note($"{(flow && flow.Director ? flow.Director.time : 0),6:0.0}s  GATE {SceneDirector.Gate} (autoAnswer)"); Later("gate_" + SceneDirector.Gate, GateShotDelay); }
                lastGate = SceneDirector.Gate;
                lastProgress = Time.realtimeSinceStartup;
            }
            if (flow && flow.Director && flow.Director.time != lastTime) { lastTime = flow.Director.time; lastProgress = Time.realtimeSinceStartup; }
            if (flow && flow.Director)
            {
                PlanCaptures(flow.Director, scene);
                foreach (var capture in captures.Where(c => flow.Director.time >= c.due).ToList()) { Shot(capture.what); captures.Remove(capture); }
            }

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
            ModuleFlow.Leaving -= OnLeaving;
            pending.Clear();
            captures.Clear();
            capturesFor = null;
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
