using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using UnityEngine.Timeline;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OhmsLaw.Editor
{
    // Shared machinery for the blockout scene builders (Scenes 5-13): greybox props, placeholders, cameras,
    // a Timeline planned as an ordered list of beats, dialogue and agent assets, and the build log.
    // Each SceneNNBuilder subclass only describes its stage and beats.
    public abstract partial class SceneBuilderBase
    {
        public const string Root = Scene04Builder.Root, Shared = Scene04Builder.Shared;
        protected const string WattsonPrefab = "Assets/AdaptiveLearning/Prefabs/Wattson/Wattson.prefab";
        protected const string OhmModel = "Assets/AdaptiveLearning/Art/GEORGE OHM/GEORGE OHM/GEORGE OHM.fbx";
        protected static readonly string[] Kids = { "Kabir", "Manav", "Ishaan", "Meera" };

        public abstract string SceneId { get; }        // "Scene05"
        public abstract string SceneName { get; }      // "Scene05_InsideTheWire"
        public abstract string Title { get; }
        protected string Folder => Root + "/" + SceneId;
        public string ScenePath => Root + "/Scenes/" + SceneName + ".unity";

        protected Scene scene;
        protected ModulePalette palette;
        protected SceneDirector director;
        protected PlayableDirector playable;
        protected WattsonController wattson;
        protected Camera camera;
        protected readonly Dictionary<string, DialogueLine> lines = new Dictionary<string, DialogueLine>();
        protected readonly Dictionary<string, CinemachineCamera> cams = new Dictionary<string, CinemachineCamera>();
        protected readonly Dictionary<string, GameObject> refs = new Dictionary<string, GameObject>();
        protected readonly List<Beat> beats = new List<Beat>();
        protected Material grey, dark, placeholderMat, magenta, cardBacking, chargeMat;
        protected AudioSource voice, sfx;
        protected Dictionary<string, AudioClip> sounds;

        // ------------------------------------------------------------------ beats

        public enum Kind { Line, Visual, Gate, Cue, Camera, Handoff, Wattson }

        public class Beat
        {
            public Kind kind;
            public double at, length;
            public string text, id;         // line id / cue command / camera name / gate id; text = description
            public DialogueLine[] gateLines;
            public WattsonController.Gesture gesture;
            public string look, point, moveTo;
            public bool gadget;
            public bool cut;                 // Handoff: match-cut (no fade) when true
        }

        protected double clock;

        // The next beat starts here. Beats are appended in order; lines advance the clock, cues and cameras do not.
        protected void Line(string id, double gapAfter = 0.3)
        {
            var line = lines[id];
            beats.Add(new Beat { kind = Kind.Line, at = clock, length = line.Duration, id = id, text = line.text });
            clock += line.Duration + gapAfter;
        }

        protected void Wait(double seconds) => clock += seconds;

        protected void Visual(string description, double seconds = 0)
        {
            beats.Add(new Beat { kind = Kind.Visual, at = clock, length = seconds, text = description });
            clock += seconds;
        }

        protected void Cue(string command, string note = "", double offset = 0) =>
            beats.Add(new Beat { kind = Kind.Cue, at = clock + offset, id = command, text = note });

        protected void Show(string obj, double offset = 0) => Cue("show " + obj, "", offset);
        protected void Hide(string obj, double offset = 0) => Cue("hide " + obj, "", offset);

        // Sound: a one-shot from the bank, the ambience bed, the music bed (crossfaded; "off" stops one).
        protected void Sfx(string clip, double offset = 0) => Cue("sfx " + clip, "", offset);
        protected void Ambience(string clip, double offset = 0) => Cue("ambience " + clip, "", offset);
        protected void Music(string clip, double offset = 0) => Cue("music " + clip, "", offset);
        // The lesson panel: the one instruction, a note (title | body), a formula line, a hero reveal (big | small).
        protected void Instruction(string text, double offset = 0) => Cue("prompt " + text, "", offset);
        protected void Info(string id, string title, string body) => Cue("callout show " + id, title + " / " + body);
        // Reference px the HUD's right dock reserves in this scene; cinematic scenes return 0 and get the full frame.
        protected virtual float DockWidth => LessonPanel.NormalWidth;

        protected void Formula(string text, double offset = 0) => Cue("formula " + text, "", offset);
        protected void Reveal(string big, string small, double offset = 0) => Cue("reveal " + big + "|" + small, "", offset);

        // offset (seconds from the clock) lands a cut or a gesture on a word inside the next line, like Cue's offset.
        protected void Shot(string camName, bool cut = false, double offset = 0) => beats.Add(new Beat { kind = Kind.Camera, at = clock + offset, id = camName, cut = cut });

        protected void Gesture(WattsonController.Gesture gesture, string look = "Main Camera", string point = null, string moveTo = null, bool gadget = false, double offset = 0) =>
            beats.Add(new Beat { kind = Kind.Wattson, at = clock + offset, gesture = gesture, look = look, point = point, moveTo = moveTo, gadget = gadget });

        // The gate pauses the Timeline here; the next beat sits a little later so it never starts on the gate's frame.
        protected void Gate(string id, string description, params string[] lineIds)
        {
            beats.Add(new Beat { kind = Kind.Gate, at = clock, id = id, text = description, gateLines = lineIds.Select(l => lines[l]).ToArray() });
            clock += 0.2;
        }

        protected void Handoff(string signal, bool cut, double afterSeconds = 0.5)
        {
            clock += afterSeconds;
            beats.Add(new Beat { kind = Kind.Handoff, at = clock, id = signal, cut = cut, text = cut ? "match-cut" : "fade to white" });
            clock += cut ? 0.1 : 0.8;
        }

        // ------------------------------------------------------------------ build entry

        public void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) throw new InvalidOperationException("Import TextMeshPro essentials first (Ohm's Law > Build Scene 04 does it).");
            foreach (var folder in new[] { "Scenes", "Shared/Resources", SceneId + "/Dialogue", SceneId + "/Dialogue/Proposed", SceneId + "/Agent", SceneId + "/Audio/VO", SceneId + "/Timeline" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();

            palette = BlockoutAssets.Palette();
            BlockoutAssets.ExperimentData();
            sounds = BlockoutAssets.Sfx();
            lines.Clear(); cams.Clear(); refs.Clear(); beats.Clear(); clock = 0;
            Dialogue();
            var context = Agent();

            var previous = SceneManager.GetActiveScene();
            scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.isLoaded) foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
            else if (!string.IsNullOrEmpty(previous.path)) scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            else if (previous.isDirty) throw new InvalidOperationException("Save or discard your untitled scene, then build again.");
            else scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            Materials();
            var host = Node(SceneId + " Director");
            playable = host.AddComponent<PlayableDirector>();
            director = host.AddComponent<SceneDirector>();
            director.sceneId = SceneId;
            voice = Audio("Voice", host.transform, AudioBus.Kind.Voice);
            sfx = Audio("SFX", host.transform, AudioBus.Kind.Sfx);
            director.voice = voice;
            director.sfx = sfx;
            director.click = sounds["Click"];
            director.pop = sounds["Pop"];
            // The whole synthesized bank, by clip name, for the "sfx" cue and the gates; two looping beds for the
            // "ambience" and "music" cues (quiet, ducked under the voice like every other source).
            director.sounds = sounds.Values.Where(c => c).ToArray();
            director.dockWidth = DockWidth; // the HUD's right dock strip in this scene (0 = cinematic, full frame)
            director.ambience = Audio("Ambience", host.transform, AudioBus.Kind.Sfx);
            director.ambience.loop = true;
            director.ambience.GetComponent<AudioBus>().gain = 0.5f;
            director.music = Audio("Music", host.transform, AudioBus.Kind.Music);
            director.music.loop = true;
            director.music.GetComponent<AudioBus>().gain = 0.35f;
            MainCamera();

            Stage();
            if (wattson) wattson.agentContext = context;
            director.voice = voice; // Wattson's own source when his prefab was used
            Beats();
            Timeline();
            director.refs = refs.Values.ToArray();
            director.wattson = wattson;
            ModuleRoot();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            if (previous.IsValid() && previous != scene) { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
            Debug.Log(Report());
        }

        protected abstract void Dialogue();
        protected abstract SceneAgentContext Agent();
        protected abstract void Stage();
        protected abstract void Beats();

        // ------------------------------------------------------------------ assets

        protected static T Data<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // Storyboard line. A VO clip named after the id in Audio/VO is picked up automatically.
        protected DialogueLine L(string id, string text, float fallback, bool proposed = false)
        {
            string folder = Folder + "/Dialogue" + (proposed ? "/Proposed" : "");
            var line = Data<DialogueLine>(folder + "/" + id + ".asset");
            line.id = id;
            line.speaker = "WATTSON";
            line.text = text;
            line.ttsText = text;
            line.fallbackDuration = fallback;
            line.vo = BlockoutAssets.FindVo(Folder, id);
            EditorUtility.SetDirty(line);
            lines[id] = line;
            return line;
        }

        protected SceneAgentContext Context(string[] covered, string[] doNotSpoil, params (string question, string answer)[] chips)
        {
            var faq = Data<LocalFAQ>(Folder + "/Agent/" + SceneId + "_FAQ.asset");
            faq.entries = chips.Select((c, i) => new LocalFAQ.Entry
            {
                question = c.question,
                answer = L(SceneId + "_FAQ_" + (i + 1), c.answer, Mathf.Clamp(c.answer.Length * 0.062f, 3f, 14f)),
            }).ToArray();
            EditorUtility.SetDirty(faq);
            var context = Data<SceneAgentContext>(Folder + "/Agent/" + SceneId + "_Context.asset");
            context.sceneId = SceneId;
            context.sceneTitle = Title;
            context.askWattsonEnabled = true;
            context.covered = covered;
            context.doNotSpoil = doNotSpoil;
            context.faq = faq;
            EditorUtility.SetDirty(context);
            return context;
        }

        // ------------------------------------------------------------------ scene objects

        protected GameObject Node(string name, Transform parent = null, Vector3 position = default)
        {
            var go = new GameObject(name);
            if (parent) { go.transform.SetParent(parent, false); go.transform.localPosition = position; }
            else { SceneManager.MoveGameObjectToScene(go, scene); go.transform.position = position; }
            return go;
        }

        protected GameObject Ref(GameObject go) { refs[go.name] = go; return go; }
        protected GameObject Marker(string name, Vector3 position) => Ref(Node(name, null, position));

        protected GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material = null, Vector3 euler = default, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (parent) go.transform.SetParent(parent, false); else SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material ? material : grey;
            return go;
        }

        // Greybox prop: a primitive with a BlockoutPlaceholder note. label = true adds a floating name at PropLabel size,
        // wrapping, facing `face` (a camera position) instead of world -Z; real models should not ask for one.
        protected GameObject Prop(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, string note, Material material = null, Vector3 euler = default, bool collider = false, bool label = false, Vector3? face = null)
        {
            var go = Prim(type, name, parent, position, scale, material ? material : placeholderMat, euler, collider);
            Placeholder(go, note);
            if (!label) return go;
            // The label is built at scene root (unit scale), then parented so it moves with the prop.
            var text = WorldText.Card("Label " + name, name, Color.white, null, Vector3.zero, WorldText.Cap.PropLabel, null, WorldText.Wrap.PropLabel);
            text.transform.position = go.transform.position + Vector3.up * (go.GetComponent<Renderer>().bounds.extents.y + 0.05f);
            if (face.HasValue) text.transform.rotation = Quaternion.LookRotation(text.transform.position - face.Value);
            text.transform.SetParent(go.transform, true);
            return go;
        }

        protected BlockoutPlaceholder Placeholder(GameObject go, string note, bool pulse = false)
        {
            var tag = go.TryGetComponent(out BlockoutPlaceholder existing) ? existing : go.AddComponent<BlockoutPlaceholder>();
            tag.note = note;
            tag.pulse = pulse;
            return tag;
        }

        // Reviewer marker for an effect that is not built yet: a small magenta-framed "[VFX] ..." tag hugging its text,
        // pulsing while active. width is the wrap width. Starts hidden; cues show and hide it.
        protected GameObject Vfx(string description, Transform parent, Vector3 position, float width = 1.2f)
        {
            var go = Node("[VFX] " + description, parent, position);
            const float cap = WorldText.Cap.VfxTag;
            var text = WorldText.Card("Text", "[VFX] " + description, new Color(1f, 0.3f, 1f), go.transform, new Vector3(0, 0, -0.01f), cap, null, width * 0.9f);
            var bounds = text.textBounds;
            var size = new Vector2(bounds.size.x + cap * 2f * WorldText.PadX, bounds.size.y + cap * 2f * WorldText.PadY);
            Prim(PrimitiveType.Quad, "Frame", go.transform, new Vector3(bounds.center.x, bounds.center.y, 0), new Vector3(size.x + 0.02f, size.y + 0.02f, 1), magenta);
            Prim(PrimitiveType.Quad, "Fill", go.transform, new Vector3(bounds.center.x, bounds.center.y, -0.005f), new Vector3(size.x, size.y, 1), dark);
            Placeholder(go, "VFX: " + description, true);
            go.SetActive(false);
            return Ref(go);
        }

        // Floating text card, registered as "Card <name>" so cues can show/hide it. Starts hidden unless told otherwise.
        // height is the cap height in metres (pick a WorldText.Cap tier). Hero, concept and formula words glow (Scene 4's
        // recipe); pass WorldText.Look.Panel for prose and data that need a backing.
        protected GameObject Card(string name, string text, Color color, Transform parent, Vector3 position, float height = WorldText.Cap.DataCard, bool shown = false, float width = 0) =>
            Card(name, text, color, parent, position, height, WorldText.Look.Glow, shown, width);

        // The card is a root (Ref, Face, boost, billboard) with the text as a child, so PopIn's scale and the "boost" cue's
        // scale never fight. lineSpacing null = -20 for all-caps text, 0 for prose. billboard = refDepth in metres for a
        // WorldLabel that keeps the screen size across shots (do not combine with the "boost" cue).
        protected GameObject Card(string name, string text, Color color, Transform parent, Vector3 position, float height, WorldText.Look look, bool shown = false, float width = 0, float? lineSpacing = null, bool sparks = true, float billboard = 0)
        {
            var root = Node("Card " + name, parent, position);
            var label = WorldText.Card("Text", text, color, root.transform, Vector3.zero, height, look == WorldText.Look.Panel ? cardBacking : null, width, lineSpacing);
            if (look == WorldText.Look.Glow)
            {
                WorldText.Glow(label, WordMaterial(color));
                if (sparks) Sparks(root.transform, color, Mathf.Max(0.2f, height));
            }
            if (billboard > 0) WorldText.Billboard(root.transform, billboard);
            Placeholder(root, "Styled floating card: " + text.Replace("\n", " / "));
            root.SetActive(shown);
            return Ref(root);
        }

        // Scene 4's word material: TMP Distance Field with an HDR face and outer glow, one asset per colour. The face sits
        // just over the bloom threshold (1.5x, was 2.6x) and the glow is tighter, so the palette hue survives bloom and
        // ACES (VOLTAGE stays blue, not white) and thin stems stay letters (the sans "I" bloomed into a pill).
        protected Material WordMaterial(Color color)
        {
            if (!palette.font) return null;
            Directory.CreateDirectory(Art + "/Materials");
            var material = Asset(Art + "/Materials/Word_" + ColorUtility.ToHtmlStringRGB(color) + ".mat", () => new Material(palette.font.material));
            material.shader = Shader.Find("TextMeshPro/Distance Field");
            material.SetTexture(ShaderUtilities.ID_MainTex, palette.font.atlasTexture);
            material.SetColor(ShaderUtilities.ID_FaceColor, Hdr(color, 1.5f));
            material.EnableKeyword("GLOW_ON");
            material.SetColor(ShaderUtilities.ID_GlowColor, new Color(color.r, color.g, color.b, 0.5f));
            material.SetFloat(ShaderUtilities.ID_GlowOuter, 0.28f);
            material.SetFloat(ShaderUtilities.ID_GlowPower, 0.6f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Scene 4's spark burst when a word appears: 60 additive sparks from a sphere, once per activation.
        protected ParticleSystem Sparks(Transform parent, Color color, float radius) =>
            Emitter("Sparks", parent, Vector3.zero, Glow("Glow_Spark_" + ColorUtility.ToHtmlStringRGB(color), Hdr(color, 4f)), ps =>
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
                shape.radius = radius;
            });

        protected GameObject Capsule(string name, Transform parent, Vector3 position, string note, float height = 1.6f)
        {
            var go = Prim(PrimitiveType.Capsule, name, parent, position + Vector3.up * (height / 2), new Vector3(0.45f, height / 2, 0.45f), placeholderMat);
            Placeholder(go, note);
            WorldText.Card("Name", name, Color.white, go.transform, new Vector3(0, 1.3f, 0), WorldText.Cap.Name).transform.localScale = new Vector3(1 / 0.45f, 1 / (height / 2), 1 / 0.45f);
            return go;
        }

        // The four kids: project models when present. Without models the kids are the camera's point of view, so
        // nothing is placed (stand-ins in front of the lens blocked every bench shot).
        protected void KidsAt(Vector3 centre, Vector3 facing, Transform parent = null)
        {
            bool any = false;
            for (int i = 0; i < Kids.Length; i++)
            {
                var go = BlockoutAssets.KidModel(Kids[i], scene);
                if (!go) continue;
                any = true;
                go.transform.position = centre + Vector3.Cross(Vector3.up, facing).normalized * (i - 1.5f) * 0.55f;
                go.transform.rotation = Quaternion.LookRotation(facing);
                if (parent) go.transform.SetParent(parent, true);
            }
            if (!any) Debug.Log(SceneId + ": no student models in the project; the kids are the camera POV, no stand-ins placed.");
        }

        // Georg Ohm: the project model with the Scene 4 work loop, or a capsule.
        protected GameObject Ohm(Vector3 feet, Vector3 facing, Transform notebook, Transform galvanometer, Transform wirePost)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(OhmModel);
            if (!model) return Capsule("Georg Ohm", null, feet, "Georg Ohm model, working at his bench", 1.75f);
            var ohm = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            ohm.name = "Georg Ohm";
            ohm.transform.position = feet;
            ohm.transform.rotation = Quaternion.LookRotation(facing);
            ohm.transform.localScale = Vector3.one * 1.55f;
            var worker = ohm.AddComponent<OhmWorker>();
            worker.notebook = notebook;
            worker.galvanometer = galvanometer;
            worker.wirePost = wirePost;
            return Ref(ohm);
        }

        // Wattson: the project model with the shared controller, or a glowing sphere with a label.
        // Wattson: Scene 4's finished variant (controller, Ohm-niscope, spark trail, follow light, voice sources),
        // or a glowing sphere with a label when the prefab is missing.
        protected WattsonController Wattson(Vector3 position, Vector3 lookAt)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Scene04/Prefabs/Wattson_Scene04.prefab");
            WattsonController w;
            GameObject go;
            if (prefab)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                w = go.GetComponent<WattsonController>();
                w.path = null;
                w.pathT = 0;
                voice = w.voice; // the Timeline's VO track plays through his own mouth
                if (w.sparks)
                {
                    // A shorter, slower spark trail than Scene 4's flights: the sparks stay on his body instead of drifting
                    // onto the board or the cards beside him.
                    var main = w.sparks.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.18f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(w.sparks);
                }
            }
            else
            {
                go = Prim(PrimitiveType.Sphere, "Wattson", null, Vector3.zero, Vector3.one * 0.3f, chargeMat);
                WorldText.Card("Name", "WATTSON", palette.wattsonAccent, go.transform, new Vector3(0, 0.9f, 0), 0.04f).transform.localScale = Vector3.one / 0.3f;
                Placeholder(go, "Wattson model");
                w = go.AddComponent<WattsonController>();
                w.visual = go.transform;
                w.voice = voice;
                w.liveVoice = Audio("Voice (live answers)", go.transform, AudioBus.Kind.Voice);
                w.sfx = sfx;
                w.chirp = sounds["Chirp"];
            }
            go.name = "Wattson";
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(lookAt - position, Vector3.up).normalized);
            w.lookTarget = camera ? camera.transform : null;
            Ref(go);
            return wattson = w;
        }

        protected AudioSource Audio(string name, Transform parent, AudioBus.Kind kind)
        {
            var source = Node(name, parent).AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            var bus = source.gameObject.AddComponent<AudioBus>();
            bus.kind = kind;
            return source;
        }

        protected void Materials()
        {
            grey = BlockoutAssets.Mat("Greybox", new Color(0.55f, 0.55f, 0.57f));
            dark = BlockoutAssets.Mat("GreyboxDark", new Color(0.2f, 0.2f, 0.22f));
            placeholderMat = BlockoutAssets.Mat("Placeholder", new Color(0.72f, 0.72f, 0.75f));
            magenta = BlockoutAssets.Mat("VfxMagenta", new Color(1f, 0.1f, 1f), emissive: true);
            cardBacking = BlockoutAssets.Mat("CardBacking", WorldText.PanelColor); // palette panel at 0.92 alpha; the rounded sprite takes its colour from this
            chargeMat = BlockoutAssets.Mat("Charge", palette.charge, emissive: true);
        }

        protected void GreyLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.52f);
            var sun = Node("Sun", null, Vector3.up * 5).AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            sun.shadows = LightShadows.Soft;
        }

        // Floor grid: a plane with a line every metre.
        protected void FloorGrid(Vector3 centre, float size, float y = 0)
        {
            var root = Ref(Node("Floor Grid", null, centre + Vector3.up * y));
            Prim(PrimitiveType.Plane, "Floor", root.transform, Vector3.zero, Vector3.one * (size / 10f), dark);
            for (float i = -size / 2; i <= size / 2; i += 1f)
            {
                Prim(PrimitiveType.Cube, "Grid X", root.transform, new Vector3(i, 0.005f, 0), new Vector3(0.015f, 0.005f, size), grey);
                Prim(PrimitiveType.Cube, "Grid Z", root.transform, new Vector3(0, 0.005f, i), new Vector3(size, 0.005f, 0.015f), grey);
            }
            Placeholder(root, "Environment: " + Title);
        }

        // ------------------------------------------------------------------ cameras

        void MainCamera()
        {
            var go = Node("Main Camera", null, new Vector3(0, 1.5f, -4));
            go.tag = "MainCamera";
            camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            camera.nearClipPlane = 0.05f;
            camera.allowHDR = true;
            go.AddComponent<AudioListener>();
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var brain = go.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0); // blends come from clip overlaps
            Lenses();
            Ref(go); // "Main Camera": what Wattson looks at to face the learner
        }

        protected CinemachineCamera Cam(string name, Vector3 position, Vector3 lookAt, float fov = 40, Transform track = null)
        {
            var go = Ref(Node("Cam " + name, null, position));
            go.transform.rotation = Quaternion.LookRotation(lookAt - position);
            var cam = go.AddComponent<CinemachineCamera>();
            var lens = LensSettings.Default;
            lens.FieldOfView = fov;
            lens.NearClipPlane = 0.05f;
            lens.FarClipPlane = 80f;
            cam.Lens = lens;
            cam.Priority = 0;
            if (track) { cam.Target.TrackingTarget = track; go.AddComponent<CinemachineRotationComposer>(); }
            cams[name] = cam;
            return cam;
        }

        // ------------------------------------------------------------------ timeline

        void Timeline()
        {
            var timeline = Data<TimelineAsset>(Folder + "/Timeline/" + SceneId + "_Timeline.playable");
            foreach (var old in timeline.GetRootTracks().ToArray()) timeline.DeleteTrack(old);
            if (timeline.markerTrack) foreach (var old in timeline.markerTrack.GetMarkers().ToArray()) timeline.markerTrack.DeleteMarker(old);
            timeline.editorSettings.frameRate = 30;
            playable.playableAsset = timeline;
            playable.playOnAwake = true;
            playable.extrapolationMode = DirectorWrapMode.Hold;

            // Cameras: one clip per shot, held until the next shot. The last one runs past the Timeline's end (clock + 1):
            // Hold parks the playhead exactly on the end, where a clip ending there is no longer live, and the brain fell
            // back to the entry camera under Scene 13's end card.
            var cameraTrack = timeline.CreateTrack<CinemachineTrack>(null, "Cameras");
            playable.SetGenericBinding(cameraTrack, camera.GetComponent<CinemachineBrain>());
            var shots = beats.Where(b => b.kind == Kind.Camera).OrderBy(b => b.at).ToList();
            TimelineClip previous = null;
            for (int i = 0; i < shots.Count; i++)
            {
                var cam = cams[shots[i].id];
                var clip = cameraTrack.CreateClip<CinemachineShot>();
                clip.displayName = shots[i].id;
                clip.start = shots[i].at;
                double next = i + 1 < shots.Count ? shots[i + 1].at : clock + 2;
                // Blended shots overlap the next one by a second; a cut ends exactly where the next begins.
                double overlap = i + 1 < shots.Count && !shots[i + 1].cut ? Math.Min(1.0, next - shots[i].at) : 0;
                clip.duration = next - shots[i].at + overlap;
                var shot = (CinemachineShot)clip.asset;
                shot.VirtualCamera = new ExposedReference<CinemachineVirtualCameraBase> { exposedName = shots[i].id };
                playable.SetReferenceValue(shots[i].id, cam);
                if (previous != null && previous.end > clip.start) { clip.blendInDuration = previous.end - clip.start; previous.blendOutDuration = clip.blendInDuration; }
                if (i == 0) { cam.Priority = 10; director.entryCamera = cam; }
                previous = clip;
            }

            // Wattson gestures, held until the next gesture.
            if (wattson)
            {
                var gestures = timeline.CreateTrack<WattsonTrack>(null, "Wattson");
                playable.SetGenericBinding(gestures, wattson);
                var poses = beats.Where(b => b.kind == Kind.Wattson).OrderBy(b => b.at).ToList();
                for (int i = 0; i < poses.Count; i++)
                {
                    var clip = gestures.CreateClip<WattsonClip>();
                    clip.displayName = poses[i].gesture.ToString();
                    clip.start = poses[i].at;
                    clip.duration = Math.Max(0.1, (i + 1 < poses.Count ? poses[i + 1].at : clock + 1) - poses[i].at);
                    var pose = (WattsonClip)clip.asset;
                    pose.gesture = poses[i].gesture;
                    pose.gadgetOut = poses[i].gadget;
                    pose.lookAt = Exposed("look", poses[i].look);
                    pose.pointAt = Exposed("point", poses[i].point);
                    pose.moveTo = Exposed("move", poses[i].moveTo);
                }
            }

            // Dialogue: subtitles always; the VO track plays the clip when the line has one.
            var subtitles = timeline.CreateTrack<SubtitleTrack>(null, "Subtitles");
            var vo = timeline.CreateTrack<AudioTrack>(null, "VO");
            playable.SetGenericBinding(vo, voice);
            foreach (var beat in beats.Where(b => b.kind == Kind.Line))
            {
                var line = lines[beat.id];
                var clip = subtitles.CreateClip<SubtitleClip>();
                clip.displayName = line.id + "  " + line.text;
                clip.start = beat.at;
                clip.duration = line.Duration;
                ((SubtitleClip)clip.asset).line = line;
                if (!line.vo) continue;
                var spoken = vo.CreateClip(line.vo);
                spoken.start = beat.at;
                spoken.duration = line.vo.length;
            }

            // Cues and gates on the Timeline's own marker track: its notifications go to the INotificationReceivers
            // on the director's GameObject, which is where the SceneDirector lives.
            timeline.CreateMarkerTrack();
            var markers = timeline.markerTrack;
            foreach (var beat in beats.Where(b => b.kind == Kind.Cue))
            {
                var cue = markers.CreateMarker<Cue>(beat.at);
                cue.command = beat.id;
                cue.note = beat.text;
            }
            // Visual beats leave a "note" marker so Docs/FLOW.md can list them; the SceneDirector ignores it.
            foreach (var beat in beats.Where(b => b.kind == Kind.Visual && !string.IsNullOrEmpty(b.text)))
            {
                var cue = markers.CreateMarker<Cue>(beat.at);
                cue.command = "note " + beat.length.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                cue.note = beat.text;
            }
            foreach (var beat in beats.Where(b => b.kind == Kind.Gate))
            {
                var gate = markers.CreateMarker<InteractionGate>(beat.at);
                gate.gateId = beat.id;
                gate.description = beat.text;
                gate.lines = beat.gateLines;
            }

            // Hand-off signal at the end.
            var receiver = playable.gameObject.AddComponent<SignalReceiver>();
            var signals = timeline.CreateTrack<SignalTrack>(null, "Signals");
            playable.SetGenericBinding(signals, receiver);
            foreach (var beat in beats.Where(b => b.kind == Kind.Handoff))
            {
                var asset = Data<SignalAsset>(Folder + "/Timeline/" + beat.id + ".signal");
                signals.CreateMarker<SignalEmitter>(beat.at).asset = asset;
                var reaction = new UnityEvent();
                UnityEventTools.AddPersistentListener(reaction, beat.cut ? director.CutToNext : (UnityAction)director.Complete);
                receiver.AddReaction(asset, reaction);
            }
            // Timeline length: a little past the last beat so Hold keeps the final frame.
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = clock + 1.0;
            EditorUtility.SetDirty(timeline);
        }

        ExposedReference<Transform> Exposed(string prefix, string refName)
        {
            if (string.IsNullOrEmpty(refName) || !refs.TryGetValue(refName, out var go)) return default;
            var reference = new ExposedReference<Transform> { exposedName = prefix + " " + refName };
            playable.SetReferenceValue(reference.exposedName, go.transform);
            return reference;
        }

        void ModuleRoot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Shared + "/ModuleRoot.prefab");
            if (!prefab) { Debug.LogWarning("ModuleRoot.prefab is missing; build Scene 04 first."); return; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var flow = instance.GetComponent<ModuleFlow>();
            flow.scenes = BlockoutAssets.SceneOrder;
            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
        }

        void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ report

        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{SceneName} built: {ScenePath}  ({clock:0.0} s)");
            foreach (var b in beats.Where(b => b.kind == Kind.Line)) sb.AppendLine($"  {b.at,6:0.0}s  {b.length,5:0.0}s  {b.id}  {(lines[b.id].vo ? "" : "(silent, fallback)")}");
            foreach (var b in beats.Where(b => b.kind == Kind.Gate)) sb.AppendLine($"  {b.at,6:0.0}s  GATE  {b.id}: {b.text}");
            foreach (var b in beats.Where(b => b.kind == Kind.Handoff)) sb.AppendLine($"  {b.at,6:0.0}s  HANDOFF {b.id} ({b.text})");
            return sb.ToString();
        }

        public IReadOnlyList<Beat> PlannedBeats => beats;
        public IReadOnlyDictionary<string, DialogueLine> Lines => lines;
        public double Length => clock;
    }
}
