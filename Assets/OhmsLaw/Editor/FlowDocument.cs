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

        // Hand-written from the builders' Gate(...) calls and the gate contract (Docs/Polish/WOW_PLAN.md §8). Graded questions
        // (Ask): a wrong pick gets its feedback line, a hint and a retry, at most twice, then the reveal; option A is correct.
        const string Chart = @"flowchart TD
    S04[Scene 4: Meet Wattson] -->|flash to white| S05[Scene 5: Inside the Wire]
    S05 --> D{S05_Drift graded<br>A minus to plus / B plus to minus / C no overall direction}
    D -->|A correct, C partial| S05b[Drift Arrow, S05_L06 ..]
    D -->|B wrong| DH[S05_Drift_FB, hint S05_Drift_H, retry] --> D
    D -->|out of retries| DX[S05_Drift_X over the running drift] --> S05b
    S05b -->|match-cut, handoff state| S06[Scene 6: Adaptive Question]
    S06 --> P{S06_Prediction ungraded<br>A Increase / B Decrease / C Stay the same}
    P -->|stored in LearnerSession.prediction| S06R[S06_R_A/B/C, then S06_L03]
    S06R -->|Ohm-niscope flash, fade white| S07[Scene 7: Back to Georg Ohm]
    S07 -->|match-cut| S08[Scene 8: Our Lab]
    S08 -->|match-cut, CircuitState| S09[Scene 9: Build the Circuit]
    S09 --> G1[S09_PlaceAmmeter] --> G1b{S09_Conventional say gate}
    G1b -->|picked B at S05_Drift| G1c[S09_Conventional_If_S05_Drift_B]
    G1b -->|else| G1d[S09_Conventional]
    G1c --> G2[S09_PlaceVoltmeter]
    G1d --> G2
    G2 --> G3[S09_CloseKey]
    G3 -->|reminder chalk, showLearnerPrediction recalls Scene 6| S10[Scene 10: Experiment]
    S10 --> G4[S10_Set1V, controls capped at 1 V] --> G5[S10_Record row 1, cap 1 V] --> G5b{S10_Predict2V ungraded<br>A 0.40 A / B 0.20 A / C 0.10 A}
    G5b -->|stored in LearnerSession.s10Prediction2V| G5c[S10_Set2V] --> G5d[S10_Record2 row 2<br>the meter grades the prediction: S10_Predict2V_FA/_FB/_FC]
    G5d -->|3-5 V auto-run, rows 3-5| G6{S10_Pattern graded}
    G6 -->|A correct| G7[S10_Discover: V x 2 to I x 2]
    G6 -->|wrong| W1[S10_Pattern_FB/_FC, S10_R_WRONG, re-ask]
    W1 -->|Struggling Relationship| W1b[rows 1-2 highlighted, simpler question S10_Pattern_E]
    W1 --> G6
    W1b --> G6
    G6 -->|out of retries| W2[V then I column sweep, S10_Pattern_X] --> G7
    G7 -->|Scene 6 callback S10_Discover_FA/_FB/_FC| S11[Scene 11: Graph]
    S11 --> T1[S11_Target: make the ammeter read 0.60 A<br>miss: S11_Target_H, 2nd miss: Wattson sets 3 V with _X, hit: _OK]
    T1 -->|Confident Graph| T2[bonus 0.80 A: _GO2 / _X2 / _OK2] --> S12
    T1 -->|else| S12[Scene 12: Resistance]
    S12 --> R1{S12_Ratio graded<br>5 / 0.2 / 1.8}
    R1 -->|A correct| R2
    R1 -->|wrong| R1h[S12_Ratio_FB/_FC, hint _H, retry] --> R1
    R1 -->|out of retries| R1x[S12_Ratio_X, row 3 fills] --> R2
    R2{S12_AQ1 checkpointEnabled<br>skipped when Confident Relationship}
    R2 -->|A correct, or skipped| G8
    R2 -->|wrong| R2h[S12_AQ1_FB/_FC, AQ_EXPLAIN, retry on AQ_EASY] --> R2
    R2 -->|out of retries| R2x[S12_AQ1_X: 2 V then 4 V on the readings strip] --> G8
    G8{S12_Slope graded<br>5 / 0.2 / 2.5}
    G8 -->|A correct| S12b[S12_L13 .. S12_L15, readings.r: the ohm is taught]
    G8 -->|wrong| W3[S12_Slope_FB/_FC, hint _H, retry] --> G8
    G8 -->|out of retries| W3x[S12_Slope_X] --> S12b
    S12b -->|match-cut| S13[Scene 13: Ohm's Law]
    S13 --> C{checkpointEnabled?}
    C -->|yes| Q2{S13_AQ2 graded}
    Q2 -->|A correct| AP
    Q2 -->|wrong| Q2h[S13_AQ2_FB/_FC, hint _H, retry] --> Q2
    Q2 -->|out of retries| Q2x[S13_AQ2_X] --> AP
    C -->|no| AP[S13_Apply: make the ammeter read 0.80 A with V = IR<br>miss: S13_Apply_H, 2nd miss: Wattson sets 4 V with _X, hit: _OK]
    AP --> RC{S13_Recap say gate}
    RC -->|Struggling OhmsLaw| RC1[S13_Recap_If_Struggling_OhmsLaw]
    RC -->|Struggling Resistance| RC2[S13_Recap_If_Struggling_Resistance]
    RC -->|else| RC3[S13_Recap]
    RC1 --> S13end[S13_L07]
    RC2 --> S13end
    RC3 --> S13end
    S13end -->|UnitySectionComplete| V14[Scene 14 video card]";
    }
}
