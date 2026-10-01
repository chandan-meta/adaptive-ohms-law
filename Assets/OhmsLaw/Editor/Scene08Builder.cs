using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 8: From Ohm's Lab to Our Lab. The old apparatus becomes today's instruments, one item at a time.
    public class Scene08Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene08";
        public override string SceneName => "Scene08_OurLab";
        public override string Title => "From Ohm's Lab to Our Lab";

        protected override void Dialogue()
        {
            L("S08_L01", "Ohm didn't have the modern instruments sitting in your school laboratory today.", 4.5f);
            L("S08_L02", "So let's investigate his relationship using the instruments you would use today.", 4.5f);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 7: voltage as the push (V, volt), current as charge per second (I, ampere), Ohm's repeated 1826 investigation and his apparatus.",
                "An ammeter measures current (I, ampere). A voltmeter measures potential difference (V, volt).",
                "The nichrome wire XY is the test conductor; X and Y are its two ends.",
                "A variable DC source (cells) sets the potential difference; a key (switch) opens and closes the circuit.",
            },
            new[] { "How the ammeter and voltmeter are connected (series and parallel, Scene 9)", "The pattern between V and I", "V is proportional to I", "V = IR", "Resistance", "Ohms", "The Christmas-lights answer" },
            ("What is nichrome?", "Nichrome is an alloy, a metal mixture of mostly nickel and chromium. It is tough, it does not rust, and it behaves very steadily in experiments like ours, which is exactly why school labs use a nichrome wire as the test conductor."),
            ("What does a voltmeter measure?", "A voltmeter measures the potential difference, the voltage, between two points. In our circuit it will tell us the push across the two ends of the nichrome wire, X and Y, in volts."),
            ("Why not use Ohm's old instruments?", "We could, but Ohm's torsion galvanometer needed a steady hand and a lot of patience, and his thermoelectric source gave only a tiny push. Today's ammeter and voltmeter give clear numbers straight away, so we can see the relationship much more clearly."));

        protected override void Stage()
        {
            LabGreybox(modernBench: false);
            OhmTable();
            KidsAt(new Vector3(0.2f, 0, -2.2f), Vector3.forward);
            Wattson(WattsonHome, BenchWideCam);
            director.matchCutFrom = "Scene07_BackToOhm";
            var rig = Rig(assembled: false, keyClosed: false, voltage: 0, active: false);
            director.rig = rig;
            // The modern parts appear one at a time; hidden until their cue.
            foreach (var part in new[] { "AMMETER", "VOLTMETER", "NICHROME WIRE — XY", "Card X", "Card Y", "cells / controllable DC source", "switch / key" }) refs[part].SetActive(false);
            Vfx("freeze (Ohm holds still)", null, new Vector3(0, 2.2f, 1.0f), 1.2f);
            Vfx("prop transforms old → new", null, new Vector3(0, BenchTop + 0.8f, -0.1f), 1.4f);
        }

        protected override void Beats()
        {
            Shot("Bench Wide");
            Cue("ohm.freeze", "Wattson freezes the historical experiment; Ohm holds still");
            Show("[VFX] freeze (Ohm holds still)");
            Gesture(WattsonController.Gesture.RaiseGadget, point: "Georg Ohm", gadget: true);
            Visual("Wattson freezes Ohm's experiment", 1.0);
            Hide("[VFX] freeze (Ohm holds still)");
            Gesture(WattsonController.Gesture.Talking);
            Line("S08_L01");

            Shot("Bench Close");
            Gesture(WattsonController.Gesture.Point, point: "Bench Point", gadget: true);
            Show("Circuit Rig");
            Show("[VFX] prop transforms old → new");
            // One item every 0.8 s: old prop scales down, new labelled prop scales up.
            Hide("torsion galvanometer"); Show("AMMETER");
            Visual("galvanometer → AMMETER (in the parts tray) with its label", 0.8);
            Show("VOLTMETER");
            Visual("VOLTMETER appears with its label", 0.8);
            Hide("Copper Test Wires"); Show("NICHROME WIRE — XY"); Show("Card X"); Show("Card Y");
            Visual("copper wires fade; one straight NICHROME WIRE — XY appears, X and Y light up", 0.8);
            Hide("Thermoelectric Source"); Show("cells / controllable DC source");
            Visual("cells / controllable DC source appears", 0.8);
            Hide("Mercury Cups"); Show("switch / key");
            Visual("switch / key appears", 0.8);
            Hide("Old Table"); Show("Bench"); Hide("Ohm Apparatus");
            Visual("wooden table → clean modern bench; the lab around stays historical", 0.8);
            Hide("[VFX] prop transforms old → new");

            Gesture(WattsonController.Gesture.Talking);
            Line("S08_L02", 0.2);
            Cue("handoff", "SceneHandoffState: camera pose + CircuitState");
            Handoff("Scene08Complete", cut: true, afterSeconds: 0.3);
        }
    }
}
