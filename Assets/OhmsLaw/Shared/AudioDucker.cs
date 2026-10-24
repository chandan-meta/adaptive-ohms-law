using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OhmsLaw
{
    // Gain ducking before the mixer: while a voice speaks (VoiceChannel.Speaking) every other AudioSource drops to a fraction of
    // its volume, and comes back when the voice stops. Runs in LateUpdate, after AudioBus.Update has written the bus
    // volumes for this frame; a source without a bus keeps the volume it had, remembered here. Created by ModuleFlow.
    // The envelope eases (smoothstep on a linear clock) so neither the dip nor the return is ever heard as a step: the
    // beds sink about 12 dB under narration, effects and ambience about 6 dB.
    [DefaultExecutionOrder(1000)]
    public class AudioDucker : MonoBehaviour
    {
        [Tooltip("Music volume while a voice speaks, as a fraction of normal.")] [Range(0, 1)] public float music = 0.25f;
        [Tooltip("Effects and every other non-voice source while a voice speaks.")] [Range(0, 1)] public float effects = 0.5f;
        // The release holds the duck across the gaps between consecutive lines (Say() leaves 0.3 s, the Timeline often under
        // a second), so a rhythmic bed (PadLabTheme's arpeggio) does not swell and dip between sentences.
        public float attackSeconds = 0.3f, releaseSeconds = 0.9f;

        public float Amount { get; private set; } // 0 = normal, 1 = fully ducked (eased)
        float clock; // the envelope's linear position; Amount is its smoothstep
        readonly Dictionary<AudioSource, (float normal, float applied)> ducked = new Dictionary<AudioSource, (float, float)>();
        AudioSource[] sources = new AudioSource[0];
        float rescanIn;
        static AudioDucker instance;

        void OnEnable() { instance = this; SceneManager.sceneLoaded += Rescan; }

        void OnDisable()
        {
            if (instance == this) instance = null;
            SceneManager.sceneLoaded -= Rescan;
            foreach (var pair in ducked) if (pair.Key) pair.Key.volume = pair.Value.normal;
            ducked.Clear();
            Amount = clock = 0;
        }

        void Rescan(Scene scene, LoadSceneMode mode) => rescanIn = 0;

        // A source made at runtime (SceneDirector's pitched one-shots) is ducked from its first frame, not after the next 1 s scan.
        public static void RescanSoon() { if (instance) instance.rescanIn = 0; }

        void LateUpdate()
        {
            if ((rescanIn -= Time.unscaledDeltaTime) <= 0)
            {
                sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
                foreach (var gone in ducked.Keys.Where(source => !source).ToList()) ducked.Remove(gone);
                rescanIn = 1f;
            }
            Step(Time.unscaledDeltaTime, VoiceChannel.Speaking, sources);
        }

        // One frame of ducking over `all` (public for the editor self-check).
        public void Step(float dt, bool speaking, AudioSource[] all)
        {
            clock = Mathf.MoveTowards(clock, speaking ? 1 : 0, dt / Mathf.Max(0.01f, speaking ? attackSeconds : releaseSeconds));
            Amount = clock * clock * (3f - 2f * clock);
            foreach (var source in all)
            {
                if (!source || VoiceChannel.IsVoice(source)) continue;
                bool known = ducked.TryGetValue(source, out var state);
                if (!known && Amount <= 0) continue;
                // A volume this script did not set means its owner (AudioBus, a fade) changed it: that is the new normal.
                if (!known || !Mathf.Approximately(source.volume, state.applied)) state.normal = source.volume;
                float level = source.TryGetComponent(out AudioBus bus) && bus.kind == AudioBus.Kind.Music ? music : effects;
                state.applied = state.normal * Mathf.Lerp(1, level, Amount);
                source.volume = state.applied;
                if (Amount <= 0) ducked.Remove(source);
                else ducked[source] = state;
            }
        }
    }
}
