using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 13: the finale. The three pieces come back together in the panel, V, I and R resolve into V = IR centre
    // screen and the name OHM'S LAW, with a warm musical resolution; Georg Ohm reappears beside the bench to take a bow,
    // Wattson celebrates, the lab lights warm, and the composition holds. The panel then offers replay and exit; the
    // teaching ends here (the optional Scene 14 video plays over the hold if one is configured).
    public class Scene13Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene13";
        public override string SceneName => "Scene13_OhmsLaw";
        public override string Title => "Ohm's Law";

        static readonly Vector3 FinaleCamC = new Vector3(0f, 1.35f, -2.9f), FinaleLookC = new Vector3(0f, 1.05f, 0.3f);
        static readonly Vector3 FrontPoint = new Vector3(-0.6f, 1.4f, -0.8f); // front-left of the bench, inside the Finale frame beside the panel

        protected override void Dialogue()
        {
            L("S13_L01", "So now we know all three pieces.", 2.4f);
            L("S13_L02", "The push.", 1.2f);
            L("S13_L03", "The flow.", 1.2f);
            L("S13_L04", "And the opposition.", 1.6f);
            L("S13_L05", "And the relationship connecting them...", 2.6f);
            L("S13_L06", "And today... we call it OHM'S LAW.", 3.0f);
            L("S13_N01", "You've followed the charges, measured the current, and discovered the pattern. Voltage, current, and resistance are connected by one relationship: V equals I times R. That's Ohm's law!", 11.0f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 12: voltage V (the push, volt), current I (the flow, ampere), resistance R (the opposition, ohm), V ∝ I for the nichrome wire at constant temperature, V/I constant, slope = R, 1 Ω = 1 V / 1 A, this wire's R = 5 Ω.",
                "Ohm's law: V = IR. The potential difference across a conductor equals the current through it times its resistance, at constant temperature.",
                "If asked about the Christmas lights, give only a hint: think about which battery could keep a bigger potential difference across the lights.",
            },
            new[] { "The full explanation of the Christmas-lights mystery (the Scene 14 video does that)" },
            ("What does V = IR mean?", "It says the potential difference across a conductor equals the current through it multiplied by its resistance. Our nichrome wire: 5 ohms times 0.2 ampere gives 1 volt, 5 ohms times 1 ampere gives 5 volts. Three clues, one relationship."),
            ("Can I use Ohm's law for any wire?", "For metal wires at a steady temperature, yes, and that covers most of what you meet in a school lab. Some things, like bulb filaments that heat up, or diodes, do not keep a constant resistance, so their V-I graph is not a straight line. Scientists call the well-behaved ones ohmic conductors."),
            ("Who was Ohm?", "Georg Simon Ohm was a German teacher and physicist, born in 1789. He built much of his own equipment, measured patiently for years, and published this relationship in 1827. The unit of resistance, the ohm, is named in his honour."));

        protected override void Stage()
        {
            ClassroomSet(modernBench: true, audience: false);
            LabBoardLaw(); // the lab board's own chalk (V = IR), hidden until the learner has built the law
            director.matchCutFrom = "Scene12_Resistance";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            director.rig.showPath = false;
            MeterPlates(false); // the strip carries V, I and R through the finale: no plate reads through the hero glass
            Wattson(Scene12Builder.RevealSide, Scene10Builder.StudyCamC); // where Scene 12 leaves him, above its ohm plate
            Vcam("Bench Study", Scene10Builder.StudyCamC, Scene10Builder.StudyLookC, Scene10Builder.StudyFov, refs["Bench Point"].transform, lensMedium);
            Vcam("Finale", FinaleCamC, FinaleLookC, 42f, refs["Bench Point"].transform, lensMedium);
            // The finale dolly (audit finding 10): 0.4 m along the Finale axis toward Ohm and the bench through S13_N01
            // ("push 11"; a cut there under reduced motion). The composer keeps the bench centred; Wattson at Front Point ends
            // with his left edge about 88 % out to the free frame's edge; the end card holds this pushed-in pose.
            Marker("Cam Finale Goal", FinaleCamC + (FinaleLookC - FinaleCamC).normalized * 0.4f);
            Marker("Front Point", FrontPoint);

            // Georg Ohm, back for the finale: behind the bench, presenting, hidden until the law has its name.
            var ohm = Model(Root + "/Scene04/Prefabs/Ohm_Scene04.prefab", "Georg Ohm", null, OhmFeetC, Quaternion.LookRotation(Vector3.back).eulerAngles, 1.55f, collider: false);
            var worker = ohm.GetComponent<OhmWorker>();
            if (!worker) worker = ohm.AddComponent<OhmWorker>();
            worker.mode = OhmWorker.Mode.Present;
            worker.deskTop = ClassroomDeskY;
            ohm.SetActive(false);
            Ref(ohm);
            FinaleGlow();
            // The module's theme under the finale is the library's BGM_Professor_Ohm_Laboratory (the title bed), cued at L01 in
            // Beats; the old Scene 4 bgm.mp3 is no longer appended to the bank.
        }

        // Warm sparks rising from the bench as the law gets its name (a one-shot the fx cue plays; thinned under reduced motion).
        // A slow lift and a long life, so they hang around the bench and Ohm through the hold instead of racing to the ceiling.
        void FinaleGlow()
        {
            var fx = Emitter("Finale Glow", null, DeskTopC + Vector3.up * 0.1f, Glow("Glow_Finale", Hdr(new Color(1f, 0.85f, 0.5f), 3.5f)), ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = 1.5f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.04f);
                main.gravityModifier = -0.02f;
                main.maxParticles = 320;
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 160), new ParticleSystem.Burst(0.5f, 120) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(2.0f, 0.1f, 0.9f);
            });
            fx.gameObject.SetActive(false);
            Ref(fx.gameObject);
        }

        protected override void Beats()
        {
            // Match-cut from Scene 12: R in the readings strip and this wire's 5 Ω in the dock (the ohm plate goes with the cut).
            Shot("Bench Study");
            Ambience("RoomTone");
            Music("BGM_Professor_Ohm_Laboratory"); // audio: the warm theme returns on S13_L01 (2.5 s crossfade out of the discovery theme) and holds to the end card
            Cue("readings.r 1"); Cue("readings.top 1"); // the finale keeps only the three readings and the law: no table, no graph
            Formula("this wire:  5.0 V ÷ 1.00 A = 5.0 Ω"); // the ohm, as Scene 12 left it, in the dock (the bench stays clear)
            Gesture(WattsonController.Gesture.Talking, moveTo: "Tray Point");
            Line("S13_L01");

            // The three pieces, one line each, then the relationship.
            Cue("formula.clear");
            Cue("hero.clear");
            Gesture(WattsonController.Gesture.Point, point: "Panel Point");
            Formula("V   —   the push: potential difference");
            Line("S13_L02");
            Formula("I   —   the flow: current");
            Line("S13_L03");
            Formula("R   —   the opposition: resistance");
            Line("S13_L04");
            Gesture(WattsonController.Gesture.Talking);
            Line("S13_L05", 0.2);
            Gate("S13_BuildLaw", "the learner taps V, then I, then R: V = I × R builds in the panel");
            Cue("formula.clear");
            Cue("hero V = I × R|", "the law the learner just built, centre screen");
            Visual("The three pieces resolve into V = IR", 1.2);

            // The finale composition: the lab warms, Georg Ohm returns to take a bow, Wattson celebrates; the whole
            // investigation is in one frame: the bench and its meters, the readings strip and the law. Timed inside S13_L06
            // ("And today... we call it OHM'S LAW."): the camera pulls back on "we call it" (1.0 s) while Wattson comes
            // forward, and the name, the stings and the sparks land on "OHM'S LAW" (1.8 s): the hero rule's SFX_Scientific_Discovery
            // as the attack, SFX_Magic_Success 0.2 s behind it as the swell (the theme has been under it since L01; the end
            // card's SFX_Final_Success is the "end" rule's, so no Finale sting here). The name owns the screen for L06; then it
            // gives way to the man it is named after: Ohm appears behind the bench (where the plate stood) as the summary starts.
            Shot("Finale", offset: 1.0);
            Gesture(WattsonController.Gesture.Talking, moveTo: "Front Point", offset: 1.0);
            Cue("hero OHM'S LAW|", "the name, centre screen", 1.8);
            Sfx("SFX_Magic_Success", 2.0);
            Cue("fx Finale Glow", "", 1.8);
            Gesture(WattsonController.Gesture.Greet, offset: 1.8);
            Line("S13_L06", 0.1);
            Visual("The name holds centre screen; the lab warms", 0.8);
            Cue("hero.clear", "the plate gives way to Ohm: the case-closed card carries the law at the end");
            Cue("look 2.0 finale", "the lab warms for Ohm (WP-L's look; a look name the director lacks only logs a warning)");
            Cue("rise 1.4 Board Law", "the lab's board writes up the law the learner just built (its chalk, wiped until now)");
            Cue("appear 0.9 Georg Ohm", "Georg Ohm reappears behind the bench, presenting");
            Cue("push 11 Cam Finale", "a slow dolly toward Ohm and the bench through the summary");
            Cue("ohm.pose Present", "", 0.2);
            Cue("ohm.look Main Camera", "", 0.2);
            Line("S13_N01", 0.3);
            Gesture(WattsonController.Gesture.Idle);
            Visual("The completed composition holds: bench, meters, Ohm and Wattson", 2.5);
            Cue("end", "the panel offers replay and exit; the module holds here");
        }
    }
}
