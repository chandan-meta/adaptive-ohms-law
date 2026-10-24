using System.Collections.Generic;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 10: the experiment, guided from the panel. The battery slider at the bottom of the screen; "Set the voltage
    // to 1 V." ... "5 V."; each setting is observed on the meters (the live readings top-centre), recorded into the
    // dock's table and plotted on the graph beside it (the empty graph is there from the start). With the table full the
    // data comes to the centre of the screen and Wattson, beside it, points at the first two rows and their points while
    // they are compared: double the voltage, double the current, as a centre-screen reveal. The bench stays in the free
    // part of the frame on Bench Study.
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
        // The ammeter's face as the first reading lands (between the 1 V and 2 V steps): 0.93 m from the dial, from a little
        // left and above so Wattson at Source Side (behind, left) stays out of the frame; FOV 30 holds the dial, the needle at
        // 0.20 A and the digital plate above it (a billboard, it turns to this lens); the live strip is HUD. The dial sits on
        // the authored centre, so the dock's crop (PanelFraming) keeps it; the voltmeter's edge shows at the right.
        static readonly Vector3 MeterCloseCamC = new Vector3(-0.62f, 1.22f, -0.74f), MeterCloseLookC = new Vector3(-0.28f, 0.95f, 0.08f);
        const float MeterCloseFov = 30f;

        // The lab bed Scenes 8 and 9 cue (PadLabTheme, the library alias of the Wattson theme). A bed the director cannot find
        // fades the bed out (SceneDirector.Bed), so a clip missing from the bank at build time falls back to the old one
        // here, logged. Scenes 10-13 name their library clips directly (the runtime resolves them through the library).
        public static string LabTheme(IReadOnlyDictionary<string, AudioClip> sounds) => Bed(sounds, "PadLabTheme", "PadLab");
        public static string Bed(IReadOnlyDictionary<string, AudioClip> sounds, string clip, string fallback)
        {
            if (sounds.TryGetValue(clip, out var found) && found) return clip;
            Debug.Log($"Sound bank has no '{clip}' yet (WP-A); the bed cue uses '{fallback}'.");
            return fallback;
        }

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
            Vcam("Meter Close", MeterCloseCamC, MeterCloseLookC, MeterCloseFov, refs["AMMETER"].transform, lensWide); // the deep lens: dial and plate both sharp
        }

        protected override void Beats()
        {
            Shot("Bench Study"); // match-cut: Scene 9's last frame (circuit complete, key closed, voltage carried over)
            Ambience("RoomTone");
            Music("BGM_Conductor_Pulse"); // audio: the experiment's own bed, crossfaded in from the Wattson theme over 2.5 s at the scene start
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
            // The first reading has landed (row 1: 1.0 V, 0.20 A): a cut onto the ammeter's face, the needle at 0.20 A and its
            // plate, then back onto the whole bench before the 2 V step opens with its line ("One volt, and the ammeter reads
            // 0.20 amperes..."): the slider is HUD, and the gate framing stays Bench Study, settled 0.2 s before the gate.
            Shot("Meter Close", cut: true);
            Gesture(WattsonController.Gesture.Point, point: "AMMETER");
            Visual("Close on the ammeter: the needle at 0.20 A, the plate reads 0.20 A", 2.6);
            Shot("Bench Study");
            Wait(1.2);
            Gate("S10_Set2V", "Panel: 'Set the voltage to 2 V.' → observe → row 2 records, point 2 plots.", "S10_Set2V_GO", "S10_Set2V_H");
            Gate("S10_Set3V", "Panel: 'Set the voltage to 3 V.' → observe → row 3, point 3.", "S10_Set3V_H");
            Gate("S10_Set4V", "Panel: 'Set the voltage to 4 V.' → observe → row 4, point 4.", "S10_Set4V_GO", "S10_Set4V_H");
            Gate("S10_Set5V", "Panel: 'Set the voltage to 5 V.' → observe → row 5, point 5.", "S10_Set5V_GO", "S10_Set5V_H");
            Sfx("SFX_Experiment_Complete", 0.5); // audio: the table is full, 0.7 s after the gate (after row 5's own Record chime)
            Cue("rig.slider 0", "the slider goes: the data holds the attention now");

            // Look at the data, centre screen: the table and the graph come forward (the dock steps back) with Wattson
            // beside them; a pause to read it, then the first two rows and their points, linked, while he points at each.
            Gesture(WattsonController.Gesture.Idle);
            Cue("stage 1", "data stage: the table and the graph come to the centre of the screen; Wattson stands beside them");
            Line("S10_L06", 0.2);
            Line("S10_L07", 1.2);
            Cue("link 0 1", "rows 1 and 2 light with their points; a spark runs from each row to its point");
            Cue("point.row 0", "Wattson points at row 1 (1.0 V, 0.20 A)");
            Cue("point.row 1", "then row 2 (2.0 V, 0.40 A)", 1.0);
            Line("S10_L09");
            // The doubling lands centre screen on "It also doubled" and holds through the callback: the data steps back first
            // and the meters' plates step aside with it (behind the glass they would read through; the strip keeps the
            // numbers). He points at the second point on "doubled" before the stage goes.
            // Audio: the hero rule's SFX_Scientific_Discovery alone. This is the first clue, not the verdict: the bigger stings
            // are kept for V ∝ I (Scene 11, with its rise and music change) and OHM'S LAW (Scene 13).
            Cue("point.point 1", "Wattson points at point 2 on the graph, (0.40, 2.0)", 1.4);
            Cue("stage 0", "the data returns to the dock for the reveal", 3.1);
            Cue("hero V × 2  →  I × 2|double the voltage, double the current", "the doubling lands on 'It also doubled' (his snap reveals it)", 3.4);
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
