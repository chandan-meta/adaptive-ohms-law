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
        static TimelineAsset sceneTimeline; // the active scene's Timeline, for Verify() when no builder owns the scene (Scene 4)
        static int shotIndex;
        static string currentScene = "", lastGate;
        static double lastTime = -1;
        static float lastProgress, started;
        static int errors, probe, hud;
        static float endedAt, probeAt, hudAt;
        static double hudTime;
        // "hud" in the request: the probe replays and skips, so repeated or missing lines are expected in that scene.
        static bool HudProbe => SessionState.GetString(SceneKey + ".req", "").Contains("hud");
        static bool hooked;

        [MenuItem("Ohm's Law/Play Test/Whole module (autoAnswer)")] static void Module() => Run("all");
        [MenuItem("Ohm's Law/Play Test/Current scene (autoAnswer)")] static void Current() => Run(SceneManager.GetActiveScene().name);

        // scene: a scene name, a scene id ("Scene05"), or "all" (Scene 4 to the end card). Append " 2x" for double speed.
        public static void Run(string request)
        {
            string[] words = request.Split(' ');
            string scene = words[0];
            var res = words.FirstOrDefault(w => w.StartsWith("@") && w.Contains("x"));
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
            SessionState.SetString(SceneKey + ".req", request);
            SessionState.SetFloat(SpeedKey, speed);
            void Start()
            {
                EditorApplication.EnterPlaymode();
                EditorApplication.LockReloadAssemblies(); // script edits during the run must not reload the domain mid-play
            }
            if (res == null) { Start(); return; }
            // "@WxH": the Game view takes that size first and gets a few editor updates to lay out at it (Screen.width follows
            // the view's own size selection, not a plain index change), then play starts.
            var wh = res.Substring(1).Split('x');
            bool sized = GameViewSize.Set(int.Parse(wh[0]), int.Parse(wh[1]));
            Note(sized ? $"SIZE  Game view set to {wh[0]}x{wh[1]} ({GameViewSize.Views} view(s))" : "SIZE  Game view size could not be set (see the console); running at the current size");
            int updates = 0;
            void Countdown()
            {
                if (++updates < 4) return;
                EditorApplication.update -= Countdown;
                Start();
            }
            EditorApplication.update += Countdown;
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (!SessionState.GetBool(Key, false) || hooked) return;
            hooked = true;
            errors = 0; probe = 0; endedAt = 0; probeAt = 0; resNoted = false; firstFrame = null; hud = 0;
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
            Check($"{(director ? director.time : 0),6:0.0}s  {line.id}  {(line.vo ? (BlockoutAssets.IsScratch(line.vo) ? "scratch" : "real") : "silent")}  \"{line.text}\"", last != line.id || HudProbe);
            spoken.Add(line.id);
            lastProgress = Time.realtimeSinceStartup;
            Later(line.id, LineShotDelay);
        }

        // Game-view screenshot for the visual review: Logs/Blockout/shots/<scene>/<nn>_<what>.png
        static bool resNoted;
        static string firstFrame; // the first real frame of the run: its pixel size is the resolution the run played at

        static void Shot(string what)
        {
            if (!EditorApplication.isPlaying) return;
            string scene = SceneManager.GetActiveScene().name;
            Directory.CreateDirectory($"Logs/Blockout/shots/{scene}");
            string path = $"Logs/Blockout/shots/{scene}/{++shotIndex:00}_{what}.png";
            ScreenCapture.CaptureScreenshot(path);
            if (!resNoted && what != "enter") { resNoted = true; firstFrame = path; } // the first "enter" frame can precede a Game view resize
        }

        // The resolution the run played at, read from the first captured frame's PNG header (Screen.width from an editor
        // update callback reports whichever view painted last, so it is not trusted here).
        static void NoteResolution()
        {
            try
            {
                if (firstFrame == null || !File.Exists(firstFrame)) return;
                var bytes = new byte[24];
                using (var file = File.OpenRead(firstFrame)) if (file.Read(bytes, 0, 24) < 24) return;
                int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                Note($"RES   {w}x{h} (captured frames)");
            }
            catch (Exception) { }
        }

        // The Captures rules for this director's Timeline, once per director (a new scene brings a new one). Only a
        // director in the active scene counts, so a scene being unloaded never plans for the next one.
        static void PlanCaptures(PlayableDirector director, string scene)
        {
            if (director == capturesFor || director.gameObject.scene.name != scene) return;
            capturesFor = director;
            captures.Clear();
            sceneTimeline = director.playableAsset as TimelineAsset;
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
            if (started == 0) { started = Time.realtimeSinceStartup; lastProgress = started; Application.runInBackground = true; SubtitleUI.Enabled = true; ReducedMotion.On = SessionState.GetString(SceneKey + ".req", "").Contains("calm"); } // captures show full motion unless the request says "calm"
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
                captures.Clear(); capturesFor = null; sceneTimeline = null; // planned again from the new scene's own Timeline
                if (Directory.Exists($"Logs/Blockout/shots/{scene}")) Directory.Delete($"Logs/Blockout/shots/{scene}", true); // no frames left over from a longer run
                Later("enter", 0.05f); // the scene's first frame, for the match-cut review against the previous scene's "exit"
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

            // "ask" in the request: in Scene 7 the test opens Ask Wattson, asks a question and closes it again (frames
            // ask_open / ask_answer / ask_closed), so the conversation, the pause and the resume get a visual check.
            if (SessionState.GetString(SceneKey + ".req", "").Contains("ask") && scene.StartsWith("Scene07") && flow && flow.Director && LessonPanel.Instance && probe < 3)
            {
                if (probe == 0 && flow.Director.time >= 5) { LessonPanel.Instance.AskButton.onClick.Invoke(); probe = 1; probeAt = Time.realtimeSinceStartup; Later("ask_open", 0.8f); }
                else if (probe == 1 && Time.realtimeSinceStartup - probeAt > 1.6f) { AskWattsonUI.Probe("What is electric current?"); probe = 2; probeAt = Time.realtimeSinceStartup; Later("ask_answer", 3f); }
                else if (probe == 2 && Time.realtimeSinceStartup - probeAt > 4.5f) { LessonPanel.Instance.AskButton.onClick.Invoke(); probe = 3; Later("ask_closed", 0.8f); Note($"ASK   probe in {scene}: opened, asked, closed (open: {AskWattsonUI.IsOpen})"); }
                lastProgress = Time.realtimeSinceStartup;
            }

            // "hud" in the request: in Scene 5 the probe presses every top control (PAUSE, RESUME, CC, VOLUME, REPLAY, SKIP) and
            // checks what each one did, with a frame of each state.
            if (HudProbe && scene.StartsWith("Scene05") && flow && flow.Director && LessonPanel.Instance && hud < 99)
            {
                var panel = LessonPanel.Instance;
                double t = flow.Director.time;
                float since = Time.realtimeSinceStartup - hudAt;
                switch (hud)
                {
                    case 0: if (t >= 4) { panel.Press("pause"); hudTime = t; hudAt = Time.realtimeSinceStartup; hud = 1; Later("hud_paused", 0.6f); } break;
                    case 1: if (since > 1.5f) { Check($"HUD PAUSE holds the Timeline ({flow.Director.time:0.00} s, was {hudTime:0.00} s) and reads RESUME", Math.Abs(flow.Director.time - hudTime) < 0.05 && flow.IsPaused(ModuleFlow.PauseReason.User)); panel.Press("pause"); hudAt = Time.realtimeSinceStartup; hud = 2; } break;
                    case 2: if (since > 1.0f) { Check($"HUD RESUME runs the Timeline again ({flow.Director.time:0.00} s)", flow.Director.time > hudTime + 0.3 && !flow.IsPaused(ModuleFlow.PauseReason.User)); panel.Press("cc"); hudAt = Time.realtimeSinceStartup; hud = 3; Later("hud_cc_off", 0.6f); } break;
                    case 3: if (since > 0.9f) { Check("HUD CC OFF hides the captions", !SubtitleUI.Enabled); panel.Press("cc"); Check("HUD CC ON shows them again", SubtitleUI.Enabled); panel.Press("volume"); hudAt = Time.realtimeSinceStartup; hud = 4; Later("hud_volume", 0.6f); } break;
                    case 4: if (since > 0.9f) { Check("HUD VOLUME opens the popup", panel.PopupOpen); panel.ClosePopup(); Check("HUD a click elsewhere closes the popup", !panel.PopupOpen); hudTime = flow.Director.time; panel.Press("replay"); hudAt = Time.realtimeSinceStartup; hud = 5; } break;
                    case 5: if (since > 0.4f) { Check($"HUD REPLAY restarts the current line ({flow.Director.time:0.00} s, line start {SubtitleTrack.LastLineStart:0.00} s)", flow.Director.time < hudTime && Math.Abs(flow.Director.time - SubtitleTrack.LastLineStart) < 1.5); hudTime = flow.Director.time; panel.Press("skip"); hudAt = Time.realtimeSinceStartup; hud = 6; Later("hud_skipped", 0.6f); } break;
                    case 6: if (since > 0.4f) { Check($"HUD SKIP jumps ahead to the next line ({flow.Director.time:0.00} s, was {hudTime:0.00} s)", flow.Director.time > hudTime + 0.5); hud = 99; } break;
                }
                lastProgress = Time.realtimeSinceStartup;
            }

            bool stalled = Time.realtimeSinceStartup - lastProgress > StallSeconds;
            var note = GameObject.Find("WhiteFade") ? GameObject.Find("WhiteFade").GetComponentInChildren<TMPro.TMP_Text>() : null;
            bool ended = note && (note.text.Contains("Scene 14") || note.text.Contains("not built yet") || note.text.Contains("End of module"))
                      || (ModuleFlow.Instance && ModuleFlow.Instance.Ended);
            bool timedOut = Time.realtimeSinceStartup - started > MaxMinutes * 60;
            if (ended && endedAt == 0) { endedAt = Time.realtimeSinceStartup; Later("end_card", 0.5f); }
            if (ended && !stalled && Time.realtimeSinceStartup - endedAt < 1.5f) return; // the end card's frame (replay / exit) lands before play mode ends
            if (!stalled && !ended && !timedOut) return;

            if (ended) Verify(currentScene);
            Check("module reached the finale / end card without stalling", ended && !stalled);
            if (ended) Note("END  " + (note && note.text.Length > 0 ? note.text : "finale (replay / exit offered)"));
            if (stalled) Note($"STALL  no progress for {StallSeconds} s in {scene} at {lastTime:0.0} s (gate: {lastGate ?? "none"})");
            NoteResolution();
            Note(errors == 0 ? "RESULT PASS" : "RESULT FAIL (" + errors + ")");
            Finish();
        }

        // Every subtitle clip in the scene's Timeline must have been spoken, in order. A scene with no SceneBuilderBase
        // (Scene 4) is checked against the Timeline its director was playing.
        static void Verify(string scene)
        {
            var builder = BlockoutAssets.Builders().FirstOrDefault(b => b.SceneName == scene);
            var timeline = builder != null
                ? AssetDatabase.LoadAssetAtPath<TimelineAsset>(SceneBuilderBase.Root + "/" + builder.SceneId + "/Timeline/" + builder.SceneId + "_Timeline.playable")
                : sceneTimeline;
            if (!timeline) return;
            var planned = timeline.GetOutputTracks().OfType<SubtitleTrack>().SelectMany(t => t.GetClips()).OrderBy(c => c.start).Select(c => ((SubtitleClip)c.asset).line.id).ToList();
            var heard = spoken.Where(planned.Contains).ToList();
            if (HudProbe && scene.StartsWith("Scene05")) Note($"NOTE  {scene}: {heard.Count} of {planned.Count} lines heard (the HUD probe replayed and skipped here)");
            else Check($"{scene}: all {planned.Count} Timeline lines spoken in order ({heard.Count} heard)", heard.SequenceEqual(planned));
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
            if (SessionState.GetString(SceneKey + ".req", "").Contains("@")) GameViewSize.Set(1920, 1080);
        }
    }

    // The Game view's resolution, through the editor's internal GameViewSizes (there is no public API): a custom
    // fixed-resolution entry is added to the Standalone group when none matches, then selected.
    public static class GameViewSize
    {
        public static int Views { get; private set; }

        public static bool Set(int width, int height)
        {
            try
            {
                var asm = typeof(UnityEditor.Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singleton.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null);
                var groupType = asm.GetType("UnityEditor.GameViewSizeGroupType");
                var group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { (int)Enum.Parse(groupType, "Standalone") });
                var groupClass = asm.GetType("UnityEditor.GameViewSizeGroup");
                var sizeClass = asm.GetType("UnityEditor.GameViewSize");
                int count = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null);
                int found = -1;
                for (int i = 0; i < count && found < 0; i++)
                {
                    var size = groupClass.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                    int w = (int)sizeClass.GetProperty("width").GetValue(size), h = (int)sizeClass.GetProperty("height").GetValue(size);
                    if (w == width && h == height && sizeClass.GetProperty("sizeType").GetValue(size).ToString() == "FixedResolution") found = i;
                }
                if (found < 0)
                {
                    var kind = asm.GetType("UnityEditor.GameViewSizeType");
                    var size = sizeClass.GetConstructor(new[] { kind, typeof(int), typeof(int), typeof(string) }).Invoke(new object[] { Enum.Parse(kind, "FixedResolution"), width, height, $"Test {width}x{height}" });
                    groupClass.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                    found = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                }
                // Every Game view takes the size (Screen.width/height follow the main play-mode view, the capture the focused
                // one; with one layout they are the same window), through the view's own selection path (what its size
                // dropdown calls): a bare index change only re-sizes the capture.
                var gameViewType = asm.GetType("UnityEditor.GameView");
                var views = Resources.FindObjectsOfTypeAll(gameViewType);
                if (views.Length == 0) views = new UnityEngine.Object[] { EditorWindow.GetWindow(gameViewType) };
                var select = gameViewType.GetMethod("SizeSelectionCallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var index = gameViewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                foreach (var view in views)
                {
                    if (select != null) select.Invoke(view, new object[] { found, null });
                    else index.SetValue(view, found);
                    ((EditorWindow)view).Repaint();
                }
                Views = views.Length;
                return true;
            }
            catch (Exception error) { Debug.LogWarning("GameViewSize: " + error.Message); return false; }
        }
    }
}
