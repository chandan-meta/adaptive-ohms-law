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

        // Hand-written from the builders' Gate(...) calls and Docs/Polish/REVISION_2026-10-22.md. The two detective challenges
        // are graded: a miss marks the option out, shows a hint aimed at it (and a clue in the scene), and the learner picks
        // again; a second miss shows the answer and goes on.
        const string Chart = @"flowchart TD
    S04[Scene 4: Meet Wattson, tap the wire] -->|the wire bed plays on through the dive| S05[Scene 5: Inside the Wire]
    S05 --> AV[S05_ApplyVoltage: tap the + terminal] --> S05b[V the push, I the charge per second]
    S05b -->|match-cut, the bed carries| S06[Scene 6: Detective Test]
    S06 --> P{S06_Prediction graded<br>V up, R constant: A increases / B decreases / C stays the same}
    P -->|A| S06R[S06_R_A, then S06_L03]
    P -->|B or C| PH[option out, hint, + terminal pulses, pick again] --> P
    P -->|second miss| PX[answer shown] --> S06R
    S06R -->|Ohm-niscope flash; the wire bed crossfades in Scene 7| S07[Scene 7: Back to Georg Ohm]
    S07 -->|match-cut| S08[Scene 8: Our Lab]
    S08 -->|match-cut, CircuitState| S09[Scene 9: Build the Circuit]
    S09 --> G1[S09_PlaceAmmeter: one tap, it flies in, in series] --> G2[S09_PlaceVoltmeter: one tap, across XY]
    G2 -->|the key closes by itself| S10[Scene 10: Experiment]
    S10 --> G4[S10_Set1V .. S10_Set5V: slider, rows record, points plot]
    G4 --> ST[centre stage: rows 1-2 linked to their points, V x 2 to I x 2]
    ST -->|S10_Callback on the Scene 6 first pick| S11[Scene 11: The Pattern]
    S11 --> DL[S11_DrawLine] --> AX[centre stage: the axes named, the doubling read off, V ∝ I]
    AX --> S12[Scene 12: Resistance]
    S12 --> SW[S12_SwapWire: copper, then nichrome, same length, thickness, temperature, voltage] --> RN[resistance named]
    RN --> Q2{S12_MoreResistance graded<br>same V, more R: A increases / B decreases / C stays the same}
    Q2 -->|B| Q2R[S12_MR_R]
    Q2 -->|A or C| Q2H[option out, hint, the resistance note and the wire light, pick again] --> Q2
    Q2 -->|second miss| Q2X[answer shown] --> Q2R
    Q2R --> RD[centre stage: V / I = 5 V/A every time, slope = ΔV ÷ ΔI, the ohm]
    RD -->|match-cut| S13[Scene 13: Ohm's Law]
    S13 --> LP[V, I, R arrive on his snaps] --> BL[S13_BuildLaw: tap V, I, R into V = I × R]
    BL --> NM[OHM'S LAW, conditions, I = V ÷ R and R = V ÷ I, the bench sweeps 1-5 V]
    NM --> END[case closed]
    END -->|Explore Now| LAB[Ohm's Law lab: slider, meters, table, graph, R = V ÷ I check, resistance control, double-the-current challenge]
    END -->|UnitySectionComplete| V14[Scene 14 video card]";
    }
}
