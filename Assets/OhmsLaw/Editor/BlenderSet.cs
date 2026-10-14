using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw.Editor
{
    // A set made in Blender (Tools/blend_to_unity_set.py with a Tools/sets/*.json config): <Name>.fbx, its textures and
    // <Name>.json, the materials' values. Load() makes a URP Lit material per entry (base map x colour, metallic-smoothness
    // map or values, normal map, alpha-clipped cut-outs, tiling, emission or an emission map), remaps the model's materials
    // to them and caps textures at 2048. Wire: Scenes 5-6 use it in place of the code-built floor, terminal blocks, cables
    // and copper lattice. Lab: Scenes 8-13 use it in place of the code-built classroom room.
    public class BlenderSet
    {
        // Glows set here rather than in the .blend (HDR, so they bloom), by material name: (base colour, emission, the emission
        // follows the base map). "yellow light" is a grey in Blender and the user asked for it to glow yellow; the module's
        // cyan halo material is a faint see-through halo in the game, used as a solid ring and wall strips here, so it glows
        // cyan; "Material.002" is a sci-fi screen image on the backdrop panel, self-lit like a screen.
        public static readonly BlenderSet Wire = new BlenderSet("Assets/OhmsLaw/Shared/Art/WireSet", "WireSet",
            new Dictionary<string, (Color, Color, bool)>
            {
                ["yellow_light"] = (new Color(1f, 0.72f, 0.15f), new Color(1f, 0.62f, 0.08f) * 2f, false), // saturated and under 3x: brighter washed it to cream under bloom
                ["Glow_WireBadgeHalo_4FD1FF"] = (new Color(0.31f, 0.82f, 1f), new Color(0.31f, 0.82f, 1f) * 2f, false),
                ["Material.002"] = (Color.white, Color.white * 1.2f, true),
            });

        // The lab's glass is plain grey in Blender: here it is clear glass, a faint cyan, the floor ring and posts lit from
        // within. The screens and light strips glow from emission maps the exporter makes (Tools/sets/lab.json "glow").
        // "Material.001" is the user's Ohm's-law mural wrapped round the walls: a self-lit display, a little under the screens.
        public static readonly BlenderSet Lab = new BlenderSet("Assets/OhmsLaw/Shared/Art/LabSet", "LabSet",
            new Dictionary<string, (Color, Color, bool)> { ["Material.001"] = (Color.white, Color.white * 0.9f, true) },
            new Dictionary<string, (Color, Color)>
        {
            ["Glass"] = (new Color(0.62f, 0.86f, 1f, 0.16f), new Color(0.05f, 0.16f, 0.24f)),
            ["Glass1"] = (new Color(0.5f, 0.85f, 1f, 0.24f), new Color(0.04f, 0.16f, 0.28f)), // brighter, the floor ring read as a pale band across the floor
        });

        public readonly string Folder, Model, Json;
        readonly Dictionary<string, (Color baseColor, Color emission, bool emissionMap)> glows;
        readonly Dictionary<string, (Color tint, Color emission)> glass;

        BlenderSet(string folder, string name, Dictionary<string, (Color, Color, bool)> glows = null, Dictionary<string, (Color, Color)> glass = null)
        {
            Folder = folder;
            Model = folder + "/" + name + ".fbx";
            Json = folder + "/" + name + ".json";
            this.glows = glows ?? new Dictionary<string, (Color, Color, bool)>();
            this.glass = glass ?? new Dictionary<string, (Color, Color)>();
        }

        public bool Exists => File.Exists(Model);

        [System.Serializable] class Spec { public Entry[] materials = new Entry[0]; }
        [System.Serializable] class Entry
        {
            public string name, baseMap, normalMap, maskMap, emissionMap;
            public float[] baseColor, emission, tiling;
            public float metallic, roughness;
            public bool alphaClip;
        }

        public GameObject Load()
        {
            if (!Exists) return null;
            var spec = JsonUtility.FromJson<Spec>(File.ReadAllText(Json));
            Directory.CreateDirectory(Folder + "/Materials");
            AssetDatabase.Refresh(); // a new export: changed and new files import once here, not per texture per scene
            // Every texture imported and set up before the model importer is fetched: importing new files mid-loop left that
            // importer stale, so only some remaps stuck and scenes built then kept the FBX's own materials (magenta later).
            foreach (var e in spec.materials)
            {
                Texture(e.baseMap, TextureRole.Colour);
                Texture(e.maskMap, TextureRole.Data);
                Texture(e.normalMap, TextureRole.Normal);
                Texture(e.emissionMap, TextureRole.Colour);
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(Model);
            var remaps = importer.GetExternalObjectMap();
            bool reimport = importer.importCameras || importer.importLights;
            importer.importCameras = importer.importLights = false;
            foreach (var e in spec.materials)
            {
                string path = Folder + "/Materials/" + e.name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
                var baseColor = new Color(e.baseColor[0], e.baseColor[1], e.baseColor[2], 1f);
                var emission = e.emission != null && e.emission.Length == 3 ? new Color(e.emission[0], e.emission[1], e.emission[2]) : Color.black;
                bool emissionMap = false;
                if (glows.TryGetValue(e.name, out var glow)) { baseColor = glow.baseColor; emission = glow.emission; emissionMap = glow.emissionMap; }
                bool clear = glass.TryGetValue(e.name, out var pane);
                if (clear) { baseColor = pane.tint; emission = pane.emission; }

                mat.SetTexture("_BaseMap", Texture(e.baseMap, TextureRole.Colour));
                mat.SetColor("_BaseColor", baseColor);
                var tiling = e.tiling != null && e.tiling.Length == 2 ? new Vector2(e.tiling[0], e.tiling[1]) : Vector2.one;
                mat.SetTextureScale("_BaseMap", tiling); // URP Lit reads every map with _BaseMap's tiling

                var mask = Texture(e.maskMap, TextureRole.Data);
                mat.SetTexture("_MetallicGlossMap", mask);
                Keyword(mat, "_METALLICSPECGLOSSMAP", mask);
                mat.SetFloat("_Metallic", clear ? 0f : e.metallic);
                mat.SetFloat("_Smoothness", clear ? 0.95f : mask ? 1f : 1f - e.roughness); // with the map, the slider scales its alpha

                var normal = Texture(e.normalMap, TextureRole.Normal);
                mat.SetTexture("_BumpMap", normal);
                Keyword(mat, "_NORMALMAP", normal);

                mat.SetFloat("_AlphaClip", e.alphaClip ? 1f : 0f);
                mat.SetFloat("_Cutoff", 0.5f);
                Keyword(mat, "_ALPHATEST_ON", e.alphaClip);
                Transparent(mat, clear);
                mat.renderQueue = clear ? (int)RenderQueue.Transparent : e.alphaClip ? (int)RenderQueue.AlphaTest : -1;
                mat.SetOverrideTag("RenderType", clear ? "Transparent" : e.alphaClip ? "TransparentCutout" : "Opaque");

                var glowMap = Texture(e.emissionMap, TextureRole.Colour);
                bool lit = emission.maxColorComponent > 0f;
                mat.SetColor("_EmissionColor", emission);
                mat.SetTexture("_EmissionMap", glowMap ? glowMap : emissionMap ? mat.GetTexture("_BaseMap") : null);
                Keyword(mat, "_EMISSION", lit);
                mat.globalIlluminationFlags = lit ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);

                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), e.name);
                if (!remaps.TryGetValue(id, out var current) || current != mat) { importer.AddRemap(id, mat); reimport = true; }
            }
            AssetDatabase.SaveAssets();
            if (reimport) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        }

        enum TextureRole { Colour, Data, Normal }

        static void Keyword(Material mat, string keyword, bool on) { if (on) mat.EnableKeyword(keyword); else mat.DisableKeyword(keyword); }

        // URP Lit surface type: alpha-blended (premultiplied-free SrcAlpha / OneMinusSrcAlpha, no depth write) or opaque.
        static void Transparent(Material mat, bool on)
        {
            mat.SetFloat("_Surface", on ? 1f : 0f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)(on ? BlendMode.SrcAlpha : BlendMode.One));
            mat.SetFloat("_DstBlend", (float)(on ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)(on ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            mat.SetFloat("_ZWrite", on ? 0f : 1f);
            Keyword(mat, "_SURFACE_TYPE_TRANSPARENT", on);
        }

        // The texture under Textures/ with the import settings its role needs; the Blender images are up to 4K, 2048 is plenty
        // at the distances the set is seen from.
        Texture2D Texture(string file, TextureRole role)
        {
            if (string.IsNullOrEmpty(file)) return null;
            string path = Folder + "/Textures/" + file;
            if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path); // a file the refresh has not seen yet
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;
            var type = role == TextureRole.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            bool srgb = role == TextureRole.Colour;
            if (importer.maxTextureSize != 2048 || importer.textureType != type || importer.sRGBTexture != srgb)
            {
                importer.maxTextureSize = 2048;
                importer.textureType = type;
                importer.sRGBTexture = srgb;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
