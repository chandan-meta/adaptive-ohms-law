using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Grade = OhmsLaw.LearnerSession.Grade;

namespace OhmsLaw
{
    // Every learner interaction in the module, by gate id. Each one works with mouse and touch, never traps the
    // learner, and completes by itself when ReviewMode.autoAnswer is on. The line ids each gate speaks are listed in
    // Docs/Polish/WOW_PLAN.md, "Gate contract"; a missing line is skipped. The bench activities (set, record, target
    // readings) live in SceneDirector.Experiment.cs.
    public partial class SceneDirector
    {
        Dictionary<string, DialogueLine> sceneLines;
        QuestionRun lastRun; // the last Ask(): its final grade, pick and answer count

        // A gate's line by id; a line declared on another gate of this scene is found too (S10_Record2 speaks
        // S10_Predict2V's feedback).
        DialogueLine Line(InteractionGate gate, string id)
        {
            var line = gate ? gate.lines.FirstOrDefault(l => l && l.id == id) : null;
            if (line) return line;
            if (sceneLines == null)
            {
                sceneLines = new Dictionary<string, DialogueLine>();
                if (TryGetComponent(out PlayableDirector director) && director.playableAsset is TimelineAsset timeline && timeline.markerTrack)
                    foreach (var other in timeline.markerTrack.GetMarkers().OfType<InteractionGate>())
                        foreach (var l in other.lines)
                            if (l && !sceneLines.ContainsKey(l.id)) sceneLines[l.id] = l;
            }
            return sceneLines.TryGetValue(id, out line) ? line : null;
        }

        IEnumerator RunGate(InteractionGate gate)
        {
            switch (gate.gateId)
            {
                case "Test_MCQ": yield return TestMcq(gate); break;
                case "S05_Drift": yield return Ask(gate, QuestionBank.S05Drift); break;
                case "S06_Prediction": yield return Prediction(gate); break;
                case "S09_PlaceAmmeter": yield return PlacePart(gate, "ammeter"); break;
                case "S09_PlaceVoltmeter": yield return PlacePart(gate, "voltmeter"); break;
                case "S09_CloseKey": yield return CloseKey(); break;
                // Before the S10_Predict2V prediction the controls stop at 1 V (no peeking at the 2 V reading).
                case "S10_Set1V": ControlsMax = 1f; yield return SetVoltage(gate, 1); break;
                case "S10_Record": ControlsMax = 1f; yield return Record(gate, 0); break;
                case "S10_Predict2V": yield return Predict2V(gate); break;
                case "S10_Set2V": yield return SetVoltage(gate, 2); break;
                case "S10_Record2": yield return Record(gate, 1); yield return ComparePrediction(gate); break;
                case "S10_Pattern": yield return Pattern(gate); break;
                case "S10_Discover": yield return Discover(gate); break;
                case "S11_Target": yield return Target(gate, Concepts.Graph, 0.60f, bonusAmps: 0.80f); break;
                case "S12_Ratio": yield return Ratio(gate); break;
                case "S12_Slope": yield return Slope(gate); break;
                case "S12_AQ1": if (checkpointEnabled) yield return AQ1(gate); break;
                case "S13_AQ2": if (checkpointEnabled) yield return AQ2(gate); break;
                case "S13_Apply": yield return Target(gate, Concepts.OhmsLaw, 0.80f); break;
                case "S13_Checkpoint": break; // retired: AQ1 moved to S12_AQ1, AQ2 to S13_AQ2 (an older Scene 13 build still has the marker)
                default:
                    if (IsSayGate(gate)) yield return SayGate(gate);
                    else Debug.LogWarning($"{sceneId}: no handler for gate '{gate.gateId}'", this);
                    break;
            }
        }

        // ------------------------------------------------------------------ say gates (branch lines)

        // A gate with no handler of its own whose lines are all "<gate>" or "<gate>_If_<condition>" is a say gate: the
        // Timeline holds while Wattson says ONE line, the first _If_ line (in the gate's order) whose condition holds for
        // this learner (LearnerSession.Holds), else "<gate>", else nothing. E.g. Scene 9: "S09_Conventional" for everyone,
        // "S09_Conventional_If_S05_Drift_B" for a learner who said electrons flow + to −. TriggerOnce, like every gate.
        public const string IfTag = "_If_";

        public static bool IsSayGate(InteractionGate gate)
        {
            bool any = false;
            foreach (var line in gate.lines)
            {
                if (!line) continue;
                if (line.id != gate.gateId && !line.id.StartsWith(gate.gateId + IfTag)) return false;
                any = true;
            }
            return any;
        }

        // The line a say gate speaks for this learner, or null.
        public static DialogueLine SayGateLine(InteractionGate gate)
        {
            string prefix = gate.gateId + IfTag;
            var chosen = gate.lines.FirstOrDefault(l => l && l.id.StartsWith(prefix) && LearnerSession.Holds(l.id.Substring(prefix.Length)));
            return chosen ? chosen : gate.lines.FirstOrDefault(l => l && l.id == gate.gateId);
        }

        IEnumerator SayGate(InteractionGate gate) { yield return Say(SayGateLine(gate)); }

        // Wattson's quick reaction to a graded answer, layered over whatever gesture he is in.
        static void React(Grade grade)
        {
            var reacting = WattsonController.Active;
            if (reacting) reacting.React(grade == Grade.Correct ? WattsonController.Reaction.Correct
                : grade == Grade.Partial ? WattsonController.Reaction.Partial : WattsonController.Reaction.Incorrect);
        }

        IEnumerator TestMcq(InteractionGate gate)
        {
            var panel = new MCQPanel("Framework test: which option is correct?", new[] { "A. This one", "B. Not this", "C. Nor this" });
            yield return panel.WaitForPick(0);
            panel.Close();
            yield return Say(Line(gate, panel.Picked == 0 ? "TEST_RIGHT" : "TEST_WRONG"));
        }

        // Scene 6: the prediction is stored, never judged here. Each option gets its own (proposed) reaction.
        IEnumerator Prediction(InteractionGate gate)
        {
            var panel = new MCQPanel("If we increase the potential difference while everything else stays the same, what do you predict will happen to the current?",
                new[] { "A. Increase", "B. Decrease", "C. Stay the same" }, dockRight: true, width: 740f); // left edge ~1130 px, clear of the counter card
            yield return panel.WaitForPick(0);
            panel.Close();
            LearnerSession.prediction = LearnerSession.Letter(panel.Picked);
            yield return Say(Line(gate, "S06_R_" + LearnerSession.prediction));
        }

        // Scene 9: drag or tap the part into its glowing slot. AutoPlace and autoAnswer fly it in.
        IEnumerator PlacePart(InteractionGate gate, string part)
        {
            if (rig.IsPlaced(part)) yield break;
            bool auto = buildMode == BuildMode.AutoPlace || ReviewMode.autoAnswer;
            rig.draggable = part; // its slot glows in auto runs too, until the part lands (CircuitRig clears it)
            if (auto) { yield return Wait(auto && buildMode != BuildMode.AutoPlace ? ReviewMode.autoDelay : 1f); rig.Place(part); }
            float waited = 0;
            while (!rig.IsPlaced(part))
            {
                if (!AskWattsonUI.IsOpen) waited += Dt;
                if (ReviewMode.autoAnswer && waited > ReviewMode.autoDelay + 2f) rig.PlaceNow(part);
                yield return null;
            }
            rig.draggable = "";
            React(Grade.Correct); // the part snapped in (gesture only, no line)
        }

        IEnumerator CloseKey()
        {
            if (rig.state.keyClosed) yield break;
            rig.keyArmed = true;
            // The fingertip on the lever's grip, not the base, so the hand points at what the learner taps.
            var grip = rig.keyLever ? rig.keyLever.Find("Handle") : null;
            StartCoroutine(TapHint(grip ? grip : rig.key, () => rig.state.keyClosed, new Vector2(0, -FingerTip)));
            float waited = 0;
            while (!rig.state.keyClosed)
            {
                if (!AskWattsonUI.IsOpen) waited += Dt;
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay) rig.CloseKey();
                yield return null;
            }
            React(Grade.Correct);
            // CircuitRig.CloseKey applies the small default voltage (0.5 V) so the needles move; Scene 10 starts from it.
        }

        // ------------------------------------------------------------------ graded questions

        // Puts the panel up while `spoken` plays (cut short if the learner answers first); autoAnswer waits for the line.
        IEnumerator Pick(MCQPanel panel, DialogueLine spoken)
        {
            var saying = spoken ? StartCoroutine(Say(spoken)) : null;
            yield return panel.WaitForPick(0, () => spoken && Speaking == spoken);
            StopSaying(saying, spoken);
        }

        // A graded, adaptive question (QuestionRun records every answer). The question line plays while the panel is up;
        // each answer gets its option's feedback line (<gate>_FA/_FB/_FC) and a mark. Correct: the reasoning shows on the
        // panel. Partial: acknowledged, and the correct option is shown with the reasoning. Incorrect: the option is marked
        // and disabled, the panel shakes and shows the hint, `scaffold` runs if the learner is now Struggling with the concept,
        // the spoken hint plays unless they came in Confident, and they try again, on `easier` when `simplify` says so. When
        // the retries are used up: the answer is marked, and Wattson shows it with `reveal` (an experiment, with the panel
        // out of the way) or says <gate>_X.
        IEnumerator Ask(InteractionGate gate, Question question, System.Action<MCQPanel> scaffold = null,
            Question easier = null, System.Func<bool> simplify = null, System.Func<IEnumerator> reveal = null)
        {
            string id = gate.gateId;
            bool confident = LearnerSession.Confident(question.concept);
            var hinted = new HashSet<DialogueLine>(); // each spoken hint plays once per question; later retries keep the panel hint
            var run = lastRun = new QuestionRun(question);
            var spoken = Line(gate, question.questionLine ?? id + "_Q");
            MCQPanel panel = null;
            while (true)
            {
                var q = run.Question;
                if (panel == null) panel = new MCQPanel(q.text, q.Texts, dockRight: q.dockRight, width: q.width);
                yield return Pick(panel, spoken);
                spoken = null;
                int pick = panel.Picked;
                var step = run.Answer(pick);
                React(step.grade);

                var feedback = Line(gate, id + "_F" + LearnerSession.Letter(pick));
                if (!feedback && step.grade == Grade.Correct && q.correctFallbackLine != null) feedback = Line(gate, q.correctFallbackLine);
                if (step.grade == Grade.Incorrect)
                {
                    panel.Disable(pick, MCQPanel.WrongMark);
                    yield return panel.Shake();
                }
                else
                {
                    panel.Mark(pick, step.grade == Grade.Correct ? MCQPanel.RightMark : MCQPanel.PartMark);
                    panel.Mark(q.CorrectIndex, MCQPanel.RightMark);
                    panel.Hint(q.reveal);
                }
                yield return Say(feedback);
                if (step.grade != Grade.Incorrect) { yield return Wait(1f); break; }

                if (step.retry)
                {
                    panel.Hint(q.hint);
                    if (scaffold != null && LearnerSession.Struggling(question.concept)) scaffold(panel);
                    if (easier != null && run.Question != easier && simplify != null && simplify())
                    {
                        panel.Close();
                        panel = null;
                        run.Simplify(easier);
                        spoken = Line(gate, easier.questionLine ?? id + "_E");
                    }
                    var hint = Line(gate, q.hintLine ?? id + "_H");
                    if (!confident && hint && hinted.Add(hint)) yield return Say(hint);
                    continue;
                }

                panel.Mark(q.CorrectIndex, MCQPanel.RightMark);
                panel.Hint(q.reveal);
                if (reveal != null)
                {
                    yield return Wait(1.2f);
                    panel.Close();
                    panel = null;
                    yield return reveal();
                }
                else
                {
                    yield return Say(Line(gate, id + "_X"));
                    yield return Wait(1f);
                }
                break;
            }
            if (panel != null) panel.Close();
        }

        // Speaks `line` while `show` plays; done when both are.
        IEnumerator SayWhile(DialogueLine line, IEnumerator show)
        {
            bool spoken = false;
            StartCoroutine(Then(Say(line), () => spoken = true));
            yield return show;
            while (!spoken) yield return null;
        }

        static IEnumerator Then(IEnumerator first, System.Action after)
        {
            yield return first;
            after();
        }

        // Scene 10, after row 1: predict the 2 V reading. Not graded here: S10_Record2 compares it with the meter.
        IEnumerator Predict2V(InteractionGate gate)
        {
            var q = QuestionBank.S10Predict2V;
            var panel = new MCQPanel(q.text, q.Texts, dockRight: q.dockRight, width: q.width);
            yield return Pick(panel, Line(gate, gate.gateId + "_Q"));
            LearnerSession.s10Prediction2V = LearnerSession.Letter(panel.Picked);
            panel.Mark(panel.Picked, UI.AccentFill); // "your prediction", neither right nor wrong yet
            yield return Say(Line(gate, gate.gateId + "_OK"));
            yield return Wait(0.6f);
            panel.Close();
        }

        // Scene 10, after row 2 is recorded at 2 V: the prediction against the meter (S10_Predict2V_FA/_FB/_FC).
        IEnumerator ComparePrediction(InteractionGate gate)
        {
            string letter = LearnerSession.s10Prediction2V;
            if (letter == "") yield break; // the scene was entered by a jump past the prediction
            var grade = QuestionBank.GradePrediction(letter, out var misconception);
            LearnerSession.Record(Concepts.Relationship, grade, misconception);
            LearnerSession.answers.Add($"S10_Predict2V {letter} {grade}");
            React(grade);
            yield return Say(Line(gate, "S10_Predict2V_F" + letter));
        }

        // Scene 10 pattern question. Wrong: feedback, the storyboard nudge S10_R_WRONG, re-ask with the wrong option out;
        // a learner now struggling with the relationship gets rows 1-2 highlighted and the simpler question instead. Out of
        // retries: the V column then the I column light up row by row while Wattson says S10_Pattern_X.
        IEnumerator Pattern(InteractionGate gate)
        {
            yield return Ask(gate, QuestionBank.S10Pattern,
                scaffold: _ => { table.Highlight(0, true, table.Pair(0)); table.Highlight(1, true, table.Pair(1)); },
                easier: QuestionBank.S10PatternEasy, simplify: () => LearnerSession.Struggling(Concepts.Relationship),
                reveal: () => SayWhile(Line(gate, "S10_Pattern_X"), SweepColumns()));
            LearnerSession.s10PatternAttempts = lastRun.Answers;
            LearnerSession.s10PatternAnswer = LearnerSession.Letter(lastRun.LastPick);
            table.ClearHighlights(); // S10_L08's cue brings rows 1-2 back with their tags
        }

        IEnumerator SweepColumns()
        {
            yield return table.Sweep(0);
            yield return table.Sweep(1);
        }

        // Scene 10: tap either highlighted row (or the ×2 chip) to reveal the doubling. Reveals itself after 5 s. Then the
        // callback to the learner's Scene 6 prediction: S10_Discover_FA (matched) / _FB / _FC (revised).
        IEnumerator Discover(InteractionGate gate)
        {
            bool revealed = false;
            void OnRow(int row) { if (row <= 1) revealed = true; }
            table.RowTapped += OnRow;
            var chip = ScreenButton("Times Two", "× 2", new Vector2(720, 250), () => revealed = true);
            for (float t = 0; t < 5f && !revealed; t += Dt) yield return null;
            table.RowTapped -= OnRow;
            Destroy(chip.gameObject);
            SetActive("Card Discovery", true);
            if (sfx && pop) sfx.PlayOneShot(pop);
            yield return Wait(1.5f);
            if (LearnerSession.prediction != "") yield return Say(Line(gate, "S10_Discover_F" + LearnerSession.prediction));
        }

        // Scene 12: the learner works row 3's V / I (in place of Wattson working it); the answer fills the cell.
        IEnumerator Ratio(InteractionGate gate)
        {
            table.ShowRatio(true);
            table.Highlight(2, true);
            yield return Ask(gate, QuestionBank.S12Ratio, reveal: () => SayWhile(Line(gate, "S12_Ratio_X"), WorkRow(2)));
            table.Highlight(2, false);
            table.SetRatio(2, true);
        }

        IEnumerator WorkRow(int row)
        {
            table.SetRatio(row, true);
            yield return Wait(1.5f);
        }

        // Scene 12 slope (storyboard: wrong -> shake, show 2.0 ÷ 0.40, re-ask once, then continue). Adaptive: a learner
        // struggling with the graph also gets the triangle's two corners lit and the plot pulsed; one who came in
        // confident gets the on-panel hint without the spoken one.
        IEnumerator Slope(InteractionGate gate)
        {
            bool lit = false;
            yield return Ask(gate, QuestionBank.S12Slope,
                scaffold: _ => { lit = true; graph.Highlight(0, true); graph.Highlight(2, true); graph.Pulse(); },
                reveal: () => SayWhile(Line(gate, "S12_Slope_X"), PulseGraph()));
            LearnerSession.slopeAnswer = QuestionBank.S12Slope.options[lastRun.LastPick].text;
            if (lit) { graph.Highlight(0, false); graph.Highlight(2, false); }
        }

        IEnumerator PulseGraph()
        {
            if (graph) graph.Pulse();
            yield return Wait(1.5f);
        }

        // Storyboard AQ1, now in Scene 12 after resistance is defined. Skipped for a learner already confident with the
        // relationship. Wrong: feedback, AQ_EXPLAIN, then AQ_EASY; out of retries: Wattson turns the source to 2 V and
        // then 4 V while the live readings show the current double (S12_AQ1_X), and puts it back.
        IEnumerator AQ1(InteractionGate gate)
        {
            if (LearnerSession.Confident(Concepts.Relationship)) yield break;
            yield return Ask(gate, QuestionBank.AQ1, easier: QuestionBank.AQEasy, simplify: () => true, reveal: () => Reveal4V(gate));
        }

        // The readings strip shows 4 V / 0.80 A for as long as S12_AQ1_X says so (Demonstrate alone would restore the source
        // and drop the strip at ~3.6 s of the 5.8 s line); then the source goes back to where it was.
        IEnumerator Reveal4V(InteractionGate gate)
        {
            bool hadReadings = readings;
            float before = rig ? rig.state.voltage : 0;
            Readings(true);
            yield return SayWhile(Line(gate, "S12_AQ1_X"), Demonstrate(true, 2f, 4f));
            if (rig) rig.SetVoltage(before);
            if (!hadReadings) Readings(false);
        }

        // Storyboard AQ2, now in Scene 13. A learner struggling with resistance gets the table's own numbers as the hint
        // and the V / I column lit.
        IEnumerator AQ2(InteractionGate gate)
        {
            bool lit = false;
            yield return Ask(gate, QuestionBank.AQ2,
                scaffold: panel => { panel.Hint("Our wire: 5.0 V ÷ 1.00 A = 5 Ω. Same I, larger V → larger or smaller R?"); if (table) { lit = true; table.GlowColumn(2, true); } });
            if (lit) table.GlowColumn(2, false);
        }
    }
}
