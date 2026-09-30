using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class Scene07Refinement
    {
        static Scene07Refinement(){EditorApplication.update+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Logs/scene07-refine.req")){File.Delete("Logs/scene07-refine.req");Apply();}};}
        public static void Apply()
        {
            EditorSceneManager.OpenScene(Scene07Builder.ScenePath);
            Label("VoltageSource","SOURCE",new Vector3(0,.1f,-.65f),.065f);
            Label("Ammeter","A",new Vector3(0,.25f,-.455f),.055f);
            Label("Voltmeter","V",new Vector3(0,.25f,-.455f),.055f);
            Label("Resistor","10 Ω",new Vector3(0,.75f,0),.06f);
            Label("Switch","SWITCH",new Vector3(0,.6f,0),.045f);
            var f=Object.FindFirstObjectByType<Scene07FlowManager>();
            // Hide disconnected demonstration charges: circuit readings are the focus here.
            Object.FindFirstObjectByType<ChargeDriftVisualization>(FindObjectsInactive.Include).gameObject.SetActive(false);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        static void Label(string parent,string text,Vector3 position,float scale)
        {
            var root=LessonReviewTools.Find<Transform>(parent);if(root.Find("ApparatusLabel")!=null){root.Find("ApparatusLabel").GetComponent<TextMesh>().characterSize=1;return;}
            var g=new GameObject("ApparatusLabel");g.transform.SetParent(root,false);g.transform.localPosition=position;g.transform.localScale=Vector3.one*scale;
            var t=g.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=1;t.anchor=TextAnchor.MiddleCenter;t.color=parent=="Ammeter"||parent=="Voltmeter"?new Color(.05f,.12f,.17f):Color.white;
        }
    }
}


