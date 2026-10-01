using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OhmsLaw.Editor
{
    // Docs/BLOCKOUT_REPORT.md: every BlockoutPlaceholder in every module scene, grouped by scene. The polish checklist.
    public static class BlockoutReport
    {
        [MenuItem("Ohm's Law/Blockout Report")]
        public static void Write()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var sb = new StringBuilder("# Blockout report\n\nGenerated " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ". Every placeholder object, by scene, with the intended final result.\n\n");
            var counts = new StringBuilder("| Scene | Placeholders | VFX stand-ins |\n|---|---:|---:|\n");
            int total = 0;
            foreach (var path in Directory.GetFiles(SceneBuilderBase.Root + "/Scenes", "Scene*.unity").OrderBy(p => p))
            {
                var scene = EditorSceneManager.OpenScene(path.Replace('\\', '/'), OpenSceneMode.Single);
                var tags = Object.FindObjectsByType<BlockoutPlaceholder>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .OrderBy(t => t.pulse ? 1 : 0).ThenBy(t => Path(t.transform)).ToArray();
                counts.AppendLine($"| {scene.name} | {tags.Length} | {tags.Count(t => t.pulse)} |");
                total += tags.Length;
                sb.AppendLine($"## {scene.name}  ({tags.Length})\n");
                if (tags.Length == 0) sb.AppendLine("No placeholders tagged (Scene 4 is a polished build).\n");
                else
                {
                    sb.AppendLine("| Object | Intended final result |\n|---|---|");
                    foreach (var t in tags) sb.AppendLine($"| `{Path(t.transform)}` | {t.note.Replace("|", "/").Replace("\n", " ")} |");
                    sb.AppendLine();
                }
            }
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            Directory.CreateDirectory("Docs");
            File.WriteAllText("Docs/BLOCKOUT_REPORT.md", sb.ToString().Replace("Every placeholder object, by scene", $"{total} placeholders in all. Every placeholder object, by scene") + "\n## Totals\n\n" + counts);
            Debug.Log($"Blockout report: {total} placeholders -> Docs/BLOCKOUT_REPORT.md");
        }

        static string Path(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (; t; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}
