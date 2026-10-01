using System;
using UnityEngine;

namespace OhmsLaw
{
    // Suggested questions for a scene, each with a pre-written, spoiler-free answer used when the proxy is offline.
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

        public DialogueLine Find(string question)
        {
            foreach (var entry in entries)
                if (string.Equals(entry.question, question, StringComparison.OrdinalIgnoreCase)) return entry.answer;
            return null;
        }
    }
}
