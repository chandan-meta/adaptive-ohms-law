using TMPro;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 10: Become the Experimenter. Set 1 V and record it; predict the 2 V reading, set 2 V and record it (the meter
    // grades the prediction); auto-run 3-5 V; spot the pattern; find the doubling; call back the Scene 6 prediction.
    // Classroom bench (match-cut from Scene 9 on Bench Close); the observation table lives on the teaching board.
    // Gate lines follow the gate contract in Docs/Polish/WOW_PLAN.md §8; every one of them is optional at runtime.
    public class Scene10Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene10";
        public override string SceneName => "Scene10_Experiment";
        public override string Title => "Become the Experimenter";

        // The chalk discovery beside rows 1-2 (storyboard text, exact). Scene 11 opens on the same board, so it rebuilds this
        // chalk at the same UV, cap and alignment for the match-cut. Table rows 1-2 sit at board v 0.49-0.64; with the tags
        // beside the table's frame (u 0.34-0.43) the chalk starts clear of them at u 0.52.
        public const string DiscoveryText = "V × 2 → I × 2";
        public static readonly Vector2 DiscoveryUV = new Vector2(0.52f, 0.56f);

        // Wattson behind the bench for the Bench Close beats, above the source's back-left corner: the lens's top edge is
        // level at y ~1.42, so his head (pivot + 0.25 m, ±0.12 m bob) stays in frame, and he shows from the waist up over
        // the source (on screen ~70-270 px across, head ~40-190 px down, boots behind the source's top edge at ~360-400 px).
        // His pointing arm ends ~430 px across, clear of the prompt pill (from ~460 px) and the ammeter readout (~610 px).
        static readonly Vector3 SourceSide = new Vector3(-1.35f, 0.98f, 1.1f);

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

            // S10_Set1V: a wrong setting held (once).
            L("S10_Set1V_H", "Slide the battery knob to the 1 V tick.", 2.8f, proposed: true);

            // S10_Predict2V → S10_Set2V → S10_Record2. Values quoted are guaranteed by the gates: row 1 records only at 1 V
            // (0.20 A) and row 2 only at 2 V (0.40 A). The feedback names the prediction and the meter, not the doubling:
            // S10_L08-S10_L10 reveal that.
            L("S10_Predict2V_Q", "Detective prediction: 1 volt gave 0.20 amperes. What will 2 volts give?", 4.4f, proposed: true);
            L("S10_Predict2V_OK", "Locked in. Let's test it.", 1.8f, proposed: true);
            L("S10_Set2V_GO", "Now set the battery to 2 volts.", 2.4f, proposed: true);
            L("S10_Set2V_H", "Slide the knob to the 2 V tick.", 2.4f, proposed: true);
            L("S10_Record2_GO", "Moment of truth! Record the ammeter reading.", 2.8f, proposed: true);
            L("S10_Predict2V_FA", "You said 0.40, and the meter agrees. Your hunch survived the test.", 3.8f, proposed: true);
            L("S10_Predict2V_FB", "You said it would stay at 0.20, but the meter jumped to 0.40.", 3.8f, proposed: true);
            L("S10_Predict2V_FC", "You expected less current, but the meter climbed to 0.40.", 3.4f, proposed: true);

            // S10_Pattern (adaptive). No _FA: S10_L08 ("Good. ...") follows a right answer.
            L("S10_Pattern_FB", "Check the I column: each row is bigger than the one above.", 3.4f, proposed: true);
            L("S10_Pattern_FC", "There is one! Read the amperes from top to bottom.", 3.0f, proposed: true);
            L("S10_Pattern_E", "Rows 1 and 2: when the potential difference went up, what did the current do?", 4.6f, proposed: true);
            L("S10_Pattern_X", "Watch the columns: the volts climb row by row, and so do the amperes.", 4.0f, proposed: true);

            // S10_Discover: callback to the Scene 6 prediction (A increase / B decrease / C stay the same).
            L("S10_Discover_FA", "Back in the wire you predicted the current would rise. Your data agrees.", 4.0f, proposed: true);
            L("S10_Discover_FB", "You predicted the current would drop. The data says it rises. Good detectives update.", 4.8f, proposed: true);
            L("S10_Discover_FC", "You predicted no change. The data shows it rises with the push.", 3.8f, proposed: true);
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
            ClassroomSet(modernBench: true, audience: false);
            Wattson(WattsonHomeC, BenchWideCamC); // where Scene 9 leaves him (just out of the Bench Close frame); he flies in
            director.matchCutFrom = "Scene09_BuildCircuit";
            // Scene 9 closed the key at the small default voltage; the hand-off carries the live value across the cut.
            director.rig = Rig(assembled: true, keyClosed: true, voltage: CircuitRig.KeyClosedDefaultVolts);
            director.rig.showPath = false; // Scene 9 cues "rig.path 0" before its last frame: plain cables on both sides of the cut (Scenes 11-13 too)
            director.rig.readoutsShown = false; // plates hidden since Scene 9; they come up with the 1 V baseline (rig.readouts 1 after S10_Set1V)
            Boards(withTable: true, withGraph: false);

            // Wattson's spots: behind the source for the bench beats (see SourceSide), and the kit's spot beside the board's
            // left rail for the Boards shot, clear of the table's V column, where Scenes 11-13 hold him (match-cut).
            Marker("Source Side", SourceSide);
            Marker("Table Side", TableSideC);

            // The S10_Discover gate reveals the ref "Card Discovery" (SceneDirector.Gates), so the chalk takes that name.
            Board(DiscoveryText, palette.current, DiscoveryUV, BoardsShotTitleCap, align: TextAlignmentOptions.Left, name: "Discovery")
                .name = "Card Discovery";
        }

        protected override void Beats()
        {
            Shot("Bench Close"); // match-cut: Scene 9's last frame (circuit complete, key closed, voltage carried over)
            Cue("table.rows 0", "empty observation table with headers");
            Gesture(WattsonController.Gesture.Point, point: "cells / controllable DC source", moveTo: "Source Side");
            Cue("rig.slider 1", "voltage controller appears at the carried voltage (0-5 V in 1 V steps)");
            Visual("Voltage controller appears by the source; Wattson flies in behind the source and points at it", 0.8);
            Line("S10_L01", 0.2);
            Cue("prompt Let's start low. Set the battery to 1 volt.", "on-screen prompt takes over as the subtitle ends", -0.2);
            Gate("S10_Set1V", "Set the slider to 1 V (mouse/touch, or ask Wattson). Ammeter needle moves to 0.20 A. A wrong setting held 0.8 s: prompt 'N V now. Set the battery to 1 V.' + S10_Set1V_H once. autoAnswer sets it.",
                "S10_Set1V_H");
            Cue("rig.readouts 1", "the digital plates appear at the baseline: 1.0 V, 0.20 A (the first numbers the learner reads)");
            Cue("prompt", "");
            Gesture(WattsonController.Gesture.Point, point: "VOLTMETER");
            Line("S10_L02");
            Gesture(WattsonController.Gesture.Point, point: "AMMETER");
            Line("S10_L03", 0.2);
            Cue("prompt Record the current reading in ammeter.", "on-screen prompt takes over as the subtitle ends", -0.2);
            Gate("S10_Record", "Tap RECORD (or the ammeter). Row 1 takes the meters' live values (1, 0.2); only at 1 V, otherwise the controls open and the prompt says to go back to 1 V.");
            Cue("prompt", "");

            // Predict, then test: the learner predicts the 2 V reading, sets 2 V and records it, and the meter grades the
            // prediction (the 2 V step is the learner's; 3-5 V stay automatic).
            Gesture(WattsonController.Gesture.Thinking);
            Gate("S10_Predict2V", "MCQ (prediction, panel 740 px docked right, clear of the meters): At 1 V the ammeter read 0.20 A. What will it read at 2 V? A. 0.40 A · B. 0.20 A · C. 0.10 A. Stored in LearnerSession.s10Prediction2V, marked 'your prediction', S10_Predict2V_OK; graded at S10_Record2.",
                "S10_Predict2V_Q", "S10_Predict2V_OK");
            Gesture(WattsonController.Gesture.Point, point: "cells / controllable DC source");
            Line("S10_Set2V_GO", 0.2);
            Cue("prompt Set the battery to 2 volts.", "on-screen prompt takes over as the subtitle ends", -0.2);
            Gate("S10_Set2V", "Set the slider to 2 V (or ask Wattson). Ammeter needle moves to 0.40 A. A wrong setting held 0.8 s: prompt 'N V now. Set the battery to 2 V.' + S10_Set2V_H once. autoAnswer sets it.",
                "S10_Set2V_H");
            Cue("prompt", "");
            Gesture(WattsonController.Gesture.Point, point: "AMMETER");
            Line("S10_Record2_GO", 0.2);
            Cue("prompt Record the current reading in ammeter.", "on-screen prompt takes over as the subtitle ends", -0.2);
            Gate("S10_Record2", "Tap RECORD (or the ammeter). Row 2 takes the meters' live values (2, 0.4); only at 2 V. Then the prediction is graded against the meter (LearnerSession Relationship) and Wattson speaks S10_Predict2V_FA/_FB/_FC for the learner's pick.",
                "S10_Predict2V_FA", "S10_Predict2V_FB", "S10_Predict2V_FC");
            Cue("prompt", "");

            Gesture(WattsonController.Gesture.Talking);
            Line("S10_L04");
            // 3 V on the dials (knob, needles, readouts, slider), then the camera rises to the board (1 s blend), where rows
            // 3-5 fill in. The battery control leaves with the bench (in the Boards shot it would sit over table rows 3-5);
            // the live readings card (HUD, bottom right: V and I only, no R in Scene 10) keeps the meters' values in view
            // while the bench is out of frame, and leaves once the run is done so the table is the data.
            Cue("rig.voltage 3", "controller steps to 3 V; meters update");
            Visual("Auto step to 3 V on the bench dials", 0.8);
            Shot("Boards");
            Cue("rig.slider 0", "voltage controller hidden with the bench");
            Cue("readings 1", "live V / I card bottom-right above the subtitles: 3.0 V, 0.60 A, then each step with ▲ and 'was'");
            Gesture(WattsonController.Gesture.Point, point: "Observation Table", moveTo: "Table Side");
            Cue("table.rows 3", "row 3 auto-fills as the board comes into view", 0.6);
            Visual("Camera rises to the observation table on the board", 1.0);
            for (int v = 4; v <= 5; v++)
            {
                Cue("rig.voltage " + v, $"controller steps to {v} V; meters update");
                Cue("table.rows " + v, $"row {v} auto-fills", 0.6);
                Visual($"Auto step to {v} V", 1.5);
            }
            Cue("readings 0", "live readings card leaves: the table holds the data now");
            Gesture(WattsonController.Gesture.Idle, look: "Observation Table");
            Line("S10_L05");
            Line("S10_L06");
            Line("S10_L07", 0.2);
            Gate("S10_Pattern", "MCQ (graded, adaptive): A. Current increases as potential difference increases. · B. Current decreases. · C. There is no pattern. (correct A) → LearnerSession.s10PatternAnswer. Wrong: S10_Pattern_FB/_FC, panel hint, S10_R_WRONG (not for a Confident learner), re-ask with the wrong option out; a Struggling learner gets rows 1-2 highlighted and the easier question (S10_Pattern_E). Out of retries: the V then I column sweep with S10_Pattern_X.",
                "S10_R_WRONG", "S10_Pattern_FB", "S10_Pattern_FC", "S10_Pattern_E", "S10_Pattern_X");

            Gesture(WattsonController.Gesture.Point, point: "Observation Table");
            Line("S10_L08", 0.2);
            Cue("table.highlight 0 1", "rows 1 and 2 highlight: 1 V → 0.2 A and 2 V → 0.4 A");
            Line("S10_L09");
            Line("S10_L10", 0.2);
            Gate("S10_Discover", "Tap either highlighted row or the ×2 chip to reveal V × 2 → I × 2 beside rows 1-2. Auto-reveals after 5 s. Then the Scene 6 callback: S10_Discover_FA (predicted increase) / _FB (decrease) / _FC (no change).",
                "S10_Discover_FA", "S10_Discover_FB", "S10_Discover_FC");
            Gesture(WattsonController.Gesture.Greet);
            Line("S10_L11", 0.2);
            Cue("handoff", "SceneHandoffState: table rows + camera; Wattson at TableSideC, rows 1-2 highlighted and the V × 2 → I × 2 chalk up for Scene 11's match-cut");
            Handoff("Scene10Complete", cut: true, afterSeconds: 0.4);
        }
    }
}
