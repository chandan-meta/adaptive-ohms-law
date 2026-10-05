namespace OhmsLaw
{
    // Cues added by the polish areas, each in its own partial file (SceneDirector.Wire/Lab/Experiment.cs) so they can
    // change in parallel. Run() falls through to here for any verb it does not handle; a handler returns true if the
    // verb is its own.
    public partial class SceneDirector
    {
        bool AreaCue(string verb, string[] words, string rest, float value, bool on) =>
            WireCue(verb, words, rest, value, on) || LabCue(verb, words, rest, value, on) || ExperimentCue(verb, words, rest, value, on);
    }
}
