using UnityEngine;

namespace OhmsLaw
{
    public class PopIn : MonoBehaviour
    {
        public float duration = 0.45f, fromScale = 0f, overshoot = 1.25f;
        public float bob = 0f, bobSpeed = 1.2f, pulse = 0f, pulseSpeed = 4f;
        Vector3 scale, position;
        float time;
        bool cached;

        void OnEnable()
        {
            if (!cached) { scale = transform.localScale; position = transform.localPosition; cached = true; }
            time = 0;
            transform.localScale = scale * fromScale;
        }

        void Update()
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            // Rises past the final size and settles back.
            float ease = 1 + (overshoot - 1) * Mathf.Sin(t * Mathf.PI) * (1 - t) * 2f;
            float grow = Mathf.LerpUnclamped(fromScale, 1, Mathf.SmoothStep(0, 1, t)) * ease;
            transform.localScale = scale * (grow * (1 + pulse * Mathf.Sin(time * pulseSpeed)));
            if (bob > 0) transform.localPosition = position + Vector3.up * (Mathf.Sin(time * bobSpeed + position.x) * bob);
        }
    }
}
