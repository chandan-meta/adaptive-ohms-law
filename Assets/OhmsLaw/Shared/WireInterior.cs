using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw
{
    // The electron space (Scenes 5, 6, 12): a translucent conduit (the inside of the wire) between a − and a + terminal.
    // A small, representative sample of free electrons, spread evenly along the conduit so single ones can be followed.
    // Every electron moves randomly all the time (thermal motion: scattered into a new direction several times a second,
    // never still) around its place in the stream. With the field on, every place drifts together from the − end (local
    // −X) toward the + end (local +X), slower than the random motion; nothing is pushed electron to electron. A place that
    // reaches the + end carries on from the − end inside the terminal blocks (the circuit takes it round), so no electron
    // appears from nowhere and none pile up. Two tracer electrons leave a short trail. The counter counts the net charges
    // crossing the ring (random back-and-forth crossings cancel) and shows the completed value of each second. Lattice
    // ions and collisions are Scene 12 only; there the drift can follow the bench's current (rig + driftPerAmp).
    // While this section is active it owns the scene's atmosphere (fog, ambient, background), so Scene 12 can dive in
    // and out of the wire with a show/hide.
    public class WireInterior : MonoBehaviour
    {
        [Header("Free electrons (a small, representative sample)")]
        [Tooltip("Electrons in the conduit, spread evenly along it (count / length per metre): no gaps, no clumps.")] public int count = 49;
        [Tooltip("Thermal speed, m/s: the random motion. Faster than the drift, so it stays visible once the field is on.")] public float thermalSpeed = 1.1f;
        [Tooltip("Random scatterings per second; each sends the electron off in a new direction.")] public float scatterRate = 4f;
        [Tooltip("Typical distance an electron wanders from its place in the stream, metres.")] public float wander = 0.35f;
        [Tooltip("Drift with the field on, m/s, when no rig drives it (Scenes 5-6).")] public float driftSpeed = 0.6f;
        [Tooltip("Scene 12: the drift follows this rig's current, driftPerAmp m/s per ampere, so a voltage change on the bench shows inside the wire.")]
        public CircuitRig rig;
        public float driftPerAmp;
        public bool fieldOn;
        [Tooltip("Ohm-niscope time-freeze: no drift, a little random motion only, electrons frosted.")] public bool frozen;
        [Range(0, 1), Tooltip("Collisions with the lattice ions (Scene 12): the drift slows and electrons bounce.")] public float impede;
        public float carrierSize = 0.26f;

        [Header("Following one electron")]
        [Tooltip("A short trail behind each tracer electron (one per line, spread along the stream). Off with reduced motion.")] public LineRenderer[] trails = new LineRenderer[0];
        public bool trailsOn = true;

        [Header("Wire")]
        [Tooltip("Glass colour of the conduit, alpha included; the frost pales it while frozen.")] public Color conduitColor = new Color(0.31f, 0.82f, 1f, 0.12f);
        [Range(0, 1), Tooltip("Target visibility of the lattice ions; they fade toward it.")] public float latticeVisibility;
        [Tooltip("Target radius of the conduit in metres: a cue narrows it (Scene 12), never below minRadiusFraction of the built radius and never above it.")]
        public float tunnelRadius = 0.6f;
        [Range(0.5f, 1f), Tooltip("The narrowing metaphor stays subtle: never below this fraction of the built radius.")]
        public float minRadiusFraction = 0.8f;
        public float length = 9.8f;

        [Header("Atmosphere (applied while the section is active)")]
        public Color fogColor, ambient, background;
        public float fogDensity = 0.07f;

        [Header("Parts")]
        public ParticleSystem carriers;
        public ParticleSystem lattice;
        public Renderer tunnel;
        [Tooltip("Cross-section ring (unit radius, child ring scaled 1.1); follows the conduit radius. Electrons flash and count only while it is shown.")] public Transform crossSection;
        [Tooltip("Decorative bands around the conduit (unit radius); follow the conduit radius.")] public Transform[] bands = new Transform[0];
        [Tooltip("Net charges through the cross-section in the last completed second.")] public TextMeshPro counter;
        public LineRenderer counterRing;
        public float counterRingRadius = 0.55f;
        public bool counting;
        [Tooltip("Scene 12: the bench's live readings (the rig's voltage and current) beside the stream.")] public TextMeshPro readoutVolts, readoutAmps;

        const float TrailStep = 0.06f;  // seconds between trail points
        const int TrailPoints = 16;     // about one second of path
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly Color FrostTint = new Color(0.6f, 0.72f, 0.9f), IonTint = new Color(0.62f, 0.66f, 0.56f);

        Vector3[] home, offset, velocity, position;   // home: the electron's place in the stream (drifts); offset: its random motion
        float[] flash, size, alpha;
        bool[] pending, tracer, wrapped;
        ParticleSystem.Particle[] particles, ions;
        Vector3[][] trail;
        int[] trailCount, tracerIndex;
        float[] trailWidth;
        MaterialPropertyBlock block;
        float radius, builtRadius, relax, drift, second, frost, trailClock, shownLattice, shownVolts = -1, shownAmps = -1;
        int tally;
        bool align = true, skip, shown;
        Color tint;

        // Atmosphere of the scene before this section took over.
        bool fogWas; FogMode fogModeWas; AmbientMode ambientModeWas; Color fogColorWas, ambientWas, backgroundWas; float fogDensityWas;

        public int LastSecond { get; private set; }
        // This second so far: the charges counted since the window started (1, 2, 3 ...), and how far through it we are.
        public int Tally => counting ? tally : 0;
        public float SecondProgress => counting ? Mathf.Clamp01(second) : 0f;
        // A completed second has been shown on the counter at least once.
        public bool Shown => shown;
        // A charge crossed the ring and was counted (the panel's counter ticks on it).
        public event System.Action Counted;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            radius = builtRadius = tunnelRadius;
            // Random motion relaxes back toward its place: with scattering at scatterRate the spread settles near `wander`.
            relax = wander * wander * scatterRate / (0.22f * thermalSpeed * thermalSpeed);
            home = new Vector3[count];
            offset = new Vector3[count];
            velocity = new Vector3[count];
            position = new Vector3[count];
            flash = new float[count];
            size = new float[count];
            alpha = new float[count];
            pending = new bool[count];
            tracer = new bool[count];
            wrapped = new bool[count];
            particles = new ParticleSystem.Particle[count];
            float spacing = length / count;
            for (int i = 0; i < count; i++)
            {
                size[i] = carrierSize * Random.Range(0.85f, 1.15f);
                var disc = Random.insideUnitCircle * (radius - size[i] * 0.6f);
                home[i] = new Vector3(-length / 2 + (i + 0.5f) * spacing, disc.x, disc.y);
                offset[i] = Random.insideUnitSphere * wander;
                velocity[i] = Random.onUnitSphere * thermalSpeed * Random.Range(0.6f, 1f);
            }

            // Tracers spread evenly along the stream, so one is always in view.
            trail = new Vector3[trails.Length][];
            trailCount = new int[trails.Length];
            tracerIndex = new int[trails.Length];
            trailWidth = new float[trails.Length];
            for (int k = 0; k < trails.Length; k++)
            {
                trail[k] = new Vector3[TrailPoints];
                tracerIndex[k] = (k * count / trails.Length + count / (2 * trails.Length)) % count;
                tracer[tracerIndex[k]] = true;
                trailWidth[k] = trails[k] ? trails[k].widthMultiplier : 0;
                if (trails[k]) trails[k].positionCount = 0;
            }

            var palette = UI.Theme;
            tint = palette ? palette.charge : new Color(0.84f, 0.98f, 1f);
            Restore();
            if (carriers)
            {
                var main = carriers.main;
                if (main.maxParticles < count) main.maxParticles = count;
                if (!carriers.isPlaying) carriers.Play();
            }

            // Lattice ions: a hexagonal column of dull spheres along the conduit, larger than the electrons; they fade in.
            var grid = new System.Collections.Generic.List<ParticleSystem.Particle>();
            float ring = tunnelRadius * 0.55f, ionSize = tunnelRadius * 0.42f;
            for (float x = -length / 2 + 0.5f; x < length / 2 - 0.3f; x += 0.5f)
                for (int s = 0; s < 7; s++)
                {
                    float a = s * Mathf.PI / 3f + (Mathf.Round(x * 2) % 2 == 0 ? 0 : Mathf.PI / 6f); // alternate slices turn half a step
                    var p = s == 6 ? new Vector3(x, 0, 0) : new Vector3(x, Mathf.Cos(a) * ring, Mathf.Sin(a) * ring);
                    grid.Add(new ParticleSystem.Particle { position = p, startSize = ionSize, remainingLifetime = 1e6f, startLifetime = 1e6f });
                }
            ions = grid.ToArray();
            if (lattice)
            {
                lattice.SetParticles(ions, 0);
                if (!lattice.isPlaying) lattice.Play();
            }
        }

        void OnEnable()
        {
            if (fogColor == default) { fogColor = new Color(0.043f, 0.086f, 0.188f); ambient = fogColor * 1.5f; background = fogColor; } // scene built before the atmosphere fields existed
            fogWas = RenderSettings.fog; fogModeWas = RenderSettings.fogMode; fogColorWas = RenderSettings.fogColor; fogDensityWas = RenderSettings.fogDensity;
            ambientModeWas = RenderSettings.ambientMode; ambientWas = RenderSettings.ambientLight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            var cam = Camera.main;
            if (cam) { backgroundWas = cam.backgroundColor; cam.backgroundColor = background; }
        }

        void OnDisable()
        {
            RenderSettings.fog = fogWas; RenderSettings.fogMode = fogModeWas; RenderSettings.fogColor = fogColorWas; RenderSettings.fogDensity = fogDensityWas;
            RenderSettings.ambientMode = ambientModeWas; RenderSettings.ambientLight = ambientWas;
            var cam = Camera.main;
            if (cam) cam.backgroundColor = backgroundWas;
        }

        // Drift speed the field gives right now (0 with the field off or frozen).
        float TargetDrift() =>
            fieldOn && !frozen ? (rig && driftPerAmp > 0 ? rig.Amps * driftPerAmp : driftSpeed) * (1 - 0.55f * impede) : 0;

        void Update() => Step(Time.deltaTime);

        // One simulation step (Editor/WireChecks.cs drives it directly with a fixed dt).
        void Step(float dt)
        {
            float half = length / 2;
            bool calm = ReducedMotion.On;
            radius = Mathf.MoveTowards(radius, Mathf.Clamp(tunnelRadius, builtRadius * minRadiusFraction, builtRadius), dt * 0.3f);
            frost = Mathf.MoveTowards(frost, frozen ? 1 : 0, dt * 2f);
            // The field acts along the whole wire at once: every place drifts together. A short ease keeps a change readable.
            float target = TargetDrift();
            if (drift != target) { drift = Mathf.MoveTowards(drift, target, dt * 1.5f); align = true; }
            float speed = thermalSpeed * (frozen ? 0.3f : 1f);
            bool ring = crossSection && crossSection.gameObject.activeInHierarchy;
            float crossX = ring ? transform.InverseTransformPoint(crossSection.position).x : 0;
            var cam = Camera.main;
            Vector3 eye = cam ? transform.InverseTransformPoint(cam.transform.position) : Vector3.one * 1e4f;
            Color body = Color.Lerp(tint, FrostTint, frost);

            for (int i = 0; i < count; i++)
            {
                // Thermal motion: scattered into a new random direction every so often, never still; it relaxes back
                // toward the electron's place, so the stream stays even.
                if (Random.value < dt * scatterRate) velocity[i] = Random.onUnitSphere * speed * Random.Range(0.6f, 1f);
                // Scene 12: bouncing off the lattice ions, with a little flash on each hit.
                if (impede > 0 && Random.value < dt * 6f * impede) { velocity[i] = -velocity[i] * 0.6f + Random.insideUnitSphere * speed * 0.4f; flash[i] = Mathf.Max(flash[i], calm ? 0.3f : 0.6f); }
                offset[i] += (velocity[i] - offset[i] / relax) * dt;

                // The place drifts toward +; past the + end it carries on from the − end, inside the terminal blocks.
                float was = home[i].x;
                home[i].x += drift * dt;
                wrapped[i] = home[i].x > half;
                if (wrapped[i]) { home[i].x -= length; pending[i] = false; }
                else if (ring && was < crossX && home[i].x >= crossX) { pending[i] = true; if (counting) Crossed(target); }

                // Inside the glass: the random motion bounces off the wall.
                Vector3 p = home[i] + offset[i];
                float limit = Mathf.Max(0.05f, radius - size[i] * 0.6f);
                var radial = new Vector2(p.y, p.z);
                if (radial.sqrMagnitude > limit * limit)
                {
                    radial = radial.normalized * limit;
                    offset[i].y = radial.x - home[i].y;
                    offset[i].z = radial.y - home[i].z;
                    p.y = radial.x;
                    p.z = radial.y;
                    var inward = -new Vector3(0, radial.x, radial.y).normalized;
                    if (Vector3.Dot(velocity[i], inward) < 0) velocity[i] = Vector3.Reflect(velocity[i], inward);
                }
                // A counted electron flashes as it passes the ring itself.
                if (!ring) pending[i] = false;
                else if (pending[i] && p.x >= crossX) { pending[i] = false; flash[i] = calm ? 0.5f : 1f; }

                // Fade into the terminal blocks (where the stream goes round), by the electron or its place, whichever is
                // nearer an end, so going round never pops; and never a dinner plate over the lens.
                float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(half - 0.75f, half - 0.35f, Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(home[i].x))));
                a *= Mathf.InverseLerp(0.5f, 1f, Vector3.Distance(p, eye));
                position[i] = p;
                alpha[i] = a;
                flash[i] = Mathf.Max(0, flash[i] - dt * 3f);

                var colour = Color.Lerp(tracer[i] ? Color.Lerp(body, Color.white, 0.35f) : body, Color.white, flash[i]);
                colour.a = a;
                particles[i].position = p;
                particles[i].startSize = size[i] * (tracer[i] ? 1.15f : 1f) + (calm ? 0f : 0.1f * flash[i]);
                particles[i].startColor = colour;
                particles[i].remainingLifetime = particles[i].startLifetime = 1e6f;
            }
            if (carriers) carriers.SetParticles(particles, count);

            Trails(dt, calm);
            Lattice(dt);
            Tunnel();
            Readout();
            Count(dt);
            Remember();
        }

        // A place crossed the ring. The first crossing after a change lines the one-second window up halfway between
        // crossings, so a steady drift always reads the same number; that partial window is not shown.
        void Crossed(float target)
        {
            if (align && drift == target && drift > 0)
            {
                align = false;
                skip = true;
                tally = 0;
                second = Mathf.Max(0f, 1f - 0.5f * length / count / drift);
            }
            tally++;
            if (!skip) Counted?.Invoke();
        }

        void Count(float dt)
        {
            if (!counting) { second = 0; tally = 0; align = true; return; }
            second += dt;
            if (second >= 1f)
            {
                second -= 1f;
                if (skip) skip = false;
                else { LastSecond = tally; shown = true; }
                tally = 0;
            }
            string text = shown ? LastSecond.ToString(CultureInfo.InvariantCulture) : "";
            if (counter && counter.text != text) counter.text = text;
            if (counterRing)
            {
                int points = Mathf.Max(2, Mathf.CeilToInt(second * 48));
                counterRing.positionCount = points;
                for (int i = 0; i < points; i++)
                {
                    float a = Mathf.PI / 2 - i / 48f * Mathf.PI * 2;
                    counterRing.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * counterRingRadius);
                }
            }
        }

        // The tracers' trails: a point every TrailStep, the head on the electron; cleared when the electron goes round.
        void Trails(float dt, bool calm)
        {
            if (trails.Length == 0) return;
            trailClock += dt;
            bool sample = trailClock >= TrailStep;
            if (sample) trailClock %= TrailStep;
            for (int k = 0; k < trails.Length; k++)
            {
                var line = trails[k];
                if (!line) continue;
                int i = tracerIndex[k];
                bool on = trailsOn && !calm && alpha[i] > 0.02f;
                if (!on || wrapped[i]) trailCount[k] = 0;
                if (!on) { if (line.positionCount != 0) line.positionCount = 0; continue; }
                var points = trail[k];
                if (trailCount[k] == 0) { points[0] = points[1] = position[i]; trailCount[k] = 2; }
                else if (sample)
                {
                    if (trailCount[k] < points.Length) trailCount[k]++;
                    else System.Array.Copy(points, 1, points, 0, points.Length - 1);
                    points[trailCount[k] - 1] = position[i];
                }
                else points[trailCount[k] - 1] = position[i];
                line.positionCount = trailCount[k];
                for (int j = 0; j < trailCount[k]; j++) line.SetPosition(j, points[j]);
                line.widthMultiplier = trailWidth[k] * alpha[i];
            }
        }

        void Lattice(float dt)
        {
            if (!lattice || shownLattice == latticeVisibility) return;
            shownLattice = Mathf.MoveTowards(shownLattice, latticeVisibility, dt * 0.6f); // fades in over ~1.7 s
            var ionColor = new Color(IonTint.r, IonTint.g, IonTint.b, shownLattice);
            for (int i = 0; i < ions.Length; i++) ions[i].startColor = ionColor;
            lattice.SetParticles(ions, shownLattice > 0 ? ions.Length : 0);
        }

        void Tunnel()
        {
            if (tunnel)
            {
                tunnel.transform.localScale = new Vector3(length, radius, radius);
                var glass = Color.Lerp(conduitColor, new Color(FrostTint.r, FrostTint.g, FrostTint.b, conduitColor.a + 0.08f), frost);
                tunnel.GetPropertyBlock(block);
                block.SetColor(BaseColor, glass);
                block.SetColor(EmissionColor, new Color(glass.r, glass.g, glass.b) * 0.12f); // a faint self-glow so the glass reads in the dark
                tunnel.SetPropertyBlock(block);
            }
            var ringScale = new Vector3(radius, radius, 1); // unit-radius rings around the conduit
            if (crossSection) crossSection.localScale = ringScale;
            foreach (var band in bands) if (band) band.localScale = ringScale;
        }

        // The rig's own values, never a request: what the bench meters read.
        void Readout()
        {
            if (!rig) return;
            float volts = rig.Volts, amps = rig.Amps;
            if (readoutVolts && volts != shownVolts) { shownVolts = volts; readoutVolts.text = "V = " + volts.ToString("0.0", CultureInfo.InvariantCulture) + " V"; }
            if (readoutAmps && amps != shownAmps) { shownAmps = amps; readoutAmps.text = "I = " + amps.ToString("0.00", CultureInfo.InvariantCulture) + " A"; }
        }

        // ------------------------------------------------------------------ match-cut carry-over

        // Scene 5 → 6 is a match-cut: the next scene's wire carries on with the same electrons, trails and counter.
        class Carry
        {
            public WireInterior owner;
            public string scene;
            public Vector3[] home, offset, velocity;
            public Vector3[][] trail;
            public int[] trailCount;
            public float drift, second;
            public int tally, lastSecond;
            public bool align, skip, shown;
        }

        static Carry carry;

        void Remember()
        {
            if (carry == null || carry.owner != this)
                carry = new Carry { owner = this, scene = gameObject.scene.name, home = home, offset = offset, velocity = velocity, trail = trail, trailCount = trailCount };
            carry.drift = drift;
            carry.second = second;
            carry.tally = tally;
            carry.lastSecond = LastSecond;
            carry.align = align;
            carry.skip = skip;
            carry.shown = shown;
        }

        // Only when this scene match-cuts from the scene that left the carry, and the hand-off says we came from there.
        void Restore()
        {
            var c = carry;
            if (c == null || c.scene == gameObject.scene.name || c.home.Length != count || c.trail.Length != trails.Length) return;
            SceneDirector director = null; // this scene's own, even while the previous scene is still being unloaded
            foreach (var root in gameObject.scene.GetRootGameObjects()) if (root.TryGetComponent(out director)) break;
            if (!director || director.matchCutFrom != c.scene || SceneHandoffState.fromScene != c.scene) return;
            c.home.CopyTo(home, 0);
            c.offset.CopyTo(offset, 0);
            c.velocity.CopyTo(velocity, 0);
            for (int k = 0; k < trails.Length; k++) { c.trail[k].CopyTo(trail[k], 0); trailCount[k] = c.trailCount[k]; }
            drift = c.drift;
            second = c.second;
            tally = c.tally;
            LastSecond = c.lastSecond;
            align = c.align;
            skip = c.skip;
            shown = c.shown;
        }
    }
}
