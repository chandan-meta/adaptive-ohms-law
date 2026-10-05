using System.Linq;
using UnityEngine;
using Grade = OhmsLaw.LearnerSession.Grade;

namespace OhmsLaw
{
    // A graded multiple-choice question: per-option grade and misconception, the on-panel hint and reveal texts, and the
    // dialogue line ids the gate speaks (the builders declare those lines; Docs/Polish/WOW_PLAN.md "Gate contract").
    // The Correct option is always index 0, so ReviewMode.autoAnswer (which picks 0) plays every question correctly.
    public class Question
    {
        public struct Option
        {
            public string text, misconception;
            public Grade grade;
        }

        public string id, concept, text;   // id: the answer log's tag (the gate id)
        public Option[] options;
        public string hint = "";           // on the panel before a retry
        public string reveal = "";         // on the panel when Wattson gives the answer, or after a partly right one
        public float width = 900f;         // MCQPanel width
        public bool dockRight = true;      // MCQPanel dock: against the right edge, or (false) top-centre
        // Line ids. null: the gate's own scheme (<gate>_Q, <gate>_H); the storyboard AQ items keep their own ids.
        public string questionLine, hintLine, correctFallbackLine;

        public string[] Texts => options.Select(o => o.text).ToArray();
        public int CorrectIndex => System.Array.FindIndex(options, o => o.grade == Grade.Correct);

        public static Option Right(string text) => new Option { text = text, grade = Grade.Correct };
        public static Option Half(string text, string misconception) => new Option { text = text, grade = Grade.Partial, misconception = misconception };
        public static Option Wrong(string text, string misconception) => new Option { text = text, grade = Grade.Incorrect, misconception = misconception };
    }

    // Every graded question in the module (WOW_PLAN §5). Texts are on-screen strings: storyboard ones are word for word.
    public static class QuestionBank
    {
        public static readonly Question S05Drift = new Question
        {
            // 640 px top-centre (x 640-1280): the − disc and label, Wattson, the + badge and its label stay in view either side
            // (docked right it covered the whole + end the question asks about).
            id = "S05_Drift", concept = Concepts.ElectronDrift, width = 640f, dockRight = false,
            text = "Which way are the free electrons drifting now?",
            options = new[]
            {
                Question.Right("A. From the − end toward the + end"),
                Question.Wrong("B. From the + end toward the − end", "electrons flow from + to −"),
                Question.Half("C. Every which way, with no overall direction", "misses the drift under the random jiggle"),
            },
            hint = "Which end attracts negative charge?",
            reveal = "Electrons are negative: they drift from − toward +.",
        };

        // Asked before the 2 V step, graded at S10_Record2 against the meter (no retry: the experiment answers it).
        public static readonly Question S10Predict2V = new Question
        {
            // 700 px docked right (left edge ~1172 px): the voltmeter's readout plate in Bench Close spans ~968-1150 px
            // (slot x 0.12 m, readout 0.27 m up, 2.0 m from BenchCloseCamC at 36°; "0.0 V" plate ~0.22 m wide); 740 covered its edge.
            id = "S10_Predict2V", concept = Concepts.Relationship, width = 700f,
            text = "At 1 V the ammeter read 0.20 A. What will it read at 2 V?",
            options = new[]
            {
                Question.Right("A. 0.40 A"),
                Question.Wrong("B. 0.20 A", "current does not depend on voltage"),
                Question.Wrong("C. 0.10 A", "more voltage, less current"),
            },
        };

        public static readonly Question S10Pattern = new Question
        {
            id = "S10_Pattern", concept = Concepts.Relationship,
            text = "What do you notice?",
            options = new[]
            {
                Question.Right("A. Current increases as potential difference increases."),
                Question.Wrong("B. Current decreases.", "more voltage, less current"),
                Question.Wrong("C. There is no pattern.", "the readings are unrelated"),
            },
            hint = "As V goes up, what does I do?",
            reveal = "As V goes up, I goes up too.",
            hintLine = "S10_R_WRONG", // the storyboard's proposed nudge
        };

        // S10_Pattern's simpler retry for a learner who is struggling with the relationship (rows 1 and 2 highlighted).
        public static readonly Question S10PatternEasy = new Question
        {
            id = "S10_Pattern", concept = Concepts.Relationship,
            text = "Rows 1 and 2: when the potential difference went up, what did the current do?",
            options = new[]
            {
                Question.Right("A. It went up"),
                Question.Wrong("B. It went down", "more voltage, less current"),
                Question.Wrong("C. It stayed the same", "current does not depend on voltage"),
            },
            hint = "Compare 0.2 A and 0.4 A.",
            reveal = "As V goes up, I goes up too.",
            questionLine = "S10_Pattern_E", hintLine = "S10_R_WRONG",
        };

        public static readonly Question S12Ratio = new Question
        {
            id = "S12_Ratio", concept = Concepts.Resistance, width = 760f,
            text = "3.0 ÷ 0.60 = ?",
            options = new[]
            {
                Question.Right("5"),
                Question.Wrong("0.2", "divides I by V"),
                Question.Wrong("1.8", "multiplies V by I"),
            },
            hint = "How many 0.60s make 3.0?",
            reveal = "3.0 ÷ 0.60 = 5",
        };

        public static readonly Question S12Slope = new Question
        {
            id = "S12_Slope", concept = Concepts.Graph, width = 760f,
            text = "Slope = ΔV / ΔI = ?",
            options = new[]
            {
                Question.Right("5"),
                Question.Wrong("0.2", "inverts the slope (ΔI ÷ ΔV)"),
                Question.Wrong("2.5", "mixes a reading with a difference"),
            },
            hint = "2.0 ÷ 0.40",
            reveal = "Slope = ΔV / ΔI = 2.0 ÷ 0.40 = 5",
        };

        // Storyboard AQ1 (moved from the Scene 13 checkpoint to Scene 12's S12_AQ1 gate).
        public static readonly Question AQ1 = new Question
        {
            id = "S12_AQ1", concept = Concepts.Relationship,
            text = "If resistance stays constant and voltage doubles, what happens to current?",
            options = new[]
            {
                Question.Right("A. It doubles"),
                Question.Wrong("B. It halves", "more voltage, less current"),
                Question.Wrong("C. It stays the same", "current does not depend on voltage"),
            },
            hint = "1 V → 0.20 A, 2 V → 0.40 A",
            reveal = "Same R: double V → double I",
            questionLine = "AQ1", hintLine = "AQ_EXPLAIN", correctFallbackLine = "AQ_GOOD",
        };

        // Storyboard AQ_EASY: AQ1's simpler retry.
        public static readonly Question AQEasy = new Question
        {
            id = "S12_AQ1", concept = Concepts.Relationship,
            text = "When the potential difference across the nichrome wire went up, what happened to the current?",
            options = new[]
            {
                Question.Right("A. It went up"),
                Question.Wrong("B. It went down", "more voltage, less current"),
                Question.Wrong("C. Nothing changed", "current does not depend on voltage"),
            },
            hint = "1 V → 0.20 A, 2 V → 0.40 A",
            reveal = "Same R: double V → double I",
            questionLine = "AQ_EASY", hintLine = "AQ_EXPLAIN", correctFallbackLine = "AQ_GOOD",
        };

        // Storyboard AQ2 (moved to Scene 13's S13_AQ2 gate). 640 px keeps V = IR clear in the Cards shot.
        public static readonly Question AQ2 = new Question
        {
            id = "S13_AQ2", concept = Concepts.Resistance, width = 640f,
            text = "Two conductors carry the same current. One requires a larger potential difference. Which has greater resistance?",
            options = new[]
            {
                Question.Right("A. The one that needs the larger potential difference"),
                Question.Wrong("B. The one that needs the smaller potential difference", "a bigger push means less opposition"),
                Question.Wrong("C. Both have the same resistance", "resistance depends only on the current"),
            },
            hint = "R = V ÷ I: same I, so compare V.",
            reveal = "Same I, larger V → larger R",
            questionLine = "AQ2", correctFallbackLine = "AQ_GOOD",
        };

        public static readonly Question[] All = { S05Drift, S10Predict2V, S10Pattern, S10PatternEasy, S12Ratio, S12Slope, AQ1, AQEasy, AQ2 };

        // S10_Predict2V is graded by the experiment: the meter at 2 V against the learner's letter.
        public static Grade GradePrediction(string letter, out string misconception)
        {
            int i = string.IsNullOrEmpty(letter) ? -1 : letter[0] - 'A';
            var option = i >= 0 && i < S10Predict2V.options.Length ? S10Predict2V.options[i] : Question.Wrong("", "");
            misconception = option.misconception;
            return option.grade;
        }
    }

    // One learner's run through a question: grades each pick, records it (LearnerSession.Record), and decides what follows.
    // Correct: done. Partial: acknowledged, the correct option shown, done. Incorrect: hint and retry, at most MaxRetries
    // times and only while two or more options are still untried (a lone remaining option is shown, not asked); then
    // Wattson shows the answer. Simplify() swaps in an easier question for the retries (S10_Pattern, AQ1 -> AQ_EASY).
    public class QuestionRun
    {
        public const int MaxRetries = 2;

        public struct Step
        {
            public Grade grade;
            public string misconception;
            public bool retry, reveal;
        }

        public Question Question { get; private set; }
        public int Retries { get; private set; }
        public int Answers { get; private set; }
        public int LastPick { get; private set; } = -1;
        public Grade Last { get; private set; } = Grade.Incorrect;
        bool[] tried;

        public QuestionRun(Question question) { Question = question; tried = new bool[question.options.Length]; }

        public bool Tried(int option) => option >= 0 && option < tried.Length && tried[option];

        public Step Answer(int pick)
        {
            pick = Mathf.Clamp(pick, 0, Question.options.Length - 1);
            var option = Question.options[pick];
            tried[pick] = true;
            LastPick = pick;
            Answers++;
            Last = option.grade;
            LearnerSession.Record(Question.concept, option.grade, option.misconception);
            LearnerSession.answers.Add($"{Question.id} {LearnerSession.Letter(pick)} {option.grade}");
            var step = new Step { grade = option.grade, misconception = option.misconception };
            if (option.grade != Grade.Incorrect) return step;
            bool canRetry = Retries < MaxRetries && tried.Count(t => !t) >= 2;
            if (canRetry) { Retries++; step.retry = true; }
            else step.reveal = true;
            return step;
        }

        public void Simplify(Question easier)
        {
            Question = easier;
            tried = new bool[easier.options.Length];
        }
    }

    // A target-reading activity (S11_Target, S13_Apply): the learner sets the source so the ammeter reads `targetAmps`.
    // Each settled setting is one answer: a hit is Correct; a miss is Incorrect, gives a hint, and after MaxMisses
    // Wattson sets the target voltage himself and explains.
    public class TargetRun
    {
        public const int MaxMisses = 2;
        public const float Tolerance = 0.005f;
        public enum Outcome { Hit, Hint, Show }

        public readonly string id, concept;
        public readonly float targetAmps;
        public int Misses { get; private set; }

        public TargetRun(string id, string concept, float targetAmps) { this.id = id; this.concept = concept; this.targetAmps = targetAmps; }

        // The source voltage that gives the target on this wire (V = IR).
        public float TargetVolts => targetAmps * ExperimentData.Main.resistance;

        public bool IsHit(float amps) => Mathf.Abs(amps - targetAmps) <= Tolerance;

        public Outcome Judge(float volts, float amps)
        {
            bool hit = IsHit(amps);
            string tried = hit ? null : $"set {volts:0.#} V for a {targetAmps:0.00} A target";
            LearnerSession.Record(concept, hit ? LearnerSession.Grade.Correct : LearnerSession.Grade.Incorrect, tried);
            LearnerSession.answers.Add($"{id} {volts:0.#} V {(hit ? "Correct" : "Incorrect")}");
            if (hit) return Outcome.Hit;
            Misses++;
            return Misses >= MaxMisses ? Outcome.Show : Outcome.Hint;
        }
    }
}
