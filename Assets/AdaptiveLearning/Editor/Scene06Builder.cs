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
    public static class Scene06Builder
    {
        public const string Root = "Assets/AdaptiveLearning";
        public const string ScenePath = Root + "/Scenes/Scene_06_AdaptivePrediction.unity";
        static readonly Color Ink = new Color(.025f,.055f,.085f,.96f);
        static readonly Color Cyan = new Color(.25f,.91f,.91f);
        static Font font;
        static Scene06Builder() { EditorApplication.delayCall += FirstBuild; }
        static void FirstBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath)) return;
            Build();
        }
        [MenuItem("Adaptive Learning/Open Scene 06")]
        public static void Open()
        {
            if (!File.Exists(ScenePath)) Build();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        static T Asset<T>(string path, Action<T> configure) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path); if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>(); configure(asset); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        public static void Build()
        {
            foreach (var folder in new[] { "Scenes", "Materials", "Prefabs/Wattson", "Prefabs/UI", "ScriptableObjects/Questions", "ScriptableObjects/Dialogue", "ScriptableObjects/Concepts", "Audio", "Art" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh(); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rules = Asset<AdaptiveRules>(Root + "/ScriptableObjects/Concepts/PrototypeRules.asset", a => {});
            var question = Asset<QuestionDefinition>(Root + "/ScriptableObjects/Questions/Scene06Prediction.asset", a => {
                a.questionId = "S06_VoltageCurrentPrediction";
                a.concepts = new[] { ConceptId.Voltage, ConceptId.Current, ConceptId.VoltageCurrentRelationship };
                a.questionText = "If we increase the potential difference while everything else stays the same, what do you predict will happen to the current?";
                a.answers = new[] { "Increase", "Decrease", "Stay the same" }; a.correctAnswer = 0;
                a.hints = new[] { "Watch the source change from 1 V to 2 V. Look for a change in the gentle net drift of the charges.",
                    "The wire and its resistance stay the same. A greater electrical push produces more directed charge flow each second.",
                    "One step at a time: compare 1 V with 2 V, then choose the prediction that matches the greater charge flow." };
            });
            var script = Asset<DialogueSequence>(Root + "/ScriptableObjects/Dialogue/Scene06Dialogue.asset", a => {
                a.lines = new[] {
                    Line("detective", "Detective test.", 2.4f),
                    Line("question", question.questionText, 7),
                    Line("accepted", "Interesting. Remember your answer. Let's see if Ohm's experiment agrees with you.", 5),
                    Line("curious", "Interesting prediction.", 1.8f),
                    Line("retry", "Watch the charges carefully. Do you want to keep your answer or try again?", 5),
                    Line("supported", "Same wire. Greater electrical push. Notice the stronger net drift. You can try another prediction or keep yours for the experiment.", 7),
                    Line("keep", "Let's keep your prediction. Experiments help us test our ideas, and we can revise them as we learn.", 6),
                    Line("complete", "Prediction recorded. Next, we'll test it in Ohm's experiment. Our next learning stage will begin here.", 6) };
            });
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var environment = Go("Environment");
            var metal = Material("Conductor", new Color(.045f,.12f,.17f), false);
            var teal = Material("Charge", Cyan, true);
            var ringMat = Material("ConductorRibs", new Color(.08f,.31f,.36f), true);
            var white = Material("WattsonShell", new Color(.8f,.88f,.91f), false);
            var dark = Material("WattsonFace", new Color(.014f,.038f,.06f), false);
            var gold = Material("SourceAmber", new Color(1,.61f,.2f), true);
            var conductor = Go("Conductor", environment.transform);
            // Faceted open tunnel, viewed from inside. Opaque surfaces keep tablet overdraw low.
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2 / 16;
                var segment = Shape("Wall", PrimitiveType.Cube, conductor.transform,
                    new Vector3(Mathf.Cos(angle)*7, Mathf.Sin(angle)*7, 12), new Vector3(2.85f,.22f,48), metal);
                segment.transform.localRotation = Quaternion.Euler(0,0,angle*Mathf.Rad2Deg-90);
            }
            for (int z = -4; z < 34; z += 6)
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI*2/16;
                    var rib = Shape("Rib", PrimitiveType.Cube, conductor.transform,
                        new Vector3(Mathf.Cos(angle)*6.85f,Mathf.Sin(angle)*6.85f,z), new Vector3(2.7f,.055f,.06f), ringMat);
                    rib.transform.localRotation = Quaternion.Euler(0,0,angle*Mathf.Rad2Deg-90);
                }
            var chargeRoot = Go("ChargeParticles", environment.transform);
            var random = new System.Random(6006); var particles = new Transform[64];
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i] = Shape("Charge_" + i, PrimitiveType.Sphere, chargeRoot.transform,
                    new Vector3((float)random.NextDouble()*12-6, (float)random.NextDouble()*5-2.5f, (float)random.NextDouble()*20+2), Vector3.one*.09f, teal).transform;
            }
            var drift = chargeRoot.AddComponent<ChargeDriftVisualization>(); drift.Configure(particles);
            var source = Go("BatteryVisualization", environment.transform);
            Shape("SourceBody", PrimitiveType.Cube, source.transform, new Vector3(-2.2f,.3f,5), new Vector3(1.55f,.85f,.45f), white);
            Shape("PositiveTerminal", PrimitiveType.Cube, source.transform, new Vector3(-1.35f,.3f,5), new Vector3(.22f,.38f,.3f), gold);
            Shape("SourceBand", PrimitiveType.Cube, source.transform, new Vector3(-2.65f,.3f,4.74f), new Vector3(.32f,.86f,.02f), gold);
            var highlight = Shape("VoltageHighlight", PrimitiveType.Cube, source.transform, new Vector3(-2.2f,-.28f,5), new Vector3(2.1f,.055f,.55f), gold);
            var battery = source.AddComponent<VoltageVisualization>();
            var lighting = Go("Lighting", environment.transform);
            var light = Go("SoftKey", lighting.transform).AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(35,-25,0); light.shadows = LightShadows.None;
            RenderSettings.ambientLight = new Color(.34f,.43f,.5f); RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.012f,.035f,.055f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .027f;
            var camera = Go("LearnerCamera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0,.6f,-8); camera.fieldOfView = 48; camera.nearClipPlane = .1f; camera.farClipPlane = 70;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = RenderSettings.fogColor;
            camera.gameObject.AddComponent<AudioListener>();
            var characters = Go("Characters"); var companion = Go("Wattson", characters.transform);
            companion.transform.position = new Vector3(2,.7f,3); companion.transform.rotation = Quaternion.Euler(0,110,0);
            var visual = Go("Visual", companion.transform);
            Shape("Body", PrimitiveType.Capsule, visual.transform, Vector3.zero, new Vector3(.85f,.65f,.65f), white);
            Shape("Head", PrimitiveType.Sphere, visual.transform, new Vector3(0,.85f,0), new Vector3(1.05f,.85f,.85f), white);
            Shape("Face", PrimitiveType.Sphere, visual.transform, new Vector3(0,.87f,.32f), new Vector3(.82f,.49f,.27f), dark);
            Shape("EyeLeft", PrimitiveType.Sphere, visual.transform, new Vector3(-.2f,.92f,.455f), new Vector3(.105f,.16f,.035f), teal);
            Shape("EyeRight", PrimitiveType.Sphere, visual.transform, new Vector3(.2f,.92f,.455f), new Vector3(.105f,.16f,.035f), teal);
            Shape("Core", PrimitiveType.Sphere, visual.transform, new Vector3(0,.1f,.32f), new Vector3(.26f,.26f,.07f), teal);
            Shape("ArmLeft", PrimitiveType.Capsule, visual.transform, new Vector3(-.61f,.02f,0), new Vector3(.17f,.32f,.17f), white);
            Shape("ArmRight", PrimitiveType.Capsule, visual.transform, new Vector3(.61f,.02f,0), new Vector3(.17f,.32f,.17f), white);
            var animation = Go("Animator", companion.transform).AddComponent<Animator>();
            var target = Go("LookTarget", companion.transform); var anchor = Go("DialogueAnchor", companion.transform); anchor.transform.localPosition = Vector3.up*1.6f;
            var trigger = Go("InteractionTrigger", companion.transform).AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = 1.5f;
            var voice = Go("AudioSource", companion.transform).AddComponent<AudioSource>(); voice.playOnAwake = false;
            var wattson = companion.AddComponent<WattsonController>(); wattson.Configure(visual.transform,target.transform,anchor.transform,animation,voice);
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(companion, Root + "/Prefabs/Wattson/Wattson.prefab", InteractionMode.AutomatedAction);
            var audio = Go("Audio"); var sfx = Go("SFX",audio.transform).AddComponent<AudioSource>(); sfx.playOnAwake = false;
            var ambient = Go("Ambient",audio.transform).AddComponent<AudioSource>(); ambient.playOnAwake = false; ambient.loop = true; ambient.volume = .15f;
            var systems = Go("LearningSystems");
            var profiles = Go("StudentProfileManager",systems.transform).AddComponent<StudentProfileManager>();
            var learning = Go("AdaptiveLearningManager",systems.transform).AddComponent<AdaptiveLearningManager>(); learning.Configure(rules,profiles);
            var questions = Go("QuestionManager",systems.transform).AddComponent<QuestionManager>();
            var chat = Go("WattsonChatManager",systems.transform).AddComponent<WattsonChatManager>();
            var flow = Go("SceneFlowManager",systems.transform).AddComponent<SceneFlowManager>();
            var canvas = Go("UI", null, typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            var safe = Rect("SafeArea",canvas.transform,0,0,1,1); safe.gameObject.AddComponent<SafeAreaLayout>();
            Label("Lesson",safe,"ADAPTIVE OHM'S LAW   /   06",26,Cyan,.04f,.9f,.66f,.96f);
            Label("Objective",safe,"Predict. Observe. Investigate.",21,Color.white,.04f,.85f,.6f,.91f);
            var ask = Button("AskWattson",safe,"ASK WATTSON",.77f,.875f,.96f,.96f);
            var voltageText = Label("Voltage",safe,"POTENTIAL DIFFERENCE    0 V",24,new Color(1,.73f,.38f),.07f,.57f,.57f,.63f);
            Label("Condition",safe,"SAME WIRE  •  RESISTANCE HELD CONSTANT",18,Color.white,.07f,.53f,.61f,.57f);
            Label("ModelNote",safe,"Electron motion enlarged and slowed for learning.\nNet electron drift →   |   Conventional current ←",17,new Color(.59f,.76f,.8f),.07f,.455f,.65f,.525f);
            battery.Configure(drift,voltageText,highlight.transform,sfx);
            var hint = Label("HintUI",safe,"",21,Cyan,.07f,.365f,.91f,.45f);
            var dialoguePanel = Panel("DialoguePanel",safe,.04f,.215f,.96f,.355f);
            var speaker = Label("Speaker",dialoguePanel,"WATTSON",19,Cyan,.025f,.72f,.95f,.95f);
            var subtitle = Label("Subtitle",dialoguePanel,"",27,Color.white,.025f,.08f,.975f,.73f);
            var presenter = dialoguePanel.gameObject.AddComponent<DialoguePresenter>(); presenter.Configure(speaker,subtitle,voice);
            var questionPanel = Panel("QuestionPanel",safe,.04f,.065f,.96f,.205f);
            var prompt = Label("AccessibleQuestion",questionPanel,question.questionText,22,Color.white,0,0,1,1); prompt.gameObject.SetActive(false);
            var options = Rect("Answers",questionPanel,.02f,.13f,.98f,.88f);
            var layout = options.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 18; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            var buttons = new Button[3];
            for (int i = 0; i < 3; i++) buttons[i] = Button("Answer"+(char)('A'+i),options,((char)('A'+i))+"   "+question.answers[i],0,0,1,1);
            var decisions = Rect("PredictionDecision",questionPanel,.02f,.13f,.98f,.88f);
            var decisionLayout = decisions.gameObject.AddComponent<HorizontalLayoutGroup>(); decisionLayout.spacing = 18;
            decisionLayout.childControlWidth = decisionLayout.childControlHeight = true; decisionLayout.childForceExpandWidth = decisionLayout.childForceExpandHeight = true;
            var retry = Button("TryAgain",decisions,"TRY AGAIN",0,0,1,1);
            var keep = Button("KeepPrediction",decisions,"KEEP MY PREDICTION",0,0,1,1);
            decisions.gameObject.SetActive(false);
            var view = questionPanel.gameObject.AddComponent<QuestionUI>(); view.Configure(prompt,buttons,options.gameObject,decisions.gameObject,retry,keep);
            var status = Label("Support",safe,"",17,Cyan,.04f,.013f,.49f,.055f);
            var progress = Label("LearningProgress",safe,"",16,new Color(.62f,.75f,.81f),.49f,.013f,.96f,.055f); progress.alignment = TextAnchor.MiddleRight;
            var end = Panel("Completion",safe,.04f,.065f,.96f,.205f);
            Label("CompletionText",end,"Your prediction is ready for the experiment.",25,Color.white,.025f,.12f,.65f,.88f);
            var next = Button("Continue",end,"CONTINUE",.68f,.15f,.975f,.85f); end.gameObject.SetActive(false);
            var chatBlocker = Panel("WattsonChatPanel",safe,0,0,1,1); chatBlocker.GetComponent<Image>().color = new Color(0,0,0,.6f);
            var chatPanel = Panel("ChatCard",chatBlocker,.12f,.13f,.88f,.86f);
            Label("ChatTitle",chatPanel,"ASK WATTSON",30,Cyan,.04f,.865f,.7f,.95f);
            var close = Button("CloseChat",chatPanel,"CLOSE",.77f,.86f,.96f,.96f);
            var context = Label("Context",chatPanel,"SCENE 06 / PREDICTION  •  LOCAL LESSON GUIDE",18,new Color(.6f,.76f,.82f),.04f,.77f,.96f,.85f);
            var reply = Label("Reply",chatPanel,"What would you like to explore? Asking for help never lowers your mastery.",27,Color.white,.04f,.38f,.96f,.76f);
            var quick = Rect("QuickQuestions",chatPanel,.04f,.25f,.96f,.36f);
            var quickLayout = quick.gameObject.AddComponent<HorizontalLayoutGroup>(); quickLayout.spacing = 14; quickLayout.childControlWidth = quickLayout.childControlHeight = true;
            quickLayout.childForceExpandWidth = quickLayout.childForceExpandHeight = true;
            var suggestions = new[] { Button("VoltageHelp",quick,"What is voltage?",0,0,1,1),Button("CurrentHelp",quick,"What is current?",0,0,1,1),Button("Simplify",quick,"I don't understand.",0,0,1,1) };
            foreach (var b in suggestions) b.GetComponentInChildren<Text>().fontSize = 22;
            var entry = Panel("MessageInput",chatPanel,.04f,.08f,.74f,.21f); entry.GetComponent<Image>().color = new Color(.08f,.15f,.2f);
            var input = entry.gameObject.AddComponent<InputField>(); input.targetGraphic = entry.GetComponent<Image>(); input.characterLimit = 240;
            var inputText = Label("InputText",entry,"",25,Color.white,.025f,.07f,.97f,.93f); inputText.supportRichText = false;
            var placeholder = Label("Placeholder",entry,"Ask about this activity…",24,new Color(.6f,.72f,.76f),.025f,.07f,.97f,.93f);
            input.textComponent = inputText; input.placeholder = placeholder;
            var send = Button("Send",chatPanel,"ASK",.77f,.08f,.96f,.21f);
            var chatUI = canvas.AddComponent<WattsonChatUI>(); chatUI.Configure(chatBlocker.gameObject,ask,close,send,suggestions,input,reply,context);
            chatBlocker.gameObject.SetActive(false);
            var curtain = Panel("CinematicFade",canvas.transform,0,0,1,1); curtain.GetComponent<Image>().color = Color.black;
            var fade = curtain.gameObject.AddComponent<CanvasGroup>(); fade.alpha = 0; fade.blocksRaycasts = false;
            Go("EventSystem",null,typeof(EventSystem),typeof(InputSystemUIInputModule));
            flow.Configure(rules,question,script,profiles,learning,questions,wattson,chat,chatUI,presenter,view,battery,camera,fade,status,hint,progress,end.gameObject,next);
            PrefabUtility.SaveAsPrefabAsset(questionPanel.gameObject,Root + "/Prefabs/UI/QuestionPanel.prefab");
            EditorSceneManager.SaveScene(scene,ScenePath);
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(s => s.path == ScenePath)) buildScenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
            AssetDatabase.SaveAssets();
            if (previous.IsValid()) { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
            File.WriteAllText("Logs/Scene06-build.txt","Scene 06 created successfully at " + DateTime.Now);
        }
        static DialogueLine Line(string id,string text,float duration) => new DialogueLine { id = id,text = text,duration = duration };
        static GameObject Go(string name, Transform parent = null, params Type[] types)
        { var go = new GameObject(name, types); if (parent != null) go.transform.SetParent(parent,false); return go; }
        static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent,false); go.transform.localPosition = position; go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static Material Material(string name,Color color,bool unlit)
        {
            string path = Root + "/Materials/" + name + ".mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(path); if (mat != null) return mat;
            mat = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor",color); mat.enableInstancing = true; AssetDatabase.CreateAsset(mat,path); return mat;
        }
        static RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
        {
            var rt = (RectTransform)Go(name,parent,typeof(RectTransform)).transform; rt.anchorMin = new Vector2(x0,y0); rt.anchorMax = new Vector2(x1,y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero; return rt;
        }
        static RectTransform Panel(string name,Transform parent,float x0,float y0,float x1,float y1)
        { var rt = Rect(name,parent,x0,y0,x1,y1); rt.gameObject.AddComponent<Image>().color = Ink; return rt; }
        static Text Label(string name,Transform parent,string text,int size,Color color,float x0,float y0,float x1,float y1)
        {
            var rt = Rect(name,parent,x0,y0,x1,y1); var label = rt.gameObject.AddComponent<Text>(); label.font = font; label.text = text; label.fontSize = size;
            label.color = color; label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; return label;
        }
        static Button Button(string name,Transform parent,string text,float x0,float y0,float x1,float y1)
        {
            var rt = Panel(name,parent,x0,y0,x1,y1); var graphic = rt.GetComponent<Image>(); graphic.color = new Color(.08f,.24f,.29f);
            var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
            var colors = button.colors; colors.highlightedColor = new Color(.65f,1,1); colors.pressedColor = new Color(.35f,.8f,.85f); colors.selectedColor = new Color(.65f,1,1); button.colors = colors;
            var label = Label("Label",rt,text,27,Color.white,.035f,.05f,.965f,.95f); label.alignment = TextAnchor.MiddleCenter;
            var element = rt.gameObject.AddComponent<LayoutElement>(); element.flexibleWidth = 1; element.minHeight = 72;
            return button;
        }
    }
}
