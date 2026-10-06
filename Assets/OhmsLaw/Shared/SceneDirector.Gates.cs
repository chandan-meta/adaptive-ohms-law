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
    // learner, and completes by itself when ReviewMode.autoAnswer is on. The instruction for every step is written to
    // the lesson panel before the learner is expected to act. The bench activities (set a voltage, which records its row)
    // live in SceneDirector.Experiment.cs.
    public partial class SceneDirector
    {
        Dictionary<string, DialogueLine> sceneLines;

        // A gate's line by id; a line declared on another gate of this scene is found too.
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
                case "S06_Prediction": yield return Prediction(gate); break;
                case "S09_PlaceAmmeter": yield return PlacePart(gate, "ammeter"); break;
                case "S09_PlaceVoltmeter": yield return PlacePart(gate, "voltmeter"); break;
                case "S09_CloseKey": yield return CloseKey(); break;
                // Scene 10: the learner sets 1 V, 2 V ... 5 V in turn; each setting records its row and plots its point.
                case "S10_Set1V": yield return SetVoltage(gate, 1); break;
                case "S10_Set2V": yield return SetVoltage(gate, 2); break;
                case "S10_Set3V": yield return SetVoltage(gate, 3); break;
                case "S10_Set4V": yield return SetVoltage(gate, 4); break;
                case "S10_Set5V": yield return SetVoltage(gate, 5); break;
                default:
                    if (IsSayGate(gate)) yield return SayGate(gate);
                    else Debug.LogWarning($"{sceneId}: no handler for gate '{gate.gateId}'", this);
                    break;
            }
        }

        // ------------------------------------------------------------------ say gates (branch lines)

        // A gate with no handler of its own whose lines are all "<gate>" or "<gate>_If_<condition>" is a say gate: the
        // Timeline holds while Wattson says ONE line, the first _If_ line (in the gate's order) whose condition holds for
        // this learner (LearnerSession.Holds), else "<gate>", else nothing. TriggerOnce, like every gate.
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

        // Wattson's quick reaction to what the learner did, layered over whatever gesture he is in.
        static void React(Grade grade)
        {
            var reacting = WattsonController.Active;
            if (reacting) reacting.React(grade == Grade.Correct ? WattsonController.Reaction.Correct
                : grade == Grade.Partial ? WattsonController.Reaction.Partial : WattsonController.Reaction.Incorrect);
        }

        IEnumerator TestMcq(InteractionGate gate)
        {
            Instruction("Tap an answer.");
            var panel = new MCQPanel("Framework test: which option is correct?", new[] { "A. This one", "B. Not this", "C. Nor this" });
            yield return panel.WaitForPick(0);
            panel.Close();
            Instruction("");
            yield return Say(Line(gate, panel.Picked == 0 ? "TEST_RIGHT" : "TEST_WRONG"));
        }

        // Puts the panel up while `spoken` plays (cut short if the learner answers first); autoAnswer waits for the line.
        IEnumerator Pick(MCQPanel panel, DialogueLine spoken)
        {
            var saying = spoken ? StartCoroutine(Say(spoken)) : null;
            yield return panel.WaitForPick(0, () => spoken && Speaking == spoken);
            StopSaying(saying, spoken);
        }

        // Scene 6: the prediction is stored, never judged here. Each option gets its own reaction.
        IEnumerator Prediction(InteractionGate gate)
        {
            Instruction("Tap your prediction.");
            var panel = new MCQPanel("If we increase the potential difference while everything else stays the same, what will happen to the current?",
                new[] { "A. Increase", "B. Decrease", "C. Stay the same" });
            yield return panel.WaitForPick(0);
            Play("Select");
            LearnerSession.prediction = LearnerSession.Letter(panel.Picked);
            panel.Mark(panel.Picked, UI.AccentFill);
            yield return Wait(0.5f);
            panel.Close();
            Instruction("");
            yield return Say(Line(gate, "S06_R_" + LearnerSession.prediction));
        }

        // Scene 9: select the instrument, then tap the highlighted position (or drag it there). The instruction names each
        // step; AutoPlace and autoAnswer fly it in.
        IEnumerator PlacePart(InteractionGate gate, string part)
        {
            if (rig.IsPlaced(part)) yield break;
            string title = part == "ammeter" ? "ammeter" : "voltmeter";
            bool auto = buildMode == BuildMode.AutoPlace || ReviewMode.autoAnswer;
            rig.draggable = part; // its slot glows in auto runs too, until the part lands (CircuitRig clears it)
            Instruction($"Select the {title}.");
            bool selectedShown = false;
            void OnSelected(string which) { if (which == part && !selectedShown) { selectedShown = true; Play("Select"); } }
            rig.Selected += OnSelected;
            if (auto) { yield return Wait(buildMode != BuildMode.AutoPlace ? ReviewMode.autoDelay : 1f); rig.Place(part); }
            float waited = 0;
            while (!rig.IsPlaced(part))
            {
                if (!AskWattsonUI.IsOpen) waited += Dt;
                if (rig.SelectedPart == part && CurrentInstruction != "Click the highlighted position to place it.") Instruction("Click the highlighted position to place it.");
                else if (rig.SelectedPart != part && CurrentInstruction != $"Select the {title}.") Instruction($"Select the {title}.");
                if (ReviewMode.autoAnswer && waited > ReviewMode.autoDelay + 2f) rig.PlaceNow(part);
                yield return null;
            }
            rig.Selected -= OnSelected;
            rig.draggable = "";
            Play("Place");
            Instruction("");
            React(Grade.Correct); // the part snapped in (gesture only, no line)
        }

        IEnumerator CloseKey()
        {
            if (rig.state.keyClosed) yield break;
            rig.keyArmed = true;
            Instruction("Close the switch to complete the circuit.");
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
            Play("Switch");
            Instruction("");
            React(Grade.Correct);
            // CircuitRig.CloseKey applies the small default voltage (0.5 V) so the needles move; Scene 10 starts from it.
        }
    }
}
