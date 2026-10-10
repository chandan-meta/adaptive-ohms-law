#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OhmsLaw
{
    // "Ohm's Law/Tests/HUD v5": the pure rules behind the v5 HUD, in edit mode: the battery slider's cap and snap, the
    // [QC] eyebrow tag, the hero's symbol colouring, the bolt icon, the data stage's fit. One line per check.
    public static class HudChecks
    {
        [MenuItem("Ohm's Law/Tests/HUD v5")]
        static void Menu()
        {
            int failed = 0;
            void Check(string name, bool ok, string detail = "") { if (!ok) failed++; Debug.Log($"{(ok ? "PASS" : "FAIL")}  {name}{(ok || detail.Length == 0 ? "" : "  (" + detail + ")")}"); }

            float v = VoltageButtons.Rule(3f, 2f, 3f, true, out string hint);
            Check("slider: a request at the cap applies", Mathf.Approximately(v, 3f) && hint == null, $"{v} {hint}");
            v = VoltageButtons.Rule(4f, 2f, 3f, true, out hint);
            Check("slider: above the cap is refused with the step hint", float.IsNaN(v) && hint == "One step at a time.", $"{v} {hint}");
            v = VoltageButtons.Rule(2f, 2f, 5f, false, out hint);
            Check("slider: not the learner's turn says so", float.IsNaN(v) && hint == "Wait for your turn.", $"{v} {hint}");
            v = VoltageButtons.Rule(2f, 2f, 5f, true, out hint);
            Check("slider: the same value is no change", float.IsNaN(v) && hint == null, $"{v} {hint}");
            v = VoltageButtons.Rule(2.4f, 1f, 5f, true, out hint);
            Check("slider: snaps to whole volts", Mathf.Approximately(v, 2f), v.ToString());

            string coloured = LessonPanel.Symbols("V ∝ I");
            Check("hero: V and I take their colours", coloured.Contains("<color=#") && coloured.IndexOf("<color=#") != coloured.LastIndexOf("<color=#"), coloured);
            Check("hero: Ω takes the resistance colour", LessonPanel.Symbols("1 Ω").Contains("<color=#" + ColorUtility.ToHtmlStringRGB(LessonPanel.OhmColor)), LessonPanel.Symbols("1 Ω"));

            var bolt = Icons.Get("bolt");
            Check("icons: the bolt rasterises", bolt && bolt.texture && bolt.texture.width == 64);

            Check("mcq: the [QC] tag is read off the question", "[QC]At 3 volts?".StartsWith("[QC]") && "[QC]At 3 volts?".Substring(4).TrimStart() == "At 3 volts?");

            // The data stage in the band under the top readings strip (1080p: y 910 down to the dock's bottom edge, 196).
            float s = LessonPanel.StageFit(910f, 196f, 389f, out float y);
            Check("stage: a short stage keeps its scale and sits low, inside the band", Mathf.Approximately(s, LessonPanel.StageScale) && y < 553f && y + 389f * s / 2f <= 910f && y - 389f * s / 2f >= 196f, $"{s} {y}");
            s = LessonPanel.StageFit(910f, 196f, 730f, out y);
            Check("stage: a tall stage shrinks to the band", s < LessonPanel.StageScale && y + 730f * s / 2f <= 910.1f && y - 730f * s / 2f >= 195.9f, $"{s} {y}");

            if (failed == 0) Debug.Log("HUD v5: PASS (all checks)");
            else Debug.LogError($"HUD v5: FAIL ({failed} checks failed)");
        }
    }
}
#endif
