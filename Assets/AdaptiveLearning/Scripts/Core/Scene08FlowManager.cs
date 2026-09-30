using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    public enum Scene08Stage { Opening, Plotting, Interpretation, Explanation, Completed }
    public class Scene08FlowManager : MonoBehaviour
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
        [SerializeField] QuestionUI choices;
        [SerializeField] GraphPlotView graph;
        [SerializeField] Camera learnerCamera;
        [SerializeField] CanvasGroup fade;
        [SerializeField] Text hint,source,support,progress;
        [SerializeField] GameObject completion;
        [SerializeField] Button finish,help;
        [SerializeField] UnityEvent onScene08Completed=new UnityEvent();
        public event Action OnScene08Completed;
        public Scene08Stage Stage {get;private set;}
        public GraphActivityRecord Record {get;private set;}
        public StudentProfileManager Profiles=>profiles;
        public QuestionManager Questions=>questions;
        public GraphPlotView Graph=>graph;
        public WattsonChatUI ChatUI=>chatUI;
        bool chatOpen,appPaused,resolving,replay,canFinish;
        public void Configure(AdaptiveRules scoring,QuestionDefinition definition,DialogueSequence script,StudentProfileManager profile,
            AdaptiveLearningManager learning,QuestionManager quiz,WattsonController companion,WattsonChatManager assistant,WattsonChatUI chatView,
            DialoguePresenter presenter,QuestionUI questionView,GraphPlotView plot,Camera camera,CanvasGroup curtain,Text instruction,Text provenance,
            Text supportLabel,Text saved,GameObject end,Button next,Button clue)
        {
            rules=scoring;question=definition;lines=script;profiles=profile;adaptive=learning;questions=quiz;wattson=companion;chat=assistant;chatUI=chatView;
            dialogue=presenter;choices=questionView;graph=plot;learnerCamera=camera;fade=curtain;hint=instruction;source=provenance;support=supportLabel;progress=saved;completion=end;finish=next;help=clue;
        }
        void Start()
        {
            profiles.Initialize(rules.initialMastery);adaptive.Configure(rules,profiles);questions.Configure(adaptive,profiles);wattson.Bind(learnerCamera.transform,dialogue);
            var experiment=profiles.Profile.experiments.FindLast(e=>e.completed&&ValidData(e.observations));
            Record=profiles.Profile.graphs.FindLast(g=>!g.completed&&ValidData(g.observations)&&g.sourceSessionId==experiment?.sessionId);
            if(Record==null)
            {
                Record=new GraphActivityRecord {sessionId=Guid.NewGuid().ToString("N"),sourceSessionId=experiment?.sessionId,startedUtc=DateTime.UtcNow.ToString("o"),sampleData=experiment==null};
                if(experiment!=null)Record.observations=experiment.observations.OrderBy(o=>o.voltage).Select(o=>new ExperimentObservation{voltage=o.voltage,current=o.current,recordedUtc=o.recordedUtc}).ToList();
                else for(int i=1;i<=LessonExperimentData.ReadingCount;i++)Record.observations.Add(new ExperimentObservation{voltage=i,current=i*LessonExperimentData.CurrentStep});
                profiles.Profile.graphs.Add(Record);
            }
            Record.plottedIndices.RemoveAll(i=>i<0||i>=Record.observations.Count);Record.plottedIndices=Record.plottedIndices.Distinct().ToList();
            replay=profiles.Profile.completedActivities.Contains("Scene08Graph");
            source.text=Record.sampleData?"PRACTICE READINGS / No saved experiment":"YOUR MEASUREMENTS / SAME WIRE, CONSTANT TEMPERATURE";
            var benchCircuit = FindFirstObjectByType<SimpleCircuitModel>();
            if (benchCircuit != null) benchCircuit.Configure(LessonExperimentData.Resistance, Record.observations.Max(o => o.voltage), true);
            adaptive.SupportFor(question,Record.plottingMistakes);graph.Initialize(Record.observations,Record.plottedIndices);graph.SetInteraction(false);
            graph.Placement+=Placed;help.onClick.AddListener(RequestHint);finish.onClick.AddListener(Complete);
            choices.Selected+=questions.Submit;choices.RetryRequested+=Retry;choices.KeepRequested+=Guided;questions.Answered+=Answered;
            chat.Configure(()=>new LearningContext{scene=gameObject.scene.name,activity="Scene08Graph",step=Stage.ToString(),concept=ConceptId.GraphInterpretation,
                question=Stage==Scene08Stage.Interpretation?question.questionText:"",answerChoices=question.answers,
                hasSelectedReading=true,usingPracticeData=Record.sampleData,
                mastery=profiles.Profile.Mastery(ConceptId.GraphInterpretation),support=profiles.Profile.supportLevel,
                recentMistake=Record.plottingMistakes>0?"A point did not match its voltage and current":"",recordedObservationsCount=Record.observations.Count,
                plottedObservationsCount=Record.plottedIndices.Count,selectedVoltage=Record.observations[graph.Selected].voltage,selectedCurrent=Record.observations[graph.Selected].current});
            chat.ExplanationRequested+=Explanation;chatUI.Bind(chat);chatUI.VisibilityChanged+=PauseForChat;profiles.Changed+=UpdateStatus;
            completion.SetActive(false);choices.gameObject.SetActive(false);help.interactable=false;profiles.Save();StartCoroutine(Opening());
        }
        static bool ValidData(List<ExperimentObservation> data)
        {
            return LessonExperimentData.IsValid(data);
        }
        IEnumerator Wait(float seconds){for(float t=0;t<seconds;){if(!chatOpen&&!appPaused)t+=Time.deltaTime;yield return null;}}
        IEnumerator Say(string id){var line=lines.Get(id);wattson.Speak(line);while(dialogue.IsPresenting)yield return null;}
        IEnumerator Opening()
        {
            fade.alpha=1;fade.blocksRaycasts=true;
            for(float t=0;t<1;t+=Time.deltaTime){fade.alpha=1-t;yield return null;}fade.alpha=0;fade.blocksRaycasts=false;
            wattson.FaceLearner();yield return Say(Record.sampleData?"sample":"intro");yield return Say("axes");
            if(Record.plottedIndices.Count==Record.observations.Count){StartCoroutine(Interpret());yield break;}
            Stage=Scene08Stage.Plotting;graph.SetInteraction(!chatOpen);help.interactable=true;hint.text="Select a reading, then tap the intersection of its current and voltage.";graph.Highlight(profiles.Profile.supportLevel);
        }
        void Placed(int index,bool correct)
        {
            if(Stage!=Scene08Stage.Plotting)return;
            if(!correct)
            {
                Record.plottingMistakes++;adaptive.SupportFor(question,Record.plottingMistakes);GiveHint();wattson.Speak(lines.Get("placement_hint"));
            }
            else
            {
                hint.text="Point recorded. Follow the current column and voltage row for the next reading.";
                graph.Highlight(profiles.Profile.supportLevel);
                if(Record.plottedIndices.Count==Record.observations.Count)StartCoroutine(Interpret());
            }
            profiles.Save();
        }
        void GiveHint()
        {
            Record.hints++;profiles.Profile.hintCount++;Record.usedExplanation=true;
            var o=Record.observations[graph.Selected];
            hint.text="Find "+o.current.ToString("0.00")+" A along the bottom. Move up to "+o.voltage.ToString("0.0")+" V. Tap where they meet.";
            var level=Record.plottingMistakes>=2?SupportLevel.Supported:SupportLevel.Guided;
            profiles.Profile.supportLevel=(SupportLevel)Mathf.Max((int)profiles.Profile.supportLevel,(int)level);graph.Highlight(profiles.Profile.supportLevel);profiles.Save();
        }
        void RequestHint(){if(Stage==Scene08Stage.Plotting&&!chatOpen)GiveHint();}
        void Explanation(){Record.usedExplanation=true;adaptive.RecordExplanation(questions.Record);if(Stage==Scene08Stage.Plotting)GiveHint();}
        IEnumerator Interpret()
        {
            Stage=Scene08Stage.Explanation;graph.SetInteraction(false);help.gameObject.SetActive(false);hint.gameObject.SetActive(false);
            yield return Say("pattern");
            questions.Begin(question,!Record.sampleData && !replay && !(Record.conclusionOutcome??"").StartsWith("Correct"));questions.Record.usedExplanation=Record.usedExplanation||Record.hints>0;adaptive.SupportFor(question,Record.plottingMistakes);
            questions.SetPaused(chatOpen||appPaused);profiles.Save();
            Stage=Scene08Stage.Interpretation;choices.Show(question);wattson.Speak(lines.Get("question"));
            hint.text="The line includes the origin predicted by this ideal, fixed-resistance model. The five dots are your measured points.";
        }
        void Answered(bool correct)
        {
            choices.SetInteractable(false);resolving=true;
            if(correct){Record.conclusionOutcome=questions.Record.attempts.Count==1&&!questions.Record.usedExplanation?"CorrectFirstAttempt":"CorrectAssisted";profiles.Save();StartCoroutine(Conclude(false));}
            else StartCoroutine(QuestionHint());
        }
        IEnumerator QuestionHint()
        {
            yield return Say("question_hint");adaptive.RecordHint(questions.Record);Record.hints++;profiles.Save();choices.ShowDecision();resolving=false;
        }
        void Retry(){if(resolving||chatOpen||Stage!=Scene08Stage.Interpretation)return;questions.Retry();choices.ShowAnswers();wattson.Speak(lines.Get("question"));}
        void Guided(){if(resolving||chatOpen||Stage!=Scene08Stage.Interpretation)return;Record.usedExplanation=true;Record.conclusionOutcome="GuidedCompleted";StartCoroutine(Conclude(true));}
        IEnumerator Conclude(bool guided)
        {
            resolving=true;Stage=Scene08Stage.Explanation;choices.gameObject.SetActive(false);
            yield return Say("conclusion");completion.SetActive(true);canFinish=true;hint.text="A straight line through the origin: current is proportional to voltage while resistance and temperature stay fixed.";profiles.Save();resolving=false;
        }
        public void Complete()
        {
            if(!canFinish||chatOpen||Stage==Scene08Stage.Completed)return;
            canFinish=false;Stage=Scene08Stage.Completed;questions.Finish(Record.sampleData?"Scene08GraphPractice":"Scene08Graph");Record.completed=true;Record.completedUtc=DateTime.UtcNow.ToString("o");profiles.Save();
            finish.interactable=false;finish.GetComponentInChildren<Text>().text="ACTIVITY SAVED";wattson.Speak(lines.Get("complete"));OnScene08Completed?.Invoke();onScene08Completed.Invoke();
        }
        void PauseForChat(bool value){chatOpen=value;questions.SetPaused(value||appPaused);graph.SetInteraction(Stage==Scene08Stage.Plotting&&!value&&!appPaused);graph.Highlight(profiles.Profile.supportLevel);}
        void OnApplicationPause(bool value){appPaused=value;if(questions!=null)questions.SetPaused(value||chatOpen);if(graph!=null&&Record!=null)graph.SetInteraction(Stage==Scene08Stage.Plotting&&!value&&!chatOpen);}
        void UpdateStatus(){support.text="WATTSON / "+profiles.Profile.supportLevel.ToString().ToUpperInvariant();progress.text=profiles.SaveError==null?"PROGRESS SAVED LOCALLY":"SAVE UNAVAILABLE • PROGRESS IN MEMORY";}
        void OnDestroy(){if(profiles!=null)profiles.Changed-=UpdateStatus;if(questions!=null)questions.Answered-=Answered;if(chat!=null)chat.ExplanationRequested-=Explanation;if(chatUI!=null)chatUI.VisibilityChanged-=PauseForChat;if(graph!=null)graph.Placement-=Placed;}
    }
}





