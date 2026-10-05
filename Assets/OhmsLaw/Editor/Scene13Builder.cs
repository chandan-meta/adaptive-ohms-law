using TMPro;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Scene 13: The Law Gets Its Name. A recap and an application, not a quiz: V, I and R combine into V = IR and the law
    // gets its name; one adaptive question (storyboard AQ2); then the law is put to work on the real circuit (S13_Apply:
    // make the ammeter read 0.80 A) and a recap that reacts to how that went; hand-off to the video.
    // Staging: the classroom with the bench and the assembled rig. Scene 12's last frame (table, graph, Ω cards) holds for
    // the match-cut, then the board empties as the "Cards" frame pulls back (the Boards camera pulled back and up: the
    // board fills the middle of the frame, the hero cards float above the bench in front of it). "Question" is the same
    // frame slid right, so V = IR and the title stay lit on the left while the 640 px question panel docks right. "Apply"
    // frames the whole investigation at once: the bench meters, the graph with its line and live point, the readings strip
    // above it and V = IR on the board. The last line plays in the "Wattson" frame, the title and formula behind him.
    public class Scene13Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene13";
        public override string SceneName => "Scene13_OhmsLaw";
        public override string Title => "The Law Gets Its Name";

        // The hero row and title hang 0.4 m behind the bench's centre line, 3.9-4.1 m from the Cards lens (the depth the
        // LetterCard / HeroFormula tiers are tuned for). From (0, 2.65, -3.5) the board spans 29-71 % across: the title at
        // 0.65 of the Title tier (~1.9 m wide, 32-68 %, ~80 px caps) sits inside it with its cap top ~0.15 m under the board's
        // top edge, 0.25 m above the formula. Every hero card is turned parallel to the Cards lens (no roll off-centre).
        static readonly Vector3 CardsCamC = new Vector3(0f, 2.65f, -3.5f), CardsLookC = new Vector3(0f, 1.9f, 0.4f);
        static readonly Vector3 WattsonCamC = new Vector3(-0.1f, 2.55f, -3.2f), WattsonLookC = new Vector3(-0.3f, 1.95f, 0.4f);
        const float CardsFov = 40f;
        const float RowY = 1.75f, RowZ = 0.4f, TitleY = 2.25f, LetterWrap = 1.3f, IgniteSeconds = 1.2f;
        const float TitleCap = WorldText.Cap.Title * 0.65f;
        static readonly float[] RowX = { -1.2f, 0.1f, 1.4f };
        // Question: the Cards camera slid 0.75 m right. V = IR spans 25-46 % across, the title 17-54 %, Wattson at
        // "Question Point" ~9 %; the S13_AQ2 panel (640 px, docked right) starts at ~64 %, over the wall.
        static readonly Vector3 QuestionShift = new Vector3(0.75f, 0f, 0f);
        // Apply (16:9, FOV 28, ~385 px/m on the board): graph 52-80 % x 41-78 %, the readings strip above it up to ~86 %
        // (clear of the top-right controls at 88.5 %), the ammeter and voltmeter readouts at 46 % / 60 % x 28 % (above the
        // subtitle band, right of the battery slider), V = IR on the board at 32-48 % x 72 % (right of the docked Ask
        // panel), Wattson at "Apply Point" ~21 % x 56 % (above the slider; the Ask panel takes his side of the frame).
        // Lens_Wide focused at ~4.2 m keeps the meters (3.2 m) and the board (5.6 m) sharp.
        static readonly Vector3 ApplyCamC = new Vector3(-0.4f, 1.4f, -3.0f), ApplyLookC = new Vector3(0f, 1.43f, 2.55f);
        const float ApplyFov = 28f;
        static readonly Vector3 ApplyPoint = new Vector3(-1.2f, 1.55f, 1.4f), ApplyFocus = new Vector3(0.3f, 1.5f, 1.2f);
        static readonly Vector2 LawBoardUV = new Vector2(0.35f, 0.72f);
        // Wattson: lower-left third for the cards (8-14 % across, 24-44 % up, clear of the V card and the subtitle band);
        // for the last line he comes forward to 2 m from the Wattson lens (23-36 % across, 39 % of the frame tall, plain wall behind).
        static readonly Vector3 CardsPoint = new Vector3(-1.95f, 1.5f, 0.2f), FrontPoint = new Vector3(-0.71f, 2.2f, -1.37f);
        static readonly Vector3 QuestionPoint = new Vector3(-1.3f, 1.5f, 0.2f);
        // Scene 12's last frame (Scene12Builder: BoardSide, AboveGraph, AboveTable, BoardCard's 1.6 cm lift, the Ω at
        // Px.DataCard x 1.6 on the Boards shot, the bench left at 2 V by its dive) - change together.
        static readonly Vector3 Scene12WattsonC = new Vector3(-1.48f, 1.5f, 2.2f);
        const float Scene12OmegaV = 0.935f, Scene12OneOhmV = 0.867f, Scene12CardLift = 0.016f, Scene12Volts = 2f;

        protected override void Dialogue()
        {
            L("S13_L01", "So now we know all three pieces.", 2.4f);
            L("S13_L02", "The push.", 1.2f);
            L("S13_L03", "The flow.", 1.2f);
            L("S13_L04", "And the opposition.", 1.6f);
            L("S13_L05", "And the relationship connecting them...", 2.6f);
            L("S13_L06", "And today... we call it OHM'S LAW.", 3.0f);
            L("S13_L07", "We've got our law. Now let's test it against the mystery that started all of this.", 4.5f);
            // S13_AQ2: the storyboard question (spoken while the panel is up) and the storyboard's proposed right-answer line
            // (no S13_AQ2_FA, so AQ_GOOD answers a right pick); the rest is proposed (WOW_PLAN §8 gate contract).
            L("AQ2", "Two conductors carry the same current. One requires a larger potential difference. Which has greater resistance?", 6.5f);
            L("AQ_GOOD", "Case closed, detective.", 1.8f, proposed: true);
            L("S13_AQ2_FB", "Careful: needing more push means it resists more, not less.", 3.6f, proposed: true);
            L("S13_AQ2_FC", "Same current, different pushes, so their resistances can't match.", 3.6f, proposed: true);
            L("S13_AQ2_H", "Think R = V divided by I. Same I for both, so compare V.", 4.2f, proposed: true);
            L("S13_AQ2_X", "Same current, bigger push needed: that one opposes more. Bigger V, bigger R.", 4.6f, proposed: true);
            // S13_Apply: the law at work on the real circuit. Every value a line quotes is the gate's own: R is the wire's
            // 5 Ω, a hit is 0.80 A, which on this wire and the slider's whole volts is 4 V; _X plays while Wattson sets 4 V.
            L("S13_A01", "Laws are for using. Your turn, and ask me if you get stuck.", 3.6f, proposed: true);
            L("S13_Apply_GO", "Use V = IR: set the voltage so the current is 0.80 amperes.", 4.4f, proposed: true);
            L("S13_Apply_H", "R is 5 ohms. V = I times R: 0.80 times 5 is…?", 4.2f, proposed: true);
            L("S13_Apply_X", "0.80 amperes times 5 ohms is 4 volts. There: 0.80 amperes.", 4.2f, proposed: true);
            L("S13_Apply_OK", "4 volts gives 0.80 amperes. V = IR works.", 3.2f, proposed: true);
            // S13_Recap (say gate): one line, the first whose condition holds for this learner (LearnerSession.Holds).
            L("S13_Recap_If_Struggling_OhmsLaw", "The trick: amperes times ohms gives the volts you need. Every time.", 3.8f, proposed: true);
            L("S13_Recap_If_Struggling_Resistance", "Remember: more push for the same current means more resistance. R = V ÷ I.", 4.6f, proposed: true);
            L("S13_Recap", "Push, flow, opposition: know any two, and V = IR gives you the third.", 4.4f, proposed: true);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 12: voltage V (the push, volt), current I (the flow, ampere), resistance R (the opposition, ohm), V ∝ I for the nichrome wire at constant temperature, V/I constant, slope = R, 1 Ω = 1 V / 1 A.",
                "Ohm's law: V = IR. The potential difference across a conductor equals the current through it times its resistance, at constant temperature.",
                "The closing activity (S13_Apply): the learner sets the battery so the ammeter reads 0.80 A, using V = IR with the nichrome wire's R = 5 Ω. While it runs, hint the reasoning (R is 5 ohms; V = I times R) and let the learner find the voltage.",
                "If asked about the Christmas lights, give only a hint: think about which battery could keep a bigger potential difference across the lights.",
            },
            // The S13_AQ2 answer and the S13_Apply voltage are withheld by the runtime's [App rule] only while each activity runs
            // (WattsonAgent.ActivityRule); a doNotSpoil entry would be sent for the whole scene and refuse them afterwards.
            new[] { "The full explanation of the Christmas-lights mystery (the Scene 14 video does that)" },
            ("What does V = IR mean?", "It says the potential difference across a conductor equals the current through it multiplied by its resistance. Our nichrome wire: 5 ohms times 0.2 ampere gives 1 volt, 5 ohms times 1 ampere gives 5 volts. Three clues, one relationship."),
            ("Can I use Ohm's law for any wire?", "For metal wires at a steady temperature, yes, and that covers most of what you meet in a school lab. Some things, like bulb filaments that heat up, or diodes, do not keep a constant resistance, so their V-I graph is not a straight line. Scientists call the well-behaved ones ohmic conductors."),
            ("Who was Ohm?", "Georg Simon Ohm was a German teacher and physicist, born in 1789. He built much of his own equipment, measured patiently for years, and published this relationship in 1827. The unit of resistance, the ohm, is named in his honour."));

        protected override void Stage()
        {
            ClassroomSet(modernBench: true, audience: false);
            director.matchCutFrom = "Scene12_Resistance";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: Scene12Volts); // a direct jump starts where Scene 12 leaves the bench
            director.rig.showPath = false;                                              // off since Scene 9's "rig.path 0": plain cables in the Apply frame
            Boards(withTable: true, withGraph: true, unitsInTable: true); // Scene 12's table and graph: the match-cut, then S13_Apply's graph
            Wattson(Scene12WattsonC, BoardsCamC);                           // where Scene 12 leaves him
            CarriedOver();

            Marker("Cards Point", CardsPoint);
            Marker("Question Point", QuestionPoint);
            Marker("Apply Point", ApplyPoint);
            var front = Marker("Front Point", FrontPoint).transform;
            var focus = Marker("Cards Focus", new Vector3(0f, RowY, RowZ)).transform;
            var applyFocus = Marker("Apply Focus", ApplyFocus).transform;
            Vcam("Cards", CardsCamC, CardsLookC, CardsFov, focus, lensShallow);      // the bench softens behind the cards
            Vcam("Question", CardsCamC + QuestionShift, CardsLookC + QuestionShift, CardsFov, focus, lensShallow);
            Vcam("Apply", ApplyCamC, ApplyLookC, ApplyFov, applyFocus, lensWide);   // meters, graph and readings all sharp
            Vcam("Wattson", WattsonCamC, WattsonLookC, CardsFov, front, lensMedium); // L07: a small drift toward him and a rack-focus

            // The three pieces: letter with its name as the 36 % sub-line (wrapped to two lines under the V), then the
            // formula they combine into, coloured by piece, and the title above it. All parallel to the Cards lens.
            var facing = Quaternion.LookRotation(CardsLookC - CardsCamC);
            var pieces = new[] { ("V", "Potential Difference", palette.voltage), ("I", "Current", palette.current), ("R", "Resistance", palette.resistance) };
            var letters = new GameObject[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                var (letter, name, color) = pieces[i];
                letters[i] = Card(letter, WorldText.Sub(letter, name, WorldText.Cap.LetterCard, WorldText.Cap.LetterSub), color, null, new Vector3(RowX[i], RowY, RowZ), WorldText.Cap.LetterCard, false, LetterWrap);
                letters[i].transform.rotation = facing;
            }
            // One baseline for V, I and R: each block is centred vertically, so the V (two sub-lines) sits higher than the
            // others; lower it by the difference of the letters' own baselines, measured from the laid-out text.
            float baseline = LetterBaseline(letters[1]);
            foreach (var card in letters) card.transform.position += card.transform.up * (baseline - LetterBaseline(card));

            string law = Tint("V", palette.voltage) + " = " + Tint("I", palette.current) + Tint("R", palette.resistance);
            var lawAt = new Vector3(0f, RowY, RowZ);
            Card("Law", law, Color.white, null, lawAt, WorldText.Cap.HeroFormula).transform.rotation = facing;
            // The title's reveal sparks are its own sweep (TitleIgnite), not the card's burst.
            var titleAt = new Vector3(0f, TitleY, RowZ);
            var title = Card("Title", "OHM'S LAW", Color.white, null, titleAt, TitleCap, WorldText.Look.Glow, sparks: false);
            title.transform.rotation = facing;
            TitleIgnite(title);
            // The same two, back behind Wattson for the last line: they simply return (no pop, no sparks, no second reveal).
            Settled(Card("Law Back", law, Color.white, null, lawAt, WorldText.Cap.HeroFormula, WorldText.Look.Glow, sparks: false)).transform.rotation = facing;
            Settled(Card("Title Back", "OHM'S LAW", Color.white, null, titleAt, TitleCap, WorldText.Look.Glow, sparks: false)).transform.rotation = facing;

            // S13_Apply: V = IR written on the board left of the graph (the floating one would sit over the graph in the
            // Apply frame), ~78 px tall there, under the Boards Set like Scene 12's board cards.
            var (lawBoardAt, boardRotation) = BoardSurface(LawBoardUV, Scene12CardLift);
            float lawBoardCap = WorldText.CapAt(WorldText.Px.DataCard * 1.4f, lawBoardAt, ApplyCamC, ApplyLookC, ApplyFov);
            Card("Law Board", law, Color.white, refs["Boards Set"].transform, lawBoardAt, lawBoardCap, WorldText.Look.Glow, sparks: false).transform.rotation = boardRotation;
        }

        static string Tint(string text, Color color) => "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";

        // Local y of a card's first character's baseline (the card's text child has no offset, rotation or scale).
        static float LetterBaseline(GameObject card)
        {
            var text = card.transform.Find("Text").GetComponent<TextMeshPro>();
            text.ForceMeshUpdate(true, true);
            return text.textInfo.characterCount > 0 ? text.textInfo.characterInfo[0].baseLine : 0f;
        }

        // A glow card that appears already settled: it keeps the glow word's bob, skips the pop.
        static GameObject Settled(GameObject card)
        {
            var pop = card.transform.Find("Text").GetComponent<PopIn>();
            if (pop)
            {
                pop.fromScale = 1f;
                pop.overshoot = 1f;
            }
            return card;
        }

        // Scene 12's last two cards, for the match-cut: the Ω glyph above the graph and the one-ohm card above the table,
        // built exactly as Scene12Builder.BoardCard builds them (on the Boards Set, lying on the chalk face), visible from
        // frame 0 and already settled: no pop, no sparks. The camera move hides them.
        void CarriedOver()
        {
            var set = refs["Boards Set"].transform;
            var (omegaAt, rotation) = BoardSurface(new Vector2(GraphBoardUV.x, Scene12OmegaV), Scene12CardLift);
            float omegaCap = WorldText.CapAt(WorldText.Px.DataCard * 1.6f, BoardCentreC, BoardsCamC, BoardsLookC, BoardsFov);
            Settled(Card("Ohm Symbol", "Ω", palette.resistance, set, omegaAt, omegaCap, WorldText.Look.Glow, true, sparks: false)).transform.rotation = rotation;
            var (oneOhmAt, _) = BoardSurface(new Vector2(TableBoardUV.x, Scene12OneOhmV), Scene12CardLift);
            Card("One Ohm", "1 Ω = 1 V / 1 A", palette.resistance, set, oneOhmAt, BoardsShotTitleCap, WorldText.Look.Panel, true).transform.rotation = rotation;
        }

        // The storyboard's "VFX title reveal": sparks run along the title's own baseline from one end to the other in one
        // sweep, so the letters read as igniting left to right. A child of the title card, so it rides its rotation and
        // position. A real effect (no "[VFX]" reviewer panel): the `fx` cue plays it once, hides it when its sparks are
        // gone, and thins it under reduced motion.
        void TitleIgnite(GameObject title)
        {
            var bounds = title.transform.Find("Text").GetComponent<TextMeshPro>().textBounds;
            var ignite = Emitter("Title Ignite", title.transform, new Vector3(bounds.center.x, bounds.min.y + TitleCap * 0.2f, -0.02f),
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
            // Match-cut from Scene 12: its last frame exactly. The table with units and the worked V / I column glowing,
            // the graph with its line and slope triangle, the Ω and one-ohm cards, Wattson beside the board, the bench at 2 V.
            Shot("Boards");
            Cue("readings.r 1", "resistance and the ohm are taught (Scene 12): the live readings add R, Ask Wattson may quote it");
            Cue("table.rows 5", "match-cut from Scene 12: 5 rows with units");
            Cue("table.ratio 1");
            Cue("table.work 0", "1.0 ÷ 0.20 = 5"); Cue("table.work 1", "2.0 ÷ 0.40 = 5"); Cue("table.work 2", "3.0 ÷ 0.60 = 5");
            Cue("table.result 3", "5"); Cue("table.result 4", "5");
            Cue("table.glow 2 1", "the whole V / I column glows, as Scene 12 leaves it");
            Cue("graph.axes 1"); Cue("graph.plot 5"); Cue("graph.line 1"); Cue("graph.triangle 0 2");
            Visual("Match-cut from Scene 12: the table with its V / I column filled and glowing, the graph with the slope triangle, Ω and 1 Ω = 1 V / 1 A", 0.6);

            // The board empties while the camera pulls back: the cards get a clean dark backdrop. The graph keeps its axes
            // and line for S13_Apply; its recorded points stay off (their labels would give that answer away).
            Shot("Cards");
            Gesture(WattsonController.Gesture.Flick, moveTo: "Cards Point");
            Hide("Card Ohm Symbol"); Hide("Card One Ohm");
            Cue("table.clear", "no glow or highlights left behind"); Cue("graph.clear"); Cue("graph.triangle -1 0"); Cue("graph.plot 0");
            Hide("Observation Table"); Hide("Graph"); Hide("Graph Panel");
            Show("Card V", 0.6); Show("Card I", 0.85); Show("Card R", 1.1);
            Visual("Three cards float: V — Potential Difference (blue), I — Current (amber), R — Resistance (coral), one baseline", 1.2);
            Line("S13_L01");
            Gesture(WattsonController.Gesture.Point, point: "Card V");
            Cue("pulse Card V", "V highlights (peaks and settles back)");
            Line("S13_L02");
            Gesture(WattsonController.Gesture.Point, point: "Card I");
            Cue("pulse Card I", "I highlights");
            Line("S13_L03");
            Gesture(WattsonController.Gesture.Point, point: "Card R");
            Cue("pulse Card R", "R highlights");
            Line("S13_L04");
            Gesture(WattsonController.Gesture.Talking);
            Line("S13_L05", 0.2);
            Hide("Card V"); Hide("Card I"); Hide("Card R"); Show("Card Law");
            Visual("The three cards combine into V = IR", 1.2);
            Line("S13_L06", 0.1);
            Show("Card Title", -1.0);
            Cue("fx Title Ignite", "sparks sweep along the title's baseline once: the letters ignite left to right", -0.95);
            Visual("OHM'S LAW title card inside the board: the letters ignite with sparks", 1.5);

            // AQ2 (storyboard): the camera eases right so the panel docks over the wall, V = IR and the title stay lit.
            Gesture(WattsonController.Gesture.Listening, moveTo: "Question Point");
            Shot("Question");
            Visual("The frame slides right: V = IR and OHM'S LAW on the left, room for the question on the right", 1.0);
            Gate("S13_AQ2", "Storyboard AQ2 (graded, Resistance; checkpointEnabled): A. The one that needs the larger potential difference (correct) · B. The smaller · C. Both the same. Right: AQ_GOOD. Wrong: S13_AQ2_FB/_FC, hint S13_AQ2_H, retry; out of tries: S13_AQ2_X. Panel 640 px docked right.",
                "AQ2", "AQ_GOOD", "S13_AQ2_FB", "S13_AQ2_FC", "S13_AQ2_H", "S13_AQ2_X");

            // The law at work: the whole investigation in one frame. The floating hero cards make way (they would sit over
            // the graph), V = IR lands on the board, the graph returns with its line, the readings strip (V, I and R) and
            // the live point.
            Gesture(WattsonController.Gesture.Talking, moveTo: "Apply Point");
            Shot("Apply");
            Cue("vanish 0.5 Card Law", "the floating formula and title make way while the camera moves");
            Cue("vanish 0.5 Card Title");
            Show("Graph Panel", 0.3); Show("Graph", 0.3);
            Cue("readings 1", "live V, I and R above the graph (the Ω card is long gone from that band)", 0.5);
            Cue("graph.live 1", "the live point on the line", 0.5);
            Show("Card Law Board", 0.7);
            Visual("Apply frame: bench meters at 2.0 V and 0.40 A, the graph's line with the live point, the readings strip (2.0 V · 0.40 A · 5.0 Ω), V = IR on the board", 1.4);
            Cue("spotlight", "the Ask Wattson button pulses while he offers help");
            Line("S13_A01", 0.2);
            Gesture(WattsonController.Gesture.Listening);
            Gate("S13_Apply", "Target (OhmsLaw): make the ammeter read 0.80 A with the battery slider or by asking Wattson. Readings strip with a target column, live point. First miss / 15 s idle: S13_Apply_H; 2nd miss: Wattson sets 4 V (S13_Apply_X); hit: S13_Apply_OK.",
                "S13_Apply_GO", "S13_Apply_H", "S13_Apply_X", "S13_Apply_OK");
            Gesture(WattsonController.Gesture.Point, point: "Card Law Board");
            Wait(0.3);
            Gate("S13_Recap", "Say gate (recap that reacts to the learner): Struggling(OhmsLaw) → the multiply trick; else Struggling(Resistance) → more push, same current, more resistance; else the push/flow/opposition recap.",
                "S13_Recap_If_Struggling_OhmsLaw", "S13_Recap_If_Struggling_Resistance", "S13_Recap");

            // The last line: Wattson comes forward, the board is clean again, OHM'S LAW and V = IR glow behind him.
            Cue("readings 0"); Cue("graph.live 0");
            Hide("Graph"); Hide("Graph Panel");
            Cue("vanish 0.4 Card Law Board");
            Gesture(WattsonController.Gesture.Greet, moveTo: "Front Point");
            Shot("Wattson");
            Show("Card Title Back", 0.5); Show("Card Law Back", 0.5);
            Visual("Wattson comes forward for the last line; OHM'S LAW and V = IR glow behind him", 1.3);
            Line("S13_L07", 0.3);
            Handoff("Scene13Complete", cut: false, afterSeconds: 0.3);
        }
    }
}
