using UnityEngine;
namespace AdaptiveOhmsLaw
{
    [CreateAssetMenu(menuName = "Adaptive Learning/Scoring rules")]
    public class AdaptiveRules : ScriptableObject
    {
        [Range(0,100)] public float initialMastery = 50;
        public float firstAttemptGain = 10, assistedGain = 4, incorrectLoss = 2, difficultyBonus = 2;
        public int supportedAfterFailures = 2;
        public float guidedBelowMastery = 45, supportedBelowMastery = 30;
    }
}
