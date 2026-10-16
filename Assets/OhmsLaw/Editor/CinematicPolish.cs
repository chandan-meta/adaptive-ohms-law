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

        [MenuItem("Ohm's Law/Polish/Apply study + lab (Scenes 7-13)")]
        public static void ApplyFrom7() => Apply(new[] { 7, 8, 9, 10, 11, 12, 13 }); // never 5/6: the user hand-edits the wire scenes

        [MenuItem("Ohm's Law/Polish/Apply lab (Scenes 8-13)")]
        public static void ApplyLab() => Apply(new[] { 8, 9, 10, 11, 12, 13 });

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
                    if (wire) PolishWire(wire); // first: it may destroy the old chamber, so the list below holds no destroyed objects
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
                        string baseName = original.name.Replace("_Polished", "");
                        string destination = Art + "/" + baseName + "_Polished.asset";
                        if (!AssetDatabase.LoadAssetAtPath<VolumeProfile>(destination)) AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), destination);
                        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(destination);
                        volume.sharedProfile = profile;
                        // The lab's grade is its builder's (LabSetLighting / LabGrade, matched to the user's golden-hour reference):
                        // mirrored whole, so a builder change reaches the scenes (the copy above is made only once).
                        var labBuilt = baseName == "Classroom_Global" || baseName == "Finale_Global"
                            ? AssetDatabase.LoadAssetAtPath<VolumeProfile>(SceneBuilderBase.Art + "/Post/" + baseName + ".asset") : null;
                        if (labBuilt) { Mirror(labBuilt, profile); continue; }
                        bool electric = profile.name.Contains("Electron");
                        if (profile.TryGet<Bloom>(out var bloom)) { bloom.intensity.Override(electric ? 0.45f : 0.3f); bloom.threshold.Override(1.1f); bloom.scatter.Override(electric ? 0.7f : 0.62f); }
                        if (profile.TryGet<FilmGrain>(out var grain)) grain.intensity.Override(0.025f);
                        if (profile.TryGet<Vignette>(out var vignette)) vignette.intensity.Override(electric ? 0.22f : 0.18f);
                        if (profile.TryGet<ColorAdjustments>(out var colour))
                        {
                            colour.contrast.Override(8f); colour.saturation.Override(-5f);
                            // The copy is made once, so it kept whatever exposure the builder had at the time (the lab sat at the
                            // classroom's 0.2 while its builder means 0.45). The builder's profile owns the exposure: read it every apply.
                            var built = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SceneBuilderBase.Art + "/Post/" + baseName + ".asset");
                            if (built && built.TryGet<ColorAdjustments>(out var builder)) colour.postExposure.Override(builder.postExposure.value);
                        }
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

        // The builder's profile onto its polished copy, component by component and value by value (a component the copy lacks
        // is added as a sub-asset).
        static void Mirror(VolumeProfile from, VolumeProfile to)
        {
            foreach (var source in from.components)
            {
                var target = to.components.FirstOrDefault(c => c.GetType() == source.GetType());
                if (!target)
                {
                    target = to.Add(source.GetType());
                    target.name = source.name;
                    AssetDatabase.AddObjectToAsset(target, to);
                }
                target.active = source.active;
                for (int i = 0; i < source.parameters.Count; i++)
                {
                    target.parameters[i].overrideState = source.parameters[i].overrideState;
                    target.parameters[i].SetValue(source.parameters[i]);
                }
                EditorUtility.SetDirty(target);
            }
            EditorUtility.SetDirty(to);
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

        // Scenes 5, 6 after a rebuild: the builder owns the environment now (the copper lattice cavern, its fog and lights,
        // SceneBuilderBase.Wire.cs). This keeps the material and trail settings the neon pass found, drops the 2026-10-09
        // pillar hall if a saved scene still carries it, and re-bakes the floor's probe.
        static void PolishWire(WireInterior wire)
        {
            wire.carrierSize = 0.30f;
            wire.conduitColor.a = 0.075f;
            foreach (var trail in wire.trails) if (trail) { trail.widthMultiplier = 0.035f; trail.numCapVertices = 3; }
            // The polished copies were made once from older builder values, and the material pass swaps them in: refresh each
            // from its builder material first (the pass then re-applies its overrides), so builder changes (the darker − badge
            // face, the cable's dimmer glow) survive the polish. The conduit's copy runs its own shader and is kept as it is.
            foreach (var source in wire.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct())
                if (source && source.name.StartsWith("Wire_") && !source.name.EndsWith("_Polished")
                    && AssetDatabase.LoadAssetAtPath<Material>(Art + "/" + source.name + "_Polished.mat") is Material stale && stale.shader == source.shader)
                    stale.CopyPropertiesFromMaterial(source);
            var floor = wire.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "Floor");
            if (floor)
            {
                // Black glass: opaque (a see-through floor rendered as a flat cyan plane during the gate), the grid seams
                // faint. On the polished copy: the material loop below swaps the builder's floor for it, so values set on
                // the builder's material were lost once the copy existed.
                var m = PolishMaterial(floor.sharedMaterial);
                floor.sharedMaterial = m;
                m.SetFloat("_Surface", 0); m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                m.SetFloat("_ZWrite", 1); m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = -1;
                m.SetColor("_BaseColor", new Color(0.012f, 0.02f, 0.045f, 1f));
                m.SetFloat("_Metallic", 0.1f);
                m.SetFloat("_Smoothness", 0.8f);
                m.SetColor("_EmissionColor", new Color(0.31f, 0.82f, 1f) * 0.04f); // the builder's seams: faint, not a Tron grid
                EditorUtility.SetDirty(m);
            }
            if (wire.transform.Find("Chamber Depth") is Transform old) Object.DestroyImmediate(old.gameObject); // the pillar hall: the cavern replaced it
            // The floor's probe mirrors the static set (layer 4): re-bake it now that the set has changed.
            var probe = wire.GetComponentInChildren<ReflectionProbe>(true);
            if (probe)
            {
                probe.resolution = 512;
                string path = AssetDatabase.GetAssetPath(probe.bakedTexture);
                if (string.IsNullOrEmpty(path)) path = "Assets/OhmsLaw/Shared/Art/Post/Probe_" + wire.gameObject.scene.name.Substring(0, 7) + "_Wire.exr";
                probe.mode = ReflectionProbeMode.Baked;
                if (Lightmapping.BakeReflectionProbe(probe, path)) probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                else Debug.LogWarning(wire.gameObject.scene.name + ": the wire floor probe did not re-bake; its mirror is stale.");
            }
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
