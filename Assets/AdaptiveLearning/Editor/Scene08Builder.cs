using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class Scene08Builder
    {
        public const string ScenePath="Assets/AdaptiveLearning/Scenes/Scene_11_TurnDataIntoGraph.unity";
        static Scene08Builder(){EditorApplication.update+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Logs/scene08-build.req")){File.Delete("Logs/scene08-build.req");try{Build();File.WriteAllText("Logs/scene08-build.txt","COMPLETE");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/scene08-build.txt",e.ToString());}}};}
        static T F<T>(string name) where T:Component=>LessonReviewTools.Find<T>(name);
        static RectTransform Area(string name,Transform parent,float x,float y,float r,float t)=>LessonReviewTools.Area(name,parent,x,y,r,t);
        static Text Text(string name,Transform parent,string text,int size,float x,float y,float r,float t)=>LessonReviewTools.Label(name,parent,text,size,x,y,r,t);
        static Button Button(string name,Transform parent,string text,float x,float y,float r,float t)
        {
            var rt=Area(name,parent,x,y,r,t);var image=rt.gameObject.AddComponent<Image>();image.color=new Color(.075f,.22f,.27f);
            var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(.7f,1,1);colors.pressedColor=new Color(.4f,.8f,.9f);colors.selectedColor=new Color(.85f,1,1);colors.disabledColor=new Color(.7f,.8f,.8f,.7f);button.colors=colors;
            var txt=Text("Label",rt,text,22,.04f,.08f,.96f,.92f);txt.alignment=TextAnchor.MiddleCenter;
            var o=rt.gameObject.AddComponent<Outline>();o.effectColor=new Color(.3f,1,.85f);o.effectDistance=new Vector2(3,-3);o.enabled=false;
            return button;
        }
        static void Hide(string name)=>F<Transform>(name).gameObject.SetActive(false);
        [MenuItem("Adaptive Learning/Create Scene 11 Graph Activity")]
        public static void Build()
        {
            if(File.Exists(ScenePath))throw new InvalidOperationException("Scene 08 already exists. Edit the existing scene rather than rebuilding it.");
            Scene07Refinement.Apply();
            var f7=UnityEngine.Object.FindFirstObjectByType<Scene07FlowManager>();
            var transition=f7.gameObject.GetComponent<Scene07To08Transition>()??f7.gameObject.AddComponent<Scene07To08Transition>();transition.Configure(f7,F<CanvasGroup>("CinematicFade"));
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
            UnityEngine.Object.DestroyImmediate(transition);UnityEngine.Object.DestroyImmediate(f7);
            UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindFirstObjectByType<ExperimentPresentation>());
            UnityEngine.Object.FindFirstObjectByType<CircuitExperimentController>().enabled=false;
            UnityEngine.Object.FindFirstObjectByType<CircuitSwitchInteraction>().enabled=false;
            foreach(var name in new[]{"MeterUIPanel","CircuitControlBar","ObservationTablePanel","ModelNote","SourceState","ExperimentStep","ApparatusViewport"})Hide(name);
            var safe=F<RectTransform>("SafeArea");
            F<Text>("Lesson").text="08 / THE PATTERN IN THE EVIDENCE";F<Text>("Objective").text="Turn your measurements into a voltage–current graph.";
            F<Text>("CompletionText").text="Graph complete. Your evidence and interpretation are saved.";F<Button>("Continue").GetComponentInChildren<Text>().text="FINISH ACTIVITY";
            F<Text>("Context").text="SCENE 08 / GRAPH EVIDENCE • LOCAL LESSON GUIDE";
            F<Button>("AmmeterHelp").GetComponentInChildren<Text>().text="How do I plot a point?";
            F<Button>("VoltmeterHelp").GetComponentInChildren<Text>().text="Which axes do I use?";
            F<Button>("CurrentZero").GetComponentInChildren<Text>().text="What does this pattern mean?";
            F<Button>("WhatNext").GetComponentInChildren<Text>().text="I don't understand the graph.";
            var panel=Area("GraphPanel",safe,.035f,.37f,.68f,.855f);panel.gameObject.AddComponent<Image>().color=new Color(.027f,.08f,.12f);
            Text("GraphTitle",panel,"POTENTIAL DIFFERENCE AGAINST CURRENT",20,.03f,.9f,.95f,.98f);
            Text("YAxisTitle",panel,"Potential difference / V",18,.025f,.81f,.35f,.9f);
            var plot=Area("PlotArea",panel,.14f,.15f,.95f,.83f);
            var grid=plot.gameObject.AddComponent<GraphGridGraphic>();grid.raycastTarget=false;
            for(int i=0;i<=5;i++)
            {
                float x=i/6f,y=i/6f;
                var xt=Text("VoltageTick"+i,plot,(i*.2f).ToString("0.0"),19,x-.04f,-.10f,x+.04f,-.01f);xt.alignment=TextAnchor.MiddleCenter;
                var yt=Text("CurrentTick"+i,plot,i.ToString(),19,-.15f,y-.05f,-.025f,y+.05f);yt.alignment=TextAnchor.MiddleRight;
            }
            var axis=Text("XAxisTitle",panel,"Current / A",18,.35f,.01f,.8f,.075f);axis.alignment=TextAnchor.MiddleCenter;
            var targets=new Button[25];
            for(int i=0;i<25;i++)
            {
                float x=(i%5+1)/6f,y=(i/5+1)/6f;
                targets[i]=Button("Plot_"+(i%5+1)+"_"+(i/5+1),plot,"+",x,y,x,y);
                ((RectTransform)targets[i].transform).sizeDelta=new Vector2(64,64);
            }
            var view=Area("CompanionViewport",safe,.71f,.60f,.965f,.855f);
            var companion=UnityEngine.Object.FindFirstObjectByType<WattsonController>();
            var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
            var frame=camera.gameObject.AddComponent<CompanionViewport>();frame.Configure(camera,view,companion.transform);
            var provenance=Text("DataProvenance",safe,"",17,.71f,.555f,.965f,.605f);
            var readings=new Button[5];
            for(int i=0;i<5;i++)readings[i]=Button("GraphReading"+i,safe,"",.04f+i*.123f,.30f,.157f+i*.123f,.365f);
            var status=Text("GraphStatus",safe,"PREPARING THE GRAPH",20,.04f,.325f,.69f,.367f);
            var help=Button("GraphHint",safe,"SHOW A CLUE",.72f,.07f,.96f,.17f);
            var instruction=F<Text>("HintUI");LessonReviewTools.Set((RectTransform)instruction.transform,.045f,.075f,.70f,.17f);instruction.text="";
            // Keep the action hint below the dialogue; the question replaces it during interpretation.
            var plotView=panel.gameObject.AddComponent<GraphPlotView>();plotView.Configure(grid,targets,readings,status);
            var question=ScriptableObject.CreateInstance<QuestionDefinition>();question.questionId="S08_GraphProportionality";
            question.concepts=new[]{ConceptId.GraphInterpretation,ConceptId.VoltageCurrentRelationship};question.questionText="What does this straight line through the origin tell us, for this fixed resistor?";
            question.answers=new[]{"Current is proportional to voltage","Current stays constant","Current falls as voltage rises"};question.correctAnswer=0;question.difficulty=1;question.masteryImpact=1;
            question.hints=new[]{"Compare 1 V, 0.20 A with 2 V, 0.40 A.","When voltage doubles, current doubles too.","A straight line through the origin shows proportionality."};
            AssetDatabase.CreateAsset(question,"Assets/AdaptiveLearning/ScriptableObjects/Questions/Scene08GraphQuestion.asset");
            var script=ScriptableObject.CreateInstance<DialogueSequence>();script.lines=new[]{
                Line("intro","These are the measurements you collected. Let's see their pattern on a graph.",3.5f),
                Line("sample","We're using clearly marked practice readings from the same ideal 5-ohm circuit.",3.5f),
                Line("axes","Current goes along the bottom. Potential difference goes up the side. Select a reading, then tap where its two values meet.",5),
                Line("placement_hint","Let's follow one value at a time. Find the current column, then the voltage row.",3.5f),
                Line("pattern","Your five points form a straight line. In this ideal model, zero volts also gives zero current, so the line goes through the origin.",5),
                Line("question",question.questionText,4),
                Line("question_hint","At 1 volt we measured 0.20 amperes. At 2 volts we measured 0.40 amperes. Both values doubled. What relationship fits?",4.5f),
                Line("conclusion","The evidence shows proportionality: doubling voltage doubles current. This holds for our fixed resistor at constant temperature.",5),
                Line("complete","Your graph and explanation are saved. We have a pattern we can use in our next investigation.",4)};
            AssetDatabase.CreateAsset(script,"Assets/AdaptiveLearning/ScriptableObjects/Dialogue/Scene08Dialogue.asset");
            var systems=F<Transform>("LearningSystems");var flow=systems.gameObject.AddComponent<Scene08FlowManager>();
            flow.Configure(AssetDatabase.LoadAssetAtPath<AdaptiveRules>("Assets/AdaptiveLearning/ScriptableObjects/Concepts/PrototypeRules.asset"),question,script,
                UnityEngine.Object.FindFirstObjectByType<StudentProfileManager>(),UnityEngine.Object.FindFirstObjectByType<AdaptiveLearningManager>(),UnityEngine.Object.FindFirstObjectByType<QuestionManager>(),
                companion,UnityEngine.Object.FindFirstObjectByType<WattsonChatManager>(),UnityEngine.Object.FindFirstObjectByType<WattsonChatUI>(),UnityEngine.Object.FindFirstObjectByType<DialoguePresenter>(),
                UnityEngine.Object.FindFirstObjectByType<QuestionUI>(FindObjectsInactive.Include),plotView,camera,F<CanvasGroup>("CinematicFade"),instruction,provenance,F<Text>("Support"),F<Text>("LearningProgress"),F<Transform>("Completion").gameObject,F<Button>("Continue"),help);
            foreach(var t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None)){t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Max(14,t.fontSize-4);t.resizeTextMaxSize=t.fontSize;}
            F<Transform>("WattsonChatPanel").SetAsLastSibling();
            var builds=EditorBuildSettings.scenes.ToList();if(!builds.Any(s=>s.path==ScenePath))builds.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=builds.ToArray();
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        static DialogueLine Line(string id,string text,float seconds)=>new DialogueLine{id=id,text=text,duration=seconds};
    }
}


