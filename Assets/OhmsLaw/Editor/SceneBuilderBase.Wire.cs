using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw.Editor
{
    // Stage kit: the inside of a wire (Scenes 5, 6, 12). A 16 m copper (or nichrome) tube along +X around WireOrigin,
    // seen from "Wire Inside" looking down the tunnel: Wattson's platform lower-left at 2.6 m, the VOLTAGE and CURRENT
    // cards upper-left and upper-right at the same 3.5 m depth, the cross-section ring and its counter in the middle
    // distance, the + terminal cap at the far end. Free electrons are the glowing "−" carriers WireInterior drives.
    public abstract partial class SceneBuilderBase
    {
        // ------------------------------------------------------------------ wire interior

        protected const float TunnelRadius = 2.5f, TunnelLength = 16f;
        protected const float CrossSectionX = 1.5f;                                                    // where the counting plane cuts the tunnel
        protected static readonly Vector3 WireOrigin = new Vector3(100, 0, 0);                        // far from the lab in Scene 12
        protected static readonly Vector3 WireCamPos = WireOrigin + new Vector3(-3.2f, -0.4f, -0.35f); // near the axis: the narrowed tunnel still surrounds it
        protected static readonly Vector3 WireCamLook = WireOrigin + new Vector3(3.0f, 0.1f, 0.2f);
        protected static readonly Vector3 PlatformPoint = WireOrigin + new Vector3(-0.65f, -0.75f, 0.85f); // Wattson hovers here: 2.6 m from the lens, lower-left third
        protected static readonly Vector3 PlatformPos = PlatformPoint + new Vector3(0, -0.4f, 0);          // the platform disc under him
        protected static readonly Vector3 WallPoint = WireOrigin + new Vector3(2.0f, 0.7f, 2.2f);           // the wall Wattson aims the Ohm-niscope at (Scene 6)
        protected static readonly Color CopperTint = new Color(0.72f, 0.38f, 0.2f), NichromeTint = new Color(0.55f, 0.56f, 0.5f);
        protected static readonly Color MinusTint = new Color(0.86f, 0.92f, 1f), PlusTint = new Color(1f, 0.93f, 0.8f); // battery whites, no palette hue
        // Text inside the wire (Docs/Polish/TYPOGRAPHY.md): concept word + "<size=56%>" sub-lines at 3.5 m, the question card at 6 m.
        protected const float WireCardDepth = 3.5f, WireConceptCap = 0.23f, WireQuestionDepth = 6f, WireQuestionCap = 0.38f, WireQuestionWidth = 6f;

        // A point in the "Wire Inside" frame (FOV 60, 16:9): x and y in [-1, 1] across the frame, 0,0 at its centre; depth along the view in metres.
        protected static Vector3 WireFramePoint(float x, float y, float depth)
        {
            Vector3 forward = (WireCamLook - WireCamPos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized, up = Vector3.Cross(forward, right);
            const float tanHalfFov = 0.5774f, aspect = 16f / 9f;
            return WireCamPos + forward * depth + right * (x * depth * tanHalfFov * aspect) + up * (y * depth * tanHalfFov);
        }

        // Concept cards: side -1 (VOLTAGE, upper-left third) or +1 (CURRENT, upper-right third), both at one depth so they read at one size.
        protected static Vector3 WireCardPosition(int side, float depth = WireCardDepth) => WireFramePoint(side * 0.36f, 0.32f, depth);
        protected static Vector3 WireQuestionPos => WireFramePoint(0, 0.3f, WireQuestionDepth);     // "WHAT MAKES CHARGE FLOW?"
        protected static Vector3 WireResistancePos => WireFramePoint(0, 0.1f, 4.5f);                  // "RESISTANCE" (Scene 12), "Symbol: R" goes 0.6 m under it

        protected WireInterior WireStage(Color tint, bool lattice, bool active = true)
        {
            string hex = ColorUtility.ToHtmlStringRGB(tint);
            var section = Node("Wire Section", null, WireOrigin);
            var wire = section.AddComponent<WireInterior>();
            wire.materialTint = tint;
            wire.latticeVisibility = lattice ? 1 : 0;
            wire.tunnelRadius = TunnelRadius;
            wire.length = TunnelLength;
            wire.count = 700;

            // The tube: wound to face the axis and lit from inside (default culling), ribs every 1.2 m so depth reads, a capped
            // end each way. Ribs and caps are children of the (length, radius, radius)-scaled tube so they follow the narrowing.
            var tunnel = Prim(PrimitiveType.Cylinder, "Tunnel", section.transform, Vector3.zero, new Vector3(TunnelLength, TunnelRadius, TunnelRadius),
                Lit("Wire_Tunnel_" + hex, tint, 0.7f, 0.5f, tint * 0.08f));
            tunnel.GetComponent<MeshFilter>().sharedMesh = BlockoutAssets.TunnelMesh();
            wire.tunnel = tunnel.GetComponent<Renderer>();
            var rib = Torus("WireRibRing", 0.012f);
            var ribMat = Glow("Glow_WireRib_" + hex, Hdr(Color.Lerp(tint, Color.white, 0.4f), 1.6f), softTexture: false);
            for (float x = -TunnelLength / 2 + 0.8f; x < TunnelLength / 2 - 0.4f; x += 1.2f)
                WireRing("Rib", tunnel.transform, x / TunnelLength, 0.985f, rib, ribMat);
            var cap = Torus("WireCapRing", 0.02f);
            var capMat = Glow("Glow_WireCap", Hdr(new Color(1f, 0.95f, 0.85f), 2.6f), softTexture: false);
            var endMat = Translucent("Wire_EndCap_" + hex, new Color(tint.r * 0.3f, tint.g * 0.3f, tint.b * 0.3f, 0.92f));
            foreach (float end in new[] { -0.5f, 0.5f })
            {
                WireRing("Cap Ring", tunnel.transform, end * 0.985f, 0.96f, cap, capMat);
                WireRing("End Cap", tunnel.transform, end, 0.995f, WireDisc(), endMat);
            }

            // Scene 4's wire look: tinted fog and ambient, three tunnel lights, the Wire_Global ACES/bloom volume. The lighting
            // node joins the section (Scene 12 shows and hides it with the dive) and the atmosphere lives on WireInterior so
            // it switches with the section too; a scene that opens in the lab keeps the lab's atmosphere in its file.
            var was = (fog: RenderSettings.fog, mode: RenderSettings.fogMode, colour: RenderSettings.fogColor, density: RenderSettings.fogDensity,
                ambientMode: RenderSettings.ambientMode, sky: RenderSettings.ambientSkyColor, background: camera.backgroundColor);
            var before = new HashSet<GameObject>(scene.GetRootGameObjects());
            WireLighting(tint);
            wire.fogColor = RenderSettings.fogColor;
            wire.fogDensity = RenderSettings.fogDensity;
            wire.ambient = RenderSettings.ambientLight;
            wire.background = camera.backgroundColor;
            foreach (var lighting in scene.GetRootGameObjects().Where(g => !before.Contains(g)))
            {
                lighting.name = "Wire " + lighting.name;
                lighting.transform.SetParent(section.transform, true);
                foreach (var volume in lighting.GetComponentsInChildren<Volume>(true)) volume.priority = 1;      // beats a lab volume while the section is active
                if (lattice) foreach (var light in lighting.GetComponentsInChildren<Light>(true)) light.intensity *= 0.7f; // nichrome: dimmer, the opposition mood
            }
            if (!active)
            {
                RenderSettings.fog = was.fog; RenderSettings.fogMode = was.mode; RenderSettings.fogColor = was.colour; RenderSettings.fogDensity = was.density;
                RenderSettings.ambientMode = was.ambientMode; RenderSettings.ambientSkyColor = was.sky; camera.backgroundColor = was.background;
            }
            var warm = Node("Platform Light", section.transform, PlatformPoint - WireOrigin + new Vector3(0, 0.7f, 0)).AddComponent<Light>();
            warm.type = LightType.Point;
            warm.color = new Color(1f, 0.78f, 0.5f);
            warm.intensity = 1.6f;
            warm.range = 4f;
            warm.shadows = LightShadows.None;

            // Free electrons: glowing "−" sprites, positions driven by WireInterior. Lattice ions: dull grey spheres (Scene 12).
            wire.carriers = Particles("Carriers", section.transform, BlockoutAssets.ChargeMaterial(palette.charge), wire.count);
            wire.lattice = Particles("Lattice Ions", section.transform, BlockoutAssets.ChargeMaterial(Color.white, ion: true), 600);
            Ref(wire.lattice.gameObject);

            // Cross-section: a thin additive ring with a faint disc of light across the tunnel; carriers flash white as they cross.
            var cross = Node("Cross Section", section.transform, new Vector3(CrossSectionX, 0, 0));
            cross.transform.localRotation = Quaternion.Euler(0, 90, 0); // faces along the tunnel; WireInterior scales it to the radius
            Prim(PrimitiveType.Quad, "Disc", cross.transform, Vector3.zero, new Vector3(2, 2, 1), Glow("Glow_WireCrossDisc", new Color(palette.charge.r, palette.charge.g, palette.charge.b, 0.12f)));
            WireMesh("Ring", cross.transform, Torus("WireCrossRing", 0.008f), Glow("Glow_WireCrossRing", Hdr(palette.charge, 3f), softTexture: false));
            cross.SetActive(false);
            wire.crossSection = Ref(cross).transform;

            // Counter: the big number in a one-second ring with its caption under it, faced to the camera above the cross-section.
            var counter = Node("Counter", section.transform, WireFramePoint(0, 0.56f, 4.8f) - WireOrigin);
            wire.counter = WorldText.Card("Count", "0", palette.current, counter.transform, Vector3.zero, 0.5f);
            WireGlowFace(wire.counter, palette.current);
            WireGlowFace(WorldText.Card("Caption", "charges per second", Color.white, counter.transform, new Vector3(0, -0.78f, 0), 0.14f), Color.white);
            wire.counterRing = Node("Ring", counter.transform).AddComponent<LineRenderer>();
            wire.counterRing.sharedMaterial = Glow("Glow_WireCounter", Hdr(palette.current, 2.4f), softTexture: false);
            wire.counterRing.widthMultiplier = 0.05f;
            wire.counterRing.useWorldSpace = false;
            wire.counterRing.loop = false;
            wire.counterRing.numCapVertices = 2;
            wire.counterRing.shadowCastingMode = ShadowCastingMode.Off;
            Face(counter);
            counter.SetActive(false);
            Ref(counter);

            // Battery terminals. Electrons drift − → +. The −X end is behind the lens, so the "−" card hangs top-left in the near
            // field and the "+" card floats before the far cap; each has a glowing cap ring so it reads as a terminal.
            var minus = Card("Terminal −", "–\n<size=45%>terminal", MinusTint, section.transform, WireFramePoint(-0.74f, 0.62f, 2.4f) - WireOrigin, 0.2f);
            var plus = Card("Terminal +", "+\n<size=45%>terminal", PlusTint, section.transform, new Vector3(5.5f, 0.85f, 0f), 0.45f);
            WireTerminalCap(minus, 0.2f, MinusTint);
            WireTerminalCap(plus, 0.45f, PlusTint);
            WireGlowFace(minus.GetComponentInChildren<TMP_Text>(), MinusTint);
            WireGlowFace(plus.GetComponentInChildren<TMP_Text>(), PlusTint);
            Face(minus); Face(plus);

            // The detective platform: dark deck, brass trim, glowing rim. Wattson hovers 0.4 m over it.
            var platform = Node("Platform", section.transform, PlatformPos - WireOrigin);
            Prim(PrimitiveType.Cylinder, "Deck", platform.transform, Vector3.zero, new Vector3(1.6f, 0.04f, 1.6f), Lit("Wire_Platform", new Color(0.14f, 0.13f, 0.15f), 0.7f, 0.5f));
            Prim(PrimitiveType.Cylinder, "Trim", platform.transform, new Vector3(0, 0.012f, 0), new Vector3(1.3f, 0.04f, 1.3f), Lit("Wire_Brass", new Color(0.83f, 0.66f, 0.3f), 1f, 0.68f));
            var rim = WireMesh("Rim", platform.transform, Torus("WireRimRing", 0.03f), Glow("Glow_WireRim", Hdr(palette.wattsonAccent, 2.2f), softTexture: false));
            rim.transform.localPosition = new Vector3(0, 0.03f, 0);
            rim.transform.localRotation = Quaternion.Euler(90, 0, 0);
            rim.transform.localScale = Vector3.one * 0.82f;

            Marker("Platform Point", PlatformPoint);
            Marker("Wall Point", WallPoint);
            var focus = Marker("Wire Focus Point", WireFramePoint(0, 0.1f, WireCardDepth));
            Vcam("Wire Inside", WireCamPos, WireCamLook, 60, focus.transform, lensWide);
            Vcam("Wire Wattson", PlatformPoint + new Vector3(-1.4f, 0.45f, -1.45f), PlatformPoint + new Vector3(0.4f, 0.3f, 0.3f), 40, refs["Platform Point"].transform, lensMedium);

            // The effects the scene cues name, in place and hidden; a builder may re-place one with WireFx(name, position).
            foreach (var name in new[] { "letters assemble from charges", "field wave sweeps whole tunnel", "time-freeze (Ohm-niscope holds the carriers)",
                "Ohm-niscope flash", "carriers impeded by lattice (slow, bounce)", "RESISTANCE word forms from the lattice" })
                WireFx(name, WireFxDefault(name), section.transform);

            section.SetActive(active);
            Ref(section);
            return wire;
        }

        // Turns a card toward the wire camera so it reads correctly.
        protected void Face(GameObject card, Vector3? from = null)
        {
            Vector3 eye = from ?? WireCamPos;
            card.transform.rotation = Quaternion.LookRotation(card.transform.position - eye);
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

        // Scene 5: two points along the wire light up with a dashed bracket between them (the potential difference between
        // them). Registered as `name` with "Point A" / "Point B" children; starts hidden. The defaults run along the wire at
        // eye level right of centre, receding toward the far end: clear of Wattson (lower-left) and of both cards.
        protected GameObject WireBracket(Vector3? a = null, Vector3? b = null, string name = "Bracket")
        {
            Vector3 pa = a ?? WireOrigin + new Vector3(-0.7f, -0.3f, -1.3f), pb = b ?? WireOrigin + new Vector3(2.3f, -0.3f, -1.3f);
            var root = Node(name, null, (pa + pb) / 2);
            var glow = Glow("Glow_WireBracket", Hdr(palette.voltage, 2.6f), softTexture: false);
            var halo = Glow("Glow_WireBracketHalo", new Color(palette.voltage.r, palette.voltage.g, palette.voltage.b, 0.5f));
            Vector3 Local(Vector3 world) => world - root.transform.position;
            foreach (var (label, p) in new[] { ("Point A", pa), ("Point B", pb) })
            {
                var point = Ref(Prim(PrimitiveType.Sphere, label, root.transform, Local(p), Vector3.one * 0.16f, glow));
                Face(Prim(PrimitiveType.Quad, "Halo", point.transform, Vector3.zero, Vector3.one * 4f, halo));
                Polyline("Tick", root.transform, glow, 0.03f, false, false, Local(p) + Vector3.up * 0.22f, Local(p) - Vector3.up * 0.22f);
            }
            int dashes = Mathf.Max(2, Mathf.RoundToInt((pb - pa).magnitude / 0.25f));
            for (int i = 0; i < dashes; i++)
                Polyline("Dash", root.transform, glow, 0.03f, false, false, Local(Vector3.Lerp(pa, pb, (i + 0.15f) / dashes)), Local(Vector3.Lerp(pa, pb, (i + 0.6f) / dashes)));
            root.SetActive(false);
            return Ref(root);
        }

        // Real effect for a "[VFX] <name>" cue: plays when the object is enabled, so the scene's show/hide cues drive it.
        // Known names get their designed effect, anything else a spark burst. Position is local to `parent` (world when
        // none; the tunnel's own default when omitted). The field wave always starts at the −X end and sweeps the whole tube.
        protected GameObject WireFx(string name, Vector3? position = null, Transform parent = null)
        {
            string full = "[VFX] " + name;
            if (refs.TryGetValue(full, out var old) && old) Object.DestroyImmediate(old); // replaces a magenta tag or an earlier placement
            Vector3 at = position ?? (parent ? Vector3.zero : WireOrigin + WireFxDefault(name));
            if (name.StartsWith("field wave")) at = (parent ? Vector3.zero : WireOrigin) + new Vector3(-TunnelLength / 2, 0, 0);
            var go = Node(full, parent, at);
            Transform t = go.transform;
            Vector3 centre = (parent ? Vector3.zero : WireOrigin) - at;   // the tunnel's middle, relative to this effect
            Color charge = palette.charge, accent = palette.wattsonAccent;
            var spark = Glow("Glow_WireSpark", Hdr(charge, 3.5f));
            var glyph = BlockoutAssets.ChargeMaterial(charge);

            if (name.StartsWith("race into wire"))
            {
                // Scene 4's dive streaks: they stream out of the wire surface past the lens. Forward = toward the camera.
                t.rotation = Quaternion.LookRotation(Vector3.back);
                Emitter("Streaks", t, Vector3.zero, Glow("Glow_WireStreaks", Hdr(Color.Lerp(accent, palette.current, 0.3f), 5f)), ps =>
                {
                    var main = ps.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
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
                // Carriers gather onto the question card's plane (which faces down the tunnel, so the box is wide in Z).
                WireGather(t, glyph, new Vector3(0.6f, 2f, 5f), new ParticleSystem.MinMaxCurve(0.08f, 0.16f));
                WireBurst(t, "Sparks", spark, 60, 1.2f, 0.3f);
            }
            else if (name.StartsWith("field wave"))
            {
                // One ring of light sweeps −X → +X along the whole tunnel in half a second while the whole volume shimmers:
                // the field is set up everywhere at once, nothing is pushed from one end.
                Emitter("Ring", t, Vector3.zero, Glow("Glow_WireWave", Hdr(charge, 4f), softTexture: false), ps =>
                {
                    ps.transform.localRotation = Quaternion.Euler(0, 90, 0); // emits along +X
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 0.5f;
                    main.startLifetime = 0.5f;
                    main.startSpeed = TunnelLength / 0.5f;
                    main.startSize = TunnelRadius * 0.96f;
                    main.maxParticles = 4;
                    var emission = ps.emission;
                    emission.rateOverTime = 0;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                    var shape = ps.shape;
                    shape.enabled = false;
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode = ParticleSystemRenderMode.Mesh;
                    renderer.mesh = Torus("WireWaveRing", 0.02f);
                    renderer.alignment = ParticleSystemRenderSpace.Local;
                });
                Emitter("Shimmer", t, new Vector3(TunnelLength / 2, 0, 0), spark, ps =>
                {
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 0.6f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
                    main.startSpeed = 0;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
                    main.maxParticles = 300;
                    var emission = ps.emission;
                    emission.rateOverTime = 0;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 240) });
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(TunnelLength, TunnelRadius * 1.76f, TunnelRadius * 1.76f);
                });
            }
            else if (name.StartsWith("time-freeze"))
            {
                // A cool pulse spreads from the Ohm-niscope, then frost sparkles hang in the whole tunnel for as long as it is held.
                var frost = Glow("Glow_WireFrost", Hdr(new Color(0.75f, 0.9f, 1f), 3.5f));
                Emitter("Pulse", t, Vector3.zero, frost, ps =>
                {
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 1f;
                    main.startLifetime = 0.9f;
                    main.startSpeed = 0;
                    main.startSize = 6f;
                    main.startColor = new Color(1, 1, 1, 0.45f); // a wash of cold light, not a white-out
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
                    main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.07f);
                    main.maxParticles = 200;
                    var emission = ps.emission;
                    emission.rateOverTime = 70;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(10f, TunnelRadius * 1.76f, TunnelRadius * 1.76f);
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
                    main.startSize = 3.5f;
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
                WireBurst(t, "Sparks", flash, 90, 0f, 0.2f, 1.5f, 4.5f);
            }
            else if (name.StartsWith("carriers impeded"))
            {
                // Collision sparks all through the tunnel while the carriers bounce (the carriers themselves flash in WireInterior).
                Emitter("Collisions", t, centre, Glow("Glow_WireCollide", Hdr(palette.resistance, 3.5f)), ps =>
                {
                    var main = ps.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                    main.maxParticles = 150;
                    var emission = ps.emission;
                    emission.rateOverTime = 110;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(TunnelLength * 0.9f, TunnelRadius * 1.7f, TunnelRadius * 1.7f);
                });
            }
            else if (name.StartsWith("RESISTANCE word"))
            {
                var coral = Glow("Glow_WireCollide", Hdr(palette.resistance, 3.5f));
                WireGather(t, coral, new Vector3(0.6f, 1.6f, 4.5f), new ParticleSystem.MinMaxCurve(0.05f, 0.12f));
                WireBurst(t, "Sparks", coral, 60, 1.2f, 0.3f);
            }
            else WireBurst(t, "Burst", spark, 60, 0f, 0.3f);

            go.SetActive(false);
            return Ref(go);
        }

        // Where each named effect belongs, relative to WireOrigin.
        protected static Vector3 WireFxDefault(string name)
        {
            if (name.StartsWith("letters assemble")) return WireQuestionPos - WireOrigin;
            if (name.StartsWith("field wave")) return new Vector3(-TunnelLength / 2, 0, 0);
            if (name.StartsWith("time-freeze")) return PlatformPoint - WireOrigin + new Vector3(0.3f, 0.3f, 0.2f);
            if (name.StartsWith("Ohm-niscope flash")) return WallPoint - WireOrigin;
            if (name.StartsWith("RESISTANCE word")) return WireResistancePos - WireOrigin;
            return Vector3.zero;
        }

        // Particles drift inward from a box onto its centre for 2.4 s: "the word forms from them".
        void WireGather(Transform parent, Material material, Vector3 box, ParticleSystem.MinMaxCurve size) =>
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
                emission.rateOverTime = 140;
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

        // ------------------------------------------------------------------ pieces

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

        // A ring (or disc) across the tube at fraction x of its length, as a child of the (length, radius, radius)-scaled
        // tube: `scale` is its radius as a fraction of the wall, and the z scale undoes the tube's length stretch so a
        // ring's cross-section stays round.
        static GameObject WireRing(string name, Transform tunnel, float x, float scale, Mesh mesh, Material material)
        {
            var go = WireMesh(name, tunnel, mesh, material);
            go.transform.localPosition = new Vector3(x, 0, 0);
            go.transform.localRotation = Quaternion.Euler(0, 90, 0);
            go.transform.localScale = new Vector3(scale, scale, scale * TunnelRadius / TunnelLength);
            return go;
        }

        // Unit disc in the XY plane, a fan of 48 triangles.
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

        // A terminal card is real: no placeholder note, and a glowing cap ring in front of its text (hidden, shown and boosted with it).
        void WireTerminalCap(GameObject card, float cap, Color tint)
        {
            var note = card.GetComponent<BlockoutPlaceholder>();
            if (note) Object.DestroyImmediate(note);
            var ring = WireMesh("Cap", card.transform, Torus("WireTerminalRing", 0.05f), Glow("Glow_WireTerminal_" + ColorUtility.ToHtmlStringRGB(tint), Hdr(tint, 2.4f), softTexture: false));
            ring.transform.localPosition = new Vector3(0, 0, -0.02f);
            ring.transform.localScale = Vector3.one * (cap * 2.3f);
        }
    }
}
