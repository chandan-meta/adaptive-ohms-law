using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class LessonContinuityValidation
    {
        static LessonContinuityValidation()
        {
            EditorApplication.update+=()=>
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
                if(File.Exists("Logs/rotary-validate.req")){File.Delete("Logs/rotary-validate.req");RunRotary();}
                if(File.Exists("Logs/graph-validate.req")){File.Delete("Logs/graph-validate.req");RunGraph();}
                if(File.Exists("Logs/lesson-validate.req")){File.Delete("Logs/lesson-validate.req");Run();}
                if(File.Exists("Logs/lesson-build-player.req")){File.Delete("Logs/lesson-build-player.req");Build();}
            };
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("LessonContinuityValidation",false))new GameObject("LessonContinuityRunner").AddComponent<LessonContinuityRunner>();
                if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool("LessonContinuityValidation",false);
            };
        }
        [MenuItem("Adaptive Learning/Validate Scenes 06 to 08")]
        public static void Run()
        {
            SessionState.SetBool("RotaryReview",false);
            SessionState.SetBool("GraphReview",false);
            EditorSceneManager.OpenScene(Scene06Builder.ScenePath);Scene07Validation.SetResolution(1920,1080);
            SessionState.SetBool("LessonContinuityValidation",true);EditorApplication.isPlaying=true;
        }
        [MenuItem("Adaptive Learning/Validate Scene 07 Rotary Controls")]
        public static void RunRotary()
        {
            SessionState.SetBool("RotaryReview",true);SessionState.SetBool("GraphReview",false);
            EditorSceneManager.OpenScene(Scene07Builder.ScenePath);Scene07Validation.SetResolution(1920,1080);
            SessionState.SetBool("LessonContinuityValidation",true);EditorApplication.isPlaying=true;
        }
        [MenuItem("Adaptive Learning/Validate Scene 08 Graph")]
        public static void RunGraph()
        {
            SessionState.SetBool("RotaryReview",false);
            EditorSceneManager.OpenScene(Scene08Builder.ScenePath);Scene07Validation.SetResolution(1920,1080);
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear").Invoke(null,null);
            SessionState.SetBool("LessonContinuityValidation",true);SessionState.SetBool("GraphReview",true);EditorApplication.isPlaying=true;
        }
        [MenuItem("Adaptive Learning/Build Scenes 06 to 08 Windows")]
        public static void Build()
        {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/AdaptiveLearning/Scenes/Scene_08_OurLabIntroduction.unity","Assets/AdaptiveLearning/Scenes/Scene_09_BuildTheCircuit.unity",Scene07Builder.ScenePath,Scene08Builder.ScenePath,Scene06Builder.ScenePath},locationPathName="Builds/Scenes06-08/AdaptiveOhmsLaw.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/Scenes06-08-build.txt",report.summary.result+"; errors="+report.summary.totalErrors+"; warnings="+report.summary.totalWarnings+"; duration="+report.summary.totalTime);
            using(var details=new StreamWriter("Logs/Scenes06-08-build-messages.txt"))foreach(var step in report.steps)foreach(var message in step.messages)if(message.type==LogType.Warning||message.type==LogType.Error||message.type==LogType.Exception)details.WriteLine(message.type+": "+message.content);
        }
    }
}


