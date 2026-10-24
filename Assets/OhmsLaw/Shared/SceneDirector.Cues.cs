using System.Collections;
using UnityEngine;

namespace OhmsLaw
{
    // Cues added by the polish areas, each in its own partial file (SceneDirector.Wire/Lab/Experiment.cs) so they can
    // change in parallel. Run() falls through to here for any verb it does not handle; a handler returns true if the
    // verb is its own.
    public partial class SceneDirector
    {
        bool AreaCue(string verb, string[] words, string rest, float value, bool on) =>
            WireCue(verb, words, rest, value, on) || LabCue(verb, words, rest, value, on) || ExperimentCue(verb, words, rest, value, on);

        // A reveal lands on Wattson's finger snap: he snaps (when he is on screen and not mid-snap) and the thing appears
        // SnapLead later, on the click; without a snap it appears at once. Never on a scene's restoring cues (its first
        // quarter second: a match-cut re-cueing what the last scene left on screen) or in the frame of a Timeline jump.
        public static bool SnapReveal()
        {
            if (Time.frameCount - ModuleFlow.JumpFrame <= 1) return false;
            var flow = ModuleFlow.Instance;
            if (flow && flow.Director && flow.Director.time < 0.25) return false;
            if (!GuideCalls.Snap()) return false;
            SnappedFrame = Time.frameCount;
            return true;
        }

        // The frame of the last snap that started a reveal: a cue's sound rule waits for the click too (Run).
        public static int SnappedFrame { get; private set; } = -1;

        // Snap, wait for the click when he snapped, then reveal: StartCoroutine(SnapThen(() => ...)).
        public IEnumerator SnapThen(System.Action reveal)
        {
            if (SnapReveal()) yield return Wait(GuideCalls.SnapLead);
            reveal?.Invoke();
        }
    }

    // The lesson panel's calls into Wattson (WP-B): his snap and WattsonGuide's pointing, in one place.
    static class GuideCalls
    {
        public static float SnapLead => WattsonController.SnapLead;
        public static bool Snap() { var wattson = WattsonController.Active; return wattson && wattson.Snap(); }
        public static void Point(RectTransform target, WattsonGuide.Mark mark, Vector2 pivot = default) { if (target) WattsonGuide.Point(target, pivot, null, mark); }
        public static void Release() => WattsonGuide.Release();
    }
}
