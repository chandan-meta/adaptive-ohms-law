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
            public bool present = true, controlsOpen = true, keyClosed = true, ammeterIn = true;
            public double volts = 1, start = 0.5, targetAmps = -1;
            public int scene = 10, setCalls;
            public string gate;
            public double TargetAmps => targetAmps;

            public bool Present => present;
            public bool ControlsOpen => controlsOpen;
            public double Voltage => volts;
            public double Amps => keyClosed && ammeterIn ? AmpsAt(volts) : 0;
            public bool KeyClosed => keyClosed;
            public bool AmmeterIn => ammeterIn;
            public int Scene => scene;
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
            Does("resistance declined", new FakeBench { volts = 2 }, "lower the resistance", 2, 0, "same nichrome wire", "!ohms");
            Does("resistance declined after Scene 12", new FakeBench { volts = 2, scene = 12 }, "lower the resistance", 2, 0, "stays at 5 ohms");
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
            Does("Scene 13 target hint uses V = IR",new FakeBench { volts = 1, scene = 13, gate = "S13_Apply" }, "set the current to 0.8 amps", 1, 0, "V = IR", "!4 V");

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
