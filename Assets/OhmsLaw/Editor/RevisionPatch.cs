using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace OhmsLaw.Editor
{
    // The 2026-10-22 revision's changes to Scenes 5 and 6 (Docs/Polish/REVISION_2026-10-22.md, decision 3). The user
    // hand-edits those scenes, so they are never rebuilt: this edits only their generated Timeline assets, in place, and
    // never opens or saves a .unity file.
    //   Scene 5: S05_V10 ("how do you think the two are related?") and S05_L09 ("want to predict?") repeated the question
    //            Scene 6 asks. Both lines go, and everything after them moves up by the time they took, so the scene ends
    //            sooner and the Detective Test follows the explanation of V and I at once.
    //   Scene 6: its "music off" / "ambience off" cues go, so the wire bed carries into Scene 7 and crossfades there instead
    //            of fading to silence under the flash.
    // Idempotent: a second run finds nothing to change. Menu (and batch: "menu Ohm's Law/Revise/Patch Scene 5-6 Timelines
    // (no rebuild)").
    public static class RevisionPatch
    {
        public const string Scene05Timeline = "Assets/OhmsLaw/Scene05/Timeline/Scene05_Timeline.playable";
        public const string Scene06Timeline = "Assets/OhmsLaw/Scene06/Timeline/Scene06_Timeline.playable";
        static readonly string[] Scene05Cut = { "S05_V10", "S05_L09" };
        static readonly string[] Scene06Removed = { "music off", "ambience off" };
        const double Epsilon = 0.0005;

        [MenuItem("Ohm's Law/Revise/Patch Scene 5-6 Timelines (no rebuild)")]
        public static void PatchWireTimelines()
        {
            var log = new StringBuilder("RevisionPatch:\n");
            bool changed = CutLines(Scene05Timeline, Scene05Cut, log) | RemoveCues(Scene06Timeline, Scene06Removed, log);
            if (changed) AssetDatabase.SaveAssets();
            Debug.Log(log.Append(changed ? "saved." : "nothing to change (already patched).").ToString());
        }

        // Removes the lines' subtitle and VO clips and closes the gap: every time t in the timeline maps to
        //   t                  before the cut,
        //   the cut's start    inside it (a gesture or a cue that began with the cut lines begins where they began),
        //   t - (cut length)   after it,
        // for clip starts and ends (so a camera shot or a gesture spanning the cut is shortened), markers, signals and the
        // timeline's length. The cut runs from the first line's start to the first beat after the last line (the gap after
        // a line is the next beat's lead-in, so the beat after the cut keeps the lead-in the first cut line had).
        static bool CutLines(string path, string[] ids, StringBuilder log)
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (!timeline) { log.AppendLine("  " + path + ": missing"); return false; }
            var tracks = timeline.GetOutputTracks().ToList();
            var lineClips = tracks.OfType<SubtitleTrack>().SelectMany(t => t.GetClips())
                .Where(c => c.asset is SubtitleClip s && s.line && ids.Contains(s.line.id)).ToList();
            if (lineClips.Count == 0) { log.AppendLine("  " + path + ": " + string.Join(", ", ids) + " already gone"); return false; }

            double cutStart = lineClips.Min(c => c.start), lastEnd = lineClips.Max(c => c.end);
            // The VO clips of those lines sit at the same starts on the VO track.
            var starts = lineClips.Select(c => c.start).ToList();
            var voClips = tracks.OfType<AudioTrack>().Where(t => t.name == "VO").SelectMany(t => t.GetClips())
                .Where(c => starts.Any(s => System.Math.Abs(c.start - s) < Epsilon)).ToList();
            var removed = new HashSet<TimelineClip>(lineClips.Concat(voClips));

            // The first beat after the last line: the earliest clip start or marker at or after its end.
            double cutEnd = double.MaxValue;
            foreach (var track in tracks)
            {
                foreach (var clip in track.GetClips()) if (!removed.Contains(clip) && clip.start >= lastEnd - Epsilon) cutEnd = System.Math.Min(cutEnd, clip.start);
                foreach (var marker in track.GetMarkers()) if (marker.time >= lastEnd - Epsilon) cutEnd = System.Math.Min(cutEnd, marker.time);
            }
            if (timeline.markerTrack && !tracks.Contains(timeline.markerTrack)) foreach (var marker in timeline.markerTrack.GetMarkers()) if (marker.time >= lastEnd - Epsilon) cutEnd = System.Math.Min(cutEnd, marker.time);
            if (cutEnd == double.MaxValue) cutEnd = lastEnd;
            double cut = cutEnd - cutStart;
            double Map(double t) => t < cutStart ? t : t < cutEnd ? cutStart : t - cut;

            foreach (var clip in removed)
            {
                var track = clip.GetParentTrack();
                timeline.DeleteClip(clip);
                if (track) EditorUtility.SetDirty(track);
            }
            int moved = 0, dropped = 0, markers = 0;
            foreach (var track in timeline.GetOutputTracks().ToList())
            {
                bool dirty = false;
                foreach (var clip in track.GetClips().ToList())
                {
                    double start = Map(clip.start), end = Map(clip.end);
                    if (end - start < 0.01)
                    {
                        if (clip.end > cutStart && clip.start < cutEnd) { timeline.DeleteClip(clip); dropped++; dirty = true; } // wholly inside the cut
                        continue;
                    }
                    if (System.Math.Abs(start - clip.start) < Epsilon && System.Math.Abs(end - clip.end) < Epsilon) continue;
                    clip.start = start;
                    clip.duration = end - start;
                    clip.blendInDuration = System.Math.Min(clip.blendInDuration, clip.duration);
                    clip.blendOutDuration = System.Math.Min(clip.blendOutDuration, clip.duration);
                    moved++;
                    dirty = true;
                }
                foreach (var marker in track.GetMarkers().ToList()) dirty |= MoveMarker(marker, Map, ref markers);
                if (dirty) EditorUtility.SetDirty(track);
            }
            if (timeline.markerTrack && !timeline.GetOutputTracks().Contains(timeline.markerTrack)) // never map a marker twice
            {
                bool dirty = false;
                foreach (var marker in timeline.markerTrack.GetMarkers().ToList()) dirty |= MoveMarker(marker, Map, ref markers);
                if (dirty) EditorUtility.SetDirty(timeline.markerTrack);
            }
            if (timeline.durationMode == TimelineAsset.DurationMode.FixedLength) timeline.fixedDuration = Map(timeline.fixedDuration);
            EditorUtility.SetDirty(timeline);
            log.AppendLine($"  {path}: removed {string.Join(", ", ids)} ({lineClips.Count} subtitle + {voClips.Count} VO clips); " +
                           $"cut {cutStart:0.00}-{cutEnd:0.00} s ({cut:0.00} s); {moved} clips and {markers} markers moved, {dropped} dropped; " +
                           $"length now {timeline.duration:0.00} s");
            return true;
        }

        static bool MoveMarker(IMarker marker, System.Func<double, double> map, ref int count)
        {
            double time = map(marker.time);
            if (System.Math.Abs(time - marker.time) < Epsilon) return false;
            marker.time = time;
            count++;
            return true;
        }

        // Deletes the marker-track cues with these exact commands.
        static bool RemoveCues(string path, string[] commands, StringBuilder log)
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (!timeline || !timeline.markerTrack) { log.AppendLine("  " + path + ": missing or no marker track"); return false; }
            var gone = timeline.markerTrack.GetMarkers().OfType<Cue>().Where(c => commands.Contains(c.command)).ToList();
            if (gone.Count == 0) { log.AppendLine("  " + path + ": no " + string.Join(" / ", commands) + " cues (already patched)"); return false; }
            foreach (var cue in gone) timeline.markerTrack.DeleteMarker(cue);
            EditorUtility.SetDirty(timeline.markerTrack);
            EditorUtility.SetDirty(timeline);
            log.AppendLine($"  {path}: removed {gone.Count} cue(s): " + string.Join(", ", gone.Select(c => $"'{c.command}' at {c.time:0.00} s")));
            return true;
        }
    }
}
