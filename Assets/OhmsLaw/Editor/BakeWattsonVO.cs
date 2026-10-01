using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace OhmsLaw.Editor
{
    // Bakes every DialogueLine through the proxy's /tts, so scripted lines use the same voice as live answers.
    // To use a voice actor instead, drop clips named after the line id (S04_L01.wav ...) into Scene04/Audio/VO and rebuild.
    public static class BakeWattsonVO
    {
        const string Folder = Scene04Builder.S4 + "/Audio/VO";

        [MenuItem("Ohm's Law/Bake Wattson VO")]
        public static void Bake()
        {
            var config = AssetDatabase.LoadAssetAtPath<ProxyConfig>(Scene04Builder.Shared + "/Agent/ProxyConfig.asset");
            if (!config) { Debug.LogError("Run Ohm's Law > Build Scene 04 first; it creates the ProxyConfig asset."); return; }
            Directory.CreateDirectory(Folder);
            var lines = AssetDatabase.FindAssets("t:DialogueLine", new[] { Scene04Builder.Root })
                .Select(guid => AssetDatabase.LoadAssetAtPath<DialogueLine>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(line => line && !string.IsNullOrWhiteSpace(line.Spoken)).ToArray();
            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    EditorUtility.DisplayProgressBar("Bake Wattson VO", line.id, i / (float)lines.Length);
                    using var request = new UnityWebRequest(config.BaseUrl + "/tts", "POST")
                    {
                        uploadHandler = new UploadHandlerRaw(ProxyTextToSpeech.Json(line.Spoken)),
                        downloadHandler = new DownloadHandlerBuffer(),
                        timeout = 60,
                    };
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("X-Session-Id", ProxyConfig.SessionId);
                    var sending = request.SendWebRequest();
                    while (!sending.isDone) Thread.Sleep(20);
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Bake stopped at {line.id}: {request.error} {request.downloadHandler.text}\nIs the proxy running at {config.BaseUrl} with ELEVENLABS_API_KEY set?");
                        return;
                    }

                    string path = Folder + "/" + line.id + ".mp3";
                    File.WriteAllBytes(path, request.downloadHandler.data);
                    AssetDatabase.ImportAsset(path);
                    // Decompressed in memory so the mouth and glow can read the waveform while it plays.
                    var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    importer.defaultSampleSettings = settings;
                    importer.forceToMono = true;
                    importer.SaveAndReimport();
                    line.vo = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    EditorUtility.SetDirty(line);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.SaveAssets();
            Debug.Log($"Baked {lines.Length} Wattson lines. Rebuilding Scene 04 so the Timeline follows the new clip lengths.");
            Scene04Builder.Build();
        }
    }
}
