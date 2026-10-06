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
    // One per scene, on the PlayableDirector. Applies Timeline cues, holds the Timeline at interaction
    // gates while the learner acts, speaks branch lines, and hands the scene on to the next one. Everything the
    // learner reads or taps goes through the lesson panel (LessonPanel.Instance).
    public partial class SceneDirector : MonoBehaviour, INotificationReceiver
    {
        public enum BuildMode { LearnerPlaces, AutoPlace }

        public static DialogueLine Speaking { get; private set; }
        public static string Gate { get; private set; }
        // The highest voltage the learner and Ask Wattson may set while the controls are open (the Scene 10 steps cap it
        // at the step being taken, so the rows record in order). Hold() lifts it after every gate.
        public static float ControlsMax { get; private set; } = CircuitRig.MaxVolts;

        public string sceneId;
        [Tooltip("Start on the previous scene's last camera pose when arriving from this scene.")] public string matchCutFrom;
        public CinemachineCamera entryCamera;

        [Header("Review toggles")]
        [Tooltip("Scene 9: the learner places the meters, or they fly in by themselves (the key tap stays).")] public BuildMode buildMode = BuildMode.LearnerPlaces;
        [Tooltip("Scene 9: add \"Your prediction: ...\" from Scene 6 under the reminder (Reminder() leaves it off when none was recorded).")] public bool showLearnerPrediction = true;

        [Header("Scene parts")]
        public WattsonController wattson;
        public WireInterior wire;
        public CircuitRig rig;
        [Tooltip("Objects cues and gates refer to by name.")] public GameObject[] refs = new GameObject[0];
        public AudioSource voice, sfx;
        [Tooltip("Looping beds the 'ambience' and 'music' cues crossfade.")] public AudioSource ambience, music;
        public AudioClip click, pop;
        [Tooltip("The sound bank the 'sfx' cue and the gates play, by clip name.")] public AudioClip[] sounds = new AudioClip[0];

        bool capPushed; // someone asked for more than ControlsMax while it was lowered: the gate says why the buttons stop
        VoltageButtons volts;

        static ModuleFlow Flow => ModuleFlow.Instance;
        static LessonPanel Panel => LessonPanel.Instance;
        GameObject Ref(string name) => refs.FirstOrDefault(r => r && r.name == name);
        ObservationTableUI Table => Panel ? Panel.Table : null;
        GraphUI Graph => Panel ? Panel.Graph : null;

        // A gate line holds while the Ask panel is open or the learner pressed PAUSE (the Timeline is already frozen by then;
        // the gates run outside it, so they hold themselves). Gate timers advance by Dt, which is zero under PAUSE;
        // autoAnswer timers also stop while the Ask panel is open (each caller checks IsOpen).
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
            if (Panel && wattson && wattson.agentContext) Panel.SetTitle(wattson.agentContext.sceneTitle);
        }

        // The cap: a setting above it made through the open controls is undone at once (a request that did not check it).
        void Update()
        {
            if (!rig) return;
            if (rig.controlsOpen && rig.state.voltage > ControlsMax + 0.001f) { rig.SetVoltage(ControlsMax); capPushed = true; }
        }

        // The learner's request (a voltage button): whole volts, never above ControlsMax.
        float LearnerVoltage(float volts)
        {
            float want = Mathf.Round(volts);
            if (want > ControlsMax + 0.001f) { want = ControlsMax; capPushed = true; }
            float applied = rig.SetVoltage(want);
            Play("VoltStep", 1f + 0.07f * (applied - 1f));
            return applied;
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

        // Speaks a line outside the Timeline (interaction branches): caption, voice and Wattson's talking glow. It takes its
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
                if (Held) // the Ask panel is open, or PAUSE: the voice and the caption wait where they are
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

        // ------------------------------------------------------------------ sound

        AudioClip Sound(string name) => sounds.FirstOrDefault(c => c && c.name == name);

        // One-shot from the bank, pitched (1 = as recorded). A missing clip is silent, never an error.
        public void Play(string name, float pitch = 1f)
        {
            var clip = Sound(name);
            if (!clip || !sfx) return;
            sfx.pitch = pitch;
            sfx.PlayOneShot(clip);
            sfx.pitch = 1f;
        }

        readonly Dictionary<AudioSource, Coroutine> fades = new Dictionary<AudioSource, Coroutine>();

        // A looping bed (ambience or music) crossfades to the named clip, or out ("off"). Idempotent: the same clip again
        // is left playing, so a replayed cue never restarts the bed.
        void Bed(AudioSource source, string name, float seconds)
        {
            if (!source) return;
            var clip = name == "off" || name == "" ? null : Sound(name);
            if (source.clip == clip && (clip == null || source.isPlaying)) return;
            if (fades.TryGetValue(source, out var running) && running != null) StopCoroutine(running);
            fades[source] = StartCoroutine(Crossfade(source, clip, seconds));
        }

        IEnumerator Crossfade(AudioSource source, AudioClip to, float seconds)
        {
            var bus = source.GetComponent<AudioBus>();
            float gain = bus ? bus.gain : 1f;
            if (source.isPlaying && source.clip)
            {
                for (float t = 0; t < 1; t += Time.unscaledDeltaTime / Mathf.Max(0.05f, seconds * 0.5f))
                {
                    if (bus) bus.gain = gain * (1 - t);
                    yield return null;
                }
                source.Stop();
            }
            if (bus) bus.gain = 0f;
            source.clip = to;
            if (to) { source.loop = true; source.Play(); }
            for (float t = 0; t < 1 && to; t += Time.unscaledDeltaTime / Mathf.Max(0.05f, seconds * 0.5f))
            {
                if (bus) bus.gain = gain * t;
                yield return null;
            }
            if (bus) bus.gain = gain;
            fades.Remove(source);
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
                case "boost": Boost(Ref(rest)); break;
                case "pulse": Boost(Ref(rest), settle: true); break;
                case "rig.voltage": rig.SetVoltage(value); break;
                case "rig.key": if (on) rig.CloseKey(); else rig.state.keyClosed = false; break;
                case "rig.place": rig.Place(arg); break;
                case "rig.slider": Volts(on); break;
                case "table.rows": Table.ShowRows(Int(1)); LearnerSession.tableRows = Mathf.Max(LearnerSession.tableRows, Int(1)); break;
                case "table.ratio": Table.ShowRatio(on); if (on) LearnerSession.ratioShown = true; break;
                case "table.work": Table.SetRatio(Int(1), true); Play("Plot", 1f + 0.06f * Int(1)); break;
                case "table.result": Table.SetRatio(Int(1), false); Play("Plot", 1f + 0.06f * Int(1)); break;
                case "table.highlight": foreach (var w in words.Skip(1)) Table.Highlight(int.Parse(w), true); break;
                case "table.clear": Table.ClearHighlights(); Table.GlowColumn(2, false); break;
                case "table.glow": Table.GlowColumn(Int(1), Int(2) != 0); break;
                case "graph.axes": Graph.Axes(on); break;
                case "graph.plot": Graph.Plot(Int(1)); break;
                case "graph.line": Graph.Line(on, instant: value >= 2f); if (on && value < 2f) Play("Rise"); break; // "graph.line 2": restored without the sweep
                case "graph.highlight": Graph.Highlight(Int(1), true); Graph.Highlight(Int(2), true); break;
                case "graph.clear": for (int i = 0; i < 5; i++) Graph.Highlight(i, false); break;
                case "graph.triangle": Graph.Triangle(Int(1), Int(2)); if (Int(1) >= 0) Play("Reveal"); break;
                case "graph.pulse": Graph.Pulse(); break;
                case "prompt": Instruction(rest); break;
                case "spotlight": StartCoroutine(Spotlight()); break;
                case "ohm.freeze": foreach (var worker in FindObjectsByType<OhmWorker>(FindObjectsSortMode.None)) worker.frozen = on; break;
                case "ohm.pose": foreach (var worker in FindObjectsByType<OhmWorker>(FindObjectsSortMode.None)) worker.SetMode((OhmWorker.Mode)System.Enum.Parse(typeof(OhmWorker.Mode), arg, true)); break;
                case "ohm.swing": foreach (var worker in FindObjectsByType<OhmWorker>(FindObjectsSortMode.None)) worker.Swing(); break;
                // "ohm.look <ref>" / "ohm.look": Ohm glances at something (Wattson while he speaks) or back to his work.
                case "ohm.look": { var at = Ref(rest); foreach (var worker in FindObjectsByType<OhmWorker>(FindObjectsSortMode.None)) worker.Attend(at ? at.transform : null); break; }
                case "reminder": Reminder(); break;
                // "callout show <id>" / "callout hide <id>" / "callout hideall": the object's note in the panel.
                case "callout":
                {
                    string id = words.Length > 2 ? rest.Substring(arg.Length + 1) : "";
                    if (arg == "show") Callouts.Show(id); else if (arg == "hide") Callouts.Hide(id); else Callouts.HideAll();
                    break;
                }
                case "show": SetActive(rest, true); break;
                case "hide": SetActive(rest, false); break;
                case "handoff": SaveHandoff(); break;
                case "note": break; // visual beat description for Docs/FLOW.md
                default: if (!AreaCue(verb, words, rest, value, on)) Debug.LogWarning($"{sceneId}: unknown cue '{command}'", this); break;
            }
        }

        void SetActive(string name, bool on) { var go = Ref(name); if (go) go.SetActive(on); }

        // Scene 9 reminder, in the panel: the question from Scene 6 and the learner's own prediction when that toggle is on.
        void Reminder()
        {
            if (!Panel) return;
            string mine = showLearnerPrediction && LearnerSession.prediction != "" ? "Your prediction: " + LearnerSession.PredictionText : "";
            Panel.Info("reminder", "If the potential difference increases, what happens to the current?", mine);
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
            var button = Panel ? Panel.AskButton.transform : null;
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
                button.localScale = Vector3.one * (1 + 0.12f * Mathf.Sin(t / 1.6f * Mathf.PI * 3) * (1 - t / 1.6f));
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
            SceneHandoffState.graphShown = Panel && Panel.Showing("Graph");
        }

        // Signal targets at the end of the scene.
        public void Complete() { SaveHandoff(); Flow.CompleteScene(); }
        public void CutToNext() { SaveHandoff(); Flow.Cut(); }

        // ------------------------------------------------------------------ on-screen controls

        // The one current instruction, in the panel. An empty text clears it.
        void Instruction(string text)
        {
            if (Panel) Panel.Instruction(text ?? "");
        }

        string CurrentInstruction => Panel ? Panel.CurrentInstruction : "";

        // Battery voltage buttons, 1-5 V, in the panel: they request through LearnerVoltage (whole volts, the cap) and take
        // input only while an activity has opened the controls.
        void Volts(bool on)
        {
            if (!on) { if (Panel) Panel.Clear("Volt"); volts = null; return; }
            if (volts || !Panel || !rig) return;
            volts = VoltageButtons.Create();
            volts.set = LearnerVoltage;
            volts.open = () => rig && rig.controlsOpen;
            volts.current = () => rig ? rig.state.voltage : 0f;
            volts.max = () => ControlsMax;
        }

        // Scene 4's tap-hand following a world object on screen: faint disc, ring, finger and palm, pulsing. `offset` (canvas
        // px) moves the ring off the target, e.g. below a meter so its reading stays visible; (0, -FingerTip) puts the
        // fingertip on the target. Kept inside the part of the frame the panel leaves free.
        const float TapRing = 150f, FingerTip = 54f;

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
                    float free = frame.x - (Panel ? Panel.Cover : 0f) - reach;
                    var p = (Vector2)Camera.main.WorldToScreenPoint(target.position) / canvas.scaleFactor + offset;
                    ring.anchoredPosition = new Vector2(Mathf.Min(p.x, free), Mathf.Max(p.y, reach + 8f));
                }
                ring.localScale = Vector3.one * (1 + 0.12f * Mathf.Sin(Time.unscaledTime * 5f));
                yield return null;
            }
            Destroy(ring.gameObject);
        }

        void OnDestroy() => Shutdown();

        // Ends whatever a gate was doing: SKIP / NEXT SCENE unloads the scene mid-gate (OnDestroy), and the end card
        // comes up over a running Scene 13 (ModuleFlow.Load). The gate coroutines die, the gate's spoken line and caption
        // stop, and what a gate put on the module canvas (which outlives the scene) is swept: the panel's question,
        // voltage buttons and instruction, and the tap hint. Safe to call twice.
        public void Shutdown()
        {
            StopAllCoroutines();
            if (voice && Speaking) voice.Stop();
            if (SubtitleUI.Instance) SubtitleUI.Instance.Hide(this);
            Speaking = null;
            Gate = null;
            ControlsMax = CircuitRig.MaxVolts; // static: never carried into a scene left mid-gate
            if (Panel) { Panel.Clear("MCQ"); Panel.Clear("Volt"); Panel.Instruction(""); }
            volts = null;
            if (!Flow) return;
            var dead = new List<GameObject>();
            foreach (Transform child in Flow.transform) if (child.name == "Tap Hint") dead.Add(child.gameObject);
            foreach (var go in dead) { go.SetActive(false); Destroy(go); }
        }
    }
}
