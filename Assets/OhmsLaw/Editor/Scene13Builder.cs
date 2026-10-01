using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 13: The Law Gets Its Name. V, I, R combine into V = IR; the adaptive checkpoint; hand-off to the video.
    public class Scene13Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene13";
        public override string SceneName => "Scene13_OhmsLaw";
        public override string Title => "The Law Gets Its Name";

        protected override void Dialogue()
        {
            L("S13_L01", "So now we know all three pieces.", 2.4f);
            L("S13_L02", "The push.", 1.2f);
            L("S13_L03", "The flow.", 1.2f);
            L("S13_L04", "And the opposition.", 1.6f);
            L("S13_L05", "And the relationship connecting them...", 2.6f);
            L("S13_L06", "And today... we call it OHM'S LAW.", 3.0f);
            L("S13_L07", "We've got our law. Now let's test it against the mystery that started all of this.", 4.5f);
            // Adaptive checkpoint. AQ1 and AQ2 are storyboard questions (AQ1's "vo ltage" typo fixed); the rest is proposed.
            L("AQ1", "If resistance stays constant and voltage doubles, what happens to current?", 4.5f);
            L("AQ2", "Two conductors carry the same current. One requires a larger potential difference. Which has greater resistance?", 6.5f);
            L("AQ_EXPLAIN", "Remember the table, detective: every time V went up, I went up with it, and V divided by I stayed at 5.", 6.5f, proposed: true);
            L("AQ_EASY", "When the potential difference across the nichrome wire went up, what happened to the current?", 5.0f, proposed: true);
            L("AQ_GOOD", "Case closed, detective.", 1.8f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 12: voltage V (the push, volt), current I (the flow, ampere), resistance R (the opposition, ohm), V ∝ I for the nichrome wire at constant temperature, V/I constant, slope = R, 1 Ω = 1 V / 1 A.",
                "Ohm's law: V = IR. The potential difference across a conductor equals the current through it times its resistance, at constant temperature.",
                "If asked about the Christmas lights, give only a hint: think about which battery could keep a bigger potential difference across the lights.",
            },
            new[] { "The full explanation of the Christmas-lights mystery (the Scene 14 video does that)" },
            ("What does V = IR mean?", "It says the potential difference across a conductor equals the current through it multiplied by its resistance. Our nichrome wire: 5 ohms times 0.2 ampere gives 1 volt, 5 ohms times 1 ampere gives 5 volts. Three clues, one relationship."),
            ("Can I use Ohm's law for any wire?", "For metal wires at a steady temperature, yes, and that covers most of what you meet in a school lab. Some things, like bulb filaments that heat up, or diodes, do not keep a constant resistance, so their V-I graph is not a straight line. Scientists call the well-behaved ones ohmic conductors."),
            ("Who was Ohm?", "Georg Simon Ohm was a German teacher and physicist, born in 1789. He built much of his own equipment, measured patiently for years, and published this relationship in 1827. The unit of resistance, the ohm, is named in his honour."));

        protected override void Stage()
        {
            LabGreybox(modernBench: true);
            KidsAt(new Vector3(0.2f, 0, -2.2f), Vector3.forward);
            Wattson(WattsonHome, BenchWideCam);
            director.matchCutFrom = "Scene12_Resistance";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            Card("V", "V — Potential Difference", palette.voltage, null, new Vector3(-1.4f, 2.1f, 0.3f), 0.2f, true);
            Card("I", "I — Current", palette.current, null, new Vector3(0, 2.1f, 0.3f), 0.2f, true);
            Card("R", "R — Resistance", palette.resistance, null, new Vector3(1.4f, 2.1f, 0.3f), 0.2f, true);
            Card("Law", "V = IR", Color.white, null, new Vector3(0, 2.1f, 0.3f), 0.5f);
            Card("Title", "OHM'S LAW", Color.white, null, new Vector3(0, 2.9f, 0.4f), 0.55f);
            Vfx("title reveal", null, new Vector3(0, 3.5f, 0.4f), 1.6f);
            Cam("Cards", new Vector3(0, 2.1f, -3.6f), new Vector3(0, 2.2f, 0.3f), 40);
        }

        protected override void Beats()
        {
            Shot("Bench Wide");
            Visual("Three cards float: V — Potential Difference (blue), I — Current (amber), R — Resistance (coral)", 0.8);
            Shot("Cards");
            Gesture(WattsonController.Gesture.Flick);
            Line("S13_L01");
            Gesture(WattsonController.Gesture.Point, point: "Card V");
            Cue("boost Card V", "V highlights");
            Line("S13_L02");
            Gesture(WattsonController.Gesture.Point, point: "Card I");
            Cue("boost Card I", "I highlights");
            Line("S13_L03");
            Gesture(WattsonController.Gesture.Point, point: "Card R");
            Cue("boost Card R", "R highlights");
            Line("S13_L04");
            Gesture(WattsonController.Gesture.Talking);
            Line("S13_L05", 0.2);
            Hide("Card V"); Hide("Card I"); Hide("Card R"); Show("Card Law");
            Visual("The three cards combine into V = IR", 1.2);
            Line("S13_L06", 0.1);
            Show("Card Title", -1.0);
            Show("[VFX] title reveal", -1.0);
            Visual("OHM'S LAW title card", 1.5);
            Hide("[VFX] title reveal");
            Gate("S13_Checkpoint", "Adaptive checkpoint (checkpointEnabled, placement note on SceneDirector): AQ1 → correct: AQ_GOOD, AQ2 (harder) → done; wrong: AQ_EXPLAIN, AQ_EASY → done. Results in LearnerSession.checkpoint.",
                "AQ1", "AQ2", "AQ_EXPLAIN", "AQ_EASY", "AQ_GOOD");
            Gesture(WattsonController.Gesture.Greet);
            Line("S13_L07", 0.3);
            Handoff("Scene13Complete", cut: false, afterSeconds: 0.3);
        }
    }
}
