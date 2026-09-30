#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdaptiveOhmsLaw.Editor
{
    public static class CinematicSceneUpgrade
    {
        static readonly string[] ScenePaths =
        {
            "Assets/AdaptiveLearning/Scenes/Scene_08_OurLabIntroduction.unity",
            "Assets/AdaptiveLearning/Scenes/Scene_09_BuildTheCircuit.unity",
            "Assets/AdaptiveLearning/Scenes/Scene_10_BecomeTheExperimenter.unity",
            "Assets/AdaptiveLearning/Scenes/Scene_11_TurnDataIntoGraph.unity",
            "Assets/AdaptiveLearning/Scenes/Scene_12_DiscoverResistance.unity",
            "Assets/AdaptiveLearning/Scenes/Scene_13_OhmsLaw.unity"
        };

        [MenuItem("Adaptive Learning/Cinematics/Install Editable Scene Rigs")]
        public static void ApplyAll()
        {
            Directory.CreateDirectory("Logs/CinematicBackups");
            foreach (string path in ScenePaths)
            {
                string backup = "Logs/CinematicBackups/" + Path.GetFileName(path);
                if (!File.Exists(backup)) File.Copy(path, backup);
                var scene = EditorSceneManager.OpenScene(path);
                var transforms = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
                var root = transforms.FirstOrDefault(t => t.name == "Cinematic Direction - edit targets");
                if (root == null)
                {
                    root = new GameObject("Cinematic Direction - edit targets").transform;
                    SceneManager.MoveGameObjectToScene(root.gameObject, scene);
                }
                // Remove obsolete director references left by the initial prototype.
                // This root contains only presentation components, never assessment data.
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root.gameObject);
                var director = root.GetComponent<LessonCinematicDirector>();
                if (director == null) director = root.gameObject.AddComponent<LessonCinematicDirector>();
                var camera = transforms.Select(t => t.GetComponent<Camera>()).FirstOrDefault(c => c != null && c.CompareTag("MainCamera"));
                var wattson = transforms.Select(t => t.GetComponent<WattsonController>()).FirstOrDefault(c => c != null);
                var presenter = transforms.Select(t => t.GetComponent<DialoguePresenter>()).FirstOrDefault(c => c != null);
                var targets = new[]
                {
                    Target(root, CinematicSubject.Source, Find(transforms, "DC SOURCE", "DCSource", "VoltageSource"), 4.8f, -1f),
                    Target(root, CinematicSubject.Ammeter, Find(transforms, "AMMETER  /  I (A)", "Ammeter"), 4.2f, 1f),
                    Target(root, CinematicSubject.Voltmeter, Find(transforms, "VOLTMETER", "Voltmeter"), 4.2f, -1f),
                    Target(root, CinematicSubject.WireXY, Find(transforms, "NICHROME WIRE  XY", "NichromeWireXY", "WireHighlight"), 3.8f, 1f),
                    Target(root, CinematicSubject.Graph, Find(transforms, "GraphPanel", "GraphCanvas", "GraphArea"), 4.6f, 1f)
                };
                director.Configure(camera, wattson, presenter, camera != null ? camera.GetComponent<LabCameraOrbit>() : null, targets);
                EditorUtility.SetDirty(director);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            EquipmentOutlineUpgrade.Run();
            File.WriteAllText("Logs/Cinematic-upgrade.txt", "Installed editable cinematic rigs in Scenes 8-13 at " + DateTime.Now.ToString("u"));
        }

        static CinematicFocusTarget Target(Transform root, CinematicSubject subject, Transform source, float distance, float side)
        {
            string name = "Focus - " + subject;
            var anchor = root.Find(name);
            if (anchor == null)
            {
                anchor = new GameObject(name).transform;
                anchor.SetParent(root, false);
                anchor.position = source != null ? Center(source) : DefaultPosition(subject);
            }
            GameObject highlight = null;
            if (subject != CinematicSubject.Graph)
            {
                var marker = anchor.Find("Focus highlight");
                highlight = marker != null ? marker.gameObject : null;
            }
            return new CinematicFocusTarget { subject = subject, target = anchor, cameraDistance = distance, wattsonSide = side, highlight = highlight };
        }

        static Transform Find(Transform[] all, params string[] names)
        {
            foreach (string name in names)
            {
                var exact = all.FirstOrDefault(t => string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;
            }
            return null;
        }

        static Vector3 Center(Transform source)
        {
            var renderers = source.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return source.position;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }

        static Vector3 DefaultPosition(CinematicSubject subject)
        {
            switch (subject)
            {
                case CinematicSubject.Source: return new Vector3(-3f, .8f, 2f);
                case CinematicSubject.Ammeter: return new Vector3(-.7f, .8f, 2.4f);
                case CinematicSubject.Voltmeter: return new Vector3(1.7f, .8f, 0f);
                case CinematicSubject.WireXY: return new Vector3(1.8f, .5f, 2.4f);
                case CinematicSubject.Graph: return new Vector3(0f, 1.4f, 2.2f);
                default: return Vector3.zero;
            }
        }
    }
}
#endif
