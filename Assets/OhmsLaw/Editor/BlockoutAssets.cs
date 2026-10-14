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
        const string DisplayFont = Shared + "/UI/Fonts/Fredoka-SemiBold.ttf", DisplayAsset = Shared + "/Resources/Fredoka SDF.asset";

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
            EnsureDisplayFont(palette.font);
            UI.Theme = palette;
            return palette;
        }

        // The kids UI's display face: a Fredoka SemiBold (OFL, Shared/UI/Fonts) atlas in Shared/Resources, made the way the
        // Nunito SDF is (Scene04Builder.BuildPalette) and loaded at runtime by UI.DisplayFont. Nunito is its fallback, so a
        // glyph Fredoka lacks (Ω, ∝, the arrows) comes from Nunito or the symbol atlas behind it.
        static void EnsureDisplayFont(TMP_FontAsset body)
        {
            var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayAsset);
            if (!display)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(DisplayFont);
                if (!source) { Debug.LogWarning("Display font missing: " + DisplayFont); return; }
                Directory.CreateDirectory(Shared + "/Resources");
                display = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                AssetDatabase.CreateAsset(display, DisplayAsset);
                display.material.name = "Fredoka SDF Material";
                display.atlasTexture.name = "Fredoka SDF Atlas";
                AssetDatabase.AddObjectToAsset(display.material, display);
                AssetDatabase.AddObjectToAsset(display.atlasTexture, display);
                display.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~…–—’‘“”°×÷=Ω", out string missing);
                if (!string.IsNullOrEmpty(missing)) Debug.Log("Fredoka SDF: from the fallback: " + missing);
                // Written to disk now: a later refresh would otherwise reload the asset without its atlas and material.
                EditorUtility.SetDirty(display);
                AssetDatabase.SaveAssets();
            }
            if (display.fallbackFontAssetTable == null) display.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>(); // null in memory right after CreateAsset + SaveAssets
            if (body && !display.fallbackFontAssetTable.Contains(body))
            {
                display.fallbackFontAssetTable.Add(body);
                EditorUtility.SetDirty(display);
            }
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

        // Clip for a line, best first: the real voice "<id>.mp3" (the proxy bake, BakeBlockoutVO), a voice actor's
        // "<id>.ogg" / "<id>.wav", then the stand-in "scratch_<id>.wav" (local text-to-speech).
        static readonly string[] VoNames = { "{0}.mp3", "{0}.ogg", "{0}.wav", "scratch_{0}.wav", "scratch_{0}.mp3", "scratch_{0}.ogg" };

        public static AudioClip FindVo(string sceneFolder, string id)
        {
            foreach (var name in VoNames)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(sceneFolder + "/Audio/VO/" + string.Format(name, id));
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

        // Open tube along +X, unit radius, unit length, wound to face OUTWARD: the conduit of the electron space is seen
        // from outside with the default back-face culling. No end caps (the ends sit inside the terminal blocks).
        // Rebuilt every build; WireInterior scales it to (length, radius, radius).
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
            const int segments = 48, rings = 8;
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
                    triangles.AddRange(new[] { v, v + rings + 1, v + 1, v + 1, v + rings + 1, v + rings + 2 }); // outward
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

        // Sprite material for the electrons: a glossy glowing sphere with a soft halo and a dark navy "−" bar (the
        // reference's look), HDR so Bloom lights every one; the particle colour carries the tint and the fades.
        // ion: the dull lattice sphere of Scene 12, no bar, no halo, plain colour. Both sprites are regenerated each build.
        public static Material ChargeMaterial(Color tint, bool ion = false)
        {
            string png = Shared + (ion ? "/UI/IonGlyph.png" : "/UI/ChargeGlyph.png");
            int n = ion ? 128 : 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            const float body = 0.68f; // sphere radius as a fraction of the half-size; the halo fills the rest
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) * 2;
                    float edge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(body - 0.05f, body + 0.03f, r)); // anti-aliased sphere edge
                    float halo = ion ? 0 : 0.28f * Mathf.Pow(Mathf.Clamp01(1f - Mathf.InverseLerp(body, 1f, r)), 2f);
                    float alpha = Mathf.Max(edge, halo);
                    // Lit from the upper left: white highlight, saturating toward sky blue at the lower-right rim, so
                    // it reads as a glossy sphere after the HDR tint and tonemapping, not a flat dot.
                    float light = Mathf.Clamp01(1f - Mathf.Sqrt((dx + 0.14f) * (dx + 0.14f) + (dy - 0.16f) * (dy - 0.16f)) * 1.9f);
                    float rim = Mathf.InverseLerp(body - 0.22f, body, r) * Mathf.Clamp01(0.5f - (dy - dx) * 1.2f);
                    var pixel = ion ? new Color(0.9f, 0.9f, 0.9f, alpha) * (0.8f + 0.2f * light)
                                    : Color.Lerp(new Color(0.42f, 0.72f, 1f), Color.white, light * light) * (1f - 0.3f * rim);
                    pixel.a = alpha;
                    if (!ion)
                    {
                        // Rounded "−" bar, 56 % of the body wide; anti-aliased so it stays crisp when small.
                        float bx = Mathf.Max(0, Mathf.Abs(dx) - (0.28f * body - 0.03f)), by = Mathf.Max(0, Mathf.Abs(dy) - (0.085f * body - 0.03f));
                        float bar = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.035f, Mathf.Sqrt(bx * bx + by * by)));
                        pixel = Color.Lerp(pixel, new Color(0.012f, 0.025f, 0.06f, 1f), bar); // stays navy after the ×2 HDR face
                    }
                    tex.SetPixel(x, y, pixel);
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
            var face = ion ? tint : Color.Lerp(tint, Color.white, 0.4f) * 2.0f; // HDR face: the highlight blooms, the blue rim stays blue
            face.a = 1;
            material.SetColor("_BaseColor", face);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(png));
            EditorUtility.SetDirty(material);
            return material;
        }

        public static IEnumerable<SceneBuilderBase> Builders() =>
            TypeCache.GetTypesDerivedFrom<SceneBuilderBase>().Where(t => !t.IsAbstract && !t.IsNested) // nested kits (Scene04Builder.ApparatusKit) are not scenes
                .Select(t => (SceneBuilderBase)Activator.CreateInstance(t)).OrderBy(b => b.SceneId);
    }
}
