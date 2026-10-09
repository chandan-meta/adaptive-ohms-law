using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OhmsLaw.Editor
{
    // Patch the saved scenes, including their hand edits. Never rebuild the introductory scenes.
    public static class CinematicPolish
    {
        const string Art = "Assets/OhmsLaw/Shared/Art/Cinematic";
        static readonly Color Blue = new Color(0.12f, 0.55f, 0.85f);
        static readonly Color Amber = new Color(1f, 0.56f, 0.22f);

        [MenuItem("Ohm's Law/Polish/Create audio routing")]
        static void CreateMixer()
        {
            const string path = "Assets/OhmsLaw/Resources/OhmsLawMix.mixer";
            Directory.CreateDirectory("Assets/OhmsLaw/Resources");
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(path)) return;
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
            var mixer = type.GetMethod("CreateMixerControllerAtPath", flags).Invoke(null, new object[] { path });
            var master = type.GetProperty("masterGroup", flags).GetValue(mixer);
            foreach (var name in new[] { "Voice", "Music", "SFX", "UI", "Ambience" })
            {
                var group = type.GetMethod("CreateNewGroup", flags).Invoke(mixer, new object[] { name, true });
                type.GetMethod("AddChildToParent", flags).Invoke(mixer, new[] { group, master });
            }
            EditorUtility.SetDirty((Object)mixer);
            AssetDatabase.SaveAssets();
            Debug.Log("Created unity-gain Voice / Music / SFX / UI / Ambience mixer; existing volume controls and ducking preserved.");
        }

        [MenuItem("Ohm's Law/Polish/Apply later scenes")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play mode before patching scenes.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            var report = new StringBuilder("Cinematic polish: existing scenes patched in place\n");
            try
            {
                foreach (var path in Directory.GetFiles("Assets/OhmsLaw/Scenes", "Scene*.unity").OrderBy(p => p))
                {
                    if (!int.TryParse(Path.GetFileName(path).Substring(5, 2), out int id) || id < 5 || id > 13) continue;
                    var scene = EditorSceneManager.OpenScene(path);
                    var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                    var wire = all.Select(t => t.GetComponent<WireInterior>()).FirstOrDefault(w => w);
                    foreach (var renderer in all.Select(t => t.GetComponent<Renderer>()).Where(r => r && !(r is ParticleSystemRenderer)))
                    {
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(PolishMaterial).ToArray();
                        Persist(renderer);
                    }
                    foreach (var volume in all.Select(t => t.GetComponent<Volume>()).Where(v => v && v.sharedProfile))
                    {
                        if (!volume.sharedProfile.name.Contains("Global")) continue;
                        var original = volume.sharedProfile;
                        string destination = Art + "/" + original.name.Replace("_Polished", "") + "_Polished.asset";
                        if (!AssetDatabase.LoadAssetAtPath<VolumeProfile>(destination)) AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), destination);
                        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(destination);
                        volume.sharedProfile = profile;
                        bool electric = profile.name.Contains("Electron");
                        if (profile.TryGet<Bloom>(out var bloom)) { bloom.intensity.Override(electric ? 0.48f : 0.3f); bloom.threshold.Override(1.1f); bloom.scatter.Override(0.62f); }
                        if (profile.TryGet<FilmGrain>(out var grain)) grain.intensity.Override(0.025f);
                        if (profile.TryGet<Vignette>(out var vignette)) vignette.intensity.Override(electric ? 0.22f : 0.18f);
                        if (profile.TryGet<ColorAdjustments>(out var colour)) { colour.contrast.Override(8f); colour.saturation.Override(-5f); }
                        EditorUtility.SetDirty(profile);
                    }
                    if (wire) PolishWire(wire);
                    foreach (var wattson in all.Select(t => t.GetComponent<WattsonController>()).Where(w => w))
                    {
                        // Mesh bounds follow the original desks during the Scene 8 transformation.
                        wattson.furniture = all.Where(t => t.name == "Bench" || t.name == "Old Table" || t.name == "Study Desk")
                            .SelectMany(t => t.GetComponentsInChildren<Renderer>(true))
                            .Where(r => r is MeshRenderer && !r.name.Contains("Shadow")).Distinct().ToArray();
                        wattson.conversationCamera = all.Where(t => t.name == (id == 7 ? "Cam Study Wide" : "Cam Bench Wide"))
                            .Select(t => t.GetComponent<Unity.Cinemachine.CinemachineCamera>()).FirstOrDefault(c => c);
                        Persist(wattson);
                    }
                    foreach (var ohm in all.Select(t => t.GetComponent<OhmWorker>()).Where(o => o))
                    { ohm.naturalPerformance = true; ohm.blendSpeed = 2.2f; Persist(ohm); }
                    foreach (var light in all.Select(t => t.GetComponent<Light>()).Where(l => l))
                    {
                        if (light.name == "Window Key") light.color = new Color(0.79f, 0.88f, 1f);
                        if (light.name == "Ceiling Fill") light.color = new Color(1f, 0.88f, 0.72f);
                        if (light.name == "Bench Light") { light.intensity = 2.1f; light.color = new Color(1f, 0.88f, 0.72f); }
                        if (light.name == "Rim") light.color = new Color(1f, 0.74f, 0.46f);
                        if (light.shadows != LightShadows.None) { light.shadowBias = 0.035f; light.shadowNormalBias = 0.25f; }
                        Persist(light);
                    }
                    // LabLook contains the crossfade's source levels: keep those consistent with the lights.
                    foreach (var director in all.Select(t => t.GetComponent<SceneDirector>()).Where(d => d))
                        foreach (var look in director.looks)
                        {
                            look.tintFrom = PolishMaterial(look.tintFrom);
                            look.tintTo = PolishMaterial(look.tintTo);
                            for (int i = 0; i < look.lights.Length && i < look.intensities.Length; i++)
                                if (look.lights[i] && look.lights[i].name == "Bench Light") look.intensities[i] = 2.1f;
                        }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.AppendLine(path + " | wire=" + (wire != null) + " | preserved original transforms, Timeline, dialogue and apparatus");
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText("Logs/CinematicPolish/applied.txt", report.ToString());
                Debug.Log(report.ToString());
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        static void Persist(Component component)
        {
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        static Material PolishMaterial(Material source)
        {
            if (!source || source.name.EndsWith("_Polished")) return source;
            string name = source.name;
            if (!(name.StartsWith("Classroom_") || name.StartsWith("Wire_"))) return source;
            if (!source.HasProperty("_Smoothness")) return source;
            string path = Art + "/" + name + "_Polished.mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!copy) { copy = new Material(source); AssetDatabase.CreateAsset(copy, path); }
            copy.enableInstancing = true;
            if (name.Contains("Floor")) copy.SetFloat("_Smoothness", name.StartsWith("Wire") ? 0.55f : 0.32f);
            if (name.Contains("Steel")) { copy.SetFloat("_Metallic", 0.72f); copy.SetFloat("_Smoothness", 0.56f); }
            if (name == "Classroom_BenchTop") { copy.SetColor("_BaseColor", new Color(0.72f, 0.69f, 0.61f)); copy.SetFloat("_Smoothness", 0.32f); }
            if (name == "Classroom_Wall") copy.SetColor("_BaseColor", new Color(0.48f, 0.53f, 0.57f));
            if (name == "Classroom_Wainscot") copy.SetColor("_BaseColor", new Color(0.22f, 0.30f, 0.38f));
            if (name == "Classroom_Board") copy.SetColor("_BaseColor", new Color(0.065f, 0.135f, 0.12f));
            if (name.Contains("Conduit"))
            {
                copy.shader = Shader.Find("OhmsLaw/WireGlass");
                copy.SetColor("_EmissionColor", new Color(0.1f, 0.5f, 0.8f));
                copy.renderQueue = 3000;
            }
            EditorUtility.SetDirty(copy);
            return copy;
        }

        static void PolishWire(WireInterior wire)
        {
            wire.fogDensity = 0.035f;
            wire.fogColor = new Color(0.07f, 0.12f, 0.2f);
            wire.background = wire.fogColor;
            wire.ambient = new Color(0.09f, 0.14f, 0.22f);
            wire.carrierSize = 0.22f;
            wire.conduitColor.a = 0.075f;
            foreach (var trail in wire.trails) if (trail) { trail.widthMultiplier = 0.035f; trail.numCapVertices = 3; }
            var wall = Material("Chamber navy", new Color(0.2f, 0.3f, 0.45f), 0.45f);
            var inset = Material("Chamber inset", new Color(0.27f, 0.4f, 0.55f), 0.38f);
            var blue = Material("Chamber cyan", Blue * 2.2f, 0.5f, Blue * 2.2f);
            var amber = Material("Chamber amber", Amber * 2f, 0.4f, Amber * 2f);
            foreach (var light in wire.GetComponentsInChildren<Light>(true))
                if (light.name == "Space Key") light.intensity = 0.9f;
            var floor = wire.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "Floor");
            if (floor) { floor.sharedMaterial.SetFloat("_Smoothness", 0.75f); EditorUtility.SetDirty(floor.sharedMaterial); }
            if (wire.transform.Find("Chamber Depth") is Transform existing)
            {
                foreach (var r in existing.GetComponentsInChildren<MeshRenderer>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic);
                    r.gameObject.layer = 4;
                    if (r.name == "Back wall") { r.transform.localPosition = new Vector3(0, 28.3f, 19); r.transform.localScale = new Vector3(180, 60, 0.4f); }
                    int i = Mathf.RoundToInt((r.transform.localPosition.x + 19) / 3.8f);
                    var p = r.transform.localPosition; var size = r.transform.localScale;
                    if (r.name == "Recessed pier") { size.y = 9f + (i % 3) * 2.5f; p.y = -1.7f + size.y / 2; }
                    if (r.name == "Inset panel") { p.y = 3.3f; size.y = 6.3f; }
                    if (r.name == "Recess light") { p.y = 2.3f + (i % 3) * 1.2f; size.y = 4.5f; }
                    if (r.name == "Panel seam") p.y = 6.7f;
                    r.transform.localPosition = p; r.transform.localScale = size;
                }
                foreach (var light in existing.GetComponentsInChildren<Light>()) { light.intensity = 18; light.range = 20; }
                return;
            }
            var root = new GameObject("Chamber Depth").transform;
            root.SetParent(wire.transform, false);
            // Behind every teaching object: simple layered surfaces, no machines or scientific claims.
            Box("Back wall", root, new Vector3(0, 28.3f, 19), new Vector3(180, 60, 0.4f), wall);
            for (int i = 0; i < 11; i++)
            {
                float x = -19 + i * 3.8f, h = 9f + (i % 3) * 2.5f, z = 9.5f + (i % 2) * 5f;
                Box("Recessed pier", root, new Vector3(x, -1.7f + h / 2, z), new Vector3(1.8f, h, 0.9f), wall);
                Box("Inset panel", root, new Vector3(x, 3.3f, z - 0.49f), new Vector3(1.38f, 6.3f, 0.06f), inset);
                Box("Recess light", root, new Vector3(x - 0.8f, 2.3f + (i % 3) * 1.2f, z - 0.51f), new Vector3(0.045f, 4.5f, 0.055f), i % 3 == 0 ? amber : blue);
                Box("Panel seam", root, new Vector3(x, 6.7f, z - 0.53f), new Vector3(1.4f, 0.018f, 0.03f), blue);
            }
            foreach (float x in new[] { -8f, 8f })
            {
                var go = new GameObject("Chamber wash");
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(x, 3, 6);
                var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = Blue;
                l.intensity = 18; l.range = 20; l.shadows = LightShadows.None;
            }
        }

        static Material Material(string name, Color colour, float smoothness, Color? emission = null)
        {
            string path = Art + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", colour); material.SetFloat("_Smoothness", smoothness); material.SetFloat("_Metallic", 0.35f);
            material.enableInstancing = true;
            if (emission.HasValue) { material.shader = Shader.Find("Universal Render Pipeline/Unlit"); material.SetColor("_BaseColor", emission.Value); }
            EditorUtility.SetDirty(material);
            return material;
        }

        static void Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.layer = 4; // existing static wire set reflection layer
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        [MenuItem("Ohm's Law/Polish/Build Windows player")]
        public static void BuildPlayer()
        {
            Directory.CreateDirectory("Builds/CinematicPolish");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled && s.path.StartsWith("Assets/OhmsLaw/Scenes/") && !s.path.Contains("FrameworkTest")).Select(s => s.path).ToArray(),
                locationPathName = "Builds/CinematicPolish/OhmsLaw.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            File.WriteAllText("Logs/CinematicPolish/build.txt", result.summary.result + "\nErrors: " + result.summary.totalErrors + "\nBytes: " + result.summary.totalSize);
            if (result.summary.totalErrors > 0) throw new Exception("Windows build failed");
        }

        [MenuItem("Ohm's Law/Polish/Regression checks")]
        public static void Checks()
        {
            WattsonPoseTest.Run();
            WattsonPoseTest.RunOhm();
            WattsonActionsTests.Run(message => Debug.Log(message));
            WireChecks.Run();
            BakeBlockoutVO.SelfCheck();
            LabContinuityTest.Run();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in Directory.GetFiles("Assets/OhmsLaw/Scenes", "Scene*.unity").OrderBy(p => p))
                {
                    if (!int.TryParse(Path.GetFileName(path).Substring(5, 2), out int id) || id < 7 || id > 13) continue;
                    var scene = EditorSceneManager.OpenScene(path);
                    foreach (var w in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WattsonController>(true)))
                        if (w.furniture.Length == 0) throw new Exception(path + ": furniture overrides were not saved");
                    foreach (var o in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<OhmWorker>(true)))
                        if (!o.naturalPerformance) throw new Exception(path + ": performance override was not saved");
                    Debug.Log("PASS saved character overrides " + Path.GetFileNameWithoutExtension(path));
                }
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
            if (!ExperimentData.Main || !ExperimentData.Main.Consistent()) throw new Exception("Scientific readings disagree");
            // A route with endpoints outside the table must still clear the support during a slow frame.
            var preview = new PreviewRenderUtility();
            try
            {
                var w = preview.InstantiatePrefabInScene(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OhmsLaw/Scene04/Prefabs/Wattson_Scene04.prefab")).GetComponent<WattsonController>();
                var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
                preview.AddSingleGO(table);
                table.transform.position = new Vector3(0, 0.4f, 0);
                table.transform.localScale = new Vector3(2, 0.8f, 1);
                w.furniture = new[] { table.GetComponent<Renderer>() };
                w.Simulate(0); // PreviewRenderUtility does not call Awake; initialize before issuing movement.
                var mark = new GameObject("Clearance target"); preview.AddSingleGO(mark);
                mark.transform.position = new Vector3(-2, 1.2f, 0);
                w.Warp(mark.transform);
                mark.transform.position = new Vector3(2, 0.3f, 0);
                w.MoveTo(mark.transform);
                for (int i = 0; i < 150; i++)
                {
                    w.Simulate(i == 25 ? 0.5f : 1f / 60f);
                    if (Mathf.Abs(w.transform.position.x) < 1.1f && w.transform.position.y < 0.86f)
                        throw new Exception("Wattson entered the table volume");
                    if (!float.IsFinite(w.transform.position.y)) throw new Exception("Unstable movement spring");
                }
                if (Vector3.Distance(w.transform.position, mark.transform.position) > 0.15f) throw new Exception("Wattson never reached the destination: " + w.transform.position + " -> " + mark.transform.position);
                Debug.Log("PASS furniture clearance: swept desk crossing, frame hitch, and destination settling");
            }
            finally { preview.Cleanup(); }
            Debug.Log("PASS cinematic regression checks completed; review the individual checks above");
        }
    }
}
