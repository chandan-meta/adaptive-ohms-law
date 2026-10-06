using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 7: Back to Georg Ohm, short. Out of the white onto the very wire the class dived into, back along the dive
    // axis to the same desk in Scene 4's study; the question Ohm asked; one close look at his 1826 apparatus and his
    // method (change one thing, observe, repeat, look for the pattern); then the hand-over to the modern instruments.
    // The two clues stay on the slate for Scene 8's match-cut.
    public class Scene07Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene07";
        public override string SceneName => "Scene07_BackToOhm";
        public override string Title => "Back to Georg Ohm";

        static readonly Vector3 HeroCam = new Vector3(0.1f, 1.35f, -1.15f), HeroLook = new Vector3(0.25f, 0.92f, 0.1f);
        const float HeroFov = 44f;
        const string NeedleSwing = "[VFX] galvanometer needle swing", Transforming = "[VFX] historical apparatus starts transforming → modern circuit";

        protected override void Dialogue()
        {
            L("S07_P01", "And we're back! That's the very wire we just explored.", 3.2f, proposed: true);
            L("S07_L01", "Now you know the two clues.", 2.2f);
            L("S07_L04", "Is there a mathematical relationship between voltage and current?", 4.2f);
            L("S07_L05", "In 1826, Ohm kept changing one electrical condition, the potential difference and observing what happened to the current.", 7.5f);
            L("S07_L07", "He wasn't looking for one reading. He was looking for a pattern.", 4.5f);
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
            TwoCluesBoard(shown: false);
            Vcam("Study Hero", HeroCam, HeroLook, HeroFov, refs["Apparatus Point"].transform, lensMedium);
            Callout("galvanometer", refs["torsion galvanometer"], CalloutDef.Anchor.TopRight);
            Callout("notebook", refs["notebook"], CalloutDef.Anchor.TopCentre);
            Callout("source", refs["Thermoelectric Source"], CalloutDef.Anchor.MidLeft);
            Ref(Node(NeedleSwing, null, ApparatusC + Vector3.up * 0.3f)).SetActive(false);
            TransformShimmer();
        }

        // The first flicker of Scene 8's transformation: a sparkle shimmer over the apparatus while the cue is on.
        void TransformShimmer()
        {
            var fx = Ref(Node(Transforming, null, OhmDeskC + new Vector3(0f, 0.12f, 0.05f)));
            fx.AddComponent<ReducedMotionFx>().calm = 0.35f;
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
            // Out of the white on the wire itself, back along the dive axis, then out to the room: Ohm at his desk,
            // the slate beside it, Wattson between them. The study's own room tone and a soft pad come back.
            Shot("Study Macro");
            Ambience("StudyTone");
            Music("PadLab");
            Gesture(WattsonController.Gesture.Idle);
            Visual("Fade in from white on the copper wire the class dived into (Scene 4's wire, same pose)", 0.8);
            Shot("Study Aim");
            Line("S07_P01", 0.2);
            Shot("Study Wide");
            Gesture(WattsonController.Gesture.Point, point: "Georg Ohm");
            Cue("ohm.look Wattson", "Ohm glances up at Wattson while he speaks");
            Visual("Pull back to the room: Ohm works at his desk (loop), the slate beside it, Wattson beside the group", 0.8);
            Show("Board Voltage");
            Show("Board Current", 0.5);
            Sfx("Highlight", 0.5);
            Gesture(WattsonController.Gesture.Flick, look: "Board Anchor");
            Line("S07_L01");
            Show("Board Circuit");
            Show("Board Question Panel");
            Show("Board Question");
            Gesture(WattsonController.Gesture.Point, point: "Board Anchor");
            Line("S07_L04");

            // One close look at the apparatus: his method, with the galvanometer and the notebook noted in the panel.
            Cue("ohm.look", "back to his work");
            Shot("Study Hero", cut: true);
            Gesture(WattsonController.Gesture.Point, look: "notebook", point: "torsion galvanometer");
            Show(NeedleSwing);
            Cue("callout show galvanometer");
            Visual("Ohm's experiment loop: picks a short copper wire, places it, changes the source, watches the galvanometer, writes a reading", 0.4);
            Line("S07_L05");
            foreach (var (page, at) in new[] { ("Page 1", 0.0), ("Page 2", 1.1), ("Page 3", 2.1) })
            {
                Cue("ohm.swing", "", at);
                Show(page, at);
            }
            Cue("callout show notebook");
            Line("S07_L07");
            Hide(NeedleSwing);
            Cue("callout hideall");

            Shot("Study Wide", cut: true);
            Cue("ohm.pose Work", "the loop for the match-cut into Scene 8");
            Cue("ohm.look Wattson");
            Gesture(WattsonController.Gesture.Talking);
            Line("S07_L09", 0.2);
            Show(Transforming);
            Sfx("Transform");
            Visual("First flicker of the apparatus transforming (the transformation itself is Scene 8)", 1.0);
            Hide(Transforming);
            Handoff("Scene07Complete", cut: true, afterSeconds: 0.1);
        }
    }
}
