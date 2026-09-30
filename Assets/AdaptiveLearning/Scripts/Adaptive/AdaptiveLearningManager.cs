using UnityEngine;
namespace AdaptiveOhmsLaw
{
    public class AdaptiveLearningManager : MonoBehaviour
    {
        [SerializeField] AdaptiveRules rules;
        [SerializeField] StudentProfileManager profiles;
        public StudentLearningProfile Profile => profiles.Profile;
        public AdaptiveRules Rules => rules;
        public void Configure(AdaptiveRules value, StudentProfileManager profile) { rules = value; profiles = profile; }
        public SupportLevel SupportFor(QuestionDefinition question, int failures = 0)
        {
            float mastery = 100;
            foreach (var c in question.concepts) mastery = Mathf.Min(mastery, Profile.Mastery(c));
            var level = failures >= rules.supportedAfterFailures || mastery < rules.supportedBelowMastery ? SupportLevel.Supported
                : failures > 0 || mastery < rules.guidedBelowMastery ? SupportLevel.Guided : SupportLevel.Independent;
            Profile.supportLevel = level;
            return level;
        }
        public void RecordAnswer(QuestionDefinition question, QuestionRecord record, bool correct)
        {
            float gain = correct ? (record.attempts.Count == 1 && record.hintCount == 0 && !record.usedExplanation
                ? rules.firstAttemptGain : rules.assistedGain) + Mathf.Max(0, question.difficulty - 1) * rules.difficultyBonus : -rules.incorrectLoss;
            foreach (var c in question.concepts)
            {
                var entry = Profile.concepts.Find(x => x.concept == c);
                if (entry != null) entry.mastery = Mathf.Clamp(entry.mastery + gain * question.masteryImpact, 0, 100);
            }
            if (!correct) Profile.incorrectAttempts++;
            if (correct && record.attempts.Count == 1) { record.correctFirstAttempt = true; Profile.correctFirstAttempts++; }
            SupportFor(question, record.attempts.FindAll(a => !a.correct).Count);
            profiles.Save();
        }
        public void RecordHint(QuestionRecord record)
        {
            record.hintCount++; Profile.hintCount++; profiles.Save();
        }
        public void RecordExplanation(QuestionRecord record)
        {
            if (record != null && !record.completed) record.usedExplanation = true;
            // Help never subtracts mastery.
            profiles.Save();
        }
    }
}
