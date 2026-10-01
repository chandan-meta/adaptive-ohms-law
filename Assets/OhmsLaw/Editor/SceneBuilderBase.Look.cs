using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace OhmsLaw.Editor
{
    // The polished look, lifted from Scene 4 so Scenes 5-13 share it: the study prefab and its coordinate frame,
    // the candle-and-window lighting rig, the global post volume, per-shot lens profiles, HDR glow materials,
    // and real-model instantiation. Asset-writing helpers put module-wide assets under Shared/Art.
    public abstract partial class SceneBuilderBase
    {
        public const string Art = Shared + "/Art";
        const string StudyPrefab = Root + "/Scene04/Prefabs/Lab_Scene04.prefab";
        const string StudyReflection = Root + "/Scene04/VFX/Scene04_Reflection.exr";

        // ---- Scene 4's study, in the lab model's own coordinates (floor y = -1.08, desk top y = -0.26) ----
        public const float FloorY = -1.08f, DeskY = -0.26f;
        public static readonly Vector3 Group = new Vector3(-0.2f, 0.27f, 0.3f);          // the kids' eye point (camera POV)
        public static readonly Vector3 Hover = new Vector3(-1.5f, 0.2f, -0.45f);        // where Wattson settled in Scene 4
        public static readonly Vector3 OhmFeet4 = new Vector3(-2.4f, -1.08f, -2.36f);   // Ohm, facing +Z into the room
        public static readonly Vector3 OhmHead = new Vector3(-2.4f, 0.45f, -2.2f);
        public static readonly Vector3 Wire4 = new Vector3(-2.71f, -0.18f, -1.9f);      // the copper test wire on the desk
        public static readonly Vector3 Notebook4 = new Vector3(-2.22f, -0.252f, -2.03f);
        public static readonly Vector3 Galvanometer4 = new Vector3(-2.6f, -0.26f, -2.02f);
        public static readonly Vector3 Pile4 = new Vector3(-2.9f, -0.26f, -2.03f);
        public static readonly Vector3 DeskTop = new Vector3(-2.42f, DeskY, -1.77f);     // Victorian desk top centre, 1.61 x 0.66 m
        // The modern bench (Scene 8 onward) stands where the desk was: 2.0 x 0.9 m, top at DeskY, between the -X wall and the room.
        public static readonly Vector3 BenchCentre = new Vector3(-2.3f, DeskY, -1.8f);
        public const float BenchWidth = 2.0f, BenchDepth = 0.9f;
        // Study-frame camera and marker positions shared by Scenes 7-13 so match-cuts line up.
        public static readonly Vector3 StudyWideCam = new Vector3(-1.2f, 0.85f, 0.75f), StudyWideLook = new Vector3(-2.35f, -0.15f, -1.85f);
        public static readonly Vector3 StudyBenchCam = new Vector3(-1.75f, 0.55f, -0.25f), StudyBenchLook = new Vector3(-2.35f, -0.2f, -1.8f);
        public static readonly Vector3 StudyCloseCam = new Vector3(-2.1f, 0.25f, -0.75f), StudyCloseLook = new Vector3(-2.4f, -0.2f, -1.85f);
        public static readonly Vector3 StudyBoardsCam = new Vector3(-1.1f, 0.65f, 0.4f), StudyBoardsLook = new Vector3(-1.5f, 0.55f, -1.9f);
        public static readonly Vector3 StudyWattsonHome = new Vector3(-1.25f, 0.3f, -1.05f);
        public static readonly Vector3 BoardsAnchor = new Vector3(-1.35f, 0.75f, -2.25f); // table + graph float above the right end of the bench

        protected VolumeProfile lensWide, lensMedium, lensShallow, lensTracking, lensDive;

        // ------------------------------------------------------------------ assets and materials

        protected static T Asset<T>(string path, Func<T> create) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        protected static Material Mat(string name, string shader, Action<Material> setup)
        {
            Directory.CreateDirectory(Art + "/Materials");
            var material = Asset(Art + "/Materials/" + name + ".mat", () => new Material(Shader.Find(shader)));
            material.shader = Shader.Find(shader);
            setup(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        protected static Material Lit(string name, Color color, float metallic, float smoothness, Color? emission = null) =>
            Mat(name, "Universal Render Pipeline/Lit", m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", smoothness);
                if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
            });

        // Additive, unlit, HDR: Bloom turns it into light. Particles, lines, flares, glows.
        protected static Material Glow(string name, Color hdr, bool softTexture = true) =>
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

        // Alpha-blended unlit colour, for translucent planes and tinted glass.
        protected static Material Translucent(string name, Color color) =>
            Mat(name, "Universal Render Pipeline/Unlit", m =>
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0);
                m.SetFloat("_Cull", (float)CullMode.Off);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetColor("_BaseColor", color);
            });

        protected static Material EmissiveCopy(Material source, string name, Color emission)
        {
            Directory.CreateDirectory(Art + "/Materials");
            string path = Art + "/Materials/" + name + ".mat";
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

        protected static Color Hdr(Color color, float intensity) => new Color(color.r * intensity, color.g * intensity, color.b * intensity, color.a);

        protected static Mesh MeshAsset(string name, Action<Mesh> fill)
        {
            Directory.CreateDirectory(Art + "/Meshes");
            var mesh = Asset(Art + "/Meshes/" + name + ".mesh", () => new Mesh());
            mesh.Clear();
            fill(mesh);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        protected static Mesh Torus(string name, float tube, int segments = 40, int sides = 10) => MeshAsset(name, mesh =>
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

        // ------------------------------------------------------------------ real models

        // Instantiates a project prefab/model, places it with the given uniform scale, and adds a box collider
        // around its renderers (tap and drag targets). Position is where the prefab's own pivot lands.
        protected GameObject Model(string assetPath, string name, Transform parent, Vector3 position, Vector3 euler, float scale, bool collider = true)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (!asset) throw new FileNotFoundException("Model missing: " + assetPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            go.name = name;
            if (parent) go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = Vector3.one * scale;
            foreach (var old in go.GetComponentsInChildren<MonoBehaviour>(true)) if (old && old.GetType().Namespace == "AdaptiveOhmsLaw") Object.DestroyImmediate(old);
            if (collider) FitCollider(go);
            return go;
        }

        protected static BoxCollider FitCollider(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) return null;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var box = go.GetComponent<BoxCollider>();
            if (!box) box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(bounds.center);
            var size = go.transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            return box;
        }

        protected static Transform FindChild(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name && t != root.transform)
            ?? throw new MissingReferenceException(root.name + " has no child named " + name);

        // ------------------------------------------------------------------ the study

        // Scene 4's dressed study (lights off, clutter hidden, emissive flames) at the origin.
        protected GameObject Study()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StudyPrefab);
            if (!prefab) throw new FileNotFoundException("Build Scene 04 first; it creates " + StudyPrefab);
            var lab = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            lab.name = "Study";
            foreach (var light in lab.GetComponentsInChildren<Light>(true)) light.gameObject.SetActive(false);
            Ref(lab);
            // The pieces later scenes swap or hide, by name, reachable through refs.
            foreach (var t in lab.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Victorian_Desk") || t.name.StartsWith("Draw_") || t.name.StartsWith("Handle_") || t.name.StartsWith("Desk_Cabinet") || t.name.StartsWith("Chair_"))
                    refs["Study/" + t.name] = t.gameObject;
            return lab;
        }

        // Scene 4's lighting rig: window light, flickering flames, fill, probes, god rays, and the global ACES/bloom volume.
        protected void StudyLighting()
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
            camera.backgroundColor = new Color(0.5f, 0.63f, 0.85f);

            var root = Node("Lighting").transform;
            var window = Node("Window Light", root).AddComponent<Light>();
            window.type = LightType.Directional;
            window.transform.rotation = Quaternion.Euler(28, 82, 0);
            window.color = new Color(0.74f, 0.84f, 1f);
            window.intensity = 2.4f;
            window.shadows = LightShadows.Soft;

            Flame("Oil Lamp", root, new Vector3(-1.7f, 0.16f, -1.72f), 3.2f, 4f, true);
            Flame("Desk Candle", root, new Vector3(-2.36f, 0.1f, -1.9f), 1.6f, 2.4f, false);
            Flame("Fireplace", root, new Vector3(-1.35f, -0.5f, -3.15f), 4.5f, 4.5f, false).GetComponent<FlickerLight>().speed = 4f;
            Flame("Bookcase Candle", root, new Vector3(-2.85f, 0.22f, 0.87f), 1.4f, 3f, false);

            var fill = Node("Group Fill", root, new Vector3(0f, 0.85f, 0.55f)).AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.9f, 0.78f);
            fill.intensity = 2f;
            fill.range = 5f;
            fill.shadows = LightShadows.None;

            // Same room, same light: reuse Scene 4's baked reflection instead of baking again per scene.
            var probe = Node("Reflection Probe", root, new Vector3(-1.45f, 0.3f, -1f)).AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(3.9f, 2.9f, 4.9f);
            probe.boxProjection = true;
            probe.resolution = 128;
            probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(StudyReflection);

            var probes = Node("Light Probes", root).AddComponent<LightProbeGroup>();
            var points = new List<Vector3>();
            foreach (float x in new[] { -2.9f, -1.5f, 0f })
                foreach (float y in new[] { -0.8f, 0.3f, 1.3f })
                    foreach (float z in new[] { -2.9f, -1.3f, 0.6f })
                        points.Add(new Vector3(x, y, z));
            probes.probePositions = points.ToArray();

            GodRays(root, window.transform.forward * 3.4f);
            GlobalPost(root, "Study_Global", bloom: 0.85f, vignette: 0.26f, exposure: 0.25f);
        }

        protected Light Flame(string name, Transform root, Vector3 position, float intensity, float range, bool shadows)
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

        void GodRays(Transform root, Vector3 direction)
        {
            var rays = Node("God Rays", root);
            rays.AddComponent<MeshFilter>().sharedMesh = MeshAsset("GodRays", mesh =>
            {
                var vertices = new List<Vector3>();
                var uvs = new List<Vector2>();
                var triangles = new List<int>();
                for (int i = 0; i < 6; i++)
                {
                    float z = Mathf.Lerp(-3.05f, -2.2f, i / 5f);
                    int v = vertices.Count;
                    Vector3 top = new Vector3(-3.3f, 1.45f, z), bottom = new Vector3(-3.3f, -0.1f, z);
                    vertices.AddRange(new[] { top, bottom, bottom + direction, top + direction });
                    uvs.AddRange(new[] { new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 1), new Vector2(0, 1) });
                    triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
                }
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
            });
            var renderer = rays.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Glow("Glow_GodRay", new Color(0.6f, 0.74f, 1f, 0.16f));
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        // Dark, coloured look for the inside of a wire: no window, the tint as fog, stronger bloom.
        protected void WireLighting(Color tint)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = tint * 0.35f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = tint * 0.45f;
            RenderSettings.fogDensity = 0.035f;
            camera.backgroundColor = tint * 0.25f;
            var root = Node("Lighting").transform;
            foreach (var (offset, colour, intensity) in new[]
            {
                (new Vector3(-4f, 1.2f, 0f), palette.charge, 2.2f),
                (new Vector3(3f, -0.8f, 1.2f), Color.Lerp(tint, Color.white, 0.4f), 1.6f),
                (new Vector3(0f, 1.8f, -1.5f), palette.voltage, 1.2f),
            })
            {
                var light = Node("Tunnel Light", root, WireOrigin + offset).AddComponent<Light>();
                light.type = LightType.Point;
                light.color = colour;
                light.intensity = intensity;
                light.range = 9f;
                light.shadows = LightShadows.None;
            }
            GlobalPost(root, "Wire_Global", bloom: 1.2f, vignette: 0.32f, exposure: 0.15f);
        }

        protected void GlobalPost(Transform root, string name, float bloom, float vignette, float exposure)
        {
            var profile = Profile(name, p =>
            {
                Override<Tonemapping>(p).mode.Override(TonemappingMode.ACES);
                var b = Override<Bloom>(p);
                b.threshold.Override(1f);
                b.intensity.Override(bloom);
                b.scatter.Override(0.72f);
                var v = Override<Vignette>(p);
                v.intensity.Override(vignette);
                v.smoothness.Override(0.45f);
                var grain = Override<FilmGrain>(p);
                grain.type.Override(FilmGrainLookup.Thin1);
                grain.intensity.Override(0.22f);
                var color = Override<ColorAdjustments>(p);
                color.postExposure.Override(exposure);
                color.contrast.Override(8f);
                color.saturation.Override(6f);
            });
            var volume = Node("Global Volume", root).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        protected static VolumeProfile Profile(string name, Action<VolumeProfile> setup)
        {
            Directory.CreateDirectory(Art + "/Post");
            var profile = Asset(Art + "/Post/" + name + ".asset", ScriptableObject.CreateInstance<VolumeProfile>);
            setup(profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        protected static T Override<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(false);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            foreach (var parameter in component.parameters) parameter.overrideState = false;
            EditorUtility.SetDirty(component);
            return component;
        }

        // Per-shot depth of field (Bokeh); the focus distance follows the shot's focus target at runtime.
        protected static VolumeProfile Lens(string name, float focalLength, float aperture, bool dive = false, bool motionBlur = false) => Profile(name, p =>
        {
            var dof = Override<DepthOfField>(p);
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(2f);
            dof.focalLength.Override(focalLength);
            dof.aperture.Override(aperture);
            if (motionBlur || dive) Override<MotionBlur>(p).intensity.Override(dive ? 0.9f : 0.45f);
            if (dive) Override<ChromaticAberration>(p).intensity.Override(0.9f);
        });

        protected void Lenses()
        {
            lensWide = Lens("Lens_Wide", 28, 5.6f);
            lensMedium = Lens("Lens_Medium", 45, 3.2f);
            lensShallow = Lens("Lens_Shallow", 70, 1.8f);
            lensTracking = Lens("Lens_Tracking", 35, 4f, motionBlur: true);
            lensDive = Lens("Lens_Dive", 30, 2.8f, dive: true);
        }

        // A shot with depth of field on a focus target, optional handheld noise and tracking.
        protected CinemachineCamera Vcam(string name, Vector3 position, Vector3 lookAt, float fov, Transform focus, VolumeProfile lens, Transform track = null, float noise = 0)
        {
            var cam = Cam(name, position, lookAt, fov, track);
            if (noise > 0)
            {
                string guid = AssetDatabase.FindAssets("Handheld_normal_mild t:NoiseSettings").FirstOrDefault();
                var shake = cam.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
                shake.NoiseProfile = guid != null ? AssetDatabase.LoadAssetAtPath<NoiseSettings>(AssetDatabase.GUIDToAssetPath(guid)) : null;
                shake.AmplitudeGain = noise;
                shake.FrequencyGain = 0.8f;
            }
            if (lens)
            {
                var post = cam.gameObject.AddComponent<CinemachineVolumeSettings>();
                post.Profile = lens;
                post.FocusTracking = focus ? CinemachineVolumeSettings.FocusTrackingMode.CustomTarget : CinemachineVolumeSettings.FocusTrackingMode.None;
                post.FocusTarget = focus;
            }
            return cam;
        }

        // Scene 4's particle base: soft additive sprites fading in and out over their life.
        protected ParticleSystem Emitter(string name, Transform parent, Vector3 position, Material material, Action<ParticleSystem> setup)
        {
            var ps = Node(name, parent, position).AddComponent<ParticleSystem>();
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
            return ps;
        }

        protected LineRenderer Polyline(string name, Transform parent, Material material, float width, bool loop, bool world, params Vector3[] points)
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
            line.numCornerVertices = 3;
            return line;
        }
    }
}
