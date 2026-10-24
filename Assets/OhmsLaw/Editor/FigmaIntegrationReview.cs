using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace OhmsLaw.Editor
{
    public static class FigmaIntegrationReview
    {
        const string Key = "ohm.figma.review";
        const string PlayReport = "Logs/Blockout/play-report.txt";
        const string ReviewReport = "Logs/FigmaUI/integration-review.txt";
        const string Request = "all 2x miss hud pointer @1920x1080";
        static double titleReady, experimentReady, finaleReady;

        [Serializable] sealed class IntPreference { public string key; public bool existed; public int value; }
        [Serializable] sealed class FloatPreference { public string key; public bool existed; public float value; }
        [Serializable] sealed class Preferences { public IntPreference[] ints; public FloatPreference[] floats; }

        [MenuItem("Ohm's Law/Figma/Run integration review (preserve preferences)")]
        public static void Run() => RunRequest(Request);

        public static void RunExperiment() => RunRequest("Scene10 2x miss @1920x1080");
        public static void RunFinale() => RunRequest("Scene13 2x @1920x1080");

        static void RunRequest(string request)
        {
            if (SessionState.GetBool(Key, false) || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("A play/review run is already active.");
            var prefs = new Preferences
            {
                ints = Array.ConvertAll(new[] { "ohm.reached", "ohm.finished", "ohm.largerText", "ohm.subtitles", "ohm.reducedMotion" },
                    name => new IntPreference { key = name, existed = PlayerPrefs.HasKey(name), value = PlayerPrefs.GetInt(name) }),
                floats = Array.ConvertAll((AudioBus.Kind[])Enum.GetValues(typeof(AudioBus.Kind)), kind =>
                {
                    string name = "ohm.volume." + kind;
                    return new FloatPreference { key = name, existed = PlayerPrefs.HasKey(name), value = PlayerPrefs.GetFloat(name) };
                })
            };
            SessionState.SetString(Key + ".prefs", JsonUtility.ToJson(prefs));
            SessionState.SetBool(Key, true);
            SessionState.SetBool(Key + ".requested", false);
            SessionState.SetBool(Key + ".playing", false);
            SessionState.SetBool(Key + ".title", false);
            SessionState.SetBool(Key + ".experiment", false);
            SessionState.SetBool(Key + ".finale", false);
            SessionState.SetFloat(Key + ".started", (float)EditorApplication.timeSinceStartup);
            Directory.CreateDirectory("Logs/FigmaUI");
            titleReady = experimentReady = finaleReady = 0;
            File.WriteAllText(ReviewReport, "Figma import and gameplay integration review\nRequest: " + request + "\n");
            try
            {
                FigmaSkinImport.ConfigureAndVerify();
                FigmaSkinChecks.Run();
                BlockoutPlayTest.Run(request);
                SessionState.SetString(Key + ".reportTime", File.GetLastWriteTimeUtc(PlayReport).Ticks.ToString());
                SessionState.SetBool(Key + ".requested", true);
                Arm();
            }
            catch (Exception error)
            {
                Finish(false, "Setup failed: " + error.GetBaseException().Message);
                if (!Application.isBatchMode) throw;
            }
        }

        [InitializeOnLoadMethod]
        static void Rearm()
        {
            if (SessionState.GetBool(Key, false)) Arm();
        }

        static void Arm()
        {
            EditorApplication.update -= Watch;
            EditorApplication.update += Watch;
        }

        static void Watch()
        {
            if (!SessionState.GetBool(Key, false)) { EditorApplication.update -= Watch; return; }
            if (EditorApplication.isCompiling) return;
            try
            {
                double elapsed = EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".started", 0);
                if (elapsed > 40 * 60)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.ExitPlaymode(); return; }
                    Finish(false, "Review exceeded 40 minutes.");
                    return;
                }
                if (EditorApplication.isPlaying)
                {
                    SessionState.SetBool(Key + ".playing", true);
                    CaptureStates();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode || !SessionState.GetBool(Key + ".requested", false)) return;
                string report = File.Exists(PlayReport) ? File.ReadAllText(PlayReport) : "";
                bool current = long.TryParse(SessionState.GetString(Key + ".reportTime", ""), out long stamp)
                    && File.Exists(PlayReport) && File.GetLastWriteTimeUtc(PlayReport).Ticks >= stamp;
                if (current && report.Contains("RESULT "))
                    Finish(report.Contains("RESULT PASS"), "Gameplay harness: " + (report.Contains("RESULT PASS") ? "PASS" : "FAIL"));
                else if (SessionState.GetBool(Key + ".playing", false))
                    Finish(false, "Play mode stopped before the harness produced a result.");
                else if (elapsed > 90) Finish(false, "Play mode did not start within 90 seconds.");
            }
            catch (Exception error) { Finish(false, "Review watcher failed: " + error.GetBaseException().Message); }
        }

        static void CaptureStates()
        {
            var flow = ModuleFlow.Instance;
            var panel = LessonPanel.Instance;
            if (!flow || !panel || !Camera.main) return;
            string scene = SceneManager.GetActiveScene().name;
            bool title = TitleScreen.IsOpen;
            if (title)
            {
                var view = UnityEngine.Object.FindFirstObjectByType<TitleScreen>();
                title = view && view.TryGetComponent(out CanvasGroup fade) && fade.alpha >= 0.95f;
            }
            CaptureWhen("title", title, ref titleReady, 0.05, flow, "Logs/FigmaUI/Unity-title.png");
            CaptureWhen("experiment", scene.StartsWith("Scene10", StringComparison.Ordinal) && panel.Showing("Volt"),
                ref experimentReady, 0.6, flow, "Logs/FigmaUI/Unity-experiment.png");
            CaptureWhen("finale", scene.StartsWith("Scene13", StringComparison.Ordinal) && ExploreLab.IsOpen,
                ref finaleReady, 0.25, flow, "Logs/FigmaUI/Unity-finale.png");
        }

        static void CaptureWhen(string name, bool ready, ref double since, double delay, ModuleFlow flow, string path)
        {
            if (SessionState.GetBool(Key + "." + name, false)) return;
            if (!ready) { since = 0; return; }
            if (since == 0) since = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - since < delay) return;
            SessionState.SetBool(Key + "." + name, true);
            try
            {
                Capture(flow.GetComponent<Canvas>(), Camera.main, path);
                File.AppendAllText(ReviewReport, "CAPTURED " + path + " (actual gameplay canvas and camera; visual review pending)\n");
            }
            catch (Exception error)
            {
                string message = "CAPTURE FAILED " + path + ": " + error.GetBaseException().Message;
                File.AppendAllText(ReviewReport, message + "\n");
                Debug.LogWarning(message);
            }
        }

        static void Capture(Canvas canvas, Camera camera, string path)
        {
            if (!canvas || !camera) throw new InvalidOperationException("Gameplay canvas or main camera is unavailable.");
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                throw new InvalidOperationException("Capture expects the unchanged gameplay Overlay canvas.");
            Canvas.ForceUpdateCanvases();
            int width = Mathf.RoundToInt(canvas.pixelRect.width), height = Mathf.RoundToInt(canvas.pixelRect.height);
            if (width <= 0 || height <= 0) throw new InvalidOperationException("Gameplay canvas has no screen bounds.");
            var oldTarget = camera.targetTexture;
            float aspect = camera.aspect;
            var projection = camera.projectionMatrix;
            var active = RenderTexture.active;
            bool asynchronous = ShaderUtil.allowAsyncCompilation;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D image = null;
            bool overlayDrawn = false;
            void DrawOverlay(ScriptableRenderContext context, Camera rendered)
            {
                if (rendered != camera || overlayDrawn) return;
                // URP skips Overlay UI for RT requests; append its own UI pass after the world's postprocessing.
                var commands = CommandBufferPool.Get("Figma review: unchanged screen overlay");
                try
                {
                    var ui = context.CreateUIOverlayRendererList(camera, UISubset.UIToolkit_UGUI);
                    commands.SetRenderTarget(target);
                    commands.SetViewport(new Rect(0, 0, width, height));
                    commands.DrawRendererList(ui);
                    context.ExecuteCommandBuffer(commands);
                    context.Submit();
                    overlayDrawn = true;
                }
                finally { CommandBufferPool.Release(commands); }
            }
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                camera.targetTexture = target;
                camera.aspect = (float)width / height;
                if (GraphicsSettings.currentRenderPipeline)
                {
                    RenderPipelineManager.endCameraRendering += DrawOverlay;
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                }
                else throw new InvalidOperationException("Overlay capture requires the project's URP renderer.");
                if (!overlayDrawn) throw new InvalidOperationException("URP did not execute the overlay capture pass.");
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                byte[] bytes = image.EncodeToPNG();
                if (bytes.Length < 10000) throw new InvalidOperationException("Rendered PNG is unexpectedly empty.");
                File.WriteAllBytes(path, bytes);
            }
            finally
            {
                RenderPipelineManager.endCameraRendering -= DrawOverlay;
                camera.targetTexture = oldTarget;
                camera.aspect = aspect;
                camera.projectionMatrix = projection;
                RenderTexture.active = active;
                ShaderUtil.allowAsyncCompilation = asynchronous;
                if (image) UnityEngine.Object.DestroyImmediate(image);
                RenderTexture.ReleaseTemporary(target);
            }
        }

        static void Finish(bool passed, string message)
        {
            EditorApplication.update -= Watch;
            try
            {
                var prefs = JsonUtility.FromJson<Preferences>(SessionState.GetString(Key + ".prefs", ""));
                if (prefs == null) throw new InvalidOperationException("Saved learner preferences are missing.");
                foreach (var entry in prefs.ints)
                    if (entry.existed) PlayerPrefs.SetInt(entry.key, entry.value); else PlayerPrefs.DeleteKey(entry.key);
                foreach (var entry in prefs.floats)
                    if (entry.existed) PlayerPrefs.SetFloat(entry.key, entry.value); else PlayerPrefs.DeleteKey(entry.key);
                PlayerPrefs.Save();
                File.AppendAllText(ReviewReport, message + "\nLearner preferences restored.\nRESULT " + (passed ? "PASS" : "FAIL") + "\n");
            }
            catch (Exception error)
            {
                passed = false;
                File.WriteAllText("Logs/FigmaUI/preferences-recovery.json", SessionState.GetString(Key + ".prefs", ""));
                Debug.LogError("Figma review preference restoration failed: " + error.Message);
            }
            SessionState.SetBool(Key, false);
            SessionState.EraseString(Key + ".prefs");
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else Debug.Log("Figma integration review " + (passed ? "PASS" : "FAIL") + ": " + message);
        }
    }
}
