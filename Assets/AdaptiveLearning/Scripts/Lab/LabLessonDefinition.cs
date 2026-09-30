using UnityEngine;
namespace AdaptiveOhmsLaw
{
    [CreateAssetMenu(menuName="Adaptive Learning/Lab introduction")]
    public sealed class LabLessonDefinition : ScriptableObject
    {
        public string title;
        public bool buildCircuit;
        public string nextScene;
        public DialogueLine[] steps;
    }
}
