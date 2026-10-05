using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Grade = OhmsLaw.LearnerSession.Grade;

namespace OhmsLaw.Editor
{
    // Pure-data checks of the graded questions (QuestionBank / QuestionRun / TargetRun) and the LearnerSession record they
    // write: grading, retry limits, Struggling / Confident, Record on every answer. No scene needed. One PASS / FAIL line per
    // check in the Console (and Logs/ohm-build.txt when run through Tools/unity_request.sh).
    public static class AdaptiveQuestionsTest
    {
        static int passed, failed;

        [MenuItem("Ohm's Law/Tests/Adaptive Questions")]
        public static void Run()
        {
            passed = failed = 0;
            var savedConcepts = new Dictionary<string, LearnerSession.ConceptRecord>(LearnerSession.concepts);
            var savedAnswers = new List<string>(LearnerSession.answers);
            string savedPrediction = LearnerSession.prediction, savedPrediction2V = LearnerSession.s10Prediction2V;
            try
            {
                Bank();
                CorrectFirstTime();
                WrongThenPartial();
                ThreeOptionsRevealAfterTwoWrong();
                RetryLimitOnFourOptions();
                SimplifyKeepsRetryCount();
                StrugglingAndConfident();
                Prediction();
                Target();
                SayGateConditions();
            }
            catch (System.Exception e) { Check("no exception", false, e.ToString()); }
            finally
            {
                LearnerSession.concepts.Clear();
                foreach (var pair in savedConcepts) LearnerSession.concepts[pair.Key] = pair.Value;
                LearnerSession.answers.Clear();
                LearnerSession.answers.AddRange(savedAnswers);
                LearnerSession.prediction = savedPrediction;
                LearnerSession.s10Prediction2V = savedPrediction2V;
            }
            string summary = $"Adaptive Questions: {passed} passed, {failed} failed";
            if (failed == 0) Debug.Log("PASS " + summary); else Debug.LogError("FAIL " + summary);
        }

        static void Check(string name, bool ok, string detail = "")
        {
            if (ok) { passed++; Debug.Log("PASS " + name); }
            else { failed++; Debug.LogError("FAIL " + name + (detail != "" ? " — " + detail : "")); }
        }

        static void Fresh() { LearnerSession.concepts.Clear(); LearnerSession.answers.Clear(); }

        static LearnerSession.ConceptRecord Rec(string concept) => LearnerSession.Concept(concept);

        static void Bank()
        {
            foreach (var q in QuestionBank.All)
            {
                bool shape = q.options.Length >= 2 && !string.IsNullOrEmpty(q.concept) && !string.IsNullOrEmpty(q.text);
                int correct = 0;
                bool tagged = true;
                foreach (var o in q.options)
                {
                    if (o.grade == Grade.Correct) correct++;
                    else tagged &= !string.IsNullOrEmpty(o.misconception);
                }
                Check($"bank {q.id} '{q.text}': correct option first (autoAnswer), exactly one, wrong options tagged",
                    shape && q.options[0].grade == Grade.Correct && correct == 1 && tagged && q.CorrectIndex == 0);
            }
        }

        static void CorrectFirstTime()
        {
            Fresh();
            var run = new QuestionRun(QuestionBank.S05Drift);
            var step = run.Answer(0);
            Check("correct answer ends the question", step.grade == Grade.Correct && !step.retry && !step.reveal);
            Check("correct answer recorded", Rec(Concepts.ElectronDrift).correct == 1 && Rec(Concepts.ElectronDrift).Asked == 1);
            Check("answer logged", LearnerSession.answers.Count == 1 && LearnerSession.answers[0] == "S05_Drift A Correct", string.Join(",", LearnerSession.answers));
        }

        static void WrongThenPartial()
        {
            Fresh();
            var run = new QuestionRun(QuestionBank.S05Drift);
            var first = run.Answer(1);
            Check("incorrect answer: hint + retry", first.grade == Grade.Incorrect && first.retry && !first.reveal && run.Retries == 1);
            Check("misconception tagged", Rec(Concepts.ElectronDrift).misconceptions.Contains("electrons flow from + to −"));
            var second = run.Answer(2);
            Check("partial answer acknowledged and ends the question", second.grade == Grade.Partial && !second.retry && !second.reveal);
            var r = Rec(Concepts.ElectronDrift);
            Check("both answers recorded", r.incorrect == 1 && r.partial == 1 && r.correct == 0 && LearnerSession.answers.Count == 2);
        }

        static void ThreeOptionsRevealAfterTwoWrong()
        {
            Fresh();
            var run = new QuestionRun(QuestionBank.S12Ratio);
            var a = run.Answer(1);
            var b = run.Answer(2);
            Check("3 options: second wrong reveals (only the right one left)", a.retry && !b.retry && b.reveal && run.Retries == 1 && run.Answers == 2);
            Check("wrong answers recorded with their misconceptions", Rec(Concepts.Resistance).incorrect == 2 &&
                Rec(Concepts.Resistance).misconceptions.Contains("divides I by V") && Rec(Concepts.Resistance).misconceptions.Contains("multiplies V by I"));
        }

        static void RetryLimitOnFourOptions()
        {
            Fresh();
            var q = new Question
            {
                id = "Test_Four", concept = "test concept", text = "Four options",
                options = new[] { Question.Right("A"), Question.Wrong("B", "b"), Question.Wrong("C", "c"), Question.Wrong("D", "d") },
            };
            var run = new QuestionRun(q);
            var s1 = run.Answer(1);
            var s2 = run.Answer(2);
            var s3 = run.Answer(3);
            Check($"retries stop at {QuestionRun.MaxRetries}, then the answer is shown", s1.retry && s2.retry && !s3.retry && s3.reveal && run.Retries == QuestionRun.MaxRetries);
            Check("every answer recorded", Rec("test concept").incorrect == 3 && LearnerSession.answers.Count == 3);
        }

        static void SimplifyKeepsRetryCount()
        {
            Fresh();
            var run = new QuestionRun(QuestionBank.AQ1);
            var s1 = run.Answer(1);
            run.Simplify(QuestionBank.AQEasy);
            var s2 = run.Answer(1);
            var s3 = run.Answer(2);
            Check("AQ1 -> AQ_EASY: one retry each, then the answer is shown", s1.retry && s2.retry && s3.reveal && run.Retries == 2 && run.Question == QuestionBank.AQEasy);
            Check("simpler question records to the same concept", Rec(Concepts.Relationship).incorrect == 3);
        }

        static void StrugglingAndConfident()
        {
            Fresh();
            Check("no answers: neither struggling nor confident", !LearnerSession.Struggling(Concepts.Relationship) && !LearnerSession.Confident(Concepts.Relationship));
            new QuestionRun(QuestionBank.S10Pattern).Answer(1);
            Check("one miss: not struggling yet", !LearnerSession.Struggling(Concepts.Relationship));
            new QuestionRun(QuestionBank.S10Pattern).Answer(2);
            Check("two misses, no right answer: struggling", LearnerSession.Struggling(Concepts.Relationship) && !LearnerSession.Confident(Concepts.Relationship));

            Fresh();
            new QuestionRun(QuestionBank.S10Pattern).Answer(0);
            Check("right first time: confident", LearnerSession.Confident(Concepts.Relationship) && !LearnerSession.Struggling(Concepts.Relationship));
            new QuestionRun(QuestionBank.AQ1).Answer(1);
            Check("one miss after: no longer confident", !LearnerSession.Confident(Concepts.Relationship));
        }

        static void Prediction()
        {
            Fresh();
            var a = QuestionBank.GradePrediction("A", out var none);
            var b = QuestionBank.GradePrediction("B", out var flat);
            var c = QuestionBank.GradePrediction("C", out var inverse);
            var missing = QuestionBank.GradePrediction("", out _);
            Check("S10_Predict2V graded by the meter: 0.40 A right, 0.20 A / 0.10 A wrong with their misconceptions",
                a == Grade.Correct && string.IsNullOrEmpty(none) && b == Grade.Incorrect && flat == "current does not depend on voltage" &&
                c == Grade.Incorrect && inverse == "more voltage, less current" && missing == Grade.Incorrect);
        }

        static void Target()
        {
            Fresh();
            var run = new TargetRun("S11_Target", Concepts.Graph, 0.60f);
            Check("target 0.60 A on the 5 Ω wire needs 3 V", Mathf.Approximately(run.TargetVolts, 3f), run.TargetVolts.ToString());
            var miss1 = run.Judge(2f, 0.40f);
            var miss2 = run.Judge(4f, 0.80f);
            Check("target: first miss hints, second miss: Wattson shows it", miss1 == TargetRun.Outcome.Hint && miss2 == TargetRun.Outcome.Show && run.Misses == TargetRun.MaxMisses);
            var hit = new TargetRun("S11_Target", Concepts.Graph, 0.60f).Judge(3f, 0.60f);
            var r = Rec(Concepts.Graph);
            Check("target: a hit is correct; every attempt recorded", hit == TargetRun.Outcome.Hit && r.incorrect == 2 && r.correct == 1 && LearnerSession.answers.Count == 3);
            Check("target: two misses and one hit leave the learner struggling with the graph (fewer right than wrong)", LearnerSession.Struggling(Concepts.Graph));
            Fresh();
            new TargetRun("S11_Target", Concepts.Graph, 0.60f).Judge(3f, 0.60f);
            Check("target: hit first time makes the learner confident (S11 bonus target)", LearnerSession.Confident(Concepts.Graph));
        }

        // Say gates pick a branch line with LearnerSession.Holds (the part of a line id after "_If_").
        static void SayGateConditions()
        {
            Fresh();
            LearnerSession.prediction = "";
            LearnerSession.s10Prediction2V = "";
            Check("say gate: nothing recorded, no condition holds",
                !LearnerSession.Holds("S05_Drift_B") && !LearnerSession.Holds("Struggling_ElectronDrift") && !LearnerSession.Holds("") && !LearnerSession.Holds("S05_Drift_"));

            var drift = new QuestionRun(QuestionBank.S05Drift);
            drift.Answer(1); // B: electrons flow + to −
            drift.Answer(0); // then right
            Check("say gate: a wrong pick counts after it was corrected (S05_Drift_B), other letters do not",
                LearnerSession.Holds("S05_Drift_B") && LearnerSession.Holds("S05_Drift_A") && !LearnerSession.Holds("S05_Drift_C"),
                string.Join(",", LearnerSession.answers));
            Check("say gate: a letter only counts at its own gate", !LearnerSession.Holds("S12_Ratio_B") && !LearnerSession.Holds("S05_B"));

            // The gate itself: which line it speaks, and which gates count as say gates.
            var made = new List<Object>();
            DialogueLine Made(string id) { var l = ScriptableObject.CreateInstance<DialogueLine>(); l.id = id; made.Add(l); return l; }
            InteractionGate Gate(string id, params DialogueLine[] lines) { var g = ScriptableObject.CreateInstance<InteractionGate>(); g.gateId = id; g.lines = lines; made.Add(g); return g; }
            try
            {
                var plain = Made("S09_Conventional");
                var fix = Made("S09_Conventional_If_S05_Drift_B");
                var other = Made("S09_Conventional_If_S05_Drift_C");
                var say = Gate("S09_Conventional", other, fix, plain);
                Check("say gate: recognised; the first _If_ line that holds wins, in the gate's order",
                    SceneDirector.IsSayGate(say) && SceneDirector.SayGateLine(say) == fix);
                Check("say gate: no _If_ line holds -> the plain line; none listed -> nothing",
                    SceneDirector.SayGateLine(Gate("S09_Conventional", other, plain)) == plain && SceneDirector.SayGateLine(Gate("S09_Conventional", other)) == null);
                Check("say gate: a gate listing a line of another id is not a say gate (unknown gates still warn)",
                    !SceneDirector.IsSayGate(Gate("S09_Typo", plain)) && !SceneDirector.IsSayGate(Gate("S09_Conventional")) && !SceneDirector.IsSayGate(Gate("S09_Conventional", Made("S09_L01"), plain)));
            }
            finally { foreach (var o in made) Object.DestroyImmediate(o); }

            LearnerSession.prediction = "C";
            LearnerSession.s10Prediction2V = "B";
            Check("say gate: predictions count (S06_Prediction_C, S10_Predict2V_B before it is graded)",
                LearnerSession.Holds("S06_Prediction_C") && !LearnerSession.Holds("S06_Prediction_A") && LearnerSession.Holds("S10_Predict2V_B") && !LearnerSession.Holds("S10_Predict2V_A"));

            new QuestionRun(QuestionBank.S12Ratio).Answer(1);
            new QuestionRun(QuestionBank.S12Ratio).Answer(2);
            Check("say gate: Struggling_/Confident_ by Concepts field name",
                LearnerSession.Holds("Struggling_Resistance") && !LearnerSession.Holds("Confident_Resistance") &&
                !LearnerSession.Holds("Struggling_ElectronDrift") && !LearnerSession.Holds("Struggling_NoSuchConcept"));
            Check("Concepts.Named maps every field name", Concepts.Named("ElectronDrift") == Concepts.ElectronDrift && Concepts.Named("OhmsLaw") == Concepts.OhmsLaw &&
                Concepts.Named("Relationship") == Concepts.Relationship && Concepts.Named("Graph") == Concepts.Graph && Concepts.Named("nope") == null);
        }
    }
}
