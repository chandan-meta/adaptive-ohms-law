using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OhmsLaw
{
    // The bench experiment, live readings and activities (Scenes 9-13): their cues (Run() hands unknown verbs to
    // ExperimentCue, see SceneDirector.Cues.cs) and the gates where the learner works the source. Every voltage change goes
    // through CircuitRig.SetVoltage; the learner (slider) and Ask Wattson may change it only while a gate has opened
    // rig.controlsOpen, and Hold() closes it again whatever the gate did.
    public partial class SceneDirector
    {
        // A reading counts once the source has held it this long with no finger or button down (a drag passes through values).
        const float SettleSeconds = 0.8f, IdleHintSeconds = 15f;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        LiveReadingsPanel readings;
        bool readingsResistance;

        // "readings 1|0": live V / I panel beside the graph (a HUD card where the scene has no graph).
        // "readings.r 1|0": add R (from Scene 12, once ohms are introduced). "graph.live 1|0": the live point on the graph.
        // "rig.drag ammeter|voltmeter|none": that part's slot glows and it may be dragged (Scene 9, before its gate); a
        // part already in its slot clears it, so a replay after placement is a no-op. "rig.path 1|0": the lit current path.
        bool ExperimentCue(string verb, string[] words, string rest, float value, bool on)
        {
            switch (verb)
            {
                case "readings": Readings(on); return true;
                case "readings.r": readingsResistance = on; if (on) LearnerSession.resistanceTaught = true; if (readings) readings.showResistance = on; return true;
                case "graph.live": if (graph) graph.Live(on); return true;
                case "rig.drag": if (rig) rig.draggable = rest == "" || rest == "none" || rig.IsPlaced(rest) ? "" : rest; return true;
                case "rig.path": if (rig) rig.showPath = on; return true;
                case "rig.readouts": if (rig) rig.readoutsShown = on; return true; // the digital plates above the meters
                default: return false;
            }
        }

        void Readings(bool on)
        {
            if (on && !readings && Flow)
            {
                readings = LiveReadingsPanel.Create(graph, Flow.transform.Find("SafeArea"));
                readings.showResistance = readingsResistance;
            }
            else if (!on && readings)
            {
                Destroy(readings.gameObject);
                readings = null;
            }
        }

        static bool Pressing => Pointer.current != null && Pointer.current.press.isPressed;

        static string Volts(float v) => v.ToString("0.#", Inv) + " V";

        // Scene 10: the learner sets the source with the slider (or asks Wattson). <gate>_GO (optional) says what to do; a
        // wrong setting held for a moment updates the prompt with the reading and, once, plays <gate>_H. autoAnswer sets it.
        // While ControlsMax is lowered (S10_Set1V) the knob stops there; a learner who pushed past it is told so on the prompt.
        IEnumerator SetVoltage(InteractionGate gate, float volts)
        {
            capPushed = false;
            if (!slider) Slider(true);
            string instruction = promptText;
            yield return Say(Line(gate, gate.gateId + "_GO"));
            rig.controlsOpen = true;
            float waited = 0, held = 0, last = rig.state.voltage, judged = last;
            bool hinted = false;
            while (!Mathf.Approximately(rig.state.voltage, volts))
            {
                if (!AskWattsonUI.IsOpen) waited += Dt; // autoAnswer never acts behind the Ask panel or under PAUSE
                float v = rig.state.voltage;
                if (!Mathf.Approximately(v, last)) { last = v; held = 0; } else held += Dt;
                if (held >= SettleSeconds && !Pressing && !Mathf.Approximately(v, judged))
                {
                    judged = v;
                    Prompt($"{Volts(v)} now. Set the battery to {Volts(volts)}.");
                    if (!hinted) { hinted = true; yield return Say(Line(gate, gate.gateId + "_H")); }
                }
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay) rig.SetVoltage(volts);
                yield return null;
            }
            rig.controlsOpen = false;
            if (capPushed) Prompt($"The knob stops at {Volts(ControlsMax)} for this reading."); // until the builder's next prompt cue
            else if (promptText != instruction) Prompt(instruction);
            yield return Wait(1.2f); // let the needles settle on the reading
        }

        // Scene 10: tap RECORD (or the ammeter) to write row `row` from the meters as they read now. The row belongs to one
        // setting (ExperimentData's voltage): if the source is elsewhere, the controls open and the prompt says which
        // setting to go back to; nothing is written until it matches.
        IEnumerator Record(InteractionGate gate, int row)
        {
            float want = ExperimentData.Main.voltages[row];
            string instruction = promptText;
            yield return Say(Line(gate, gate.gateId + "_GO"));
            bool tapped = false, done = false;
            var button = ScreenButton("Record", "RECORD", new Vector2(720, 250), () => tapped = true);
            StartCoroutine(TapHint(rig.ammeter, () => done, new Vector2(0, -130))); // ring under the meter, finger up to it: the reading stays clear
            float waited = 0;
            while (!done)
            {
                // PAUSE: the button greys out and taps (button, ammeter) are ignored until PLAY.
                if (ModuleFlow.UserPaused) { button.interactable = false; tapped = false; yield return null; continue; }
                button.interactable = true;
                if (!AskWattsonUI.IsOpen) waited += Dt; // autoAnswer never acts behind the Ask panel
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay) { rig.SetVoltage(want); tapped = true; }
                var pointer = Pointer.current;
                if (pointer != null && pointer.press.wasPressedThisFrame && Camera.main &&
                    Physics.Raycast(Camera.main.ScreenPointToRay(pointer.position.ReadValue()), out var hit, 50f) && hit.transform.IsChildOf(rig.ammeter))
                    tapped = true;
                if (tapped)
                {
                    tapped = false;
                    if (Mathf.Approximately(rig.state.voltage, want)) done = true;
                    else
                    {
                        rig.controlsOpen = true;
                        Prompt($"Row {row + 1} is the {Volts(want)} reading. Set the battery to {Volts(want)}, then record.");
                    }
                }
                yield return null;
            }
            rig.controlsOpen = false;
            if (promptText != instruction) Prompt(instruction);
            Destroy(button.gameObject);
            if (sfx && pop) sfx.PlayOneShot(pop);
            table.Record(row, rig.Volts, rig.Amps);
            LearnerSession.tableRows = Mathf.Max(LearnerSession.tableRows, row + 1);
            yield return Wait(0.8f);
        }

        // S11_Target / S13_Apply: make the ammeter read `amps` with the slider or by asking Wattson. The live readings show
        // the target and the live point moves on the graph. A learner who is confident with the concept afterwards gets
        // `bonusAmps` as well (S11: 0.80 A, lines <gate>_GO2 / _X2 / _OK2).
        IEnumerator Target(InteractionGate gate, string concept, float amps, float bonusAmps = -1f)
        {
            bool ownSlider = !slider;
            if (ownSlider) Slider(true);
            Readings(true);
            if (graph) graph.Live(true);
            yield return TargetRound(gate, new TargetRun(gate.gateId, concept, amps), "");
            if (bonusAmps > 0f && LearnerSession.Confident(concept))
                yield return TargetRound(gate, new TargetRun(gate.gateId, concept, bonusAmps), "2");
            rig.targetAmps = -1f;
            if (ownSlider) Slider(false);
        }

        // One target. Each setting the learner holds is judged (TargetRun): a hit plays <gate>_OK; a miss shows under the
        // target which way to go, and the first miss plays the spoken hint <gate>_H (not for a learner who came in
        // confident); the second miss: Wattson sets the source himself and explains (<gate>_X) while the matching graph
        // point lights. A learner who has not tried anything after IdleHintSeconds hears the hint once (not recorded).
        IEnumerator TargetRound(InteractionGate gate, TargetRun run, string n)
        {
            string id = gate.gateId;
            bool confident = LearnerSession.Confident(run.concept);
            rig.targetAmps = run.targetAmps;
            if (readings) readings.targetNote = "";
            yield return Say(Line(gate, id + "_GO" + n));
            rig.controlsOpen = true;
            float waited = 0, held = 0, last = rig.state.voltage, judged = last;
            bool hinted = false;
            while (true)
            {
                if (!AskWattsonUI.IsOpen) waited += Dt; // talking to Wattson is not being idle; PAUSE holds the clock
                float v = rig.state.voltage;
                if (!Mathf.Approximately(v, last)) { last = v; held = 0; } else held += Dt;
                if (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay && Mathf.Approximately(v, judged)) rig.SetVoltage(run.TargetVolts);

                if (held >= SettleSeconds && !Pressing && !Mathf.Approximately(v, judged))
                {
                    judged = v;
                    var outcome = run.Judge(rig.Volts, rig.Amps);
                    if (outcome == TargetRun.Outcome.Hit) { React(LearnerSession.Grade.Correct); break; }
                    if (readings) readings.targetNote = rig.Amps < run.targetAmps ? "▲ more volts" : "▼ fewer volts";
                    if (outcome == TargetRun.Outcome.Show) { yield return ShowTarget(gate, run, n); break; }
                    if (!confident && !hinted) { hinted = true; yield return Say(Line(gate, id + "_H")); }
                }
                else if (!hinted && run.Misses == 0 && waited >= IdleHintSeconds)
                {
                    hinted = true;
                    yield return Say(Line(gate, id + "_H"));
                }
                yield return null;
            }
            rig.controlsOpen = false;
            if (readings) readings.targetNote = "";
            if (run.IsHit(rig.Amps) && run.Misses < TargetRun.MaxMisses) yield return Say(Line(gate, id + "_OK" + n));
        }

        // After the last miss: Wattson turns the source to the target himself while he explains it.
        IEnumerator ShowTarget(InteractionGate gate, TargetRun run, string n)
        {
            rig.controlsOpen = false;
            yield return SayWhile(Line(gate, gate.gateId + "_X" + n), Demonstrate(true, run.TargetVolts));
        }

        // The visible experiment behind a reveal: Wattson steps the source through `steps`, holding each long enough to read,
        // with the readings up and the matching graph point lit; then he puts the source back, unless `keep`.
        IEnumerator Demonstrate(bool keep, params float[] steps)
        {
            if (!rig || !rig.isActiveAndEnabled) yield break;
            bool hadReadings = readings;
            Readings(true);
            float before = rig.state.voltage;
            var data = ExperimentData.Main;
            foreach (float v in steps)
            {
                rig.SetVoltage(v);
                int row = System.Array.FindIndex(data.voltages, x => Mathf.Approximately(x, v));
                if (graph && row >= 0) graph.Highlight(row, true);
                yield return Wait(1.8f);
                if (graph && row >= 0) graph.Highlight(row, false);
            }
            if (!keep) rig.SetVoltage(before);
            if (!hadReadings) Readings(false);
        }
    }
}
