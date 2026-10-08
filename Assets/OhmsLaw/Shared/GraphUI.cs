using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // V-I graph in the lesson panel (a Canvas-sized rect scaled into the Graph block): V on the vertical axis, I on the
    // horizontal axis, points from ExperimentData, the straight line through the origin (drawn with a sweep), two
    // highlighted points, the ΔV/ΔI rise-and-run marker (drawn leg by leg) and the live point that follows the rig.
    // Layout in canvas px: at the panel's 0.48 scale the 45 px ticks read 21.6 px, still 15 px when a crowded dock
    // shrinks the view to LessonPanel.YieldShare. Point labels sit up-left of their point (the line runs up-right), so
    // nothing crosses the line; one that would cross the V axis sits right of its point, under the line.
    public class GraphUI : MonoBehaviour
    {
        public static readonly Vector2 Canvas = new Vector2(1000, 665);
        public const float Width = 740f, Height = 500f, MaxI = 1.2f, MaxV = 6f;   // plot area inside the canvas
        public const float MarginLeft = 150f, MarginBottom = 125f;
        public const float TickFont = 45f, PointFont = 45f, AxisTitleFont = 48f, TagFont = 48f;
        const float PointSize = 34f, PointHighlight = 50f;
        static readonly Vector2 TextRect = new Vector2(320, 60);

        RectTransform plot, line, triangle;
        float lineLength;
        Coroutine drawing;
        GameObject axes;
        RectTransform[] points;
        TextMeshProUGUI[] tags, labels;
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

        void Awake()
        {
            // v3: the dock card is the surface (no board of its own).
            plot = UI.Rect("Plot", transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(MarginLeft, MarginBottom), new Vector2(Width, Height));
            axes = plot.gameObject;
            var grid = new Color(1, 1, 1, 0.1f);
            for (int i = 1; i <= 6; i++)
            {
                Segment(plot, At(i * 0.2f, 0), At(i * 0.2f, MaxV), grid, 2);
                Segment(plot, At(0, i), At(MaxI, i), grid, 2);
            }
            Segment(plot, Vector2.zero, new Vector2(Width + 20, 0), new Color(1, 1, 1, 0.55f), 4);
            Segment(plot, Vector2.zero, new Vector2(0, Height + 20), new Color(1, 1, 1, 0.55f), 4);
            for (int i = 1; i <= 6; i++)
            {
                Text(plot, (i * 0.2f).ToString("0.0"), At(i * 0.2f, 0) + new Vector2(0, -50), TickFont, LessonPanel.Muted);
                Text(plot, i.ToString("0"), At(0, i) + new Vector2(-80, 0), TickFont, LessonPanel.Muted);
            }
            var iLabel = Text(plot, "Current, I (A)", new Vector2(Width / 2, -100), AxisTitleFont, Current, new Vector2(640, 60));
            var vLabel = Text(plot, "Potential Difference, V (V)", new Vector2(-135, Height / 2), AxisTitleFont, Voltage, new Vector2(640, 60));
            vLabel.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            iLabel.textWrappingMode = vLabel.textWrappingMode = TextWrappingModes.NoWrap; // one line each, whatever the font's advance

            // The line through the origin, drawn a little past the last reading (not to the frame edge).
            line = Segment(plot, Vector2.zero, At(1.1f, 1.1f * Data.resistance), Voltage, 8);
            lineLength = line.sizeDelta.x;
            line.gameObject.SetActive(false);

            points = new RectTransform[Data.Count];
            tags = new TextMeshProUGUI[Data.Count];
            labels = new TextMeshProUGUI[Data.Count];
            for (int i = 0; i < Data.Count; i++)
            {
                var p = At(Data.currents[i], Data.voltages[i]);
                var dot = UI.Rect("Point " + (i + 1), plot, Vector2.zero, new Vector2(0.5f, 0.5f), p, Vector2.one * PointSize);
                var image = dot.gameObject.AddComponent<Image>();
                image.sprite = UI.Theme ? UI.Theme.circle : null;
                image.color = Color.white;
                image.raycastTarget = false;
                // Up-left of the point, unless that crosses the V axis (8 px clear): then right of it, under the line. The
                // offsets are from the point's centre (the label is anchored to the dot's corner).
                labels[i] = Text(dot, $"({Data.currents[i]:0.00}, {Data.voltages[i]:0.0})", Vector2.zero, PointFont, Color.white);
                float half = labels[i].GetPreferredValues().x / 2f;
                var offset = p.x - 77f - half >= 8f ? new Vector2(-77f, 53f) : new Vector2(PointSize / 2f + 8f + half, -5f);
                ((RectTransform)labels[i].transform.parent).anchoredPosition = offset + Vector2.one * PointSize / 2f;
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

        // Points up to `count`; a point that is new pops in. Several at once (a later scene restoring the graph on its
        // first frame) just appear: the pop scales each label with its dot.
        public void Plot(int count)
        {
            int fresh = 0;
            for (int i = 0; i < points.Length && i < count; i++) if (!points[i].gameObject.activeSelf) fresh++;
            for (int i = 0; i < points.Length; i++)
            {
                bool show = i < count;
                if (show && !points[i].gameObject.activeSelf) { points[i].gameObject.SetActive(true); if (fresh == 1) StartCoroutine(PopIn(points[i])); }
                else points[i].gameObject.SetActive(show);
            }
        }

        IEnumerator PopIn(RectTransform point)
        {
            if (ReducedMotion.On) yield break;
            for (float t = 0; t < 1; t += Time.deltaTime / 0.35f)
            {
                if (!point) yield break;
                point.localScale = Vector3.one * (1 + 0.6f * Mathf.Sin(t * Mathf.PI) * (1 - t));
                yield return null;
            }
            if (point) point.localScale = Vector3.one;
        }

        // The line sweeps out from the origin (at once under reduced motion, or when a later scene restores it); off clears it.
        public void Line(bool on, bool instant = false)
        {
            if (drawing != null) { StopCoroutine(drawing); drawing = null; }
            line.gameObject.SetActive(on);
            if (!on || instant || ReducedMotion.On) { line.sizeDelta = new Vector2(lineLength, line.sizeDelta.y); return; }
            drawing = StartCoroutine(Sweep());
        }

        IEnumerator Sweep()
        {
            for (float t = 0; t < 1; t += Time.deltaTime / 1.1f)
            {
                line.sizeDelta = new Vector2(lineLength * Mathf.SmoothStep(0, 1, t), line.sizeDelta.y);
                yield return null;
            }
            line.sizeDelta = new Vector2(lineLength, line.sizeDelta.y);
            drawing = null;
        }

        public void Highlight(int row, bool on, string tag = "")
        {
            if (row < 0 || row >= points.Length) return;
            points[row].sizeDelta = Vector2.one * (on ? PointHighlight : PointSize);
            points[row].GetComponent<Image>().color = on ? Current : Color.white;
            tags[row].text = on ? tag : "";
        }

        // Rise-and-run marker between two points: the run (ΔI, amber) along the bottom, then the rise (ΔV, blue) up the
        // side, each drawn in turn with its value (red stays for the slope's result, R). ΔI reads under its leg, starting
        // right of the first point (the line runs down-left from it), ΔV right of the rise; the point labels step aside
        // while the marker shows.
        public void Triangle(int a, int b)
        {
            if (triangle) Destroy(triangle.gameObject);
            foreach (var label in labels) label.gameObject.SetActive(a < 0);
            if (a < 0) return;
            triangle = UI.Rect("Triangle", plot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Vector2 pa = At(Data.currents[a], Data.voltages[a]), pb = At(Data.currents[b], Data.voltages[b]), corner = new Vector2(pb.x, pa.y);
            var run = Segment(triangle, pa, corner, Current, 6);
            var rise = Segment(triangle, corner, pb, Voltage, 6);
            float dI = Data.currents[b] - Data.currents[a], dV = Data.voltages[b] - Data.voltages[a];
            var runLabel = Text(triangle, $"run: ΔI = {dI:0.00} A", pa + new Vector2(12f + 210f, -42f), TagFont, Current, new Vector2(420, 60));
            var riseLabel = Text(triangle, $"rise: ΔV = {dV:0.0} V", new Vector2(corner.x + 14f + 210f, (corner.y + pb.y) / 2), TagFont, Voltage, new Vector2(420, 60));
            runLabel.alignment = riseLabel.alignment = TextAlignmentOptions.MidlineLeft;
            StartCoroutine(DrawMarker(run, rise, runLabel, riseLabel));
        }

        IEnumerator DrawMarker(RectTransform run, RectTransform rise, TextMeshProUGUI runLabel, TextMeshProUGUI riseLabel)
        {
            float runLength = run.sizeDelta.x, riseLength = rise.sizeDelta.x;
            if (!ReducedMotion.On)
            {
                rise.sizeDelta = new Vector2(0, rise.sizeDelta.y);
                riseLabel.alpha = 0;
                runLabel.alpha = 0;
                for (float t = 0; t < 1; t += Time.deltaTime / 0.7f) { if (!run) yield break; run.sizeDelta = new Vector2(runLength * t, run.sizeDelta.y); runLabel.alpha = t; yield return null; }
                for (float t = 0; t < 1; t += Time.deltaTime / 0.7f) { if (!rise) yield break; rise.sizeDelta = new Vector2(riseLength * t, rise.sizeDelta.y); riseLabel.alpha = t; yield return null; }
            }
            if (!run || !rise) yield break;
            run.sizeDelta = new Vector2(runLength, run.sizeDelta.y);
            rise.sizeDelta = new Vector2(riseLength, rise.sizeDelta.y);
            runLabel.alpha = riseLabel.alpha = 1;
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
            bool shown = rig && rig.readoutsShown; // no live reading while the meters' plates are hidden (Scenes 9-10 before the baseline)
            if (live.gameObject.activeSelf != shown) live.gameObject.SetActive(shown);
            if (!shown) return;
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
