using UnityEngine;

namespace OhmsLaw
{
    public class Niscope : MonoBehaviour
    {
        public Transform[] rings;
        public Light flare;
        public Renderer flash;
        public float flarePeak = 6f;
        [Range(0.05f, 1f)] public float timeScale = 1f;
        Vector3[] scales;
        Vector3 flashScale;
        float time;

        void Awake()
        {
            scales = new Vector3[rings.Length];
            for (int i = 0; i < rings.Length; i++) scales[i] = rings[i].localScale;
            if (flash) flashScale = flash.transform.localScale;
        }

        void OnEnable() { time = 0; }

        void Update()
        {
            float dt = Time.deltaTime * timeScale;
            time += dt;
            for (int i = 0; i < rings.Length; i++)
            {
                float open = Mathf.Clamp01((time - 0.25f * i) / 0.6f);
                float ease = open + Mathf.Sin(open * Mathf.PI) * 0.25f;
                rings[i].localScale = scales[i] * ease;
                rings[i].Rotate(0, 0, (i % 2 == 0 ? 1 : -1) * (120f + 70f * i) * dt, Space.Self);
            }
            // One bright flash once the last ring is open, then a steady glow.
            float sinceOpen = time - (0.25f * rings.Length + 0.5f);
            float burst = sinceOpen < 0 ? 0 : Mathf.Exp(-sinceOpen * 3f);
            float glow = Mathf.Clamp01(time / 1.2f);
            if (flare) flare.intensity = flarePeak * burst + 0.8f * glow;
            if (flash) flash.transform.localScale = flashScale * (0.35f * glow + 2.2f * burst);
        }
    }
}
