using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw
{
    // The electron space (Scenes 5, 6, 12): a translucent conduit between a − and a + terminal in a dark-blue void.
    // Free electrons always move randomly (thermal motion), never still. With the field on, ALL of them drift together
    // from the − end (local −X) to the + end (local +X); the whole conduit responds at once, nothing is pushed electron
    // to electron. Besides the carriers inside the conduit there are ambient electrons in the space around it, laid out
    // in depth layers in front of the "Wire Inside" camera; they fade out near the lens, in the subtitle band and over
    // Wattson so they never cover him or the text. Lattice ions and collisions are Scene 12 only.
    // While this section is active it also owns the scene's atmosphere (fog, ambient, background), so Scene 12 can dive
    // in and out of the wire with a show/hide.
    public class WireInterior : MonoBehaviour
    {
        [System.Serializable]
        public struct SpaceLayer
        {
            [Tooltip("Depth range from the Wire Inside camera, metres.")] public float near, far;
            public int count;
            public float size;
        }

        [Header("Charge carriers (inside the conduit)")]
        public int count = 150;
        public float thermalJitter = 0.45f;
        public float driftSpeed = 0.9f;
        public bool fieldOn;
        [Tooltip("Ohm-niscope time-freeze: drift held at zero, a little jitter only, carriers frosted.")] public bool frozen;
        [Range(0, 1), Tooltip("Collisions with the lattice ions: carriers slow down and bounce.")] public float impede;
        public float carrierSize = 0.26f;

        [Header("Electrons in the space (depth layers in the Wire Inside frame)")]
        public SpaceLayer[] spaceLayers = new SpaceLayer[0];
        [Tooltip("Wire Inside camera, in this section's local space: the layers are frustum slabs of this view.")]
        public Vector3 spaceEye, spaceForward = Vector3.forward;
        public float spaceFov = 60f;
        [Tooltip("Electrons fade out this close to the lens (fully visible at twice the distance).")] public float nearFade = 0.5f;
        [Tooltip("Viewport height of the subtitle band; space electrons fade out below it.")] [Range(0, 0.5f)] public float subtitleBand = 0.25f;
        [Tooltip("Space electrons fade out in front of this (Wattson); found at runtime when empty.")] public Transform avoid;
        public float avoidRadius = 0.13f;
        public float floorY = -1.7f;

        [Header("Wire")]
        public Color materialTint = new Color(0.85f, 0.45f, 0.25f);
        [Tooltip("Glass colour of the conduit, alpha included; the frost pales it while frozen.")] public Color conduitColor = new Color(0.31f, 0.82f, 1f, 0.12f);
        [Range(0, 1)] public float latticeVisibility;
        [Tooltip("Target radius of the conduit in metres: a cue narrows it (Scene 12), never below minRadiusFraction of the built radius and never above it.")]
        public float tunnelRadius = 0.6f;
        [Range(0.5f, 1f), Tooltip("The narrowing metaphor stays subtle: never below this fraction of the built radius.")]
        public float minRadiusFraction = 0.8f;
        public float length = 9.8f;

        [Header("Atmosphere (applied while the section is active)")]
        public Color fogColor, ambient, background;
        public float fogDensity = 0.07f;

        [Header("Parts")]
        [Tooltip("One system for every electron: carriers first, then the space layers, so they depth-sort together.")] public ParticleSystem carriers;
        public ParticleSystem lattice;
        public Renderer tunnel;
        [Tooltip("Cross-section ring (unit radius, child ring scaled 1.1); follows the conduit radius.")] public Transform crossSection;
        [Tooltip("Decorative bands around the conduit (unit radius); follow the conduit radius.")] public Transform[] bands = new Transform[0];
        [Tooltip("Net charges through the cross-section in the current second.")] public TextMeshPro counter;
        public LineRenderer counterRing;
        public float counterRingRadius = 0.55f;
        public bool counting;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly Color FrostTint = new Color(0.6f, 0.72f, 0.9f), IonTint = new Color(0.5f, 0.56f, 0.5f);
        Vector3[] position, velocity;
        float[] flash, size;
        int[] layer;             // -1 = conduit carrier, else index into spaceLayers
        ParticleSystem.Particle[] particles, ions;
        MaterialPropertyBlock block;
        float radius, builtRadius, shownLattice = -1, second, frost;
        int total, tally;
        bool avoidSearched;
        Color tint;
        Vector3 spaceRight, spaceUp;
        float tanHalf;
        const float Aspect = 16f / 9f, LateralMargin = 1.15f; // layers reach a little past the frame edges so wrapping is off-screen

        // Atmosphere of the scene before this section took over.
        bool fogWas; FogMode fogModeWas; AmbientMode ambientModeWas; Color fogColorWas, ambientWas, backgroundWas; float fogDensityWas;

        public int LastSecond { get; private set; }

        void Awake()
        {
            block = new MaterialPropertyBlock();
            radius = builtRadius = tunnelRadius;
            tanHalf = Mathf.Tan(spaceFov * 0.5f * Mathf.Deg2Rad);
            spaceForward = spaceForward.sqrMagnitude > 1e-6f ? spaceForward.normalized : Vector3.forward;
            spaceRight = Vector3.Cross(Vector3.up, spaceForward).normalized;
            spaceUp = Vector3.Cross(spaceForward, spaceRight);

            total = count;
            foreach (var l in spaceLayers) total += Mathf.Max(0, l.count);
            position = new Vector3[total];
            velocity = new Vector3[total];
            flash = new float[total];
            size = new float[total];
            layer = new int[total];
            particles = new ParticleSystem.Particle[total];
            int i = 0;
            for (; i < count; i++)
            {
                size[i] = carrierSize * Random.Range(0.8f, 1.2f);
                layer[i] = -1;
                position[i] = RandomPoint(radius - size[i] * 0.6f);
                velocity[i] = Random.onUnitSphere * thermalJitter;
            }
            for (int k = 0; k < spaceLayers.Length; k++)
                for (int n = 0; n < spaceLayers[k].count; n++, i++)
                {
                    size[i] = spaceLayers[k].size * Random.Range(0.8f, 1.25f);
                    layer[i] = k;
                    position[i] = SpawnInLayer(spaceLayers[k]);
                    velocity[i] = Random.onUnitSphere * thermalJitter * 0.7f;
                }
            var palette = UI.Theme;
            tint = palette ? palette.wattsonAccent : new Color(0.75f, 0.965f, 1f);
            if (carriers)
            {
                var main = carriers.main;
                if (main.maxParticles < total) main.maxParticles = total;
            }

            // Lattice ions: a hexagonal column of dull spheres along the conduit; only shown when latticeVisibility > 0.
            var grid = new System.Collections.Generic.List<ParticleSystem.Particle>();
            float ring = tunnelRadius * 0.55f, ionSize = tunnelRadius * 0.2f;
            for (float x = -length / 2 + 0.5f; x < length / 2 - 0.3f; x += 0.5f)
                for (int s = 0; s < 7; s++)
                {
                    float a = s * Mathf.PI / 3f + (Mathf.Round(x * 2) % 2 == 0 ? 0 : Mathf.PI / 6f); // alternate slices turn half a step
                    var p = s == 6 ? new Vector3(x, 0, 0) : new Vector3(x, Mathf.Cos(a) * ring, Mathf.Sin(a) * ring);
                    grid.Add(new ParticleSystem.Particle { position = p, startSize = ionSize, remainingLifetime = 1e6f, startLifetime = 1e6f });
                }
            ions = grid.ToArray();
            if (carriers && !carriers.isPlaying) carriers.Play();
            if (lattice && !lattice.isPlaying) lattice.Play();
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

        // Inside the conduit, within `limit` of the axis.
        Vector3 RandomPoint(float limit)
        {
            var disc = Random.insideUnitCircle * limit;
            return new Vector3(Random.Range(-length / 2, length / 2), disc.x, disc.y);
        }

        // A point in the layer's frustum slab: uniform in volume (so the far layers are naturally denser on screen),
        // across the frame and a little beyond, between the subtitle band and the frame top, above the floor, and
        // for the far layers not inside the conduit.
        Vector3 SpawnInLayer(SpaceLayer l)
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float depth = Mathf.Pow(Mathf.Lerp(l.near * l.near * l.near, l.far * l.far * l.far, Random.value), 1f / 3f);
                float x = Random.Range(-LateralMargin, LateralMargin), y = Random.Range(-(1f - 2f * subtitleBand) + 0.1f, 0.95f);
                var p = ViewPoint(x, y, depth);
                if (p.y < floorY + 0.3f) continue;
                if (l.near > 4f && Mathf.Abs(p.x) < length / 2 + 0.8f && new Vector2(p.y, p.z).magnitude < builtRadius + 0.5f) continue; // the conduit's own volume
                return p;
            }
            return ViewPoint(0, 0.6f, (l.near + l.far) * 0.5f);
        }

        // Frame coordinates (x, y in [-1, 1]) at a depth along the Wire Inside view, in local space.
        Vector3 ViewPoint(float x, float y, float depth) =>
            spaceEye + spaceForward * depth + spaceRight * (x * depth * tanHalf * Aspect) + spaceUp * (y * depth * tanHalf);

        void Update()
        {
            float dt = Time.deltaTime;
            radius = Mathf.MoveTowards(radius, Mathf.Clamp(tunnelRadius, builtRadius * minRadiusFraction, builtRadius), dt * 0.3f);
            frost = Mathf.MoveTowards(frost, frozen ? 1 : 0, dt * 2f);
            float drift = fieldOn && !frozen ? driftSpeed * (1 - 0.55f * impede) : 0;
            float jitter = thermalJitter * (frozen ? 0.3f : 1);
            float crossX = crossSection ? transform.InverseTransformPoint(crossSection.position).x : 0;
            var cam = Camera.main;
            Vector3 eye = cam ? transform.InverseTransformPoint(cam.transform.position) : Vector3.one * 1e4f;
            Color body = Color.Lerp(tint, FrostTint, frost);
            if (!avoid && !avoidSearched) { avoidSearched = true; var w = FindFirstObjectByType<WattsonController>(); if (w) avoid = w.transform; }
            Vector3 avoidView = Vector3.zero;
            bool avoiding = cam && avoid;
            if (avoiding) avoidView = cam.WorldToViewportPoint(avoid.position + Vector3.up * 0.1f);

            for (int i = 0; i < total; i++)
            {
                bool carrier = layer[i] < 0;
                // Thermal motion: a new random direction every so often, as if scattered. Never still.
                if (Random.value < dt * 5f) velocity[i] = Random.onUnitSphere * jitter * Random.Range(0.5f, 1f) * (carrier ? 1f : 0.7f);
                // Scene 12: bouncing off lattice ions, with a spark of a flash on each hit.
                if (carrier && impede > 0 && Random.value < dt * 6f * impede) { velocity[i] = -velocity[i] * 0.6f + Random.insideUnitSphere * jitter * 0.4f; flash[i] = Mathf.Max(flash[i], 0.6f); }
                Vector3 was = position[i];
                Vector3 p = was + (velocity[i] + Vector3.right * drift) * dt;
                float alpha = 1f;

                if (carrier)
                {
                    float limit = radius - size[i] * 0.6f;
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
                }
                else
                {
                    var l = spaceLayers[layer[i]];
                    // Keep the electron in its frustum slab: wrap sideways (off-screen), bounce in depth and height, stay above the floor.
                    Vector3 d = p - spaceEye;
                    float depth = Vector3.Dot(d, spaceForward), lat = Vector3.Dot(d, spaceRight), up = Vector3.Dot(d, spaceUp);
                    float halfWidth = LateralMargin * depth * tanHalf * Aspect;
                    if (lat > halfWidth) lat -= 2 * halfWidth; else if (lat < -halfWidth) lat += 2 * halfWidth;
                    if (depth > l.far || depth < l.near) { depth = Mathf.Clamp(depth, l.near, l.far); velocity[i] = Vector3.Reflect(velocity[i], spaceForward); }
                    float top = 0.95f * depth * tanHalf, bottom = -(1f - 2f * subtitleBand) * depth * tanHalf;
                    if (up > top || up < bottom) { up = Mathf.Clamp(up, bottom, top); velocity[i].y = -velocity[i].y; }
                    p = spaceEye + spaceForward * depth + spaceRight * lat + spaceUp * up;
                    if (p.y < floorY + 0.3f) { p.y = floorY + 0.3f; velocity[i].y = Mathf.Abs(velocity[i].y); }
                    if (cam)
                    {
                        var view = cam.WorldToViewportPoint(transform.TransformPoint(p));
                        if (view.z <= 0) alpha = 0;
                        else
                        {
                            alpha *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(subtitleBand, subtitleBand + 0.08f, view.y));   // the subtitle band
                            if (avoiding && view.z < avoidView.z + 0.5f)
                            {
                                float dist = new Vector2((view.x - avoidView.x) * Aspect, view.y - avoidView.y).magnitude;
                                alpha *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(avoidRadius, avoidRadius + 0.08f, dist));  // never over Wattson
                            }
                        }
                    }
                }
                position[i] = p;
                flash[i] = Mathf.Max(0, flash[i] - dt * 3f);

                var colour = Color.Lerp(body, Color.white, flash[i]);
                colour.a = alpha * Mathf.InverseLerp(nearFade, nearFade * 2f, Vector3.Distance(p, eye)); // never a dinner plate over the lens
                particles[i].position = p;
                particles[i].startSize = size[i] + 0.1f * flash[i];
                particles[i].startColor = colour;
                particles[i].remainingLifetime = particles[i].startLifetime = 1e6f;
            }
            if (carriers) carriers.SetParticles(particles, total);

            if (lattice && !Mathf.Approximately(shownLattice, latticeVisibility))
            {
                shownLattice = latticeVisibility;
                var ionColor = new Color(IonTint.r, IonTint.g, IonTint.b, latticeVisibility * 0.6f);
                for (int i = 0; i < ions.Length; i++) ions[i].startColor = ionColor;
                lattice.SetParticles(ions, latticeVisibility > 0 ? ions.Length : 0);
            }

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
                    counterRing.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * counterRingRadius);
                }
            }
        }
    }
}
