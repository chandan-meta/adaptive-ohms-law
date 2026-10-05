using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Live readings beside the graph: V and I (and R once Scene 12 has introduced it) with units, read from
    // CircuitRig.Active every frame, so a slider move, a cue or an Ask Wattson action shows at once. When a reading
    // changes it gets a small arrow and a soft tint that fades (no blinking; no tint with ReducedMotion), and the
    // line under it keeps the previous value ("was 1.0 V"). A target activity adds a target column (rig.targetAmps).
    //
    // On the board: a strip in the free band above the graph's chalk frame, on the graph canvas (so it moves and hides with
    // the graph and stays clear of the left-docked Ask Wattson panel). Its sizes are designed in screen px for the Boards
    // shot (TYPOGRAPHY: values at the 36 px "L" meter-digit tier, notes at the 24 px "S" tier) and scaled into canvas px.
    // With no graph in the scene (Scene 10) the same panel is a HUD card at the bottom right, above the subtitles.
    public class LiveReadingsPanel : MonoBehaviour
    {
        const float ValueFont = 50f, NoteFont = 34f;                 // caps ~36 px and ~24 px
        const float Pad = 16f, Gap = 30f, VPad = 5f, ValueRow = 44f, NoteRow = 30f, RowGap = 2f;
        const float ScreenPxPerCanvasPx = 0.672f;                    // Boards shot: 0.00135 m per canvas px at ~498 px/m
        const float AboveCanvas = 34f;                               // canvas px: the chalk frame's 0.03 m margin and half its line
        const float ChangeSeconds = 1.4f;

        public bool showResistance;
        [Tooltip("Under the target value after a miss (\"▲ more volts\"); a target activity sets it.")] public string targetNote = "";

        enum Cell { V, I, R, Target }
        static readonly int CellCount = 4;

        RectTransform root;
        readonly RectTransform[] cells = new RectTransform[CellCount];
        readonly TextMeshProUGUI[] values = new TextMeshProUGUI[CellCount], notes = new TextMeshProUGUI[CellCount];
        float volts = float.NaN, amps, wasVolts, wasAmps, changedAt = -10f;
        int dirVolts, dirAmps;
        bool hasWas;
        string shown = ""; // what the texts were last written for (readings, target, note, R): rewritten only when it changes

        static Color VoltageColor => UI.Theme ? UI.Theme.voltage : new Color(0.25f, 0.66f, 1f);
        static Color CurrentColor => UI.Theme ? UI.Theme.current : new Color(1f, 0.72f, 0.19f);
        static Color ResistanceColor => UI.Theme ? UI.Theme.resistance : new Color(1f, 0.35f, 0.37f);
        static Color BaseColor(int c) => c == (int)Cell.V ? VoltageColor : c == (int)Cell.R ? ResistanceColor : CurrentColor;

        // On the graph's canvas when there is a graph, else under `hud` (the module canvas's SafeArea).
        public static LiveReadingsPanel Create(GraphUI graph, Transform hud)
        {
            RectTransform root;
            if (graph)
            {
                root = UI.Rect("Live Readings", graph.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, AboveCanvas), Vector2.zero);
                root.localScale = Vector3.one / ScreenPxPerCanvasPx;
                UI.Panel(root, ObservationTableUI.BoardTint, true, 18f).raycastTarget = false;
            }
            else
            {
                root = UI.Rect("Live Readings", hud, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-48f, UI.SubtitleTop + 16f), Vector2.zero);
                UI.Panel(root, UI.HudColor, true, 18f).raycastTarget = false;
                root.gameObject.AddComponent<UIFade>();
            }
            var panel = root.gameObject.AddComponent<LiveReadingsPanel>();
            panel.root = root;
            for (int c = 0; c < CellCount; c++)
            {
                var cell = panel.cells[c] = UI.Rect(((Cell)c).ToString(), root, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
                panel.values[c] = Text(cell, ValueFont, BaseColor(c), 0f, ValueRow);
                panel.notes[c] = Text(cell, NoteFont, UI.Alpha(Color.white, 0.85f), -(ValueRow + RowGap), NoteRow);
            }
            return panel;
        }

        static TextMeshProUGUI Text(RectTransform cell, float size, Color color, float y, float height)
        {
            var rect = UI.Rect("Text", cell, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y), new Vector2(0, height));
            rect.anchorMax = new Vector2(1, 1); // full cell width
            rect.sizeDelta = new Vector2(0, height);
            var label = UI.Label(rect, "", size, color, TextAlignmentOptions.Midline); // Midline centres the ink, so rows can be tight
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        void LateUpdate()
        {
            var rig = CircuitRig.Active;
            if (!rig) return; // the rig is hidden (Scene 12's dive): keep the last readings rather than invent a change to 0
            if (!rig.readoutsShown) return; // the meters' plates are hidden (Scenes 9-10 before the 1 V baseline): no number here either
            float v = rig.Volts, i = rig.Amps;
            if (float.IsNaN(volts)) { volts = v; amps = i; }
            else if (!Mathf.Approximately(v, volts) || !Mathf.Approximately(i, amps))
            {
                wasVolts = volts; wasAmps = amps; hasWas = true;
                dirVolts = Sign(v - volts); dirAmps = Sign(i - amps);
                volts = v; amps = i;
                changedAt = Time.time;
            }

            float target = rig.targetAmps;
            bool showTarget = target >= 0f, onTarget = showTarget && Mathf.Abs(amps - target) <= TargetRun.Tolerance;
            string state = $"{volts}|{amps}|{changedAt}|{target}|{onTarget}|{targetNote}|{showResistance}";
            if (state != shown)
            {
                shown = state;
                var inv = CultureInfo.InvariantCulture;
                values[0].text = volts.ToString("0.0", inv) + " V" + Arrow(dirVolts);
                values[1].text = amps.ToString("0.00", inv) + " A" + Arrow(dirAmps);
                values[2].text = amps > 0f ? (volts / amps).ToString("0.0", inv) + " Ω" : "— Ω";
                values[3].text = showTarget ? target.ToString("0.00", inv) + " A" : "";
                notes[0].text = hasWas ? "was " + wasVolts.ToString("0.0", inv) + " V" : "";
                notes[1].text = hasWas ? "was " + wasAmps.ToString("0.00", inv) + " A" : "";
                notes[2].text = "R = V ÷ I";
                notes[3].text = !showTarget ? "" : onTarget ? "✓ target" : targetNote != "" ? targetNote : "target";
                Layout(new[] { true, true, showResistance, showTarget });
            }

            // The tint of what changed fades out over ChangeSeconds.
            float k = ReducedMotion.On ? 0f : 0.6f * Mathf.Clamp01(1f - (Time.time - changedAt) / ChangeSeconds);
            values[0].color = Color.Lerp(VoltageColor, Color.white, dirVolts != 0 ? k : 0f);
            values[1].color = Color.Lerp(CurrentColor, Color.white, dirAmps != 0 ? k : 0f);
        }

        static int Sign(float d) => Mathf.Abs(d) < 1e-4f ? 0 : d > 0 ? 1 : -1;
        static string Arrow(int dir) => dir == 0 ? "" : dir > 0 ? "<size=70%> ▲</size>" : "<size=70%> ▼</size>";

        // Cells side by side, each as wide as its widest line.
        void Layout(bool[] visible)
        {
            float x = Pad;
            for (int c = 0; c < CellCount; c++)
            {
                cells[c].gameObject.SetActive(visible[c]);
                if (!visible[c]) continue;
                float width = Mathf.Max(values[c].GetPreferredValues(values[c].text, 4000f, 0).x, notes[c].GetPreferredValues(notes[c].text, 4000f, 0).x, 120f);
                cells[c].anchoredPosition = new Vector2(x, -VPad);
                cells[c].sizeDelta = new Vector2(width, ValueRow + RowGap + NoteRow);
                x += width + Gap;
            }
            root.sizeDelta = new Vector2(x - Gap + Pad, 2 * VPad + ValueRow + RowGap + NoteRow);
        }
    }
}
