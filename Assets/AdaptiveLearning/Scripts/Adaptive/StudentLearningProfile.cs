using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveOhmsLaw
{
    public enum ConceptId { Voltage, Current, Ammeter, Voltmeter, SeriesConnection, ParallelConnection, VoltageCurrentRelationship, Resistance, GraphInterpretation, OhmsLaw }
    public enum SupportLevel { Independent = 1, Guided = 2, Supported = 3 }
    [Serializable] public class ConceptMastery { public ConceptId concept; public float mastery = 50; }
    [Serializable] public class AnswerAttempt { public int answer; public float responseSeconds; public bool correct; }
    [Serializable] public class QuestionRecord
    {
        public string questionId, startedUtc;
        public int firstAnswer = -1, finalAnswer = -1, hintCount;
        public float responseSeconds;
        public bool completed, correctFirstAttempt, usedExplanation;
        public List<AnswerAttempt> attempts = new List<AnswerAttempt>();
    }
    [Serializable] public class ExperimentObservation
    {
        public float voltage;
        public float current;
        public string recordedUtc;
    }
    [Serializable] public class ExperimentRecord
    {
        public string activityId = "Scene07OhmsExperiment";
        public string sessionId;
        public string startedUtc, completedUtc;
        public float fixedResistance = 10f;
        public List<ExperimentObservation> observations = new List<ExperimentObservation>();
        public SupportLevel supportLevel = SupportLevel.Independent;
        public int hintCount;
        public bool usedExplanation;
        public string conclusionOutcome = "";
        public bool completed;
    }
    [Serializable] public class GraphActivityRecord
    {
        public string activityId = "Scene08Graph", sessionId, sourceSessionId, startedUtc, completedUtc;
        public bool sampleData, completed, usedExplanation;
        public List<ExperimentObservation> observations = new List<ExperimentObservation>();
        public List<int> plottedIndices = new List<int>();
        public int plottingMistakes, hints;
        public string conclusionOutcome;
    }
    [Serializable] public class Scene12ActivityRecord
    {
        public string activityId = "Scene12DiscoverResistance", sessionId, startedUtc, completedUtc;
        public bool sampleData, completed, usedExplanation;
        public int ratioMistakes, hints, slopeMistakes;
        public int selectedPoint1 = -1, selectedPoint2 = -1;
        public float calculatedSlope = 5f;
        public string questionOutcome;
        public SupportLevel supportLevel = SupportLevel.Independent;
    }
    [Serializable] public class Scene13ActivityRecord
    {
        public string activityId = "Scene13OhmsLaw", sessionId, startedUtc, completedUtc;
        public bool completed, usedExplanation;
        public int equationMistakes, hints, challengeMistakes;
        public float challengeSetVoltage;
        public SupportLevel supportLevel = SupportLevel.Independent;
    }
    [Serializable] public class StudentLearningProfile
    {
        public int version = 1;
        public List<CircuitAssemblyRecord> circuitAssemblies = new List<CircuitAssemblyRecord>();
        public List<ConceptMastery> concepts = new List<ConceptMastery>();
        public List<QuestionRecord> questions = new List<QuestionRecord>();
        public List<ExperimentRecord> experiments = new List<ExperimentRecord>();
        public List<GraphActivityRecord> graphs = new List<GraphActivityRecord>();
        public List<Scene12ActivityRecord> scene12Activities = new List<Scene12ActivityRecord>();
        public List<Scene13ActivityRecord> scene13Activities = new List<Scene13ActivityRecord>();
        public List<string> completedActivities = new List<string>();
        public int questionsAttempted, correctFirstAttempts, incorrectAttempts, hintCount;
        public float responseSeconds;
        public SupportLevel supportLevel = SupportLevel.Independent;
        public void EnsureConcepts(float initial)
        {
            concepts ??= new List<ConceptMastery>(); questions ??= new List<QuestionRecord>();
            experiments ??= new List<ExperimentRecord>();
            graphs ??= new List<GraphActivityRecord>();
            scene12Activities ??= new List<Scene12ActivityRecord>();
            scene13Activities ??= new List<Scene13ActivityRecord>();
            foreach (var experiment in experiments) experiment.observations ??= new List<ExperimentObservation>();
            foreach (var graph in graphs) { graph.observations ??= new List<ExperimentObservation>(); graph.plottedIndices ??= new List<int>(); }
            version = 3;
            completedActivities ??= new List<string>();
            foreach (ConceptId id in Enum.GetValues(typeof(ConceptId)))
                if (!concepts.Exists(c => c.concept == id)) concepts.Add(new ConceptMastery { concept = id, mastery = initial });
            foreach (var c in concepts) c.mastery = Mathf.Clamp(c.mastery, 0, 100);
            supportLevel = (SupportLevel)Mathf.Clamp((int)supportLevel, 1, 3);
        }
        public float Mastery(ConceptId id) => concepts.Find(c => c.concept == id)?.mastery ?? 50;
    }
}

