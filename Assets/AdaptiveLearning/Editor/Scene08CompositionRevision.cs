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
    [InitializeOnLoad] public static class Scene08CompositionRevision
    {
        static Scene08CompositionRevision(){EditorApplication.update+=()=>{
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Logs/scene08-composition.req"))return;
            File.Delete("Logs/scene08-composition.req");try{Apply();File.WriteAllText("Logs/scene08-composition.txt","COMPLETE");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/scene08-composition.txt",e.ToString());}
        };}
        static T F<T>(string name)where T:Component=>LessonReviewTools.Find<T>(name);
        static void Move(string name,float x,float y,float r,float t)=>LessonReviewTools.Set(F<RectTransform>(name),x,y,r,t);
        static void Type(Text text,int size){text.fontSize=size;text.resizeTextMaxSize=size;text.resizeTextMinSize=Mathf.Max(14,size-3);text.resizeTextForBestFit=true;}
        static Material ChamberMaterial(string name,Color color,bool emissive)
        {
            string path="Assets/AdaptiveLearning/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetFloat("_Smoothness",.15f);
            if(emissive){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*.35f);}return material;
        }
        [MenuItem("Adaptive Learning/Apply Scene 08 Composition")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene(Scene08Builder.ScenePath);
            var bench=F<Transform>("ExperimentStation");
            UnityEngine.Object.FindFirstObjectByType<CompanionViewport>().ConfigureStudy(bench);
            foreach(var apparatusLabel in bench.GetComponentsInChildren<TextMesh>(true))apparatusLabel.gameObject.SetActive(apparatusLabel.text=="A"||apparatusLabel.text=="V");
            F<Transform>("BenchLightEdge").gameObject.SetActive(false);
            var dark=ChamberMaterial("StudyChamber",new Color(.016f,.036f,.052f),false);
            var rib=ChamberMaterial("StudyChamberTrim",new Color(.025f,.13f,.16f),true);
            foreach(var renderer in F<Transform>("ConductorChamber").GetComponentsInChildren<Renderer>())renderer.sharedMaterial=renderer.name.StartsWith("Rib")?rib:dark;
            Move("Lesson",.04f,.94f,.70f,.985f);F<Text>("Lesson").text="OHM / FIELD LAB     •     11 / FIND THE PATTERN";Type(F<Text>("Lesson"),22);
            Move("Objective",.04f,.892f,.73f,.935f);F<Text>("Objective").text="What do your measurements reveal?";Type(F<Text>("Objective"),23);
            Move("GraphPanel",.04f,.39f,.65f,.85f);
            var panel=F<Image>("GraphPanel");panel.color=new Color(.025f,.065f,.09f,1);panel.canvasRenderer.SetAlpha(1);
            var edge=panel.GetComponent<Outline>()??panel.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.12f,.3f,.34f);edge.effectDistance=new Vector2(1,-1);
            F<Text>("GraphTitle").text="Potential difference vs current";Type(F<Text>("GraphTitle"),24);Move("GraphTitle",.035f,.89f,.96f,.975f);
            Move("PlotArea",.14f,.16f,.955f,.79f);Move("YAxisTitle",.035f,.79f,.3f,.88f);Type(F<Text>("YAxisTitle"),18);Type(F<Text>("XAxisTitle"),18);
            Move("DataProvenance",.04f,.855f,.65f,.885f);Type(F<Text>("DataProvenance"),16);F<Text>("DataProvenance").text="MEASUREMENTS / FIXED 10 Ω";
            Move("GraphStatus",.04f,.27f,.65f,.305f);Type(F<Text>("GraphStatus"),20);F<Text>("GraphStatus").text="Place each reading on the graph";
            for(int i=0;i<3;i++){
                var button=F<Button>("GraphReading"+i);Move(button.name,.04f+i*.207f,.315f,.235f+i*.207f,.377f);
                var text=button.GetComponentInChildren<Text>();Type(text,21);text.text=(i+1).ToString("0.0")+" V   /   "+((i+1)*.1f).ToString("0.00")+" A";
            }
            foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(b=>b.name.StartsWith("Plot_"))){
                var previous=button.GetComponent<Image>();var marker=previous as GraphPointGraphic;
                if(marker==null){UnityEngine.Object.DestroyImmediate(previous);marker=button.gameObject.AddComponent<GraphPointGraphic>();}
                marker.color=new Color(.38f,.66f,.73f);marker.raycastTarget=true;button.targetGraphic=marker;
                button.GetComponentInChildren<Text>().text="";((RectTransform)button.transform).sizeDelta=new Vector2(68,68);
            }
            Move("HintUI",.04f,.185f,.65f,.26f);Type(F<Text>("HintUI"),21);F<Text>("HintUI").text="Choose a reading. Tap its position on the graph.";
            Move("GraphHint",.73f,.185f,.95f,.255f);F<Button>("GraphHint").GetComponentInChildren<Text>().text="NEED A CLUE?";
            var safe=F<Transform>("SafeArea");var label=UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="CircuitReferenceLabel");
            if(label==null)label=LessonReviewTools.Label("CircuitReferenceLabel",safe,"YOUR CIRCUIT  /  10 Ω",17,.69f,.465f,.96f,.505f);
            label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.5f,.69f,.73f);
            Move("DialoguePanel",.04f,.045f,.96f,.155f);F<Image>("DialoguePanel").color=new Color(.016f,.039f,.055f,1);
            F<Text>("Subtitle").text="Let's reveal the pattern in your measurements.";Type(F<Text>("Subtitle"),24);
            F<Transform>("Support").gameObject.SetActive(false);F<Transform>("LearningProgress").gameObject.SetActive(false);
            F<Transform>("WattsonChatPanel").SetAsLastSibling();
            Scene07Validation.SetResolution(1920,1080);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
    }
}


