using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 10: the experiment, guided from the panel. The battery slider at the bottom of the screen; "Set the voltage
    // to 1 V." ... "5 V."; each setting is observed on the meters (the live readings top-centre), recorded into the
    // dock's table and plotted on the graph beside it (the empty graph is there from the start); after 2 V the learner
    // predicts the 3 V reading and the ammeter judges it; then the first two rows are compared: double the voltage,
    // double the current, as a centre-screen reveal. The bench stays in the free part of the frame on Bench Study.
    public class Scene10Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene10";
        public override string SceneName => "Scene10_Experiment";
        public override string Title => "Become the Experimenter";

        // A bench shot that keeps the whole 2.0 m bench inside the frame the wide panel leaves free (the panel crops ~23 %
        // off each side): 2.4 m back, FOV 38, Wattson behind the source.
        public static readonly Vector3 StudyCamC = new Vector3(0f, 1.3f, -2.45f), StudyLookC = new Vector3(0f, 0.9f, 0.1f);
        public const float StudyFov = 38f;
        public static readonly Vector3 SourceSide = new Vector3(-0.75f, 1.4f, 0.8f); // behind and above the source, inside the frame the wide panel leaves free (Bench Study)

        protected override void Dialogue()
        {
            L("S10_L01", "Let's start low. Set the battery to 1 volt.", 3.0f);
            L("S10_L06", "Look at your data.", 1.6f);
            L("S10_L07", "What do you notice?", 1.6f);
            L("S10_L09", "Compare just these two.", 1.8f);
            L("S10_L10", "What happened to the current when the potential difference doubled? It also doubled", 5.0f);
            L("S10_L11", "Now we're getting somewhere.", 2.0f);
            // The steps (each gate's _GO line) and the one hint a wrong setting earns.
            L("S10_Set2V_GO", "One volt, and the ammeter reads 0.20 amperes. Now set the voltage to 2 volts.", 4.6f, proposed: true);
            L("S10_Set4V_GO", "And 4 volts.", 1.6f, proposed: true);
            L("S10_Set5V_GO", "Last one: 5 volts.", 1.8f, proposed: true);
            L("S10_Set1V_H", "Slide the battery to 1 volt.", 2.0f, proposed: true);
            L("S10_Set2V_H", "Slide the battery to 2 volts.", 2.0f, proposed: true);
            L("S10_Set3V_H", "Slide the battery to 3 volts.", 2.0f, proposed: true);
            L("S10_Set4V_H", "Slide the battery to 4 volts.", 2.0f, proposed: true);
            L("S10_Set5V_H", "Slide the battery to 5 volts.", 2.0f, proposed: true);
            // The 3 V prediction: asked after the 2 V row, judged by the ammeter after the 3 V row.
            L("S10_Predict3V_Q", "Detective's wager: at three volts, what will the ammeter read?", 3.6f, proposed: true);
            L("S10_Predict3V_R", "Locked in. Set three volts and let the ammeter judge.", 3.2f, proposed: true);
            L("S10_After3V_If_S10_Predict3V_B", "Spot on. Three volts, zero point six amperes: the pattern holds.", 4.0f, proposed: true);
            L("S10_After3V_If_S10_Predict3V_A", "The ammeter says zero point six, not zero point four. The current climbed with the voltage.", 5.0f, proposed: true);
            L("S10_After3V_If_S10_Predict3V_C", "Zero point six, not zero point eight: it rises in step, not faster.", 4.0f, proposed: true);
            // The callback to the Scene 6 prediction, keyed on what the learner picked.
            L("S10_Callback_If_S06_Prediction_A", "Back in the wire you predicted the current would rise. Your data agrees.", 4.0f, proposed: true);
            L("S10_Callback_If_S06_Prediction_B", "You predicted the current would drop. The data says it rises. Good detectives update.", 4.8f, proposed: true);
            L("S10_Callback_If_S06_Prediction_C", "You predicted no change. The data shows it rises with the push.", 3.8f, proposed: true);
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
            ("What does 'record a reading' mean?", "It means writing down what the instruments show at that moment: the voltmeter's volts and the ammeter's amperes, side by side in the observation table. One row per setting, so afterwards we can compare them and hunt for a pattern."));

        protected override void Stage()
        {
            ClassroomSet(modernBench: true, audience: false);
            Wattson(KeyPointC, StudyCamC); // Scene 9's last frame: Wattson at the key, Bench Study
            director.matchCutFrom = "Scene09_BuildCircuit";
            // Start low: the source at 0 V (Scene 9 closes the key at 0.5 V; the "rig.voltage 0" cue turns it down on a
            // match-cut), so the slider's knob, its label and the meters all read zero before the first step.
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 0);
            director.rig.showPath = false;
            director.rig.readoutsShown = false; // as Scene 9 left it; the plates and the strip come up once the needles sit at zero
            MeterPlates(true);
            Marker("Source Side", SourceSide);
            Vcam("Bench Study", StudyCamC, StudyLookC, StudyFov, refs["Bench Point"].transform, lensMedium);
        }

        protected override void Beats()
        {
            Shot("Bench Study"); // match-cut: Scene 9's last frame (circuit complete, key closed, voltage carried over)
            Ambience("RoomTone");
            Music("PadLab");
            Cue("rig.voltage 0", "start low: the source turns down to zero");
            Show("Meter Plates");
            Cue("table.rows 0", "the empty observation table (V and I) in the dock; the dock widens");
            Cue("graph.axes 1", "the empty V-I graph beside it: points plot as the rows record");
            Cue("readings.top 1", "live V / I, top-centre of the screen");
            Cue("rig.slider 1", "the battery slider, bottom-centre");
            // The live readings from the first step: the meters' plates and the top strip (0.0 V, 0.00 A) once the needles
            // have settled at zero, so nothing reads "was 0.5 V".
            Cue("rig.readouts 1", "the digital plates and the readings strip appear: 0.0 V, 0.00 A", 0.6);
            Shot("Bench Study");
            Gesture(WattsonController.Gesture.Point, point: "cells / controllable DC source", moveTo: "Source Side");
            Visual("The camera settles on the whole bench; Wattson flies in behind the source and points at it", 1.0);
            Line("S10_L01", 0.1);
            Gate("S10_Set1V", "Panel: 'Set the voltage to 1 V.' (slider steps above 1 V dim). Once it holds: 'Observe the current reading.', row 1 records from the live meters with a confirmation chime, point 1 plots.",
                "S10_Set1V_H");
            Gesture(WattsonController.Gesture.Point, point: "AMMETER");
            Gate("S10_Set2V", "Panel: 'Set the voltage to 2 V.' → observe → row 2 records, point 2 plots.", "S10_Set2V_GO", "S10_Set2V_H");

            // The wager: before the 3 V step the learner predicts the reading (recorded, not judged yet). He points at the
            // question (Thinking's spinning gadget blooms into a white streak under the chapter chip).
            Gesture(WattsonController.Gesture.Point, point: "Panel Point");
            Gate("S10_Predict3V", "Quick check (MCQ, [QC]): 'At 3 volts, what will the ammeter read?' A 0.40 A · B 0.60 A · C 0.80 A. Recorded under Relationship, no mark; Wattson: 'Locked in.'",
                "S10_Predict3V_Q", "S10_Predict3V_R");
            Gesture(WattsonController.Gesture.Point, point: "AMMETER");
            Gate("S10_Set3V", "Panel: 'Set the voltage to 3 V.' → observe → row 3, point 3.", "S10_Set3V_H");
            Gate("S10_After3V", "Say gate: the ammeter's verdict on the wager (B spot on / A climbed / C in step, not faster).",
                "S10_After3V_If_S10_Predict3V_A", "S10_After3V_If_S10_Predict3V_B", "S10_After3V_If_S10_Predict3V_C");
            Gate("S10_Set4V", "Panel: 'Set the voltage to 4 V.' → observe → row 4, point 4.", "S10_Set4V_GO", "S10_Set4V_H");
            Gate("S10_Set5V", "Panel: 'Set the voltage to 5 V.' → observe → row 5, point 5.", "S10_Set5V_GO", "S10_Set5V_H");
            Cue("rig.slider 0", "the slider goes: the data holds the attention now");

            // Look at the data: a pause to read it, then the first two rows side by side, and the doubling centre screen.
            Gesture(WattsonController.Gesture.Idle);
            Line("S10_L06");
            Line("S10_L07", 1.2);
            Gesture(WattsonController.Gesture.Point, point: "Panel Point");
            Cue("table.highlight 0 1", "rows 1 and 2 highlight");
            Line("S10_L09");
            // The doubling lands centre screen on "It also doubled" and holds through the callback; the meters' plates step
            // aside with it (behind the glass they would read through; the strip keeps the numbers).
            Cue("hero V × 2  →  I × 2|double the voltage, double the current", "the doubling lands on 'It also doubled'", 3.4);
            Hide("Meter Plates", 3.4);
            Line("S10_L10", 0.2);
            Visual("The doubling, centre screen", 1.4);
            Gate("S10_Callback", "Say gate: the Scene 6 prediction called back (A rose / B dropped / C no change).",
                "S10_Callback_If_S06_Prediction_A", "S10_Callback_If_S06_Prediction_B", "S10_Callback_If_S06_Prediction_C");
            Cue("hero.clear", "the plate goes on 'Now we're getting somewhere.'");
            Gesture(WattsonController.Gesture.Greet);
            Line("S10_L11", 0.2);
            Cue("handoff", "SceneHandoffState: table rows + camera");
            Handoff("Scene10Complete", cut: true, afterSeconds: 0.3);
        }
    }
}
