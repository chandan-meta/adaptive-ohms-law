using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Stage kit for Scenes 7-13: Scene 4's study with its lighting and cameras, Ohm's 1826 apparatus at Scene 4's
    // positions, the modern bench that replaces the desk from Scene 8 on, and the groups the cues show and hide.
    public abstract partial class SceneBuilderBase
    {
        const string WorkbenchPrefab = "Assets/AdaptiveLearning/Prefabs/ImportedLabEquipment/Workbench.prefab";
        const string OhmPrefab = Root + "/Scene04/Prefabs/Ohm_Scene04.prefab";
        const float BenchCloseDepth = 1.8f;                                                   // StudyBenchCam to the desk
        // The spare test wires hang on a rack and the mercury cups stand in front of it at the desk's left end: the only
        // part of the desk every study camera sees down to the desk top (the open book, inkstand and lamp hide the rest).
        static readonly Vector3 Rack4 = new Vector3(-3.115f, DeskY, -1.9f), Cups4 = new Vector3(-3.115f, DeskY, -1.72f);

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

        // ------------------------------------------------------------------ Ohm's apparatus

        // Scene 4's bench experiment at Scene 4's positions, plus the storyboard's extras, grouped for Scene 8's swap:
        //   "torsion galvanometer"  galvanometer with its two leads                 -> AMMETER
        //   "Copper Test Wires"     mounted wire, posts, source lead, rack of spares -> NICHROME WIRE — XY
        //   "Thermoelectric Source" copper-bismuth source (replaces the pile)       -> cells / DC source
        //   "Mercury Cups"          four cups on their block                        -> switch / key
        //   "notebook", "Page 1..3", "Pages Fan", "Ohm Apparatus" (root), "Georg Ohm"
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
            var root = Ref(Node("Ohm Apparatus")).transform;
            const float half = 0.21f;
            Vector3 postA = new Vector3(Wire4.x - half, DeskY + 0.09f, Wire4.z), postB = new Vector3(Wire4.x + half, DeskY + 0.09f, Wire4.z);
            Vector3 sag = Vector3.down * 0.05f;

            // Copper-bismuth source where Scene 4's pile stood: two vessels, a junction block in the mouth of each,
            // the bismuth bar across them, steam over the hot side and ice on the cold side.
            var source = Ref(Node("Thermoelectric Source", root)).transform;
            Vector3 hot = Pile4 + new Vector3(-0.07f, 0, 0), cold = Pile4 + new Vector3(0.07f, 0, 0);
            Prim(PrimitiveType.Cube, "Source Base", source, Pile4 + Vector3.up * 0.006f, new Vector3(0.22f, 0.012f, 0.1f), wood);
            Prim(PrimitiveType.Cylinder, "Hot Vessel", source, hot + Vector3.up * 0.042f, new Vector3(0.06f, 0.03f, 0.06f), copper);
            Prim(PrimitiveType.Cylinder, "Cold Vessel", source, cold + Vector3.up * 0.042f, new Vector3(0.06f, 0.03f, 0.06f), zinc);
            Prim(PrimitiveType.Cube, "Hot Junction", source, hot + Vector3.up * 0.078f, Vector3.one * 0.026f, copper);
            Prim(PrimitiveType.Cube, "Cold Junction", source, cold + Vector3.up * 0.078f, Vector3.one * 0.026f, copper);
            Prim(PrimitiveType.Cube, "Bismuth Bar", source, Pile4 + Vector3.up * 0.085f, new Vector3(0.165f, 0.01f, 0.01f), bismuth);
            for (int i = 0; i < 3; i++)
            {
                float a = 0.6f + i * 2.2f;
                Prim(PrimitiveType.Cube, "Ice", source, cold + new Vector3(Mathf.Cos(a) * 0.02f, 0.077f, Mathf.Sin(a) * 0.02f), Vector3.one * 0.013f, ice, new Vector3(0, a * Mathf.Rad2Deg, 0));
            }
            Emitter("Steam", source, hot + Vector3.up * 0.09f, Glow("Glow_Steam", new Color(0.85f, 0.88f, 0.95f, 0.16f)), ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
                main.maxParticles = 40;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = ps.emission;
                emission.rateOverTime = 14;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 12;
                shape.radius = 0.012f;
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.02f;
                noise.frequency = 0.6f;
            });
            PropLabel("hot", source, hot + Vector3.up * 0.13f);
            PropLabel("cold", source, cold + Vector3.up * 0.13f);
            PropLabel("copper–bismuth\nthermoelectric source", source, Pile4 + Vector3.up * 0.22f);

            // Galvanometer: wooden base, brass drum, pale dial, the needle on a pivot OhmWorker swings, and the torsion
            // column above it: a thin glass tube around the thread (narrow, so his hand clears it) with a brass head.
            var galvanometer = Ref(Node("torsion galvanometer", root)).transform;
            Prim(PrimitiveType.Cylinder, "Galvanometer Base", galvanometer, Galvanometer4 + Vector3.up * 0.01f, new Vector3(0.15f, 0.01f, 0.15f), wood);
            Prim(PrimitiveType.Cylinder, "Galvanometer Drum", galvanometer, Galvanometer4 + Vector3.up * 0.04f, new Vector3(0.12f, 0.02f, 0.12f), brass);
            Prim(PrimitiveType.Cylinder, "Galvanometer Dial", galvanometer, Galvanometer4 + Vector3.up * 0.061f, new Vector3(0.105f, 0.001f, 0.105f), paper);
            var pivot = Node("Galvanometer Needle Pivot", galvanometer, Galvanometer4 + Vector3.up * 0.065f).transform;
            Prim(PrimitiveType.Cube, "Galvanometer Needle", pivot, Vector3.zero, new Vector3(0.085f, 0.002f, 0.004f), ink, new Vector3(0, 35, 0));
            Prim(PrimitiveType.Cylinder, "Galvanometer Glass", galvanometer, Galvanometer4 + Vector3.up * 0.115f, new Vector3(0.04f, 0.05f, 0.04f), Translucent("Glass_Torsion", new Color(0.78f, 0.88f, 1f, 0.14f)));
            Prim(PrimitiveType.Cylinder, "Torsion Thread", galvanometer, Galvanometer4 + Vector3.up * 0.116f, new Vector3(0.0016f, 0.05f, 0.0016f), ink);
            Prim(PrimitiveType.Cylinder, "Torsion Head", galvanometer, Galvanometer4 + Vector3.up * 0.168f, new Vector3(0.032f, 0.002f, 0.032f), brass);
            Prim(PrimitiveType.Sphere, "Torsion Knob", galvanometer, Galvanometer4 + Vector3.up * 0.18f, Vector3.one * 0.014f, brass);
            Polyline("Lead Post-Galvanometer", galvanometer, copper, 0.004f, false, true, postB, (postB + Galvanometer4) / 2 + Vector3.up * 0.06f + sag, Galvanometer4 + new Vector3(0.05f, 0.04f, 0));
            Polyline("Lead Galvanometer-Source", galvanometer, copper, 0.004f, false, true, Galvanometer4 + new Vector3(-0.05f, 0.04f, 0), (Galvanometer4 + cold) / 2 + Vector3.up * 0.05f, cold + Vector3.up * 0.091f);
            PropLabel("torsion galvanometer", galvanometer, Galvanometer4 + Vector3.up * 0.25f);

            // The test wire between its brass posts, its lead to the source, and the spare wires of four lengths on a rack.
            var wires = Ref(Node("Copper Test Wires", root)).transform;
            Prim(PrimitiveType.Cylinder, "Copper Wire", wires, Wire4, new Vector3(0.007f, half, 0.007f), copper, new Vector3(0, 0, 90));
            foreach (float side in new[] { -1f, 1f })
            {
                var foot = new Vector3(Wire4.x + side * half, DeskY, Wire4.z);
                Prim(PrimitiveType.Cube, "Post Base", wires, foot + Vector3.up * 0.008f, new Vector3(0.05f, 0.016f, 0.05f), wood);
                Prim(PrimitiveType.Cylinder, "Post", wires, foot + Vector3.up * 0.05f, new Vector3(0.012f, 0.045f, 0.012f), brass);
                Prim(PrimitiveType.Sphere, "Post Cap", wires, foot + Vector3.up * 0.098f, Vector3.one * 0.02f, brass);
            }
            Polyline("Lead Source-Post", wires, copper, 0.004f, false, true, hot + Vector3.up * 0.091f, (hot + postA) / 2 + Vector3.up * 0.1f + sag, postA);
            Prim(PrimitiveType.Cube, "Rack Foot", wires, Rack4 + Vector3.up * 0.006f, new Vector3(0.18f, 0.012f, 0.06f), wood);
            Prim(PrimitiveType.Cube, "Wire Rack", wires, Rack4 + Vector3.up * 0.126f, new Vector3(0.18f, 0.24f, 0.012f), wood);
            for (int i = 0; i < 4; i++)
            {
                float length = 0.2f - i * 0.04f;
                var hook = Rack4 + new Vector3(-0.0675f + i * 0.045f, 0.215f, 0.014f);
                Prim(PrimitiveType.Sphere, "Rack Hook", wires, hook, Vector3.one * 0.01f, brass);
                Prim(PrimitiveType.Cylinder, "Test Wire " + (i + 1), wires, hook + Vector3.down * (length / 2), new Vector3(0.009f, length / 2, 0.009f), copper);
            }
            PropLabel("copper test wires", wires, Rack4 + Vector3.up * 0.3f);

            // Mercury cups: four zinc cups with a mercury surface, on a block in front of the rack.
            var cups = Ref(Node("Mercury Cups", root)).transform;
            Prim(PrimitiveType.Cube, "Cup Block", cups, Cups4 + Vector3.up * 0.01f, new Vector3(0.19f, 0.02f, 0.05f), wood);
            for (int i = 0; i < 4; i++)
            {
                var cup = Cups4 + new Vector3(-0.0675f + i * 0.045f, 0, 0);
                Prim(PrimitiveType.Cylinder, "Mercury Cup " + (i + 1), cups, cup + Vector3.up * 0.03f, new Vector3(0.032f, 0.01f, 0.032f), zinc);
                Prim(PrimitiveType.Cylinder, "Mercury", cups, cup + Vector3.up * 0.0395f, new Vector3(0.026f, 0.0005f, 0.026f), mercury);
            }
            PropLabel("mercury cups", cups, Cups4 + new Vector3(0, 0.07f, 0.06f));

            // Ohm's notebook with its written lines; the pages that pile onto it one per cycle, and the fan above.
            var notebook = Ref(Node("notebook", root)).transform;
            var book = Prim(PrimitiveType.Cube, "Ohm Notebook", notebook, Notebook4, new Vector3(0.2f, 0.01f, 0.14f), paper, new Vector3(0, 8, 0)).transform;
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Cube, "Ink Line", book, Vector3.zero, new Vector3(0.7f - i * 0.1f, 0.2f, 0.03f), ink).transform.localPosition = new Vector3(-0.05f * i, 0.55f, 0.3f - i * 0.18f);
            var pages = Node("Pages", notebook, Notebook4);
            pages.transform.localRotation = Quaternion.Euler(0, 8, 0);
            for (int i = 0; i < 3; i++)
            {
                // Flat on the book, written for a reader on the room side (top of the page toward -Z).
                var page = Prim(PrimitiveType.Quad, "Page " + (i + 1), pages.transform, new Vector3(0.004f * i, 0.0075f + 0.0008f * i, -0.003f * i), new Vector3(0.17f, 0.12f, 1), paper, new Vector3(90, 180, 0));
                Ink(page.transform, InkPages[i], ink.GetColor("_BaseColor"));
                page.AddComponent<PopIn>();
                page.SetActive(false);
                Ref(page);
            }
            var fan = Ref(Node("Pages Fan", root, Notebook4 + Vector3.up * 0.25f));
            for (int i = 0; i < 5; i++)
            {
                var page = Prim(PrimitiveType.Quad, "Fanned Page", fan.transform, new Vector3((i - 2) * 0.1f, Mathf.Abs(i - 2) * -0.025f, 0.002f * i), new Vector3(0.17f, 0.12f, 1), paper, new Vector3(0, 0, (2 - i) * 12f));
                Ink(page.transform, InkPages[i % InkPages.Length], ink.GetColor("_BaseColor"));
            }
            Face(fan, StudyBenchCam);
            fan.AddComponent<PopIn>();
            fan.SetActive(false);

            // Georg Ohm behind the desk, with the targets his work loop reaches for (Scene 4's offsets).
            var notebookTarget = Marker("Ohm Notebook Target", Notebook4 + Vector3.up * 0.02f).transform;
            var galvanometerTarget = Marker("Ohm Galvanometer Target", Galvanometer4 + new Vector3(0.02f, 0.08f, -0.02f)).transform;
            var wireTarget = Marker("Ohm Wire Target", Wire4 + new Vector3(0.21f, 0.03f, -0.03f)).transform;
            GeorgOhm(notebookTarget, galvanometerTarget, wireTarget, pivot);
        }

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

        // Georg Ohm: the Scene 4 variant (scale 1.55, OhmWorker attached) at Scene 4's spot, facing +Z into the room.
        GameObject GeorgOhm(Transform notebook, Transform galvanometer, Transform wirePost, Transform needle)
        {
            OhmWorker worker;
            GameObject ohm;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OhmPrefab))
            {
                ohm = Model(OhmPrefab, "Georg Ohm", null, OhmFeet4, Vector3.zero, 1.55f, collider: false);
                worker = ohm.GetComponent<OhmWorker>() ?? ohm.AddComponent<OhmWorker>();
                Ref(ohm);
            }
            else
            {
                ohm = Ohm(OhmFeet4, Vector3.forward, notebook, galvanometer, wirePost);
                worker = ohm.GetComponent<OhmWorker>();
                if (!worker) return ohm; // capsule stand-in
            }
            worker.notebook = notebook;
            worker.galvanometer = galvanometer;
            worker.wirePost = wirePost;
            worker.needle = needle;
            if (PrefabUtility.IsPartOfPrefabInstance(worker)) PrefabUtility.RecordPrefabInstancePropertyModifications(worker); // scene references are instance overrides
            return ohm;
        }
    }
}
