using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw.Editor
{
    // Scene 9: Build the Circuit, guided. Every step is stated in the panel before the learner acts (select the
    // ammeter, click the highlighted position, select the voltmeter, click its position, close the switch), with a live
    // schematic on a glass board floating over the back of the bench: the ammeter in series, the voltmeter in parallel
    // across XY, the key. The dock keeps the turn card. Bench Study holds the whole bench with Wattson behind it, so
    // taps land where the learner looks.
    public class Scene09Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene09";
        public override string SceneName => "Scene09_BuildCircuit";
        public override string Title => "Build the Circuit";

        // The schematic board: 1.15 m wide over the back of the bench, left of centre so Wattson's points at the tray, the
        // voltmeter and the key (frame right) stay clear of it, tilted 10° down toward the Bench Study lens. On Bench Study
        // it spans about x 240-877, y 131-404 of 1920 x 1080: clear of the chapter chip above, the meter tops (~450) below.
        static readonly Vector3 BoardC = new Vector3(-0.28f, 1.34f, 0.42f);
        const float BoardWidth = 1.15f;

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
                "The schematic on the board over the bench shows the same circuit: conventional current is drawn from + to − around the loop; the free electrons themselves drift the other way, from − to +.",
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
            SchematicBoard();
            Vcam("Bench Study", Scene10Builder.StudyCamC, Scene10Builder.StudyLookC, Scene10Builder.StudyFov, refs["Bench Point"].transform, lensMedium);
            // Two slow dollies for the explanations (audit finding 4), each pushed 0.5 m along its own axis ("push" to
            // "Cam <name> Goal"; a cut there under reduced motion) and back on Bench Study before its gate. ClassroomSet's
            // "Bench Close" is the whole apparatus row, so these carry their own names. Subjects sit near the authored
            // centre, inside the dock's crop. Tray Close (S09_L05): the parts tray with both meters and Wattson pointing from
            // Tray Point above it (his hat 2.6 deg inside the top edge at the goal); the key and X fall just outside.
            // Voltmeter Close (S09_L07): Y, the glowing parallel slot behind the wire, the tray's voltmeter and Wattson at
            // Voltmeter Point; X is outside the free frame (both badges pulse on Bench Study at the gate).
            Dolly("Tray Close", new Vector3(0.32f, 1.45f, -1.84f), new Vector3(0.7f, 1.2f, 0.5f));
            Dolly("Voltmeter Close", new Vector3(0.1f, 1.38f, -1.94f), new Vector3(0.4f, 1.0f, 0.3f));
        }

        const float DollyPush = 0.5f;

        // A fixed-aim close-up at `at` looking at `aim` (its focus), with its goal 0.5 m further along the same axis.
        void Dolly(string name, Vector3 at, Vector3 aim)
        {
            Vcam(name, at, aim, Scene10Builder.StudyFov, Marker(name + " Aim", aim).transform, lensMedium);
            Marker("Cam " + name + " Goal", at + (aim - at).normalized * DollyPush);
        }

        // The live schematic on a world board (WorldSchematic builds the glass canvas and CircuitSchematic.CreateOn at
        // runtime). Hidden until "show Schematic Board"; pops in.
        void SchematicBoard()
        {
            var board = Node("Schematic Board", null, BoardC);
            board.transform.rotation = Quaternion.Euler(-10f, 0f, 0f);
            board.AddComponent<WorldSchematic>().width = BoardWidth;
            var pop = board.AddComponent<PopIn>();
            pop.duration = 0.4f;
            pop.fromScale = 0.6f;
            pop.overshoot = 1.08f;
            board.SetActive(false);
            Ref(board);
        }

        // "Points X and Y pulse" while the voltmeter waits: a breathing amber halo with slow rings behind each badge. The
        // badge masks the middle, so the glow rims it and the letter stays readable (in front, bloom turned it into a blob).
        void BadgePulse()
        {
            var root = Node("[VFX] X and Y pulse");
            var glow = PulseGlow();
            foreach (var letter in new[] { "X", "Y" })
                Halo(root.transform, letter, refs["Card " + letter].transform.position + Vector3.forward * 0.045f, 0.16f, 0.05f, glow);
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
            Music(Scene10Builder.LabTheme(sounds));
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
            Show("Schematic Board"); // the circuit schematic pops in on the glass board over the bench: the loop with the empty positions

            // The ammeter: select it, then click the highlighted position (the gate writes both instructions). The camera
            // dollies onto the tray while he says why; back on the whole bench (tray and glowing slot) 0.4 s before the gate.
            Gesture(WattsonController.Gesture.Point, point: "AMMETER", moveTo: "Tray Point");
            Cue("callout show ammeter");
            Cue("rig.drag ammeter", "the series slot glows: the ammeter may be selected now");
            Shot("Tray Close");
            Cue("push 3.2 Cam Tray Close", "a slow dolly onto the tray and the ammeter waiting in it");
            Shot("Bench Study", offset: 3.8);
            Line("S09_L05", 0.2);
            Gate("S09_PlaceAmmeter", "Panel: 'Select the ammeter.' then 'Click the highlighted position to place it.' (tap the meter, tap the glowing slot; or drag it there). Snaps with a placement sound; the series path lights, the X → Y pulse runs, the board's A lights. AutoPlace / autoAnswer fly it in.");
            Sfx("SFX_Condition_Reached", 0.3); // audio: the placement confirmed, 0.5 s after the gate's own Place sound, as the series path lights
            Visual("The ammeter snaps in: the series path lights, the X → Y pulse starts", 0.8);
            Gesture(WattsonController.Gesture.Point, point: "AMMETER");
            Line("S09_L06");

            // The voltmeter, across XY: the dolly onto the voltmeter side while he says why; back on the whole bench before
            // the gate (X and Y both pulse there).
            Gesture(WattsonController.Gesture.Point, point: "VOLTMETER", moveTo: "Voltmeter Point");
            Cue("callout show voltmeter");
            Cue("rig.drag voltmeter", "the parallel slot glows: the voltmeter may be selected now");
            Shot("Voltmeter Close");
            Cue("push 3.2 Cam Voltmeter Close", "a slow dolly onto Y, the parallel slot and the voltmeter in the tray");
            Shot("Bench Study", offset: 3.8);
            Line("S09_L07", 0.2);
            Show("[VFX] X and Y pulse");
            Gate("S09_PlaceVoltmeter", "Panel: 'Select the voltmeter.' then 'Click the highlighted position to place it.' Its leads reach X and Y; the board's V and its parallel leads light.");
            Sfx("SFX_Condition_Reached", 0.3); // audio: the placement confirmed as the leads reach X and Y
            Hide("[VFX] X and Y pulse");
            Cue("callout hideall");
            Cue("retract 0.5 Parts Tray", "the empty tray folds away, so the last frame matches Scene 10's bench");
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY");
            Line("S09_L08");

            // The key.
            Gesture(WattsonController.Gesture.Point, point: "switch / key", moveTo: "Key Point");
            Show("[VFX] Key pulse");
            Line("S09_L09", 0.2);
            Gate("S09_CloseKey", "Panel: 'Close the switch to complete the circuit.' Tap the key (tap-hand hint): the lever closes with a clack, the needles move at the small default voltage, the loop on the board warms to amber (conventional current, + → −).");
            Hide("[VFX] Key pulse");
            // Audio: the key's clack is the gate's own Switch sound, and the current's hum is the runtime ElectricalAudio component
            // (it follows the rig's current, fading out at 0 V), so no "ambience CircuitHum" bed here: the room tone carries on.
            Gesture(WattsonController.Gesture.Talking);
            Line("S09_L10");
            // The twist gets its picture once current flows: the board's loop is amber with its "+ → −" note, and the
            // pulse runs X (+ side) to Y on the bench while Wattson points at the board (restarted: the key's own run is
            // nearly over by now).
            Hide("[VFX] Pulse travels from X to Y");
            Show("[VFX] Pulse travels from X to Y", 0.1);
            Gesture(WattsonController.Gesture.Point, point: "Schematic Board");
            Line("S09_Conventional");
            Gesture(WattsonController.Gesture.Talking);
            Cue("reminder", "the panel recalls the question and the learner's own prediction");
            Line("S09_L11", 0.2);
            Line("S09_L12", 0.2);
            Cue("rig.path 0", "the lit path goes: Scene 10 shows plain cables");
            Cue("callout hideall");
            Hide("Schematic Board"); // the board goes with the notes: the last frame matches Scene 10's first
            Cue("handoff", "SceneHandoffState: CircuitState + camera");
            Handoff("Scene09Complete", cut: true, afterSeconds: 0.3);
        }
    }
}
