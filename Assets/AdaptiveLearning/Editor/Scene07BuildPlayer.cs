using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AdaptiveOhmsLaw.Editor
{
    public static class Scene07BuildPlayer
    {
        [MenuItem("Adaptive Learning/Build Lab Scenes 08–11/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Scene07-Windows/AdaptiveOhmsLaw.exe");

        [MenuItem("Adaptive Learning/Build Lab Scenes 08–11/Android Tablet")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/Scene07-Android/AdaptiveOhmsLaw.apk");

        static void Build(BuildTarget target, string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Include Scene 06 and Scene 07 so continuous flow can be played
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/AdaptiveLearning/Scenes/Scene_08_OurLabIntroduction.unity", "Assets/AdaptiveLearning/Scenes/Scene_09_BuildTheCircuit.unity", Scene07Builder.ScenePath, Scene08Builder.ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.Development
            });

            string summary = target + ": " + report.summary.result + "; errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings
                + "; duration=" + report.summary.totalTime + "; output=" + path;
            File.WriteAllText("Logs/Scene07-build-" + target + ".txt", summary);

            if (report.summary.result != BuildResult.Succeeded)
                Debug.LogError(summary);
            else
                Debug.Log(summary);
        }
    }
}

