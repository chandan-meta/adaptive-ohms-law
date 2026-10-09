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

        [MenuItem("Ohm's Law/Polish/Apply wire chamber (Scenes 5, 6)")]
        public static void ApplyWire() => Apply(new[] { 5, 6 });

        [MenuItem("Ohm's Law/Polish/Apply later scenes")]
        public static void Apply() => Apply(null);

        static void Apply(int[] only)
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
                    if (!int.TryParse(Path.GetFileName(path).Substring(5, 2), out int id) || id < 5 || id > 13 || (only != null && !only.Contains(id))) continue;
                    var scene = EditorSceneManager.OpenScene(path);
                    var wire = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WireInterior>(true)).FirstOrDefault();
                    if (wire) PolishWire(wire); // first: it rebuilds the chamber, so the list below holds no destroyed objects
                    var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
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
                        if (profile.TryGet<Bloom>(out var bloom)) { bloom.intensity.Override(electric ? 0.75f : 0.3f); bloom.threshold.Override(1.1f); bloom.scatter.Override(electric ? 0.7f : 0.62f); }
                        if (profile.TryGet<FilmGrain>(out var grain)) grain.intensity.Override(0.025f);
                        if (profile.TryGet<Vignette>(out var vignette)) vignette.intensity.Override(electric ? 0.22f : 0.18f);
                        if (profile.TryGet<ColorAdjustments>(out var colour)) { colour.contrast.Override(8f); colour.saturation.Override(-5f); }
                        EditorUtility.SetDirty(profile);
                    }
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

        // The electron space to the neon reference (Docs/Polish/References, 2026-10-09 brief): a field of tall dark pillars
        // in three rows with bright cyan (a few amber) light strips, a glossy dark floor that mirrors them, a lit horizon
        // line at the back, haze. Rebuilt in full on every apply (the old dressing goes), then the floor probe re-bakes.
        static void PolishWire(WireInterior wire)
        {
            wire.fogDensity = 0.016f;
            wire.fogColor = new Color(0.035f, 0.075f, 0.17f); // deeper navy: the floor stays black glass into the distance
            wire.background = wire.fogColor;
            wire.ambient = new Color(0.04f, 0.075f, 0.15f);
            wire.carrierSize = 0.22f;
            wire.conduitColor.a = 0.075f;
            foreach (var trail in wire.trails) if (trail) { trail.widthMultiplier = 0.035f; trail.numCapVertices = 3; }
            foreach (var light in wire.GetComponentsInChildren<Light>(true))
                if (light.name == "Space Key") light.intensity = 0.3f;
            var floor = wire.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "Floor");
            if (floor)
            {
                // Black glass: opaque (a see-through floor rendered as a flat cyan plane during the gate), with the strips'
                // reflections drawn as streaks on it (URP has no planar reflection; the probe gives only a blurred sheen).
                var m = floor.sharedMaterial;
                m.SetFloat("_Surface", 0); m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                m.SetFloat("_ZWrite", 1); m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = -1;
                m.SetColor("_BaseColor", new Color(0.012f, 0.02f, 0.045f, 1f));
                m.SetFloat("_Metallic", 0.1f);
                m.SetFloat("_Smoothness", 0.8f);
                m.SetColor("_EmissionColor", new Color(0.31f, 0.82f, 1f) * 0.1f); // the seams, faint
                EditorUtility.SetDirty(m);
            }
            foreach (var t in wire.GetComponentsInChildren<Transform>(true)) if (t.name == "Silhouette") t.gameObject.SetActive(false); // the builder's flat slabs: the pillars replace them

            if (wire.transform.Find("Chamber Depth") is Transform old) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Chamber Depth").transform;
            root.SetParent(wire.transform, false);
            var pillar = Material("Chamber navy", new Color(0.05f, 0.075f, 0.12f), 0.62f);
            pillar.SetFloat("_Metallic", 0.55f);
            var cap = Material("Chamber inset", new Color(0.14f, 0.2f, 0.3f), 0.5f);
            var cyan = Material("Chamber cyan", Blue * 5.5f, 0.5f, Blue * 5.5f);
            var amber = Material("Chamber amber", Amber * 4f, 0.4f, Amber * 4f);
            var cyanMirror = Additive("Chamber cyan mirror", new Color(Blue.r * 1.4f, Blue.g * 1.4f, Blue.b * 1.4f, 0.55f), soft: true);
            var amberMirror = Additive("Chamber amber mirror", new Color(Amber.r, Amber.g, Amber.b, 0.45f), soft: true);
            // A light strip and its reflection on the floor: a mirror would show the strip's image from its base toward the
            // viewer's foot, D·h/(c+h) long (D the distance to the camera's foot, h the strip's top, c the eye 1 m up), so a
            // dim streak lies there, pointed at the camera of the "Wire Inside" shot.
            var shot = wire.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "Wire Inside");
            Vector3 foot = shot ? wire.transform.InverseTransformPoint(shot.position) : new Vector3(-2.6f, -0.7f, -8.7f);
            float eye = foot.y + 1.7f;
            foot.y = -1.7f;
            void Strip(Vector3 at, Vector3 size, bool warm)
            {
                Box("Strip", root, at, size, warm ? amber : cyan);
                var baseAt = new Vector3(at.x, -1.7f, at.z);
                float top = at.y + size.y / 2 + 1.7f;
                Vector3 toFoot = foot - baseAt;
                float length = toFoot.magnitude * top / (eye + top);
                var mid = baseAt + toFoot.normalized * (length * 0.42f) + Vector3.up * 0.006f; // the soft quad fades at both ends: its far end reaches the base
                var streak = GameObject.CreatePrimitive(PrimitiveType.Quad); // soft radial texture stretched along the view: a blurred reflection, bright at the base
                streak.name = "Strip reflection"; streak.transform.SetParent(root, false);
                streak.transform.localPosition = mid; streak.transform.localRotation = Quaternion.LookRotation(toFoot) * Quaternion.Euler(90, 0, 0); streak.transform.localScale = new Vector3(size.x * 3.2f, length * 1.15f, 1f);
                streak.layer = 4; Object.DestroyImmediate(streak.GetComponent<Collider>());
                var sr = streak.GetComponent<Renderer>(); sr.sharedMaterial = warm ? amberMirror : cyanMirror; sr.shadowCastingMode = ShadowCastingMode.Off;
            }
            // Three rows (near 7 m, mid 11 m, far 16 m behind the conduit), heights and widths from the index so an apply is
            // repeatable; the near row leaves the centre open behind the conduit. Each pillar: body, a lip on top, one or two
            // strips down a front edge, every seventh one amber.
            int n = 0;
            foreach (var (z, step, h0) in new[] { (7f, 4.2f, 5.5f), (11f, 3.4f, 8f), (16f, 2.8f, 11f) })
                for (float x = -22f; x <= 24f; x += step, n++)
                {
                    if (z < 8f && Mathf.Abs(x) < 6.5f) continue;
                    float h = h0 + (n * 7 % 11) * 0.7f, w = 1.0f + (n * 5 % 7) * 0.22f, d = w * 0.8f;
                    float xj = x + (n * 3 % 5 - 2) * 0.25f;
                    Box("Pillar", root, new Vector3(xj, -1.7f + h / 2, z), new Vector3(w, h, d), pillar);
                    Box("Pillar lip", root, new Vector3(xj, -1.7f + h + 0.06f, z), new Vector3(w + 0.14f, 0.12f, d + 0.14f), cap);
                    bool warm = n % 7 == 3;
                    float sh = h * (0.55f + (n * 2 % 4) * 0.1f);
                    int edge = n % 2 == 0 ? -1 : 1;
                    Strip(new Vector3(xj + edge * (w / 2 - 0.08f), -1.7f + sh / 2 + 0.3f, z - d / 2 - 0.03f), new Vector3(0.06f, sh, 0.05f), warm);
                    if (n % 3 == 0) Strip(new Vector3(xj - edge * (w / 2 - 0.08f), -1.7f + sh * 0.35f + 0.3f, z - d / 2 - 0.03f), new Vector3(0.06f, sh * 0.7f, 0.05f), false);
                }
            Box("Horizon line", root, new Vector3(0, -1.62f, 19.5f), new Vector3(70f, 0.1f, 0.1f), cyan); // the lit baseline at the back of the hall
            var band = GameObject.CreatePrimitive(PrimitiveType.Quad); // and its glow: a soft cyan band where the floor meets the back, as in the reference
            band.name = "Horizon glow"; band.transform.SetParent(root, false);
            band.transform.localPosition = new Vector3(0, -0.4f, 19.2f); band.transform.localScale = new Vector3(72f, 4f, 1f);
            band.layer = 4; Object.DestroyImmediate(band.GetComponent<Collider>());
            var br = band.GetComponent<Renderer>(); br.sharedMaterial = Additive("Chamber horizon", new Color(Blue.r * 1.2f, Blue.g * 1.2f, Blue.b * 1.2f, 0.35f), soft: true); br.shadowCastingMode = ShadowCastingMode.Off;
            // The conduit's own light on the glossy floor (the baked probe cannot mirror what moves): a soft cyan pool under it.
            var halo = AssetDatabase.LoadAssetAtPath<Material>("Assets/OhmsLaw/Shared/Art/Materials/Glow_WireBadgeHalo_4FD1FF.mat");
            if (halo)
            {
                var pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
                pool.name = "Floor glow"; pool.transform.SetParent(root, false);
                pool.transform.localPosition = new Vector3(0, -1.69f, 0.2f); pool.transform.localRotation = Quaternion.Euler(90, 0, 0); pool.transform.localScale = new Vector3(13f, 3f, 1f);
                Object.DestroyImmediate(pool.GetComponent<Collider>());
                var pr = pool.GetComponent<Renderer>(); pr.sharedMaterial = halo; pr.shadowCastingMode = ShadowCastingMode.Off;
            }
            // Cyan washes along the pillar line light their faces and the floor between them; one warm one.
            foreach (var (x, z, c) in new[] { (-14f, 6f, Blue), (-5f, 8f, Blue), (6f, 8f, Blue), (15f, 6f, Blue), (0f, 13f, Blue), (-9f, 12f, Amber) })
            {
                var go = new GameObject("Chamber wash");
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(x, 6f, z);
                var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c;
                l.intensity = c == Amber ? 4f : 7f; l.range = 16f; l.shadows = LightShadows.None;
            }
            // The floor's probe mirrors the static set (layer 4): re-bake it now that the set has changed.
            var probe = wire.GetComponentInChildren<ReflectionProbe>(true);
            if (probe)
            {
                probe.resolution = 512;
                probe.size = new Vector3(64, 30, 60);
                probe.center = new Vector3(0, -1.7f + 15f, 6f);
                string path = AssetDatabase.GetAssetPath(probe.bakedTexture);
                if (string.IsNullOrEmpty(path)) path = "Assets/OhmsLaw/Shared/Art/Post/Probe_" + wire.gameObject.scene.name.Substring(0, 7) + "_Wire.exr";
                probe.mode = ReflectionProbeMode.Baked;
                if (Lightmapping.BakeReflectionProbe(probe, path)) probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                else Debug.LogWarning(wire.gameObject.scene.name + ": the wire floor probe did not re-bake; its mirror is stale.");
            }
        }

        // Additive, unlit, translucent (alpha in the colour): light laid over the floor, not a solid bar.
        static Material Additive(string name, Color colour, bool soft = false)
        {
            string path = Art + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Universal Render Pipeline/Unlit");
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3000;
            material.SetColor("_BaseColor", colour);
            material.SetTexture("_BaseMap", soft ? AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd") : null);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
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
