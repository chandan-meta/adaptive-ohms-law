using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OhmsLaw
{
    // Ask Wattson's action layer (Docs/Polish/WOW_PLAN.md section 6). The learner's words (typed or heard) become a
    // validated voltage change on the live rig, and the reply is written by the app from the value actually applied.
    // It runs before any network call, so it works offline and never waits for the AI; the AI never executes anything.
    // Parsing and validation are pure (no scene objects) so "Ohm's Law/Tests/Wattson Actions" runs them with a fake bench.
    public static class WattsonActions
    {
        public enum Kind { None, SetVoltage, ChangeVoltage, Double, Halve, Reset, Resistance, Current, Unclear }

        public struct Intent
        {
            public Kind kind;
            public double value;   // volts (Set: target, Change: signed step) or amperes (Current); NaN when not given
            public string note;    // Unclear: what to ask back. Current: "double" / "halve" / "up" / "down" or ""
        }

        // What the action layer reads and writes. The live one wraps CircuitRig.Active; tests pass a fake.
        public interface IBench
        {
            bool Present { get; }          // there is a circuit in this scene
            bool ControlsOpen { get; }     // the learner (and so Wattson) may change the voltage now
            double ControlsMax { get; }    // the highest voltage anyone may set now (SceneDirector.ControlsMax: 1 V in S10_Set1V / S10_Record, else 5)
            double Voltage { get; }        // the source setting
            double Amps { get; }           // the ammeter's reading: 0 while the key is open or the meter is out
            bool KeyClosed { get; }
            bool AmmeterIn { get; }
            bool ReadoutsShown { get; }    // the meters' digital plates are up (CircuitRig.readoutsShown): until then no reading is quoted
            int Scene { get; }             // 4..13: what has been taught so far (spoiler rules)
            int TableRows { get; }         // observation table rows recorded so far (LearnerSession.tableRows)
            bool ResistanceTaught { get; } // R and the ohm have been named (Scene 12, after S12_L14): only then is R = 5 ohms quoted
            bool RatioShown { get; }       // Scene 12 has revealed the V / I column (cue table.ratio 1): before that "V/I is always 5" is a spoiler
            string Gate { get; }           // the activity in progress, or null
            double TargetAmps { get; }     // the current a target activity asks for (S11_Target, S13_Apply); negative when none
            double StartVoltage { get; }   // the scene's starting setting (CircuitRig.StartVoltage), what "reset" goes back to
            double AmpsAt(double volts);   // this wire's current at a voltage (ExperimentData)
            double SetVoltage(double volts); // returns the value applied
        }

        public const double MinVolts = 0, MaxVolts = 5;

        // The most recent change made through Ask Wattson, for the AI's live-state context ("why did the reading change?").
        public static string LastChange { get; private set; } = "";
        public static double LastChangeVolts { get; private set; } = double.NaN;

        public static bool TryHandle(string text, IBench bench, out string reply)
        {
            var intent = Parse(text);
            reply = intent.kind == Kind.None ? null : Apply(intent, bench);
            return reply != null;
        }

        // ------------------------------------------------------------------ parsing (words -> intent)

        static readonly Dictionary<string, double> NumberWords = new Dictionary<string, double>
        {
            { "zero", 0 }, { "nought", 0 }, { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 }, { "five", 5 },
            { "six", 6 }, { "seven", 7 }, { "eight", 8 }, { "nine", 9 }, { "ten", 10 }, { "eleven", 11 }, { "twelve", 12 },
            { "twenty", 20 }, { "fifty", 50 }, { "hundred", 100 },
        };

        static readonly HashSet<string> VoltUnits = Set("v", "volt", "volts");
        static readonly HashSet<string> AmpUnits = Set("a", "amp", "amps", "ampere", "amperes");
        static readonly HashSet<string> OhmUnits = Set("ohm", "ohms", "ω");
        static readonly HashSet<string> VoltWords = Set("voltage", "voltages", "volt", "volts", "v", "battery", "source", "supply", "pd", "potential", "push", "knob", "eliminator", "slider");
        static readonly HashSet<string> CurrentWords = Set("current", "amps", "amp", "ampere", "amperes", "ammeter");
        static readonly HashSet<string> ResistWords = Set("resistance", "resistances", "resistor", "ohms", "ω");
        static readonly HashSet<string> SetVerbs = Set("set", "make", "put", "turn", "change", "switch", "go", "move", "adjust", "bring", "use", "apply", "select", "slide", "give", "try", "pick", "choose");
        static readonly HashSet<string> UpVerbs = Set("increase", "increases", "increased", "raise", "raised", "add", "boost", "bump", "crank", "increment");
        static readonly HashSet<string> DownVerbs = Set("decrease", "decreases", "decreased", "lower", "lowered", "reduce", "reduced", "drop", "subtract", "decrement", "lessen");
        // Bare comparatives count as a direction only in an order ("turn it up", "go up to 4 volts", "the voltage up by 1"):
        // "more voltage means more current" and "less voltage, less current?" are the learner thinking aloud, for the AI.
        static readonly HashSet<string> UpWords = Set("up", "higher", "more", "plus", "rise", "bigger");
        static readonly HashSet<string> DownWords = Set("down", "less", "minus", "smaller", "fewer");
        static readonly HashSet<string> Directing = Set("it", "that", "this", "by", "to");
        // A report of what happened, not an order: "I set the voltage to 4 volts and the current was 0.8".
        static readonly HashSet<string> Narrative = Set("was", "were", "went", "had", "got", "gave", "showed", "earlier", "before", "already", "because", "since");
        static readonly HashSet<string> DoubleWords = Set("double", "doubles", "doubled", "doubling", "twice");
        static readonly HashSet<string> HalveWords = Set("halve", "halves", "halved", "halving", "half");
        static readonly HashSet<string> ResetWords = Set("reset", "resets", "restart");
        static readonly HashSet<string> MaxWords = Set("max", "maximum", "highest", "full", "top");
        static readonly HashSet<string> MinWords = Set("min", "minimum", "lowest");
        // A question about the lesson, not an order: these go to the AI.
        static readonly HashSet<string> QuestionStarts = Set("what", "whats", "what's", "why", "how", "when", "where", "who", "which", "whose", "is", "are", "was", "were", "does", "do", "did", "will", "would", "could", "should", "can", "may", "might", "if", "isn't", "doesn't", "explain", "tell");
        static readonly HashSet<string> ChatVerbs = Set("explain", "tell", "describe", "say", "repeat", "mean", "means", "teach", "help", "understand", "know", "think", "talk", "answer", "simpler", "simply", "why");
        static readonly HashSet<string> Fillers = Set("wattson", "hey", "hi", "ok", "okay", "so", "now", "and", "um", "uh", "well", "please");

        static HashSet<string> Set(params string[] words) => new HashSet<string>(words);

        struct Token
        {
            public string word;
            public double number;   // NaN for words
            public string unit;     // "V", "A", "ohm" or null, for numbers
            public bool IsNumber => !double.IsNaN(number);
        }

        public static Intent Parse(string text)
        {
            var none = new Intent { kind = Kind.None, value = double.NaN, note = "" };
            var tokens = Tokenise(text);
            if (tokens.Count == 0) return none;
            bool Has(HashSet<string> set) => tokens.Exists(t => t.word != null && set.Contains(t.word));
            bool Phrase(params string[] words)
            {
                for (int i = 0; i + words.Length <= tokens.Count; i++)
                {
                    bool match = true;
                    for (int j = 0; j < words.Length && match; j++) match = tokens[i + j].word == words[j];
                    if (match) return true;
                }
                return false;
            }

            // Questions and talk go to the AI, unless the learner asks to be shown ("show me what happens when...").
            int first = 0;
            while (first < tokens.Count && tokens[first].word != null && Fillers.Contains(tokens[first].word)) first++;
            if (first >= tokens.Count) return none;
            string lead = tokens[first].word, next = first + 1 < tokens.Count ? tokens[first + 1].word : null;
            bool polite = (lead == "can" || lead == "could" || lead == "would" || lead == "will") && (next == "you" || next == "u" || next == "we");
            bool showMe = Phrase("show", "me") || Phrase("show", "us") || Phrase("let's") || Phrase("lets") || Phrase("let", "us") || Has(Set("demonstrate"));
            if (!showMe && (Has(ChatVerbs) || (lead != null && QuestionStarts.Contains(lead) && !polite))) return none;
            // A question mark or a told story ("current goes up when voltage goes up?", "the current went down when the
            // voltage went down") is never an order, whatever words it uses.
            if (!showMe && !polite && ((text ?? "").TrimEnd().EndsWith("?") || Has(Narrative))) return none;

            bool reset = Has(ResetWords) || Phrase("start", "over") || Phrase("start", "again");
            bool resist = tokens.Exists(t => t.unit == "ohm");
            for (int i = 0; i < tokens.Count; i++)   // "ohms law" is the law, not the wire
                resist |= tokens[i].word != null && ResistWords.Contains(tokens[i].word) && !(i + 1 < tokens.Count && tokens[i + 1].word == "law");
            bool set = Has(SetVerbs);
            bool it = Has(Set("it", "that", "this"));
            bool directed = set || showMe || Has(Directing);
            bool up = Has(UpVerbs) || (directed && Has(UpWords)), down = Has(DownVerbs) || (directed && Has(DownWords));
            bool twice = Has(DoubleWords) || TwoTimes(tokens);
            bool half = Has(HalveWords);
            bool verb = up || down || set || twice || half;

            double voltsNumber = First(tokens, "V", 0), ampsNumber = First(tokens, "A", 0), plain = First(tokens, null, 0);
            double extreme = Has(MaxWords) ? MaxVolts : Has(MinWords) ? MinVolts : double.NaN;
            bool voltWord = Has(VoltWords);
            bool voltSubject = voltWord || !double.IsNaN(voltsNumber);
            bool currentSubject = !double.IsNaN(ampsNumber);
            double currentTarget = double.NaN;   // a plain number after the word "current": "make the current 0.6"
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].word == null || !CurrentWords.Contains(tokens[i].word)) continue;
                // "the current voltage" means the present one.
                if (tokens[i].word == "current" && i + 1 < tokens.Count && tokens[i + 1].word != null && VoltWords.Contains(tokens[i + 1].word)) continue;
                currentSubject = true;
                if (double.IsNaN(currentTarget)) currentTarget = First(tokens, null, i + 1);
            }

            Intent Make(Kind kind, double value = double.NaN, string note = "") => new Intent { kind = kind, value = value, note = note };
            const string SayVoltage = "Do you mean the voltage? Try \"set the voltage to 3 volts\".";

            if (reset && !resist) return Make(Kind.Reset);
            if (resist && (verb || tokens.Exists(t => t.IsNumber))) return Make(Kind.Resistance);

            if (twice || half)
            {
                if (voltSubject) return Make(twice ? Kind.Double : Kind.Halve);
                if (currentSubject) return Make(Kind.Current, double.NaN, twice ? "double" : "halve");
                return Make(Kind.Unclear, double.NaN, SayVoltage);
            }

            // A current target ("set the voltage so the current is 0.6 A", "make the ammeter read 0.6"), unless volts are
            // given too. With both words and no target, the voltage is what can change: "raise the voltage and watch the current".
            if (double.IsNaN(voltsNumber) && currentSubject && (!double.IsNaN(ampsNumber) || !double.IsNaN(currentTarget) || (!voltWord && verb)))
                return Make(Kind.Current, !double.IsNaN(ampsNumber) ? ampsNumber : currentTarget, up ? "up" : down ? "down" : "");

            if (voltSubject)
            {
                double to = After(tokens, "to"), by = After(tokens, "by");
                if (up || down)
                {
                    if (up && down) return Make(Kind.Unclear, double.NaN, "Up or down? Try \"increase the voltage by 1 volt\".");
                    if (!double.IsNaN(to) && double.IsNaN(by)) return Make(Kind.SetVoltage, to);
                    if (!double.IsNaN(extreme)) return Make(Kind.SetVoltage, extreme);
                    // With a comparative and no real verb, a volts number is the target ("turn it up, 3 volts"); a step only after "by".
                    if (!double.IsNaN(voltsNumber) && double.IsNaN(by) && !Has(UpVerbs) && !Has(DownVerbs)) return Make(Kind.SetVoltage, voltsNumber);
                    double step = !double.IsNaN(by) ? by : !double.IsNaN(voltsNumber) ? voltsNumber : !double.IsNaN(plain) ? plain : 1;
                    return Make(Kind.ChangeVoltage, up ? step : -step);
                }
                double target = !double.IsNaN(to) ? to : !double.IsNaN(voltsNumber) ? voltsNumber : !double.IsNaN(plain) ? plain : extreme;
                // "4 volts", "I want 4 volts", "maximum voltage please": a short order with no verb.
                bool bare = !double.IsNaN(target) && tokens.Count - first <= 4;
                if (set || bare)
                    return double.IsNaN(target) ? Make(Kind.Unclear, double.NaN, "How many volts? Try \"set the voltage to 3 volts\".") : Make(Kind.SetVoltage, target);
                return none;
            }

            // An order with a number or "it" but nothing it applies to: "set it to 4", "turn it up".
            if ((up || down || (set && !double.IsNaN(plain))) && (it || !double.IsNaN(plain))) return Make(Kind.Unclear, double.NaN, SayVoltage);
            return none;
        }

        // "2 times", "times 2", "2x", "x2"
        static bool TwoTimes(List<Token> tokens)
        {
            for (int i = 0; i + 1 < tokens.Count; i++)
            {
                bool two = tokens[i].IsNumber && tokens[i].number == 2 && tokens[i].unit == null;
                bool twoNext = tokens[i + 1].IsNumber && tokens[i + 1].number == 2 && tokens[i + 1].unit == null;
                if (two && (tokens[i + 1].word == "times" || tokens[i + 1].word == "x")) return true;
                if (twoNext && (tokens[i].word == "times" || tokens[i].word == "x")) return true;
            }
            return false;
        }

        static double First(List<Token> tokens, string unit, int from)
        {
            for (int i = from; i < tokens.Count; i++) if (tokens[i].IsNumber && tokens[i].unit == unit) return tokens[i].number;
            return double.NaN;
        }

        // The number right after a word ("to 4", "by 1"), skipping "the" / "a".
        static double After(List<Token> tokens, string word)
        {
            for (int i = 0; i + 1 < tokens.Count; i++)
            {
                if (tokens[i].word != word) continue;
                int j = i + 1;
                while (j < tokens.Count && (tokens[j].word == "the" || tokens[j].word == "a")) j++;
                if (j < tokens.Count && tokens[j].IsNumber && tokens[j].unit != "A") return tokens[j].number;
            }
            return double.NaN;
        }

        static List<Token> Tokenise(string text)
        {
            string s = (text ?? "").ToLowerInvariant().Replace('’', '\'').Replace('−', '-').Replace("p.d.", "pd").Replace("Ω", "ω");
            s = Regex.Replace(s, @"(\d)(?=[a-zω])", "$1 ");  // "4v" -> "4 v", "0.6a" -> "0.6 a"
            var raw = new List<string>();
            foreach (Match m in Regex.Matches(s, @"-?\d+(?:\.\d+)?|-?\.\d+|[a-zω']+")) raw.Add(m.Value);

            string At(int k) => k < raw.Count ? raw[k] : null;
            var tokens = new List<Token>();
            bool negate = false;
            for (int i = 0; i < raw.Count; i++)
            {
                string w = raw[i];
                double n = double.NaN;
                if (double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) n = parsed;
                else if (NumberWords.TryGetValue(w, out var word)) n = word;
                else if ((w == "a" || w == "an") && IsUnit(At(i + 1))) n = 1;             // "add a volt"
                else if (w == "for" && VoltUnits.Contains(At(i + 1) ?? "")) n = 4;          // speech: "set it to for volts"
                else if (w == "half" && At(i + 1) == "a" && IsUnit(At(i + 2))) { n = 0.5; i++; }
                if (double.IsNaN(n))
                {
                    // "minus 2 volts" is a negative number, not "lower by 2".
                    if ((w == "minus" || w == "negative") && i + 1 < raw.Count && (double.TryParse(raw[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out _) || NumberWords.ContainsKey(raw[i + 1]))) { negate = true; continue; }
                    tokens.Add(new Token { word = w, number = double.NaN });
                    continue;
                }
                if (negate) { n = -n; negate = false; }

                // "two point five", "one and a half"
                if (At(i + 1) == "point" && i + 2 < raw.Count && NumberWords.TryGetValue(raw[i + 2], out var tenths) && tenths < 10) { n += tenths / 10; i += 2; }
                else if (At(i + 1) == "and" && At(i + 2) == "a" && At(i + 3) == "half") { n += 0.5; i += 3; }

                string unit = null, u = At(i + 1);
                if (u != null && VoltUnits.Contains(u)) unit = "V";
                else if (u != null && AmpUnits.Contains(u)) unit = "A";
                else if (u != null && OhmUnits.Contains(u)) unit = "ohm";
                tokens.Add(new Token { word = null, number = n, unit = unit });
            }
            return tokens;
        }

        static bool IsUnit(string w) => w != null && (VoltUnits.Contains(w) || AmpUnits.Contains(w) || OhmUnits.Contains(w));

        // ------------------------------------------------------------------ validation and execution

        public static string Apply(Intent intent, IBench bench)
        {
            if (intent.kind == Kind.None) return null;
            if (!bench.Present) return "There's no circuit for me to control in this part of the case.";
            // Declined: the same wire keeps the same resistance. Its value and unit only once they have been taught (spoiler rule:
            // Scene 12's own question asks for V / I before S12_L14 names the ohm).
            if (intent.kind == Kind.Resistance)
                return bench.ResistanceTaught
                    ? "This experiment keeps the same nichrome wire, so its resistance stays at " + Volts(Ohms(bench)) + " ohms. You can change the voltage instead."
                    : "This experiment keeps the same nichrome wire, so its resistance stays the same. You can change the voltage instead.";
            if (!bench.ControlsOpen) return "The battery control isn't open right now. When it is, tap a voltage button or ask me to set the voltage.";
            if (intent.kind == Kind.Unclear) return intent.note.Replace(Example, ExampleCommand(bench));
            if (intent.kind == Kind.Current) return CurrentReply(intent, bench);

            double now = bench.Voltage, ampsBefore = bench.Amps;
            double target;
            bool snapped = false;
            switch (intent.kind)
            {
                case Kind.Reset:
                    target = bench.StartVoltage; // the scene's start value, through the same cap check as every other setting
                    break;
                case Kind.SetVoltage:
                    target = intent.value;
                    if (target < MinVolts || target > MaxVolts) return $"This source goes from 0 to 5 volts, so I can't set {Volts(target)} V.";
                    if (!Whole(target)) return $"The battery control clicks in whole volts, from 0 to 5. Try {Volts(Math.Floor(target))} V or {Volts(Math.Ceiling(target))} V.";
                    break;
                case Kind.ChangeVoltage:
                    if (!Whole(intent.value) || intent.value == 0) return "The battery control clicks in whole volts. Try \"increase the voltage by 1 volt\".";
                    target = now + intent.value;
                    if (target > MaxVolts + 1e-6) return now >= MaxVolts - 1e-6 ? "It's already at the top: 5 V." : $"That would be {Volts(target)} V, but this source stops at 5 V. It's at {Volts(now)} V now.";
                    if (target < MinVolts - 1e-6) return now <= MinVolts + 1e-6 ? "It's already at 0 V." : $"That would go below 0 V. It's at {Volts(now)} V now.";
                    if (!Whole(target)) { target = intent.value > 0 ? Math.Floor(target) : Math.Ceiling(target); snapped = true; } // from the 0.5 V start
                    break;
                case Kind.Double:
                    if (now <= 1e-6) return "The voltage is 0 V, and double zero is still zero. Set 1 or 2 volts first, then ask me to double it.";
                    target = now * 2;
                    if (target > MaxVolts + 1e-6) return $"Double {Volts(now)} V would be {Volts(target)} V, but this source stops at 5 V. Set 1 or 2 volts first, then double it.";
                    break;
                case Kind.Halve:
                    if (now <= 1e-6) return "It's already at 0 V.";
                    target = now / 2;
                    if (!Whole(target)) return $"Half of {Volts(now)} V is {Volts(target)} V, but the battery control clicks in whole volts. Try {Volts(Math.Floor(target))} V or {Volts(Math.Ceiling(target))} V.";
                    break;
                default: return null;
            }
            // The cap (S10_Set1V / S10_Record hold it at 1 V): nothing above it is applied, so no 2 V reading leaks before
            // the S10_Predict2V prediction, and no reply quotes a setting the director would undo the next frame.
            if (target > bench.ControlsMax + 1e-6)
                return $"For this reading the knob stops at {Volts(bench.ControlsMax)} V. Higher settings come after your prediction.";
            double applied = bench.SetVoltage(target);
            string verb = intent.kind == Kind.Double ? "doubled from " + Volts(now) + " V to" :
                          intent.kind == Kind.Halve ? "halved from " + Volts(now) + " V to" :
                          intent.kind == Kind.ChangeVoltage ? (applied > now ? "up to" : "down to") :
                          intent.kind == Kind.Reset ? "back to the starting" : "";
            string reply = Applied(bench, now, ampsBefore, applied, verb, snapped, intent.kind);
            return reply;
        }

        // The confirmation: always the applied value and the live ammeter, plus at most one fixed explanatory sentence
        // once the relationship has been taught (Scene 11 on; before that it would answer the Scene 6/10 questions).
        static string Applied(IBench bench, double before, double ampsBefore, double applied, string verb, bool snapped, Kind kind)
        {
            bool changed = Math.Abs(applied - before) > 1e-6;
            if (changed) { LastChange = $"At the learner's request the app changed the voltage from {Volts(before)} V to {Volts(applied)} V."; LastChangeVolts = applied; }
            string head = !changed
                ? (kind == Kind.Reset ? $"It's already at the starting {Volts(applied)} V." : $"It's already at {Volts(applied)} V.")
                : $"Done: {(verb.Length > 0 ? verb + " " : "")}{Volts(applied)} V." + (snapped ? " The control clicks in whole volts." : "");
            string reading = !bench.AmmeterIn ? "" :
                             !bench.KeyClosed ? " The key is open, so no current flows yet." :
                             !bench.ReadoutsShown ? ReadoutHidden :
                             (kind == Kind.Double || kind == Kind.Halve) && changed ? $" The ammeter went from {Amps(ampsBefore)} A to {Amps(bench.Amps)} A." :
                             $" The ammeter {(changed ? "now reads" : "reads")} {Amps(bench.Amps)} A.";
            string explain = "";
            if (changed && bench.Scene >= 11 && bench.AmmeterIn && bench.KeyClosed && bench.ReadoutsShown)
                explain = kind == Kind.Double ? " Double the voltage, double the current." :
                          kind == Kind.Halve ? " Half the voltage, half the current." :
                          kind == Kind.ChangeVoltage ? (applied > before ? " More push, more current." : " Less push, less current.") :
                          " The current follows the voltage.";
            return head + reading + explain;
        }

        static string CurrentReply(Intent intent, IBench bench)
        {
            const string Cant = "I can't set the current directly: it follows the voltage.";
            bool target = TargetActivity(bench);
            double perVolt = bench.AmpsAt(1);
            bool asked = !double.IsNaN(intent.value);
            double goal = asked ? intent.value : bench.TargetAmps;
            if (target)
            {
                // The learner's own puzzle: a hint, never the answer. R only once it has been taught.
                if (bench.ResistanceTaught) return Cant + $" Use V = IR with R = {Volts(Ohms(bench))} ohms to work out the voltage, then ask me to set it.";
                return Cant + $" Each volt gave {Amps(perVolt)} A in our table." + (goal >= 0 ? $" Which voltage gives {Amps(goal)} A?" : " Which voltage gives your target?");
            }
            if (bench.Scene < 11) return "I can't set the current directly: the current is whatever the circuit gives. Change the voltage and watch the ammeter.";
            if (asked)
            {
                for (int v = (int)MinVolts; v <= (int)MaxVolts; v++)
                    if (Math.Abs(bench.AmpsAt(v) - intent.value) < 0.005) return Cant + $" {v} V gives {Amps(intent.value)} A on this wire, so try \"set the voltage to {v} volts\".";
                return Cant + $" With whole volts from 0 to 5, this wire gives 0.00 to {Amps(bench.AmpsAt(MaxVolts))} A in steps of {Amps(perVolt)} A.";
            }
            switch (intent.note)
            {
                case "double": return Cant + " Double the voltage and the current doubles too: say \"double the voltage\".";
                case "halve": return Cant + " Halve the voltage and the current halves too: say \"halve the voltage\".";
                case "up": return Cant + " Raise the voltage and the current rises with it.";
                case "down": return Cant + " Lower the voltage and the current falls with it.";
                default: return Cant + " Change the voltage and the current follows.";
            }
        }

        static bool GateIs(IBench bench, string part) => bench.Gate != null && bench.Gate.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        static bool TargetActivity(IBench bench) => bench.TargetAmps >= 0 || GateIs(bench, "target") || GateIs(bench, "apply");

        // ------------------------------------------------------------------ the AI's rules for this moment

        // The app's rules inside the AI's live state (WattsonAgent.DescribeState). The proxy prompt follows text in this form
        // and ignores any other instruction in the app's sections. The proxy keeps only the rules whose text is, word for
        // word, one of the seven it knows (Server/wattson-proxy/index.js APP_RULES) and drops any other, so a new or
        // reworded rule here needs its full text copied there.
        public static string Rule(string text) => "[App rule: " + text + "]";

        // Activities are the learner's own puzzles: a prediction favours no option, a question or a target gets one
        // reasoning hint, never the option, the number or the voltage. "" when no activity is running.
        public static string ActivityRule(IBench bench, bool questionOnScreen)
        {
            if (GateIs(bench, "predict"))
                return Rule("the learner is making a prediction; do not favour any option or hint which one is right: any honest guess is fine, and the experiment will tell");
            if (TargetActivity(bench))
                return Rule("target activity: never say the voltage that hits the target; give one hint about the reasoning and ask what they notice");
            if (questionOnScreen)
                return Rule("on-screen question: never give the option, its letter or the number it asks for; give one hint about the reasoning and ask what they notice");
            return "";
        }

        // R = 5 ohms only once it has been taught (Scene 12, after S12_L14); before that a rule keeps the value and the unit out.
        public static string ResistanceState(IBench bench)
        {
            if (bench.ResistanceTaught) return "Resistance has been taught: the nichrome wire XY has R = " + Volts(Ohms(bench)) + " ohms";
            return bench.Scene == 12
                ? Rule("resistance is being introduced in this scene; do not state its value or its unit yet")
                : Rule("resistance has not been taught yet; never give its value or mention ohms. If asked to change it, this experiment keeps the same wire, so its resistance stays the same");
        }

        // ------------------------------------------------------------------ saved answers and examples

        // Saved FAQ answers and suggested questions are written for the whole scene, so none during an activity (some answer
        // it outright: Scene 5's "Which way do electrons move?" in S05_Drift, Scene 12's "Why is V divided by I always 5?" in
        // S12_Ratio), none naming the ohm before it is taught, and none telling Scene 10's "Why does the current change..."
        // before row 2 is recorded.
        public static bool SavedAnswerAllowed(IBench bench, bool activity, string question, string answer) =>
            !activity && FactAllowed(bench, question) && FactAllowed(bench, answer);

        // A scene's covered fact (or saved answer) the AI may be given now: the ohm only once taught; a reading above 1 V
        // or the way the current goes with the voltage only once Scene 10 has recorded row 2; the constant V / I only once
        // Scene 12's board has shown the column (its punchline, and the S12_Ratio answer).
        public static bool FactAllowed(IBench bench, string text) =>
            (bench.ResistanceTaught || !NamesOhm(text)) && !(OneVoltOnly(bench) && RevealsRelationship(text)) && !(RatioPending(bench) && RevealsRatio(text));

        // Scene 12 before the cue "table.ratio 1" (the dive and the first board lines): "V/I is always 5" stays out.
        public static bool RatioPending(IBench bench) => bench.Scene == 12 && !bench.RatioShown;

        // "V / I", "ΔV/ΔI", "V ÷ I", "divided by", "always 5", "= 5", "comes out at 5".
        static readonly Regex RatioFact = new Regex(@"(?<![A-Za-z])Δ?V\s*[/÷]\s*Δ?I\b|÷|divided by|(?:\b(?:always|comes out at)|=)\s*5\b", RegexOptions.IgnoreCase);
        public static bool RevealsRatio(string text) => RatioFact.IsMatch(text ?? "");

        // Scene 10 before row 2 (S10_Record2): the learner has measured only 1 V, and S10_Predict2V / S10_Pattern ask what
        // comes next. Until then the facts "2 V → 0.4 A" and "the current increases with the voltage" stay out.
        public static bool OneVoltOnly(IBench bench) => bench.Scene == 10 && bench.TableRows < 2;
        public static string OneVoltRule(IBench bench) => OneVoltOnly(bench)
            ? Rule("the learner has measured only 1 V so far; never say what the ammeter reads at another voltage or how the current changes with the voltage: the experiment will show it")
            : "";

        // A voltage above 1 V or a current above 0.2 A ("2 V", "2.0 volts", "0.4 A", "1.0 A"), or the current with a change
        // word in the same sentence ("the current increases", "more ... current", "the current change when I change...").
        static readonly Regex AboveOneVolt = new Regex(@"(?<![\d.])\b(?:[2-9]|[1-9]\d)(?:\.\d+)?\s*(?:V|volts?)\b|(?<![\d.])\b(?:0\.(?:2[1-9]|[3-9])\d*|[1-9](?:\.\d+)?)\s*(?:A|amperes?)\b");
        const string ChangeWords = @"(?:increas\w*|decreas\w*|doubl\w*|halv\w*|chang\w*|ris(?:e|es|ing)|fall\w*|bigger|smaller|more|less|follow\w*)";
        static readonly Regex CurrentChanges = new Regex(@"\b(?:current|amperes?)\b[^.?!]*\b" + ChangeWords + @"\b|\b" + ChangeWords + @"\b[^.?!]*\b(?:current|amperes?)\b", RegexOptions.IgnoreCase);
        public static bool RevealsRelationship(string text) => AboveOneVolt.IsMatch(text ?? "") || CurrentChanges.IsMatch(text ?? "");

        // The unit (lowercase "ohm", "ohms", or Ω); Georg Ohm and Ohm's law are not it.
        static readonly Regex OhmUnit = new Regex(@"\bohms?\b|Ω");
        public static bool NamesOhm(string text) => OhmUnit.IsMatch(text ?? "");

        // The offline reply while an activity runs: no saved answer, and the puzzle stays the learner's.
        public const string OfflinePuzzle = "My radio's fuzzy, but this puzzle is yours anyway. Give it a go!";

        // The example command in a reply: never above the cap (the knob stops at 1 V in S10_Set1V / S10_Record), never the
        // answer to a running target (S11_Target's 0.60 A is 3 V).
        const string Example = "set the voltage to 3 volts";
        static string ExampleCommand(IBench bench)
        {
            if (bench.ControlsMax < MaxVolts) return $"set the voltage to {Volts(bench.ControlsMax)} volt{(Math.Abs(bench.ControlsMax - 1) < 1e-6 ? "" : "s")}";
            if (!TargetActivity(bench)) return Example;
            foreach (int v in new[] { 2, 1, 5 })
                if (bench.TargetAmps < 0 || Math.Abs(bench.AmpsAt(v) - bench.TargetAmps) > 0.005) return $"set the voltage to {v} volt{(v == 1 ? "" : "s")}";
            return Example;
        }

        // Scenes 9-10 hide the meters' digital plates until the 1 V baseline (CircuitRig.readoutsShown): no chat text quotes a
        // number the screen does not show.
        const string ReadoutHidden = " The ammeter is connected, but its reading isn't shown yet.";

        // Essential guidance when the AI is unreachable: the real readings and what the learner can still do. App text,
        // shown as an offline answer.
        public static string Guidance(IBench bench)
        {
            if (!bench.Present) return "You can still tap a suggested question for a saved answer, or carry on with the case.";
            string s = $"Here's what I can see: the source is set to {Volts(bench.Voltage)} V";
            s += !bench.AmmeterIn ? "." : !bench.KeyClosed ? " and the key is open, so no current flows." : !bench.ReadoutsShown ? "." + ReadoutHidden : $" and the ammeter reads {Amps(bench.Amps)} A.";
            if (bench.ControlsOpen) s += $" You can tap a voltage button, or type \"{ExampleCommand(bench)}\": I can still do that without my radio.";
            return s;
        }

        // The confirmation as it should sound: units in words for the voice (the screen keeps the symbols).
        public static string Spoken(string text)
        {
            text = text.Replace("V = IR", "V equals I times R");
            text = Regex.Replace(text, @"(?<![\d.])1 V\b", "1 volt");
            text = Regex.Replace(text, @"(\d) V\b", "$1 volts");
            text = Regex.Replace(text, @"(\d) A\b", "$1 amperes");
            return text;
        }

        // "Scene10", "Scene10_Experiment" -> 10; 0 when there is no number.
        public static int SceneNumber(string sceneId)
        {
            var m = Regex.Match(sceneId ?? "", @"\d+");
            return m.Success && int.TryParse(m.Value, out int n) ? n : 0;
        }

        static bool Whole(double v) => Math.Abs(v - Math.Round(v)) < 1e-6;
        static double Ohms(IBench bench) { double perVolt = bench.AmpsAt(1); return perVolt > 0 ? 1 / perVolt : 0; }
        public static string Volts(double v) => (Whole(v) ? Math.Round(v) : v).ToString(Whole(v) ? "0" : "0.0#", CultureInfo.InvariantCulture);
        public static string Amps(double a) => a.ToString("0.00", CultureInfo.InvariantCulture);

#if UNITY_5_3_OR_NEWER
        public static bool TryHandle(string text, out string reply) => TryHandle(text, LiveBench.Instance, out reply);
        public static string Guidance() => Guidance(LiveBench.Instance);

        // The real bench: CircuitRig.Active, the gate in progress and the scene's number.
        public sealed class LiveBench : IBench
        {
            public static readonly LiveBench Instance = new LiveBench();
            static CircuitRig Rig => CircuitRig.Active;

            public bool Present => Rig;
            public bool ControlsOpen => Rig && Rig.controlsOpen;
            public double ControlsMax => SceneDirector.ControlsMax;
            public double Voltage => Rig ? Rig.state.voltage : 0;
            public double Amps => Rig ? Rig.Amps : 0;
            public bool KeyClosed => Rig && Rig.state.keyClosed;
            public bool AmmeterIn => Rig && Rig.ammeterPlaced;
            public bool ReadoutsShown => Rig && Rig.readoutsShown;
            public string Gate => SceneDirector.Gate;
            public double TargetAmps => Rig ? Rig.targetAmps : -1;
            public double StartVoltage => Rig ? Rig.StartVoltage : 0;
            public int TableRows => LearnerSession.tableRows;
            // Set by the cue "readings.r 1" after S12_L14. Scene 13 comes after that teaching, so a direct jump there counts too.
            public bool ResistanceTaught => LearnerSession.resistanceTaught || Scene >= 13;
            // Set by the cue "table.ratio 1" (Scene 12's board stretch); only Scene 12 before it filters the ratio facts.
            public bool RatioShown => LearnerSession.ratioShown;
            public int Scene
            {
                get
                {
                    var wattson = WattsonController.Active;
                    var context = wattson ? wattson.agentContext : null;
                    return SceneNumber(context ? context.sceneId : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                }
            }
            public double AmpsAt(double volts) => ExperimentData.Main ? ExperimentData.Main.CurrentAt((float)volts) : volts / 5.0;
            public double SetVoltage(double volts) => Rig ? Rig.SetVoltage((float)volts) : 0;
        }
#endif
    }
}
