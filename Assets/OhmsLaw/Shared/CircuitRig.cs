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
    // the source knob turns with the set voltage and its pilot lamp lights while the key is closed.
    public class CircuitRig : MonoBehaviour
    {
        // "Small default source voltage so the needles move" when the key is first closed (Scene 9); Scene 10 starts here.
        public const float KeyClosedDefaultVolts = 0.5f;

        public CircuitState state;
        public bool ammeterPlaced, voltmeterPlaced;

        [Header("Parts")]
        public Transform ammeter, voltmeter, key, keyLever;
        public Transform ammeterSlot, voltmeterSlot;
        public TextMeshPro ammeterReading, voltmeterReading, pathLabel;
        public Transform ammeterNeedle, voltmeterNeedle;
        [Tooltip("Glowing overlay along Source → Ammeter → XY → Key → Source; lit once the ammeter is in.")] public LineRenderer currentPath;
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
        [Tooltip("World text that turns to face the live camera (readouts, captions, badges, path label).")] public Transform[] billboards = new Transform[0];

        [Header("Learner input")]
        [Tooltip("Part the learner may drag right now: \"ammeter\", \"voltmeter\" or empty.")] public string draggable = "";
        public bool keyArmed;

        public event Action<string> Placed;
        public event Action KeyClosed;

        float shownVolts, shownAmps, shownKnob;
        Quaternion ammeterZero, voltmeterZero, knobZero;
        Vector3 seriesGlowScale, parallelGlowScale, highlightScale;
        bool lampLit;
        Transform dragging;
        Vector3 dragHome;
        Vector2 pressAt;
        bool moving;

        public float Volts => state.keyClosed && ammeterPlaced && voltmeterPlaced ? state.voltage : 0;
        public float Amps => state.keyClosed && ammeterPlaced ? ExperimentData.Main.CurrentAt(state.voltage) : 0;

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

        public void CloseKey()
        {
            if (state.keyClosed) return;
            state.keyClosed = true;
            if (state.voltage <= 0) state.voltage = KeyClosedDefaultVolts;
            keyArmed = false;
            if (sfx && click) sfx.PlayOneShot(click);
            KeyClosed?.Invoke();
        }

        Transform Part(string part) => part == "ammeter" ? ammeter : part == "voltmeter" ? voltmeter : null;

        void SetPlaced(string part)
        {
            if (part == "ammeter") ammeterPlaced = true; else voltmeterPlaced = true;
            if (draggable == part) draggable = "";
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
        }

        void Update()
        {
            Pointing();

            float ease = 1 - Mathf.Exp(-6f * Time.deltaTime);
            shownVolts = Mathf.Lerp(shownVolts, Volts, ease);
            shownAmps = Mathf.Lerp(shownAmps, Amps, ease);
            shownKnob = Mathf.Lerp(shownKnob, state.voltage, ease);
            if (voltmeterReading) voltmeterReading.text = shownVolts.ToString("0.0") + " V";
            if (ammeterReading) ammeterReading.text = shownAmps.ToString("0.00") + " A";
            Swing(voltmeterNeedle, voltmeterZero, needleSweep * Mathf.Clamp01(shownVolts / voltmeterFullScale));
            Swing(ammeterNeedle, ammeterZero, needleSweep * Mathf.Clamp01(shownAmps / ammeterFullScale));
            Swing(sourceKnob, knobZero, knobDegreesPerVolt * shownKnob);
            if (keyLever) keyLever.localRotation = Quaternion.Slerp(keyLever.localRotation, Quaternion.Euler(0, 0, state.keyClosed ? 0 : 35), 1 - Mathf.Exp(-12f * Time.deltaTime));
            if (lampLit != state.keyClosed) Lamp(lampLit = state.keyClosed);

            // The series loop reads as one path once the ammeter is in it.
            var palette = UI.Theme;
            Color lit = palette ? palette.current : new Color(1f, 0.72f, 0.19f);
            if (currentPath) currentPath.startColor = currentPath.endColor = ammeterPlaced ? lit : Color.clear;
            if (ammeterLeads && ammeterLeads.activeSelf != ammeterPlaced) ammeterLeads.SetActive(ammeterPlaced);
            if (pathLabel) pathLabel.gameObject.SetActive(ammeterPlaced);
            if (voltmeterLeads) voltmeterLeads.enabled = voltmeterPlaced;
            if (voltmeterLeadY) voltmeterLeadY.enabled = voltmeterPlaced;

            // Drop targets breathe while a part may be dragged; the wire highlight breathes while a cue shows it.
            float pulse = 1 + 0.08f * Mathf.Sin(Time.time * 4.5f);
            Breathe(seriesGlow, seriesGlowScale, draggable == "ammeter", pulse);
            Breathe(parallelGlow, parallelGlowScale, draggable == "voltmeter", pulse);
            if (wireHighlight && wireHighlight.activeSelf) wireHighlight.transform.localScale = highlightScale * (1 + 0.05f * Mathf.Sin(Time.time * 3f));
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam) return;
            foreach (var card in billboards)
                if (card && card.gameObject.activeInHierarchy) card.rotation = Quaternion.LookRotation(card.position - cam.transform.position);
        }

        static void Swing(Transform pivot, Quaternion zero, float degrees)
        {
            if (pivot) pivot.localRotation = zero * Quaternion.AngleAxis(degrees, Vector3.forward);
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

        // Drag a part into its glowing slot, or just tap it. Tap the key to close the circuit. Mouse and touch.
        void Pointing()
        {
            var pointer = Pointer.current;
            var cam = Camera.main;
            if (pointer == null || !cam) return;
            Vector2 screen = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            {
                if (!Physics.Raycast(cam.ScreenPointToRay(screen), out var hit, 50f)) return;
                if (keyArmed && hit.transform.IsChildOf(key)) { CloseKey(); return; }
                var part = Part(draggable);
                if (part && hit.transform.IsChildOf(part)) { dragging = part; dragHome = part.position; pressAt = screen; moving = false; }
            }
            if (!dragging) return;

            if (pointer.press.isPressed)
            {
                moving |= (screen - pressAt).magnitude > 18f;
                var bench = new Plane(Vector3.up, dragHome + Vector3.up * 0.08f);
                var ray = cam.ScreenPointToRay(screen);
                if (moving && bench.Raycast(ray, out float at)) dragging.position = ray.GetPoint(at);
                return;
            }

            var dropped = dragging;
            dragging = null;
            var slot = dropped == ammeter ? ammeterSlot : voltmeterSlot;
            if (!moving || Vector3.Distance(dropped.position, slot.position) < 0.2f) StartCoroutine(Fly(dropped, slot, draggable));
            else StartCoroutine(ShakeHome(dropped, dragHome));
        }
    }
}
