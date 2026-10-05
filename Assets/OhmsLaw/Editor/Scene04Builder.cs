using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace OhmsLaw.Editor
{
    // Builds Scene 4 from code: assets, prefab variants, scene objects, cameras, Timeline, lighting, post and UI.
    // Safe to run again at any time; recorded VO on the dialogue assets is kept and re-times the Timeline.
    [InitializeOnLoad]
    public static partial class Scene04Builder
    {
        public const string Root = "Assets/OhmsLaw", Shared = Root + "/Shared", S4 = Root + "/Scene04";
        public const string ScenePath = Root + "/Scenes/Scene04_MeetWattson.unity";
        const string Art = "Assets/AdaptiveLearning/Art";
        const string LabModel = Art + "/env/Environment.fbx";
        const string OhmModel = Art + "/GEORGE OHM/GEORGE OHM/GEORGE OHM.fbx";
        const string WattsonPrefab = "Assets/AdaptiveLearning/Prefabs/Wattson/Wattson.prefab";
        const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        // ---- Stage layout, in the lab model's own coordinates (floor y = -1.08, desk top y = -0.26) ----
        static readonly Vector3 Group = new Vector3(-0.2f, 0.27f, 0.3f);       // eye point of the arriving group (camera POV)
        static readonly Vector3 Hover = new Vector3(-1.5f, 0.2f, -0.45f);      // where Wattson settles
        static readonly Vector3 OhmFeet = new Vector3(-2.4f, -1.08f, -2.36f);
        static readonly Vector3 OhmHead = new Vector3(-2.4f, 0.45f, -2.2f);
        static readonly Vector3 Wire = new Vector3(-2.71f, -0.18f, -1.9f);       // = SceneBuilderBase.Wire4: the 1826 apparatus's test wire
        static readonly Vector3 WordsCamera = new Vector3(0.06f, 0.3f, 0.45f);
        const float FloorY = -1.08f, DeskY = -0.26f;

        static ModulePalette palette;
        static Scene scene;

        static Scene04Builder() { EditorApplication.update += CheckRequests; }

        // Same request-file convention as the project's SceneCommandWatcher, so an open editor can be driven from outside.
        static void CheckRequests()
        {
            if (EditorApplication.isPlaying && File.Exists("Logs/scene04-stop.req")) { File.Delete("Logs/scene04-stop.req"); EditorApplication.ExitPlaymode(); return; }
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists("Logs/scene04-play.req")) { File.Delete("Logs/scene04-play.req"); Scene04Smoke.Run(); return; }
            if (!File.Exists("Logs/scene04-build.req")) return;
            if (!EnsureTextMeshPro()) return;
            File.Delete("Logs/scene04-build.req");
            try { Build(); File.WriteAllText("Logs/scene04-build.txt", "OK " + DateTime.Now); }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Logs/scene04-build.txt", "ERROR " + e); }
        }

        static bool importingTmp;
        static bool EnsureTextMeshPro()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return true;
            if (!importingTmp) { importingTmp = true; AssetDatabase.ImportPackage(TmpEssentials, false); }
            return false;
        }

        // One-time batchmode step. The package import finishes on a later editor tick, so this exits by itself.
        public static void ImportTextMeshPro()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) { EditorApplication.Exit(0); return; }
            AssetDatabase.importPackageCompleted += _ => EditorApplication.Exit(0);
            AssetDatabase.importPackageFailed += (_, error) => { Debug.LogError(error); EditorApplication.Exit(1); };
            AssetDatabase.ImportPackage(TmpEssentials, false);
        }

        [MenuItem("Ohm's Law/Build Scene 04")]
        public static void BuildFromMenu()
        {
            if (!EnsureTextMeshPro()) { Debug.Log("Importing TextMeshPro essentials first. Run Build Scene 04 again when the import finishes."); return; }
            Build();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        // Batchmode entry: -executeMethod OhmsLaw.Editor.Scene04Builder.Build
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
                throw new InvalidOperationException("TextMeshPro essentials are not imported. In batchmode run -executeMethod OhmsLaw.Editor.Scene04Builder.ImportTextMeshPro once (without -quit); in the Editor the menu item imports them for you.");
            foreach (var path in new[] { LabModel, OhmModel, WattsonPrefab })
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(path)) throw new FileNotFoundException("Scene 4 needs this project model: " + path);

            foreach (var folder in new[] { "Scenes", "Shared/Agent", "Shared/UI", "Scene04/Prefabs", "Scene04/Timeline", "Scene04/Dialogue", "Scene04/Agent", "Scene04/VFX", "Scene04/Materials", "Scene04/Audio/VO", "Scene04/Audio/SFX" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();

            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly; // http://localhost proxy in the Editor only
            PlayerSettings.iOS.microphoneUsageDescription = "Wattson listens when you hold the microphone button to ask a question.";

            palette = BuildPalette();
            var lines = BuildDialogue();
            var context = BuildAgentAssets();
            var sfx = BuildSfx();

            // Build beside whatever scene is open, so nobody's unsaved work is touched.
            var previous = SceneManager.GetActiveScene();
            scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.isLoaded) foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
            else if (!string.IsNullOrEmpty(previous.path)) scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            else if (previous.isDirty) throw new InvalidOperationException("Save or discard your untitled scene, then build Scene 04 again.");
            else scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // batchmode starts on an empty untitled scene
            SceneManager.SetActiveScene(scene);

            var stage = BuildStage(context, sfx);
            BuildLighting(stage);
            BuildVfx(stage);
            BuildCameras(stage);
            BuildTimeline(stage, lines, sfx);
            BuildModuleRoot();

            // Bake the room's reflections into a cubemap so brass and copper have something to reflect.
            const string reflections = S4 + "/VFX/Scene04_Reflection.exr";
            if (Lightmapping.BakeReflectionProbe(stage.probe, reflections)) stage.probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(reflections);
            else Debug.LogWarning("Scene 04: the reflection probe did not bake. Use Window > Rendering > Lighting > Generate Lighting.");

            EditorSceneManager.SaveScene(scene, ScenePath);
            // Shot previews (Logs/Scene04) are a batchmode extra; the Ask button portrait is made whenever it is missing.
            if (Application.isBatchMode || !palette.wattsonFace)
            {
                RenderPreviews(stage);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            AddToBuildSettings();
            AssetDatabase.SaveAssets();

            if (previous.IsValid() && previous != scene)
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            Debug.Log("Scene 04 built: " + ScenePath + "  (" + stage.total.ToString("0.0") + " s)");
        }

        // Everything the later build steps need to refer to.
        class Stage
        {
            public GameObject lab, vfx;
            public WattsonController wattson;
            public Niscope niscope;
            public OhmWorker ohm;
            public Transform groupPoint, ohmPoint, wirePoint, wordsPoint, words, voltage, current, wire;
            public Scene04Director director04;
            public PlayableDirector director;
            public SignalReceiver receiver;
            public Camera camera;
            public AudioSource sfx, music;
            public SplineContainer path;
            public ReflectionProbe probe;
            public Dictionary<string, CinemachineCamera> cams = new Dictionary<string, CinemachineCamera>();
            public Dictionary<string, GameObject> fx = new Dictionary<string, GameObject>();
            public float total;
        }

        // ------------------------------------------------------------------ assets

        static T Asset<T>(string path, Func<T> create) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static T Data<T>(string path) where T : ScriptableObject => Asset(path, ScriptableObject.CreateInstance<T>);

        static ModulePalette BuildPalette()
        {
            var p = Data<ModulePalette>(Shared + "/ModulePalette.asset");
            p.roundedPanel = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Interface/RoundedPanel.png");
            p.circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            const string fontPath = Shared + "/UI/Nunito SDF.asset";
            p.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (!p.font)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(Art + "/Interface/Fonts/NunitoSans-Bold.ttf");
                p.font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                AssetDatabase.CreateAsset(p.font, fontPath);
                p.font.material.name = "Nunito SDF Material";
                p.font.atlasTexture.name = "Nunito SDF Atlas";
                AssetDatabase.AddObjectToAsset(p.font.material, p.font);
                AssetDatabase.AddObjectToAsset(p.font.atlasTexture, p.font);
                p.font.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~…–—’‘“”°Ω▶");
                // Written to disk now: a later refresh would otherwise reload the asset without its atlas and material.
                EditorUtility.SetDirty(p.font);
                AssetDatabase.SaveAssets();
            }
            EditorUtility.SetDirty(p.font);
            EditorUtility.SetDirty(p);
            return p;
        }

        static DialogueLine Line(string folder, string id, string text, float fallback)
        {
            var line = Data<DialogueLine>(folder + "/" + id + ".asset");
            line.id = id;
            line.speaker = "WATTSON";
            line.text = text;
            line.ttsText = text;
            line.fallbackDuration = fallback;
            // The best clip in Scene 4's Audio/VO, as every scene picks it (BlockoutAssets.FindVo): a baked <id>.mp3, then a
            // voice actor's <id>.ogg/.wav, then the local stand-in scratch_<id>.wav, so a recreated line is never silent.
            line.vo = BlockoutAssets.FindVo(S4, id);
            EditorUtility.SetDirty(line);
            return line;
        }

        static Dictionary<string, DialogueLine> BuildDialogue()
        {
            // Storyboard dialogue, word for word. Do not edit the text here.
            var script = new (string id, string text, float fallback)[]
            {
                ("S04_L01", "Welcome, detectives.", 2.0f),
                ("S04_L02", "Your Christmas lights gave you a question. Nearly two centuries ago, Georg Ohm was investigating the relationship that can help us answer it.", 8.5f),
                ("S04_L03", "He wanted to know how these two are connected.", 3.0f),
                ("S04_L04", "But before we understand his experiment...", 2.8f),
                ("S04_L05", "...you need to see what electricity looks like from the inside.", 3.8f),
                ("S04_L06", "Everybody in.", 1.4f),
            };
            return script.ToDictionary(l => l.id, l => Line(S4 + "/Dialogue", l.id, l.text, l.fallback));
        }

        static SceneAgentContext BuildAgentAssets()
        {
            var proxy = Data<ProxyConfig>(Shared + "/Agent/ProxyConfig.asset");
            proxy.offlineLine = Line(Shared + "/Agent", "Agent_Offline", "My detective radio is fuzzy right now. Try one of these clues!", 4.2f);
            EditorUtility.SetDirty(proxy);

            // Offline answers for the three suggested questions. They stay inside what Scene 4 has covered.
            var faq = Data<LocalFAQ>(S4 + "/Agent/Scene04_FAQ.asset");
            faq.entries = new[]
            {
                new LocalFAQ.Entry { question = "Who was Georg Ohm?", answer = Line(S4 + "/Dialogue", "S04_FAQ_Ohm",
                    "Georg Ohm was a German teacher and scientist, born in 1789. He loved careful experiments and built much of his own equipment to study electricity. We are standing in his lab right now, detective!", 11f) },
                new LocalFAQ.Entry { question = "What is voltage?", answer = Line(S4 + "/Dialogue", "S04_FAQ_Voltage",
                    "Voltage, also called potential difference, is the electrical push a battery gives to the charges in a circuit. We measure it in volts. Keep that clue handy, we are about to see it in action!", 11f) },
                new LocalFAQ.Entry { question = "What is an Ohm-niscope?", answer = Line(S4 + "/Dialogue", "S04_FAQ_Niscope",
                    "It's my detective gadget! The Ohm-niscope lets us zoom right inside a wire to see what electricity is really doing in there. Stick with me and you'll get to look through it.", 10f) },
            };
            EditorUtility.SetDirty(faq);

            var context = Data<SceneAgentContext>(S4 + "/Agent/Scene04_Context.asset");
            context.sceneId = "Scene04";
            context.sceneTitle = "Meet Wattson & Georg Ohm";
            context.askWattsonEnabled = true;
            context.covered = new[]
            {
                "The Christmas-lights mystery: the lights were dim, and a new battery made them bright.",
                "Georg Ohm investigated the relationship between voltage and current.",
                "The words voltage and current.",
                "The Ohm-niscope, Wattson's gadget for looking inside a wire.",
            };
            context.doNotSpoil = new[]
            {
                "V = IR", "V is proportional to I", "R = V / I",
                "What happens to current when voltage increases (the Scene 6 prediction)",
                "Resistance and ohms",
                "The final answer to the Christmas-lights mystery",
            };
            context.faq = faq;
            EditorUtility.SetDirty(context);
            return context;
        }

        // ------------------------------------------------------------------ materials and meshes

        static Material Mat(string name, string shader, Action<Material> setup)
        {
            var material = Asset(S4 + "/Materials/" + name + ".mat", () => new Material(Shader.Find(shader)));
            material.shader = Shader.Find(shader);
            setup(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Lit(string name, Color color, float metallic, float smoothness, Color? emission = null) =>
            Mat(name, "Universal Render Pipeline/Lit", m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", smoothness);
                if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
            });

        // Additive, unlit and HDR, so Bloom makes it glow. Used by particles, lines, flares and light shafts.
        static Material Glow(string name, Color hdr, bool softTexture = true) =>
            Mat(name, "Universal Render Pipeline/Particles/Unlit", m =>
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 2);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.One);
                if (m.HasProperty("_SrcBlendAlpha")) { m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.One); }
                m.SetFloat("_ZWrite", 0);
                m.SetFloat("_Cull", (float)CullMode.Off);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetColor("_BaseColor", hdr);
                m.SetTexture("_BaseMap", softTexture ? AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd") : null);
            });

        // A copy of a project material with emission switched on; the original asset is left alone.
        static Material EmissiveCopy(Material source, string name, Color emission)
        {
            string path = S4 + "/Materials/" + name + ".mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!copy) { copy = new Material(source); AssetDatabase.CreateAsset(copy, path); }
            copy.CopyPropertiesFromMaterial(source);
            copy.EnableKeyword("_EMISSION");
            copy.SetColor("_EmissionColor", emission);
            if (copy.HasProperty("_EmissionMap")) copy.SetTexture("_EmissionMap", source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null);
            copy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(copy);
            return copy;
        }

        static Color Hdr(Color color, float intensity) => new Color(color.r * intensity, color.g * intensity, color.b * intensity, color.a);

        static Mesh MeshAsset(string name, Action<Mesh> fill)
        {
            var mesh = Asset(S4 + "/VFX/" + name + ".mesh", () => new Mesh());
            mesh.Clear();
            fill(mesh);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static Mesh Torus(string name, float tube)
        {
            const int segments = 40, sides = 10;
            return MeshAsset(name, mesh =>
            {
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                for (int i = 0; i <= segments; i++)
                    for (int j = 0; j <= sides; j++)
                    {
                        float a = i * Mathf.PI * 2 / segments, b = j * Mathf.PI * 2 / sides;
                        var centre = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                        vertices.Add(centre * (1 + tube * Mathf.Cos(b)) + Vector3.forward * (tube * Mathf.Sin(b)));
                    }
                for (int i = 0; i < segments; i++)
                    for (int j = 0; j < sides; j++)
                    {
                        int v = i * (sides + 1) + j;
                        triangles.AddRange(new[] { v, v + sides + 1, v + 1, v + 1, v + sides + 1, v + sides + 2 });
                    }
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
            });
        }

        // ------------------------------------------------------------------ scene object helpers

        static GameObject Node(string name, Transform parent = null, Vector3 position = default)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            else SceneManager.MoveGameObjectToScene(go, scene);
            if (parent) go.transform.localPosition = position; // every parent used here sits at the origin or wants a local offset
            else go.transform.position = position;
            return go;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static LineRenderer Lines(string name, Transform parent, Material material, float width, bool loop, bool world, params Vector3[] points)
        {
            var line = Node(name, parent).AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.loop = loop;
            line.useWorldSpace = world;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.numCapVertices = 2;
            return line;
        }

        static Transform Find(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)
            ?? throw new MissingReferenceException(root.name + " has no child named " + name);

        static AudioSource Audio(string name, Transform parent, AudioBus.Kind kind, float gain = 1f)
        {
            var source = Node(name, parent).AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            var bus = source.gameObject.AddComponent<AudioBus>();
            bus.kind = kind;
            bus.gain = gain;
            return source;
        }

        static GameObject Variant(GameObject instance, string name)
        {
            return PrefabUtility.SaveAsPrefabAssetAndConnect(instance, S4 + "/Prefabs/" + name + ".prefab", InteractionMode.AutomatedAction);
        }

        // ------------------------------------------------------------------ stage: lab, Ohm, apparatus, Wattson

        static Stage BuildStage(SceneAgentContext context, Dictionary<string, AudioClip> sfx)
        {
            var s = new Stage();

            // Laboratory: a variant of the project's Victorian study, dressed for candlelight.
            s.lab = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LabModel), scene);
            s.lab.name = "Lab";
            foreach (var light in s.lab.GetComponentsInChildren<Light>(true)) light.gameObject.SetActive(false);
            // Props that float in mid-air in the source model, and the desk dressing that stands where Ohm's 1826 apparatus
            // goes (book piles, the open book, the magnifier, the top hat; the glasses stay). Scenes 5, 7 and 8 use this variant.
            foreach (string clutter in new[] { "chock_and_duster", "glass_and_ice_cube_", "Book_Pile_1", "Book_Pile_4", "Open_Book", "magnify_glass" })
                foreach (var t in s.lab.GetComponentsInChildren<Transform>(true).Where(t => t.name == clutter)) t.gameObject.SetActive(false);
            Find(Find(s.lab, "hat_and_glass").gameObject, "Object_4").gameObject.SetActive(false);
            int flames = 0;
            foreach (var r in s.lab.GetComponentsInChildren<Renderer>(true))
            {
                bool flame = r.name.StartsWith("Flame");
                bool window = r.name.StartsWith("Window") || r.name.StartsWith("Wall_Window");
                // The window wall lets the "window light" in; everything else shadows from both sides.
                r.shadowCastingMode = flame || window ? ShadowCastingMode.Off : ShadowCastingMode.TwoSided;
                if (flame && r.sharedMaterial) r.sharedMaterial = EmissiveCopy(r.sharedMaterial, "Flame_" + (r.name.Contains("low") ? "Candle" : "Fire"), new Color(4f, 1.9f, 0.55f));
                if (flame) flames++;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
            }
            Variant(s.lab, "Lab_Scene04");

            s.groupPoint = Node("Group Point", null, Group).transform;
            s.ohmPoint = Node("Ohm Point", null, OhmHead).transform;
            s.wirePoint = Node("Wire Point", null, Wire).transform;

            var apparatus = BuildApparatus(s);

            // Georg Ohm, working behind his desk and facing the room.
            var ohm = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(OhmModel), scene);
            ohm.name = "Georg Ohm";
            ohm.transform.position = OhmFeet;
            ohm.transform.localScale = Vector3.one * 1.55f;
            s.ohm = ohm.AddComponent<OhmWorker>();
            Variant(ohm, "Ohm_Scene04");
            s.ohm.notebook = apparatus["Ohm Notebook Target"];
            s.ohm.galvanometer = apparatus["Ohm Galvanometer Target"];
            s.ohm.wirePost = apparatus["Ohm Wire Target"];
            s.ohm.needle = apparatus["Galvanometer Needle Pivot"];
            PrefabUtility.RecordPrefabInstancePropertyModifications(s.ohm); // scene references are instance overrides

            BuildWattson(s, context, sfx);

            s.sfx = Audio("SFX", null, AudioBus.Kind.Sfx);
            s.music = Audio("Music", null, AudioBus.Kind.Music, 0.35f);
            return s;
        }

        static void BuildWattson(Stage s, SceneAgentContext context, Dictionary<string, AudioClip> sfx)
        {
            var accent = palette.wattsonAccent;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WattsonPrefab), scene);
            go.name = "Wattson";
            // The earlier lesson controller is replaced by the shared cinematic one on this variant only.
            foreach (var old in go.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(old);
            go.transform.position = Hover;
            go.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Group - Hover, Vector3.up));
            go.transform.localScale = Vector3.one * 0.34f;

            var w = go.AddComponent<WattsonController>();
            go.AddComponent<Animator>(); // lets the Timeline animate pathT
            w.visual = Find(go, "Wattson LB Visual");
            w.rightShoulder = Find(go, "right_Sholder_jnt");
            w.rightElbow = Find(go, "right_elbow_jnt");
            w.leftShoulder = Find(go, "left_Sholder_jnt");
            w.leftElbow = Find(go, "left_elbow_jnt");
            w.mouth = Find(go, "LB_geo").GetComponent<SkinnedMeshRenderer>();
            w.mouthShape = Mathf.Max(0, w.mouth.sharedMesh.GetBlendShapeIndex("blendShape1.Mouth"));
            w.mouth.sharedMaterial = EmissiveCopy(w.mouth.sharedMaterial, "Wattson_Glow", Color.black);
            w.glowRenderers = new Renderer[] { w.mouth };
            w.glowColor = new Color(1f, 0.92f, 0.7f); // multiplies his own texture, so he glows in his own yellow

            w.voice = Audio("Voice (scripted)", go.transform, AudioBus.Kind.Voice);
            w.liveVoice = Audio("Voice (live answers)", go.transform, AudioBus.Kind.Voice);
            w.sfx = Audio("Chirps", go.transform, AudioBus.Kind.Sfx);
            w.chirp = sfx["Chirp"];

            var spark = Glow("Glow_WattsonSpark", Hdr(accent, 3f));
            w.sparks = Particles("Spark Trail", go.transform, Vector3.zero, spark, ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.05f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 300;
                var emission = ps.emission;
                emission.rateOverTime = 4;
                emission.rateOverDistance = 45;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.12f;
            });
            var glow = Node("Follow Light", go.transform).AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.95f, 0.8f);
            glow.intensity = 0.7f;
            glow.range = 2.2f;
            glow.shadows = LightShadows.None;

            // Ohm-niscope: a brass and glass scanner, built along the arm so the lens rings lead the hand.
            var wrist = Find(go, "right_wrist_jnt");
            var brass = Lit("Brass", new Color(0.83f, 0.66f, 0.3f), 1f, 0.68f);
            var ringGlow = Lit("NiscopeRing", new Color(0.83f, 0.66f, 0.3f), 1f, 0.75f, Hdr(accent, 2.2f));
            var torus = Torus("NiscopeRing", 0.09f);
            var gadget = Node("Ohm-niscope", null, wrist.position);
            gadget.transform.rotation = Quaternion.LookRotation((wrist.position - w.rightElbow.position).normalized, go.transform.forward);
            var grip = Prim(PrimitiveType.Cylinder, "Handle", gadget.transform, Vector3.zero, new Vector3(0.016f, 0.05f, 0.016f), brass).transform;
            grip.localRotation = Quaternion.Euler(90, 0, 0);
            grip.localPosition = new Vector3(0, 0, 0.05f);
            var rings = new List<Transform>();
            float[] radius = { 0.085f, 0.065f, 0.045f }, offset = { 0.18f, 0.215f, 0.245f };
            for (int i = 0; i < 3; i++)
            {
                var ring = Node("Lens Ring " + (i + 1), gadget.transform);
                ring.transform.localPosition = new Vector3(0, 0, offset[i]);
                ring.transform.localScale = Vector3.one * radius[i];
                ring.AddComponent<MeshFilter>().sharedMesh = torus;
                ring.AddComponent<MeshRenderer>().sharedMaterial = ringGlow;
                rings.Add(ring.transform);
            }
            var lens = Prim(PrimitiveType.Quad, "Lens Flash", gadget.transform, Vector3.zero, Vector3.one * 0.3f, Glow("Glow_Lens", Hdr(accent, 2.5f)));
            lens.transform.localPosition = new Vector3(0, 0, offset[2]);
            lens.transform.localRotation = Quaternion.identity;
            var flare = Node("Lens Light", gadget.transform).AddComponent<Light>();
            flare.transform.localPosition = new Vector3(0, 0, offset[2] + 0.05f);
            flare.type = LightType.Point;
            flare.color = accent;
            flare.range = 1.8f;
            flare.intensity = 0;
            flare.shadows = LightShadows.None;

            var beam = Lines("Beam", gadget.transform, Glow("Glow_Beam", Hdr(accent, 6f)), 0.035f, false, false, new Vector3(0, 0, offset[2]), new Vector3(0, 0, 2.4f));
            beam.gameObject.SetActive(false);
            s.fx["Beam"] = beam.gameObject;

            s.niscope = gadget.AddComponent<Niscope>();
            s.niscope.rings = rings.ToArray();
            s.niscope.flare = flare;
            s.niscope.flash = lens.GetComponent<Renderer>();
            gadget.transform.SetParent(wrist, true);
            gadget.SetActive(false);
            w.gadget = gadget.transform;

            Variant(go, "Wattson_Scene04");

            // Scene-only wiring: flight path and what he looks at.
            s.path = Node("Wattson Flight Path").AddComponent<SplineContainer>();
            var spline = s.path.Spline;
            spline.Clear();
            foreach (var knot in new[]
            {
                new Vector3(-3.0f, -0.6f, -2.38f),   // hidden behind the desk and apparatus
                new Vector3(-2.95f, 0.3f, -2.2f),    // pops up over the back of the desk
                new Vector3(-2.6f, 0.8f, -1.3f),     // arcs over the desk
                new Vector3(-1.75f, 0.62f, -0.95f),
                new Vector3(-1.2f, 0.0f, -0.8f),     // playful dip
                Hover,
            })
                spline.Add(new BezierKnot(knot), TangentMode.AutoSmooth);
            w.path = s.path;
            w.pathT = 0;
            w.agentContext = context;
            w.lookTarget = s.groupPoint;
            w.pointTarget = s.ohmPoint;
            PrefabUtility.RecordPrefabInstancePropertyModifications(w);
            s.wattson = w;
        }

        // ------------------------------------------------------------------ lighting, atmosphere, post

        static void BuildLighting(Stage s)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.18f, 0.26f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.15f, 0.11f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.05f, 0.04f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.3f, 0.24f, 0.2f);
            RenderSettings.fogDensity = 0.03f;

            var root = Node("Lighting").transform;

            // Cool window light: the one shadowed directional.
            var window = Node("Window Light", root).AddComponent<Light>();
            window.type = LightType.Directional;
            window.transform.rotation = Quaternion.Euler(28, 82, 0);
            window.color = new Color(0.74f, 0.84f, 1f);
            window.intensity = 2.4f;
            window.shadows = LightShadows.Soft;

            Light Flame(string name, Vector3 position, float intensity, float range, bool shadows)
            {
                var light = Node(name, root, position).AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.6f, 0.28f);
                light.intensity = intensity;
                light.range = range;
                light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
                light.gameObject.AddComponent<FlickerLight>();
                return light;
            }
            // Two realtime shadow casters in total: the window and the desk lamp. The lamp and candle are accents (same numbers
            // as SceneBuilderBase.StudyLighting, which Scenes 7-8 light this room with): brighter, they clipped Ohm's face.
            Flame("Oil Lamp", new Vector3(-1.7f, 0.16f, -1.72f), 1.6f, 3f, true);
            Flame("Desk Candle", new Vector3(-2.36f, 0.1f, -1.9f), 0.7f, 1.8f, false);
            Flame("Fireplace", new Vector3(-1.35f, -0.5f, -3.15f), 4.5f, 4.5f, false).GetComponent<FlickerLight>().speed = 4f;
            Flame("Bookcase Candle", new Vector3(-2.85f, 0.22f, 0.87f), 1.4f, 3f, false);

            // Soft fill from the group's side, so faces turned to the camera are not left in silhouette.
            var fill = Node("Group Fill", root, new Vector3(0f, 0.85f, 0.55f)).AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.9f, 0.78f);
            fill.intensity = 2f;
            fill.range = 5f;
            fill.shadows = LightShadows.None;

            var probe = Node("Reflection Probe", root, new Vector3(-1.45f, 0.3f, -1f)).AddComponent<ReflectionProbe>();
            // Baked, not realtime: a realtime probe has no texture in edit mode, which URP's Forward+ probe atlas cannot handle.
            probe.mode = ReflectionProbeMode.Baked;
            s.probe = probe;
            probe.size = new Vector3(3.9f, 2.9f, 4.9f);
            probe.boxProjection = true;
            probe.resolution = 128;

            var probes = Node("Light Probes", root).AddComponent<LightProbeGroup>();
            var points = new List<Vector3>();
            foreach (float x in new[] { -2.9f, -1.5f, 0f })
                foreach (float y in new[] { -0.8f, 0.3f, 1.3f })
                    foreach (float z in new[] { -2.9f, -1.3f, 0.6f })
                        points.Add(new Vector3(x, y, z));
            probes.probePositions = points.ToArray();

            // Global look: ACES, bloom, vignette, grain. Per-shot depth of field lives on the cameras.
            var profile = Profile("Scene04_Global", p =>
            {
                Override<Tonemapping>(p).mode.Override(TonemappingMode.ACES);
                var bloom = Override<Bloom>(p);
                bloom.threshold.Override(1f);
                bloom.intensity.Override(0.85f);
                bloom.scatter.Override(0.72f);
                var vignette = Override<Vignette>(p);
                vignette.intensity.Override(0.26f);
                vignette.smoothness.Override(0.45f);
                var grain = Override<FilmGrain>(p);
                grain.type.Override(FilmGrainLookup.Thin1);
                grain.intensity.Override(0.22f);
                var color = Override<ColorAdjustments>(p);
                color.postExposure.Override(0.25f);
                color.contrast.Override(8f);
                color.saturation.Override(6f);
            });
            var volume = Node("Global Volume", root).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            // God rays: additive shafts from the window along the light direction, fading with distance.
            var rays = Node("God Rays", root);
            rays.AddComponent<MeshFilter>().sharedMesh = MeshAsset("GodRays", mesh =>
            {
                Vector3 direction = window.transform.forward * 3.4f;
                var vertices = new List<Vector3>();
                var colors = new List<Color>();
                var uvs = new List<Vector2>();
                var triangles = new List<int>();
                // Slanted sheets through the window opening. The soft texture runs across each sheet's height,
                // so the shafts have no hard edges and fade out along their length.
                for (int i = 0; i < 6; i++)
                {
                    float z = Mathf.Lerp(-3.05f, -2.2f, i / 5f);
                    int v = vertices.Count;
                    Vector3 top = new Vector3(-3.3f, 1.45f, z), bottom = new Vector3(-3.3f, -0.1f, z);
                    vertices.AddRange(new[] { top, bottom, bottom + direction, top + direction });
                    uvs.AddRange(new[] { new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 1), new Vector2(0, 1) });
                    colors.AddRange(new[] { Color.white, Color.white, Color.white, Color.white });
                    triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
                }
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
            });
            var rayRenderer = rays.AddComponent<MeshRenderer>();
            rayRenderer.sharedMaterial = Glow("Glow_GodRay", new Color(0.6f, 0.74f, 1f, 0.16f));
            rayRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        static VolumeProfile Profile(string name, Action<VolumeProfile> setup)
        {
            var profile = Asset(S4 + "/VFX/" + name + ".asset", ScriptableObject.CreateInstance<VolumeProfile>);
            setup(profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T Override<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(false);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            // Only the values set by the caller are overridden; everything else keeps the URP default.
            foreach (var parameter in component.parameters) parameter.overrideState = false;
            EditorUtility.SetDirty(component);
            return component;
        }

        // ------------------------------------------------------------------ VFX

        static ParticleSystem Particles(string name, Transform parent, Vector3 position, Material material, Action<ParticleSystem> setup)
        {
            var ps = Node(name, parent, position).AddComponent<ParticleSystem>();
            if (parent) ps.transform.localPosition = position;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            setup(ps);
            // Scene 4 has no SceneDirector to soften fx-cued effects, so every emitter calms itself under MOTION REDUCED
            // (the spark trail is already scaled by WattsonController).
            if (name != "Spark Trail") ps.gameObject.AddComponent<ReducedMotionFx>().calm = 0.35f;
            return ps;
        }

        static GameObject Word(Stage s, string text, Vector3 position, Color color, string sparkName)
        {
            var go = Node(text, s.words, position);
            go.transform.rotation = Quaternion.LookRotation(position - WordsCamera);
            var label = go.AddComponent<TextMeshPro>();
            label.font = palette.font;
            label.text = text;
            label.fontSize = 1.3f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(1.4f, 0.4f);
            // Emissive face with an outer glow, bright enough for Bloom.
            label.fontSharedMaterial = Asset(S4 + "/Materials/Word_" + (text == "?" ? "Question" : text) + ".mat", () => new Material(palette.font.material));
            var material = label.fontSharedMaterial;
            material.shader = Shader.Find("TextMeshPro/Distance Field");
            material.SetTexture(ShaderUtilities.ID_MainTex, palette.font.atlasTexture);
            material.SetColor(ShaderUtilities.ID_FaceColor, Hdr(color, 2.6f));
            material.EnableKeyword("GLOW_ON");
            material.SetColor(ShaderUtilities.ID_GlowColor, new Color(color.r, color.g, color.b, 0.6f));
            material.SetFloat(ShaderUtilities.ID_GlowOuter, 0.45f);
            material.SetFloat(ShaderUtilities.ID_GlowPower, 0.6f);
            EditorUtility.SetDirty(material);

            var pop = go.AddComponent<PopIn>();
            pop.duration = 0.55f;
            pop.overshoot = 1.45f;
            pop.bob = 0.025f;
            Particles("Sparks", go.transform, Vector3.zero, Glow(sparkName, Hdr(color, 4f)), ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.05f);
                main.gravityModifier = 0.4f;
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;
            });
            go.SetActive(false);
            s.fx[text] = go;
            return go;
        }

        static void BuildVfx(Stage s)
        {
            s.vfx = Node("VFX");
            var root = s.vfx.transform;
            Color voltage = palette.voltage, current = palette.current, accent = palette.wattsonAccent;

            // SH01: blue and gold charge streams swirl in front of the group, then thin out into dust.
            foreach (var (name, color) in new[] { ("Charge Streams Blue", voltage), ("Charge Streams Gold", current) })
                s.fx[name] = Particles(name, root, Group + new Vector3(-0.9f, 0.1f, -0.9f), Glow("Glow_" + name.Replace(" ", ""), Hdr(color, 3.5f)), ps =>
                {
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 2.6f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.07f);
                    main.maxParticles = 500;
                    var emission = ps.emission;
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(160, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 1.1f;
                    var velocity = ps.velocityOverLifetime;
                    velocity.enabled = true;
                    velocity.orbitalY = color == voltage ? 2.2f : -1.7f;
                    velocity.orbitalX = 0.6f;
                    velocity.radial = -0.35f;
                    var trails = ps.trails;
                    trails.enabled = true;
                    trails.lifetime = 0.25f;
                    trails.dieWithParticles = true;
                    trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));
                    ps.GetComponent<ParticleSystemRenderer>().trailMaterial = ps.GetComponent<ParticleSystemRenderer>().sharedMaterial;
                }).gameObject;

            s.fx["Landing Dust"] = Particles("Landing Dust", root, new Vector3(Group.x - 0.3f, FloorY + 0.03f, Group.z - 0.5f), Glow("Glow_Dust", new Color(0.8f, 0.68f, 0.5f, 0.2f)), ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Hemisphere;
                shape.radius = 0.5f;
                shape.rotation = new Vector3(-90, 0, 0);
            }).gameObject;

            // Always on: dust motes drifting through the window light.
            Particles("Dust Motes", root, new Vector3(-1.5f, 0.2f, -1.3f), Glow("Glow_Motes", new Color(0.9f, 0.85f, 0.75f, 0.55f)), ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
                main.startSpeed = 0.01f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.016f);
                main.maxParticles = 260;
                main.prewarm = true;
                var emission = ps.emission;
                emission.rateOverTime = 26;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(3.4f, 2.4f, 4.2f);
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.03f;
                noise.frequency = 0.25f;
            });

            // SH05: the two floating words, left and right of Wattson as the group sees him.
            s.words = Node("Words").transform;
            Vector3 toCamera = Vector3.ProjectOnPlane(WordsCamera - Hover, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, -toCamera); // screen-right for a camera looking at Wattson
            Vector3 centre = Hover + Vector3.up * 0.46f;
            s.voltage = Word(s, "VOLTAGE", centre - right * 0.52f, voltage, "Glow_SparkVoltage").transform;
            s.current = Word(s, "CURRENT", centre + right * 0.52f, current, "Glow_SparkCurrent").transform;
            s.wordsPoint = Node("Words Point", null, centre).transform;

            // SH06: an arc that tries to join the words and breaks, with a pulsing question mark between them.
            var arc = Lines("Arc", s.words, Glow("Glow_Arc", Hdr(Color.Lerp(voltage, current, 0.5f) + Color.white * 0.3f, 5f), false), 0.012f, false, true, new Vector3[14]).gameObject.AddComponent<ElectricArc>();
            var arcFrom = Node("Arc From", s.voltage, Vector3.zero).transform;
            arcFrom.localPosition = new Vector3(0.36f, 0, 0);
            var arcTo = Node("Arc To", s.current, Vector3.zero).transform;
            arcTo.localPosition = new Vector3(-0.36f, 0, 0);
            arc.from = arcFrom;
            arc.to = arcTo;
            arc.gameObject.SetActive(false);
            s.fx["Arc"] = arc.gameObject;

            var question = Word(s, "?", centre + Vector3.up * 0.22f, Color.white, "Glow_SparkQuestion");
            question.GetComponent<TextMeshPro>().fontSize = 2.4f;
            var pulse = question.GetComponent<PopIn>();
            pulse.pulse = 0.14f;
            pulse.pulseSpeed = 5f;

            // SH09: reticle that locks onto the wire, plus the wire's highlight outline and shimmer.
            Vector3 view = (Wire - Hover).normalized;
            var reticle = Node("Reticle", root, Wire - view * 0.05f);
            reticle.transform.rotation = Quaternion.LookRotation(view);
            var reticleGlow = Glow("Glow_Reticle", Hdr(accent, 2.2f), false);
            var circle = Enumerable.Range(0, 48).Select(i => new Vector3(Mathf.Cos(i * Mathf.PI / 24), Mathf.Sin(i * Mathf.PI / 24), 0) * 0.16f).ToArray();
            Lines("Ring", reticle.transform, reticleGlow, 0.005f, true, false, circle);
            for (int i = 0; i < 4; i++)
            {
                var d = Quaternion.Euler(0, 0, i * 90) * Vector3.right;
                Lines("Tick", reticle.transform, reticleGlow, 0.005f, false, false, d * 0.12f, d * 0.22f);
            }
            var lockOn = reticle.AddComponent<PopIn>();
            lockOn.fromScale = 2.6f;
            lockOn.overshoot = 0.8f;
            lockOn.duration = 0.5f;
            lockOn.pulse = 0.04f;
            reticle.SetActive(false);
            s.fx["Reticle"] = reticle;

            var highlight = Node("Wire Highlight", root, Wire);
            var outline = Object.Instantiate(s.wire.gameObject, highlight.transform);
            outline.name = "Outline";
            outline.transform.SetPositionAndRotation(s.wire.position, s.wire.rotation);
            outline.transform.localScale = s.wire.lossyScale;
            // The project's outline shader: a stencil mask pass, then the widened hull drawn only outside it.
            var mask = Mat("Outline_WireMask", "AdaptiveLearning/EquipmentOutline", m => { m.SetFloat("_Width", 0); m.SetFloat("_StencilComp", 8); m.SetFloat("_StencilPass", 2); m.SetFloat("_ColorMask", 0); m.renderQueue = 2990; });
            var hull = Mat("Outline_Wire", "AdaptiveLearning/EquipmentOutline", m => { m.SetColor("_Color", Hdr(current, 2.5f)); m.SetFloat("_Width", 5); m.SetFloat("_StencilComp", 6); m.SetFloat("_StencilPass", 0); m.SetFloat("_ColorMask", 15); m.renderQueue = 3000; });
            outline.GetComponent<Renderer>().sharedMaterials = new[] { mask, hull };
            Particles("Shimmer", highlight.transform, Vector3.zero, Glow("Glow_Shimmer", Hdr(current, 4f)), ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                main.startSpeed = 0.03f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.022f);
                var emission = ps.emission;
                emission.rateOverTime = 70;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(0.42f, 0.012f, 0.012f);
            });
            highlight.SetActive(false);
            s.fx["Wire Highlight"] = highlight;

            // SH11: energy streaks rushing out of the wire at the camera.
            s.fx["Dive Streaks"] = Particles("Dive Streaks", root, Wire, Glow("Glow_Streaks", Hdr(Color.Lerp(accent, current, 0.3f), 5f)), ps =>
            {
                ps.transform.rotation = Quaternion.LookRotation(-view);
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
                main.maxParticles = 600;
                var emission = ps.emission;
                emission.rateOverTime = 320;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 24;
                shape.radius = 0.03f;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.12f;
                renderer.lengthScale = 2f;
            }).gameObject;

            foreach (string name in new[] { "Charge Streams Blue", "Charge Streams Gold", "Landing Dust", "Dive Streaks" }) s.fx[name].SetActive(false);
        }

        // ------------------------------------------------------------------ cameras

        static CinemachineCamera Vcam(Stage s, Transform parent, string name, Vector3 position, Vector3 lookAt, float fov, Transform focus, VolumeProfile lens, Transform track = null, float noise = 0)
        {
            var go = Node(name, parent, position);
            go.transform.rotation = Quaternion.LookRotation(lookAt - position);
            var cam = go.AddComponent<CinemachineCamera>();
            var lensSettings = LensSettings.Default;
            lensSettings.FieldOfView = fov;
            lensSettings.NearClipPlane = 0.05f;
            lensSettings.FarClipPlane = 60f;
            cam.Lens = lensSettings;
            cam.Priority = 0;
            if (track)
            {
                cam.Target.TrackingTarget = track;
                go.AddComponent<CinemachineRotationComposer>();
            }
            if (noise > 0)
            {
                string guid = AssetDatabase.FindAssets("Handheld_normal_mild t:NoiseSettings").FirstOrDefault();
                var shake = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
                shake.NoiseProfile = guid != null ? AssetDatabase.LoadAssetAtPath<NoiseSettings>(AssetDatabase.GUIDToAssetPath(guid)) : null;
                shake.AmplitudeGain = noise;
                shake.FrequencyGain = 0.8f;
                go.AddComponent<ReducedMotionCamera>(); // no handheld noise while the HUD's MOTION toggle is on
            }
            // Per-shot lens: Bokeh depth of field that follows the focus target, so blends between cameras rack focus.
            var post = go.AddComponent<CinemachineVolumeSettings>();
            post.Profile = lens;
            post.FocusTracking = CinemachineVolumeSettings.FocusTrackingMode.CustomTarget;
            post.FocusTarget = focus;
            s.cams[name] = cam;
            return cam;
        }

        static VolumeProfile Lens(string name, float focalLength, float aperture, bool dive = false, bool motionBlur = false) => Profile(name, p =>
        {
            var dof = Override<DepthOfField>(p);
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(2f);
            dof.focalLength.Override(focalLength);
            dof.aperture.Override(aperture);
            if (motionBlur || dive)
            {
                var blur = Override<MotionBlur>(p);
                blur.intensity.Override(dive ? 0.9f : 0.45f);
            }
            if (dive)
            {
                var aberration = Override<ChromaticAberration>(p);
                aberration.intensity.Override(0.9f);
            }
        });

        static void BuildCameras(Stage s)
        {
            var camGo = Node("Main Camera", null, new Vector3(0.15f, 0.75f, 0.95f));
            camGo.tag = "MainCamera";
            s.camera = camGo.AddComponent<Camera>();
            s.camera.clearFlags = CameraClearFlags.SolidColor;
            s.camera.backgroundColor = new Color(0.5f, 0.63f, 0.85f); // pale sky outside the window
            s.camera.nearClipPlane = 0.05f;
            s.camera.allowHDR = true;
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var brain = camGo.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);

            var wide = Lens("Lens_Wide", 28, 5.6f);
            var medium = Lens("Lens_Medium", 45, 3.2f);
            var shallow = Lens("Lens_Shallow", 70, 1.8f);
            var tracking = Lens("Lens_Tracking", 35, 4f, motionBlur: true);
            var dive = Lens("Lens_Dive", 30, 2.8f, dive: true);

            var root = Node("Cameras").transform;
            Transform W = s.wattson.transform, ohm = s.ohmPoint, wire = s.wirePoint, words = s.wordsPoint;
            Vector3 up = Vector3.up;
            Vector3 toGroup = (Group - Hover).normalized;
            Vector3 deskLook = new Vector3(-2.0f, -0.25f, -1.8f);

            // SH01 arrival: wide, drifting down and in as the group "lands".
            Vcam(s, root, "SH01a Arrival", new Vector3(0.15f, 0.75f, 0.95f), deskLook, 50, ohm, wide, null, 1.4f).Priority = 10;
            Vcam(s, root, "SH01b Arrival", new Vector3(-0.15f, 0.42f, 0.55f), deskLook, 46, ohm, wide, null, 0.6f);
            // SH02: tracking dolly that keeps Wattson framed as he flies in.
            Vcam(s, root, "SH02a Wattson Enters", new Vector3(-0.45f, 0.35f, 0.15f), Hover, 45, W, tracking, W);
            Vcam(s, root, "SH02b Wattson Enters", new Vector3(-0.3f, 0.3f, 0.3f), Hover, 42, W, tracking, W);
            // SH03: medium close-up from slightly below.
            Vcam(s, root, "SH03 Welcome", Hover + toGroup * 0.95f - up * 0.12f, Hover + up * 0.05f, 32, W, shallow);
            // SH04: over Wattson's shoulder to Ohm; focus racks from Wattson to Ohm and back.
            Vector3 fromOhm = Vector3.ProjectOnPlane(Hover - OhmHead, up).normalized;
            Vector3 shoulder = Vector3.Cross(up, -fromOhm) * 0.3f;
            Vector3 ots = Hover + fromOhm * 0.85f + shoulder + up * 0.15f;
            Vcam(s, root, "SH04a Over Shoulder", ots, OhmHead + new Vector3(0.2f, -0.1f, 0), 38, W, shallow);
            Vcam(s, root, "SH04b Rack To Ohm", ots - fromOhm * 0.35f, OhmHead + new Vector3(0.1f, -0.1f, 0), 36, ohm, shallow);
            // SH05: medium, Wattson centred, room for the words on both sides (fits 4:3 too).
            Vcam(s, root, "SH05 Words", WordsCamera, Hover + up * 0.3f, 44, W, medium);
            // SH06: slow orbit of about 25 degrees around the words.
            Vector3 radius = Vector3.ProjectOnPlane(WordsCamera - Hover, up).normalized * 1.8f;
            Vcam(s, root, "SH06a Orbit", Hover + Quaternion.Euler(0, -12.5f, 0) * radius + up * 0.15f, words.position, 44, words, medium);
            Vcam(s, root, "SH06b Orbit", Hover + Quaternion.Euler(0, 12.5f, 0) * radius + up * 0.15f, words.position, 42, words, medium);
            // SH07: medium.
            Vcam(s, root, "SH07 Before", Hover + toGroup * 1.4f - up * 0.04f, Hover + up * 0.22f, 38, W, medium);
            // SH08: hero low angle with a slow push.
            Vcam(s, root, "SH08a Hero", Hover + toGroup * 0.98f - up * 0.42f, Hover + up * 0.3f, 36, W, shallow);
            Vcam(s, root, "SH08b Hero", Hover + toGroup * 0.74f - up * 0.38f, Hover + up * 0.3f, 34, W, shallow);
            // SH09: over the shoulder towards the copper wire, pushing in.
            Vector3 toWire = (Wire - Hover).normalized, flatWire = Vector3.ProjectOnPlane(toWire, up).normalized;
            Vcam(s, root, "SH09a Aim", Hover - flatWire * 0.8f + Vector3.Cross(up, flatWire) * 0.28f + up * 0.22f, Wire, 30, W, shallow);
            Vcam(s, root, "SH09b Aim", Wire - toWire * 1.05f + up * 0.12f, Wire, 30, wire, medium);
            // SH10: Wattson's face as he looks back at the group.
            Vcam(s, root, "SH10 Everybody In", Hover + toGroup * 0.72f + up * 0.03f, Hover + up * 0.08f, 30, W, shallow);
            // SH11: dolly-zoom into the wire, then a fast push.
            Vcam(s, root, "SH11a Dive", Wire - toWire * 1.5f + up * 0.1f, Wire, 22, wire, medium); // calm while it waits for the tap; the dive look blends in with SH11b
            Vcam(s, root, "SH11b Dive", Wire - toWire * 0.16f, Wire, 82, wire, dive);
        }

        static void BuildModuleRoot()
        {
            var go = Node("OhmsLaw Module");
            var flow = go.AddComponent<ModuleFlow>();
            flow.palette = palette;
            flow.proxy = AssetDatabase.LoadAssetAtPath<ProxyConfig>(Shared + "/Agent/ProxyConfig.asset");
            flow.scenes = BlockoutAssets.SceneOrder; // Scene 4 then the Scene 5-13 blockouts
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, Shared + "/ModuleRoot.prefab", InteractionMode.AutomatedAction);
        }

        static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(sc => sc.path == ScenePath)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); // the module starts at Scene 4
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
