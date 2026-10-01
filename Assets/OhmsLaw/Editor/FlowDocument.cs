using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace OhmsLaw.Editor
{
    // Docs/FLOW.md: every scene's beats in order from its Timeline (lines, cues, gates, hand-offs), the totals,
    // and a Mermaid chart of the module with its branches.
    public static class FlowDocument
    {
        [MenuItem("Ohm's Law/Generate FLOW.md")]
        public static void Write()
        {
            var sb = new StringBuilder("# Ohm's Law module flow\n\nGenerated " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " from the scene Timelines. Scene 4 is the polished build; Scenes 5-13 are blockouts.\n\n");
            double module = 0;
            var totals = new StringBuilder("| Scene | Timeline length | Lines | Interactions |\n|---|---:|---:|---:|\n");
            var proposed = new List<string>();

            foreach (var builder in BlockoutAssets.Builders().Where(b => b.SceneId != "Scene00Test"))
            {
                string folder = SceneBuilderBase.Root + "/" + builder.SceneId;
                var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(folder + "/Timeline/" + builder.SceneId + "_Timeline.playable");
                sb.AppendLine($"## {builder.SceneId} — {builder.Title}\n");
                if (!timeline) { sb.AppendLine("_Not built yet._\n"); totals.AppendLine($"| {builder.SceneId} | – | – | – |"); continue; }

                var rows = new List<(double at, int order, string type, string what, string length)>();
                foreach (var clip in timeline.GetOutputTracks().OfType<SubtitleTrack>().SelectMany(t => t.GetClips()))
                {
                    var line = ((SubtitleClip)clip.asset).line;
                    rows.Add((clip.start, 1, "VO", $"`{line.id}` {line.text}", $"{clip.duration:0.0} s"));
                }
                foreach (var clip in timeline.GetOutputTracks().OfType<Unity.Cinemachine.CinemachineTrack>().SelectMany(t => t.GetClips()))
                    rows.Add((clip.start, 0, "Camera", clip.displayName, ""));
                foreach (var marker in timeline.GetOutputTracks().Where(t => t != timeline.markerTrack).SelectMany(t => t.GetMarkers()).Concat(timeline.markerTrack ? timeline.markerTrack.GetMarkers() : new IMarker[0]))
                {
                    switch (marker)
                    {
                        case InteractionGate gate:
                            rows.Add((gate.time, 2, "Interaction", $"`{gate.gateId}` — {gate.description}" + (gate.lines.Length > 0 ? "<br>Lines: " + string.Join("; ", gate.lines.Select(l => $"`{l.id}` {l.text}")) : ""), "waits"));
                            break;
                        case Cue cue when cue.command.StartsWith("note"):
                            rows.Add((cue.time, 3, "Visual", cue.note, cue.command.Length > 5 ? cue.command.Substring(5) + " s" : ""));
                            break;
                        case Cue cue when cue.command.StartsWith("handoff") || cue.command.StartsWith("fade"):
                            rows.Add((cue.time, 3, "Visual", cue.command + (cue.note != "" ? " — " + cue.note : ""), ""));
                            break;
                        case Cue cue when cue.note != "":
                            rows.Add((cue.time, 3, "Visual", cue.note + $" (`{cue.command}`)", ""));
                            break;
                        case SignalEmitter signal when signal.asset:
                            rows.Add((signal.time, 4, "Hand-off", signal.asset.name + (signal.asset.name.Contains("Complete") ? " → next scene" : ""), ""));
                            break;
                    }
                }
                sb.AppendLine("| Start | Type | Beat | Duration |\n|---:|---|---|---:|");
                foreach (var row in rows.OrderBy(r => r.at).ThenBy(r => r.order)) sb.AppendLine($"| {row.at:0.0} s | {row.type} | {row.what} | {row.length} |");
                double length = timeline.duration;
                module += length;
                int lines = rows.Count(r => r.type == "VO"), gates = rows.Count(r => r.type == "Interaction");
                sb.AppendLine($"\n**Scene total:** {length:0.0} s of Timeline, plus time spent at {gates} interaction gate(s).\n");
                totals.AppendLine($"| {builder.SceneId} | {length:0.0} s | {lines} | {gates} |");

                foreach (var guid in AssetDatabase.FindAssets("t:DialogueLine", new[] { folder + "/Dialogue/Proposed" }))
                {
                    var line = AssetDatabase.LoadAssetAtPath<DialogueLine>(AssetDatabase.GUIDToAssetPath(guid));
                    proposed.Add($"| {builder.SceneId} | `{line.id}` | {line.text} |");
                }
            }

            sb.AppendLine("## Totals\n");
            sb.AppendLine(totals.ToString());
            sb.AppendLine($"**Module Timeline total (Scenes 5-13):** {module:0.0} s ≈ {module / 60:0.0} min, before interaction wait times. Scene 4 adds about 50 s.\n");
            sb.AppendLine("## Proposed lines (not in the storyboard)\n\n| Scene | ID | Text |\n|---|---|---|");
            foreach (var p in proposed) sb.AppendLine(p);
            sb.AppendLine("\n## Module chart\n");
            sb.AppendLine("```mermaid");
            sb.AppendLine(Chart);
            sb.AppendLine("```");
            Directory.CreateDirectory("Docs");
            File.WriteAllText("Docs/FLOW.md", sb.ToString());
            Debug.Log("Wrote Docs/FLOW.md");
        }

        const string Chart = @"flowchart TD
    S04[Scene 4: Meet Wattson] -->|flash to white| S05[Scene 5: Inside the Wire]
    S05 -->|match-cut, handoff state| S06[Scene 6: Adaptive Question]
    S06 --> P{S06_Prediction<br>A Increase / B Decrease / C Stay the same}
    P -->|stored in LearnerSession.prediction| S06R[Proposed reaction S06_R_A/B/C, then S06_L03]
    S06R -->|Ohm-niscope flash, fade white| S07[Scene 7: Back to Georg Ohm]
    S07 -->|match-cut| S08[Scene 8: Our Lab]
    S08 -->|match-cut, CircuitState| S09[Scene 9: Build the Circuit]
    S09 --> G1[S09_PlaceAmmeter] --> G2[S09_PlaceVoltmeter] --> G3[S09_CloseKey]
    G3 -->|reminder card; showLearnerPrediction recalls Scene 6| S10[Scene 10: Experiment]
    S10 --> G4[S10_Set1V] --> G5[S10_Record] --> G6{S10_Pattern}
    G6 -->|correct A| G7[S10_Discover ×2]
    G6 -->|wrong| W1[S10_R_WRONG, re-ask]
    W1 -->|wrong again| W2[Sweep V then I column, re-ask]
    W2 -->|3rd answer accepted| G7
    G7 --> S11[Scene 11: Graph]
    S11 -->|graph + table kept| S12[Scene 12: Resistance]
    S12 --> G8{S12_Slope 5 / 0.2 / 2.5}
    G8 -->|correct| S12b[S12_L13 .. S12_L15]
    G8 -->|wrong| W3[shake, show 2.0 ÷ 0.40, re-ask once] --> S12b
    S12b --> S13[Scene 13: Ohm's Law]
    S13 --> C{checkpoint enabled?}
    C -->|no| S13end[S13_L07]
    C -->|yes| Q1{AQ1}
    Q1 -->|correct| Q2{AQ2 harder} --> S13end
    Q1 -->|wrong| E[AQ_EXPLAIN] --> Q3{AQ_EASY} --> S13end
    S13end -->|UnitySectionComplete| V14[Scene 14 video card]";
    }
}
