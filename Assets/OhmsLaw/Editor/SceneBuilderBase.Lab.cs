using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw.Editor
{
    // Stage kit for the historical lab: Ohm's 1826 apparatus (one layout, Scene 4 on), Scene 4's study placed in the
    // classroom frame for Scenes 7-8 (same desk, same apparatus, Ohm on the same spot, same light, the same camera side),
    // its chalk slate, and the pieces Scene 8's transformation turns into the classroom lab.
    public abstract partial class SceneBuilderBase
    {
        const string OhmPrefab = Root + "/Scene04/Prefabs/Ohm_Scene04.prefab";

        // ------------------------------------------------------------------ the study in the classroom frame

        // Scene 4's study turned half round about Y and shifted so the Victorian desk (1.61 x 0.66 m, top 0.80) sits 0.25 m
        // right of the bench centre: the 2.0 m modern bench that replaces it in Scene 8 then still clears the window wall
        // (inner face x 1.045). Ohm, the desk and Scene 4's wire keep their places in the room, the cameras stay on the
        // room side (-Z) like Scene 4's, and the classroom shell (x ±3.2, z -4..2.6, ceiling 3.0) encloses the whole study,
        // so Scenes 9-13 and every classroom-frame kit keep working. (Properties: static initializer order across the
        // partial files is unspecified.)
        public static readonly Vector3 StudyOrigin = new Vector3(-2.17f, 1.06f, -1.77f);
        public static readonly Quaternion StudyTurn = Quaternion.Euler(0f, 180f, 0f);
        public static Vector3 StudyPoint(Vector3 study) => StudyOrigin + StudyTurn * study;
        public static Vector3 OhmDeskC => StudyPoint(DeskTop);                 // (0.25, 0.80, 0): the Victorian desk top centre
        public static Vector3 WireStudyC => StudyPoint(Wire4);                 // (0.54, 0.88, 0.13): the wire we dived into
        public static Vector3 HoverStudyC => StudyPoint(Hover);                // (-0.67, 1.26, -1.32): where Wattson settled in Scene 4
        public static Vector3 OhmHeadStudyC => StudyPoint(OhmHead);
        // Scene 4's shots mapped into this frame: SH01 (the group's view of the desk) and SH09b / SH11b on the dive axis.
        static Vector3 DiveAxis => (Wire4 - Hover).normalized;                 // study frame
        public static Vector3 StudyWideCamC => new Vector3(-1.6f, 1.65f, -2.6f);
        public static Vector3 StudyWideLookC => new Vector3(0f, 0.95f, 0.3f);
        public const float StudyWideFov = 46f;
        public static Vector3 StudyAimCamC => StudyPoint(Wire4 - DiveAxis * 1.05f + Vector3.up * 0.12f);
        public static Vector3 StudyMacroCamC => StudyPoint(Wire4 - DiveAxis * 0.12f);    // the wire ~11 % of the frame tall at 30°
        // The desk straight on (Scene 8's transformation): the whole desk, the 2.0 m bench that replaces it, Ohm's head
        // and Wattson beside the desk all in frame.
        public static Vector3 StudyDeskCamC => new Vector3(0.05f, 1.5f, -2.05f);
        public static Vector3 StudyDeskLookC => new Vector3(0.05f, 0.98f, 0.15f);
        public const float StudyDeskFov = 46f;                                         // bench 10-87 % across, Ohm's head top at 89 %
        public static Vector3 WattsonStudyC => new Vector3(0.78f, 1.18f, -0.55f);     // right of Ohm over the desk's right half in Study Wide (~70 %): never between the lens and his face
        public static Vector3 TransformPointC => new Vector3(-0.95f, 0.9f, -0.35f);   // beside the desk's left end in Study Desk (13 %), below the slate's chalk
        // The chalk slate: left of the desk in front of the fireplace (the fire still glows below it), turned to Study Wide,
        // 3.0 m from that lens. The board's UV space at x0.6 (a 1.92 x 0.9 m slate) keeps the two clues' chalk at the pixel
        // sizes they were set for on the classroom board (36 px headlines, 24 px question).
        static readonly Vector3 EaselFoot = new Vector3(-1.05f, -0.02f, 0.75f);
        const float SlateScale = 0.6f, SlateCentreY = 1.59f;

        // Scene 4's study (Lab_Scene04 and its candle-and-window light) in the classroom frame, grouped for Scene 8's sweep.
        // Refs: "Historical Lab" (root), "Study Lighting", pivot groups "Study Desk" (the Victorian desk and its extension),
        // "Study Floor", "Study Back" (fireplace wall), "Study Window Side", "Study Door Side", "Study Ceiling", the desk
        // dressing "Study Lamp" / "Study Candle" / "Study Inkstand" / "Study Papers" / "Study Globe" / "Study Eyewear" (each
        // pivoted on itself, the lamp and candle lights inside), "Study Easel" with boardFace on its slate; markers "Wattson
        // Study", "Transform Point", "Ohm Point", "Study Wire Point", "Bench Point"; cameras "Study Wide", "Study Aim",
        // "Study Macro", "Study Desk". The wall behind the cameras is hidden. The study's reflection probe is baked here, so
        // hide anything else that should not show in it (Scene 8's classroom) before calling.
        protected GameObject HistoricalLab()
        {
            var root = Ref(Node("Historical Lab"));
            var room = Study();
            // Grouping needs a plain hierarchy: children of a prefab instance cannot be re-parented.
            PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            room.transform.SetParent(root.transform, false);
            var lighting = StudyLighting();
            lighting.name = "Study Lighting";
            lighting.SetParent(root.transform, false);
            Ref(lighting.gameObject);

            // Room for the apparatus on the desk (Scene 4's variant hides the same pieces); the chair would stand in the
            // desk shots and against the bench, and Ohm works standing.
            foreach (var name in new[] { "Book_Pile_4", "Open_Book", "magnify_glass", "Chair" })
                FindChild(room, name).gameObject.SetActive(false);
            var hat = room.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Object_4"); // the top hat (gone from newer exports of the model); the glasses stay
            if (hat) hat.gameObject.SetActive(false);
            // The paper on the floor by the fireplace belongs to the room, not the desk.
            FindChild(room, "Paper_001").SetParent(FindChild(room, "Interior"), true);

            // Sort the room into regions while it still stands in Scene 4's own frame, then place it.
            var regions = StudyRegions(room);
            root.transform.SetPositionAndRotation(StudyOrigin, StudyTurn);

            // The desk dressing, each piece pivoted on its own base so it shrinks in place (and out from under the desk, so
            // hiding the desk does not cut it short); the lamp and candle take their flames' lights with them.
            Transform Dress(string piece, string name, string light = null)
            {
                var wrap = PivotOn(FindChild(room, piece), name);
                wrap.SetParent(room.transform, true);
                if (light != null) FindChild(lighting.gameObject, light).SetParent(wrap, true);
                return wrap;
            }
            Dress("Oil_Lamp_Large", "Study Lamp", "Oil Lamp");
            Dress("Candle_Holder_5", "Study Candle");
            Dress("Inkstand", "Study Inkstand");
            Dress("Paper", "Study Papers");
            Dress("Globe", "Study Globe");
            Dress("hat_and_glass", "Study Eyewear");

            var top = ClassroomCeilingY - 0.25f;   // the study ceiling (y 2.75): walls lift away into it
            PivotGroup("Study Desk", root.transform, OhmDeskC, regions["Desk"]);
            PivotGroup("Study Floor", root.transform, new Vector3(0f, -0.02f, 0f), regions["Floor"]);
            PivotGroup("Study Back", root.transform, new Vector3(-0.85f, top, 1.55f), regions["Back"]);
            PivotGroup("Study Window Side", root.transform, new Vector3(1.05f, top, -0.7f), regions["Window"].Append(FindChild(lighting.gameObject, "God Rays")));
            PivotGroup("Study Door Side", root.transform, new Vector3(-2.52f, top, -0.7f), regions["Door"]);
            PivotGroup("Study Ceiling", root.transform, new Vector3(-0.72f, top, -0.72f), regions["Ceiling"]);
            PivotGroup("Study North", root.transform, new Vector3(-0.72f, 0f, -3f), regions["North"]).gameObject.SetActive(false); // behind every camera

            StudyEasel(root.transform);

            Marker("Wattson Study", WattsonStudyC);
            Marker("Transform Point", TransformPointC);
            var ohmPoint = Marker("Ohm Point", OhmHeadStudyC).transform;
            var wirePoint = Marker("Study Wire Point", WireStudyC).transform;
            var deskPoint = refs.TryGetValue("Bench Point", out var bench) ? bench.transform : Marker("Bench Point", OhmDeskC + Vector3.up * 0.1f).transform; // Scene 7: Ohm's desk
            Vcam("Study Wide", StudyWideCamC, StudyWideLookC, StudyWideFov, ohmPoint, lensWide, noise: 0.5f);
            Vcam("Study Aim", StudyAimCamC, WireStudyC, 30f, wirePoint, lensMedium);
            Vcam("Study Macro", StudyMacroCamC, WireStudyC, 30f, wirePoint, lensMedium); // same lens as Aim: the pull-back is a dolly
            Vcam("Study Desk", StudyDeskCamC, StudyDeskLookC, StudyDeskFov, deskPoint, lensMedium);

            // Same room, same light as Scene 4, but the baked cubemap is sampled in world orientation: bake it again here.
            var probe = lighting.GetComponentInChildren<ReflectionProbe>(true);
            if (probe) BakeProbe(probe, "Study");
            return root;
        }

        // Region of every piece of the study, decided in Scene 4's frame by name, else by where its renderers stand:
        // the window wall is -X, the fireplace wall -Z, the door wall +X, the wall behind the cameras +Z.
        static Dictionary<string, List<Transform>> StudyRegions(GameObject room)
        {
            var regions = new[] { "Desk", "Floor", "Back", "Window", "Door", "Ceiling", "North" }.ToDictionary(r => r, r => new List<Transform>());
            var units = new List<Transform>();
            foreach (Transform child in room.transform)
                if (child.name == "INTERIOR_") units.AddRange(FindChild(room, "Interior").Cast<Transform>());
                else if (child.name == "walls") units.AddRange(child.Cast<Transform>());
                else units.Add(child);
            var wall = units.FirstOrDefault(u => u.name == "Wall");
            if (wall) { units.Remove(wall); units.AddRange(wall.Cast<Transform>()); }

            foreach (var unit in units)
            {
                string name = unit.name;
                var renderers = unit.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue; // empty picture slots stay where they are
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                Vector3 c = bounds.center;
                string region =
                    name == "Victorian_Desk" || name == "Chair" || name == "chock_and_duster" || name == "glass_and_ice_cube_" ? "Desk"
                    : name == "floor" || name == "Interior_Interior_0" || name == "Interior_Interior_0__1_" || name == "Victorian_Rug" || name == "Paper_001" ? "Floor"
                    : name == "ceilling_" || name == "Interior_Interior_0__2_" || name.StartsWith("ceiling_lamo") ? "Ceiling"
                    : name == "mystery_board" || name.StartsWith("Point_Light") ? "North"
                    : name.StartsWith("Floor_Book_Pile") ? "Back"
                    : c.x < -3.0f ? "Window"
                    : c.z < -2.9f ? "Back"
                    : c.x > 0.1f ? "Door"
                    : c.z > 1.0f ? "North"
                    : null;
                if (region != null) regions[region].Add(unit);
            }
            return regions;
        }

        // A pivot node at `pivot` (world) under `parent` holding `members` (world poses kept), registered by name: the cues
        // scale the node, so the pivot is where the group collapses to or grows from.
        protected Transform PivotGroup(string name, Transform parent, Vector3 pivot, IEnumerable<Transform> members)
        {
            var group = Ref(Node(name, parent)).transform;
            group.position = pivot;
            group.rotation = Quaternion.identity;
            foreach (var member in members.ToArray()) member.SetParent(group, true);
            return group;
        }

        // Wraps one object in a node at the bottom centre of its render bounds (same parent), registered as `name`, so it
        // can shrink or grow in place whatever its own pivot is.
        protected Transform PivotOn(Transform item, string name)
        {
            var bounds = RenderBounds(item.gameObject);
            var wrap = Ref(Node(name, item.parent)).transform;
            wrap.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            item.SetParent(wrap, true);
            return wrap;
        }

        // A Victorian lecture slate in a wooden frame on a three-legged easel, turned to Study Wide. Its chalk face becomes
        // boardFace, scaled so the classroom board's 3.2 x 1.5 m UV space maps onto the slate: Board(), BoardPanel() and
        // TwoCluesBoard() write on it unchanged. Ref "Study Easel" (pivot at its feet).
        void StudyEasel(Transform parent)
        {
            if (!chalkFace) chalkFace = ChalkMaterial();
            var wood = Lit("DarkWood", new Color(0.2f, 0.11f, 0.06f), 0f, 0.4f);
            var slate = Lit("Slate", new Color(0.1f, 0.12f, 0.11f), 0f, 0.3f);
            var easel = Ref(Node("Study Easel", parent)).transform;
            easel.position = EaselFoot;
            var toCamera = Vector3.ProjectOnPlane(StudyWideCamC - EaselFoot, Vector3.up).normalized;
            easel.rotation = Quaternion.LookRotation(-toCamera); // local +Z away from the camera: the slate's face looks along -Z

            float w = BoardSizeC.x * SlateScale, h = BoardSizeC.y * SlateScale, y = SlateCentreY;
            Prim(PrimitiveType.Cube, "Slate", easel, new Vector3(0f, y, 0f), new Vector3(w, h, 0.025f), slate);
            const float rail = 0.045f;
            foreach (float s in new[] { -1f, 1f })
            {
                Prim(PrimitiveType.Cube, "Rail", easel, new Vector3(0f, y + s * (h + rail) / 2, -0.004f), new Vector3(w + 2 * rail, rail, 0.035f), wood);
                Prim(PrimitiveType.Cube, "Stile", easel, new Vector3(s * (w + rail) / 2, y, -0.004f), new Vector3(rail, h, 0.035f), wood);
            }
            float ledge = y - h / 2 - rail - 0.012f;
            Prim(PrimitiveType.Cube, "Chalk Ledge", easel, new Vector3(0f, ledge, -0.04f), new Vector3(w + 2 * rail, 0.024f, 0.08f), wood);
            Prim(PrimitiveType.Cylinder, "Chalk", easel, new Vector3(-0.35f, ledge + 0.017f, -0.05f), new Vector3(0.011f, 0.035f, 0.011f), Lit("Classroom_Chalk_" + ColorUtility.ToHtmlStringRGB(Chalk), Chalk, 0f, 0.1f), new Vector3(0f, 0f, 90f));
            Prim(PrimitiveType.Cube, "Duster", easel, new Vector3(0.45f, ledge + 0.022f, -0.05f), new Vector3(0.12f, 0.022f, 0.045f), wood);
            // Legs: two in front leaning back to the slate's top corners, one behind as a prop.
            void Strut(string name, Vector3 a, Vector3 b, float radius)
            {
                var leg = Prim(PrimitiveType.Cylinder, name, easel, (a + b) / 2, new Vector3(radius * 2, Vector3.Distance(a, b) / 2, radius * 2), wood).transform;
                leg.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            }
            foreach (float s in new[] { -1f, 1f }) Strut("Leg", new Vector3(s * (w / 2 - 0.05f), 0.02f, -0.16f), new Vector3(s * (w / 2 - 0.12f), y + h / 2 + 0.12f, 0.03f), 0.018f);
            Strut("Back Leg", new Vector3(0f, 0.02f, 0.62f), new Vector3(0f, y + h / 2 + 0.08f, 0.04f), 0.016f);
            Strut("Peg", new Vector3(-w / 3, ledge - 0.01f, 0.02f), new Vector3(-w / 3, ledge - 0.01f, -0.07f), 0.01f);
            Strut("Peg", new Vector3(w / 3, ledge - 0.01f, 0.02f), new Vector3(w / 3, ledge - 0.01f, -0.07f), 0.01f);

            // A warm picture light over the slate, so its face reads as slate under chalk in the firelit room instead of a flat
            // black panel (no light reached it in front of the fireplace). One of the study's lights, so Scene 8's room change
            // fades it with them (the easel itself vanishes earlier).
            var lamp = Node("Slate Light", easel, new Vector3(0f, y + h / 2 + 0.3f, -0.55f)).AddComponent<Light>();
            lamp.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, y, 0f) - lamp.transform.localPosition);
            lamp.transform.SetParent(refs["Study Lighting"].transform, true);
            lamp.type = LightType.Spot;
            lamp.color = new Color(1f, 0.8f, 0.58f);
            lamp.intensity = 2f;
            lamp.range = 2.5f;
            lamp.spotAngle = 110f;
            lamp.innerSpotAngle = 60f;
            lamp.shadows = LightShadows.None;

            boardFace = Node("Board Face", easel, new Vector3(0f, y, -0.0135f)).transform;
            boardFace.localScale = Vector3.one * SlateScale; // identity rotation: +Z into the slate, text reads from the camera side
            Ref(Node("Board Anchor", easel, new Vector3(0f, y, -0.02f)));
        }

        // ------------------------------------------------------------------ Scene 8's two looks

        // The lighting state the last *Lighting() call left (RenderSettings, the camera background) plus that rig's lights at
        // their built intensities, its global volume, reflection probe and directional key, for the SceneDirector's 'look' cue.
        protected SceneDirector.LabLook CaptureLook(string name, Transform lighting)
        {
            var lights = lighting.GetComponentsInChildren<Light>(true);
            return new SceneDirector.LabLook
            {
                name = name,
                lights = lights,
                intensities = lights.Select(l => l.intensity).ToArray(),
                volume = lighting.GetComponentInChildren<Volume>(true),
                probe = lighting.GetComponentInChildren<ReflectionProbe>(true),
                sun = lights.FirstOrDefault(l => l.type == LightType.Directional),
                ambientSky = RenderSettings.ambientSkyColor,
                ambientEquator = RenderSettings.ambientEquatorColor,
                ambientGround = RenderSettings.ambientGroundColor,
                background = camera.backgroundColor,
                fog = RenderSettings.fog,
                fogColor = RenderSettings.fogColor,
                fogDensity = RenderSettings.fogDensity,
            };
        }

        // Builds the scene in `look`: its lights on, every other look's lights off, volumes weighted, the right probe active.
        // RenderSettings and the camera background must already be the look's (its *Lighting() ran last).
        protected static void StartIn(SceneDirector.LabLook look, params SceneDirector.LabLook[] others)
        {
            foreach (var other in others)
            {
                foreach (var light in other.lights) if (light && !look.lights.Contains(light)) light.enabled = false;
                if (other.volume) other.volume.weight = 0f;
                if (other.probe) other.probe.gameObject.SetActive(false);
            }
            if (look.volume) look.volume.weight = 1f;
            if (look.sun) RenderSettings.sun = look.sun;
        }

        // ------------------------------------------------------------------ Scenes 4-8: Ohm's 1826 apparatus

        // The layout on the Victorian desk, in desk-local metres: origin at the desk top centre, +X to Ohm's left (frame
        // right from the room), +Z toward Ohm. Scene 4's test wire stays exactly where Wattson's beam hits it (Scene 5's
        // macro wire and Scene 7's opening copy it); the notebook is Scene 4's; the torsion galvanometer stands where the
        // simple one stood, behind the wire; the source at his right; the mercury cups and the loose wires along the front.
        // Each part sits near the modern instrument it becomes in Scene 8 (source left, galvanometer by the meter tray,
        // loose wires where XY lies, cups where the key goes).
        static readonly Vector3 AppWire = new Vector3(0.29f, 0.08f, 0.13f);
        const float AppWireHalf = 0.21f;
        static readonly Vector3 AppSource = new Vector3(-0.55f, 0f, 0.245f);
        static readonly Vector3 AppNotebook = new Vector3(-0.21f, 0f, 0.26f);
        static readonly Vector3 AppGalvanometer = new Vector3(0.17f, 0f, 0.235f);
        const float AppGalvanometerScale = 0.75f;
        static readonly Vector3 AppCups = new Vector3(0.46f, 0f, -0.17f);
        static readonly Vector3 AppTestWires = new Vector3(-0.20f, 0f, -0.21f);
        static readonly Vector3 AppOhmFeet = new Vector3(-0.02f, -0.82f, 0.59f);     // Scene 4's spot, 0.26 m behind the desk
        // In the classroom frame (Scenes 7-8).
        public static Vector3 ApparatusC => OhmDeskC + AppGalvanometer;              // the torsion galvanometer's base
        public static Vector3 SourceStudyC => OhmDeskC + AppSource;
        public static Vector3 NotebookStudyC => OhmDeskC + AppNotebook;
        public static Vector3 CupsStudyC => OhmDeskC + AppCups;
        public static Vector3 TestWiresStudyC => OhmDeskC + AppTestWires;
        const float GlassRadius = 0.09f, GlassHeight = 0.20f, NeedleHeight = 0.12f, GlassBottom = 0.035f;

        // Ohm's 1826 apparatus on the desk whose top centre is `deskTop`, `turn` taking desk-local axes to the world's
        // (identity in the classroom frame, a half turn in Scene 4's study). Groups, each pivoted on its prop so callouts,
        // pointing and Scene 8's shrink land on it:
        //   "torsion galvanometer"  glass cylinder, base, brass cap, arc scale, needle, its leads             -> AMMETER
        //   "Copper Test Wires"     "Wire Mount" (Scene 4's wire "Copper Wire" on its posts, the source lead) and
        //                           "Loose Test Wires" (four lengths, "Test Wire 1..4")                       -> NICHROME WIRE — XY
        //   "Thermoelectric Source" copper-bismuth junctions ("Hot Junction", "Cold Junction")                -> cells / DC source
        //   "Mercury Cups"          three brushed-metal cups                                                  -> switch / key
        //   "notebook" (+ pen, "Page 1..3"), "Pages Fan", "Ohm Apparatus" (root), "Ohm Hand Wire", "Galvanometer Needle Pivot";
        //   markers "Apparatus Point", "Ohm Notebook Target", "Ohm Galvanometer Target", "Ohm Wire Target".
        // Names are the storyboard's; the callout layer labels them, so nothing here carries world text. Leads are local-space
        // lines, so they move and shrink with their group.
        protected GameObject HistoricalApparatus(Vector3 deskTop, Quaternion turn)
        {
            // Scene 4's polished copper for its wire (and Scene 5's macro copy); satin copper elsewhere: polished metal
            // caught the light and bloomed like a heated filament on the thicker loose wires.
            var polished = Lit("Copper", new Color(0.86f, 0.46f, 0.26f), 1f, 0.72f);
            var copper = Lit("Classroom_Copper", new Color(0.88f, 0.5f, 0.3f), 0.6f, 0.5f);
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
            root.SetPositionAndRotation(deskTop, turn);
            Vector3 W(Vector3 local) => root.TransformPoint(local);
            // Groups start at the root's origin, so their local space is desk space until Pivot() moves their pivots.
            Transform Part(string name, Transform parent = null) => Ref(Node(name, parent ? parent : root)).transform;
            LineRenderer LeadLine(string name, Transform group, params Vector3[] points) => Polyline(name, group, copper, 0.004f, false, false, points);

            // Copper-bismuth source: two vessels, a junction block in the mouth of each, the bismuth bar across them,
            // steam over the hot side and ice on the cold side. The junctions are refs so the hot/cold callouts can aim at them.
            var source = Part("Thermoelectric Source");
            Vector3 hot = AppSource + Vector3.left * 0.09f, cold = AppSource + Vector3.right * 0.09f;
            Prim(PrimitiveType.Cube, "Source Base", source, AppSource + Vector3.up * 0.007f, new Vector3(0.3f, 0.014f, 0.14f), wood);
            Prim(PrimitiveType.Cylinder, "Hot Vessel", source, hot + Vector3.up * 0.054f, new Vector3(0.09f, 0.04f, 0.09f), copper);
            Prim(PrimitiveType.Cylinder, "Cold Vessel", source, cold + Vector3.up * 0.054f, new Vector3(0.09f, 0.04f, 0.09f), zinc);
            Ref(Prim(PrimitiveType.Cube, "Hot Junction", source, hot + Vector3.up * 0.1f, Vector3.one * 0.03f, copper));
            Ref(Prim(PrimitiveType.Cube, "Cold Junction", source, cold + Vector3.up * 0.1f, Vector3.one * 0.03f, copper));
            Prim(PrimitiveType.Cube, "Bismuth Bar", source, AppSource + Vector3.up * 0.108f, new Vector3(0.2f, 0.012f, 0.012f), bismuth);
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
            Pivot(source, W(AppSource + Vector3.up * 0.08f));

            // Scene 4's test wire between its brass posts on dark-wood blocks; the source lead runs along the desk's back edge.
            var wires = Part("Copper Test Wires");
            var mount = Part("Wire Mount", wires);
            Ref(Prim(PrimitiveType.Cylinder, "Copper Wire", mount, AppWire, new Vector3(0.007f, AppWireHalf, 0.007f), polished, new Vector3(0, 0, 90)));
            Vector3 postA = new Vector3(AppWire.x - AppWireHalf, 0f, AppWire.z), postB = new Vector3(AppWire.x + AppWireHalf, 0f, AppWire.z); // A on his right (-X)
            foreach (var foot in new[] { postA, postB })
            {
                Prim(PrimitiveType.Cube, "Post Base", mount, foot + Vector3.up * 0.008f, new Vector3(0.05f, 0.016f, 0.05f), wood);
                Prim(PrimitiveType.Cylinder, "Post", mount, foot + Vector3.up * 0.05f, new Vector3(0.012f, 0.045f, 0.012f), brass);
                Prim(PrimitiveType.Sphere, "Post Cap", mount, foot + Vector3.up * 0.098f, Vector3.one * 0.02f, brass);
            }
            LeadLine("Lead Source-Post", mount, cold + new Vector3(0f, 0.115f, 0f), cold + new Vector3(0.05f, 0.004f, 0.07f), new Vector3(-0.05f, 0.004f, 0.305f),
                new Vector3(0.06f, 0.004f, 0.28f), postA + new Vector3(0f, 0.016f, 0.025f));
            Pivot(mount, W(AppWire));
            // Four loose test wires of different lengths resting on the front of the desk (Scene07_ohm_copper_wires).
            var loose = Part("Loose Test Wires", wires);
            for (int i = 0; i < 4; i++)
            {
                float length = 0.40f - i * 0.06f;
                var centre = AppTestWires + new Vector3(0.03f * i, 0.007f, 0.08f - i * 0.06f);
                Prim(PrimitiveType.Cylinder, "Test Wire " + (i + 1), loose, centre, new Vector3(0.014f, length / 2, 0.014f), copper, new Vector3(0, -4.5f + i * 3f, 90)); // 14 mm reads at the hero lens
            }
            Pivot(loose, W(AppTestWires + Vector3.up * 0.01f));
            Pivot(wires, W(AppTestWires + Vector3.up * 0.01f));

            // Torsion galvanometer (Scene07_galvanometer_closeup), at 3/4 size so it stands behind the wire on the desk: dark
            // metal base with two brass terminals at the front, a brass ring, the glass cylinder with its brass cap and
            // torsion knob, and inside it the needle hanging on its thread in front of the arc scale.
            var galvanometer = Part("torsion galvanometer");
            var body = Node("Galvanometer Body", galvanometer, AppGalvanometer).transform;
            body.localScale = Vector3.one * AppGalvanometerScale;
            float glassTop = GlassBottom + GlassHeight;
            Prim(PrimitiveType.Cylinder, "Galvanometer Base", body, Vector3.up * 0.0125f, new Vector3(0.26f, 0.0125f, 0.26f), gunmetal);
            Prim(PrimitiveType.Cylinder, "Galvanometer Ring", body, Vector3.up * 0.03f, new Vector3(0.2f, 0.005f, 0.2f), brass);
            Prim(PrimitiveType.Cylinder, "Galvanometer Glass", body, Vector3.up * (GlassBottom + GlassHeight / 2), new Vector3(GlassRadius * 2, GlassHeight / 2, GlassRadius * 2), glass);
            Prim(PrimitiveType.Cylinder, "Galvanometer Cap", body, Vector3.up * (glassTop + 0.0075f), new Vector3(0.19f, 0.0075f, 0.19f), brass);
            Prim(PrimitiveType.Cylinder, "Torsion Head", body, Vector3.up * (glassTop + 0.021f), new Vector3(0.04f, 0.006f, 0.04f), brass);
            Prim(PrimitiveType.Sphere, "Torsion Knob", body, Vector3.up * (glassTop + 0.034f), Vector3.one * 0.018f, brass);
            Prim(PrimitiveType.Cylinder, "Torsion Thread", body, Vector3.up * ((NeedleHeight + glassTop) / 2), new Vector3(0.0016f, (glassTop - NeedleHeight) / 2, 0.0016f), ink);
            var needlePivot = Ref(Node("Galvanometer Needle Pivot", body, Vector3.up * NeedleHeight)).transform;
            Prim(PrimitiveType.Cube, "Galvanometer Needle", needlePivot, Vector3.zero, new Vector3(0.14f, 0.0025f, 0.005f), needleSteel);
            Prim(PrimitiveType.Sphere, "Needle Hub", needlePivot, Vector3.zero, Vector3.one * 0.012f, brass);
            var scale = Node("Galvanometer Scale", body, Vector3.up * NeedleHeight);
            scale.AddComponent<MeshFilter>().sharedMesh = TorsionScaleMesh(GlassRadius - 0.01f, 0.03f);
            scale.AddComponent<MeshRenderer>().sharedMaterials = new[]
            {
                TwoSided("ScaleCard", new Color(0.93f, 0.9f, 0.82f), 0.2f),
                TwoSided("ScaleInk", new Color(0.1f, 0.09f, 0.1f), 0.3f),
            };
            Vector3 terminalA = new Vector3(-0.07f, 0.025f, -0.1f), terminalB = new Vector3(0.07f, 0.025f, -0.1f); // body space
            foreach (var terminal in new[] { terminalA, terminalB })
            {
                Prim(PrimitiveType.Cylinder, "Galvanometer Terminal", body, terminal + Vector3.up * 0.012f, new Vector3(0.012f, 0.012f, 0.012f), brass);
                Prim(PrimitiveType.Sphere, "Terminal Cap", body, terminal + Vector3.up * 0.026f, Vector3.one * 0.014f, brass);
            }
            // Desk space: post B to the right terminal in front of the base; the left terminal back to the hot junction
            // round the left of the base and along the back edge, behind the candle and under the notebook's far edge.
            Vector3 Terminal(Vector3 t) => AppGalvanometer + (t + Vector3.up * 0.03f) * AppGalvanometerScale;
            LeadLine("Lead Post-Galvanometer", galvanometer, postB + new Vector3(0f, 0.016f, 0.025f), new Vector3(0.40f, 0.004f, 0.16f), Terminal(terminalB));
            LeadLine("Lead Galvanometer-Source", galvanometer, Terminal(terminalA), new Vector3(0.05f, 0.004f, 0.17f), new Vector3(0.04f, 0.004f, 0.32f),
                new Vector3(-0.45f, 0.004f, 0.32f), hot + new Vector3(-0.06f, 0.004f, 0.05f), hot + new Vector3(0f, 0.115f, 0f));
            Pivot(galvanometer, W(AppGalvanometer + Vector3.up * NeedleHeight * AppGalvanometerScale));
            // The hero close-up's focus target: the set may have registered it already (it is a camera's FocusTarget), so move that one.
            var needleAt = W(AppGalvanometer + Vector3.up * NeedleHeight * AppGalvanometerScale);
            if (refs.TryGetValue("Apparatus Point", out var apparatusPoint)) apparatusPoint.transform.position = needleAt;
            else Marker("Apparatus Point", needleAt);

            // Mercury cups: three brushed-metal cups filled to the brim, in a row along the front right.
            var cups = Part("Mercury Cups");
            for (int i = 0; i < 3; i++)
            {
                var cup = AppCups + Vector3.right * ((i - 1) * 0.12f);
                Prim(PrimitiveType.Cylinder, "Mercury Cup " + (i + 1), cups, cup + Vector3.up * 0.025f, new Vector3(0.06f, 0.025f, 0.06f), steel);
                Prim(PrimitiveType.Cylinder, "Mercury", cups, cup + Vector3.up * 0.0502f, new Vector3(0.048f, 0.0006f, 0.048f), mercury);
            }
            Pivot(cups, W(AppCups + Vector3.up * 0.05f));

            // Ohm's notebook with its written lines and a pen; the pages that pile onto it one per cycle, and the fan above.
            var notebook = Part("notebook");
            var book = Prim(PrimitiveType.Cube, "Ohm Notebook", notebook, AppNotebook + Vector3.up * 0.006f, new Vector3(0.2f, 0.012f, 0.14f), paper, new Vector3(0, -8, 0)).transform;
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Cube, "Ink Line", book, Vector3.zero, new Vector3(0.7f - i * 0.1f, 0.2f, 0.03f), ink).transform.localPosition = new Vector3(-0.05f * i, 0.55f, 0.3f - i * 0.18f);
            var pen = Node("Pen", notebook, AppNotebook + new Vector3(0.085f, 0.021f, -0.025f));
            pen.transform.localRotation = Quaternion.Euler(0, 55, 90);
            Prim(PrimitiveType.Cylinder, "Pen Barrel", pen.transform, Vector3.zero, new Vector3(0.008f, 0.065f, 0.008f), ink);
            Prim(PrimitiveType.Sphere, "Pen Tip", pen.transform, Vector3.up * 0.065f, Vector3.one * 0.007f, brass);
            var pages = Node("Pages", notebook, AppNotebook);
            pages.transform.localRotation = Quaternion.Euler(0, -8, 0);
            for (int i = 0; i < 3; i++)
            {
                // Flat on the book, written for a reader on the room side (top of the page toward Ohm).
                var page = Prim(PrimitiveType.Quad, "Page " + (i + 1), pages.transform, new Vector3(0.004f * i, 0.0135f + 0.0008f * i, -0.003f * i), new Vector3(0.16f, 0.11f, 1), paper, new Vector3(90, 0, 0));
                Ink(page.transform, InkPages[i], ink.GetColor("_BaseColor"));
                page.AddComponent<PopIn>();
                page.SetActive(false);
                Ref(page);
            }
            Pivot(notebook, W(AppNotebook + Vector3.up * 0.012f));
            var fan = Ref(Node("Pages Fan", root, AppNotebook + Vector3.up * 0.25f));
            for (int i = 0; i < 5; i++)
            {
                var page = Prim(PrimitiveType.Quad, "Fanned Page", fan.transform, new Vector3((i - 2) * 0.1f, Mathf.Abs(i - 2) * -0.025f, 0.002f * i), new Vector3(0.17f, 0.12f, 1), paper, new Vector3(0, 0, (2 - i) * 12f));
                Ink(page.transform, InkPages[i % InkPages.Length], ink.GetColor("_BaseColor"));
            }
            Face(fan, W(new Vector3(0f, 0.4f, -1.2f)));
            fan.AddComponent<PopIn>();
            fan.SetActive(false);

            // The copper wire he examines (Examine mode): OhmWorker holds it between his hands, hidden otherwise.
            var handWire = Ref(Node("Ohm Hand Wire", null, W(AppOhmFeet + new Vector3(0, 1.02f, -0.28f))));
            Prim(PrimitiveType.Cylinder, "Held Copper Wire", handWire.transform, Vector3.zero, new Vector3(0.009f, 0.21f, 0.009f), copper, new Vector3(0, 0, 90));
            handWire.SetActive(false);

            // Ohm's work targets (wrist positions, Scene 4's): the page, the torsion knob, post A's cap.
            Marker("Ohm Notebook Target", W(AppNotebook + Vector3.up * 0.02f));
            Marker("Ohm Galvanometer Target", W(AppGalvanometer + new Vector3(0f, (glassTop + 0.02f) * AppGalvanometerScale, 0.07f)));
            Marker("Ohm Wire Target", W(postA + new Vector3(0f, 0.11f, 0.03f)));
            return root.gameObject;
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

        // Scales a model so its render bounds measure `size` and stands it with the bottom centre of those bounds at `foot`.
        protected static void Fit(GameObject go, Vector3 size, Vector3 foot)
        {
            var bounds = RenderBounds(go);
            var s = go.transform.localScale;
            go.transform.localScale = new Vector3(s.x * size.x / bounds.size.x, s.y * size.y / bounds.size.y, s.z * size.z / bounds.size.z);
            bounds = RenderBounds(go);
            go.transform.position += foot - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        protected static Bounds RenderBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
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
            label.rectTransform.sizeDelta = new Vector2(0.15f, label.rectTransform.sizeDelta.y);
            // The quad is scaled to the page size; the text keeps metre units.
            label.transform.localScale = new Vector3(1 / page.localScale.x, 1 / page.localScale.y, 1);
        }

        // Georg Ohm (Scene 4's variant: scale 1.55, OhmWorker, Scene 4's lean) on Scene 4's spot behind the desk the
        // apparatus stands on, facing the room, working with its notebook, torsion knob, wire post, needle and hand wire.
        protected GameObject GeorgOhm(Transform apparatus)
        {
            Transform At(string name) => refs[name].transform;
            var feet = apparatus.TransformPoint(AppOhmFeet);
            var facing = apparatus.rotation * Vector3.back; // desk-local -Z: toward the room
            OhmWorker worker;
            GameObject ohm;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OhmPrefab))
            {
                ohm = Model(OhmPrefab, "Georg Ohm", null, feet, Quaternion.LookRotation(facing).eulerAngles, 1.55f, collider: false);
                if (!ohm.TryGetComponent(out worker)) worker = ohm.AddComponent<OhmWorker>();
                Ref(ohm);
            }
            else
            {
                ohm = Ohm(feet, facing, At("Ohm Notebook Target"), At("Ohm Galvanometer Target"), At("Ohm Wire Target"));
                if (!ohm.TryGetComponent(out worker)) return ohm; // capsule stand-in
            }
            worker.notebook = At("Ohm Notebook Target");
            worker.galvanometer = At("Ohm Galvanometer Target");
            worker.wirePost = At("Ohm Wire Target");
            worker.needle = At("Galvanometer Needle Pivot");
            worker.handWire = At("Ohm Hand Wire");
            worker.mode = OhmWorker.Mode.Work;
            worker.naturalPerformance = true; // breathing, slow head and torso variation (CinematicPolish's values, kept by a rebuild)
            worker.blendSpeed = 2.2f;
            if (PrefabUtility.IsPartOfPrefabInstance(worker)) PrefabUtility.RecordPrefabInstancePropertyModifications(worker); // scene references are instance overrides
            return ohm;
        }
    }
}
