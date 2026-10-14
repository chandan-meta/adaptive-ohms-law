using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 11: the discovery. The five points on the dock's graph are joined by a line that sweeps out from the
    // origin; the table and graph come to the centre of the screen (the data stage) while the doubling is read off them;
    // V ∝ I is revealed centre screen with a musical rise and Wattson's reaction. No challenge interrupts it: the learner
    // sees why the relationship is true in their own data.
    public class Scene11Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene11";
        public override string SceneName => "Scene11_Graph";
        public override string Title => "The Pattern";

        protected override void Dialogue()
        {
            L("S11_L01", "Now we've got our biggest clue. They're all in a line!", 3.5f);
            L("S11_L04", "For this conductor that is the nichrome wire, while its temperature remains constant...the potential difference across it is directly proportional to the current flowing through it.", 9.0f);
            L("S11_L05", "But there's still one clue we haven't explained.", 2.8f);
            L("S11_N01", "Look at the pattern! Double the voltage, and the current doubles too. They change together in the same proportion.", 6.0f, proposed: true);
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
            ClassroomSet(modernBench: true, audience: false);
            Wattson(Scene10Builder.SourceSide, Scene10Builder.StudyCamC);
            director.matchCutFrom = "Scene10_Experiment";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            director.rig.showPath = false;
            MeterPlates(false); // as Scene 10 left them: the readings strip carries the numbers, nothing reads through the glass
            Vcam("Bench Study", Scene10Builder.StudyCamC, Scene10Builder.StudyLookC, Scene10Builder.StudyFov, refs["Bench Point"].transform, lensMedium);
        }

        protected override void Beats()
        {
            // Match-cut from Scene 10: the same data in the dock (the cues rebuild it), rows 1-2 still highlighted.
            Shot("Bench Study");
            Ambience("RoomTone");
            Music(Scene10Builder.LabTheme(sounds));
            Cue("table.rows 5");
            Cue("table.highlight 0 1");
            Cue("readings.top 1");
            Cue("graph.axes 1"); Cue("graph.plot 5");
            Gesture(WattsonController.Gesture.Point, point: "Panel Point");
            Wait(0.4);
            Gate("S11_DrawLine", "the learner taps the graph: the line sweeps out through the five points");
            Cue("table.clear", "the table hands its highlights to the graph");
            Cue("formula.clear");
            Cue("graph.line 1", "the line sweeps out from the origin through every point, with the musical rise");
            Visual("Line through the origin", 1.4);
            Gesture(WattsonController.Gesture.Greet);
            Line("S11_L01");

            // The data stage: table and graph come to the centre of the screen while the doubling is read off them.
            Cue("stage 1", "data stage: the table and the graph move to the centre, in front of the bench");
            Gesture(WattsonController.Gesture.Talking);
            Cue("graph.highlight 0 1", "points (0.20, 1.0) and (0.40, 2.0) highlight");
            Cue("table.highlight 0 1");
            Line("S11_N01");
            Cue("graph.pulse");
            Cue("graph.clear");
            Cue("table.clear");
            // The table and the graph stay in front through the formal statement and hand over to the plate on its last words.
            Line("S11_L04", 0.2);
            Cue("stage 0", "the data returns to the dock", -3.4);
            Cue("panel Table 0", "the table steps aside: the graph stays in the dock", -3.3);
            Cue("hero V ∝ I|directly proportional, for this wire at constant temperature", "centre-screen reveal; stays through the handoff", -3.0);
            Sfx("Rise", -3.0);
            Visual("V ∝ I glows centre screen", 1.0);
            Gesture(WattsonController.Gesture.Point, point: "Panel Point"); // not Thinking: its spinning gadget blooms to a white swirl
            Line("S11_L05", 0.2);
            Cue("handoff", "the data stays for Scene 12");
            Handoff("Scene11Complete", cut: true, afterSeconds: 0.3);
        }
    }
}
