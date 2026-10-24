using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Observation table in the lesson panel (a Canvas-sized rect scaled into the Table block). Values come from
    // ExperimentData; rows fill one by one (a recorded row shows what the meters read then), a V/I column can be added,
    // rows and cells can be highlighted and tapped. Layout in canvas px: at the panel's 0.49 scale the 44 px heads and
    // cells read 21.6 px, still 15 px when a crowded dock shrinks the view to LessonPanel.YieldShare. The frame sizes
    // itself to the visible columns.
    public class ObservationTableUI : MonoBehaviour
    {
        public static readonly Vector2 Canvas = new Vector2(1080, 540);   // HeaderRow + 5 rows + the board margins: the frame never overruns its block
        public const float ColumnWidth = 360f, RowHeight = 76f, HeaderRow = 116f, TagWidth = 280f, TagGap = 20f;
        public const float HeaderFont = 44f, CellFont = 44f, TagFont = 44f;
        // The board-panel look the classroom kit's BoardPanel gives the graph, in canvas px at BoardCanvasScale 0.00135:
        // margins 0.015 x 0.02 board UV around the content, a 0.012 m chalk line, Classroom_BoardPanel's green.
        public const float BoardMarginX = 36f, BoardMarginY = 22f, ChalkLine = 9f;
        public static readonly Color BoardTint = new Color(0.05f, 0.07f, 0.1f, 0.9f), ChalkColor = new Color(0.93f, 0.93f, 0.87f, 0.75f);
        // Per-column widths summing to 3 x ColumnWidth: V holds "Potential Difference" at 44 px, V/I holds "1.0 ÷ 0.20 = 5".
        static readonly float[] Columns = { 450f, 250f, 380f };

        // The ratio's head carries its unit like the others: volts per ampere (the ohm is named only at Scene 12's end).
        public string[] headers = { "Potential Difference\nV (V)", "Current\nI (A)", "V / I\n(V/A)" };
        [Tooltip("Cells read \"1.0 V | 0.20 A\" (Scene 12) instead of \"1 | 0.2\" (Scenes 10-11).")] public bool units;

        public event Action<int> RowTapped;

        TextMeshProUGUI[,] cells;
        Image[,] cellBack;
        TextMeshProUGUI[] tags;
        GameObject[] rowObjects;
        Image[] marks;
        readonly RectTransform[] headCells = new RectTransform[3];
        GameObject ratioHeader, ratioRule;
        RectTransform frame;
        int rows;
        bool ratio;
        // Rows the learner recorded show what the meters read then (RECORD); the others show ExperimentData's row.
        float[] recordedVolts, recordedAmps;

        static ExperimentData Data => ExperimentData.Main;
        // Cells are bare chalk on the board until a highlight, glow or sweep tints them.
        static Color Rest => Color.clear;
        // Column colours follow the concept palette: V blue, I amber, V/I coral.
        static Color Column(int c) => c == 0 ? LessonPanel.VoltColor : c == 1 ? LessonPanel.AmpColor : LessonPanel.OhmColor;
        static Color CellText(int c) => LessonPanel.Paper;   // values in paper; the column heads carry the V / I / R colours
        // Highlights are the HUD's guidance spark, faint on the glass (amber means current only, a red block reads as an
        // error, and a white wash blends in linear light into an opaque-looking grey pill); the values stay paper.
        // A row and its graph point light in one family (GraphUI.Highlight): the spark tint here plus a spark bar on the row's
        // left edge, a spark ring round the point there. Amber stays the current's own colour.
        static Color RowHighlight => UI.Alpha(LessonPanel.Spark, 0.22f);
        static Color ColumnGlow => RowHighlight;
        static float X(int column) { float x = 0; for (int c = 0; c < column; c++) x += Columns[c]; return x; }

        // Record / ShowRows may run before Awake (a builder keeps the table inactive until its shot): the record keeps, and
        // Awake builds the cells around it.
        void EnsureRecords()
        {
            if (recordedVolts != null) return;
            int n = Data.Count;
            recordedVolts = new float[n];
            recordedAmps = new float[n];
            for (int r = 0; r < n; r++) recordedVolts[r] = recordedAmps[r] = float.NaN;
        }

        void Awake()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas && !canvas.worldCamera) canvas.worldCamera = Camera.main;
            int n = Data.Count;
            EnsureRecords();
            cells = new TextMeshProUGUI[n, 3];
            cellBack = new Image[n, 3];
            tags = new TextMeshProUGUI[n];
            rowObjects = new GameObject[n];
            marks = new Image[n];
            var root = (RectTransform)transform;

            // Frame first, so the cells draw over it: board tint, chalk outline, a firm header rule, faint row rules and
            // column rules from outline to outline (the V / I one only with its column).
            frame = UI.Rect("Frame", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-BoardMarginX, BoardMarginY), Vector2.zero);
            // v3: the dock card is the surface; the table keeps only hairline rules (no board fill, no outer frame).
            UI.Panel(frame, Color.clear, false).raycastTarget = false;
            for (int r = 0; r < n; r++)
                Chalk("Row Rule", new Vector2(0, 1), Vector2.one, new Vector2(0, -(BoardMarginY + HeaderRow + r * RowHeight)), new Vector2(-ChalkLine, r == 0 ? 4f : 2f), r == 0 ? 0.85f : 0.35f);
            Chalk("Column Rule", Vector2.zero, new Vector2(0, 1), new Vector2(BoardMarginX + X(1), 0), new Vector2(3f, -ChalkLine), 0.55f);
            ratioRule = Chalk("Column Rule", Vector2.zero, new Vector2(0, 1), new Vector2(BoardMarginX + X(2), 0), new Vector2(3f, -ChalkLine), 0.55f).gameObject;

            for (int c = 0; c < 3; c++)
            {
                var head = Cell(root, c, 0, HeaderRow, headers[c], HeaderFont, Column(c));
                headCells[c] = (RectTransform)head.transform.parent;
                // Two lines at full size (the V column fits "Potential Difference" at 44 px), a little tighter than body text.
                head.lineSpacing = -10f;
                if (c == 2) ratioHeader = head.transform.parent.gameObject;
            }
            for (int r = 0; r < n; r++)
            {
                int row = r;
                var line = UI.Rect("Row " + (r + 1), root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -HeaderRow - r * RowHeight), new Vector2(X(3), RowHeight));
                rowObjects[r] = line.gameObject;
                var tap = line.gameObject.AddComponent<Image>();
                tap.color = Color.clear;
                line.gameObject.AddComponent<Button>().onClick.AddListener(() => RowTapped?.Invoke(row));
                for (int c = 0; c < 3; c++)
                {
                    cells[r, c] = Cell(line, c, 0, RowHeight, "", CellFont, CellText(c));
                    cellBack[r, c] = cells[r, c].transform.parent.GetComponent<Image>();
                }
                // The highlight's bar, just left of the first cell.
                var mark = UI.Rect("Mark", line, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-4f, 0), new Vector2(7f, RowHeight - 16f));
                marks[r] = UI.Panel(mark, LessonPanel.Spark, true, 3.5f);
                marks[r].raycastTarget = false;
                mark.gameObject.SetActive(false);
                // Refresh() moves this "Tag" rect (not the stretched label inside it) beside the frame.
                tags[r] = UI.Label(UI.Rect("Tag", line, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(TagWidth, RowHeight)),
                    "", TagFont, UI.Theme ? UI.Theme.current : Color.yellow, TextAlignmentOptions.MidlineLeft);
            }
            Refresh();
        }

        // A chalk bar on the frame: anchored along an edge (or a rule's line) and stretched with the frame.
        Image Chalk(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, float alpha)
        {
            var rect = UI.Rect(name, frame, anchorMin, new Vector2(0.5f, 0.5f), position, size);
            rect.anchorMax = anchorMax;
            var image = UI.Panel(rect, new Color(1f, 1f, 1f, 0.28f * alpha), false);
            image.raycastTarget = false;
            return image;
        }

        // Cells sit centred in their slot (3 px clear of each rule) so a highlight never covers the chalk lines.
        TextMeshProUGUI Cell(RectTransform parent, int column, float y, float height, string text, float size, Color color)
        {
            var rect = UI.Rect("Cell", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(X(column) + 3, y - 3), new Vector2(Columns[column] - 6, height - 6));
            UI.Panel(rect, Rest).raycastTarget = false;
            var label = UI.Label(rect, text, size, color);
            label.rectTransform.offsetMin = new Vector2(8, 4);
            label.rectTransform.offsetMax = new Vector2(-8, -4);
            return label;
        }

        public void ShowRows(int count) { rows = Mathf.Clamp(count, 0, Data.Count); Refresh(); }

        // RECORD: the meters' live values go into `row` (0-based), and the table shows at least that many rows.
        public void Record(int row, float volts, float amps)
        {
            if (row < 0 || row >= Data.Count) return;
            EnsureRecords();
            recordedVolts[row] = volts;
            recordedAmps[row] = amps;
            rows = Mathf.Max(rows, row + 1);
            Refresh();
        }

        float Volts(int row) => float.IsNaN(recordedVolts[row]) ? Data.voltages[row] : recordedVolts[row];
        float Amps(int row) => float.IsNaN(recordedAmps[row]) ? Data.currents[row] : recordedAmps[row];
        public void ShowRatio(bool on) { ratio = on; Refresh(); }

        // Third column: the worked division "1.0 ÷ 0.20 = 5", or just the result.
        public void SetRatio(int row, bool working)
        {
            if (row < 0 || row >= Data.Count) return;
            ratio = true;
            string result = Data.Ratio(row).ToString("0.##");
            cells[row, 2].text = working ? $"{Data.voltages[row]:0.0} ÷ {Data.currents[row]:0.00} = {result}" : result;
            Refresh();
        }

        public void Highlight(int row, bool on, string tag = "")
        {
            if (row < 0 || row >= Data.Count || cellBack == null) return; // no cells before Awake
            for (int c = 0; c < 3; c++) cellBack[row, c].color = on ? RowHighlight : Rest;
            marks[row].gameObject.SetActive(on);
            tags[row].text = on ? tag : "";
            Centre();
        }

        // In the dock (no Canvas of its own, unlike a board's world-space table) the visible columns sit centred in their
        // card; a row note beside the table needs the room on the right, so the table moves back left while one shows.
        void Centre()
        {
            if (GetComponent<Canvas>() || tags == null) return;
            var rect = (RectTransform)transform;
            bool noted = System.Array.Exists(tags, t => t && t.text.Length > 0);
            rect.pivot = new Vector2(noted ? 0.5f : X(ratio ? 3 : 2) / 2f / rect.rect.width, 1f);
        }

        // Where a row, a cell or a column head is on screen (WattsonGuide points at them; the link spark leaves from a row).
        // Null before Awake.
        public RectTransform Row(int row) => rowObjects != null && row >= 0 && row < rowObjects.Length ? (RectTransform)rowObjects[row].transform : null;
        public RectTransform Cell(int row, int column) => cellBack != null && row >= 0 && row < Data.Count && column >= 0 && column < 3 ? cellBack[row, column].rectTransform : null;
        public RectTransform Header(int column) => column >= 0 && column < 3 ? headCells[column] : null;

        // "1 V → 0.2 A", in the same short form as the table.
        public string Pair(int row) => $"{Data.voltages[row]:0} V → {Data.currents[row]:0.0} A";

        public void ClearHighlights() { for (int r = 0; r < Data.Count; r++) Highlight(r, false); }

        public void GlowColumn(int column, bool on)
        {
            if (cellBack == null) return; // no cells before Awake (Scene 13 keeps the table hidden)
            var color = on ? ColumnGlow : Rest;
            for (int r = 0; r < Data.Count; r++) cellBack[r, column].color = color;
        }

        // Steps down one column, lighting each filled cell in turn.
        public IEnumerator Sweep(int column, float perRow = 0.6f)
        {
            for (int r = 0; r < rows; r++)
            {
                cellBack[r, column].color = RowHighlight;
                yield return new WaitForSeconds(perRow);
                cellBack[r, column].color = Rest;
            }
        }

        void Refresh()
        {
            if (cells == null) return;
            float width = X(ratio ? 3 : 2); // the visible columns: two until Scene 12's V / I
            ratioHeader.SetActive(ratio);
            ratioRule.SetActive(ratio);
            frame.sizeDelta = new Vector2(width + 2 * BoardMarginX, HeaderRow + Data.Count * RowHeight + 2 * BoardMarginY);
            for (int r = 0; r < Data.Count; r++)
            {
                rowObjects[r].SetActive(r < rows);
                ((RectTransform)rowObjects[r].transform).sizeDelta = new Vector2(width, RowHeight); // the visible columns only: a mark on its edge lands on the table
                cells[r, 0].text = units ? Volts(r).ToString("0.0") + " V" : Volts(r).ToString("0.#");
                cells[r, 1].text = units ? Amps(r).ToString("0.00") + " A" : Amps(r).ToString("0.0#");
                cellBack[r, 2].transform.gameObject.SetActive(ratio);
                // Tags stand on their row just outside the frame, like a chalk note beside the table.
                ((RectTransform)tags[r].transform.parent).anchoredPosition = new Vector2(width + BoardMarginX + ChalkLine / 2 + TagGap, 0);
            }
            Centre();
        }
    }
}
