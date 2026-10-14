using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Menu items for the blockout scenes, plus a request-file driver so an open editor can be built from outside:
    // write a scene id (or "all") to Logs/ohm-build.req; the result lands in Logs/ohm-build.txt.
    [InitializeOnLoad]
    public static class BlockoutCommands
    {
        static BlockoutCommands()
        {
            EditorApplication.update += CheckRequests;
            // Stamp every domain reload, so an outside driver knows when the editor has finished recompiling.
            try { Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/ohm-reload.txt", DateTime.Now.ToString("o")); } catch { }
        }

        [MenuItem("Ohm's Law/Build Scene 00 (framework test)")] static void B00() => Build("Scene00Test", true);
        [MenuItem("Ohm's Law/Build Scene 05")] static void B05() => Build("Scene05", true);
        [MenuItem("Ohm's Law/Build Scene 06")] static void B06() => Build("Scene06", true);
        [MenuItem("Ohm's Law/Build Scene 07")] static void B07() => Build("Scene07", true);
        [MenuItem("Ohm's Law/Build Scene 08")] static void B08() => Build("Scene08", true);
        [MenuItem("Ohm's Law/Build Scene 09")] static void B09() => Build("Scene09", true);
        [MenuItem("Ohm's Law/Build Scene 10")] static void B10() => Build("Scene10", true);
        [MenuItem("Ohm's Law/Build Scene 11")] static void B11() => Build("Scene11", true);
        [MenuItem("Ohm's Law/Build Scene 12")] static void B12() => Build("Scene12", true);
        [MenuItem("Ohm's Law/Build Scene 13")] static void B13() => Build("Scene13", true);
        [MenuItem("Ohm's Law/Build All Blockout Scenes")] static void BAll() => BuildAll();

        public static SceneBuilderBase Build(string sceneId, bool open = false)
        {
            var builder = BlockoutAssets.Builders().FirstOrDefault(b => b.SceneId == sceneId);
            if (builder == null) throw new ArgumentException("No builder for " + sceneId);
            builder.Build();
            if (open && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(builder.ScenePath);
            return builder;
        }

        // Batchmode entry: -executeMethod OhmsLaw.Editor.BlockoutCommands.BuildAll
        public static void BuildAll()
        {
            foreach (var builder in BlockoutAssets.Builders().Where(b => b.SceneId != "Scene00Test")) builder.Build();
        }

        // Batchmode entry for build requests with the editor closed: -executeMethod OhmsLaw.Editor.BlockoutCommands.Batch
        // -ohmRequests "Scene08|Scene09|menu Ohm's Law/Polish/Apply lab (Scenes 8-13)|stills". "stills" renders LabShots.
        public static void Batch()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-ohmRequests");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("-ohmRequests \"A|B|…\" missing");
            foreach (var request in args[at + 1].Split('|').Select(r => r.Trim()).Where(r => r.Length > 0))
            {
                if (request == "stills") LabShots.Run();
                else if (request.StartsWith("menu ")) { if (!EditorApplication.ExecuteMenuItem(request.Substring(5).Trim())) throw new ArgumentException("No menu item " + request.Substring(5)); }
                else Build(request);
                Debug.Log("OHM-BATCH OK " + request);
            }
        }

        static void CheckRequests()
        {
            if (EditorApplication.isPlaying && File.Exists("Logs/ohm-stop.req")) { File.Delete("Logs/ohm-stop.req"); EditorApplication.ExitPlaymode(); return; }
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists("Logs/ohm-play.req"))
            {
                string scene = File.ReadAllText("Logs/ohm-play.req").Trim();
                File.Delete("Logs/ohm-play.req");
                BlockoutPlayTest.Run(scene);
                return;
            }
            if (!File.Exists("Logs/ohm-build.req")) return;
            string request = File.ReadAllText("Logs/ohm-build.req").Trim();
            File.Delete("Logs/ohm-build.req");
            try
            {
                string log = "";
                Application.LogCallback capture = (message, stack, type) => log += (type == LogType.Log ? "" : type + ": ") + message + "\n" + (type == LogType.Exception ? stack + "\n" : "");
                Application.logMessageReceived += capture;
                try
                {
                    if (request == "refresh") { AssetDatabase.Refresh(); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); } // an unfocused editor skips its own refresh
                    else if (request == "all") BuildAll();
                    else if (request == "report") BlockoutReport.Write();
                    else if (request == "flow") FlowDocument.Write();
                    else if (request == "inventory") AssetInventory.Write();
                    else if (request == "player") ModulePlayerBuilder.Build();
                    else if (request.StartsWith("hierarchy ")) AssetInventory.Hierarchy(request.Substring(10).Trim());
                    else if (request.StartsWith("menu ")) { if (!EditorApplication.ExecuteMenuItem(request.Substring(5).Trim())) throw new ArgumentException("No menu item " + request.Substring(5)); } // editor self-checks
                    else if (request.StartsWith("vo ")) { if (request == "vo all") BakeBlockoutVO.All(); else BakeBlockoutVO.Bake(request.Substring(3).Trim()); } // Scene 4 has no SceneBuilderBase
                    else Build(request);
                }
                finally { Application.logMessageReceived -= capture; }
                File.WriteAllText("Logs/ohm-build.txt", "OK " + DateTime.Now + "\n" + log);
            }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Logs/ohm-build.txt", "ERROR " + e); }
        }
    }
}
