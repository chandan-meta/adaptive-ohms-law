using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw.Editor
{
    // Scene 9: Build the Circuit. Ammeter in series, voltmeter in parallel, close the key. On the classroom bench
    // (Scene09_series_ammeter_L06 / Scene09_pulse_XY_callouts): Bench Close is the reference frame with the screen
    // callouts, Bench Present shows Wattson behind the bench for the drags and the key, the teaching board carries
    // the current path and the reminder in chalk.
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
            ClassroomSet(modernBench: true, audience: false);
            Wattson(WattsonHomeC, BenchWideCamC);
            director.matchCutFrom = "Scene08_OurLab";
            director.rig = Rig(assembled: false, keyClosed: false, voltage: 0);
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
        }

        // The current path in amber chalk, low on the board's left half: the strip Bench Close sees above the bench
        // (board y 0.95-1.30, left of the HUD), two lines at 24 px from Bench Present where the ammeter snaps.
        void PathLabel()
        {
            var uv = new Vector2(0.06f, 0.09f);
            float cap = WorldText.CapAt(WorldText.Px.Small, BoardSurface(uv).position, BenchPresentCamC, BenchPresentLookC, BenchPresentFov);
            Board("Source → Ammeter → Nichrome Wire XY → Switch → Source", palette.current, uv, cap, 1.55f, TextAlignmentOptions.Left, name: "Path");
        }

        // The reminder is chalk at the top of the board, read in the Boards shot at 56 px, a little right of centre so
        // Wattson's hat at Boards Point clears its first line; the learner's prediction is an amber line under it.
        // The director's `reminder` cue finds "Card Reminder" and its "Your Prediction" TextMeshPro by name, so the
        // board node takes the card's name.
        void Reminder()
        {
            var reminder = Board("If potential difference increases, what happens to current?", Chalk, new Vector2(0.53f, 0.78f), BoardsShotTitleCap, 3f, name: "Reminder");
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
            var tint = Hdr(palette.current, 2f);
            tint.a = 0.6f;
            var glow = Glow("Glow_BadgePulse", tint);
            foreach (var letter in new[] { "X", "Y" })
            {
                var at = refs["Card " + letter].transform.position + Vector3.back * 0.045f; // just in front of the badge face
                var halo = Prim(PrimitiveType.Quad, "Halo " + letter, root.transform, at, new Vector3(0.16f, 0.16f, 1f), glow);
                halo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var pop = halo.AddComponent<PopIn>();
                pop.duration = 0.35f;
                pop.fromScale = 0.4f;
                pop.overshoot = 1.3f;
                pop.pulse = 0.2f;
                pop.pulseSpeed = 5f;
                Emitter("Rings " + letter, root.transform, at, glow, ps =>
                {
                    var main = ps.main;
                    main.loop = true;
                    main.duration = 1f;
                    main.startLifetime = 0.9f;
                    main.startSpeed = 0f;
                    main.startSize = 0.05f;
                    main.maxParticles = 8;
                    var emission = ps.emission;
                    emission.rateOverTime = 2f;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.002f;
                    var size = ps.sizeOverLifetime;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 3f)); // 2 cm → 15 cm rings
                });
            }
            root.SetActive(false);
            Ref(root);
        }

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
            Line("S09_L05", 0.2);
            Gate("S09_PlaceAmmeter", "Drag or tap the ammeter into the glowing series slot (snap; shake back if dropped elsewhere). buildMode=AutoPlace or autoAnswer flies it in. On snap the current path lights: Source → Ammeter → Nichrome Wire XY → Switch → Source.");
            // On the snap CircuitRig lights the path amber and plays the X → Y pulse (its [VFX] panel shows itself); the
            // path goes on the board in chalk and the camera settles on the Scene09_series_ammeter_L06 frame.
            Show("Board Path");
            Shot("Bench Close");
            Gesture(WattsonController.Gesture.Talking, moveTo: "Wattson Home");
            Cue("callout show source");
            Cue("callout show wire");
            Cue("callout show key");
            Line("S09_L06");

            Shot("Bench Present");
            Cue("callout hideall");
            Gesture(WattsonController.Gesture.Point, point: "VOLTMETER", moveTo: "Voltmeter Point");
            Visual("Wattson moves to the voltmeter", 0.8);
            Cue("callout show voltmeter", "Voltmeter callout at the tray");
            Line("S09_L07", 0.2);
            Show("[VFX] X and Y pulse");
            Gate("S09_PlaceVoltmeter", "Drag or tap the voltmeter into the glowing parallel slot across XY.");
            Hide("[VFX] X and Y pulse");
            Cue("callout hideall");
            Gesture(WattsonController.Gesture.Point, point: "NICHROME WIRE — XY");
            Line("S09_L08");

            Gesture(WattsonController.Gesture.Point, point: "switch / key", moveTo: "Key Point");
            Cue("prompt Click on the key to close the circuit.", "on-screen prompt");
            Line("S09_L09", 0.2);
            Gate("S09_CloseKey", "Tap/click the key (tap-hand hint). CLICK sfx, CircuitState.keyClosed = true, small default voltage so the needles move. The nichrome wire stays visually normal.");
            Cue("prompt", "prompt cleared");
            // The key closes at 0.5 V: the needles move, the lamp lights and the pulse runs again. Stay on the bench for it.
            Gesture(WattsonController.Gesture.Talking);
            Line("S09_L10");
            // The reminder is chalk on the teaching board; the Boards shot reads it with Wattson beside the board. The
            // key's pulse (3 runs from the close) and its panel end here, where the wire leaves the frame.
            Shot("Boards");
            Gesture(WattsonController.Gesture.Point, point: "Card Reminder", moveTo: "Boards Point");
            Hide("[VFX] Pulse travels from X to Y");
            Hide("Board Path");
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
