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
    public class ObservationTableUI : MonoBehaviour
    {
        public const float ColumnWidth = 360f, RowHeight = 84f, HeaderRow = 100f, TagWidth = 280f;
        public const float HeaderFont = 36f, CellFont = 44f, TagFont = 40f;
        // Per-column widths summing to 3 x ColumnWidth: V holds "Potential Difference", V/I holds "1.0 ÷ 0.20 = 5".
        static readonly float[] Columns = { 380f, 280f, 420f };

        public string[] headers = { "Potential Difference\nV (V)", "Current\nI (A)", "V / I" };
        [Tooltip("Cells read \"1.0 V | 0.20 A\" (Scene 12) instead of \"1 | 0.2\" (Scenes 10-11).")] public bool units;

        public event Action<int> RowTapped;

        TextMeshProUGUI[,] cells;
        Image[,] cellBack;
        TextMeshProUGUI[] tags;
        GameObject[] rowObjects;
        GameObject ratioHeader;
        int rows;
        bool ratio;

        static ExperimentData Data => ExperimentData.Main;
        static Color Panel => WorldText.PanelColor;
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
                tags[r] = UI.Label(UI.Rect("Tag", line, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(X(3) + 16, 0), new Vector2(TagWidth, RowHeight)),
                    "", TagFont, UI.Theme ? UI.Theme.current : Color.yellow, TextAlignmentOptions.MidlineLeft);
            }
            Refresh();
        }

        TextMeshProUGUI Cell(RectTransform parent, int column, float y, float height, string text, float size, Color color)
        {
            var rect = UI.Rect("Cell", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(X(column), y), new Vector2(Columns[column] - 6, height - 6));
            UI.Panel(rect, Panel).raycastTarget = false;
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
            for (int c = 0; c < 3; c++) cellBack[row, c].color = on ? RowHighlight : Panel;
            tags[row].text = on ? tag : "";
        }

        // "1 V → 0.2 A", in the same short form as the table.
        public string Pair(int row) => $"{Data.voltages[row]:0} V → {Data.currents[row]:0.0} A";

        public void ClearHighlights() { for (int r = 0; r < Data.Count; r++) Highlight(r, false); }

        public void GlowColumn(int column, bool on)
        {
            var color = on ? ColumnGlow : Panel;
            for (int r = 0; r < Data.Count; r++) cellBack[r, column].color = color;
        }

        // Steps down one column, lighting each filled cell in turn.
        public IEnumerator Sweep(int column, float perRow = 0.6f)
        {
            for (int r = 0; r < rows; r++)
            {
                cellBack[r, column].color = RowHighlight;
                yield return new WaitForSeconds(perRow);
                cellBack[r, column].color = Panel;
            }
        }

        void Refresh()
        {
            if (cells == null) return;
            ratioHeader.SetActive(ratio);
            for (int r = 0; r < Data.Count; r++)
            {
                rowObjects[r].SetActive(r < rows);
                cells[r, 0].text = units ? Data.voltages[r].ToString("0.0") + " V" : Data.voltages[r].ToString("0");
                cells[r, 1].text = units ? Data.currents[r].ToString("0.00") + " A" : Data.currents[r].ToString("0.0");
                cellBack[r, 2].transform.gameObject.SetActive(ratio);
                tags[r].rectTransform.anchoredPosition = new Vector2(X(ratio ? 3 : 2) + 16, 0); // tags hug the last visible column
            }
        }
    }
}
