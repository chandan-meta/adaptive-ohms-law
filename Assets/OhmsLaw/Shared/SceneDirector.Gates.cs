using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Every learner interaction in the module, by gate id. Each one works with mouse and touch, never traps the
    // learner, and completes by itself when ReviewMode.autoAnswer is on.
    public partial class SceneDirector
    {
        DialogueLine Line(InteractionGate gate, string id) => gate.lines.FirstOrDefault(l => l && l.id == id);

        IEnumerator RunGate(InteractionGate gate)
        {
            switch (gate.gateId)
            {
                case "Test_MCQ": yield return TestMcq(gate); break;
                case "S06_Prediction": yield return Prediction(gate); break;
                case "S09_PlaceAmmeter": yield return PlacePart(gate, "ammeter"); break;
                case "S09_PlaceVoltmeter": yield return PlacePart(gate, "voltmeter"); break;
                case "S09_CloseKey": yield return CloseKey(); break;
                case "S10_Set1V": yield return SetVoltage(1); break;
                case "S10_Record": yield return Record(); break;
                case "S10_Pattern": yield return Pattern(gate); break;
                case "S10_Discover": yield return Discover(); break;
                case "S12_Slope": yield return Slope(gate); break;
                case "S13_Checkpoint": if (checkpointEnabled) yield return Checkpoint(gate); break;
                default: Debug.LogWarning($"{sceneId}: no handler for gate '{gate.gateId}'", this); break;
            }
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
            if (auto) { yield return new WaitForSeconds(auto && buildMode != BuildMode.AutoPlace ? ReviewMode.autoDelay : 1f); rig.Place(part); }
            float waited = 0;
            while (!rig.IsPlaced(part))
            {
                waited += Time.deltaTime;
                if (ReviewMode.autoAnswer && waited > ReviewMode.autoDelay + 2f) rig.PlaceNow(part);
                yield return null;
            }
            rig.draggable = "";
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
                waited += Time.deltaTime;
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay) rig.CloseKey();
                yield return null;
            }
            // CircuitRig.CloseKey applies the small default voltage (0.5 V) so the needles move; Scene 10 starts from it.
        }

        // Scene 10: the learner sets the source with the slider. autoAnswer sets it after the review delay.
        IEnumerator SetVoltage(float volts)
        {
            if (!slider) Slider(true);
            float waited = 0;
            while (!Mathf.Approximately(rig.state.voltage, volts))
            {
                waited += Time.deltaTime;
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay) rig.SetVoltage(volts);
                yield return null;
            }
            yield return new WaitForSeconds(1.2f); // let the needles settle on the reading
        }

        // Scene 10: tap Record (or the ammeter) to write row 1.
        IEnumerator Record()
        {
            bool recorded = false;
            var button = ScreenButton("Record", "RECORD", new Vector2(720, 250), () => recorded = true);
            StartCoroutine(TapHint(rig.ammeter, () => recorded, new Vector2(0, -130))); // ring under the meter, finger up to it: the reading stays clear
            float waited = 0;
            while (!recorded)
            {
                waited += Time.deltaTime;
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay) recorded = true;
                var pointer = UnityEngine.InputSystem.Pointer.current;
                if (pointer != null && pointer.press.wasPressedThisFrame && Camera.main &&
                    Physics.Raycast(Camera.main.ScreenPointToRay(pointer.position.ReadValue()), out var hit, 50f) && hit.transform.IsChildOf(rig.ammeter))
                    recorded = true;
                yield return null;
            }
            Destroy(button.gameObject);
            if (sfx && pop) sfx.PlayOneShot(pop);
            table.ShowRows(1);
            LearnerSession.tableRows = 1;
            yield return new WaitForSeconds(0.8f);
        }

        // Scene 10 pattern question. Wrong: a proposed nudge, then the same question again; second wrong: the V column
        // then the I column light up row by row and it is asked once more; the third attempt accepts any answer.
        IEnumerator Pattern(InteractionGate gate)
        {
            string[] options =
            {
                "A. Current increases as potential difference increases.",
                "B. Current decreases.",
                "C. There is no pattern.",
            };
            LearnerSession.s10PatternAttempts = 0;
            for (int attempt = 1; ; attempt++)
            {
                var panel = new MCQPanel("What do you notice?", options, dockRight: true);
                yield return panel.WaitForPick(0);
                panel.Close();
                LearnerSession.s10PatternAttempts = attempt;
                LearnerSession.s10PatternAnswer = LearnerSession.Letter(panel.Picked);
                if (panel.Picked == 0 || attempt >= 3) yield break;
                yield return Say(Line(gate, "S10_R_WRONG"));
                if (attempt == 2)
                {
                    yield return table.Sweep(0);
                    yield return table.Sweep(1);
                }
            }
        }

        // Scene 10: tap either highlighted row (or the ×2 chip) to reveal the doubling. Reveals itself after 5 s.
        IEnumerator Discover()
        {
            bool revealed = false;
            void OnRow(int row) { if (row <= 1) revealed = true; }
            table.RowTapped += OnRow;
            var chip = ScreenButton("Times Two", "× 2", new Vector2(720, 250), () => revealed = true);
            for (float t = 0; t < 5f && !revealed; t += Time.deltaTime) yield return null;
            table.RowTapped -= OnRow;
            Destroy(chip.gameObject);
            SetActive("Card Discovery", true);
            if (sfx && pop) sfx.PlayOneShot(pop);
            yield return new WaitForSeconds(1.5f);
        }

        // Scene 12: slope as multiple choice (proposed). Wrong: shake, show the division, ask once more, then continue.
        IEnumerator Slope(InteractionGate gate)
        {
            string[] options = { "5", "0.2", "2.5" };
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                var panel = new MCQPanel("Slope = ΔV / ΔI = ?", options, dockRight: true, width: 760f);
                yield return panel.WaitForPick(0);
                LearnerSession.slopeAnswer = options[panel.Picked];
                if (panel.Picked == 0 || attempt == 2)
                {
                    panel.Mark(0, new Color(0.2f, 0.5f, 0.25f));
                    panel.Hint("Slope = ΔV / ΔI = 2.0 ÷ 0.40 = 5");
                    yield return new WaitForSeconds(2f);
                    panel.Close();
                    yield break;
                }
                panel.Mark(panel.Picked, new Color(0.5f, 0.2f, 0.2f));
                panel.Hint("2.0 ÷ 0.40");
                yield return panel.Shake();
                yield return new WaitForSeconds(1.8f);
                panel.Close();
            }
        }

        // Scene 13 adaptive checkpoint: Q1; right -> harder Q2; wrong -> explanation and the easier question.
        IEnumerator Checkpoint(InteractionGate gate)
        {
            LearnerSession.checkpoint.Clear();
            yield return Say(Line(gate, "AQ1"));
            var q1 = new MCQPanel(Line(gate, "AQ1").text, new[] { "A. It doubles", "B. It halves", "C. It stays the same" }, dockRight: true);
            yield return q1.WaitForPick(0);
            q1.Close();
            bool right = q1.Picked == 0;
            LearnerSession.checkpoint.Add("AQ1 " + LearnerSession.Letter(q1.Picked) + (right ? " correct" : " wrong"));
            if (right)
            {
                yield return Say(Line(gate, "AQ_GOOD"));
                yield return Say(Line(gate, "AQ2"));
                var q2 = new MCQPanel(Line(gate, "AQ2").text, new[] { "A. The one that needs the larger potential difference", "B. The one that needs the smaller potential difference", "C. Both have the same resistance" }, dockRight: true);
                yield return q2.WaitForPick(0);
                q2.Close();
                LearnerSession.checkpoint.Add("AQ2 " + LearnerSession.Letter(q2.Picked) + (q2.Picked == 0 ? " correct" : " wrong"));
                yield return Say(Line(gate, q2.Picked == 0 ? "AQ_GOOD" : "AQ_EXPLAIN"));
            }
            else
            {
                yield return Say(Line(gate, "AQ_EXPLAIN"));
                yield return Say(Line(gate, "AQ_EASY"));
                var easy = new MCQPanel(Line(gate, "AQ_EASY").text, new[] { "A. It went up", "B. It went down", "C. Nothing changed" }, dockRight: true);
                yield return easy.WaitForPick(0);
                easy.Close();
                LearnerSession.checkpoint.Add("AQ_EASY " + LearnerSession.Letter(easy.Picked) + (easy.Picked == 0 ? " correct" : " wrong"));
                yield return Say(Line(gate, easy.Picked == 0 ? "AQ_GOOD" : "AQ_EXPLAIN"));
            }
        }
    }
}
