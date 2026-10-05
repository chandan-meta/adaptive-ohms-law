using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace OhmsLaw
{
    // Suggested questions for a scene, each with a pre-written, spoiler-free answer used when the proxy is offline.
    // Ask Wattson always labels these answers "Offline answer"; they are never shown as live AI.
    [CreateAssetMenu(menuName = "Ohm's Law/Local FAQ")]
    public class LocalFAQ : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string question;
            public DialogueLine answer;
        }

        public Entry[] entries;

        // A typed "what is voltage" finds the chip "What is voltage?": case, punctuation and spacing are ignored.
        public DialogueLine Find(string question)
        {
            if (entries == null) return null;
            string key = Key(question);
            foreach (var entry in entries)
                if (entry != null && key.Length > 0 && Key(entry.question) == key) return entry.answer;
            return null;
        }

        public static string Key(string text) => Regex.Replace((text ?? "").ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
    }
}
