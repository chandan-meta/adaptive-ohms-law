using Unity.Cinemachine;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 7: Back to Georg Ohm. His 1826 investigation, again and again, looking for a pattern.
    public class Scene07Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene07";
        public override string SceneName => "Scene07_BackToOhm";
        public override string Title => "Back to Georg Ohm";

        // The apparatus hero shot (Scene07_galvanometer_closeup): source frame-left (x 18 %), mercury cups centre (31-48 %),
        // galvanometer right (74-85 %, glass 210 px wide, its knob at 73 % up just under the top-right callout), the loose
        // copper wires in the foreground above the subtitle band (26-44 % up), Ohm's torso at the top with his head out of
        // frame like the reference. Replaces the kit's Table Close aim, which predates the apparatus layout.
        static readonly Vector3 HeroCam = new Vector3(0.1f, 1.35f, -1.2f), HeroLook = new Vector3(-0.05f, 0.82f, 0.1f);
        const float HeroFov = 40f;
        // Board layout (board UV). The headlines use the L tier: two XL headlines (2.0 + 1.8 m with TMP's bold spacing)
        // do not fit side by side on the 3.2 m board; the question uses the S tier so its two lines (2.6 m) fit a chalk
        // panel inside the board (at BoardCap they span 3.0 m). Both are sized for the Bench Wide lens that reads them.
        static float BoardHeadlineCap => WorldText.CapAt(WorldText.Px.Large, BoardCentreC, BenchWideCamC, BenchWideLookC, BenchWideFov);   // 0.14
        static float BoardSmallCap => WorldText.CapAt(WorldText.Px.Small, BoardCentreC, BenchWideCamC, BenchWideLookC, BenchWideFov);      // 0.093
        static readonly Rect QuestionPanel = new Rect(0.075f, 0.48f, 0.85f, 0.27f);   // under the headlines, above Ohm's head (v ≤ 0.45)
        const string NeedleSwing = "[VFX] galvanometer needle swing", Transforming = "[VFX] historical apparatus starts transforming → modern circuit";

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
            ClassroomSet(modernBench: false, audience: true);
            ClassroomApparatus();
            Wattson(WattsonHomeC, BenchWideCamC);

            // The teaching board (ART_DIRECTION §3): the two clues as chalk headlines, the question in a chalk panel, a
            // battery-wire loop. Nothing about resistance or proportionality (§9). Headlines at v 0.9 clear the panel;
            // the panel bottom (v 0.48) clears Ohm's head, which reaches v ≈ 0.45 in Bench Wide even when he stands up.
            Board("Voltage → V", palette.voltage, new Vector2(0.25f, 0.9f), BoardHeadlineCap, name: "Voltage");
            Board("Current → I", palette.current, new Vector2(0.75f, 0.9f), BoardHeadlineCap, name: "Current");
            BoardPanel(QuestionPanel, "Board Question Panel");
            Board("Is there a mathematical relationship\nbetween voltage and current?", Chalk, QuestionPanel.center, BoardSmallCap, name: "Question");
            CircuitSketch();

            HeroShot();
            HeroCallouts();

            // The needle swing is real (OhmWorker binds to this name and swings the needle whenever it is shown); the
            // object is just the trigger the cues show and hide, and the VfxPanel follows it.
            Ref(Node(NeedleSwing, null, Galvanometer7 + Vector3.up * 0.3f)).SetActive(false);
            TransformShimmer();
        }

        // Re-aims the kit's Table Close onto the current apparatus layout (see HeroCam) and widens the lens: the loose
        // wires sit 0.6 m nearer than the needle the shot focuses on, so the f/1.8 profile would wash them out.
        // The pages fan moves above-left of the notebook, clear of the galvanometer glass in this framing, and faces the shot.
        void HeroShot()
        {
            var hero = cams["Table Close"];
            hero.transform.SetPositionAndRotation(HeroCam, Quaternion.LookRotation(HeroLook - HeroCam));
            var lens = hero.Lens;
            lens.FieldOfView = HeroFov;
            hero.Lens = lens;
            if (hero.TryGetComponent(out CinemachineVolumeSettings post)) post.Profile = lensMedium;

            var fan = refs["Pages Fan"];
            fan.transform.position = Notebook7 + new Vector3(-0.23f, 0.2f, 0f);
            fan.transform.localScale = Vector3.one * 0.8f;
            Face(fan, HeroCam);
        }

        // Screen-space labels for the hero shot, anchored where the reference puts them around the props as this camera
        // sees them: the "[VFX]" panel keeps the top-left, the source panel floats over the source (its mid-left slot
        // would cover it), the notebook panel sits bottom-right over bare desk with its dot on the page left of the pen,
        // and the wires panel points at the second loose wire rather than the group's bounds centre (the mounted wire).
        void HeroCallouts()
        {
            Callout("galvanometer", refs["torsion galvanometer"], CalloutDef.Anchor.TopRight);
            Callout("cups", refs["Mercury Cups"], CalloutDef.Anchor.TopCentre);
            Callout("source", refs["Thermoelectric Source"], CalloutDef.Anchor.AboveTarget);
            Callout("notebook", refs["notebook"], CalloutDef.Anchor.BottomRight, new Vector3(-0.06f, 0.02f, 0f));
            var wires = CalloutTexts["Copper Test Wires"];
            Callout("wires", refs["Copper Test Wires"].transform.Find("Test Wire 2").gameObject, wires.title, wires.description, CalloutDef.Anchor.BottomLeft);
        }

        // Battery-wire loop in chalk (Scene07_two_clues board), lower-left of the board where neither Ohm nor Wattson
        // stands: a cell on the left side of the loop with the "V" clue beside it, the "I" clue over the current arrow
        // on the top wire. Conventional current leaves the long (+) plate, so the arrow on the top wire points right.
        void CircuitSketch()
        {
            var loop = BoardGroup("Circuit");
            const float l = 0.08f, r = 0.28f, b = 0.10f, t = 0.34f, minus = 0.19f, plus = 0.26f, width = 0.014f;
            BoardLine(new[] { new Vector2(l, plus), new Vector2(l, t), new Vector2(r, t), new Vector2(r, b), new Vector2(l, b), new Vector2(l, minus) }, Chalk, width, loop);
            BoardLine(new[] { new Vector2(l - 0.025f, plus), new Vector2(l + 0.025f, plus) }, Chalk, width, loop);
            BoardLine(new[] { new Vector2(l - 0.012f, minus), new Vector2(l + 0.012f, minus) }, Chalk, width * 1.7f, loop);
            BoardLine(new[] { new Vector2(0.165f, t + 0.025f), new Vector2(0.18f, t), new Vector2(0.165f, t - 0.025f) }, Chalk, width, loop);
            Board("V", palette.voltage, new Vector2(l - 0.05f, (plus + minus) * 0.5f), BoardSmallCap, parent: loop);
            Board("I", palette.current, new Vector2(0.18f, t + 0.065f), BoardSmallCap, parent: loop);
        }

        // The first flicker of Scene 8's transformation: a sparkle shimmer over the whole apparatus while the cue is on.
        // The "[VFX]" name keeps the cue text and puts up its screen panel.
        void TransformShimmer()
        {
            var fx = Ref(Node(Transforming, null, DeskTopC + new Vector3(-0.1f, 0.12f, 0.1f)));
            Emitter("Shimmer", fx.transform, Vector3.zero, Glow("Glow_TransformShimmer", Hdr(palette.wattsonAccent, 3f)), ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.035f);
                main.maxParticles = 300;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = ps.emission;
                emission.rateOverTime = 240;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(1.5f, 0.3f, 0.8f);
            });
            fx.SetActive(false);
        }

        protected override void Beats()
        {
            Shot("Bench Wide");
            Gesture(WattsonController.Gesture.Idle);
            Visual("Fade in from white. Ohm works at his table (loop). Wattson floats beside the group.", 1.2);
            Show("Board Voltage");
            Show("Board Current", 0.6);
            Gesture(WattsonController.Gesture.Flick);
            Line("S07_L01");
            Gesture(WattsonController.Gesture.Point, point: "Georg Ohm");
            Line("S07_L02");
            Show("Board Circuit");
            Line("S07_L03");
            Show("Board Question Panel");
            Show("Board Question");
            Gesture(WattsonController.Gesture.Point, point: "Board Question");
            Line("S07_L04");

            Shot("Table Close");
            Gesture(WattsonController.Gesture.Point, look: "notebook", point: "torsion galvanometer");
            Show(NeedleSwing);
            foreach (var id in new[] { "galvanometer", "source", "cups", "wires", "notebook" }) Cue("callout show " + id);
            Visual("Ohm's experiment loop: picks a short copper wire, places it, changes the source, watches the galvanometer, writes a reading", 0.5);
            Line("S07_L05");
            // One cycle per "again": a page lands and the needle takes a fresh swing (re-showing the trigger swings it at once).
            foreach (var (page, at) in new[] { ("Page 1", 0.0), ("Page 2", 1.1), ("Page 3", 2.1) })
            {
                Hide(NeedleSwing, at - 0.12);
                Show(NeedleSwing, at);
                Show(page, at);
            }
            Line("S07_L06", 0.1);
            Show("Pages Fan");
            Cue("callout hide cups", "the fan rises where the cups panel sits");
            Line("S07_L07");
            Hide(NeedleSwing);

            Shot("Bench Wide");
            Cue("callout hideall");
            Hide("Pages Fan", 0.4);
            Cue("ohm.pose Examine", "Ohm straightens up with a copper wire in his hands (Scene07_ohm_copper_wires)");
            Gesture(WattsonController.Gesture.Point, point: "Georg Ohm");
            Line("S07_L08");
            Cue("ohm.pose Work", "back to the loop for the match-cut into Scene 8");
            Gesture(WattsonController.Gesture.Talking);
            Line("S07_L09", 0.2);
            Show(Transforming);
            Visual("First flicker of the apparatus transforming (the transformation itself is Scene 8)", 1.2);
            Hide(Transforming);
            Handoff("Scene07Complete", cut: true, afterSeconds: 0.1);
        }
    }
}
