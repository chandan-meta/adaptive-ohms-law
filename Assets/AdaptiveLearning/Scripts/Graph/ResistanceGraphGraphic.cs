using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    // Draws a quantitative V-versus-I grid, trend line, and slope triangle.
    // Horizontal axis: Current I (A)
    // Vertical axis: Potential difference V (V)
    [RequireComponent(typeof(CanvasRenderer))]
    public class ResistanceGraphGraphic : MaskableGraphic
    {
        public float maxCurrent = 1.2f;
        public float maxVoltage = 6.0f;
        public bool showTrend = true;
        public bool showSlopeTriangle = false;
        public Vector2 slopePointA; // in data units (I, V)
        public Vector2 slopePointB; // in data units (I, V)

        public readonly List<Vector2> points = new List<Vector2>();

        public void Refresh() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;

            // Grid lines
            // Horizontal ticks (Current I: 0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.2 A)
            for (int i = 0; i <= 6; i++)
            {
                float currentVal = i * 0.2f;
                float x = currentVal / maxCurrent;
                Line(vh, new Vector2(x, 0), new Vector2(x, 1), i == 0 ? 3f : 1f, new Color(0.18f, 0.35f, 0.42f, 0.6f), rect);
            }

            // Vertical ticks (Voltage V: 0, 1, 2, 3, 4, 5, 6 V)
            for (int v = 0; v <= 6; v++)
            {
                float y = v / maxVoltage;
                Line(vh, new Vector2(0, y), new Vector2(1, y), v == 0 ? 3f : 1f, new Color(0.18f, 0.35f, 0.42f, 0.6f), rect);
            }

            // Trend line through origin (0, 0) to max (1.2 A, 6.0 V)
            if (showTrend)
            {
                var origin = new Vector2(0, 0);
                foreach (var point in points)
                {
                    var end = new Vector2(point.x / maxCurrent, point.y / maxVoltage);
                    Line(vh, origin, end, 3.5f, WarmLabTheme.Sage, rect);
                    origin = end;
                }
            }

            // Slope triangle
            if (showSlopeTriangle)
            {
                float x1 = slopePointA.x / maxCurrent;
                float y1 = slopePointA.y / maxVoltage;
                float x2 = slopePointB.x / maxCurrent;
                float y2 = slopePointB.y / maxVoltage;

                // Sort so x1 <= x2
                if (x1 > x2)
                {
                    float tx = x1; x1 = x2; x2 = tx;
                    float ty = y1; y1 = y2; y2 = ty;
                }

                var pStart = new Vector2(x1, y1);
                var pCorner = new Vector2(x2, y1); // Horizontal base
                var pEnd = new Vector2(x2, y2);    // Vertical height

                // Horizontal change ΔI (Amber line)
                Line(vh, pStart, pCorner, 4f, WarmLabTheme.Amber, rect);
                // Vertical change ΔV (Cyan/Purple line)
                Line(vh, pCorner, pEnd, 4f, WarmLabTheme.Plum, rect);
            }
        }

        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color, Rect rect)
        {
            a = rect.min + Vector2.Scale(a, rect.size);
            b = rect.min + Vector2.Scale(b, rect.size);
            var dir = b - a;
            if (dir.sqrMagnitude < 0.001f) return;
            var n = new Vector2(-dir.y, dir.x).normalized * width * 0.5f;
            int start = vh.currentVertCount;
            vh.AddVert(a - n, color, Vector2.zero);
            vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero);
            vh.AddVert(b - n, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
