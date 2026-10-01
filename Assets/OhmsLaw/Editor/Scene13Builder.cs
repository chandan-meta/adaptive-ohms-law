using TMPro;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 13: The Law Gets Its Name. V, I, R combine into V = IR; the adaptive checkpoint; hand-off to the video.
    // Staging: the classroom with the bench and the assembled rig, Scene 12's table and graph kept on the teaching board
    // for the match-cut, then the "Cards" frame: the Boards camera pulled back and up so the board fills the middle of
    // the frame and the hero cards float above the bench in front of it.
    public class Scene13Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene13";
        public override string SceneName => "Scene13_OhmsLaw";
        public override string Title => "The Law Gets Its Name";

        // The hero row and title hang 0.4 m behind the bench's centre line, 3.9-4.1 m from the Cards lens (the depth the
        // LetterCard / HeroFormula / Title tiers are tuned for). From (0, 2.65, -3.5) the title's backdrop is the dark board
        // (its cap top lands at board y 2.43), the row sits in front of the canvases, the bench stays under the subtitle band.
        // Row x is shifted 0.1 m right so the boosted V card (1.5x, 1.1 m wide) clears Wattson at Cards Point by 3 % of the frame.
        static readonly Vector3 CardsCamC = new Vector3(0f, 2.65f, -3.5f), CardsLookC = new Vector3(0f, 1.9f, 0.4f);
        static readonly Vector3 WattsonCamC = new Vector3(-0.1f, 2.55f, -3.2f), WattsonLookC = new Vector3(-0.3f, 1.95f, 0.4f);
        const float CardsFov = 40f;
        const float RowY = 1.75f, RowZ = 0.4f, TitleY = 2.35f, LetterWrap = 1.3f, IgniteSeconds = 1.2f;
        static readonly float[] RowX = { -1.2f, 0.1f, 1.4f };
        // Wattson: lower-left third for the cards (8-14 % across, 24-44 % up, clear of the V card and the subtitle band);
        // for the last line he comes forward to 2 m from the Wattson lens (23-36 % across, 39 % of the frame tall, plain wall behind).
        static readonly Vector3 CardsPoint = new Vector3(-1.95f, 1.5f, 0.2f), FrontPoint = new Vector3(-0.71f, 2.2f, -1.37f);
        // Scene 12's last frame (Scene12Builder: BoardSide, AboveGraph, AboveTable, BoardCard's 1.6 cm lift) - change together.
        static readonly Vector3 Scene12WattsonC = new Vector3(-1.48f, 1.5f, 2.2f);
        const float Scene12OmegaV = 0.91f, Scene12OneOhmV = 0.867f, Scene12CardLift = 0.016f;

        protected override void Dialogue()
        {
            L("S13_L01", "So now we know all three pieces.", 2.4f);
            L("S13_L02", "The push.", 1.2f);
            L("S13_L03", "The flow.", 1.2f);
            L("S13_L04", "And the opposition.", 1.6f);
            L("S13_L05", "And the relationship connecting them...", 2.6f);
            L("S13_L06", "And today... we call it OHM'S LAW.", 3.0f);
            L("S13_L07", "We've got our law. Now let's test it against the mystery that started all of this.", 4.5f);
            // Adaptive checkpoint. AQ1 and AQ2 are storyboard questions (AQ1's "vo ltage" typo fixed); the rest is proposed.
            L("AQ1", "If resistance stays constant and voltage doubles, what happens to current?", 4.5f);
            L("AQ2", "Two conductors carry the same current. One requires a larger potential difference. Which has greater resistance?", 6.5f);
            L("AQ_EXPLAIN", "Remember the table, detective: every time V went up, I went up with it, and V divided by I stayed at 5.", 6.5f, proposed: true);
            L("AQ_EASY", "When the potential difference across the nichrome wire went up, what happened to the current?", 5.0f, proposed: true);
            L("AQ_GOOD", "Case closed, detective.", 1.8f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 12: voltage V (the push, volt), current I (the flow, ampere), resistance R (the opposition, ohm), V ∝ I for the nichrome wire at constant temperature, V/I constant, slope = R, 1 Ω = 1 V / 1 A.",
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
            director.matchCutFrom = "Scene12_Resistance";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            Boards(withTable: true, withGraph: true, unitsInTable: true); // Scene 12's table and graph stay on the board for the match-cut
            Wattson(Scene12WattsonC, BoardsCamC);                           // where Scene 12 leaves him
            CarriedOver();

            Marker("Cards Point", CardsPoint);
            var front = Marker("Front Point", FrontPoint).transform;
            var focus = Marker("Cards Focus", new Vector3(0f, RowY, RowZ)).transform;
            Vcam("Cards", CardsCamC, CardsLookC, CardsFov, focus, lensShallow);      // the board and the bench soften behind the cards
            Vcam("Wattson", WattsonCamC, WattsonLookC, CardsFov, front, lensMedium); // L07: a small drift toward him and a rack-focus

            // The three pieces: letter with its name as the 36 % sub-line (wrapped to two lines under the V), then the
            // formula they combine into, coloured by piece, and the title above it. Each faces the Cards lens.
            var pieces = new[] { ("V", "Potential Difference", palette.voltage), ("I", "Current", palette.current), ("R", "Resistance", palette.resistance) };
            for (int i = 0; i < pieces.Length; i++)
            {
                var (letter, name, color) = pieces[i];
                Face(Card(letter, WorldText.Sub(letter, name, WorldText.Cap.LetterCard, WorldText.Cap.LetterSub), color, null, new Vector3(RowX[i], RowY, RowZ), WorldText.Cap.LetterCard, false, LetterWrap), CardsCamC);
            }
            Face(Card("Law", Tint("V", palette.voltage) + " = " + Tint("I", palette.current) + Tint("R", palette.resistance), Color.white, null, new Vector3(0f, RowY, RowZ), WorldText.Cap.HeroFormula), CardsCamC);
            var titlePos = new Vector3(0f, TitleY, RowZ);
            var title = Card("Title", "OHM'S LAW", Color.white, null, titlePos, WorldText.Cap.Title);
            Face(title, CardsCamC);
            // The same words, chalky and translucent: swapped in while the checkpoint panel is up so the title reads as dimmed.
            Face(Card("Title Dim", "OHM'S LAW", new Color(Chalk.r, Chalk.g, Chalk.b, 0.45f), null, titlePos, WorldText.Cap.Title, WorldText.Look.Plain), CardsCamC);
            TitleIgnite(title);
        }

        static string Tint(string text, Color color) => "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";

        // Scene 12's last two cards, for the match-cut: the Ω glyph above the graph and the one-ohm card above the table,
        // built exactly as Scene12Builder.BoardCard builds them (on the Boards Set, lying on the chalk face), visible from
        // frame 0 and already settled: no pop, no sparks. The camera move hides them.
        void CarriedOver()
        {
            var set = refs["Boards Set"].transform;
            var (omegaAt, rotation) = BoardSurface(new Vector2(GraphBoardUV.x, Scene12OmegaV), Scene12CardLift);
            var omega = Card("Ohm Symbol", "Ω", palette.resistance, set, omegaAt, WorldText.CapAt(WorldText.Px.Hero, BoardCentreC, BoardsCamC, BoardsLookC, BoardsFov), WorldText.Look.Glow, true, sparks: false);
            omega.transform.rotation = rotation;
            var pop = omega.transform.Find("Text").GetComponent<PopIn>();
            pop.fromScale = 1f;   // keeps the glow word's bob, skips the pop
            pop.overshoot = 1f;
            var (oneOhmAt, _) = BoardSurface(new Vector2(TableBoardUV.x, Scene12OneOhmV), Scene12CardLift);
            Card("One Ohm", "1 Ω = 1 V / 1 A", palette.resistance, set, oneOhmAt, BoardsShotTitleCap, WorldText.Look.Panel, true).transform.rotation = rotation;
        }

        // The storyboard's "VFX title reveal": sparks run along the title's own baseline from one end to the other in one
        // sweep, so the letters read as igniting left to right. A child of the title card, so it rides its Face() and
        // position; it keeps the "[VFX] title reveal" name, so the existing cues show and hide it and its screen panel.
        void TitleIgnite(GameObject title)
        {
            var bounds = title.transform.Find("Text").GetComponent<TextMeshPro>().textBounds;
            var ignite = Emitter("[VFX] title reveal", title.transform, new Vector3(bounds.center.x, bounds.min.y + WorldText.Cap.Title * 0.2f, -0.02f),
                Glow("Glow_Spark_" + ColorUtility.ToHtmlStringRGB(Color.white), Hdr(Color.white, 4f)), ps =>
                {
                    var main = ps.main;
                    main.loop = false;
                    main.duration = IgniteSeconds;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
                    main.gravityModifier = 0.5f;
                    var emission = ps.emission;
                    emission.rateOverTime = 260f;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.SingleSidedEdge; // emits up through the letters
                    shape.radius = bounds.extents.x;
                    shape.radiusMode = ParticleSystemShapeMultiModeValue.Loop;  // the emission point travels the edge once per sweep
                    shape.radiusSpeed = 1f / IgniteSeconds;
                    shape.randomDirectionAmount = 0.35f;
                });
            ignite.gameObject.SetActive(false);
            Ref(ignite.gameObject);
        }

        protected override void Beats()
        {
            Shot("Boards");
            Cue("table.rows 5", "match-cut from Scene 12: the board as it left it (5 rows with units, V/I column glowing)");
            Cue("table.ratio 1"); Cue("table.glow 2 1");
            Cue("graph.axes 1"); Cue("graph.plot 5"); Cue("graph.line 1"); Cue("graph.triangle 0 2");
            Visual("Match-cut from Scene 12: table, V/I column and graph on the board", 0.6);
            Shot("Cards");
            Gesture(WattsonController.Gesture.Flick, moveTo: "Cards Point");
            Hide("Card Ohm Symbol"); Hide("Card One Ohm");
            Cue("table.clear", "the boards quieten behind the cards: no glow, highlights or triangle"); Cue("graph.clear"); Cue("graph.triangle -1 0");
            Show("Card V", 0.6); Show("Card I", 0.85); Show("Card R", 1.1);
            Visual("Three cards float: V — Potential Difference (blue), I — Current (amber), R — Resistance (coral)", 1.2);
            Line("S13_L01");
            Gesture(WattsonController.Gesture.Point, point: "Card V");
            Cue("boost Card V", "V highlights");
            Line("S13_L02");
            Gesture(WattsonController.Gesture.Point, point: "Card I");
            Cue("boost Card I", "I highlights");
            Line("S13_L03");
            Gesture(WattsonController.Gesture.Point, point: "Card R");
            Cue("boost Card R", "R highlights");
            Line("S13_L04");
            Gesture(WattsonController.Gesture.Talking);
            Line("S13_L05", 0.2);
            Hide("Card V"); Hide("Card I"); Hide("Card R"); Show("Card Law");
            Visual("The three cards combine into V = IR", 1.2);
            Line("S13_L06", 0.1);
            Show("Card Title", -1.0);
            Show("[VFX] title reveal", -1.0);
            Visual("OHM'S LAW title card: the letters ignite with sparks", 1.5);
            Hide("[VFX] title reveal");
            Hide("Card Title", -0.2); Show("Card Title Dim", -0.2); // the title dims under the checkpoint panel
            Gate("S13_Checkpoint", "Adaptive checkpoint (checkpointEnabled, placement note on SceneDirector): AQ1 → correct: AQ_GOOD, AQ2 (harder) → done; wrong: AQ_EXPLAIN, AQ_EASY → done. Results in LearnerSession.checkpoint.",
                "AQ1", "AQ2", "AQ_EXPLAIN", "AQ_EASY", "AQ_GOOD");
            Hide("Card Title Dim"); Show("Card Title");             // and pops back for the last line
            Gesture(WattsonController.Gesture.Greet, moveTo: "Front Point");
            Shot("Wattson");
            Line("S13_L07", 0.3);
            Handoff("Scene13Complete", cut: false, afterSeconds: 0.3);
        }
    }
}
