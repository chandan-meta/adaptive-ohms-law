using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 12: Resistance, without leaving the laboratory. The camera pulls back to the physical test wire and a 3D
    // pop-up card opens over the bench: two cut-away conductors, copper and nichrome, same voltage, length and thickness,
    // their free electrons threading a lattice of ions; copper lets them through, nichrome scatters them (WireSample).
    // A quick check names the one with the higher resistance. Then the learner's own data on the data stage: the V / I
    // column fills, the ratio is the same every time, V ∝ I → V = kI → V/I = k → k is the resistance R → V = IR; the same
    // answer from the graph's slope (rise over run); then the ohm, centre screen: R, the unit, Ω, 1 Ω = 1 V / 1 A.
    public class Scene12Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene12";
        public override string SceneName => "Scene12_Resistance";
        public override string Title => "Discover Resistance";

        // Wattson beside the 3D pop-up on Wire Study (x 1168-1294 of 1920: between the card's right edge and the dock), and
        // high on the right for the ohm plates on Bench Study (boots at y ≈ 288, above the wide "1 Ω = 1 V ÷ 1 A" plate;
        // x 1118-1244, left of the controls). Scene 13 opens where this one leaves him.
        static readonly Vector3 SampleSide = new Vector3(1.1f, 1.2f, 1.0f);
        public static readonly Vector3 RevealSide = new Vector3(1.0f, 1.58f, 1.0f);

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
            // Inside the metals, over the 3D pop-up, and the quick check.
            L("S12_R01", "Zoom in. Inside any metal the atoms sit in a lattice, and the free electrons have to thread their way through it.", 6.4f, proposed: true);
            L("S12_R02", "In copper the way is easy. The electrons slip past with few collisions, so the same push gives a big current.", 6.2f, proposed: true);
            L("S12_R03", "In nichrome the lattice gets in the way far more. The electrons bump, scatter, lose headway. Same push, less current.", 7.0f, proposed: true);
            L("S12_R04", "Every collision hands a little energy to the lattice as heat. That's why a nichrome wire warms up, and why a heater glows.", 6.8f, proposed: true);
            L("S12_QC", "Quick check, detective. Same voltage, less current. Which wire has the higher resistance?", 5.2f, proposed: true);
            L("S12_QC_R_If_S12_WhichResists_B", "Nichrome it is. More opposition, less current, higher resistance.", 4.0f, proposed: true);
            L("S12_QC_R_If_S12_WhichResists_A", "Careful: copper let more current through, so it opposes less. Nichrome has the higher resistance.", 5.4f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 11: voltage (V, volt), current (I, ampere), the five readings, V ∝ I for the nichrome wire at constant temperature, the straight-line graph through the origin.",
                "The 3D comparison showed two wires of the same length, thickness and temperature at the same voltage: inside a metal the atoms sit in a lattice and the free electrons thread through it; in copper they slip past with few collisions (big current), in nichrome they scatter and lose headway (less current). Each collision hands energy to the lattice as heat. Resistance limits the current; the charges are not destroyed.",
                "The material of a conductor opposes the flow of current. That opposition is electrical resistance, symbol R. Nichrome has the higher resistance.",
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
            MeterPlates(false); // the strip carries V and I: no plate reads through the hero, stage or pop-up glass
            Wattson(Scene10Builder.SourceSide, Scene10Builder.StudyCamC);
            Marker("Sample Side", SampleSide);
            Marker("Reveal Side", RevealSide);
            Vcam("Bench Study", Scene10Builder.StudyCamC, Scene10Builder.StudyLookC, Scene10Builder.StudyFov, refs["Bench Point"].transform, lensMedium);
            // The pull-back to the physical test wire: a little wider and lower, the wire XY centred in the free frame, the
            // whole circuit from the source to the key inside it even with the wide dock.
            Vcam("Wire Study", new Vector3(0f, 1.15f, -2.2f), new Vector3(0f, 0.85f, -0.1f), 40f, refs["NICHROME WIRE — XY"].transform, lensMedium);
            WireSampleStage(); // the copper / nichrome sample under the lab, rendered into the 3D pop-up
            Note("resistance", "Resistance (R)", "How much a conductor opposes the flow of current. Same voltage, less current = more resistance. The charges are not lost: they just get through more slowly.");
        }

        protected override void Beats()
        {
            // Match-cut from Scene 11: the same data in the dock and V ∝ I centre screen.
            Shot("Bench Study");
            Ambience("RoomTone");
            Music("BGM_Wattson_Magical_Discovery"); // audio: Scene 11's discovery theme continues (a no-op on the match-cut; the bed on a fresh load)
            Cue("table.rows 5"); Cue("readings.top 1"); Cue("panel Table 0", "as Scene 11 left it: the graph in the dock", 0.05);
            Cue("graph.axes 1"); Cue("graph.plot 5"); Cue("graph.line 2");
            Cue("hero V ∝ I|directly proportional, for this wire at constant temperature", "as Scene 11 left it");
            Gesture(WattsonController.Gesture.Talking); // not Thinking: its spinning gadget blooms to a white swirl
            Wait(0.3);
            Line("S12_L03");

            // Inside the metals: the camera pulls back to the physical wire and the 3D pop-up opens over the bench. The
            // readings strip steps aside (a fixed 1.00 A on screen would argue with "copper: more current").
            Shot("Wire Study");
            Cue("rig.readouts 0");
            Cue("readings.top 0", "the readings strip steps aside while the materials are compared");
            Cue("hero.clear");
            Cue("panel Graph 0", "the graph steps aside for the comparison");
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY", moveTo: "Sample Side");
            Show("Wire Highlight");
            Sfx("Highlight");
            Visual("The camera pulls back to the test wire; it highlights; Wattson flies to the pop-up's side", 1.0);
            // His magic opens it: the gadget zaps the wire and the magnified view pops up beside him. Audio: the cast's wand
            // sparkle (SFX_Wattson_Magic) is WP-W's gesture hook on Transform, so nothing is cued for it here; the pop-up's
            // own hit stays (Transform → SFX_Magic_Poof) and the mystery bed takes over for the inside of the metals.
            Gesture(WattsonController.Gesture.Transform, point: "NICHROME WIRE — XY", gadget: true);
            Wait(WattsonController.TransformActionDelay);
            Show("Wire Sample"); // the 3D pop-up card opens centre screen: copper alone, cut away, lattice ions and 24 electrons, swaying
            Sfx("Transform");
            Music("BGM_Conductor_Mystery");
            Visual("The pop-up: a copper segment, magnified, its electrons threading the lattice", 0.8);
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY");
            Line("S12_R01");
            Line("S12_R02");
            Gate("S12_SwapWire", "copper shows alone; the learner taps 'Swap in nichrome' (dock) or Enter: nichrome pops in under it, its electrons scattering hard and drifting slow. A drag on the card turns the sample.");
            Line("S12_R03");
            Line("S12_R04");
            Gate("S12_WhichResists", "Quick check (MCQ, [QC]): 'Same voltage. Which wire has the higher resistance?' A Copper · B Nichrome; graded under Resistance; Wattson reacts per pick.",
                "S12_QC", "S12_QC_R_If_S12_WhichResists_A", "S12_QC_R_If_S12_WhichResists_B");
            Line("S12_N01");
            Cue("callout show resistance");
            Line("S12_L05");
            Hide("Wire Sample"); // the pop-up closes on the real wire, still glowing: its opposition is what the data measures
            Visual("The real nichrome wire XY glows on the bench", 1.2);
            Hide("Wire Highlight");
            Cue("callout hideall");

            // Back to the learner's own data, on the data stage: the V / I column (the strip returns; the plates stay hidden).
            Shot("Bench Study");
            Cue("rig.readouts 1");
            Cue("readings.top 1");
            Gesture(WattsonController.Gesture.Point, point: "Panel Point", moveTo: "Tray Point");
            Line("S12_L06", 0.2);
            Music("BGM_Wattson_Magical_Discovery"); // audio: back to the discovery theme on "You've already measured it"
            Line("S12_L07");
            Cue("panel Table 1", "the learner's table returns");
            Cue("readings.top 0", "the strip makes room: the stage's plate reaches the top of the free frame", 0.05);
            Cue("rig.readouts 0", "", 0.05);
            Cue("stage 1", "data stage: table, graph and formula come to the centre of the screen", 0.05);
            Cue("table.ratio 1", "a new column V / I appears", 0.1);
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

            // The same answer from the graph: rise over run between two of the learner's points. The reasoning steps give
            // way to the law they led to, so the taller graph card keeps the plate clear of the strip and the captions.
            Cue("table.glow 2 0");
            Cue("panel Table 0"); Cue("panel Graph 1", "the graph takes the table's place for the slope");
            Cue("formula.clear");
            Formula("V = I × R", 0.1);
            Line("S12_L11");
            Cue("graph.triangle 0 2", "rise and run between (0.20, 1.0) and (0.60, 3.0): ΔV = 2.0 V, ΔI = 0.40 A");
            Line("S12_L12", 0.2);
            Formula("slope = ΔV ÷ ΔI = 2.0 V ÷ 0.40 A = 5");
            Line("S12_L13");
            Cue("stage 0", "the data returns to the dock");
            Cue("rig.readouts 1");
            Cue("readings.top 1", "the live strip returns for the ohm");

            // The ohm, centre screen.
            Cue("formula.clear");
            Cue("hero Ω|the ohm, unit of resistance", "centre-screen reveal"); // audio: the hero rule's SFX_Scientific_Discovery (the explicit Omega sting doubled it)
            Gesture(WattsonController.Gesture.Greet, moveTo: "Reveal Side"); // above the wide ohm plate, not behind it
            Line("S12_L14");
            Line("S12_N02", 0.2);
            Cue("hero 1 Ω = 1 V ÷ 1 A|one ohm: one volt drives one ampere", "replaces the Ω plate");
            Line("S12_L15", 0.2);
            Formula("this wire:  5.0 V ÷ 1.00 A = 5.0 Ω");
            Cue("readings.r 1", "R and the ohm are taught: the readings show R");
            Line("S12_N03", 0.3);
            Cue("handoff");
            Handoff("Scene12Complete", cut: true, afterSeconds: 0.3);
        }
    }
}
