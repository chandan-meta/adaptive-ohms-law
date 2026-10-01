using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 6: Adaptive Question. The prediction is stored; the carriers are frozen so nothing gives the answer away.
    public class Scene06Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene06";
        public override string SceneName => "Scene06_AdaptiveQuestion";
        public override string Title => "Adaptive Question";

        protected override void Dialogue()
        {
            L("S06_L01", "Detective test.", 1.5f);
            L("S06_L02", "If we increase the potential difference while everything else stays the same, what do you predict will happen to the current?", 6.5f);
            L("S06_L03", "Interesting. Remember your answer. Let's see if Ohm's experiment agrees with you.", 5.0f);
            L("S06_R_A", "A bold prediction, detective. Hold on to it.", 2.8f, proposed: true);
            L("S06_R_B", "A surprising theory. Every good detective tests a theory.", 3.4f, proposed: true);
            L("S06_R_C", "A careful guess. Let's put it to the test.", 2.8f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 4 and 5: free electrons, random motion before the battery, potential difference (voltage, V, volt) as the push, the field along the whole circuit, current (I, ampere) as charge per second.",
                "Wattson has asked the learner to predict what happens to the current when the potential difference increases, with everything else the same.",
                "If asked for the right answer: a good detective tests it, and the experiment is next.",
            },
            new[] { "The answer to the prediction (what happens to current when voltage increases)", "V is proportional to I", "V = IR", "R = V / I", "Resistance", "Ohms", "The name 'Ohm's law'", "The Christmas-lights answer" },
            ("What does 'everything else stays the same' mean?", "It means we change only one thing, the potential difference, and keep the rest fixed: the same wire, the same material, the same length and thickness, and the same temperature. Then whatever happens to the current, we know what caused it."),
            ("What is potential difference again?", "Potential difference, or voltage, is the electrical push between two points in a circuit. The battery sets it up, and it is what makes the charges drift along the wire together. We measure it in volts."),
            ("How did Ohm test things?", "Ohm did exactly what we are about to do: he changed the electrical conditions in his circuit, step by step, and measured what happened each time. He repeated it over and over and wrote every reading down. Let's go and see."));

        protected override void Stage()
        {
            var wire = WireStage(CopperTint, lattice: false);
            wire.fieldOn = true;
            director.wire = wire;
            director.matchCutFrom = "Scene05_InsideTheWire";
            Wattson(PlatformPos + new Vector3(0.6f, 1.1f, 0.4f), WireCamPos);
            Vector3 o = WireOrigin;
            Face(Card("Voltage", "VOLTAGE\nSymbol: V\nUnit: volt (V)", palette.voltage, null, o + new Vector3(-1.2f, 1.3f, 1.8f), 0.3f, true, 4f));
            Face(Card("Current", "CURRENT\nSymbol: I\nUnit: ampere (A)", palette.current, null, o + new Vector3(4.2f, 1.0f, 0.4f), 0.3f, true, 4f));
            var readout = Card("Battery Readout", "BATTERY ▲", palette.voltage, null, o + new Vector3(TunnelLength / 2 - 1f, 1.9f, 0), 0.35f);
            Face(readout);
            Placeholder(readout, "Battery readout / terminal brightness rising as Wattson turns the voltage up");
            Face(Vfx("time-freeze (Ohm-niscope holds the carriers)", null, o + new Vector3(0.5f, 1.7f, 0.8f), 2.4f));
            Face(Vfx("Ohm-niscope flash", null, o + new Vector3(2f, 1.2f, 2f), 1.6f));
        }

        protected override void Beats()
        {
            Shot("Wire Inside");
            Cue("wire.field 1", "match-cut from Scene 5: carriers still drifting − → +");
            Gesture(WattsonController.Gesture.Greet);
            Visual("Wattson turns toward the learner", 0.8);
            Line("S06_L01");

            Gesture(WattsonController.Gesture.RaiseGadget, point: "Card Terminal +", gadget: true);
            Show("Card Battery Readout");
            Cue("boost Card Terminal +", "Wattson raises the battery voltage: terminal readout and brightness rise");
            Show("[VFX] time-freeze (Ohm-niscope holds the carriers)");
            Cue("wire.freeze 1", "carriers frozen (jitter only) until the scene ends, so the drift never reveals the answer");
            Visual("Voltage rises while the Ohm-niscope freezes the carriers", 1.6);

            Gesture(WattsonController.Gesture.Talking);
            Line("S06_L02", 0.2);
            Gate("S06_Prediction", "MCQ: A. Increase · B. Decrease · C. Stay the same → LearnerSession.prediction; proposed reaction S06_R_A/B/C; no right/wrong feedback",
                "S06_R_A", "S06_R_B", "S06_R_C");
            Hide("[VFX] time-freeze (Ohm-niscope holds the carriers)");
            Line("S06_L03");

            Gesture(WattsonController.Gesture.Aim, look: "Wall Point", point: "Wall Point", gadget: true);
            Shot("Wire Wattson");
            Show("[VFX] Ohm-niscope flash");
            Visual("Wattson raises the Ohm-niscope toward the tunnel wall; flash", 1.4);
            Cue("fade 1", "fade to white");
            Handoff("Scene06Complete", cut: false, afterSeconds: 0.8);
        }
    }
}
