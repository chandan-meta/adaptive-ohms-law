#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace AdaptiveOhmsLaw.Editor
{
    public static class WebLessonBuild
    {
        [MenuItem("Adaptive Learning/Build Complete Web Lesson")]
        public static void Run()
        {
            var scenes = Directory.GetFiles("Assets/AdaptiveLearning/Scenes", "*.unity").OrderBy(p => p, StringComparer.Ordinal).ToArray();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.companyName = "Adaptive Learning";
            PlayerSettings.productName = "Ohm's Law - Wattson Lab";
            var output = Environment.GetEnvironmentVariable("OHM_WEB_OUTPUT");
            if (string.IsNullOrEmpty(output)) output = "Builds/WebGL";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, target = BuildTarget.WebGL, locationPathName = output, options = BuildOptions.None });
            File.WriteAllText("Logs/WebGL-build-result.txt", report.summary.result + " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings + "\n" + string.Join("\n",scenes));
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("WebGL build failed. Inspect build log.");
            // Distribute the OFL notices alongside the fonts embedded in the player.
            var licenses = Path.Combine(output, "Licenses"); Directory.CreateDirectory(licenses);
            foreach (var name in new[] { "NunitoSans-OFL.txt", "SourceSans-LICENSE.txt", "SOURCES.txt" })
                File.Copy("Assets/AdaptiveLearning/Art/Interface/Fonts/" + name, Path.Combine(licenses, name), true);
        }
    }
}
#endif
