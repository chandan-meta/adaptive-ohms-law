using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw
{
    // Cues for the historical lab and the Scene 8 transformation. Return true for a verb handled here (see SceneDirector.Cues.cs).
    //   vanish  [s] <name>   shrink-out on every axis (PopIn.Out), drifting up a little, then hidden
    //   retract [s] <name>   collapse its height onto its pivot (a wall lifting away with its pivot at the top), then hidden
    //   appear  [s] <name>   grow in on every axis; an object with its own PopIn just shows and pops
    //   rise    [s] <name>   grow its height up from its pivot (walls and furniture with the pivot on the floor)
    //   spread  [s] <name>   grow across X and Z from its pivot (a floor spreading out from under the bench)
    //   glide   [s] <name>   slide to the marker "<name> Goal" (its position and scale), staying level
    //   push    [s] <name>   a camera dolly: glide to "<name> Goal", or a cut to it under reduced motion (Scenes 9, 13)
    //   settle  [s] <name>   lift, move and set down on "<name> Goal"
    //   fx      <name>       play a one-shot effect object (shown, hidden again when its particles are done); thinned
    //                        under reduced motion
    //   look    [s] <name>   crossfade lights, ambient, fog, camera background, post volumes and reflection probe to a LabLook
    // Every verb sets a target state: a repeat is ignored, an opposite cue takes over from the current pose, and an object
    // already in the target state is left alone, so REPLAY, Ask Wattson's resume and the retroactive cue markers all land
    // on the same final state.
    public partial class SceneDirector
    {
        // One lighting state of the room (built by SceneBuilderBase.LabLooks). Lights of the other looks fade to zero.
        [System.Serializable]
        public class LabLook
        {
            public string name;
            public Light[] lights = new Light[0];
            public float[] intensities = new float[0];
            public Volume volume;
            public ReflectionProbe probe;
            public Light sun;
            public Color ambientSky, ambientEquator, ambientGround, background, fogColor;
            public bool fog;
            public float fogDensity;
            [Tooltip("Renderers wearing tintFrom (Scene 8's bench top under the study light, a darker copy so it does not clip to white); the crossfade into this look lerps them to tintTo's colour, then assigns tintTo.")]
            public Renderer[] tinted = new Renderer[0];
            public Material tintFrom, tintTo;
        }

        [Header("Lab looks (Scene 8: the study becomes the classroom)")]
        public LabLook[] looks = new LabLook[0];

        readonly Dictionary<GameObject, (Coroutine run, bool show, Vector3 scale, Vector3 position)> morphing = new Dictionary<GameObject, (Coroutine, bool, Vector3, Vector3)>();
        readonly HashSet<Transform> moving = new HashSet<Transform>();
        readonly HashSet<GameObject> softened = new HashSet<GameObject>(), playing = new HashSet<GameObject>();
        Coroutine lookRun;
        string lookTarget;

        bool LabCue(string verb, string[] words, string rest, float value, bool on)
        {
            switch (verb)
            {
                case "vanish": case "retract": case "appear": case "rise": case "spread":
                {
                    var (seconds, name) = Timed(words, rest, 0.6f);
                    Morph(Ref(name), verb, seconds, name);
                    return true;
                }
                case "glide": case "settle": case "push":
                {
                    var (seconds, name) = Timed(words, rest, 0.8f);
                    var go = Ref(name);
                    var goal = Ref(name + " Goal");
                    if (go && goal) Move(go.transform, goal.transform, verb == "push" && ReducedMotion.On ? 0f : seconds, verb == "settle");
                    else Debug.LogWarning($"{sceneId}: '{verb}' needs '{name}' and '{name} Goal' in the director's refs", this);
                    return true;
                }
                case "fx": Fx(Ref(rest)); return true;
                case "look":
                {
                    var (seconds, name) = Timed(words, rest, 2f);
                    Look(name, seconds);
                    return true;
                }
                default: return false;
            }
        }

        // "<verb> 0.8 Some Name" → (0.8, "Some Name"); without a number, the default and the whole rest.
        static (float seconds, string name) Timed(string[] words, string rest, float fallback) =>
            words.Length > 2 && float.TryParse(words[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var s)
                ? (s, rest.Substring(words[1].Length + 1)) : (fallback, rest);

        // ------------------------------------------------------------------ show / hide with motion

        void Morph(GameObject go, string verb, float seconds, string name)
        {
            if (!go) { Debug.LogWarning($"{sceneId}: '{verb}' found no '{name}'", this); return; }
            bool show = verb == "appear" || verb == "rise" || verb == "spread";
            if (morphing.TryGetValue(go, out var running))
            {
                if (running.show == show) return;                       // already on its way there
                if (running.run != null) StopCoroutine(running.run);
                go.transform.localScale = running.scale;                // back to its rest pose before turning round
                go.transform.localPosition = running.position;
                if (go.TryGetComponent(out PopIn paused)) paused.enabled = true;
                morphing.Remove(go);
            }
            if (go.activeSelf == show) return;                          // already in the target state
            if (show && go.TryGetComponent(out PopIn _)) { go.SetActive(true); return; }

            var axes = verb == "retract" || verb == "rise" ? Vector3.up : verb == "spread" ? new Vector3(1, 0, 1) : Vector3.one;
            var t = go.transform;
            var body = show ? PopIn.In(t, seconds, axes) : PopIn.Out(t, seconds, axes, verb == "vanish" ? 0.05f : 0f);
            morphing[go] = (null, show, t.localScale, t.localPosition);
            var run = StartCoroutine(MorphRun(go, body));
            if (morphing.TryGetValue(go, out var entry) && entry.run == null) morphing[go] = (run, entry.show, entry.scale, entry.position);
        }

        IEnumerator MorphRun(GameObject go, IEnumerator body)
        {
            yield return body;
            morphing.Remove(go);
        }

        // To the goal marker's position and scale (the marker is a scene root, so its scale is the part's target local
        // scale under an unscaled parent). settle lifts in an arc and sets down; glide stays level; 0 s is a cut.
        void Move(Transform t, Transform goal, float seconds, bool lift)
        {
            if (moving.Contains(t)) return;
            if ((t.position - goal.position).sqrMagnitude < 1e-6f && (t.localScale - goal.localScale).sqrMagnitude < 1e-6f) return;
            if (seconds <= 0f) { t.position = goal.position; t.localScale = goal.localScale; return; } // "push" under reduced motion
            moving.Add(t);
            StartCoroutine(MoveRun(t, goal, seconds, lift && !ReducedMotion.On));
        }

        IEnumerator MoveRun(Transform t, Transform goal, float seconds, bool lift)
        {
            Vector3 from = t.position, scale = t.localScale;
            float height = lift ? 0.06f + 0.08f * Vector3.Distance(from, goal.position) : 0f;
            for (float e = 0; e < 1; e += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                if (!t) yield break;
                float k = Mathf.SmoothStep(0, 1, e);
                t.position = Vector3.Lerp(from, goal.position, k) + Vector3.up * (height * Mathf.Sin(e * Mathf.PI));
                t.localScale = Vector3.Lerp(scale, goal.localScale, k);
                yield return null;
            }
            if (t) { t.position = goal.position; t.localScale = goal.localScale; }
            moving.Remove(t);
        }

        // A one-shot effect object: shown (its systems play on activation) unless it is still playing, and hidden again
        // once every particle has died, so a replay can play it again.
        void Fx(GameObject go)
        {
            if (!go || playing.Contains(go)) return;
            if (ReducedMotion.On && softened.Add(go)) Soften(go);
            playing.Add(go);
            go.SetActive(false);
            go.SetActive(true);
            StartCoroutine(FxDone(go));
        }

        IEnumerator FxDone(GameObject go)
        {
            var systems = go.GetComponentsInChildren<ParticleSystem>();
            yield return null;
            while (go && go.activeSelf && System.Array.Exists(systems, ps => ps && ps.IsAlive(true))) yield return null;
            if (go) go.SetActive(false);
            playing.Remove(go);
        }

        // Reduced motion: about a third of the particles, slower, no swirl.
        static void Soften(GameObject go)
        {
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var emission = ps.emission;
                emission.rateOverTimeMultiplier *= 0.35f;
                var bursts = new ParticleSystem.Burst[emission.burstCount];
                emission.GetBursts(bursts);
                for (int i = 0; i < bursts.Length; i++) bursts[i].count = new ParticleSystem.MinMaxCurve(Mathf.Max(4f, bursts[i].count.constantMax * 0.35f));
                emission.SetBursts(bursts);
                var main = ps.main;
                main.startSpeedMultiplier *= 0.6f;
                var velocity = ps.velocityOverLifetime;
                if (velocity.enabled) { velocity.orbitalXMultiplier *= 0.3f; velocity.orbitalYMultiplier *= 0.3f; velocity.orbitalZMultiplier *= 0.3f; }
            }
        }

        // ------------------------------------------------------------------ looks

        void Look(string name, float seconds)
        {
            var to = System.Array.Find(looks, l => l != null && l.name == name);
            if (to == null) { Debug.LogWarning($"{sceneId}: no lab look '{name}'", this); return; }
            if (lookTarget == name) return;
            lookTarget = name;
            if (lookRun != null) StopCoroutine(lookRun);
            lookRun = StartCoroutine(Crossfade(to, seconds));
        }

        IEnumerator Crossfade(LabLook to, float seconds)
        {
            // Every light any look owns: the target look's go to their built intensity, the rest to zero. Flicker pauses
            // while the intensity is driven from here; a light that ends at zero is switched off.
            var fades = new List<(Light light, float from, float to, FlickerLight flicker)>();
            foreach (var look in looks)
                for (int i = 0; i < look.lights.Length; i++)
                {
                    var light = look.lights[i];
                    if (!light || fades.Exists(f => f.light == light)) continue;
                    float target = look == to && i < look.intensities.Length ? look.intensities[i] : 0f;
                    var flicker = light.GetComponent<FlickerLight>();
                    if (flicker) flicker.enabled = false;
                    float from = light.enabled ? light.intensity : 0f;
                    if (target > 0f && !light.enabled) { light.intensity = 0f; light.enabled = true; }
                    fades.Add((light, from, target, flicker));
                }
            var volumes = new List<(Volume volume, float from, float to)>();
            foreach (var look in looks)
                if (look.volume && !volumes.Exists(v => v.volume == look.volume)) volumes.Add((look.volume, look.volume.weight, look == to ? 1f : 0f));

            var cam = Camera.main;
            Color sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor;
            Color fogColor = RenderSettings.fogColor, background = cam ? cam.backgroundColor : to.background;
            float density = RenderSettings.fog ? RenderSettings.fogDensity : 0f, targetDensity = to.fog ? to.fogDensity : 0f;
            RenderSettings.fog = density > 0f || targetDensity > 0f;
            bool swapped = false;
            bool tinting = to.tintFrom && to.tintTo && to.tinted.Length > 0;
            Color tintA = tinting ? to.tintFrom.GetColor("_BaseColor") : Color.white, tintB = tinting ? to.tintTo.GetColor("_BaseColor") : Color.white;
            var block = new MaterialPropertyBlock();
            void Tint(float k)
            {
                if (!tinting) return;
                foreach (var r in to.tinted)
                {
                    if (!r) continue;
                    var materials = r.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        if (materials[i] == to.tintFrom) { block.SetColor("_BaseColor", Color.Lerp(tintA, tintB, k)); r.SetPropertyBlock(block, i); }
                }
            }

            void Apply(float k)
            {
                Tint(k);
                foreach (var f in fades) if (f.light) f.light.intensity = Mathf.Lerp(f.from, f.to, k);
                foreach (var v in volumes) if (v.volume) v.volume.weight = Mathf.Lerp(v.from, v.to, k);
                RenderSettings.ambientSkyColor = Color.Lerp(sky, to.ambientSky, k);
                RenderSettings.ambientEquatorColor = Color.Lerp(equator, to.ambientEquator, k);
                RenderSettings.ambientGroundColor = Color.Lerp(ground, to.ambientGround, k);
                RenderSettings.fogColor = Color.Lerp(fogColor, to.fogColor, k);
                RenderSettings.fogDensity = Mathf.Lerp(density, targetDensity, k);
                if (cam) cam.backgroundColor = Color.Lerp(background, to.background, k);
                // Halfway: the reflections and the shadowing main light change over.
                if (k >= 0.5f && !swapped)
                {
                    swapped = true;
                    foreach (var look in looks) if (look.probe) look.probe.gameObject.SetActive(look == to);
                    if (to.sun) RenderSettings.sun = to.sun;
                }
            }

            for (float e = 0; e < 1; e += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                Apply(Mathf.SmoothStep(0, 1, e));
                yield return null;
            }
            Apply(1f);
            if (tinting) // the real material from here on (a replay finds nothing left to tint and keeps this state)
                foreach (var r in to.tinted)
                {
                    if (!r) continue;
                    var materials = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < materials.Length; i++)
                        if (materials[i] == to.tintFrom) { materials[i] = to.tintTo; r.SetPropertyBlock(null, i); changed = true; }
                    if (changed) r.sharedMaterials = materials;
                }
            RenderSettings.fog = to.fog;
            foreach (var f in fades)
            {
                if (!f.light) continue;
                if (f.to <= 0f) f.light.enabled = false;
                else if (f.flicker) f.flicker.enabled = true;
            }
            lookRun = null;
        }
    }
}
