using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw.Editor
{
    // Scene 9: Build the Circuit, guided. Every step is stated in the panel before the learner acts (select the
    // ammeter, click the highlighted position, select the voltmeter, click its position, close the switch), with a live
    // schematic in the panel: the ammeter in series, the voltmeter in parallel across XY, the key. Bench Present holds
    // the whole bench with Wattson behind it, so taps land where the learner looks.
    public class Scene09Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene09";
        public override string SceneName => "Scene09_BuildCircuit";
        public override string Title => "Build the Circuit";

        protected override void Dialogue()
        {
            L("S09_L01", "That's our test conductor.", 1.8f);
            L("S09_L02", "We're going to change the potential difference across this nichrome wire...measure the current through it...", 5.5f);
            L("S09_L03", "...and see whether your prediction was right.", 2.8f);
            L("S09_L05", "If we want to know how much current flows through the nichrome wire, we use an ammeter.", 5.0f);
            L("S09_L06", "Exactly. An ammeter is connected in series, so the same current passes through it and the test wire.", 5.5f);
            L("S09_L07", "And when we need the potential difference between the two ends of the wire... we use the voltmeter.", 5.0f);
            L("S09_L08", "That's why a voltmeter is connected in parallel across the nichrome wire.", 4.0f);
            L("S09_L09", "Close the switch to complete the circuit.", 2.4f);
            L("S09_L10", "Investigation ready.", 1.6f);
            L("S09_L11", "Remember your prediction?", 1.8f);
            L("S09_L12", "Let's find out.", 1.6f);
            L("S09_Conventional", "Plot twist: we draw conventional current plus to minus. Electrons drift minus to plus!", 4.4f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 8: voltage (V, volt) as the push, current (I, ampere), Ohm's investigation, the modern instruments.",
                "The nichrome wire XY is the test conductor; we will change the potential difference across it and measure the current through it.",
                "The ammeter is connected in series so the same current passes through it and the test wire.",
                "The voltmeter is connected in parallel across the two ends of the wire, X and Y, to measure the potential difference between them.",
                "Closing the key completes the circuit so current can flow.",
                "The schematic in the panel shows the same circuit: conventional current is drawn from + to − around the loop; the free electrons themselves drift the other way, from − to +.",
            },
            new[] { "The readings", "The pattern between V and I", "V is proportional to I", "V = IR", "Resistance", "Ohms", "The Christmas-lights answer" },
            ("Why is the ammeter in series?", "Because we want the ammeter to measure the current through the nichrome wire. In series, the ammeter is part of the same single path, so every bit of charge that passes through the wire also passes through the ammeter. Same path, same current."),
            ("Why is the voltmeter in parallel?", "A voltmeter compares two points. We connect one lead to X and the other to Y, the two ends of the nichrome wire, so it reads the potential difference across the wire itself, while the current carries on through the wire as before."),
            ("What happens if the key is open?", "An open key is a gap in the path. The push from the source is still there, but charge cannot cross the gap, so no current flows and the ammeter reads zero. Close the key and the loop is complete."));

        protected override void Stage()
        {
            ClassroomSet(modernBench: true, audience: false);
            Wattson(WattsonHomeC, BenchWideCamC); // Scene 8's last frame: Wattson at "Wattson Home", Bench Wide, no Ohm
            director.matchCutFrom = "Scene08_OurLab";
            director.rig = Rig(assembled: false, keyClosed: false, voltage: 0);
            director.rig.readoutsShown = false; // closing the key is a needle move; the plates come up with the 1 V baseline
            if (director.rig.pathLabel) { Object.DestroyImmediate(director.rig.pathLabel.gameObject); director.rig.pathLabel = null; }

            // The panel's notes for each part (the cues name them).
            Callout("wire", refs["NICHROME WIRE — XY"], CalloutDef.Anchor.BottomCentre);
            Callout("source", refs["cells / controllable DC source"], CalloutDef.Anchor.TopLeft);
            Callout("ammeter", refs["AMMETER"], CalloutDef.Anchor.TopCentre);
            Callout("key", refs["switch / key"], CalloutDef.Anchor.TopRight);
            Callout("voltmeter", refs["VOLTMETER"], CalloutDef.Anchor.MidRight);
            BadgePulse();
            KeyPulse();
            Vcam("Bench Study", Scene10Builder.StudyCamC, Scene10Builder.StudyLookC, Scene10Builder.StudyFov, refs["Bench Point"].transform, lensMedium);
        }

        // "Points X and Y pulse" while the voltmeter waits: a breathing amber halo with slow rings in front of each badge.
        void BadgePulse()
        {
            var root = Node("[VFX] X and Y pulse");
            var glow = PulseGlow();
            foreach (var letter in new[] { "X", "Y" })
                Halo(root.transform, letter, refs["Card " + letter].transform.position + Vector3.back * 0.045f, 0.16f, 0.05f, glow);
            Calm(root);
            root.SetActive(false);
            Ref(root);
        }

        // The key pulses while it waits to be closed: the same halo and rings, set just behind the lever.
        void KeyPulse()
        {
            var root = Node("[VFX] Key pulse");
            Halo(root.transform, "Key", refs["switch / key"].transform.position + new Vector3(0f, 0.09f, 0.08f), 0.36f, 0.1f, PulseGlow());
            Calm(root);
            root.SetActive(false);
            Ref(root);
        }

        Material PulseGlow()
        {
            var tint = Hdr(palette.current, 2f);
            tint.a = 0.6f;
            return Glow("Glow_BadgePulse", tint);
        }

        void Halo(Transform root, string label, Vector3 at, float size, float ring, Material glow)
        {
            var halo = Prim(PrimitiveType.Quad, "Halo " + label, root, at, new Vector3(size, size, 1f), glow);
            halo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var pop = halo.AddComponent<PopIn>();
            pop.duration = 0.35f;
            pop.fromScale = 0.4f;
            pop.overshoot = 1.3f;
            pop.pulse = 0.2f;
            pop.pulseSpeed = 5f;
            Emitter("Rings " + label, root, at, glow, ps =>
            {
                var main = ps.main;
                main.loop = true;
                main.duration = 1f;
                main.startLifetime = 0.9f;
                main.startSpeed = 0f;
                main.startSize = ring;
                main.maxParticles = 8;
                var emission = ps.emission;
                emission.rateOverTime = 2f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.002f;
                var grow = ps.sizeOverLifetime;
                grow.enabled = true;
                grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 3f));
            });
        }

        static void Calm(GameObject root) => root.AddComponent<ReducedMotionFx>().calm = 0.5f;

        protected override void Beats()
        {
            // Match-cut from Scene 8: the classroom's room tone and pad continue. Wattson flies to the wire and it lights.
            Shot("Bench Wide");
            Ambience("RoomTone");
            Music("PadLab");
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY", moveTo: "Wire Point");
            Show("Wire Highlight");
            Sfx("Highlight");
            Visual("Wattson moves to the nichrome wire XY; it highlights", 0.8);
            Shot("Bench Study");
            Cue("callout show wire");
            Line("S09_L01");
            Line("S09_L02");
            Line("S09_L03");
            Hide("Wire Highlight");
            Cue("callout hideall");
            Cue("schematic 1", "the circuit schematic appears in the panel: the loop with the empty positions");

            // The ammeter: select it, then click the highlighted position (the gate writes both instructions).
            Gesture(WattsonController.Gesture.Point, point: "AMMETER", moveTo: "Tray Point");
            Cue("callout show ammeter");
            Cue("rig.drag ammeter", "the series slot glows: the ammeter may be selected now");
            Line("S09_L05", 0.2);
            Gate("S09_PlaceAmmeter", "Panel: 'Select the ammeter.' then 'Click the highlighted position to place it.' (tap the meter, tap the glowing slot; or drag it there). Snaps with a placement sound; the series path lights, the X → Y pulse runs, the schematic's A lights. AutoPlace / autoAnswer fly it in.");
            Visual("The ammeter snaps in: the series path lights, the X → Y pulse starts", 0.8);
            Gesture(WattsonController.Gesture.Point, point: "AMMETER");
            Line("S09_L06");
            Line("S09_Conventional");

            // The voltmeter, across XY.
            Gesture(WattsonController.Gesture.Point, point: "VOLTMETER", moveTo: "Voltmeter Point");
            Cue("callout show voltmeter");
            Cue("rig.drag voltmeter", "the parallel slot glows: the voltmeter may be selected now");
            Line("S09_L07", 0.2);
            Show("[VFX] X and Y pulse");
            Gate("S09_PlaceVoltmeter", "Panel: 'Select the voltmeter.' then 'Click the highlighted position to place it.' Its leads reach X and Y; the schematic's V and its parallel leads light.");
            Hide("[VFX] X and Y pulse");
            Cue("callout hideall");
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY");
            Line("S09_L08");

            // The key.
            Gesture(WattsonController.Gesture.Point, point: "switch / key", moveTo: "Key Point");
            Show("[VFX] Key pulse");
            Line("S09_L09", 0.2);
            Gate("S09_CloseKey", "Panel: 'Close the switch to complete the circuit.' Tap the key (tap-hand hint): the lever closes with a clack, the needles move at the small default voltage, the loop in the schematic warms to amber (conventional current, + → −).");
            Hide("[VFX] Key pulse");
            Gesture(WattsonController.Gesture.Talking);
            Line("S09_L10");
            Cue("reminder", "the panel recalls the question and the learner's own prediction");
            Line("S09_L11", 0.2);
            Line("S09_L12", 0.2);
            Cue("rig.path 0", "the lit path goes: Scene 10 shows plain cables");
            Cue("callout hideall");
            Cue("handoff", "SceneHandoffState: CircuitState + camera");
            Handoff("Scene09Complete", cut: true, afterSeconds: 0.3);
        }
    }
}
