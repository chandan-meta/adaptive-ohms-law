using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 7: Back to Georg Ohm. His 1826 investigation, again and again, looking for a pattern.
    // Back in Scene 4's study (storyboard: "the historical lab (Scene 4's study)"): the same desk, the same 1826 apparatus,
    // Ohm on the same spot, the same candle-and-window light, the cameras on the room side like Scene 4's. It opens on the
    // copper wire the class dived into and pulls back along the dive axis to Ohm's desk, so "we went into this wire and we
    // are back at the same apparatus" reads on screen. The two clues are chalk on a slate beside the desk; Scene 8 opens on
    // this scene's last frame (match-cut).
    public class Scene07Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene07";
        public override string SceneName => "Scene07_BackToOhm";
        public override string Title => "Back to Georg Ohm";

        // The apparatus hero shot (Scene07_galvanometer_closeup): the source frame-left (19 %), the notebook and candle
        // centre, the torsion galvanometer right of centre behind Scene 4's wire, the mercury cups right, the loose copper
        // wires in the foreground above the subtitle band (25-35 % up), Ohm's torso at the top with his head out of frame.
        static readonly Vector3 HeroCam = new Vector3(0.1f, 1.35f, -1.15f), HeroLook = new Vector3(0.25f, 0.85f, 0.1f);
        const float HeroFov = 40f;
        const string NeedleSwing = "[VFX] galvanometer needle swing", Transforming = "[VFX] historical apparatus starts transforming → modern circuit";

        protected override void Dialogue()
        {
            // Proposed (WOW plan): names the return, over the pull-back from the wire to the desk.
            L("S07_P01", "And we're back! That's the very wire we just explored.", 3.2f, proposed: true);
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
                "We are back in Georg Ohm's study from Scene 4, at the same desk and the same copper wire we zoomed into.",
                "In 1826 Georg Ohm investigated by changing the potential difference and observing the current, many times, looking for a pattern rather than one reading.",
                "His apparatus: a copper-bismuth thermoelectric source (hot and cold junctions), a torsion galvanometer, copper test wires of different lengths, mercury cups as connection points, a notebook of measurements.",
            },
            new[] { "The pattern Ohm found", "V is proportional to I", "V = IR", "R = V / I", "Resistance", "Ohms", "The name 'Ohm's law'", "The Christmas-lights answer" },
            ("Why did Ohm repeat it so many times?", "One reading can fool you: a wobble in the needle, a warm wire, a loose contact. Ohm repeated every measurement so that only the real pattern was left when the accidents averaged out. That is what careful detectives do with evidence."),
            ("What is a galvanometer?", "A galvanometer detects electric current. Ohm's torsion galvanometer had a magnetic needle hanging on a thread; current in a nearby wire twisted the needle, and how far he had to twist the thread back told him how strong the current was. Our ammeter does that job today."),
            ("Why did Ohm use a thermoelectric source instead of a battery?", "The batteries of 1826 faded as they worked, so the push kept changing during a measurement. A copper-bismuth junction, with one end in steam and the other in ice, gave a small but steady potential difference. Steady conditions, reliable readings."));

        protected override void Stage()
        {
            HistoricalLab();
            ClassroomApparatus();                 // the 1826 apparatus on Ohm's desk, Ohm on Scene 4's spot
            Wattson(WattsonStudyC, StudyWideCamC);

            // The two clues in chalk on the study's slate (hidden for the cues; Scene 8 rebuilds them shown for the cut).
            TwoCluesBoard(shown: false);

            HeroShot();
            HeroCallouts();

            // The needle swing is real (OhmWorker binds to this name and swings the needle whenever it is shown); the
            // object is just the trigger the cues show and hide, and the VfxPanel follows it.
            Ref(Node(NeedleSwing, null, ApparatusC + Vector3.up * 0.3f)).SetActive(false);
            TransformShimmer();
        }

        // The galvanometer close-up (an insert: cut to and from it) on a medium lens; the loose wires sit 0.5 m nearer than
        // the needle the shot focuses on, so the f/1.8 profile would wash them out. The pages fan rises above-left of the
        // notebook, clear of the galvanometer glass in this framing, and faces the shot.
        void HeroShot()
        {
            Vcam("Study Hero", HeroCam, HeroLook, HeroFov, refs["Apparatus Point"].transform, lensMedium);
            var fan = refs["Pages Fan"];
            fan.transform.position = NotebookStudyC + new Vector3(-0.12f, 0.22f, 0f);
            fan.transform.localScale = Vector3.one * 0.8f;
            Face(fan, HeroCam);
        }

        // Screen-space labels for the hero shot, around the props as this camera sees them: the source panel at mid-left
        // beside it, the galvanometer top-right, the cups mid-right, the notebook top-centre (it gives way to the pages fan),
        // the wires panel bottom-left on the second loose wire rather than the group's bounds centre.
        void HeroCallouts()
        {
            Callout("galvanometer", refs["torsion galvanometer"], CalloutDef.Anchor.TopRight);
            Callout("cups", refs["Mercury Cups"], CalloutDef.Anchor.MidRight);
            Callout("source", refs["Thermoelectric Source"], CalloutDef.Anchor.MidLeft);
            Callout("notebook", refs["notebook"], CalloutDef.Anchor.TopCentre);
            var wires = CalloutTexts["Copper Test Wires"];
            Callout("wires", FindChild(refs["Copper Test Wires"], "Test Wire 2").gameObject, wires.title, wires.description, CalloutDef.Anchor.BottomLeft);
        }

        // The first flicker of Scene 8's transformation: a sparkle shimmer over the apparatus while the cue is on.
        // The "[VFX]" name keeps the cue text and puts up its screen panel.
        void TransformShimmer()
        {
            var fx = Ref(Node(Transforming, null, OhmDeskC + new Vector3(0f, 0.12f, 0.05f)));
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
                shape.scale = new Vector3(1.4f, 0.3f, 0.6f);
            });
            fx.SetActive(false);
        }

        protected override void Beats()
        {
            // Out of the white on the wire itself (Scene 4's SH11b axis), back along the dive axis to Scene 4's aim, then
            // out to the room: Ohm at his desk in the study, the slate beside it, Wattson between them.
            Shot("Study Macro");
            Gesture(WattsonController.Gesture.Idle);
            Visual("Fade in from white on the copper wire the class dived into (Scene 4's wire, same pose)", 0.9);
            Shot("Study Aim");
            Line("S07_P01", 0.2);
            Shot("Study Wide");
            Gesture(WattsonController.Gesture.Point, point: "Georg Ohm");
            Visual("Pull back to the room: Ohm works at his desk (loop), the slate beside it, Wattson beside the group", 1.0);
            Show("Board Voltage");
            Show("Board Current", 0.6);
            Gesture(WattsonController.Gesture.Flick, look: "Board Anchor");
            Line("S07_L01");
            Gesture(WattsonController.Gesture.Point, point: "Georg Ohm");
            Line("S07_L02");
            Show("Board Circuit");
            Line("S07_L03");
            Show("Board Question Panel");
            Show("Board Question");
            Gesture(WattsonController.Gesture.Point, point: "Board Anchor");
            Line("S07_L04");

            // The galvanometer close-up is an insert: a cut, so the panels come up on a settled frame.
            Shot("Study Hero", cut: true);
            Gesture(WattsonController.Gesture.Point, look: "notebook", point: "torsion galvanometer");
            Show(NeedleSwing);
            foreach (var id in new[] { "galvanometer", "source", "cups", "wires", "notebook" }) Cue("callout show " + id);
            Visual("Ohm's experiment loop: picks a short copper wire, places it, changes the source, watches the galvanometer, writes a reading", 0.5);
            Line("S07_L05");
            // One cycle per "again": a page lands and the needle takes a fresh swing; the needle's VFX panel stays up.
            foreach (var (page, at) in new[] { ("Page 1", 0.0), ("Page 2", 1.1), ("Page 3", 2.1) })
            {
                Cue("ohm.swing", "", at);
                Show(page, at);
            }
            Line("S07_L06", 0.1);
            Show("Pages Fan");
            Cue("callout hide notebook", "the fan rises above the notebook, where its panel's leader runs");
            Line("S07_L07");
            Hide(NeedleSwing);

            Shot("Study Wide", cut: true);
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
