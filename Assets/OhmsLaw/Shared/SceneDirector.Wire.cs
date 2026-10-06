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
                default: return false;
            }
        }

        void Tick()
        {
            Play("Count", 1f + 0.04f * (wire ? wire.Tally % 4 : 0));
            if (counterView) counterView.Hit();
        }
    }
}
