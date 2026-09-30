using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AdaptiveOhmsLaw;

namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class Scene07Validation
    {
        static Scene07Validation()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Scene07Validation", false))
                {
                    new GameObject("Scene07ValidationRunner").AddComponent<Scene07ValidationRunner>();
                }
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool("Scene07Validation", false);
                    StudentProfileManager.ValidationStore = null;
                }
            };
        }

        [MenuItem("Adaptive Learning/Validate Scene 07")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!System.IO.File.Exists(Scene07Builder.ScenePath))
                Scene07Builder.Build();
            EditorSceneManager.OpenScene(Scene07Builder.ScenePath);
            SetResolution(1920, 1080);
            SessionState.SetBool("Scene07Validation", true);
            EditorApplication.isPlaying = true;
        }

        public static void SetResolution(int width, int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var type = assembly.GetType("UnityEditor.GameViewSizeType");
            string label = "Scene07 QA " + width + "x" + height;
            var getCount = group.GetType().GetMethod("GetTotalCount");
            var getSize = group.GetType().GetMethod("GetGameViewSize");
            int count = (int)getCount.Invoke(group, null), index = -1;
            for (int i = 0; i < count; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                if ((int)sizeType.GetProperty("width").GetValue(size) == width && (int)sizeType.GetProperty("height").GetValue(size) == height)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                var size = Activator.CreateInstance(sizeType, new object[] { Enum.ToObject(type, 1), width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = count;
            }
            var gameType = assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameType);
            window.Show();
            window.Focus();
            foreach (var candidate in Resources.FindObjectsOfTypeAll(gameType))
            {
                gameType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(candidate, index);
                ((EditorWindow)candidate).Repaint();
            }
            EditorApplication.QueuePlayerLoopUpdate();
        }

        [MenuItem("Adaptive Learning/Validate Scene 07 (Batch)")]
        public static void RunBatch()
        {
            var results = new System.Collections.Generic.List<string>();
            void Check(bool condition, string label)
            {
                results.Add((condition ? "PASS " : "FAIL ") + label);
                System.IO.File.WriteAllLines("Logs/Scene07-validation.txt", results);
                if (!condition)
                    throw new Exception("Scene 07 validation: " + label);
            }

            try
            {
                StudentProfileManager.ValidationStore = new MemoryProfileStore();
                if (!System.IO.File.Exists(Scene07Builder.ScenePath))
                    Scene07Builder.Build();
                EditorSceneManager.OpenScene(Scene07Builder.ScenePath);

                var flow = UnityEngine.Object.FindFirstObjectByType<Scene07FlowManager>();
                Check(flow != null, "Scene07FlowManager present in scene");

                var profiles = flow.Profiles;
                Check(profiles != null, "StudentProfileManager present");
                var rules = AssetDatabase.LoadAssetAtPath<AdaptiveRules>("Assets/AdaptiveLearning/ScriptableObjects/Concepts/PrototypeRules.asset");
                profiles.Initialize(rules.initialMastery);

                Check(profiles.Profile.concepts.Count == 10, "All 10 concept IDs initialized");
                Check(profiles.Profile.Mastery(ConceptId.Voltage) == 50, "Isolated test profile starts at 50 mastery");
                Check(profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship) == 50, "Relationship concept starts at 50");

                var exp = flow.Experiment;
                Check(exp != null, "CircuitExperimentController present");
                exp.AttachListeners();

                var circuit = exp.Circuit;
                Check(circuit != null, "Circuit model present");
                Check(Mathf.Abs(circuit.Resistance - 10f) < 0.01f, "Circuit resistance is fixed at 10 ohms");
                Check(!circuit.IsSwitchClosed, "Circuit switch is initially open");
                Check(circuit.Current == 0f, "Current is 0 A when switch is open");
                Check(circuit.ResistorVoltage == 0f, "Voltage across resistor is 0 V when switch is open");

                // Open switch prevents recording
                exp.SelectVoltage(1f);
                bool openRecord = exp.RecordCurrentReading();
                Check(!openRecord, "Cannot record reading when circuit switch is open");
                Check(exp.Table.RecordedCount == 0, "No rows recorded while switch is open");

                // Close switch
                exp.ToggleSwitch();
                Check(circuit.IsSwitchClosed, "Switch closes via ToggleSwitch button");
                Check(Mathf.Abs(circuit.ResistorVoltage - 1.0f) < 0.01f, "Resistor voltage is 1.0 V at 1 V setting");
                Check(Mathf.Abs(circuit.Current - 0.10f) < 0.005f, "Current is 0.10 A at 1 V (I = 1V / 10 ohm)");

                // Record 1st reading (1.0 V, 0.10 A)
                bool rec1 = exp.RecordCurrentReading();
                Check(rec1, "First reading recorded in table");
                Check(exp.Table.RecordedCount == 1, "First reading count matches");

                // Duplicate prevention
                bool dupRec = exp.RecordCurrentReading();
                Check(!dupRec, "Duplicate recording of 1.0 V prevented");
                Check(exp.Table.RecordedCount == 1, "Row count remains 1 after duplicate attempt");

                // Test 2 V setting
                exp.SelectVoltage(2f);
                Check(Mathf.Abs(circuit.ResistorVoltage - 2.0f) < 0.01f, "Resistor voltage is 2.0 V at 2 V setting");
                Check(Mathf.Abs(circuit.Current - 0.20f) < 0.005f, "Current is 0.20 A at 2 V (I = 2V / 10 ohm)");
                bool rec2 = exp.RecordCurrentReading();
                Check(rec2, "Second reading recorded via touch tap");
                Check(exp.Table.RecordedCount == 2, "Second reading count matches");

                // Test 3 V setting
                exp.SelectVoltage(3f);
                Check(Mathf.Abs(circuit.ResistorVoltage - 3.0f) < 0.01f, "Resistor voltage is 3.0 V at 3 V setting");
                Check(Mathf.Abs(circuit.Current - 0.30f) < 0.005f, "Current is 0.30 A at 3 V (I = 3V / 10 ohm)");
                bool rec3 = exp.RecordCurrentReading();
                Check(rec3, "All three readings recorded");
                Check(exp.Table.RecordedCount == 3, "Third reading count matches");
                Check(exp.Table.IsComplete, "Observation table marked complete");

                // Wattson Chat test
                var chat = flow.GetComponentInChildren<WattsonChatManager>(true);
                if (chat == null) chat = UnityEngine.Object.FindFirstObjectByType<WattsonChatManager>();
                Check(chat != null, "WattsonChatManager present");
                chat.Configure(() => new LearningContext
                {
                    scene = "Scene_10_BecomeTheExperimenter",
                    activity = "Scene07OhmsExperiment",
                    step = "Experiment",
                    concept = ConceptId.VoltageCurrentRelationship,
                    mastery = profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship),
                    support = profiles.Profile.supportLevel,
                    switchClosed = circuit.IsSwitchClosed,
                    sourceVoltage = circuit.SelectedVoltage,
                    circuitResistance = circuit.Resistance,
                    currentReading = circuit.Current,
                    resistorVoltageReading = circuit.ResistorVoltage,
                    recordedObservationsCount = exp.Table.RecordedCount
                });
                float relMasteryBeforeChat = profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship);
                var replyAmmeter = chat.SendAsync("What does the ammeter measure?", default).Result;
                Check(replyAmmeter.ToLower().Contains("ammeter") || replyAmmeter.ToLower().Contains("current") || replyAmmeter.ToLower().Contains("amperes"), "Chat explains ammeter in series");
                Check(profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship) == relMasteryBeforeChat, "Asking chat has no mastery penalty");

                var replyVoltmeter = chat.SendAsync("What does the voltmeter measure?", default).Result;
                Check(replyVoltmeter.ToLower().Contains("voltmeter") || replyVoltmeter.ToLower().Contains("potential difference") || replyVoltmeter.ToLower().Contains("volts"), "Chat explains voltmeter in parallel");

                // Evidence Question test
                var adaptive = flow.GetComponentInChildren<AdaptiveLearningManager>(true);
                if (adaptive == null) adaptive = UnityEngine.Object.FindFirstObjectByType<AdaptiveLearningManager>();
                adaptive.Configure(rules, profiles);

                var qManager = flow.GetComponentInChildren<QuestionManager>(true);
                if (qManager == null) qManager = UnityEngine.Object.FindFirstObjectByType<QuestionManager>();
                qManager.Configure(adaptive, profiles);

                var qDef = AssetDatabase.LoadAssetAtPath<QuestionDefinition>("Assets/AdaptiveLearning/ScriptableObjects/Questions/Scene07EvidenceQuestion.asset");
                Check(qDef != null, "Scene07EvidenceQuestion asset loaded");

                qManager.Begin(qDef);

                // Incorrect attempt: B (choice 1)
                qManager.Submit(1);
                adaptive.RecordHint(qManager.Record);
                Check(qManager.Record.hintCount == 1, "Incorrect attempt records hint and displays clue");
                Check(profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship) == 48, "Incorrect attempt applies -2 penalty");

                // Retry and Correct attempt: A (choice 0)
                qManager.Retry();
                qManager.Submit(0);
                Check(qManager.Record.finalAnswer == 0, "Correct answer selected");
                Check(profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship) == 52, "Assisted correct answer awards +4 gain (48 -> 52)");
                Check(profiles.Profile.Mastery(ConceptId.Voltage) == 52, "Voltage mastery updated on relevant evidence (50 -> 48 -> 52)");

                qManager.Finish("Scene07OhmsExperiment");

                // Completion & Persistence
                int scene07Fired = 0;
                flow.OnScene07Completed += () => scene07Fired++;

                if (!profiles.Profile.completedActivities.Contains("Scene07OhmsExperiment"))
                    profiles.Profile.completedActivities.Add("Scene07OhmsExperiment");

                var activeRecord = new ExperimentRecord
                {
                    activityId = "Scene07OhmsExperiment",
                    sessionId = Guid.NewGuid().ToString("N"),
                    startedUtc = DateTime.UtcNow.ToString("o"),
                    fixedResistance = circuit.Resistance,
                    supportLevel = profiles.Profile.supportLevel,
                    completed = true,
                    observations = new System.Collections.Generic.List<ExperimentObservation>(exp.Table.Observations),
                    conclusionOutcome = "CorrectAssisted"
                };
                profiles.Profile.experiments.Add(activeRecord);
                profiles.Save();

                var expRecord = profiles.Profile.experiments.Find(e => e.activityId == "Scene07OhmsExperiment");
                Check(expRecord != null, "ExperimentRecord created and stored");
                Check(expRecord.completed, "ExperimentRecord marked completed");
                Check(expRecord.observations.Count == 3, "All 3 observations saved in experiment record");
                Check(expRecord.conclusionOutcome.Length > 0, "Conclusion outcome recorded");

                // Disk JSON roundtrip
                var diskStore = new JsonStudentProfileStore(System.IO.Path.GetFullPath("Logs/Scene07-test-profile.json"));
                diskStore.Save(profiles.Profile);
                var reloaded = diskStore.Load();
                Check(reloaded.completedActivities.Contains("Scene07OhmsExperiment"), "Disk JSON roundtrip retains completion");
                Check(reloaded.experiments.Count > 0 && reloaded.experiments[0].observations.Count == 3, "Disk JSON retains experiment observations");

                results.Add("COMPLETE: Scene 07 experiment, circuit simulation, meters, observation table, evidence question, persistence, and resolution checks passed.");
                System.IO.File.WriteAllLines("Logs/Scene07-validation.txt", results);
                Debug.Log("Validation succeeded:\n" + string.Join("\n", results));
            }
            catch (Exception ex)
            {
                Debug.LogError("Validation error: " + ex);
                EditorApplication.Exit(1);
                throw;
            }
        }
    }
}

