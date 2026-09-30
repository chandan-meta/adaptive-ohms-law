#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdaptiveOhmsLaw.Editor
{
    public static class RepairLabSceneGeometry
    {
        public static void Run()
        {
            foreach (var sceneName in new[] { "Scene_12_DiscoverResistance", "Scene_13_OhmsLaw" })
            {
                string path = "Assets/AdaptiveLearning/Scenes/" + sceneName + ".unity";
                Directory.CreateDirectory("Logs/BeforeSceneRepair");
                string backup = "Logs/BeforeSceneRepair/" + sceneName + ".unity";
                if (!File.Exists(backup)) File.Copy(path, backup);
                var scene = EditorSceneManager.OpenScene(path);
                var table = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t =>
                    PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) &&
                    AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)).EndsWith("/ImportedLabEquipment/Workbench.prefab"));
                table.localScale = Vector3.one;
                table.position = Vector3.zero;
                var renderers = table.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                table.localScale = new Vector3(9f / bounds.size.x, 1f, 4.8f / bounds.size.z);
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                table.position = new Vector3(-bounds.center.x, -bounds.max.y, 1.2f - bounds.center.z);
                PrefabUtility.RecordPrefabInstancePropertyModifications(table);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
#endif
