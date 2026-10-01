using UnityEngine;

namespace OhmsLaw
{
    [CreateAssetMenu(menuName = "Ohm's Law/Dialogue Line")]
    public class DialogueLine : ScriptableObject
    {
        public string id;
        public string speaker = "WATTSON";
        [TextArea, Tooltip("Shown in subtitles, exactly as written.")] public string text;
        [TextArea, Tooltip("Sent to text-to-speech. Normally identical to text.")] public string ttsText;
        public AudioClip vo;
        public float fallbackDuration = 3f;

        public string Spoken => string.IsNullOrWhiteSpace(ttsText) ? text : ttsText;
        public float Duration => vo ? vo.length : fallbackDuration;
    }
}
