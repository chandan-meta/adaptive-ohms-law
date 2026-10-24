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
            if (string.IsNullOrEmpty(id)) return null;
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
                // The module's two detective challenges (graded, a hint and a retry on a miss): the first right after voltage
                // and current are met, the second once resistance has been named.
                case "S06_Prediction": yield return DetectiveTest(gate); break;
                case "S12_MoreResistance": yield return ResistanceChallenge(gate); break;
                case "S09_PlaceAmmeter": yield return PlacePart(gate, "ammeter"); break;
                case "S09_PlaceVoltmeter": yield return PlacePart(gate, "voltmeter"); break;
                // Scene 10: the learner sets 1 V, 2 V ... 5 V in turn; each setting records its row and plots its point.
                case "S10_Set1V": yield return SetVoltage(gate, 1); break;
                case "S10_Set2V": yield return SetVoltage(gate, 2); break;
                case "S10_Set3V": yield return SetVoltage(gate, 3); break;
                case "S10_Set4V": yield return SetVoltage(gate, 4); break;
                case "S10_Set5V": yield return SetVoltage(gate, 5); break;
                // v3: the four chapters that used to be watched only now each ask for one meaningful action.
                case "S05_ApplyVoltage": yield return ApplyVoltage(); break;
                case "S11_DrawLine": yield return DrawLine(); break;
                case "S12_SwapWire": yield return SwapWire(); break;
                case "S13_BuildLaw": yield return BuildLaw(); break;
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
        IEnumerator Pick(MCQPanel panel, DialogueLine spoken, int autoPick = 0)
        {
            var saying = spoken ? StartCoroutine(Say(spoken)) : null;
            yield return panel.WaitForPick(autoPick, () => spoken && Speaking == spoken);
            StopSaying(saying, spoken);
        }

        // A detective challenge: the question is spoken while the options are up (cut short by a pick). Right: the row
        // turns green with a check and a chime, Wattson pumps a fist, and he says the one short `right` line. Wrong: the
        // row is marked and stays out, a hint aimed at that wrong idea shows under the options and `visualHint` points the
        // scene at the clue; the learner picks again at once, with no extra narration. A second miss shows the answer and
        // goes on with the same line, so nobody is stuck. Every pick is recorded ("<gate> <letter> Correct|Incorrect");
        // the concept and `firstPick` get the first one only. autoAnswer picks the right option (a wrong one first under
        // ReviewMode.missFirst, so a hands-free run shows the hint and the retry).
        // A challenge's hint went up after a miss (the play test shoots that frame): the gate id.
        public static event System.Action<string> ChallengeMissed;

        IEnumerator Challenge(InteractionGate gate, string question, string[] options, int correct, string concept, string ask, string right,
            System.Func<int, string> hintFor, System.Action<int> visualHint = null, System.Action<string> firstPick = null)
        {
            Turn("Detective challenge.", null, options.Length > 2 ? "Tap an answer or press 1, 2 or 3." : "Tap an answer or press 1 or 2.");
            var panel = new MCQPanel(question, options, -1, "Detective challenge");
            yield return Pick(panel, Line(gate, ask), ReviewMode.missFirst ? (correct + 1) % options.Length : correct);
            for (int misses = 0; ; misses++)
            {
                if (panel.Picked < 0) yield break; // the panel was swept away (the scene was skipped)
                int picked = panel.Picked;
                string letter = LearnerSession.Letter(picked);
                var grade = picked == correct ? Grade.Correct : Grade.Incorrect;
                LearnerSession.answers.Add($"{gate.gateId} {letter} {grade}");
                if (misses == 0) { LearnerSession.Record(concept, grade); firstPick?.Invoke(letter); }
                if (grade == Grade.Correct)
                {
                    panel.Select(picked);
                    panel.Mark(picked, MCQPanel.RightMark);
                    React(grade);
                    if (Panel) Panel.Done("Correct.");
                    break;
                }
                panel.Mark(picked, MCQPanel.WrongMark);
                panel.Disable(picked, MCQPanel.WrongMark);
                React(grade);
                panel.Hint(hintFor(picked));
                visualHint?.Invoke(picked);
                ChallengeMissed?.Invoke(gate.gateId);
                if (misses >= 1)
                {
                    panel.Reveal(correct);
                    if (Panel) Panel.Done("Here's the answer.");
                    yield return Wait(1.6f);
                    break;
                }
                if (Panel) Panel.Turn("Try again.", null, "One option is out. Pick again.");
                panel.Reopen();
                yield return panel.WaitForPick(correct);
            }
            yield return Wait(1.0f);
            panel.Close();
            yield return Say(Line(gate, right));
        }

        // Scene 6, right after voltage and current: a stronger push with the wire unchanged. The first pick is the
        // learner's prediction (Scene 10 calls it back); a miss pulses the + terminal and its rising gauge: the push.
        IEnumerator DetectiveTest(InteractionGate gate) => Challenge(gate,
            "If voltage increases while resistance stays constant, what happens to current?",
            new[] { "A. It increases", "B. It decreases", "C. It stays the same" }, 0, Concepts.Relationship, null, "S06_R_A",
            picked => picked == 1
                ? "Same wire, same temperature, so its resistance stays put. Voltage is the push: would a bigger push give less flow?"
                : "Same wire, same temperature, so its resistance stays put. The push got stronger: watch the + terminal. Can the flow stay the same?",
            _ => { Run("pulse Card Terminal +"); Run("pulse Battery Readout"); Play("Highlight"); },
            letter => LearnerSession.prediction = letter);

        // Scene 12, once resistance is named: the same push through more opposition. A miss lights the note on resistance
        // and the test wire again.
        IEnumerator ResistanceChallenge(InteractionGate gate) => Challenge(gate,
            "Same voltage. If the resistance increases, what happens to the current?",
            new[] { "A. It increases", "B. It decreases", "C. It stays the same" }, 1, Concepts.Resistance, "S12_MR_Q", "S12_MR_R",
            picked => picked == 0
                ? "Resistance opposes the flow. Same push, more opposition: more current or less?"
                : "Remember the two wires: same push, but nichrome let less current through. Resistance changes the current.",
            _ => { Callouts.Show("resistance"); Run("pulse Wire Highlight"); Play("Highlight"); });

        // The current turn in the panel: what to do, the step ("1 OF 3"), a hint. SceneDirector.Instruction keeps the
        // plain one-line form for cues.
        void Turn(string text, string step, string hint) { if (Panel) Panel.Turn(text, step, hint); }

        // Scene 9: one tap on the instrument and it flies into its place in the circuit (a tap on its glowing slot, Enter,
        // AutoPlace and autoAnswer fly it in too). The turn names the meter and where it goes, the hand points at it, and
        // once it lands the turn says what it measures. When the voltmeter lands with the ammeter already in, the circuit
        // completes itself: the key closes with its clack and the needles move (no separate switch step).
        IEnumerator PlacePart(InteractionGate gate, string part)
        {
            if (rig.IsPlaced(part)) yield break;
            bool ammeter = part == "ammeter";
            string step = ammeter ? "Step 1 of 2" : "Step 2 of 2";
            string tap = ammeter ? "Tap the ammeter." : "Tap the voltmeter.";
            string hint = ammeter ? "The meter marked A: it joins the loop in series, in line with the wire." : "The meter marked V: it joins in parallel, across X and Y.";
            bool auto = buildMode == BuildMode.AutoPlace || ReviewMode.autoAnswer;
            rig.draggable = part; // its slot glows in auto runs too, until the part lands (CircuitRig clears it)
            Turn(tap, step, hint);
            // The two meters look alike but for the letter on the dial: a tap on the other one says which it was.
            void OnWrong(string which) { if (Panel) Panel.ShowHint(which == "voltmeter" ? "That's the voltmeter: find the A." : "That's the ammeter: find the V."); }
            rig.WrongPart += OnWrong;
            // The part's origin is its base: from the upper left the fingertip lands on the meter's face, not the bench edge.
            StartCoroutine(TapHint(ammeter ? rig.ammeter : rig.voltmeter, () => rig.IsPlaced(part) || rig.InFlight(part), new Vector2(-0.6f, 0.8f)));
            if (auto) { yield return Wait(buildMode != BuildMode.AutoPlace ? ReviewMode.autoDelay : 1f); rig.Place(part); }
            float waited = 0;
            while (!rig.IsPlaced(part))
            {
                if (!AskWattsonUI.IsOpen) waited += Dt;
                if (HudKeys.ConsumeAct()) rig.Place(part);
                if (ReviewMode.autoAnswer && waited > ReviewMode.autoDelay + 2f && !rig.InFlight(part)) rig.PlaceNow(part);
                yield return null;
            }
            rig.WrongPart -= OnWrong;
            rig.draggable = "";
            Play("Place");
            if (Panel) Panel.Done(ammeter ? "Ammeter in series: it measures the current I." : "Voltmeter across XY: it measures the voltage V.");
            React(Grade.Correct); // the part snapped in (gesture only, no line)
            if (ammeter || !rig.ammeterPlaced || rig.state.keyClosed) yield break;
            yield return Wait(0.5f);
            rig.CloseKey();
            Play("Switch");
            if (Panel) Panel.Done("Circuit complete.");
            // CircuitRig.CloseKey applies the small default voltage (0.5 V) so the needles move; Scene 10 starts from it.
        }

        // ------------------------------------------------------------------ v3 actions (Scenes 5, 11, 12, 13)

        // A click or tap this frame on (or right next to) a world object: a collider under it, else within ~110 px of its
        // renderers' centre on screen (no collider needed). Never through the HUD.
        bool Tapped(GameObject target)
        {
            if (!target || !HudKeys.Tapped || !Camera.main) return false;
            if (UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return false;
            Vector2 pointer = UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero;
            var ray = Camera.main.ScreenPointToRay(pointer);
            foreach (var hit in Physics.RaycastAll(ray, 500f)) if (hit.transform.IsChildOf(target.transform)) return true;
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var onScreen = Camera.main.WorldToScreenPoint(bounds.center);
            var canvas = Flow ? Flow.GetComponent<Canvas>() : null;
            return onScreen.z > 0 && Vector2.Distance(onScreen, pointer) < 110f * (canvas ? canvas.scaleFactor : 1f);
        }

        // Scene 5: the learner applies the voltage: a tap on the + terminal, and the field goes up (the drift follows).
        IEnumerator ApplyVoltage()
        {
            if (wire && wire.fieldOn) yield break;
            var plus = Ref("Card Terminal +");
            Turn("Apply the voltage.", null, "Tap the + terminal to connect the push.");
            bool done = false;
            if (plus) StartCoroutine(TapHint(plus.transform, () => done, Vector2.right)); // from the right: its "+ positive" label is below
            float waited = 0;
            while (!done)
            {
                if (!Held) waited += Dt;
                if (Tapped(plus) || HudKeys.ConsumeAct() || (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay)) done = true;
                yield return null;
            }
            Play("Switch");
            if (Panel) Panel.Done("Voltage applied.");
            React(Grade.Correct);
        }

        // Scene 11: the learner draws the line: a tap on the graph card (the line cue right after the gate sweeps it in).
        IEnumerator DrawLine()
        {
            Turn("Draw the line.", null, "Tap the graph to run a line through your five points.");
            bool done = false;
            var block = Panel ? Panel.Block("Graph") : null;
            UnityEngine.UI.Button hit = null;
            if (block && block.gameObject.activeInHierarchy)
            {
                if (!block.TryGetComponent(out hit)) hit = block.gameObject.AddComponent<UnityEngine.UI.Button>();
                hit.transition = UnityEngine.UI.Selectable.Transition.None;
                hit.onClick.AddListener(() => done = true);
                Panel.Attention("Graph", true);
            }
            float waited = 0;
            while (!done)
            {
                if (!Held) waited += Dt;
                if (HudKeys.ConsumeAct() || (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay)) done = true;
                yield return null;
            }
            if (hit) Destroy(hit);
            if (Panel) { Panel.Attention("Graph", false); Panel.Done("Line drawn."); }
        }

        // Scene 12: the learner swaps the wire. With the 3D sample up (WireSample, opened by the "show Wire Sample" cue)
        // copper shows alone and the swap pops nichrome in under it; the dock inset still works where a scene cues it.
        IEnumerator SwapWire()
        {
            var sample = WireSample.Active;
            if (!sample) { if (inset) yield return SwapInset(); yield break; }
            sample.ShowNichrome(false);
            Turn("Change the wire material.", null, "Same voltage, same length: tap Swap in nichrome.");
            bool done = false;
            var home = Panel ? Panel.Fresh("MCQ", null, 12f) : null;
            if (home) LessonPanel.Pill("Swap", home, "Swap in nichrome", () => done = true, LessonPanel.Kind.Primary, null, 56f, 20f)
                .GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            float waited = 0;
            while (!done)
            {
                if (!Held) waited += Dt;
                if (HudKeys.ConsumeAct() || (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay)) done = true;
                yield return null;
            }
            if (Panel) Panel.Clear("MCQ");
            sample.ShowNichrome(true);
            Play("Reveal");
            if (Panel) Panel.Done("Nichrome in: less current.");
        }

        IEnumerator SwapInset()
        {
            inset.ShowNichrome(false);
            Turn("Change the wire material.", null, "Same voltage, same length: tap Swap in nichrome.");
            bool done = false;
            var swap = inset.OfferSwap(() => done = true);
            float waited = 0;
            while (!done)
            {
                if (!Held) waited += Dt;
                if (HudKeys.ConsumeAct() || (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay)) done = true;
                yield return null;
            }
            if (swap) Destroy(swap.gameObject); // however the swap came (tap, Enter, autoAnswer), its button goes
            inset.ShowNichrome(true);
            Play("Reveal");
            if (Panel) Panel.Done("Nichrome in: less current.");
        }

        // Scene 13: the learner builds the law: V, then I, then R.
        IEnumerator BuildLaw()
        {
            Turn("Build the law.", null, "Tap V, then I, then R.");
            var tiles = LawTiles.Create();
            float waited = 0;
            while (!tiles.Complete)
            {
                if (!Held) waited += Dt;
                if (HudKeys.ConsumeAct() || (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay)) { tiles.Advance(); waited = ReviewMode.autoDelay - 0.6f; }
                yield return null;
            }
            Play("Reveal");
            if (Panel) Panel.Done("That's the law.");
            yield return Wait(0.9f);
            if (Panel) Panel.Clear("MCQ");
        }
    }
}
