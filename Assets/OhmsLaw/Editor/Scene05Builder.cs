using System.Linq;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 5: Journey Inside the Wire. Opens on Scene 4's last frame (the copper test wire on Ohm's desk under the
    // Ohm-niscope beam), one short push into the wire's surface through white, then the electron space on one steady
    // camera, one idea at a time, each explained in the panel:
    //   A  free electrons in random motion, no overall flow;
    //   B  the two ends named (− and +), a voltage applied between them, the drift that follows (random motion kept);
    //   C  current: the charge crossing one section each second, counted live in the panel.
    // Ends on the frame Scene 6 match-cuts from (the prediction).
    public class Scene05Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene05";
        public override string SceneName => "Scene05_InsideTheWire";
        public override string Title => "Journey Inside the Wire";

        protected override void Dialogue()
        {
            L("S05_L01", "These are electric charges.", 2.2f);
            L("S05_L06", "And when electric charge flows...", 2.4f);
            L("S05_L07", "...we call that electric current.", 2.4f);
            L("S05_L08", "And that's exactly the clue we need for your Christmas lights. The new battery changed the electrical conditions—but before we solve your mystery, we need to find out exactly how voltage and current are connected.", 12.0f);
            L("S05_L09", "Want to predict what we'll find?", 2.2f);
            // The two ends, named as Wattson points at them.
            L("S05_W02", "This end connects to the battery's negative terminal.", 2.8f, proposed: true);
            L("S05_W03", "That end, to the positive terminal.", 2.2f, proposed: true);
            // The redesign's lines: random motion first, then voltage as a potential difference, then the count.
            L("S05_N01", "Free electrons, inside the copper wire. These electrons are already moving around. But there's no overall flow in one direction yet.", 6.0f, proposed: true);
            L("S05_N02", "Now we apply a voltage: a difference in electric potential between the two ends.", 4.4f, proposed: true);
            L("S05_N03", "That creates the electrical push that gives the electrons an overall direction. They still jiggle, but now they all drift together.", 6.0f, proposed: true);
            L("S05_N04", "Watch this section. One, two, three... Now count how much charge passes in one second.", 6.0f, proposed: true);
            L("S05_N05", "That rate of charge flow, the charge passing each second, is the current.", 4.4f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scene 4: the Christmas-lights mystery, Georg Ohm, the words voltage and current, the Ohm-niscope.",
                "Inside a metal the charge carriers are free electrons.",
                "Before a voltage is applied the electrons move randomly in every direction with no net drift.",
                "A voltage is a potential difference between two points; applying it across the wire sets up an electric field that pushes the electrons. Symbol V, unit volt (V).",
                "With the voltage applied the electrons keep their random motion and all drift together, from the − terminal end toward the + terminal end. Conventional current is drawn the other way, + to −.",
                "Current is the charge crossing a section of the wire each second (I = ΔQ/Δt). Symbol I, unit ampere (A). The counter in the panel counts a few representative charges per second; a real wire carries billions.",
                "The scene shows only a small, evenly spread sample of the free electrons so one can be followed; the drift shown is a picture, real drift is very slow.",
            },
            new[] { "How V and I are related", "V is proportional to I", "V = IR", "R = V / I", "Resistance", "Ohms", "The name 'Ohm's law'", "The answer to the Christmas-lights mystery" },
            ("Why were the charges moving before the voltage?", "Good eye, detective. Free electrons in a metal are always jiggling about because of heat. They dart in every direction, so on average they go nowhere. The voltage doesn't start them moving, it gives all of them a steady drift in one direction."),
            ("Which way do electrons move?", "Electrons carry negative charge, so they drift from the negative terminal end toward the positive terminal end. Scientists also talk about conventional current, which is drawn from positive to negative. Same circuit, two ways of describing it."),
            ("What is one ampere?", "One ampere means one coulomb of charge crossing a section of the wire every second, and one coulomb is about six billion billion electrons. Our counter is doing exactly that job: counting charge per second."));

        protected override void Stage()
        {
            // The macro beat is Scene 4's study with its own lighting, grouped so the flash to white takes it all away.
            var macro = Ref(Node("Macro"));
            Study().transform.SetParent(macro.transform, true);
            StudyLighting();
            scene.GetRootGameObjects().Last(g => g.name == "Lighting").transform.SetParent(macro.transform, true);
            MacroWire(macro.transform);

            // The electron space waits inactive: shown at the cut, it brings its own atmosphere, lights and post volume.
            var wire = WireStage(CopperTint, lattice: false, active: false);
            wire.fieldOn = false;
            director.wire = wire;
            WireDriftArrow();
            WireBracket();
            // v3 cinematic pass: a medium on the conduit for the drift (the push-in lands as the field goes up). Cause and
            // effect share the free frame: the drift arrow from its tail, the + terminal it drifts toward and its
            // "+ positive" label all sit left of the dock (the old, tighter lens put them under it).
            Vcam("Wire Drift", WireOrigin + new Vector3(-1.4f, -0.45f, -7.2f), WireOrigin + new Vector3(1.8f, -0.3f, 0f), 56f, refs["Electrons Point"].transform, lensMedium); // focus on the stream (Wattson is beside and behind this lens)
            Wattson(PlatformPoint + new Vector3(-3.5f, 0.3f, 0.6f), WireCamPos); // glides to Platform Point: lower-left, on the floor

            // The panel's notes for this scene (the cues name them).
            Note("electrons", "Free electrons", "Inside a metal some electrons are free to move. Before a voltage is applied they jiggle randomly: no overall flow in any direction.");
            Note("voltage", "Voltage (V)", "The difference in electric potential between two points. Applying it across the wire creates the push on the electrons. Unit: volt (V).");
            Note("drift", "Electron drift", "The free electrons keep jiggling, and now they all drift from the − end toward the + end. Conventional current is drawn the other way: + → −.");
            Note("current", "Current (I)", "The charge passing a section each second: I = ΔQ ÷ Δt. Unit: ampere (A).");
        }

        // Scene 4's desk as it was at the flash: the same 1826 apparatus, the copper wire on its posts, the beam's hot
        // spot, the shimmer of the lock-on, and the streaks racing out of the surface. Both macro cameras stand on
        // Scene 4's dive axis; the push from A to B is short.
        void MacroWire(Transform macro)
        {
            var group = Node("Macro Wire", macro).transform;
            const float half = 0.21f;
            HistoricalApparatus(DeskTop, StudyTurn).transform.SetParent(group, true);
            foreach (var loose in new[] { "Ohm Hand Wire", "Apparatus Point", "Ohm Notebook Target", "Ohm Galvanometer Target", "Ohm Wire Target" })
                if (refs.TryGetValue(loose, out var piece)) piece.transform.SetParent(group, true);

            Vector3 toWire = (Wire4 - Hover).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, toWire).normalized;
            Vector3 hit = Wire4 - toWire * 0.007f;
            Color accent = palette.wattsonAccent;
            var beam = Polyline("Beam", group, Glow("Glow_Beam", Hdr(accent, 6f)), 0.035f, false, true, Hover + right * 0.3f + Vector3.up * 0.08f, hit);
            beam.widthCurve = AnimationCurve.Linear(0, 1f, 1, 0.15f);
            Prim(PrimitiveType.Quad, "Beam Hit", group, hit - toWire * 0.002f, Vector3.one * 0.02f, Glow("Glow_Lens", Hdr(accent, 2.5f))).transform.rotation = Quaternion.LookRotation(toWire);
            Emitter("Shimmer", group, Wire4, Glow("Glow_Shimmer", Hdr(palette.current, 4f)), ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                main.startSpeed = 0.03f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.022f);
                var emission = ps.emission;
                emission.rateOverTime = 70;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(half * 2, 0.012f, 0.012f);
            });
            WireFx("race into wire surface", hit, group).transform.rotation = Quaternion.LookRotation(-toWire);

            var focus = Marker("Macro Wire Point", Wire4).transform;
            Vcam("Macro A", Wire4 - toWire * 0.16f, Wire4, 82, focus, lensDive);
            Vcam("Macro B", Wire4 - toWire * 0.1f, Wire4, 26, focus, lensDive);
        }

        protected override void Beats()
        {
            // One coherent entry: a short push into the wire's surface, the whoosh, white, and the inside with its own
            // hum and a darker pad (the study's room tone goes with the flash).
            Shot("Macro A");
            Show("[VFX] race into wire surface");
            Sfx("Enter");
            Visual("Macro copper wire under the Ohm-niscope beam; a short push in", 0.5);
            Shot("Macro B");
            Visual("Into the wire surface", 1.1);
            Cue("fade 1", "flash to white");
            Wait(0.5);
            Shot("Wire Inside", cut: true);
            Show("Wire Section");
            Hide("[VFX] race into wire surface");
            Hide("Macro");
            Ambience("WireHum");
            Music("PadWire");
            Cue("fade 0", "fade in inside the wire; electrons in random motion, no drift");
            Gesture(WattsonController.Gesture.Idle, moveTo: "Platform Point");
            Visual("Wattson glides in to his spot on the floor, lower-left", 1.5);

            // A: where we are, what the particles are, and that nothing flows yet.
            Gesture(WattsonController.Gesture.Point, point: "Electrons Point");
            Line("S05_L01");
            Cue("callout show electrons");
            Line("S05_N01");
            Cue("wire.trails 1", "two tracer electrons leave short trails: zig-zags that go nowhere overall");
            Visual("Random motion: the trails zig-zag and go nowhere overall", 1.8);

            // B: the two ends, then the voltage between them, then the drift that follows.
            Cue("callout hideall");
            Gesture(WattsonController.Gesture.Point, point: "Card Terminal −");
            Show("Card Terminal −");
            Show("Card Terminal Label −");
            Sfx("Highlight");
            Line("S05_W02", 0.2);
            Gesture(WattsonController.Gesture.Point, point: "Card Terminal +");
            Show("Card Terminal +");
            Show("Card Terminal Label +");
            Sfx("Highlight");
            Line("S05_W03");
            Gesture(WattsonController.Gesture.Point, point: "Bracket");
            Show("Bracket");
            Cue("callout show voltage");
            Line("S05_N02", 0.1);
            Gate("S05_ApplyVoltage", "the learner taps the + terminal: the voltage is applied, and the field goes up");
            Shot("Wire Drift");
            Gesture(WattsonController.Gesture.RaiseGadget, gadget: true);
            Show("[VFX] field wave sweeps whole tunnel");
            Sfx("Highlight");
            Cue("wire.field 1", "field set up along the whole conduit at once; every electron starts drifting − → +, random motion kept", 0.3);
            Line("S05_N03");
            Hide("[VFX] field wave sweeps whole tunnel");
            Hide("Bracket");
            Gesture(WattsonController.Gesture.Idle);
            Show("Drift Arrow");
            Cue("callout show drift");
            Visual("The electrons keep jiggling and now all drift together; the arrow under the conduit says electron drift, − → +", 2.4);

            // C: current, counted at the section in the panel: one, two, three ... and the completed second (wide again:
            // Wattson points at the ring).
            Shot("Wire Inside");
            Gesture(WattsonController.Gesture.Point, point: "Cross Section");
            Show("Cross Section");
            Hide("Drift Arrow");
            Cue("callout hideall");
            Cue("counter 1", "the panel's counter: charges through the section this second, the one-second ring");
            Cue("wire.ticks 1", "a soft tick for every counted charge");
            Cue("wire.count 1");
            Line("S05_N04", 0.2);
            Visual("A full second completes: the panel shows that second's count", 1.6);
            Line("S05_L06");
            Line("S05_L07");
            Cue("callout show current");
            Line("S05_N05");

            Gesture(WattsonController.Gesture.Talking);
            Line("S05_L08");
            Gesture(WattsonController.Gesture.Idle);
            Line("S05_L09", 0.2);
            Cue("spotlight", "Ask Wattson pulses once");
            Wait(0.8);
            Cue("handoff", "SceneHandoffState: camera pose, fieldOn, drift speed, card positions");
            Handoff("Scene05Complete", cut: true, afterSeconds: 0.2);
        }
    }
}
