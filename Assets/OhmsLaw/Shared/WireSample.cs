using UnityEngine;

namespace OhmsLaw
{
    // Scene 12's 3D pop-up: two cut-away wire segments on a small stage far under the lab, copper and nichrome, each with
    // lattice ions and `count` free electrons drifting through it. Copper: few collisions, good headway. Nichrome: the
    // electrons scatter hard and drift slowly, and the collisions warm its lattice to a red glow. Its own camera renders the
    // stage to a texture the lesson panel shows as a centre card (LessonPanel.Popup3D) while this object is active ("show
    // Wire Sample" opens it, "hide" closes it); the pair sways gently side-on until a drag on the card turns it. Copper
    // alone sits in the middle of the view; the swap lifts it to make room for nichrome. The builder
    // (SceneBuilderBase.Sample.cs) lays the stage out; this spawns the electrons and moves them. Ohm's Law > Tests > Wire
    // Sample steps the motion without Play mode.
    public class WireSample : MonoBehaviour
    {
        public static WireSample Active { get; private set; }

        [Header("Stage")]
        public Transform turntable;             // both wires, turned by the drag
        public Transform copper, nichrome;      // the wire groups (glass, ions, label); the electrons spawn under them
        public Camera stageCamera;
        public string title = "Copper vs nichrome", caption = "same voltage · same length · same thickness";
        [Tooltip("Idle sway, degrees either side of side-on: the lattice reads side-on, never end-on.")] public float sway = 22f;

        [Header("Electrons")]
        public Material electronMaterial;
        public int count = 24;
        public float length = 2.4f, radius = 0.26f, electronSize = 0.07f, thermalSpeed = 0.9f;
        [Tooltip("Drift along the wire, m/s: copper, nichrome.")] public float copperDrift = 0.7f, nichromeDrift = 0.22f;
        [Tooltip("Scatterings per second: copper, nichrome. Above HardScatter the lattice throws the electron back.")] public float copperScatter = 2.5f, nichromeScatter = 12f;
        const float HardScatter = 6f, Relax = 0.25f;

        [Header("Heat")]
        [Tooltip("Seconds from the swap until nichrome's lattice glows fully (S12_R04: every collision hands it energy).")] public float heatSeconds = 16f;
        [ColorUsage(false, true)] public Color hotGlow = new Color(1.5f, 0.45f, 0.08f);
        static readonly Color HotIon = new Color(0.5f, 0.12f, 0.05f), HotGlass = new Color(1f, 0.38f, 0.12f, 0.4f);

        public RenderTexture Texture { get; private set; }

        class Strip
        {
            public Transform[] e;
            public Vector3[] home, off, vel;   // home: the place in the stream (drifts); off: the random motion around it
            public float[] flash;
            public float drift, scatter;
            public int hits;
        }

        Strip a, b;
        float yaw, pitch, swing, centre, heat;
        bool dragged;
        Transform[] labels = new Transform[0];
        Material hotIons, hotGlass;            // nichrome's own copies: the heat never touches the shared assets
        Color ionBase, ionGlow, glassBase;
        readonly System.Random random = new System.Random(12);

        public int CopperHits => a?.hits ?? 0;
        public int NichromeHits => b?.hits ?? 0;
        public Vector3[] Homes(bool copperWire) => (copperWire ? a : b)?.home;

        void Awake()
        {
            Init();
            if (!nichrome) return;
            foreach (var r in nichrome.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.sharedMaterial) continue;
                if (r.name == "Ion")
                {
                    if (!hotIons) { hotIons = new Material(r.sharedMaterial); ionBase = hotIons.GetColor("_BaseColor"); ionGlow = hotIons.GetColor("_EmissionColor"); }
                    r.sharedMaterial = hotIons;
                }
                else if (r.name == "Glass") { hotGlass = new Material(r.sharedMaterial); glassBase = hotGlass.GetColor("_BaseColor"); r.sharedMaterial = hotGlass; }
            }
        }

        // Spawns the electrons once (the editor check calls it outside Play mode, where Awake does not run).
        public void Init()
        {
            if (a != null) return;
            a = Make(copper, copperDrift, copperScatter);
            b = Make(nichrome, nichromeDrift, nichromeScatter);
            labels = new[] { copper ? copper.Find("Label") : null, nichrome ? nichrome.Find("Label") : null };
        }

        // How far the turntable sits below the stage's middle: copper's height while it is alone (centred), none for the pair.
        float Alone => (nichrome && nichrome.gameObject.activeSelf) || !copper ? 0f : copper.localPosition.y;

        Strip Make(Transform group, float drift, float scatter)
        {
            var s = new Strip { drift = drift, scatter = scatter, e = new Transform[count], home = new Vector3[count], off = new Vector3[count], vel = new Vector3[count], flash = new float[count] };
            float spacing = length / count;
            for (int i = 0; i < count; i++)
            {
                var disc = Vector2.ClampMagnitude(new Vector2(R(), R()), 1f) * (radius - electronSize);
                s.home[i] = new Vector3(-length / 2 + (i + 0.5f) * spacing, disc.x, disc.y);   // evenly spaced: no gaps, no clumps
                s.off[i] = Rand3() * 0.08f;
                s.vel[i] = Rand3().normalized * thermalSpeed;
                var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                e.name = "Electron";
                DestroyImmediate(e.GetComponent<Collider>());
                e.transform.SetParent(group, false);
                e.transform.localScale = Vector3.one * electronSize;
                e.layer = group ? group.gameObject.layer : gameObject.layer;
                if (electronMaterial) e.GetComponent<Renderer>().sharedMaterial = electronMaterial;
                s.e[i] = e.transform;
            }
            return s;
        }

        void OnEnable()
        {
            Active = this;
            if (!Texture) Texture = new RenderTexture(960, 540, 24) { name = "WireSample" };
            if (stageCamera) stageCamera.targetTexture = Texture;
            if (nichrome) nichrome.gameObject.SetActive(false);   // copper alone until the swap (SceneDirector.SwapWire)
            swing = heat = 0f;
            centre = Alone;
            var panel = LessonPanel.Instance;
            if (panel) { panel.Popup3D(Texture, title, caption); panel.Popup3DDrag = Turn; }
        }

        void OnDisable()
        {
            if (Active == this) Active = null;
            var panel = LessonPanel.Instance;
            if (panel) { panel.Popup3DDrag = null; panel.Popup3DClose(); }
        }

        void OnDestroy()
        {
            if (Texture) Texture.Release();
            if (hotIons) Destroy(hotIons);
            if (hotGlass) Destroy(hotGlass);
        }

        // The swap: nichrome pops in under the copper (its PopIn runs on enable).
        public void ShowNichrome(bool on) { if (nichrome) nichrome.gameObject.SetActive(on); }

        // A drag on the pop-up card: yaw with x, a little pitch with y.
        public void Turn(Vector2 delta)
        {
            dragged = true;
            yaw += delta.x * 0.35f;
            pitch = Mathf.Clamp(pitch - delta.y * 0.2f, -35f, 35f);
        }

        void Update()
        {
            float dt = ModuleFlow.UserPaused ? 0f : Time.deltaTime;
            if (!dragged && !ReducedMotion.On) yaw = sway * Mathf.Sin(swing += dt * 0.35f);   // a slow sway until the learner takes it
            // The turn pivots on the middle of what is shown; the swap eases copper up as nichrome pops in under it.
            centre = Mathf.Lerp(centre, Alone, 1f - Mathf.Exp(-5f * dt));
            if (turntable)
            {
                var turn = Quaternion.Euler(pitch, yaw, 0f);
                turntable.localRotation = turn;
                turntable.localPosition = turn * (Vector3.down * centre);
            }
            // The names face the camera as the pair turns (mirrored or edge-on they cannot be read).
            if (stageCamera) foreach (var label in labels) if (label) label.rotation = stageCamera.transform.rotation;
            // From the swap the collisions warm nichrome's lattice: faint through S12_R03, glowing red by the end of R04.
            heat = nichrome && nichrome.gameObject.activeSelf ? Mathf.MoveTowards(heat, 1f, dt / heatSeconds) : 0f;
            float glow = heat * heat * heat;
            if (hotIons) { hotIons.SetColor("_BaseColor", Color.Lerp(ionBase, HotIon, glow)); hotIons.SetColor("_EmissionColor", Color.Lerp(ionGlow, hotGlow, glow)); }
            if (hotGlass) hotGlass.SetColor("_BaseColor", Color.Lerp(glassBase, HotGlass, glow));
            Step(dt);
        }

        // One simulation step (the editor check drives it with a fixed dt).
        public void Step(float dt)
        {
            Move(a, dt);
            Move(b, dt);
        }

        void Move(Strip s, float dt)
        {
            if (s == null) return;
            bool calm = ReducedMotion.On;
            float half = length / 2, limit = radius - electronSize * 0.6f;
            for (int i = 0; i < count; i++)
            {
                if (random.NextDouble() < dt * s.scatter)
                {
                    // A collision with the lattice: a new direction; the hard way in nichrome (thrown back, a flash).
                    s.hits++;
                    bool hard = s.scatter >= HardScatter;
                    s.vel[i] = hard ? -s.vel[i] * 0.7f + Rand3() * thermalSpeed * 0.5f : Rand3().normalized * thermalSpeed;
                    s.flash[i] = hard ? 1f : 0.3f;
                }
                s.off[i] += (s.vel[i] - s.off[i] / Relax) * dt;
                s.home[i].x += s.drift * dt;
                if (s.home[i].x > half) s.home[i].x -= length;   // round the loop: in from the other end, no pile-up
                var p = s.home[i] + s.off[i];
                var r = new Vector2(p.y, p.z);
                if (r.sqrMagnitude > limit * limit)   // the random motion bounces off the wall
                {
                    r = r.normalized * limit;
                    p.y = r.x; p.z = r.y;
                    s.off[i].y = p.y - s.home[i].y; s.off[i].z = p.z - s.home[i].z;
                    var inward = -new Vector3(0, r.x, r.y).normalized;
                    if (Vector3.Dot(s.vel[i], inward) < 0) s.vel[i] = Vector3.Reflect(s.vel[i], inward);
                }
                if (p.x > half) p.x -= length; else if (p.x < -half) p.x += length;
                s.flash[i] = Mathf.Max(0f, s.flash[i] - dt * 4f);
                s.e[i].localPosition = p;
                s.e[i].localScale = Vector3.one * electronSize * (1f + (calm ? 0.2f : 0.7f) * s.flash[i]);
            }
        }

        float R() => (float)(random.NextDouble() * 2 - 1);
        Vector3 Rand3() => new Vector3(R(), R(), R());
    }
}
