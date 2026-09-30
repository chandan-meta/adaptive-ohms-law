using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class ImmersiveLessonRevision
    {
        const string ChamberPath="Assets/AdaptiveLearning/Prefabs/Learning/ConductorChamber.prefab";
        static ImmersiveLessonRevision(){EditorApplication.update+=()=>{
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Logs/immersive-polish.req")){File.Delete("Logs/immersive-polish.req");try{Polish();File.WriteAllText("Logs/immersive-polish.txt","COMPLETE");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/immersive-polish.txt",e.ToString());}}
            if(File.Exists("Logs/immersive-build.req")){File.Delete("Logs/immersive-build.req");try{Apply();File.WriteAllText("Logs/immersive-build.txt","COMPLETE");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/immersive-build.txt",e.ToString());}}
        };}
        static void Polish()
        {
            foreach(var path in new[]{Scene07Builder.ScenePath,Scene08Builder.ScenePath}){
                EditorSceneManager.OpenScene(path);
                F<Transform>("Wattson").position=new Vector3(1.6f,1.4f,3.5f);
                F<InstrumentReadouts>("Circuit").ConfigureVisuals(F<MeterDisplay>("Ammeter"),F<MeterDisplay>("Voltmeter"),F<Transform>("VoltageKnobModel"),F<CircuitSwitchInteraction>("Switch"));
                F<Transform>("Resistor").GetComponentInChildren<TextMesh>().transform.localScale=Vector3.one*.035f;
                F<Transform>("Switch").GetComponentInChildren<TextMesh>().transform.localScale=Vector3.one*.035f;
                foreach(var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name=="DigitalReadout")){
                    var position=label.transform.localPosition;position.y=-.22f;label.transform.localPosition=position;
                }
                if(path==Scene07Builder.ScenePath){Move("ExperimentStep",.03f,.83f,.29f,.89f);Move("MeterUIPanel",.30f,.83f,.61f,.895f);Move("ObservationTablePanel",.81f,.60f,.97f,.885f);UnityEngine.Object.FindFirstObjectByType<ExperimentPresentation>().SetRotary(F<RotaryDialGraphic>("VoltageKnob"));}
                else {
                    Move("GraphPanel",.035f,.42f,.575f,.825f);F<Image>("GraphPanel").color=new Color(.015f,.055f,.08f,.96f);
                    Move("DataProvenance",.035f,.83f,.60f,.875f);Move("GraphStatus",.04f,.375f,.60f,.415f);
                    for(int i=0;i<3;i++){Move("GraphReading"+i,.035f+i*.185f,.30f,.21f+i*.185f,.365f);var text=F<Button>("GraphReading"+i).GetComponentInChildren<Text>();text.fontSize=18;text.resizeTextMaxSize=18;text.resizeTextMinSize=14;}
                }
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            EditorSceneManager.OpenScene(Scene07Builder.ScenePath);
        }
        static T F<T>(string name)where T:Component=>LessonReviewTools.Find<T>(name);
        static void Move(string name,float x,float y,float r,float t)=>LessonReviewTools.Set(F<RectTransform>(name),x,y,r,t);
        static Text Label(string name,Transform parent,string text,int size,float x,float y,float r,float t)=>LessonReviewTools.Label(name,parent,text,size,x,y,r,t);
        static void Hide(string name)=>F<Transform>(name).gameObject.SetActive(false);
        static Material Mat(string name,Color color,bool glow=false)
        {
            string path="Assets/AdaptiveLearning/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;mat.SetFloat("_Metallic",.4f);mat.SetFloat("_Smoothness",.55f);if(glow){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.6f);}return mat;
        }
        static GameObject Shape(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static TextMesh WorldText(string name,Transform parent,string text,Vector3 position,float scale,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*scale;
            var label=go.AddComponent<TextMesh>();label.fontSize=64;label.characterSize=1;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=color;label.text=text;return label;
        }
        static void Common(int scene)
        {
            Move("Lesson",.03f,.935f,.72f,.985f);F<Text>("Lesson").text="OHM / FIELD LAB     •     "+scene.ToString("00")+" / "+(scene==6?"PREDICT":scene==7?"EXPERIMENT":"DISCOVER");F<Text>("Lesson").fontSize=22;
            Move("Objective",.03f,.895f,.72f,.935f);F<Text>("Objective").fontSize=18;
            Move("AskWattson",.82f,.91f,.97f,.98f);F<Button>("AskWattson").GetComponentInChildren<Text>().fontSize=20;
            Move("DialoguePanel",.03f,.055f,.97f,.16f);Move("QuestionPanel",.03f,.18f,.97f,.275f);Move("Completion",.03f,.18f,.97f,.275f);
            F<Image>("DialoguePanel").color=new Color(.018f,.045f,.065f,.84f);F<Image>("QuestionPanel").color=Color.clear;F<Image>("QuestionPanel").raycastTarget=false;
            F<Image>("Completion").color=new Color(.018f,.045f,.065f,.85f);F<Text>("Subtitle").fontSize=23;
            Move("Support",.03f,.012f,.45f,.043f);Move("LearningProgress",.49f,.012f,.97f,.043f);
            F<Text>("Support").fontSize=14;F<Text>("LearningProgress").fontSize=13;
            Move("HintUI",.25f,.285f,.77f,.345f);F<Text>("HintUI").fontSize=18;
            F<Transform>("WattsonChatPanel").SetAsLastSibling();
            foreach(var t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None)){t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Max(13,t.fontSize-4);t.resizeTextMaxSize=t.fontSize;}
        }
        static void Chamber()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ChamberPath);var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name="ConductorChamber";
            go.transform.SetParent(F<Transform>("Environment"),false);
            Hide("Backdrop");RenderSettings.fogColor=new Color(.012f,.035f,.055f);RenderSettings.fogDensity=.022f;
            F<Transform>("Wattson").position=new Vector3(3.9f,1.65f,2.8f);F<Transform>("Wattson").localScale=Vector3.one*.8f;
        }
        static Transform RefineInstruments()
        {
            var source=F<Transform>("VoltageSource");var details=new GameObject("PrecisionInstrumentDetails");details.transform.SetParent(source,false);
            var dark=Mat("InstrumentGraphite",new Color(.075f,.12f,.15f));var silver=Mat("InstrumentAlloy",new Color(.42f,.53f,.57f));var cyan=Mat("InstrumentCyan",new Color(.1f,.7f,.8f),true);var amber=Mat("InstrumentAmber",new Color(1,.55f,.1f),true);
            source.Find("SourceBody").GetComponent<Renderer>().sharedMaterial=silver;
            var old=source.Find("ApparatusLabel");old.localPosition=new Vector3(0,.21f,-.66f);old.localScale=Vector3.one*.021f;old.GetComponent<TextMesh>().text="DC POWER SUPPLY";
            var pivot=new GameObject("VoltageKnobModel").transform;pivot.SetParent(details.transform,false);pivot.localPosition=new Vector3(-.43f,-.14f,-.69f);
            Shape("KnobBezel",Primitive(pivot),PrimitiveType.Cylinder,Vector3.zero,new Vector3(.43f,.055f,.43f),silver).transform.localRotation=Quaternion.Euler(90,0,0);
            Shape("KnurledKnob",pivot,PrimitiveType.Cylinder,new Vector3(0,0,-.055f),new Vector3(.34f,.075f,.34f),dark).transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<20;i++){float a=i*Mathf.PI*2/20;var grip=Shape("Grip",pivot,PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*.165f,Mathf.Sin(a)*.165f,-.08f),new Vector3(.025f,.045f,.10f),silver);grip.transform.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);}
            Shape("DialIndex",pivot,PrimitiveType.Cube,new Vector3(.105f,0,-.14f),new Vector3(.11f,.028f,.015f),amber);
            var sourceValue=WorldText("SupplyReadout",source,"1.0 V",new Vector3(.32f,-.1f,-.68f),.033f,new Color(.3f,1,.85f));
            var meterValues=new TextMesh[2];int meterIndex=0;
            foreach(var name in new[]{"Ammeter","Voltmeter"})
            {
                var meter=F<Transform>(name);var face=meter.Find("MeterFace");
                var decoration=new GameObject("PrecisionFace");decoration.transform.SetParent(meter,false);
                for(int i=0;i<=10;i++){float a=Mathf.Lerp(45,-45,i/10f)*Mathf.Deg2Rad;var tick=Shape("ScaleTick",decoration.transform,PrimitiveType.Cube,new Vector3(-Mathf.Sin(a)*.42f,-.15f+Mathf.Cos(a)*.42f,-.457f),new Vector3(.012f,i%5==0?.08f:.035f,.009f),dark);tick.transform.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);}
                meterValues[meterIndex++]=WorldText("DigitalReadout",meter,"0.00",new Vector3(0,-.31f,-.46f),.021f,new Color(.035f,.11f,.15f));
                for(int x=-1;x<=1;x+=2)Shape("BindingPost",meter,PrimitiveType.Sphere,new Vector3(x*.55f,.23f,0),Vector3.one*.14f,cyan);
            }
            var circuit=F<SimpleCircuitModel>("Circuit");var readouts=circuit.gameObject.AddComponent<InstrumentReadouts>();readouts.Configure(circuit,sourceValue,meterValues[0],meterValues[1]);
            var table=F<Transform>("Tabletop");table.GetComponent<Renderer>().sharedMaterial=dark;
            Shape("BenchLightEdge",table.parent,PrimitiveType.Cube,new Vector3(0,-.16f,-1.2f),new Vector3(9,.035f,.035f),cyan);
            return pivot;
        }
        static Transform Primitive(Transform t)=>t;
        [MenuItem("Adaptive Learning/Apply Immersive Simulation Revision")]
        public static void Apply()
        {
            Directory.CreateDirectory("Assets/AdaptiveLearning/Prefabs/Learning");
            EditorSceneManager.OpenScene(Scene06Builder.ScenePath);
            if(F<Transform>("Environment").Find("ImmersiveRevision")!=null)throw new InvalidOperationException("Revision already applied; edit the existing scenes.");
            PrefabUtility.SaveAsPrefabAsset(F<Transform>("Conductor").gameObject,ChamberPath);
            Common(6);Move("HintUI",.04f,.29f,.96f,.35f);Move("Voltage",.04f,.78f,.45f,.84f);Move("Condition",.04f,.735f,.58f,.78f);Move("ModelNote",.04f,.68f,.52f,.733f);
            F<Text>("Voltage").fontSize=22;F<Text>("Condition").fontSize=16;F<Text>("ModelNote").fontSize=15;
            new GameObject("ImmersiveRevision").transform.SetParent(F<Transform>("Environment"),false);EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(Scene07Builder.ScenePath);Common(7);Chamber();var knobModel=RefineInstruments();
            Move("ApparatusViewport",0,0,1,1);F<Transform>("ApparatusViewport").SetAsFirstSibling();
            Move("ObservationTablePanel",.80f,.545f,.97f,.855f);F<Image>("ObservationTablePanel").color=new Color(.018f,.055f,.075f,.86f);
            F<Text>("TableTitle").text="FIELD NOTES";F<Text>("TableTitle").fontSize=19;F<Text>("TableSubtitle").fontSize=14;F<Text>("TableFeedback").fontSize=14;F<Text>("TableProgress").fontSize=14;
            Move("MeterUIPanel",.31f,.83f,.70f,.895f);Hide("MeterUITitle");F<Image>("MeterUIPanel").color=new Color(.015f,.045f,.06f,.7f);
            Move("VoltmeterReadout",.03f,.07f,.25f,.93f);Move("VLabel",.26f,.07f,.49f,.93f);F<Text>("VLabel").text="VOLTAGE";
            Move("AmmeterReadout",.52f,.07f,.75f,.93f);Move("ILabel",.76f,.07f,.99f,.93f);F<Text>("ILabel").text="CURRENT";
            Move("ExperimentStep",.03f,.85f,.65f,.888f);F<Text>("ExperimentStep").fontSize=15;
            Move("SourceState",.04f,.68f,.37f,.72f);F<Text>("SourceState").fontSize=16;
            Move("ModelNote",.04f,.64f,.41f,.68f);F<Text>("ModelNote").text="10 Ω • CONSTANT TEMPERATURE";
            Move("CircuitControlBar",0,0,1,1);F<Image>("CircuitControlBar").color=Color.clear;F<Image>("CircuitControlBar").raycastTarget=false;
            Move("ToggleSwitch",.56f,.18f,.755f,.255f);Move("RecordReading",.775f,.18f,.97f,.255f);Hide("VoltageGroup");
            var controls=F<RectTransform>("CircuitControlBar");var knobRect=LessonReviewTools.Area("VoltageKnob",controls,.125f,.43f,.125f,.43f);knobRect.sizeDelta=new Vector2(240,240);
            var dial=knobRect.gameObject.AddComponent<RotaryDialGraphic>();dial.raycastTarget=true;
            var value=Label("KnobValue",knobRect,"1.0 V",27,.22f,.35f,.78f,.65f);value.alignment=TextAnchor.MiddleCenter;
            var title=Label("KnobTitle",knobRect,"SOURCE VOLTAGE",18,-.12f,1.02f,1.12f,1.18f);title.alignment=TextAnchor.MiddleCenter;
            var help=Label("KnobHelp",knobRect,"DRAG TO TURN\n0–5 V • snaps at each whole volt",15,-.20f,-.22f,1.20f,-.02f);help.alignment=TextAnchor.MiddleCenter;
            for(int i=0;i<=5;i++){float a=(135-i*54)*Mathf.Deg2Rad;float x=.5f+Mathf.Cos(a)*.61f,y=.5f+Mathf.Sin(a)*.61f;var tick=Label("Setting"+i,knobRect,i.ToString(),17,x-.07f,y-.06f,x+.07f,y+.06f);tick.alignment=TextAnchor.MiddleCenter;}
            var rotary=knobRect.gameObject.AddComponent<RotaryVoltageControl>();var experiment=UnityEngine.Object.FindFirstObjectByType<CircuitExperimentController>();rotary.Configure(experiment,dial,value,knobModel);experiment.SetKnob(rotary);
            F<Transform>("WattsonChatPanel").SetAsLastSibling();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(Scene08Builder.ScenePath);Common(8);F<Transform>("ExperimentStation").gameObject.SetActive(true);Chamber();RefineInstruments();F<SimpleCircuitModel>("Circuit").Configure(10,3,true);
            Move("CompanionViewport",0,0,1,1);F<Transform>("CompanionViewport").SetAsFirstSibling();
            Move("GraphPanel",.035f,.37f,.575f,.825f);F<Image>("GraphPanel").color=new Color(.015f,.055f,.08f,.88f);F<Text>("GraphTitle").text="WATTSON / EVIDENCE DISPLAY";F<Text>("GraphTitle").fontSize=18;
            Move("DataProvenance",.76f,.56f,.97f,.61f);F<Text>("DataProvenance").fontSize=14;
            for(int i=0;i<3;i++)Move("GraphReading"+i,.76f,.475f-i*.075f,.97f,.54f-i*.075f);
            Move("GraphStatus",.04f,.326f,.60f,.365f);F<Text>("GraphStatus").fontSize=16;
            Move("GraphHint",.79f,.18f,.97f,.255f);Move("HintUI",.04f,.18f,.75f,.27f);
            F<Transform>("WattsonChatPanel").SetAsLastSibling();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            var dialogue=AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/AdaptiveLearning/ScriptableObjects/Dialogue/Scene07Dialogue.asset");
            foreach(var line in dialogue.lines){line.text=line.text.Replace("select 1 V","turn the knob to 1 V").Replace("select 1","turn the knob to 1").Replace("Try another voltage setting next.","Turn the knob to the next voltage.");}EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Scene07Builder.ScenePath);
        }
    }
}

