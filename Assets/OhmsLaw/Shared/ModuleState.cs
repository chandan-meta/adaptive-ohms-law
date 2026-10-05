using System.Collections.Generic;
using UnityEngine;

namespace OhmsLaw
{
    // Reviewer switches. Static, so they last for the whole play session and reset when Play mode restarts.
    public static class ReviewMode
    {
        // Gates answer themselves: correct option, parts placed, slider set, key closed. The module plays hands-free.
        public static bool autoAnswer;
        public static float autoDelay = 3f;

        static float scale = 1f;
        public static float timeScale
        {
            get => scale;
            set { scale = value; Time.timeScale = value; }
        }
        // skipToScene is ModuleFlow.JumpToScene, reachable from the Module Player and the F1 overlay.
    }

    // What the learner did, for later scenes to read back. Kept for the session only, never written to disk.
    public static class LearnerSession
    {
        public static string prediction = "";        // Scene 6: "A" increase, "B" decrease, "C" stay the same
        public static string s10Prediction2V = "";    // Scene 10 S10_Predict2V letter: A 0.40 A, B 0.20 A, C 0.10 A (graded at S10_Record2)
        public static string s10PatternAnswer = "";   // Scene 10 pattern question, final answer letter
        public static int s10PatternAttempts;
        public static string slopeAnswer = "";        // Scene 12 slope, the option text chosen last
        public static readonly List<string> answers = new List<string>(); // every graded answer in order: "S12_Ratio B Incorrect"
        public static int tableRows;                  // observation table rows recorded so far
        public static bool resistanceTaught;          // set when Scene 12 names R and the ohm (cue readings.r 1): before that, nobody may quote R = 5 Ω

        public static string PredictionText =>
            prediction == "A" ? "Increase" : prediction == "B" ? "Decrease" : prediction == "C" ? "Stay the same" : "";

        public static string Letter(int index) => index < 0 ? "" : ((char)('A' + index)).ToString();

        // Concept-level understanding for this session: every graded answer lands here, the questions adapt on it
        // (Struggling / Confident) and Ask Wattson gets Summary(). Concept ids: Concepts.*.
        public enum Grade { Correct, Partial, Incorrect }

        public class ConceptRecord
        {
            public int correct, partial, incorrect;
            public readonly List<string> misconceptions = new List<string>();
            public int Asked => correct + partial + incorrect;
        }

        public static readonly Dictionary<string, ConceptRecord> concepts = new Dictionary<string, ConceptRecord>();

        public static ConceptRecord Concept(string id)
        {
            if (!concepts.TryGetValue(id, out var record)) concepts[id] = record = new ConceptRecord();
            return record;
        }

        public static void Record(string concept, Grade grade, string misconception = null)
        {
            var record = Concept(concept);
            if (grade == Grade.Correct) record.correct++;
            else if (grade == Grade.Partial) record.partial++;
            else record.incorrect++;
            if (!string.IsNullOrEmpty(misconception) && !record.misconceptions.Contains(misconception)) record.misconceptions.Add(misconception);
        }

        // Two or more misses on a concept with fewer right answers than misses: simplify and guide.
        public static bool Struggling(string concept) => concepts.TryGetValue(concept, out var r) && r.incorrect >= 2 && r.correct < r.incorrect;
        // Right at least once and never wrong: skip the optional follow-up, offer the harder prediction.
        public static bool Confident(string concept) => concepts.TryGetValue(concept, out var r) && r.correct > 0 && r.incorrect == 0;

        // One line per concept for the AI context: "voltage: 2 right, 1 wrong (thinks: ...)".
        public static string Summary()
        {
            var lines = new List<string>();
            foreach (var pair in concepts)
            {
                var r = pair.Value;
                string line = $"{pair.Key}: {r.correct} right, {r.partial} partly right, {r.incorrect} wrong";
                if (r.misconceptions.Count > 0) line += " (misconceptions: " + string.Join("; ", r.misconceptions) + ")";
                lines.Add(line);
            }
            if (prediction != "") lines.Add("Scene 6 prediction: " + PredictionText);
            if (answers.Count > 0) lines.Add("Recent answers: " + string.Join(", ", answers.GetRange(Mathf.Max(0, answers.Count - 4), Mathf.Min(4, answers.Count))));
            return string.Join("\n", lines);
        }
    }

    // Concept ids for LearnerSession.Record.
    public static class Concepts
    {
        public const string ElectronDrift = "electron drift direction", Voltage = "voltage", Current = "current",
            Relationship = "voltage-current relationship", Graph = "reading the V-I graph", Resistance = "resistance", OhmsLaw = "Ohm's law";
    }

    // Carried across a scene change so the next scene can start on the same frame (match-cuts).
    public static class SceneHandoffState
    {
        public static string fromScene = "";
        public static bool hasCamera;
        public static Vector3 cameraPosition;
        public static Quaternion cameraRotation;
        public static float fieldOfView = 40f;
        public static CircuitState circuit;
        public static bool fieldOn;
        public static float driftSpeed;
        public static Vector3[] cardPositions = new Vector3[0];
        public static int tableRows;
        public static bool graphShown;

        public static void SaveCamera()
        {
            var cam = Camera.main;
            if (!cam) return;
            hasCamera = true;
            cameraPosition = cam.transform.position;
            cameraRotation = cam.transform.rotation;
            fieldOfView = cam.fieldOfView;
        }
    }
}
