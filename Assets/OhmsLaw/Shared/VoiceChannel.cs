using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OhmsLaw
{
    // One voice at a time: Wattson's scripted voice and the Ask Wattson voice register here (ModuleFlow registers every
    // voice source when a scene starts). Busy() tells a speaker whether someone else is talking, so nothing starts over
    // active narration; Speaking drives the ducking of everything else (AudioDucker).
    public static class VoiceChannel
    {
        static readonly List<AudioSource> voices = new List<AudioSource>();

        public static void Register(AudioSource source)
        {
            if (source && !voices.Contains(source)) voices.Add(source);
        }

        public static bool IsVoice(AudioSource source) => source && voices.Contains(source);

        // Someone other than `except` is talking: a registered source is playing, or a scripted line is being voiced on
        // the running Timeline (that one counts even for its own source, so a branch line never starts over it).
        public static bool Busy(AudioSource except = null)
        {
            voices.RemoveAll(v => !v);
            return voices.Exists(v => v != except && v.isPlaying) || TimelineSpeaking;
        }

        public static bool Speaking => Busy();

        // The Timeline's VO track drives its AudioSource through a playable, which AudioSource.isPlaying does not report,
        // so the current subtitle line counts while the Timeline runs and its clip has not finished.
        public static bool TimelineSpeaking
        {
            get
            {
                var line = SubtitleTrack.CurrentLine;
                var flow = ModuleFlow.Instance;
                if (!line || !line.vo || !flow || !flow.TimelineRunning) return false;
                double t = flow.Director.time - SubtitleTrack.LastLineStart;
                return t >= 0 && t <= line.vo.length;
            }
        }

        // For a speaker outside the Timeline: wait until nobody else is talking (at most maxWait seconds), then speak.
        //   yield return VoiceChannel.WaitTurn(voice);
        public static IEnumerator WaitTurn(AudioSource self, float maxWait = 10f)
        {
            for (float t = 0; t < maxWait && Busy(self); t += Time.unscaledDeltaTime) yield return null;
        }
    }
}
