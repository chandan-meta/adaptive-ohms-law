using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace OhmsLaw
{
    [Serializable]
    public struct CircuitState
    {
        public bool keyClosed;
        public float voltage;
    }

    // The modern bench: variable DC source, ammeter (series), voltmeter (parallel across XY), nichrome wire XY, key.
    // Readings: voltmeter = V, ammeter = V / R from ExperimentData. The nichrome wire never glows.
    // The instruments are the imported models: needles swing from their modelled rest pose about their own axis,
    // the source knob turns with the set voltage and its pilot lamp lights while the key is closed. A magenta pulse
    // runs X → Y along the rod when the ammeter snaps in and when the key closes (or when a cue shows it).
    public class CircuitRig : MonoBehaviour
    {
        // "Small default source voltage so the needles move" when the key is first closed (Scene 9); Scene 10 starts here.
        public const float KeyClosedDefaultVolts = 0.5f;

        public CircuitState state;
        public bool ammeterPlaced, voltmeterPlaced;

        // The source's range (the repainted panel reads 0-5 V). Every writer goes through SetVoltage so the slider, meters,
        // readouts, graph and Ask Wattson all see one value; VoltageChanged tells the views.
        public const float MinVolts = 0f, MaxVolts = 5f;
        public static CircuitRig Active { get; private set; }
        public event Action<float> VoltageChanged;
        [Tooltip("The learner (slider) and Ask Wattson may change the voltage now. Activities open it; cue-driven runs close it.")] public bool controlsOpen;
        [Tooltip("Current the running target activity asks for (S11_Target, S13_Apply), in amperes; negative when none. The readings panel shows it; Ask Wattson hints toward it.")] public float targetAmps = -1f;
        public float PreviousVoltage { get; private set; }
        public float StartVoltage { get; private set; }

        [Header("Parts")]
        public Transform ammeter, voltmeter, key, keyLever;
        public Transform ammeterSlot, voltmeterSlot;
        public TextMeshPro ammeterReading, voltmeterReading;
        [Tooltip("Optional world label shown while the ammeter is in (the kit no longer builds one: the board carries the path).")] public TextMeshPro pathLabel;
        public Transform ammeterNeedle, voltmeterNeedle;
        [Tooltip("Amber overlay along the cables Source → Ammeter → X; lit once the ammeter is in (and showPath). Nothing runs along XY.")] public LineRenderer currentPath;
        [Tooltip("The path's return leg along the cables Y → Key → Source, lit with currentPath.")] public LineRenderer currentPathReturn;
        [Tooltip("Builders and cues turn the lit path off where the bench should show plain red / black cables.")] public bool showPath = true;
        [Tooltip("The digital plates above the meters. Scenes 9-10 hide them until the 1 V baseline, so the key-close reading is a needle move, not a number a learner can pattern-match before the prediction.")] public bool readoutsShown = true;
        [Tooltip("Leads X → voltmeter and Y → voltmeter; shown once the voltmeter is in.")] public LineRenderer voltmeterLeads, voltmeterLeadY;
        [Tooltip("Leads Source → ammeter → X; shown once the ammeter is in.")] public GameObject ammeterLeads;
        public GameObject seriesGlow, parallelGlow, wireHighlight;
        public AudioSource sfx;
        public AudioClip click, snap;

        [Header("Instruments")]
        [Tooltip("Printed full-scale values of the repainted dials.")] public float ammeterFullScale = 1.2f, voltmeterFullScale = 6f;
        [Tooltip("Angle of the printed arc about each needle pivot's local Z; negative sweeps clockwise as seen from the dial.")] public float needleSweep = -96f;
        public Transform sourceKnob;
        [Tooltip("Knob turn per volt about its local Z; the repainted panel reads 0-5 V over 180°.")] public float knobDegreesPerVolt = -36f;
        public Renderer sourceBody;
        public int lampMaterialIndex;
        [ColorUsage(false, true)] public Color lampColor = new Color(2.5f, 2.5f, 2.5f);
        [Tooltip("World quads that turn to face the live camera (readouts, the pulse halo).")] public Transform[] billboards = new Transform[0];

        [Header("Pulse X → Y")]
        [Tooltip("\"[VFX] Pulse travels from X to Y\": hidden; runs its repeats whenever it is shown (a cue or PlayPulse()) and hides itself after.")] public GameObject pulse;
        public Transform pulseHead;
        public TrailRenderer pulseTrail;
        [Tooltip("Chevrons ahead of the head, nearest first.")] public Transform[] pulseChevrons = new Transform[0];
        [Tooltip("Y's end of the wire in the pulse root's space; the head starts at the root (X).")] public Vector3 pulseTo = new Vector3(0.7f, 0, 0);
        public float pulseSeconds = 1.2f, pulseGap = 0.4f, chevronLead = 0.07f, chevronSpacing = 0.055f;
        public int pulseRepeats = 3;

        [Header("Learner input")]
        [Tooltip("Part the learner may select right now: \"ammeter\", \"voltmeter\" or empty.")] public string draggable = "";
        public bool keyArmed;
        // The part the learner has selected (tapped) and not yet placed: it lifts and waits for a tap on its slot.
        public string SelectedPart { get; private set; } = "";

        public event Action<string> Placed;
        public event Action<string> Selected;
        public event Action<string> WrongPart; // a tap on the meter that is not the one wanted now (still in the tray)
        public event Action KeyClosed;

        float shownVolts, shownAmps, shownKnob;
        Quaternion ammeterZero, voltmeterZero, knobZero;
        Vector3 seriesGlowScale, parallelGlowScale, highlightScale;
        bool lampLit;
        Transform dragging;
        Vector3 dragHome, selectedHome;
        Transform bench => transform;
        Vector2 pressAt;
        bool moving;
        Coroutine pulsing;

        public float Volts => VoltsAt(state.voltage);
        public float Amps => AmpsAt(state.voltage);
        // What the meters would read at another source voltage with the circuit as it is now ("was" readings, previews).
        public float VoltsAt(float volts) => state.keyClosed && ammeterPlaced && voltmeterPlaced ? volts : 0;
        public float AmpsAt(float volts) => state.keyClosed && ammeterPlaced ? ExperimentData.Main.CurrentAt(volts) : 0;

        public void Place(string part)
        {
            var t = Part(part);
            if (t && !IsPlaced(part)) StartCoroutine(Fly(t, part == "ammeter" ? ammeterSlot : voltmeterSlot, part));
        }

        public bool IsPlaced(string part) => part == "ammeter" ? ammeterPlaced : voltmeterPlaced;

        // Snaps a part straight into its slot (scene setup, hand-offs).
        public void PlaceNow(string part)
        {
            var t = Part(part);
            var slot = part == "ammeter" ? ammeterSlot : voltmeterSlot;
            t.SetPositionAndRotation(slot.position, slot.rotation);
            SetPlaced(part);
        }

        // Clamps to the source's range and returns the value actually applied (callers report that, never the request).
        public float SetVoltage(float volts)
        {
            float applied = Mathf.Clamp(volts, MinVolts, MaxVolts);
            if (Mathf.Approximately(applied, state.voltage)) return applied;
            PreviousVoltage = state.voltage;
            state.voltage = applied;
            VoltageChanged?.Invoke(applied);
            return applied;
        }


        // Match-cut: take over the previous scene's circuit as this scene's starting state, settled (no needle swing-up).
        public void Restore(CircuitState carried)
        {
            state.keyClosed = carried.keyClosed;
            SetVoltage(carried.voltage);
            StartVoltage = PreviousVoltage = state.voltage;
            shownVolts = Volts;
            shownAmps = Amps;
            shownKnob = state.voltage;
        }

        public void CloseKey()
        {
            if (state.keyClosed) return;
            state.keyClosed = true;
            if (state.voltage <= 0) SetVoltage(KeyClosedDefaultVolts);
            keyArmed = false;
            if (sfx && click) sfx.PlayOneShot(click);
            PlayPulse();
            KeyClosed?.Invoke();
        }

        // The X → Y pulse: shows the "[VFX] Pulse travels from X to Y" object; Update runs it and hides it again.
        public void PlayPulse()
        {
            if (pulse) pulse.SetActive(true);
        }

        Transform Part(string part) => part == "ammeter" ? ammeter : part == "voltmeter" ? voltmeter : null;

        void SetPlaced(string part)
        {
            if (part == "ammeter") { ammeterPlaced = true; PlayPulse(); } else voltmeterPlaced = true;
            if (draggable == part) draggable = "";
            if (SelectedPart == part) SelectedPart = "";
            Placed?.Invoke(part);
        }

        IEnumerator Fly(Transform part, Transform slot, string name)
        {
            Vector3 from = part.position;
            Quaternion turn = part.rotation;
            for (float t = 0; t < 1; t += Time.deltaTime / 0.45f)
            {
                float e = Mathf.SmoothStep(0, 1, t);
                part.SetPositionAndRotation(Vector3.Lerp(from, slot.position, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.12f, Quaternion.Slerp(turn, slot.rotation, e));
                yield return null;
            }
            part.SetPositionAndRotation(slot.position, slot.rotation);
            if (sfx && snap) sfx.PlayOneShot(snap);
            SetPlaced(name);
        }

        IEnumerator ShakeHome(Transform part, Vector3 home)
        {
            Vector3 from = part.position;
            for (float t = 0; t < 1; t += Time.deltaTime / 0.5f)
            {
                part.position = Vector3.Lerp(from, home, Mathf.SmoothStep(0, 1, t)) + part.right * (Mathf.Sin(t * 40f) * 0.02f * (1 - t));
                yield return null;
            }
            part.position = home;
        }

        // The imported pivots rest tilted with the dial (niddle_grp Euler (47, 0, 58)): every swing is relative to that pose.
        void Awake()
        {
            if (ammeterNeedle) ammeterZero = ammeterNeedle.localRotation;
            if (voltmeterNeedle) voltmeterZero = voltmeterNeedle.localRotation;
            if (sourceKnob) knobZero = sourceKnob.localRotation;
            if (seriesGlow) seriesGlowScale = seriesGlow.transform.localScale;
            if (parallelGlow) parallelGlowScale = parallelGlow.transform.localScale;
            if (wireHighlight) highlightScale = wireHighlight.transform.localScale;
            shownVolts = Volts; // no swing-up on a match-cut into a running circuit
            shownAmps = Amps;
            shownKnob = state.voltage;
            lampLit = !state.keyClosed; // so the first Update applies the lamp
            StartVoltage = PreviousVoltage = state.voltage;
        }

        void OnEnable() => Active = this;

        void Update()
        {
            Pointing();
            Lift();

            float ease = 1 - Mathf.Exp(-6f * Time.deltaTime);
            shownVolts = Mathf.Lerp(shownVolts, Volts, ease);
            shownAmps = Mathf.Lerp(shownAmps, Amps, ease);
            shownKnob = Mathf.Lerp(shownKnob, state.voltage, ease);
            // A meter waiting in the tray is not in the circuit, so it shows no readout.
            Readout(voltmeterReading, voltmeterPlaced && readoutsShown, shownVolts.ToString("0.0") + " V");
            Readout(ammeterReading, ammeterPlaced && readoutsShown, shownAmps.ToString("0.00") + " A");
            Swing(voltmeterNeedle, voltmeterZero, needleSweep * Mathf.Clamp01(shownVolts / voltmeterFullScale));
            Swing(ammeterNeedle, ammeterZero, needleSweep * Mathf.Clamp01(shownAmps / ammeterFullScale));
            Swing(sourceKnob, knobZero, knobDegreesPerVolt * shownKnob);
            if (keyLever) keyLever.localRotation = Quaternion.Slerp(keyLever.localRotation, Quaternion.Euler(0, 0, state.keyClosed ? 0 : 35), 1 - Mathf.Exp(-12f * Time.deltaTime));
            if (lampLit != state.keyClosed) Lamp(lampLit = state.keyClosed);

            // The series loop reads as one path once the ammeter is in it.
            var palette = UI.Theme;
            Color lit = palette ? palette.current : new Color(1f, 0.72f, 0.19f);
            Color path = Color.clear;
            if (ammeterPlaced && showPath) { path = lit; if (!state.keyClosed) path.a *= 0.4f; } // open key: the route is marked, not lit; full on CloseKey (additive SrcAlpha, so alpha is brightness)
            Tint(currentPath, path);
            Tint(currentPathReturn, path);
            if (ammeterLeads && ammeterLeads.activeSelf != ammeterPlaced) ammeterLeads.SetActive(ammeterPlaced);
            if (pathLabel) pathLabel.gameObject.SetActive(ammeterPlaced);
            if (voltmeterLeads) voltmeterLeads.enabled = voltmeterPlaced;
            if (voltmeterLeadY) voltmeterLeadY.enabled = voltmeterPlaced;

            // Drop targets breathe while a part may be dragged; the wire highlight breathes while a cue shows it.
            float breath = 1 + 0.08f * Mathf.Sin(Time.time * 4.5f);
            Breathe(seriesGlow, seriesGlowScale, draggable == "ammeter", breath);
            Breathe(parallelGlow, parallelGlowScale, draggable == "voltmeter", breath);
            if (wireHighlight && wireHighlight.activeSelf) wireHighlight.transform.localScale = highlightScale * (1 + 0.05f * Mathf.Sin(Time.time * 3f));

            // The pulse plays whenever something shows its object (cue, snap, key) and puts it away afterwards.
            if (pulse && pulse.activeSelf && pulsing == null) pulsing = StartCoroutine(Pulse());
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam) return;
            foreach (var card in billboards)
                if (card && card.gameObject.activeInHierarchy) card.rotation = Quaternion.LookRotation(card.position - cam.transform.position);
        }

        void OnDisable()
        {
            pulsing = null; // Unity stops the coroutine with the object; let a later show start a fresh run
            if (Active == this) Active = null;
        }

        // Head and chevrons run from X to Y along the rod `pulseRepeats` times, with a dark gap between runs; the
        // trail is cleared at each restart so nothing streaks back. Hiding the object mid-run ends it.
        IEnumerator Pulse()
        {
            float length = pulseTo.magnitude;
            Vector3 along = length > 0 ? pulseTo / length : Vector3.right;
            for (int repeat = 0; repeat < pulseRepeats && pulse.activeSelf; repeat++)
            {
                if (pulseHead) { pulseHead.localPosition = Vector3.zero; pulseHead.gameObject.SetActive(true); }
                if (pulseTrail) pulseTrail.Clear();
                for (float t = 0; t < 1f && pulse.activeSelf; t += Time.deltaTime / pulseSeconds)
                {
                    float travelled = t * length;
                    if (pulseHead) pulseHead.localPosition = along * travelled;
                    for (int i = 0; i < pulseChevrons.Length; i++)
                    {
                        var chevron = pulseChevrons[i];
                        if (!chevron) continue;
                        float ahead = travelled + chevronLead + i * chevronSpacing;
                        bool shown = ahead < length;
                        if (chevron.gameObject.activeSelf != shown) chevron.gameObject.SetActive(shown);
                        chevron.localPosition = along * ahead;
                    }
                    yield return null;
                }
                if (pulseHead) pulseHead.gameObject.SetActive(false);
                foreach (var chevron in pulseChevrons) if (chevron) chevron.gameObject.SetActive(false);
                for (float t = 0; t < pulseGap && pulse.activeSelf; t += Time.deltaTime) yield return null;
            }
            pulse.SetActive(false);
            pulsing = null;
        }

        static void Swing(Transform pivot, Quaternion zero, float degrees)
        {
            if (pivot) pivot.localRotation = zero * Quaternion.AngleAxis(degrees, Vector3.forward);
        }

        static void Tint(LineRenderer line, Color colour)
        {
            if (line) line.startColor = line.endColor = colour;
        }

        static void Readout(TextMeshPro reading, bool shown, string text)
        {
            if (!reading) return;
            if (reading.gameObject.activeSelf != shown) reading.gameObject.SetActive(shown);
            if (shown) reading.text = text;
        }

        static void Breathe(GameObject glow, Vector3 baseScale, bool on, float pulse)
        {
            if (!glow) return;
            if (glow.activeSelf != on) glow.SetActive(on);
            if (on) glow.transform.localScale = baseScale * pulse;
        }

        // The source's pilot lamp: emission map on its panel material, driven per renderer so the shared asset stays dark.
        void Lamp(bool on)
        {
            if (!sourceBody) return;
            var block = new MaterialPropertyBlock();
            sourceBody.GetPropertyBlock(block, lampMaterialIndex);
            block.SetColor("_EmissionColor", on ? lampColor : Color.black);
            sourceBody.SetPropertyBlock(block, lampMaterialIndex);
        }

        // Select the part with a tap (it lifts), then tap the glowing slot to place it, or drag it there; a tap on the slot
        // alone places it too. Tap the key to close the circuit. Mouse and touch. Nothing while the learner has pressed
        // PAUSE (a part mid-drag waits where it is).
        void Pointing()
        {
            var pointer = Pointer.current;
            var cam = Camera.main;
            if (pointer == null || !cam || ModuleFlow.UserPaused) return;
            Vector2 screen = pointer.position.ReadValue();
            var part = Part(draggable);
            var slot = draggable == "ammeter" ? ammeterSlot : draggable == "voltmeter" ? voltmeterSlot : null;

            if (pointer.press.wasPressedThisFrame && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            {
                // Everything under the pointer, nearest first: the part to take wins over the key's lever in front of it.
                var hits = Physics.RaycastAll(cam.ScreenPointToRay(screen), 50f);
                if (hits.Length == 0) return;
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                bool Under(Transform t) { if (t) foreach (var h in hits) if (h.transform.IsChildOf(t)) return true; return false; }
                if (keyArmed && Under(key)) { CloseKey(); return; }
                if (part && Under(part))
                {
                    if (SelectedPart != draggable) { SelectedPart = draggable; selectedHome = part.position; Selected?.Invoke(draggable); }
                    dragging = part; dragHome = selectedHome; pressAt = screen; moving = false;
                    return;
                }
                string other = draggable == "ammeter" ? "voltmeter" : "ammeter";
                if (part && !IsPlaced(other) && Under(Part(other))) { WrongPart?.Invoke(other); return; }
                // The highlighted position: tapping the slot (or its glow) places the part, selected or not.
                var first = hits[0].transform;
                if (part && slot && (first.IsChildOf(slot) || first == bench) && SlotHit(cam, screen, slot))
                    StartCoroutine(Fly(part, slot, draggable));
            }
            if (!dragging) return;

            if (pointer.press.isPressed)
            {
                moving |= (screen - pressAt).magnitude > 18f;
                var plane = new Plane(Vector3.up, dragHome + Vector3.up * 0.08f);
                var ray = cam.ScreenPointToRay(screen);
                if (moving && plane.Raycast(ray, out float at)) dragging.position = ray.GetPoint(at);
                return;
            }

            var dropped = dragging;
            dragging = null;
            var target = dropped == ammeter ? ammeterSlot : voltmeterSlot;
            if (!moving) return; // a tap: the part stays selected and lifted, waiting for the slot
            if (Vector3.Distance(dropped.position, target.position) < 0.2f) StartCoroutine(Fly(dropped, target, draggable));
            else StartCoroutine(ShakeHome(dropped, dragHome));
        }

        // The slot's footprint on the bench top, generously: a tap within 0.22 m of the slot counts as the slot.
        static bool SlotHit(Camera cam, Vector2 screen, Transform slot)
        {
            var plane = new Plane(Vector3.up, slot.position);
            var ray = cam.ScreenPointToRay(screen);
            return plane.Raycast(ray, out float at) && Vector3.Distance(ray.GetPoint(at), slot.position) < 0.22f;
        }

        // The selected part hovers a little above its place until it is placed or dragged.
        void Lift()
        {
            var part = Part(SelectedPart);
            if (!part || dragging || IsPlaced(SelectedPart)) return;
            part.position = Vector3.Lerp(part.position, selectedHome + Vector3.up * (0.035f + 0.008f * Mathf.Sin(Time.time * 3f)), 1 - Mathf.Exp(-8f * Time.deltaTime));
        }
    }
}
