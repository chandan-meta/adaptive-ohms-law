using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 6: the prediction. Opens on Scene 5's last frame (the same electrons, trails and counter carry over the
    // match-cut); Wattson freezes the electrons with the Ohm-niscope so nothing on screen gives the answer away, turns
    // the voltage up, and asks. One tap in the panel; the experiment answers it later.
    public class Scene06Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene06";
        public override string SceneName => "Scene06_AdaptiveQuestion";
        public override string Title => "Your Prediction";

        protected override void Dialogue()
        {
            L("S06_L01", "Detective test.", 1.5f);
            L("S06_L02", "If we increase the voltage across the wire, with everything else the same, what happens to the current?", 5.6f);
            L("S06_L03", "Interesting. Remember your answer. Let's see if Ohm's experiment agrees with you.", 5.0f);
            // The reaction only says the answer back, without judging it: S06_L03 right after it asks the learner to
            // remember it and promises the test.
            L("S06_R_A", "More current, you say.", 1.8f, proposed: true);
            L("S06_R_B", "Less current, you say.", 1.8f, proposed: true);
            L("S06_R_C", "No change, you say.", 1.8f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 4 and 5: free electrons, random motion before the voltage, potential difference (voltage, V, volt) as the push, the drift − to +, current (I, ampere) as charge per second.",
                "Wattson has asked the learner to predict what happens to the current when the potential difference increases, with everything else the same.",
                "If asked for the right answer: a good detective tests it, and the experiment is next.",
            },
            new[] { "The answer to the prediction (what happens to current when voltage increases)", "V is proportional to I", "V = IR", "R = V / I", "Resistance", "Ohms", "The name 'Ohm's law'", "The Christmas-lights answer" },
            ("What does 'everything else stays the same' mean?", "It means we change only one thing, the potential difference, and keep the rest fixed: the same wire, the same material, the same length and thickness, and the same temperature. Then whatever happens to the current, we know what caused it."),
            ("What is potential difference again?", "Potential difference, or voltage, is the electrical push between two points in a circuit. The battery sets it up, and it is what makes the charges drift along the wire together. We measure it in volts."),
            ("How did Ohm test things?", "Ohm did exactly what we are about to do: he changed the electrical conditions in his circuit, step by step, and measured what happened each time. He repeated it over and over and wrote every reading down. Let's go and see."));

        protected override void Stage()
        {
            // Scene 5's last frame, already lit: field on, terminals and their labels up, the counter counting at the
            // cross-section. Scene 5 hides its bracket and drift arrow before the end, so neither is built here.
            var wire = WireStage(CopperTint, lattice: false, terminalsOn: true);
            wire.fieldOn = true;
            wire.counting = true;
            director.wire = wire;
            director.matchCutFrom = "Scene05_InsideTheWire";
            refs["Cross Section"].SetActive(true);
            Wattson(PlatformPoint, WireCamPos);
            Note("voltage", "Voltage (V)", "The electrical push between two points. Unit: volt (V).");
            Note("current", "Current (I)", "Charge flowing past a point each second. Unit: ampere (A).");
            BatteryReadout();
            NiscopeBeam();
        }

        protected override void Beats()
        {
            Shot("Wire Inside");
            Cue("wire.field 1", "match-cut from Scene 5: carriers still drifting − → +");
            Cue("counter 1"); Cue("wire.count 1"); Cue("wire.ticks 1");
            Ambience("WireHum");
            Music("PadWire");
            Gesture(WattsonController.Gesture.Greet);
            Visual("Wattson turns toward the learner", 0.6);
            Shot("Wire Wattson"); // close on him for the greeting and the freeze: the magic the scene hangs on (in the wide it was a 130 px figure and the burst went unseen)
            Line("S06_L01");

            // First the freeze, and the counter goes with it (a held number beside a rising gauge would read as an answer);
            // then the voltage rise on the + terminal and its gauge. The drift never changes on screen.
            Gesture(WattsonController.Gesture.RaiseGadget, point: "Card Terminal +", gadget: true);
            Cue("wire.freeze 1", "electrons frozen (a little random motion only) until the scene ends, so the drift never reveals the answer");
            Cue("wire.ticks 0"); Cue("wire.count 0"); Cue("counter 0");
            Show("[VFX] time-freeze (Ohm-niscope holds the carriers)");
            Sfx("Highlight");
            Visual("The Ohm-niscope freezes the electrons in the two-shot: the cold pulse from his gadget, frost round the conduit, the charges turn ice-blue and stop drifting; the counter is put away", 1.2);
            Shot("Wire Inside"); // back to the wide for the voltage rise and the question; the gauge pops in as the 1 s blend lands
            Show("Battery Readout", 0.6);
            Cue("boost Card Terminal +", "Wattson raises the battery voltage: the + terminal brightens as its V gauge rises", 0.6);
            Sfx("VoltStep", 0.6);
            Visual("Voltage rises on the + terminal's gauge while the electrons stay frozen", 1.4);

            Gesture(WattsonController.Gesture.Point, point: "Card Terminal +");
            Line("S06_L02", 0.2);
            Gesture(WattsonController.Gesture.Listening);
            Gate("S06_Prediction", "One tap in the panel: A. Increase · B. Decrease · C. Stay the same → LearnerSession.prediction; a reaction line per option; no right/wrong feedback",
                "S06_R_A", "S06_R_B", "S06_R_C");
            Hide("[VFX] time-freeze (Ohm-niscope holds the carriers)");
            Gesture(WattsonController.Gesture.Thinking);
            Line("S06_L03");

            // Two-shot on Wattson and the conduit wall, one thing at a time: the camera settles, the beam reaches the
            // wall, the flash, then white and the way out.
            Gesture(WattsonController.Gesture.Aim, look: "Wall Point", point: "Wall Point", gadget: true);
            Shot("Wire Wattson");
            Visual("Two-shot: Wattson raises the Ohm-niscope toward the tunnel wall", 0.8);
            Show("Beam");
            Visual("The beam reaches the wall", 0.4);
            Show("[VFX] Ohm-niscope flash");
            Sfx("Exit");
            Ambience("off");
            Music("off");
            Visual("Ohm-niscope flash", 0.6);
            Cue("fade 1 0.3", "a white flash (0.3 s), like the way in");
            Handoff("Scene06Complete", cut: true, afterSeconds: 0.35); // a cut under the white, no second fade: Scene 7's pull-back off the wire is seen
        }

        // The + terminal's own readout: a level gauge standing on top of the + block (front-left of its cap), its blue fill
        // rising from 30 % to 80 % over 1.4 s when it is shown (a legacy Animation clip, so the show cue restarts it), marked
        // "V" above its slot. Up there it has clear space over the conduit's + end, away from the "+ positive" label and the
        // stream, at 1.5x: on the floor it was 45 x 130 px, the smallest thing in the frame, crowding the label.
        void BatteryReadout()
        {
            const float width = 0.44f, height = 1.5f, depth = 0.3f, face = -0.05f, scale = 1.5f;
            const float slotWidth = 0.2f, slotHeight = 1.1f, slotY = -0.12f, edge = 0.012f;
            var gauge = Node("Battery Readout", null, PlusBlock + new Vector3(-0.35f, 1.8f + scale * height / 2, -0.35f)); // the block's cap top is 1.8 m over its centre
            gauge.transform.rotation = Quaternion.Euler(0, 45, 0);
            gauge.transform.localScale = Vector3.one * scale;
            var pop = gauge.AddComponent<PopIn>();
            pop.duration = 0.4f;
            pop.overshoot = 1.15f;

            Prim(PrimitiveType.Cube, "Housing", gauge.transform, new Vector3(0, 0, face + depth / 2), new Vector3(width, height, depth),
                Lit("Wire_GaugeHousing", new Color(0.09f, 0.11f, 0.16f), 0.7f, 0.5f));
            Prim(PrimitiveType.Quad, "Track", gauge.transform, new Vector3(0, slotY, face - 0.002f), new Vector3(slotWidth + 0.02f, slotHeight + 0.02f, 1),
                Lit("Wire_GaugeTrack", new Color(0.02f, 0.03f, 0.06f), 0f, 0.3f));
            var rim = Glow("Glow_WireGaugeEdge", Hdr(ConduitBlue, 1.6f), softTexture: false);
            float rimX = slotWidth / 2 + 0.01f + edge / 2, rimY = slotHeight / 2 + 0.01f + edge / 2;
            foreach (var (name, pos, size) in new[]
            {
                ("Edge", new Vector3(-rimX, 0, 0), new Vector3(edge, slotHeight + 0.02f + 2 * edge, 1)),
                ("Edge", new Vector3(rimX, 0, 0), new Vector3(edge, slotHeight + 0.02f + 2 * edge, 1)),
                ("Edge", new Vector3(0, -rimY, 0), new Vector3(slotWidth + 0.02f, edge, 1)),
                ("Edge", new Vector3(0, rimY, 0), new Vector3(slotWidth + 0.02f, edge, 1)),
            })
                Prim(PrimitiveType.Quad, name, gauge.transform, pos + new Vector3(0, slotY, face - 0.004f), size, rim);
            for (int i = 0; i <= 4; i++)
                Prim(PrimitiveType.Quad, "Tick", gauge.transform, new Vector3(rimX + 0.05f, slotY - slotHeight / 2 + i * slotHeight / 4, face - 0.004f), new Vector3(0.08f, 0.025f, 1), rim);
            var mark = Card("Gauge V", "V", palette.voltage, gauge.transform, new Vector3(0, slotY + slotHeight / 2 + 0.15f, face - 0.006f), 0.18f, WorldText.Look.Glow, true, sparks: false);
            mark.transform.localRotation = Quaternion.identity;
            refs.Remove(mark.name);

            var pivot = Node("Fill Pivot", gauge.transform, new Vector3(0, slotY - slotHeight / 2, face - 0.006f));
            Prim(PrimitiveType.Quad, "Fill", pivot.transform, new Vector3(0, slotHeight / 2, 0), new Vector3(slotWidth, slotHeight, 1),
                Glow("Glow_WireGauge", Hdr(palette.voltage, 1.6f), softTexture: false));
            var rise = Asset(Folder + "/Timeline/Scene06_BatteryRise.anim", () => new AnimationClip());
            rise.legacy = true;
            rise.wrapMode = WrapMode.ClampForever;
            rise.ClearCurves();
            rise.SetCurve("Fill Pivot", typeof(Transform), "localScale.y", AnimationCurve.EaseInOut(0, 0.3f, 1.4f, 0.8f));
            EditorUtility.SetDirty(rise);
            var animation = gauge.AddComponent<Animation>();
            animation.AddClip(rise, "Battery Rise");
            animation.clip = rise;
            animation.playAutomatically = true;
            animation.wrapMode = WrapMode.ClampForever;
            animation.cullingType = AnimationCullingType.AlwaysAnimate;

            gauge.SetActive(false);
            Ref(gauge);
        }

        // Scene 4's Ohm-niscope beam travels with Wattson's gadget; lengthened to reach the conduit wall for the final aim
        // and registered so the cue can switch it on.
        void NiscopeBeam()
        {
            var beam = refs["Wattson"].GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Beam");
            if (!beam) return;
            var line = beam.GetComponent<LineRenderer>();
            if (line && line.positionCount == 2) line.SetPosition(1, new Vector3(0, 0, Vector3.Distance(PlatformPoint, WallPoint) - 0.3f));
            Ref(beam.gameObject);
        }
    }
}
