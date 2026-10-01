using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 10: Become the Experimenter. Set 1 V, record, auto-run 2-5 V, spot the pattern, find the doubling.
    public class Scene10Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene10";
        public override string SceneName => "Scene10_Experiment";
        public override string Title => "Become the Experimenter";

        protected override void Dialogue()
        {
            L("S10_L01", "Let's start low. Set the battery to 1 volt.", 3.0f);
            L("S10_L02", "Note the reading of Potential difference in voltmeter.", 3.2f);
            L("S10_L03", "Record the current reading in ammeter.", 2.6f);
            L("S10_L04", "Now lets keep increasing the battery voltage, and see the readings in voltmeter and ammeter.", 5.5f);
            L("S10_L05", "Don't look at me.", 1.4f);
            L("S10_L06", "Look at your data.", 1.6f);
            L("S10_L07", "What do you notice?", 1.6f);
            L("S10_L08", "Good. But I think there's an even better pattern hiding in there.", 4.0f);
            L("S10_L09", "Compare just these two.", 1.8f);
            L("S10_L10", "What happened to the current when the potential difference doubled? It also doubled", 5.0f);
            L("S10_L11", "Now we're getting somewhere.", 2.0f);
            L("S10_R_WRONG", "Hmm. Look at the table once more. As the potential difference goes up, what is the current doing?", 5.5f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 9: voltage (V, volt) as the push, current (I, ampere), the circuit with the ammeter in series and the voltmeter in parallel across the nichrome wire XY.",
                "The five readings: 1 V → 0.2 A, 2 V → 0.4 A, 3 V → 0.6 A, 4 V → 0.8 A, 5 V → 1.0 A.",
                "The current increases when the potential difference increases.",
                "Doubling the potential difference doubles the current (1 V → 0.2 A and 2 V → 0.4 A).",
            },
            new[] { "V is proportional to I as a law statement", "The straight-line graph (Scene 11)", "Resistance", "V / I = 5 or V / I is constant", "Ohms", "V = IR", "The Christmas-lights answer" },
            ("Why do we start at 1 volt?", "Starting low is good detective practice. A small push gives a small, safe current, we can check that everything is connected properly, and then we raise the voltage one step at a time so we can see clearly what each step does to the current."),
            ("Why does the current change when I change the voltage?", "The potential difference is the push on the charges in the wire. A bigger push makes the free electrons drift along faster, so more charge crosses each section of the wire every second, and that is a bigger current. Look at your table to see exactly how much bigger."),
            ("What does 'record a reading' mean?", "It means writing down what the instrument shows at that moment: the voltmeter's volts and the ammeter's amperes, side by side in the observation table. One row per setting, so afterwards we can compare them and hunt for a pattern."));

        protected override void Stage()
        {
            LabGreybox(modernBench: true);
            KidsAt(new Vector3(0.2f, 0, -2.2f), Vector3.forward);
            Wattson(WattsonHome, BenchWideCam);
            director.matchCutFrom = "Scene09_BuildCircuit";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 0);
            Boards(withTable: true, withGraph: false);
            Card("Discovery", "V × 2 → I × 2", palette.current, null, new Vector3(1.0f, 2.0f, 0.1f), 0.22f);
        }

        protected override void Beats()
        {
            Shot("Bench Wide");
            Cue("rig.voltage 0", "match-cut: circuit complete, key closed, source at 0 V");
            Cue("table.rows 0", "empty observation table with headers");
            Gesture(WattsonController.Gesture.Point, point: "cells / controllable DC source", moveTo: "Source Point");
            Cue("rig.slider 1", "voltage controller appears (0-5 V in 1 V steps)");
            Visual("Voltage controller appears by the source", 0.8);
            Cue("prompt Let's start low. Set the battery to 1 volt.", "on-screen prompt");
            Line("S10_L01", 0.2);
            Gate("S10_Set1V", "Set the slider to 1 V (mouse/touch). Ammeter needle moves to 0.20 A. autoAnswer sets it after 3 s.");
            Cue("prompt", "");
            Gesture(WattsonController.Gesture.Point, point: "VOLTMETER");
            Line("S10_L02");
            Gesture(WattsonController.Gesture.Point, point: "AMMETER");
            Cue("prompt Record the current reading in ammeter.", "on-screen prompt");
            Line("S10_L03", 0.2);
            Gate("S10_Record", "Tap RECORD (or the ammeter). Row 1 (1, 0.2) animates into the observation table.");
            Cue("prompt", "");

            Gesture(WattsonController.Gesture.Talking);
            Line("S10_L04");
            Shot("Boards");
            Gesture(WattsonController.Gesture.Point, point: "Observation Table", moveTo: "Table Point");
            for (int v = 2; v <= 5; v++)
            {
                Cue("rig.voltage " + v, $"controller steps to {v} V; meters update");
                Cue("table.rows " + v, $"row {v} auto-fills", 0.6);
                Visual($"Auto step to {v} V", 1.5);
            }
            Gesture(WattsonController.Gesture.Idle, look: "Observation Table");
            Line("S10_L05");
            Line("S10_L06");
            Line("S10_L07", 0.2);
            Gate("S10_Pattern", "MCQ: A. Current increases as potential difference increases. · B. Current decreases. · C. There is no pattern. (correct A) → LearnerSession.s10PatternAnswer. Wrong: S10_R_WRONG then re-ask; second wrong: sweep V then I column, re-ask; third answer accepted.", "S10_R_WRONG");

            Gesture(WattsonController.Gesture.Point, point: "Observation Table");
            Line("S10_L08", 0.2);
            Cue("table.highlight 0 1", "rows 1 and 2 highlight: 1 V → 0.2 A and 2 V → 0.4 A");
            Line("S10_L09");
            Line("S10_L10", 0.2);
            Gate("S10_Discover", "Tap either highlighted row or the ×2 chip to reveal V × 2 → I × 2. Auto-reveals after 5 s.");
            Gesture(WattsonController.Gesture.Greet);
            Line("S10_L11", 0.2);
            Cue("rig.slider 0", "voltage controller hidden");
            Cue("handoff", "SceneHandoffState: table rows + camera");
            Handoff("Scene10Complete", cut: true, afterSeconds: 0.4);
        }
    }
}
