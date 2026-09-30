using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AdaptiveOhmsLaw.Editor
{
    public static class Scene06BuildPlayer
    {
        [MenuItem("Adaptive Learning/Build Scene 06/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64,"Builds/Scene06-Windows/AdaptiveOhmsLaw.exe");
        [MenuItem("Adaptive Learning/Build Scene 06/Android Tablet")]
        public static void BuildAndroid() => Build(BuildTarget.Android,"Builds/Scene06-Android/AdaptiveOhmsLaw.apk");
        static void Build(BuildTarget target,string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Scene06Builder.ScenePath }, locationPathName = path, target = target, options = BuildOptions.Development });
            string summary = target + ": " + report.summary.result + "; errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings
                + "; duration=" + report.summary.totalTime + "; output=" + path;
            File.WriteAllText("Logs/Scene06-build-" + target + ".txt",summary);
            if (report.summary.result != BuildResult.Succeeded) Debug.LogError(summary); else Debug.Log(summary);
        }
    }
}
