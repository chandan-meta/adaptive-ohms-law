using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public enum Scene12Stage { Opening, InspectWire, ConstantRatio, GraphSlope, NameUnit, Completed }

    public class Scene12FlowManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] AdaptiveRules rules;
        [SerializeField] StudentProfileManager profiles;
        [SerializeField] AdaptiveLearningManager adaptive;
        [SerializeField] QuestionManager questions;
        [SerializeField] WattsonController wattson;
        [SerializeField] WattsonChatManager chat;
        [SerializeField] WattsonChatUI chatUI;
        [SerializeField] DialoguePresenter dialogue;
        [SerializeField] Camera learnerCamera;
        [SerializeField] CanvasGroup fade;
        [SerializeField] DialogueSequence lines;

        [Header("Scene 12 Views")]
        [SerializeField] MicroscopicWireView microView;
        [SerializeField] RatioCalculationView ratioView;
        [SerializeField] ResistanceGraphView graphView;
        [SerializeField] QuestionUI questionUI;
        [SerializeField] QuestionDefinition resistanceQuestion;

        [Header("UI Panels & Indicators")]
        [SerializeField] Text supportText;
        [SerializeField] Text progressText;
        [SerializeField] Text hintText;
        [SerializeField] GameObject unitRevealPanel;
        [SerializeField] Text unitRevealText;
        [SerializeField] GameObject completionPanel;
        [SerializeField] Button continueButton;
        [SerializeField] Button inspectWirePromptButton;

        [Header("3D Bench Highlights")]
        [SerializeField] GameObject voltageHighlight;
        [SerializeField] GameObject currentHighlight;
        [SerializeField] GameObject wireHighlight;

        [SerializeField] UnityEvent onScene12Completed = new UnityEvent();
        public event Action OnScene12Completed;

        public Scene12Stage Stage { get; private set; }
        public bool IsSpeaking => dialogue != null && dialogue.IsPresenting;

        bool chatOpen, appPaused, isPracticeData;
        Scene12ActivityRecord record;
        List<ExperimentObservation> measuredObservations;

        void Awake()
        {
            if (profiles == null) profiles = FindFirstObjectByType<StudentProfileManager>();
            if (adaptive == null) adaptive = FindFirstObjectByType<AdaptiveLearningManager>();
            if (questions == null) questions = FindFirstObjectByType<QuestionManager>();
            if (wattson == null) wattson = FindFirstObjectByType<WattsonController>();
            if (chat == null) chat = FindFirstObjectByType<WattsonChatManager>();
            if (chatUI == null) chatUI = FindFirstObjectByType<WattsonChatUI>();
            if (dialogue == null) dialogue = FindFirstObjectByType<DialoguePresenter>();
            if (microView == null) microView = FindFirstObjectByType<MicroscopicWireView>(FindObjectsInactive.Include);
            if (ratioView == null) ratioView = FindFirstObjectByType<RatioCalculationView>(FindObjectsInactive.Include);
            if (graphView == null) graphView = FindFirstObjectByType<ResistanceGraphView>(FindObjectsInactive.Include);
            if (questionUI == null) questionUI = FindFirstObjectByType<QuestionUI>(FindObjectsInactive.Include);
            if (learnerCamera == null) learnerCamera = Camera.main;
            if (fade == null) fade = FindFirstObjectByType<CanvasGroup>();
            if (inspectWirePromptButton == null) inspectWirePromptButton = GameObject.Find("InspectWireButton")?.GetComponent<Button>();
            if (continueButton == null) continueButton = GameObject.Find("FinishSceneBtn")?.GetComponent<Button>();
            if (unitRevealPanel == null) unitRevealPanel = GameObject.Find("UnitRevealPanel");
            if (completionPanel == null) completionPanel = GameObject.Find("CompletionPanel");
#if UNITY_EDITOR
            if (rules == null) rules = UnityEditor.AssetDatabase.LoadAssetAtPath<AdaptiveRules>("Assets/AdaptiveLearning/ScriptableObjects/Concepts/PrototypeRules.asset");
            if (lines == null) lines = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/AdaptiveLearning/ScriptableObjects/Dialogue/Scene12Dialogue.asset");
            if (resistanceQuestion == null) resistanceQuestion = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestionDefinition>("Assets/AdaptiveLearning/ScriptableObjects/Questions/Scene12ResistanceQuestion.asset");
#endif
        }

        void Start()
        {
            if (rules == null || profiles == null)
            {
                Debug.LogError("Scene 12 references are missing.");
                return;
            }

            profiles.Initialize(rules.initialMastery);
            adaptive.Configure(rules, profiles);
            questions.Configure(adaptive, profiles);
            wattson.Bind(learnerCamera.transform, dialogue);

            // Check data provenance
            var exp = profiles.Profile.experiments.FindLast(e => e.completed && LessonExperimentData.IsValid(e.observations) && Mathf.Abs(e.fixedResistance - LessonExperimentData.Resistance) < .01f);
            isPracticeData = (exp == null);
            measuredObservations = exp != null ? new List<ExperimentObservation>(exp.observations) : null;

            record = new Scene12ActivityRecord
            {
                sessionId = Guid.NewGuid().ToString("N"),
                startedUtc = DateTime.UtcNow.ToString("o"),
                sampleData = isPracticeData
            };
            profiles.Profile.scene12Activities.Add(record);

            chat.Configure(() => new LearningContext
            {
                scene = gameObject.scene.name,
                activity = "Scene12DiscoverResistance",
                step = Stage.ToString(),
                concept = ConceptId.Resistance,
                mastery = profiles.Profile.Mastery(ConceptId.Resistance),
                support = profiles.Profile.supportLevel,
                usingPracticeData = isPracticeData,
                circuitResistance = 5.0f
            });

            chat.ExplanationRequested += OnChatExplanation;
            if (chatUI != null)
            {
                chatUI.Bind(chat);
                chatUI.VisibilityChanged += PauseForChat;
            }

            if (questionUI != null)
            {
                questionUI.Selected += questions.Submit;
                questionUI.RetryRequested += OnQuestionRetry;
                questionUI.KeepRequested += OnQuestionKeep;
            }
            questions.Answered += OnResistanceQuestionAnswered;
            profiles.Changed += UpdateStatus;

            if (microView != null)
            {
                microView.InspectionCompleted += OnMicroInspectionDone;
                microView.CloseRequested += OnMicroInspectionDone;
                microView.gameObject.SetActive(false);
            }

            if (ratioView != null)
            {
                ratioView.CalculationAnswered += (c) => { if (!c) record.ratioMistakes++; };
                ratioView.RatioObservationAnswered += (c) => { if (!c) record.ratioMistakes++; };
                ratioView.RatioStageCompleted += OnRatioStageDone;
                ratioView.gameObject.SetActive(false);
            }

            if (graphView != null)
            {
                graphView.SlopeDetermined += OnGraphSlopeDetermined;
                graphView.ContinueClicked += OnGraphStageDone;
                graphView.gameObject.SetActive(false);
            }

            if (continueButton != null)
                continueButton.onClick.AddListener(CompleteScene12);

            if (inspectWirePromptButton != null)
            {
                inspectWirePromptButton.onClick.AddListener(OpenMicroscopicView);
                inspectWirePromptButton.gameObject.SetActive(false);
            }

            if (completionPanel != null) completionPanel.SetActive(false);
            if (unitRevealPanel != null) unitRevealPanel.SetActive(false);
            if (questionUI != null) questionUI.gameObject.SetActive(false);
            SetBenchHighlights(false, false, false);

            UpdateStatus();
            StartCoroutine(OpeningFlow());
        }

        void SetBenchHighlights(bool v, bool i, bool r)
        {
            if (voltageHighlight != null) voltageHighlight.SetActive(v);
            if (currentHighlight != null) currentHighlight.SetActive(i);
            if (wireHighlight != null) wireHighlight.SetActive(r);
        }

        IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds;)
            {
                if (!chatOpen && !appPaused) t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator Say(string lineId)
        {
            var line = lines != null ? lines.Get(lineId) : null;
            if (line != null)
            {
                wattson.Speak(line);
                while (dialogue.IsPresenting) yield return null;
            }
            else
            {
                yield return Wait(1.5f);
            }
        }

        IEnumerator OpeningFlow()
        {
            Stage = Scene12Stage.Opening;
            if (fade != null)
            {
                fade.alpha = 1f;
                fade.blocksRaycasts = true;
                for (float t = 0; t < 1f; t += Time.deltaTime)
                {
                    fade.alpha = 1f - t;
                    yield return null;
                }
                fade.alpha = 0f;
                fade.blocksRaycasts = false;
            }

            wattson.FaceLearner();
            SetBenchHighlights(true, false, false);
            yield return Say("intro_voltage");

            SetBenchHighlights(false, true, false);
            yield return Say("intro_current");

            SetBenchHighlights(false, false, true);
            yield return Say("intro_why");

            // Prompt learner to select wire XY
            if (inspectWirePromptButton != null)
                inspectWirePromptButton.gameObject.SetActive(true);

            if (hintText != null)
                hintText.text = "Tap INSPECT WIRE XY to look inside its microscopic structure.";
        }

        public void OpenMicroscopicView()
        {
            if (chatOpen) return;
            if (inspectWirePromptButton != null)
                inspectWirePromptButton.gameObject.SetActive(false);

            Stage = Scene12Stage.InspectWire;
            SetBenchHighlights(false, false, true);
            if (microView != null)
            {
                microView.gameObject.SetActive(true);
                microView.Initialize();
            }

            StartCoroutine(InsideWireDialogue());
        }

        IEnumerator InsideWireDialogue()
        {
            yield return Say("inspect_prompt");
        }

        void OnMicroInspectionDone()
        {
            if (microView != null)
                microView.gameObject.SetActive(false);

            StartCoroutine(AfterMicroscopicFlow());
        }

        IEnumerator AfterMicroscopicFlow()
        {
            SetBenchHighlights(false, false, false);
            yield return Say("wire_opposes");
            yield return Say("wire_resistance_name");

            // Move to Part B: Constant Ratio
            Stage = Scene12Stage.ConstantRatio;
            yield return Say("table_intro");

            if (ratioView != null)
            {
                ratioView.gameObject.SetActive(true);
                  ratioView.Initialize(measuredObservations, isPracticeData);
            }
        }

        void OnRatioStageDone()
        {
            StartCoroutine(AfterRatioFlow());
        }

        IEnumerator AfterRatioFlow()
        {
            yield return Say("table_constant");

            if (ratioView != null)
                ratioView.gameObject.SetActive(false);

            // Move to Part C: Graph Slope
            Stage = Scene12Stage.GraphSlope;
            yield return Say("graph_intro");

            if (graphView != null)
            {
                graphView.gameObject.SetActive(true);
                  graphView.Initialize(measuredObservations);
            }
        }

        void OnGraphSlopeDetermined(float slope)
        {
            record.calculatedSlope = slope;
            StartCoroutine(Say("graph_slope_explain"));
        }

        void OnGraphStageDone()
        {
            if (graphView != null)
                graphView.gameObject.SetActive(false);

            StartCoroutine(PartDNameUnitFlow());
        }

        IEnumerator PartDNameUnitFlow()
        {
            Stage = Scene12Stage.NameUnit;
            if (unitRevealPanel != null)
            {
                unitRevealPanel.SetActive(true);
                if (unitRevealText != null)
                    unitRevealText.text = "OHM  (Ω)\n1 Ω  =  1 V  /  1 A";
            }

            yield return Say("unit_reveal");

            // Adaptive check question
            if (resistanceQuestion != null && questionUI != null)
            {
                bool alreadyAssessed = profiles.Profile.scene12Activities.Exists(activity =>
                    !activity.sampleData && (activity.completed || activity.questionOutcome == "Correct"));
                questions.Begin(resistanceQuestion, !isPracticeData && !alreadyAssessed);
                questionUI.Show(resistanceQuestion);
                yield return Say("adaptive_question_prompt");
            }
        }

        void OnResistanceQuestionAnswered(bool correct)
        {
            if (correct)
            {
                record.questionOutcome = "Correct";
                if (questionUI != null) questionUI.gameObject.SetActive(false);
                StartCoroutine(ConcludeScene12());
            }
            else
            {
                record.hints++;
                adaptive.RecordHint(questions.Record);
                if (questionUI != null) questionUI.ShowDecision();
            }
        }

        void OnQuestionRetry()
        {
            questions.Retry();
            if (questionUI != null) questionUI.ShowAnswers();
        }

        void OnQuestionKeep()
        {
            if (questionUI != null) questionUI.gameObject.SetActive(false);
            record.questionOutcome = "Guided";
            StartCoroutine(ConcludeScene12());
        }

        IEnumerator ConcludeScene12()
        {
            Stage = Scene12Stage.Completed;
            yield return Say("scene12_complete");

            if (completionPanel != null)
                completionPanel.SetActive(true);

            if (hintText != null)
                hintText.text = "Resistance discovered: R = V / I = 5.0 Ω. Tap CONTINUE to assemble Ohm's Law.";
        }

        public void CompleteScene12()
        {
            if (Stage != Scene12Stage.Completed || chatOpen) return;

            questions.Finish("Scene12DiscoverResistance");
            record.completed = true;
            record.completedUtc = DateTime.UtcNow.ToString("o");
            if (!profiles.Profile.completedActivities.Contains("Scene12DiscoverResistance"))
                profiles.Profile.completedActivities.Add("Scene12DiscoverResistance");

            profiles.Save();
            if (continueButton != null) continueButton.interactable = false;

            StartCoroutine(HandoffToScene13());
        }

        IEnumerator HandoffToScene13()
        {
            yield return Say("final_handoff");
            OnScene12Completed?.Invoke();
            onScene12Completed.Invoke();
        }

        void OnChatExplanation()
        {
            record.usedExplanation = true;
            adaptive.RecordExplanation(questions.Record);
        }

        void PauseForChat(bool open)
        {
            chatOpen = open;
            if (questions != null) questions.SetPaused(open || appPaused);
        }

        void OnApplicationPause(bool paused)
        {
            appPaused = paused;
            if (questions != null) questions.SetPaused(paused || chatOpen);
        }

        void UpdateStatus()
        {
            if (profiles == null || profiles.Profile == null) return;
            if (supportText != null)
                supportText.text = "WATTSON / " + profiles.Profile.supportLevel.ToString().ToUpperInvariant();
            if (progressText != null)
                progressText.text = profiles.SaveError == null ? "PROGRESS SAVED LOCALLY" : "SAVE UNAVAILABLE • IN MEMORY";
        }

        void OnDestroy()
        {
            if (profiles != null) profiles.Changed -= UpdateStatus;
            if (questions != null) questions.Answered -= OnResistanceQuestionAnswered;
            if (chat != null) chat.ExplanationRequested -= OnChatExplanation;
            if (chatUI != null) chatUI.VisibilityChanged -= PauseForChat;
        }
    }
}
