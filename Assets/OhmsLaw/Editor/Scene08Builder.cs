using TMPro;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 8: From Ohm's Lab to Our Lab. The old apparatus becomes today's instruments, one item at a time.
    // Classroom frame (ART_DIRECTION §2): opens on Scene 7's last frame (Bench Wide, oak desk, Ohm at work), pushes in to
    // Bench Present for the swap, and ends there with Ohm presenting the modern bench (Scene08_modern_apparatus_presented).
    public class Scene08Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene08";
        public override string SceneName => "Scene08_OurLab";
        public override string Title => "From Ohm's Lab to Our Lab";

        // The parts that appear one per 0.8 s; each pops up on its cue (PopIn) with a spark burst and its callout.
        static readonly string[] NewParts = { "AMMETER", "VOLTMETER", "NICHROME WIRE — XY", "Card X", "Card Y", "cells / controllable DC source", "switch / key" };

        protected override void Dialogue()
        {
            L("S08_L01", "Ohm didn't have the modern instruments sitting in your school laboratory today.", 4.5f);
            L("S08_L02", "So let's investigate his relationship using the instruments you would use today.", 4.5f);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 7: voltage as the push (V, volt), current as charge per second (I, ampere), Ohm's repeated 1826 investigation and his apparatus.",
                "An ammeter measures current (I, ampere). A voltmeter measures potential difference (V, volt).",
                "The nichrome wire XY is the test conductor; X and Y are its two ends.",
                "A variable DC source (cells) sets the potential difference; a key (switch) opens and closes the circuit.",
            },
            new[] { "How the ammeter and voltmeter are connected (series and parallel, Scene 9)", "The pattern between V and I", "V is proportional to I", "V = IR", "Resistance", "Ohms", "The Christmas-lights answer" },
            ("What is nichrome?", "Nichrome is an alloy, a metal mixture of mostly nickel and chromium. It is tough, it does not rust, and it behaves very steadily in experiments like ours, which is exactly why school labs use a nichrome wire as the test conductor."),
            ("What does a voltmeter measure?", "A voltmeter measures the potential difference, the voltage, between two points. In our circuit it will tell us the push across the two ends of the nichrome wire, X and Y, in volts."),
            ("Why not use Ohm's old instruments?", "We could, but Ohm's torsion galvanometer needed a steady hand and a lot of patience, and his thermoelectric source gave only a tiny push. Today's ammeter and voltmeter give clear numbers straight away, so we can see the relationship much more clearly."));

        protected override void Stage()
        {
            ClassroomSet(modernBench: false, audience: true);   // starts on the oak desk; the swap cues Hide("Old Table"); Show("Bench")
            ClassroomApparatus();
            Wattson(WattsonHomeC, BenchWideCamC);
            director.matchCutFrom = "Scene07_BackToOhm";
            // Scene 7 ends with the three measurement pages on the notebook: same first frame here, without their pop-in.
            foreach (var page in new[] { "Page 1", "Page 2", "Page 3" })
            {
                Object.DestroyImmediate(refs[page].GetComponent<PopIn>());
                refs[page].SetActive(true);
            }

            var rig = Rig(assembled: false, keyClosed: false, voltage: 0, active: false);
            director.rig = rig;
            // The modern parts are hidden until their cue, then scale up (the imported meters are 4 cm apart in the tray, so a soft overshoot).
            foreach (var part in NewParts)
            {
                refs[part].SetActive(false);
                refs[part].AddComponent<PopIn>().overshoot = 1.1f;
            }

            // Labels (ART_DIRECTION §4), placed for the Bench Present frame: source at mid-left over the source; the three
            // right-hand parts stack top-right (key, voltmeter, ammeter) so each leader drops straight to its part without
            // crossing another panel - the key's runs down the right edge to the lever tip, the meters' to a top corner of
            // each dial, clear of the readouts; the wire's panel hangs just above the rod between Ohm's open hands.
            Callout("source", refs["cells / controllable DC source"], CalloutDef.Anchor.MidLeft, new Vector3(0f, 0.10f, -0.07f));
            Callout("ammeter", refs["AMMETER"], CalloutDef.Anchor.AboveTarget, new Vector3(-0.11f, 0.06f, -0.17f)).aboveOffset = new Vector2(120f, 34f);
            Callout("voltmeter", refs["VOLTMETER"], CalloutDef.Anchor.AboveTarget, new Vector3(0.11f, 0.06f, -0.17f)).aboveOffset = new Vector2(-210f, 186f);
            Callout("wire", refs["NICHROME WIRE — XY"], CalloutDef.Anchor.AboveTarget).aboveOffset = new Vector2(0f, 60f);
            Callout("key", refs["switch / key"], CalloutDef.Anchor.TopRight, new Vector3(0.115f, 0.095f, 0f));

            // The board (ART_DIRECTION §3): the heading and the three instrument lines, left-aligned down the left half so they
            // stay clear of Ohm's head (board u 0.44-0.56 below v 0.39 from Bench Present), the HUD (u > 0.55 above v 0.69)
            // and the top-left VFX panel. Heading 40 px / items 28 px from Bench Present (28 / 20 from Bench Wide).
            float itemCap = WorldText.CapAt(WorldText.Px.Medium, BoardCentreC, BenchPresentCamC, BenchPresentLookC, BenchPresentFov);
            Board("Our instruments today", Chalk, new Vector2(0.04f, 0.76f), BoardCap, 0f, TextAlignmentOptions.Left, "Instruments");
            Board("AMMETER", palette.current, new Vector2(0.04f, 0.64f), itemCap, 0f, TextAlignmentOptions.Left, "Ammeter");
            Board("VOLTMETER", palette.voltage, new Vector2(0.04f, 0.545f), itemCap, 0f, TextAlignmentOptions.Left, "Voltmeter");
            Board("NICHROME WIRE — XY", Chalk, new Vector2(0.04f, 0.45f), itemCap, 0f, TextAlignmentOptions.Left, "Nichrome");

            // Effects (named "[VFX] ..." so the screen panel comes up with them). Freeze: a frost burst over Ohm's bent torso.
            Burst("[VFX] freeze (Ohm holds still)", null, OhmFeetC + new Vector3(0f, 1.0f, -0.12f), palette.charge, new Vector3(0.6f, 0.8f, 0.5f), 80, 0.2f);
            // Swap: one spark burst where each prop changes, children of one panel object shown for the whole sequence.
            var swap = Ref(Node("[VFX] prop swap"));
            var cyan = palette.wattsonAccent;
            Burst("Swap Sparks Ammeter", swap.transform, new Vector3(0.5f, 0.95f, 0.15f), cyan, new Vector3(0.4f, 0.3f, 0.35f), 60);     // galvanometer -> tray
            Burst("Swap Sparks Voltmeter", swap.transform, new Vector3(0.84f, 0.95f, 0.21f), cyan, new Vector3(0.3f, 0.3f, 0.35f), 50);
            Burst("Swap Sparks Wire", swap.transform, new Vector3(-0.1f, 0.86f, -0.2f), cyan, new Vector3(1.1f, 0.06f, 0.14f), 70);        // copper wires -> XY
            Burst("Swap Sparks X", swap.transform, new Vector3(-0.306f, 0.865f, -0.168f), palette.current, Vector3.one * 0.08f, 30);
            Burst("Swap Sparks Y", swap.transform, new Vector3(0.394f, 0.865f, -0.168f), palette.current, Vector3.one * 0.08f, 30);
            Burst("Swap Sparks Source", swap.transform, new Vector3(-0.7f, 0.92f, 0.16f), cyan, new Vector3(0.45f, 0.3f, 0.35f), 60);      // thermoelectric -> DC
            Burst("Swap Sparks Cups", swap.transform, new Vector3(-0.26f, 0.85f, 0.15f), cyan, new Vector3(0.45f, 0.1f, 0.1f), 40);        // mercury cups -> key
            Burst("Swap Sparks Key", swap.transform, new Vector3(0.75f, 0.88f, -0.18f), cyan, new Vector3(0.3f, 0.2f, 0.15f), 40);
            Burst("Swap Sparks Bench", swap.transform, new Vector3(0f, 0.82f, 0f), cyan, new Vector3(2.0f, 0.02f, 0.9f), 150, 0.25f);       // the top's material changes
            swap.SetActive(false);
        }

        // A one-shot burst of soft additive sparks (Scene 4's recipe) inside a box, lifting slowly like dust in the light.
        // Hidden until a cue shows it; the particle system plays once on activation.
        GameObject Burst(string name, Transform parent, Vector3 position, Color color, Vector3 box, int count, float speed = 0.5f)
        {
            var ps = Emitter(name, parent, position, Glow("Glow_Burst_" + ColorUtility.ToHtmlStringRGB(color), Hdr(color, 4f)), p =>
            {
                var main = p.main;
                main.loop = false;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);   // done before the 1 s freeze cue hides it
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
            });
            ps.gameObject.SetActive(false);
            return Ref(ps.gameObject);
        }

        protected override void Beats()
        {
            Shot("Bench Wide");                                            // Scene 7's last frame: Ohm at work, Wattson at home
            Gesture(WattsonController.Gesture.RaiseGadget, point: "Georg Ohm", gadget: true);
            Wait(0.5);
            Cue("ohm.freeze 1", "Wattson freezes the historical experiment; Ohm holds the pose he is in");
            Show("[VFX] freeze (Ohm holds still)");
            Visual("Wattson freezes Ohm's experiment", 1.0);
            Hide("[VFX] freeze (Ohm holds still)");
            Gesture(WattsonController.Gesture.Talking);
            Line("S08_L01");

            Shot("Bench Present");                                         // push in (1 s blend): instrument row, Ohm's head, the board
            Gesture(WattsonController.Gesture.Point, point: "Bench Point", gadget: true);
            Wait(0.6);
            Show("Circuit Rig");
            Show("[VFX] prop swap");
            Show("Board Instruments");
            Visual("Wattson's gadget starts the transformation; the board heading 'Our instruments today' appears", 0.4);
            // One item every 0.8 s: the old prop goes in a burst of sparks, the new one pops up with its callout and its chalk line.
            Hide("torsion galvanometer"); Show("Swap Sparks Ammeter"); Show("AMMETER"); Cue("callout show ammeter"); Show("Board Ammeter");
            Visual("galvanometer → AMMETER (in the parts tray) with its label", 0.8);
            Show("Swap Sparks Voltmeter"); Show("VOLTMETER"); Cue("callout show voltmeter"); Show("Board Voltmeter");
            Visual("VOLTMETER appears with its label", 0.8);
            Hide("Copper Test Wires"); Show("Swap Sparks Wire"); Show("NICHROME WIRE — XY"); Cue("callout show wire"); Show("Board Nichrome");
            Show("Card X", 0.35); Show("Card Y", 0.35); Show("Swap Sparks X", 0.35); Show("Swap Sparks Y", 0.35);
            Visual("copper wires fade; one straight NICHROME WIRE — XY appears, X and Y light up", 0.8);
            Hide("Thermoelectric Source"); Show("Swap Sparks Source"); Show("cells / controllable DC source"); Cue("callout show source");
            Visual("cells / controllable DC source appears", 0.8);
            Hide("Mercury Cups"); Show("Swap Sparks Cups"); Show("Swap Sparks Key"); Show("switch / key"); Cue("callout show key");
            Visual("switch / key appears", 0.8);
            Hide("Old Table"); Show("Bench"); Show("Swap Sparks Bench"); Hide("Ohm Apparatus");
            Cue("ohm.pose Present", "Ohm comes out of the freeze and presents the modern apparatus (the reference frame)");
            Visual("wooden table → clean modern bench; the lab around stays historical", 0.8);
            Hide("[VFX] prop swap");

            Gesture(WattsonController.Gesture.Talking);
            Line("S08_L02", 0.2);
            Cue("callout hideall", "labels fade before the match-cut", -0.5);
            Cue("handoff", "SceneHandoffState: camera pose + CircuitState");
            Handoff("Scene08Complete", cut: true, afterSeconds: 0.3);
        }
    }
}
