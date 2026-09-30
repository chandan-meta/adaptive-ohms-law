using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class Scene08VisualReview
    {
        static Scene08VisualReview(){EditorApplication.update+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Logs/scene08-review.req")){File.Delete("Logs/scene08-review.req");Apply();}};}
        public static void Apply()
        {
            EditorSceneManager.OpenScene(Scene08Builder.ScenePath);
            LessonReviewTools.Find<UnityEngine.UI.InputField>("MessageInput").placeholder.GetComponent<UnityEngine.UI.Text>().text="Ask about axes, plotting or the pattern…";
            var grid=UnityEngine.Object.FindFirstObjectByType<GraphGridGraphic>();if(grid.GetComponent<CanvasRenderer>()==null)grid.gameObject.AddComponent<CanvasRenderer>();grid.Refresh();
            LessonReviewTools.Find<Transform>("ExperimentStation").gameObject.SetActive(false);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(Scene07Builder.ScenePath);
            LessonReviewTools.Find<Transform>("WattsonChatPanel").SetAsLastSibling();
            var label=LessonReviewTools.Find<Transform>("VoltageSource").Find("ApparatusLabel");label.localScale=Vector3.one*.043f;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            var dialogue=AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/AdaptiveLearning/ScriptableObjects/Dialogue/Scene06Dialogue.asset");
            dialogue.Get("complete").text="Prediction recorded. Let's test it in Ohm's experiment.";dialogue.Get("complete").duration=4;EditorUtility.SetDirty(dialogue);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/scene08-review.txt","COMPLETE");
        }
    }
}


