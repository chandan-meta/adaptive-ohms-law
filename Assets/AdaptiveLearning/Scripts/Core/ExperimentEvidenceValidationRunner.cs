#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public sealed class ExperimentEvidenceValidationRunner : MonoBehaviour
    {
        readonly List<string> results = new List<string>();
        readonly List<string> errors = new List<string>();
        void Awake() { DontDestroyOnLoad(gameObject); Application.logMessageReceived += Log; }
        void Log(string text, string stack, LogType type)
        { if ((type == LogType.Error || type == LogType.Exception) && !(stack ?? "").Contains("UnityEditor.Search.SearchDatabase")) errors.Add(text); }
        void Check(bool condition, string label)
        {
            results.Add((condition ? "PASS " : "FAIL ") + label); File.WriteAllLines("Logs/Measurement-handoff.txt", results);
            if (!condition) { if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(1); throw new Exception(label); }
        }
        IEnumerator Until(Func<bool> condition, string label)
        {
            float end = Time.realtimeSinceStartup + 30;
            while (!condition() && Time.realtimeSinceStartup < end)
            {
                var presenter = FindFirstObjectByType<DialoguePresenter>();
                if (presenter != null && presenter.IsPresenting) presenter.SkipCurrentLine();
                yield return null;
            }
            Check(condition(), label);
        }
        IEnumerator Start()
        {
            yield return null;
            var experiment = FindFirstObjectByType<Scene07FlowManager>();
            yield return Until(() => experiment.Stage == Scene07Stage.Experiment, "Scene 10 reaches interaction");
            var controls = experiment.Experiment;
            Check(!controls.RecordCurrentReading(), "Open circuit cannot record a reading");
            controls.SetSwitch(true);
            controls.SelectVoltage(1.4f);
            Check(!controls.RecordCurrentReading(), "Between-detent reading is rejected");
            for (int i = 1; i <= 5; i++)
            {
                controls.SelectVoltage(i);
                Check(Mathf.Abs(controls.Circuit.Current - i / 5f) < .001f, "Meter current matches actual 5-ohm circuit at " + i + " V");
                Check(controls.RecordCurrentReading(), "Record measurement " + i);
                if (i == 1) Check(!controls.RecordCurrentReading(), "Duplicate reading does not advance progress");
                yield return null;
            }
            Check(LessonExperimentData.IsValid(controls.Table.Observations), "All five measured observations are preserved");
            var readouts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check(readouts.First(t => t.name == "AmmeterReadout").text == "1.00 A" && readouts.First(t => t.name == "VoltmeterReadout").text == "5.00 V", "HUD agrees with the imported meters and circuit");
            ScreenCapture.CaptureScreenshot("Logs/Five-reading-experiment-1440x1080.png"); yield return null; yield return null;
            yield return Until(() => experiment.Stage == Scene07Stage.EvidenceQuestion, "Evidence question follows fifth reading");
            experiment.Questions.Submit(0);
            // Wait through the conclusion using normal skip behavior, then request completion.
            for (int i = 0; i < 120 && experiment.Stage != Scene07Stage.Completed; i++)
            { FindFirstObjectByType<DialoguePresenter>().SkipCurrentLine(); experiment.CompleteScene07(); yield return null; }
            Check(experiment.Stage == Scene07Stage.Completed, "Experiment completion saves collected data");
            string sourceId = experiment.Profiles.Profile.experiments.Last().sessionId;
            yield return SceneManager.LoadSceneAsync("Scene_11_TurnDataIntoGraph"); yield return null;
            var graph = FindFirstObjectByType<Scene08FlowManager>();
            Check(!graph.Record.sampleData && graph.Record.sourceSessionId == sourceId, "Graph uses the learner's experiment, not samples");
            yield return Until(() => graph.Stage == Scene08Stage.Plotting, "Scene 11 reaches plotting");
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            buttons.First(b => b.name == "Plot_2_1").onClick.Invoke();
            Check(graph.Record.plottingMistakes == 1 && graph.Record.plottedIndices.Count == 0, "Incorrect graph point gives support without recording a point");
            for (int i = 1; i <= 5; i++) buttons.First(b => b.name == "Plot_" + i + "_" + i).onClick.Invoke();
            Check(graph.Record.plottedIndices.Count == 5, "Five graph intersections accept the actual measurements");
            yield return new WaitForSecondsRealtime(1.5f);
            ScreenCapture.CaptureScreenshot("Logs/Five-reading-graph-1440x1080.png"); yield return new WaitForSecondsRealtime(.5f);
            yield return SceneManager.LoadSceneAsync("Scene_12_DiscoverResistance"); yield return null;
            var profile = FindFirstObjectByType<StudentProfileManager>().Profile;
            Check(!profile.scene12Activities.Last().sampleData, "Scene 12 recognizes the measured dataset without practice substitution");
            Check(profile.experiments.Last().sessionId == sourceId && LessonExperimentData.IsValid(profile.experiments.Last().observations), "Scene changes preserve evidence and provenance");
            Check(errors.Count == 0, "No lesson runtime errors during measurement handoff");
            results.Add("COMPLETE"); File.WriteAllLines("Logs/Measurement-handoff.txt", results);
            UnityEditor.EditorApplication.isPlaying = false;
            if (Application.isBatchMode) UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.Exit(0);
        }
        void OnDestroy() => Application.logMessageReceived -= Log;
    }
}
#endif
