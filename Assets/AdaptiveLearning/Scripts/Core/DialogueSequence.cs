using System;
using UnityEngine;
namespace AdaptiveOhmsLaw
{
    [Serializable] public class DialogueLine
    {
        public string id, speaker = "WATTSON";
        [TextArea(2,5)] public string text;
        public AudioClip audio;
        public float duration = 3;
        public string emotion = "Curious", nextEvent;
        [Tooltip("Use an explicit apparatus focus instead of automatic text matching.")]
        public bool overrideCinematicSubject;
        public CinematicSubject cinematicSubject = CinematicSubject.Learner;
    }
    [CreateAssetMenu(menuName = "Adaptive Learning/Dialogue sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public DialogueLine[] lines;
        public DialogueLine Get(string id) => Array.Find(lines, line => line.id == id);
    }
}
