using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // V-I graph on a world-space canvas: V on the vertical axis, I on the horizontal axis, points from ExperimentData,
    // the straight line through the origin, two highlighted points, the ΔV/ΔI triangle and the live point that follows the rig.
    // Layout in canvas px (WorldCanvasScale 0.0015 on the Boards shot): 40 px ticks read ~20 px on screen, titles ~24.
    // Point labels sit up-left of their point (the line runs up-right), tags down-right, so nothing crosses the line.
    public class GraphUI : MonoBehaviour
    {
        public const float Width = 740f, Height = 520f, MaxI = 1.2f, MaxV = 6f;   // plot area; canvas is 1060 x 750
        public const float MarginLeft = 170f, MarginBottom = 170f;
        public const float TickFont = 40f, PointFont = 40f, AxisTitleFont = 48f, TagFont = 48f;
        const float PointSize = 34f, PointHighlight = 50f;
        static readonly Vector2 TextRect = new Vector2(320, 60);

        RectTransform plot, line, triangle;
        GameObject axes;
        RectTransform[] points;
        TextMeshProUGUI[] tags;
        RectTransform pulseTarget;
        float pulse = -1;
        // The live point: an amber ring labelled "now" at (rig.Amps, rig.Volts), easing like the meter needles; the faint
        // ring is where it was before the last change. Recorded points are white discs, so the two never read alike.
        const float LiveSize = 56f;
        RectTransform live, ghost;
        float liveVolts = float.NaN;

        static ExperimentData Data => ExperimentData.Main;
        static Color Voltage => UI.Theme ? UI.Theme.voltage : Color.cyan;
        static Color Current => UI.Theme ? UI.Theme.current : Color.yellow;
        static Color Resistance => UI.Theme ? UI.Theme.resistance : Color.red;

        void Awake()
        {
            // The same lighter patch of board as the observation table's frame, out to the inner edge of the classroom kit's
            // chalk frame around this canvas, so the graph reads as chalk on the board rather than a dark tablet.
            var tint = UI.Stretch("Board", transform);
            var reach = new Vector2(ObservationTableUI.BoardMarginX, ObservationTableUI.BoardMarginY) - Vector2.one * (ObservationTableUI.ChalkLine / 2);
            tint.offsetMin = -reach;
            tint.offsetMax = reach;
            UI.Panel(tint, ObservationTableUI.BoardTint, false).raycastTarget = false;
            plot = UI.Rect("Plot", transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(MarginLeft, MarginBottom), new Vector2(Width, Height));
            axes = plot.gameObject;
            var grid = new Color(1, 1, 1, 0.1f);
            for (int i = 1; i <= 6; i++)
            {
                Segment(plot, At(i * 0.2f, 0), At(i * 0.2f, MaxV), grid, 2);
                Segment(plot, At(0, i), At(MaxI, i), grid, 2);
            }
            Segment(plot, Vector2.zero, new Vector2(Width + 20, 0), Color.white, 6);
            Segment(plot, Vector2.zero, new Vector2(0, Height + 20), Color.white, 6);
            for (int i = 1; i <= 6; i++)
            {
                Text(plot, (i * 0.2f).ToString("0.0"), At(i * 0.2f, 0) + new Vector2(0, -50), TickFont, Color.white);
                Text(plot, i.ToString("0"), At(0, i) + new Vector2(-80, 0), TickFont, Color.white);
            }
            var iLabel = Text(plot, "Current, I (A)", new Vector2(Width / 2, -130), AxisTitleFont, Current, new Vector2(640, 60));
            var vLabel = Text(plot, "Potential Difference, V (V)", new Vector2(-135, Height / 2), AxisTitleFont, Voltage, new Vector2(640, 60));
            vLabel.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            iLabel.textWrappingMode = vLabel.textWrappingMode = TextWrappingModes.NoWrap; // one line each, whatever the font's advance

            // The line through the origin, drawn a little past the last reading (not to the frame edge).
            line = Segment(plot, Vector2.zero, At(1.1f, 1.1f * Data.resistance), Voltage, 8);
            line.gameObject.SetActive(false);

            points = new RectTransform[Data.Count];
            tags = new TextMeshProUGUI[Data.Count];
            for (int i = 0; i < Data.Count; i++)
            {
                var dot = UI.Rect("Point " + (i + 1), plot, Vector2.zero, new Vector2(0.5f, 0.5f), At(Data.currents[i], Data.voltages[i]), Vector2.one * PointSize);
                var image = dot.gameObject.AddComponent<Image>();
                image.sprite = UI.Theme ? UI.Theme.circle : null;
                image.color = Color.white;
                image.raycastTarget = false;
                // Labels sit up-left of their point; the first point is only ~110 px from the V axis, so its label slides right until it clears the line.
                Text(dot, $"({Data.currents[i]:0.00}, {Data.voltages[i]:0.0})", new Vector2(Mathf.Max(-60f, 110f - At(Data.currents[i], Data.voltages[i]).x), 70), PointFont, Color.white);
                tags[i] = Text(dot, "", new Vector2(170, -70), TagFont, Current, new Vector2(320, 140));
                points[i] = dot;
                dot.gameObject.SetActive(false);
            }
            Axes(false);
        }

        Vector2 At(float amps, float volts) => new Vector2(amps / MaxI * Width, volts / MaxV * Height);

        static TextMeshProUGUI Text(RectTransform parent, string text, Vector2 position, float size, Color color, Vector2? rect = null)
        {
            var r = UI.Rect("Text", parent, Vector2.zero, new Vector2(0.5f, 0.5f), position, rect ?? TextRect);
            return UI.Label(r, text, size, color);
        }

        static RectTransform Segment(RectTransform parent, Vector2 from, Vector2 to, Color color, float thickness)
        {
            var rect = UI.Rect("Line", parent, Vector2.zero, new Vector2(0, 0.5f), from, new Vector2((to - from).magnitude, thickness));
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        public void Axes(bool on) => axes.SetActive(on);
        public void Plot(int count) { for (int i = 0; i < points.Length; i++) points[i].gameObject.SetActive(i < count); }
        public void Line(bool on) => line.gameObject.SetActive(on);

        public void Highlight(int row, bool on, string tag = "")
        {
            if (row < 0 || row >= points.Length) return;
            points[row].sizeDelta = Vector2.one * (on ? PointHighlight : PointSize);
            points[row].GetComponent<Image>().color = on ? Current : Color.white;
            tags[row].text = on ? tag : "";
        }

        // Right-angled triangle between two points: ΔI along the bottom, ΔV up the side, with their values.
        // ΔI reads below its leg (point labels are above the points), ΔV to the right of the vertical leg.
        public void Triangle(int a, int b)
        {
            if (triangle) Destroy(triangle.gameObject);
            if (a < 0) return;
            triangle = UI.Rect("Triangle", plot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Vector2 pa = At(Data.currents[a], Data.voltages[a]), pb = At(Data.currents[b], Data.voltages[b]), corner = new Vector2(pb.x, pa.y);
            Segment(triangle, pa, corner, Resistance, 6);
            Segment(triangle, corner, pb, Resistance, 6);
            float dI = Data.currents[b] - Data.currents[a], dV = Data.voltages[b] - Data.voltages[a];
            Text(triangle, $"ΔI = {dI:0.00} A", (pa + corner) / 2 + new Vector2(0, -50), TagFont, Resistance);
            Text(triangle, $"ΔV = {dV:0.0} V", (corner + pb) / 2 + new Vector2(150, 0), TagFont, Resistance);
        }

        public void Pulse() { pulse = 0; pulseTarget = plot; }

        // "graph.live 1": the live point follows CircuitRig.Active from now on (it lives on the plot, so it shows with the axes).
        public void Live(bool on)
        {
            if (on && !live)
            {
                ghost = Ring("Live Point Was", UI.Alpha(Current, 0.35f), "");
                live = Ring("Live Point", Current, "now");
                ghost.gameObject.SetActive(false);
                liveVolts = float.NaN;
            }
            else if (!on && live)
            {
                Destroy(live.gameObject);
                Destroy(ghost.gameObject);
                live = ghost = null;
            }
        }

        RectTransform Ring(string name, Color color, string label)
        {
            var ring = UI.Rect(name, plot, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * LiveSize);
            var image = ring.gameObject.AddComponent<Image>();
            image.sprite = UI.Circle(0.6f);
            image.color = color;
            image.raycastTarget = false;
            // Right of and below the point: the line runs up-right and the recorded labels sit up-left, so "now" is clear of both.
            if (label != "") Text(ring, label, new Vector2(70, -40), PointFont, color, new Vector2(120, 60));
            return ring;
        }

        void Update()
        {
            if (live) FollowRig();
            if (pulse < 0) return;
            pulse += Time.deltaTime;
            pulseTarget.localScale = Vector3.one * (1 + 0.05f * Mathf.Sin(pulse * Mathf.PI * 3) * (1 - pulse / 1.2f));
            if (pulse > 1.2f) { pulse = -1; pulseTarget.localScale = Vector3.one; }
        }

        void FollowRig()
        {
            var rig = CircuitRig.Active;
            if (live.gameObject.activeSelf != (bool)rig) live.gameObject.SetActive(rig);
            if (!rig) return;
            Vector2 target = At(rig.Amps, rig.Volts);
            if (!Mathf.Approximately(rig.state.voltage, liveVolts))
            {
                // The ghost marks the reading at the previous voltage; the first reading has no ghost.
                if (!float.IsNaN(liveVolts)) { ghost.anchoredPosition = At(rig.AmpsAt(liveVolts), rig.VoltsAt(liveVolts)); ghost.gameObject.SetActive(true); }
                else live.anchoredPosition = target;
                liveVolts = rig.state.voltage;
            }
            live.anchoredPosition = ReducedMotion.On ? target : Vector2.Lerp(live.anchoredPosition, target, 1 - Mathf.Exp(-6f * Time.deltaTime));
        }
    }
}
