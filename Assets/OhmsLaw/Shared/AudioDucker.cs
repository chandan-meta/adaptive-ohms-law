using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OhmsLaw
{
    // Gain ducking before the mixer: while a voice speaks (VoiceChannel.Speaking) every other AudioSource drops to a fraction of
    // its volume, and comes back when the voice stops. Runs in LateUpdate, after AudioBus.Update has written the bus
    // volumes for this frame; a source without a bus keeps the volume it had, remembered here. Created by ModuleFlow.
    [DefaultExecutionOrder(1000)]
    public class AudioDucker : MonoBehaviour
    {
        [Tooltip("Music volume while a voice speaks, as a fraction of normal.")] [Range(0, 1)] public float music = 0.3f;
        [Tooltip("Effects and every other non-voice source while a voice speaks.")] [Range(0, 1)] public float effects = 0.55f;
        public float attackSeconds = 0.15f, releaseSeconds = 0.6f;

        public float Amount { get; private set; } // 0 = normal, 1 = fully ducked
        readonly Dictionary<AudioSource, (float normal, float applied)> ducked = new Dictionary<AudioSource, (float, float)>();
        AudioSource[] sources = new AudioSource[0];
        float rescanIn;

        void OnEnable() => SceneManager.sceneLoaded += Rescan;

        void OnDisable()
        {
            SceneManager.sceneLoaded -= Rescan;
            foreach (var pair in ducked) if (pair.Key) pair.Key.volume = pair.Value.normal;
            ducked.Clear();
            Amount = 0;
        }

        void Rescan(Scene scene, LoadSceneMode mode) => rescanIn = 0;

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
            Amount = Mathf.MoveTowards(Amount, speaking ? 1 : 0, dt / Mathf.Max(0.01f, speaking ? attackSeconds : releaseSeconds));
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
