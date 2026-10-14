using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // "Ohm's Law/Audio/Build library": reads Assets/OhmsLaw/Audio/CueDefinitions/library.json (written by
    // Tools/audio_organize.py and hand-editable), sets each clip's import settings (music streams, effects stay
    // compressed in memory), fills Assets/OhmsLaw/Resources/OhmsAudioLibrary.asset and makes sure the mixer has the
    // Character and Electrical groups. Idempotent; the request-file word "audio" runs it too.
    public static class AudioLibraryBuilder
    {
        public const string Json = "Assets/OhmsLaw/Audio/CueDefinitions/library.json", Asset = "Assets/OhmsLaw/Resources/OhmsAudioLibrary.asset";

        [Serializable] class JEntry { public string name, path, group, source, author, license; public bool loop; public float gain = 1f; }
        [Serializable] class JRule { public string cueStartsWith, clip; public float gain = 1f, cooldown = 0.25f; }
        [Serializable] class JFile { public JEntry[] entries; public JRule[] cueRules; }

        [MenuItem("Ohm's Law/Audio/Build library")]
        public static void Build()
        {
            if (!File.Exists(Json)) throw new InvalidOperationException("Run python Tools/audio_organize.py first: " + Json + " is missing.");
            string text = File.ReadAllText(Json);
            var file = JsonUtility.FromJson<JFile>(text);
            var aliases = ParseAliases(text);
            EnsureMixerGroups("Character", "Electrical");

            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(Asset);
            if (!library)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Asset));
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, Asset);
            }
            var entries = new List<AudioLibrary.Entry>();
            int missing = 0;
            foreach (var e in file.entries)
            {
                Import(e.path, e.loop || e.group == "Music");
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(e.path);
                if (!clip) { Debug.LogWarning("Audio library: missing clip " + e.path); missing++; continue; }
                entries.Add(new AudioLibrary.Entry { name = e.name, clip = clip, group = e.group, gain = Mathf.Clamp01(e.gain), loop = e.loop, source = e.source, author = e.author, license = e.license });
            }
            library.entries = entries.ToArray();
            library.aliases = aliases.Select(kv => new AudioLibrary.Alias { from = kv.Key, to = kv.Value }).ToArray();
            library.cueRules = (file.cueRules ?? new JRule[0]).Select(r => new AudioLibrary.CueRule { cueStartsWith = r.cueStartsWith, clip = r.clip, gain = Mathf.Clamp01(r.gain), cooldown = r.cooldown }).ToArray();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"Audio library: {entries.Count} clips, {aliases.Count} aliases, {library.cueRules.Length} cue rules" + (missing > 0 ? $", {missing} MISSING" : ""));
        }

        // JsonUtility cannot read a dictionary: the "aliases" object is parsed by hand ("from": "to" pairs).
        static Dictionary<string, string> ParseAliases(string text)
        {
            var result = new Dictionary<string, string>();
            int start = text.IndexOf("\"aliases\"", StringComparison.Ordinal);
            if (start < 0) return result;
            int open = text.IndexOf('{', start), close = text.IndexOf('}', open);
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text.Substring(open, close - open), "\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\""))
                result[m.Groups[1].Value] = m.Groups[2].Value;
            return result;
        }

        // Music and long loops stream (Vorbis 0.6), everything else is Vorbis compressed in memory (WebGL: Unity picks
        // its web format from these); short effects under 2 s decompress on load for instant playback.
        static void Import(string path, bool stream)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (!importer) return;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var s = importer.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = stream ? 0.6f : 0.7f;
            s.loadType = stream ? AudioClipLoadType.Streaming : (clip && clip.length < 2f ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory);
            s.preloadAudioData = !stream;
            var old = importer.defaultSampleSettings;
            bool changed = old.compressionFormat != s.compressionFormat || old.loadType != s.loadType || old.preloadAudioData != s.preloadAudioData || Mathf.Abs(old.quality - s.quality) > 0.01f || importer.loadInBackground != stream;
            importer.defaultSampleSettings = s;
            importer.forceToMono = false;
            importer.loadInBackground = stream;
            if (changed) importer.SaveAndReimport();
        }

        // Same reflection route as CinematicPolish.CreateMixer, for groups added later to an existing mixer.
        public static void EnsureMixerGroups(params string[] names)
        {
            var mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/OhmsLaw/Resources/OhmsLawMix.mixer");
            if (!mixer) { Debug.LogWarning("Audio library: mixer missing, run Ohm's Law/Polish/Create audio routing"); return; }
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
            var master = type.GetProperty("masterGroup", flags).GetValue(mixer);
            bool added = false;
            foreach (var name in names)
            {
                if (mixer.FindMatchingGroups(name).Any(g => g.name == name)) continue;
                var group = type.GetMethod("CreateNewGroup", flags).Invoke(mixer, new object[] { name, true });
                type.GetMethod("AddChildToParent", flags).Invoke(mixer, new[] { group, master });
                added = true;
            }
            if (added) { EditorUtility.SetDirty(mixer); AssetDatabase.SaveAssets(); Debug.Log("Audio library: mixer groups added: " + string.Join(", ", names)); }
        }
    }
}
