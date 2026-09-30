using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class LessonReviewTools
    {
        static LessonReviewTools() { EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Logs/lesson-review.req")) {File.Delete("Logs/lesson-review.req");try{Patch();File.WriteAllText("Logs/lesson-review.txt","PATCH COMPLETE");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/lesson-review.txt",e.ToString());}}
        }
        public static T Find<T>(string name) where T:Component => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.name==name);
        public static RectTransform Area(string name,Transform parent,float x,float y,float right,float top)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;Set(rt,x,y,right,top);return rt;}
        public static void Set(RectTransform rt,float x,float y,float right,float top){rt.anchorMin=new Vector2(x,y);rt.anchorMax=new Vector2(right,top);rt.offsetMin=rt.offsetMax=Vector2.zero;}
        static void Move(string name,float x,float y,float r,float t)=>Set(Find<RectTransform>(name),x,y,r,t);
        public static Text Label(string name,Transform parent,string value,int size,float x,float y,float r,float t)
        {var rt=Area(name,parent,x,y,r,t);var txt=rt.gameObject.AddComponent<Text>();txt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");txt.text=value;txt.fontSize=size;txt.color=new Color(.8f,.9f,.94f);txt.alignment=TextAnchor.MiddleLeft;txt.raycastTarget=false;return txt;}
        public static void Float(UnityEngine.Object obj,string key,float value){var s=new SerializedObject(obj);s.FindProperty(key).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        [MenuItem("Adaptive Learning/Apply Scene 07 Review Fixes")]
        public static void Patch()
        {
            EditorSceneManager.OpenScene(Scene06Builder.ScenePath);
            var f6=UnityEngine.Object.FindFirstObjectByType<SceneFlowManager>();
            var transition=f6.GetComponent<Scene06To07Transition>()??f6.gameObject.AddComponent<Scene06To07Transition>();
            transition.Configure(f6,Find<CanvasGroup>("CinematicFade"));Float(transition,"delayBeforeFade",4);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(Scene07Builder.ScenePath);
            if(UnityEngine.Object.FindFirstObjectByType<ExperimentPresentation>()!=null)return;
            Move("Lesson",.035f,.93f,.7f,.98f);Move("Objective",.035f,.875f,.70f,.927f);
            Find<Text>("Lesson").text="07 / THE EXPERIMENT";
            Find<Text>("Objective").text="Change the voltage. Keep the resistor. Gather evidence.";
            Move("AskWattson",.77f,.89f,.965f,.97f);
            Move("ObservationTablePanel",.705f,.36f,.965f,.85f);
            Move("MeterUIPanel",.035f,.74f,.675f,.85f);
            Move("MeterUITitle",.025f,.64f,.975f,.95f);
            Find<Text>("MeterUITitle").text="LIVE METERS / FIXED 10 Ω RESISTOR";
            Move("VoltmeterReadout",.025f,.10f,.22f,.64f);Move("VLabel",.235f,.10f,.49f,.64f);
            Find<Text>("VLabel").text="Voltage across resistor";
            Move("AmmeterReadout",.52f,.10f,.715f,.64f);Move("ILabel",.735f,.10f,.98f,.64f);
            Find<Text>("ILabel").text="Current in series";
            Move("ModelNote",.04f,.367f,.68f,.40f);
            Find<Text>("ModelNote").text="IDEAL CIRCUIT • CONSTANT TEMPERATURE • MOTION NOT TO SCALE";
            Find<Text>("ModelNote").fontSize=14;
            Move("HintUI",.04f,.32f,.96f,.36f);
            Move("DialoguePanel",.035f,.20f,.965f,.31f);
            Move("CircuitControlBar",.035f,.065f,.965f,.185f);
            Move("QuestionPanel",.035f,.065f,.965f,.185f);
            Move("Completion",.035f,.065f,.965f,.185f);
            Find<Text>("CompletionText").text="Evidence collected. Turn your measurements into a graph.";
            Find<Button>("Continue").GetComponentInChildren<Text>().text="PLOT THE EVIDENCE";
            Move("ToggleSwitch",.02f,.15f,.30f,.85f);Move("VoltageGroup",.33f,.15f,.74f,.85f);
            var record=Find<Button>("RecordReading");record.transform.SetParent(Find<RectTransform>("CircuitControlBar"),false);Set((RectTransform)record.transform,.77f,.15f,.98f,.85f);
            record.GetComponentInChildren<Text>().fontSize=22;
            Move("TableProgress",.05f,.13f,.95f,.23f);Move("TableFeedback",.05f,.01f,.95f,.125f);
            Find<Text>("TableFeedback").fontSize=17;
            var safe=Find<RectTransform>("SafeArea");
            var viewport=Area("ApparatusViewport",safe,.035f,.405f,.685f,.715f);
            var step=Label("ExperimentStep",safe,"PREPARING YOUR EXPERIMENT",19,.04f,.706f,.68f,.74f);
            var source=Label("SourceState",safe,"SOURCE 1 V / CIRCUIT OPEN",18,.04f,.40f,.68f,.43f);
            var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();camera.backgroundColor=new Color(.022f,.045f,.067f);camera.clearFlags=CameraClearFlags.SolidColor;
            if(camera.GetComponent<PhysicsRaycaster>()==null)camera.gameObject.AddComponent<PhysicsRaycaster>();
            var flow=UnityEngine.Object.FindFirstObjectByType<Scene07FlowManager>();
            var presentation=flow.gameObject.AddComponent<ExperimentPresentation>();
            presentation.Configure(camera,viewport,flow.Experiment.Circuit,source,step,Find<RectTransform>("CircuitControlBar").gameObject,Find<Button>("ToggleSwitch"),record,new[]{Find<Button>("Voltage1V"),Find<Button>("Voltage2V"),Find<Button>("Voltage3V")});flow.SetPresentation(presentation);
            var wattson=UnityEngine.Object.FindFirstObjectByType<WattsonController>();wattson.transform.position=new Vector3(-4.3f,1.2f,1.2f);wattson.transform.localScale=Vector3.one*.6f;
            var sw=UnityEngine.Object.FindFirstObjectByType<CircuitSwitchInteraction>();var so=new SerializedObject(sw);so.FindProperty("openEuler").vector3Value=new Vector3(0,0,35);so.ApplyModifiedPropertiesWithoutUndo();
            var drift=UnityEngine.Object.FindFirstObjectByType<ChargeDriftVisualization>();var ds=new SerializedObject(drift);ds.FindProperty("bounds").vector3Value=new Vector3(.9f,.3f,.3f);ds.ApplyModifiedPropertiesWithoutUndo();
            Find<Transform>("DriftTube").gameObject.SetActive(false);
            Find<Transform>("Wires").gameObject.SetActive(false);
            var wires=new GameObject("ConnectedWires");wires.transform.SetParent(Find<Transform>("Circuit"),false);
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/AdaptiveLearning/Materials/CircuitWire.mat");
            var sense=AssetDatabase.LoadAssetAtPath<Material>("Assets/AdaptiveLearning/Materials/SourceAmber.mat")??mat;
            Wire(wires.transform,mat,new Vector3(-2.1f,.77f,2),new Vector3(-2.1f,.12f,2.6f),new Vector3(-1.4f,.22f,2.6f));
            Wire(wires.transform,mat,new Vector3(-.6f,.22f,2.6f),new Vector3(.4f,.22f,2.6f),new Vector3(.45f,.65f,2.5f));
            Wire(wires.transform,mat,new Vector3(1.55f,.65f,2.5f),new Vector3(2.6f,.2f,2.5f),new Vector3(2.6f,.5f,1.7f));
            Wire(wires.transform,mat,new Vector3(2.6f,.5f,.7f),new Vector3(2.6f,.12f,-.55f),new Vector3(-3.4f,.12f,-.55f),new Vector3(-3.4f,.12f,2),new Vector3(-3.1f,.77f,2));
            Wire(wires.transform,sense,new Vector3(2.6f,.5f,1.7f),new Vector3(3.1f,.18f,1.7f),new Vector3(3.1f,.18f,.1f),new Vector3(1.15f,.60f,.2f));
            Wire(wires.transform,sense,new Vector3(2.6f,.5f,.7f),new Vector3(2.3f,.18f,-.15f),new Vector3(.05f,.18f,-.15f),new Vector3(.05f,.60f,.2f));
            var q=AssetDatabase.LoadAssetAtPath<QuestionDefinition>("Assets/AdaptiveLearning/ScriptableObjects/Questions/Scene07EvidenceQuestion.asset");q.concepts=new[]{ConceptId.VoltageCurrentRelationship,ConceptId.Voltage,ConceptId.Current};EditorUtility.SetDirty(q);
            var d=AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/AdaptiveLearning/ScriptableObjects/Dialogue/Scene07Dialogue.asset");
            d.Get("final_handoff").text="Let's turn these measurements into a graph and look for a pattern.";d.Get("final_handoff").duration=4;EditorUtility.SetDirty(d);
            foreach(var t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Max(14,t.fontSize-5);t.resizeTextMaxSize=t.fontSize;}
            foreach(var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None)){var o=b.GetComponent<Outline>()??b.gameObject.AddComponent<Outline>();o.effectColor=new Color(.25f,.9f,.9f);o.effectDistance=new Vector2(2,-2);o.enabled=false;}
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        static void Wire(Transform parent,Material material,params Vector3[] points)
        {for(int i=1;i<points.Length;i++){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="Connection";g.transform.SetParent(parent,false);var delta=points[i]-points[i-1];g.transform.localPosition=(points[i]+points[i-1])*.5f;g.transform.up=delta.normalized;g.transform.localScale=new Vector3(.055f,delta.magnitude*.5f,.055f);g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}}
    }
}


