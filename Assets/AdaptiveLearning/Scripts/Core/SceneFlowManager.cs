using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    public enum LearningStage { Cinematic, Interaction, Explanation, NextActivity }
    public class SceneFlowManager : MonoBehaviour
    {
        [SerializeField] AdaptiveRules rules;
        [SerializeField] QuestionDefinition question;
        [SerializeField] DialogueSequence lines;
        [SerializeField] StudentProfileManager profiles;
        [SerializeField] AdaptiveLearningManager adaptive;
        [SerializeField] QuestionManager questions;
        [SerializeField] WattsonController wattson;
        [SerializeField] WattsonChatManager chat;
        [SerializeField] WattsonChatUI chatUI;
        [SerializeField] DialoguePresenter dialogue;
        [SerializeField] QuestionUI questionUI;
        [SerializeField] VoltageVisualization voltage;
        [SerializeField] Camera learnerCamera;
        [SerializeField] CanvasGroup fade;
        [SerializeField] Text support, hint, progress;
        [SerializeField] GameObject completion;
        [SerializeField] Button continueButton;
        [SerializeField] UnityEvent onScene06Completed = new UnityEvent();
        public event Action OnScene06Completed;
        public LearningStage Stage { get; private set; }
        public QuestionManager Questions => questions;
        public StudentProfileManager Profiles => profiles;
        public WattsonChatUI ChatUI => chatUI;
        public bool IsSpeaking => dialogue.IsPresenting;
        bool chatOpen;
        public void Configure(AdaptiveRules scoring, QuestionDefinition definition, DialogueSequence script,
            StudentProfileManager profileManager, AdaptiveLearningManager learning, QuestionManager questionManager,
            WattsonController companion, WattsonChatManager chatManager, WattsonChatUI chatView, DialoguePresenter presenter,
            QuestionUI choices, VoltageVisualization battery, Camera camera, CanvasGroup curtain,
            Text supportText, Text hintText, Text progressText, GameObject end, Button next)
        {
            rules = scoring; question = definition; lines = script; profiles = profileManager; adaptive = learning;
            questions = questionManager; wattson = companion; chat = chatManager; chatUI = chatView; dialogue = presenter;
            questionUI = choices; voltage = battery; learnerCamera = camera; fade = curtain;
            support = supportText; hint = hintText; progress = progressText; completion = end; continueButton = next;
        }
        void Start()
        {
            profiles.Initialize(rules.initialMastery); adaptive.Configure(rules, profiles); questions.Configure(adaptive, profiles);
            wattson.Bind(learnerCamera.transform, dialogue);
            chat.Configure(() => new LearningContext {
                scene = gameObject.scene.name, activity = "Scene06Prediction", concept = ConceptId.VoltageCurrentRelationship,
                hasVoltageDemonstration=true,sourceVoltage=voltage.Voltage,
                  mastery = profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship), support = profiles.Profile.supportLevel,
                  step=Stage.ToString(),question=Stage==LearningStage.Interaction?question.questionText:"",answerChoices=question.answers,
                recentMistake = questions.Record != null && questions.Record.finalAnswer > 0 ? question.answers[questions.Record.finalAnswer] : "" });
            chat.ExplanationRequested += () => adaptive.RecordExplanation(questions.Record);
            chatUI.Bind(chat); chatUI.VisibilityChanged += PauseForChat;
            questionUI.Selected += questions.Submit; questionUI.RetryRequested += Retry; questionUI.KeepRequested += Keep;
            questions.Answered += Answered; profiles.Changed += UpdateProgress;
            continueButton.onClick.AddListener(Complete);
            completion.SetActive(false); questionUI.gameObject.SetActive(false);
            UpdateProgress(); StartCoroutine(Opening());
        }
        void PauseForChat(bool value) { chatOpen = value; questions.SetPaused(value); }
        IEnumerator WaitForLearner(float seconds)
        {
            float elapsed = 0;
            while (elapsed < seconds) { if (!chatOpen) elapsed += Time.deltaTime; yield return null; }
        }
        IEnumerator Opening()
        {
            Stage = LearningStage.Cinematic; fade.alpha = 1; fade.blocksRaycasts = true; voltage.SetVoltage(0);
            for (float t = 0; t < 1.8f; t += Time.deltaTime) { fade.alpha = 1 - t / 1.8f; yield return null; }
            fade.alpha = 0; fade.blocksRaycasts = false;
            wattson.FaceLearner(); wattson.Speak(lines.Get("detective")); yield return WaitForLearner(lines.Get("detective").duration);
            yield return voltage.Demonstrate(); yield return WaitForLearner(.3f);
            wattson.Speak(lines.Get("question")); questions.Begin(question); questions.SetPaused(chatOpen);
            Stage = LearningStage.Interaction; questionUI.Show(question);
            if (profiles.Profile.supportLevel != SupportLevel.Independent) ShowHint(false);
        }
        void Answered(bool correct)
        {
            questionUI.SetInteractable(false); Stage = LearningStage.Explanation;
            if (correct) { wattson.Speak(lines.Get("accepted")); StartCoroutine(PrepareCompletion()); }
            else StartCoroutine(Clue());
        }
        IEnumerator Clue()
        {
            wattson.Speak(lines.Get("curious")); yield return WaitForLearner(lines.Get("curious").duration);
            ShowHint(true); yield return voltage.Demonstrate(); yield return WaitForLearner(.2f);
            wattson.Speak(lines.Get(profiles.Profile.supportLevel == SupportLevel.Supported ? "supported" : "retry"));
            questionUI.ShowDecision();
        }
        void ShowHint(bool count)
        {
            if (count || questions.Record.hintCount == 0) adaptive.RecordHint(questions.Record);
            int index = Mathf.Clamp((int)profiles.Profile.supportLevel - 2, 0, question.hints.Length - 1);
            hint.text = question.hints[index];
            if (profiles.Profile.supportLevel == SupportLevel.Supported) hint.text += "\n" + question.hints[question.hints.Length - 1];
            voltage.ShowSupport(profiles.Profile.supportLevel);
            UpdateProgress();
        }
        public void Retry()
        {
            if (chatOpen || questions.Record.completed) return;
            questions.Retry(); Stage = LearningStage.Interaction; questionUI.ShowAnswers(); wattson.Speak(lines.Get("question"));
        }
        public void Keep()
        {
            if (chatOpen || questions.Record.completed) return;
            questionUI.gameObject.SetActive(false); wattson.Speak(lines.Get("keep")); StartCoroutine(PrepareCompletion());
        }
        IEnumerator PrepareCompletion()
        {
            questionUI.gameObject.SetActive(false); yield return WaitForLearner(3);while(dialogue.IsPresenting)yield return null;
            completion.SetActive(true);
        }
        public void Complete()
        {
            if (Stage == LearningStage.NextActivity || questions.Record == null || questions.Record.finalAnswer < 0 || chatOpen) return;
            questions.Finish("Scene06Prediction"); Stage = LearningStage.NextActivity;
            continueButton.interactable = false; continueButton.GetComponentInChildren<Text>().text = "PREDICTION RECORDED";
            wattson.Speak(lines.Get("complete")); OnScene06Completed?.Invoke(); onScene06Completed.Invoke();
        }
        void UpdateProgress()
        {
            if (profiles.Profile == null) return;
            support.text = "WATTSON INTERFACE  /  " + profiles.Profile.supportLevel.ToString().ToUpperInvariant();
            progress.text = "LEARNING PROFILE   V " + profiles.Profile.Mastery(ConceptId.Voltage).ToString("0")
                + "  •  I " + profiles.Profile.Mastery(ConceptId.Current).ToString("0")
                + "  •  RELATIONSHIP " + profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship).ToString("0")
                + (profiles.SaveError == null ? "" : "  •  SAVE UNAVAILABLE");
        }
        void OnApplicationPause(bool paused) { if (questions != null) questions.SetPaused(paused || chatOpen); }
        void OnDestroy()
        {
            if (profiles != null) profiles.Changed -= UpdateProgress;
            if (questions != null) questions.Answered -= Answered;
        }
    }
}
