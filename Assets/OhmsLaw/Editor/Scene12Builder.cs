using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 12: Resistance, without leaving the laboratory. The camera pulls back to the physical test wire and the
    // panel opens a magnified comparison: two conductors of different materials at the same voltage, length, thickness
    // and temperature; one lets more current through, the other opposes the flow more. Then the learner's own data: the
    // V / I column fills, the ratio is the same every time, V ∝ I → V = kI → V/I = k → k is the resistance R → V = IR;
    // the same answer from the graph's slope (rise over run); then the ohm: R, the unit, Ω, 1 Ω = 1 V / 1 A.
    public class Scene12Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene12";
        public override string SceneName => "Scene12_Resistance";
        public override string Title => "Discover Resistance";

        protected override void Dialogue()
        {
            L("S12_L03", "But if voltage pushes charge...why doesn't charge move through every conductor equally easily?", 5.5f);
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
            L("S12_N01", "Keep the voltage the same, but change the wire material. This one allows more current. The other opposes the flow more.", 6.5f, proposed: true);
            L("S12_N02", "Written with this symbol: omega.", 2.2f, proposed: true);
            L("S12_N03", "Our nichrome wire needs five volts for one ampere, so its resistance is five ohms.", 4.4f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 11: voltage (V, volt), current (I, ampere), the five readings, V ∝ I for the nichrome wire at constant temperature, the straight-line graph through the origin.",
                "The panel compared two wires of the same length, thickness and temperature at the same voltage: copper lets more current through, nichrome opposes the flow more. Resistance limits the current; the charges are not destroyed.",
                "The material of a conductor opposes the flow of current. That opposition is electrical resistance, symbol R.",
                "For the nichrome wire XY, V/I is the same for every reading (1.0 ÷ 0.20 = 5, 2.0 ÷ 0.40 = 5, ...). V ∝ I means V = kI, so V/I = k, and that constant k is the resistance R: V = IR.",
                "The slope of the V-I graph (V vertical, I horizontal) is ΔV/ΔI = 2.0 V ÷ 0.40 A = 5, the resistance.",
                "Resistance is measured in ohms (Ω). 1 Ω = 1 V / 1 A: one ohm when one volt produces one ampere. This wire: 5 Ω.",
            },
            new[] { "The name 'Ohm's law' and V = IR as the named law (Scene 13)", "The Christmas-lights answer" },
            ("Why is V divided by I always 5?", "Because this nichrome wire always opposes current by the same amount. Push it with one volt and you get 0.2 ampere; push it with two volts and you get 0.4 ampere. Volts divided by amperes comes out at 5 every single time, and that constant is the wire's resistance."),
            ("What is one ohm?", "One ohm is the resistance of a conductor when a potential difference of one volt across it produces a current of one ampere through it. Our nichrome wire needs five volts for one ampere, so its resistance is five ohms."),
            ("Does a thicker wire have more resistance?", "Careful, detective: it is the other way round. A thicker wire gives the charges more room to flow, so it has less resistance. The comparison kept the length and thickness the same on purpose, so only the material differed."));

        protected override void Stage()
        {
            ClassroomSet(modernBench: true, audience: false);
            director.matchCutFrom = "Scene11_Graph";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            director.rig.showPath = false;
            Wattson(Scene10Builder.SourceSide, Scene10Builder.StudyCamC);
            Vcam("Bench Study", Scene10Builder.StudyCamC, Scene10Builder.StudyLookC, Scene10Builder.StudyFov, refs["Bench Point"].transform, lensMedium);
            // The pull-back to the physical test wire: a little wider and lower, the wire XY centred in the free frame, the
            // whole circuit from the source to the key inside it even with the wide dock of the comparison.
            Vcam("Wire Study", new Vector3(0f, 1.15f, -2.2f), new Vector3(0f, 0.85f, -0.1f), 40f, refs["NICHROME WIRE — XY"].transform, lensMedium);
            Note("resistance", "Resistance (R)", "How much a conductor opposes the flow of current. Same voltage, less current = more resistance. The charges are not lost: they just get through more slowly.");
        }

        protected override void Beats()
        {
            // Match-cut from Scene 11: the same data and V ∝ I in the panel.
            Shot("Bench Study");
            Ambience("RoomTone");
            Music("PadLab");
            Cue("table.rows 5"); Cue("readings 1"); Cue("panel Table 0", "as Scene 11 left it: the graph and V ∝ I", 0.05);
            Cue("graph.axes 1"); Cue("graph.plot 5"); Cue("graph.line 2");
            Reveal("V ∝ I", "directly proportional, for this wire at constant temperature");
            Gesture(WattsonController.Gesture.Thinking);
            Wait(0.3);
            Line("S12_L03");

            // The magnified comparison, opened beside the physical wire. Wattson points from beside the voltmeter, clear of
            // the meters; their plates step aside while copper and nichrome are compared (the readings card keeps V and I,
            // and a fixed 1.00 A in the room would argue with "copper: MORE current").
            Shot("Wire Study");
            Cue("rig.readouts 0");
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY", moveTo: "Voltmeter Point");
            Show("Wire Highlight");
            Sfx("Highlight");
            Visual("The camera pulls back to the test wire; it highlights", 1.0);
            Cue("formula.clear");
            Cue("panel Graph 0", "the graph steps aside for the comparison");
            Cue("inset 1", "the magnified comparison opens in the panel: copper above, nichrome below, same voltage");
            Gate("S12_SwapWire", "copper shows alone; the learner swaps in nichrome: same voltage, less current");
            Line("S12_N01");
            Cue("callout show resistance");
            Line("S12_L05");
            Hide("Wire Highlight");
            Visual("The comparison holds a moment", 1.2);
            Cue("inset 0");
            Cue("callout hideall");

            // Back to the learner's own data: the V / I column.
            Shot("Bench Study");
            Cue("rig.readouts 1");
            Gesture(WattsonController.Gesture.Point, point: "Panel Point", moveTo: "Tray Point");
            Line("S12_L06", 0.2);
            Line("S12_L07");
            Cue("panel Table 1", "the learner's table returns, on its own");
            Cue("table.ratio 1", "a new column V / I appears");
            Visual("V / I column appears", 0.6);
            Cue("table.work 0", "1.0 ÷ 0.20 = 5"); Visual("", 0.9);
            Cue("table.work 1", "2.0 ÷ 0.40 = 5"); Visual("", 0.9);
            Cue("table.work 2", "3.0 ÷ 0.60 = 5"); Visual("", 0.9);
            Cue("table.result 3", "5"); Cue("table.result 4", "5", 0.3);
            Visual("the last two fill", 0.7);
            Cue("table.glow 2 1", "the whole V/I column glows");
            Line("S12_L08", 0.2);
            Formula("V ∝ I");
            Formula("V = k I", 0.5);                                   // staggered: cues at one Timeline time fire in no fixed order
            Formula("V ÷ I = k   (the same 5 every time)", 1.0);
            Line("S12_L09", 0.2);
            Formula("k = R   (the resistance)");
            Line("S12_L10");
            Formula("V = I × R");
            Visual("The reasoning, one line at a time", 0.8);

            // The same answer from the graph: rise over run between two of the learner's points.
            Cue("table.glow 2 0");
            Cue("panel Table 0"); Cue("panel Graph 1", "the graph takes the table's place for the slope");
            Line("S12_L11");
            Cue("graph.triangle 0 2", "rise and run between (0.20, 1.0) and (0.60, 3.0): ΔV = 2.0 V, ΔI = 0.40 A");
            Line("S12_L12", 0.2);
            Formula("slope = ΔV ÷ ΔI = 2.0 V ÷ 0.40 A = 5");
            Line("S12_L13");

            // The ohm.
            Cue("formula.clear");
            Reveal("Ω", "the ohm: the unit of resistance (R)");
            Sfx("Omega");
            Gesture(WattsonController.Gesture.Greet);
            Line("S12_L14");
            Line("S12_N02", 0.2);
            Formula("1 Ω = 1 V ÷ 1 A");
            Line("S12_L15", 0.2);
            Formula("this wire:  5.0 V ÷ 1.00 A = 5.0 Ω");
            Cue("readings.r 1", "R and the ohm are taught: the readings show R");
            Line("S12_N03", 0.3);
            Cue("handoff");
            Handoff("Scene12Complete", cut: true, afterSeconds: 0.3);
        }
    }
}
