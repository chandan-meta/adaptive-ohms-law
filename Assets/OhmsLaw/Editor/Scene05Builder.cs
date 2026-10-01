using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 5: Journey Inside the Wire. Carriers in random motion, then the field; voltage and current named.
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
            // Macro shot of the copper wire on Ohm's table, before the dive.
            var macro = Ref(Node("Macro Wire", null, new Vector3(-100, 0, 0)));
            Prop(PrimitiveType.Cylinder, "copper wire (macro)", macro.transform, Vector3.zero, new Vector3(0.6f, 6f, 0.6f), "Macro copper wire surface from Scene 4's table", BlockoutAssets.Mat("CopperProp", CopperTint), new Vector3(0, 0, 90));
            Vfx("race into wire surface", macro.transform, new Vector3(0, 1.2f, -1.5f), 1.6f);
            Cam("Macro A", new Vector3(-100, 0.6f, -4f), new Vector3(-100, 0, 0), 40);
            Cam("Macro B", new Vector3(-100, 0.2f, -1.2f), new Vector3(-100, 0, 0), 55);

            var wire = WireStage(CopperTint, lattice: false);
            wire.fieldOn = false;
            director.wire = wire;
            Wattson(PlatformPos + new Vector3(-4f, 1.1f, 0.4f), WireCamPos); // glides onto the platform at the start
            Placeholder(refs["Wattson"], "Wattson and the four kids (Kabir, Manav, Ishaan, Meera) on the platform; the kids are the camera's POV in this blockout");

            Vector3 o = WireOrigin;
            var question = Card("Question", "WHAT MAKES CHARGE FLOW?", Color.white, null, o + new Vector3(2.5f, 0.9f, 1.2f), 0.45f, false, 6f);
            Face(question);
            Vfx("letters assemble from charges", null, o + new Vector3(2.5f, -0.4f, 1.2f), 2f).transform.rotation = question.transform.rotation;
            Vfx("field wave sweeps whole tunnel", null, o + new Vector3(0.5f, 1.6f, 0.6f), 2.2f).transform.rotation = question.transform.rotation;
            var bracket = Node("Bracket", null, o + new Vector3(0, 0, 0));
            Prim(PrimitiveType.Sphere, "Point A", bracket.transform, new Vector3(-3f, -0.6f, 1.6f), Vector3.one * 0.3f, BlockoutAssets.Mat("Highlight", new Color(1f, 0.85f, 0.3f, 0.35f)));
            Prim(PrimitiveType.Sphere, "Point B", bracket.transform, new Vector3(3f, -0.6f, 1.6f), Vector3.one * 0.3f, BlockoutAssets.Mat("Highlight", new Color(1f, 0.85f, 0.3f, 0.35f)));
            Prim(PrimitiveType.Cube, "Bracket Bar", bracket.transform, new Vector3(0, -0.2f, 1.6f), new Vector3(6f, 0.03f, 0.03f), BlockoutAssets.Mat("Highlight", new Color(1f, 0.85f, 0.3f, 0.35f)));
            Placeholder(bracket, "Two points along the tunnel light up with a bracket between them (potential difference)");
            bracket.SetActive(false);
            Ref(bracket);
            Face(Card("Voltage", "VOLTAGE\nSymbol: V\nUnit: volt (V)", palette.voltage, null, o + new Vector3(-1.2f, 1.3f, 1.8f), 0.3f, false, 4f));
            Face(Card("Current", "CURRENT\nSymbol: I\nUnit: ampere (A)", palette.current, null, o + new Vector3(4.2f, 1.0f, 0.4f), 0.3f, false, 4f));
        }

        protected override void Beats()
        {
            // Fade in from white (ModuleFlow does it on entry) on the macro wire, push in, then white again.
            Shot("Macro A");
            Visual("Macro copper wire; camera pushes in", 0.2);
            Shot("Macro B");
            Show("[VFX] race into wire surface");
            Visual("Race into the wire surface", 2.6);
            Cue("fade 1", "flash to white");
            Wait(0.7);
            Shot("Wire Inside", cut: true);
            Hide("Macro Wire");
            Cue("fade 0", "fade in inside the tunnel; carriers in random motion, no drift");
            Gesture(WattsonController.Gesture.Idle, moveTo: "Platform Point");
            Visual("Platform with Wattson and the kids glides in", 2.5);

            Gesture(WattsonController.Gesture.Point, point: "Cross Section");
            Line("S05_L01");
            Line("S05_L02");
            Line("S05_L03");

            Show("Card Question");
            Show("[VFX] letters assemble from charges");
            Visual("Giant WHAT MAKES CHARGE FLOW? card holds", 3.0);
            Hide("Card Question");
            Hide("[VFX] letters assemble from charges");
            Show("Card Terminal −");
            Show("Card Terminal +");
            Visual("Battery terminals appear at the two ends: + and −", 1.5);

            Gesture(WattsonController.Gesture.RaiseGadget, gadget: true);
            Show("[VFX] field wave sweeps whole tunnel");
            Cue("wire.field 1", "field set up along the whole tunnel; every carrier starts drifting − → +, jitter kept", 0.3);
            Visual("Field wave sweeps the whole tunnel", 1.2);
            Hide("[VFX] field wave sweeps whole tunnel");

            Gesture(WattsonController.Gesture.Point, point: "Bracket");
            Show("Bracket");
            Line("S05_L04");
            Line("S05_L05");
            Show("Card Voltage");
            Visual("VOLTAGE card: Symbol V, Unit volt (V)", 1.2);

            Gesture(WattsonController.Gesture.Point, point: "Cross Section");
            Show("Cross Section");
            Line("S05_L06");
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
