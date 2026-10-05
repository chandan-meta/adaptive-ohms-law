using System.Linq;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 12: Discover Resistance. Inside the nichrome wire, then V/I is constant, slope = R, ohms.
    // Opens on the classroom board (match-cut from Scene 11), dives from the bench into the nichrome wire XY: first the
    // voltage → current link seen from inside (a baseline at 1 V, then 2 V: the drift and the charges per second double
    // with the bench's own readings), then the lattice and resistance. Comes back out of the same wire to the same bench,
    // then to the board, where the learner works row 3's V / I, the data cards, AQ1, the slope and the Ω follow.
    public class Scene12Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene12";
        public override string SceneName => "Scene12_Resistance";
        public override string Title => "Discover Resistance";

        // Wattson beside the board, left of the table: the kit's Boards Point (-1.05, 1.55, 1.7) sits over the V column
        // in the Boards shot (5-20 % across vs the column at 14-26 %); here he spans 1-12 %, the table panel starts at 12.8 %.
        // Scene 12 ends with him here (Scene13Builder.Scene12WattsonC mirrors it: change together).
        static readonly Vector3 BoardSide = new Vector3(-1.48f, 1.5f, 2.2f);
        // Board UV of the two free strips: above the table (v 0.73-1.0) and above the graph (v 0.82-1.0); the graph's strip
        // is only ~0.2 m tall, so its text sits high in it (Scene13Builder.Scene12OmegaV mirrors this v).
        static readonly Vector2 AboveTable = new Vector2(TableBoardUV.x, 0.867f), AboveGraph = new Vector2(GraphBoardUV.x, 0.935f);
        // Inside the wire the drift follows the bench's current: 2 m/s per ampere, so 1 V (0.20 A) drifts at 0.4 m/s and
        // 5 electrons per metre cross the ring 2 times a second; 2 V (0.40 A) doubles both.
        const float DriftPerAmp = 2f;

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

            // Proposed: the voltage → current link seen from inside the wire (after the bench experiment: nothing to spoil).
            L("S12_W01", "One volt on the bench. Now let's see it from inside the wire.", 3.8f, proposed: true);
            L("S12_W02", "Inside the nichrome wire. Count the charges crossing the ring each second.", 3.8f, proposed: true);
            L("S12_W03", "Now I'll double the voltage to 2 volts.", 2.8f, proposed: true);
            L("S12_W04", "Double the push, double the charge per second. Just like your table.", 3.8f, proposed: true);
            // S12_Ratio (gate contract: _Q while the panel is up, _FA/_FB/_FC per option, _H before a retry, _X after the last).
            L("S12_Ratio_Q", "Your turn, detective. What is 3.0 divided by 0.60?", 3.2f, proposed: true);
            L("S12_Ratio_FA", "Five again. The same as rows one and two.", 2.8f, proposed: true);
            L("S12_Ratio_FB", "That's 0.60 divided by 3.0. Flip it: volts divided by amps.", 3.8f, proposed: true);
            L("S12_Ratio_FC", "That's 3.0 times 0.60. We need to divide: volts by amps.", 3.6f, proposed: true);
            L("S12_Ratio_H", "Think: how many times does 0.60 go into 3.0?", 3.0f, proposed: true);
            L("S12_Ratio_X", "Watch: 3.0 divided by 0.60 is 5. Five, again.", 3.2f, proposed: true);
            // S12_Slope (existing gate, now graded with the same contract).
            L("S12_Slope_Q", "So, what is the slope? ΔV divided by ΔI.", 2.8f, proposed: true);
            L("S12_Slope_FA", "Five. The same five as V divided by I.", 2.8f, proposed: true);
            L("S12_Slope_FB", "That's ΔI divided by ΔV, upside down. Volts go on top.", 3.6f, proposed: true);
            L("S12_Slope_FC", "Careful: use the two differences, 2.0 volts and 0.40 amps.", 3.6f, proposed: true);
            L("S12_Slope_H", "ΔV is 2.0 volts, ΔI is 0.40 amps. Divide them.", 3.4f, proposed: true);
            L("S12_Slope_X", "2.0 divided by 0.40 is 5. The slope is 5.", 3.2f, proposed: true);
            // S12_AQ1: the storyboard's checkpoint items, word for word (moved here from Scene 13), and the demonstration line.
            L("AQ1", "If resistance stays constant and voltage doubles, what happens to current?", 4.5f);
            L("AQ_EXPLAIN", "Remember the table, detective: every time V went up, I went up with it, and V divided by I stayed at 5.", 6.5f, proposed: true);
            L("AQ_EASY", "When the potential difference across the nichrome wire went up, what happened to the current?", 5.0f, proposed: true);
            L("AQ_GOOD", "Case closed, detective.", 1.8f, proposed: true);
            // Wrong picks (AQ1's B/C and AQ_EASY's B/C share the gate's _FB/_FC), before the AQ_EXPLAIN hint; a right pick
            // falls back to AQ_GOOD. The reveal runs in the Boards shot: the readings strip above the graph is what shows it.
            L("S12_AQ1_FB", "More push, but less flow? That's not what our wire did.", 3.2f, proposed: true);
            L("S12_AQ1_FC", "If the current stayed put, the I column would never change. It does!", 3.8f, proposed: true);
            L("S12_AQ1_X", "Watch the readings: 2 volts gives 0.40 amperes, 4 volts gives 0.80.", 4.8f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 11: voltage (V, volt), current (I, ampere), the five readings, V ∝ I for the nichrome wire at constant temperature, the straight-line graph through the origin.",
                "Inside the nichrome wire Wattson set the bench to 1 V (0.20 A), then 2 V (0.40 A): every electron's drift doubled and so did the charges crossing the ring each second. The counter shows a small sample, not the real number of electrons.",
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
            ClassroomSet(modernBench: true, audience: false);
            director.matchCutFrom = "Scene11_Graph";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            director.rig.showPath = false;                                       // off since Scene 9's "rig.path 0": plain cables on the bench
            refs["Circuit Rig"].transform.SetParent(refs["Lab"].transform, true); // the dive's `hide Lab` empties the bench too
            // Known pop at the 11 → 12 cut: Scene 11's cells read "1 | 0.2", these read "1.0 V | 0.20 A" (ObservationTableUI.units
            // is set at build time; there is no cue to switch it under the dive's white). Scene 13 matches Scene 12.
            Boards(withTable: true, withGraph: true, unitsInTable: true);
            refs["Boards Point"].transform.position = BoardSide;               // the `lab` cue moves this marker for tableInWire
            Wattson(TableSideC, BoardsCamC);                                     // where Scene 11 leaves him (its Table Point, all scene); BoardSide after the wire
            // Scene 11's last card, carried over the match-cut exactly as Scene11Builder.Stage builds it (same text, colour,
            // graph point, lift, cap, glow look and board rotation; its constants), shown from frame 0 and settled (no pop,
            // no sparks). It goes under the dive's white, where the board is out of sight (Bench Wide still shows the board).
            var proportional = Card("Proportional", "V ∝ I", palette.voltage, null, GraphPoint(Scene11Builder.CardAmps, Scene11Builder.CardVolts, Scene11Builder.CardLift), BoardsShotTitleCap, WorldText.Look.Glow, true, sparks: false);
            proportional.transform.rotation = BoardSurface(GraphBoardUV).rotation;
            var settled = proportional.transform.Find("Text").GetComponent<PopIn>();
            settled.fromScale = 1f;
            settled.overshoot = 1f;
            // Scene 11's "Graph Line Glow", the additive strand that keeps the UI line blooming, carried over the same way
            // (Scene11Builder.LineDraw: same material, width, lift and end points); it goes under the white with the card.
            var data = BlockoutAssets.ExperimentData();
            var strand = Polyline("Graph Line Glow", refs["Boards Set"].transform, Glow("Glow_GraphLine", Hdr(palette.voltage, 2.2f), softTexture: false), 0.014f, false, true,
                GraphPoint(0, 0, Scene11Builder.GlowLift), GraphPoint(1.1f, 1.1f * data.resistance, Scene11Builder.GlowLift));
            Ref(strand.gameObject);

            // The dive. Wattson aims from off the left end of the bench, level with the wire, so the Ohm-niscope beam runs
            // along the rod and crosses nothing; the beam is the Scene 4 one inside his gadget. Scene 4's streaks race out
            // of the rod at the camera while the Dive camera dolly-zooms onto it (Lens_Dive: motion blur + aberration).
            var wireNode = refs["NICHROME WIRE — XY"].transform;
            Vector3 wireAt = wireNode.position;
            Marker("Niscope Point", new Vector3(-1.3f, 1.1f, wireAt.z));
            var beam = refs["Wattson"].GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Beam");
            if (beam) Ref(beam.gameObject);
            var streaks = WireFx("race into wire surface", wireAt);
            refs.Remove(streaks.name);
            streaks.name = "Dive Streaks";                                      // a real effect, not a reviewer tag: no "[VFX]" panel
            Ref(streaks);
            Vcam("Dive", wireAt + new Vector3(0, 0.095f, -0.12f), wireAt, 70, wireNode, lensDive);
            Vcam("Dive Out", wireAt + new Vector3(0, 0.095f, -0.12f), wireAt, 70, wireNode, lensMedium); // the way back: same pose, no dive blur

            // The electron space in nichrome grey-green with the ion lattice, far away at WireOrigin; shown by the dive.
            var wire = WireStage(NichromeTint, lattice: true, active: false, terminalsOn: true);
            wire.fieldOn = true;
            wire.latticeVisibility = 0;                                           // the `wire.lattice 1` cue fades the ions in
            wire.driftPerAmp = DriftPerAmp;
            director.wire = wire;
            WireReadout(wire, director.rig);                                      // "Wire Readout": the bench's V and I beside the stream
            Face(Card("Voltage", "VOLTAGE", palette.voltage, null, WireCardPosition(-1), WireConceptCap));
            Face(Card("Current", "CURRENT", palette.current, null, WireCardPosition(+1), WireConceptCap));
            Face(Card("Resistance", "RESISTANCE", palette.resistance, null, WireResistancePos, WireConceptCap));
            Face(Card("Symbol R", "Symbol: R", palette.resistance, null, WireResistancePos + Vector3.down * 0.5f, WireConceptCap * 0.56f));
            // tableInWire: the boards, their camera and Wattson's marker land above the conduit (camera outside the glass, conduit below the frame).
            Marker("Wire Boards Anchor", WireOrigin + new Vector3(0, 0.6f, -1f));

            // Back on the board (Boards shot, 56 px cards): the V/I sequence above the table, the slope in chalk above the
            // graph, then the Ω hero glyph takes the slope's place and the one-ohm card the formula's.
            float dataCap = BoardsShotTitleCap;
            // The Ω fits the ~0.2 m strip above the graph (at the Hero tier it sat on the graph's frame); Scene 13 mirrors it.
            float heroCap = WorldText.CapAt(WorldText.Px.DataCard * 1.6f, BoardCentreC, BoardsCamC, BoardsLookC, BoardsFov);
            BoardCard("Constant", "V / I = CONSTANT", AboveTable, dataCap * 0.88f, WorldText.Look.Panel);  // the long cards stay inside the table's span
            BoardCard("Constant Word", "CONSTANT", AboveTable, dataCap, WorldText.Look.Panel);
            BoardCard("Resistance R", "RESISTANCE — R", AboveTable, dataCap * 0.88f, WorldText.Look.Panel);
            BoardCard("Formula", "R = V / I", AboveTable, dataCap, WorldText.Look.Panel);
            Board("Slope = ΔV / ΔI", Chalk, AboveGraph, dataCap, name: "Slope");
            BoardCard("Ohm Symbol", "Ω", AboveGraph, heroCap, WorldText.Look.Glow);
            BoardCard("One Ohm", "1 Ω = 1 V / 1 A", AboveTable, dataCap, WorldText.Look.Panel);

            // Lean-in on the graph for the triangle and the slope question: the graph fills the left 8-56 % of the frame so a
            // right-docked MCQ leaves it readable; Wattson stands at its lower right ("Graph Point"), under the docked panel.
            // The look point sits 0.08 m under the board's centre so the frame rises ~40 px and the x-axis title "Current, I (A)"
            // clears the subtitle band. Under the Boards Set (a plain node) so the tableInWire move takes them along.
            Vcam("Graph Close", new Vector3(1.33f, 1.6f, 0.2f), new Vector3(1.33f, BoardCentreC.y - 0.08f, BoardCentreC.z), BoardsFov, refs["Graph"].transform, lensMedium);
            refs["Cam Graph Close"].transform.SetParent(refs["Boards Set"].transform, true);
            Marker("Graph Point", new Vector3(2.0f, 1.3f, 2.2f)).transform.SetParent(refs["Boards Set"].transform, true);
        }

        // A resistance-coral card lying flat on the board, 1.6 cm off the chalk face so its backing clears the slab, under the
        // Boards Set so the `lab` cue's tableInWire move carries it with the table and graph.
        GameObject BoardCard(string name, string text, Vector2 uv, float cap, WorldText.Look look)
        {
            var (position, rotation) = BoardSurface(uv, 0.016f);
            var card = Card(name, text, palette.resistance, refs["Boards Set"].transform, position, cap, look);
            card.transform.rotation = rotation;
            return card;
        }

        protected override void Beats()
        {
            // Match-cut from Scene 11: the Boards shot, the full table and graph with V ∝ I, Wattson beside the board.
            Shot("Boards");
            Cue("table.rows 5", "match-cut from Scene 11: table and graph visible");
            Cue("graph.axes 1"); Cue("graph.plot 5"); Cue("graph.line 1");
            Gesture(WattsonController.Gesture.Thinking);
            Wait(0.3);
            Gesture(WattsonController.Gesture.Aim, look: "NICHROME WIRE — XY", point: "NICHROME WIRE — XY", moveTo: "Niscope Point", gadget: true);
            Wait(0.7);
            // The bench first: Wattson sets it to 1 V (row 1) as the camera goes to the bench; the 1 s blend and the meters'
            // easing (from Scene 11's last setting) are done before "One volt on the bench" names the reading. Then the dive.
            Cue("rig.voltage 1", "Wattson sets the bench to 1 V: the meters ease to 1.0 V and 0.20 A (ExperimentData row 1)");
            Shot("Bench Wide");
            Show("Beam", 0.3);
            Visual("The camera settles on the bench; the meters settle at 1.0 V, 0.20 A", 1.0);
            Line("S12_W01");
            Shot("Dive");
            Show("Dive Streaks", 0.5);
            Visual("Dolly-zoom into the nichrome wire XY: streaks race out of the rod", 1.4);
            Cue("fade 1", "flash");
            Wait(0.6);

            // Inside, set up under the white: the stream at the bench's 1 V, the ring, the counter and the bench readings,
            // the lattice fading in; Wattson already on his spot (no 98 m spring flight on screen).
            Shot("Wire Inside", cut: true);
            Show("Wire Section"); Hide("Lab"); Hide("Beam"); Hide("Dive Streaks");
            Hide("Card Proportional"); Hide("Graph Line Glow");                  // Scene 11's V ∝ I and its line glow go under the white
            Cue("wire.field 1");
            Show("Cross Section"); Show("Counter"); Show("Wire Readout");
            Cue("wire.count 1", "the counter counts the net charges through the ring each second");
            Cue("wire.lattice 1", "the lattice of ions fades in (Scene 12 only)", 0.1);
            Cue("wire.warp Platform Point", "Wattson is on his spot before the white clears", 0.1);
            Cue("fade 0", "the white clears inside the nichrome wire", 0.4);
            Gesture(WattsonController.Gesture.Idle, moveTo: "Platform Point");
            Visual("Inside the nichrome wire at 1 V: electrons drift past the fading-in lattice; the counter completes its first second", 1.5);

            // Voltage change → current change, one change at a time: a baseline, then double the voltage. The readings are
            // the bench's own (the rig), the count is what crosses the ring.
            Gesture(WattsonController.Gesture.Point, point: "Counter");
            Line("S12_W02");
            Visual("Baseline: V = 1.0 V, I = 0.20 A; the counter reads 2 charges per second", 1.5);
            Gesture(WattsonController.Gesture.RaiseGadget, gadget: true);
            Line("S12_W03", 0.1);
            Cue("rig.voltage 2", "the bench goes to 2 V: the readings show 2.0 V and 0.40 A; every electron's drift doubles at once");
            Visual("The whole stream drifts twice as fast; after a full second the counter reads 4", 2.5);
            Gesture(WattsonController.Gesture.Point, point: "Wire Readout");
            Line("S12_W04");

            Gesture(WattsonController.Gesture.Point, point: "Card Terminal +");
            Show("Card Voltage");
            Line("S12_L01");
            Gesture(WattsonController.Gesture.Point, point: "Counter");
            Show("Card Current");
            Line("S12_L02");
            // The counting has done its job: the ring, the counter and the readings go before the material's opposition.
            Cue("wire.count 0");
            Hide("Counter"); Hide("Wire Readout"); Hide("Cross Section");
            Gesture(WattsonController.Gesture.Thinking);
            Line("S12_L03");

            Show("[VFX] carriers impeded by lattice (slow, bounce)");
            Cue("wire.impede 0.8", "electrons slow and bounce off the lattice ions");
            Cue("wire.radius 0.5", "conduit narrows ~17 % (narrowingMetaphor toggle; WireInterior clamps the narrowing at 20 %)");
            Gesture(WattsonController.Gesture.Point, point: "Lattice Ions");
            Line("S12_L04");
            Hide("[VFX] carriers impeded by lattice (slow, bounce)");
            Wait(0.4); // the first panel is gone before the next comes, so they never stack over the VOLTAGE card
            Show("[VFX] RESISTANCE word forms from the lattice");
            Show("Card Resistance", 0.8);
            Line("S12_L05");
            Hide("[VFX] RESISTANCE word forms from the lattice");
            Show("Card Symbol R");
            Visual("Symbol: R card", 1.0);

            Gesture(WattsonController.Gesture.Greet);
            Line("S12_L06", 0.2);
            // Out of the same wire to the same bench: under the white the bench is back and Wattson is at its end, where he
            // dived from; the white clears on the rod up close, the camera pulls back to the whole bench (its meters read
            // 2.0 V and 0.40 A), then goes to the board. No narration while the camera moves.
            Cue("fade 1", "white: out of the wire (tableInWire keeps the boards inside it instead)");
            Wait(0.6);
            Cue("lab", "the bench and the board are back; the electron space goes");
            Cue("wire.radius 0.6", "the conduit is back to its built radius");
            Hide("Card Voltage"); Hide("Card Current"); Hide("Card Resistance"); Hide("Card Symbol R");
            Cue("wire.warp Niscope Point", "Wattson beside the end of the bench, where he dived from", 0.1);
            Shot("Dive Out", cut: true);
            Gesture(WattsonController.Gesture.Idle);
            Cue("fade 0", "the white clears on the nichrome wire XY, up close", 0.2);
            Wait(0.8);
            Shot("Bench Wide");
            Visual("The camera pulls back off the rod to the whole bench: the same wire XY, its meters at 2.0 V and 0.40 A", 1.6);
            Shot("Boards");
            Gesture(WattsonController.Gesture.Point, point: "Observation Table", moveTo: "Boards Point");
            Visual("To the board: Wattson flies back beside the table", 1.4);
            Line("S12_L07");
            Cue("table.ratio 1", "a new column V / I appears");
            Visual("V / I column appears", 0.8);
            for (int r = 0; r < 2; r++)
            {
                Cue("table.work " + r, $"Wattson works row {r + 1}: {new[] { "1.0 ÷ 0.20 = 5", "2.0 ÷ 0.40 = 5" }[r]}");
                Visual("", 1.2);
            }
            // Row 3 is the learner's: 3.0 ÷ 0.60 = ?
            Gesture(WattsonController.Gesture.Listening);
            Gate("S12_Ratio", "MCQ (graded): 3.0 ÷ 0.60 = ? · 5 (correct) · 0.2 (divides I by V) · 1.8 (multiplies). Hint + retry; Resistance. The answer fills row 3.",
                "S12_Ratio_Q", "S12_Ratio_FA", "S12_Ratio_FB", "S12_Ratio_FC", "S12_Ratio_H", "S12_Ratio_X");
            Cue("table.work 2", "row 3: 3.0 ÷ 0.60 = 5");
            Visual("", 0.6);
            Cue("table.result 3", "5"); Cue("table.result 4", "5", 0.3);
            Visual("the last two fill rapidly", 0.7);
            Cue("table.glow 2 1", "the whole V/I column glows");
            Gesture(WattsonController.Gesture.Point, point: "Observation Table");
            Line("S12_L08", 0.2);
            Show("Card Constant");
            Visual("V / I = CONSTANT card above the table", 0.8);
            Line("S12_L09", 0.2);
            Hide("Card Constant"); Show("Card Constant Word");
            Visual("CONSTANT", 0.8);
            Hide("Card Constant Word"); Show("Card Resistance R");
            Visual("CONSTANT morphs into RESISTANCE — R", 1.0);
            Hide("Card Resistance R"); Show("Card Formula");
            Visual("R = V / I", 0.8);
            Line("S12_L10");

            // AQ1, now that resistance is defined (skipped for a learner already confident with the relationship).
            Gesture(WattsonController.Gesture.Listening);
            Gate("S12_AQ1", "Storyboard AQ1 (graded, moved from Scene 13): A. It doubles (correct) · B. It halves · C. It stays the same. Wrong: S12_AQ1_FB/_FC, AQ_EXPLAIN, then AQ_EASY; out of tries: S12_AQ1_X over 2 V → 4 V on the readings strip. Skipped when Confident(Relationship); checkpointEnabled gates it.",
                "AQ1", "AQ_EASY", "AQ_EXPLAIN", "AQ_GOOD", "S12_AQ1_FB", "S12_AQ1_FC", "S12_AQ1_X");

            // Wattson goes to the graph's lower right first, then the lean-in, then the line: one movement at a time.
            Gesture(WattsonController.Gesture.Point, point: "Graph", moveTo: "Graph Point");
            Visual("Wattson moves beside the graph", 1.0);
            Shot("Graph Close");
            Wait(0.6);
            Line("S12_L11");
            Cue("graph.triangle 0 2", "triangle between (0.20, 1.0) and (0.60, 3.0): ΔV = 2.0 V, ΔI = 0.40 A");
            Show("Board Slope");
            Visual("Wattson draws the ΔV/ΔI triangle; Slope = ΔV / ΔI in chalk above the graph", 1.2);
            Line("S12_L12", 0.2);
            Gate("S12_Slope", "MCQ (proposed format): Slope = ΔV / ΔI = ? · 5 · 0.2 · 2.5 (correct 5). Wrong: shake, show 2.0 ÷ 0.40, re-ask once, then continue.",
                "S12_Slope_Q", "S12_Slope_FA", "S12_Slope_FB", "S12_Slope_FC", "S12_Slope_H", "S12_Slope_X");
            Gesture(WattsonController.Gesture.Talking);
            Line("S12_L13");
            // A cut (the Ω appears fully on screen, not during a pull-out); Wattson is back beside the table behind it,
            // where Scene 13 match-cuts from.
            Shot("Boards", cut: true);
            Cue("wire.warp Boards Point", "Wattson beside the table again (hidden by the cut)");
            Hide("Board Slope"); Hide("Card Formula");
            Show("Card Ohm Symbol");
            Line("S12_L14");
            // The ohm is named: from here the live readings may add R (5 Ω) and Ask Wattson may quote it
            // (LearnerSession.resistanceTaught). State only: the readings strip itself stays off, the Ω owns that band.
            Cue("readings.r 1", "R and the ohm are taught: the readings may show R");
            Show("Card One Ohm");
            Line("S12_L15", 0.2);
            Cue("handoff");
            Handoff("Scene12Complete", cut: true, afterSeconds: 0.5);
        }
    }
}
