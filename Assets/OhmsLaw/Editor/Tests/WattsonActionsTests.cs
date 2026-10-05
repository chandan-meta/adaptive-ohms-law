using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace OhmsLaw
{
    // "Ohm's Law/Tests/Wattson Actions": the Ask Wattson action layer (parser + validator + reply) on the example
    // requests, against a fake bench, in edit mode. Logs one PASS/FAIL line per check and a summary line.
    public static class WattsonActionsTests
    {
#if UNITY_EDITOR
        [MenuItem("Ohm's Law/Tests/Wattson Actions")]
        static void Menu()
        {
            int failed = Run(line => Debug.Log(line));
            // The offline FAQ match ignores case and punctuation ("what is voltage" finds "What is voltage?").
            failed += Check(line => Debug.Log(line), "LocalFAQ key ignores case/punctuation", LocalFAQ.Key("what is voltage") == LocalFAQ.Key("What is voltage?"), LocalFAQ.Key("What is voltage?"));
            failed += Check(line => Debug.Log(line), "LocalFAQ key keeps different questions apart", LocalFAQ.Key("What is current?") != LocalFAQ.Key("What is voltage?"), "");
            if (failed == 0) Debug.Log("Wattson Actions: PASS (all checks)");
            else Debug.LogError($"Wattson Actions: FAIL ({failed} checks failed)");
        }
#endif

        public sealed class FakeBench : WattsonActions.IBench
        {
            public bool present = true, controlsOpen = true, keyClosed = true, ammeterIn = true, resistanceTaught;
            public double volts = 1, start = 0.5, targetAmps = -1, controlsMax = 5;
            public int scene = 10, tableRows, setCalls;
            public string gate;
            public double TargetAmps => targetAmps;

            public bool Present => present;
            public bool ControlsOpen => controlsOpen;
            public double ControlsMax => controlsMax;
            public double Voltage => volts;
            public double Amps => keyClosed && ammeterIn ? AmpsAt(volts) : 0;
            public bool KeyClosed => keyClosed;
            public bool AmmeterIn => ammeterIn;
            public int Scene => scene;
            public int TableRows => tableRows;
            public bool ResistanceTaught => resistanceTaught;
            public string Gate => gate;
            public double AmpsAt(double v) => v / 5.0;   // ExperimentData: R = 5
            public double SetVoltage(double v) { setCalls++; return volts = Math.Max(0, Math.Min(5, v)); }
            public double ResetVoltage() => SetVoltage(start);
        }

        static int Check(Action<string> log, string name, bool ok, string detail)
        {
            log((ok ? "PASS " : "FAIL ") + name + (ok || string.IsNullOrEmpty(detail) ? "" : "  -> " + detail));
            return ok ? 0 : 1;
        }

        public static int Run(Action<string> log)
        {
            int failed = 0;

            // Parsing: words -> intent.
            void Parses(string text, WattsonActions.Kind kind, double value = double.NaN)
            {
                var intent = WattsonActions.Parse(text);
                bool ok = intent.kind == kind && (double.IsNaN(value) || Math.Abs(intent.value - value) < 1e-6);
                failed += Check(log, $"parse \"{text}\" -> {kind}{(double.IsNaN(value) ? "" : " " + value)}", ok, $"got {intent.kind} {intent.value}");
            }
            Parses("set the voltage to four volts", WattsonActions.Kind.SetVoltage, 4);
            Parses("Set the voltage to 4 volts.", WattsonActions.Kind.SetVoltage, 4);
            Parses("4V", WattsonActions.Kind.SetVoltage, 4);
            Parses("4 V please", WattsonActions.Kind.SetVoltage, 4);
            Parses("I want 4 volts", WattsonActions.Kind.SetVoltage, 4);
            Parses("set voltage 3", WattsonActions.Kind.SetVoltage, 3);
            Parses("can you set the voltage to 3 volts please", WattsonActions.Kind.SetVoltage, 3);
            Parses("go up to 4 volts", WattsonActions.Kind.SetVoltage, 4);
            Parses("maximum voltage", WattsonActions.Kind.SetVoltage, 5);
            Parses("set the voltage to two point five volts", WattsonActions.Kind.SetVoltage, 2.5);
            Parses("set the voltage to minus 2", WattsonActions.Kind.SetVoltage, -2);
            Parses("increase the voltage by one volt", WattsonActions.Kind.ChangeVoltage, 1);
            Parses("increase the voltage", WattsonActions.Kind.ChangeVoltage, 1);
            Parses("add a volt", WattsonActions.Kind.ChangeVoltage, 1);
            Parses("lower the voltage by 2 volts", WattsonActions.Kind.ChangeVoltage, -2);
            Parses("increase the current voltage by one", WattsonActions.Kind.ChangeVoltage, 1);
            Parses("raise the voltage and watch the current", WattsonActions.Kind.ChangeVoltage, 1);
            Parses("show me what happens when the voltage doubles", WattsonActions.Kind.Double);
            Parses("double the voltage", WattsonActions.Kind.Double);
            Parses("2x the voltage", WattsonActions.Kind.Double);
            Parses("halve the voltage", WattsonActions.Kind.Halve);
            Parses("lower the resistance", WattsonActions.Kind.Resistance);
            Parses("make the resistance 10 ohms", WattsonActions.Kind.Resistance);
            Parses("reset the experiment", WattsonActions.Kind.Reset);
            Parses("start over", WattsonActions.Kind.Reset);
            Parses("make the ammeter read 0.6 A", WattsonActions.Kind.Current, 0.6);
            Parses("set the current to 0.4 amps", WattsonActions.Kind.Current, 0.4);
            Parses("set the voltage so the current is 0.8 amperes", WattsonActions.Kind.Current, 0.8);
            Parses("double the current", WattsonActions.Kind.Current);
            Parses("double it", WattsonActions.Kind.Unclear);
            Parses("set it to 4", WattsonActions.Kind.Unclear);
            Parses("turn it up", WattsonActions.Kind.Unclear);
            Parses("set the voltage", WattsonActions.Kind.Unclear);
            // Questions and talk are for the AI, never actions.
            Parses("What is voltage?", WattsonActions.Kind.None);
            Parses("What is an Ohm-niscope?", WattsonActions.Kind.None);
            Parses("What happens if I set the voltage to 4 volts?", WattsonActions.Kind.None);
            Parses("what happens when the voltage doubles?", WattsonActions.Kind.None);
            Parses("Why does the current change when I change the voltage?", WattsonActions.Kind.None);
            Parses("can you explain what happens if you increase the voltage", WattsonActions.Kind.None);
            Parses("explain it more simply", WattsonActions.Kind.None);
            Parses("Tell me a joke about cricket", WattsonActions.Kind.None);
            Parses("what is the current?", WattsonActions.Kind.None);
            Parses("show me ohms law", WattsonActions.Kind.None);
            Parses("make it brighter", WattsonActions.Kind.None);
            Parses("", WattsonActions.Kind.None);

            // Validation and execution against a fake bench: replies quote the applied value and the live ammeter.
            void Does(string name, FakeBench bench, string text, double expectVolts, int expectSets, params string[] replyHas)
            {
                bool handled = WattsonActions.TryHandle(text, bench, out string reply);
                bool ok = handled && Math.Abs(bench.volts - expectVolts) < 1e-6 && bench.setCalls == expectSets;
                foreach (var part in replyHas)
                    ok &= part.StartsWith("!") ? reply != null && !reply.Contains(part.Substring(1)) : reply != null && reply.Contains(part);
                failed += Check(log, $"{name}: \"{text}\"", ok, $"handled={handled} volts={bench.volts} sets={bench.setCalls} reply=\"{reply}\"");
            }
            Does("set applies and confirms", new FakeBench { volts = 1 }, "set the voltage to four volts", 4, 1, "Done: 4 V.", "0.80 A");
            Does("step up from the real value", new FakeBench { volts = 2 }, "increase the voltage by one volt", 3, 1, "up to 3 V", "0.60 A");
            Does("step down", new FakeBench { volts = 3 }, "lower the voltage", 2, 1, "down to 2 V", "0.40 A");
            Does("double within range", new FakeBench { volts = 2 }, "show me what happens when the voltage doubles", 4, 1, "doubled from 2 V to 4 V", "0.40 A to 0.80 A");
            Does("double beyond range explains, applies nothing", new FakeBench { volts = 3 }, "show me what happens when the voltage doubles", 3, 0, "stops at 5 V");
            Does("double from zero", new FakeBench { volts = 0 }, "double the voltage", 0, 0, "double zero");
            Does("halve to a whole volt", new FakeBench { volts = 4 }, "halve the voltage", 2, 1, "halved from 4 V to 2 V");
            Does("halve to a half volt is refused", new FakeBench { volts = 3 }, "halve the voltage", 3, 0, "whole volts");
            Does("resistance declined", new FakeBench { volts = 2 }, "lower the resistance", 2, 0, "same nichrome wire", "resistance stays the same", "change the voltage instead", "!ohm", "!5");
            // Spoiler rule: Scene 12 asks for V / I (S12_Ratio) before S12_L14 names the ohm, so no value or unit until then.
            Does("resistance in Scene 12 before ohms are taught", new FakeBench { volts = 2, scene = 12, gate = "S12_Ratio", controlsOpen = false }, "lower the resistance", 2, 0, "resistance stays the same", "change the voltage instead", "!ohm", "!5", "!Ω");
            Does("resistance value once ohms are taught", new FakeBench { volts = 2, scene = 12, resistanceTaught = true }, "lower the resistance", 2, 0, "stays at 5 ohms");
            Does("reset", new FakeBench { volts = 4, start = 0.5 }, "reset the experiment", 0.5, 1, "starting 0.5 V");
            Does("out of range", new FakeBench { volts = 2 }, "set the voltage to 7 volts", 2, 0, "0 to 5 volts", "!Done");
            Does("negative", new FakeBench { volts = 2 }, "set the voltage to minus 2", 2, 0, "0 to 5 volts");
            Does("not a whole volt", new FakeBench { volts = 2 }, "set the voltage to 2.5 volts", 2, 0, "whole volts", "2 V or 3 V");
            Does("top of the range", new FakeBench { volts = 5 }, "increase the voltage", 5, 0, "already at the top");
            Does("bottom of the range", new FakeBench { volts = 0 }, "decrease the voltage", 0, 0, "already at 0 V");
            Does("past the top", new FakeBench { volts = 4 }, "increase the voltage by 2 volts", 4, 0, "stops at 5 V");
            Does("snaps from the 0.5 V start", new FakeBench { volts = 0.5 }, "increase the voltage by 1 volt", 1, 1, "Done: up to 1 V.", "whole volts");
            Does("same value", new FakeBench { volts = 3 }, "set the voltage to 3 volts", 3, 1, "already at 3 V", "0.60 A");
            Does("ambiguous asks back", new FakeBench { volts = 2 }, "set it to 4", 2, 0, "Do you mean the voltage?");
            Does("controls closed", new FakeBench { volts = 2, controlsOpen = false }, "set the voltage to 3 volts", 2, 0, "isn't open");
            Does("no circuit in the scene", new FakeBench { present = false, volts = 0, scene = 5 }, "set the voltage to 3 volts", 0, 0, "no circuit");
            Does("key open: no current claimed", new FakeBench { volts = 1, keyClosed = false }, "set the voltage to 2 volts", 2, 1, "Done: 2 V.", "key is open", "!0.40");
            Does("ammeter not in: no reading claimed", new FakeBench { volts = 1, ammeterIn = false }, "set the voltage to 2 volts", 2, 1, "Done: 2 V.", "!ammeter");
            // Spoiler rules: no "current follows / doubles" sentence before Scene 11 (the Scene 6 and 10 questions).
            Does("no relationship hint in Scene 10", new FakeBench { volts = 2, scene = 10 }, "double the voltage", 4, 1, "!double the current", "!follows");
            Does("relationship sentence from Scene 11", new FakeBench { volts = 2, scene = 11 }, "double the voltage", 4, 1, "Double the voltage, double the current.");
            Does("current request in Scene 10 stays spoiler-free", new FakeBench { volts = 1, scene = 10 }, "make the ammeter read 0.4 A", 1, 0, "can't set the current", "!2 V", "!0.20");
            Does("current request in Scene 11 suggests the voltage", new FakeBench { volts = 1, scene = 11 }, "make the ammeter read 0.6 A", 1, 0, "3 V gives 0.60 A");
            Does("current target activity gets a hint, not the answer", new FakeBench { volts = 1, scene = 11, gate = "S11_Target" }, "make the ammeter read 0.6 A", 1, 0, "0.20 A", "Which voltage", "!3 V");
            Does("a running target (rig.targetAmps) also gets the hint", new FakeBench { volts = 1, scene = 11, targetAmps = 0.6 }, "make the current go up", 1, 0, "Which voltage gives 0.60 A?", "!3 V");
            Does("the learner may still set the voltage during a target", new FakeBench { volts = 1, scene = 11, targetAmps = 0.6 }, "set the voltage to 3 volts", 3, 1, "Done: 3 V.", "0.60 A");
            Does("Scene 13 target hint uses V = IR",new FakeBench { volts = 1, scene = 13, gate = "S13_Apply", resistanceTaught = true }, "set the current to 0.8 amps", 1, 0, "V = IR", "R = 5 ohms", "!4 V");
            Does("a target hint before ohms are taught quotes no R", new FakeBench { volts = 1, scene = 13, gate = "S13_Apply", targetAmps = 0.8 }, "set the current to 0.8 amps", 1, 0, "Which voltage gives 0.80 A?", "!ohm", "!R =", "!4 V");

            // The cap (SceneDirector.ControlsMax, 1 V in S10_Set1V / S10_Record): nothing above it is applied, so no 0.40 A
            // reading leaks before S10_Predict2V, and the reply names the cap instead of a setting the director would undo.
            FakeBench Capped(double volts = 1) => new FakeBench { volts = volts, scene = 10, gate = "S10_Set1V", controlsMax = 1 };
            Does("cap: a setting above it is refused", Capped(), "set the voltage to 2 volts", 1, 0, "knob stops at 1 V", "after your prediction", "!Done", "!0.40");
            Does("cap: doubling past it is refused", Capped(), "double the voltage", 1, 0, "knob stops at 1 V", "!Done", "!0.40", "!2 V");
            Does("cap: a step past it is refused", Capped(), "increase the voltage by 1 volt", 1, 0, "knob stops at 1 V", "!Done", "!0.40");
            Does("cap: maximum is refused", Capped(), "maximum voltage", 1, 0, "knob stops at 1 V", "!Done", "!1.00");
            Does("cap: 'double it' asks back with an example at the cap", Capped(), "double it", 1, 0, "Do you mean the voltage?", "set the voltage to 1 volt\"", "!3 volts");
            Does("cap: a setting at the cap applies", Capped(0.5), "set the voltage to 1 volt", 1, 1, "Done: 1 V.", "0.20 A");
            Does("cap: a setting below the cap applies", Capped(), "set the voltage to 0 volts", 0, 1, "Done: 0 V.");
            string cappedGuide = WattsonActions.Guidance(Capped());
            failed += Check(log, "offline guidance at the cap suggests no higher setting", cappedGuide.Contains("set the voltage to 1 volt\"") && !cappedGuide.Contains("3 volts"), cappedGuide);

            // The AI's live state: resistance only once taught; the app's rules for the activity in progress.
            void State(string name, string text, params string[] has)
            {
                bool ok = text != null;
                foreach (var part in has) ok &= part.StartsWith("!") ? !text.Contains(part.Substring(1)) : text.Contains(part);
                failed += Check(log, name, ok, $"\"{text}\"");
            }
            State("state: resistance before Scene 12 is a rule with no value", WattsonActions.ResistanceState(new FakeBench { scene = 10 }), "[App rule:", "not been taught", "!R =", "!5");
            State("state: Scene 12 before S12_L14 gives no value or unit", WattsonActions.ResistanceState(new FakeBench { scene = 12 }), "[App rule:", "do not state its value or its unit", "!R =", "!5", "!ohms");
            State("state: R = 5 ohms once taught", WattsonActions.ResistanceState(new FakeBench { scene = 12, resistanceTaught = true }), "R = 5 ohms", "![App rule");
            State("state rule: a prediction favours no option", WattsonActions.ActivityRule(new FakeBench { scene = 6, gate = "S06_Prediction" }, true), "[App rule:", "do not favour any option");
            State("state rule: the 2 V prediction favours no option", WattsonActions.ActivityRule(new FakeBench { scene = 10, gate = "S10_Predict2V" }, true), "do not favour any option");
            State("state rule: a graded question is hint-only", WattsonActions.ActivityRule(new FakeBench { scene = 12, gate = "S12_Ratio" }, true), "[App rule:", "never give the option", "one hint about the reasoning", "ask what they notice");
            State("state rule: a target never gets its voltage", WattsonActions.ActivityRule(new FakeBench { scene = 11, gate = "S11_Target", targetAmps = 0.6 }, false), "[App rule:", "never say the voltage", "one hint about the reasoning");
            failed += Check(log, "state rule: none without an activity", WattsonActions.ActivityRule(new FakeBench { scene = 10 }, false) == "", "");

            // Scene 10 before row 2 is recorded: the learner has measured only 1 V, so the AI gets a rule, and Scene 10's
            // covered facts that quote 2 V → 0.4 A or the V-I direction stay out (S10_Predict2V and S10_Pattern ask that).
            string[] scene10Facts =
            {
                "Everything from Scenes 5 to 9: voltage (V, volt) as the push, current (I, ampere), the circuit with the ammeter in series and the voltmeter in parallel across the nichrome wire XY.",
                "The five readings: 1 V → 0.2 A, 2 V → 0.4 A, 3 V → 0.6 A, 4 V → 0.8 A, 5 V → 1.0 A.",
                "The current increases when the potential difference increases.",
                "Doubling the potential difference doubles the current (1 V → 0.2 A and 2 V → 0.4 A).",
            };
            var oneVolt = new FakeBench { scene = 10, tableRows = 1 };
            var twoRows = new FakeBench { scene = 10, tableRows = 2 };
            State("state rule: only 1 V measured in Scene 10 before row 2", WattsonActions.OneVoltRule(oneVolt), "[App rule:", "measured only 1 V", "never say what the ammeter reads at another voltage", "how the current changes with the voltage");
            failed += Check(log, "state rule: none once row 2 is recorded, none outside Scene 10", WattsonActions.OneVoltRule(twoRows) == "" && WattsonActions.OneVoltRule(new FakeBench { scene = 11 }) == "", "");
            failed += Check(log, "covered facts before row 2: the Scene 5-9 recap stays, readings above 1 V and the direction go",
                WattsonActions.FactAllowed(oneVolt, scene10Facts[0]) && !WattsonActions.FactAllowed(oneVolt, scene10Facts[1]) && !WattsonActions.FactAllowed(oneVolt, scene10Facts[2]) && !WattsonActions.FactAllowed(oneVolt, scene10Facts[3]), "");
            failed += Check(log, "covered facts from row 2: all four", Array.TrueForAll(scene10Facts, fact => WattsonActions.FactAllowed(twoRows, fact)), "");
            failed += Check(log, "covered facts: Scene 11 is not filtered", Array.TrueForAll(scene10Facts, fact => WattsonActions.FactAllowed(new FakeBench { scene = 11 }, fact)), "");
            failed += Check(log, "a reading above 1 V or 0.2 A is found; 1 V and 0.20 A are not",
                WattsonActions.RevealsRelationship("2.0 volts gives 0.40 amperes") && WattsonActions.RevealsRelationship("the meter showed 1.0 A") && !WattsonActions.RevealsRelationship("1 V gave 0.20 A, and 0.2 A again"), "");

            // Saved FAQ answers and suggested questions: none during an activity, none naming the ohm before it is taught.
            void Saved(string name, FakeBench bench, bool activity, string question, string answer, bool expect)
            {
                bool allowed = WattsonActions.SavedAnswerAllowed(bench, activity, question, answer);
                failed += Check(log, name, allowed == expect, $"allowed={allowed}");
            }
            Saved("saved answer: none during an activity", new FakeBench { scene = 5, gate = "S05_Drift" }, true, "Which way do electrons move?", "From the negative end toward the positive end.", false);
            Saved("saved answer: allowed outside an activity", new FakeBench { scene = 12 }, false, "Why is V divided by I always 5?", "Because this nichrome wire always opposes current by the same amount.", true);
            Saved("saved answer: no ohm before it is taught", new FakeBench { scene = 12 }, false, "What is one ohm?", "Its resistance is five ohms.", false);
            Saved("saved answer: no Ω before it is taught", new FakeBench { scene = 12 }, false, "What is R?", "R = 5 Ω.", false);
            Saved("saved answer: the ohm once taught", new FakeBench { scene = 12, resistanceTaught = true }, false, "What is one ohm?", "Its resistance is five ohms.", true);
            Saved("saved answer: Georg Ohm is not the unit", new FakeBench { scene = 6 }, false, "How did Ohm test things?", "Ohm did exactly what we are about to do. Ohm's law comes later.", true);
            // Scene 10's chips: "Why does the current change..." only once row 2 is recorded; the other two from the start.
            const string WhyChange = "Why does the current change when I change the voltage?";
            const string WhyChangeAnswer = "The potential difference is the push on the charges in the wire. A bigger push makes the free electrons drift along faster, so more charge crosses each section of the wire every second, and that is a bigger current. Look at your table to see exactly how much bigger.";
            Saved("saved answer: no 'why does the current change' before row 2", oneVolt, false, WhyChange, WhyChangeAnswer, false);
            Saved("saved answer: 'why does the current change' from row 2", twoRows, false, WhyChange, WhyChangeAnswer, true);
            Saved("saved answer: 'why start at 1 volt' from the start", new FakeBench { scene = 10, tableRows = 0 }, false, "Why do we start at 1 volt?", "Starting low is good detective practice. A small push gives a small, safe current, we can check that everything is connected properly, and then we raise the voltage one step at a time so we can see clearly what each step does to the current.", true);
            Saved("saved answer: 'record a reading' from the start", new FakeBench { scene = 10, tableRows = 0 }, false, "What does 'record a reading' mean?", "It means writing down what the instrument shows at that moment: the voltmeter's volts and the ammeter's amperes, side by side in the observation table. One row per setting, so afterwards we can compare them and hunt for a pattern.", true);
            failed += Check(log, "the ohm unit is found, Georg Ohm and Ohm's law are not",
                WattsonActions.NamesOhm("Resistance is measured in ohms (Ω).") && WattsonActions.NamesOhm("R = 5 Ω") && WattsonActions.NamesOhm("What is one ohm?")
                && !WattsonActions.NamesOhm("Georg Simon Ohm and Ohm's law") && !WattsonActions.NamesOhm(null), "");

            // The example command never names a running target's answer (S11_Target: 0.60 A is 3 V).
            Does("ambiguous order during a target suggests another voltage", new FakeBench { volts = 1, scene = 11, gate = "S11_Target", targetAmps = 0.6 }, "set it to 4", 1, 0, "Do you mean the voltage?", "set the voltage to 2 volts", "!3 volts");
            Does("ambiguous order outside a target keeps the usual example", new FakeBench { volts = 1, scene = 10, gate = "S10_Set1V" }, "set it to 4", 1, 0, "set the voltage to 3 volts");
            string targetGuide = WattsonActions.Guidance(new FakeBench { volts = 1, scene = 11, gate = "S11_Target", targetAmps = 0.6 });
            failed += Check(log, "offline guidance during a target never suggests its voltage", targetGuide.Contains("set the voltage to 2 volts") && !targetGuide.Contains("3 volts"), targetGuide);

            // A question never moves the knob.
            var quiet = new FakeBench { volts = 2 };
            bool handledQuestion = WattsonActions.TryHandle("What happens if I set the voltage to 4 volts?", quiet, out _);
            failed += Check(log, "a hypothetical question goes to the AI and changes nothing", !handledQuestion && quiet.setCalls == 0, $"handled={handledQuestion} sets={quiet.setCalls}");

            // Offline guidance quotes the real readings.
            string guide = WattsonActions.Guidance(new FakeBench { volts = 2 });
            failed += Check(log, "offline guidance quotes the live readings", guide.Contains("2 V") && guide.Contains("0.40 A") && guide.Contains("slider"), guide);
            string guideKey = WattsonActions.Guidance(new FakeBench { volts = 2, keyClosed = false, controlsOpen = false });
            failed += Check(log, "offline guidance with the key open claims no current", guideKey.Contains("key is open") && !guideKey.Contains("slider"), guideKey);

            // Voice text: units in words.
            string spoken = WattsonActions.Spoken("Done: 1 V. The ammeter now reads 0.20 A. Then 0.5 V and 4 V. Use V = IR.");
            failed += Check(log, "spoken units", spoken == "Done: 1 volt. The ammeter now reads 0.20 amperes. Then 0.5 volts and 4 volts. Use V equals I times R.", spoken);
            failed += Check(log, "scene number from ids", WattsonActions.SceneNumber("Scene10_Experiment") == 10 && WattsonActions.SceneNumber("Scene04") == 4 && WattsonActions.SceneNumber("") == 0, "");

            log(failed == 0 ? "Wattson Actions (logic): all checks passed" : $"Wattson Actions (logic): {failed} failed");
            return failed;
        }
    }
}
