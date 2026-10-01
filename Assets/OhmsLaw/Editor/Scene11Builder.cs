using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 11: Turn Data into a Graph. Points in a straight line through the origin; V ∝ I.
    public class Scene11Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene11";
        public override string SceneName => "Scene11_Graph";
        public override string Title => "Turn Data into a Graph";

        protected override void Dialogue()
        {
            L("S11_L01", "Now we've got our biggest clue. They're all in a line!", 3.5f);
            L("S11_L02", "Double the potential difference...and the current doubles.", 3.5f);
            L("S11_L03", "And it keeps happening.", 1.8f);
            L("S11_L04", "For this conductor that is the nichrome wire, while its temperature remains constant...the potential difference across it is directly proportional to the current flowing through it.", 9.0f);
            L("S11_L05", "But there's still one clue we haven't explained.", 2.8f);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 10: voltage (V, volt), current (I, ampere), the circuit, the five readings (1 V → 0.2 A ... 5 V → 1.0 A), current rises with potential difference, doubling V doubles I.",
                "Plotted with current I on the horizontal axis and potential difference V on the vertical axis, the five points lie on a straight line through the origin.",
                "For the nichrome wire at constant temperature, the potential difference across it is directly proportional to the current through it: V ∝ I.",
            },
            new[] { "Resistance", "V / I = constant", "The slope of the graph is the resistance", "Ohms", "V = IR", "The name 'Ohm's law'", "The Christmas-lights answer" },
            ("Why does the line go through the origin?", "Think about zero volts: no push, so no current, which is the point zero-zero. And every reading doubled together with its partner: 1 volt, 0.2 ampere; 2 volts, 0.4 ampere. Points that scale together like that always line up on a straight line that starts at the origin."),
            ("What does 'directly proportional' mean?", "Two quantities are directly proportional when doubling one doubles the other, tripling one triples the other, and so on. Our table shows exactly that: double the volts, double the amperes. Scientists write it with the symbol V ∝ I."),
            ("Why does the temperature have to stay constant?", "Because a hot wire behaves differently from a cool one. If the wire warmed up during our readings, the same push would give a slightly different current, and our neat straight line would start to bend. Keeping the temperature steady keeps the comparison fair."));

        protected override void Stage()
        {
            LabGreybox(modernBench: true);
            KidsAt(new Vector3(0.2f, 0, -2.2f), Vector3.forward);
            Wattson(WattsonHome, BenchWideCam);
            director.matchCutFrom = "Scene10_Experiment";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            Boards(withTable: true, withGraph: true);
            Card("Proportional", "V ∝ I", palette.voltage, null, new Vector3(0.95f, 2.75f, 0.35f), 0.3f);
            Vfx("graph pulse", null, new Vector3(-1.1f, 2.7f, 0.4f), 1.0f);
        }

        protected override void Beats()
        {
            Shot("Boards");
            Cue("table.rows 5", "match-cut from Scene 10 with the full table on screen");
            Gesture(WattsonController.Gesture.Point, point: "Graph", moveTo: "Boards Point");
            Show("[VFX] columns become axes");
            Visual("A giant graph materialises above the bench: the I column slides out to become the x-axis, the V column rises to become the y-axis", 1.5);
            Cue("graph.axes 1", "axes labelled: Current, I (A) horizontal · Potential Difference, V (V) vertical");
            Hide("[VFX] columns become axes", 1.0);
            Visual("Axis labels appear", 1.2);
            for (int i = 1; i <= 5; i++)
            {
                Cue("graph.plot " + i, $"point {i} plots with its coordinate label");
                Visual("", 0.6);
            }
            Cue("graph.line 1", "a glowing line draws through the points and the origin");
            Visual("Line through the origin", 1.0);
            Gesture(WattsonController.Gesture.Greet);
            Line("S11_L01");

            Gesture(WattsonController.Gesture.Point, point: "Graph");
            Cue("graph.highlight 0 1", "points (0.20, 1.0) and (0.40, 2.0) highlight with I → 2I and V → 2V tags");
            Line("S11_L02");
            Cue("graph.pulse", "graph pulse");
            Show("[VFX] graph pulse");
            Line("S11_L03");
            Hide("[VFX] graph pulse");

            Gesture(WattsonController.Gesture.Talking);
            Line("S11_L04", 0.2);
            Show("Card Proportional", -1.0);
            Visual("V ∝ I card at the end of the line", 0.8);
            Gesture(WattsonController.Gesture.Thinking);
            Line("S11_L05", 0.2);
            Cue("handoff", "graph and table stay for Scene 12");
            Handoff("Scene11Complete", cut: true, afterSeconds: 0.4);
        }
    }
}
