using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Lists the project's real models, prefabs and materials with their sizes and parts, plus a thumbnail each,
    // so the polish pass can pick assets without guessing: Logs/Blockout/assets.md and assets/*.png
    public static class AssetInventory
    {
        // Full transform hierarchy of one prefab/model with world bounds per renderer: Logs/Blockout/hierarchy_<name>.txt
        public static void Hierarchy(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (!asset) { Debug.LogError("No prefab/model at " + assetPath); return; }
            var preview = EditorSceneManager.NewPreviewScene();
            var sb = new StringBuilder(assetPath + "\n");
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, preview);
                void Walk(Transform t, int depth)
                {
                    var r = t.GetComponent<Renderer>();
                    string size = r ? $"  bounds centre {r.bounds.center:F2} size {r.bounds.size:F2}  mat {string.Join("|", r.sharedMaterials.Where(m => m).Select(m => m.name))}" : "";
                    sb.AppendLine(new string(' ', depth * 2) + t.name + (t.gameObject.activeSelf ? "" : "  [inactive]") + size);
                    foreach (Transform c in t) Walk(c, depth + 1);
                }
                Walk(instance.transform, 0);
                Object.DestroyImmediate(instance);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Directory.CreateDirectory("Logs/Blockout");
            string file = "Logs/Blockout/hierarchy_" + Path.GetFileNameWithoutExtension(assetPath).Replace(' ', '_') + ".txt";
            File.WriteAllText(file, sb.ToString());
            Debug.Log("Hierarchy written to " + file);
        }

        static readonly string[] Folders = { "Assets/AdaptiveLearning/Prefabs", "Assets/AdaptiveLearning/Art", "Assets/OhmsLaw/Scene04/Prefabs" };

        [MenuItem("Ohm's Law/Asset Inventory")]
        public static void Write()
        {
            Directory.CreateDirectory("Logs/Blockout/assets");
            var sb = new StringBuilder("# Asset inventory\n\nSizes are renderer bounds in metres at the asset's own scale. Thumbnails in Logs/Blockout/assets/.\n\n");
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab t:Model", Folders).Distinct())
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!asset) continue;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, preview);
                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    var bounds = new Bounds(instance.transform.position, Vector3.zero);
                    bool first = true;
                    foreach (var r in renderers) { if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds); }
                    var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().Take(12).Select(m => $"{m.name} ({(m.shader ? m.shader.name : "?")})");
                    var children = instance.GetComponentsInChildren<Transform>(true).Select(t => t.name).Distinct().Take(40);
                    var scripts = instance.GetComponentsInChildren<MonoBehaviour>(true).Where(m => m).Select(m => m.GetType().Name).Distinct();
                    var animations = instance.GetComponentsInChildren<Animator>(true).Length + instance.GetComponentsInChildren<Animation>(true).Length;
                    sb.AppendLine($"## {path}\n");
                    sb.AppendLine($"- size (x y z): {bounds.size.x:0.00} × {bounds.size.y:0.00} × {bounds.size.z:0.00} m, centre offset {bounds.center - instance.transform.position}, root scale {instance.transform.localScale}");
                    sb.AppendLine($"- renderers: {renderers.Length}, animators: {animations}, scripts: {string.Join(", ", scripts)}");
                    sb.AppendLine($"- materials: {string.Join("; ", materials)}");
                    sb.AppendLine($"- parts: {string.Join(", ", children)}");
                    string thumb = "Logs/Blockout/assets/" + Path.GetFileNameWithoutExtension(path).Replace(' ', '_') + ".png";
                    var tex = AssetPreview.GetAssetPreview(asset);
                    for (int i = 0; i < 50 && !tex; i++) { System.Threading.Thread.Sleep(50); tex = AssetPreview.GetAssetPreview(asset); }
                    if (tex) { File.WriteAllBytes(thumb, tex.EncodeToPNG()); sb.AppendLine($"- thumbnail: {thumb}"); }
                    sb.AppendLine();
                    Object.DestroyImmediate(instance);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }

            sb.AppendLine("## Materials and textures\n");
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/AdaptiveLearning/Art", "Assets/AdaptiveLearning/Materials", "Assets/OhmsLaw/Scene04/Materials" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m) sb.AppendLine($"- {path} — {(m.shader ? m.shader.name : "?")}");
            }
            sb.AppendLine();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/AdaptiveLearning/Art/Interface" }))
                sb.AppendLine("- texture: " + AssetDatabase.GUIDToAssetPath(guid));
            sb.AppendLine("\n## Shaders\n");
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/AdaptiveLearning/Shaders" }))
                sb.AppendLine("- " + AssetDatabase.GUIDToAssetPath(guid));
            File.WriteAllText("Logs/Blockout/assets.md", sb.ToString());
            Debug.Log("Asset inventory written to Logs/Blockout/assets.md");
        }
    }
}
