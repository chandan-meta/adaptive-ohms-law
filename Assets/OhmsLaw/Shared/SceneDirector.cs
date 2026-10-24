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
        // The gate in progress. Scene04Director sets it too for its tap-the-wire hold (Scene 4 runs no SceneDirector), so
        // Enter, Ask Wattson and the play test treat that hold like every other gate.
        public static string Gate { get; internal set; }
        // The highest voltage the learner and Ask Wattson may set while the controls are open (the Scene 10 steps cap it
        // at the step being taken, so the rows record in order). Hold() lifts it after every gate.
        public static float ControlsMax { get; private set; } = CircuitRig.MaxVolts;

        public string sceneId;
        [Tooltip("Start on the previous scene's last camera pose when arriving from this scene.")] public string matchCutFrom;
        public CinemachineCamera entryCamera;

        [Header("Review toggles")]
        [Tooltip("Scene 9: the learner places the meters, or they fly in by themselves (the key tap stays).")] public BuildMode buildMode = BuildMode.LearnerPlaces;
        [Tooltip("Reference px the HUD's right dock reserves in this scene (cards: notes, readings, table, graph ...); the camera composes into the rest. 0 = cinematic, full frame; -1 = decide from the scene (a bench or a wire reserves it, a cinematic scene does not).")] public float dockWidth = -1f;

        // The dock strip this scene reserves: the serialized value, or, for a scene built before the field existed (or
        // left on -1), the rule the builders follow: activity scenes (a circuit rig or an electron wire) reserve the
        // normal width, cinematic scenes (Ohm's study, the transformation) keep the whole frame.
        public float DockWidth => dockWidth >= 0f ? dockWidth : (rig || wire ? LessonPanel.NormalWidth : 0f);

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
            hintSaid = false;
            if (Flow) Flow.SetPaused(ModuleFlow.PauseReason.Hold, true);
            HudKeys.ConsumeAct();   // an Enter pressed before this gate does not answer it
            var hint = Line(gate, gate.gateId + "_H");
            var nudge = StartCoroutine(Nudge(hint));
            yield return RunGate(gate);
            if (hint) StopSaying(nudge, hint); // a nudge cut short by the action it asked for
            StopCoroutine(nudge);
            if (rig) { rig.controlsOpen = false; rig.targetAmps = -1f; } // whatever the gate did, cue-driven time follows
            ControlsMax = CircuitRig.MaxVolts;
            capPushed = false;
            Gate = null;
            if (Flow) Flow.SetPaused(ModuleFlow.PauseReason.Hold, false);
        }

        // An idle learner is never left in silence: after IdleSeconds with no tap and no line playing, the turn card pulses
        // with the hint sound, and the first time Wattson also says the gate's recorded hint (<gate>_H) where it has one.
        public const float IdleSeconds = 8f;
        bool hintSaid; // this gate's _H line has played (the nudge and SetVoltage's wrong-value hint share it)

        IEnumerator Nudge(DialogueLine hint)
        {
            float idle = 0;
            while (true)
            {
                idle = HudKeys.Tapped || Speaking || Held ? 0f : idle + Dt;
                if (idle >= IdleSeconds && Panel && Panel.Showing("Instruction"))
                {
                    idle = 0;
                    LessonPanel.Pop(Panel.Block("Instruction"));
                    UiSounds.Play("hint");
                    if (hint && !hintSaid) { hintSaid = true; yield return Say(hint); }
                }
                yield return null;
            }
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

        // A clip by name: the audio library first (the old bank names are its aliases: "PadWire" is BGM_Conductor_Airy,
        // "Select" is SFX_Part_Select), with the clip's level and mixer group; else the scene's own serialized bank.
        AudioClip Sound(string name) => Sound(name, out _, out _);

        AudioClip Sound(string name, out float gain, out string group)
        {
            var clip = AudioLibrary.ClipOrNull(name);
            gain = clip ? AudioLibrary.GainOf(name) : 1f;
            group = clip ? AudioLibrary.GroupOf(name) : null;
            return clip ? clip : sounds.FirstOrDefault(c => c && c.name == name);
        }

        // One-shot by name, pitched (1 = as recorded), at `volume` x the library's level for the clip. A missing clip is
        // silent, never an error.
        // A pitched one-shot needs a source of its own: PlayOneShot follows its source's pitch for as long as the clip plays,
        // so setting it and putting it back in one call played every VoltStep and Plot at 1. So does a clip on another
        // mixer group (UI, Character, Electrical): the group is the source's. Either takes an idle source cloned beside
        // `sfx` (same bus kind and gain, so the learner's slider and the ducker apply as usual) and keeps its pitch and
        // group until the clip ends; plain calls on the SFX group keep using `sfx` itself.
        public void Play(string name, float pitch = 1f, float volume = 1f)
        {
            var clip = Sound(name, out float gain, out string group);
            if (!clip || !sfx) return;
            var mixer = group != null ? AudioBus.Group(group) : null;
            if (!mixer) mixer = sfx.outputAudioMixerGroup;
            var source = Mathf.Approximately(pitch, 1f) && mixer == sfx.outputAudioMixerGroup ? sfx : pitched.Find(s => s && !s.isPlaying);
            if (!source)
            {
                if (pitched.Count < 6) { source = Clone(sfx); pitched.Add(source); AudioDucker.RescanSoon(); }
                else source = pitched[0]; // all busy: the first takes the new pitch mid-clip, no worse than before
            }
            source.pitch = pitch;
            if (source != sfx) source.outputAudioMixerGroup = mixer;
            source.PlayOneShot(clip, volume * gain);
        }

        readonly List<AudioSource> pitched = new List<AudioSource>();

        // A source beside `of` with its group and bus (kind, built gain). `name` for a bed's twin, which shares the bed's
        // name so AudioBus carries whichever of the two plays into the next scene under it.
        static AudioSource Clone(AudioSource of, string name = null)
        {
            var go = new GameObject(name ?? of.name + " pitched");
            go.SetActive(false); // the bus reads its gain in Awake
            go.transform.SetParent(of.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = of.spatialBlend;
            source.outputAudioMixerGroup = of.outputAudioMixerGroup;
            if (of.TryGetComponent(out AudioBus bus))
            {
                var mine = go.AddComponent<AudioBus>();
                mine.kind = bus.kind;
                mine.gain = bus.NominalGain > 0f ? bus.NominalGain : bus.gain; // the built level, not a fade's
                source.volume = mine.gain * AudioBus.Get(bus.kind);
            }
            else source.volume = of.volume;
            go.SetActive(true);
            return source;
        }

        // A looping bed (ambience or music) has two sources: the serialized one and a twin made on first use under the
        // same name, so AudioBus carries whichever plays into the next scene (the other is stopped by then). A change of
        // bed starts the new clip on the idle source and raises it while the playing one lowers, equal power, over the
        // whole crossfade: never a dip to silence. The same clip again is a no-op, so a replayed cue never restarts the
        // bed; "off" lowers the playing one alone. The clip's library level goes on the bus as trim, under the fade.
        class BedPair { public AudioSource a, b, active; public AudioClip target; public Coroutine fade; }
        readonly Dictionary<AudioSource, BedPair> beds = new Dictionary<AudioSource, BedPair>();

        void Bed(AudioSource source, string name, float seconds)
        {
            if (!source) return;
            float gain = 1f;
            var clip = name == "off" || name == "" ? null : Sound(name, out gain, out _);
            if (!beds.TryGetValue(source, out var bed)) // a bed carried in from the last scene is already playing on it
                beds[source] = bed = new BedPair { a = source, active = source.isPlaying && source.clip ? source : null, target = source.isPlaying ? source.clip : null };
            if (bed.target == clip) return;
            bed.target = clip;
            if (bed.fade != null) StopCoroutine(bed.fade);
            var from = bed.active;
            AudioSource to = null;
            if (clip)
            {
                if (!bed.b) { bed.b = Clone(source, source.name); bed.b.transform.SetParent(source.transform.parent, false); bed.b.loop = true; }
                to = from == bed.a ? bed.b : bed.a;
            }
            bed.active = to;
            bed.fade = StartCoroutine(Crossfade(bed, from, to, clip, gain, seconds));
        }

        IEnumerator Crossfade(BedPair bed, AudioSource from, AudioSource to, AudioClip clip, float trim, float seconds)
        {
            var outBus = from ? from.GetComponent<AudioBus>() : null;
            var inBus = to ? to.GetComponent<AudioBus>() : null;
            float outFrom = outBus ? outBus.gain : 0f, inFrom = 0f, inTo = inBus ? inBus.NominalGain : 1f;
            if (to)
            {
                // Still lowering this very clip (a replay turned back): it just comes back up, never restarts.
                if (to.clip != clip || !to.isPlaying) { if (inBus) inBus.gain = 0f; to.volume = 0f; to.clip = clip; to.loop = true; to.Play(); }
                if (inBus) { inBus.trim = trim; inFrom = inBus.gain; }
            }
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / Mathf.Max(0.05f, seconds))
            {
                float a = t * Mathf.PI * 0.5f;
                if (outBus) outBus.gain = outFrom * Mathf.Cos(a);
                if (inBus) inBus.gain = Mathf.Lerp(inFrom, inTo, Mathf.Sin(a));
                yield return null;
            }
            if (from) { from.Stop(); if (outBus) outBus.gain = 0f; }
            if (inBus) inBus.gain = inTo; // at its built level: AudioBus carries it into the next scene
            bed.fade = null;
        }

        // The library's cue rule for this cue (the first whose prefix matches, outside its cooldown): its clip at the
        // rule's gain x the clip's level, on the clip's group, through the pooled one-shots. Scenes 5 and 6 get their
        // sounds this way without a rebuild ("wire.field 1", "show Drift Arrow", "counter 1" ...). Not in the frame of a
        // jump (REPLAY, SKIP, Ask Wattson's resume): every cue re-fired between would sound at once.
        void CueSound(string command)
        {
            if (Time.frameCount - ModuleFlow.JumpFrame <= 1) return;
            var rule = AudioLibrary.Instance ? AudioLibrary.Instance.Rule(command, Time.unscaledTime) : null;
            if (rule != null) Play(rule.clip, 1f, rule.gain);
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
                case "fade": if (Flow) Flow.FadeWhite(value, words.Length > 2 && float.TryParse(words[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var fadeSeconds) ? fadeSeconds : -1f); break;   // "fade <alpha> [seconds]"
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
                case "table.ratio": if (on) { LearnerSession.ratioShown = true; StartCoroutine(SnapThen(() => Table.ShowRatio(true))); } else Table.ShowRatio(false); break; // the new column lands on his snap
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
                // "callout show <id>" / "callout hide <id>" / "callout hideall": the object's note in the panel.
                case "callout":
                {
                    string id = words.Length > 2 ? rest.Substring(arg.Length + 1) : "";
                    // A concept note (voltage, current, resistance) lands on his snap; an object's note just shows.
                    if (arg == "show" && ConceptNotes.Contains(id)) StartCoroutine(SnapThen(() => Callouts.Show(id)));
                    else if (arg == "show") Callouts.Show(id); else if (arg == "hide") Callouts.Hide(id); else Callouts.HideAll();
                    break;
                }
                case "show": SetActive(rest, true); break;
                case "hide": SetActive(rest, false); break;
                case "handoff": SaveHandoff(); break;
                case "note": break; // visual beat description for Docs/FLOW.md
                default: if (!AreaCue(verb, words, rest, value, on)) Debug.LogWarning($"{sceneId}: unknown cue '{command}'", this); break;
            }
            // A reveal that waits for his snap gets its sound rule on the click too.
            if (SnappedFrame == Time.frameCount) StartCoroutine(SoundOnClick(command)); else CueSound(command);
        }

        static readonly HashSet<string> ConceptNotes = new HashSet<string> { "voltage", "current", "resistance" };

        IEnumerator SoundOnClick(string command)
        {
            yield return Wait(WattsonController.SnapLead);
            CueSound(command);
        }

        void SetActive(string name, bool on) { var go = Ref(name); if (go) go.SetActive(on); }

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
            var button = Panel ? Panel.AskRoot : null;
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

        // Scene 4's tap-hand following a world object on screen: faint disc, ring, finger and palm, pulsing (steady under
        // reduced motion). It stands off the target on the `from` side (a screen direction, y up; default below-right),
        // tilted so the fingertip meets the edge of the target's footprint: the target itself stays in view. Kept inside
        // the part of the frame the panel leaves free.
        const float TapRing = 150f, FingerTip = 54f, HandScale = 0.6f, HandGap = 10f;

        IEnumerator TapHint(Transform target, System.Func<bool> until, Vector2 from = default)
        {
            from = from == Vector2.zero ? new Vector2(0.6f, -0.8f) : from.normalized;
            var ring = UI.Rect("Tap Hint", Flow.transform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TapRing, TapRing));
            ring.SetAsFirstSibling();
            ring.localRotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, -from)); // the finger points back at the target
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
            float reach = TapRing * HandScale * 0.56f; // half the ring at the top of its pulse, plus a few px
            while (!until())
            {
                var cam = Camera.main;
                if (cam && target)
                {
                    float free = frame.x - (Panel ? Panel.Cover : 0f) - reach;
                    var centre = (Vector2)cam.WorldToScreenPoint(target.position) / canvas.scaleFactor;
                    var p = centre + from * (Footprint(target, cam, centre, from, canvas.scaleFactor) + HandGap + FingerTip * HandScale);
                    ring.anchoredPosition = new Vector2(Mathf.Min(p.x, free), Mathf.Max(p.y, reach + 8f));
                }
                ring.localScale = Vector3.one * HandScale * (ReducedMotion.On ? 1f : 1 + 0.12f * Mathf.Sin(Time.unscaledTime * 5f));
                yield return null;
            }
            Destroy(ring.gameObject);
        }

        // How far the target reaches on screen from `centre` towards `dir` (canvas px): its renderers' bounds, projected,
        // capped so one large glow never sends the hand across the frame.
        static float Footprint(Transform target, Camera cam, Vector2 centre, Vector2 dir, float scale)
        {
            float reach = 0f;
            foreach (var r in target.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer) continue;
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    reach = Mathf.Max(reach, Vector2.Dot((Vector2)cam.WorldToScreenPoint(corner) / scale - centre, dir));
                }
            }
            return Mathf.Min(reach, 120f);
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
