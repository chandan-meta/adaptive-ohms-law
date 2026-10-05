using System.Collections.Generic;
using UnityEngine;

namespace OhmsLaw
{
    // One voice at a time: Wattson's scripted voice and the Ask Wattson voice register here. Busy() tells a speaker
    // whether someone else is talking, so nothing starts over active narration.
    public static class VoiceChannel
    {
        static readonly List<AudioSource> voices = new List<AudioSource>();

        public static void Register(AudioSource source)
        {
            if (source && !voices.Contains(source)) voices.Add(source);
        }

        public static bool IsVoice(AudioSource source) => voices.Contains(source);

        public static bool Busy(AudioSource except = null)
        {
            voices.RemoveAll(v => !v);
            return voices.Exists(v => v != except && v.isPlaying);
        }
    }
}
