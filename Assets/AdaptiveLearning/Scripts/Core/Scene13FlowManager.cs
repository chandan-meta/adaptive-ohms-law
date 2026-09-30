using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public enum Scene13Stage { Opening, AssembleEquation, TryTheLaw, ExplainCondition, Completed }

    public class Scene13FlowManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] AdaptiveRules rules;
        [SerializeField] StudentProfileManager profiles;
        [SerializeField] AdaptiveLearningManager adaptive;
        [SerializeField] WattsonController wattson;
        [SerializeField] WattsonChatManager chat;
        [SerializeField] WattsonChatUI chatUI;
        [SerializeField] DialoguePresenter dialogue;
        [SerializeField] Camera learnerCamera;
        [SerializeField] CanvasGroup fade;
        [SerializeField] DialogueSequence lines;

        [Header("Scene 13 Views")]
        [SerializeField] EquationAssemblyView equationView;
        [SerializeField] BenchCircuitChallenge challengeView;

        [Header("Highlights & Visuals")]
        [SerializeField] GameObject voltageHighlight;
        [SerializeField] GameObject currentHighlight;
        [SerializeField] GameObject wireHighlight;

        [Header("UI Panels")]
        [SerializeField] Text supportText;
        [SerializeField] Text progressText;
        [SerializeField] Text hintText;
        [SerializeField] GameObject conditionCard;
        [SerializeField] Text conditionCardText;
        [SerializeField] GameObject completionPanel;
        [SerializeField] Button replayButton;
        [SerializeField] Button returnToLabButton;
        [SerializeField] Button nextActivityButton;
        [SerializeField] string nextSceneName = ""; // Optional future scene

        [SerializeField] UnityEvent onScene13Completed = new UnityEvent();
        public event Action OnScene13Completed;

        public Scene13Stage Stage { get; private set; }
        public bool IsSpeaking => dialogue != null && dialogue.IsPresenting;

        bool chatOpen, appPaused;
        Scene13ActivityRecord record;

        void Awake()
        {
            if (profiles == null) profiles = FindFirstObjectByType<StudentProfileManager>();
            if (adaptive == null) adaptive = FindFirstObjectByType<AdaptiveLearningManager>();
            if (wattson == null) wattson = FindFirstObjectByType<WattsonController>();
            if (chat == null) chat = FindFirstObjectByType<WattsonChatManager>();
            if (chatUI == null) chatUI = FindFirstObjectByType<WattsonChatUI>();
            if (dialogue == null) dialogue = FindFirstObjectByType<DialoguePresenter>();
            if (equationView == null) equationView = FindFirstObjectByType<EquationAssemblyView>(FindObjectsInactive.Include);
            if (challengeView == null) challengeView = FindFirstObjectByType<BenchCircuitChallenge>(FindObjectsInactive.Include);
            if (learnerCamera == null) learnerCamera = Camera.main;
            if (fade == null) fade = FindFirstObjectByType<CanvasGroup>();
            if (conditionCard == null) conditionCard = GameObject.Find("ConditionCard") ?? System.Array.Find(Resources.FindObjectsOfTypeAll<Transform>(), t => t.name == "ConditionCard")?.gameObject;
            if (completionPanel == null) completionPanel = GameObject.Find("CompletionPanel") ?? System.Array.Find(Resources.FindObjectsOfTypeAll<Transform>(), t => t.name == "CompletionPanel")?.gameObject;
            if (replayButton == null) replayButton = GameObject.Find("ReplayBtn")?.GetComponent<Button>() ?? System.Array.Find(Resources.FindObjectsOfTypeAll<Button>(), b => b.name == "ReplayBtn");
            if (returnToLabButton == null) returnToLabButton = GameObject.Find("ReturnToLabBtn")?.GetComponent<Button>() ?? GameObject.Find("ReturnLabBtn")?.GetComponent<Button>() ?? System.Array.Find(Resources.FindObjectsOfTypeAll<Button>(), b => b.name == "ReturnToLabBtn" || b.name == "ReturnLabBtn");
#if UNITY_EDITOR
            if (rules == null) rules = UnityEditor.AssetDatabase.LoadAssetAtPath<AdaptiveRules>("Assets/AdaptiveLearning/ScriptableObjects/Concepts/PrototypeRules.asset");
            if (lines == null) lines = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/AdaptiveLearning/ScriptableObjects/Dialogue/Scene13Dialogue.asset");
#endif
        }

        void Start()
        {
            if (rules == null || profiles == null)
            {
                Debug.LogError("Scene 13 references are missing.");
                return;
            }

            profiles.Initialize(rules.initialMastery);
            adaptive.Configure(rules, profiles);
            wattson.Bind(learnerCamera.transform, dialogue);

            record = new Scene13ActivityRecord
            {
                sessionId = Guid.NewGuid().ToString("N"),
                startedUtc = DateTime.UtcNow.ToString("o")
            };
            profiles.Profile.scene13Activities.Add(record);

            chat.Configure(() => new LearningContext
            {
                scene = gameObject.scene.name,
                activity = "Scene13OhmsLaw",
                step = Stage.ToString(),
                concept = ConceptId.OhmsLaw,
                mastery = profiles.Profile.Mastery(ConceptId.OhmsLaw),
                support = profiles.Profile.supportLevel,
                circuitResistance = 5.0f,
                sourceVoltage = challengeView != null ? challengeView.CurrentVoltage : 3.0f,
                currentReading = challengeView != null ? challengeView.CurrentAmperage : 0.60f
            });

            chat.ExplanationRequested += OnChatExplanation;
            if (chatUI != null)
            {
                chatUI.Bind(chat);
                chatUI.VisibilityChanged += PauseForChat;
            }
            profiles.Changed += UpdateStatus;

            if (equationView != null)
            {
                equationView.EquationAssembled += OnEquationDone;
                equationView.AttemptEvaluated += (c) => { if (!c) record.equationMistakes++; };
                equationView.gameObject.SetActive(false);
            }

            if (challengeView != null)
            {
                challengeView.ChallengeCompleted += OnChallengeDone;
                challengeView.HintGiven += (h) => record.hints++;
                challengeView.gameObject.SetActive(false);
            }

            if (replayButton != null)
                replayButton.onClick.AddListener(ReplayScene);
            if (returnToLabButton != null)
                returnToLabButton.onClick.AddListener(ReturnToLab);
            if (nextActivityButton != null)
            {
                bool hasNext = !string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName);
                nextActivityButton.gameObject.SetActive(hasNext);
                if (hasNext) nextActivityButton.onClick.AddListener(LoadNextScene);
            }

            if (completionPanel != null) completionPanel.SetActive(false);
            if (conditionCard != null) conditionCard.SetActive(false);
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
            Stage = Scene13Stage.Opening;
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
            yield return Say("intro_three_pieces");

            SetBenchHighlights(true, false, false);
            yield return Say("piece_voltage");

            SetBenchHighlights(false, true, false);
            yield return Say("piece_current");

            SetBenchHighlights(false, false, true);
            yield return Say("piece_resistance");

            SetBenchHighlights(false, false, false);

            // Move to Part A: Assemble Equation
            Stage = Scene13Stage.AssembleEquation;
            if (equationView != null)
            {
                equationView.gameObject.SetActive(true);
                equationView.Initialize();
            }

            yield return Say("assemble_prompt");
        }

        void OnEquationDone()
        {
            StartCoroutine(AfterEquationFlow());
        }

        IEnumerator AfterEquationFlow()
        {
            yield return Say("law_naming");
            yield return Say("law_revealed");

            if (equationView != null)
                equationView.gameObject.SetActive(false);

            // Move to Part B: Try The Law (Bench Challenge)
            Stage = Scene13Stage.TryTheLaw;
            if (challengeView != null)
            {
                challengeView.gameObject.SetActive(true);
                challengeView.Initialize();
            }

            yield return Say("challenge_prompt");
        }

        void OnChallengeDone(bool guided)
        {
            record.challengeSetVoltage = challengeView != null ? challengeView.CurrentVoltage : 3.0f;
            StartCoroutine(AfterChallengeFlow(guided));
        }

        IEnumerator AfterChallengeFlow(bool guided)
        {
            yield return Say("challenge_success");

            if (challengeView != null)
                challengeView.gameObject.SetActive(false);

            // Move to Part C: Explain Condition & Finish
            Stage = Scene13Stage.ExplainCondition;
            if (conditionCard != null)
            {
                conditionCard.SetActive(true);
                if (conditionCardText != null)
                    conditionCardText.text = "OHM'S LAW:  V  =  I  ×  R\n\n"
                        + "• Potential difference V (Volts)\n"
                        + "• Electric current I (Amperes)\n"
                        + "• Resistance R (Ohms)\n\n"
                        + "Essential Condition: Valid for this conductor provided its TEMPERATURE remains constant.";
            }

            yield return Say("condition_temp");
            yield return Say("condition_constant");
            yield return Say("finish_summary");

            AwardMastery();
            CompleteScene13();
        }

        void AwardMastery()
        {
            bool isReplay = profiles.Profile.completedActivities.Contains("Scene13OhmsLaw");
            if (isReplay) return; // Prevent duplicate mastery inflation on replay

            float gain = (record.equationMistakes == 0 && record.hints == 0) ? rules.firstAttemptGain : rules.assistedGain;

            var conceptsToAward = new[] { ConceptId.OhmsLaw, ConceptId.Voltage, ConceptId.Current, ConceptId.Resistance };
            foreach (var cid in conceptsToAward)
            {
                var entry = profiles.Profile.concepts.Find(c => c.concept == cid);
                if (entry != null)
                    entry.mastery = Mathf.Clamp(entry.mastery + gain, 0f, 100f);
            }
        }

        void CompleteScene13()
        {
            if (conditionCard != null) conditionCard.SetActive(false);
            Stage = Scene13Stage.Completed;
            record.completed = true;
            record.completedUtc = DateTime.UtcNow.ToString("o");
            if (!profiles.Profile.completedActivities.Contains("Scene13OhmsLaw"))
                profiles.Profile.completedActivities.Add("Scene13OhmsLaw");

            profiles.Save();

            if (completionPanel != null)
                completionPanel.SetActive(true);

            OnScene13Completed?.Invoke();
            onScene13Completed.Invoke();
        }

        void ReplayScene()
        {
            SceneManager.LoadScene(gameObject.scene.name);
        }

        void ReturnToLab()
        {
            SceneManager.LoadScene("Scene_08_OurLabIntroduction");
        }

        void LoadNextScene()
        {
            if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        void OnChatExplanation()
        {
            record.usedExplanation = true;
        }

        void PauseForChat(bool open)
        {
            chatOpen = open;
        }

        void OnApplicationPause(bool paused)
        {
            appPaused = paused;
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
            if (chat != null) chat.ExplanationRequested -= OnChatExplanation;
            if (chatUI != null) chatUI.VisibilityChanged -= PauseForChat;
        }
    }
}
