using UnityEngine;

namespace OhmsLaw
{
    public class ElectricArc : MonoBehaviour
    {
        public Transform from, to;
        public float jitter = 0.05f, cycle = 1.3f, maxReach = 0.92f;
        LineRenderer line;
        float time;

        void OnEnable() { line = GetComponent<LineRenderer>(); time = 0; }

        void Update()
        {
            if (!from || !to) return;
            time += Time.deltaTime;
            float phase = time % cycle / cycle;
            // Grows for most of the cycle, then snaps off.
            float reach = phase < 0.8f ? maxReach * Mathf.SmoothStep(0, 1, phase / 0.8f) : 0;
            line.enabled = reach > 0.02f && Random.value > 0.12f;
            int count = line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f) * reach;
                Vector3 p = Vector3.Lerp(from.position, to.position, t);
                if (i > 0) p += Random.insideUnitSphere * jitter * (i == count - 1 ? 2f : 1f);
                line.SetPosition(i, p);
            }
        }
    }
}
