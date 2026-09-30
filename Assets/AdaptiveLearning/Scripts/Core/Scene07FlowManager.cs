using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    public enum Scene07Stage { Cinematic, Experiment, EvidenceQuestion, Conclusion, Completed }
    public class Scene07FlowManager : MonoBehaviour
    {
        public bool IsSpeaking => dialogue.IsPresenting;
        [SerializeField] AdaptiveRules rules;
        [SerializeField] QuestionDefinition evidenceQuestion;
        [SerializeField] DialogueSequence lines;
        [SerializeField] StudentProfileManager profiles;
        [SerializeField] AdaptiveLearningManager adaptive;
        [SerializeField] QuestionManager questions;
        [SerializeField] WattsonController wattson;
        [SerializeField] WattsonChatManager chat;
        [SerializeField] WattsonChatUI chatUI;
        [SerializeField] DialoguePresenter dialogue;
        [SerializeField] QuestionUI questionUI;
        [SerializeField] CircuitExperimentController experimentController;
        [SerializeField] Camera learnerCamera;
        [SerializeField] CanvasGroup fade;
        [SerializeField] Text supportText, hintText, progressText;
        [SerializeField] GameObject completionPanel;
        [SerializeField] Button continueButton;
        [SerializeField] ExperimentPresentation presentation;
        [SerializeField] UnityEvent onScene07Completed = new UnityEvent();
        public event Action OnScene07Completed;
        public Scene07Stage Stage { get; private set; }
        public QuestionManager Questions => questions;
        public StudentProfileManager Profiles => profiles;
        public WattsonChatUI ChatUI => chatUI;
        public CircuitExperimentController Experiment => experimentController;
        public int PriorPrediction => priorPredictionIndex;
        bool chatOpen, applicationPaused, resolving, canComplete, replay;
        int questionMistakes, priorPredictionIndex=-1;
        ExperimentRecord activeExperimentRecord;
        public void SetPresentation(ExperimentPresentation value) => presentation=value;
        public void Configure(AdaptiveRules scoring,QuestionDefinition questionDef,DialogueSequence script,
            StudentProfileManager profileManager,AdaptiveLearningManager learning,QuestionManager questionManager,
            WattsonController companion,WattsonChatManager chatManager,WattsonChatUI chatView,DialoguePresenter presenter,
            QuestionUI choices,CircuitExperimentController controller,Camera camera,CanvasGroup curtain,
            Text support,Text hint,Text progress,GameObject completion,Button next)
        {
            rules=scoring; evidenceQuestion=questionDef; lines=script; profiles=profileManager; adaptive=learning;
            questions=questionManager; wattson=companion; chat=chatManager; chatUI=chatView; dialogue=presenter;
            questionUI=choices; experimentController=controller; learnerCamera=camera; fade=curtain;
            supportText=support; hintText=hint; progressText=progress; completionPanel=completion; continueButton=next;
        }
        void Start()
        {
            profiles.Initialize(rules.initialMastery); adaptive.Configure(rules,profiles); questions.Configure(adaptive,profiles);
            wattson.Bind(learnerCamera.transform,dialogue); adaptive.SupportFor(evidenceQuestion);
            replay=profiles.Profile.completedActivities.Contains("Scene07OhmsExperiment");
            activeExperimentRecord=profiles.Profile.experiments.FindLast(e=>e.activityId=="Scene07OhmsExperiment" && !e.completed && Mathf.Abs(e.fixedResistance-experimentController.Circuit.Resistance)<.01f);
            if(activeExperimentRecord==null)
            {
                activeExperimentRecord=new ExperimentRecord { sessionId=Guid.NewGuid().ToString("N"), startedUtc=DateTime.UtcNow.ToString("o"), fixedResistance=experimentController.Circuit.Resistance };
                profiles.Profile.experiments.Add(activeExperimentRecord);
            }
            experimentController.Table.Restore(activeExperimentRecord.observations);
            var prediction=profiles.Profile.questions.FindLast(q=>q.questionId=="S06_VoltageCurrentPrediction" && q.completed && q.finalAnswer>=0);
            if(prediction!=null) priorPredictionIndex=prediction.finalAnswer;
            chat.Configure(()=>new LearningContext {
                scene=gameObject.scene.name, activity="Scene07OhmsExperiment", step=Stage.ToString(),
                hasCircuitMeasurements=true,
                question=Stage==Scene07Stage.EvidenceQuestion?evidenceQuestion.questionText:"",answerChoices=evidenceQuestion.answers,
                concept=ConceptId.VoltageCurrentRelationship, mastery=profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship), support=profiles.Profile.supportLevel,
                switchClosed=experimentController.Circuit.IsSwitchClosed, sourceVoltage=experimentController.Circuit.SourceVoltage,
                circuitResistance=experimentController.Circuit.Resistance, currentReading=experimentController.Circuit.Current,
                resistorVoltageReading=experimentController.Circuit.ResistorVoltage, recordedObservationsCount=experimentController.Table.RecordedCount,
                recentMistake=questions.Record!=null && questions.Record.finalAnswer>0 ? evidenceQuestion.answers[questions.Record.finalAnswer] : "" });
            chat.ExplanationRequested+=Explanation; chatUI.Bind(chat); chatUI.VisibilityChanged+=PauseForChat;
            questionUI.Selected+=questions.Submit; questionUI.RetryRequested+=OnRetry; questionUI.KeepRequested+=OnKeep;
            questions.Answered+=OnQuestionAnswered; profiles.Changed+=UpdateProgress;
            experimentController.ReadingRecorded+=OnReadingRecorded; experimentController.AllReadingsCompleted+=OnAllReadingsCompleted;
            experimentController.Circuit.StateChanged+=CircuitChanged;
            continueButton.onClick.AddListener(CompleteScene07);
            completionPanel.SetActive(false); questionUI.gameObject.SetActive(false); experimentController.SetInteraction(false);
            profiles.Save(); StartCoroutine(Opening());
        }
        void Explanation()
        { activeExperimentRecord.usedExplanation=true; adaptive.RecordExplanation(questions.Record); }
        void PauseForChat(bool value)
        { chatOpen=value; questions.SetPaused(value||applicationPaused); experimentController.SetInteraction(Stage==Scene07Stage.Experiment && !value && !applicationPaused); }
        IEnumerator Wait(float seconds)
        { for(float t=0;t<seconds;) { if(!chatOpen&&!applicationPaused) t+=Time.deltaTime; yield return null; } }
        IEnumerator Say(string id)
        { var line=lines.Get(id); wattson.Speak(line); while(dialogue.IsPresenting)yield return null; }
        IEnumerator Opening()
        {
            Stage=Scene07Stage.Cinematic; fade.alpha=1; fade.blocksRaycasts=true;
            for(float t=0;t<1.2f;t+=Time.deltaTime) {fade.alpha=1-t/1.2f;yield return null;}
            fade.alpha=0;fade.blocksRaycasts=false; wattson.FaceLearner();
            yield return Say("intro_test");
            yield return Say(priorPredictionIndex==0?"recall_increase":priorPredictionIndex==1?"recall_decrease":priorPredictionIndex==2?"recall_same":"intro_neutral");
            yield return Say("intro_same_resistor");
            if(experimentController.Table.IsComplete) { StartCoroutine(Evidence()); yield break; }
            Stage=Scene07Stage.Experiment;experimentController.SetInteraction(!chatOpen); Guidance();
        }
        void CircuitChanged(SimpleCircuitModel model) { if(Stage==Scene07Stage.Experiment) Guidance(); }
        void Guidance()
        {
            int next=1; while(next<=LessonExperimentData.ReadingCount && experimentController.Table.Observations.Exists(o=>Mathf.Abs(o.voltage-next)<.01f)) next++;
            string instruction=!experimentController.Circuit.IsSwitchClosed ? "01 / CLOSE THE SWITCH" : "02 / COLLECT YOUR MEASUREMENTS";
            hintText.text=!experimentController.Circuit.IsSwitchClosed ? "Complete the circuit to begin. Keep the same wire at constant temperature."
                : "Turn the knob to " + next + " V, compare the meters, then record.";
            if(presentation!=null) presentation.SetStage(true,instruction,profiles.Profile.supportLevel,next);
            experimentController.Table.SetFeedback(experimentController.Circuit.IsSwitchClosed ? "Choose a new voltage for each observation." : "Current is zero while the circuit is open.");
        }
        void OnReadingRecorded(float voltage,float current)
        {
            activeExperimentRecord.observations=new List<ExperimentObservation>(experimentController.Table.Observations);
            activeExperimentRecord.supportLevel=profiles.Profile.supportLevel; profiles.Save();
            if(!experimentController.Table.IsComplete) { Guidance(); wattson.Speak(lines.Get("reading_recorded")); }
        }
        void OnAllReadingsCompleted() { if(Stage==Scene07Stage.Experiment) StartCoroutine(Evidence()); }
        IEnumerator Evidence()
        {
            Stage=Scene07Stage.Conclusion; experimentController.SetInteraction(false);
            hintText.text = "Compare your five observations while the wire stays at constant temperature.";
            if(presentation!=null)presentation.SetStage(false,"03 / READ THE EVIDENCE",profiles.Profile.supportLevel);
            yield return Say("readings_complete");
            questions.Begin(evidenceQuestion,!replay && !(activeExperimentRecord.conclusionOutcome??"").StartsWith("Correct")); questions.Record.usedExplanation=activeExperimentRecord.usedExplanation;
            questions.SetPaused(chatOpen||applicationPaused); wattson.Speak(lines.Get("evidence_prompt"));
            Stage=Scene07Stage.EvidenceQuestion; questionUI.Show(evidenceQuestion);
            hintText.text="Compare the current at 1 V and 3 V in your recorded data.";
        }
        void OnQuestionAnswered(bool correct)
        {
            questionUI.SetInteractable(false); resolving=true;
            if(correct)
            {
                activeExperimentRecord.conclusionOutcome=questions.Record.attempts.Count==1 && !questions.Record.usedExplanation && questions.Record.hintCount==0 ? "CorrectFirstAttempt":"CorrectAssisted";
                profiles.Save(); StartCoroutine(Conclusion(false));
            }
            else {questionMistakes++;StartCoroutine(Clue());}
        }
        IEnumerator Clue()
        {
            yield return Say("evidence_clue"); adaptive.RecordHint(questions.Record); activeExperimentRecord.hintCount++;
            hintText.text=questionMistakes>=2 ? "0.60 A is greater than 0.20 A. Use this comparison to revise your answer." : evidenceQuestion.hints[0];
            activeExperimentRecord.supportLevel=profiles.Profile.supportLevel;profiles.Save();
            if(questionMistakes>=2) wattson.Speak(lines.Get("evidence_supported_clue"));
            questionUI.ShowDecision(); resolving=false;
        }
        public void OnRetry()
        {
            if(chatOpen||resolving||Stage!=Scene07Stage.EvidenceQuestion)return;
            questions.Retry();questionUI.ShowAnswers();wattson.Speak(lines.Get("evidence_prompt"));
        }
        public void OnKeep()
        {
            if(chatOpen||resolving||Stage!=Scene07Stage.EvidenceQuestion||questions.Record==null||questions.Record.attempts.Count==0)return;
            activeExperimentRecord.conclusionOutcome="GuidedCompleted";activeExperimentRecord.usedExplanation=true;
            resolving=true;StartCoroutine(Conclusion(true));
        }
        IEnumerator Conclusion(bool guided)
        {
            Stage=Scene07Stage.Conclusion;questionUI.gameObject.SetActive(false);
            yield return Say(guided?"evidence_guided_complete":"evidence_accepted");
            if(priorPredictionIndex>=0) yield return Say(priorPredictionIndex==0?"prediction_matched":"prediction_revised");
            hintText.text="Five measurements. One pattern. Let's plot the evidence.";
            completionPanel.SetActive(true);canComplete=true;resolving=false;profiles.Save();
            wattson.Speak(lines.Get("scene_complete"));
        }
        public void CompleteScene07()
        {
            if(!canComplete||Stage==Scene07Stage.Completed||chatOpen||!experimentController.Table.IsComplete)return;
            Stage=Scene07Stage.Completed;canComplete=false;questions.Finish("Scene07Evidence");
            activeExperimentRecord.completed=true;activeExperimentRecord.completedUtc=DateTime.UtcNow.ToString("o");
            if(!profiles.Profile.completedActivities.Contains("Scene07OhmsExperiment"))profiles.Profile.completedActivities.Add("Scene07OhmsExperiment");
            profiles.Save();continueButton.interactable=false;wattson.Speak(lines.Get("final_handoff"));
            OnScene07Completed?.Invoke();onScene07Completed.Invoke();
        }
        void UpdateProgress()
        {
            if(profiles.Profile==null)return;
            supportText.text="WATTSON / "+profiles.Profile.supportLevel.ToString().ToUpperInvariant();
            progressText.text=profiles.SaveError==null ? "PROGRESS SAVED LOCALLY" : "SAVE UNAVAILABLE — PROGRESS IN MEMORY";
        }
        void OnApplicationPause(bool paused)
        {applicationPaused=paused;if(questions!=null)questions.SetPaused(paused||chatOpen);}
        void OnDestroy()
        {
            if(profiles!=null)profiles.Changed-=UpdateProgress;
            if(questions!=null)questions.Answered-=OnQuestionAnswered;
            if(chat!=null)chat.ExplanationRequested-=Explanation;
            if(chatUI!=null)chatUI.VisibilityChanged-=PauseForChat;
            if(experimentController!=null) {experimentController.ReadingRecorded-=OnReadingRecorded;experimentController.AllReadingsCompleted-=OnAllReadingsCompleted;experimentController.Circuit.StateChanged-=CircuitChanged;}
        }
    }
}



