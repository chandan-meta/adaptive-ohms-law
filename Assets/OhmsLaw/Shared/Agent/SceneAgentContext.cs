using UnityEngine;

namespace OhmsLaw
{
    // What Wattson may talk about in one scene. Sent to the proxy with every question.
    [CreateAssetMenu(menuName = "Ohm's Law/Scene Agent Context")]
    public class SceneAgentContext : ScriptableObject
    {
        public string sceneId, sceneTitle;
        public bool askWattsonEnabled = true;
        [TextArea] public string[] covered;
        [TextArea] public string[] doNotSpoil;
        [Tooltip("The suggested question chips and their offline answers.")] public LocalFAQ faq;
    }
}
