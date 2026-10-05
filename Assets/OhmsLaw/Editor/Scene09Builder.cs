using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw.Editor
{
    // Scene 9: Build the Circuit. Ammeter in series, voltmeter in parallel, close the key. On the classroom bench
    // (Scene09_series_ammeter_L06 / Scene09_pulse_XY_callouts): Bench Close is the reference frame with the screen
    // callouts, Bench Present shows Wattson behind the bench for the drags and the key, the teaching board carries
    // the current path, its conventional-current heading and the reminder in chalk.
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

            // Conventional current, said once while the lit path is new (WOW_PLAN §1, §5), by the say gate S09_Conventional:
            // the _If_ line replaces the plain one for a learner who answered S05_Drift with B (electrons flow + to −).
            // The key is still open here: neither line claims a current is flowing (the plain one says we "draw" it).
            L("S09_Conventional", "Plot twist: we draw conventional current plus to minus. Electrons drift minus to plus!", 4.4f, proposed: true);
            L("S09_Conventional_If_S05_Drift_B", "Your plus-to-minus answer in the wire? That's conventional current; electrons drift minus to plus.", 5.0f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 8: voltage (V, volt) as the push, current (I, ampere), Ohm's investigation, the modern instruments.",
                "The nichrome wire XY is the test conductor; we will change the potential difference across it and measure the current through it.",
                "The ammeter is connected in series so the same current passes through it and the test wire.",
                "The voltmeter is connected in parallel across the two ends of the wire, X and Y, to measure the potential difference between them.",
                "Closing the key completes the circuit so current can flow.",
                "The lit path and the X → Y pulse show conventional current, which is taken from + to − around the circuit; the free electrons themselves drift the other way, from − to +.",
            },
            new[] { "The readings", "The pattern between V and I", "V is proportional to I", "V = IR", "Resistance", "Ohms", "The Christmas-lights answer" },
            ("Why is the ammeter in series?", "Because we want the ammeter to measure the current through the nichrome wire. In series, the ammeter is part of the same single path, so every bit of charge that passes through the wire also passes through the ammeter. Same path, same current."),
            ("Why is the voltmeter in parallel?", "A voltmeter compares two points. We connect one lead to X and the other to Y, the two ends of the nichrome wire, so it reads the potential difference across the wire itself, while the current carries on through the wire as before."),
            ("What happens if the key is open?", "An open key is a gap in the path. The push from the source is still there, but charge cannot cross the gap, so no current flows and the ammeter reads zero. Close the key and the loop is complete."));

        protected override void Stage()
        {
            ClassroomSet(modernBench: true, audience: false);
            Wattson(WattsonHomeC, BenchWideCamC); // Scene 8's last frame: Wattson at "Wattson Home", Bench Wide, no Ohm (Lab Continuity)
            director.matchCutFrom = "Scene08_OurLab";
            director.rig = Rig(assembled: false, keyClosed: false, voltage: 0);
            // Closing the key is a needle move, not a number: the digital plates stay off all scene (they also sat in the Boards
            // shot's subtitle band), so no reading precedes Scene 10's 1 V baseline and the S10_Predict2V prediction (rig.readouts 1).
            director.rig.readoutsShown = false;
            director.showLearnerPrediction = true; // "Remember your prediction?": the learner's Scene 6 pick in amber under the reminder
            // The path label is chalk on the board ("Board Path"); the rig's floating copy would duplicate it over the readouts.
            if (director.rig.pathLabel) { Object.DestroyImmediate(director.rig.pathLabel.gameObject); director.rig.pathLabel = null; }

            // Screen callouts in the reference layout: source top-left (dot on its top edge), ammeter top-centre, key
            // top-right, wire bottom-centre under the rod; the voltmeter's panel sits at the right, where it waits in the tray.
            Callout("wire", refs["NICHROME WIRE — XY"], CalloutDef.Anchor.BottomCentre);
            Callout("source", refs["cells / controllable DC source"], CalloutDef.Anchor.TopLeft, new Vector3(0, 0.08f, 0));
            Callout("ammeter", refs["AMMETER"], CalloutDef.Anchor.TopCentre);
            Callout("key", refs["switch / key"], CalloutDef.Anchor.TopRight);
            Callout("voltmeter", refs["VOLTMETER"], CalloutDef.Anchor.MidRight);

            PathLabel();
            Reminder();
            BadgePulse();
            KeyPulse();
        }

        // The current path in chalk on the board's lower left, read from Bench Present (where the ammeter snaps) at 24 px:
        // the storyboard string in two lines, the longer one second so it ends (~x 1120 px) left of Wattson's hat at
        // Voltmeter Point; the block (v 0.08-0.26) clears the chalk ledge and the ammeter's readout plate (top at v ≈ 0.03 in
        // Bench Present). Above it, the conventional-current heading for the S09_Conventional beat (v 0.33, clear of Wattson's
        // hat at every Bench Present marker but Wire Point). Bench Close sees this strip only behind its callouts. Amber
        // eased toward chalk, so it reads as chalk, not neon.
        static readonly Vector2 PathUV = new Vector2(0.04f, 0.17f), ConventionUV = new Vector2(0.04f, 0.33f);

        void PathLabel()
        {
            var tint = Color.Lerp(palette.current, Chalk, 0.3f);
            float cap = WorldText.CapAt(WorldText.Px.Small, BoardSurface(PathUV).position, BenchPresentCamC, BenchPresentLookC, BenchPresentFov);
            Board("Source → Ammeter →\nNichrome Wire XY → Switch → Source", tint, PathUV, cap, 0f, TextAlignmentOptions.Left, name: "Path");
            Board("Conventional current: + → −", tint, ConventionUV, cap, 0f, TextAlignmentOptions.Left, name: "Conventional");
        }

        // The reminder is chalk on the board, read in the Boards shot at 56 px, a little right of centre and in the board's
        // middle (v 0.62: line 1 stays above Wattson's hat at Boards Point, line 2 starts right of it); the learner's
        // prediction is an amber line under it. The director's `reminder` cue finds "Card Reminder" and its "Your Prediction"
        // TextMeshPro by name, so the board node takes the card's name.
        void Reminder()
        {
            var reminder = Board("If potential difference increases, what happens to current?", Chalk, new Vector2(0.53f, 0.62f), BoardsShotTitleCap, 3f, name: "Reminder");
            refs.Remove("Board Reminder");
            reminder.name = "Card Reminder";
            Ref(reminder);
            var question = reminder.transform.Find("Text").GetComponent<TextMeshPro>();
            float cap = WorldText.CapAt(WorldText.Px.Large, BoardCentreC, BoardsCamC, BoardsLookC, BoardsFov);
            reminder.SetActive(true); // TextMeshPro lays out only under an awake parent
            var mine = WorldText.Card("Your Prediction", "Your prediction: ", palette.current, reminder.transform, new Vector3(0, -0.22f * BoardSizeC.y, 0), cap, null, 3f);
            mine.fontSharedMaterial = question.fontSharedMaterial; // same chalk face
            mine.gameObject.SetActive(false);
            reminder.SetActive(false);
        }

        // "Points X and Y pulse" while the voltmeter waits: a breathing amber halo with slow rings in front of each
        // badge. The "[VFX] …" name keeps the blockout's cues and the screen panel; the wire itself never changes.
        void BadgePulse()
        {
            var root = Node("[VFX] X and Y pulse");
            var glow = PulseGlow();
            foreach (var letter in new[] { "X", "Y" })
                Halo(root.transform, letter, refs["Card " + letter].transform.position + Vector3.back * 0.045f, 0.16f, 0.05f, glow); // just in front of the badge face
            Calm(root);
            root.SetActive(false);
            Ref(root);
        }

        // "The key pulses" during S09_L09 / S09_CloseKey: the same halo and rings, larger and set just BEHIND the key
        // (away from the cameras), so it glows around the lever against the tray and the wall while the lever and its grip
        // stay unwashed in front (the tap hint's finger lands on the grip). No collider: the key keeps the tap.
        void KeyPulse()
        {
            var root = Node("[VFX] Key pulse");
            Halo(root.transform, "Key", refs["switch / key"].transform.position + new Vector3(0f, 0.09f, 0.08f), 0.36f, 0.1f, PulseGlow());
            Calm(root);
            root.SetActive(false);
            Ref(root);
        }

        Material PulseGlow()
        {
            var tint = Hdr(palette.current, 2f);
            tint.a = 0.6f;
            return Glow("Glow_BadgePulse", tint);
        }

        // One halo quad (PopIn: pops in, then breathes; no breathing under reduced motion) and an emitter of slow rings
        // growing from 0.4x to 3x `ring` (metres): 2 cm to 15 cm at the badges.
        void Halo(Transform root, string label, Vector3 at, float size, float ring, Material glow)
        {
            var halo = Prim(PrimitiveType.Quad, "Halo " + label, root, at, new Vector3(size, size, 1f), glow);
            halo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var pop = halo.AddComponent<PopIn>();
            pop.duration = 0.35f;
            pop.fromScale = 0.4f;
            pop.overshoot = 1.3f;
            pop.pulse = 0.2f;
            pop.pulseSpeed = 5f;
            Emitter("Rings " + label, root, at, glow, ps =>
            {
                var main = ps.main;
                main.loop = true;
                main.duration = 1f;
                main.startLifetime = 0.9f;
                main.startSpeed = 0f;
                main.startSize = ring;
                main.maxParticles = 8;
                var emission = ps.emission;
                emission.rateOverTime = 2f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.002f;
                var grow = ps.sizeOverLifetime;
                grow.enabled = true;
                grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 3f));
            });
        }

        // Reduced motion: the rings thin out (and dim) each time the pulse is shown.
        static void Calm(GameObject root) => root.AddComponent<ReducedMotionFx>().calm = 0.5f;

        protected override void Beats()
        {
            // Match-cut from Scene 8: the entry camera takes its last pose. Wattson flies to the wire and it lights.
            Shot("Bench Wide");
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY", moveTo: "Wire Point");
            Show("Wire Highlight");
            Visual("Wattson moves to the nichrome wire XY; it highlights", 1.0);
            Shot("Bench Present");
            Line("S09_L01");
            // Push in to the reference framing of the bench. Bench Close cuts anyone hovering behind the bench at the
            // hat, so Wattson clears the frame and talks from his home spot while the wire keeps its callout.
            Shot("Bench Close");
            Gesture(WattsonController.Gesture.Talking, moveTo: "Wattson Home");
            Cue("callout show wire", "Nichrome wire — XY callout under the rod", 0.6);
            Line("S09_L02");
            Line("S09_L03");
            Line("S09_L04");
            Hide("Wire Highlight");
            Cue("callout hide wire");

            // Tilt up to Bench Present: tray, glowing series slot and Wattson behind the bench are all in frame for the
            // drag; the ammeter callout follows the meter into its slot.
            Shot("Bench Present");
            Gesture(WattsonController.Gesture.Point, point: "AMMETER", moveTo: "Tray Point");
            Visual("Wattson moves to the ammeter in the tray", 0.8);
            Cue("callout show ammeter", "Ammeter callout, tracking the meter");
            Cue("rig.drag ammeter", "the series slot glows with S09_L05: the ammeter may be dragged now (the gate sets it too; a replay after the snap clears it)");
            Line("S09_L05", 0.2);
            Gate("S09_PlaceAmmeter", "Drag or tap the ammeter into the glowing series slot (snap; shake back if dropped elsewhere). buildMode=AutoPlace or autoAnswer flies it in. On snap the current path lights: Source → Ammeter → Nichrome Wire XY → Switch → Source.");
            // On the snap CircuitRig lights the path amber and plays the X → Y pulse (its [VFX] panel shows itself) and the
            // path goes up on the board in chalk. Bench Present holds a beat so the snap, the lit path and Wattson's reaction
            // read before the push-in to the Scene09_series_ammeter_L06 frame.
            Show("Board Path");
            Visual("The ammeter snaps in: the series path lights, the X → Y pulse starts, the path appears in chalk", 1.0);
            Shot("Bench Close");
            Gesture(WattsonController.Gesture.Talking, moveTo: "Wattson Home");
            Cue("callout show source");
            Cue("callout show wire");
            Cue("callout show key");
            Line("S09_L06");

            // Conventional current, labelled once while the path is new (WOW_PLAN §1): back on Bench Present with the
            // callouts down, the chalk heading goes up over the path as the 1 s tilt settles and Wattson points at it from
            // the tray, clear of the board's lower left. The say gate then holds the Timeline for ONE line: S09_Conventional,
            // or, for a learner who said electrons flow + to − in Scene 5 (S05_Drift B), S09_Conventional_If_S05_Drift_B.
            Shot("Bench Present");
            Cue("callout hideall");
            Gesture(WattsonController.Gesture.Point, point: "Board Conventional", moveTo: "Tray Point");
            Show("Board Conventional", 0.8);
            Visual("Chalk heading over the lit path: Conventional current: + → −", 1.2);
            Gate("S09_Conventional", "Say gate (no input): the board labels the lit series path 'Conventional current: + → −'. Wattson says S09_Conventional, or S09_Conventional_If_S05_Drift_B for a learner who picked B (electrons flow + to −) at S05_Drift.",
                "S09_Conventional_If_S05_Drift_B", "S09_Conventional");

            Gesture(WattsonController.Gesture.Point, point: "VOLTMETER", moveTo: "Voltmeter Point");
            Visual("Wattson moves to the voltmeter", 0.8);
            Cue("callout show voltmeter", "Voltmeter callout at the tray");
            Cue("rig.drag voltmeter", "the parallel slot glows with S09_L07: the voltmeter may be dragged now");
            Line("S09_L07", 0.2);
            Show("[VFX] X and Y pulse");
            Gate("S09_PlaceVoltmeter", "Drag or tap the voltmeter into the glowing parallel slot across XY.");
            Hide("[VFX] X and Y pulse");
            Cue("callout hideall");
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY");
            Line("S09_L08");

            // The key pulses (halo behind it, the director's tap hand on its grip) with the prompt until it is closed.
            Gesture(WattsonController.Gesture.Point, point: "switch / key", moveTo: "Key Point");
            Cue("prompt Click on the key to close the circuit.", "on-screen prompt");
            Show("[VFX] Key pulse");
            Line("S09_L09", 0.2);
            Gate("S09_CloseKey", "Tap/click the key (tap-hand hint). CLICK sfx, CircuitState.keyClosed = true, small default voltage so the needles move. The nichrome wire stays visually normal.");
            Cue("prompt", "prompt cleared");
            Hide("[VFX] Key pulse");
            // The key closes at 0.5 V: the needles move, the lamp lights and the pulse runs again. Stay on the bench for it.
            Gesture(WattsonController.Gesture.Talking);
            Line("S09_L10");
            // The reminder is chalk on the teaching board; the Boards shot reads it with Wattson beside the board. The
            // key's pulse (3 runs from the close) and its panel end here, where the wire leaves the frame.
            Shot("Boards");
            Cue("rig.path 0", "the lit path goes while the bench is off screen: the last Bench Close frame and Scene 10 show plain cables");
            Gesture(WattsonController.Gesture.Point, point: "Card Reminder", moveTo: "Boards Point");
            Hide("[VFX] Pulse travels from X to Y");
            Hide("Board Path");
            Hide("Board Conventional");
            Cue("reminder", "reminder chalk: If potential difference increases, what happens to current? (+ Your prediction when showLearnerPrediction)");
            Line("S09_L11", 0.2);
            Wait(1.0);
            // Back to Bench Close for the match-cut: Scene 10 opens on this frame (circuit complete, key closed, 0.5 V,
            // Wattson at home, nothing on the visible strip of the board).
            Shot("Bench Close");
            Gesture(WattsonController.Gesture.Talking, moveTo: "Wattson Home");
            Line("S09_L12", 0.2);
            Cue("handoff", "SceneHandoffState: CircuitState + camera");
            Handoff("Scene09Complete", cut: true, afterSeconds: 0.4);
        }
    }
}
