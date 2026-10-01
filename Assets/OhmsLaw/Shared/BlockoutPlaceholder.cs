using UnityEngine;

namespace OhmsLaw
{
    // Tags rough stand-in geometry with what the final version should be. Ohm's Law > Blockout Report lists them all.
    public class BlockoutPlaceholder : MonoBehaviour
    {
        [TextArea] public string note;
        [Tooltip("Visual-effect stand-ins pulse while active, so reviewers notice when the effect plays.")] public bool pulse;

        Vector3 scale;
        void Awake() { scale = transform.localScale; }
        void Update() { if (pulse) transform.localScale = scale * (1 + 0.07f * Mathf.Sin(Time.time * 7f)); }
    }
}
