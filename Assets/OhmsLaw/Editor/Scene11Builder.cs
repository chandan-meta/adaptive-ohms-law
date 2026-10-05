using UnityEngine;
using Object = UnityEngine.Object;

namespace OhmsLaw.Editor
{
    // Scene 11: Turn Data into a Graph. Points in a straight line through the origin; V ∝ I.
    // Classroom frame: the observation table and the V-I graph sit on the teaching board (Boards kit), the Boards
    // camera holds the whole scene (match-cut from Scene 10, hand-off to Scene 12 on the same shot) and Wattson
    // teaches from beside the board. The graph effects are built here on top of the canvases' own layout.
    public class Scene11Builder : SceneBuilderBase
    {
        public override string SceneId => "Scene11";
        public override string SceneName => "Scene11_Graph";
        public override string Title => "Turn Data into a Graph";

        // Heights above the chalk face: the canvases sit at 0.006, the card over them, the effects over the card.
        const float GlowLift = 0.018f, CardLift = 0.026f, FxLift = 0.03f;
        const float ColumnsFlight = 1.2f;      // the cells' travel to the axes (the beat's Visual is 1.5 s)
        const float LineSweep = 0.9f;          // the head's run from the origin to the end of the line (Visual 1.0 s)
        // ObservationTableUI lays its V and I columns out 380 and 280 px wide (its private Columns): the digits' centres.
        static readonly float[] TableColumnX = { 190f, 380f + 140f };

        ExperimentData data;
        Material amber, blue, lineGlow, ringGlow, pointGlow;

        protected override void Dialogue()
        {
            L("S11_L01", "Now we've got our biggest clue. They're all in a line!", 3.5f);
            L("S11_L02", "Double the potential difference...and the current doubles.", 3.5f);
            L("S11_L03", "And it keeps happening.", 1.8f);
            L("S11_L04", "For this conductor that is the nichrome wire, while its temperature remains constant...the potential difference across it is directly proportional to the current flowing through it.", 9.0f);
            L("S11_L05", "But there's still one clue we haven't explained.", 2.8f);
        }

        protected override SceneAgentContext Agent() => Context(
            new[]
            {
                "Everything from Scenes 5 to 10: voltage (V, volt), current (I, ampere), the circuit, the five readings (1 V → 0.2 A ... 5 V → 1.0 A), current rises with potential difference, doubling V doubles I.",
                "Plotted with current I on the horizontal axis and potential difference V on the vertical axis, the five points lie on a straight line through the origin.",
                "For the nichrome wire at constant temperature, the potential difference across it is directly proportional to the current through it: V ∝ I.",
            },
            new[] { "Resistance", "V / I = constant", "The slope of the graph is the resistance", "Ohms", "V = IR", "The name 'Ohm's law'", "The Christmas-lights answer" },
            ("Why does the line go through the origin?", "Think about zero volts: no push, so no current, which is the point zero-zero. And every reading doubled together with its partner: 1 volt, 0.2 ampere; 2 volts, 0.4 ampere. Points that scale together like that always line up on a straight line that starts at the origin."),
            ("What does 'directly proportional' mean?", "Two quantities are directly proportional when doubling one doubles the other, tripling one triples the other, and so on. Our table shows exactly that: double the volts, double the amperes. Scientists write it with the symbol V ∝ I."),
            ("Why does the temperature have to stay constant?", "Because a hot wire behaves differently from a cool one. If the wire warmed up during our readings, the same push would give a slightly different current, and our neat straight line would start to bend. Keeping the temperature steady keeps the comparison fair."));

        protected override void Stage()
        {
            ClassroomSet(modernBench: true, audience: false);
            Wattson(BoardsWattsonC, BoardsCamC); // where Scene 10 leaves him: beside the board, so the match-cut holds
            director.matchCutFrom = "Scene10_Experiment";
            director.rig = Rig(assembled: true, keyClosed: true, voltage: 5);
            Boards(withTable: true, withGraph: true);
            data = BlockoutAssets.ExperimentData();
            GraphMaterials();

            // Scene 10 has only the table on the board, so the graph is not there at the cut: it pops onto the board
            // in the first beat (soft PopIn on the canvas and its chalk-framed panel).
            foreach (var name in new[] { "Graph", "Graph Panel" })
            {
                var pop = refs[name].AddComponent<PopIn>();
                pop.duration = 0.5f;
                pop.fromScale = 0.6f;
                pop.overshoot = 1.04f;
                refs[name].SetActive(false);
            }
            ColumnsBecomeAxes();
            LineDraw();
            for (int i = 0; i < data.Count; i++) PointSpark(i);
            GraphPulse();
            Marker("Graph Pair", GraphPoint((data.currents[0] + data.currents[1]) / 2, (data.voltages[0] + data.voltages[1]) / 2, FxLift));

            // V ∝ I: the formula glows beside the top of the line (under its end, right of the last points, clear of
            // their labels and of the highlight tags), inside the graph panel, lying on the board like the canvases
            // (a Face() toward the camera would tilt its far edge into the slab).
            var card = Card("Proportional", "V ∝ I", palette.voltage, null, GraphPoint(1.15f, 3.3f, CardLift), BoardsShotTitleCap);
            card.transform.rotation = BoardSurface(GraphBoardUV).rotation;
        }

        protected override void Beats()
        {
            Shot("Boards");
            Cue("table.rows 5", "match-cut from Scene 10 with the full table on screen");
            Cue("table.highlight 0 1", "rows 1 and 2 still highlighted from Scene 10's discovery");
            Gesture(WattsonController.Gesture.Point, point: "Observation Table", moveTo: "Boards Point");
            Wait(0.5);
            Show("Graph Panel");
            Show("Graph");
            Cue("table.clear", "the table hands its highlights over to the graph");
            Show("[VFX] columns become axes");
            Gesture(WattsonController.Gesture.Point, point: "Graph");
            Visual("The graph panel pops onto the board; the I column's cells fly across to become the x-axis ticks, the V column's rise to become the y-axis ticks", 1.5);
            Cue("graph.axes 1", "axes labelled: Current, I (A) horizontal · Potential Difference, V (V) vertical");
            Hide("[VFX] columns become axes", 1.0);
            Visual("Axis labels appear", 1.2);
            for (int i = 1; i <= 5; i++)
            {
                Cue("graph.plot " + i, $"point {i} plots with its coordinate label");
                Show("Graph Spark " + i);
                Visual("", 0.6);
            }
            Show("Graph Line Sweep");
            Cue("graph.line 1", "a glowing line draws through the points and the origin", LineSweep);
            Show("Graph Line Glow", LineSweep);
            Visual("Line through the origin", 1.0);
            Gesture(WattsonController.Gesture.Greet);
            Line("S11_L01");

            Gesture(WattsonController.Gesture.Point, point: "Graph Pair");
            Cue("graph.highlight 0 1", "points (0.20, 1.0) and (0.40, 2.0) highlight with I → 2I and V → 2V tags");
            Line("S11_L02");
            Cue("graph.pulse", "graph pulse");
            Show("[VFX] graph pulse");
            Line("S11_L03");
            Hide("[VFX] graph pulse");

            Gesture(WattsonController.Gesture.Point, point: "Card Proportional"); // at the end of the line, where V ∝ I appears
            Line("S11_L04", 0.2);
            Show("Card Proportional", -3.2); // on "...directly proportional..."
            Visual("V ∝ I glows at the end of the line", 0.8);
            Gesture(WattsonController.Gesture.Thinking);
            Line("S11_L05", 0.2);
            Cue("handoff", "graph and table stay for Scene 12");
            Handoff("Scene11Complete", cut: true, afterSeconds: 0.4);
        }

        // ------------------------------------------------------------------ board geometry

        // World position of a table cell's centre (column 0 = V, 1 = I; row 0 = the first reading).
        Vector3 TableCell(int column, int row, float lift)
        {
            var (pos, rot) = BoardSurface(TableBoardUV, lift);
            var px = new Vector2(TableColumnX[column] - TableCanvas.x / 2, TableCanvas.y / 2 - ObservationTableUI.HeaderRow - (row + 0.5f) * ObservationTableUI.RowHeight);
            return pos + rot * (Vector3)(px * BoardCanvasScale);
        }

        void GraphMaterials()
        {
            amber = Glow("Glow_GraphCurrent", Hdr(palette.current, 3f));
            blue = Glow("Glow_GraphVoltage", Hdr(palette.voltage, 3f));
            lineGlow = Glow("Glow_GraphLine", Hdr(palette.voltage, 2.2f), softTexture: false);
            ringGlow = Glow("Glow_GraphRing", Hdr(palette.current, 3f), softTexture: false);
            pointGlow = Glow("Glow_Spark_" + ColorUtility.ToHtmlStringRGB(palette.charge), Hdr(palette.charge, 4f));
        }

        // ------------------------------------------------------------------ effects

        // "[VFX] columns become axes", in place of the Boards kit's magenta tag at the seam: each cell of the table's
        // I column slides across to its tick on the graph's x-axis and each V cell rises to its tick on the y-axis
        // (glowing clouds the size of the digits, straight flight), then both axes flash as the cells land and
        // graph.axes shows the real ones. Plays whenever the object is shown; the cue hides it after.
        void ColumnsBecomeAxes()
        {
            const string name = "[VFX] columns become axes";
            var set = refs["Boards Set"].transform;
            var (seam, rot) = BoardSurface(new Vector2(0.505f, 0.5f), FxLift);
            if (refs.TryGetValue(name, out var tag)) Object.DestroyImmediate(tag);
            var root = Ref(Node(name, set, set.InverseTransformPoint(seam)));
            root.transform.rotation = rot;
            for (int r = 0; r < data.Count; r++)
            {
                CellFlight(root.transform, "I " + (r + 1), amber, TableCell(1, r, FxLift), GraphPoint(data.currents[r], 0, FxLift), new Vector2(0.12f, 0.07f));
                CellFlight(root.transform, "V " + (r + 1), blue, TableCell(0, r, FxLift), GraphPoint(0, data.voltages[r], FxLift), new Vector2(0.06f, 0.07f));
            }
            AxisFlash(root.transform, "X Axis", amber, GraphPoint(0, 0, FxLift), GraphPoint(GraphUI.MaxI, 0, FxLift));
            AxisFlash(root.transform, "Y Axis", blue, GraphPoint(0, 0, FxLift), GraphPoint(0, GraphUI.MaxV, FxLift));
            root.SetActive(false);
        }

        // A cloud of sparks the size of a cell's digits, flying in a straight line from the cell to its tick.
        void CellFlight(Transform root, string name, Material glow, Vector3 from, Vector3 to, Vector2 size) =>
            Emitter("Cell " + name, root, root.InverseTransformPoint(from), glow, ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = ColumnsFlight;
                main.startLifetime = ColumnsFlight + 0.1f;
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.028f, 0.042f);
                main.maxParticles = 32;
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(size.x, size.y, 0.005f);
                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                var v = root.InverseTransformVector(to - from) / ColumnsFlight;
                velocity.x = v.x;
                velocity.y = v.y;
                velocity.z = v.z;
                Hold(ps);
            });

        // Sparks along an axis the moment the cells land on it.
        void AxisFlash(Transform root, string name, Material glow, Vector3 a, Vector3 b) =>
            Emitter(name + " Flash", root, root.InverseTransformPoint((a + b) / 2), glow, ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = ColumnsFlight + 0.5f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
                main.maxParticles = 64;
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(ColumnsFlight, 48) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                var along = root.InverseTransformVector(b - a);
                shape.scale = new Vector3(Mathf.Abs(along.x) + 0.02f, Mathf.Abs(along.y) + 0.02f, 0.005f);
                shape.randomDirectionAmount = 1f;
            });

        // The line draws itself: one bright head runs from the origin past the last point, trailing the line behind
        // it ("Graph Line Sweep"); as it arrives, graph.line shows the real line and "Graph Line Glow", a thin additive
        // strand over it, keeps it blooming (UI images do not).
        void LineDraw()
        {
            var set = refs["Boards Set"].transform;
            Vector3 origin = GraphPoint(0, 0, GlowLift), end = GraphPoint(1.1f, 1.1f * data.resistance, GlowLift);
            var glow = Polyline("Graph Line Glow", set, lineGlow, 0.014f, false, true, origin, end); // ~7 px over the 5 px UI line
            glow.gameObject.SetActive(false);
            Ref(glow.gameObject);
            var sweep = Emitter("Graph Line Sweep", set, set.InverseTransformPoint(origin), blue, ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = LineSweep;
                main.startLifetime = LineSweep;
                main.startSpeed = 0f;
                main.startSize = 0.05f;
                main.maxParticles = 2;
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                var shape = ps.shape;
                shape.enabled = false;
                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                var v = set.InverseTransformVector(end - origin) / LineSweep;
                velocity.x = v.x;
                velocity.y = v.y;
                velocity.z = v.z;
                var trails = ps.trails;
                trails.enabled = true;
                trails.lifetime = 1f; // the whole run stays lit until the head dies, exactly when the real line appears
                trails.minVertexDistance = 0.005f;
                trails.widthOverTrail = 0.4f;
                trails.inheritParticleColor = false;
                ps.GetComponent<ParticleSystemRenderer>().trailMaterial = lineGlow;
                Hold(ps);
            });
            sweep.gameObject.SetActive(false);
            Ref(sweep.gameObject);
        }

        // A small burst where a point lands as it plots ("Graph Spark n", shown with graph.plot n).
        void PointSpark(int i)
        {
            var set = refs["Boards Set"].transform;
            var spark = Emitter("Graph Spark " + (i + 1), set, set.InverseTransformPoint(GraphPoint(data.currents[i], data.voltages[i], FxLift)), pointGlow, ps =>
            {
                var main = ps.main;
                main.loop = false;
                main.duration = 0.6f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.02f);
                main.gravityModifier = 0.25f;
                main.maxParticles = 32;
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.015f;
            });
            spark.gameObject.SetActive(false);
            Ref(spark.gameObject);
        }

        // "[VFX] graph pulse", in place of the magenta tag: rings pulse twice out of the two highlighted points
        // (I → 2I, V → 2V) with a spark burst each while graph.pulse breathes the plot. Hidden; cues frame S11_L03 with it.
        void GraphPulse()
        {
            var set = refs["Boards Set"].transform;
            var (centre, rot) = BoardSurface(GraphBoardUV, FxLift);
            var root = Ref(Node("[VFX] graph pulse", set, set.InverseTransformPoint(centre)));
            root.transform.rotation = rot;
            var ring = Torus("GraphPulseRing", 0.12f);
            for (int i = 0; i < 2; i++)
            {
                var at = root.transform.InverseTransformPoint(GraphPoint(data.currents[i], data.voltages[i], FxLift));
                Emitter("Ring " + (i + 1), root.transform, at, ringGlow, ps =>
                {
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 1.4f;
                    main.startLifetime = 0.7f;
                    main.startSpeed = 0f;
                    main.startSize = 0.16f; // the torus is unit-radius: the ring grows to 0.16 m, half-way to the next point
                    main.maxParticles = 4;
                    var emission = ps.emission;
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1), new ParticleSystem.Burst(0.6f, 1) });
                    var shape = ps.shape;
                    shape.enabled = false;
                    var size = ps.sizeOverLifetime;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.15f, 1f, 1f));
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode = ParticleSystemRenderMode.Mesh;
                    renderer.mesh = ring;
                    renderer.alignment = ParticleSystemRenderSpace.Local; // lies in the board plane, facing the room
                });
                Emitter("Sparks " + (i + 1), root.transform, at, amber, ps =>
                {
                    var main = ps.main;
                    main.loop = false;
                    main.duration = 1.4f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.025f);
                    main.gravityModifier = 0.2f;
                    main.maxParticles = 64;
                    var emission = ps.emission;
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24), new ParticleSystem.Burst(0.6f, 24) });
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.02f;
                });
            }
            root.SetActive(false);
        }

        // Bright all the way, out in the last tenth (Emitter's default fades the whole second half of the life).
        static void Hold(ParticleSystem ps)
        {
            var fade = ps.colorOverLifetime;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.08f), new GradientAlphaKey(1, 0.88f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
        }
    }
}
