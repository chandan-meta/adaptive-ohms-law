using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 9: Build the Circuit. Ammeter in series, voltmeter in parallel, close the key.
    public class Scene09Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene09";
        public override string SceneName => "Scene09_BuildCircuit";
        public override string Title => "Build the Circuit";

        protected override void Dialogue()
        {
            L("S09_L01", "That's our test conductor.", 1.8f);
            L("S09_L02", "We're going to change the potential difference across this nichrome wire...measure the current through it...", 5.5f);
            L("S09_L03", "...and see whether your prediction was right.", 2.8f);
            L("S09_L04", "Everything we're measuring revolves around this wire.", 3.2f);
            L("S09_L05", "If we want to know how much current flows through the nichrome wire, we use an ammeter.", 5.0f);
            L("S09_L06", "Exactly. An ammeter is connected in series, so the same current passes through it and the test wire.", 5.5f);
            L("S09_L07", "And when we need the potential difference between the two ends of the wire... we use the voltmeter.", 5.0f);
            L("S09_L08", "That's why a voltmeter is connected in parallel across the nichrome wire.", 4.0f);
            L("S09_L09", "Click on the key to close the circuit.", 2.8f);
            L("S09_L10", "Investigation ready.", 1.6f);
            L("S09_L11", "Remember your prediction?", 1.8f);
            L("S09_L12", "Let's find out.", 1.6f);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 8: voltage (V, volt) as the push, current (I, ampere), Ohm's investigation, the modern instruments.",
                "The nichrome wire XY is the test conductor; we will change the potential difference across it and measure the current through it.",
                "The ammeter is connected in series so the same current passes through it and the test wire.",
                "The voltmeter is connected in parallel across the two ends of the wire, X and Y, to measure the potential difference between them.",
                "Closing the key completes the circuit so current can flow.",
            },
            new[] { "The readings", "The pattern between V and I", "V is proportional to I", "V = IR", "Resistance", "Ohms", "The Christmas-lights answer" },
            ("Why is the ammeter in series?", "Because we want the ammeter to measure the current through the nichrome wire. In series, the ammeter is part of the same single path, so every bit of charge that passes through the wire also passes through the ammeter. Same path, same current."),
            ("Why is the voltmeter in parallel?", "A voltmeter compares two points. We connect one lead to X and the other to Y, the two ends of the nichrome wire, so it reads the potential difference across the wire itself, while the current carries on through the wire as before."),
            ("What happens if the key is open?", "An open key is a gap in the path. The push from the source is still there, but charge cannot cross the gap, so no current flows and the ammeter reads zero. Close the key and the loop is complete."));

        protected override void Stage()
        {
            LabGreybox(modernBench: true);
            KidsAt(new Vector3(0.2f, 0, -2.2f), Vector3.forward);
            Wattson(WattsonHome, BenchWideCam);
            director.matchCutFrom = "Scene08_OurLab";
            director.rig = Rig(assembled: false, keyClosed: false, voltage: 0);
            var reminder = Card("Reminder", "If potential difference increases, what happens to current?", Color.white, null, new Vector3(0, 2.1f, 0.5f), 0.16f, false, 5f);
            var mine = WorldText.Card("Your Prediction", "Your prediction: ", palette.current, reminder.transform, new Vector3(0, -0.35f, 0), 0.12f, cardBacking);
            mine.gameObject.SetActive(false);
            Vfx("X and Y pulse", null, new Vector3(0, BenchTop + 0.55f, -0.25f), 1.0f);
        }

        protected override void Beats()
        {
            Shot("Bench Wide");
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY", moveTo: "Wire Point");
            Show("Wire Highlight");
            Visual("Wattson moves to the nichrome wire XY; it highlights", 1.0);
            Shot("Bench Close");
            Line("S09_L01");
            Line("S09_L02");
            Line("S09_L03");
            Line("S09_L04");
            Hide("Wire Highlight");

            Gesture(WattsonController.Gesture.Point, point: "AMMETER", moveTo: "Tray Point");
            Visual("Wattson moves to the ammeter in the tray", 0.8);
            Line("S09_L05", 0.2);
            Gate("S09_PlaceAmmeter", "Drag or tap the ammeter into the glowing series slot (snap; shake back if dropped elsewhere). buildMode=AutoPlace or autoAnswer flies it in. On snap the current path lights: Source → Ammeter → Nichrome Wire XY → Switch → Source.");
            Line("S09_L06");

            Gesture(WattsonController.Gesture.Point, point: "VOLTMETER", moveTo: "Voltmeter Point");
            Visual("Wattson moves to the voltmeter", 0.8);
            Line("S09_L07", 0.2);
            Show("[VFX] X and Y pulse");
            Gate("S09_PlaceVoltmeter", "Drag or tap the voltmeter into the glowing parallel slot across XY.");
            Hide("[VFX] X and Y pulse");
            Line("S09_L08");

            Gesture(WattsonController.Gesture.Point, point: "switch / key", moveTo: "Source Point");
            Cue("prompt Click on the key to close the circuit.", "on-screen prompt");
            Line("S09_L09", 0.2);
            Gate("S09_CloseKey", "Tap/click the key (tap-hand hint). CLICK sfx, CircuitState.keyClosed = true, small default voltage so the needles move. The nichrome wire stays visually normal.");
            Cue("prompt", "prompt cleared");
            Gesture(WattsonController.Gesture.Talking, moveTo: "Wattson Home");
            Line("S09_L10");
            Line("S09_L11", 0.2);
            Cue("reminder", "reminder card: If potential difference increases, what happens to current? (+ Your prediction when showLearnerPrediction)");
            Wait(1.0);
            Line("S09_L12", 0.2);
            Cue("handoff", "SceneHandoffState: CircuitState + camera");
            Handoff("Scene09Complete", cut: true, afterSeconds: 0.4);
        }
    }
}
