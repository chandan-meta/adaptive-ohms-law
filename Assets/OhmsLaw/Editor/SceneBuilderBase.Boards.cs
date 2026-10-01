using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace OhmsLaw.Editor
{
    // Stage kit: observation table and V-I graph boards (Scenes 10-12), laid out for the study's Boards camera.
    public abstract partial class SceneBuilderBase
    {
        // StudyBoardsCam (-1.1, 0.65, 0.4) -> StudyBoardsLook, FOV 44, looks toward -Z: screen-right is -X (window wall),
        // screen-left is +X (room). At the boards' depth (2.7-2.8 m) one metre is ~485 px, so with this scale the
        // table's 44 px cells read ~23 px tall and the graph's 40 px ticks ~20 px (TYPOGRAPHY floor 20, target 24).
        public const float WorldCanvasScale = 0.0015f;
        // Table lower-left (frame 5-47 % across, 27-61 % up, above the subtitle box), graph upper-right (50-87 %, 42-93 %);
        // neither crosses the other, the window wall (x -3.21) or the ceiling (y 1.69). The table sits 0.2 m deeper so its
        // 1.62 m fits the foreshortened left side. Scene 12's "lab" cue moves the whole set, so these stay relative to
        // BoardsAnchor. Free for cards: above the table (5-47 % x 62-95 %) and over the bench below the graph (50-87 % x 25-42 %).
        // (Properties, not fields: static initializer order across partial files is unspecified and these use BoardsAnchor.)
        public static Vector3 TableBoard => BoardsAnchor + new Vector3(0.67f, -0.36f, -0.2f);   // (-0.68, 0.39, -2.45), 1.62 x 0.78 m
        public static Vector3 GraphBoard => BoardsAnchor + new Vector3(-0.985f, 0.175f, 0f);  // (-2.335, 0.925, -2.25), 1.59 x 1.125 m
        // Wattson's spot for the Boards shots: upper-left third (33 % across, 78 % up), 2.2 m from the lens, hovering just
        // above the table's header and clear of both boards and of the camera-to-board lines.
        public static readonly Vector3 BoardsWattson = new Vector3(-0.95f, 1.05f, -1.9f);
        public static readonly Vector2 TableCanvas = new Vector2(1080, 520), GraphCanvas = new Vector2(1060, 750);

        // Observation table and V-I graph on world-space canvases facing the Boards camera (default StudyBoardsCam).
        protected void Boards(bool withTable, bool withGraph, bool unitsInTable = false, Vector3? face = null)
        {
            Vector3 eye = face ?? StudyBoardsCam;
            var set = Node("Boards Set", null, Vector3.zero);
            Ref(set);
            if (withTable)
            {
                var canvas = WorldCanvas("Observation Table", set.transform, TableBoard, TableCanvas, eye);
                Ref(canvas.gameObject);
                var table = canvas.gameObject.AddComponent<ObservationTableUI>();
                table.units = unitsInTable;
                director.table = table;
                Placeholder(canvas.gameObject, "Observation table board: real frame/material, fill-in animation");
                Marker("Table Point", BoardsWattson);
            }
            if (withGraph)
            {
                var canvas = WorldCanvas("Graph", set.transform, GraphBoard, GraphCanvas, eye);
                Ref(canvas.gameObject);
                director.graph = canvas.gameObject.AddComponent<GraphUI>();
                Placeholder(canvas.gameObject, "V-I graph board: real frame/material, line-draw animation", false);
                // Where the table's columns lift off toward the axes: the seam between the two boards.
                var seam = (TableBoard + GraphBoard) * 0.5f + new Vector3(0, 0.1f, 0.2f);
                Vfx("columns become axes", set.transform, seam, 1.6f).transform.rotation = Quaternion.LookRotation(seam - eye);
            }
            Marker("Boards Point", BoardsWattson);
        }

        Canvas WorldCanvas(string name, Transform parent, Vector3 position, Vector2 size, Vector3 face)
        {
            var go = Node(name, parent, position);
            go.transform.rotation = Quaternion.LookRotation(position - face); // UGUI reads from the canvas's -Z side
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one * WorldCanvasScale;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }
    }
}
