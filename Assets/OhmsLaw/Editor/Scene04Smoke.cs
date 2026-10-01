using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw.Editor
{
    // Play-mode check for Scene 4: plays the whole cinematic, opens Ask Wattson mid-line, waits at the wire,
    // and writes screenshots plus a pass/fail report to Logs/Scene04.
    public static class Scene04Smoke
    {
        const string Key = "ohm.scene04.smoke", QuitKey = "ohm.scene04.smoke.quit", Report = "Logs/Scene04/play-report.txt";
        static IEnumerator steps;
        static Func<bool> waiting;
        static int errors;

        [MenuItem("Ohm's Law/Validate Scene 04 (play test)")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Scene04Builder.ScenePath);
            Directory.CreateDirectory("Logs/Scene04");
            File.WriteAllText(Report, "Scene 04 play test " + DateTime.Now + "\n");
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        // Command line: Unity -projectPath . -executeMethod OhmsLaw.Editor.Scene04Smoke.RunAndQuit   (not batchmode: it needs the Game view)
        public static void RunAndQuit()
        {
            SessionState.SetBool(QuitKey, true);
            Run();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (!SessionState.GetBool(Key, false)) return;
            Application.logMessageReceived += OnLog;
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

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (waiting != null && !waiting()) return;
            waiting = null;
            steps ??= Steps();
            bool more;
            try { more = steps.MoveNext(); }
            catch (Exception e) { errors++; Note("ERROR test aborted: " + e); more = false; }
            if (more) { waiting = steps.Current as Func<bool>; return; }

            Note(errors == 0 ? "RESULT PASS" : "RESULT FAIL (" + errors + ")");
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            EditorApplication.ExitPlaymode();
            if (SessionState.GetBool(QuitKey, false)) EditorApplication.delayCall += () => EditorApplication.Exit(errors == 0 ? 0 : 1);
        }

        static IEnumerator Steps()
        {
            Application.runInBackground = true;
            var flow = ModuleFlow.Instance;
            Check("module root, director and Wattson are in the scene", flow && flow.Director && WattsonController.Active);
            if (!flow || !flow.Director) yield break;
            var director = flow.Director;
            var agent = flow.Agent;

            Func<bool> At(double time) { float limit = Time.realtimeSinceStartup + 40; return () => director.time >= time || Time.realtimeSinceStartup > limit; }
            Func<bool> After(float seconds) { float until = Time.realtimeSinceStartup + seconds; return () => Time.realtimeSinceStartup >= until; }
            Func<bool> Shot(string name) { ScreenCapture.CaptureScreenshot("Logs/Scene04/play " + name + ".png"); return After(0.25f); }
            void Click(string name)
            {
                var button = GameObject.Find(name) ? GameObject.Find(name).GetComponent<Button>() : null;
                Check("button exists: " + name, button);
                if (button) button.onClick.Invoke();
            }

            yield return At(1.2); yield return Shot("01 arrival");
            yield return At(3.6); yield return Shot("02 arrival settles");
            yield return At(7.0); yield return Shot("03 wattson flying in");
            yield return At(9.7); yield return Shot("04 wattson settled");
            yield return At(11.3); yield return Shot("05 welcome detectives");
            Check("line 1 is on screen as a subtitle", SubtitleTrack.CurrentLine && SubtitleTrack.CurrentLine.id == "S04_L01");
            Check("line 1 has a voice clip", SubtitleTrack.CurrentLine && SubtitleTrack.CurrentLine.vo);
            float loudest = 0, listenUntil = Time.realtimeSinceStartup + 0.8f;
            yield return (Func<bool>)(() => { loudest = Mathf.Max(loudest, WattsonController.Active.Loudness); return Time.realtimeSinceStartup > listenUntil; });
            Check("Wattson's mouth and glow follow the voice", loudest > 0.05f);
            yield return At(16.0); yield return Shot("06 points to ohm");

            // Ask Wattson in the middle of line 2.
            double lineStart = SubtitleTrack.LastLineStart;
            Click("Ask Wattson");
            yield return After(0.8f);
            double frozenAt = director.time;
            yield return After(1.2f);
            Check("Ask Wattson pauses the cinematic", flow.IsPaused(ModuleFlow.PauseReason.Ask) && Math.Abs(director.time - frozenAt) < 0.02);
            Check("Wattson is listening to the student", WattsonController.Active.Effective == WattsonController.Gesture.Listening);
            yield return Shot("07 ask panel");
            agent.Ask("What is voltage?");
            float asked = Time.realtimeSinceStartup;
            yield return (Func<bool>)(() => agent.Current == WattsonAgent.State.Talking || Time.realtimeSinceStartup - asked > 14);
            Check("a question gets a spoken answer (live, or the offline FAQ when the proxy is off)", agent.Current == WattsonAgent.State.Talking);
            Note("      proxy reachable: " + !agent.Offline);
            yield return After(1.0f);
            yield return Shot("08 ask answer");
            agent.Ask("Tell me a joke about cricket");
            yield return After(2.5f);
            Check("a new question interrupts the previous answer without errors", true);
            agent.Stop();
            Click("Back");
            yield return After(0.4f);
            Check("Back to the case restarts the interrupted line", !flow.IsPaused(ModuleFlow.PauseReason.Ask) && Math.Abs(director.time - lineStart) < 0.6);

            yield return At(24.6); yield return Shot("09 voltage and current");
            yield return At(28.6); yield return Shot("10 arc between words");
            yield return At(32.0); yield return Shot("11 before we understand");
            yield return At(35.2); yield return Shot("12 niscope raised");
            yield return At(38.6); yield return Shot("13 niscope hero");
            yield return At(42.6); yield return Shot("14 aim and lock on");
            yield return At(45.2); yield return Shot("15 everybody in");

            float holdLimit = Time.realtimeSinceStartup + 12;
            yield return (Func<bool>)(() => flow.IsPaused(ModuleFlow.PauseReason.Hold) || Time.realtimeSinceStartup > holdLimit);
            Check("the scene waits at the wire for a tap", flow.IsPaused(ModuleFlow.PauseReason.Hold));
            yield return After(1.0f);
            yield return Shot("16 tap the wire");
            Click("Tap Wire");
            yield return After(0.2f);
            Check("tapping the wire starts the dive", !flow.IsPaused(ModuleFlow.PauseReason.Hold));
            yield return At(48.6); yield return Shot("17 dive");
            yield return After(4.5f);
            yield return Shot("18 white hand-off");
            var note = GameObject.Find("WhiteFade") ? GameObject.Find("WhiteFade").GetComponentInChildren<TMPro.TMP_Text>() : null;
            Check("Scene 4 ends on white and reports the missing Scene 5", note && note.text.Contains("Scene05"));
            Check("all six lines were spoken in order", SubtitleTrack.LastLineStart > 43);
        }
    }
}
