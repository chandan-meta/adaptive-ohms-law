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
        const float SettleSeconds = 0.6f;
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
        // Revision 2026-10-22 (Docs/Polish/REVISION_2026-10-22.md):
        //   link <row> [row…]   a row and its point light together; a spark travels from the row to the point
        //   graph.axis V|I      that axis's title and line glow while it is talked about
        //   point.row <i> · point.point <i> · point.axis V|I · point.column <c> · point.formula · point.hero · point.law
        //                       Wattson stands beside the surface and points at it (WattsonGuide); point.clear lets him go
        //   snap                Wattson's finger snap (hero, law.piece and law.name snap by themselves)
        //   law.pieces · law.piece V|I|R · law.name · law.conditions · law.forms · law.demo [seconds] · law.clear
        //                       the finale's centred surface (LawReveal); S13_BuildLaw taps its tiles into V = I × R
        bool ExperimentCue(string verb, string[] words, string rest, float value, bool on)
        {
            switch (verb)
            {
                case "link": if (Panel) for (int i = 1; i < words.Length; i++) if (int.TryParse(words[i], out int row)) Link(row); return true;
                case "graph.axis": if (Panel && rest.Length > 0) Graph.AxisGlow(rest[0]); return true;
                case "point.row": if (Panel) GuideCalls.Point(Table.Row((int)value), WattsonGuide.Mark.Bracket); return true;
                case "point.point": if (Panel) GuideCalls.Point(Graph.Point((int)value), WattsonGuide.Mark.Ring); return true;
                case "point.axis":
                {
                    if (!Panel) return true;
                    bool v = rest.StartsWith("V");
                    GuideCalls.Point(Graph.AxisTitle(v ? 'V' : 'I'), v ? WattsonGuide.Mark.None : WattsonGuide.Mark.Underline); // the V title stands on its side: graph.axis lights it
                    return true;
                }
                case "point.column": if (Panel) GuideCalls.Point(Table.Header((int)value), WattsonGuide.Mark.Bracket); return true;
                case "point.formula": if (Panel) GuideCalls.Point(Panel.Block("Formula"), WattsonGuide.Mark.Bracket); return true;
                case "point.hero": if (Panel) GuideCalls.Point(Panel.HeroRect, WattsonGuide.Mark.Auto); return true;
                case "point.law": if (LawReveal.Instance) GuideCalls.Point(LawReveal.Instance.EquationRect, WattsonGuide.Mark.None); return true; // the law needs no mark of its own
                case "point.clear": GuideCalls.Release(); return true;
                case "snap": SnapReveal(); return true;
                case "law.pieces": LawReveal.Ensure(); return true;
                case "law.piece": { string symbol = rest.Trim(); if (LawReveal.Ensure()) StartCoroutine(SnapThen(() => { if (LawReveal.Instance) LawReveal.Instance.Piece(symbol); })); return true; }
                case "law.name":
                    if (LawReveal.Ensure()) StartCoroutine(SnapThen(() =>
                    {
                        if (!LawReveal.Instance) return;
                        LawReveal.Instance.Name();
                        Play("SFX_Magic_Success", 1f, 0.55f);   // the one accent of the finale, kept low
                    }));
                    return true;
                case "law.conditions": if (LawReveal.Ensure()) LawReveal.Instance.Conditions(); return true;
                case "law.forms": if (LawReveal.Ensure()) LawReveal.Instance.Forms(); Play("Reveal"); return true;
                case "law.demo": if (LawReveal.Ensure()) LawReveal.Instance.Demo(value > 0f ? value : 4f, v => Play("VoltStep", 1f + 0.07f * (v - 1f))); return true;
                case "law.clear": if (LawReveal.Instance) LawReveal.Instance.Close(); return true;
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
                case "music": Bed(music, rest, 2.0f); return true;
                case "end": if (Flow) Flow.EndOfModule(); return true;
                case "hero":
                {
                    if (!Panel) return true;
                    int bar = rest.IndexOf('|');
                    string big = bar < 0 ? rest : rest.Substring(0, bar).Trim(), small = bar < 0 ? "" : rest.Substring(bar + 1).Trim();
                    if (pendingHero != null) StopCoroutine(pendingHero);
                    // The plate lands on Wattson's snap (at once when he does not snap: off screen, a restoring cue).
                    pendingHero = StartCoroutine(SnapThen(() => { pendingHero = null; if (Panel) { Panel.Hero(big, small); Play("Reveal"); } }));
                    return true;
                }
                case "hero.clear": if (pendingHero != null) { StopCoroutine(pendingHero); pendingHero = null; } if (Panel) Panel.HeroClear(); return true;
                case "readings.top":
                {
                    if (!Panel) return true;
                    bool had = readings;
                    Readings(false);                 // the card rebuilds at the strip's size (or the dock's)
                    Panel.TopReadings(on);
                    Readings(on || had);
                    return true;
                }
                case "stage": if (Panel) Panel.Stage(on); if (!on) GuideCalls.Release(); return true;
                default: return false;
            }
        }

        Coroutine pendingHero; // a hero plate waiting for the snap's click

        // "link": the row lights at once, a spark carries it to its point, and the point lights (with a pop) as it lands.
        void Link(int row)
        {
            Table.Highlight(row, true);
            var graph = Graph;
            LinkSpark.Fly(Table.Row(row), graph.Point(row), () => { if (graph) { graph.Highlight(row, true); graph.Bump(row); } });
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
            // Observe, then record: the meters' own values go into the row, a spark carries the reading from the row to the
            // graph, and the point pops in as it lands (row and point lit together, one pair).
            Turn("Watch the ammeter.", $"{volts:0} of 5", "The current settles, then your row records itself.");
            yield return Wait(1.0f);
            Table.Record(row, rig.Volts, rig.Amps);
            LearnerSession.tableRows = Mathf.Max(LearnerSession.tableRows, row + 1);
            Play("Record");
            Table.Highlight(row, true);
            yield return Wait(0.25f);
            Graph.Axes(true);
            bool landed = false;
            LinkSpark.Fly(Table.Row(row), Graph.Point(row), () => landed = true);
            for (float t = 0; !landed && t < LinkSpark.Seconds + 0.5f; t += Dt) yield return null;
            Graph.Plot(row + 1);
            Graph.Highlight(row, true);
            Play("Plot", 1f + 0.08f * row);
            React(LearnerSession.Grade.Correct);
            if (Panel) Panel.Done($"Row {row + 1} recorded: {rig.Volts.ToString("0.0", Inv)} V, {rig.Amps.ToString("0.00", Inv)} A.");
            yield return Wait(0.6f);
            Table.Highlight(row, false);
            Graph.Highlight(row, false);
        }
    }
}
