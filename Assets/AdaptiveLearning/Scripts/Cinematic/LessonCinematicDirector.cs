using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public enum CinematicSubject { Learner, Source, Ammeter, Voltmeter, WireXY, Graph }

    [Serializable]
    public sealed class CinematicFocusTarget
    {
        public CinematicSubject subject;
        public Transform target;
        public GameObject highlight;
        [Range(2f, 8f)] public float cameraDistance = 4.8f;
        [Range(-2f, 2f)] public float wattsonSide = 1f;
    }

    // Reusable real-time direction layer. Lesson scoring and state stay in the scene flow managers.
    public sealed class LessonCinematicDirector : MonoBehaviour
    {
        public static LessonCinematicDirector Active { get; private set; }

        [Header("Scene references — safe to tune in Inspector")]
        [SerializeField] Camera learnerCamera;
        [SerializeField] WattsonController wattson;
        [SerializeField] DialoguePresenter dialogue;
        [SerializeField] LabCameraOrbit explorationCamera;
        [SerializeField] CinematicFocusTarget[] focusTargets;

        [Header("Comfort")]
        [SerializeField, Range(.4f, 2.5f)] float shotBlendSeconds = 1.1f;
        [SerializeField, Range(1.5f, 8f)] float defaultShotDistance = 4.8f;
        [SerializeField] bool createPlaybackControls = true;

        Vector3 shotStartPosition, desiredPosition;
        Quaternion shotStartRotation, desiredRotation;
        Coroutine blend;
        Coroutine replay;
        bool applicationSuspended;
        bool windowHasFocus = true, applicationPaused;
        string lastText;
        CinematicSubject lastSubject;
        bool shotActive, chatOpen, cameraTransitioning;
        Button skipButton, replayButton;

        public bool ShotActive => shotActive;
        public bool CameraTransitioning => cameraTransitioning;

        void Awake()
        {
            Active = this;
            if (learnerCamera == null) learnerCamera = Camera.main;
            if (wattson == null) wattson = FindFirstObjectByType<WattsonController>();
            if (dialogue == null) dialogue = FindFirstObjectByType<DialoguePresenter>();
            if (learnerCamera != null && explorationCamera == null) explorationCamera = learnerCamera.GetComponent<LabCameraOrbit>();
            ResolveMissingTargets();
        }

        IEnumerator Start()
        {
            yield return null;
            if (dialogue == null) dialogue = FindFirstObjectByType<DialoguePresenter>();
            var chat = FindFirstObjectByType<WattsonChatUI>();
            if (chat != null) chat.VisibilityChanged += SetChatOpen;
            if (createPlaybackControls) BuildControls();
        }

        public void Configure(Camera camera, WattsonController companion, DialoguePresenter presenter,
            LabCameraOrbit orbit, CinematicFocusTarget[] targets)
        {
            learnerCamera = camera; wattson = companion; dialogue = presenter; explorationCamera = orbit;
            focusTargets = targets; ResolveMissingTargets();
        }

        public void PresentLine(DialogueLine line)
        {
            if (line == null) return;
            if (!line.overrideCinematicSubject) { PresentSpokenText(line.text); return; }
            lastText = line.text; lastSubject = line.cinematicSubject;
            if (!chatOpen && !applicationSuspended) BeginShot(lastSubject);
        }

        public void PresentSpokenText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || learnerCamera == null || chatOpen) return;
            lastText = text;
            lastSubject = InferSubject(text);
            BeginShot(lastSubject);
        }

        public void SkipCurrentExplanation()
        {
            if (dialogue != null && dialogue.IsPresenting) dialogue.SkipCurrentLine();
            wattson?.StopChatReaction();
            EndShot();
        }

        public void ReplayCurrentExplanation()
        {
            if (string.IsNullOrWhiteSpace(lastText) || chatOpen) return;
            BeginShot(lastSubject);
            if (dialogue != null && dialogue.CurrentLine != null) dialogue.ReplayCurrentLine();
            else
            {
                if (replay != null) StopCoroutine(replay);
                if (dialogue != null)
                {
                    // Replays use the same subtitle/voice lifecycle as authored lines.
                    // No scene-flow event or assessment action is attached.
                    dialogue.Show(new DialogueLine
                    {
                        speaker = "WATTSON", text = lastText,
                        duration = Mathf.Max(3f, lastText.Split(' ').Length / 2.5f),
                        overrideCinematicSubject = true, cinematicSubject = lastSubject
                    });
                }
                else
                {
                    wattson?.Speech?.Speak(lastText);
                    replay = StartCoroutine(FinishReplay());
                }
            }
        }

        IEnumerator FinishReplay()
        {
            yield return null;
            while (wattson != null && wattson.Speech != null && wattson.Speech.IsSpeaking) yield return null;
            replay = null; EndShot();
        }

        public void EndShot()
        {
            SetHighlight(null);
            if (!shotActive || learnerCamera == null) return;
            shotActive = false;
            wattson?.ReturnToHome();
            StartBlend(learnerCamera.transform.position, learnerCamera.transform.rotation, shotStartPosition, shotStartRotation, false);
        }

        void BeginShot(CinematicSubject subject)
        {
            // Graph activities use a screen-space evidence display. Moving the 3D
            // camera toward screen coordinates creates a disorienting close-up.
            if (subject == CinematicSubject.Graph || IsScreenGraphVisible())
            {
                EndShot();
                wattson?.FaceLearner();
                return;
            }
            var entry = focusTargets?.FirstOrDefault(t => t != null && t.subject == subject && t.target != null);
            var targetCanvas = entry != null ? entry.target.GetComponentInParent<Canvas>() : null;
            if (targetCanvas != null && targetCanvas.renderMode != RenderMode.WorldSpace) entry = null;
            if (subject == CinematicSubject.Learner || entry == null)
            {
                EndShot();
                wattson?.FaceLearner();
                return;
            }

            if (!shotActive && !cameraTransitioning)
            {
                shotStartPosition = learnerCamera.transform.position;
                shotStartRotation = learnerCamera.transform.rotation;
            }
            shotActive = true;
            SetHighlight(entry);
            explorationCamera?.SetSuspended(true);

            var target = entry.target;
            Vector3 focus = TargetCenter(target);
            Vector3 away = learnerCamera.transform.position - focus;
            if (away.sqrMagnitude < .01f) away = Vector3.back;
            away.Normalize();
            float distance = entry.cameraDistance > 0 ? entry.cameraDistance : defaultShotDistance;
            // Frame the companion as well as the apparatus. A tight instrument-only
            // shot clips Wattson when he arrives beside it, particularly on tablets.
            focus += Vector3.up * .65f;
            float verticalHalfAngle = learnerCamera.fieldOfView * Mathf.Deg2Rad * .5f;
            float horizontalHalfAngle = Mathf.Atan(Mathf.Tan(verticalHalfAngle) * learnerCamera.aspect);
            float framingDistance = 2.2f / Mathf.Tan(Mathf.Min(verticalHalfAngle, horizontalHalfAngle));
            distance = Mathf.Max(distance, framingDistance);
            desiredPosition = focus + away * distance + Vector3.up * .45f;
            desiredRotation = Quaternion.LookRotation(focus - desiredPosition, Vector3.up);
            wattson?.MoveBeside(target, entry.wattsonSide);
            StartBlend(learnerCamera.transform.position, learnerCamera.transform.rotation, desiredPosition, desiredRotation, true);
        }

        void SetHighlight(CinematicFocusTarget selected)
        {
            if (focusTargets == null) return;
            foreach (var entry in focusTargets)
                if (entry != null && entry.highlight != null) entry.highlight.SetActive(entry == selected);
        }

        static bool IsScreenGraphVisible()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.WorldSpace) continue;
                var graph = canvas.GetComponentsInChildren<Transform>(false)
                    .FirstOrDefault(t => t.name == "GraphPanel" || t.name == "ResistanceGraphView" || t.name == "GraphArea");
                if (graph != null && graph.gameObject.activeInHierarchy) return true;
            }
            return false;
        }

        void StartBlend(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation, bool movingIntoShot)
        {
            if (blend != null) StopCoroutine(blend);
            cameraTransitioning = true;
            blend = StartCoroutine(BlendCamera(fromPosition, fromRotation, toPosition, toRotation, movingIntoShot));
        }

        IEnumerator BlendCamera(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation, bool movingIntoShot)
        {
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(.1f, shotBlendSeconds))
            {
                while (applicationSuspended) yield return null;
                float eased = t * t * (3f - 2f * t);
                learnerCamera.transform.SetPositionAndRotation(Vector3.Lerp(fromPosition, toPosition, eased), Quaternion.Slerp(fromRotation, toRotation, eased));
                yield return null;
            }
            learnerCamera.transform.SetPositionAndRotation(toPosition, toRotation);
            blend = null;
            cameraTransitioning = false;
            explorationCamera?.SetSuspended(chatOpen || shotActive || applicationSuspended);
        }

        void SetChatOpen(bool open)
        {
            chatOpen = open;
            if (open) EndShot();
            dialogue?.PauseVoice(open || applicationSuspended);
            explorationCamera?.SetSuspended(open || shotActive || cameraTransitioning || applicationSuspended);
        }

        void OnApplicationFocus(bool focused) { windowHasFocus = focused; SetApplicationSuspended(!windowHasFocus || applicationPaused); }
        void OnApplicationPause(bool paused) { applicationPaused = paused; SetApplicationSuspended(!windowHasFocus || applicationPaused); }
        void SetApplicationSuspended(bool value)
        {
            if (Application.isBatchMode) return;
            applicationSuspended = value;
            wattson?.SetPresentationPaused(value);
            dialogue?.PauseVoice(value || chatOpen);
            explorationCamera?.SetSuspended(value || chatOpen || shotActive || cameraTransitioning);
        }

        void BuildControls()
        {
            var canvas = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
            if (canvas == null || canvas.transform.Find("CinematicControls") != null) return;
            var root = new GameObject("CinematicControls", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(.635f, .91f); rect.anchorMax = new Vector2(.79f, .98f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            skipButton = MakeButton("SkipExplanation", rect, "SKIP", new Vector2(0, 0), new Vector2(.48f, 1));
            replayButton = MakeButton("ReplayExplanation", rect, "REPLAY", new Vector2(.52f, 0), Vector2.one);
            skipButton.onClick.AddListener(SkipCurrentExplanation);
            replayButton.onClick.AddListener(ReplayCurrentExplanation);
            WarmLabTheme.Current?.Apply(rect);
        }

        static Button MakeButton(string name, Transform parent, string label, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(.015f, .10f, .15f, .96f);
            var outline = go.GetComponent<Outline>(); outline.effectColor = new Color(.05f, .83f, .95f, .8f); outline.effectDistance = new Vector2(1, -1);
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text)); textGo.transform.SetParent(go.transform, false);
            var tr = (RectTransform)textGo.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = label; text.fontSize = 24; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        void ResolveMissingTargets()
        {
            if (focusTargets == null || focusTargets.Length == 0)
            {
                focusTargets = new[]
                {
                    NewTarget(CinematicSubject.Source, FindNamed("DC SOURCE", "DCSource", "VoltageSource"), 4.8f, -1f),
                    NewTarget(CinematicSubject.Ammeter, FindNamed("AMMETER  /  I (A)", "Ammeter"), 4.2f, 1f),
                    NewTarget(CinematicSubject.Voltmeter, FindNamed("VOLTMETER", "Voltmeter"), 4.2f, -1f),
                    NewTarget(CinematicSubject.WireXY, FindNamed("NICHROME WIRE  XY", "NichromeWireXY", "WireHighlight"), 3.8f, 1f),
                    NewTarget(CinematicSubject.Graph, FindNamed("GraphPanel", "GraphCanvas", "GraphArea"), 4.6f, 1f)
                };
            }
        }

        static CinematicFocusTarget NewTarget(CinematicSubject subject, Transform target, float distance, float side) =>
            new CinematicFocusTarget { subject = subject, target = target, cameraDistance = distance, wattsonSide = side };

        static Transform FindNamed(params string[] names)
        {
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (string name in names)
            {
                var exact = all.FirstOrDefault(t => string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;
            }
            return null;
        }

        static Vector3 TargetCenter(Transform target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return target.position;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }

        static CinematicSubject InferSubject(string text)
        {
            string value = text.ToLowerInvariant();
            if (value.Contains("?")) return CinematicSubject.Learner;
            if (value.Contains("graph") || value.Contains("slope") || value.Contains("line")) return CinematicSubject.Graph;
            if (value.Contains("ammeter") || value.Contains("current") || value.Contains("flow")) return CinematicSubject.Ammeter;
            if (value.Contains("voltmeter") || value.Contains("potential difference")) return CinematicSubject.Voltmeter;
            if (value.Contains("resistance") || value.Contains("nichrome") || value.Contains("wire") || value.Contains("conductor")) return CinematicSubject.WireXY;
            if (value.Contains("voltage") || value.Contains("battery") || value.Contains("source")) return CinematicSubject.Source;
            return CinematicSubject.Learner;
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
        }
    }

    public static class CinematicBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (!scene.StartsWith("Scene_0") && !scene.StartsWith("Scene_1")) return;
            if (scene != "Scene_08_OurLabIntroduction" && scene != "Scene_09_BuildTheCircuit" &&
                scene != "Scene_10_BecomeTheExperimenter" && scene != "Scene_11_TurnDataIntoGraph" &&
                scene != "Scene_12_DiscoverResistance" && scene != "Scene_13_OhmsLaw") return;
            if (UnityEngine.Object.FindFirstObjectByType<LessonCinematicDirector>() == null)
                new GameObject("Cinematic Direction - edit targets").AddComponent<LessonCinematicDirector>();
        }
    }
}
