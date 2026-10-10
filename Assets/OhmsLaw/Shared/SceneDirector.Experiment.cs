using System.Collections;
using System.Globalization;
using UnityEngine;

namespace OhmsLaw
{
    // The bench experiment and the panel's data views (Scenes 9-13): their cues (Run() hands unknown verbs to
    // ExperimentCue, see SceneDirector.Cues.cs) and the gate where the learner sets the source. Every voltage change goes
    // through CircuitRig.SetVoltage; the learner (voltage buttons) and Ask Wattson may change it only while a gate has
    // opened rig.controlsOpen, and Hold() closes it again whatever the gate did.
    public partial class SceneDirector
    {
        // A reading counts once the source has held it this long with no finger down (a drag passes through values).
        const float SettleSeconds = 0.9f;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        LiveReadingsPanel readings;
        bool readingsResistance;
        WireCounterView counterView;
        ResistanceInset inset;
        CircuitSchematic schematic;

        // Panel and sound cues:
        //   readings 1|0        live V / I in the panel          readings.r 1|0   add R (once the ohm is introduced)
        //   graph.live 1|0      the live point on the graph       rig.drag <part>  that part may be selected (its slot glows)
        //   rig.path 1|0        the lit current path on the bench rig.readouts 1|0 the meters' digital plates
        //   counter 1|0         the charge counter view           schematic 1|0    the live circuit schematic
        //   inset 1|0           the resistance comparison         expand 1|0       widen the panel (data views do it themselves)
        //   formula <text>      append a formula line             formula.clear    empty the formula block
        //   reveal <big>|<small>  a hero reveal in the formula block
        //   sfx <name>          one-shot from the bank            ambience <name>|off · music <name>|off  looping beds (crossfade)
        //   end                 the finale: replay / exit in the panel
        //   panel <Block> 1|0   show or hide one panel block (Table, Graph ...) so each beat holds only what it is about
        // v5 (Docs/Polish/V5_PLAN.md):
        //   hero <big>|<small>  a centre-screen reveal plate      hero.clear       takes it away
        //   readings.top 1|0    the live readings on the top strip instead of the dock card (readings.r still adds R)
        //   stage 1|0           the table, graph and formula cards in front of the 3D view, and back to the dock
        bool ExperimentCue(string verb, string[] words, string rest, float value, bool on)
        {
            switch (verb)
            {
                case "panel": if (Panel && words.Length > 1) Panel.Show(words[1], words.Length < 3 || words[2] != "0"); return true;
                case "readings": Readings(on); return true;
                case "readings.r": readingsResistance = on; if (on) LearnerSession.resistanceTaught = true; if (readings) readings.showResistance = on; return true;
                case "graph.live": if (Panel) Graph.Live(on); return true;
                case "rig.drag": if (rig) rig.draggable = rest == "" || rest == "none" || rig.IsPlaced(rest) ? "" : rest; return true;
                case "rig.path": if (rig) rig.showPath = on; return true;
                case "rig.readouts": if (rig) rig.readoutsShown = on; return true;
                case "counter": Counter(on); return true;
                case "schematic": Schematic(on); return true;
                case "inset": Inset(on); return true;
                case "expand": if (Panel) Panel.Expand(on); return true;
                case "formula": if (Panel) Panel.Formula(rest); Play("Reveal"); return true;
                case "formula.clear": if (Panel) Panel.FormulaClear(); return true;
                case "reveal":
                {
                    if (!Panel) return true;
                    int bar = rest.IndexOf('|');
                    Panel.Reveal(bar < 0 ? rest : rest.Substring(0, bar).Trim(), bar < 0 ? "" : rest.Substring(bar + 1).Trim());
                    Play("Reveal");
                    return true;
                }
                case "sfx": Play(rest); return true;
                case "ambience": Bed(ambience, rest, 1.5f); return true;
                case "music": Bed(music, rest, 2.5f); return true;
                case "end": if (Flow) Flow.EndOfModule(); return true;
                case "hero":
                {
                    if (!Panel) return true;
                    int bar = rest.IndexOf('|');
                    Panel.Hero(bar < 0 ? rest : rest.Substring(0, bar).Trim(), bar < 0 ? "" : rest.Substring(bar + 1).Trim());
                    Play("Reveal");
                    return true;
                }
                case "hero.clear": if (Panel) Panel.HeroClear(); return true;
                case "readings.top":
                {
                    if (!Panel) return true;
                    bool had = readings;
                    Readings(false);                 // the card rebuilds at the strip's size (or the dock's)
                    Panel.TopReadings(on);
                    Readings(on || had);
                    return true;
                }
                case "stage": if (Panel) Panel.Stage(on); return true;
                default: return false;
            }
        }

        void Readings(bool on)
        {
            if (on && !readings && Panel)
            {
                readings = LiveReadingsPanel.Create();
                readings.showResistance = readingsResistance;
            }
            else if (!on && readings)
            {
                Panel.Clear("Readings");
                readings = null;
            }
        }

        void Counter(bool on)
        {
            if (on && !counterView && Panel && wire) counterView = WireCounterView.Create(wire);
            else if (!on && counterView) { Panel.Clear("Counter"); counterView = null; }
        }

        void Schematic(bool on)
        {
            if (on && !schematic && Panel) schematic = CircuitSchematic.Create();
            else if (!on && schematic) { Panel.Clear("Schematic"); schematic = null; }
        }

        // The comparison is two magnified strips, not the bench: the live readings step aside while it shows, so the
        // nichrome bench's numbers never sit beside the copper strip.
        void Inset(bool on)
        {
            if (on && !inset && Panel) { inset = ResistanceInset.Create(); if (readings) Panel.Show("Readings", false); }
            else if (!on && inset) { Panel.Clear("Inset"); inset = null; Panel.Expand(Panel.HasViews); if (readings) Panel.Show("Readings", true); }
        }

        static string Volts(float v) => v.ToString("0.#", Inv) + " V";

        // Scene 10: "Set the voltage to N V." The learner taps the button (or asks Wattson); once the setting has held for a
        // moment the row records itself from the live meters (with a confirmation sound) and the graph plots its point.
        // The buttons above N stay dim (ControlsMax), so the rows record in order and no later reading leaks early.
        IEnumerator SetVoltage(InteractionGate gate, float volts)
        {
            int row = Mathf.RoundToInt(volts) - 1;
            capPushed = false;
            ControlsMax = volts;
            Volts(true);
            // The turn and the open controls come with Wattson's "Now N volts": the learner may act while he speaks.
            Turn($"Set the battery to {Volts(volts)}.", $"{volts:0} of 5", "Slide the battery, then watch the meters.");
            rig.controlsOpen = true;
            var go = Line(gate, gate.gateId + "_GO");
            if (go) StartCoroutine(Say(go));
            float waited = 0, held = 0, last = rig.state.voltage, start = last;
            while (true)
            {
                if (!AskWattsonUI.IsOpen && !(go && Speaking == go)) waited += Dt; // autoAnswer lets the line finish, never acts behind the Ask panel or under PAUSE
                float v = rig.state.voltage;
                if (!Mathf.Approximately(v, last)) { last = v; held = 0; } else held += Dt;
                if (Mathf.Approximately(v, volts) && held >= SettleSeconds) break;
                // A wrong setting the learner chose (the last step's value is not one: idling is the nudge's job).
                if (held >= SettleSeconds * 4 && !Mathf.Approximately(v, volts) && !Mathf.Approximately(v, start) && !hintSaid && !Mathf.Approximately(v, CircuitRig.KeyClosedDefaultVolts))
                {
                    hintSaid = true;
                    yield return Say(Line(gate, gate.gateId + "_H"));
                }
                if (HudKeys.ConsumeAct() || (ReviewMode.autoAnswer && waited >= ReviewMode.autoDelay)) rig.SetVoltage(volts);
                yield return null;
            }
            rig.controlsOpen = false;
            while (go && Speaking == go) yield return null; // he finishes his sentence before the reading
            // Observe, then record: the meters' own values go into the row and the point lands on the graph.
            Turn("Watch the ammeter.", $"{volts:0} of 5", "The current settles, then your row records itself.");
            yield return Wait(1.6f);
            Table.Record(row, rig.Volts, rig.Amps);
            LearnerSession.tableRows = Mathf.Max(LearnerSession.tableRows, row + 1);
            Play("Record");
            Table.Highlight(row, true);
            yield return Wait(0.5f);
            Graph.Axes(true);
            Graph.Plot(row + 1);
            Play("Plot", 1f + 0.08f * row);
            React(LearnerSession.Grade.Correct);
            if (Panel) Panel.Done($"Row {row + 1} recorded: {rig.Volts.ToString("0.0", Inv)} V, {rig.Amps.ToString("0.00", Inv)} A.");
            yield return Wait(0.9f);
            Table.Highlight(row, false);
        }
    }
}
