using System;
using UnityEngine;
namespace AdaptiveOhmsLaw
{
    public class QuestionManager : MonoBehaviour
    {
        public QuestionDefinition Definition { get; private set; }
        public QuestionRecord Record { get; private set; }
        public bool AcceptingAnswer { get; private set; }
        public event Action<bool> Answered;
        public event Action Completed;
        AdaptiveLearningManager adaptive;
        StudentProfileManager profiles;
        float started, attemptStarted, pauseStarted, pausedDuration;
        bool paused;
        bool scoreAnswers = true;
        public void Configure(AdaptiveLearningManager learning, StudentProfileManager profile) { adaptive = learning; profiles = profile; }
        public void Begin(QuestionDefinition question, bool awardMastery = true)
        {
            scoreAnswers = awardMastery;
            Definition = question;
            Record = new QuestionRecord { questionId = question.questionId, startedUtc = DateTime.UtcNow.ToString("O") };
            profiles.Profile.questions.Add(Record); profiles.Profile.questionsAttempted++;
            started = attemptStarted = Time.realtimeSinceStartup; pausedDuration = 0; paused = false;
            AcceptingAnswer = true; adaptive.SupportFor(question); profiles.Save();
        }
        public void SetPaused(bool value)
        {
            if (paused == value || Record == null || Record.completed) return;
            if (value) pauseStarted = Time.realtimeSinceStartup;
            else { float duration = Time.realtimeSinceStartup - pauseStarted; pausedDuration += duration; attemptStarted += duration; }
            paused = value;
        }
        public void Submit(int index)
        {
            if (!AcceptingAnswer || paused || index < 0 || index >= Definition.answers.Length) return;
            AcceptingAnswer = false;
            bool correct = index == Definition.correctAnswer;
            float elapsed = Time.realtimeSinceStartup - attemptStarted;
            if (Record.firstAnswer < 0) Record.firstAnswer = index;
            Record.finalAnswer = index;
            Record.attempts.Add(new AnswerAttempt { answer = index, correct = correct, responseSeconds = elapsed });
            Record.responseSeconds = Time.realtimeSinceStartup - started - pausedDuration;
            profiles.Profile.responseSeconds += elapsed;
            if (scoreAnswers) adaptive.RecordAnswer(Definition, Record, correct);
            else
            {
                // Practice still has an accurate attempt history; only mastery rewards are disabled.
                if (!correct) profiles.Profile.incorrectAttempts++;
                if (correct && Record.attempts.Count == 1) { Record.correctFirstAttempt = true; profiles.Profile.correctFirstAttempts++; }
                adaptive.SupportFor(Definition, Record.attempts.FindAll(a => !a.correct).Count);
                profiles.Save();
            }
            Answered?.Invoke(correct);
        }
        public void Retry()
        {
            if (Record == null || Record.completed || AcceptingAnswer || Record.finalAnswer == Definition.correctAnswer) return;
            attemptStarted = Time.realtimeSinceStartup; AcceptingAnswer = true;
        }
        public void Finish(string activity)
        {
            if (Record == null || Record.completed || Record.finalAnswer < 0) return;
            AcceptingAnswer = false; Record.completed = true;
            Record.responseSeconds = Time.realtimeSinceStartup - started - pausedDuration;
            if (!profiles.Profile.completedActivities.Contains(activity)) profiles.Profile.completedActivities.Add(activity);
            profiles.Save(); Completed?.Invoke();
        }
    }
}

