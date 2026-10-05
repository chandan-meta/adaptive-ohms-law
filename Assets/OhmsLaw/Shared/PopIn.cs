using System.Collections;
using UnityEngine;

namespace OhmsLaw
{
    public class PopIn : MonoBehaviour
    {
        public float duration = 0.45f, fromScale = 0f, overshoot = 1.25f;
        public float bob = 0f, bobSpeed = 1.2f, pulse = 0f, pulseSpeed = 4f;
        Vector3 scale, position;
        float time;
        bool cached, calm;

        void OnEnable()
        {
            if (!cached) { scale = transform.localScale; position = transform.localPosition; cached = true; }
            time = 0;
            calm = ReducedMotion.On;
            transform.localScale = scale * fromScale;
        }

        void Update()
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            // Rises past the final size and settles back. Reduced motion: no overshoot, no breathing, no bob.
            float ease = 1 + ((calm ? 1f : overshoot) - 1) * Mathf.Sin(t * Mathf.PI) * (1 - t) * 2f;
            float grow = Mathf.LerpUnclamped(fromScale, 1, Mathf.SmoothStep(0, 1, t)) * ease;
            transform.localScale = scale * (grow * (1 + (calm ? 0f : pulse) * Mathf.Sin(time * pulseSpeed)));
            if (bob > 0 && !calm) transform.localPosition = position + Vector3.up * (Mathf.Sin(time * bobSpeed + position.x) * bob);
        }

        // ---------------------------------------------------------------- the reverse, and a grow along chosen axes

        // Shrink-out: a small swell, then `t` collapses along `axes` (1 = that axis goes to zero, 0 = it keeps its size)
        // over `seconds`, drifting up by `lift` (dust rising off it); then it is deactivated and its scale and position are
        // restored, so a later show (or PopIn) brings it back whole. A PopIn on it is paused meanwhile (it would rewrite
        // the scale every frame) and re-armed for the next activation.
        public static IEnumerator Out(Transform t, float seconds, Vector3 axes, float lift = 0f)
        {
            if (!t || !t.gameObject.activeSelf) yield break;
            var pop = t.GetComponent<PopIn>();
            if (pop) pop.enabled = false;
            Vector3 rest = t.localScale, at = t.localPosition, flat = Collapse(rest, axes);
            bool calm = ReducedMotion.On;
            if (calm) lift = 0f;
            for (float e = 0; e < 1; e += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                // 0-20 %: swell to 106 % (calm: none); then an ease-in collapse.
                float swell = calm ? 0f : 0.06f * Mathf.Sin(Mathf.Clamp01(e / 0.2f) * Mathf.PI * 0.5f) * (1 - Mathf.Clamp01((e - 0.2f) / 0.3f));
                float k = Mathf.Clamp01((e - (calm ? 0f : 0.15f)) / (calm ? 1f : 0.85f));
                t.localScale = Vector3.Lerp(rest, flat, k * k) + Vector3.Scale(rest, axes) * swell;
                t.localPosition = at + Vector3.up * (lift * Mathf.SmoothStep(0, 1, e));
                yield return null;
            }
            t.gameObject.SetActive(false);
            t.localScale = rest;
            t.localPosition = at;
            if (pop) pop.enabled = true;
        }

        // Grow-in along `axes` from collapsed to the scale `t` has now (activating it first), settling with a soft
        // overshoot (none under reduced motion). For objects without their own PopIn; one with a PopIn just needs showing.
        public static IEnumerator In(Transform t, float seconds, Vector3 axes, float overshoot = 1.05f)
        {
            if (!t) yield break;
            Vector3 rest = t.localScale, flat = Collapse(rest, axes);
            if (ReducedMotion.On) overshoot = 1f;
            t.localScale = flat;
            t.gameObject.SetActive(true);
            for (float e = 0; e < 1; e += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                float grow = Mathf.SmoothStep(0, 1, e) * (1 + (overshoot - 1) * Mathf.Sin(e * Mathf.PI) * (1 - e) * 2f);
                t.localScale = Vector3.LerpUnclamped(flat, rest, grow);
                yield return null;
            }
            t.localScale = rest;
        }

        static Vector3 Collapse(Vector3 scale, Vector3 axes) => Vector3.Scale(scale, Vector3.one - axes);
    }
}
