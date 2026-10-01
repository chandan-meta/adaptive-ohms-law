using System.Linq;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 12: Discover Resistance. Inside the nichrome wire, then V/I is constant, slope = R, ohms.
    // Opens on the classroom board (match-cut from Scene 11), dives from the bench into the electron space and
    // comes back to the board, where the data cards, the Ω glyph and the slope sit beside the table and the graph.
    public class Scene12Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene12";
        public override string SceneName => "Scene12_Resistance";
        public override string Title => "Discover Resistance";

        // Wattson beside the board, left of the table: the kit's Boards Point (-1.05, 1.55, 1.7) sits over the V column
        // in the Boards shot (5-20 % across vs the column at 14-26 %); here he spans 1-12 %, the table panel starts at 12.8 %.
        static readonly Vector3 BoardSide = new Vector3(-1.48f, 1.5f, 2.2f);
        // Board UV of the two free strips: above the table (v 0.73-1.0) and above the graph (v 0.82-1.0).
        static readonly Vector2 AboveTable = new Vector2(TableBoardUV.x, 0.867f), AboveGraph = new Vector2(GraphBoardUV.x, 0.91f);

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
            ClassroomSet(modernBench: true, audience: false);
            director.matchCutFrom = "Scene11_Graph";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            refs["Circuit Rig"].transform.SetParent(refs["Lab"].transform, true); // the dive's `hide Lab` empties the bench too
            Boards(withTable: true, withGraph: true, unitsInTable: true);
            refs["Boards Point"].transform.position = BoardSide;               // the `lab` cue moves this marker for tableInWire
            Wattson(BoardSide, BoardsCamC);                                      // where Scene 11 leaves him

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

            // The electron space in nichrome grey-green with the ion lattice, far away at WireOrigin; shown by the dive.
            var wire = WireStage(NichromeTint, lattice: true, active: false, terminalsOn: true);
            wire.fieldOn = true;
            wire.latticeVisibility = 0;                                           // the `wire.lattice 1` cue brings the ions up
            director.wire = wire;
            Face(Card("Voltage", "VOLTAGE", palette.voltage, null, WireCardPosition(-1), WireConceptCap));
            Face(Card("Current", "CURRENT", palette.current, null, WireCardPosition(+1), WireConceptCap));
            Face(Card("Resistance", "RESISTANCE", palette.resistance, null, WireResistancePos, WireConceptCap));
            Face(Card("Symbol R", "Symbol: R", palette.resistance, null, WireResistancePos + Vector3.down * 0.5f, WireConceptCap * 0.56f));
            // tableInWire: the boards, their camera and Wattson's marker land above the conduit (camera outside the glass, conduit below the frame).
            Marker("Wire Boards Anchor", WireOrigin + new Vector3(0, 0.6f, -1f));

            // Back on the board (Boards shot, 56 px cards): the V/I sequence above the table, the slope in chalk above the
            // graph, then the Ω hero glyph takes the slope's place and the one-ohm card the formula's.
            float dataCap = BoardsShotTitleCap;
            float heroCap = WorldText.CapAt(WorldText.Px.Hero, BoardCentreC, BoardsCamC, BoardsLookC, BoardsFov);
            BoardCard("Constant", "V / I = CONSTANT", AboveTable, dataCap, WorldText.Look.Panel);
            BoardCard("Constant Word", "CONSTANT", AboveTable, dataCap, WorldText.Look.Panel);
            BoardCard("Resistance R", "RESISTANCE — R", AboveTable, dataCap, WorldText.Look.Panel);
            BoardCard("Formula", "R = V / I", AboveTable, dataCap, WorldText.Look.Panel);
            Board("Slope = ΔV / ΔI", Chalk, AboveGraph, dataCap, name: "Slope");
            BoardCard("Ohm Symbol", "Ω", AboveGraph, heroCap, WorldText.Look.Glow);
            BoardCard("One Ohm", "1 Ω = 1 V / 1 A", AboveTable, dataCap, WorldText.Look.Panel);

            // Lean-in on the graph for the triangle and the slope question: the graph fills the left 8-56 % of the frame so a
            // right-docked MCQ leaves it readable. Under the Boards Set (a plain node) so the tableInWire move takes it along.
            Vcam("Graph Close", new Vector3(1.25f, 1.6f, 0.35f), new Vector3(1.25f, BoardCentreC.y, BoardCentreC.z), BoardsFov, refs["Graph"].transform, lensMedium);
            refs["Cam Graph Close"].transform.SetParent(refs["Boards Set"].transform, true);
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
            // Match-cut from Scene 11: the Boards shot, the full table and graph, Wattson thinking beside the board.
            Shot("Boards");
            Cue("table.rows 5", "match-cut from Scene 11: table and graph visible");
            Cue("graph.axes 1"); Cue("graph.plot 5"); Cue("graph.line 1");
            Gesture(WattsonController.Gesture.Thinking);
            Wait(0.3);
            Gesture(WattsonController.Gesture.Aim, look: "NICHROME WIRE — XY", point: "NICHROME WIRE — XY", moveTo: "Niscope Point", gadget: true);
            Wait(0.7);
            Shot("Bench Wide");
            Show("Beam", 0.3);
            Visual("Wattson swoops to the end of the bench and aims the Ohm-niscope at the nichrome wire; the beam lands on it", 1.4);
            Shot("Dive");
            Show("Dive Streaks", 0.5);
            Visual("Dolly-zoom into the nichrome wire XY: streaks race out of the rod", 1.4);
            Cue("fade 1", "flash");
            Wait(0.6);
            Shot("Wire Inside", cut: true);
            Show("Wire Section"); Hide("Lab"); Hide("Beam"); Hide("Dive Streaks");
            Cue("wire.field 1"); Cue("wire.lattice 1", "the lattice of ions becomes visible (Scene 12 only)");
            Cue("fade 0", "the white clears once Wattson has crossed into the space", 0.4);
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
            Cue("wire.impede 0.8", "carriers slow and bounce off the lattice ions");
            Cue("wire.radius 0.5", "conduit narrows ~17 % (narrowingMetaphor toggle; WireInterior clamps the narrowing at 20 %)");
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
            Wait(0.6);
            Cue("lab", "back at the bench: graph and table side by side on the board");
            Cue("wire.radius 0.6", "the conduit is back to its built radius");
            Hide("Card Voltage"); Hide("Card Current"); Hide("Card Resistance"); Hide("Card Symbol R");
            Shot("Boards", cut: true);
            Gesture(WattsonController.Gesture.Point, point: "Observation Table", moveTo: "Boards Point");
            Cue("fade 0", "the white clears once Wattson has settled beside the board", 0.3);
            Wait(0.9);
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
            Visual("V / I = CONSTANT card above the table", 0.8);
            Line("S12_L09", 0.2);
            Hide("Card Constant"); Show("Card Constant Word");
            Visual("CONSTANT", 0.8);
            Hide("Card Constant Word"); Show("Card Resistance R");
            Visual("CONSTANT morphs into RESISTANCE — R", 1.0);
            Hide("Card Resistance R"); Show("Card Formula");
            Visual("R = V / I", 0.8);
            Line("S12_L10");
            Shot("Graph Close");
            Gesture(WattsonController.Gesture.Point, point: "Graph");
            Line("S12_L11");
            Cue("graph.triangle 0 2", "triangle between (0.20, 1.0) and (0.60, 3.0): ΔV = 2.0 V, ΔI = 0.40 A");
            Show("Board Slope");
            Visual("Wattson draws the ΔV/ΔI triangle; Slope = ΔV / ΔI in chalk above the graph", 1.2);
            Line("S12_L12", 0.2);
            Gate("S12_Slope", "MCQ (proposed format): Slope = ΔV / ΔI = ? · 5 · 0.2 · 2.5 (correct 5). Wrong: shake, show 2.0 ÷ 0.40, re-ask once, then continue.");
            Gesture(WattsonController.Gesture.Talking);
            Line("S12_L13");
            Shot("Boards");
            Hide("Board Slope"); Hide("Card Formula");
            Show("Card Ohm Symbol");
            Line("S12_L14");
            Show("Card One Ohm");
            Line("S12_L15", 0.2);
            Cue("handoff");
            Handoff("Scene12Complete", cut: true, afterSeconds: 0.5);
        }
    }
}
