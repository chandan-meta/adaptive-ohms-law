using UnityEngine;
namespace AdaptiveOhmsLaw
{
    [CreateAssetMenu(menuName = "Adaptive Learning/Question")]
    public class QuestionDefinition : ScriptableObject
    {
        public string questionId;
        public ConceptId[] concepts;
        [TextArea(2,5)] public string questionText;
        public string[] answers;
        public int correctAnswer;
        [Range(1,5)] public int difficulty = 1;
        [TextArea(2,5)] public string[] hints;
        [Min(0)] public float masteryImpact = 1;
    }
}
