using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Live readings in the lesson panel's Readings block: V and I (and R once Scene 12 has introduced it) with units,
    // read from CircuitRig.Active every frame, so a voltage button, a cue or an Ask Wattson action shows at once. When a
    // reading changes it gets a small arrow and a soft tint that fades (no blinking; no tint with ReducedMotion), and
    // the line under it keeps the previous value ("was 1.0 V"). v5: on the top strip (LessonPanel.ReadingsOnTop) the
    // values read 54 px in fixed-width cells (wide enough for "1.00 A" and its change arrow, ~230 px), and the table
    // never sends the card aside.
    public class LiveReadingsPanel : MonoBehaviour
    {
        const float ValueFont = 36f, NoteFont = 18f, Pad = 14f, VPad = 4f, ValueRow = 42f, NoteRow = 24f;
        const float TopValueFont = 54f, TopValueRow = 62f, TopCell = 240f;
        const float ChangeSeconds = 1.4f;

        public bool showResistance;

        enum Cell { V, I, R }
        const int CellCount = 3;

        RectTransform root;
        readonly RectTransform[] cells = new RectTransform[CellCount];
        readonly TextMeshProUGUI[] values = new TextMeshProUGUI[CellCount], notes = new TextMeshProUGUI[CellCount];
        float volts = float.NaN, amps, wasVolts, wasAmps, changedAt = -10f;
        int dirVolts, dirAmps;
        bool hasWas, seen;
        string shown = "";

        static Color VoltageColor => UI.Theme ? UI.Theme.voltage : new Color(0.25f, 0.66f, 1f);
        static Color CurrentColor => UI.Theme ? UI.Theme.current : new Color(1f, 0.72f, 0.19f);
        static Color ResistanceColor => UI.Theme ? UI.Theme.resistance : new Color(1f, 0.35f, 0.37f);
        static Color BaseColor(int c) => c == (int)Cell.V ? VoltageColor : c == (int)Cell.R ? ResistanceColor : CurrentColor;

        public static LiveReadingsPanel Create()
        {
            var lesson = LessonPanel.Instance;
            lesson.Clear("Readings");
            var block = lesson.Block("Readings");
            bool top = lesson.ReadingsOnTop;
            float valueFont = top ? TopValueFont : ValueFont, valueRow = top ? TopValueRow : ValueRow;
            var root = UI.Rect("Live Readings", block, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 2 * VPad + valueRow + NoteRow));
            LessonPanel.Fixed(root, 2 * VPad + valueRow + NoteRow);
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(0, 0, (int)VPad, (int)VPad);
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            var panel = root.gameObject.AddComponent<LiveReadingsPanel>();
            panel.root = root;
            for (int c = 0; c < CellCount; c++)
            {
                var cell = panel.cells[c] = UI.Rect(((Cell)c).ToString(), root, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
                if (top) { var size = cell.gameObject.AddComponent<LayoutElement>(); size.preferredWidth = size.minWidth = TopCell; }
                panel.values[c] = Text(cell, valueFont, BaseColor(c), 0f, valueRow);
                panel.notes[c] = Text(cell, NoteFont, LessonPanel.Muted, -valueRow, NoteRow);
            }
            lesson.Show("Readings", true);
            return panel;
        }

        static TextMeshProUGUI Text(RectTransform cell, float size, Color color, float y, float height)
        {
            var rect = UI.Rect("Text", cell, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y), new Vector2(0, height));
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(0, height);
            var label = UI.Label(rect, "", size, color, TextAlignmentOptions.Midline);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.fontStyle = FontStyles.Normal;
            if (size > 30) UI.Display(label);   // the values in the display face (Fredoka); the notes stay in the body face
            return label;
        }

        void LateUpdate()
        {
            var rig = CircuitRig.Active;
            if (!rig) return;
            // The card waits with the meters' plates: nothing to read until the baseline shows them. The block stays active
            // (this LateUpdate must keep running), so it is faded out and dropped from the column's layout instead.
            var block = (RectTransform)transform.parent;
            if (!block.TryGetComponent(out LayoutElement element)) element = block.gameObject.AddComponent<LayoutElement>();
            if (!block.TryGetComponent(out CanvasGroup group)) group = block.gameObject.AddComponent<CanvasGroup>();
            // v3: while the data card is up the meters' own readouts and the highlighted row already say it, so the card
            // steps aside (and the dock fits a 720p screen); so it does when the dock is too crowded for the graph.
            // On the top strip the card waits for the plates once (the baseline), then stays when they go: Scene 13's
            // finale hides the meters' plates and keeps the strip.
            var lesson = LessonPanel.Instance;
            seen |= rig.readoutsShown;
            bool visible = lesson && lesson.ReadingsOnTop ? seen : rig.readoutsShown && !(lesson && (lesson.InDock("Table") || lesson.SteppedAside("Readings")));
            element.ignoreLayout = !visible;
            group.alpha = visible ? 1f : 0f;
            if (!visible) return;
            float v = rig.Volts, i = rig.Amps;
            if (float.IsNaN(volts)) { volts = v; amps = i; }
            else if (!Mathf.Approximately(v, volts) || !Mathf.Approximately(i, amps))
            {
                wasVolts = volts; wasAmps = amps; hasWas = true;
                dirVolts = Sign(v - volts); dirAmps = Sign(i - amps);
                volts = v; amps = i;
                changedAt = Time.time;
            }

            string state = $"{volts}|{amps}|{changedAt}|{showResistance}";
            if (state != shown)
            {
                shown = state;
                var inv = CultureInfo.InvariantCulture;
                values[0].text = volts.ToString("0.0", inv) + " V" + Arrow(dirVolts);
                values[1].text = amps.ToString("0.00", inv) + " A" + Arrow(dirAmps);
                values[2].text = amps > 0f ? (volts / amps).ToString("0.0", inv) + " Ω" : "— Ω";
                notes[0].text = hasWas ? "was " + wasVolts.ToString("0.0", inv) + " V" : "voltage";
                notes[1].text = hasWas ? "was " + wasAmps.ToString("0.00", inv) + " A" : "current";
                notes[2].text = "R = V ÷ I";
                cells[2].gameObject.SetActive(showResistance);
            }

            // The tint of what changed fades out over ChangeSeconds.
            float k = ReducedMotion.On ? 0f : 0.6f * Mathf.Clamp01(1f - (Time.time - changedAt) / ChangeSeconds);
            values[0].color = Color.Lerp(VoltageColor, Color.white, dirVolts != 0 ? k : 0f);
            values[1].color = Color.Lerp(CurrentColor, Color.white, dirAmps != 0 ? k : 0f);
        }

        static int Sign(float d) => Mathf.Abs(d) < 1e-4f ? 0 : d > 0 ? 1 : -1;
        static string Arrow(int dir) => dir == 0 ? "" : dir > 0 ? "<size=70%> ▲</size>" : "<size=70%> ▼</size>";
    }
}
