using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

namespace OhmsLaw
{
    // One audio source's bus (voice, effects, music): its volume is gain x the learner's setting for the bus. A music bus
    // may also carry a background-music clip that starts with the scene and loops (Scene 4's study uses bgm.mp3).
    // Only a music bus with a clip touches its source's loop / play-on-awake: voice lines played with Play() must never
    // loop, and a bus added by a builder keeps full gain unless the builder sets it.
    // Beds carry across chapter loads: a looping bus still playing at its own level when the module leaves the scene
    // (ModuleFlow.Leaving) is lifted out of it and plays on, and the next scene's bus of the same kind and name takes it
    // over at the same sample, so that scene's "music" / "ambience" cue for the same clip finds it playing (a no-op) and
    // only a real change of bed crossfades. A bed nobody takes over stops once the new scene has loaded, as before.
    [RequireComponent(typeof(AudioSource))]
    public class AudioBus : MonoBehaviour
    {
        public enum Kind
        {
            Voice,
            Sfx,
            Music
        }

        [Header("Audio Bus")]
        public Kind kind;

        [Range(0f, 1f)]
        public float gain = 1f;

        [Tooltip("The clip's own level from the audio library (its Gain), under the bus gain: fades move gain, trim stays with the clip and carries with it into the next scene.")]
        [Range(0f, 1f)] public float trim = 1f;

        [Tooltip("Mixer group by name (Music, Ambience, SFX, UI, Character, Electrical, Voice). Empty: by kind. A group the mixer lacks falls back to SFX.")]
        public string group;

        [Header("Background Music (music bus only)")]
        [Tooltip("Played by a music bus when the scene starts, e.g. Assets/OhmsLaw/Shared/Art/bgm.mp3. With Play On Start off the bus only carries the clip: Scene 4's music bus holds the title bed (PadIntro) this way for ModuleFlow.")]
        public AudioClip bgm;

        [Tooltip("Play the background music automatically when the scene starts.")]
        public bool playOnStart = true;

        [Tooltip("Loop the background music.")]
        public bool loopBGM = true;

        AudioSource source;
        float level; // the gain this bus was built with: fades move gain, and a bed caught mid-fade is not carried
        public float NominalGain => level;
        static AudioMixer mixer;
        static readonly Dictionary<string, AudioMixerGroup> groups = new Dictionary<string, AudioMixerGroup>();

        // The mixer group by name (exact name first); one the mixer lacks (Character / Electrical before the audio library
        // was built) falls back to SFX, so a bus or a library clip asking for it still routes somewhere sensible.
        public static AudioMixerGroup Group(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "SFX";
            if (groups.TryGetValue(name, out var group) && group) return group;
            if (!mixer) mixer = Resources.Load<AudioMixer>("OhmsLawMix");
            if (!mixer) return null;
            var found = mixer.FindMatchingGroups(name);
            group = found.Length > 0 ? found[0] : null;
            foreach (var g in found) if (g.name == name) { group = g; break; }
            if (!group && name != "SFX") group = Group("SFX");
            return groups[name] = group;
        }

        static readonly List<AudioBus> carried = new List<AudioBus>();

        bool PlaysMusic => kind == Kind.Music && bgm;

        // Play mode without a domain reload keeps statics.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCarried()
        {
            carried.Clear();
            SceneManager.sceneLoaded -= DropCarried;
            SceneManager.sceneLoaded += DropCarried;
        }

        public static float Get(Kind kind) => PlayerPrefs.GetFloat("ohm.volume." + kind, 1f);

        public static void Set(Kind kind, float value)
        {
            PlayerPrefs.SetFloat("ohm.volume." + kind, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }

        void Awake()
        {
            source = GetComponent<AudioSource>();
            level = gain;
            if (PlaysMusic)
            {
                source.playOnAwake = false;
                source.loop = loopBGM;
            }
            if (!(PlaysMusic && playOnStart)) TakeOver(); // a bus that starts its own music keeps its scene's opening as built
        }

        void OnEnable() => ModuleFlow.Leaving += Carry;
        void OnDisable() => ModuleFlow.Leaving -= Carry;

        void Start()
        {
            // Existing learner sliders and narration ducking remain the gain owners. The mixer adds edit-time routing.
            if (!source.outputAudioMixerGroup)
                source.outputAudioMixerGroup = Group(!string.IsNullOrEmpty(group) ? group : kind == Kind.Voice ? "Voice" : kind == Kind.Music ? "Music" : name.ToLowerInvariant().Contains("ambien") || source.loop ? "Ambience" : "SFX");
            if (PlaysMusic && playOnStart) PlayBGM();
        }

        void Carry(string next)
        {
            if (kind == Kind.Voice || !source || !source.loop || !source.isPlaying || gain < level - 0.001f) return;
            if (gameObject.scene != SceneManager.GetActiveScene()) return; // the HUD's own buses already live outside it
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            carried.Add(this);
        }

        // In Awake, before the scene's director plays its first cue: the same bed carries on from where it was.
        void TakeOver()
        {
            int i = carried.FindIndex(b => b && b.kind == kind && b.name == name);
            if (i < 0) return;
            var from = carried[i];
            carried.RemoveAt(i);
            if (from.source.isPlaying && from.source.clip)
            {
                source.clip = from.source.clip;
                source.loop = true;
                trim = from.trim;
                source.volume = gain * trim * Get(kind);
                source.Play();
                source.timeSamples = from.source.timeSamples;
            }
            from.source.Stop();
            Destroy(from.gameObject);
        }

        static void DropCarried(Scene scene, LoadSceneMode mode)
        {
            foreach (var bus in carried) if (bus) Destroy(bus.gameObject);
            carried.Clear();
        }

        void Update()
        {
            if (source) source.volume = gain * trim * Get(kind);
        }

        public void PlayBGM()
        {
            if (!source || !PlaysMusic) return;
            trim = AudioLibrary.GainOf(bgm.name); // a library clip plays at its library level (1 for any other clip)
            source.clip = bgm;
            source.loop = loopBGM;
            source.Play();
        }

        public void StopBGM() { if (source) source.Stop(); }
        public void PauseBGM() { if (source) source.Pause(); }
        public void ResumeBGM() { if (source) source.UnPause(); }

        public void SetGain(float value) => gain = Mathf.Clamp01(value);
        public void FadeInBGM() => gain = 1f;
        public void FadeOutBGM() => gain = 0f;
    }
}
