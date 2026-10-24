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
    // only a real change of bed crossfades. A bed nobody takes over stops once the new scene has loaded, as before. A bed
    // still fading in when the scene ends (a music change just before a hand-off) is carried too and eases on up to its
    // level in the next scene; one fading out is not carried (it was on its way out).
    // Every music bus plays MusicLevel under its gain: the beds sit a little lower under the narration than they were built
    // (a runtime trim, so the saved scenes need no rebuild).
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

        [Tooltip("A looping bed still playing when the scene hands off is carried into the next one. Off for loops another script drives (the current's hum), which go with their scene.")]
        public bool carry = true;

        [Header("Background Music (music bus only)")]
        [Tooltip("Played by a music bus when the scene starts, e.g. Assets/OhmsLaw/Shared/Art/bgm.mp3. With Play On Start off the bus only carries the clip: Scene 4's music bus holds the title bed (PadIntro) this way for ModuleFlow.")]
        public AudioClip bgm;

        [Tooltip("Play the background music automatically when the scene starts.")]
        public bool playOnStart = true;

        [Tooltip("Loop the background music.")]
        public bool loopBGM = true;

        public const float MusicLevel = 0.85f;
        public static float Level(Kind kind) => kind == Kind.Music ? MusicLevel : 1f;

        AudioSource source;
        float level; // the gain this bus was built with: fades move gain
        float lastGain, easeTo = -1f, eased;
        bool falling; // the gain went down since the last frame: a fade-out in progress
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
            level = lastGain = gain;
            AudioDucker.RescanSoon(); // a bus made at runtime (a bed's crossfade twin, a pitched one-shot) is ducked from its first frame
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
            if (!carry || kind == Kind.Voice || !source || !source.loop || !source.isPlaying) return;
            if (gain < level - 0.001f && (falling || gain < 0.5f * level)) return; // fading out, or barely in: let it go
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
                // Carried mid fade-in: it starts where it was and eases on up to this bus's level.
                float share = from.level > 0f ? Mathf.Clamp01(from.gain / from.level) : 1f;
                if (share < 0.999f) { easeTo = gain; gain = eased = lastGain = gain * share; }
                source.volume = gain * trim * Get(kind) * Level(kind);
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

        const float CarryEaseSeconds = 1.5f;

        void Update()
        {
            if (easeTo >= 0f)
            {
                // Someone else (a crossfade) took the gain over: the carried ease stops.
                if (!Mathf.Approximately(gain, eased)) easeTo = -1f;
                else
                {
                    gain = eased = Mathf.MoveTowards(gain, easeTo, Time.unscaledDeltaTime * Mathf.Max(0.01f, easeTo) / CarryEaseSeconds);
                    if (Mathf.Approximately(gain, easeTo)) easeTo = -1f;
                }
            }
            falling = gain < lastGain - 1e-5f;
            lastGain = gain;
            if (source) source.volume = gain * trim * Get(kind) * Level(kind);
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
