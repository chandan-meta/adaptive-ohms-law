using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace OhmsLaw
{
    // Persistent module root: scene order, white-fade transitions, cinematic pausing and the shared UI.
    [DefaultExecutionOrder(-100)]
    public class ModuleFlow : MonoBehaviour
    {
        [Flags] public enum PauseReason { User = 1, Ask = 2, Hold = 4 }

        public static ModuleFlow Instance { get; private set; }
        // Raised once Scene 13 hands off; Scene 14 onward are external videos.
        public static event Action UnitySectionComplete;
        public static event Action<string> Leaving; // a scene is about to hand off (before the fade or the cut); the play test shoots its last frame

        public ModulePalette palette;
        public ProxyConfig proxy;
        [Tooltip("Scene names in play order, Scene 4 to Scene 13.")] public string[] scenes;
        [Tooltip("Shows Skip without a long-press.")] public bool debugMode;
        public float fadeSeconds = 0.6f;
        [Tooltip("Optional path or URL of the Scene 14 video, played after Scene 13. Empty holds on the end card.")] public string scene14Video;

        public PlayableDirector Director { get; private set; }
        public WattsonAgent Agent { get; private set; }

        Image fade;
        TextMeshProUGUI devNote;
        PauseReason paused;
        Scene entered;
        Coroutine fading;

        void Awake()
        {
            if (Instance) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            UI.Theme = palette;
            Application.targetFrameRate = 60;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var safe = UI.Stretch("SafeArea", transform).gameObject;
            safe.AddComponent<AdaptiveOhmsLaw.SafeAreaLayout>();
            Agent = gameObject.AddComponent<WattsonAgent>();
            safe.AddComponent<SubtitleUI>();
            safe.AddComponent<CinematicControlsUI>();
            safe.AddComponent<AskWattsonUI>();
            safe.AddComponent<FlowOverlay>();

            fade = UI.Stretch("WhiteFade", transform).gameObject.AddComponent<Image>();
            fade.color = Color.white;
            devNote = UI.Label(fade.transform, "", 26, new Color(0, 0, 0, 0.45f), TextAlignmentOptions.Bottom);
            devNote.rectTransform.offsetMin = new Vector2(0, 40);

            SceneManager.sceneLoaded += (_, _) => EnterScene();
        }

        void Start() => EnterScene();

        void EnterScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene == entered) return;
            entered = scene;
            paused = 0;
            Director = FindFirstObjectByType<PlayableDirector>();
            if (!FindFirstObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Medium tier for tablets: half the particles, no point-light shadows.
            if (Application.isMobilePlatform)
            {
                foreach (var system in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                {
                    var main = system.main;
                    main.maxParticles = Mathf.Max(8, main.maxParticles / 2);
                }
                foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (light.type != LightType.Directional) light.shadows = LightShadows.None;
            }
            devNote.text = "";
            FadeTo(0);
        }

        public bool IsPaused(PauseReason reason) => (paused & reason) != 0;

        // The Timeline is frozen by setting its speed to zero, so Cinemachine keeps the current shot on screen.
        public void SetPaused(PauseReason reason, bool on)
        {
            paused = on ? paused | reason : paused & ~reason;
            if (Director && Director.playableGraph.IsValid())
                Director.playableGraph.GetRootPlayable(0).SetSpeed(paused == 0 ? 1 : 0);
        }

        public void JumpTo(double time)
        {
            if (!Director) return;
            Director.time = Math.Max(0, time);
            Director.Evaluate();
        }

        // Signal target at the end of every scene: flash to white, then load the next scene in the list (Next()).
        public void CompleteScene() => StartCoroutine(Load(NextScene(), true));

        // Match-cut: the next scene starts on the same frame, with no white in between.
        public void Cut() => StartCoroutine(Load(NextScene(), false));

        public void JumpToScene(string scene) => StartCoroutine(Load(scene, true));

        string NextScene()
        {
            int index = Array.IndexOf(scenes, SceneManager.GetActiveScene().name);
            return index >= 0 && index + 1 < scenes.Length ? scenes[index + 1] : null;
        }

        IEnumerator Load(string next, bool fadeOut)
        {
            Leaving?.Invoke(next);
            if (fadeOut) yield return FadeTo(1);
            if (next != null && Application.CanStreamedLevelBeLoaded(next)) { yield return SceneManager.LoadSceneAsync(next); yield break; }
            if (!fadeOut) yield return FadeTo(1);
            if (next != null) { devNote.text = next + " is not built yet. Holding on white."; yield break; }

            // End of the Unity section.
            UnitySectionComplete?.Invoke();
            devNote.text = "→ Scene 14 (video)";
            devNote.fontSize = 64;
            devNote.color = new Color(0, 0, 0, 0.7f);
            devNote.alignment = TextAlignmentOptions.Center;
            if (string.IsNullOrEmpty(scene14Video) || !Camera.main) yield break;
            var video = Camera.main.gameObject.AddComponent<VideoPlayer>();
            video.renderMode = VideoRenderMode.CameraNearPlane;
            video.url = scene14Video.Contains("://") ? scene14Video : "file://" + scene14Video;
            video.Play();
            yield return FadeTo(0);
        }

        // White fade for scene beats (Timeline cue "fade 1" / "fade 0").
        public Coroutine FadeWhite(float alpha) => FadeTo(alpha);

        Coroutine FadeTo(float alpha)
        {
            if (fading != null) StopCoroutine(fading);
            return fading = StartCoroutine(Fade(alpha));
        }

        IEnumerator Fade(float alpha)
        {
            fade.raycastTarget = true;
            float from = fade.color.a;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / fadeSeconds)
            {
                fade.color = new Color(1, 1, 1, Mathf.Lerp(from, alpha, t));
                yield return null;
            }
            fade.color = new Color(1, 1, 1, alpha);
            fade.raycastTarget = alpha > 0;
        }
    }
}
