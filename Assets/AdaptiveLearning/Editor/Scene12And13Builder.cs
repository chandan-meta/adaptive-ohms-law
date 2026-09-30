using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class Scene12And13Builder
    {
        const string Root = "Assets/AdaptiveLearning/";
        const string AudioFolder = Root + "Audio/GeneratedVoice/";

        static Scene12And13Builder()
        {
            EditorApplication.update += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (File.Exists("Logs/scene12-13-build.req"))
                {
                    try { File.Delete("Logs/scene12-13-build.req"); } catch { }
                    BuildAll();
                }
            };
        }

        static Material Mat(string name, Color color, bool emissive = false)
        {
            string path = Root + "Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", color);
                if (emissive)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", color * 2f);
                }
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            if (parent != null) t.SetParent(parent, false);
            return t;
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.name = name;
            o.transform.SetParent(parent, false);
            o.transform.localPosition = pos;
            o.transform.localScale = scale;
            o.GetComponent<Renderer>().sharedMaterial = m;
            return o;
        }

        [MenuItem("Adaptive Learning/Build Scenes 12 and 13")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before building scenes.");

            // 1. Build Dialogue assets
            BuildDialogueAssets();

            // 2. Build Scene 12
            BuildScene12();

            // 3. Build Scene 13
            BuildScene13();

            // 4. Connect Scene 11 transition
            ConnectScene11();

            // 5. Update Build Settings
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Build Scenes 12 & 13 COMPLETE: Created authored scenes, dialogue assets, transitions, and updated Build Settings.");
        }

        public static void BuildDialogueAssets()
        {
            AssetDatabase.Refresh();
            // Scene 12
            string p12 = Root + "ScriptableObjects/Dialogue/Scene12Dialogue.asset";
            var d12 = AssetDatabase.LoadAssetAtPath<DialogueSequence>(p12);
            if (d12 == null)
            {
                d12 = ScriptableObject.CreateInstance<DialogueSequence>();
                AssetDatabase.CreateAsset(d12, p12);
            }
            d12.lines = new[]
            {
                Line("intro_voltage", "We have seen what pushes charge.", 3.5f),
                Line("intro_current", "And we have measured how much charge flows.", 3.5f),
                Line("intro_why", "But why does not charge flow equally easily through every conductor?", 4.5f),
                Line("inspect_prompt", "Select wire XY to inspect its microscopic structure.", 4.0f),
                Line("wire_opposes", "The material opposes the flow of electric current.", 3.8f),
                Line("wire_resistance_name", "That opposition is called electrical resistance. Symbol R.", 4.5f),
                Line("table_intro", "And the best part? You have already collected the measurements we need.", 4.5f),
                Line("table_constant", "The value of V divided by I is the same for every reading. For this wire at constant temperature, that constant is its resistance.", 7.5f),
                Line("graph_intro", "There is another place we can find the same resistance.", 4.0f),
                Line("graph_slope_explain", "On a graph with voltage vertically and current horizontally, the slope gives us resistance. We found the same five ohms in the table and in the graph.", 8.5f),
                Line("unit_reveal", "A conductor has a resistance of one ohm when one volt across it produces a current of one ampere.", 6.5f),
                Line("adaptive_question_prompt", "Which calculation gives the resistance of our wire?", 4.0f),
                Line("scene12_complete", "Outstanding work! You have discovered electrical resistance and how to find it from measurements and graphs.", 6.0f),
                Line("final_handoff", "Now let us see how all three pieces come together to name the law.", 4.5f)
            };
            BindAudio(d12, "Scene12Dialogue");
            EditorUtility.SetDirty(d12);

            // Scene 13
            string p13 = Root + "ScriptableObjects/Dialogue/Scene13Dialogue.asset";
            var d13 = AssetDatabase.LoadAssetAtPath<DialogueSequence>(p13);
            if (d13 == null)
            {
                d13 = ScriptableObject.CreateInstance<DialogueSequence>();
                AssetDatabase.CreateAsset(d13, p13);
            }
            d13.lines = new[]
            {
                Line("intro_three_pieces", "Now we know all three pieces.", 3.5f),
                Line("piece_voltage", "The electrical push. V, potential difference.", 4.0f),
                Line("piece_current", "The flow. I, current.", 3.5f),
                Line("piece_resistance", "The opposition. R, resistance.", 3.5f),
                Line("assemble_prompt", "Start with R equals V divided by I. Drag or tap the symbols into the slots to express voltage V.", 6.5f),
                Line("law_naming", "And the relationship connecting them is...", 3.5f),
                Line("law_revealed", "Ohms Law! V equals I times R.", 4.5f),
                Line("challenge_prompt", "Our wire has a resistance of five ohms. Set the voltage so the current becomes 0.60 amperes.", 6.0f),
                Line("challenge_success", "Spot on! At 3.0 volts across our five-ohm wire, the current is exactly 0.60 amperes.", 5.5f),
                Line("condition_temp", "For this conductor, while its temperature stays constant, voltage is directly proportional to current.", 6.5f),
                Line("condition_constant", "The constant connecting them is resistance.", 4.0f),
                Line("finish_summary", "We have got our law. Now let us return to the mystery that started all of this.", 5.0f)
            };
            BindAudio(d13, "Scene13Dialogue");
            EditorUtility.SetDirty(d13);
        }

        static DialogueLine Line(string id, string text, float dur) => new DialogueLine { id = id, text = text, duration = dur, speaker = "WATTSON", emotion = "Curious" };

        static void BindAudio(DialogueSequence seq, string prefix)
        {
            foreach (var l in seq.lines)
            {
                string path = AudioFolder + prefix + "_" + l.id + ".wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    l.audio = clip;
                    l.duration = Mathf.Max(l.duration, clip.length + 0.1f);
                }
            }
        }

        public static void BuildScene12()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Environment & Room
            var env = Group("Environment");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/ReferenceWarmWood.mat") ?? Mat("LabWarmWood", new Color(.28f, .15f, .075f));
            var steel = Mat("LabFrame", new Color(.075f, .1f, .12f));
            var wall = Mat("LabWall", new Color(.11f, .18f, .22f));
            var floor = Mat("LabFloor", new Color(.08f, .10f, .12f));
            var amber = Mat("ReferenceAmberLight", new Color(1, .48f, .08f), true);

            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/ImportedLabEquipment/Workbench.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/LabEquipment/Tabletop.prefab");
            var table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
            table.name = "Workbench";
            table.transform.SetParent(env, false);
            table.transform.position = new Vector3(0, -.2f, 1.2f);
            table.transform.localScale = new Vector3(9, .4f, 4.8f);

            foreach (float x in new[] { -3.9f, 3.9f })
                foreach (float z in new[] { -.7f, 3.1f })
                    Cube("Bench leg", env, new Vector3(x, -1.15f, z), new Vector3(.22f, 1.8f, .22f), steel);

            Cube("Floor", env, new Vector3(0, -2.1f, 1), new Vector3(22, .2f, 22), floor);
            Cube("Rear wall", env, new Vector3(0, 1.2f, 6), new Vector3(18, 6.5f, .2f), wall);
            Cube("Left wall", env, new Vector3(-8, 1.2f, 0), new Vector3(.2f, 6.5f, 12), wall);
            Cube("Bench front light strip", env, new Vector3(0, -.04f, -1.19f), new Vector3(9, .018f, .02f), amber);

            // Lighting
            var mainLightGo = new GameObject("SoftLabLight", typeof(Light));
            mainLightGo.transform.SetParent(env);
            var mainLight = mainLightGo.GetComponent<Light>();
            mainLight.type = LightType.Directional;
            mainLight.intensity = 1.4f;
            mainLight.color = new Color(1, .92f, .82f);
            mainLight.transform.rotation = Quaternion.Euler(45, -30, 0);

            var warmLightGo = new GameObject("WarmTabletopLight", typeof(Light));
            warmLightGo.transform.SetParent(env);
            var warmLight = warmLightGo.GetComponent<Light>();
            warmLight.type = LightType.Point;
            warmLight.range = 8;
            warmLight.intensity = 2.0f;
            warmLight.color = new Color(1, .65f, .32f);
            warmLight.transform.position = new Vector3(0, 3, 1);

            // 2. Equipment
            var eq = Group("Equipment - edit models here");
            string[] prefabs = { "DCSource", "Ammeter", "NichromeWireXY", "Voltmeter", "Switch" };
            Vector3[] positions = { new Vector3(-3, .4f, 2.1f), new Vector3(-.6f, .48f, 2.5f), new Vector3(1.8f, .12f, 2.5f), new Vector3(1.6f, .48f, -.15f), new Vector3(-2.8f, .1f, -.15f) };
            Transform[] spawned = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/ImportedLabEquipment/" + prefabs[i] + ".prefab");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(p);
                inst.name = prefabs[i];
                inst.transform.SetParent(eq, false);
                inst.transform.position = positions[i];
                spawned[i] = inst.transform;
            }

            // Leads
            var leadsGroup = Group("Circuit leads", eq);
            var redMat = Mat("ReferenceRedLead", new Color(.7f, .025f, .018f));
            var blackMat = Mat("ReferenceBlackLead", new Color(.018f, .018f, .025f));
            CreateWireLead("Lead_Source_to_Ammeter", leadsGroup, positions[0] + new Vector3(.4f, 0, 0), positions[1] + new Vector3(-.4f, 0, 0), redMat);
            CreateWireLead("Lead_Ammeter_to_Wire", leadsGroup, positions[1] + new Vector3(.4f, 0, 0), positions[2] + new Vector3(-.7f, 0, 0), blackMat);
            CreateWireLead("Lead_Wire_to_Switch", leadsGroup, positions[2] + new Vector3(.7f, 0, 0), positions[4] + new Vector3(.4f, 0, 0), blackMat);
            CreateWireLead("Lead_Switch_to_Source", leadsGroup, positions[4] + new Vector3(-.4f, 0, 0), positions[0] + new Vector3(-.4f, 0, 0), blackMat);
            CreateWireLead("Lead_Voltmeter_Pos", leadsGroup, positions[3] + new Vector3(-.4f, 0, 0), positions[2] + new Vector3(-.7f, 0, 0), redMat);
            CreateWireLead("Lead_Voltmeter_Neg", leadsGroup, positions[3] + new Vector3(.4f, 0, 0), positions[2] + new Vector3(.7f, 0, 0), blackMat);

            // 3. Characters / Wattson
            var charGroup = Group("Characters");
            var wattsonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Wattson/Wattson.prefab");
            var wattsonInst = (GameObject)PrefabUtility.InstantiatePrefab(wattsonPrefab);
            wattsonInst.transform.SetParent(charGroup, false);
            wattsonInst.transform.position = new Vector3(3.7f, 1.8f, 2.7f);
            var wattsonCtrl = wattsonInst.GetComponent<WattsonController>();

            // 4. Camera
            var camGo = new GameObject("LearnerCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.transform.position = new Vector3(0, 4.5f, -6.7f);
            cam.transform.LookAt(new Vector3(0, .35f, 1.45f));
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 60f;
            cam.fieldOfView = 46f;
            wattsonInst.transform.LookAt(cam.transform);

            // 5. Canvas & UI
            var canvasGo = new GameObject("LabInterface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Interface/RoundedPanel.png");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Header Badge
            var header = CreateUIBox("LessonBadge", canvas.transform, new Vector2(.025f, .91f), new Vector2(.35f, .98f), new Color(.015f, .08f, .12f, .95f), panelSprite);
            var title = CreateLabel("Title", header.transform, "12 / DISCOVER RESISTANCE", 26, Vector2.zero, Vector2.one, font);
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleCenter;

            // Dialogue Strip
            var footer = CreateUIBox("WattsonDialogue", canvas.transform, new Vector2(.025f, .025f), new Vector2(.975f, .22f), new Color(.012f, .055f, .09f, .96f), panelSprite);
            var speaker = CreateLabel("Speaker", footer.transform, "WATTSON", 18, new Vector2(.08f, .78f), new Vector2(.6f, .98f), font);
            speaker.color = new Color(.03f, .85f, .95f);
            var subtitle = CreateLabel("Subtitle", footer.transform, "Welcome to wire resistance investigation.", 24, new Vector2(.08f, .22f), new Vector2(.75f, .78f), font);
            var progress = CreateLabel("Progress", footer.transform, "Ready", 16, new Vector2(.08f, .02f), new Vector2(.75f, .22f), font);

            var nextBtn = CreateButton("ContinueButton", footer.transform, "CONTINUE", new Vector2(.78f, .12f), new Vector2(.96f, .48f), font, panelSprite);
            var inspectBtn = CreateButton("InspectWireButton", footer.transform, "INSPECT WIRE XY", new Vector2(.78f, .52f), new Vector2(.96f, .88f), font, panelSprite);

            // Camera Orbit Controls
            var orbit = camGo.AddComponent<LabCameraOrbit>();
            var joystickRoot = CreateUIBox("ExploreControls", canvas.transform, new Vector2(.025f, .46f), new Vector2(.025f, .46f), Color.clear, null);
            var jRect = (RectTransform)joystickRoot.transform;
            jRect.pivot = new Vector2(0, .5f);
            jRect.sizeDelta = new Vector2(150, 230);
            jRect.anchoredPosition = Vector2.zero;
            var joyLabel = CreateLabel("LookLabel", joystickRoot.transform, "LOOK AROUND", 18, new Vector2(0, .88f), Vector2.one, font);
            joyLabel.alignment = TextAnchor.MiddleCenter;
            var joyGo = new GameObject("ViewJoystick", typeof(RectTransform), typeof(CanvasRenderer), typeof(LabOrbitJoystick));
            joyGo.transform.SetParent(joystickRoot.transform, false);
            var joyRect = (RectTransform)joyGo.transform;
            joyRect.anchorMin = new Vector2(0, .22f);
            joyRect.anchorMax = new Vector2(1, .88f);
            joyRect.offsetMin = joyRect.offsetMax = Vector2.zero;
            var resetBtn = CreateButton("ResetCamera", joystickRoot.transform, "RESET VIEW", new Vector2(0, 0), new Vector2(1, .21f), font, panelSprite);

            // Bench 3D Highlights
            var vHl = CreateWorldHighlight("VoltageHighlight", positions[0] + new Vector3(0, .8f, 0), "VOLTAGE (V)");
            var iHl = CreateWorldHighlight("CurrentHighlight", positions[1] + new Vector3(0, .8f, 0), "CURRENT (I)");
            var rHl = CreateWorldHighlight("WireHighlight", positions[2] + new Vector3(0, .8f, 0), "WIRE XY (R)");

            // Views:
            // 1. MicroscopicWireView
            var microBox = CreateUIBox("MicroscopicWireView", canvas.transform, new Vector2(.15f, .25f), new Vector2(.85f, .88f), new Color(.015f, .06f, .09f, .98f), panelSprite);
            var microView = microBox.gameObject.AddComponent<MicroscopicWireView>();
            var microTitle = CreateLabel("MicroTitle", microBox.transform, "MICROSCOPIC STRUCTURE  •  WIRE XY", 26, new Vector2(.05f, .88f), new Vector2(.95f, .97f), font);
            microTitle.alignment = TextAnchor.MiddleCenter;
            var microDisclaimer = CreateLabel("MicroDisclaimer", microBox.transform, "SIMPLIFIED VISUALIZATION", 16, new Vector2(.05f, .80f), new Vector2(.95f, .87f), font);
            microDisclaimer.alignment = TextAnchor.MiddleCenter;
            microDisclaimer.color = new Color(1, .8f, .2f);
            var microSim = CreateUIBox("SimContainer", microBox.transform, new Vector2(.05f, .25f), new Vector2(.95f, .78f), new Color(.02f, .035f, .05f, .95f), null);
            var microCaption = CreateLabel("MicroCaption", microBox.transform, "", 20, new Vector2(.05f, .12f), new Vector2(.75f, .24f), font);
            var microActBtn = CreateButton("MicroActionButton", microBox.transform, "FOLLOW DRIFT", new Vector2(.76f, .12f), new Vector2(.95f, .24f), font, panelSprite);
            var microBadge = CreateUIBox("ResistanceBadge", microBox.transform, new Vector2(.35f, .45f), new Vector2(.65f, .62f), new Color(.02f, .12f, .16f, .98f), panelSprite);
            var microBadgeText = CreateLabel("BadgeText", microBadge.transform, "RESISTANCE  (R)", 28, Vector2.zero, Vector2.one, font);
            microBadgeText.alignment = TextAnchor.MiddleCenter;
            microBadgeText.color = new Color(.03f, .85f, .95f);
            var microClose = CreateButton("CloseMicro", microBox.transform, "✕", new Vector2(.92f, .88f), new Vector2(.98f, .97f), font, panelSprite);

            SetField(microView, "simulationContainer", (RectTransform)microSim.transform);
            SetField(microView, "titleLabel", microTitle);
            SetField(microView, "captionLabel", microCaption);
            SetField(microView, "disclaimerLabel", microDisclaimer);
            SetField(microView, "actionButton", microActBtn);
            SetField(microView, "actionButtonText", microActBtn.GetComponentInChildren<Text>());
            SetField(microView, "resistanceBadge", microBadge.gameObject);
            SetField(microView, "closeButton", microClose);

            // 2. RatioCalculationView
            var ratioBox = CreateUIBox("RatioCalculationView", canvas.transform, new Vector2(.18f, .25f), new Vector2(.82f, .88f), new Color(.015f, .06f, .09f, .98f), panelSprite);
            var ratioView = ratioBox.gameObject.AddComponent<RatioCalculationView>();
            var ratioSrc = CreateLabel("RatioSource", ratioBox.transform, "OBSERVATION RATIOS", 20, new Vector2(.05f, .91f), new Vector2(.95f, .98f), font);
            ratioSrc.alignment = TextAnchor.MiddleCenter;
            var ratioPrompt = CreateLabel("RatioPrompt", ratioBox.transform, "Calculate V / I", 22, new Vector2(.05f, .83f), new Vector2(.95f, .90f), font);
            ratioPrompt.alignment = TextAnchor.MiddleCenter;

            // Table rows container
            var tableBox = CreateUIBox("TableContainer", ratioBox.transform, new Vector2(.05f, .38f), new Vector2(.95f, .82f), new Color(.02f, .04f, .06f, .95f), null);
            var rowList = new List<RatioCalculationView.TableRowUI>();
            float rowH = 1f / 6f;
            // Header
            var hV = CreateLabel("HV", tableBox.transform, "VOLTAGE (V)", 18, new Vector2(.02f, 1f - rowH), new Vector2(.32f, 1f), font);
            var hI = CreateLabel("HI", tableBox.transform, "CURRENT (I)", 18, new Vector2(.35f, 1f - rowH), new Vector2(.65f, 1f), font);
            var hR = CreateLabel("HR", tableBox.transform, "V ÷ I  (Ω)", 18, new Vector2(.68f, 1f - rowH), new Vector2(.98f, 1f), font);
            hV.alignment = hI.alignment = hR.alignment = TextAnchor.MiddleCenter;
            hV.color = hI.color = hR.color = new Color(.03f, .85f, .95f);

            for (int r = 0; r < 5; r++)
            {
                float yTop = 1f - (r + 1) * rowH;
                float yBottom = 1f - (r + 2) * rowH;
                var rowBg = CreateUIBox("Row_" + r, tableBox.transform, new Vector2(0, yBottom), new Vector2(1, yTop), r % 2 == 0 ? new Color(.04f, .08f, .11f) : new Color(.03f, .06f, .08f), null);
                var tV = CreateLabel("V", rowBg.transform, "", 20, new Vector2(.02f, 0), new Vector2(.32f, 1), font);
                var tI = CreateLabel("I", rowBg.transform, "", 20, new Vector2(.35f, 0), new Vector2(.65f, 1), font);
                var tR = CreateLabel("R", rowBg.transform, "", 20, new Vector2(.68f, 0), new Vector2(.98f, 1), font);
                tV.alignment = tI.alignment = tR.alignment = TextAnchor.MiddleCenter;
                rowList.Add(new RatioCalculationView.TableRowUI { voltageText = tV, currentText = tI, ratioText = tR, rowHighlight = rowBg.gameObject });
            }

            // Options group
            var optBox = CreateUIBox("OptionsGroup", ratioBox.transform, new Vector2(.15f, .22f), new Vector2(.85f, .36f), Color.clear, null);
            var optButtons = new Button[3];
            var optTexts = new Text[3];
            for (int b = 0; b < 3; b++)
            {
                float x1 = b * 0.34f;
                float x2 = x1 + 0.31f;
                optButtons[b] = CreateButton("Option_" + b, optBox.transform, "5 Ω", new Vector2(x1, 0), new Vector2(x2, 1), font, panelSprite);
                optTexts[b] = optButtons[b].GetComponentInChildren<Text>();
            }

            // Question group
            var qBox = CreateUIBox("QuestionGroup", ratioBox.transform, new Vector2(.05f, .12f), new Vector2(.95f, .36f), Color.clear, null);
            var qPrompt = CreateLabel("QPrompt", qBox.transform, "What do you notice about V divided by I?", 20, new Vector2(0, .65f), new Vector2(1, 1), font);
            qPrompt.alignment = TextAnchor.MiddleCenter;
            var qButtons = new Button[3];
            var qTexts = new Text[3];
            for (int b = 0; b < 3; b++)
            {
                float x1 = b * 0.34f;
                float x2 = x1 + 0.31f;
                qButtons[b] = CreateButton("QOpt_" + b, qBox.transform, "Same", new Vector2(x1, 0), new Vector2(x2, .60f), font, panelSprite);
                qTexts[b] = qButtons[b].GetComponentInChildren<Text>();
            }

            var ratioFeedback = CreateLabel("Feedback", ratioBox.transform, "", 20, new Vector2(.05f, .02f), new Vector2(.95f, .12f), font);
            ratioFeedback.alignment = TextAnchor.MiddleCenter;
            var formulaBanner = CreateUIBox("FormulaBanner", ratioBox.transform, new Vector2(.1f, .02f), new Vector2(.9f, .15f), new Color(.02f, .14f, .18f, .98f), panelSprite);
            var formulaText = CreateLabel("FormulaText", formulaBanner.transform, "R = V / I = 5 Ω", 24, Vector2.zero, Vector2.one, font);
            formulaText.alignment = TextAnchor.MiddleCenter;
            formulaText.color = new Color(.03f, .85f, .95f);

            SetField(ratioView, "rows", rowList.ToArray());
            SetField(ratioView, "sourceLabel", ratioSrc);
            SetField(ratioView, "promptText", ratioPrompt);
            SetField(ratioView, "optionsGroup", optBox.gameObject);
            SetField(ratioView, "optionButtons", optButtons);
            SetField(ratioView, "optionTexts", optTexts);
            SetField(ratioView, "questionGroup", qBox.gameObject);
            SetField(ratioView, "questionPromptText", qPrompt);
            SetField(ratioView, "questionButtons", qButtons);
            SetField(ratioView, "questionButtonTexts", qTexts);
            SetField(ratioView, "feedbackText", ratioFeedback);
            SetField(ratioView, "formulaRevealBanner", formulaBanner.gameObject);
            SetField(ratioView, "formulaRevealText", formulaText);

            // 3. ResistanceGraphView
            var graphBox = CreateUIBox("ResistanceGraphView", canvas.transform, new Vector2(.18f, .24f), new Vector2(.82f, .88f), new Color(.015f, .06f, .09f, .98f), panelSprite);
            var graphView = graphBox.gameObject.AddComponent<ResistanceGraphView>();
            var graphTitle = CreateLabel("GraphTitle", graphBox.transform, "V-VERSUS-I GRAPH  •  SLOPE DETERMINATION", 22, new Vector2(.05f, .92f), new Vector2(.95f, .98f), font);
            graphTitle.alignment = TextAnchor.MiddleCenter;
            var graphInstruction = CreateLabel("GraphInstruction", graphBox.transform, "Select two points to find resistance (slope = ΔV / ΔI)", 18, new Vector2(.05f, .85f), new Vector2(.95f, .91f), font);
            graphInstruction.alignment = TextAnchor.MiddleCenter;

            var graphArea = CreateUIBox("GraphArea", graphBox.transform, new Vector2(.15f, .22f), new Vector2(.85f, .82f), new Color(.015f, .035f, .05f, .95f), null);
            var graphCanvas = new GameObject("GraphCanvas", typeof(RectTransform));
            graphCanvas.transform.SetParent(graphArea.transform, false);
            var gcr = (RectTransform)graphCanvas.transform;
            gcr.anchorMin = Vector2.zero;
            gcr.anchorMax = Vector2.one;
            gcr.offsetMin = gcr.offsetMax = Vector2.zero;
            var graphGraphic = graphCanvas.AddComponent<ResistanceGraphGraphic>();

            // Axis labels
            CreateLabel("AxisV", graphBox.transform, "Potential Difference V (V)  ↑", 18, new Vector2(.01f, .22f), new Vector2(.14f, .82f), font).alignment = TextAnchor.MiddleRight;
            CreateLabel("AxisI", graphBox.transform, "Electric Current I (A)  →", 18, new Vector2(.15f, .14f), new Vector2(.85f, .21f), font).alignment = TextAnchor.MiddleCenter;

            // 5 interactive point buttons
            var ptButtons = new Button[5];
            for (int p = 0; p < 5; p++)
            {
                var ptBtn = CreateButton("Point_" + p, graphArea.transform, "●", Vector2.zero, Vector2.zero, font, null);
                var prt = (RectTransform)ptBtn.transform;
                prt.sizeDelta = new Vector2(64, 64);
                ptBtn.GetComponentInChildren<Text>().fontSize = 28;
                ptButtons[p] = ptBtn;
            }

            var slopePanel = CreateUIBox("SlopePanel", graphBox.transform, new Vector2(.15f, .02f), new Vector2(.85f, .14f), new Color(.02f, .12f, .16f, .95f), panelSprite);
            var slopeResultText = CreateLabel("SlopeResult", slopePanel.transform, "Slope = ΔV / ΔI = 5.0 Ω", 20, Vector2.zero, Vector2.one, font);
            slopeResultText.alignment = TextAnchor.MiddleCenter;
            slopeResultText.color = new Color(.03f, .85f, .95f);
            var graphContinueBtn = CreateButton("GraphContinue", graphBox.transform, "CONFIRM SLOPE", new Vector2(.75f, .02f), new Vector2(.96f, .14f), font, panelSprite);

            SetField(graphView, "graphGraphic", graphGraphic);
            SetField(graphView, "graphRect", (RectTransform)graphArea.transform);
            SetField(graphView, "pointButtons", ptButtons);
            SetField(graphView, "instructionText", graphInstruction);
            SetField(graphView, "slopeResultText", slopeResultText);
            SetField(graphView, "slopePanel", slopePanel.gameObject);
            SetField(graphView, "continueButton", graphContinueBtn);

            // Unit reveal panel
            var unitBox = CreateUIBox("UnitRevealPanel", canvas.transform, new Vector2(.25f, .4f), new Vector2(.75f, .75f), new Color(.015f, .08f, .12f, .98f), panelSprite);
            var unitText = CreateLabel("UnitText", unitBox.transform, "OHM  (Ω)\n\n1 Ω  =  1 V  /  1 A", 28, Vector2.zero, Vector2.one, font);
            unitText.alignment = TextAnchor.MiddleCenter;
            unitText.color = new Color(.03f, .85f, .95f);

            // Question Panel
            var questionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/UI/QuestionPanel.prefab");
            var qPanel = (GameObject)PrefabUtility.InstantiatePrefab(questionPrefab);
            qPanel.name = "QuestionUI";
            qPanel.transform.SetParent(canvas.transform, false);
            var questionUI = qPanel.GetComponent<QuestionUI>();

            // Completion Panel
            var compBox = CreateUIBox("CompletionPanel", canvas.transform, new Vector2(.25f, .35f), new Vector2(.75f, .75f), new Color(.015f, .08f, .12f, .98f), panelSprite);
            var compTitle = CreateLabel("CompTitle", compBox.transform, "DISCOVER RESISTANCE COMPLETE", 28, new Vector2(0, .6f), new Vector2(1, .9f), font);
            compTitle.alignment = TextAnchor.MiddleCenter;
            compTitle.color = new Color(.03f, .85f, .95f);
            var compDesc = CreateLabel("CompDesc", compBox.transform, "You have found resistance both in the data table and from the graph slope: R = V / I = 5.0 Ω.", 22, new Vector2(.05f, .25f), new Vector2(.95f, .55f), font);
            compDesc.alignment = TextAnchor.MiddleCenter;
            var compBtn = CreateButton("FinishSceneBtn", compBox.transform, "CONTINUE TO OHM'S LAW", new Vector2(.25f, .05f), new Vector2(.75f, .22f), font, panelSprite);

            // Fade CanvasGroup
            var fadeGo = new GameObject("CinematicFade", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            fadeGo.transform.SetParent(canvas.transform, false);
            var frt = (RectTransform)fadeGo.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            var fadeImg = fadeGo.GetComponent<Image>();
            fadeImg.color = Color.black;
            var fadeGroup = fadeGo.GetComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;

            // Chat UI
            var chatMgr = canvasGo.AddComponent<WattsonChatManager>();
            chatMgr.SetService(AssetDatabase.LoadAssetAtPath<WattsonServiceSettings>(Root + "ScriptableObjects/WattsonService.asset"));
            var chatCard = CreateChatCard(canvas.transform, font, panelSprite);
            var chatUI = canvasGo.AddComponent<WattsonChatUI>();
            var askChatBtn = CreateButton("AskWattson", canvas.transform, "ASK WATTSON", new Vector2(.80f, .91f), new Vector2(.965f, .98f), font, panelSprite);
            ConfigureChatUI(chatUI, chatCard, askChatBtn, wattsonCtrl);

            // Learning Systems
            var sys = Group("LearningSystems");
            var flow = sys.gameObject.AddComponent<Scene12FlowManager>();
            var profileMgr = sys.gameObject.AddComponent<StudentProfileManager>();
            var adaptMgr = sys.gameObject.AddComponent<AdaptiveLearningManager>();
            var questMgr = sys.gameObject.AddComponent<QuestionManager>();
            var trans = sys.gameObject.AddComponent<Scene12To13Transition>();
            trans.Configure(flow, fadeGroup);

            var diagPres = sys.gameObject.AddComponent<DialoguePresenter>();
            var audSource = sys.gameObject.AddComponent<AudioSource>();
            audSource.playOnAwake = false;
            diagPres.Configure(speaker, subtitle, audSource);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Wire Scene12FlowManager
            var rulesAsset = AssetDatabase.LoadAssetAtPath<AdaptiveRules>(Root + "ScriptableObjects/Concepts/PrototypeRules.asset");
            var seq12 = AssetDatabase.LoadAssetAtPath<DialogueSequence>(Root + "ScriptableObjects/Dialogue/Scene12Dialogue.asset");
            var q12 = AssetDatabase.LoadAssetAtPath<QuestionDefinition>(Root + "ScriptableObjects/Questions/Scene12ResistanceQuestion.asset");

            SetField(flow, "rules", rulesAsset);
            SetField(flow, "profiles", profileMgr);
            SetField(flow, "adaptive", adaptMgr);
            SetField(flow, "questions", questMgr);
            SetField(flow, "wattson", wattsonCtrl);
            SetField(flow, "chat", chatMgr);
            SetField(flow, "chatUI", chatUI);
            SetField(flow, "dialogue", diagPres);
            SetField(flow, "learnerCamera", cam);
            SetField(flow, "fade", fadeGroup);
            SetField(flow, "lines", seq12);
            SetField(flow, "microView", microView);
            SetField(flow, "ratioView", ratioView);
            SetField(flow, "graphView", graphView);
            SetField(flow, "questionUI", questionUI);
            SetField(flow, "resistanceQuestion", q12);
            SetField(flow, "supportText", progress);
            SetField(flow, "progressText", progress);
            SetField(flow, "hintText", subtitle);
            SetField(flow, "unitRevealPanel", unitBox.gameObject);
            SetField(flow, "unitRevealText", unitText);
            SetField(flow, "completionPanel", compBox.gameObject);
            SetField(flow, "continueButton", compBtn);
            SetField(flow, "inspectWirePromptButton", inspectBtn);
            SetField(flow, "voltageHighlight", vHl);
            SetField(flow, "currentHighlight", iHl);
            SetField(flow, "wireHighlight", rHl);

            EditorSceneManager.SaveScene(scene, Root + "Scenes/Scene_12_DiscoverResistance.unity");
            Debug.Log("Scene_12_DiscoverResistance saved.");
        }

        public static void BuildScene13()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Environment & Room
            var env = Group("Environment");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/ReferenceWarmWood.mat") ?? Mat("LabWarmWood", new Color(.28f, .15f, .075f));
            var steel = Mat("LabFrame", new Color(.075f, .1f, .12f));
            var wall = Mat("LabWall", new Color(.11f, .18f, .22f));
            var floor = Mat("LabFloor", new Color(.08f, .10f, .12f));
            var amber = Mat("ReferenceAmberLight", new Color(1, .48f, .08f), true);

            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/ImportedLabEquipment/Workbench.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/LabEquipment/Tabletop.prefab");
            var table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
            table.name = "Workbench";
            table.transform.SetParent(env, false);
            table.transform.position = new Vector3(0, -.2f, 1.2f);
            table.transform.localScale = new Vector3(9, .4f, 4.8f);

            foreach (float x in new[] { -3.9f, 3.9f })
                foreach (float z in new[] { -.7f, 3.1f })
                    Cube("Bench leg", env, new Vector3(x, -1.15f, z), new Vector3(.22f, 1.8f, .22f), steel);

            Cube("Floor", env, new Vector3(0, -2.1f, 1), new Vector3(22, .2f, 22), floor);
            Cube("Rear wall", env, new Vector3(0, 1.2f, 6), new Vector3(18, 6.5f, .2f), wall);
            Cube("Left wall", env, new Vector3(-8, 1.2f, 0), new Vector3(.2f, 6.5f, 12), wall);
            Cube("Bench front light strip", env, new Vector3(0, -.04f, -1.19f), new Vector3(9, .018f, .02f), amber);

            // Lighting
            var mainLightGo = new GameObject("SoftLabLight", typeof(Light));
            mainLightGo.transform.SetParent(env);
            var mainLight = mainLightGo.GetComponent<Light>();
            mainLight.type = LightType.Directional;
            mainLight.intensity = 1.4f;
            mainLight.color = new Color(1, .92f, .82f);
            mainLight.transform.rotation = Quaternion.Euler(45, -30, 0);

            var warmLightGo = new GameObject("WarmTabletopLight", typeof(Light));
            warmLightGo.transform.SetParent(env);
            var warmLight = warmLightGo.GetComponent<Light>();
            warmLight.type = LightType.Point;
            warmLight.range = 8;
            warmLight.intensity = 2.0f;
            warmLight.color = new Color(1, .65f, .32f);
            warmLight.transform.position = new Vector3(0, 3, 1);

            // 2. Equipment
            var eq = Group("Equipment - edit models here");
            string[] prefabs = { "DCSource", "Ammeter", "NichromeWireXY", "Voltmeter", "Switch" };
            Vector3[] positions = { new Vector3(-3, .4f, 2.1f), new Vector3(-.6f, .48f, 2.5f), new Vector3(1.8f, .12f, 2.5f), new Vector3(1.6f, .48f, -.15f), new Vector3(-2.8f, .1f, -.15f) };
            Transform[] spawned = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/ImportedLabEquipment/" + prefabs[i] + ".prefab");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(p);
                inst.name = prefabs[i];
                inst.transform.SetParent(eq, false);
                inst.transform.position = positions[i];
                spawned[i] = inst.transform;
            }

            // Leads
            var leadsGroup = Group("Circuit leads", eq);
            var redMat = Mat("ReferenceRedLead", new Color(.7f, .025f, .018f));
            var blackMat = Mat("ReferenceBlackLead", new Color(.018f, .018f, .025f));
            CreateWireLead("Lead_Source_to_Ammeter", leadsGroup, positions[0] + new Vector3(.4f, 0, 0), positions[1] + new Vector3(-.4f, 0, 0), redMat);
            CreateWireLead("Lead_Ammeter_to_Wire", leadsGroup, positions[1] + new Vector3(.4f, 0, 0), positions[2] + new Vector3(-.7f, 0, 0), blackMat);
            CreateWireLead("Lead_Wire_to_Switch", leadsGroup, positions[2] + new Vector3(.7f, 0, 0), positions[4] + new Vector3(.4f, 0, 0), blackMat);
            CreateWireLead("Lead_Switch_to_Source", leadsGroup, positions[4] + new Vector3(-.4f, 0, 0), positions[0] + new Vector3(-.4f, 0, 0), blackMat);
            CreateWireLead("Lead_Voltmeter_Pos", leadsGroup, positions[3] + new Vector3(-.4f, 0, 0), positions[2] + new Vector3(-.7f, 0, 0), redMat);
            CreateWireLead("Lead_Voltmeter_Neg", leadsGroup, positions[3] + new Vector3(.4f, 0, 0), positions[2] + new Vector3(.7f, 0, 0), blackMat);

            // 3. Characters / Wattson
            var charGroup = Group("Characters");
            var wattsonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Wattson/Wattson.prefab");
            var wattsonInst = (GameObject)PrefabUtility.InstantiatePrefab(wattsonPrefab);
            wattsonInst.transform.SetParent(charGroup, false);
            wattsonInst.transform.position = new Vector3(3.7f, 1.8f, 2.7f);
            var wattsonCtrl = wattsonInst.GetComponent<WattsonController>();

            // 4. Camera
            var camGo = new GameObject("LearnerCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.transform.position = new Vector3(0, 4.5f, -6.7f);
            cam.transform.LookAt(new Vector3(0, .35f, 1.45f));
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 60f;
            cam.fieldOfView = 46f;
            wattsonInst.transform.LookAt(cam.transform);

            // 5. Canvas & UI
            var canvasGo = new GameObject("LabInterface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Interface/RoundedPanel.png");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Header Badge
            var header = CreateUIBox("LessonBadge", canvas.transform, new Vector2(.025f, .91f), new Vector2(.35f, .98f), new Color(.015f, .08f, .12f, .95f), panelSprite);
            var title = CreateLabel("Title", header.transform, "13 / OHM'S LAW", 26, Vector2.zero, Vector2.one, font);
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleCenter;

            // Dialogue Strip
            var footer = CreateUIBox("WattsonDialogue", canvas.transform, new Vector2(.025f, .025f), new Vector2(.975f, .22f), new Color(.012f, .055f, .09f, .96f), panelSprite);
            var speaker = CreateLabel("Speaker", footer.transform, "WATTSON", 18, new Vector2(.08f, .78f), new Vector2(.6f, .98f), font);
            speaker.color = new Color(.03f, .85f, .95f);
            var subtitle = CreateLabel("Subtitle", footer.transform, "Assembling the relationship between V, I, and R.", 24, new Vector2(.08f, .22f), new Vector2(.75f, .78f), font);
            var progress = CreateLabel("Progress", footer.transform, "Ready", 16, new Vector2(.08f, .02f), new Vector2(.75f, .22f), font);

            // Camera Orbit Controls
            var orbit = camGo.AddComponent<LabCameraOrbit>();
            var joystickRoot = CreateUIBox("ExploreControls", canvas.transform, new Vector2(.025f, .46f), new Vector2(.025f, .46f), Color.clear, null);
            var jRect = (RectTransform)joystickRoot.transform;
            jRect.pivot = new Vector2(0, .5f);
            jRect.sizeDelta = new Vector2(150, 230);
            jRect.anchoredPosition = Vector2.zero;
            var joyLabel = CreateLabel("LookLabel", joystickRoot.transform, "LOOK AROUND", 18, new Vector2(0, .88f), Vector2.one, font);
            joyLabel.alignment = TextAnchor.MiddleCenter;
            var joyGo = new GameObject("ViewJoystick", typeof(RectTransform), typeof(CanvasRenderer), typeof(LabOrbitJoystick));
            joyGo.transform.SetParent(joystickRoot.transform, false);
            var joyRect = (RectTransform)joyGo.transform;
            joyRect.anchorMin = new Vector2(0, .22f);
            joyRect.anchorMax = new Vector2(1, .88f);
            joyRect.offsetMin = joyRect.offsetMax = Vector2.zero;
            var resetBtn = CreateButton("ResetCamera", joystickRoot.transform, "RESET VIEW", new Vector2(0, 0), new Vector2(1, .21f), font, panelSprite);

            // Bench 3D Highlights
            var vHl = CreateWorldHighlight("VoltageHighlight", positions[0] + new Vector3(0, .8f, 0), "V — PUSH");
            var iHl = CreateWorldHighlight("CurrentHighlight", positions[1] + new Vector3(0, .8f, 0), "I — FLOW");
            var rHl = CreateWorldHighlight("WireHighlight", positions[2] + new Vector3(0, .8f, 0), "R — OPPOSITION");

            // Views:
            // 1. EquationAssemblyView
            var eqBox = CreateUIBox("EquationAssemblyView", canvas.transform, new Vector2(.2f, .26f), new Vector2(.8f, .86f), new Color(.015f, .06f, .09f, .98f), panelSprite);
            var eqView = eqBox.gameObject.AddComponent<EquationAssemblyView>();
            var eqTitle = CreateLabel("EqTitle", eqBox.transform, "ASSEMBLE THE RELATIONSHIP", 26, new Vector2(.05f, .88f), new Vector2(.95f, .97f), font);
            eqTitle.alignment = TextAnchor.MiddleCenter;
            var eqInitial = CreateLabel("EqInitial", eqBox.transform, "Starting from:  R  =  V  /  I", 22, new Vector2(.05f, .78f), new Vector2(.95f, .86f), font);
            eqInitial.alignment = TextAnchor.MiddleCenter;
            eqInitial.color = new Color(1, .8f, .2f);
            var eqPrompt = CreateLabel("EqPrompt", eqBox.transform, "Drag or tap the symbols into the slots:", 20, new Vector2(.05f, .68f), new Vector2(.95f, .76f), font);
            eqPrompt.alignment = TextAnchor.MiddleCenter;

            // Slots area: [Slot 0] = [Slot 1] × [Slot 2]
            var slotsBox = CreateUIBox("SlotsArea", eqBox.transform, new Vector2(.05f, .42f), new Vector2(.95f, .65f), Color.clear, null);
            var s0 = CreateSlot("Slot_0", slotsBox.transform, new Vector2(.10f, .1f), new Vector2(.28f, .9f), font, panelSprite);
            var eqSign = CreateLabel("SignEqual", slotsBox.transform, "=", 36, new Vector2(.30f, .1f), new Vector2(.38f, .9f), font);
            eqSign.alignment = TextAnchor.MiddleCenter;
            var s1 = CreateSlot("Slot_1", slotsBox.transform, new Vector2(.40f, .1f), new Vector2(.58f, .9f), font, panelSprite);
            var mulSign = CreateLabel("SignMul", slotsBox.transform, "×", 36, new Vector2(.60f, .1f), new Vector2(.68f, .9f), font);
            mulSign.alignment = TextAnchor.MiddleCenter;
            var s2 = CreateSlot("Slot_2", slotsBox.transform, new Vector2(.70f, .1f), new Vector2(.88f, .9f), font, panelSprite);

            // Token pool
            var tokenPool = CreateUIBox("TokenPool", eqBox.transform, new Vector2(.1f, .20f), new Vector2(.9f, .38f), new Color(.02f, .04f, .06f, .95f), panelSprite);
            var tV = CreateToken("Token_V", tokenPool.transform, EquationAssemblyView.TokenType.V, "V", new Vector2(.1f, .15f), new Vector2(.3f, .85f), font, panelSprite);
            var tI = CreateToken("Token_I", tokenPool.transform, EquationAssemblyView.TokenType.I, "I", new Vector2(.4f, .15f), new Vector2(.6f, .85f), font, panelSprite);
            var tR = CreateToken("Token_R", tokenPool.transform, EquationAssemblyView.TokenType.R, "R", new Vector2(.7f, .15f), new Vector2(.9f, .85f), font, panelSprite);

            var eqBanner = CreateUIBox("SuccessBanner", eqBox.transform, new Vector2(.05f, .03f), new Vector2(.95f, .16f), new Color(.02f, .14f, .18f, .98f), panelSprite);
            var eqBannerText = CreateLabel("SuccessText", eqBanner.transform, "OHM'S LAW:  V  =  I  ×  R", 24, Vector2.zero, Vector2.one, font);
            eqBannerText.alignment = TextAnchor.MiddleCenter;
            eqBannerText.color = new Color(.03f, .85f, .95f);
            var clearBtn = CreateButton("ResetEq", eqBox.transform, "RESET", new Vector2(.82f, .88f), new Vector2(.95f, .96f), font, panelSprite);

            SetField(eqView, "promptText", eqPrompt);
            SetField(eqView, "initialFormulaText", eqInitial);
            SetField(eqView, "slots", new[] { s0, s1, s2 });
            SetField(eqView, "tokens", new[] { tV, tI, tR });
            SetField(eqView, "tokenPool", (RectTransform)tokenPool.transform);
            SetField(eqView, "successBanner", eqBanner.gameObject);
            SetField(eqView, "successBannerText", eqBannerText);
            SetField(eqView, "clearButton", clearBtn);

            // 2. BenchCircuitChallenge
            var chalBox = CreateUIBox("BenchCircuitChallenge", canvas.transform, new Vector2(.2f, .25f), new Vector2(.8f, .88f), new Color(.015f, .06f, .09f, .98f), panelSprite);
            var chalView = chalBox.gameObject.AddComponent<BenchCircuitChallenge>();
            var chalTitle = CreateLabel("ChalTitle", chalBox.transform, "PRACTICAL CHALLENGE  •  TRY THE LAW", 24, new Vector2(.05f, .90f), new Vector2(.95f, .97f), font);
            chalTitle.alignment = TextAnchor.MiddleCenter;
            var chalPrompt = CreateLabel("ChalPrompt", chalBox.transform, "Wire R = 5 Ω. Set voltage so current reaches 0.60 A.", 20, new Vector2(.05f, .82f), new Vector2(.95f, .89f), font);
            chalPrompt.alignment = TextAnchor.MiddleCenter;
            chalPrompt.color = new Color(1, .8f, .2f);

            // Meters display box
            var metersBox = CreateUIBox("MetersBox", chalBox.transform, new Vector2(.1f, .48f), new Vector2(.9f, .78f), new Color(.02f, .04f, .06f, .95f), null);
            var vDisplay = CreateLabel("VDisplay", metersBox.transform, "0.0 V", 32, new Vector2(.05f, .2f), new Vector2(.45f, .8f), font);
            vDisplay.alignment = TextAnchor.MiddleCenter;
            vDisplay.color = new Color(.03f, .85f, .95f);
            var iDisplay = CreateLabel("IDisplay", metersBox.transform, "0.00 A", 32, new Vector2(.55f, .2f), new Vector2(.95f, .8f), font);
            iDisplay.alignment = TextAnchor.MiddleCenter;
            iDisplay.color = new Color(1, .85f, .2f);
            CreateLabel("VLabel", metersBox.transform, "VOLTMETER (V)", 16, new Vector2(.05f, .75f), new Vector2(.45f, .95f), font).alignment = TextAnchor.MiddleCenter;
            CreateLabel("ILabel", metersBox.transform, "AMMETER (I)", 16, new Vector2(.55f, .75f), new Vector2(.95f, .95f), font).alignment = TextAnchor.MiddleCenter;

            // Voltage Stepping Controls
            var stepDownBtn = CreateButton("StepDownBtn", chalBox.transform, "- 0.1 V", new Vector2(.10f, .34f), new Vector2(.28f, .44f), font, panelSprite);
            var stepUpBtn = CreateButton("StepUpBtn", chalBox.transform, "+ 0.1 V", new Vector2(.72f, .34f), new Vector2(.90f, .44f), font, panelSprite);
            var p1 = CreateButton("Preset1", chalBox.transform, "1 V", new Vector2(.30f, .34f), new Vector2(.37f, .44f), font, panelSprite);
            var p2 = CreateButton("Preset2", chalBox.transform, "2 V", new Vector2(.38f, .34f), new Vector2(.45f, .44f), font, panelSprite);
            var p3 = CreateButton("Preset3", chalBox.transform, "3 V", new Vector2(.46f, .34f), new Vector2(.54f, .44f), font, panelSprite);
            var p4 = CreateButton("Preset4", chalBox.transform, "4 V", new Vector2(.55f, .34f), new Vector2(.62f, .44f), font, panelSprite);
            var p5 = CreateButton("Preset5", chalBox.transform, "5 V", new Vector2(.63f, .34f), new Vector2(.70f, .44f), font, panelSprite);

            var chalFeedback = CreateLabel("ChalFeedback", chalBox.transform, "Adjust voltage using the knob or step buttons.", 18, new Vector2(.05f, .22f), new Vector2(.95f, .32f), font);
            chalFeedback.alignment = TextAnchor.MiddleCenter;

            var confirmBtn = CreateButton("ConfirmBtn", chalBox.transform, "CONFIRM SETTING", new Vector2(.3f, .06f), new Vector2(.7f, .18f), font, panelSprite);
            var chalHintBtn = CreateButton("HintBtn", chalBox.transform, "HINT", new Vector2(.05f, .06f), new Vector2(.25f, .18f), font, panelSprite);
            var autoBtn = CreateButton("AutoBtn", chalBox.transform, "SET 3.0 V", new Vector2(.75f, .06f), new Vector2(.95f, .18f), font, panelSprite);

            SetField(chalView, "stepDownButton", stepDownBtn);
            SetField(chalView, "stepUpButton", stepUpBtn);
            SetField(chalView, "presetButtons", new[] { p1, p2, p3, p4, p5 });
            SetField(chalView, "confirmButton", confirmBtn);
            SetField(chalView, "hintButton", chalHintBtn);
            SetField(chalView, "autoSetButton", autoBtn);
            SetField(chalView, "promptText", chalPrompt);
            SetField(chalView, "voltmeterText", vDisplay);
            SetField(chalView, "ammeterText", iDisplay);
            SetField(chalView, "currentVoltageDisplay", vDisplay);
            SetField(chalView, "currentAmperageDisplay", iDisplay);
            SetField(chalView, "feedbackText", chalFeedback);

            // Condition Card
            var condBox = CreateUIBox("ConditionCard", canvas.transform, new Vector2(.25f, .32f), new Vector2(.75f, .78f), new Color(.015f, .08f, .12f, .98f), panelSprite);
            var condText = CreateLabel("CondText", condBox.transform, "OHM'S LAW:  V  =  I  ×  R", 22, new Vector2(.05f, .05f), new Vector2(.95f, .95f), font);
            condText.alignment = TextAnchor.MiddleCenter;

            // Completion Panel
            var compBox = CreateUIBox("CompletionPanel", canvas.transform, new Vector2(.22f, .28f), new Vector2(.78f, .82f), new Color(.015f, .08f, .12f, .98f), panelSprite);
            var compTitle = CreateLabel("CompTitle", compBox.transform, "OHM'S LAW INVESTIGATION COMPLETE", 28, new Vector2(0, .75f), new Vector2(1, .95f), font);
            compTitle.alignment = TextAnchor.MiddleCenter;
            compTitle.color = new Color(.03f, .85f, .95f);
            var compDesc = CreateLabel("CompDesc", compBox.transform, "You have completed the investigation!\n\n• Verified circuit topology and meters\n• Plotted V vs I graph with slope R = 5 Ω\n• Discovered resistance R = V / I\n• Assembled and applied Ohm's Law: V = IR", 20, new Vector2(.05f, .30f), new Vector2(.95f, .72f), font);
            compDesc.alignment = TextAnchor.MiddleCenter;

            var replayBtn = CreateButton("ReplayBtn", compBox.transform, "REPLAY SCENE 13", new Vector2(.1f, .08f), new Vector2(.45f, .22f), font, panelSprite);
            var labBtn = CreateButton("ReturnToLabBtn", compBox.transform, "RETURN TO LAB", new Vector2(.55f, .08f), new Vector2(.9f, .22f), font, panelSprite);

            // Fade CanvasGroup
            var fadeGo = new GameObject("CinematicFade", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            fadeGo.transform.SetParent(canvas.transform, false);
            var frt = (RectTransform)fadeGo.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            var fadeImg = fadeGo.GetComponent<Image>();
            fadeImg.color = Color.black;
            var fadeGroup = fadeGo.GetComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;

            // Chat UI
            var chatMgr = canvasGo.AddComponent<WattsonChatManager>();
            chatMgr.SetService(AssetDatabase.LoadAssetAtPath<WattsonServiceSettings>(Root + "ScriptableObjects/WattsonService.asset"));
            var chatCard = CreateChatCard(canvas.transform, font, panelSprite);
            var chatUI = canvasGo.AddComponent<WattsonChatUI>();
            var askChatBtn = CreateButton("AskWattson", canvas.transform, "ASK WATTSON", new Vector2(.80f, .91f), new Vector2(.965f, .98f), font, panelSprite);
            ConfigureChatUI(chatUI, chatCard, askChatBtn, wattsonCtrl);

            // Learning Systems
            var sys = Group("LearningSystems");
            var flow = sys.gameObject.AddComponent<Scene13FlowManager>();
            var profileMgr = sys.gameObject.AddComponent<StudentProfileManager>();
            var adaptMgr = sys.gameObject.AddComponent<AdaptiveLearningManager>();

            var diagPres = sys.gameObject.AddComponent<DialoguePresenter>();
            var audSource = sys.gameObject.AddComponent<AudioSource>();
            audSource.playOnAwake = false;
            diagPres.Configure(speaker, subtitle, audSource);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Wire Scene13FlowManager
            var rulesAsset = AssetDatabase.LoadAssetAtPath<AdaptiveRules>(Root + "ScriptableObjects/Concepts/PrototypeRules.asset");
            var seq13 = AssetDatabase.LoadAssetAtPath<DialogueSequence>(Root + "ScriptableObjects/Dialogue/Scene13Dialogue.asset");

            SetField(flow, "rules", rulesAsset);
            SetField(flow, "profiles", profileMgr);
            SetField(flow, "adaptive", adaptMgr);
            SetField(flow, "wattson", wattsonCtrl);
            SetField(flow, "chat", chatMgr);
            SetField(flow, "chatUI", chatUI);
            SetField(flow, "dialogue", diagPres);
            SetField(flow, "learnerCamera", cam);
            SetField(flow, "fade", fadeGroup);
            SetField(flow, "lines", seq13);
            SetField(flow, "equationView", eqView);
            SetField(flow, "challengeView", chalView);
            SetField(flow, "supportText", progress);
            SetField(flow, "progressText", progress);
            SetField(flow, "hintText", subtitle);
            SetField(flow, "conditionCard", condBox.gameObject);
            SetField(flow, "conditionCardText", condText);
            SetField(flow, "completionPanel", compBox.gameObject);
            SetField(flow, "replayButton", replayBtn);
            SetField(flow, "returnToLabButton", labBtn);
            SetField(flow, "voltageHighlight", vHl);
            SetField(flow, "currentHighlight", iHl);
            SetField(flow, "wireHighlight", rHl);

            EditorSceneManager.SaveScene(scene, Root + "Scenes/Scene_13_OhmsLaw.unity");
            Debug.Log("Scene_13_OhmsLaw saved.");
        }

        public static void ConnectScene11()
        {
            string p11 = Root + "Scenes/Scene_11_TurnDataIntoGraph.unity";
            if (!File.Exists(p11)) return;
            var scene = EditorSceneManager.OpenScene(p11);
            var flow08 = UnityEngine.Object.FindFirstObjectByType<Scene08FlowManager>();
            if (flow08 != null)
            {
                var trans = flow08.GetComponent<Scene11To12Transition>();
                if (trans == null)
                    trans = flow08.gameObject.AddComponent<Scene11To12Transition>();

                var fade = flow08.transform.root.GetComponentsInChildren<CanvasGroup>(true).FirstOrDefault(cg => cg.name == "CinematicFade");
                trans.Configure(flow08, fade);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Scene 11 hooked to transition into Scene 12.");
            }
        }

        public static void UpdateBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            string s12 = Root + "Scenes/Scene_12_DiscoverResistance.unity";
            string s13 = Root + "Scenes/Scene_13_OhmsLaw.unity";

            if (!scenes.Any(s => s.path == s12))
                scenes.Add(new EditorBuildSettingsScene(s12, true));
            if (!scenes.Any(s => s.path == s13))
                scenes.Add(new EditorBuildSettingsScene(s13, true));

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Updated EditorBuildSettings with Scenes 12 and 13.");
        }

        // Helpers
        static RectTransform CreateUIBox(string name, Transform parent, Vector2 min, Vector2 max, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (color.a > 0.001f || sprite != null)
            {
                var img = go.AddComponent<Image>();
                img.color = color;
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.type = Image.Type.Sliced;
                }
            }
            return rt;
        }

        static Text CreateLabel(string name, Transform parent, string text, int size, Vector2 min, Vector2 max, Font font)
        {
            var box = CreateUIBox(name, parent, min, max, Color.clear, null);
            var t = box.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.raycastTarget = false;
            return t;
        }

        static Button CreateButton(string name, Transform parent, string text, Vector2 min, Vector2 max, Font font, Sprite sprite)
        {
            var box = CreateUIBox(name, parent, min, max, new Color(.015f, .10f, .15f, .97f), sprite);
            var btn = box.gameObject.AddComponent<Button>();
            if (sprite != null)
            {
                var outline = box.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.05f, .83f, .95f, .8f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            var lbl = CreateLabel(name + "_Text", box.transform, text, 18, Vector2.zero, Vector2.one, font);
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.fontStyle = FontStyle.Bold;
            return btn;
        }

        static GameObject CreateWorldHighlight(string name, Vector3 pos, string labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.01f;
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(200, 50);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.03f, 0.85f, 0.95f, 0.35f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var lbl = CreateLabel("HlLabel", go.transform, labelText, 20, Vector2.zero, Vector2.one, font);
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = Color.white;
            go.SetActive(false);
            return go;
        }

        static void CreateWireLead(string name, Transform parent, Vector3 a, Vector3 b, Material mat)
        {
            var go = new GameObject(name, typeof(LineRenderer));
            go.transform.SetParent(parent, false);
            var lr = go.GetComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.startWidth = lr.endWidth = 0.045f;
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;

            Vector3 mid = (a + b) * 0.5f + Vector3.down * 0.15f;
            lr.positionCount = 3;
            lr.SetPosition(0, a);
            lr.SetPosition(1, mid);
            lr.SetPosition(2, b);
        }

        static RectTransform CreateChatCard(Transform canvas, Font font, Sprite sprite)
        {
            var panel = CreateUIBox("ChatCard", canvas, new Vector2(.035f, .055f), new Vector2(.67f, .89f), new Color(.025f, .07f, .095f, .99f), sprite);
            CreateLabel("ChatTitle", panel.transform, "Talk to Wattson", 30, new Vector2(.04f, .90f), new Vector2(.7f, .98f), font);
            CreateButton("CloseChat", panel.transform, "CLOSE", new Vector2(.78f, .90f), new Vector2(.96f, .98f), font, sprite);
            CreateLabel("ChatContext", panel.transform, "Lesson companion", 18, new Vector2(.04f, .83f), new Vector2(.96f, .89f), font);

            var scrollArea = CreateUIBox("ConversationScroll", panel.transform, new Vector2(.04f, .39f), new Vector2(.96f, .815f), new Color(.02f, .055f, .07f), null);
            scrollArea.gameObject.AddComponent<RectMask2D>();
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = scrollArea;
            scroll.horizontal = false;

            var content = CreateUIBox("Content", scrollArea.transform, new Vector2(0, 1), Vector2.one, Color.clear, null);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.spacing = 16;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            CreateLabel("Reply", content.transform, "", 22, Vector2.zero, Vector2.one, font);

            var quick = CreateUIBox("QuickQuestions", panel.transform, new Vector2(.04f, .27f), new Vector2(.96f, .36f), Color.clear, null);
            CreateButton("QuickHelp", quick.transform, "What should I do next?", Vector2.zero, new Vector2(.48f, 1), font, sprite);
            CreateButton("QuickConcept", quick.transform, "What is resistance?", new Vector2(.52f, 0), Vector2.one, font, sprite);

            var inputArea = CreateUIBox("MessageInput", panel.transform, new Vector2(.245f, .055f), new Vector2(.755f, .18f), new Color(.07f, .14f, .18f), sprite);
            var input = inputArea.gameObject.AddComponent<InputField>();
            input.characterLimit = 1200;
            input.textComponent = CreateLabel("InputText", inputArea.transform, "", 22, new Vector2(.025f, .05f), new Vector2(.975f, .95f), font);
            input.placeholder = CreateLabel("Placeholder", inputArea.transform, "Ask your question...", 22, new Vector2(.025f, .05f), new Vector2(.975f, .95f), font);

            CreateButton("Send", panel.transform, "SEND", new Vector2(.78f, .055f), new Vector2(.96f, .18f), font, sprite);
            CreateButton("VoiceInput", panel.transform, "MIC", new Vector2(.04f, .055f), new Vector2(.225f, .18f), font, sprite);
            CreateLabel("VoiceStatus", panel.transform, "Hold the microphone to talk", 16, new Vector2(.04f, .005f), new Vector2(.96f, .05f), font);

            panel.gameObject.SetActive(false);
            return panel;
        }

        static void ConfigureChatUI(WattsonChatUI ui, RectTransform panel, Button askBtn, WattsonController companion)
        {
            var close = panel.Find("CloseChat")?.GetComponent<Button>();
            var send = panel.Find("Send")?.GetComponent<Button>();
            var mic = panel.Find("VoiceInput")?.GetComponent<Button>();
            var status = panel.Find("VoiceStatus")?.GetComponent<Text>();
            var input = panel.Find("MessageInput")?.GetComponent<InputField>();
            var quick = panel.Find("QuickQuestions");
            var q1 = quick?.Find("QuickHelp")?.GetComponent<Button>();
            var q2 = quick?.Find("QuickConcept")?.GetComponent<Button>();
            var context = panel.Find("ChatContext")?.GetComponent<Text>();
            var reply = panel.Find("ConversationScroll/Content/Reply")?.GetComponent<Text>();
            var scroll = panel.Find("ConversationScroll")?.GetComponent<ScrollRect>();

            ui.Configure(panel.gameObject, askBtn, close, send, new[] { q1, q2 }, input, reply, context);
            ui.ConfigureConversation(scroll, companion);
            ui.ConfigureVoice(mic, status);
        }

        static EquationAssemblyView.SlotUI CreateSlot(string name, Transform parent, Vector2 min, Vector2 max, Font font, Sprite sprite)
        {
            var box = CreateUIBox(name, parent, min, max, new Color(.02f, .08f, .12f, .95f), sprite);
            var lbl = CreateLabel(name + "_Label", box.transform, "?", 32, Vector2.zero, Vector2.one, font);
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = new Color(.03f, .85f, .95f);
            return new EquationAssemblyView.SlotUI
            {
                rect = box,
                slotLabel = lbl,
                background = box.GetComponent<Image>()
            };
        }

        static TokenUI CreateToken(string name, Transform parent, EquationAssemblyView.TokenType type, string text, Vector2 min, Vector2 max, Font font, Sprite sprite)
        {
            var box = CreateUIBox(name, parent, min, max, new Color(.06f, .22f, .28f, .98f), sprite);
            var lbl = CreateLabel(name + "_Label", box.transform, text, 30, Vector2.zero, Vector2.one, font);
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = Color.white;

            var btn = box.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.2f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.1f, 0.6f, 0.8f, 1f);
            btn.colors = colors;

            var tok = box.gameObject.AddComponent<TokenUI>();
            tok.tokenType = type;
            tok.label = lbl;
            tok.background = box.GetComponent<Image>();
            btn.onClick.AddListener(() => tok.OnPointerClick(new PointerEventData(EventSystem.current)));
            return tok;
        }

        static void SetField(object target, string name, object val)
        {
            if (target == null) return;
            var f = target.GetType().GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null)
            {
                f.SetValue(target, val);
                if (target is UnityEngine.Object uo)
                    EditorUtility.SetDirty(uo);
            }
        }
    }
}
