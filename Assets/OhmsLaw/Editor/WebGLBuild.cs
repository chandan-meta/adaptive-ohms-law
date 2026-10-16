using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // The module for the web: Scene 00 and Scenes 04-13 (the older AdaptiveLearning scenes in the build list stay out) into
    // Builds/WebGL with the OhmsLaw template (Assets/WebGLTemplates/OhmsLaw: the module's loading screen). Brotli (Gzip left
    // the data file at 114 MB, over GitHub's 100 MB file limit) with decompression fallback: GitHub Pages serves the files
    // without a Content-Encoding header, so the loader inflates them itself. Writes Logs/WebGL/build.txt (result, file sizes, the largest assets). In an editor on another platform the
    // first run only switches it to WebGL (a reimport and a domain reload); run it again to build.
    public static class WebGLBuild
    {
        const string Output = "Builds/WebGL";

        [MenuItem("Ohm's Law/Build WebGL")]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
                Debug.Log("WebGL build: the editor is switching to WebGL; run Ohm's Law/Build WebGL again to build.");
                return;
            }
            Slim();
            PlayerSettings.WebGL.template = "PROJECT:OhmsLaw";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            var scenes = new[] { ModulePlayerBuilder.ScenePath }
                .Concat(BlockoutAssets.SceneOrder.Select(s => SceneBuilderBase.Root + "/Scenes/" + s + ".unity")).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = Output, target = BuildTarget.WebGL });

            var log = new StringBuilder($"WebGL build {report.summary.result} in {report.summary.totalTime:hh\\:mm\\:ss}, {report.summary.totalErrors} errors\n");
            foreach (var file in Directory.GetFiles(Output, "*", SearchOption.AllDirectories).OrderByDescending(f => new FileInfo(f).Length))
                log.AppendLine($"{new FileInfo(file).Length / 1048576f,9:F2} MB  {file.Replace('\\', '/')}");
            log.AppendLine("\nLargest assets in the build (uncompressed):");
            foreach (var asset in report.packedAssets.SelectMany(p => p.contents).GroupBy(c => c.sourceAssetPath)
                         .Select(g => (path: g.Key, size: g.Sum(c => (long)c.packedSize))).OrderByDescending(a => a.size).Take(40))
                log.AppendLine($"{asset.size / 1048576f,9:F2} MB  {asset.path}");
            Directory.CreateDirectory("Logs/WebGL");
            File.WriteAllText("Logs/WebGL/build.txt", log.ToString());
            Debug.Log(log.ToString());
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("WebGL build failed: see Logs/WebGL/build.txt and the console");
        }

        // The web build's weight was the rooms' textures (the lab set's and the study's, 2048 px: the data file came to 212 MB,
        // over GitHub's 100 MB file limit) and the lab set's 774k-vertex mesh. On WebGL only, the rooms' textures are 1024 px
        // (a 1080p canvas never shows a room surface near 2048 texels) and crunched, their normal maps 512, the lab mesh is compressed
        // and the Unity splash is off (Unity 6 lets every plan drop it; the module opens on its own title); the apparatus,
        // the characters and the UI keep their textures.
        static readonly string[] RoomTextures = { "Assets/OhmsLaw/Shared/Art/LabSet/Textures", "Assets/AdaptiveLearning/Art/env" };

        static void Slim()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", RoomTextures).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                var web = importer.GetPlatformTextureSettings("WebGL");
                bool normal = importer.textureType == TextureImporterType.NormalMap;
                int size = normal ? 512 : 1024; // relief that far off needs less
                if (web.overridden && web.maxTextureSize <= size && web.crunchedCompression == !normal) continue;
                web.overridden = true;
                web.maxTextureSize = size;
                web.crunchedCompression = !normal; // the colour maps crunched at 75: Brotli alone left the data at 102.75 MiB
                web.compressionQuality = 75;
                importer.SetPlatformTextureSettings(web);
                importer.SaveAndReimport();
            }
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            // A material's legacy _MainTex (re-synced from the apparatus FBX on reimport) is never drawn by URP but still ships
            // its texture: the four kit dials, 11 MB. Pointed at the base map it ships nothing extra. The wire set is not touched.
            foreach (var path in AssetDatabase.FindAssets("t:Material", new[] { "Assets/OhmsLaw", "Assets/AdaptiveLearning" })
                         .Select(AssetDatabase.GUIDToAssetPath).Where(p => !p.Contains("/WireSet/")))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat || !mat.HasProperty("_BaseMap") || !mat.HasProperty("_MainTex")) continue;
                var baseMap = mat.GetTexture("_BaseMap");
                if (mat.GetTexture("_MainTex") == baseMap) continue;
                mat.SetTexture("_MainTex", baseMap);
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            if (AssetImporter.GetAtPath(BlenderSet.Lab.Model) is ModelImporter lab && lab.meshCompression != ModelImporterMeshCompression.Medium)
            {
                lab.meshCompression = ModelImporterMeshCompression.Medium;
                lab.SaveAndReimport();
            }
        }
    }
}
