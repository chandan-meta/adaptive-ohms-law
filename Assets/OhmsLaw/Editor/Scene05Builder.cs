using System.Linq;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 5: Journey Inside the Wire. Opens on Scene 4's last frame (no white: Scene 4 cuts; the copper test wire on
    // Ohm's desk under the Ohm-niscope beam), pushes into the wire's surface, a flash, and the dive carries on inside the
    // copper lattice cavern: the entry camera keeps moving onto the "Wire Inside" shot while the streaks still stream.
    // Then one idea at a time, each explained in the panel:
    //   A  free electrons in random motion, no overall flow;
    //   B  the two ends named (− and +) on a wide shot of both terminals, the voltage between them (the learner applies
    //      it), the drift that follows (random motion kept);
    //   C  current: the charge crossing one section each second, counted live in the panel; the question for Scene 6.
    // Ends on the frame Scene 6 match-cuts from (the prediction).
    public class Scene05Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene05";
        public override string SceneName => "Scene05_InsideTheWire";
        public override string Title => "Journey Inside the Wire";

        protected override void Dialogue()
        {
            // v5 "Live Wire" narration (Docs/Polish/V5_PLAN.md): the beats in order.
            L("S05_V01", "Now you are inside the wire.", 2.2f, proposed: true);
            L("S05_V02", "These glowing charges are electrons, and every one of them is negative.", 4.4f, proposed: true);
            L("S05_V03", "Right now they just jiggle about at random. Lots of motion, but no overall flow in any direction.", 5.6f, proposed: true);
            L("S05_V04", "Let's connect a battery across the wire. This end goes to the negative terminal.", 5.0f, proposed: true);
            L("S05_V05", "And that end to the positive terminal.", 2.4f, proposed: true);
            L("S05_V06", "Between the two ends there is now a difference in electric potential. That push across the wire is the voltage, written V.", 7.2f, proposed: true);
            L("S05_V07", "Watch! The charges still jiggle, but now they all flow one way, from negative to positive. That steady flow is electron drift.", 7.4f, proposed: true); // two caption lines: the 3-line version paged mid-phrase
            L("S05_V08", "Now pick one cross-section of the wire, this ring. Count how much charge passes through this area in one second.", 6.6f, proposed: true);
            L("S05_V09", "This much charge, from negative to positive, through this area, every second. That rate of flow is the current, written I.", 7.4f, proposed: true);
            L("S05_V10", "Voltage pushes. Current flows. So, detective: how do you think the two are related?", 5.4f, proposed: true);
            // The handoff to the prediction (Scene 6).
            L("S05_L08", "And that's exactly the clue we need for your Christmas lights. The new battery changed the electrical conditions—but before we solve your mystery, we need to find out exactly how voltage and current are connected.", 12.0f);
            L("S05_L09", "Want to predict what we'll find?", 2.2f);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scene 4: the Christmas-lights mystery, Georg Ohm, the words voltage and current, the Ohm-niscope.",
                "Inside a metal the charge carriers are free electrons, every one negative. The warm glow all round the conduit is the copper's own atoms: the electrons move between them.",
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
            // The macro beat is Scene 4's study with its own lighting, grouped so the cut to the inside takes it all away.
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
            // The potential difference spans the wire: ticks at its two ends, just clear of the − and + blocks' faces (x −4.5 and
            // +4.6; the right end stops short of the "+ positive" plate in the "Wire Terminals" shot).
            WireBracket(WireOrigin + new Vector3(-4f, 0, -TunnelRadius - 0.04f), WireOrigin + new Vector3(4f, 0, -TunnelRadius - 0.04f));
            // v3 cinematic pass: a medium on the conduit for the drift (the push-in lands as the field goes up). Cause and
            // effect share the free frame: the drift arrow from its tail, the + terminal it drifts toward and its
            // "+ positive" label all sit left of the dock (the old, tighter lens put them under it).
            Vcam("Wire Drift", WireOrigin + new Vector3(-1.4f, -0.45f, -7.2f), WireOrigin + new Vector3(1.8f, -0.3f, 0f), 56f, refs["Electrons Point"].transform, lensMedium); // focus on the stream (Wattson is beside and behind this lens)
            // The dive's entry: 3 m behind "Wire Inside" on its own axis; "wire.dive" moves it onto the inside pose. Its streaks
            // stream out of the cavern past the lens for the first beat (world-placed, shown and hidden by the cues; the macro
            // streaks stay with the desk). The tracking lens: motion blur for the push, no chromatic aberration (the dive lens
            // split Wattson into RGB copies through V01, since the blend onto "Wire Inside" ends only after it starts).
            Vector3 forward = (WireCamLook - WireCamPos).normalized, enter = WireCamPos - forward * 3f;
            Vcam("Wire Enter", enter, WireCamLook, WireFov, refs["Platform Point"].transform, lensTracking);
            WireFx("race into wire interior", enter + forward * 2.5f).transform.rotation = Quaternion.LookRotation(-forward);
            Wattson(PlatformPoint + new Vector3(-3.5f, 0.3f, 0.6f), WireCamPos); // glides to Platform Point: lower-left, on the floor

            // The panel's notes for this scene (the cues name them).
            Note("electrons", "Free electrons", "Inside a metal some electrons are free to move. Before a voltage is applied they jiggle randomly: no overall flow in any direction.");
            Note("voltage", "Voltage (V)", "The difference in electric potential between two points. Applying it across the wire creates the push on the electrons. Unit: volt (V).");
            Note("drift", "Electron drift", "The free electrons keep jiggling, and now they all drift from the − end toward the + end. Conventional current is drawn the other way: + → −.");
            Note("current", "Current (I)", "The charge passing a section each second: I = ΔQ ÷ Δt. Unit: ampere (A).");
        }

        // Scene 4's desk as it was at the cut: the same 1826 apparatus, the copper wire on its posts, the beam's hot
        // spot, the shimmer of the lock-on, and the streaks racing out of the surface. Both macro cameras stand on
        // Scene 4's dive axis ("Macro A" is SH11b Dive's end pose); the push from A to B is short.
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
            // One continuous dive: Scene 4 cut here on the macro wire; the push into its surface with the streaks, a
            // flash, and under it the cut to the cavern's entry camera, which keeps pushing onto "Wire Inside" while
            // the streaks still stream and the flash clears; the hum from the cut, the darker pad with it.
            Shot("Macro A");
            Show("[VFX] race into wire surface");
            Sfx("Enter");
            Visual("Macro copper wire under the Ohm-niscope beam; the push in begins", 0.5);
            Shot("Macro B");
            Visual("Into the wire surface", 1.1);
            Cue("fade 1 0.15", "a flash of white (0.15 s)");
            Wait(0.2);
            Shot("Wire Enter", cut: true);
            Show("Wire Section");
            Hide("Macro");
            Hide("[VFX] race into wire surface");
            Show("[VFX] race into wire interior");
            Ambience("WireHum");
            Music("PadWire");
            Cue("wire.dive 1.8", "the entry camera keeps moving: 3 m on, easing out onto Wire Inside", 0.05);
            Cue("fade 0 0.5", "the flash clears on the cavern: warm copper atoms all round, the conduit ahead", 0.05);
            Hide("[VFX] race into wire interior", 0.6);
            Shot("Wire Inside", offset: 1.8);
            Gesture(WattsonController.Gesture.Idle, moveTo: "Platform Point");
            Visual("The dive settles inside the copper lattice; Wattson glides in to his spot on the floor, lower-left", 1.2);
            Line("S05_V01");

            // A: what the particles are, and that nothing flows yet.
            Gesture(WattsonController.Gesture.Point, point: "Electrons Point");
            Line("S05_V02");
            Cue("callout show electrons");
            Cue("wire.trails 1", "two tracer electrons leave short trails: zig-zags that go nowhere overall");
            Line("S05_V03");
            Visual("Random motion: the trails zig-zag and go nowhere overall", 1.2);

            // B: both terminals in one wide shot as they are named and lit, the voltage between them (the learner
            // applies it), then the drift that follows.
            Cue("callout hideall");
            Shot("Wire Terminals");
            Gesture(WattsonController.Gesture.Point, point: "Card Terminal −");
            Show("Card Terminal −");
            Show("Card Terminal Label −");
            Sfx("Highlight");
            Line("S05_V04", 0.2);
            Gesture(WattsonController.Gesture.Point, point: "Card Terminal +");
            Show("Card Terminal +");
            Show("Card Terminal Label +");
            Sfx("Highlight");
            Line("S05_V05");
            Gesture(WattsonController.Gesture.Point, point: "Bracket");
            Show("Bracket");
            Cue("callout show voltage");
            Line("S05_V06", 0.1);
            Gate("S05_ApplyVoltage", "the learner taps the + terminal: the voltage is applied, and the field goes up");
            Hide("Bracket"); // it spans the whole wire: in the drift medium it would run off the frame under the arrow
            Shot("Wire Drift");
            Gesture(WattsonController.Gesture.RaiseGadget, gadget: true);
            Show("[VFX] field wave sweeps whole tunnel");
            Sfx("Highlight");
            Cue("wire.field 1", "field set up along the whole conduit at once; every electron starts drifting − → +, random motion kept", 0.3);
            Show("Drift Arrow", 0.8);
            Cue("callout show drift", "", 0.8);
            Line("S05_V07");
            Hide("[VFX] field wave sweeps whole tunnel");
            Gesture(WattsonController.Gesture.Idle);
            Visual("The electrons keep jiggling and now all drift together; the arrow under the conduit says electron drift, − → +", 1.2);

            // C: current, counted at the section in the panel, and the completed second (wide again: Wattson points at
            // the ring); then the question Scene 6 asks.
            Shot("Wire Inside");
            Gesture(WattsonController.Gesture.Point, point: "Cross Section");
            Show("Cross Section");
            Hide("Drift Arrow");
            Cue("callout hideall");
            Cue("counter 1", "the panel's counter: charges through the section this second, the one-second ring");
            Cue("wire.ticks 1", "a soft tick for every counted charge");
            Cue("wire.count 1");
            Visual("The wide settles (1 s blend) with the ring lit, so the line and the labels start on the settled frame", 1.0);
            Line("S05_V08", 0.2);
            Visual("A full second completes: the panel shows that second's count", 1.2);
            Cue("callout show current");
            Line("S05_V09");

            Gesture(WattsonController.Gesture.Talking);
            Line("S05_L08");
            Gesture(WattsonController.Gesture.Idle);
            Line("S05_V10", 0.2);
            Line("S05_L09", 0.2);
            Cue("spotlight", "Ask Wattson pulses once");
            Wait(0.8);
            Cue("handoff", "SceneHandoffState: camera pose, fieldOn, drift speed, card positions");
            Handoff("Scene05Complete", cut: true, afterSeconds: 0.2);
        }
    }
}
