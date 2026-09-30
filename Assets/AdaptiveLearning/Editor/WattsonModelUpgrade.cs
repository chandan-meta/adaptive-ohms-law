#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace AdaptiveOhmsLaw.Editor
{
    public static class WattsonModelUpgrade
    {
        public const string ModelPath = "Assets/AdaptiveLearning/Art/Wattson/LB/LB_FK.fbx";
        public static void Apply()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            string prefabPath = "Assets/AdaptiveLearning/Prefabs/Wattson/Wattson.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            Replace(prefab.GetComponent<WattsonController>());
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);
            foreach (var path in Directory.GetFiles("Assets/AdaptiveLearning/Scenes", "*.unity"))
            {
                var scene = EditorSceneManager.OpenScene(path);
                foreach (var controller in Object.FindObjectsByType<WattsonController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Replace(controller);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        static void Replace(WattsonController controller)
        {
            if (!controller) return;
            var data = new SerializedObject(controller);
            var previous = data.FindProperty("visual").objectReferenceValue as Transform;
            if (previous && previous.name == "Wattson LB Visual") { Pose(previous); return; }
            var wrapper = new GameObject("Wattson LB Visual").transform;
            wrapper.SetParent(controller.transform, false);
            wrapper.localPosition = previous ? previous.localPosition : Vector3.zero;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), wrapper);
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float scale = 1.6f / bounds.size.y;
            Vector3 localCenter = wrapper.InverseTransformPoint(bounds.center);
            model.transform.localScale *= scale;
            model.transform.localPosition = -localCenter * scale;
            foreach (var r in renderers)
            {
                r.sharedMaterials = r.sharedMaterials.Select(m => MaterialFor(m ? m.name : "Body")).ToArray();
            }
            if (previous) { previous.gameObject.SetActive(false); previous.name = "Previous Wattson Visual - preserved"; }
            data.FindProperty("visual").objectReferenceValue = wrapper;
            data.FindProperty("animator").objectReferenceValue = model.GetComponent<Animator>();
            data.FindProperty("pointingArm").objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
            Pose(wrapper);
        }
        static void Pose(Transform wrapper)
        {
            foreach (var bone in wrapper.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "left_Sholder_jnt" || bone.name == "right_Sholder_jnt")
                {
                    // Rotate from the source bind pose; rerunning the upgrade is idempotent.
                    bone.localRotation = Quaternion.Euler(0,0,27.72f);
                    bone.Rotate(wrapper.forward, bone.name.StartsWith("left") ? 62f : -62f, Space.World);
                }
            }
        }
        static Material MaterialFor(string source)
        {
            string path = "Assets/AdaptiveLearning/Art/Wattson/LB/" + source + "_URP.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = source.Contains("eye") ? new Color(.02f,.13f,.17f) : new Color(.83f,.88f,.92f);
            material.SetFloat("_Metallic", .15f); material.SetFloat("_Smoothness", .45f);
            AssetDatabase.CreateAsset(material,path);
            return material;
        }
        public static void Inspect()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var instance = Object.Instantiate(model);
            var report = new StringBuilder();
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                report.AppendLine("RENDERER " + renderer.name + " bounds=" + renderer.bounds + " materials=" + string.Join(",", renderer.sharedMaterials.Select(m => m ? m.name : "missing")));
            foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                report.AppendLine("NODE " + child.name + " pos=" + child.localPosition + " rotation=" + child.localEulerAngles);
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>())
                report.AppendLine("CLIP " + clip.name + " duration=" + clip.length);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/Wattson-model-inspection.txt", report.ToString());
            Object.DestroyImmediate(instance);
        }
    }
}
#endif
