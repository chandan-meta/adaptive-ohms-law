using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Temporary read-only scene renderer; never saves or rebuilds a scene.
    public static class FigmaEnvironmentCapture
    {
        public static void Run()
        {
            const string output = "Docs/UI_v2/LatestEnvironment";
            Directory.CreateDirectory(output);
            bool asynchronous = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                var render = typeof(LabShots).GetMethod("Shot", BindingFlags.Static | BindingFlags.NonPublic);
                if (render == null) throw new InvalidOperationException("Existing scene capture method missing.");
                foreach (var id in new[] { "Scene04", "Scene05", "Scene06", "Scene09", "Scene10", "Scene11", "Scene12", "Scene13" })
                {
                    var path = Directory.GetFiles("Assets/OhmsLaw/Scenes", id + "_*.unity").Single();
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                    const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
                    foreach (var ohm in components.OfType<OhmWorker>()) ohm.Simulate(2.4f);
                    foreach (var wattson in components.OfType<WattsonController>()) wattson.Simulate(2.4f);
                    foreach (var rig in components.OfType<CircuitRig>())
                    {
                        rig.readoutsShown = false;
                        typeof(CircuitRig).GetMethod("Awake", instance).Invoke(rig, null);
                        typeof(CircuitRig).GetMethod("Update", instance).Invoke(rig, null);
                    }
                    foreach (var wire in components.OfType<WireInterior>().Where(w => w.gameObject.activeInHierarchy))
                    {
                        typeof(WireInterior).GetMethod("Awake", instance).Invoke(wire, null);
                        typeof(WireInterior).GetMethod("OnEnable", instance).Invoke(wire, null);
                        var step = typeof(WireInterior).GetMethod("Step", instance);
                        for (int frame = 0; frame < 120; frame++) step.Invoke(wire, new object[] { 1f / 60f });
                    }
                    foreach (var canvas in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true))) canvas.enabled = false;
                    var cameras = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CinemachineCamera>(true));
                    foreach (var camera in cameras.Where(c => c.name.Contains("Wire") || c.name.Contains("Bench") || id == "Scene04" || c.name.Contains("Title")))
                    {
                        string file = output + "/" + id + "_" + camera.name.Replace(' ', '_') + ".png";
                        var rotation = camera.LookAt ? Quaternion.LookRotation(camera.LookAt.position - camera.transform.position) : camera.transform.rotation;
                        render.Invoke(null, new object[] { scene, camera.transform.position, rotation, camera.Lens.FieldOfView, file });
                        if (!File.Exists(file) || new FileInfo(file).Length < 10000) throw new InvalidOperationException("Empty scene capture: " + file);
                    }
                }
            }
            finally { ShaderUtil.allowAsyncCompilation = asynchronous; }
        }
    }
}
