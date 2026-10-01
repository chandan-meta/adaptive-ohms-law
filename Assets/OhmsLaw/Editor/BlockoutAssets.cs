using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace OhmsLaw.Editor
{
    // Project-wide assets every blockout scene shares: palette and fonts, the experiment readings, placeholder
    // sounds, materials, the scene order, and the lookup for recorded or scratch voice clips.
    public static class BlockoutAssets
    {
        public const string Root = Scene04Builder.Root, Shared = Scene04Builder.Shared;
        const string SymbolFont = Shared + "/UI/Fonts/DejaVuSans-Bold.ttf";
        const string Symbols = "Ω∝Δ∆→÷×−±·°";

        // Scene names in play order. Scene 4 is the Scene 4 build; the rest are the blockouts.
        public static readonly string[] SceneOrder =
        {
            "Scene04_MeetWattson", "Scene05_InsideTheWire", "Scene06_AdaptiveQuestion", "Scene07_BackToOhm", "Scene08_OurLab",
            "Scene09_BuildCircuit", "Scene10_Experiment", "Scene11_Graph", "Scene12_Resistance", "Scene13_OhmsLaw",
        };

        public static ModulePalette Palette()
        {
            var palette = AssetDatabase.LoadAssetAtPath<ModulePalette>(Shared + "/ModulePalette.asset");
            if (!palette) throw new InvalidOperationException("Run Ohm's Law > Build Scene 04 first; it creates the palette, font and module root.");
            EnsureSymbols(palette.font);
            UI.Theme = palette;
            return palette;
        }

        // The module font lacks ∝ and →, so a DejaVu Sans atlas with the maths symbols is attached as a fallback.
        static void EnsureSymbols(TMP_FontAsset font)
        {
            if (!font || font.HasCharacters(Symbols)) return;
            const string path = Shared + "/UI/Symbols SDF.asset";
            var symbols = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (!symbols)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(SymbolFont);
                if (!source) { Debug.LogWarning("Symbol font missing: " + SymbolFont); return; }
                symbols = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 512, 512);
                AssetDatabase.CreateAsset(symbols, path);
                symbols.material.name = "Symbols SDF Material";
                symbols.atlasTexture.name = "Symbols SDF Atlas";
                AssetDatabase.AddObjectToAsset(symbols.material, symbols);
                AssetDatabase.AddObjectToAsset(symbols.atlasTexture, symbols);
                symbols.TryAddCharacters(Symbols + "0123456789 =/()VIRA.", out string missing);
                if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Symbol font could not add: " + missing);
                EditorUtility.SetDirty(symbols);
                AssetDatabase.SaveAssets();
            }
            if (!font.fallbackFontAssetTable.Contains(symbols)) font.fallbackFontAssetTable.Add(symbols);
            font.TryAddCharacters("Δ∆÷×−→Ω", out _); // what Nunito does have is kept in its own atlas
            EditorUtility.SetDirty(font);
        }

        public static ExperimentData ExperimentData()
        {
            Directory.CreateDirectory(Shared + "/Resources");
            var data = AssetDatabase.LoadAssetAtPath<ExperimentData>(Shared + "/Resources/ExperimentData.asset");
            if (!data)
            {
                data = ScriptableObject.CreateInstance<ExperimentData>();
                AssetDatabase.CreateAsset(data, Shared + "/Resources/ExperimentData.asset");
            }
            if (!data.Consistent()) Debug.LogError("ExperimentData readings do not satisfy I = V / R. Meters and table would disagree.");
            return data;
        }

        public static Material Mat(string name, Color color, bool emissive = false)
        {
            Directory.CreateDirectory(Shared + "/Materials");
            string path = Shared + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.2f);
            if (color.a < 1)
            {
                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", 0);
                material.SetFloat("_ZWrite", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.6f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        // Scene 4's synthesized placeholder sounds (Click, Pop, Chirp, Whoosh...). Built if they are missing.
        public static Dictionary<string, AudioClip> Sfx()
        {
            string folder = Root + "/Scene04/Audio/SFX";
            if (!File.Exists(folder + "/Click.wav")) throw new InvalidOperationException("Run Ohm's Law > Build Scene 04 first; it synthesizes the placeholder sound effects.");
            return Directory.GetFiles(folder, "*.wav").ToDictionary(f => Path.GetFileNameWithoutExtension(f), f => AssetDatabase.LoadAssetAtPath<AudioClip>(f.Replace('\\', '/')));
        }

        // Recorded clip for a line: "S05_L01.wav" (real voice) or "scratch_S05_L01.wav" (OS text-to-speech).
        public static AudioClip FindVo(string sceneFolder, string id)
        {
            foreach (var name in new[] { id, "scratch_" + id })
                foreach (var ext in new[] { ".wav", ".mp3", ".ogg" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(sceneFolder + "/Audio/VO/" + name + ext);
                    if (clip) return clip;
                }
            return null;
        }

        public static bool IsScratch(AudioClip clip) => clip && Path.GetFileName(AssetDatabase.GetAssetPath(clip)).StartsWith("scratch_");

        // Student models, when the project has them. Searched by name under Assets/AdaptiveLearning/Art.
        public static GameObject KidModel(string name, Scene scene)
        {
            string guid = AssetDatabase.FindAssets(name + " t:Model", new[] { "Assets/AdaptiveLearning/Art" }).FirstOrDefault()
                       ?? AssetDatabase.FindAssets(name + " t:Prefab", new[] { "Assets/AdaptiveLearning/Prefabs" }).FirstOrDefault();
            if (guid == null) return null;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            go.name = name;
            return go;
        }

        // Tube along +X, unit radius, unit length, wound so its front faces point at the axis: seen from inside with the
        // default back-face culling (never pair it with _Cull Front). Rings along the length keep per-vertex fog honest
        // over 16 m. Rebuilt every build; the wire interior scales it to (length, radius, radius).
        public static Mesh TunnelMesh()
        {
            const string path = Shared + "/UI/Tunnel.mesh";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh)
            {
                mesh = new Mesh { name = "Tunnel" };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            const int segments = 64, rings = 32;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                for (int j = 0; j <= rings; j++)
                {
                    vertices.Add(new Vector3(j / (float)rings - 0.5f, Mathf.Cos(a), Mathf.Sin(a)));
                    uv.Add(new Vector2(j * 8f / rings, i / (float)segments));
                    if (i == segments || j == rings) continue;
                    int v = i * (rings + 1) + j;
                    triangles.AddRange(new[] { v, v + 1, v + rings + 1, v + 1, v + rings + 2, v + rings + 1 }); // inward
                }
            }
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // Sprite material for the carriers: a shaded sphere with a "−" bar, HDR so Bloom lights every electron (the
        // particle colour carries the tint and the near-lens fade). ion: the dull lattice sphere of Scene 12, no bar,
        // plain colour. Both sprites are regenerated each build.
        public static Material ChargeMaterial(Color tint, bool ion = false)
        {
            string png = Shared + (ion ? "/UI/IonGlyph.png" : "/UI/ChargeGlyph.png");
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) * 2;
                    float alpha = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.72f, 1f, r));       // solid core, soft rim
                    float shade = 0.72f + 0.28f * Mathf.Clamp01(1f - Mathf.Sqrt((dx + 0.15f) * (dx + 0.15f) + (dy - 0.15f) * (dy - 0.15f)) * 2.2f); // lit from upper left
                    bool bar = !ion && Mathf.Abs(dy) < 0.065f && Mathf.Abs(dx) < 0.27f;
                    tex.SetPixel(x, y, bar ? new Color(0.05f, 0.1f, 0.2f, 1) : new Color(shade, shade, shade, alpha));
                }
            File.WriteAllBytes(png, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(png);

            string path = Shared + (ion ? "/Materials/IonGlyph.mat" : "/Materials/ChargeGlyph.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            var face = ion ? tint : Color.Lerp(tint, Color.white, 0.5f) * 2.4f; // HDR face: above the bloom threshold even through the tint
            face.a = 1;
            material.SetColor("_BaseColor", face);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(png));
            EditorUtility.SetDirty(material);
            return material;
        }

        public static IEnumerable<SceneBuilderBase> Builders() =>
            TypeCache.GetTypesDerivedFrom<SceneBuilderBase>().Where(t => !t.IsAbstract)
                .Select(t => (SceneBuilderBase)Activator.CreateInstance(t)).OrderBy(b => b.SceneId);
    }
}
