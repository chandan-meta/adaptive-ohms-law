using System.Collections;
using UnityEngine;

namespace OhmsLaw
{
    // Cues for the electron space (Scenes 5, 6). Return true for a verb handled here (see SceneDirector.Cues.cs).
    // Idempotent setters, so replaying or skipping lands on the same state.
    public partial class SceneDirector
    {
        bool WireCue(string verb, string[] words, string rest, float value, bool on)
        {
            switch (verb)
            {
                // "wire.trails 0|1": the tracer electrons' short trails.
                case "wire.trails": if (wire) wire.trailsOn = on; return true;
                // "wire.warp <marker>": Wattson is on the marker at once, settled (under a white fade or a cut).
                case "wire.warp": { var marker = Ref(rest); if (wattson && marker) wattson.Warp(marker.transform); return true; }
                // "wire.ticks 1|0": a soft tick for every charge the counter counts.
                case "wire.ticks": if (wire) { wire.Counted -= Tick; if (on) wire.Counted += Tick; } return true;
                // "wire.dive <seconds>": the dive's entry camera ("Wire Enter", cut to under the flash) keeps moving, easing
                // out onto the "Wire Inside" pose, so the 1 s Timeline blend that follows lands on a camera already there:
                // one continuous push, no second move.
                case "wire.dive": { var from = Ref("Cam Wire Enter"); var to = Ref("Cam Wire Inside"); if (from && to) StartCoroutine(Dive(from.transform, to.transform, value > 0 ? value : 1.8f)); return true; }
                default: return false;
            }
        }

        void Tick()
        {
            Play("Count", 1f + 0.04f * (wire ? wire.Tally % 4 : 0));
            if (counterView) counterView.Hit();
        }

        // ponytail: a REPLAY that lands before the cut finds the entry camera already on the inside pose (no second dive).
        IEnumerator Dive(Transform cam, Transform to, float seconds)
        {
            Vector3 p0 = cam.position;
            Quaternion r0 = cam.rotation;
            for (float t = 0; t < 1f; t += Dt / seconds)
            {
                float e = 1f - (1f - t) * (1f - t) * (1f - t); // fast off the cut, settling
                cam.SetPositionAndRotation(Vector3.Lerp(p0, to.position, e), Quaternion.Slerp(r0, to.rotation, e));
                yield return null;
            }
            cam.SetPositionAndRotation(to.position, to.rotation);
        }
    }
}
