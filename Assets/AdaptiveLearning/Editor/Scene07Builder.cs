using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class Scene07Builder
    {
        public const string Root = "Assets/AdaptiveLearning";
        public const string ScenePath = Root + "/Scenes/Scene_10_BecomeTheExperimenter.unity";
        public const string Scene06Path = Root + "/Scenes/Scene_06_AdaptivePrediction.unity";

        static readonly Color Ink = new Color(.025f, .055f, .085f, .96f);
        static readonly Color Cyan = new Color(.25f, .91f, .91f);
        static readonly Color Copper = new Color(.85f, .45f, .2f);
        static readonly Color TableColor = new Color(.08f, .12f, .16f);

        static Font font;

        static Scene07Builder()
        {
            EditorApplication.delayCall += CheckAndBuild;
        }

        [InitializeOnLoadMethod]
        static void AutoInit()
        {
            CheckAndBuild();
        }

        static void CheckAndBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath))
            {
                Build();
            }
            // Existing scenes are edited only by an explicit build/repair command.
        }

        [MenuItem("Adaptive Learning/Open Scene 10 Experiment")]
        public static void Open()
        {
            if (!File.Exists(ScenePath)) Build();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        public static void EnsureScene06Transition()
        {
            if (!File.Exists(Scene06Path) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var currentScene = SceneManager.GetActiveScene();
            bool isCurrent = currentScene.path == Scene06Path;
            Scene scene = isCurrent ? currentScene : EditorSceneManager.OpenScene(Scene06Path, OpenSceneMode.Additive);

            var flow = UnityEngine.Object.FindFirstObjectByType<SceneFlowManager>();
            if (flow != null && flow.GetComponent<Scene06To07Transition>() == null)
            {
                var trans = flow.gameObject.AddComponent<Scene06To07Transition>();
                // Wire fade canvas group if found
                var fade = flow.transform.root.GetComponentsInChildren<CanvasGroup>(true).FirstOrDefault(cg => cg.name == "CinematicFade");
                var field = typeof(Scene06To07Transition).GetField("fadeCanvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null && fade != null) field.SetValue(trans, fade);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!isCurrent && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }

        static T Asset<T>(string path, Action<T> configure) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static DialogueLine Line(string id, string text, float duration) => new DialogueLine { id = id, text = text, duration = duration };

        public static void Build()
        {
            foreach (var folder in new[] { "Scenes", "Materials", "Prefabs/Wattson", "Prefabs/UI", "ScriptableObjects/Questions", "ScriptableObjects/Dialogue", "ScriptableObjects/Concepts", "Audio", "Art" })
                Directory.CreateDirectory(Root + "/" + folder);

            AssetDatabase.Refresh();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var rules = AssetDatabase.LoadAssetAtPath<AdaptiveRules>(Root + "/ScriptableObjects/Concepts/PrototypeRules.asset");
            if (rules == null)
            {
                rules = Asset<AdaptiveRules>(Root + "/ScriptableObjects/Concepts/PrototypeRules.asset", a => { });
            }

            var question = Asset<QuestionDefinition>(Root + "/ScriptableObjects/Questions/Scene07EvidenceQuestion.asset", a => {
                a.questionId = "S07_EvidenceConclusion";
                a.concepts = new[] { ConceptId.VoltageCurrentRelationship, ConceptId.Voltage, ConceptId.Current };
                a.questionText = "With the resistance kept the same, what happened to the current as the voltage increased?";
                a.answers = new[] { "The current increased.", "The current decreased.", "The current stayed the same." };
                a.correctAnswer = 0;
                a.hints = new[] {
                    "Look at your observation table. Compare the current at 1 V with the current at 3 V.",
                    "At 1 V, current was 0.10 A. At 3 V, current was 0.30 A. 0.30 A is larger than 0.10 A.",
                    "With resistance held constant, higher voltage provides more electrical push, increasing current."
                };
                a.difficulty = 1;
                a.masteryImpact = 1;
            });

            var script = Asset<DialogueSequence>(Root + "/ScriptableObjects/Dialogue/Scene07Dialogue.asset", a => {
                a.lines = new[] {
                    Line("intro_test", "Let's put your prediction to the test.", 2.8f),
                    Line("recall_increase", "You predicted that the current would increase. Let's collect some evidence.", 3.6f),
                    Line("recall_decrease", "You predicted that the current would decrease. Let's investigate what the meter tells us.", 3.6f),
                    Line("recall_same", "You predicted that the current would stay the same. Let's compare a few measurements.", 3.6f),
                    Line("intro_neutral", "Let's investigate how potential difference affects current in this circuit.", 3.6f),
                    Line("intro_same_resistor", "We'll keep the same resistor and change only the voltage.", 2.8f),
                    Line("guide_supported", "Close the circuit switch first to complete the path, then select 1 V and record your reading.", 4.5f),
                    Line("guide_guided", "Close the switch to start the current flow, then record readings for 1 V, 2 V, and 3 V.", 4.0f),
                    Line("reading_recorded", "Reading recorded in the table. Try another voltage setting next.", 3.0f),
                    Line("readings_complete", "All three readings recorded! Notice the pattern in your observation table.", 3.5f),
                    Line("evidence_prompt", "With the resistance kept the same, what happened to the current as the voltage increased?", 4.0f),
                    Line("evidence_accepted", "Our readings show that increasing the voltage increased the current through this resistor.", 3.8f),
                    Line("prediction_matched", "Your prediction matches the evidence we collected!", 3.2f),
                    Line("prediction_revised", "The measurements gave us new evidence. Revising a prediction is part of doing science.", 4.0f),
                    Line("evidence_clue", "Take another look at the observation table. Compare the current at 1.0 V with 3.0 V.", 3.5f),
                    Line("evidence_supported_clue", "At 1.0 V current is 0.10 A. At 3.0 V current is 0.30 A. 0.30 A is larger than 0.10 A!", 4.0f),
                    Line("evidence_retry", "Compare the three readings and choose the answer that fits our data.", 3.5f),
                    Line("evidence_guided_complete", "Let's look at the evidence together: the table confirms that current increased as voltage increased.", 4.5f),
                    Line("scene_complete", "Great scientific investigation! You have verified how current depends on potential difference.", 3.8f),
                    Line("final_handoff", "Scene 7 complete. We are now ready to explore resistance in our next experiment.", 4.0f)
                };
            });

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            // Materials
            var metalMat = Material("Conductor", new Color(.045f, .12f, .17f), false);
            var tealMat = Material("Charge", Cyan, true);
            var tableMat = Material("Workbench", TableColor, false);
            var sourceAmberMat = Material("SourceAmber", new Color(1, .61f, .2f), true);
            var wireMat = Material("CircuitWire", new Color(.15f, .75f, .85f), true);
            var meterMat = Material("MeterHousing", new Color(.12f, .18f, .24f), false);
            var dialMat = Material("MeterDialFace", new Color(.92f, .95f, .97f), true);
            var needleMat = Material("MeterNeedle", new Color(.85f, .15f, .15f), true);
            var switchMat = Material("SwitchMetal", Copper, false);
            var resistorMat = Material("ResistorBody", new Color(.75f, .68f, .55f), false);
            var whiteMat = Material("WattsonShell", new Color(.8f, .88f, .91f), false);
            var darkMat = Material("WattsonFace", new Color(.014f, .038f, .06f), false);

            // Environment
            var environment = Go("Environment");
            var lighting = Go("Lighting", environment.transform);
            var keyLight = Go("KeyLight", lighting.transform).AddComponent<Light>();
            keyLight.type = LightType.Directional; keyLight.intensity = 1.3f;
            keyLight.transform.rotation = Quaternion.Euler(40, -30, 0); keyLight.shadows = LightShadows.None;

            var fillLight = Go("FillLight", lighting.transform).AddComponent<Light>();
            fillLight.type = LightType.Directional; fillLight.intensity = 0.5f;
            fillLight.transform.rotation = Quaternion.Euler(20, 150, 0); fillLight.shadows = LightShadows.None;

            RenderSettings.ambientLight = new Color(.35f, .45f, .55f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.012f, .035f, .055f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .025f;

            // Camera
            var camera = Go("LearnerCamera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 2.3f, -4.8f);
            camera.transform.rotation = Quaternion.Euler(22, 0, 0);
            camera.fieldOfView = 48;
            camera.nearClipPlane = .1f; camera.farClipPlane = 60;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            camera.gameObject.AddComponent<AudioListener>();

            // Experiment Station (Tabletop)
            var station = Go("ExperimentStation", environment.transform);
            Shape("Tabletop", PrimitiveType.Cube, station.transform, new Vector3(0, -0.2f, 1.2f), new Vector3(9f, 0.4f, 4.8f), tableMat);
            Shape("Backdrop", PrimitiveType.Cube, station.transform, new Vector3(0, 2.5f, 5.5f), new Vector3(14f, 6f, 0.5f), metalMat);

            // Circuit Hierarchy
            var circuitRoot = Go("Circuit", station.transform);
            var circuitModel = circuitRoot.AddComponent<SimpleCircuitModel>();
            circuitModel.Configure(5f, 1f, false);

            // 1. Voltage Source
            var sourceGo = Go("VoltageSource", circuitRoot.transform);
            sourceGo.transform.localPosition = new Vector3(-2.6f, 0.35f, 1.8f);
            Shape("SourceBody", PrimitiveType.Cube, sourceGo.transform, Vector3.zero, new Vector3(1.6f, 0.8f, 1.2f), whiteMat);
            Shape("SourcePanel", PrimitiveType.Cube, sourceGo.transform, new Vector3(0, 0.05f, -0.61f), new Vector3(1.4f, 0.65f, 0.05f), darkMat);
            Shape("TerminalPos", PrimitiveType.Cylinder, sourceGo.transform, new Vector3(0.5f, 0.42f, 0.2f), new Vector3(0.18f, 0.15f, 0.18f), sourceAmberMat);
            Shape("TerminalNeg", PrimitiveType.Cylinder, sourceGo.transform, new Vector3(-0.5f, 0.42f, 0.2f), new Vector3(0.18f, 0.15f, 0.18f), darkMat);

            // 2. Knife Switch
            var switchGo = Go("Switch", circuitRoot.transform);
            switchGo.transform.localPosition = new Vector3(-1.0f, 0.1f, 2.6f);
            Shape("SwitchBase", PrimitiveType.Cube, switchGo.transform, Vector3.zero, new Vector3(1.2f, 0.12f, 0.7f), darkMat);
            Shape("ContactA", PrimitiveType.Cube, switchGo.transform, new Vector3(-0.4f, 0.12f, 0), new Vector3(0.18f, 0.15f, 0.22f), switchMat);
            Shape("ContactB", PrimitiveType.Cube, switchGo.transform, new Vector3(0.4f, 0.12f, 0), new Vector3(0.18f, 0.15f, 0.22f), switchMat);

            var leverHinge = Go("LeverHinge", switchGo.transform);
            leverHinge.transform.localPosition = new Vector3(-0.4f, 0.18f, 0);
            var leverArm = Shape("LeverArm", PrimitiveType.Cube, leverHinge.transform, new Vector3(0.4f, 0.05f, 0), new Vector3(0.85f, 0.08f, 0.14f), switchMat);
            Shape("Handle", PrimitiveType.Cylinder, leverHinge.transform, new Vector3(0.82f, 0.18f, 0), new Vector3(0.14f, 0.22f, 0.14f), sourceAmberMat);
            var switchCollider = switchGo.AddComponent<BoxCollider>();
            switchCollider.size = new Vector3(1.6f, 1.2f, 1.2f);
            switchCollider.center = new Vector3(0, 0.4f, 0);
            var switchInteraction = switchGo.AddComponent<CircuitSwitchInteraction>();

            // 3. Ammeter (in series)
            var ammeterGo = Go("Ammeter", circuitRoot.transform);
            ammeterGo.transform.localPosition = new Vector3(1.0f, 0.4f, 2.5f);
            Shape("MeterCase", PrimitiveType.Cube, ammeterGo.transform, Vector3.zero, new Vector3(1.1f, 0.9f, 0.8f), meterMat);
            Shape("MeterFace", PrimitiveType.Cube, ammeterGo.transform, new Vector3(0, 0.05f, -0.41f), new Vector3(0.95f, 0.75f, 0.05f), dialMat);
            var aNeedleHinge = Go("NeedleHinge", ammeterGo.transform);
            aNeedleHinge.transform.localPosition = new Vector3(0, -0.15f, -0.44f);
            Shape("Needle", PrimitiveType.Cube, aNeedleHinge.transform, new Vector3(0, 0.22f, 0), new Vector3(0.04f, 0.45f, 0.02f), needleMat);
            var ammeterDisplay = ammeterGo.AddComponent<MeterDisplay>();

            // 4. Resistor
            var resistorGo = Go("Resistor", circuitRoot.transform);
            resistorGo.transform.localPosition = new Vector3(2.6f, 0.25f, 1.2f);
            Shape("ResistorStand", PrimitiveType.Cube, resistorGo.transform, Vector3.zero, new Vector3(0.7f, 0.15f, 1.4f), darkMat);
            var rBody = Shape("ResistorBody", PrimitiveType.Cylinder, resistorGo.transform, new Vector3(0, 0.25f, 0), new Vector3(0.45f, 0.5f, 0.45f), resistorMat);
            rBody.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Shape("Band1", PrimitiveType.Cylinder, resistorGo.transform, new Vector3(0, 0.25f, -0.25f), new Vector3(0.47f, 0.06f, 0.47f), darkMat).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Shape("Band2", PrimitiveType.Cylinder, resistorGo.transform, new Vector3(0, 0.25f, -0.05f), new Vector3(0.47f, 0.06f, 0.47f), darkMat).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Shape("Band3", PrimitiveType.Cylinder, resistorGo.transform, new Vector3(0, 0.25f, 0.15f), new Vector3(0.47f, 0.06f, 0.47f), darkMat).transform.localRotation = Quaternion.Euler(90, 0, 0);

            // 5. Voltmeter (in parallel across resistor)
            var voltmeterGo = Go("Voltmeter", circuitRoot.transform);
            voltmeterGo.transform.localPosition = new Vector3(0.6f, 0.35f, 0.2f);
            Shape("MeterCase", PrimitiveType.Cube, voltmeterGo.transform, Vector3.zero, new Vector3(1.1f, 0.9f, 0.8f), meterMat);
            Shape("MeterFace", PrimitiveType.Cube, voltmeterGo.transform, new Vector3(0, 0.05f, -0.41f), new Vector3(0.95f, 0.75f, 0.05f), dialMat);
            var vNeedleHinge = Go("NeedleHinge", voltmeterGo.transform);
            vNeedleHinge.transform.localPosition = new Vector3(0, -0.15f, -0.44f);
            Shape("Needle", PrimitiveType.Cube, vNeedleHinge.transform, new Vector3(0, 0.22f, 0), new Vector3(0.04f, 0.45f, 0.02f), needleMat);
            var voltmeterDisplay = voltmeterGo.AddComponent<MeterDisplay>();

            // 6. Visible Physical Wires
            var wiresRoot = Go("Wires", circuitRoot.transform);
            // Wire 1: Source (+) -> Switch Contact A
            Shape("Wire_Src_Switch", PrimitiveType.Cube, wiresRoot.transform, new Vector3(-2.0f, 0.1f, 2.2f), new Vector3(1.3f, 0.06f, 0.06f), wireMat);
            // Wire 2: Switch Contact B -> Ammeter
            Shape("Wire_Switch_Ammeter", PrimitiveType.Cube, wiresRoot.transform, new Vector3(0.1f, 0.1f, 2.6f), new Vector3(1.8f, 0.06f, 0.06f), wireMat);
            // Wire 3: Ammeter -> Resistor Terminal 1
            Shape("Wire_Ammeter_Resistor", PrimitiveType.Cube, wiresRoot.transform, new Vector3(2.0f, 0.1f, 1.8f), new Vector3(1.4f, 0.06f, 0.06f), wireMat);
            // Wire 4: Resistor Terminal 2 -> Source (-) [Closed Main Loop return]
            Shape("Wire_Resistor_Return", PrimitiveType.Cube, wiresRoot.transform, new Vector3(0.0f, 0.05f, 0.7f), new Vector3(4.8f, 0.06f, 0.06f), wireMat);
            Shape("Wire_Return_To_Src", PrimitiveType.Cube, wiresRoot.transform, new Vector3(-2.4f, 0.1f, 1.2f), new Vector3(0.06f, 0.06f, 1.2f), wireMat);

            // Voltmeter Parallel Branches across Resistor
            Shape("Wire_V_Branch1", PrimitiveType.Cube, wiresRoot.transform, new Vector3(1.6f, 0.12f, 0.6f), new Vector3(1.8f, 0.05f, 0.05f), sourceAmberMat);
            Shape("Wire_V_Branch2", PrimitiveType.Cube, wiresRoot.transform, new Vector3(1.6f, 0.12f, 1.6f), new Vector3(1.8f, 0.05f, 0.05f), darkMat);

            // 7. Small Supporting Charge Drift Viewport (near resistor)
            var chargeRoot = Go("ChargeParticlesStation", circuitRoot.transform);
            chargeRoot.transform.localPosition = new Vector3(-2.6f, 0.1f, 0.2f);
            Shape("DriftTube", PrimitiveType.Cylinder, chargeRoot.transform, Vector3.zero, new Vector3(0.5f, 0.9f, 0.5f), metalMat).transform.localRotation = Quaternion.Euler(0, 0, 90);
            var random = new System.Random(7007);
            var particles = new Transform[32];
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i] = Shape("Charge_" + i, PrimitiveType.Sphere, chargeRoot.transform,
                    new Vector3((float)random.NextDouble() * 1.6f - 0.8f, (float)random.NextDouble() * 0.35f - 0.17f, (float)random.NextDouble() * 0.35f - 0.17f),
                    Vector3.one * 0.055f, tealMat).transform;
            }
            var drift = chargeRoot.AddComponent<ChargeDriftVisualization>();
            drift.Configure(particles);

            // Characters (Wattson)
            var characters = Go("Characters");
            var companion = Go("Wattson", characters.transform);
            companion.transform.position = new Vector3(-3.2f, 1.2f, -0.6f);
            companion.transform.rotation = Quaternion.Euler(0, 45, 0);

            var visual = Go("Visual", companion.transform);
            Shape("Body", PrimitiveType.Capsule, visual.transform, Vector3.zero, new Vector3(.85f, .65f, .65f), whiteMat);
            Shape("Head", PrimitiveType.Sphere, visual.transform, new Vector3(0, .85f, 0), new Vector3(1.05f, .85f, .85f), whiteMat);
            Shape("Face", PrimitiveType.Sphere, visual.transform, new Vector3(0, .87f, .32f), new Vector3(.82f, .49f, .27f), darkMat);
            Shape("EyeLeft", PrimitiveType.Sphere, visual.transform, new Vector3(-.2f, .92f, .455f), new Vector3(.105f, .16f, .035f), tealMat);
            Shape("EyeRight", PrimitiveType.Sphere, visual.transform, new Vector3(.2f, .92f, .455f), new Vector3(.105f, .16f, .035f), tealMat);
            Shape("Core", PrimitiveType.Sphere, visual.transform, new Vector3(0, .1f, .32f), new Vector3(.26f, .26f, .07f), tealMat);
            Shape("ArmLeft", PrimitiveType.Capsule, visual.transform, new Vector3(-.61f, .02f, 0), new Vector3(.17f, .32f, .17f), whiteMat);
            Shape("ArmRight", PrimitiveType.Capsule, visual.transform, new Vector3(.61f, .02f, 0), new Vector3(.17f, .32f, .17f), whiteMat);

            var animation = Go("Animator", companion.transform).AddComponent<Animator>();
            var target = Go("LookTarget", companion.transform);
            var anchor = Go("DialogueAnchor", companion.transform);
            anchor.transform.localPosition = Vector3.up * 1.6f;
            var trigger = Go("InteractionTrigger", companion.transform).AddComponent<SphereCollider>();
            trigger.isTrigger = true; trigger.radius = 1.5f;
            var voice = Go("AudioSource", companion.transform).AddComponent<AudioSource>();
            voice.playOnAwake = false;
            var wattson = companion.AddComponent<WattsonController>();
            wattson.Configure(visual.transform, target.transform, anchor.transform, animation, voice);

            // Audio
            var audio = Go("Audio");
            var sfx = Go("SFX", audio.transform).AddComponent<AudioSource>(); sfx.playOnAwake = false;
            var ambient = Go("Ambient", audio.transform).AddComponent<AudioSource>();
            ambient.playOnAwake = false; ambient.loop = true; ambient.volume = .15f;

            // Systems
            var systems = Go("LearningSystems");
            var profiles = Go("StudentProfileManager", systems.transform).AddComponent<StudentProfileManager>();
            var learning = Go("AdaptiveLearningManager", systems.transform).AddComponent<AdaptiveLearningManager>();
            learning.Configure(rules, profiles);
            var questions = Go("QuestionManager", systems.transform).AddComponent<QuestionManager>();
            var chat = Go("WattsonChatManager", systems.transform).AddComponent<WattsonChatManager>();
            var flow = Go("Scene07FlowManager", systems.transform).AddComponent<Scene07FlowManager>();
            var experimentController = systems.AddComponent<CircuitExperimentController>();

            switchInteraction.Configure(leverHinge.transform, circuitModel, sfx, null);

            // UI
            var canvas = Go("UI", null, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            var safe = Rect("SafeArea", canvas.transform, 0, 0, 1, 1);
            safe.gameObject.AddComponent<SafeAreaLayout>();

            Label("Lesson", safe, "ADAPTIVE OHM'S LAW   /   07", 26, Cyan, .04f, .91f, .66f, .97f);
            Label("Objective", safe, "Investigate Current vs. Voltage across a fixed 10 \u03a9 resistor.", 20, Color.white, .04f, .86f, .65f, .92f);
            var ask = Button("AskWattson", safe, "ASK WATTSON", .77f, .875f, .96f, .96f);

            // Supporting note on motion
            Label("ModelNote", safe, "Microscopic charges: Net drift \u2192  |  Conventional current \u2190\nCircuit: Fixed 10 \u03a9 resistor, ideal source, negligible wire resistance.", 16, new Color(.6f, .76f, .82f), .04f, .76f, .55f, .84f);

            // Top Status Bar
            var supportStatus = Label("Support", safe, "WATTSON INTERFACE / INDEPENDENT", 17, Cyan, .04f, .013f, .49f, .055f);
            var progressStatus = Label("LearningProgress", safe, "", 16, new Color(.62f, .75f, .81f), .49f, .013f, .96f, .055f);
            progressStatus.alignment = TextAnchor.MiddleRight;

            // Observation Table (Right Side Panel)
            var tablePanel = Panel("ObservationTablePanel", safe, .68f, .22f, .97f, .85f);
            Label("TableTitle", tablePanel, "OBSERVATION TABLE", 22, Cyan, .05f, .90f, .95f, .98f);
            Label("TableSubtitle", tablePanel, "FIXED RESISTANCE: 10 \u03a9", 16, new Color(.7f, .85f, .9f), .05f, .83f, .95f, .89f);

            // Header
            var headerRect = Rect("HeaderRow", tablePanel, .05f, .72f, .95f, .81f);
            var hV = Label("HeaderV", headerRect, "VOLTAGE (V)", 17, sourceAmberMat.color, 0, 0, .5f, 1); hV.alignment = TextAnchor.MiddleCenter;
            var hI = Label("HeaderI", headerRect, "CURRENT (A)", 17, Cyan, .5f, 0, 1, 1); hI.alignment = TextAnchor.MiddleCenter;

            // 3 Rows
            var rowVTexts = new Text[5];
            var rowITexts = new Text[5];
            for (int r = 0; r < 5; r++)
            {
                float y1 = .70f - r * .08f;
                float y0 = y1 - .075f;
                var rowRect = Rect("Row_" + (r + 1), tablePanel, .05f, y0, .95f, y1);
                var rowBg = rowRect.gameObject.AddComponent<Image>();
                rowBg.color = (r % 2 == 0) ? new Color(.04f, .10f, .14f, .8f) : new Color(.06f, .14f, .18f, .8f);

                rowVTexts[r] = Label("V_Val", rowRect, "\u2014", 22, Color.white, .05f, 0, .45f, 1);
                rowVTexts[r].alignment = TextAnchor.MiddleCenter;
                rowITexts[r] = Label("I_Val", rowRect, "\u2014", 22, Color.white, .55f, 0, .95f, 1);
                rowITexts[r].alignment = TextAnchor.MiddleCenter;
            }

            var progressLabel = Label("TableProgress", tablePanel, "READINGS RECORDED:  0 / 5", 16, Cyan, .05f, .24f, .95f, .30f);
            progressLabel.alignment = TextAnchor.MiddleCenter;
            var recordBtn = Button("RecordReading", tablePanel, "RECORD READING", .08f, .11f, .92f, .22f);
            var feedbackLabel = Label("TableFeedback", tablePanel, "Close the switch to begin.", 15, new Color(.85f, .9f, .95f), .05f, .01f, .95f, .10f);
            feedbackLabel.alignment = TextAnchor.MiddleCenter;

            var obsTableUI = tablePanel.gameObject.AddComponent<ObservationTableUI>();
            obsTableUI.Configure(rowVTexts, rowITexts, recordBtn, feedbackLabel, progressLabel);

            // Circuit Live Readouts Box (Center-Bottom)
            var meterUIPanel = Panel("MeterUIPanel", safe, .04f, .38f, .38f, .55f);
            Label("MeterUITitle", meterUIPanel, "LIVE CIRCUIT MEASUREMENTS", 18, Cyan, .05f, .78f, .95f, .95f);
            var vReadout = Label("VoltmeterReadout", meterUIPanel, "0.00 V", 24, sourceAmberMat.color, .05f, .42f, .95f, .75f);
            Label("VLabel", meterUIPanel, "VOLTMETER (across 10 \u03a9 resistor)", 15, Color.white, .05f, .15f, .95f, .42f);
            var iReadout = Label("AmmeterReadout", meterUIPanel, "0.00 A", 24, Cyan, .52f, .42f, .95f, .75f);
            Label("ILabel", meterUIPanel, "AMMETER (in series)", 15, Color.white, .52f, .15f, .95f, .42f);

            ammeterDisplay.Configure(iReadout, null, aNeedleHinge.transform, "A", 0.5f);
            voltmeterDisplay.Configure(vReadout, null, vNeedleHinge.transform, "V", 5.0f);

            // Controls Bar (Center Bottom)
            var controlBar = Panel("CircuitControlBar", safe, .04f, .22f, .65f, .36f);
            var switchBtn = Button("ToggleSwitch", controlBar, "SWITCH: OPEN [CLOSE]", .02f, .18f, .36f, .82f);
            var switchBtnText = switchBtn.GetComponentInChildren<Text>();

            var vGroupRect = Rect("VoltageGroup", controlBar, .39f, .18f, .98f, .82f);
            var vLayout = vGroupRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            vLayout.spacing = 10; vLayout.childControlWidth = vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = vLayout.childForceExpandHeight = true;

            var vBtn1 = Button("Voltage1V", vGroupRect, "1 V", 0, 0, 1, 1);
            var vBtn2 = Button("Voltage2V", vGroupRect, "2 V", 0, 0, 1, 1);
            var vBtn3 = Button("Voltage3V", vGroupRect, "3 V", 0, 0, 1, 1);
            var vButtons = new[] { vBtn1, vBtn2, vBtn3 };

            experimentController.Configure(circuitModel, ammeterDisplay, voltmeterDisplay, switchInteraction,
                obsTableUI, drift, switchBtn, switchBtnText, vButtons, recordBtn, sfx, null);

            // Hint Text
            var hint = Label("HintUI", safe, "", 20, Cyan, .04f, .17f, .65f, .22f);

            // Dialogue Panel
            var dialoguePanel = Panel("DialoguePanel", safe, .04f, .06f, .96f, .17f);
            var speaker = Label("Speaker", dialoguePanel, "WATTSON", 18, Cyan, .02f, .70f, .98f, .95f);
            var subtitle = Label("Subtitle", dialoguePanel, "", 24, Color.white, .02f, .08f, .98f, .70f);
            var presenter = dialoguePanel.gameObject.AddComponent<DialoguePresenter>();
            presenter.Configure(speaker, subtitle, voice);

            // Evidence Question Panel (Reuses QuestionPanel style)
            var questionPanel = Panel("QuestionPanel", safe, .04f, .06f, .96f, .21f);
            var prompt = Label("QuestionPrompt", questionPanel, question.questionText, 22, Color.white, 0, 0, 1, 1);
            prompt.gameObject.SetActive(false);

            var options = Rect("Answers", questionPanel, .02f, .13f, .98f, .88f);
            var qLayout = options.gameObject.AddComponent<HorizontalLayoutGroup>();
            qLayout.spacing = 18; qLayout.childControlWidth = qLayout.childControlHeight = true;
            qLayout.childForceExpandWidth = qLayout.childForceExpandHeight = true;

            var buttons = new Button[3];
            for (int i = 0; i < 3; i++)
                buttons[i] = Button("Answer" + (char)('A' + i), options, ((char)('A' + i)) + "   " + question.answers[i], 0, 0, 1, 1);

            var decisions = Rect("EvidenceDecision", questionPanel, .02f, .13f, .98f, .88f);
            var dLayout = decisions.gameObject.AddComponent<HorizontalLayoutGroup>();
            dLayout.spacing = 18; dLayout.childControlWidth = dLayout.childControlHeight = true;
            dLayout.childForceExpandWidth = dLayout.childForceExpandHeight = true;
            var retry = Button("TryAgain", decisions, "TRY AGAIN", 0, 0, 1, 1);
            var keep = Button("KeepAnswer", decisions, "GUIDED EXPLANATION", 0, 0, 1, 1);
            decisions.gameObject.SetActive(false);

            var qView = questionPanel.gameObject.AddComponent<QuestionUI>();
            qView.Configure(prompt, buttons, options.gameObject, decisions.gameObject, retry, keep);
            questionPanel.gameObject.SetActive(false);

            // Completion Panel
            var end = Panel("Completion", safe, .04f, .06f, .96f, .21f);
            Label("CompletionText", end, "Investigation complete! Your measurements verify the relationship between voltage and current.", 24, Color.white, .025f, .12f, .65f, .88f);
            var next = Button("Continue", end, "CONTINUE", .68f, .15f, .975f, .85f);
            end.gameObject.SetActive(false);

            // Wattson Chat Modal
            var chatBlocker = Panel("WattsonChatPanel", safe, 0, 0, 1, 1);
            chatBlocker.GetComponent<Image>().color = new Color(0, 0, 0, .6f);
            var chatCard = Panel("ChatCard", chatBlocker, .12f, .13f, .88f, .86f);
            Label("ChatTitle", chatCard, "ASK WATTSON", 30, Cyan, .04f, .865f, .7f, .95f);
            var close = Button("CloseChat", chatCard, "CLOSE", .77f, .86f, .96f, .96f);
            var chatContext = Label("Context", chatCard, "SCENE 07 / OHM'S EXPERIMENT \u2022 LOCAL LESSON GUIDE", 18, new Color(.6f, .76f, .82f), .04f, .77f, .96f, .85f);
            var reply = Label("Reply", chatCard, "What would you like to ask about the experiment? Asking for help never lowers your mastery.", 26, Color.white, .04f, .38f, .96f, .76f);

            var quick = Rect("QuickQuestions", chatCard, .04f, .25f, .96f, .36f);
            var quickLayout = quick.gameObject.AddComponent<HorizontalLayoutGroup>();
            quickLayout.spacing = 14; quickLayout.childControlWidth = quickLayout.childControlHeight = true;
            quickLayout.childForceExpandWidth = quickLayout.childForceExpandHeight = true;
            var suggestions = new[] {
                Button("AmmeterHelp", quick, "What does the ammeter measure?", 0, 0, 1, 1),
                Button("VoltmeterHelp", quick, "What does the voltmeter measure?", 0, 0, 1, 1),
                Button("CurrentZero", quick, "Why is the current zero?", 0, 0, 1, 1),
                Button("WhatNext", quick, "What should I do next?", 0, 0, 1, 1)
            };
            foreach (var b in suggestions) b.GetComponentInChildren<Text>().fontSize = 19;

            var entry = Panel("MessageInput", chatCard, .04f, .08f, .74f, .21f);
            entry.GetComponent<Image>().color = new Color(.08f, .15f, .2f);
            var input = entry.gameObject.AddComponent<InputField>();
            input.targetGraphic = entry.GetComponent<Image>();
            input.characterLimit = 240;
            var inputText = Label("InputText", entry, "", 24, Color.white, .025f, .07f, .97f, .93f);
            inputText.supportRichText = false;
            var placeholder = Label("Placeholder", entry, "Ask about meters, circuit, or observations…", 22, new Color(.6f, .72f, .76f), .025f, .07f, .97f, .93f);
            input.textComponent = inputText; input.placeholder = placeholder;
            var send = Button("Send", chatCard, "ASK", .77f, .08f, .96f, .21f);

            var chatUI = canvas.AddComponent<WattsonChatUI>();
            chatUI.Configure(chatBlocker.gameObject, ask, close, send, suggestions, input, reply, chatContext);
            chatBlocker.gameObject.SetActive(false);

            // Fade Curtain
            var curtain = Panel("CinematicFade", canvas.transform, 0, 0, 1, 1);
            curtain.GetComponent<Image>().color = Color.black;
            var fade = curtain.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0; fade.blocksRaycasts = false;

            // Event System
            Go("EventSystem", null, typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Flow configuration
            flow.Configure(rules, question, script, profiles, learning, questions, wattson, chat, chatUI, presenter,
                qView, experimentController, camera, fade, supportStatus, hint, progressStatus, end.gameObject, next);

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Build settings
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(s => s.path == ScenePath))
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();

            if (previous.IsValid())
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }

            File.WriteAllText("Logs/Scene07-build.txt", "Scene 07 created successfully at " + DateTime.Now);
        }

        static GameObject Go(string name, Transform parent = null, params Type[] types)
        {
            var go = new GameObject(name, types);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static Material Material(string name, Color color, bool unlit)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var rt = (RectTransform)Go(name, parent, typeof(RectTransform)).transform;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static RectTransform Panel(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var rt = Rect(name, parent, x0, y0, x1, y1);
            rt.gameObject.AddComponent<Image>().color = Ink;
            return rt;
        }

        static Text Label(string name, Transform parent, string text, int size, Color color, float x0, float y0, float x1, float y1)
        {
            var rt = Rect(name, parent, x0, y0, x1, y1);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }

        static Button Button(string name, Transform parent, string text, float x0, float y0, float x1, float y1)
        {
            var rt = Panel(name, parent, x0, y0, x1, y1);
            var graphic = rt.GetComponent<Image>();
            graphic.color = new Color(.08f, .24f, .29f);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            var colors = button.colors;
            colors.highlightedColor = new Color(.65f, 1, 1);
            colors.pressedColor = new Color(.35f, .8f, .85f);
            colors.selectedColor = new Color(.65f, 1, 1);
            button.colors = colors;
            var label = Label("Label", rt, text, 24, Color.white, .035f, .05f, .965f, .95f);
            label.alignment = TextAnchor.MiddleCenter;
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1;
            element.minHeight = 56;
            return button;
        }
    }
}

