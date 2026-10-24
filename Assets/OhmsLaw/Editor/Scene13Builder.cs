using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 13: the finale, on one centred surface (LawReveal). The three pieces arrive one by one on Wattson's snap (V the
    // push, I the flow, R the opposition, each with its unit); the learner taps them into V = I × R; OHM'S LAW lands
    // above it with one restrained accent while the lab warms; the conditions (an ohmic conductor at constant
    // temperature) and the rearrangements I = V ÷ R and R = V ÷ I join it; then the bench sweeps 1 → 5 V while the
    // equation's numbers follow, R holding at 5 Ω: the current rises in step. Georg Ohm reappears behind the bench, and
    // the case-closed card offers Explore Now, replay and exit (the optional Scene 14 video plays over the hold).
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
            L("S13_N02", "For an ohmic conductor at a constant temperature, V equals I times R.", 4.8f, proposed: true);
            L("S13_N03", "Turn it around: I equals V over R, and R equals V over I.", 4.2f, proposed: true);
            L("S13_N04", "Keep R the same and raise the push: the current rises in step.", 4.2f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 12: voltage V (the push, volt), current I (the flow, ampere), resistance R (the opposition, ohm), V ∝ I for the nichrome wire at constant temperature, V/I constant, slope = R, 1 Ω = 1 V / 1 A, this wire's R = 5 Ω.",
                "Ohm's law: V = IR. The potential difference across a conductor equals the current through it times its resistance, at constant temperature.",
                "Ohm's law holds for an ohmic conductor (like this nichrome wire) while its temperature and other physical conditions stay the same. Rearranged: I = V / R and R = V / I. With R fixed, doubling V doubles I.",
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
            // The finale dolly (audit finding 10): 0.4 m along the Finale axis toward Ohm and the bench as he returns
            // ("push"; a cut there under reduced motion). The composer keeps the bench centred; Wattson at Front Point ends
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
            Music("BGM_Professor_Ohm_Laboratory"); // audio: the warm theme returns on S13_L01 (a crossfade out of the discovery theme) and holds to the end card
            Cue("readings.r 1"); Cue("readings.top 1"); // the finale keeps only the three readings and the law: no table, no graph
            Formula("this wire:  5.0 V ÷ 1.00 A = 5.0 Ω"); // the ohm, as Scene 12 left it, in the dock (the bench stays clear)
            Gesture(WattsonController.Gesture.Talking, moveTo: "Tray Point");
            Line("S13_L01");

            // The three pieces, centre screen, each arriving on his snap as he names it.
            Cue("formula.clear");
            Cue("hero.clear");
            Cue("law.pieces", "three empty places for the pieces, centre screen");
            Cue("point.law", "Wattson beside the law surface, pointing at it");
            Cue("law.piece V", "V · potential difference · volt (V), on his snap");
            Line("S13_L02");
            Cue("law.piece I", "I · current · ampere (A)");
            Line("S13_L03");
            Cue("law.piece R", "R · resistance · ohm (Ω)");
            Line("S13_L04");
            Cue("point.clear");
            Gesture(WattsonController.Gesture.Talking);
            Line("S13_L05", 0.2);
            Gate("S13_BuildLaw", "the learner taps V, then I, then R on the centre tiles: each flies into its place in V = I × R, and = and × pop in");
            Visual("The three pieces resolve into V = I × R", 1.0);

            // The name: the camera pulls back on "we call it" (1.0 s) while Wattson comes forward, and OHM'S LAW lands above
            // the equation on "OHM'S LAW" (1.8 s) with his snap, one restrained accent (law.name's own), the sparks, and the
            // lab warming. Then the law in full: its conditions, its rearrangements, and the bench showing it: the push rises
            // in whole volts while the equation's numbers follow, R staying 5 Ω.
            Shot("Finale", offset: 1.0);
            Gesture(WattsonController.Gesture.Talking, moveTo: "Front Point", offset: 1.0);
            Cue("law.name", "OHM'S LAW above the equation: his snap, one restrained accent, a soft glow", 1.8);
            Cue("fx Finale Glow", "", 1.8);
            Cue("look 2.0 finale", "the lab warms with the name (WP-L's look)", 1.8);
            Gesture(WattsonController.Gesture.Greet, offset: 1.8);
            Line("S13_L06", 0.1);
            Cue("law.conditions", "for an ohmic conductor at constant temperature");
            Cue("point.law", "Wattson beside the law, pointing at it");
            Line("S13_N02", 0.2);
            Cue("law.forms", "I = V ÷ R and R = V ÷ I slide in under the law");
            Line("S13_N03", 0.2);
            Cue("law.demo 4", "the bench sweeps 1 → 5 V; the live line under the law reads V = I × R in numbers, R held at 5 Ω");
            Line("S13_N04", 0.3);
            Cue("point.clear");
            Visual("The current rises in step with the push", 0.6);

            // The man it is named after: the law steps up out of the way, Ohm reappears behind the bench where it stood,
            // and the camera eases toward him and the bench. The case-closed card carries the law from here.
            Cue("law.clear", "the law surface steps aside: the case-closed card carries it");
            Cue("rise 1.4 Board Law", "the lab's board writes up the law the learner just built (its chalk, wiped until now)");
            Cue("appear 0.9 Georg Ohm", "Georg Ohm reappears behind the bench, presenting");
            Cue("push 4 Cam Finale", "a slow dolly toward Ohm and the bench");
            Cue("ohm.pose Present", "", 0.2);
            Cue("ohm.look Main Camera", "", 0.2);
            Gesture(WattsonController.Gesture.Greet, offset: 0.4);
            Visual("The completed composition holds: bench, meters, Ohm and Wattson", 2.6);
            Gesture(WattsonController.Gesture.Idle);
            Cue("end", "the case-closed card: Explore Now, replay, chapters, exit; the module holds here");
        }
    }
}
