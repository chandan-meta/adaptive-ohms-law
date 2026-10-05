using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace OhmsLaw
{
    // One per blockout scene, on the PlayableDirector. Applies Timeline cues, holds the Timeline at interaction
    // gates while the learner answers, speaks branch lines, and hands the scene on to the next one.
    public partial class SceneDirector : MonoBehaviour, INotificationReceiver
    {
        public enum BuildMode { LearnerPlaces, AutoPlace }

        public static DialogueLine Speaking { get; private set; }
        public static string Gate { get; private set; }
        // The highest voltage the learner (slider) and Ask Wattson may set while the controls are open. Below the source's
        // 5 V only while a reading must stay unseen: S10_Set1V / S10_Record cap it at 1 V, so nobody reads 2 V before the
        // S10_Predict2V prediction. Hold() lifts it after every gate.
        public static float ControlsMax { get; private set; } = CircuitRig.MaxVolts;

        public string sceneId;
        [Tooltip("Start on the previous scene's last camera pose when arriving from this scene.")] public string matchCutFrom;
        public CinemachineCamera entryCamera;

        [Header("Review toggles")]
        [Tooltip("Scene 9: the learner drags the meters in, or they fly in by themselves (the key tap stays).")] public BuildMode buildMode = BuildMode.LearnerPlaces;
        [Tooltip("Scene 9: add \"Your prediction: ...\" from Scene 6 under the reminder card (Reminder() leaves it off when none was recorded).")] public bool showLearnerPrediction = true;
        [Tooltip("Scene 12: the tunnel narrows while the material opposes the flow (a metaphor, not real geometry).")] public bool narrowingMetaphor = true;
        [Tooltip("Scene 12: keep the table and graph inside the wire instead of cutting back to the bench.")] public bool tableInWire;
        [Tooltip("Scenes 12-13: ask the storyboard's adaptive questions (S12_AQ1 = AQ1 / AQ_EASY, S13_AQ2 = AQ2).")] public bool checkpointEnabled = true;
        [TextArea] public string checkpointPlacement = "AQ1: Scene 12 gate S12_AQ1, after resistance is defined. AQ2: Scene 13 gate S13_AQ2, after V = IR. (The end-of-module S13_Checkpoint cluster is gone.)";

        [Header("Scene parts")]
        public WattsonController wattson;
        public WireInterior wire;
        public CircuitRig rig;
        public ObservationTableUI table;
        public GraphUI graph;
        [Tooltip("Objects cues and gates refer to by name.")] public GameObject[] refs = new GameObject[0];
        public AudioSource voice, sfx;
        public AudioClip click, pop;

        RectTransform prompt;
        string promptText = "";
        Slider slider;
        TMP_Text[] tickLabels = new TMP_Text[0];
        float ticksShownMax = -1f;
        bool capPushed; // someone asked for more than ControlsMax while it was lowered: the gate says why the knob stops
        CalloutLayer callouts; // screen-space callouts; created under the module canvas on first use, persists across scenes

        static ModuleFlow Flow => ModuleFlow.Instance;
        GameObject Ref(string name) => refs.FirstOrDefault(r => r && r.name == name);

        // A gate line holds while the Ask panel is open or the learner pressed PAUSE (the Timeline is already frozen by then;
        // the gates run outside it, so they hold themselves). Gate timers (settle, idle hint, reveal beats) advance by Dt,
        // which is zero under PAUSE; autoAnswer timers also stop while the Ask panel is open (each caller checks IsOpen).
        static bool Held => AskWattsonUI.IsOpen || ModuleFlow.UserPaused;
        static float Dt => ModuleFlow.UserPaused ? 0f : Time.deltaTime;

        // WaitForSeconds that PAUSE holds (for beats inside gate coroutines).
        static IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds; t += Dt) yield return null;
        }

        void Start()
        {
            Speaking = null;
            Gate = null;
            ControlsMax = CircuitRig.MaxVolts;
            bool matchCut = !string.IsNullOrEmpty(matchCutFrom) && SceneHandoffState.fromScene == matchCutFrom;
            if (matchCut && rig) rig.Restore(SceneHandoffState.circuit); // readings carry across 9 -> 10 -> 11 -> 12 -> 13
            if (entryCamera && SceneHandoffState.hasCamera && matchCut)
            {
                entryCamera.transform.SetPositionAndRotation(SceneHandoffState.cameraPosition, SceneHandoffState.cameraRotation);
                var lens = entryCamera.Lens;
                lens.FieldOfView = SceneHandoffState.fieldOfView;
                entryCamera.Lens = lens;
            }
        }

        // The prompt pill sits top-centre; while the Ask panel is open it moves right until it clears the panel (x 24-544,
        // plus air), kept on screen. The slider mirrors the rig (cues and Ask Wattson move the knob too) and takes input only
        // while an activity has opened the controls. While ControlsMax is lowered, the ticks above it dim, and a setting
        // above it made through the open controls (a request that did not check the cap) is undone at once.
        void Update()
        {
            if (prompt)
            {
                var safe = (RectTransform)prompt.parent;
                float half = prompt.sizeDelta.x / 2, push = AskWattsonUI.IsOpen ? Mathf.Max(0, AskPanelClear - (safe.rect.width / 2 - half)) : 0;
                prompt.anchoredPosition = new Vector2(Mathf.Min(push, safe.rect.width / 2 - half - 24f), PromptY);
            }
            if (!rig) return;
            if (rig.controlsOpen && rig.state.voltage > ControlsMax + 0.001f) { rig.SetVoltage(ControlsMax); capPushed = true; }
            if (!slider) return;
            slider.SetValueWithoutNotify(rig.state.voltage);
            bool open = rig.controlsOpen && !ModuleFlow.UserPaused; // PAUSE holds the knob too
            if (slider.interactable != open) slider.interactable = open;
            if (!Mathf.Approximately(ticksShownMax, ControlsMax))
            {
                ticksShownMax = ControlsMax;
                for (int i = 0; i < tickLabels.Length; i++) if (tickLabels[i]) tickLabels[i].color = new Color(1, 1, 1, i > ControlsMax + 0.001f ? 0.3f : 0.85f);
            }
        }

        // The slider's request: whole volts, never above ControlsMax (the knob stops there).
        float LearnerVoltage(float volts)
        {
            float want = Mathf.Round(volts);
            if (want > ControlsMax + 0.001f) { want = ControlsMax; capPushed = true; }
            return rig.SetVoltage(want);
        }

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (!Application.isPlaying) return;
            if (notification is Cue cue) Run(cue.command);
            else if (notification is InteractionGate gate && Gate == null) StartCoroutine(Hold(gate));
        }

        IEnumerator Hold(InteractionGate gate)
        {
            Gate = gate.gateId;
            if (Flow) Flow.SetPaused(ModuleFlow.PauseReason.Hold, true);
            yield return RunGate(gate);
            if (rig) { rig.controlsOpen = false; rig.targetAmps = -1f; } // whatever the gate did, cue-driven time follows
            ControlsMax = CircuitRig.MaxVolts;
            capPushed = false;
            Gate = null;
            if (Flow) Flow.SetPaused(ModuleFlow.PauseReason.Hold, false);
        }

        // Speaks a line outside the Timeline (interaction branches): subtitle, voice and Wattson's talking glow. It takes its
        // turn on the voice channel first, so it never starts over an Ask Wattson answer. Speaking is set before that wait:
        // StopSaying can still cancel a line that has not started, and autoAnswer (Pick) waits for the whole line.
        public IEnumerator Say(DialogueLine line)
        {
            if (!line) yield break;
            Speaking = line;
            yield return VoiceChannel.WaitTurn(voice);
            SubtitleTrack.Started(line);
            if (voice) { voice.clip = line.vo; if (line.vo) voice.Play(); }
            float length = line.Duration;
            for (float t = 0; t < length; t += Dt)
            {
                if (Held) // the Ask panel is open, or PAUSE: the voice and the subtitle wait where they are
                {
                    if (voice) voice.Pause();
                    yield return new WaitWhile(() => Held);
                    yield return VoiceChannel.WaitTurn(voice); // an answer still finishing after the panel closed
                    if (voice) voice.UnPause();
                }
                if (SubtitleUI.Instance) SubtitleUI.Instance.Show(this, line.speaker, line.text, t / length);
                if (wattson) wattson.ScriptedSpeech(line.vo, t);
                yield return null;
            }
            if (SubtitleUI.Instance) SubtitleUI.Instance.Hide(this);
            Speaking = null;
            yield return Wait(0.3f);
        }

        // Cuts a line started with StartCoroutine(Say(line)) short, e.g. a question the learner has already answered.
        void StopSaying(Coroutine saying, DialogueLine line)
        {
            if (saying == null || Speaking != line) return;
            StopCoroutine(saying);
            if (voice) voice.Stop();
            if (SubtitleUI.Instance) SubtitleUI.Instance.Hide(this);
            Speaking = null;
        }

        // ------------------------------------------------------------------ Timeline cues

        public void Run(string command)
        {
            var words = command.Split(' ');
            string verb = words[0], arg = words.Length > 1 ? words[1] : "";
            string rest = words.Length > 1 ? command.Substring(verb.Length + 1) : "";
            float value = float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
            bool on = arg != "0";
            int Int(int i) => words.Length > i && int.TryParse(words[i], out var n) ? n : 0;

            switch (verb)
            {
                case "fade": if (Flow) Flow.FadeWhite(value); break;
                case "wire.field": wire.fieldOn = on; break;
                case "wire.freeze": wire.frozen = on; break;
                case "wire.count": wire.counting = on; break;
                case "wire.impede": wire.impede = value; break;
                case "wire.lattice": wire.latticeVisibility = value; break;
                case "wire.radius": if (narrowingMetaphor || value >= wire.tunnelRadius) wire.tunnelRadius = value; break;
                case "boost": Boost(Ref(rest)); break;
                case "pulse": Boost(Ref(rest), settle: true); break;
                case "rig.voltage": rig.SetVoltage(value); break;
                case "rig.key": if (on) rig.CloseKey(); else rig.state.keyClosed = false; break;
                case "rig.place": rig.Place(arg); break;
                case "rig.slider": Slider(on); break;
                case "table.rows": table.ShowRows(Int(1)); LearnerSession.tableRows = Mathf.Max(LearnerSession.tableRows, Int(1)); break;
                case "table.ratio": table.ShowRatio(on); if (on) LearnerSession.ratioShown = true; break;
                case "table.work": table.SetRatio(Int(1), true); break;
                case "table.result": table.SetRatio(Int(1), false); break;
                case "table.highlight": foreach (var w in words.Skip(1)) table.Highlight(int.Parse(w), true, table.Pair(int.Parse(w))); break;
                case "table.clear": table.ClearHighlights(); table.GlowColumn(2, false); break;
                case "table.glow": table.GlowColumn(Int(1), Int(2) != 0); break;
                case "graph.axes": graph.Axes(on); break;
                case "graph.plot": graph.Plot(Int(1)); break;
                case "graph.line": graph.Line(on); break;
                case "graph.highlight": graph.Highlight(Int(1), true); graph.Highlight(Int(2), true, "I → 2I\n<color=#" + ColorUtility.ToHtmlStringRGB(UI.AccentColor) + ">V → 2V</color>"); break; // V in voltage blue
                case "graph.clear": for (int i = 0; i < 5; i++) graph.Highlight(i, false); break;
                case "graph.triangle": graph.Triangle(Int(1), Int(2)); break;
                case "graph.pulse": graph.Pulse(); break;
                case "prompt": Prompt(rest); break;
                case "spotlight": StartCoroutine(Spotlight()); break;
                case "ohm.freeze": foreach (var worker in FindObjectsByType<OhmWorker>(FindObjectsSortMode.None)) worker.frozen = on; break;
                case "ohm.pose": foreach (var worker in FindObjectsByType<OhmWorker>(FindObjectsSortMode.None)) worker.SetMode((OhmWorker.Mode)System.Enum.Parse(typeof(OhmWorker.Mode), arg, true)); break;
                case "ohm.swing": foreach (var worker in FindObjectsByType<OhmWorker>(FindObjectsSortMode.None)) worker.Swing(); break; // re-swing the needle; its [VFX] panel stays up
                case "lab": Lab(); break;
                case "reminder": Reminder(); break;
                // "callout show <id>" / "callout hide <id>" / "callout hideall" (CalloutDef ids set by the builders).
                case "callout":
                    if (callouts || (callouts = CalloutLayer.Instance))
                    {
                        string id = words.Length > 2 ? rest.Substring(arg.Length + 1) : "";
                        if (arg == "show") callouts.Show(id); else if (arg == "hide") callouts.Hide(id); else callouts.HideAll();
                    }
                    break;
                // Showing a "[VFX] ..." object also puts up the matching VfxPanel; hiding it takes the panel down.
                case "show": SetActive(rest, true); if (rest.StartsWith(CalloutLayer.Prefix) && (callouts || (callouts = CalloutLayer.Instance))) callouts.Vfx(rest, true, Ref(rest)); break;
                case "hide": SetActive(rest, false); if (rest.StartsWith(CalloutLayer.Prefix) && (callouts || (callouts = CalloutLayer.Instance))) callouts.Vfx(rest, false); break;
                case "handoff": SaveHandoff(); break;
                case "note": break; // visual beat description for Docs/FLOW.md
                default: if (!AreaCue(verb, words, rest, value, on)) Debug.LogWarning($"{sceneId}: unknown cue '{command}'", this); break;
            }
        }

        void SetActive(string name, bool on) { var go = Ref(name); if (go) go.SetActive(on); }

        // Scene 12: back from the wire to the bench, unless the table is shown inside the wire, in which case the
        // boards, their camera and Wattson's marker move to the anchor inside the tunnel.
        void Lab()
        {
            if (!tableInWire) { SetActive("Wire Section", false); SetActive("Lab", true); }
            else
            {
                var anchor = Ref("Wire Boards Anchor");
                var boards = Ref("Boards Set");
                if (anchor && boards)
                {
                    Vector3 delta = anchor.transform.position - boards.transform.position;
                    foreach (var name in new[] { "Boards Set", "Cam Boards", "Boards Point" })
                    {
                        var go = Ref(name);
                        if (go) go.transform.position += delta;
                    }
                }
            }
            // The cue runs under the white: Wattson is already beside the board when it clears (the spring flight back
            // from the wire would still be crossing the table).
            var spot = Ref("Boards Point");
            if (wattson && spot) wattson.Warp(spot.transform); // settled, no leftover spring velocity from the flight
        }

        // Scene 9 reminder card, with the learner's own prediction when that toggle is on.
        void Reminder()
        {
            var card = Ref("Card Reminder");
            if (!card) return;
            card.SetActive(true);
            var mine = card.transform.Find("Your Prediction");
            if (mine)
            {
                mine.gameObject.SetActive(showLearnerPrediction && LearnerSession.prediction != "");
                mine.GetComponent<TMP_Text>().text = "Your prediction: " + LearnerSession.PredictionText;
            }
        }

        // "boost" grows the object and brightens its glow and holds both; "pulse" (settle) peaks and returns to normal.
        // A cue that fires again while its run is still going (REPLAY, a retroactive re-fire) stops that run and starts
        // over from the rest scale, so nothing is ever measured from an inflated one.
        readonly Dictionary<GameObject, (Coroutine running, Vector3 rest)> boosts = new Dictionary<GameObject, (Coroutine, Vector3)>();

        void Boost(GameObject go, bool settle = false)
        {
            if (!go) return;
            if (boosts.TryGetValue(go, out var prior))
            {
                if (prior.running != null) StopCoroutine(prior.running);
                go.transform.localScale = prior.rest;
            }
            var rest = go.transform.localScale;
            boosts[go] = (StartCoroutine(BoostRun(go, rest, settle)), rest);
        }

        IEnumerator BoostRun(GameObject go, Vector3 from, bool settle)
        {
            float peak = settle ? 1.25f : 1.5f;
            var glow = new Brightener(go);
            for (float t = 0; t < 1; t += Time.deltaTime / 0.8f)
            {
                float k = settle ? Mathf.Sin(t * Mathf.PI) : Mathf.SmoothStep(0, 1, t);
                go.transform.localScale = from * Mathf.Lerp(1, peak, k);
                glow.Set(Mathf.Lerp(1, Brightener.Peak, k));
                yield return null;
            }
            go.transform.localScale = settle ? from : from * peak;
            glow.Set(settle ? 1 : Brightener.Peak);
        }

        // The boosted object's emissive parts (URP Lit emission, TextMeshPro face, additive particle-shader glows) and its
        // lights, scaled together. Property blocks leave the shared materials alone; a renderer that had no block gets
        // none back when the brightness returns to 1.
        class Brightener
        {
            public const float Peak = 1.8f;
            static readonly int Emission = Shader.PropertyToID("_EmissionColor"), Face = Shader.PropertyToID("_FaceColor"), Base = Shader.PropertyToID("_BaseColor");
            readonly List<(Renderer renderer, int id, Color color, bool own)> parts = new List<(Renderer, int, Color, bool)>();
            readonly List<(Light light, float intensity)> lights = new List<(Light, float)>();
            readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

            public Brightener(GameObject go)
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var m = r.sharedMaterial;
                    if (!m || r is ParticleSystemRenderer) continue; // spark bursts keep their own life
                    int id = m.IsKeywordEnabled("_EMISSION") && m.HasProperty(Emission) ? Emission
                           : m.HasProperty(Face) ? Face
                           : m.shader.name.Contains("Particles") && m.HasProperty(Base) ? Base : 0;
                    if (id != 0) parts.Add((r, id, m.GetColor(id), !r.HasPropertyBlock()));
                }
                foreach (var l in go.GetComponentsInChildren<Light>()) lights.Add((l, l.intensity));
            }

            public void Set(float k)
            {
                foreach (var (r, id, c, own) in parts)
                {
                    if (!r) continue;
                    if (own && k <= 1f) { r.SetPropertyBlock(null); continue; }
                    r.GetPropertyBlock(block);
                    block.SetColor(id, new Color(c.r * k, c.g * k, c.b * k, c.a));
                    r.SetPropertyBlock(block);
                }
                foreach (var (l, i) in lights) if (l) l.intensity = i * k;
            }
        }

        // Draws the eye to the Ask Wattson button: a scale wobble, or under ReducedMotion.On one dip of its alpha and back.
        IEnumerator Spotlight()
        {
            var button = Flow ? Flow.transform.Find("SafeArea/Ask Wattson") : null;
            if (!button) yield break;
            if (ReducedMotion.On)
            {
                if (!button.TryGetComponent(out CanvasGroup group)) group = button.gameObject.AddComponent<CanvasGroup>();
                for (float t = 0; t < 0.8f; t += Time.deltaTime)
                {
                    group.alpha = 1 - 0.6f * Mathf.Sin(t / 0.8f * Mathf.PI);
                    yield return null;
                }
                group.alpha = 1;
                yield break;
            }
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                button.localScale = Vector3.one * (1 + 0.25f * Mathf.Sin(t / 1.6f * Mathf.PI * 3) * (1 - t / 1.6f));
                yield return null;
            }
            button.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------ hand-off

        public void SaveHandoff()
        {
            SceneHandoffState.fromScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            SceneHandoffState.SaveCamera();
            if (rig) SceneHandoffState.circuit = rig.state;
            if (wire) { SceneHandoffState.fieldOn = wire.fieldOn; SceneHandoffState.driftSpeed = wire.driftSpeed; }
            SceneHandoffState.cardPositions = refs.Where(r => r && r.name.StartsWith("Card ")).Select(r => r.transform.position).ToArray();
            SceneHandoffState.tableRows = LearnerSession.tableRows;
            if (graph) SceneHandoffState.graphShown = true;
        }

        // Signal targets at the end of the scene.
        public void Complete() { SaveHandoff(); Flow.CompleteScene(); }
        public void CutToNext() { SaveHandoff(); Flow.Cut(); }

        // ------------------------------------------------------------------ on-screen controls

        // Prompt pill: below CinematicControlsUI's top-right buttons (they end at y -124), width fitted to the text.
        const float PromptY = -140f, PromptMaxWidth = 1200f, PromptMinWidth = 480f, PromptHeight = 108f, PromptPad = 48f;
        const float AskPanelClear = 24f + 520f + 24f; // the Ask panel's margin + width (AskWattsonUI) + air: the pill's left edge while it is open
        // Battery control: bottom-left, above the subtitle box (y 40..236, 210 px insets) and left of the Record / x2 chip at x 720.
        static readonly Vector2 BatteryPos = new Vector2(60, 260), BatterySize = new Vector2(620, 220);
        // TrackY puts the 96 px knob ~10 px under the BATTERY title and ~8 px over the tick labels.
        const float TrackWidth = 520f, TrackY = -4f, HandleSize = 96f, TickStep = TrackWidth / 5f;
        static readonly Vector2 TickRect = new Vector2(96, 44);
        // Tap hint: the fingertip is FingerTip px above the ring's centre; the ring keeps clear of the subtitle band and of
        // the Ask Wattson button (28 px inset + 170 px wide + 24 px air) at the bottom-right.
        const float TapRing = 150f, FingerTip = 54f, AskWattsonClear = 28f + 170f + 24f;

        // An empty text clears the prompt; the pill fades out by itself.
        void Prompt(string text)
        {
            promptText = text ?? "";
            if (prompt) { if (prompt.TryGetComponent(out UIFade fade)) fade.Dismiss(); else Destroy(prompt.gameObject); }
            prompt = null;
            if (string.IsNullOrEmpty(text) || !Flow) return;
            prompt = UI.Rect("Prompt", Flow.transform.Find("SafeArea"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, PromptY), new Vector2(PromptMaxWidth, PromptHeight));
            UI.Panel(prompt, UI.AccentFill, true, PromptHeight / 2).raycastTarget = false;
            prompt.gameObject.AddComponent<UIFade>();
            var label = UI.Label(prompt, text, UI.PromptFont, Color.white);
            label.rectTransform.offsetMin = new Vector2(PromptPad, 6);
            label.rectTransform.offsetMax = new Vector2(-PromptPad, -6);
            var needed = label.GetPreferredValues(text, PromptMaxWidth - 2 * PromptPad, 0);
            prompt.sizeDelta = new Vector2(Mathf.Clamp(Mathf.Ceil(needed.x) + 2 * PromptPad, PromptMinWidth, PromptMaxWidth), Mathf.Max(PromptHeight, Mathf.Ceil(needed.y) + 36));
        }

        // Battery voltage control: 0-5 V in 1 V steps, a 96 px handle for touch, voltage-blue fill and a live readout.
        void Slider(bool on)
        {
            if (slider) Destroy(slider.gameObject);
            if (!on || !Flow) return;
            var rect = UI.Rect("Voltage Control", Flow.transform.Find("SafeArea"), new Vector2(0, 0), new Vector2(0, 0), BatteryPos, BatterySize);
            UI.Panel(rect, UI.HudColor, true, 24f);
            slider = rect.gameObject.AddComponent<Slider>();

            var title = UI.Label(UI.Rect("Title", rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -16), new Vector2(300, 40)), "BATTERY", UI.SliderTickFont, new Color(1, 1, 1, 0.8f), TextAlignmentOptions.MidlineLeft);
            title.characterSpacing = 6f;
            // Readout at the 36 px "L" tier (meter digits), following rig.state.voltage rather than the 1 V-stepped slider.
            var readout = UI.Label(UI.Rect("Readout", rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -16), new Vector2(220, 40)), "", UI.ScreenButtonFont, UI.AccentColor, TextAlignmentOptions.MidlineRight);
            var live = rect.gameObject.AddComponent<SliderReadout>();
            live.label = readout;
            live.value = () => rig.state.voltage;

            var track = UI.Rect("Track", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, TrackY), new Vector2(TrackWidth, 14));
            UI.Panel(track, new Color(1, 1, 1, 0.25f), true, 7f).raycastTarget = false;
            var fill = UI.Stretch("Fill", track);
            UI.Panel(fill, UI.AccentColor, true, 7f).raycastTarget = false;
            tickLabels = new TMP_Text[6];
            ticksShownMax = -1f; // Update dims the ticks above ControlsMax
            for (int i = 0; i <= 5; i++)
            {
                UI.Panel(UI.Rect("Tick Mark", track, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-TrackWidth / 2 + i * TickStep, 0), new Vector2(4, 24)), new Color(1, 1, 1, 0.35f), false).raycastTarget = false;
                tickLabels[i] = UI.Label(UI.Rect("Tick", rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-TrackWidth / 2 + i * TickStep, 6), TickRect), i + " V", UI.SliderTickFont, new Color(1, 1, 1, 0.85f));
            }

            var area = UI.Rect("Handle Area", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, TrackY), new Vector2(TrackWidth, HandleSize));
            // The Slider stretches the handle over the Handle Area's height, so only its width is set: a 96 px circle.
            var handle = UI.Rect("Handle", area, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(HandleSize, 0));
            var knob = handle.gameObject.AddComponent<Image>();
            knob.sprite = UI.Circle();
            knob.color = UI.AccentColor;
            var dot = UI.Rect("Dot", handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30)).gameObject.AddComponent<Image>();
            dot.sprite = UI.Circle();
            dot.color = Color.white;
            dot.raycastTarget = false;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.minValue = 0;
            slider.maxValue = 5;
            slider.wholeNumbers = false; // the carried 0.5 V sits between the ticks; the listener snaps to whole volts
            slider.SetValueWithoutNotify(rig.state.voltage);
            slider.interactable = rig.controlsOpen;
            slider.onValueChanged.AddListener(volts => slider.SetValueWithoutNotify(rig.controlsOpen ? LearnerVoltage(volts) : rig.state.voltage));
        }

        // Record / x2: a UI.Button in the blue accent.
        Button ScreenButton(string name, string text, Vector2 position, UnityAction onClick)
        {
            var button = UI.Button(name, Flow.transform.Find("SafeArea"), new Vector2(0, 0), new Vector2(0, 0), position, new Vector2(260, 100), text, onClick, UI.ScreenButtonFont);
            button.GetComponent<Image>().color = UI.AccentFill;
            return button;
        }

        // Scene 4's tap-hand following a world object on screen: faint disc, ring, finger and palm, pulsing. `offset` (canvas
        // px) moves the ring off the target, e.g. below a meter so its reading stays visible; (0, -FingerTip) puts the
        // fingertip on the target.
        IEnumerator TapHint(Transform target, System.Func<bool> until, Vector2 offset = default)
        {
            var ring = UI.Rect("Tap Hint", Flow.transform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TapRing, TapRing));
            ring.SetAsFirstSibling();
            var disc = ring.gameObject.AddComponent<Image>();
            disc.sprite = UI.Circle();
            disc.color = new Color(1, 1, 1, 0.08f); // a hint of a disc: the target shows through
            disc.raycastTarget = false;
            var outline = UI.Stretch("Ring", ring).gameObject.AddComponent<Image>();
            outline.sprite = UI.Circle(0.9f);
            outline.color = new Color(1, 1, 1, 0.9f);
            outline.raycastTarget = false;
            var finger = UI.Rect("Finger", ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), new Vector2(-6, FingerTip - 62), new Vector2(26, 62));
            UI.Panel(finger, Color.white, true, 12f).raycastTarget = false;
            var palm = UI.Rect("Palm", ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(6, 2), new Vector2(58, 44));
            UI.Panel(palm, Color.white, true, 14f).raycastTarget = false;
            var canvas = Flow.GetComponent<Canvas>();
            var frame = ((RectTransform)Flow.transform).rect.size;
            float reach = TapRing * 0.56f; // half the ring at the top of its pulse, plus a few px
            while (!until())
            {
                if (Camera.main && target)
                {
                    var p = (Vector2)Camera.main.WorldToScreenPoint(target.position) / canvas.scaleFactor + offset;
                    ring.anchoredPosition = new Vector2(Mathf.Min(p.x, frame.x - AskWattsonClear - reach), Mathf.Max(p.y, UI.SubtitleTop + reach + 8f));
                }
                ring.localScale = Vector3.one * (1 + 0.12f * Mathf.Sin(Time.unscaledTime * 5f));
                yield return null;
            }
            Destroy(ring.gameObject);
        }

        void OnDestroy() => Shutdown();

        // Ends whatever a gate was doing: SKIP / NEXT SCENE unloads the scene mid-gate (OnDestroy), and the Scene 14 card
        // comes up over a running Scene 13 (ModuleFlow.Load). The gate coroutines die, the gate's spoken line and subtitle
        // stop, and everything a gate put on the module canvas (which outlives the scene) is swept: the MCQ panel, RECORD,
        // the × 2 chip and the tap hint, besides the prompt, slider and readings this director tracks. Collected, deactivated
        // and destroyed (no DestroyImmediate): an orphaned panel would stay interactable and make Ask Wattson see a question
        // in every later scene. Safe to call twice.
        public void Shutdown()
        {
            StopAllCoroutines();
            if (voice && Speaking) voice.Stop();
            if (SubtitleUI.Instance) SubtitleUI.Instance.Hide(this);
            Speaking = null;
            Gate = null;
            ControlsMax = CircuitRig.MaxVolts; // static: never carried into a scene left mid-gate
            if (prompt) Destroy(prompt.gameObject);
            if (slider) Destroy(slider.gameObject);
            if (readings) Destroy(readings.gameObject); // the HUD variant lives on the module canvas, which outlives the scene
            if (!Flow) return;
            var dead = new List<GameObject>();
            var safe = Flow.transform.Find("SafeArea");
            if (safe) foreach (Transform child in safe) if (child.name == "MCQ Panel" || child.name == "Record" || child.name == "Times Two") dead.Add(child.gameObject);
            foreach (Transform child in Flow.transform) if (child.name == "Tap Hint") dead.Add(child.gameObject);
            foreach (var go in dead) { go.SetActive(false); Destroy(go); }
        }
    }
}
