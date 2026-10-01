using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OhmsLaw.Editor
{
    // Scene00_ModulePlayer: the start screen, first in Build Settings, followed by Scenes 04-13 in play order.
    public static class ModulePlayerBuilder
    {
        public const string ScenePath = SceneBuilderBase.Root + "/Scenes/Scene00_ModulePlayer.unity";
        static readonly string[] Titles =
        {
            "Meet Wattson & Georg Ohm", "Journey Inside the Wire", "Adaptive Question", "Back to Georg Ohm", "From Ohm's Lab to Our Lab",
            "Build the Circuit", "Become the Experimenter", "Turn Data into a Graph", "Discover Resistance", "The Law Gets Its Name",
        };

        [MenuItem("Ohm's Law/Build Module Player")]
        public static void Build()
        {
            BlockoutAssets.Palette();
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.isLoaded) foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
            else scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(camera, scene);
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(0.05f, 0.07f, 0.1f);

            var canvasGo = new GameObject("Module Player", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var player = canvasGo.AddComponent<ModulePlayer>();
            player.entries = BlockoutAssets.SceneOrder.Select((s, i) => new ModulePlayer.Entry { scene = s, title = Titles[i] }).ToArray();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SceneBuilderBase.Shared + "/ModuleRoot.prefab");
            if (prefab) PrefabUtility.InstantiatePrefab(prefab, scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            var order = new[] { ScenePath }.Concat(BlockoutAssets.SceneOrder.Select(s => SceneBuilderBase.Root + "/Scenes/" + s + ".unity"))
                .Where(System.IO.File.Exists).Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            foreach (var old in EditorBuildSettings.scenes) if (order.All(o => o.path != old.path) && !old.path.Contains("/OhmsLaw/")) order.Add(old);
            EditorBuildSettings.scenes = order.ToArray();
            if (previous.IsValid() && previous != scene) { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
            Debug.Log("Module player built: " + ScenePath + " (first in Build Settings, then " + (order.Count - 1) + " scenes)");
        }
    }
}
