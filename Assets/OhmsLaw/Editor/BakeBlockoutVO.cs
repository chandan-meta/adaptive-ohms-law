using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace OhmsLaw.Editor
{
    // Voice for the blockout scenes. The Wattson proxy's /tts when it is running (the real voice), otherwise the
    // OS text-to-speech as "scratch_<id>.wav", otherwise the line stays silent on its fallback duration.
    // Afterwards the scene is rebuilt so the Timeline follows the clip lengths.
    public static class BakeBlockoutVO
    {
        [MenuItem("Ohm's Law/Bake VO/All Scenes")]
        public static void All()
        {
            foreach (var builder in BlockoutAssets.Builders()) Bake(builder);
        }

        [MenuItem("Ohm's Law/Bake VO/Scene 05")] static void S05() => Bake("Scene05");
        [MenuItem("Ohm's Law/Bake VO/Scene 06")] static void S06() => Bake("Scene06");
        [MenuItem("Ohm's Law/Bake VO/Scene 07")] static void S07() => Bake("Scene07");
        [MenuItem("Ohm's Law/Bake VO/Scene 08")] static void S08() => Bake("Scene08");
        [MenuItem("Ohm's Law/Bake VO/Scene 09")] static void S09() => Bake("Scene09");
        [MenuItem("Ohm's Law/Bake VO/Scene 10")] static void S10() => Bake("Scene10");
        [MenuItem("Ohm's Law/Bake VO/Scene 11")] static void S11() => Bake("Scene11");
        [MenuItem("Ohm's Law/Bake VO/Scene 12")] static void S12() => Bake("Scene12");
        [MenuItem("Ohm's Law/Bake VO/Scene 13")] static void S13() => Bake("Scene13");
        [MenuItem("Ohm's Law/Bake VO/Test Scene")] static void Test() => Bake("Scene00Test");

        static void Bake(string sceneId)
        {
            var builder = BlockoutAssets.Builders().FirstOrDefault(b => b.SceneId == sceneId);
            if (builder == null) Debug.LogError("No builder for " + sceneId + ". Build that scene first so its lines exist.");
            else Bake(builder);
        }

        public static void Bake(SceneBuilderBase builder)
        {
            string folder = SceneBuilderBase.Root + "/" + builder.SceneId;
            var lines = AssetDatabase.FindAssets("t:DialogueLine", new[] { folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<DialogueLine>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(line => line && !string.IsNullOrWhiteSpace(line.Spoken)).ToArray();
            if (lines.Length == 0) { Debug.LogWarning(builder.SceneId + ": no dialogue lines yet. Build the scene first."); return; }
            Directory.CreateDirectory(folder + "/Audio/VO");

            var config = AssetDatabase.LoadAssetAtPath<ProxyConfig>(SceneBuilderBase.Shared + "/Agent/ProxyConfig.asset");
            bool proxy = config && ProxyAlive(config);
            int baked = 0, scratch = 0, silent = 0;
            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    EditorUtility.DisplayProgressBar("Bake VO " + builder.SceneId, line.id, i / (float)lines.Length);
                    // A real clip (named after the id, no scratch_ prefix) is never overwritten.
                    if (File.Exists(folder + "/Audio/VO/" + line.id + ".wav") || File.Exists(folder + "/Audio/VO/" + line.id + ".mp3")) { baked++; continue; }
                    string spoken = PronunciationLexicon.Apply(line.Spoken);
                    string path = proxy ? folder + "/Audio/VO/" + line.id + ".mp3" : folder + "/Audio/VO/scratch_" + line.id + ".wav";
                    bool ok = proxy ? ProxyTts(config, spoken, path) : OsTts(spoken, path);
                    if (!ok) { silent++; continue; }
                    if (proxy) baked++; else scratch++;
                    AssetDatabase.ImportAsset(path);
                    var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.DecompressOnLoad; // the talking glow reads the waveform
                    importer.defaultSampleSettings = settings;
                    importer.forceToMono = true;
                    importer.SaveAndReimport();
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.SaveAssets();
            Debug.Log($"{builder.SceneId} VO: {baked} real, {scratch} scratch (OS text-to-speech), {silent} silent. Rebuilding the scene to re-time the Timeline.");
            builder.Build();
        }

        static bool ProxyAlive(ProxyConfig config)
        {
            try
            {
                using var request = UnityWebRequest.Get(config.BaseUrl + "/health");
                request.timeout = 2;
                var op = request.SendWebRequest();
                while (!op.isDone) Thread.Sleep(10);
                return request.result == UnityWebRequest.Result.Success;
            }
            catch { return false; }
        }

        static bool ProxyTts(ProxyConfig config, string text, string path)
        {
            using var request = new UnityWebRequest(config.BaseUrl + "/tts", "POST")
            {
                uploadHandler = new UploadHandlerRaw(ProxyTextToSpeech.Json(text)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 60,
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Session-Id", ProxyConfig.SessionId);
            var op = request.SendWebRequest();
            while (!op.isDone) Thread.Sleep(20);
            if (request.result != UnityWebRequest.Result.Success) { Debug.LogWarning("Proxy TTS failed: " + request.error); return false; }
            File.WriteAllBytes(path, request.downloadHandler.data);
            return true;
        }

        // Scratch voice from the operating system. Windows: System.Speech via PowerShell; macOS: say; Linux: espeak.
        static bool OsTts(string text, string path)
        {
            string full = Path.GetFullPath(path);
            try
            {
#if UNITY_EDITOR_WIN
                string script = Path.Combine(Path.GetTempPath(), "ohm_tts.ps1");
                File.WriteAllText(script,
                    "Add-Type -AssemblyName System.Speech\n" +
                    "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer\n" +
                    "$s.Rate = 0\n" +
                    "$s.SetOutputToWaveFile($args[0])\n" +
                    "$s.Speak([System.IO.File]::ReadAllText($args[1], [System.Text.Encoding]::UTF8))\n" +
                    "$s.Dispose()\n");
                string textFile = Path.Combine(Path.GetTempPath(), "ohm_tts.txt");
                File.WriteAllText(textFile, text, new System.Text.UTF8Encoding(false));
                return Run("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" \"{full}\" \"{textFile}\"") && File.Exists(full);
#elif UNITY_EDITOR_OSX
                string aiff = full + ".aiff";
                if (!Run("say", $"-o \"{aiff}\" \"{text.Replace("\"", "'")}\"")) return false;
                bool ok = Run("afconvert", $"-f WAVE -d LEI16 \"{aiff}\" \"{full}\"");
                File.Delete(aiff);
                return ok && File.Exists(full);
#else
                return Run("espeak", $"-w \"{full}\" \"{text.Replace("\"", "'")}\"") && File.Exists(full);
#endif
            }
            catch (Exception e) { Debug.LogWarning("OS text-to-speech unavailable: " + e.Message); return false; }
        }

        static bool Run(string file, string args)
        {
            using var process = Process.Start(new ProcessStartInfo(file, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
            if (process == null) return false;
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(60000);
            if (process.ExitCode != 0) Debug.LogWarning($"{file} failed: {error}");
            return process.ExitCode == 0;
        }
    }
}
