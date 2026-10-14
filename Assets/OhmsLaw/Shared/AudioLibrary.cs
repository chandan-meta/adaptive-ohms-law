using System;
using System.Collections.Generic;
using UnityEngine;

namespace OhmsLaw
{
    // The module's sound library: every downloaded clip by name, with its mixer group, a gain that levels it against the
    // others, a loop flag and its source/licence; aliases map the old synthesized bank names ("PadLab", "Select") to the
    // new clips so Timelines that were not rebuilt keep working; cue rules play a clip when a Timeline cue runs.
    // Built from Assets/OhmsLaw/Audio/CueDefinitions/library.json by "Ohm's Law/Audio/Build library" into
    // Assets/OhmsLaw/Resources/OhmsAudioLibrary.asset, loaded once at runtime by Instance.
    [CreateAssetMenu(menuName = "Ohm's Law/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string name;
            public AudioClip clip;
            [Tooltip("Mixer group: Music, Ambience, SFX, UI, Character, Electrical, Voice.")] public string group = "SFX";
            [Range(0f, 1f)] public float gain = 1f;
            public bool loop;
            public string source, author, license;
        }

        [Serializable]
        public class Alias { public string from, to; }

        [Serializable]
        public class CueRule
        {
            [Tooltip("A Timeline cue that starts with this text plays the clip, e.g. 'wire.field 1' or 'show Drift Arrow'.")] public string cueStartsWith;
            public string clip;
            [Range(0f, 1f)] public float gain = 1f;
            [Tooltip("Seconds before the same rule may fire again (0 = every time).")] public float cooldown = 0.25f;
            [NonSerialized] public float lastPlayed = -999f;
        }

        public Entry[] entries = new Entry[0];
        public Alias[] aliases = new Alias[0];
        public CueRule[] cueRules = new CueRule[0];

        static AudioLibrary instance;
        public static AudioLibrary Instance
        {
            get
            {
                if (!instance) instance = Resources.Load<AudioLibrary>("OhmsAudioLibrary");
                return instance;
            }
        }

        Dictionary<string, Entry> byName;
        Dictionary<string, string> alias;

        void Index()
        {
            if (byName != null && byName.Count == entries.Length) return;
            byName = new Dictionary<string, Entry>();
            foreach (var e in entries) if (e != null && !string.IsNullOrEmpty(e.name)) byName[e.name] = e;
            alias = new Dictionary<string, string>();
            foreach (var a in aliases) if (a != null && !string.IsNullOrEmpty(a.from)) alias[a.from] = a.to;
        }

        public string Resolve(string name)
        {
            Index();
            return name != null && alias.TryGetValue(name, out var to) ? to : name;
        }

        public Entry Find(string name)
        {
            Index();
            if (string.IsNullOrEmpty(name)) return null;
            return byName.TryGetValue(Resolve(name), out var e) && e.clip ? e : null;
        }

        public AudioClip Clip(string name) => Find(name)?.clip;
        public float Gain(string name) => Find(name)?.gain ?? 1f;
        public string Group(string name) => Find(name)?.group ?? "SFX";

        // Static conveniences that are safe when the asset is missing (the module then falls back to its old bank).
        public static AudioClip ClipOrNull(string name) => Instance ? Instance.Clip(name) : null;
        public static float GainOf(string name) => Instance ? Instance.Gain(name) : 1f;
        public static string GroupOf(string name) => Instance ? Instance.Group(name) : "SFX";

        // The rule a cue text matches, honouring its cooldown; null when nothing applies.
        public CueRule Rule(string cue, float now)
        {
            if (string.IsNullOrEmpty(cue)) return null;
            foreach (var r in cueRules)
            {
                if (r == null || string.IsNullOrEmpty(r.cueStartsWith) || !cue.StartsWith(r.cueStartsWith, StringComparison.Ordinal)) continue;
                if (now - r.lastPlayed < r.cooldown) return null;
                r.lastPlayed = now;
                return r;
            }
            return null;
        }
    }
}
