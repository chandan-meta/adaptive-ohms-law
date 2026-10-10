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
    // Persistent module root: scene order, white-fade transitions, cinematic pausing and the shared UI (the one
    // lesson panel, which hosts the captions and the Ask Wattson conversation).
    [DefaultExecutionOrder(-100)]
    public class ModuleFlow : MonoBehaviour
    {
        [Flags] public enum PauseReason { User = 1, Ask = 2, Hold = 4 }

        public static ModuleFlow Instance { get; private set; }
        // Raised once Scene 13 reaches its finale; Scene 14 onward are external videos.
        public static event Action UnitySectionComplete;
        public static event Action<string> Leaving; // a scene is about to hand off (before the fade or the cut); the play test shoots its last frame

        public ModulePalette palette;
        public ProxyConfig proxy;
        [Tooltip("Scene names in play order, Scene 4 to Scene 13.")] public string[] scenes;
        [Tooltip("Shows Skip without a long-press.")] public bool debugMode;
        public float fadeSeconds = 0.6f;
        [Tooltip("Optional path or URL of the Scene 14 video, played after the finale. Empty holds on the finale.")] public string scene14Video;

        public PlayableDirector Director { get; private set; }
        public WattsonAgent Agent { get; private set; }
        public LessonPanel Panel { get; private set; }
        public bool Ended { get; private set; }

        Image fade;
        TextMeshProUGUI devNote;
        PauseReason paused;
        Scene entered;
        Coroutine fading;
        bool loading;

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
            gameObject.AddComponent<HudScale>();   // never below 75 % (and Larger text)
            gameObject.AddComponent<HudKeys>();
            gameObject.AddComponent<GraphicRaycaster>();

            var safe = UI.Stretch("SafeArea", transform).gameObject;
            safe.AddComponent<AdaptiveOhmsLaw.SafeAreaLayout>();
            Agent = gameObject.AddComponent<WattsonAgent>();
            Panel = LessonPanel.Create(safe.transform);   // first: the captions and the conversation build inside it
            Panel.Root.gameObject.AddComponent<ChapterBanner>();
            safe.AddComponent<SubtitleUI>();
            safe.AddComponent<AskWattsonUI>();
            safe.AddComponent<FlowOverlay>();
            gameObject.AddComponent<AudioDucker>(); // lowers music and effects while anyone speaks

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
            loading = false;
            paused = 0;
            Ended = false;
            Director = FindFirstObjectByType<PlayableDirector>();
            if (!FindFirstObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            // The 3D view composes into the part of the screen the panel leaves free.
            var cam = Camera.main;
            if (cam && !cam.GetComponent<PanelFraming>()) cam.gameObject.AddComponent<PanelFraming>();
            if (cam && !cam.GetComponent<LookAround>()) cam.gameObject.AddComponent<LookAround>();   // v5: a drag on the 3D view looks around the shot
            var sceneDirector = Director ? Director.GetComponent<SceneDirector>() : null;
            if (Panel) Panel.ResetForScene("", sceneDirector ? sceneDirector.DockWidth : 0f);
            int chapter = ChapterIndex;
            if (chapter >= 0) Reached = Mathf.Max(Reached, chapter + 1);
            if (ChapterBanner.Instance) ChapterBanner.Instance.Enter(chapter);
            if (PauseMenu.IsOpen) PauseMenu.Close();
            TitleScreen.MaybeOpen(scene.name);

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
            RegisterVoices();
            devNote.text = "";
            FadeTo(0);
        }

        // Every voice joins the VoiceChannel, so speakers take turns and the ducker leaves voices alone: Wattson's scripted
        // and live-answer sources, and any other source on the Voice bus (inactive ones too; Wattson may appear later).
        static void RegisterVoices()
        {
            foreach (var wattson in FindObjectsByType<WattsonController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                VoiceChannel.Register(wattson.voice);
                VoiceChannel.Register(wattson.liveVoice);
            }
            foreach (var bus in FindObjectsByType<AudioBus>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (bus.kind == AudioBus.Kind.Voice && bus.TryGetComponent(out AudioSource source)) VoiceChannel.Register(source);
        }

        public bool IsPaused(PauseReason reason) => (paused & reason) != 0;

        // The white fade covers the screen: the scene was entered through a fade (or the module just started), not a cut.
        public bool Covered => fade && fade.color.a > 0.5f;

        // The learner pressed PAUSE. The gates run outside the Timeline, so their lines, timers and inputs read this.
        public static bool UserPaused => Instance && Instance.IsPaused(PauseReason.User);

        // The scene's Timeline is playing at normal speed (nothing holds it): its scripted voice is audible.
        public bool TimelineRunning => paused == 0 && Director && Director.state == PlayState.Playing;

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

        public void JumpToScene(string scene) { returnTo = null; StartCoroutine(Load(scene, true)); }

        // The chapter (0-based index in `scenes`) the active scene is, or -1 outside the module (the player scene).
        public int ChapterIndex => scenes == null ? -1 : Array.IndexOf(scenes, SceneManager.GetActiveScene().name);

        // How far this learner has got (chapters reached, 1-based; every chapter after the finale). The title screen's
        // Continue and the pause menu's chapter list read it.
        public static int Reached
        {
            get => PlayerPrefs.GetInt("ohm.reached", 0);
            set { PlayerPrefs.SetInt("ohm.reached", value); PlayerPrefs.Save(); }
        }

        // The finale has been reached once: no Continue on the title after that (Reached alone cannot tell a learner who
        // left during the last chapter from one who finished it).
        public static bool Finished
        {
            get => PlayerPrefs.GetInt("ohm.finished", 0) == 1;
            set { PlayerPrefs.SetInt("ohm.finished", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // Replaying a finished chapter from the pause menu: once it ends, the learner comes back to the chapter they left.
        string returnTo;
        public void ReplayChapter(int index)
        {
            int now = ChapterIndex;
            if (index < 0 || index >= scenes.Length) return;
            returnTo = now > index ? scenes[now] : null;
            StartCoroutine(Load(scenes[index], true));
        }

        string NextScene()
        {
            string active = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(returnTo) && returnTo != active) { var back = returnTo; returnTo = null; return back; }
            int index = Array.IndexOf(scenes, active);
            return index >= 0 && index + 1 < scenes.Length ? scenes[index + 1] : null;
        }

        public static void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL || UNITY_IOS
            TitleScreen.Again();   // a browser tab (or an iPad app) is not closed by the page: back to the title screen
#else
            Application.Quit();
#endif
        }

        // The finale (Scene 13's "end" cue): the composition holds, the panel offers replay and exit, and the optional
        // Scene 14 video plays over it. Safe to call twice.
        public void EndOfModule()
        {
            if (Ended) return;
            Ended = true;
            Reached = scenes != null ? scenes.Length : Reached;   // the finale opens every chapter
            Finished = true;
            UnitySectionComplete?.Invoke();
            if (ChapterBanner.Instance) ChapterBanner.Instance.Complete();
            if (Panel) Panel.End(true);
            if (string.IsNullOrEmpty(scene14Video) || !Camera.main) return;
            var video = Camera.main.gameObject.AddComponent<VideoPlayer>();
            video.renderMode = VideoRenderMode.CameraNearPlane;
            video.url = scene14Video.Contains("://") ? scene14Video : "file://" + scene14Video;
            video.Play();
        }

        IEnumerator Load(string next, bool fadeOut)
        {
            if (loading) yield break; // repeated Skip / chapter taps share one transition
            loading = true;
            Leaving?.Invoke(next);
            if (fadeOut) yield return FadeTo(1);
            if (next != null && Application.CanStreamedLevelBeLoaded(next)) { yield return SceneManager.LoadSceneAsync(next); yield break; }
            if (!fadeOut) yield return FadeTo(1);
            // Holding on the card: the scene underneath stops (a SKIP past the last scene arrives here mid-Timeline or
            // mid-gate), and no voice keeps talking under it.
            var director = Director ? Director.GetComponent<SceneDirector>() : null;
            if (director) director.Shutdown();
            if (Director) Director.Stop();
            foreach (var wattson in FindObjectsByType<WattsonController>(FindObjectsSortMode.None))
            {
                if (wattson.voice) wattson.voice.Stop();
                if (wattson.liveVoice) wattson.liveVoice.Stop();
            }
            if (next != null) { devNote.text = next + " is not built yet. Holding on white."; yield break; }

            // Past the last scene (a skip out of the finale): the end of the module, on a quiet card.
            devNote.text = "End of module";
            devNote.fontSize = 48;
            devNote.color = new Color(0, 0, 0, 0.7f);
            devNote.alignment = TextAlignmentOptions.Center;
            EndOfModule();
            loading = false;
            if (!string.IsNullOrEmpty(scene14Video)) yield return FadeTo(0);
        }

        // White fade for scene beats (Timeline cue "fade 1" / "fade 0", "fade 1 0.15" for a flash); seconds < 0 = fadeSeconds.
        public Coroutine FadeWhite(float alpha, float seconds = -1f) => FadeTo(alpha, seconds);

        Coroutine FadeTo(float alpha, float seconds = -1f)
        {
            if (fading != null) StopCoroutine(fading);
            return fading = StartCoroutine(Fade(alpha, seconds > 0f ? seconds : fadeSeconds));
        }

        // Reduced motion: the flash goes to a warm off-white instead of hard white, read every frame so the HUD toggle takes
        // mid-fade. The length stays: Scenes 5 and 6 swap their sets 0.6-0.8 s after "fade 1" and need the cover by then.
        static readonly Color SoftWhite = new Color(0.86f, 0.84f, 0.80f);
        static Color FadeColor => ReducedMotion.On ? SoftWhite : Color.white;

        IEnumerator Fade(float alpha, float seconds)
        {
            fade.raycastTarget = true;
            float from = fade.color.a;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / seconds)
            {
                fade.color = UI.Alpha(FadeColor, Mathf.Lerp(from, alpha, t));
                yield return null;
            }
            fade.color = UI.Alpha(FadeColor, alpha);
            fade.raycastTarget = alpha > 0;
        }
    }
}
