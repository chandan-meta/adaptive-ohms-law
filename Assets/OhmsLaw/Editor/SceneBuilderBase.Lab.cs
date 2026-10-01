using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw.Editor
{
    // Stage kit for Scenes 7-13: Scene 4's study with its lighting and cameras, Ohm's 1826 apparatus at Scene 4's
    // positions, the modern bench that replaces the desk from Scene 8 on, and the groups the cues show and hide.
    public abstract partial class SceneBuilderBase
    {
        const string WorkbenchPrefab = "Assets/AdaptiveLearning/Prefabs/ImportedLabEquipment/Workbench.prefab";
        const string OhmPrefab = Root + "/Scene04/Prefabs/Ohm_Scene04.prefab";
        const float BenchCloseDepth = 1.8f;                                                   // StudyBenchCam to the desk

        // ------------------------------------------------------------------ transitional names
        // The blockout frame (bench at the origin, cameras on -Z) is gone; these keep the scene builders and Rig.cs
        // compiling until they move to the Study* constants. Delete once nothing references them.

        [Obsolete("Study frame: the bench top is DeskY")] protected const float BenchTop = DeskY;
        [Obsolete("Use StudyWideCam / StudyWideLook")] protected static Vector3 BenchWideCam => StudyWideCam;
        [Obsolete("Use StudyWideCam / StudyWideLook")] protected static Vector3 BenchWideLook => StudyWideLook;
        [Obsolete("Use StudyBenchCam / StudyBenchLook")] protected static Vector3 BenchCloseCam => StudyBenchCam;
        [Obsolete("Use StudyBenchCam / StudyBenchLook")] protected static Vector3 BenchCloseLook => StudyBenchLook;
        [Obsolete("Use StudyBoardsCam / StudyBoardsLook")] protected static Vector3 BoardsCam => StudyBoardsCam;
        [Obsolete("Use StudyBoardsCam / StudyBoardsLook")] protected static Vector3 BoardsLook => StudyBoardsLook;
        [Obsolete("Use StudyWattsonHome")] protected static Vector3 WattsonHome => StudyWattsonHome;
        [Obsolete("Use OhmFeet4")] protected static Vector3 OhmFeet => OhmFeet4;
        [Obsolete("Use StudySet(modernBench)")] protected void LabGreybox(bool modernBench) => StudySet(modernBench);
        [Obsolete("Use OhmApparatus()")] protected void OhmTable() => OhmApparatus();

        // ------------------------------------------------------------------ the set

        // Scene 4's study, lit and framed, with the desk or the modern bench in place. Refs:
        //   "Lab"        everything in the room (study, desk group, bench, lighting); Scene 12 hides it for the dive
        //   "Old Table"  the Victorian desk with its dressing, the chair, the desk candle, the lamp and their lights
        //   "Bench"      the modern bench (Workbench.prefab, 2.0 x 0.82 x 0.9 m, top at DeskY) with its cool spot
        //   "Study"      the room itself (from Study()); "Study/<part>" refs stay valid
        //   markers "Wattson Home", "Bench Point", "Boards Anchor", "Apparatus Point"
        //   cameras "Bench Wide", "Bench Close", "Table Close", "Boards"
        protected void StudySet(bool modernBench)
        {
            var room = Study();
            // Grouping the desk parts needs a plain hierarchy: children of a prefab instance cannot be re-parented.
            PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var lab = Ref(Node("Lab"));
            room.transform.SetParent(lab.transform, true);

            // Paper_001 lies on the floor by the fireplace, not on the desk: it stays with the room.
            FindChild(room, "Paper_001").SetParent(FindChild(room, "Interior"), true);
            // The one book pile that stands in front of the apparatus corner and inside the bench footprint.
            FindChild(room, "Book_Pile_4").gameObject.SetActive(false);

            var oldTable = Ref(Node("Old Table", lab.transform));
            foreach (var part in new[] { "Victorian_Desk", "Chair", "Candle_Holder_5", "hat_and_glass", "magnify_glass" })
                FindChild(room, part).SetParent(oldTable.transform, true);

            StudyLighting();
            var lighting = scene.GetRootGameObjects().Last(g => g.name == "Lighting");
            lighting.transform.SetParent(lab.transform, true);
            // The flames that stand on the desk go with it.
            foreach (var flame in new[] { "Desk Candle", "Oil Lamp" })
                FindChild(lighting, flame).SetParent(oldTable.transform, true);

            var bench = Model(WorkbenchPrefab, "Bench", lab.transform, Vector3.zero, Vector3.zero, 1f, collider: false);
            Fit(bench, new Vector3(BenchWidth, DeskY - FloorY, BenchDepth), new Vector3(BenchCentre.x, FloorY, BenchCentre.z));
            Ref(bench);
            // Cool-white work light over the instruments; the candle warmth stays in the room behind.
            var spot = Node("Bench Lamp", bench.transform).AddComponent<Light>();
            spot.transform.position = new Vector3(BenchCentre.x, 1.3f, BenchCentre.z);
            spot.transform.rotation = Quaternion.Euler(90, 0, 0);
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.95f, 0.88f);
            spot.intensity = 5f;
            spot.range = 3.2f;
            spot.spotAngle = 75f;
            spot.innerSpotAngle = 40f;
            spot.shadows = LightShadows.Soft;

            oldTable.SetActive(!modernBench);
            bench.SetActive(modernBench);

            Marker("Wattson Home", StudyWattsonHome);
            var benchPoint = Marker("Bench Point", BenchCentre + Vector3.up * 0.1f).transform;
            var boardsAnchor = Marker("Boards Anchor", BoardsAnchor).transform;
            var apparatusPoint = Marker("Apparatus Point", Galvanometer4 + Vector3.up * 0.08f).transform;
            // The kids are the camera: all four shots stand in the room, on the +Z/+X side of the desk.
            Vcam("Bench Wide", StudyWideCam, StudyWideLook, 46, benchPoint, lensWide, noise: 0.35f);
            Vcam("Bench Close", StudyBenchCam, StudyBenchLook, 38, benchPoint, lensMedium);
            Vcam("Table Close", StudyCloseCam, StudyCloseLook, 36, apparatusPoint, lensMedium);
            Vcam("Boards", StudyBoardsCam, StudyBoardsLook, 44, boardsAnchor, lensMedium);
        }

        // Scales a model so its render bounds measure `size` and stands it with the bottom centre of those bounds at `foot`.
        protected static void Fit(GameObject go, Vector3 size, Vector3 foot)
        {
            var bounds = RenderBounds(go);
            var s = go.transform.localScale;
            go.transform.localScale = new Vector3(s.x * size.x / bounds.size.x, s.y * size.y / bounds.size.y, s.z * size.z / bounds.size.z);
            bounds = RenderBounds(go);
            go.transform.position += foot - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        static Bounds RenderBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        // ------------------------------------------------------------------ Ohm's apparatus (classroom frame)

        // ART_DIRECTION §2: the desk top centre is the origin at y 0.80, cameras stand on the -Z side and Ohm behind
        // the desk (+Z) facing them. His model's arms are short (0.32 m shoulder to wrist at scale 1.55), so what he
        // works with runs along the back of the desk, within reach of a 40° lean; the hero props sit toward the front.
        public static readonly Vector3 Desk7 = new Vector3(0f, 0.80f, 0f);
        public static readonly Vector3 OhmFeet7 = new Vector3(0f, 0f, 0.75f);
        public static readonly Vector3 Galvanometer7 = Desk7 + new Vector3(0.45f, 0, 0.10f);   // base centre of the torsion galvanometer
        public static readonly Vector3 Cups7 = Desk7 + new Vector3(-0.26f, 0, 0.15f);          // middle mercury cup; the row runs along X
        public static readonly Vector3 Wire7 = Desk7 + new Vector3(0f, 0.09f, 0.40f);           // the mounted test wire, between its posts
        public static readonly Vector3 Source7 = Desk7 + new Vector3(-0.62f, 0, 0.28f);         // copper-bismuth source, left-back
        public static readonly Vector3 TestWires7 = Desk7 + new Vector3(-0.50f, 0, -0.22f);     // the loose test wires, front-left
        public static readonly Vector3 Notebook7 = Desk7 + new Vector3(0.28f, 0, 0.30f);        // right of his hands, clear of the galvanometer
        public static readonly Vector3 DeskEye7 = new Vector3(0.3f, 1.08f, -1.15f);             // the desk close-up's eye (= Classroom TableCloseCamC); faces the pages fan
        const float WireHalf7 = 0.28f, GlassRadius = 0.09f, GlassHeight = 0.20f, NeedleHeight = 0.12f;

        // Ohm's 1826 apparatus on the classroom desk, grouped for Scene 8's swap (each group's pivot is on its prop,
        // so callouts and Wattson's pointing land on it):
        //   "torsion galvanometer"  glass cylinder, dark base, brass cap, arc scale, needle, its two leads -> AMMETER
        //   "Copper Test Wires"     the mounted wire between its posts, four loose wires, the source lead -> NICHROME WIRE — XY
        //   "Thermoelectric Source" copper-bismuth junctions ("Hot Junction", "Cold Junction" for the callouts) -> cells / DC source
        //   "Mercury Cups"          three brushed-metal cups                                 -> switch / key
        //   "notebook" (+ pen), "Page 1..3", "Pages Fan", "Ohm Apparatus" (root), "Georg Ohm", "Ohm Hand Wire",
        //   markers "Apparatus Point", "Ohm Notebook Target", "Ohm Galvanometer Target", "Ohm Wire Target"
        // Names are the storyboard's; the callout layer labels them, so nothing here carries world text.
        protected void OhmApparatus()
        {
            var copper = Lit("Copper", new Color(0.86f, 0.46f, 0.26f), 1f, 0.72f);
            var brass = Lit("Brass", new Color(0.83f, 0.66f, 0.3f), 1f, 0.68f);
            var zinc = Lit("Zinc", new Color(0.62f, 0.65f, 0.68f), 1f, 0.5f);
            var wood = Lit("DarkWood", new Color(0.2f, 0.11f, 0.06f), 0f, 0.4f);
            var paper = Lit("Paper", new Color(0.86f, 0.8f, 0.66f), 0f, 0.15f);
            var ink = Lit("Ink", new Color(0.08f, 0.07f, 0.1f), 0f, 0.3f);
            var bismuth = Lit("Bismuth", new Color(0.6f, 0.55f, 0.6f), 1f, 0.45f);
            var mercury = Lit("Mercury", new Color(0.78f, 0.79f, 0.82f), 1f, 0.95f);
            var ice = Lit("Ice", new Color(0.86f, 0.93f, 1f), 0f, 0.9f);
            var gunmetal = Lit("Gunmetal", new Color(0.16f, 0.17f, 0.19f), 1f, 0.5f);
            var steel = Lit("BrushedSteel", new Color(0.7f, 0.72f, 0.74f), 1f, 0.45f);
            var needleSteel = Lit("NeedleSteel", new Color(0.2f, 0.21f, 0.24f), 1f, 0.6f);
            var glass = ApparatusGlass("GalvanometerGlass", new Color(0.8f, 0.9f, 1f, 0.18f));
            var root = Ref(Node("Ohm Apparatus")).transform;
            Vector3 postA = Wire7 + Vector3.left * WireHalf7, postB = Wire7 + Vector3.right * WireHalf7; // A on his right (-X)
            Vector3 sag = Vector3.down * 0.04f;

            // Copper-bismuth source: two vessels, a junction block in the mouth of each, the bismuth bar across them,
            // steam over the hot side and ice on the cold side. The junctions are refs so the hot/cold callouts can aim at them.
            var source = Ref(Node("Thermoelectric Source", root)).transform;
            Vector3 hot = Source7 + Vector3.left * 0.09f, cold = Source7 + Vector3.right * 0.09f;
            Prim(PrimitiveType.Cube, "Source Base", source, Source7 + Vector3.up * 0.007f, new Vector3(0.3f, 0.014f, 0.14f), wood);
            Prim(PrimitiveType.Cylinder, "Hot Vessel", source, hot + Vector3.up * 0.054f, new Vector3(0.09f, 0.04f, 0.09f), copper);
            Prim(PrimitiveType.Cylinder, "Cold Vessel", source, cold + Vector3.up * 0.054f, new Vector3(0.09f, 0.04f, 0.09f), zinc);
            Ref(Prim(PrimitiveType.Cube, "Hot Junction", source, hot + Vector3.up * 0.1f, Vector3.one * 0.03f, copper));
            Ref(Prim(PrimitiveType.Cube, "Cold Junction", source, cold + Vector3.up * 0.1f, Vector3.one * 0.03f, copper));
            Prim(PrimitiveType.Cube, "Bismuth Bar", source, Source7 + Vector3.up * 0.108f, new Vector3(0.2f, 0.012f, 0.012f), bismuth);
            for (int i = 0; i < 3; i++)
            {
                float a = 0.6f + i * 2.2f;
                Prim(PrimitiveType.Cube, "Ice", source, cold + new Vector3(Mathf.Cos(a) * 0.028f, 0.1f, Mathf.Sin(a) * 0.028f), Vector3.one * 0.016f, ice, new Vector3(0, a * Mathf.Rad2Deg, 0));
            }
            Emitter("Steam", source, hot + Vector3.up * 0.11f, Glow("Glow_Steam", new Color(0.85f, 0.88f, 0.95f, 0.16f)), ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
                main.maxParticles = 40;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = ps.emission;
                emission.rateOverTime = 14;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 12;
                shape.radius = 0.015f;
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.02f;
                noise.frequency = 0.6f;
            });
            Pivot(source, Source7 + Vector3.up * 0.08f);

            // Torsion galvanometer (Scene07_galvanometer_closeup): dark metal base with two brass terminals at the front,
            // a brass ring, the glass cylinder with its brass cap and torsion knob, and inside it the needle hanging on
            // its thread at mid-height in front of the arc scale, which curves round the back half of the glass.
            var galvanometer = Ref(Node("torsion galvanometer", root)).transform;
            Vector3 g = Galvanometer7;
            float glassBottom = 0.035f, glassTop = glassBottom + GlassHeight;
            Prim(PrimitiveType.Cylinder, "Galvanometer Base", galvanometer, g + Vector3.up * 0.0125f, new Vector3(0.26f, 0.0125f, 0.26f), gunmetal);
            Prim(PrimitiveType.Cylinder, "Galvanometer Ring", galvanometer, g + Vector3.up * 0.03f, new Vector3(0.2f, 0.005f, 0.2f), brass);
            Prim(PrimitiveType.Cylinder, "Galvanometer Glass", galvanometer, g + Vector3.up * (glassBottom + GlassHeight / 2), new Vector3(GlassRadius * 2, GlassHeight / 2, GlassRadius * 2), glass);
            Prim(PrimitiveType.Cylinder, "Galvanometer Cap", galvanometer, g + Vector3.up * (glassTop + 0.0075f), new Vector3(0.19f, 0.0075f, 0.19f), brass);
            Prim(PrimitiveType.Cylinder, "Torsion Head", galvanometer, g + Vector3.up * (glassTop + 0.021f), new Vector3(0.04f, 0.006f, 0.04f), brass);
            Prim(PrimitiveType.Sphere, "Torsion Knob", galvanometer, g + Vector3.up * (glassTop + 0.034f), Vector3.one * 0.018f, brass);
            Prim(PrimitiveType.Cylinder, "Torsion Thread", galvanometer, g + Vector3.up * ((NeedleHeight + glassTop) / 2), new Vector3(0.0016f, (glassTop - NeedleHeight) / 2, 0.0016f), ink);
            var pivot = Node("Galvanometer Needle Pivot", galvanometer, g + Vector3.up * NeedleHeight).transform;
            Prim(PrimitiveType.Cube, "Galvanometer Needle", pivot, Vector3.zero, new Vector3(0.14f, 0.0025f, 0.005f), needleSteel);
            Prim(PrimitiveType.Sphere, "Needle Hub", pivot, Vector3.zero, Vector3.one * 0.012f, brass);
            var scale = Node("Galvanometer Scale", galvanometer, g + Vector3.up * NeedleHeight);
            scale.AddComponent<MeshFilter>().sharedMesh = TorsionScaleMesh(GlassRadius - 0.01f, 0.03f);
            scale.AddComponent<MeshRenderer>().sharedMaterials = new[]
            {
                TwoSided("ScaleCard", new Color(0.93f, 0.9f, 0.82f), 0.2f),
                TwoSided("ScaleInk", new Color(0.1f, 0.09f, 0.1f), 0.3f),
            };
            Vector3 terminalA = g + new Vector3(-0.07f, 0.025f, -0.1f), terminalB = g + new Vector3(0.07f, 0.025f, -0.1f);
            foreach (var terminal in new[] { terminalA, terminalB })
            {
                Prim(PrimitiveType.Cylinder, "Galvanometer Terminal", galvanometer, terminal + Vector3.up * 0.012f, new Vector3(0.012f, 0.012f, 0.012f), brass);
                Prim(PrimitiveType.Sphere, "Terminal Cap", galvanometer, terminal + Vector3.up * 0.026f, Vector3.one * 0.014f, brass);
            }
            Polyline("Lead Post-Galvanometer", galvanometer, copper, 0.004f, false, true, postB + Vector3.up * 0.01f, (postB + terminalA) / 2 + Vector3.up * 0.03f + sag, terminalA + Vector3.up * 0.03f);
            // The return lead runs along the desk in front of the cups and climbs to the cold junction.
            Polyline("Lead Galvanometer-Source", galvanometer, copper, 0.004f, false, true,
                terminalB + Vector3.up * 0.03f, g + new Vector3(0.05f, 0.004f, -0.17f), Desk7 + new Vector3(-0.1f, 0.004f, -0.03f),
                Desk7 + new Vector3(-0.52f, 0.004f, 0.02f), Source7 + new Vector3(0.12f, 0.06f, -0.12f), cold + Vector3.up * 0.115f);
            Pivot(galvanometer, g + Vector3.up * NeedleHeight);
            // The close-up's focus target: the set may have registered it already (it is a camera's FocusTarget), so move that one.
            if (refs.TryGetValue("Apparatus Point", out var apparatusPoint)) apparatusPoint.transform.position = g + Vector3.up * NeedleHeight;
            else Marker("Apparatus Point", g + Vector3.up * NeedleHeight);

            // The test wire mounted between two posts in front of him (Scene 7's loop), its lead from the source, and
            // four loose test wires of different lengths fanned out at the front-left (Scene07_ohm_copper_wires).
            var wires = Ref(Node("Copper Test Wires", root)).transform;
            Prim(PrimitiveType.Cylinder, "Copper Wire", wires, Wire7, new Vector3(0.01f, WireHalf7, 0.01f), copper, new Vector3(0, 0, 90));
            foreach (var post in new[] { postA, postB })
            {
                var foot = new Vector3(post.x, Desk7.y, post.z);
                Prim(PrimitiveType.Cube, "Post Block", wires, foot + Vector3.up * 0.015f, new Vector3(0.06f, 0.03f, 0.06f), gunmetal);
                Prim(PrimitiveType.Cylinder, "Post", wires, foot + Vector3.up * 0.065f, new Vector3(0.016f, 0.035f, 0.016f), brass);
                Prim(PrimitiveType.Sphere, "Post Cap", wires, foot + Vector3.up * 0.1f, Vector3.one * 0.024f, brass);
            }
            Polyline("Lead Source-Post", wires, copper, 0.004f, false, true, hot + Vector3.up * 0.115f, (hot + postA) / 2 + Vector3.up * 0.09f + sag, postA + Vector3.up * 0.01f);
            for (int i = 0; i < 4; i++)
            {
                float length = 0.56f - i * 0.08f;
                var centre = TestWires7 + new Vector3(0.04f * i, 0.005f, 0.12f - i * 0.08f);
                Prim(PrimitiveType.Cylinder, "Test Wire " + (i + 1), wires, centre, new Vector3(0.01f, length / 2, 0.01f), copper, new Vector3(0, -4.5f + i * 3f, 90));
            }
            Pivot(wires, TestWires7 + Vector3.up * 0.01f);

            // Mercury cups: three brushed-metal cups filled to the brim, in a row at mid-depth.
            var cups = Ref(Node("Mercury Cups", root)).transform;
            for (int i = 0; i < 3; i++)
            {
                var cup = Cups7 + Vector3.right * ((i - 1) * 0.16f);
                Prim(PrimitiveType.Cylinder, "Mercury Cup " + (i + 1), cups, cup + Vector3.up * 0.025f, new Vector3(0.06f, 0.025f, 0.06f), steel);
                Prim(PrimitiveType.Cylinder, "Mercury", cups, cup + Vector3.up * 0.0502f, new Vector3(0.048f, 0.0006f, 0.048f), mercury);
            }
            Pivot(cups, Cups7 + Vector3.up * 0.05f);

            // Ohm's notebook with its written lines and a pen; the pages that pile onto it one per cycle, and the fan above.
            var notebook = Ref(Node("notebook", root)).transform;
            var book = Prim(PrimitiveType.Cube, "Ohm Notebook", notebook, Notebook7 + Vector3.up * 0.006f, new Vector3(0.22f, 0.012f, 0.16f), paper, new Vector3(0, -12, 0)).transform;
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Cube, "Ink Line", book, Vector3.zero, new Vector3(0.7f - i * 0.1f, 0.2f, 0.03f), ink).transform.localPosition = new Vector3(-0.05f * i, 0.55f, 0.3f - i * 0.18f);
            var pen = Node("Pen", notebook, Notebook7 + new Vector3(0.09f, 0.021f, -0.03f));
            pen.transform.localRotation = Quaternion.Euler(0, 55, 90);
            Prim(PrimitiveType.Cylinder, "Pen Barrel", pen.transform, Vector3.zero, new Vector3(0.008f, 0.065f, 0.008f), ink);
            Prim(PrimitiveType.Sphere, "Pen Tip", pen.transform, Vector3.up * 0.065f, Vector3.one * 0.007f, brass);
            var pages = Node("Pages", notebook, Notebook7);
            pages.transform.localRotation = Quaternion.Euler(0, -12, 0);
            for (int i = 0; i < 3; i++)
            {
                // Flat on the book, written for a reader on the camera side (top of the page toward +Z).
                var page = Prim(PrimitiveType.Quad, "Page " + (i + 1), pages.transform, new Vector3(0.004f * i, 0.0135f + 0.0008f * i, -0.003f * i), new Vector3(0.17f, 0.12f, 1), paper, new Vector3(90, 0, 0));
                Ink(page.transform, InkPages[i], ink.GetColor("_BaseColor"));
                page.AddComponent<PopIn>();
                page.SetActive(false);
                Ref(page);
            }
            Pivot(notebook, Notebook7 + Vector3.up * 0.012f);
            var fan = Ref(Node("Pages Fan", root, Notebook7 + Vector3.up * 0.25f));
            for (int i = 0; i < 5; i++)
            {
                var page = Prim(PrimitiveType.Quad, "Fanned Page", fan.transform, new Vector3((i - 2) * 0.1f, Mathf.Abs(i - 2) * -0.025f, 0.002f * i), new Vector3(0.17f, 0.12f, 1), paper, new Vector3(0, 0, (2 - i) * 12f));
                Ink(page.transform, InkPages[i % InkPages.Length], ink.GetColor("_BaseColor"));
            }
            Face(fan, DeskEye7);
            fan.AddComponent<PopIn>();
            fan.SetActive(false);

            // The copper wire he examines (Examine mode): OhmWorker holds it between his hands, hidden otherwise.
            var handWire = Ref(Node("Ohm Hand Wire", null, OhmFeet7 + new Vector3(0, 1.02f, -0.28f)));
            Prim(PrimitiveType.Cylinder, "Held Copper Wire", handWire.transform, Vector3.zero, new Vector3(0.009f, 0.21f, 0.009f), copper, new Vector3(0, 0, 90));
            handWire.SetActive(false);

            // Georg Ohm behind the desk. The targets are wrist positions: the hand slopes on from there, so with a 40°
            // lean the pen lands on the page centre and the fingers on post A's cap (both within his 0.32 m reach); the
            // torsion knob is 0.5 m from his shoulder, so that arm straightens toward it and stops behind the cap.
            var notebookTarget = Marker("Ohm Notebook Target", Notebook7 + new Vector3(-0.01f, 0.12f, 0.15f)).transform;
            var galvanometerTarget = Marker("Ohm Galvanometer Target", g + new Vector3(-0.03f, glassTop + 0.02f, 0.1f)).transform;
            var wireTarget = Marker("Ohm Wire Target", postA + new Vector3(0.05f, 0.1f, 0.1f)).transform;
            GeorgOhm(notebookTarget, galvanometerTarget, wireTarget, pivot, handWire.transform);
        }

        // Moves a group's pivot without moving its contents, so a callout or a pointing gesture at the group lands on the prop.
        static void Pivot(Transform group, Vector3 pivot)
        {
            var children = new List<Transform>();
            foreach (Transform child in group) children.Add(child);
            foreach (var child in children) child.SetParent(null, true);
            group.position = pivot;
            foreach (var child in children) child.SetParent(group, true);
        }

        // Transparent URP Lit for the glass: alpha-blended, no depth write, polished so the highlight shows the cylinder.
        static Material ApparatusGlass(string name, Color tint) => Mat(name, "Universal Render Pipeline/Lit", m =>
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            if (m.HasProperty("_BlendModePreserveSpecular")) m.SetFloat("_BlendModePreserveSpecular", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_SrcBlendAlpha")) { m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha); }
            m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.96f);
        });

        // Opaque Lit drawn from both sides, for the thin curved scale card and its ticks.
        static Material TwoSided(string name, Color color, float smoothness) => Mat(name, "Universal Render Pipeline/Lit", m =>
        {
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Cull", (float)CullMode.Off);
        });

        // The galvanometer's arc scale: a card curved round the back half of the glass (+X through +Z to -X) with a
        // tick every 5°, longer every 15°; submesh 0 is the card, 1 the ticks.
        static Mesh TorsionScaleMesh(float radius, float height) => MeshAsset("TorsionScale", mesh =>
        {
            var vertices = new List<Vector3>();
            var card = new List<int>();
            var ticks = new List<int>();
            Vector3 Arc(float degrees, float r, float y) => new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad) * r, y, Mathf.Sin(degrees * Mathf.Deg2Rad) * r);
            void Quad(List<int> triangles, float from, float to, float r, float h)
            {
                int v = vertices.Count;
                vertices.AddRange(new[] { Arc(from, r, -h), Arc(to, r, -h), Arc(to, r, h), Arc(from, r, h) });
                triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
            }
            for (int i = 0; i < 36; i++) Quad(card, i * 5f, i * 5f + 5f, radius, height / 2);
            for (int i = 1; i < 36; i++) Quad(ticks, i * 5f - 0.6f, i * 5f + 0.6f, radius - 0.001f, i % 3 == 0 ? height * 0.42f : height * 0.24f);
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(card, 0);
            mesh.SetTriangles(ticks, 1);
        });

        // Ohm's 1826 series in his own units (wire length in Zoll, needle reading in degrees), one page per cycle.
        static readonly string[] InkPages =
        {
            "1826  Kupfer\n2 Zoll    326.75\n4 Zoll    300.75\n6 Zoll    277.75",
            "10 Zoll   238.25\n18 Zoll   190.75\n34 Zoll   134.50\n66 Zoll    83.25",
            "130 Zoll   48.50\n2 Zoll    305.25\n4 Zoll    281.50\n6 Zoll    259.00",
            "10 Zoll   224.25\n18 Zoll   178.50\n34 Zoll   125.75\n66 Zoll    78.00",
            "2 Zoll    311.00\n4 Zoll    286.75\n6 Zoll    264.25\n10 Zoll   228.50",
        };

        // Handwriting-sized ink on a page quad: a monospaced table so the columns line up.
        static void Ink(Transform page, string text, Color ink)
        {
            var label = WorldText.Card("Ink", "<mspace=0.6em>" + text + "</mspace>", ink, page, new Vector3(0, 0, -0.001f), WorldText.Cap.Ink, null, 0, WorldText.ProseLineSpacing);
            label.alignment = TextAlignmentOptions.Left;
            label.rectTransform.sizeDelta = new Vector2(0.16f, label.rectTransform.sizeDelta.y);
            // The quad is scaled to the page size; the text keeps metre units.
            label.transform.localScale = new Vector3(1 / page.localScale.x, 1 / page.localScale.y, 1);
        }

        // A storyboard prop name over a prop: Bench Close size, turned toward that camera and kept on screen size by
        // billboarding in the other shots. Line breaks are explicit (the two-line source label).
        protected TextMeshPro PropLabel(string text, Transform parent, Vector3 position)
        {
            var label = WorldText.Card("Label " + text.Replace("\n", " "), text, Color.white, parent, position, WorldText.Cap.PropLabel, null, 0, WorldText.ProseLineSpacing);
            Face(label.gameObject, StudyBenchCam);
            WorldText.Billboard(label, BenchCloseDepth);
            return label;
        }

        // Georg Ohm: the Scene 4 variant (scale 1.55, OhmWorker attached) behind the classroom desk, facing the cameras.
        GameObject GeorgOhm(Transform notebook, Transform galvanometer, Transform wirePost, Transform needle, Transform handWire)
        {
            OhmWorker worker;
            GameObject ohm;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OhmPrefab))
            {
                ohm = Model(OhmPrefab, "Georg Ohm", null, OhmFeet7, new Vector3(0, 180, 0), 1.55f, collider: false);
                worker = ohm.GetComponent<OhmWorker>() ?? ohm.AddComponent<OhmWorker>();
                Ref(ohm);
            }
            else
            {
                ohm = Ohm(OhmFeet7, Vector3.back, notebook, galvanometer, wirePost);
                worker = ohm.GetComponent<OhmWorker>();
                if (!worker) return ohm; // capsule stand-in
            }
            worker.notebook = notebook;
            worker.galvanometer = galvanometer;
            worker.wirePost = wirePost;
            worker.needle = needle;
            worker.handWire = handWire;
            worker.lean = 40f; // the back of the desk is 0.4 m in front of his feet; Scene 4's 22 reached 0.3 m
            worker.mode = OhmWorker.Mode.Work;
            if (PrefabUtility.IsPartOfPrefabInstance(worker)) PrefabUtility.RecordPrefabInstancePropertyModifications(worker); // scene references are instance overrides
            return ohm;
        }
    }
}
