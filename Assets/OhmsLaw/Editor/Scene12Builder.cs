using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 12: Discover Resistance. Inside the nichrome wire, then V/I is constant, slope = R, ohms.
    public class Scene12Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene12";
        public override string SceneName => "Scene12_Resistance";
        public override string Title => "Discover Resistance";

        protected override void Dialogue()
        {
            L("S12_L01", "We've seen what pushes charge...", 2.4f);
            L("S12_L02", "...and we've measured how much charge flows...", 2.8f);
            L("S12_L03", "But if voltage pushes charge...why doesn't charge move through every conductor equally easily?", 5.5f);
            L("S12_L04", "The material itself opposes the flow of current.", 3.0f);
            L("S12_L05", "That opposition to the flow of electric current is called electrical resistance.", 4.5f);
            L("S12_L06", "And the best part?", 1.5f);
            L("S12_L07", "You've already measured it.", 2.0f);
            L("S12_L08", "The value of V/I is the same every time.", 3.0f);
            L("S12_L09", "This constant tells us how strongly the conductor opposes current.", 3.8f);
            L("S12_L10", "We call that constant value resistance of the nichrome wire XY.", 3.8f);
            L("S12_L11", "And remember the straight line you made in the graph?", 3.2f);
            L("S12_L12", "Every straight line has a slope.", 2.2f);
            L("S12_L13", "So for a V-versus-I graph, the slope tells us the resistance of the nichrome wire XY.", 4.5f);
            L("S12_L14", "Resistance is measured in ohms.", 2.4f);
            L("S12_L15", "A conductor has a resistance of one ohm when a potential difference of one volt produces a current of one ampere.", 6.5f);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 11: voltage (V, volt), current (I, ampere), the five readings, V ∝ I for the nichrome wire at constant temperature, the straight-line graph through the origin.",
                "The material of a conductor opposes the flow of current. That opposition is electrical resistance, symbol R.",
                "For the nichrome wire XY, V/I is the same for every reading (1.0 ÷ 0.20 = 5, 2.0 ÷ 0.40 = 5, ...). R = V / I.",
                "The slope of the V-I graph, ΔV/ΔI, is the resistance: 2.0 V ÷ 0.40 A = 5.",
                "Resistance is measured in ohms (Ω). 1 Ω = 1 V / 1 A: one ohm when one volt produces one ampere.",
                "Inside the wire, the charge carriers are impeded by the lattice of metal ions; the narrowing tunnel is only a metaphor for that opposition.",
            },
            new[] { "The name 'Ohm's law' and V = IR as the named law (Scene 13)", "The Christmas-lights answer" },
            ("Why is V divided by I always 5?", "Because this nichrome wire always opposes current by the same amount. Push it with one volt and you get 0.2 ampere; push it with two volts and you get 0.4 ampere. Volts divided by amperes comes out at 5 every single time, and that constant is the wire's resistance."),
            ("What is one ohm?", "One ohm is the resistance of a conductor when a potential difference of one volt across it produces a current of one ampere through it. Our nichrome wire needs five volts for one ampere, so its resistance is five ohms."),
            ("Does a thicker wire have more resistance?", "Careful, detective: it is the other way round. A thicker wire gives the charges more room to flow, so it has less resistance. The narrowing tunnel you saw was only a picture of the material opposing the flow, not a real change in the wire's thickness."));

        protected override void Stage()
        {
            LabGreybox(modernBench: true);
            Ref(Node("Lab")).SetActive(true);
            // Everything in the lab is grouped so the dive can hide it and the "lab" cue can bring it back.
            foreach (var name in new[] { "Floor Grid", "Back Wall", "Bench", "Old Table" })
                if (refs.TryGetValue(name, out var go)) go.transform.SetParent(refs["Lab"].transform, true);
            KidsAt(new Vector3(0.2f, 0, -2.2f), Vector3.forward);
            var wattson = Wattson(WattsonHome, BenchWideCam);
            director.matchCutFrom = "Scene11_Graph";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            refs["Circuit Rig"].transform.SetParent(refs["Lab"].transform, true);
            Boards(withTable: true, withGraph: true, unitsInTable: true);

            var wire = WireStage(NichromeTint, lattice: true, active: false);
            wire.fieldOn = true;
            wire.latticeVisibility = 0;
            director.wire = wire;
            Vector3 o = WireOrigin;
            Face(Card("Voltage", "VOLTAGE", palette.voltage, null, o + new Vector3(-1.5f, 1.4f, 1.8f), 0.4f));
            Face(Card("Current", "CURRENT", palette.current, null, o + new Vector3(4.0f, 1.1f, 0.5f), 0.4f));
            Face(Card("Resistance", "RESISTANCE", palette.resistance, null, o + new Vector3(1.3f, 0.9f, 1.5f), 0.5f));
            Face(Card("Symbol R", "Symbol: R", palette.resistance, null, o + new Vector3(1.3f, 0.2f, 1.5f), 0.25f));
            Face(Vfx("carriers impeded by lattice (slow, bounce)", null, o + new Vector3(0.3f, -1.2f, 1.4f), 2.4f));
            Face(Vfx("RESISTANCE word forms from the lattice", null, o + new Vector3(1.3f, 1.6f, 1.5f), 2.2f));
            Marker("Wire Boards Anchor", o + new Vector3(0.5f, -0.4f, 2.0f));
            Cam("Dive", new Vector3(0, BenchTop + 0.25f, -0.6f), new Vector3(0, BenchTop + 0.1f, -0.25f), 70);

            // Data cards beside the boards.
            Card("Constant", "V / I = CONSTANT", palette.resistance, null, new Vector3(1.0f, 2.0f, 0.1f), 0.2f);
            Card("Constant Word", "CONSTANT", palette.resistance, null, new Vector3(1.0f, 2.0f, 0.1f), 0.22f);
            Card("Resistance R", "RESISTANCE — R", palette.resistance, null, new Vector3(1.0f, 2.0f, 0.1f), 0.22f);
            Card("Formula", "R = V / I", palette.resistance, null, new Vector3(1.0f, 2.0f, 0.1f), 0.26f);
            Card("Slope", "Slope = ΔV / ΔI", palette.resistance, null, new Vector3(-0.1f, 2.75f, 0.35f), 0.2f);
            Card("Ohm Symbol", "Ω", palette.resistance, null, new Vector3(0.5f, 2.3f, 0.2f), 0.9f);
            Card("One Ohm", "1 Ω = 1 V / 1 A", palette.resistance, null, new Vector3(0.5f, 1.6f, 0.2f), 0.24f);
        }

        protected override void Beats()
        {
            Shot("Boards");
            Cue("table.rows 5", "match-cut from Scene 11: table and graph visible");
            Cue("graph.axes 1"); Cue("graph.plot 5"); Cue("graph.line 1");
            Gesture(WattsonController.Gesture.Aim, look: "NICHROME WIRE — XY", point: "NICHROME WIRE — XY", gadget: true);
            Visual("Wattson aims the Ohm-niscope at the nichrome wire", 1.0);
            Shot("Dive");
            Visual("Camera dives into the nichrome wire XY", 1.4);
            Cue("fade 1", "flash");
            Wait(0.4);
            Shot("Wire Inside", cut: true);
            Show("Wire Section"); Hide("Lab");
            Cue("wire.field 1"); Cue("wire.lattice 1", "the lattice of ions becomes visible (Scene 12 only)");
            Cue("fade 0");
            Gesture(WattsonController.Gesture.Idle, moveTo: "Platform Point");
            Visual("Inside the nichrome wire: carriers drift, the lattice fades in", 1.5);

            Gesture(WattsonController.Gesture.Point, point: "Card Terminal +");
            Show("Card Voltage");
            Line("S12_L01");
            Gesture(WattsonController.Gesture.Point, point: "Cross Section");
            Show("Card Current");
            Line("S12_L02");
            Gesture(WattsonController.Gesture.Thinking);
            Line("S12_L03");

            Show("[VFX] carriers impeded by lattice (slow, bounce)");
            Cue("wire.impede 0.8", "carriers slow and bounce off the lattice");
            Cue("wire.radius 1.6", "tunnel narrows (narrowingMetaphor toggle)");
            Gesture(WattsonController.Gesture.Point, point: "Lattice Ions");
            Line("S12_L04");
            Hide("[VFX] carriers impeded by lattice (slow, bounce)");
            Show("[VFX] RESISTANCE word forms from the lattice");
            Show("Card Resistance", 0.8);
            Line("S12_L05");
            Hide("[VFX] RESISTANCE word forms from the lattice");
            Show("Card Symbol R");
            Visual("Symbol: R card", 1.0);

            Gesture(WattsonController.Gesture.Greet);
            Line("S12_L06", 0.2);
            Cue("fade 1", "cross-fade back to the lab bench (tableInWire keeps the boards inside the wire instead)");
            Wait(0.5);
            Cue("lab", "back at the bench: graph and table side by side");
            Cue("wire.radius 2.5");
            Shot("Boards", cut: true);
            Gesture(WattsonController.Gesture.Point, point: "Observation Table", moveTo: "Boards Point");
            Cue("fade 0");
            Wait(0.6);
            Line("S12_L07");
            Cue("table.ratio 1", "a new column V / I appears");
            Visual("V / I column appears", 0.8);
            for (int r = 0; r < 3; r++)
            {
                Cue("table.work " + r, $"Wattson works row {r + 1}: {new[] { "1.0 ÷ 0.20 = 5", "2.0 ÷ 0.40 = 5", "3.0 ÷ 0.60 = 5" }[r]}");
                Visual("", 1.2);
            }
            Cue("table.result 3", "5"); Cue("table.result 4", "5", 0.3);
            Visual("the last two fill rapidly", 0.7);
            Cue("table.glow 2 1", "the whole V/I column glows");
            Line("S12_L08", 0.2);
            Show("Card Constant");
            Visual("V / I = CONSTANT card", 0.8);
            Line("S12_L09", 0.2);
            Hide("Card Constant"); Show("Card Constant Word");
            Visual("CONSTANT", 0.8);
            Hide("Card Constant Word"); Show("Card Resistance R");
            Visual("CONSTANT morphs into RESISTANCE — R", 1.0);
            Hide("Card Resistance R"); Show("Card Formula");
            Visual("R = V / I", 0.8);
            Line("S12_L10");
            Gesture(WattsonController.Gesture.Point, point: "Graph");
            Line("S12_L11");
            Cue("graph.triangle 0 2", "triangle between (0.20, 1.0) and (0.60, 3.0): ΔV = 2.0 V, ΔI = 0.40 A");
            Show("Card Slope");
            Visual("Wattson draws the ΔV/ΔI triangle", 1.2);
            Line("S12_L12", 0.2);
            Gate("S12_Slope", "MCQ (proposed format): Slope = ΔV / ΔI = ? · 5 · 0.2 · 2.5 (correct 5). Wrong: shake, show 2.0 ÷ 0.40, re-ask once, then continue.");
            Gesture(WattsonController.Gesture.Talking);
            Line("S12_L13");
            Hide("Card Slope"); Hide("Card Formula");
            Show("Card Ohm Symbol");
            Line("S12_L14");
            Show("Card One Ohm");
            Line("S12_L15", 0.2);
            Cue("handoff");
            Handoff("Scene12Complete", cut: true, afterSeconds: 0.5);
        }
    }
}
