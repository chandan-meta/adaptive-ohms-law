using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OhmsLaw.Editor
{
    // Stage kit: the electron space (Scenes 5, 6, 12). Inside a copper wire: a cavern of soft, warm copper "atoms" (one
    // static ParticleSystem, WireInterior places them) all round and beyond a translucent conduit (the inside of the wire,
    // r 0.6 m, 9.8 m along +X) slightly above eye level between two brushed-metal terminal blocks: − with a cyan-lit disc
    // at the −X end, + with a red-lit disc at the +X end, each labelled in words too; a dark glossy floor with a faint
    // grid (Wattson's shadow), a warm copper haze for fog and void, two warm copper lights. The free electrons are a small sample of glowing
    // "−" spheres inside the conduit only, two of them with a short trail. Wattson stands lower-left on the floor at
    // "Platform Point". Scene 12's nichrome variant tints the set grey-green with the ion lattice inside the conduit.
    public abstract partial class SceneBuilderBase
    {
        // ------------------------------------------------------------------ the set

        protected const float TunnelRadius = 0.6f, TunnelLength = 9.8f;                               // the conduit (names kept for callers)
        protected const float CrossSectionX = -1.2f;                                                   // the counting ring, left of centre so the counter clears both concept cards
        protected const float SpaceFloorY = -1.7f;                                                     // the floor, below the conduit axis
        protected const float WireFov = 68f;                                                           // "Wire Inside" vertical FOV (the panel crops the sides)
        const int ReflectedLayer = 4;                                                                  // built-in "Water" layer, unused elsewhere: what the floor's probe mirrors
        const int FloorLayer = 2;   // built-in "Ignore Raycast": the floor is outside its own probe's capture (there it mirrored the default sky: a grey floor) and the look-around/tap raycasts
        const uint FloorLight = 1u << 1;   // URP rendering layer "Light Layer 1", the floor's alone: only the key and the badge glows include it, so the copper lights and Wattson's fill skip the black glass (a light's culling mask does not exclude objects in URP)
        protected static readonly Vector3 WireOrigin = new Vector3(100, 0, 0);                        // far from the lab in Scene 12; the conduit axis passes through it
        protected static readonly Vector3 WireCamPos = WireOrigin + new Vector3(-2.6f, -0.7f, -8.7f);  // front-left, 1 m above the floor: − near-left, + far-right
        protected static readonly Vector3 WireCamLook = WireOrigin + new Vector3(-0.9f, -0.7f, -0.9f); // yaw 13°, level
        protected static readonly Vector3 PlatformPoint = WireOrigin + new Vector3(-3.2f, -1.25f, -5.7f);  // Wattson: 3.0 m from the lens, lower-left third of the free frame, hovering just over the floor
        protected static readonly Vector3 PlatformPos = PlatformPoint + new Vector3(0, -0.45f, 0);          // the floor under him (kept for callers)
        protected static readonly Vector3 WallPoint = WireOrigin + new Vector3(-1.6f, 0.2f, -TunnelRadius - 0.02f); // the wire's wall the Ohm-niscope hits (Scene 6): the conduit's near surface
        protected static readonly Vector3 WireWattsonCam = PlatformPoint + new Vector3(-0.35f, 0.15f, -2.25f), WireWattsonLook = PlatformPoint + new Vector3(1.3f, 0.7f, 1.3f);
        protected static readonly Vector3 MinusBlock = WireOrigin + new Vector3(-5.4f, 0f, 0f), PlusBlock = WireOrigin + new Vector3(5.4f, -0.35f, 0f);
        protected static readonly Color CopperTint = new Color(0.72f, 0.38f, 0.2f), NichromeTint = new Color(0.55f, 0.56f, 0.5f);
        protected static readonly Color SpaceBlue = new Color(0.043f, 0.086f, 0.188f);                 // #0B1630 fog, ambient, background (nichrome mix)
        // Inside the copper: fog, ambient and background are a warm copper-brown haze (gamma; under ACES it lands near #1B100E),
        // so the far atom rows dissolve into a visible warm haze. The old navy mix rendered as black: a starfield.
        static readonly Color CopperHaze = new Color(0.24f, 0.19f, 0.18f);
        protected static readonly Color ConduitBlue = new Color(0.31f, 0.82f, 1f);                     // #4FD1FF glass and grid
        protected static readonly Color ElectronTint = new Color(0.75f, 0.965f, 1f);                   // #BFF6FF
        protected static readonly Color MinusTint = ConduitBlue, PlusTint = new Color(1f, 0.27f, 0.21f); // terminal badges: cyan −, red +
        // Text in the space (Docs/Polish/TYPOGRAPHY.md targets, sized for the "Wire Inside" depth each card is read at).
        protected const float WireCardDepth = 4f, WireQuestionDepth = 6f, WireQuestionWidth = 6f;
        protected static float WireConceptCap => WorldText.CapForScreenPx(WorldText.Px.ConceptWord, WireCardDepth, WireFov);
        protected static float WireQuestionCap => WorldText.CapForScreenPx(WorldText.Px.DataCard, WireQuestionDepth, WireFov);

        // A point in the "Wire Inside" frame (16:9): x and y in [-1, 1] across the frame, 0,0 at its centre; depth along the view in metres.
        protected static Vector3 WireFramePoint(float x, float y, float depth)
        {
            Vector3 forward = (WireCamLook - WireCamPos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized, up = Vector3.Cross(forward, right);
            float tanHalfFov = Mathf.Tan(WireFov * 0.5f * Mathf.Deg2Rad);
            const float aspect = 16f / 9f;
            return WireCamPos + forward * depth + right * (x * depth * tanHalfFov * aspect) + up * (y * depth * tanHalfFov);
        }

        // Depth of a point along the "Wire Inside" view, and the cap that reads at `px` there.
        protected static float WireDepth(Vector3 world) => Vector3.Dot(world - WireCamPos, (WireCamLook - WireCamPos).normalized);
        protected static float WireCap(float px, Vector3 world) => WorldText.CapForScreenPx(px, WireDepth(world), WireFov);

        // Concept cards above the conduit: side -1 (VOLTAGE, upper-left, clear of the − badge) or +1 (CURRENT, upper-right,
        // under the HUD row), one depth so they read at one size.
        protected static Vector3 WireCardPosition(int side, float depth = WireCardDepth) => WireFramePoint(side < 0 ? -0.4f : 0.5f, side < 0 ? 0.58f : 0.44f, depth);
        protected static Vector3 WireQuestionPos => WireFramePoint(0, 0.56f, WireQuestionDepth);     // "WHAT MAKES CHARGE FLOW?", above the conduit
        protected static Vector3 WireResistancePos => WireFramePoint(0.08f, -0.1f, 4.5f);            // "RESISTANCE" (Scene 12), under the conduit right of Wattson; "Symbol: R" goes 0.5 m under it
        protected static Vector3 WireReadoutPos => WireFramePoint(0.32f, -0.3f, 4.5f);               // Scene 12's bench readings: under the conduit, right of Wattson, above the subtitles

        // terminalsOn: the badges and their labels start lit (Scenes 6 and 12); Scene 5 shows them with its "terminals" beat.
        protected WireInterior WireStage(Color tint, bool lattice, bool active = true, bool terminalsOn = false)
        {
            string hex = ColorUtility.ToHtmlStringRGB(tint);
            bool nichrome = lattice; // Scene 12's wire: nichrome grey-green tints the glass, the void, the grid and the atoms
            Color glass = Color.Lerp(ConduitBlue, tint, nichrome ? 0.85f : 0f);
            glass.a = nichrome ? 0.22f : 0.12f;
            Color space = nichrome ? Color.Lerp(SpaceBlue, tint * 0.45f, 0.7f) : CopperHaze; // the metal's own dark
            Color grid = Color.Lerp(ConduitBlue, tint, nichrome ? 0.85f : 0f);
            Color atom = Color.Lerp(CopperTint, tint, nichrome ? 0.85f : 0f);

            var section = Node("Wire Section", null, WireOrigin);
            var wire = section.AddComponent<WireInterior>();
            wire.conduitColor = glass;
            wire.latticeVisibility = lattice ? 1 : 0;
            wire.tunnelRadius = TunnelRadius;
            wire.length = TunnelLength;
            wire.count = 49; // 5 per metre: a sample small enough to follow one electron; the counter reads whole numbers

            // Atmosphere and lights (the lighting node joins the section so Scene 12 shows and hides it with the dive; the
            // RenderSettings live on WireInterior, applied in OnEnable and restored in OnDisable). A scene that opens in
            // the lab keeps the lab's atmosphere in its file.
            var was = (fog: RenderSettings.fog, mode: RenderSettings.fogMode, colour: RenderSettings.fogColor, density: RenderSettings.fogDensity,
                ambientMode: RenderSettings.ambientMode, sky: RenderSettings.ambientSkyColor, flat: RenderSettings.ambientLight, background: camera.backgroundColor);
            WireLights(section.transform, space, atom, nichrome);
            wire.fogColor = RenderSettings.fogColor;
            wire.fogDensity = RenderSettings.fogDensity;
            wire.ambient = RenderSettings.ambientLight;
            wire.background = camera.backgroundColor;
            if (!active)
            {
                RenderSettings.fog = was.fog; RenderSettings.fogMode = was.mode; RenderSettings.fogColor = was.colour; RenderSettings.fogDensity = was.density;
                RenderSettings.ambientMode = was.ambientMode; RenderSettings.ambientSkyColor = was.sky; RenderSettings.ambientLight = was.flat; camera.backgroundColor = was.background;
            }

            // The static set (conduit, terminals, cables) lives on its own layer: the floor's reflection probe captures only
            // this, so nothing that moves (Wattson, electrons, rings) leaves a stale mirror image; the floor itself is on
            // FloorLayer, so the probe never captures the floor mirroring the default sky (a grey floor in the mirror).
            var set = Node("Set", section.transform);
            var steel = Lit("Wire_Steel", new Color(0.36f, 0.42f, 0.52f), 0.9f, 0.5f);
            var trim = Lit("Wire_SteelDark", new Color(0.14f, 0.17f, 0.23f), 0.8f, 0.45f);
            WireFloor(set.transform, space, grid, nichrome);
            WireCavern(wire, section.transform, atom, nichrome);

            // The conduit: an open glass tube (premultiplied alpha keeps its highlights), two steel bands; WireInterior
            // scales the tube and every ring to the current radius (Scene 12 narrows it by at most 20 %).
            var conduit = WireMesh("Conduit", set.transform, BlockoutAssets.TunnelMesh(), WireGlass("Wire_Conduit_" + hex, glass));
            conduit.transform.localScale = new Vector3(TunnelLength, TunnelRadius, TunnelRadius);
            wire.tunnel = conduit.GetComponent<Renderer>();
            var bandMesh = Torus("WireBand", 0.035f);
            wire.bands = new[] { -3.6f, 2.0f }.Select(x => WireRing("Band", set.transform, new Vector3(x, 0, 0), 1.04f, bandMesh, steel)).ToArray();

            // Terminals: − near-left with its disc on the front face at the conduit's height, + far-right with its disc on the
            // face the conduit enters, below it. The − disc is no bigger than it must be: at 0.72 m it read as a giant electron.
            // One style for both polarity markings: the same badge size, each on the face toward the lens at the
            // conduit's height, cyan − and red + (the glyph says which, so colour is never the only cue).
            var minus = WireTerminal(set.transform, "−", MinusBlock - WireOrigin, new Vector3(1.8f, 3.4f, 1.8f),
                new Vector3(0, 0, -0.9f), Vector3.back, 0.42f, MinusTint, 1.4f, steel, trim);
            var plus = WireTerminal(set.transform, "+", PlusBlock - WireOrigin, new Vector3(1.6f, 3.4f, 1.6f),
                new Vector3(0, 0.35f, -0.8f), Vector3.back, 0.42f, PlusTint, 1.4f, steel, trim);
            minus.SetActive(terminalsOn);
            plus.SetActive(terminalsOn);
            var cableDark = Lit("Wire_CableDark", new Color(0.05f, 0.06f, 0.09f), 0.1f, 0.7f);
            var cableRed = Lit("Wire_CableRed", new Color(0.75f, 0.08f, 0.06f), 0.1f, 0.55f, new Color(0.12f, 0.01f, 0f)); // a dim glow: it was the brightest thing on the right third
            Vector3 m = MinusBlock - WireOrigin, p = PlusBlock - WireOrigin;
            Polyline("Cable", set.transform, cableDark, 0.1f, false, false, m + new Vector3(0.2f, 1.78f, 0.4f), m + new Vector3(-0.6f, 2.25f, 0.5f), m + new Vector3(-2.0f, 2.45f, 0.9f), m + new Vector3(-3.6f, 2.0f, 1.6f), m + new Vector3(-4.6f, 0.9f, 2.4f));
            Polyline("Cable", set.transform, cableRed, 0.1f, false, false, p + new Vector3(0.5f, 1.42f, 0.3f), p + new Vector3(1.3f, 1.75f, 0.1f), p + new Vector3(2.8f, 1.6f, -0.4f), p + new Vector3(4.4f, 0.9f, -1.2f), p + new Vector3(6.2f, -2.4f, -2.4f)); // plugs into the floor (it ended in mid-air)
            foreach (var t in set.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ReflectedLayer;
            var floorPlane = set.transform.Find("Floor");
            floorPlane.gameObject.layer = FloorLayer;
            floorPlane.GetComponent<Renderer>().renderingLayerMask = FloorLight;

            // Each terminal is named in words as well as by its sign and colour: "− negative" under the − disc on the block's
            // front; "+ positive" on the + block's front beside the conduit's entry (its disc sits low on the side face,
            // just over the floor), above Scene 6's gauge and right of the CURRENT card. Dark backings keep them readable.
            // Labels sit well in front of the blocks: Face() tilts them toward the lens, and at the old depth the tilted plane's
            // left end dipped behind the block face, which swallowed the glyph after the sign ("egative", "ositive").
            WireTerminalLabel(section.transform, "−", "− negative", MinusTint, MinusBlock + new Vector3(0, -0.85f, -1.3f), terminalsOn);
            WireTerminalLabel(section.transform, "+", "+ positive", PlusTint, PlusBlock + new Vector3(0, -0.5f, -1.2f), terminalsOn);

            // Electrons: the carriers, two of them traced; the lattice ions apart, drawn first so the electrons stay on top.
            wire.carriers = Particles("Carriers", section.transform, BlockoutAssets.ChargeMaterial(ElectronTint), 64);
            wire.lattice = Particles("Lattice Ions", section.transform, BlockoutAssets.ChargeMaterial(Color.white, ion: true), 200);
            wire.lattice.GetComponent<ParticleSystemRenderer>().sortingFudge = 5f;
            Ref(wire.lattice.gameObject);
            wire.trails = WireTrails(section.transform, 2);

            // Cross-section: a thin glowing ring around the conduit; counted electrons flash as they pass through it.
            var cross = WireRing("Cross Section", section.transform, new Vector3(CrossSectionX, 0, 0), 1.1f, Torus("WireCrossRing", 0.014f), Glow("Glow_WireCrossRing", Hdr(ElectronTint, 3f), softTexture: false));
            cross.gameObject.SetActive(false);
            wire.crossSection = Ref(cross.gameObject).transform;

            WireCounter(wire, section.transform);

            Face(Marker("Concept Left", WireCardPosition(-1)));
            Face(Marker("Concept Right", WireCardPosition(+1)));
            Marker("Platform Point", PlatformPoint);
            Marker("Wall Point", WallPoint);
            Marker("Electrons Point", WireOrigin + new Vector3(-1.8f, 0f, -TunnelRadius)); // the near side of the stream, up and right of Wattson
            Marker("Wire Focus Point", PlatformPoint + new Vector3(0.3f, 0.2f, 0));
            Vcam("Wire Inside", WireCamPos, WireCamLook, WireFov, refs["Platform Point"].transform, lensMedium);
            Vcam("Wire Wattson", WireWattsonCam, WireWattsonLook, 44, refs["Platform Point"].transform, lensMedium);
            // Both terminals with their labels and the whole conduit inside the free frame (the dock takes the right 25 %,
            // so the authored shot keeps them within |x| ≤ 0.65 of its width); Wattson lower-left in front of the − block.
            Vcam("Wire Terminals", WireOrigin + new Vector3(0.2f, 0.2f, -12.2f), WireOrigin + new Vector3(0f, -0.35f, 0f), 54f, section.transform, lensMedium);

            // The effects the scene cues name, in place and hidden; a builder may re-place one with WireFx(name, position).
            foreach (var name in new[] { "letters assemble from charges", "field wave sweeps whole tunnel", "time-freeze (Ohm-niscope holds the carriers)",
                "Ohm-niscope flash", "carriers impeded by lattice (slow, bounce)", "RESISTANCE word forms from the lattice" })
                WireFx(name, WireFxDefault(name), section.transform);

            var probe = section.GetComponentInChildren<ReflectionProbe>(true);
            if (probe) BakeProbe(probe, "Wire");
            section.SetActive(active);
            Ref(section);
            return wire;
        }

        // Wire cards stand parallel to the "Wire Inside" screen (aimed at the lens, an off-centre card was keystoned ~8°);
        // with `from`, the card turns toward that eye instead.
        protected void Face(GameObject card, Vector3? from = null) =>
            card.transform.rotation = from.HasValue ? Quaternion.LookRotation(card.transform.position - from.Value) : Quaternion.LookRotation(WireCamLook - WireCamPos);

        // "Card Terminal Label <sign>": the terminal's name, with its sign, on a dark backing, in the section (it shows and
        // hides with the electron space); shown with its badge.
        void WireTerminalLabel(Transform section, string sign, string text, Color tint, Vector3 position, bool shown)
        {
            var label = Card("Terminal Label " + sign, text, Color.Lerp(tint, Color.white, 0.35f), section, position - WireOrigin, WireCap(WorldText.Px.Medium, position), WorldText.Look.Panel, shown, sparks: false);
            Face(label);
        }

        // The tracer electrons' trails: thin, fading toward the tail, in the section's space (WireInterior feeds the points).
        LineRenderer[] WireTrails(Transform section, int count)
        {
            var material = Glow("Glow_WireTrail", Hdr(ElectronTint, 1.6f), softTexture: false);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0f, 0), new GradientAlphaKey(0.7f, 1) });
            var lines = new LineRenderer[count];
            for (int k = 0; k < count; k++)
            {
                var line = Polyline("Trail " + (k + 1), section, material, 0.06f, false, false);
                line.widthCurve = AnimationCurve.Linear(0, 0.15f, 1, 1f);
                line.colorGradient = gradient;
                line.numCapVertices = 0;
                lines[k] = line;
            }
            return lines;
        }

        // Scene 12: the bench's live readings beside the stream ("Wire Readout"), V in voltage blue and I in current
        // amber over "bench meters", on a dark panel; WireInterior writes the rig's values. Starts hidden.
        protected GameObject WireReadout(WireInterior wire, CircuitRig rig)
        {
            Vector3 at = WireReadoutPos;
            float cap = WireCap(WorldText.Px.Medium, at), small = WireCap(WorldText.Px.Small, at);
            float advance = WorldText.LineAdvance(palette.font, cap, WorldText.ProseLineSpacing);
            var root = Node("Wire Readout", refs["Wire Section"].transform, at - WireOrigin); // shows and hides with the section
            Face(root);
            wire.rig = rig;
            wire.readoutVolts = WorldText.Card("Volts", "V = 0.0 V", palette.voltage, root.transform, new Vector3(0, advance * 0.5f, 0), cap);
            wire.readoutAmps = WorldText.Card("Amps", "I = 0.00 A", palette.current, root.transform, new Vector3(0, -advance * 0.5f, 0), cap);
            var caption = WorldText.Card("Caption", "bench meters", Color.white, root.transform, new Vector3(0, -advance * 0.5f - cap * 0.6f - small * 1.4f, 0), small);
            foreach (var text in new[] { wire.readoutVolts, wire.readoutAmps, caption }) text.sortingOrder = 1;
            float width = Mathf.Max(wire.readoutAmps.textBounds.size.x, wire.readoutVolts.textBounds.size.x) + cap * 1.6f;
            float top = advance * 0.5f + cap * 0.9f, bottom = -advance * 0.5f - cap * 0.6f - small * 2.2f;
            WirePanel(root.transform, new Vector3(0, (top + bottom) / 2, 0.02f), new Vector2(width, top - bottom), cap);
            root.SetActive(false);
            return Ref(root);
        }

        // Scene 5, after the drift question: one faint arrow just under the conduit, pointing from the − end toward the +
        // end, captioned "electron drift" (electrons, never conventional current). "Drift Arrow"; starts hidden.
        protected GameObject WireDriftArrow()
        {
            float y = -TunnelRadius - 0.32f, z = -0.25f;
            var root = Node("Drift Arrow", null, WireOrigin + new Vector3(-0.2f, y, z));
            var glow = Glow("Glow_WireDriftArrow", Hdr(ElectronTint, 1.3f), softTexture: false);
            Vector3 tail = new Vector3(-2.2f, 0, 0), tip = new Vector3(2.2f, 0, 0);
            Polyline("Shaft", root.transform, glow, 0.035f, false, false, tail, tip);
            Polyline("Head", root.transform, glow, 0.035f, false, false, tip + new Vector3(-0.28f, 0.17f, 0), tip, tip + new Vector3(-0.28f, -0.17f, 0));
            Vector3 labelAt = root.transform.position + new Vector3(0, -0.3f, 0);
            var label = WorldText.Card("Label", "electron drift", Color.Lerp(ElectronTint, Color.white, 0.4f), root.transform, new Vector3(0, -0.3f, 0), WireCap(WorldText.Px.Small, labelAt));
            label.transform.rotation = Quaternion.LookRotation(WireCamLook - WireCamPos);
            root.SetActive(false);
            return Ref(root);
        }

        // Static particle system whose particles a script places (carriers, lattice ions).
        protected ParticleSystem Particles(string name, Transform parent, Material material, int max)
        {
            var ps = Node(name, parent).AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = max;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 1e6f;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortMode = ParticleSystemSortMode.Distance; // alpha-blended sprites
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return ps;
        }

        // Scene 5: two points on the conduit light up, with a dashed bracket between them just under the conduit and a tick
        // up to each point (the potential difference between them). Voltage blue at a glow that keeps its hue under the
        // bloom; the line sits below the stream so the electrons never hide it. Registered as `name` with "Point A" /
        // "Point B" children; starts hidden. The defaults sit on the near surface right of the cross-section, clear of
        // Wattson and of both concept cards.
        protected GameObject WireBracket(Vector3? a = null, Vector3? b = null, string name = "Bracket")
        {
            float z = -TunnelRadius - 0.04f, below = -TunnelRadius - 0.15f;
            Vector3 pa = a ?? WireOrigin + new Vector3(0.6f, 0, z), pb = b ?? WireOrigin + new Vector3(3.6f, 0, z);
            var root = Node(name, null, (pa + pb) / 2);
            var glow = Glow("Glow_WireBracket", Hdr(palette.voltage, 1.3f), softTexture: false);
            var halo = Glow("Glow_WireBracketHalo", new Color(palette.voltage.r, palette.voltage.g, palette.voltage.b, 0.5f));
            Vector3 Local(Vector3 world) => world - root.transform.position;
            Vector3 Under(Vector3 world) => Local(new Vector3(world.x, WireOrigin.y + below, world.z));
            foreach (var (label, pt) in new[] { ("Point A", pa), ("Point B", pb) })
            {
                var point = Ref(Prim(PrimitiveType.Sphere, label, root.transform, Local(pt), Vector3.one * 0.22f, glow));
                Face(Prim(PrimitiveType.Quad, "Halo", point.transform, Vector3.zero, Vector3.one * 3f, halo));
                Polyline("Tick", root.transform, glow, 0.05f, false, false, Local(pt), Under(pt));
            }
            int dashes = Mathf.Max(2, Mathf.RoundToInt((pb - pa).magnitude / 0.25f));
            for (int i = 0; i < dashes; i++)
                Polyline("Dash", root.transform, glow, 0.05f, false, false, Under(Vector3.Lerp(pa, pb, (i + 0.15f) / dashes)), Under(Vector3.Lerp(pa, pb, (i + 0.6f) / dashes)));
            root.SetActive(false);
            return Ref(root);
        }

        // Real effect for a "[VFX] <name>" cue: plays when the object is enabled, so the scene's show/hide cues drive it.
        // Known names get their designed effect, anything else a spark burst. Position is local to `parent` (world when
        // none; the set's own default when omitted). The field wave always starts at the − end and sweeps the whole conduit.
        // Kept restrained (one effect at a time, few sparks), and calmer still with reduced motion (ReducedMotionFx).
        protected GameObject WireFx(string name, Vector3? position = null, Transform parent = null)
        {
            string full = "[VFX] " + name;
            if (refs.TryGetValue(full, out var old) && old) Object.DestroyImmediate(old); // replaces a magenta tag or an earlier placement
            Vector3 at = position ?? (parent ? Vector3.zero : WireOrigin + WireFxDefault(name));
            if (name.StartsWith("field wave")) at = (parent ? Vector3.zero : WireOrigin) + new Vector3(-TunnelLength / 2, 0, 0);
            var go = Node(full, parent, at);
            Transform t = go.transform;
            Vector3 centre = (parent ? Vector3.zero : WireOrigin) - at;   // the conduit's middle, relative to this effect
            Color charge = ElectronTint, accent = palette.wattsonAccent;
            var spark = Glow("Glow_WireSpark", Hdr(charge, 3.5f));
            var glyph = BlockoutAssets.ChargeMaterial(charge);

            if (name.StartsWith("race into wire"))
            {
                // Scene 4's dive streaks: they stream out of the wire surface past the lens. Forward = toward the camera. Scene 4's
                // colour and glow (its "Dive Streaks"), so the cut matches; thin and under 2x so they stay streaks under the bloom
                // (at 5x and 1-3 cm wide they bloomed into white bands over most of the frame).
                t.rotation = Quaternion.LookRotation(Vector3.back);
                Emitter("Streaks", t, Vector3.zero, Glow("Glow_WireStreaks", Hdr(Color.Lerp(accent, palette.current, 0.6f), 1.6f)), ps =>
                {
                    var main = ps.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.014f);
                    main.maxParticles = 600;
                    var emission = ps.emission;
                    emission.rateOverTime = 320;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 24;
                    shape.radius = 0.03f;
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode = ParticleSystemRenderMode.Stretch;
                    renderer.velocityScale = 0.12f;
                    renderer.lengthScale = 2f;
                });
            }
            else if (name.StartsWith("letters assemble"))
            {
                // Electrons gather onto the question card's plane (faced to the camera, so the box is wide in X).
                Face(go);
                WireGather(t, glyph, new Vector3(WireQuestionWidth, 1.4f, 0.6f), new ParticleSystem.MinMaxCurve(0.08f, 0.16f), 70);
            }
            else if (name.StartsWith("field wave"))
            {
                // ONE faint ring of light sweeps − → + along the whole conduit in 0.4 s while a light shimmer covers the whole
                // volume at once: the field is set up everywhere together, nothing is pushed from one end.
                Emitter("Ring", t, Vector3.zero, Glow("Glow_WireWave", Hdr(charge, 2.2f), softTexture: false), ps =>
                {
                    ps.transform.localRotation = Quaternion.Euler(0, 90, 0); // emits along +X; the torus lies across the conduit
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 0.4f;
                    main.startLifetime = 0.4f;
                    main.startSpeed = TunnelLength / 0.4f;
                    main.startSize = TunnelRadius * 1.25f;
                    main.maxParticles = 4;
                    var emission = ps.emission;
                    emission.rateOverTime = 0;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                    var shape = ps.shape;
                    shape.enabled = false;
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode = ParticleSystemRenderMode.Mesh;
                    renderer.mesh = Torus("WireWaveRing", 0.04f);
                    renderer.alignment = ParticleSystemRenderSpace.Local;
                });
                Emitter("Shimmer", t, new Vector3(TunnelLength / 2, 0, 0), spark, ps =>
                {
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 0.5f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                    main.startSpeed = 0;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                    main.maxParticles = 80;
                    var emission = ps.emission;
                    emission.rateOverTime = 0;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(TunnelLength, TunnelRadius * 1.9f, TunnelRadius * 1.9f);
                });
            }
            else if (name.StartsWith("time-freeze"))
            {
                // A cool pulse spreads from the Ohm-niscope, then frost sparkles hang around the conduit for as long as it is held.
                var frost = Glow("Glow_WireFrost", Hdr(new Color(0.75f, 0.9f, 1f), 3.5f));
                Emitter("Pulse", t, Vector3.zero, frost, ps =>
                {
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 1f;
                    main.startLifetime = 0.9f;
                    main.startSpeed = 0;
                    main.startSize = 2.4f;
                    main.startColor = new Color(1, 1, 1, 0.2f); // a wash of cold light, not a white-out (Scene 6 plays it in the two-shot, where it fills the frame)
                    main.maxParticles = 2;
                    var emission = ps.emission;
                    emission.rateOverTime = 0;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                    var shape = ps.shape;
                    shape.enabled = false;
                    var size = ps.sizeOverLifetime;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.05f, 1, 1));
                });
                Emitter("Sparkle", t, centre, frost, ps =>
                {
                    var main = ps.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
                    main.startSpeed = 0.05f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
                    main.maxParticles = 80;
                    var emission = ps.emission;
                    emission.rateOverTime = 25;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(TunnelLength, TunnelRadius * 2.8f, TunnelRadius * 2.8f);
                });
            }
            else if (name.StartsWith("Ohm-niscope flash"))
            {
                var flash = Glow("Glow_WireFlash", Hdr(accent, 6f));
                Emitter("Flash", t, Vector3.zero, flash, ps =>
                {
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 0.6f;
                    main.startLifetime = 0.5f;
                    main.startSpeed = 0;
                    main.startSize = 2.2f;
                    main.maxParticles = 2;
                    var emission = ps.emission;
                    emission.rateOverTime = 0;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                    var shape = ps.shape;
                    shape.enabled = false;
                    var size = ps.sizeOverLifetime;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0.2f));
                });
                WireBurst(t, "Sparks", flash, 40, 0f, 0.15f, 1.5f, 4.5f);
            }
            else if (name.StartsWith("carriers impeded"))
            {
                // Collision sparks all through the conduit while the carriers bounce (the carriers themselves flash in WireInterior).
                Emitter("Collisions", t, centre, Glow("Glow_WireCollide", Hdr(palette.resistance, 3.5f)), ps =>
                {
                    var main = ps.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                    main.maxParticles = 80;
                    var emission = ps.emission;
                    emission.rateOverTime = 45;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(TunnelLength * 0.95f, TunnelRadius * 1.6f, TunnelRadius * 1.6f);
                });
            }
            else if (name.StartsWith("RESISTANCE word"))
            {
                Face(go);
                var coral = Glow("Glow_WireCollide", Hdr(palette.resistance, 3.5f));
                WireGather(t, coral, new Vector3(3.2f, 0.9f, 0.6f), new ParticleSystem.MinMaxCurve(0.05f, 0.12f), 100);
                WireBurst(t, "Sparks", coral, 30, 1.2f, 0.3f);
            }
            else WireBurst(t, "Burst", spark, 60, 0f, 0.3f);

            go.AddComponent<ReducedMotionFx>();
            go.SetActive(false);
            return Ref(go);
        }

        // Where each named effect belongs, relative to WireOrigin.
        protected static Vector3 WireFxDefault(string name)
        {
            if (name.StartsWith("letters assemble")) return WireQuestionPos - WireOrigin;
            if (name.StartsWith("field wave")) return new Vector3(-TunnelLength / 2, 0, 0);
            if (name.StartsWith("time-freeze")) return PlatformPoint - WireOrigin + new Vector3(-0.25f, 0.3f, -0.1f); // the Ohm-niscope in his right hand (he faces the camera, so his right is −X)
            if (name.StartsWith("Ohm-niscope flash")) return WallPoint - WireOrigin;
            if (name.StartsWith("RESISTANCE word")) return WireResistancePos - WireOrigin;
            return Vector3.zero;
        }

        // Particles drift inward from a box onto its centre for 2.4 s: "the word forms from them".
        void WireGather(Transform parent, Material material, Vector3 box, ParticleSystem.MinMaxCurve size, float rate) =>
            Emitter("Gather", parent, Vector3.zero, material, ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = 2.4f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
                main.startSpeed = 0;
                main.startSize = size;
                main.maxParticles = 400;
                var emission = ps.emission;
                emission.rateOverTime = rate;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = box;
                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.radial = -1.6f;
            });

        // Scene 4's spark burst: `count` sparks from a small sphere at `delay`.
        void WireBurst(Transform parent, string name, Material material, int count, float delay, float radius, float minSpeed = 0.6f, float maxSpeed = 2.2f) =>
            Emitter(name, parent, Vector3.zero, material, ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = delay + 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
                main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.05f);
                main.maxParticles = count + 10;
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(delay, (short)count) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = radius;
            });

        // ------------------------------------------------------------------ the set's pieces

        // Inside the metal: fog, flat ambient and the background in the warm copper haze, so the far atoms dissolve into it;
        // a soft cool directional key from the front-left (soft shadows: Wattson's contact shadow on the floor, the highlight
        // along the glass; warm grey in nichrome), two warm copper point lights behind the conduit (the cavern's own light on
        // the blocks, not on the floor: there it put warm smears and a hot spot under the captions; the floor's own rendering
        // layer keeps it off), a warm fill on Wattson only, the global ACES/bloom volume (bloom 1.0, light grain). The terminal
        // glow lights live in the badges so they switch on with them; they and the key do light the floor.
        void WireLights(Transform section, Color space, Color atom, bool nichrome)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = space * 1.5f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = space;
            RenderSettings.fogDensity = 0.045f; // the far rows (20-30 m) dissolve into the haze; the terminals (8-13 m) keep 70-85 %
            camera.backgroundColor = space;
            var root = Node("Lighting", section).transform;
            var key = Node("Space Key", root).AddComponent<Light>();
            key.type = LightType.Directional;
            key.transform.rotation = Quaternion.Euler(50, -20, 0);
            key.color = nichrome ? new Color(0.86f, 0.85f, 0.78f) : new Color(0.72f, 0.82f, 1f);
            key.intensity = nichrome ? 0.3f : 0.4f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.75f;
            key.GetUniversalAdditionalLightData().renderingLayers = 1u | FloorLight; // Default and the floor: Wattson's shadow on the glass
            foreach (var (name, at, intensity) in new[] { ("Copper Light L", new Vector3(-7f, 4.5f, 7f), 7f), ("Copper Light R", new Vector3(8f, 3f, 10f), 5f) })
            {
                var copper = Node(name, root, at).AddComponent<Light>();
                copper.type = LightType.Point;
                copper.color = Color.Lerp(new Color(1f, 0.62f, 0.35f), atom, nichrome ? 0.8f : 0f);
                copper.intensity = intensity;
                copper.range = 22f;
                copper.shadows = LightShadows.None;
            }
            var warm = Node("Platform Light", root, PlatformPoint - WireOrigin + new Vector3(0.2f, 0.8f, -0.4f)).AddComponent<Light>();
            warm.type = LightType.Point;
            warm.color = new Color(1f, 0.8f, 0.55f);
            warm.intensity = 1.4f;
            warm.range = 4f;
            warm.shadows = LightShadows.None;
            warm.cullingMask = ~(1 << ReflectedLayer); // Wattson, not the glossy floor (the floor's rendering layer keeps it off there)
            GlobalPost(root, "Electron_Global", bloom: 1.0f, vignette: 0.3f, exposure: 0.1f);
            Profile("Electron_Global", p => // less grain: it crawled over the dark void and the glows
            {
                var grain = Override<FilmGrain>(p);
                grain.type.Override(FilmGrainLookup.Thin1);
                grain.intensity.Override(0.1f);
            });
            foreach (var volume in root.GetComponentsInChildren<Volume>(true)) volume.priority = 1; // beats a lab volume while the section is active
        }

        // Glossy dark floor with a faint emissive 1 m grid, and a reflection probe (box-projected, baked by the builder) so
        // the floor mirrors the conduit and the terminals on the haze.
        void WireFloor(Transform section, Color space, Color grid, bool nichrome)
        {
            var texture = WireGridTexture();
            var floor = Mat("Wire_Floor" + (nichrome ? "_Nichrome" : ""), "Universal Render Pipeline/Lit", mat =>
            {
                mat.SetColor("_BaseColor", Color.Lerp(new Color(0.012f, 0.02f, 0.045f), space, 0.15f)); // black glass
                mat.SetFloat("_Metallic", 0.15f);
                mat.SetFloat("_Smoothness", 0.8f);
                mat.SetTextureScale("_BaseMap", new Vector2(44, 44)); // the 44 m plane: one grid cell per metre
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", texture);
                mat.SetColor("_EmissionColor", grid * 0.04f); // the seams, faint: not a Tron floor under the action
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
            Prim(PrimitiveType.Plane, "Floor", section, new Vector3(0, SpaceFloorY, 2f), new Vector3(4.4f, 1, 4.4f), floor);

            var probe = Node("Reflection Probe", section, new Vector3(0, 0.6f, 0)).AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.resolution = 256;
            probe.hdr = true;
            probe.boxProjection = true;
            probe.size = new Vector3(48, 14, 48);
            probe.center = new Vector3(0, SpaceFloorY + 7f - 1.5f - 0.6f, 2f); // the box reaches 1.5 m under the floor: with its bottom on the floor, the 1 m blend edge handed the floor to the default sky (a grey-blue plane)
            probe.cullingMask = 1 << ReflectedLayer; // the static set only
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = space;
            probe.nearClipPlane = 0.3f;
            probe.farClipPlane = 60f;
            probe.intensity = 0.9f;
        }

        // The copper lattice cavern: one static ParticleSystem of soft warm "atoms" (WireInterior.Cavern places them at
        // Awake: a lattice with a small jitter, its rows visible, all round and beyond the conduit; none inside it, over the lens
        // or on the stage in front of the set; denser far away), additive so they bloom a little and the haze swallows the
        // far rows. Not on the reflected layer: the floor's probe mirrors the set, and a mirrored atom field under Wattson
        // read as a second floor.
        void WireCavern(WireInterior wire, Transform section, Color atom, bool nichrome)
        {
            wire.cavern = Particles("Copper Lattice", section, Glow("Glow_WireAtom" + (nichrome ? "_Nichrome" : ""), Hdr(Color.white, nichrome ? 0.9f : 1.25f)), 3000);
            wire.cavern.GetComponent<ParticleSystemRenderer>().sortMode = ParticleSystemSortMode.None; // additive: order is irrelevant
            wire.cavernTint = atom;
            wire.cavernEye = WireCamPos - WireOrigin;
        }

        // A terminal: brushed-steel block standing on the floor with a dark cap and a glowing edge line, and its badge: a
        // steel puck on `face` (local centre on the block's surface, `normal` outward) with a saturated lit disc, a bright
        // white sign, a soft halo behind the disc and the glow light. The badge is the GameObject "Card Terminal <sign>" so the scenes' show/boost cues and the
        // point gesture work on it; it starts hidden unless WireStage lights it.
        GameObject WireTerminal(Transform section, string sign, Vector3 centre, Vector3 size, Vector3 face, Vector3 normal, float radius, Color tint, float light, Material steel, Material trim)
        {
            string hex = ColorUtility.ToHtmlStringRGB(tint);
            var block = Node("Terminal " + sign, section, centre);
            Prim(PrimitiveType.Cube, "Block", block.transform, Vector3.zero, size, steel);
            Prim(PrimitiveType.Cube, "Cap", block.transform, new Vector3(0, size.y / 2 + 0.05f, 0), new Vector3(size.x + 0.12f, 0.1f, size.z + 0.12f), trim);
            Prim(PrimitiveType.Cube, "Edge Light", block.transform, new Vector3(0, size.y / 2 + 0.1f, -(size.z / 2 + 0.06f)), new Vector3(size.x + 0.12f, 0.025f, 0.025f),
                Glow("Glow_WireEdge", Hdr(ElectronTint, 0.6f), softTexture: false)); // neutral and faint: the block is lit before its terminal "appears"

            var badge = Node("Card Terminal " + sign, block.transform, face);
            badge.transform.localRotation = Quaternion.LookRotation(normal); // local +Z = outward
            Prim(PrimitiveType.Cylinder, "Puck", badge.transform, new Vector3(0, 0, 0.12f), new Vector3(radius * 2, 0.12f, radius * 2), steel, new Vector3(90, 0, 0));
            var rim = WireMesh("Rim", badge.transform, Torus("WireBadgeRim", 0.05f), Lit("Wire_SteelBright", new Color(0.6f, 0.65f, 0.72f), 1f, 0.7f));
            rim.transform.localPosition = new Vector3(0, 0, 0.235f);
            rim.transform.localScale = Vector3.one * radius;
            // Saturated, not blown to white. The cyan − face is the darker one: lit by its own glow light, a 0.6 cyan went near-white
            // under ACES and bloom and its white sign was lost on it (the red + keeps one bright channel, so its sign reads).
            bool minus = sign == "−";
            var disc = WireMesh("Face", badge.transform, WireDisc(), Lit("Wire_BadgeFace_" + hex, tint * (minus ? 0.3f : 0.6f), 0f, 0.6f, Hdr(tint, minus ? 0.5f : 0.9f)));
            disc.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0.245f), Quaternion.Euler(0, 180, 0)); // WireDisc faces −Z; the viewer is along +Z
            disc.transform.localScale = Vector3.one * (radius * 0.92f);
            var glyph = Glow("Glow_WireGlyph", Hdr(Color.white, 1.2f), softTexture: false); // a bright sign on the coloured disc, under the bloom threshold: it read as a white blob at 2×
            Prim(PrimitiveType.Cube, "Glyph", badge.transform, new Vector3(0, 0, 0.255f), new Vector3(radius * 0.9f, radius * 0.2f, 0.02f), glyph);
            if (sign == "+") Prim(PrimitiveType.Cube, "Glyph", badge.transform, new Vector3(0, 0, 0.255f), new Vector3(radius * 0.2f, radius * 0.9f, 0.02f), glyph);
            var halo = Prim(PrimitiveType.Quad, "Halo", badge.transform, new Vector3(0, 0, 0.2f), Vector3.one * (radius * 3.2f), Glow("Glow_WireBadgeHalo_" + hex, new Color(tint.r, tint.g, tint.b, 0.16f))); // behind the face: it no longer washes out the sign
            halo.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var glow = Node("Glow Light", badge.transform, new Vector3(0, 0, 0.7f)).AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = tint;
            glow.intensity = light;
            glow.range = 7f;
            glow.shadows = LightShadows.None;
            glow.GetUniversalAdditionalLightData().renderingLayers = 1u | FloorLight; // its colour streak on the glossy floor
            return Ref(badge);
        }

        // The counter: a ConceptPanel-style world card standing just above the cross-section ring, faced to the camera: the
        // number (concept-word tier, in the current colour) inside its one-second ring, "charges / per second" under it
        // (small tier, white), a dark-blue panel with a cyan edge, and a thin leader down to the ring. The TYPOGRAPHY
        // counter row (96 px) was for a bare number; with the ring, the caption and the panel it would not fit between the
        // conduit and the HUD, so the number takes the concept-word tier (64 px).
        void WireCounter(WireInterior wire, Transform section)
        {
            Vector3 forward = (WireCamLook - WireCamPos).normalized;
            float depth = Vector3.Dot(WireOrigin + new Vector3(CrossSectionX, 0, 0) - WireCamPos, forward); // the view is level: height does not change it
            float numberCap = WorldText.CapForScreenPx(WorldText.Px.ConceptWord, depth, WireFov), captionCap = WorldText.CapForScreenPx(WorldText.Px.Small, depth, WireFov);
            float ring = numberCap, advance = WorldText.LineAdvance(palette.font, captionCap, WorldText.ProseLineSpacing);
            float captionY = -(ring + numberCap * 0.25f + advance);          // two caption lines: their centre one advance below the ring
            float top = ring + numberCap * 0.3f, bottom = captionY - advance - captionCap * 0.6f;
            var counter = Node("Counter", section, new Vector3(CrossSectionX, TunnelRadius * 1.1f + 0.2f - bottom, 0)); // the panel's bottom clears the ring
            Face(counter);

            wire.counter = WorldText.Card("Count", "0", palette.current, counter.transform, Vector3.zero, numberCap);
            wire.counter.sortingOrder = 1;
            var caption = WorldText.Card("Caption", "charges\nper second", Color.white, counter.transform, new Vector3(0, captionY, 0), captionCap);
            caption.sortingOrder = 1;
            var faint = Glow("Glow_WireCounterTrack", new Color(ElectronTint.r, ElectronTint.g, ElectronTint.b, 0.25f), softTexture: false);
            var track = WireMesh("Ring Track", counter.transform, Torus("WireCounterTrack", 0.03f), faint);
            track.transform.localScale = Vector3.one * ring;
            track.GetComponent<Renderer>().sortingOrder = 1;
            wire.counterRing = Node("Ring", counter.transform).AddComponent<LineRenderer>();
            wire.counterRing.sharedMaterial = Glow("Glow_WireCounter", Hdr(ElectronTint, 2.4f), softTexture: false);
            wire.counterRing.widthMultiplier = numberCap * 0.1f;
            wire.counterRing.useWorldSpace = false;
            wire.counterRing.loop = false;
            wire.counterRing.numCapVertices = 2;
            wire.counterRing.shadowCastingMode = ShadowCastingMode.Off;
            wire.counterRing.sortingOrder = 1;
            wire.counterRingRadius = ring;

            var panelSize = new Vector2(Mathf.Max(ring * 2 + numberCap * 0.6f, caption.textBounds.size.x + captionCap * 1.6f), top - bottom);
            WirePanel(counter.transform, new Vector3(0, (top + bottom) / 2, 0.02f), panelSize, numberCap);
            Polyline("Leader", counter.transform, faint, numberCap * 0.05f, false, true,
                counter.transform.TransformPoint(new Vector3(0, bottom, 0)), WireOrigin + new Vector3(CrossSectionX, TunnelRadius * 1.1f, 0));
            counter.SetActive(false);
            Ref(counter);
        }

        // ConceptPanel-style backing for a world card: dark-blue fill with a cyan edge, rounded (the theme's 9-sliced
        // sprite; quads when it is missing). Drawn behind the text via sorting order whatever the view angle.
        void WirePanel(Transform parent, Vector3 centre, Vector2 size, float cap)
        {
            float border = Mathf.Max(0.01f, cap * 0.06f);
            WirePanelLayer("Edge", parent, centre + Vector3.forward * 0.004f, size + Vector2.one * (border * 2), new Color(ConduitBlue.r, ConduitBlue.g, ConduitBlue.b, 0.9f), cap, -2);
            WirePanelLayer("Fill", parent, centre, size, new Color(0.06f, 0.11f, 0.22f, 0.95f), cap, -1);
        }

        void WirePanelLayer(string name, Transform parent, Vector3 centre, Vector2 size, Color color, float cap, int order)
        {
            var sprite = palette ? palette.roundedPanel : null;
            if (sprite)
            {
                // 9-sliced panel: the sprite's 16 px corners at 100 px/unit are 0.16 m, scaled to ~0.25 cap.
                var panel = new GameObject(name).AddComponent<SpriteRenderer>();
                panel.transform.SetParent(parent, false);
                float s = Mathf.Max(0.01f, 0.25f * cap / 0.16f);
                panel.transform.localPosition = centre;
                panel.transform.localScale = Vector3.one * s;
                panel.sprite = sprite;
                panel.drawMode = SpriteDrawMode.Sliced;
                panel.size = size / s;
                panel.color = color;
                panel.sortingOrder = order;
                panel.shadowCastingMode = ShadowCastingMode.Off;
                var pipeline = GraphicsSettings.currentRenderPipeline;
                if (pipeline && pipeline.default2DMaterial) panel.sharedMaterial = pipeline.default2DMaterial; // URP's unlit sprite shader
                return;
            }
            var quad = Prim(PrimitiveType.Quad, name, parent, centre, new Vector3(size.x, size.y, 1), Translucent("Wire_Panel_" + ColorUtility.ToHtmlStringRGBA(color), color));
            quad.GetComponent<Renderer>().sortingOrder = order;
        }

        // Glass: URP Lit, premultiplied alpha so the specular highlights from the terminal lights stay on the nearly
        // transparent tube; drawn after the electrons so the carriers inside take the glass tint.
        static Material WireGlass(string name, Color color) =>
            Mat(name, "Universal Render Pipeline/Lit", m =>
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 1); // premultiply
                m.SetFloat("_SrcBlend", (float)BlendMode.One);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha")) { m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha); }
                m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent + 10;
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Smoothness", 0.95f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 0.12f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });

        // One grid cell: a thin bright line along two edges, black elsewhere; tiled 44× over the floor as its emission map.
        static Texture2D WireGridTexture()
        {
            Directory.CreateDirectory(Art + "/Textures");
            string png = Art + "/Textures/FloorGrid.png";
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Min(x, y);                                   // distance to the cell's left/bottom edge in texels
                    float line = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.5f, 4f, d));
                    tex.SetPixel(x, y, new Color(line, line, line, 1f));
                }
            File.WriteAllBytes(png, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(png);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(png);
        }

        // A mesh renderer child with no collider.
        static GameObject WireMesh(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // A ring around the conduit at a local position: a node WireInterior scales to (radius, radius, 1), carrying the
        // unit-radius ring mesh at `scale` (so 1.1 sits just outside the glass). Rotated so the ring lies across the conduit.
        static Transform WireRing(string name, Transform section, Vector3 position, float scale, Mesh mesh, Material material)
        {
            var node = new GameObject(name);
            node.transform.SetParent(section, false);
            node.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0, 90, 0));
            node.transform.localScale = new Vector3(TunnelRadius, TunnelRadius, 1);
            WireMesh("Mesh", node.transform, mesh, material).transform.localScale = Vector3.one * scale;
            return node.transform;
        }

        // Unit disc in the XY plane facing −Z, a fan of 48 triangles.
        static Mesh WireDisc() => MeshAsset("WireDisc", mesh =>
        {
            const int segments = 48;
            var vertices = new List<Vector3> { Vector3.zero };
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                vertices.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0));
                if (i < segments) triangles.AddRange(new[] { 0, i + 2, i + 1 });
            }
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
        });

        // Scene 4's glowing word face on a label: HDR face colour with an outer glow, so Bloom lights the text.
        protected static void WireGlowFace(TMP_Text label, Color color)
        {
            if (!label || !label.font) return;
            var font = label.font;
            var material = Asset(Art + "/Materials/Word_Wire_" + ColorUtility.ToHtmlStringRGB(color) + ".mat", () => new Material(font.material));
            material.shader = Shader.Find("TextMeshPro/Distance Field");
            material.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
            material.SetColor(ShaderUtilities.ID_FaceColor, Hdr(color, 2.6f));
            material.EnableKeyword("GLOW_ON");
            material.SetColor(ShaderUtilities.ID_GlowColor, new Color(color.r, color.g, color.b, 0.6f));
            material.SetFloat(ShaderUtilities.ID_GlowOuter, 0.45f);
            material.SetFloat(ShaderUtilities.ID_GlowPower, 0.6f);
            EditorUtility.SetDirty(material);
            label.fontSharedMaterial = material;
            label.color = Color.white; // the face colour carries the hue, as in Scene 4
        }
    }
}
