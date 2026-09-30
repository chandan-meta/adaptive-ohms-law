#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AdaptiveOhmsLaw
{
    public class Scene07ValidationRunner : MonoBehaviour
    {
        readonly List<string> results = new List<string>();
        readonly List<string> errors = new List<string>();
        Scene07FlowManager flow;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            Application.logMessageReceived += Log;
        }

        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(message);
        }

        void Check(bool condition, string label)
        {
            results.Add((condition ? "PASS " : "FAIL ") + label);
            File.WriteAllLines("Logs/Scene07-validation.txt", results);
            if (!condition)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                throw new Exception("Scene 07 validation: " + label);
            }
        }

        Button Button(string name) => FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.name == name);

        void Click(string name)
        {
            var button = Button(name);
            Check(button.isActiveAndEnabled && button.interactable, "Enabled control: " + name);
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }

        IEnumerator MouseClick(string name)
        {
            var button = Button(name);
            var center = (Vector2)((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center }); yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center }.WithButton(MouseButton.Left)); yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center }); yield return null;
            InputSystem.RemoveDevice(mouse);
        }

        IEnumerator TouchTap(string name)
        {
            var button = Button(name);
            var center = (Vector2)((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
            var touch = InputSystem.AddDevice<Touchscreen>();
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = center }); yield return null;
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = center }); yield return null;
            InputSystem.RemoveDevice(touch);
        }

        IEnumerator ReadyForExperiment()
        {
            float deadline = Time.realtimeSinceStartup + 20;
            while (Time.realtimeSinceStartup < deadline)
            {
                flow = FindFirstObjectByType<Scene07FlowManager>();
                if (flow != null && flow.Stage == Scene07Stage.Experiment) yield break;
                yield return null;
            }
            Check(false, "Scene reaches experiment stage within 20 seconds");
        }

        IEnumerator Start()
        {
            yield return ReadyForExperiment();

            // 1. Setup & UI Module Assertions
            Check(FindFirstObjectByType<InputSystemUIInputModule>() != null, "Shared mouse/touch UI module active");
            Check(flow.Profiles.Profile.concepts.Count == 10, "All 10 concept IDs initialized");
            Check(flow.Profiles.Profile.Mastery(ConceptId.Voltage) == 50, "Isolated test profile starts at 50 mastery");
            Check(flow.Profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship) == 50, "Relationship concept starts at 50");
            ScreenCapture.CaptureScreenshot("Logs/Scene07-desktop.png");

            // 2. Quantitative Circuit Model & Meter Tests
            var circuit = flow.Experiment.Circuit;
            Check(circuit != null, "Circuit model present");
            Check(Mathf.Abs(circuit.Resistance - 5f) < 0.01f, "Circuit resistance is fixed at 5 ohms");
            Check(!circuit.IsSwitchClosed, "Circuit switch is initially open");
            Check(circuit.Current == 0f, "Current is 0 A when switch is open");
            Check(circuit.ResistorVoltage == 0f, "Voltage across resistor is 0 V when switch is open");

            // 3. Recording Prevention when Switch is Open
            flow.Experiment.SelectVoltage(1f);
            bool openRecordSuccess = flow.Experiment.RecordCurrentReading();
            Check(!openRecordSuccess, "Cannot record reading when circuit switch is open");
            Check(flow.Experiment.Table.RecordedCount == 0, "No rows recorded while switch is open");

            // 4. Closed Circuit Tests (I = V / R)
            Click("ToggleSwitch");
            yield return new WaitForSeconds(0.2f);
            Check(circuit.IsSwitchClosed, "Switch closes via ToggleSwitch button");
            Check(Mathf.Abs(circuit.ResistorVoltage - 1.0f) < 0.01f, "Resistor voltage is 1.0 V at 1 V setting");
            Check(Mathf.Abs(circuit.Current - 0.20f) < 0.005f, "Current is 0.20 A at 1 V (I = 1V / 5 ohm)");

            // Record 1st reading (1.0 V, 0.10 A)
            yield return MouseClick("RecordReading");
            yield return new WaitForSeconds(0.2f);
            Check(flow.Experiment.Table.RecordedCount == 1, "First reading recorded in table");

            // Duplicate recording prevention
            bool duplicateRecordSuccess = flow.Experiment.RecordCurrentReading();
            Check(!duplicateRecordSuccess, "Duplicate recording of 1.0 V prevented");
            Check(flow.Experiment.Table.RecordedCount == 1, "Row count remains 1 after duplicate attempt");

            // Test 2 V setting
            for(int i=0;i<10;i++) FindFirstObjectByType<RotaryVoltageControl>().OnMove(new AxisEventData(EventSystem.current){moveDir=MoveDirection.Right});
            yield return new WaitForSeconds(0.2f);
            Check(Mathf.Abs(circuit.ResistorVoltage - 2.0f) < 0.01f, "Resistor voltage is 2.0 V at 2 V setting");
            Check(Mathf.Abs(circuit.Current - 0.40f) < 0.005f, "Current is 0.40 A at 2 V (I = 2V / 5 ohm)");

            // Record 2nd reading (2.0 V, 0.20 A)
            yield return TouchTap("RecordReading");
            yield return new WaitForSeconds(0.2f);
            Check(flow.Experiment.Table.RecordedCount == 2, "Second reading recorded via touch tap");

            // Test 3 V setting
            for(int i=0;i<10;i++) FindFirstObjectByType<RotaryVoltageControl>().OnMove(new AxisEventData(EventSystem.current){moveDir=MoveDirection.Right});
            yield return new WaitForSeconds(0.2f);
            Check(Mathf.Abs(circuit.ResistorVoltage - 3.0f) < 0.01f, "Resistor voltage is 3.0 V at 3 V setting");
            Check(Mathf.Abs(circuit.Current - 0.60f) < 0.005f, "Current is 0.60 A at 3 V (I = 3V / 5 ohm)");

            // Record 3rd reading (3.0 V, 0.30 A)
            Click("RecordReading");
            yield return new WaitForSeconds(0.2f);
            for (int voltage = 4; voltage <= 5; voltage++) { flow.Experiment.SelectVoltage(voltage); Click("RecordReading"); yield return null; }
            Check(flow.Experiment.Table.RecordedCount == 5, "All five readings recorded");
            Check(flow.Experiment.Table.IsComplete, "Observation table marked complete");

            // 5. Ask Wattson in Scene 07
            Click("AskWattson");
            Check(flow.ChatUI.IsOpen, "Ask Wattson opens");
            float relMasteryBeforeChat = flow.Profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship);
            Click("AmmeterHelp");
            yield return new WaitForSeconds(0.3f);
            var chatReply = FindObjectsByType<Text>(FindObjectsSortMode.None).First(t => t.name == "Reply");
            Check(chatReply.text.Contains("ammeter") || chatReply.text.Contains("amperes"), "Chat explains ammeter in series");
            Check(flow.Profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship) == relMasteryBeforeChat, "Asking chat has no mastery penalty");

            Click("VoltmeterHelp");
            yield return new WaitForSeconds(0.3f);
            Check(chatReply.text.Contains("voltmeter") || chatReply.text.Contains("potential difference"), "Chat explains voltmeter in parallel");

            Click("CloseChat");
            Check(!flow.ChatUI.IsOpen, "Chat closes");

            // 6. Transition to Evidence Question
            float deadlineQuestion = Time.realtimeSinceStartup + 10;
            while (Time.realtimeSinceStartup < deadlineQuestion && flow.Stage != Scene07Stage.EvidenceQuestion)
                yield return null;

            Check(flow.Stage == Scene07Stage.EvidenceQuestion, "Reaches evidence question after observations");

            // 7. Evidence Question Branching
            // Incorrect attempt: B (Current decreased)
            Click("AnswerB");
            yield return new WaitForSeconds(4.2f);
            Check(flow.Questions.Record.hintCount == 1, "Incorrect attempt records hint and displays clue");
            Check(flow.Profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship) == 48, "Incorrect attempt applies -2 penalty");

            // Try again
            Click("TryAgain");
            yield return new WaitForSeconds(0.3f);

            // Correct attempt: A (Current increased)
            yield return MouseClick("AnswerA");
            yield return new WaitForSeconds(0.5f);
            Check(flow.Questions.Record.finalAnswer == 0, "Correct answer selected");
            Check(flow.Profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship) == 52, "Assisted correct answer awards +4 gain (48 -> 52)");
            Check(flow.Profiles.Profile.Mastery(ConceptId.Voltage) == 52, "Voltage mastery updated on relevant evidence (50 -> 48 -> 52)");

            // 8. Completion & Persistence Roundtrip
            yield return new WaitForSeconds(7.5f);
            int scene07CompletedCount = 0;
            flow.OnScene07Completed += () => scene07CompletedCount++;
            Click("Continue");

            Check(scene07CompletedCount == 1, "OnScene07Completed fired exactly once");
            Check(flow.Stage == Scene07Stage.Completed, "Scene reaches completed stage");
            Check(flow.Profiles.Profile.completedActivities.Contains("Scene07OhmsExperiment"), "Activity ID added to completedActivities");

            // Check experiment record
            var expRecord = flow.Profiles.Profile.experiments.Find(e => e.activityId == "Scene07OhmsExperiment");
            Check(expRecord != null, "ExperimentRecord created and stored");
            Check(expRecord.completed, "ExperimentRecord marked completed");
            Check(expRecord.observations.Count == 5, "All 5 observations saved in experiment record");
            Check(expRecord.conclusionOutcome.Length > 0, "Conclusion outcome recorded");

            // Verify disk JSON serialization and backwards compatibility
            var diskStore = new JsonStudentProfileStore(Path.GetFullPath("Logs/Scene07-test-profile.json"));
            diskStore.Save(flow.Profiles.Profile);
            var reloaded = diskStore.Load();
            Check(reloaded.completedActivities.Contains("Scene07OhmsExperiment"), "Disk JSON roundtrip retains completion");
            Check(reloaded.experiments.Count > 0 && reloaded.experiments[0].observations.Count == 5, "Disk JSON retains experiment observations");

            // 9. Multi-Resolution Checks
            foreach (var dimensions in new[] { new Vector2Int(1920, 1200), new Vector2Int(1280, 800), new Vector2Int(2560, 1600), new Vector2Int(1440, 1080) })
            {
                var validation = Type.GetType("AdaptiveOhmsLaw.Editor.Scene07Validation, Assembly-CSharp-Editor");
                validation.GetMethod("SetResolution").Invoke(null, new object[] { dimensions.x, dimensions.y });
                float resizeDeadline=Time.realtimeSinceStartup+5;
                while((Screen.width!=dimensions.x||Screen.height!=dimensions.y)&&Time.realtimeSinceStartup<resizeDeadline)yield return null;
                Canvas.ForceUpdateCanvases();
                Check(Screen.width == dimensions.x && Screen.height == dimensions.y, "Game view resolution " + dimensions + " actual " + Screen.width + "x" + Screen.height);
                ScreenCapture.CaptureScreenshot("Logs/Scene07-layout-" + dimensions.x + "x" + dimensions.y + ".png");
            }

            Check(errors.Count == 0, "No runtime errors: " + string.Join("; ", errors));
            results.Add("COMPLETE: Scene 07 experiment, circuit simulation, meters, observation table, evidence question, persistence, and resolution checks passed.");
            File.WriteAllLines("Logs/Scene07-validation.txt", results);
            UnityEditor.EditorApplication.isPlaying = false;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= Log;
        }
    }
}
#endif



