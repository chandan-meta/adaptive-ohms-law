using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Observation table on a world-space canvas. Values come from ExperimentData; rows fill one by one,
    // a V/I column can be added, rows and cells can be highlighted and tapped.
    // Layout in canvas px (WorldCanvasScale 0.0015 on the Boards shot): 44 px cells read ~24 px tall on screen.
    // The table IS the board (ART_DIRECTION section 3): chalk text and rules on a lighter patch of board in a chalk frame,
    // which Refresh() sizes to the visible columns.
    public class ObservationTableUI : MonoBehaviour
    {
        public const float ColumnWidth = 360f, RowHeight = 84f, HeaderRow = 100f, TagWidth = 280f, TagGap = 20f;
        public const float HeaderFont = 36f, CellFont = 44f, TagFont = 40f;
        // The board-panel look the classroom kit's BoardPanel gives the graph, in canvas px at BoardCanvasScale 0.00135:
        // margins 0.015 x 0.02 board UV around the content, a 0.012 m chalk line, Classroom_BoardPanel's green.
        public const float BoardMarginX = 36f, BoardMarginY = 22f, ChalkLine = 9f;
        public static readonly Color BoardTint = new Color(0.16f, 0.28f, 0.25f, 0.55f), ChalkColor = new Color(0.93f, 0.93f, 0.87f, 0.92f);
        // Per-column widths summing to 3 x ColumnWidth: V holds "Potential Difference", V/I holds "1.0 ÷ 0.20 = 5".
        static readonly float[] Columns = { 380f, 280f, 420f };

        public string[] headers = { "Potential Difference\nV (V)", "Current\nI (A)", "V / I" };
        [Tooltip("Cells read \"1.0 V | 0.20 A\" (Scene 12) instead of \"1 | 0.2\" (Scenes 10-11).")] public bool units;

        public event Action<int> RowTapped;

        TextMeshProUGUI[,] cells;
        Image[,] cellBack;
        TextMeshProUGUI[] tags;
        GameObject[] rowObjects;
        GameObject ratioHeader, ratioRule;
        RectTransform frame;
        int rows;
        bool ratio;

        static ExperimentData Data => ExperimentData.Main;
        // Cells are bare chalk on the board until a highlight, glow or sweep tints them.
        static Color Rest => Color.clear;
        // Column colours follow the concept palette: V blue, I amber, V/I coral.
        static Color Column(int c) => UI.Theme ? (c == 0 ? UI.Theme.voltage : c == 1 ? UI.Theme.current : UI.Theme.resistance) : Color.white;
        static Color CellText(int c) => Color.Lerp(Column(c), Color.white, 0.55f);
        // Highlight backs are the palette colours darkened to sit under white text.
        static Color RowHighlight => Shade(UI.Theme ? UI.Theme.current : Color.yellow, 0.45f);
        static Color ColumnGlow => Shade(UI.Theme ? UI.Theme.resistance : Color.red, 0.55f);
        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 0.95f);
        static float X(int column) { float x = 0; for (int c = 0; c < column; c++) x += Columns[c]; return x; }

        void Awake()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas && !canvas.worldCamera) canvas.worldCamera = Camera.main;
            int n = Data.Count;
            cells = new TextMeshProUGUI[n, 3];
            cellBack = new Image[n, 3];
            tags = new TextMeshProUGUI[n];
            rowObjects = new GameObject[n];
            var root = (RectTransform)transform;

            // Frame first, so the cells draw over it: board tint, chalk outline, a firm header rule, faint row rules and
            // column rules from outline to outline (the V / I one only with its column).
            frame = UI.Rect("Frame", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-BoardMarginX, BoardMarginY), Vector2.zero);
            UI.Panel(frame, BoardTint, false).raycastTarget = false;
            Chalk("Top", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(ChalkLine, ChalkLine), 1f);
            Chalk("Bottom", Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(ChalkLine, ChalkLine), 1f);
            Chalk("Left", Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(ChalkLine, -ChalkLine), 1f);
            Chalk("Right", new Vector2(1, 0), Vector2.one, Vector2.zero, new Vector2(ChalkLine, -ChalkLine), 1f);
            for (int r = 0; r < n; r++)
                Chalk("Row Rule", new Vector2(0, 1), Vector2.one, new Vector2(0, -(BoardMarginY + HeaderRow + r * RowHeight)), new Vector2(-ChalkLine, r == 0 ? 4f : 2f), r == 0 ? 0.85f : 0.35f);
            Chalk("Column Rule", Vector2.zero, new Vector2(0, 1), new Vector2(BoardMarginX + X(1), 0), new Vector2(3f, -ChalkLine), 0.55f);
            ratioRule = Chalk("Column Rule", Vector2.zero, new Vector2(0, 1), new Vector2(BoardMarginX + X(2), 0), new Vector2(3f, -ChalkLine), 0.55f).gameObject;

            for (int c = 0; c < 3; c++)
            {
                var head = Cell(root, c, 0, HeaderRow, headers[c], HeaderFont, Column(c));
                // Two-line headers must stay two lines: shrink a little rather than wrap "Difference" to a third.
                head.enableAutoSizing = true;
                head.fontSizeMax = HeaderFont;
                head.fontSizeMin = HeaderFont * 0.8f;
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
            var image = UI.Panel(rect, UI.Alpha(ChalkColor, ChalkColor.a * alpha), false);
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
            if (row < 0 || row >= Data.Count) return;
            for (int c = 0; c < 3; c++) cellBack[row, c].color = on ? RowHighlight : Rest;
            tags[row].text = on ? tag : "";
        }

        // "1 V → 0.2 A", in the same short form as the table.
        public string Pair(int row) => $"{Data.voltages[row]:0} V → {Data.currents[row]:0.0} A";

        public void ClearHighlights() { for (int r = 0; r < Data.Count; r++) Highlight(r, false); }

        public void GlowColumn(int column, bool on)
        {
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
                cells[r, 0].text = units ? Data.voltages[r].ToString("0.0") + " V" : Data.voltages[r].ToString("0");
                cells[r, 1].text = units ? Data.currents[r].ToString("0.00") + " A" : Data.currents[r].ToString("0.0");
                cellBack[r, 2].transform.gameObject.SetActive(ratio);
                // Tags stand on their row just outside the frame, like a chalk note beside the table.
                ((RectTransform)tags[r].transform.parent).anchoredPosition = new Vector2(width + BoardMarginX + ChalkLine / 2 + TagGap, 0);
            }
        }
    }
}
