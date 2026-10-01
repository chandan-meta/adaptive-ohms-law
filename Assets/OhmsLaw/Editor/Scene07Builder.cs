using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 7: Back to Georg Ohm. His 1826 investigation, again and again, looking for a pattern.
    public class Scene07Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene07";
        public override string SceneName => "Scene07_BackToOhm";
        public override string Title => "Back to Georg Ohm";

        protected override void Dialogue()
        {
            L("S07_L01", "Now you know the two clues.", 2.2f);
            L("S07_L02", "But Ohm wanted something more than definitions.", 3.0f);
            L("S07_L03", "He wanted to know...", 1.8f);
            L("S07_L04", "Is there a mathematical relationship between voltage and current?", 4.2f);
            L("S07_L05", "In 1826, Ohm kept changing one electrical condition, the potential difference and observing what happened to the current.", 7.5f);
            L("S07_L06", "Again. And again. And again.", 2.8f);
            L("S07_L07", "He wasn't looking for one reading. He was looking for a pattern.", 4.5f);
            L("S07_L08", "Today, we can investigate the same relationship much more clearly.", 4.2f);
            L("S07_L09", "So let's run the investigation ourselves.", 2.8f);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 and 6: free electrons, voltage (V, volt) as the push, current (I, ampere) as charge per second, the learner's prediction.",
                "In 1826 Georg Ohm investigated by changing the potential difference and observing the current, many times, looking for a pattern rather than one reading.",
                "His apparatus: a copper-bismuth thermoelectric source (hot and cold junctions), a torsion galvanometer, copper test wires of different lengths, mercury cups as connection points, a notebook of measurements.",
            },
            new[] { "The pattern Ohm found", "V is proportional to I", "V = IR", "R = V / I", "Resistance", "Ohms", "The name 'Ohm's law'", "The Christmas-lights answer" },
            ("Why did Ohm repeat it so many times?", "One reading can fool you: a wobble in the needle, a warm wire, a loose contact. Ohm repeated every measurement so that only the real pattern was left when the accidents averaged out. That is what careful detectives do with evidence."),
            ("What is a galvanometer?", "A galvanometer detects electric current. Ohm's torsion galvanometer had a magnetic needle hanging on a thread; current in a nearby wire twisted the needle, and how far he had to twist the thread back told him how strong the current was. Our ammeter does that job today."),
            ("Why did Ohm use a thermoelectric source instead of a battery?", "The batteries of 1826 faded as they worked, so the push kept changing during a measurement. A copper-bismuth junction, with one end in steam and the other in ice, gave a small but steady potential difference. Steady conditions, reliable readings."));

        protected override void Stage()
        {
            LabGreybox(modernBench: false);
            OhmTable();
            KidsAt(new Vector3(0.2f, 0, -2.2f), Vector3.forward);
            Wattson(WattsonHome, BenchWideCam);
            Card("Voltage", "Voltage → V", palette.voltage, null, WattsonHome + new Vector3(-0.9f, 0.4f, 0.2f), 0.14f);
            Card("Current", "Current → I", palette.current, null, WattsonHome + new Vector3(0.9f, 0.4f, 0.2f), 0.14f);
            Card("Question", "Is there a mathematical relationship\nbetween voltage and current?", Color.white, null, new Vector3(0, 2.3f, 0.6f), 0.18f, false, 5f);
            Vfx("galvanometer needle swing", null, new Vector3(0.55f, BenchTop + 0.5f, -0.1f), 0.9f);
            Vfx("historical apparatus starts transforming → modern circuit", null, new Vector3(0, BenchTop + 0.75f, -0.4f), 1.6f);
            Cam("Table Close", new Vector3(0.4f, 1.5f, -1.3f), new Vector3(0.3f, BenchTop + 0.1f, 0), 40);
        }

        protected override void Beats()
        {
            Shot("Bench Wide");
            Gesture(WattsonController.Gesture.Idle);
            Visual("Fade in from white. Ohm works at his table (loop). Wattson floats beside the group.", 1.2);
            Show("Card Voltage");
            Show("Card Current");
            Gesture(WattsonController.Gesture.Flick);
            Line("S07_L01");
            Gesture(WattsonController.Gesture.Point, point: "Georg Ohm");
            Line("S07_L02");
            Line("S07_L03");
            Show("Card Question");
            Gesture(WattsonController.Gesture.Talking);
            Line("S07_L04");

            Shot("Table Close");
            Gesture(WattsonController.Gesture.Point, look: "notebook", point: "torsion galvanometer");
            Show("[VFX] galvanometer needle swing");
            Visual("Ohm's experiment loop: picks a short copper wire, places it, changes the source, watches the galvanometer, writes a reading", 0.5);
            Line("S07_L05");
            Show("Page 1");
            Line("S07_L06", 0.1);
            Show("Page 2", -1.8);
            Show("Page 3", -0.8);
            Show("Pages Fan");
            Line("S07_L07");
            Hide("[VFX] galvanometer needle swing");

            Shot("Bench Wide");
            Gesture(WattsonController.Gesture.Talking);
            Line("S07_L08");
            Line("S07_L09", 0.2);
            Hide("Pages Fan");
            Show("[VFX] historical apparatus starts transforming → modern circuit");
            Visual("First flicker of the apparatus transforming (the transformation itself is Scene 8)", 1.2);
            Handoff("Scene07Complete", cut: true, afterSeconds: 0.1);
        }
    }
}
