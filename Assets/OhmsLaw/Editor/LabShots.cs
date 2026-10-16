using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace OhmsLaw.Editor
{
    // Stills of the lab scenes (9-13) from their bench cameras, rendered in edit mode with the scene's lights, probe and
    // post-processing: a look at the room and its lighting without play mode, also from batch mode with the editor closed
    // (BlockoutCommands.Batch "stills"). Writes Logs/LabSet/<scene>_<camera>.png.
    public static class LabShots
    {
        static readonly string[] Cameras = { "Cam Bench Wide", "Cam Bench Close", "Cam Bench Present", "Cam Table Close" };
        // Scene 9 only: the whole room from its entrance end toward the board, framed like the user's lighting reference
        // (inside the 85 % lab, whose entrance wall is at z -5.6), and along the bench top at table height, so the
        // apparatus feet show on the top.
        static readonly Vector3 RoomCam = new Vector3(0f, 1.6f, -5.0f), RoomLook = new Vector3(0f, 1.35f, 3.4f);
        static readonly Vector3 EdgeCam = new Vector3(-1.7f, 0.86f, -0.95f), EdgeLook = new Vector3(0.2f, 0.82f, 0.1f);

        [MenuItem("Ohm's Law/Polish/Lab stills (Scenes 9-13)")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs/LabSet");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            bool async = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false; // a variant still compiling renders as a placeholder (magenta in batch mode)
            try
            {
                foreach (var id in new[] { "Scene09", "Scene10", "Scene11", "Scene12", "Scene13" })
                {
                    var path = Directory.GetFiles("Assets/OhmsLaw/Scenes", id + "_*.unity").FirstOrDefault();
                    if (path == null) continue;
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var vcams = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CinemachineCamera>(true)).Where(v => Cameras.Contains(v.name));
                    foreach (var vcam in vcams)
                        Shot(scene, vcam.transform.position, vcam.LookAt ? Quaternion.LookRotation(vcam.LookAt.position - vcam.transform.position) : vcam.transform.rotation,
                            vcam.Lens.FieldOfView, "Logs/LabSet/" + id + "_" + vcam.name.Replace("Cam ", "").Replace(' ', '_') + ".png");
                    if (id == "Scene09")
                    {
                        Shot(scene, RoomCam, Quaternion.LookRotation(RoomLook - RoomCam), 55f, "Logs/LabSet/Scene09_Room.png");
                        Shot(scene, EdgeCam, Quaternion.LookRotation(EdgeLook - EdgeCam), 40f, "Logs/LabSet/Scene09_Bench_Edge.png");
                    }
                }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = async;
                if (!Application.isBatchMode && setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void Shot(Scene scene, Vector3 at, Quaternion rotation, float fov, string file)
        {
            var main = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.CompareTag("MainCamera"));
            var go = new GameObject("Still Camera");
            SceneManager.MoveGameObjectToScene(go, scene);
            var cam = go.AddComponent<Camera>();
            if (main) cam.CopyFrom(main);
            cam.scene = scene;
            go.transform.SetPositionAndRotation(at, rotation);
            cam.fieldOfView = fov;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

            const int width = 1280, height = 720;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            File.WriteAllBytes(file, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(go);
            Debug.Log("still " + file);
        }
    }
}
