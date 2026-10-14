using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OhmsLaw.Editor
{
    // Stage kit for Scenes 8-13: the stylised classroom lab of the reference images (ART_DIRECTION §2, §3, §8),
    // built from primitives and the project's wood texture. Its own frame: bench top centre at the origin, top
    // y 0.80, every camera on -Z (world text faces -Z and reads). Scenes 7-8 place Scene 4's study in this same frame
    // (SceneBuilderBase.Lab.cs HistoricalLab), so the match-cuts 7 -> 8 -> 9 share one world.
    public abstract partial class SceneBuilderBase
    {
        const string BenchWoodTexture = "Assets/AdaptiveLearning/Art/Interface/BenchWood.png";
        const string ClassroomWorkbench = "Assets/AdaptiveLearning/Prefabs/ImportedLabEquipment/Workbench.prefab";

        // ---- The classroom frame (metres). Floor y 0, desk top y 0.80, back wall z 2.6, side walls x ±3.2, ceiling 3.0 ----
        public const float ClassroomFloorY = 0f, ClassroomDeskY = 0.80f, ClassroomWallZ = 2.6f, ClassroomHalfWidth = 3.2f, ClassroomCeilingY = 3f;
        public static readonly Vector3 DeskTopC = new Vector3(0f, ClassroomDeskY, 0f);           // desk / bench top centre, 2.0 x 0.9 m
        // Ohm behind the desk, facing -Z; with the Blender lab (BlenderSet.Lab) behind its round table (radius 1.22), on its floor.
        public static Vector3 OhmFeetC => BlenderSet.Lab.Exists ? new Vector3(0f, LabFloorY, 1.5f) : new Vector3(0f, 0f, 0.75f);
        public const float LabFloorY = 0.08f; // the Blender lab's floor (Exports/OurLab/OurLab.blend), 8 cm over the classroom's
        public static readonly Vector3 WattsonHomeC = new Vector3(-1.05f, 1.4f, 0.3f);           // flying left of the desk, inside the frame the lesson panel leaves free (Bench Wide: 56-79 % up)
        // Cameras, all on -Z. Bench Wide = board + Ohm + desk (Scene07_two_clues); Bench Close = the apparatus row
        // (Scene09_series_ammeter); Bench Present = the row with Ohm's head in frame (Scene08_modern_apparatus_presented);
        // Table Close = galvanometer hero (Scene07_galvanometer_closeup); Boards = the teaching board filling the frame.
        public static readonly Vector3 BenchWideCamC = new Vector3(0f, 1.32f, -3.05f), BenchWideLookC = new Vector3(0f, 0.95f, 0.2f);
        public static readonly Vector3 BenchCloseCamC = new Vector3(0f, 1.42f, -1.75f), BenchCloseLookC = new Vector3(0f, 0.85f, 0f);
        public static readonly Vector3 BenchPresentCamC = new Vector3(0f, 1.25f, -1.75f), BenchPresentLookC = new Vector3(0f, 1.1f, 0f);
        public static readonly Vector3 TableCloseCamC = new Vector3(0.3f, 1.08f, -1.15f), TableCloseLookC = new Vector3(0.2f, 0.8f, 0.15f);
        public static readonly Vector3 BoardsCamC = new Vector3(0f, 1.6f, -0.6f), BoardsLookC = new Vector3(0f, 1.7f, 2.55f);
        public const float BenchWideFov = 40f, BenchCloseFov = 36f, BenchPresentFov = 40f, TableCloseFov = 34f, BoardsFov = 38f;
        // The teaching board: 3.2 x 1.5 m, chalk face at z 2.55, centred on the desk, bottom edge 0.95 (clear of the worktop at 0.80).
        public static readonly Vector3 BoardCentreC = new Vector3(0f, 1.7f, 2.55f);
        public static readonly Vector2 BoardSizeC = new Vector2(3.2f, 1.5f);
        public static readonly Vector3 BoardsWattsonC = new Vector3(-0.93f, 1.50f, 1.7f);       // Wattson beside the board in the Boards shot (8-26 % across; at x -1.05 his hat and arm met the frame edge)
        // Beside the board's left rail, clear of the table's V column (1-13 % across): Scenes 10-13 hold him here so the
        // 10 -> 11 -> 12 -> 13 match-cuts do not pop him. The kit's "Table Point" uses it when the table is on the board.
        public static readonly Vector3 TableSideC = new Vector3(-1.45f, 1.6f, 2.2f);
        // Just outside the Bench Present frame (its left edge is x -1.31 at his depth, so his 0.5 m body and a pointing arm
        // stay 0.25 m clear): Scene 8 ends with him here, Scene 9 starts here.
        public static readonly Vector3 PresentOffstageC = new Vector3(-2.0f, 1.4f, 0.3f);
        public static readonly Color Chalk = new Color(0.93f, 0.93f, 0.87f);
        // Board text caps from the TYPOGRAPHY formula: the Bench Wide lens is 5.86 m from the board (256 px/m), the Boards
        // lens 3.15 m (498 px/m). Headline = XL (56 px), body = M (28 px); Boards-shot formula cards = XL, notes = S (24 px).
        public static float BoardTitleCap => WorldText.CapAt(WorldText.Px.DataCard, BoardCentreC, BenchWideCamC, BenchWideLookC, BenchWideFov);   // 0.218
        public static float BoardCap => WorldText.CapAt(WorldText.Px.Medium, BoardCentreC, BenchWideCamC, BenchWideLookC, BenchWideFov);          // 0.109
        public static float BoardsShotTitleCap => WorldText.CapAt(WorldText.Px.DataCard, BoardCentreC, BoardsCamC, BoardsLookC, BoardsFov);         // 0.112
        public static float BoardsShotCap => WorldText.CapAt(WorldText.Px.Small, BoardCentreC, BoardsCamC, BoardsLookC, BoardsFov);                 // 0.048

        // A half turn about Y: a node with it faces -Z, toward the cameras.
        protected static readonly Quaternion ClassroomTurn = Quaternion.Euler(0f, 180f, 0f);

        protected VolumeProfile lensClassroom;
        protected Transform boardFace;                       // the chalk face node; Board*/BoardSurface need it
        protected bool ClassroomStage => boardFace;         // true once ClassroomSet ran (Boards() can mount on the board)
        Material oak, worktopWood, floorWood, frameWood, wallPaint, wainscotPaint, navy, navyDoor, boardGreen, paper, ink, chalkFace, contactShadow;

        // ------------------------------------------------------------------ the set

        // The classroom, lit and framed, with the oak desk (modernBench false) or the clean bench in place. Refs:
        //   "Lab"        everything in the room (room, desk, bench, lighting, audience); Scene 12 hides it for the dive
        //   "Old Table"  the oak teacher's desk            "Bench"  the clean light bench (Workbench.prefab fitted to 2.0 x 0.8 x 0.9)
        //   "Audience"   the blurred student heads (Audience(bool))
        //   markers "Wattson Home", "Bench Point", "Boards Point", "Board Anchor" (board centre, facing -Z), "Apparatus Point"
        //   cameras "Bench Wide", "Bench Close", "Bench Present", "Table Close", "Boards"
        protected void ClassroomSet(bool modernBench, bool audience)
        {
            ClassroomMaterials();
            var labSet = BlenderSet.Lab.Load();
            var lab = Ref(Node("Lab"));
            if (labSet) LabSetRoom(lab.transform, labSet);
            else Room(lab.transform);
            var desk = OakDesk(lab.transform);
            var bench = labSet ? refs["Bench"] : CleanBench(lab.transform); // the Blender lab's round table, or the clean bench
            desk.SetActive(!modernBench);
            bench.SetActive(modernBench);
            if (labSet) LabSetLighting(lab.transform);
            else ClassroomLighting(lab.transform);
            Audience(audience);

            Marker("Wattson Home", WattsonHomeC);
            var benchPoint = Marker("Bench Point", DeskTopC + Vector3.up * 0.1f).transform;
            Marker("Boards Point", BoardsWattsonC);
            Marker("Panel Point", new Vector3(2.8f, 1.45f, 0.4f)); // toward the lesson panel (screen right) from behind the bench
            var boardAnchor = Marker("Board Anchor", BoardCentreC).transform;
            boardAnchor.rotation = ClassroomTurn; // faces -Z, toward the cameras
            var apparatusPoint = Marker("Apparatus Point", ApparatusC + Vector3.up * 0.08f).transform;

            // Scene 4 lens profiles; the wide shot gets a faster lens so the audience heads 0.75 m from it dissolve.
            lensClassroom = Lens("Lens_Classroom", 35, 2f);
            Vcam("Bench Wide", BenchWideCamC, BenchWideLookC, BenchWideFov, benchPoint, lensClassroom, noise: 0.25f);
            Vcam("Bench Close", BenchCloseCamC, BenchCloseLookC, BenchCloseFov, benchPoint, lensMedium);
            Vcam("Bench Present", BenchPresentCamC, BenchPresentLookC, BenchPresentFov, benchPoint, lensMedium);
            Vcam("Table Close", TableCloseCamC, TableCloseLookC, TableCloseFov, apparatusPoint, lensShallow);
            Vcam("Boards", BoardsCamC, BoardsLookC, BoardsFov, boardAnchor, lensMedium);
            var probe = lab.GetComponentInChildren<ReflectionProbe>(true);
            if (probe) BakeProbe(probe, "Classroom");
        }

        void ClassroomMaterials()
        {
            var wood = AssetDatabase.LoadAssetAtPath<Texture2D>(BenchWoodTexture);
            Material Wood(string name, Color tint, float smoothness, Vector2 tiling) => Mat(name, "Universal Render Pipeline/Lit", m =>
            {
                m.SetColor("_BaseColor", tint);
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Smoothness", smoothness);
                m.SetTexture("_BaseMap", wood);
                m.SetTextureScale("_BaseMap", tiling);
            });
            // Tints multiply the mid-brown grain: 0.77 lands the desk on oak #7A4B2B, the worktop and frames stay lighter.
            oak = Wood("Classroom_Oak", new Color(0.78f, 0.74f, 0.74f), 0.45f, new Vector2(1f, 1f));
            worktopWood = Wood("Classroom_Worktop", new Color(1f, 0.92f, 0.82f), 0.4f, new Vector2(3f, 1f));
            frameWood = Wood("Classroom_Frame", new Color(0.95f, 0.85f, 0.75f), 0.35f, new Vector2(2f, 1f));
            floorWood = Wood("Classroom_Floor", new Color(1f, 0.95f, 0.9f), 0.42f, new Vector2(3f, 10f));
            wallPaint = Lit("Classroom_Wall", new Color(0.624f, 0.702f, 0.784f), 0f, 0.12f);          // #9FB3C8
            wainscotPaint = Lit("Classroom_Wainscot", new Color(0.42f, 0.52f, 0.68f), 0f, 0.25f);
            navy = Lit("Classroom_Navy", new Color(0.231f, 0.31f, 0.478f), 0f, 0.3f);                 // #3B4F7A
            navyDoor = Lit("Classroom_NavyDoor", new Color(0.27f, 0.36f, 0.54f), 0f, 0.35f);
            boardGreen = Lit("Classroom_Board", new Color(0.118f, 0.227f, 0.204f), 0f, 0.18f);         // #1E3A34
            paper = Lit("Classroom_Paper", new Color(0.93f, 0.92f, 0.88f), 0f, 0.1f);
            ink = Lit("Classroom_Ink", new Color(0.16f, 0.22f, 0.38f), 0f, 0.2f);
            chalkFace = ChalkMaterial();
            contactShadow = Decal("ContactShadow", new Color(0f, 0f, 0f, 0.62f), ContactShadowTexture());
        }

        // Floor, walls with wainscot, ceiling, window with daylight, the teaching board, cabinets, posters and the props.
        void Room(Transform lab)
        {
            var room = Node("Room", lab).transform;
            float depth = 4f + ClassroomWallZ, zMid = (ClassroomWallZ - 4f) * 0.5f;                  // z -4.0 .. 2.6
            Prim(PrimitiveType.Plane, "Floor", room, new Vector3(0f, 0f, zMid), new Vector3(ClassroomHalfWidth * 0.2f, 1f, depth * 0.1f), floorWood);
            Prim(PrimitiveType.Plane, "Ceiling", room, new Vector3(0f, ClassroomCeilingY, zMid), new Vector3(ClassroomHalfWidth * 0.2f, 1f, depth * 0.1f),
                Lit("Classroom_Ceiling", new Color(0.62f, 0.66f, 0.72f), 0f, 0.02f), new Vector3(180f, 0f, 0f));
            Prim(PrimitiveType.Cube, "Back Wall", room, new Vector3(0f, ClassroomCeilingY * 0.5f, ClassroomWallZ + 0.05f), new Vector3(ClassroomHalfWidth * 2f + 0.2f, ClassroomCeilingY, 0.1f), wallPaint);
            foreach (float side in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, "Side Wall", room, new Vector3(side * (ClassroomHalfWidth + 0.05f), ClassroomCeilingY * 0.5f, zMid), new Vector3(0.1f, ClassroomCeilingY, depth), wallPaint);
            // Wainscot: a deeper blue band to 1.0 m with a rail, 2 cm proud of the paint.
            Prim(PrimitiveType.Cube, "Wainscot", room, new Vector3(0f, 0.5f, ClassroomWallZ - 0.01f), new Vector3(ClassroomHalfWidth * 2f, 1f, 0.02f), wainscotPaint);
            Prim(PrimitiveType.Cube, "Wainscot Rail", room, new Vector3(0f, 1.0f, ClassroomWallZ - 0.025f), new Vector3(ClassroomHalfWidth * 2f, 0.04f, 0.05f), frameWood);
            foreach (float side in new[] { -1f, 1f })
            {
                Prim(PrimitiveType.Cube, "Wainscot", room, new Vector3(side * (ClassroomHalfWidth - 0.01f), 0.5f, zMid), new Vector3(0.02f, 1f, depth), wainscotPaint);
                Prim(PrimitiveType.Cube, "Wainscot Rail", room, new Vector3(side * (ClassroomHalfWidth - 0.025f), 1.0f, zMid), new Vector3(0.05f, 0.04f, depth), frameWood);
            }

            Window(room);
            TeachingBoard(room);
            Cabinets(room);
            Posters(room);
            Plant(room, new Vector3(-2.95f, ClassroomDeskY, 2.25f)); // beside the window: at -2.5 it sat under Wattson's boots in Bench Wide
            Books(room, new Vector3(2.15f, ClassroomDeskY, 2.3f));
            Globe(room, new Vector3(2.75f, ClassroomDeskY, 2.25f));

            // Region groups for Scene 8's sweep out from the bench (the floor spreads from under the bench, the back and side
            // walls rise from the floor, the ceiling spreads); every other scene sees the same room. Refs "Classroom Floor",
            // "Classroom Back" (wall, board, cabinets, posters, worktop props), "Classroom Left" (window), "Classroom Right",
            // "Classroom Ceiling".
            var parts = room.Cast<Transform>().Select(p => (part: p, at: RenderBounds(p.gameObject).center)).ToList();
            PivotGroup("Classroom Floor", room, Vector3.zero, parts.Where(p => p.part.name == "Floor").Select(p => p.part));
            PivotGroup("Classroom Ceiling", room, new Vector3(0f, ClassroomCeilingY, 0f), parts.Where(p => p.part.name == "Ceiling").Select(p => p.part));
            var walls = parts.Where(p => p.part.name != "Floor" && p.part.name != "Ceiling").ToList();
            PivotGroup("Classroom Back", room, new Vector3(0f, 0f, ClassroomWallZ), walls.Where(p => p.at.z > 1.95f).Select(p => p.part));
            PivotGroup("Classroom Left", room, new Vector3(-ClassroomHalfWidth, 0f, -0.7f), walls.Where(p => p.at.z <= 1.95f && p.at.x < 0f).Select(p => p.part));
            PivotGroup("Classroom Right", room, new Vector3(ClassroomHalfWidth, 0f, -0.7f), walls.Where(p => p.at.z <= 1.95f && p.at.x >= 0f).Select(p => p.part));
        }

        // Window on the left wall at the back (z 1.3-2.4): the part past z 1.65 shows at the Bench Wide frame's left edge.
        void Window(Transform room)
        {
            var window = Node("Window", room, new Vector3(-ClassroomHalfWidth, 1.7f, 1.85f)).transform;
            var white = Lit("Classroom_WindowFrame", new Color(0.92f, 0.92f, 0.9f), 0f, 0.3f);
            // Daylight: an unlit HDR pane 1 cm into the room from the wall, facing +X; Bloom turns it into light.
            Prim(PrimitiveType.Quad, "Daylight", window, new Vector3(0.01f, 0f, 0f), new Vector3(1.1f, 1.4f, 1f),
                Mat("Classroom_Daylight", "Universal Render Pipeline/Unlit", m => { m.SetColor("_BaseColor", Hdr(new Color(0.85f, 0.92f, 1f), 1.6f)); if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Off); }),
                new Vector3(0f, -90f, 0f));
            foreach (float y in new[] { -0.73f, 0.73f })
                Prim(PrimitiveType.Cube, "Window Rail", window, new Vector3(0.03f, y, 0f), new Vector3(0.06f, 0.06f, 1.22f), white);
            foreach (float z in new[] { -0.58f, 0.58f })
                Prim(PrimitiveType.Cube, "Window Stile", window, new Vector3(0.03f, 0f, z), new Vector3(0.06f, 1.4f, 0.06f), white);
            Prim(PrimitiveType.Cube, "Window Mullion", window, new Vector3(0.025f, 0f, 0f), new Vector3(0.05f, 1.4f, 0.04f), white);
            Prim(PrimitiveType.Cube, "Window Transom", window, new Vector3(0.025f, 0.15f, 0f), new Vector3(0.05f, 0.04f, 1.16f), white);
            Prim(PrimitiveType.Cube, "Window Sill", window, new Vector3(0.07f, -0.78f, 0f), new Vector3(0.14f, 0.04f, 1.34f), white);
        }

        // The dark-green board with its wooden frame and chalk tray; "Board Face" is where Board()/BoardLine()/BoardPanel() write.
        void TeachingBoard(Transform room)
        {
            var board = Node("Teaching Board", room, BoardCentreC).transform;
            Vector2 s = BoardSizeC;
            Prim(PrimitiveType.Cube, "Board Slab", board, new Vector3(0f, 0f, 0.015f), new Vector3(s.x, s.y, 0.03f), boardGreen);
            const float rail = 0.07f;
            foreach (float side in new[] { -1f, 1f })
            {
                Prim(PrimitiveType.Cube, "Board Rail", board, new Vector3(0f, side * (s.y + rail) * 0.5f, 0.01f), new Vector3(s.x + rail * 2f, rail, 0.04f), frameWood);
                Prim(PrimitiveType.Cube, "Board Stile", board, new Vector3(side * (s.x + rail) * 0.5f, 0f, 0.01f), new Vector3(rail, s.y, 0.04f), frameWood);
            }
            // The tray runs the full framed width (a shorter one showed as a step in the bottom rail).
            Prim(PrimitiveType.Cube, "Chalk Tray", board, new Vector3(0f, -(s.y * 0.5f + rail + 0.015f), -0.03f), new Vector3(s.x + rail * 2f, 0.03f, 0.09f), frameWood);
            Prim(PrimitiveType.Cube, "Chalk Tray Lip", board, new Vector3(0f, -(s.y * 0.5f + rail), -0.07f), new Vector3(s.x + rail * 2f, 0.03f, 0.01f), frameWood);
            foreach (var (x, c) in new[] { (-0.5f, Chalk), (-0.44f, new Color(0.95f, 0.8f, 0.45f)), (0.3f, new Color(0.6f, 0.75f, 0.95f)) })
                Prim(PrimitiveType.Cylinder, "Chalk Stick", board, new Vector3(x, -(s.y * 0.5f + rail) + 0.006f, -0.045f), new Vector3(0.01f, 0.035f, 0.01f), Lit("Classroom_Chalk_" + ColorUtility.ToHtmlStringRGB(c), c, 0f, 0.1f), new Vector3(0f, 0f, 90f));
            boardFace = Node("Board Face", board).transform; // identity rotation: +Z into the wall, text and canvases read from -Z
        }

        // Navy base cabinets with a wooden worktop along the back wall (top at desk height), doors with brass handles.
        void Cabinets(Transform room)
        {
            var cabinets = Node("Cabinets", room).transform;
            float width = ClassroomHalfWidth * 2f;
            var brass = Lit("Classroom_Brass", new Color(0.83f, 0.66f, 0.3f), 1f, 0.7f);
            Prim(PrimitiveType.Cube, "Cabinet Run", cabinets, new Vector3(0f, 0.38f, ClassroomWallZ - 0.29f), new Vector3(width, 0.76f, 0.58f), navy);
            Prim(PrimitiveType.Cube, "Kick Plate", cabinets, new Vector3(0f, 0.05f, ClassroomWallZ - 0.26f), new Vector3(width, 0.1f, 0.54f), Lit("Classroom_Kick", new Color(0.1f, 0.12f, 0.17f), 0f, 0.2f));
            Prim(PrimitiveType.Cube, "Worktop", cabinets, new Vector3(0f, ClassroomDeskY - 0.02f, ClassroomWallZ - 0.31f), new Vector3(width + 0.04f, 0.04f, 0.62f), worktopWood);
            for (int i = 0; i < 10; i++)
            {
                float x = -width * 0.5f + 0.32f + i * 0.64f;
                Prim(PrimitiveType.Cube, "Cabinet Door", cabinets, new Vector3(x, 0.42f, ClassroomWallZ - 0.588f), new Vector3(0.56f, 0.6f, 0.016f), navyDoor);
                Prim(PrimitiveType.Cube, "Door Handle", cabinets, new Vector3(x + (i % 2 == 0 ? 0.22f : -0.22f), 0.64f, ClassroomWallZ - 0.61f), new Vector3(0.016f, 0.1f, 0.016f), brass);
            }
        }

        // Two framed posters on the back wall, either side of the board: "CIRCUITS" with a circuit sketch and
        // "THINK / EXPLORE · QUESTION · LEARN" with a bulb outline. Dressing only (0.055 m caps read as a poster, not a lesson).
        void Posters(Transform room)
        {
            var dark = Lit("Classroom_DarkFrame", new Color(0.2f, 0.14f, 0.1f), 0f, 0.3f);
            Transform Poster(string name, float x)
            {
                var poster = Node(name, room, new Vector3(x, 1.75f, ClassroomWallZ - 0.015f)).transform;
                Prim(PrimitiveType.Quad, "Paper", poster, Vector3.zero, new Vector3(0.56f, 0.76f, 1f), paper);
                foreach (float side in new[] { -1f, 1f })
                {
                    Prim(PrimitiveType.Cube, "Frame", poster, new Vector3(0f, side * 0.395f, 0.005f), new Vector3(0.62f, 0.03f, 0.03f), dark);
                    Prim(PrimitiveType.Cube, "Frame", poster, new Vector3(side * 0.295f, 0f, 0.005f), new Vector3(0.03f, 0.76f, 0.03f), dark);
                }
                return poster;
            }
            var inkColor = ink.GetColor("_BaseColor");
            var circuits = Poster("Poster Circuits", -2.45f);
            WorldText.Card("Title", "CIRCUITS", inkColor, circuits, new Vector3(0f, 0.28f, -0.004f), 0.055f);
            // Battery (two plates) on the left side, a resistor zigzag along the top, the loop closed on the right.
            var loop = new List<Vector3> { new Vector3(-0.18f, -0.22f, 0f), new Vector3(-0.18f, -0.04f, 0f) };
            Sketch(circuits, inkColor, 0.012f, false, loop.ToArray());
            Sketch(circuits, inkColor, 0.012f, false, new Vector3(-0.24f, -0.04f, 0f), new Vector3(-0.12f, -0.04f, 0f));
            Sketch(circuits, inkColor, 0.012f, false, new Vector3(-0.21f, 0.02f, 0f), new Vector3(-0.15f, 0.02f, 0f));
            var top = new List<Vector3> { new Vector3(-0.18f, 0.02f, 0f), new Vector3(-0.18f, 0.14f, 0f), new Vector3(-0.08f, 0.14f, 0f) };
            for (int i = 0; i < 6; i++) top.Add(new Vector3(-0.06f + i * 0.024f, (i % 2 == 0 ? 0.17f : 0.11f), 0f));
            top.AddRange(new[] { new Vector3(0.08f, 0.14f, 0f), new Vector3(0.18f, 0.14f, 0f), new Vector3(0.18f, -0.22f, 0f), new Vector3(-0.18f, -0.22f, 0f) });
            Sketch(circuits, inkColor, 0.012f, false, top.ToArray());
            Sketch(circuits, inkColor, 0.012f, false, new Vector3(0.02f, -0.22f, 0f), new Vector3(0.09f, -0.22f, 0f), new Vector3(0.06f, -0.19f, 0f)); // current arrow

            var think = Poster("Poster Think", 2.45f);
            WorldText.Card("Title", "THINK", inkColor, think, new Vector3(0f, 0.28f, -0.004f), 0.06f);
            WorldText.Card("Line", "EXPLORE · QUESTION · LEARN", inkColor, think, new Vector3(0f, -0.3f, -0.004f), 0.03f, null, 0.5f);
            var bulb = new List<Vector3>();
            for (int i = 0; i <= 24; i++) { float a = Mathf.PI * 0.5f + i / 24f * Mathf.PI * 2f * (300f / 360f) + Mathf.PI * (30f / 360f); bulb.Add(new Vector3(Mathf.Cos(a) * 0.1f, Mathf.Sin(a) * 0.1f + 0.02f, 0f)); }
            bulb.Insert(0, new Vector3(-0.04f, -0.11f, 0f));
            bulb.Add(new Vector3(0.04f, -0.11f, 0f));
            bulb.Add(new Vector3(-0.04f, -0.11f, 0f));
            Sketch(think, palette.current, 0.012f, false, bulb.ToArray());
            Sketch(think, palette.current, 0.012f, false, new Vector3(-0.04f, -0.14f, 0f), new Vector3(0.04f, -0.14f, 0f));
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.PI * 0.25f + i * Mathf.PI * 0.125f;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Sketch(think, palette.current, 0.01f, false, dir * 0.14f + Vector3.up * 0.02f, dir * 0.19f + Vector3.up * 0.02f);
            }
        }

        // A flat line drawing in the parent's XY plane, 4 mm in front of it.
        LineRenderer Sketch(Transform parent, Color color, float width, bool loop, params Vector3[] points)
        {
            var line = Polyline("Sketch", parent, Translucent("Classroom_Line_" + ColorUtility.ToHtmlStringRGB(color), new Color(color.r, color.g, color.b, 0.95f)), width, loop, false,
                points.Select(p => new Vector3(p.x, p.y, -0.004f)).ToArray());
            line.alignment = LineAlignment.TransformZ;
            line.textureMode = LineTextureMode.Stretch;
            return line;
        }

        void Plant(Transform room, Vector3 foot)
        {
            var plant = Node("Plant", room, foot).transform;
            var pot = Prim(PrimitiveType.Cylinder, "Pot", plant, new Vector3(0f, 0.12f, 0f), new Vector3(0.11f, 0.12f, 0.11f), Lit("Classroom_Terracotta", new Color(0.72f, 0.4f, 0.28f), 0f, 0.3f));
            Prim(PrimitiveType.Cylinder, "Pot Rim", plant, new Vector3(0f, 0.225f, 0f), new Vector3(0.12f, 0.015f, 0.12f), Lit("Classroom_Terracotta", new Color(0.72f, 0.4f, 0.28f), 0f, 0.3f));
            Prim(PrimitiveType.Cylinder, "Soil", plant, new Vector3(0f, 0.235f, 0f), new Vector3(0.1f, 0.004f, 0.1f), Lit("Classroom_Soil", new Color(0.2f, 0.14f, 0.1f), 0f, 0.1f));
            var leafA = Lit("Classroom_LeafA", new Color(0.22f, 0.5f, 0.26f), 0f, 0.35f);
            var leafB = Lit("Classroom_LeafB", new Color(0.3f, 0.62f, 0.3f), 0f, 0.35f);
            Prim(PrimitiveType.Cylinder, "Stem", plant, new Vector3(0f, 0.36f, 0f), new Vector3(0.012f, 0.13f, 0.012f), Lit("Classroom_Stem", new Color(0.3f, 0.42f, 0.2f), 0f, 0.3f));
            for (int i = 0; i < 8; i++)
            {
                float a = i * 47f, r = i < 5 ? 0.1f : 0.045f, h = 0.3f + (i % 3) * 0.06f + (i < 5 ? 0f : 0.1f);
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Prim(PrimitiveType.Sphere, "Leaf", plant, dir * r + Vector3.up * h, new Vector3(0.2f, 0.07f, 0.13f), i % 2 == 0 ? leafA : leafB, new Vector3(-28f, a, 0f));
            }
            ContactShadow(pot.transform, new Vector2(0.34f, 0.34f));
        }

        void Books(Transform room, Vector3 foot)
        {
            var books = Node("Books", room, foot).transform;
            var covers = new[] { new Color(0.2f, 0.3f, 0.55f), new Color(0.72f, 0.25f, 0.22f), new Color(0.15f, 0.5f, 0.5f), new Color(0.9f, 0.65f, 0.25f) };
            float y = 0f;
            for (int i = 0; i < covers.Length; i++)
            {
                float h = 0.035f + (i % 2) * 0.01f;
                Prim(PrimitiveType.Cube, "Book", books, new Vector3((i % 2) * 0.015f, y + h * 0.5f, 0f), new Vector3(0.24f, h, 0.17f), Lit("Classroom_Book" + i, covers[i], 0f, 0.3f), new Vector3(0f, (i - 1.5f) * 6f, 0f));
                y += h;
            }
            ContactShadow(books, new Vector2(0.36f, 0.3f));
        }

        void Globe(Transform room, Vector3 foot)
        {
            var globe = Node("Globe", room, foot).transform;
            var darkWood = Lit("Classroom_DarkFrame", new Color(0.2f, 0.14f, 0.1f), 0f, 0.3f);
            var brass = Lit("Classroom_Brass", new Color(0.83f, 0.66f, 0.3f), 1f, 0.7f);
            var baseDisc = Prim(PrimitiveType.Cylinder, "Globe Base", globe, new Vector3(0f, 0.01f, 0f), new Vector3(0.09f, 0.01f, 0.09f), darkWood);
            Prim(PrimitiveType.Cylinder, "Globe Post", globe, new Vector3(0f, 0.08f, 0f), new Vector3(0.012f, 0.06f, 0.012f), brass);
            var tilt = Node("Globe Tilt", globe, new Vector3(0f, 0.29f, 0f)).transform;
            tilt.localRotation = Quaternion.Euler(0f, 0f, 23.5f);
            Prim(PrimitiveType.Sphere, "Globe Sphere", tilt, Vector3.zero, Vector3.one * 0.26f, Lit("Classroom_Globe", new Color(0.25f, 0.45f, 0.7f), 0f, 0.55f));
            MeshPrim("Meridian", tilt, Torus("ClassroomMeridian", 0.025f), brass, Vector3.zero, new Vector3(0f, 90f, 0f), Vector3.one * 0.145f);
            Prim(PrimitiveType.Cylinder, "Globe Axis", tilt, Vector3.zero, new Vector3(0.006f, 0.15f, 0.006f), brass);
            ContactShadow(baseDisc.transform, new Vector2(0.26f, 0.26f));
        }

        protected GameObject MeshPrim(string name, Transform parent, Mesh mesh, Material material, Vector3 position, Vector3 euler, Vector3 scale)
        {
            var go = Node(name, parent, position);
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        // ------------------------------------------------------------------ desk and bench

        // "Old Table": the oak teacher's desk, 2.0 x 0.9 m, top at 0.80, with the navy front panel of the references.
        GameObject OakDesk(Transform lab)
        {
            var desk = Ref(Node("Old Table", lab));
            var t = desk.transform;
            Prim(PrimitiveType.Cube, "Desk Top", t, new Vector3(0f, ClassroomDeskY - 0.025f, 0f), new Vector3(2f, 0.05f, 0.9f), oak);
            Prim(PrimitiveType.Cube, "Desk Edge", t, new Vector3(0f, ClassroomDeskY - 0.065f, -0.43f), new Vector3(2f, 0.03f, 0.04f), oak);
            foreach (float side in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, "Desk Side", t, new Vector3(side * 0.975f, 0.375f, 0f), new Vector3(0.05f, 0.75f, 0.86f), oak);
            Prim(PrimitiveType.Cube, "Desk Front", t, new Vector3(0f, 0.43f, -0.40f), new Vector3(1.9f, 0.62f, 0.03f), navy);
            Prim(PrimitiveType.Cube, "Desk Back Rail", t, new Vector3(0f, 0.7f, 0.41f), new Vector3(1.9f, 0.08f, 0.03f), oak);
            var brass = Lit("Classroom_Brass", new Color(0.83f, 0.66f, 0.3f), 1f, 0.7f);
            foreach (float x in new[] { -0.55f, 0.55f })
            {
                Prim(PrimitiveType.Cube, "Drawer Front", t, new Vector3(x, 0.63f, -0.42f), new Vector3(0.5f, 0.16f, 0.012f), navyDoor);
                Prim(PrimitiveType.Sphere, "Drawer Knob", t, new Vector3(x, 0.63f, -0.435f), Vector3.one * 0.03f, brass);
            }
            ContactShadow(t, new Vector2(2.2f, 1.1f));
            return desk;
        }

        // "Bench": Workbench.prefab fitted to the desk's footprint (2.0 x 0.80 x 0.9, top at 0.80) with the clean light top
        // of the references (#E6E8EC); legs and edge band keep the prefab's steel and grey. Wrapped in an unscaled node so
        // its contact shadow and anything parented later stay undistorted.
        GameObject CleanBench(Transform lab)
        {
            var bench = Ref(Node("Bench", lab));
            var model = Model(ClassroomWorkbench, "Workbench", bench.transform, Vector3.zero, Vector3.zero, 1f, collider: false);
            Fit(model, new Vector3(BenchWidth, ClassroomDeskY - ClassroomFloorY, BenchDepth), new Vector3(DeskTopC.x, ClassroomFloorY, DeskTopC.z));
            var top = Lit("Classroom_BenchTop", new Color(0.86f, 0.87f, 0.89f), 0f, 0.55f); // a shade under white so the probe reflection reads
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] && materials[i].name == "Wood1") { materials[i] = top; changed = true; }
                if (changed) renderer.sharedMaterials = materials;
            }
            ContactShadow(bench.transform, new Vector2(2.2f, 1.1f));
            return bench;
        }

        // ------------------------------------------------------------------ the Blender lab

        // The round lab made in Blender (Exports/OurLab/OurLab.blend -> Tools/sets/lab.json -> Art/LabSet) in place of the
        // code-built room and bench: same frame (the bench at the origin, cameras on -Z), the apparatus still built here.
        // Its round centre table becomes "Bench"; the rest is split into Room()'s region groups so Scene 8's sweep still
        // builds it out from the bench: the floor discs spread with the floor, the false ceiling and magnet rig with the
        // ceiling, the round wall, back board and racks rise with the back, the side racks and the mural with the sides.
        void LabSetRoom(Transform lab, GameObject model)
        {
            var room = Node("Room", lab).transform;
            var set = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            set.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // the FBX holds world coordinates
            PrefabUtility.UnpackPrefabInstance(set, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var part in set.transform.Cast<Transform>().ToList()) part.SetParent(room, true);
            UnityEngine.Object.DestroyImmediate(set);

            // The round centre table is the bench now (the .blend has no other): it is "Bench", so Scene 8 grows it from Ohm's
            // desk and the apparatus stands on it (its top is 0.79, the apparatus feet 0.80).
            var bench = Ref(Node("Bench", lab)).transform;
            foreach (var part in room.Cast<Transform>().Where(p => p.name.StartsWith("Center_Table")).ToList()) part.SetParent(bench, true);

            var parts = room.Cast<Transform>().Select(p => (part: p, b: RenderBounds(p.gameObject))).ToList();
            bool Floor((Transform part, Bounds b) p) => p.b.max.y < 0.95f;
            bool Ceiling((Transform part, Bounds b) p) => p.b.min.y > 2f;
            bool Back((Transform part, Bounds b) p) => p.b.size.x > 8f || p.b.center.z > 1.95f; // the round wall spans the room
            var walls = parts.Where(p => !Floor(p) && !Ceiling(p)).ToList();
            PivotGroup("Classroom Floor", room, new Vector3(0f, LabFloorY, 0f), parts.Where(Floor).Select(p => p.part));
            PivotGroup("Classroom Ceiling", room, new Vector3(0f, ClassroomCeilingY, 0f), parts.Where(Ceiling).Select(p => p.part));
            PivotGroup("Classroom Back", room, new Vector3(0f, LabFloorY, ClassroomWallZ), walls.Where(Back).Select(p => p.part));
            PivotGroup("Classroom Left", room, new Vector3(-ClassroomHalfWidth, LabFloorY, -0.7f), walls.Where(p => !Back(p) && p.b.center.x < 0f).Select(p => p.part));
            PivotGroup("Classroom Right", room, new Vector3(ClassroomHalfWidth, LabFloorY, -0.7f), walls.Where(p => !Back(p) && p.b.center.x >= 0f).Select(p => p.part));
            var all = parts.Select(p => p.b).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Debug.Log($"{SceneId}: lab set from Blender, {parts.Count} parts, bounds {all.min} to {all.max}");
        }

        // The round lab is closed (no window, no daylight): a warm overhead key on the bench with soft shadows and its lamp
        // cone seen (dust drifting in it), the ceiling cove as a ring of soft blue points (the cool contrast), the board under
        // a slightly warm wash with a blue practical below it for the backs of the figures, a warm front fill for faces and a
        // cool rim on Ohm's spot. The set's emissive screens and strips bloom; the probe sees the room once. The 2026-10-14
        // audit found the lab dim and cold after the warm study (and the polished grade had kept the classroom's 0.2 exposure):
        // the ambient is lifted and warm at the equator, the background a step up, every practical brighter.
        // Names differ from the classroom's (Window Key, Ceiling Fill, Bench Light, Rim) so CinematicPolish's warm grade of
        // those lights leaves these alone; LabContinuityTest and the looks (LabLooks) depend on them too.
        void LabSetLighting(Transform lab)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.58f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.42f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.15f, 0.14f);
            RenderSettings.fog = false;
            camera.backgroundColor = new Color(0.05f, 0.08f, 0.12f);

            var root = Node("Lighting", lab).transform;
            Light Spot(string name, Vector3 at, Vector3 aim, Color color, float intensity, float range, float angle, LightShadows shadows)
            {
                var light = Node(name, root, at).AddComponent<Light>();
                light.type = LightType.Spot;
                light.transform.rotation = Quaternion.LookRotation(aim - at);
                light.color = color;
                light.intensity = intensity;
                light.range = range;
                light.spotAngle = angle;
                light.innerSpotAngle = angle * 0.55f;
                light.shadows = shadows;
                return light;
            }
            Light Point(string name, Vector3 at, Color color, float intensity, float range)
            {
                var light = Node(name, root, at).AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.intensity = intensity;
                light.range = range;
                light.shadows = LightShadows.None;
                return light;
            }

            // The key sits just under the magnet rig (its bottom is at 2.11; inside the rig, the coils threw wedges of shadow
            // across the bench) and toward the cameras so the meter faces get it; warm now, at the low end of the audit's
            // 3.2-3.6 (at 4, cool, the light bench top went white; the exposure is 0.45 in the polished grade too now).
            var key = Spot("Lab Key", new Vector3(0f, 2.05f, -1.0f), DeskTopC + new Vector3(0f, 0f, 0.15f), new Color(1f, 0.93f, 0.8f), 3.2f, 4.5f, 75f, LightShadows.Soft);
            key.shadowStrength = 0.7f;
            for (int i = 0; i < 6; i++)
            {
                float a = (i + 0.5f) * Mathf.PI / 3f;
                var at = new Vector3(Mathf.Sin(a) * 3.6f, 2.45f, Mathf.Cos(a) * 3.6f);
                Point("Cove " + (i + 1), at, new Color(0.55f, 0.8f, 1f), 1.6f, 4.5f);
                // Its gold twin for the finale: off and at zero until the "finale" look fades it up (and the blue one down).
                Point("Finale Cove " + (i + 1), at, new Color(1f, 0.78f, 0.45f), 0f, 4.5f).enabled = false;
            }
            // The back wall's display is now a dark green board (the .blend's "Material" panel over it, centre (0, 1.64, 5.55)):
            // a soft, slightly warm wash so it reads as a board, and a blue practical below it for the backs of the figures.
            Spot("Board Wash", new Vector3(0f, 2.7f, 3.0f), new Vector3(0f, 1.64f, 5.55f), new Color(1f, 0.96f, 0.9f), 2.6f, 5f, 75f, LightShadows.None);
            Point("Display Glow", new Vector3(0f, 1.2f, 4.2f), new Color(0.38f, 0.62f, 1f), 1.8f, 4f);
            Spot("Face Fill", new Vector3(0.4f, 2.0f, -2.6f), new Vector3(0f, 1.25f, 0.6f), new Color(1f, 0.9f, 0.78f), 1.5f, 6.5f, 70f, LightShadows.None);
            Spot("Lab Rim", new Vector3(0.8f, 2.6f, 3.2f), OhmFeetC + Vector3.up * 1.3f, new Color(0.62f, 0.8f, 1f), 2.5f, 5f, 60f, LightShadows.None);
            LabShaft();

            // One capture at start: the lab reflected in the bench top, the meter glass and the floor.
            var probe = Node("Reflection Probe", root, new Vector3(0f, 1.5f, 0f)).AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(12.2f, 3.2f, 12.2f);
            probe.center = Vector3.zero;
            probe.boxProjection = true;
            probe.resolution = 128;

            var volume = GlobalPost(root, "Classroom_Global", bloom: 0.6f, vignette: 0.2f, exposure: 0.45f);
            LabLooks(root, volume);
        }

        // The lamp over the table, seen: a soft warm cone from under the magnet rig down to the table top (it fades out before
        // the apparatus) with slow dust motes drifting in it. Under "Classroom Ceiling", as the study's God Rays sit under its
        // window side: Scene 8 keeps both hidden until the ceiling spreads in, and they grow with it. The cone is faint (the
        // only board it crosses in the wide shots is the empty slab) and still; the motes thin and dim under reduced motion.
        void LabShaft()
        {
            var ceiling = refs["Classroom Ceiling"].transform;
            ShaftCone("Lab Shaft", ceiling, ceiling.InverseTransformPoint(new Vector3(0f, 2.08f, 0f)), 1.25f, 1.0f, new Color(1f, 0.9f, 0.72f, 0.05f));
            var motes = Emitter("Lab Motes", ceiling, ceiling.InverseTransformPoint(new Vector3(0f, 1.45f, 0f)), Glow("Glow_LabMote", Hdr(new Color(1f, 0.92f, 0.75f), 1.6f)), ps =>
            {
                var main = ps.main;
                main.prewarm = true; // in the cone from the first frame
                main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.05f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.014f);
                main.gravityModifier = 0.002f; // a slow sink
                main.maxParticles = 80;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = ps.emission;
                emission.rateOverTime = 7f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(1.3f, 1.1f, 1.3f);
                shape.randomDirectionAmount = 1f;
            });
            motes.gameObject.AddComponent<ReducedMotionFx>().calm = 0.35f;
        }

        // The lab's two lighting states for the director's 'look' cue: "classroom" is the room as built; "finale" (Scene 13,
        // 'look 2.0 finale' on Ohm's return) is the same room warmer and brighter: the key up a quarter, the gold coves up and
        // the blue ones down, the fills up, a warmer and lifted ambient and background, and its own post volume (a touch more
        // exposure, a warm filter). The finale's lights and volume start off; the crossfade owns them from the cue (the shared
        // probe stays on because the finale is listed last). Scene 8 installs its own study -> classroom pair over this one.
        void LabLooks(Transform lighting, Volume classroomVolume)
        {
            var finaleVolume = GlobalPost(lighting, "Finale_Global", bloom: 0.6f, vignette: 0.18f, exposure: 0.6f);
            finaleVolume.name = "Finale Volume";
            finaleVolume.weight = 0f;
            if (finaleVolume.sharedProfile.TryGet<ColorAdjustments>(out var colour)) { colour.colorFilter.Override(new Color(1f, 0.95f, 0.86f)); EditorUtility.SetDirty(colour); }
            var classroom = CaptureLook("classroom", lighting);
            classroom.volume = classroomVolume;
            var finale = CaptureLook("finale", lighting);
            finale.volume = finaleVolume;
            for (int i = 0; i < finale.lights.Length; i++)
            {
                string name = finale.lights[i].name;
                if (name.StartsWith("Finale Cove")) finale.intensities[i] = 1.4f;
                else finale.intensities[i] *= name == "Lab Key" ? 1.25f : name == "Face Fill" ? 1.3f : name == "Board Wash" ? 1.15f : name == "Display Glow" ? 1.2f : name.StartsWith("Cove") ? 0.6f : 1f;
            }
            finale.ambientSky = new Color(0.6f, 0.6f, 0.66f);
            finale.ambientEquator = new Color(0.62f, 0.52f, 0.42f);
            finale.ambientGround = new Color(0.22f, 0.19f, 0.16f);
            finale.background = new Color(0.09f, 0.1f, 0.13f);
            director.looks = new[] { classroom, finale };
        }

        // ------------------------------------------------------------------ lighting

        // Warm key through the window (soft shadows), cool ceiling fill, a rim from behind Ohm, a board wash and a bench
        // light; Trilight ambient, fog off, Scene 4's ACES/bloom volume at the classroom values. All under "Lab".
        void ClassroomLighting(Transform lab)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.66f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.56f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.17f, 0.15f);
            RenderSettings.fog = false;
            camera.backgroundColor = new Color(0.74f, 0.83f, 0.94f);

            var root = Node("Lighting", lab).transform;
            var key = Node("Window Key", root).AddComponent<Light>();
            key.type = LightType.Directional;
            key.transform.rotation = Quaternion.Euler(35f, 60f, 0f); // from the window wall (-X), high, a little from the camera side: lights faces toward -Z
            key.color = new Color(1f, 0.93f, 0.82f);
            key.intensity = 1.5f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.6f;

            // 0.8 m under the ceiling and a little toward the cameras: 0.3 m under it the patch it threw on the ceiling went
            // past the bloom threshold (the high Cards shot sees the ceiling). Front faces get about the same fill as before.
            var fill = Node("Ceiling Fill", root, new Vector3(0.4f, ClassroomCeilingY - 0.8f, -0.3f)).AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.78f, 0.86f, 1f);
            fill.intensity = 0.6f;
            fill.range = 7f;
            fill.shadows = LightShadows.None;

            var rim = Node("Rim", root, new Vector3(0.6f, 2.5f, 2.1f)).AddComponent<Light>();
            rim.type = LightType.Spot;
            rim.transform.rotation = Quaternion.LookRotation(OhmFeetC + Vector3.up * 1.3f - rim.transform.position);
            rim.color = new Color(1f, 0.95f, 0.9f);
            rim.intensity = 2f;
            rim.range = 4f;
            rim.spotAngle = 60f;
            rim.innerSpotAngle = 30f;
            rim.shadows = LightShadows.None;

            // 1.6 m out from the wall, not 0.8: that close it lit the wall just over the board four times as hard as the board
            // itself (a white patch at the top of Scene 13's finale). Same light on the board's centre, more even top to bottom.
            var wash = Node("Board Wash", root, new Vector3(0f, 2.75f, 1.0f)).AddComponent<Light>();
            wash.type = LightType.Spot;
            wash.transform.rotation = Quaternion.LookRotation(BoardCentreC - wash.transform.position);
            wash.color = new Color(1f, 0.97f, 0.9f);
            wash.intensity = 1.6f;
            wash.range = 4.5f;
            wash.spotAngle = 120f; // wide and soft: no visible cone on the board
            wash.innerSpotAngle = 90f;
            wash.shadows = LightShadows.None;

            var benchLight = Node("Bench Light", root, new Vector3(0f, 2.4f, -0.3f)).AddComponent<Light>();
            benchLight.type = LightType.Spot;
            benchLight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            benchLight.color = new Color(1f, 0.97f, 0.92f);
            benchLight.intensity = 3f;
            benchLight.range = 4f;
            benchLight.spotAngle = 70f;
            benchLight.innerSpotAngle = 40f;
            benchLight.shadows = LightShadows.Soft;

            // One capture at start: the room reflected on the bench top and glass.
            var probe = Node("Reflection Probe", root, new Vector3(0f, 1.5f, 0.4f)).AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(ClassroomHalfWidth * 2f, ClassroomCeilingY, 6.6f);
            probe.center = new Vector3(0f, 0f, -0.7f);
            probe.boxProjection = true;
            probe.resolution = 128;

            GlobalPost(root, "Classroom_Global", bloom: 0.6f, vignette: 0.2f, exposure: 0.2f);
        }

        // ------------------------------------------------------------------ audience

        // Four student heads along the bottom edge of the Bench Wide frame, ~0.8 m from its lens (head tops 29-31 % up the
        // frame; the subtitle box covers the lowest 22 %), matte navy, inside the lens's near blur. Spread to 11-91 %
        // across so the outer two sit in the gutters beside the subtitle box (at x ±0.6 they fell outside the frame).
        // Built once under "Lab"; on sets the active state. Off for Scenes 9-13 unless the shot is wide.
        protected GameObject Audience(bool on)
        {
            if (!refs.TryGetValue("Audience", out var audience))
            {
                audience = Ref(Node("Audience", refs.TryGetValue("Lab", out var lab) ? lab.transform : null));
                var matte = Lit("Classroom_Student", new Color(0.11f, 0.13f, 0.2f), 0f, 0.25f);
                foreach (var (x, y, z, r) in new[] { (-0.42f, 0.97f, -2.25f, 0.14f), (-0.14f, 0.98f, -2.22f, 0.13f), (0.18f, 0.98f, -2.27f, 0.13f), (0.44f, 0.96f, -2.24f, 0.14f) })
                {
                    var student = Node("Student", audience.transform, new Vector3(x, 0f, z)).transform;
                    foreach (var part in new[]
                    {
                        Prim(PrimitiveType.Sphere, "Head", student, new Vector3(0f, y, 0f), Vector3.one * r * 2f, matte),
                        Prim(PrimitiveType.Capsule, "Shoulders", student, new Vector3(0f, y - r - 0.22f, 0.02f), new Vector3(0.42f, 0.2f, 0.26f), matte),
                    })
                    {
                        var renderer = part.GetComponent<Renderer>();
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        renderer.receiveShadows = false;
                    }
                }
            }
            audience.SetActive(on);
            return audience;
        }

        // ------------------------------------------------------------------ board API

        // Board space: u 0..1 left to right, v 0..1 bottom to top, as seen from the cameras.
        Vector3 BoardLocal(Vector2 uv, float lift) => new Vector3((uv.x - 0.5f) * BoardSizeC.x, (uv.y - 0.5f) * BoardSizeC.y, -lift);

        // World position and rotation of a point on the chalk face (lifted toward the cameras), for mounting canvases
        // and cards: the rotation's +Z goes into the wall, so TextMeshPro and UGUI read from the camera side.
        public (Vector3 position, Quaternion rotation) BoardSurface(Vector2 uv, float lift = 0.004f) =>
            (BoardFace.TransformPoint(BoardLocal(uv, lift)), BoardFace.rotation);

        Transform BoardFace => boardFace ? boardFace : throw new InvalidOperationException("Call ClassroomSet() before writing on the board.");

        // Chalk text on the board, centred (or left-anchored) at boardUV, cap height in metres (BoardTitleCap / BoardCap for
        // the wide shots, BoardsShotTitleCap / BoardsShotCap for the Boards shot), wrapping at `wrap` metres when > 0.
        // Registered as "Board <first two words>" (or `name`) and hidden, so a cue shows it; with a `parent` (a BoardGroup)
        // it is part of that group instead: visible, not registered.
        protected GameObject Board(string text, Color color, Vector2 boardUV, float cap, float wrap = 0f, TextAlignmentOptions align = TextAlignmentOptions.Center, string name = null, Transform parent = null)
        {
            string words = name ?? FirstWords(text);
            var go = Node(parent ? "Chalk " + words : UniqueRef("Board " + words), parent ? parent : BoardFace, BoardLocal(boardUV, 0.004f));
            var label = WorldText.Card("Text", text, color, go.transform, Vector3.zero, cap, null, wrap);
            if (chalkFace) label.fontSharedMaterial = chalkFace;
            if (align != TextAlignmentOptions.Center)
            {
                // The anchor point is the text's left (or right) edge instead of its centre.
                label.alignment = align;
                float pivot = align switch
                {
                    TextAlignmentOptions.Left or TextAlignmentOptions.TopLeft or TextAlignmentOptions.BottomLeft or TextAlignmentOptions.MidlineLeft => 0f,
                    TextAlignmentOptions.Right or TextAlignmentOptions.TopRight or TextAlignmentOptions.BottomRight or TextAlignmentOptions.MidlineRight => 1f,
                    _ => 0.5f,
                };
                label.rectTransform.pivot = new Vector2(pivot, 0.5f);
            }
            var pop = go.AddComponent<PopIn>(); // chalk appears softly: no overshoot, no bob
            pop.duration = 0.35f;
            pop.fromScale = 0.7f;
            pop.overshoot = 1.02f;
            if (parent) return go;
            go.SetActive(false);
            return Ref(go);
        }

        // A hidden group on the board for a chalk diagram (lines and labels), registered as "Board <name>".
        protected Transform BoardGroup(string name)
        {
            var group = Ref(Node(UniqueRef("Board " + name), BoardFace));
            group.SetActive(false);
            return group.transform;
        }

        // A chalk line through board-UV points; under a BoardGroup when `parent` is given, else straight on the board.
        protected LineRenderer BoardLine(Vector2[] points, Color color, float width, Transform parent = null, bool loop = false)
        {
            var line = Polyline("Chalk Line", parent ? parent : BoardFace, Translucent("Chalk_" + ColorUtility.ToHtmlStringRGB(color), new Color(color.r, color.g, color.b, 0.92f)),
                width, loop, false, points.Select(p => BoardLocal(p, 0.004f)).ToArray());
            line.alignment = LineAlignment.TransformZ;
            line.textureMode = LineTextureMode.Stretch;
            return line;
        }

        // A lighter chalk-framed area of the board (rect in board UV) behind an observation table or graph canvas.
        // Registered as `name` ("Board Panel", "Board Panel 2", ...) and hidden; mount the canvas with BoardSurface(rect.center).
        protected GameObject BoardPanel(Rect rect, string name = "Board Panel")
        {
            var panel = Ref(Node(UniqueRef(name), BoardFace, BoardLocal(rect.center, 0.002f)));
            Prim(PrimitiveType.Quad, "Panel", panel.transform, Vector3.zero, new Vector3(rect.width * BoardSizeC.x, rect.height * BoardSizeC.y, 1f),
                Lit("Classroom_BoardPanel", new Color(0.16f, 0.28f, 0.25f), 0f, 0.15f));
            var frame = Polyline("Chalk Frame", panel.transform, Translucent("Chalk_" + ColorUtility.ToHtmlStringRGB(Chalk), new Color(Chalk.r, Chalk.g, Chalk.b, 0.92f)), 0.012f, true, false,
                new[] { new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax) }
                    .Select(p => BoardLocal(p, 0.004f) - BoardLocal(rect.center, 0.002f)).ToArray());
            frame.alignment = LineAlignment.TransformZ;
            panel.SetActive(false);
            return panel;
        }

        // ---- Scene 7's chalk (ART_DIRECTION §3), shared so Scene 8 opens its match-cut with the same board.
        // The headlines use the L tier: two XL headlines (2.0 + 1.8 m with TMP's bold spacing) do not fit side by side on
        // the 3.2 m board; the question uses the S tier so its two lines (2.6 m) fit a chalk panel inside the board (at
        // BoardCap they span 3.0 m). Both are sized for the Bench Wide lens that reads them.
        protected static float TwoCluesHeadlineCap => WorldText.CapAt(WorldText.Px.Large, BoardCentreC, BenchWideCamC, BenchWideLookC, BenchWideFov);   // 0.14
        protected static float TwoCluesSmallCap => WorldText.CapAt(WorldText.Px.Small, BoardCentreC, BenchWideCamC, BenchWideLookC, BenchWideFov);      // 0.093
        protected static readonly Rect TwoCluesQuestionPanel = new Rect(0.075f, 0.48f, 0.85f, 0.27f);   // under the headlines, above Ohm's head (v ≤ 0.45)

        // The two clues as chalk headlines, the question in a chalk panel, a battery-wire loop. Nothing about resistance
        // or proportionality (§9). Headlines at v 0.9 clear the panel; the panel bottom (v 0.48) clears Ohm's head, which
        // reaches v ≈ 0.45 in Bench Wide even when he stands up. Refs "Board Voltage", "Board Current", "Board Question
        // Panel", "Board Question", "Board Circuit": hidden for Scene 7's cues, or `shown` for Scene 8's first frame.
        protected GameObject[] TwoCluesBoard(bool shown)
        {
            var items = new[]
            {
                Board("Voltage → V", palette.voltage, new Vector2(0.25f, 0.9f), TwoCluesHeadlineCap, name: "Voltage"),
                Board("Current → I", palette.current, new Vector2(0.75f, 0.9f), TwoCluesHeadlineCap, name: "Current"),
                BoardPanel(TwoCluesQuestionPanel, "Board Question Panel"),
                Board("Is there a mathematical relationship\nbetween voltage and current?", Chalk, TwoCluesQuestionPanel.center, TwoCluesSmallCap, name: "Question"),
                TwoCluesSketch().gameObject,
            };
            foreach (var item in items)
            {
                item.SetActive(shown);
                if (shown) foreach (var pop in item.GetComponentsInChildren<PopIn>(true)) { pop.fromScale = 1f; pop.overshoot = 1f; } // already there at the cut: no pop
            }
            return items;
        }

        // Battery-wire loop in chalk (Scene07_two_clues board), lower-left of the board where neither Ohm nor Wattson
        // stands: a cell on the left side of the loop with the "V" clue beside it, the "I" clue over the top wire. No
        // direction arrow: conventional current (+ → −) is labelled where it is first explained (Scene 9, S09_Conventional),
        // and an unlabelled arrow here would run against the drift the learner just answered at S05_Drift. The loop starts
        // at u 0.12 so the "V" (0.05 left of it) stays inside the slate's frame.
        Transform TwoCluesSketch()
        {
            var loop = BoardGroup("Circuit");
            const float l = 0.12f, r = 0.32f, b = 0.10f, t = 0.34f, minus = 0.19f, plus = 0.26f, width = 0.014f;
            BoardLine(new[] { new Vector2(l, plus), new Vector2(l, t), new Vector2(r, t), new Vector2(r, b), new Vector2(l, b), new Vector2(l, minus) }, Chalk, width, loop);
            BoardLine(new[] { new Vector2(l - 0.025f, plus), new Vector2(l + 0.025f, plus) }, Chalk, width, loop);
            BoardLine(new[] { new Vector2(l - 0.012f, minus), new Vector2(l + 0.012f, minus) }, Chalk, width * 1.7f, loop);
            Board("V", palette.voltage, new Vector2(l - 0.05f, (plus + minus) * 0.5f), TwoCluesSmallCap, parent: loop);
            Board("I", palette.current, new Vector2((l + r) * 0.5f, t + 0.065f), TwoCluesSmallCap, parent: loop);
            return loop;
        }

        string UniqueRef(string name)
        {
            string key = name;
            for (int i = 2; refs.ContainsKey(key); i++) key = name + " " + i;
            return key;
        }

        static string FirstWords(string text, int count = 2) =>
            string.Join(" ", Regex.Replace(text ?? "", "<[^>]+>", "").Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Take(count));

        // Chalk look for TextMeshPro: the font's SDF material with a soft, slightly dilated edge and a faint white dust halo;
        // the vertex colour carries the chalk colour, so one asset serves every tint.
        Material ChalkMaterial()
        {
            if (!palette.font) return null;
            Directory.CreateDirectory(Art + "/Materials");
            var material = Asset(Art + "/Materials/Chalk.mat", () => new Material(palette.font.material));
            material.shader = Shader.Find("TextMeshPro/Distance Field");
            material.SetTexture(ShaderUtilities.ID_MainTex, palette.font.atlasTexture);
            material.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.06f);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.08f);
            material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.32f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(1f, 1f, 1f, 0.35f));
            material.DisableKeyword("GLOW_ON");
            material.DisableKeyword("UNDERLAY_ON");
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------ contact shadows

        // A soft dark decal on the surface under an object (its render bounds' bottom), `size` metres across, so nothing
        // floats. It follows the object: a child when the object is uniformly scaled, otherwise a sibling (a non-uniform
        // parent would skew the quad). Generated gradient texture, alpha-blended, no shadows of its own.
        protected GameObject ContactShadow(Transform under, Vector2 size)
        {
            var bounds = RenderBounds(under.gameObject);
            var quad = Prim(PrimitiveType.Quad, "Contact Shadow", null, Vector3.zero, new Vector3(size.x, size.y, 1f), contactShadow ? contactShadow : Decal("ContactShadow", new Color(0f, 0f, 0f, 0.62f), ContactShadowTexture()), new Vector3(90f, 0f, 0f));
            quad.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.003f, bounds.center.z);
            var renderer = quad.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var s = under.lossyScale;
            bool uniform = Mathf.Abs(s.x - s.y) < 1e-3f && Mathf.Abs(s.y - s.z) < 1e-3f;
            quad.transform.SetParent(uniform ? under : under.parent, true);
            return quad;
        }

        // Sized from the object's footprint (1.4x, plus 4 cm of penumbra).
        protected GameObject ContactShadow(Transform under)
        {
            var size = RenderBounds(under.gameObject).size;
            return ContactShadow(under, new Vector2(size.x * 1.4f + 0.04f, size.z * 1.4f + 0.04f));
        }

        static Texture2D ContactShadowTexture()
        {
            string png = Art + "/Textures/ContactShadow.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
            if (existing) return existing;
            Directory.CreateDirectory(Art + "/Textures");
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 1f, r)); // solid core, fades to nothing at the edge
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            File.WriteAllBytes(png, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(png);
            if (AssetImporter.GetAtPath(png) is TextureImporter importer)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(png);
        }

        // Alpha-blended unlit decal: colour times a texture's alpha, drawn before the other transparents.
        static Material Decal(string name, Color color, Texture2D texture) =>
            Mat(name, "Universal Render Pipeline/Unlit", m =>
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0);
                m.SetFloat("_Cull", (float)CullMode.Off);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent - 20;
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", texture);
            });

        // ------------------------------------------------------------------ the historical apparatus in this frame

        // Ohm's 1826 apparatus on the Victorian desk of the study placed in this frame (Scenes 7-8), Georg Ohm on Scene 4's
        // spot behind it, and a contact shadow under every base.
        protected GameObject ClassroomApparatus()
        {
            var apparatus = HistoricalApparatus(OhmDeskC, Quaternion.identity);
            GeorgOhm(apparatus.transform);
            foreach (var part in apparatus.GetComponentsInChildren<Transform>(true))
                if (part.name == "Galvanometer Base" || part.name == "Source Base" || part.name == "Post Base" || part.name.StartsWith("Mercury Cup ") || part.name == "Ohm Notebook")
                    ContactShadow(part);
            return apparatus;
        }

        // The modern circuit: Rig() already builds in the classroom frame with its own contact shadows and markers.
        protected CircuitRig ClassroomRig(bool assembled, bool keyClosed, float voltage, bool active = true) => Rig(assembled, keyClosed, voltage, active);
    }
}
