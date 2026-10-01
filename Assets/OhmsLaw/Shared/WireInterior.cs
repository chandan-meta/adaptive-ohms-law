using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw
{
    // Inside a metal wire. Free electrons always move randomly (thermal motion). With the field on, all of them
    // drift together from the - terminal end (local -X) to the + terminal end (local +X); the whole tunnel
    // responds at once, nothing is pushed electron to electron. Ions of the lattice and collisions are Scene 12 only.
    // While this section is active it also owns the scene's atmosphere (tinted fog, ambient, background), so
    // Scene 12 can dive in and out of the wire with a show/hide.
    public class WireInterior : MonoBehaviour
    {
        [Header("Charge carriers")]
        public int count = 700;
        public float thermalJitter = 1.6f;
        public float driftSpeed = 0.9f;
        public bool fieldOn;
        [Tooltip("Ohm-niscope time-freeze: drift held at zero, a little jitter only, carriers frosted.")] public bool frozen;
        [Range(0, 1), Tooltip("Collisions with the lattice ions: carriers slow down and bounce.")] public float impede;
        public float carrierSize = 0.14f;
        [Tooltip("Carriers fade out this close to the lens so a sprite never covers the frame.")] public float nearFade = 0.8f;

        [Header("Wire")]
        public Color materialTint = new Color(0.85f, 0.45f, 0.25f);
        [Range(0, 1)] public float latticeVisibility;
        public float tunnelRadius = 2.5f;
        [Range(0.5f, 1f), Tooltip("The narrowing metaphor stays subtle: never below this fraction of the built radius (the camera stays inside).")]
        public float minRadiusFraction = 0.8f;
        public float length = 16f;

        [Header("Atmosphere (applied while the section is active)")]
        public Color fogColor, ambient, background;
        public float fogDensity = 0.035f;

        [Header("Parts")]
        public ParticleSystem carriers, lattice;
        public Renderer tunnel;
        public Transform crossSection;
        [Tooltip("Net charges through the cross-section in the current second.")] public TextMeshPro counter;
        public LineRenderer counterRing;
        public bool counting;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly Color FrostTint = new Color(0.6f, 0.72f, 0.9f), IonTint = new Color(0.48f, 0.49f, 0.56f);
        Vector3[] position, velocity;
        float[] flash;
        ParticleSystem.Particle[] particles, ions;
        MaterialPropertyBlock block;
        float radius, builtRadius, shownLattice = -1, second, frost;
        int tally;
        Color tint;

        // Atmosphere of the scene before this section took over.
        bool fogWas; FogMode fogModeWas; AmbientMode ambientModeWas; Color fogColorWas, ambientWas, backgroundWas; float fogDensityWas;

        public int LastSecond { get; private set; }

        void Awake()
        {
            block = new MaterialPropertyBlock();
            radius = builtRadius = tunnelRadius;
            position = new Vector3[count];
            velocity = new Vector3[count];
            flash = new float[count];
            particles = new ParticleSystem.Particle[count];
            for (int i = 0; i < count; i++)
            {
                position[i] = RandomPoint(0.92f);
                velocity[i] = Random.onUnitSphere * thermalJitter;
            }
            var palette = UI.Theme;
            tint = palette ? palette.charge : new Color(0.84f, 0.98f, 1f);

            // Lattice ions on a fixed grid; only shown when latticeVisibility > 0.
            var grid = new System.Collections.Generic.List<ParticleSystem.Particle>();
            for (float x = -length / 2 + 0.6f; x < length / 2; x += 1.2f)
                for (float y = -tunnelRadius; y <= tunnelRadius; y += 1.1f)
                    for (float z = -tunnelRadius; z <= tunnelRadius; z += 1.1f)
                        if (y * y + z * z < tunnelRadius * tunnelRadius * 0.8f)
                            grid.Add(new ParticleSystem.Particle { position = new Vector3(x, y, z), startSize = 0.3f, remainingLifetime = 1e6f, startLifetime = 1e6f });
            ions = grid.ToArray();
            if (carriers && !carriers.isPlaying) carriers.Play();
            if (lattice && !lattice.isPlaying) lattice.Play();
        }

        void OnEnable()
        {
            if (fogColor == default) { fogColor = materialTint * 0.45f; ambient = materialTint * 0.35f; background = materialTint * 0.25f; } // scene built before the atmosphere fields existed
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

        Vector3 RandomPoint(float fill)
        {
            var disc = Random.insideUnitCircle * radius * fill;
            return new Vector3(Random.Range(-length / 2, length / 2), disc.x, disc.y);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            radius = Mathf.MoveTowards(radius, Mathf.Max(tunnelRadius, builtRadius * minRadiusFraction), dt * 1.2f);
            frost = Mathf.MoveTowards(frost, frozen ? 1 : 0, dt * 2f);
            float drift = fieldOn && !frozen ? driftSpeed * (1 - 0.55f * impede) : 0;
            float jitter = thermalJitter * (frozen ? 0.3f : 1);
            float crossX = crossSection ? transform.InverseTransformPoint(crossSection.position).x : 0;
            float limit = radius * 0.92f;
            var cam = Camera.main;
            Vector3 eye = cam ? transform.InverseTransformPoint(cam.transform.position) : Vector3.one * 1e4f;
            Color body = Color.Lerp(tint, FrostTint, frost);

            for (int i = 0; i < count; i++)
            {
                // Thermal motion: a new random direction every so often, as if scattered.
                if (Random.value < dt * 5f) velocity[i] = Random.onUnitSphere * jitter * Random.Range(0.5f, 1f);
                // Scene 12: bouncing off lattice ions, with a spark of a flash on each hit.
                if (impede > 0 && Random.value < dt * 6f * impede) { velocity[i] = -velocity[i] * 0.6f + Random.insideUnitSphere * jitter * 0.4f; flash[i] = Mathf.Max(flash[i], 0.6f); }
                Vector3 was = position[i];
                Vector3 p = was + (velocity[i] + Vector3.right * drift) * dt;

                var radial = new Vector2(p.y, p.z);
                if (radial.sqrMagnitude > limit * limit)
                {
                    radial = radial.normalized * limit;
                    p.y = radial.x;
                    p.z = radial.y;
                    var inward = -new Vector3(0, radial.x, radial.y).normalized;
                    if (Vector3.Dot(velocity[i], inward) < 0) velocity[i] = Vector3.Reflect(velocity[i], inward);
                }

                bool wrapped = false;
                if (p.x > length / 2) { p.x -= length; wrapped = true; }
                else if (p.x < -length / 2) { p.x += length; wrapped = true; }
                if (!wrapped && (was.x - crossX) * (p.x - crossX) < 0)
                {
                    flash[i] = 1;
                    if (counting) tally += p.x > was.x ? 1 : -1;
                }
                position[i] = p;
                flash[i] = Mathf.Max(0, flash[i] - dt * 3f);

                var colour = Color.Lerp(body, Color.white, flash[i]);
                colour.a = Mathf.InverseLerp(nearFade * 0.55f, nearFade, Vector3.Distance(p, eye)); // never a dinner plate over the lens
                particles[i].position = p;
                particles[i].startSize = carrierSize + 0.12f * flash[i];
                particles[i].startColor = colour;
                particles[i].remainingLifetime = particles[i].startLifetime = 1e6f;
            }
            if (carriers) carriers.SetParticles(particles, count);

            if (lattice && !Mathf.Approximately(shownLattice, latticeVisibility))
            {
                shownLattice = latticeVisibility;
                var ionColor = new Color(IonTint.r, IonTint.g, IonTint.b, latticeVisibility * 0.55f);
                for (int i = 0; i < ions.Length; i++) ions[i].startColor = ionColor;
                lattice.SetParticles(ions, latticeVisibility > 0 ? ions.Length : 0);
            }

            if (tunnel)
            {
                tunnel.transform.localScale = new Vector3(length, radius, radius);
                tunnel.GetPropertyBlock(block);
                block.SetColor(BaseColor, materialTint);
                block.SetColor(EmissionColor, materialTint * 0.08f); // a faint self-glow so the metal's colour reads in the dark
                tunnel.SetPropertyBlock(block);
            }
            if (crossSection) crossSection.localScale = new Vector3(radius, radius, 1); // unit-radius ring and disc

            // The counter ticks with each net crossing; the ring fills over one second, then both reset.
            if (!counting) { tally = 0; second = 0; return; }
            second += dt;
            if (second >= 1f) { LastSecond = tally; tally = 0; second -= 1f; }
            if (counter) counter.text = Mathf.Max(0, tally).ToString();
            if (counterRing)
            {
                int points = Mathf.Max(2, Mathf.CeilToInt(second * 48));
                counterRing.positionCount = points;
                for (int i = 0; i < points; i++)
                {
                    float a = Mathf.PI / 2 - i / 48f * Mathf.PI * 2;
                    counterRing.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.55f);
                }
            }
        }
    }
}
