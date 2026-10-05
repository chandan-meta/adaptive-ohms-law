using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 6: Adaptive Question. Opens on Scene 5's last frame (the same electrons, trails and counter carry over the
    // match-cut); the two words become their definitions, Wattson freezes the electrons with the Ohm-niscope and takes
    // the counter away, then turns the battery up, so nothing on screen gives the answer away before it is given.
    public class Scene06Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene06";
        public override string SceneName => "Scene06_AdaptiveQuestion";
        public override string Title => "Adaptive Question";

        // Concept panels (AboveTarget) hang with their bottom edge 40 px below the word they replace, so each one sits
        // where its VOLTAGE / CURRENT card was: upper-left and upper-right, the right one dropping under the HUD.
        static readonly Vector2 ConceptPanelOffset = new Vector2(0, -40f);

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
            // Scene 5's last frame, already lit: field on, terminals and their labels up, the counter counting at the
            // cross-section, and both concept words settled in place (no pop, no sparks). Scene 5 hides its bracket and
            // drift arrow before the end, so neither is built here.
            var wire = WireStage(CopperTint, lattice: false, terminalsOn: true);
            wire.fieldOn = true;
            wire.counting = true;
            director.wire = wire;
            director.matchCutFrom = "Scene05_InsideTheWire";
            refs["Cross Section"].SetActive(true);
            refs["Counter"].SetActive(true);
            Wattson(PlatformPoint, WireCamPos);
            Settled(ConceptWord("Voltage", "VOLTAGE", "Symbol: V\nUnit: volt (V)", palette.voltage, -1));
            Settled(ConceptWord("Current", "CURRENT", "Symbol: I\nUnit: ampere (A)", palette.current, +1));

            // The definitions (ART_DIRECTION section 9 wording, not the references' answer-giving lines) take the words'
            // places at "Detective test": screen-space panels hung on the word markers.
            Concept("voltage", VoltageConceptTitle, VoltageConceptBody, CalloutDef.Diagram.Battery, CalloutDef.Anchor.AboveTarget, refs["Concept Left"]).aboveOffset = ConceptPanelOffset;
            Concept("current", CurrentConceptTitle, CurrentConceptBody, CalloutDef.Diagram.Tube, CalloutDef.Anchor.AboveTarget, refs["Concept Right"]).aboveOffset = ConceptPanelOffset;

            BatteryReadout();
            NiscopeBeam();
        }

        protected override void Beats()
        {
            Shot("Wire Inside");
            Cue("wire.field 1", "match-cut from Scene 5: carriers still drifting − → +");
            Gesture(WattsonController.Gesture.Greet);
            Visual("Wattson turns toward the learner", 0.8);
            // The floating words give way to their definitions as the test is announced.
            Hide("Card Voltage", 0.6);
            Hide("Card Current", 0.6);
            Cue("callout show voltage", "Voltage (V) concept panel takes the VOLTAGE card's place", 0.6);
            Cue("callout show current", "Current (I) concept panel takes the CURRENT card's place", 0.6);
            Line("S06_L01");

            // First the freeze, and the counter goes with it (a held number beside a rising gauge would read as an answer);
            // then the voltage rise on the + terminal and its gauge. The drift never changes on screen.
            Gesture(WattsonController.Gesture.RaiseGadget, point: "Card Terminal +", gadget: true);
            Cue("wire.freeze 1", "electrons frozen (a little random motion only) until the scene ends, so the drift never reveals the answer");
            Show("[VFX] time-freeze (Ohm-niscope holds the carriers)");
            Hide("Counter", 0.3);
            Visual("The Ohm-niscope freezes the electrons; the counter is put away", 0.8);
            Show("Battery Readout");
            Cue("boost Card Terminal +", "Wattson raises the battery voltage: the + terminal brightens as its V gauge rises");
            Visual("Voltage rises on the + terminal's gauge while the electrons stay frozen", 1.6);

            Gesture(WattsonController.Gesture.Point, point: "Concept Left"); // the Voltage panel: what is being changed
            Line("S06_L02", 0.2);
            Cue("callout hide voltage", "the MCQ panel takes the top of the frame", -0.2);
            Cue("callout hide current", "", -0.2);
            Gesture(WattsonController.Gesture.Listening);
            Gate("S06_Prediction", "MCQ: A. Increase · B. Decrease · C. Stay the same → LearnerSession.prediction; proposed reaction S06_R_A/B/C; no right/wrong feedback",
                "S06_R_A", "S06_R_B", "S06_R_C");
            Hide("[VFX] time-freeze (Ohm-niscope holds the carriers)");
            Gesture(WattsonController.Gesture.Thinking);
            Line("S06_L03");

            // Two-shot on Wattson and the conduit wall, one thing at a time: the camera settles, the beam reaches the
            // wall, the flash, then white.
            Gesture(WattsonController.Gesture.Aim, look: "Wall Point", point: "Wall Point", gadget: true);
            Shot("Wire Wattson");
            Visual("Two-shot: Wattson raises the Ohm-niscope toward the tunnel wall", 1.0);
            Show("Beam");
            Visual("The beam reaches the wall", 0.4);
            Show("[VFX] Ohm-niscope flash");
            Visual("Ohm-niscope flash", 0.7);
            Cue("fade 1", "fade to white");
            Handoff("Scene06Complete", cut: false, afterSeconds: 0.8);
        }

        // Scene 5's concept word, exactly as it was left there: glow headline with its symbol and unit sub-lines, at the
        // concept marker on `side` (-1 left, +1 right), faced to the Wire Inside camera.
        GameObject ConceptWord(string name, string word, string sub, Color color, int side)
        {
            var card = Card(name, WorldText.Sub(word, sub, WireConceptCap, WireConceptCap * 0.56f), color, null, WireCardPosition(side),
                WireConceptCap, WorldText.Look.Glow, shown: true, width: WorldText.Wrap.Concept, sparks: false);
            Face(card);
            return card;
        }

        // A card already on screen at the match-cut keeps its bob but must not pop in again.
        static void Settled(GameObject card)
        {
            var pop = card.transform.Find("Text").GetComponent<PopIn>();
            pop.fromScale = 1f;
            pop.overshoot = 1f;
        }

        // The + terminal's own readout: a slim level gauge let into the block's front-left corner at 45°, its face toward
        // the Wire Inside camera and its back inside the block, standing on the floor beside the + badge and under the
        // conduit's entry, marked "V" (voltage, covered in Scene 5) above its slot. Shown as Wattson turns the voltage up:
        // it pops in and its blue fill rises from 30 % to 80 % over 1.4 s (a legacy Animation clip, so the show cue
        // restarts it), stopping short of the top so it reads as a level, in a blue that survives the bloom.
        void BatteryReadout()
        {
            const float width = 0.44f, height = 1.5f, depth = 0.3f, face = -0.05f;   // face 5 cm proud of the corner; depth >= face + width/2 keeps the back corners in the block
            const float slotWidth = 0.2f, slotHeight = 1.1f, slotY = -0.12f, edge = 0.012f; // the slot sits low; the "V" takes the band above it
            Vector3 corner = PlusBlock + new Vector3(-0.8f, 0, -0.8f);                // the block's vertical front-left edge
            var gauge = Node("Battery Readout", null, new Vector3(corner.x, SpaceFloorY + height / 2, corner.z));
            gauge.transform.rotation = Quaternion.Euler(0, 45, 0);                    // local -Z bisects the corner, within ~7° of the camera direction
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
            for (int i = 0; i <= 4; i++) // the scale: five marks up the right of the slot, thick enough to read at this distance
                Prim(PrimitiveType.Quad, "Tick", gauge.transform, new Vector3(rimX + 0.05f, slotY - slotHeight / 2 + i * slotHeight / 4, face - 0.004f), new Vector3(0.08f, 0.025f, 1), rim);
            // "V" in the band above the slot; a child of the gauge, so it is not a cue target of its own.
            var mark = Card("Gauge V", "V", palette.voltage, gauge.transform, new Vector3(0, slotY + slotHeight / 2 + 0.15f, face - 0.006f), 0.18f, WorldText.Look.Glow, true, sparks: false); // ~22 px cap in Wire Inside
            mark.transform.localRotation = Quaternion.identity;
            refs.Remove(mark.name);

            // The fill grows from the slot's bottom: a pivot there whose Y scale the clip drives from 30 % to 80 %.
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
            animation.AddClip(rise, "Battery Rise"); // the default clip must also be in the list or Play Automatically finds nothing
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
