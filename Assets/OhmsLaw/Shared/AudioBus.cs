using UnityEngine;

namespace OhmsLaw
{
    public class AudioBus : MonoBehaviour
    {
        public enum Kind { Voice, Sfx, Music }
        public Kind kind;
        [Range(0, 1)] public float gain = 1f;
        AudioSource source;

        public static float Get(Kind kind) => PlayerPrefs.GetFloat("ohm.volume." + kind, 1f);
        public static void Set(Kind kind, float value) => PlayerPrefs.SetFloat("ohm.volume." + kind, value);

        void Awake() { source = GetComponent<AudioSource>(); }
        void Update() { source.volume = gain * Get(kind); }
    }
}
