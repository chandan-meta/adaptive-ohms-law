using System.Linq;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 5: Journey Inside the Wire. Opens on Scene 4's last frame (the copper test wire on Ohm's desk under the
    // Ohm-niscope beam), dives through white into the electron space: carriers in random motion, then the field;
    // voltage and current named. Ends on the frame Scene 6 match-cuts from.
    public class Scene05Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene05";
        public override string SceneName => "Scene05_InsideTheWire";
        public override string Title => "Journey Inside the Wire";

        protected override void Dialogue()
        {
            L("S05_L01", "These are electric charges.", 2.2f);
            L("S05_L02", "But seeing charges isn't enough.", 2.4f);
            L("S05_L03", "The important question is...", 2.2f);
            L("S05_L04", "The battery creates a potential difference — voltage — between two points.", 5.0f);
            L("S05_L05", "That provides the electrical push that drives charge through the circuit.", 4.5f);
            L("S05_L06", "And when electric charge flows...", 2.4f);
            L("S05_L07", "...we call that electric current.", 2.4f);
            L("S05_L08", "And that's exactly the clue we need for your Christmas lights. The new battery changed the electrical conditions—but before we solve your mystery, we need to find out exactly how voltage and current are connected.", 12.0f);
            L("S05_L09", "Want to predict what we'll find?", 2.2f);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scene 4: the Christmas-lights mystery, Georg Ohm, the words voltage and current, the Ohm-niscope.",
                "Inside a metal the charge carriers are free electrons.",
                "Before a battery is connected the electrons move randomly in every direction with no net drift.",
                "A battery creates a potential difference (voltage) between two points; voltage is the electrical push. Symbol V, unit volt (V).",
                "When the battery is connected the electric field is set up along the whole circuit almost at once and all the electrons drift together, from the − terminal end toward the + terminal end.",
                "Charge crossing a section of the wire each second is electric current. Symbol I, unit ampere (A).",
                "Conventional current is taken from + to −; electrons drift from − to +.",
            },
            new[] { "How V and I are related", "V is proportional to I", "V = IR", "R = V / I", "Resistance", "Ohms", "The name 'Ohm's law'", "The answer to the Christmas-lights mystery" },
            ("Why were the charges moving before the battery?", "Good eye, detective. Free electrons in a metal are always jiggling about because of heat. They dart in every direction, so on average they go nowhere. The battery doesn't start them moving, it gives all of them a steady drift in one direction."),
            ("Which way do electrons move?", "Electrons carry negative charge, so they drift from the negative terminal end toward the positive terminal end. Scientists also talk about conventional current, which is taken to flow from positive to negative. Same circuit, two ways of describing it."),
            ("What is one ampere?", "One ampere means one coulomb of charge crossing a section of the wire every second, and one coulomb is about six billion billion electrons. Our counter is doing exactly that job: counting charge per second."));

        protected override void Stage()
        {
            // The macro beat is Scene 4's study with its own lighting, grouped so the flash to white takes it all away
            // (its window light would otherwise reach the electron space).
            var macro = Ref(Node("Macro"));
            Study().transform.SetParent(macro.transform, true);
            StudyLighting();
            scene.GetRootGameObjects().Last(g => g.name == "Lighting").transform.SetParent(macro.transform, true);
            MacroWire(macro.transform);

            // The electron space waits inactive: shown at the cut, it brings its own atmosphere, lights and post volume.
            var wire = WireStage(CopperTint, lattice: false, active: false);
            wire.fieldOn = false;
            director.wire = wire;
            Wattson(PlatformPoint + new Vector3(-3.5f, 0.3f, 0.6f), WireCamPos); // glides to Platform Point: lower-left, on the floor

            var question = Card("Question", "WHAT MAKES CHARGE FLOW?", Color.white, null, WireQuestionPos, WorldText.Cap.Question, false, WorldText.Wrap.Question);
            Face(question);
            WireBracket();
            // Concept words at the kit's markers (one depth, so both read at the concept-word size); sub-lines keep the tier ratio.
            float sub = WireConceptCap * WorldText.Cap.ConceptSub / WorldText.Cap.ConceptWord;
            Face(Card("Voltage", WorldText.Sub("VOLTAGE", "Symbol: V\nUnit: volt (V)", WireConceptCap, sub), palette.voltage, null, refs["Concept Left"].transform.position, WireConceptCap, false, WorldText.Wrap.Concept));
            Face(Card("Current", WorldText.Sub("CURRENT", "Symbol: I\nUnit: ampere (A)", WireConceptCap, sub), palette.current, null, refs["Concept Right"].transform.position, WireConceptCap, false, WorldText.Wrap.Concept));
        }

        // Scene 4's test wire between its brass posts at Wire4, the Ohm-niscope beam ending on it with a hot spot, the
        // amber shimmer of Scene 4's lock-on, and the dive streaks racing out of the surface at the lens. Both macro
        // cameras stand on Scene 4's dive axis (Wattson's hover point → the wire): "Macro A" is SH11b's pose, the frame
        // Scene 4 flashed out on; "Macro B" is the push-in, 10 cm off the wire.
        void MacroWire(Transform macro)
        {
            var copper = Lit("Copper", new Color(0.86f, 0.46f, 0.26f), 1f, 0.72f);
            var brass = Lit("Brass", new Color(0.83f, 0.66f, 0.3f), 1f, 0.68f);
            var wood = Lit("DarkWood", new Color(0.2f, 0.11f, 0.06f), 0f, 0.4f);
            var group = Node("Macro Wire", macro).transform;
            const float half = 0.21f;
            Prim(PrimitiveType.Cylinder, "Copper Wire", group, Wire4, new Vector3(0.007f, half, 0.007f), copper, new Vector3(0, 0, 90));
            foreach (float side in new[] { -1f, 1f })
            {
                var foot = new Vector3(Wire4.x + side * half, DeskY, Wire4.z);
                Prim(PrimitiveType.Cube, "Post Base", group, foot + Vector3.up * 0.008f, new Vector3(0.05f, 0.016f, 0.05f), wood);
                Prim(PrimitiveType.Cylinder, "Post", group, foot + Vector3.up * 0.05f, new Vector3(0.012f, 0.045f, 0.012f), brass);
                Prim(PrimitiveType.Sphere, "Post Cap", group, foot + Vector3.up * 0.098f, Vector3.one * 0.02f, brass);
            }

            Vector3 toWire = (Wire4 - Hover).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, toWire).normalized;
            Vector3 hit = Wire4 - toWire * 0.007f; // the near surface of the wire
            Color accent = palette.wattsonAccent;
            // The beam comes from the gadget in Wattson's raised right hand, behind the lens, and narrows to its focus,
            // so from the cameras on its axis it is a thin line converging on the wire, not a wash over the lens.
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
            WireFx("race into wire surface", hit, group).transform.rotation = Quaternion.LookRotation(-toWire); // out of the surface, at the lens

            var focus = Marker("Macro Wire Point", Wire4).transform;
            Vcam("Macro A", Wire4 - toWire * 0.16f, Wire4, 82, focus, lensDive);
            Vcam("Macro B", Wire4 - toWire * 0.1f, Wire4, 26, focus, lensDive); // 0.1 m is the Bokeh focus floor
        }

        protected override void Beats()
        {
            // From white (ModuleFlow fades in on entry) on Scene 4's last frame; the push-in blends Macro A into Macro B,
            // then white again and the cut into the electron space, which takes over the atmosphere as the study goes.
            Shot("Macro A");
            Show("[VFX] race into wire surface");
            Visual("Macro copper wire under the Ohm-niscope beam; camera pushes in", 0.7);
            Shot("Macro B");
            Visual("Race into the wire surface", 2.1);
            Cue("fade 1", "flash to white");
            Wait(0.7);
            Shot("Wire Inside", cut: true);
            Show("Wire Section");
            Hide("[VFX] race into wire surface");
            Hide("Macro");
            Cue("fade 0", "fade in inside the wire; electrons in random motion, no drift");
            Gesture(WattsonController.Gesture.Idle, moveTo: "Platform Point");
            Visual("Wattson glides in to his spot on the floor, lower-left", 2.5);

            Gesture(WattsonController.Gesture.Point, point: "Cross Section");
            Line("S05_L01");
            Line("S05_L02");
            Line("S05_L03");

            Show("[VFX] letters assemble from charges");
            Show("Card Question", 0.8);
            Visual("WHAT MAKES CHARGE FLOW? assembles from the charges above the conduit and holds", 3.5);
            Hide("Card Question");
            Hide("[VFX] letters assemble from charges");
            Show("Card Terminal −");
            Show("Card Terminal +", 0.4);
            Visual("Battery terminals light up at the two ends: − left, + right", 1.5);

            Gesture(WattsonController.Gesture.RaiseGadget, gadget: true);
            Show("[VFX] field wave sweeps whole tunnel");
            Cue("wire.field 1", "field set up along the whole tunnel; every carrier starts drifting − → +, jitter kept", 0.3);
            Visual("One ring of light sweeps the whole conduit", 1.2);
            Hide("[VFX] field wave sweeps whole tunnel");

            Gesture(WattsonController.Gesture.Point, point: "Bracket");
            Show("Bracket");
            Line("S05_L04");
            Line("S05_L05");
            Show("Card Voltage");
            Visual("VOLTAGE card: Symbol V, Unit volt (V)", 1.2);

            Gesture(WattsonController.Gesture.Point, point: "Cross Section");
            Hide("Bracket"); // the focus moves to the counting ring; Scene 6 match-cuts without a bracket
            Show("Cross Section");
            Line("S05_L06");
            Gesture(WattsonController.Gesture.Point, point: "Counter");
            Show("Counter");
            Cue("wire.count 1", "counter ticks per crossing with a one-second ring");
            Line("S05_L07");
            Show("Card Current");
            Visual("CURRENT card: Symbol I, Unit ampere (A)", 1.2);

            Gesture(WattsonController.Gesture.Talking);
            Line("S05_L08");
            Gesture(WattsonController.Gesture.Idle);
            Line("S05_L09", 0.2);
            Cue("spotlight", "Ask Wattson button plays a one-time spotlight pulse");
            Wait(1.2);
            Cue("handoff", "SceneHandoffState: camera pose, fieldOn, drift speed, card positions");
            Handoff("Scene05Complete", cut: true, afterSeconds: 0.2);
        }
    }
}
