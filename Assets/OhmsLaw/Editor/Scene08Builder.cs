using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 8: From Ohm's Lab to Our Lab. Opens on Scene 7's last frame in Ohm's study (match-cut). Wattson freezes the
    // experiment and, in frame beside the desk, starts the transformation with a clear gesture: each 1826 instrument
    // changes in its own place into today's, one at a time (glowing dust swirls round it, the old one shrinks out while the
    // new one grows in: torsion galvanometer → ammeter, thermoelectric source → DC source, copper test wires → nichrome
    // wire XY, mercury cups → key). Then Ohm's desk becomes today's bench where it stood, the instruments settle into the
    // circuit and the voltmeter joins; the final composition holds while Wattson ties it to the lesson. Ohm goes back to
    // his time (Wattson waves him off). Last, in the wide shot, the study around the bench turns into the classroom lab:
    // a wave of light runs out from the bench, the floor spreads from under it, the study's walls lift away as the
    // classroom's rise, the ceiling follows, and the candle-and-window light crossfades to the classroom's. The last frame
    // is Scene 9's first (classroom Bench Wide, the rig with the meters in the tray, Wattson at home, no Ohm).
    public class Scene08Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene08";
        public override string SceneName => "Scene08_OurLab";
        public override string Title => "From Ohm's Lab to Our Lab";

        const string ApparatusFx = "[VFX] Ohm's apparatus becomes today's instruments", RoomFx = "[VFX] Ohm's study becomes today's lab";
        const string Source = "cells / controllable DC source", Wire = "NICHROME WIRE — XY", Key = "switch / key";
        static readonly string[] Dressing = { "Study Lamp", "Study Candle", "Study Inkstand", "Study Papers", "Study Globe", "Study Eyewear", "notebook" };
        static readonly string[] ClassroomGroups = { "Classroom Floor", "Classroom Back", "Classroom Left", "Classroom Right", "Classroom Ceiling" };

        protected override float DockWidth => 0f; // cinematic: the study and its transformation fill the frame

        protected override void Dialogue()
        {
            L("S08_L01", "Ohm didn't have the modern instruments sitting in your school laboratory today.", 4.5f);
            L("S08_L02", "So let's investigate his relationship using the instruments you would use today.", 4.5f);
            // Proposed (WOW plan): the link to the lesson over the held composition, and Ohm's farewell (he has no voice).
            L("S08_P01", "The instruments changed, detectives. The relationship we're testing is still the same.", 4.4f, proposed: true);
            L("S08_P02", "Thank you, Georg Ohm! Back to 1826 you go.", 3.0f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 7: voltage as the push (V, volt), current as charge per second (I, ampere), Ohm's repeated 1826 investigation and his apparatus.",
                "Ohm's instruments turned into today's: his torsion galvanometer into an ammeter, his thermoelectric source into a controllable DC source, his copper test wires into the nichrome wire XY, his mercury cups into a key; a voltmeter joined them. The relationship being tested is the same.",
                "An ammeter measures current (I, ampere). A voltmeter measures potential difference (V, volt).",
                "The nichrome wire XY is the test conductor; X and Y are its two ends.",
                "A variable DC source (cells) sets the potential difference; a key (switch) opens and closes the circuit.",
            },
            new[] { "How the ammeter and voltmeter are connected (series and parallel, Scene 9)", "The pattern between V and I", "V is proportional to I", "V = IR", "Resistance", "Ohms", "The Christmas-lights answer" },
            ("What is nichrome?", "Nichrome is an alloy, a metal mixture of mostly nickel and chromium. It is tough, it does not rust, and it behaves very steadily in experiments like ours, which is exactly why school labs use a nichrome wire as the test conductor."),
            ("What does a voltmeter measure?", "A voltmeter measures the potential difference, the voltage, between two points. In our circuit it will tell us the push across the two ends of the nichrome wire, X and Y, in volts."),
            ("Why not use Ohm's old instruments?", "We could, but Ohm's torsion galvanometer needed a steady hand and a lot of patience, and his thermoelectric source gave only a tiny push. Today's ammeter and voltmeter give clear numbers straight away, so we can see the relationship much more clearly."));

        // Under the study's warm fill (and the gadget's flare) the near-white bench top clips to a white slab, so it appears in
        // a darker, matte copy of its material and the "look" crossfade lerps it to the classroom top (SceneDirector.Lab.cs
        // LabLook.tinted). Not a "Classroom_" name: CinematicPolish would swap in its older, glossier copy.
        void StudyBenchTop(SceneDirector.LabLook classroom)
        {
            var top = Lit("Classroom_BenchTop", new Color(0.86f, 0.87f, 0.89f), 0f, 0.55f);
            var studyTop = Lit("Study_BenchTop", new Color(0.5f, 0.5f, 0.52f), 0f, 0.2f);
            var tinted = new List<Renderer>();
            foreach (var renderer in refs["Bench"].GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] == top) { materials[i] = studyTop; changed = true; }
                if (!changed) continue;
                renderer.sharedMaterials = materials;
                tinted.Add(renderer);
            }
            classroom.tinted = tinted.ToArray();
            classroom.tintFrom = studyTop;
            classroom.tintTo = top;
        }

        protected override void Stage()
        {
            // The classroom lab first, alone, so its reflection probe bakes without the study; captured as the "classroom"
            // look, then put away for the transformation: its room groups and the bench hidden, its lights off.
            ClassroomSet(modernBench: true, audience: false);
            var classroom = CaptureLook("classroom", refs["Lab"].transform.Find("Lighting"));
            foreach (var group in ClassroomGroups.Append("Bench")) refs[group].SetActive(false);
            foreach (var light in classroom.lights) light.enabled = false;
            if (classroom.volume) classroom.volume.weight = 0f;
            if (classroom.probe) classroom.probe.gameObject.SetActive(false);

            // Scene 7's study, apparatus, Ohm and Wattson, exactly as Scene 7 built them (the match-cut's first frame).
            HistoricalLab();
            var study = CaptureLook("study", refs["Study Lighting"].transform);
            StartIn(study, classroom);
            director.looks = new[] { study, classroom };
            StudyBenchTop(classroom);
            director.matchCutFrom = "Scene07_BackToOhm";
            ClassroomApparatus();
            // The farewell and the room's change from the front-left, closer, 20 degrees round the bench: "glide 3.4 Cam Bench
            // Orbit" sweeps it onto Bench Wide's pose (the goal) at under 6 deg/s with the composer keeping the bench framed;
            // the 1 s blend into Bench Wide then lands exactly on Scene 9's first frame. (The base Shot() caps a two-camera
            // blend at 1 s, so the sweep is a glide of one camera instead.)
            // ponytail: the glide is a straight chord, not a true arc (the camera dips ~0.1 m toward the bench mid-way);
            // a CinemachineSplineDolly on a 3-point spline if the dip shows in the frames.
            var benchPoint = refs["Bench Point"].transform;
            Vector3 orbitStart = Quaternion.Euler(0f, 20f, 0f) * new Vector3(0f, 0f, BenchWideCamC.z * 0.8f) + Vector3.up * (BenchWideCamC.y - 0.1f);
            Vcam("Bench Orbit", orbitStart, BenchWideLookC, BenchWideFov, benchPoint, lensClassroom, track: benchPoint);
            Marker("Cam Bench Orbit Goal", BenchWideCamC);
            Wattson(WattsonStudyC, StudyWideCamC);
            // Scene 7 ends with the two clues on the slate and the three measurement pages on the notebook: same first
            // frame here, without their pop-in. They stay until the study goes.
            TwoCluesBoard(shown: true);
            foreach (var page in new[] { "Page 1", "Page 2", "Page 3" })
            {
                Object.DestroyImmediate(refs[page].GetComponent<PopIn>());
                refs[page].SetActive(true);
            }

            // Today's circuit exactly as Scene 9 opens on it (meters in the tray, key open, 0 V); every part waits hidden.
            var rig = Rig(assembled: false, keyClosed: false, voltage: 0, active: true);
            director.rig = rig;
            foreach (var part in new[] { "AMMETER", "VOLTMETER", Wire, Source, Key, "Parts Tray" }) refs[part].SetActive(false);
            // The key's return lead ends at the source's final post: shown once the source has settled.
            var returnLead = Ref(FindChild(refs[Key], "Lead Key → Source").gameObject);
            returnLead.SetActive(false);
            // The meter and the source grow where the galvanometer and the thermoelectric source stand (a size that fits
            // Ohm's desk) and settle onto their places in the circuit when the bench forms; the bench appears on the desk's
            // footprint and widens; Ohm steps back from it. "<name> Goal" is where each ends (Scene 9's pose).
            Interim("AMMETER", ApparatusC, 0.55f);
            Interim(Source, SourceStudyC + new Vector3(0f, 0f, 0.01f), 0.8f);
            var bench = refs["Bench"].transform;
            Goal("Bench");
            bench.position = new Vector3(OhmDeskC.x, bench.position.y, OhmDeskC.z);
            bench.localScale = new Vector3(1.61f / BenchWidth, 1f, 0.66f / BenchDepth);
            var ohm = refs["Georg Ohm"].transform;
            Goal("Georg Ohm").transform.position = ohm.position + Vector3.forward * 0.12f;

            // Labels (ART_DIRECTION §4) for the Study Desk frame (Callouts reserves the subtitle band for every anchor): the
            // source and the wire stacked top-left (the wire's panel at mid-left clipped Wattson's hat at Transform Point), the
            // meters stacked top-right, the key bottom-right on the bench front (a third right-column panel would stack down
            // onto the key itself).
            Callout("source", refs[Source], CalloutDef.Anchor.TopLeft, new Vector3(0f, 0.10f, 0f));
            Callout("ammeter", refs["AMMETER"], CalloutDef.Anchor.TopRight, new Vector3(0f, 0.04f, 0f));
            Callout("voltmeter", refs["VOLTMETER"], CalloutDef.Anchor.TopRight, new Vector3(0f, 0.02f, 0f));
            Callout("wire", refs[Wire], CalloutDef.Anchor.TopLeft);
            Callout("key", refs[Key], CalloutDef.Anchor.BottomRight, new Vector3(0.115f, 0.095f, 0f));

            Effects();
        }

        // Moves a built rig part to where its old counterpart stands, smaller by `scale`, and leaves a "<name> Goal" marker
        // on its built pose (the 'settle' cue takes it back there).
        void Interim(string name, Vector3 at, float scale)
        {
            var part = refs[name].transform;
            Goal(name);
            part.position = at;
            part.localScale *= scale;
        }

        // "<name> Goal": the object's current world position, with its local scale stored on the marker (scene root).
        GameObject Goal(string name)
        {
            var part = refs[name].transform;
            var goal = Marker(name + " Goal", part.position);
            goal.transform.localScale = part.localScale;
            return goal;
        }

        // The restrained effects: cyan glowing dust swirling round each prop as it changes (one-shot "fx" objects, thinned
        // under reduced motion by the fx cue: SceneDirector.Lab Soften), a slow glow over the desk while the instruments
        // change, a gold dust as Ohm goes back to his time, and the wave of light that runs out from the bench across the
        // floor as the room changes. The two "[VFX]" phases carry a soft shimmer (Scene 4's) on the effects bus, under the
        // voice. Effects a plain `show` cue activates (the freeze burst, the apparatus glow) carry ReducedMotionFx instead;
        // never both, or the thinning would apply twice.
        void Effects()
        {
            var cyan = palette.wattsonAccent;
            Burst("[VFX] freeze (Ohm holds still)", null, refs["Georg Ohm"].transform.position + new Vector3(0f, 1.0f, -0.12f), palette.charge, new Vector3(0.6f, 0.8f, 0.5f), 80, 0.2f)
                .AddComponent<ReducedMotionFx>().calm = 0.35f;

            Burst("Dust Galvanometer", null, ApparatusC + Vector3.up * 0.1f, cyan, new Vector3(0.28f, 0.22f, 0.28f), 70, 0.12f, swirl: 2.2f);
            Burst("Dust Source", null, SourceStudyC + Vector3.up * 0.08f, cyan, new Vector3(0.36f, 0.18f, 0.24f), 70, 0.12f, swirl: 2.2f);
            Burst("Dust Wires", null, (TestWiresStudyC + WireStudyC) / 2 + Vector3.up * 0.04f, cyan, new Vector3(0.95f, 0.1f, 0.5f), 110, 0.12f, swirl: 1.2f);
            Burst("Dust Cups", null, CupsStudyC + Vector3.up * 0.05f, cyan, new Vector3(0.42f, 0.1f, 0.14f), 60, 0.12f, swirl: 2.2f);
            Burst("Dust Desk", null, OhmDeskC + Vector3.up * 0.06f, cyan, new Vector3(1.7f, 0.12f, 0.7f), 170, 0.15f, swirl: 1.0f);
            Burst("Dust Voltmeter", null, refs["VOLTMETER"].transform.position + Vector3.up * 0.12f, cyan, new Vector3(0.26f, 0.22f, 0.36f), 60, 0.12f, swirl: 2.2f);
            SwapSparks("AMMETER", ApparatusC + Vector3.up * 0.1f);
            SwapSparks(Source, SourceStudyC + Vector3.up * 0.08f);
            SwapSparks(Wire, (TestWiresStudyC + WireStudyC) / 2 + Vector3.up * 0.04f);
            SwapSparks(Key, CupsStudyC + Vector3.up * 0.05f);
            SwapSparks("VOLTMETER", refs["VOLTMETER"].transform.position + Vector3.up * 0.12f);
            // The time wave: a ring of electric light standing over the bench, turned to the orbit camera, that grows out past
            // the walls as the room changes ("appear 1.8 Time Wave", 0 to 3.4 m, then hidden). Lying on the floor it was only
            // a sliver under the bench; the sparks of the Room Wave fly out in the same plane.
            var electric = new Color(0.31f, 0.82f, 1f);
            Ref(MeshPrim("Time Wave", null, Torus("TimeWave", 0.03f, 64, 8), Glow("Glow_TimeWave", Hdr(electric, 4f), softTexture: false), new Vector3(0f, 0.95f, 0f), new Vector3(0f, 20f, 0f), Vector3.one * 3.4f)).SetActive(false);
            var wire = refs[Wire].transform;
            Burst("Spark X", null, wire.TransformPoint(new Vector3(-0.35f, 0.01f, 0f)), palette.current, Vector3.one * 0.08f, 30);
            Burst("Spark Y", null, wire.TransformPoint(new Vector3(0.35f, 0.01f, 0f)), palette.current, Vector3.one * 0.08f, 30);
            var ohm = refs["Georg Ohm"].transform.position;
            Burst("Ohm Farewell Dust", null, ohm + new Vector3(0f, 0.9f, 0f), new Color(1f, 0.8f, 0.48f), new Vector3(0.6f, 1.8f, 0.5f), 180, 0.25f, swirl: 1.2f, lifetime: 1.6f);

            // A slow glow over the desk while the instruments change; the wave runs from the bench.
            var apparatusFx = Ref(Node(ApparatusFx, null, OhmDeskC + new Vector3(0f, 0.2f, 0.05f)));
            apparatusFx.AddComponent<ReducedMotionFx>().calm = 0.35f;
            Emitter("Glow Dust", apparatusFx.transform, Vector3.zero, Glow("Glow_TransformDust", Hdr(cyan, 2.2f)), ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.018f);
                main.maxParticles = 120;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = -0.01f;
                var emission = ps.emission;
                emission.rateOverTime = 40f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(1.6f, 0.35f, 0.7f);
            });
            Shimmer(apparatusFx.transform, loop: true);
            apparatusFx.SetActive(false);

            var roomFx = Ref(Node(RoomFx, null, new Vector3(0f, 0.03f, 0f)));
            Shimmer(roomFx.transform, loop: false);
            roomFx.SetActive(false);
            var wave = Ref(Emitter("Room Wave", null, new Vector3(0f, 0.95f, 0f), Glow("Glow_RoomWave", Hdr(electric, 3f)), ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 1.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 1.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
                main.maxParticles = 420;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = -0.04f;
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 260), new ParticleSystem.Burst(0.25f, 160) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.7f;
                shape.radiusThickness = 0f;            // from the rim, outward in the time wave's plane
                shape.rotation = new Vector3(0f, 20f, 0f);
            }).gameObject);
            wave.SetActive(false);

            // The lab reveal's low impact under the look's sweep (the "look" cue rule plays SFX_Magic_Sweep): a one-shot on the
            // effects bus at gain 0.4 (the sfx cue carries no gain), fired by "show Lab Impact" with the look and hidden
            // again with the room effects so a replay fires it afresh. Loaded by path: the library asset is built after this.
            var impact = Audio("Lab Impact", null, AudioBus.Kind.Sfx);
            impact.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/SFX/CameraTransitions/SFX_Impact_Low.ogg");
            impact.playOnAwake = true;
            impact.GetComponent<AudioBus>().gain = 0.4f;
            impact.gameObject.SetActive(false);
            Ref(impact.gameObject);
        }

        // A spark burst (Scene 4's Sparks kit, slow and short-lived so it stays on the instrument instead of scattering over
        // the room) where an instrument swaps in; "fx Sparks <name>" plays it.
        void SwapSparks(string name, Vector3 at)
        {
            var node = Ref(Node("Sparks " + name, null, at));
            var main = Sparks(node.transform, palette.wattsonAccent, 0.1f).main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.7f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.gravityModifier = 0.15f;
            node.SetActive(false);
        }

        // Scene 4's soft shimmer on the effects bus, quiet under the voice, playing whenever its "[VFX]" object is shown.
        void Shimmer(Transform parent, bool loop)
        {
            var source = Audio("Transformation Sound", parent, AudioBus.Kind.Sfx);
            source.clip = sounds.TryGetValue("Shimmer", out var clip) ? clip : null;
            source.playOnAwake = true;
            source.loop = loop;
            source.GetComponent<AudioBus>().gain = 0.32f;
        }

        // A one-shot burst of soft additive sparks (Scene 4's recipe) inside a box, lifting slowly like dust in the light;
        // `swirl` turns it round its centre (glowing dust round a prop). Hidden until a cue shows it ("fx" replays it).
        GameObject Burst(string name, Transform parent, Vector3 position, Color color, Vector3 box, int count, float speed = 0.5f, float swirl = 0f, float lifetime = 0.9f)
        {
            var ps = Emitter(name, parent, position, Glow("Glow_Burst_" + ColorUtility.ToHtmlStringRGB(color), Hdr(color, 4f)), p =>
            {
                var main = p.main;
                main.loop = false;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.45f, lifetime);
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
                main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.04f);
                main.gravityModifier = -0.06f;
                main.maxParticles = count;
                var emission = p.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
                var shape = p.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = box;
                if (swirl == 0f) return;
                var velocity = p.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.orbitalY = swirl;
            });
            ps.gameObject.SetActive(false);
            return Ref(ps.gameObject);
        }

        // One old prop becomes its modern equivalent where it stands: dust swirls round it, the old prop shrinks out while
        // the new one grows in (they overlap, so something solid is always there), and its label comes up.
        // Audio: the cue rules carry the magic (fx → crackles, vanish → SFX_Magic_Poof, appear → SFX_Magic_Gather); the
        // builder adds only the instrument's click as the modern one grows in. (An explicit Transform here doubled the Poof.)
        void Becomes(string dust, string[] old, string modern, string callout)
        {
            Cue("fx " + dust);
            foreach (var prop in old) Cue("vanish 0.7 " + prop);
            Cue("appear 0.7 " + modern, "", 0.25);
            Cue("fx Sparks " + modern, "", 0.25);
            Sfx("SFX_Apparatus_Click", 0.25);
            Cue("callout show " + callout, "", 0.8);
            Visual(string.Join(" + ", old) + " → " + modern + ", in its own place", 1.0);
        }

        protected override void Beats()
        {
            const double Act = WattsonController.TransformActionDelay;

            // Scene 7's last frame: Ohm at work in his study, the two clues on the slate, Wattson beside the group.
            // (The scene is built in the study look; the only look cue is the room's, so a replay never relights the study.)
            Shot("Study Wide");
            Ambience("StudyTone");
            Music("PadLab");
            Gesture(WattsonController.Gesture.RaiseGadget, point: "Georg Ohm", gadget: true);
            Cue("ohm.look Wattson", "Ohm notices Wattson's gadget");
            Cue("ohm.pose Examine", "Ohm straightens with a copper wire, face to the room, so he freezes upright");
            Wait(0.5);
            Cue("ohm.freeze 1", "Wattson freezes the historical experiment; Ohm holds the pose he is in");
            Show("[VFX] freeze (Ohm holds still)");
            Sfx("Highlight");
            Visual("Wattson freezes Ohm's experiment", 1.0);
            Hide("[VFX] freeze (Ohm holds still)");

            // Attention on the apparatus: in to Ohm's desk while L01 plays; Wattson flies to his spot beside it, in frame.
            Shot("Study Desk");
            Cue("vanish 0.5 Board Voltage", "the slate's headlines would sit cropped under the chapter chip in this frame"); // the question stays
            Cue("vanish 0.5 Board Current");
            Gesture(WattsonController.Gesture.Talking, moveTo: "Transform Point");
            Line("S08_L01");

            // Wattson's gesture starts it; each instrument changes in its own place, one at a time.
            Show(ApparatusFx);
            Gesture(WattsonController.Gesture.Transform, point: "torsion galvanometer", gadget: true);
            Wait(Act);
            Becomes("Dust Galvanometer", new[] { "torsion galvanometer" }, "AMMETER", "ammeter");
            Gesture(WattsonController.Gesture.Point, point: "Thermoelectric Source", gadget: true);
            Becomes("Dust Source", new[] { "Thermoelectric Source" }, Source, "source");
            Gesture(WattsonController.Gesture.Point, point: "Copper Test Wires", gadget: true);
            Cue("fx Spark X", "X and Y light up at the ends of the new wire", 0.9);
            Cue("fx Spark Y", "", 0.9);
            Becomes("Dust Wires", new[] { "Loose Test Wires", "Wire Mount" }, Wire, "wire");
            Wait(0.2);
            Gesture(WattsonController.Gesture.Point, point: "Mercury Cups", gadget: true);
            Becomes("Dust Cups", new[] { "Mercury Cups" }, Key, "key");

            // Ohm's desk becomes today's bench where it stood (the landmark), the instruments settle into the circuit and
            // the voltmeter joins; Ohm steps back and presents it (Scene08_modern_apparatus_presented).
            Gesture(WattsonController.Gesture.Transform, point: "Bench Point", gadget: true);
            Wait(Act);
            Cue("fx Dust Desk");                                                // audio: the dressing's vanish rule plays the Poof once (an explicit Transform doubled it)
            for (int i = 0; i < Dressing.Length; i++) Cue("vanish 0.5 " + Dressing[i], "", i * 0.04);
            Hide("Study Desk", 0.35);
            Show("Bench", 0.35);
            Cue("glide 0.9 Bench", "the bench widens from the desk's footprint to 2.0 x 0.9 m", 0.4);
            Cue("glide 0.6 Georg Ohm", "Ohm steps back from the wider bench", 0.35);
            Cue("ohm.pose Present", "Ohm comes out of the freeze and presents the modern apparatus", 0.4);
            Cue("settle 0.9 " + Source, "", 0.5);
            Cue("settle 0.9 AMMETER", "", 0.5);
            Cue("appear 0.5 Parts Tray", "", 0.65);
            Cue("fx Dust Voltmeter", "", 1.0);
            Cue("appear 0.6 VOLTMETER", "the voltmeter joins, in the tray", 1.1);
            Cue("fx Sparks VOLTMETER", "", 1.1);
            Sfx("Place", 1.1);
            Cue("callout show voltmeter", "", 1.6);
            Show("Lead Key → Source", 1.45);
            Visual("Ohm's desk becomes today's bench in its place; the instruments settle into the circuit; the voltmeter joins", 1.8);
            Hide(ApparatusFx);

            // Hold the final composition and tie it to the lesson: Ohm comes out of the freeze, sees the class, presents.
            Gesture(WattsonController.Gesture.Point, point: "Bench Point");
            Cue("ohm.look Main Camera", "Ohm looks at the class as he presents");
            Line("S08_P01");
            Cue("callout hideall", "notes fade before the pull-back", -0.3);

            // Ohm goes back to his time, from the front-left and closer (the orbit camera's start): Wattson waves him off, Ohm
            // turns to the class, bows his head to the copper wire he takes back with him, and dissolves into gold dust.
            Shot("Bench Orbit");
            Cue("vanish 0.6 Study Easel", "the slate goes as the camera moves: from here it would stand cropped at the frame's left edge");
            Gesture(WattsonController.Gesture.Greet); // waves Ohm off facing the class (looking at Ohm turned his back to the lens)
            Cue("ohm.look Main Camera", "Ohm turns to the class");
            Wait(0.7);
            Cue("ohm.look", "his eyes drop to the copper wire he takes back with him");
            Cue("ohm.pose Examine", "a small bow: the dip (the hand wire shows only while he is down)");
            Cue("ohm.pose Present", "and up again, to the class", 0.7);
            Cue("ohm.look Main Camera", "", 0.7);
            Wait(1.0);
            Cue("fx Ohm Farewell Dust", "", 0.2);
            Sfx("Reveal", 0.2);
            Cue("vanish 1.0 Georg Ohm", "Ohm dissolves back to 1826", 0.4);
            Line("S08_P02", 0.2);

            // The room, with the camera on it the whole time: Wattson's gadget starts it, and the study becomes the classroom
            // lab, sweeping out from the bench.
            Gesture(WattsonController.Gesture.Transform, point: "Bench Point", gadget: true);
            Wait(Act);
            Show(RoomFx);                                                       // audio: the cast carries Wattson's magic (WP-W) and the wave's Gather (appear rule); the sweep itself plays at the look below
            Cue("appear 1.8 Time Wave", "the time wave: a ring of light grows out from the bench, past the walls");
            Cue("fade 0.12 0.1", "a brief exposure lift at the wave"); // ponytail: a 12 % white veil stands in for an exposure override; a Volume weight ramp if it reads as a fade
            Cue("fade 0 0.8", "", 0.25);
            Cue("fx Room Wave");
            Wait(0.6);                                                          // the ring stands over the bench on its own before the room answers (audit: it showed for one frame)
            Ambience("RoomTone", 1.0);
            Music(Scene10Builder.LabTheme(sounds));                             // the lab's own bed from the reveal on (PadLabTheme → the Wattson theme through the library alias)
            Cue("look 2.8 classroom", "light, ambient, fog, background, post and reflections crossfade to the classroom");
            Show("Lab Impact");                                                 // audio: SFX_Impact_Low at gain 0.4 under the look rule's SFX_Magic_Sweep (the old explicit LabSweep 0.6 s earlier doubled it)
            Cue("glide 3.4 Cam Bench Orbit", "the camera sweeps 20 degrees round the bench and pulls back onto Bench Wide's pose");
            Shot("Bench Wide", offset: 3.0);                                     // blends in over the glide's last second: Scene 9's first frame
            Hide("Time Wave", 2.0);                                             // 2.6 s after it appeared: full size for the look's first 0.8 s
            Gesture(WattsonController.Gesture.Idle, moveTo: "Wattson Home", offset: 0.6); // after the thrust, over to where Scene 9 starts him
            Cue("spread 1.2 Classroom Floor", "", 0.1);
            Cue("retract 0.9 Study Back", "", 0.35);
            Cue("rise 1.0 Classroom Back", "", 0.55);
            Cue("retract 0.9 Study Window Side", "", 0.7);
            Cue("retract 0.9 Study Door Side", "", 0.7);
            Cue("rise 1.0 Classroom Left", "", 0.9);
            Cue("rise 1.0 Classroom Right", "", 0.9);
            Cue("vanish 0.8 Study Ceiling", "", 1.2);
            Cue("spread 0.9 Classroom Ceiling", "", 1.3);
            Hide("Study Floor", 1.4);
            Hide("Historical Lab", 2.9);
            Hide("Ohm Apparatus", 2.9);
            Hide(RoomFx, 2.9);
            Hide("Lab Impact", 2.9);
            Visual("Ohm's study sweeps away from the bench into today's classroom lab", 1.2);
            Gesture(WattsonController.Gesture.Talking);
            Line("S08_L02", 0.2);
            Cue("handoff", "SceneHandoffState: camera pose + CircuitState");
            Handoff("Scene08Complete", cut: true, afterSeconds: 0.3);
        }
    }
}
