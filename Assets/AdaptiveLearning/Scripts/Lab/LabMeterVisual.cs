using UnityEngine;

namespace AdaptiveOhmsLaw
{
    // The imported dial has a 0–3 scale. Assessment values remain owned by the lesson.
    public sealed class LabMeterVisual : MonoBehaviour
    {
        [SerializeField] Transform needle;
        [SerializeField] Quaternion zeroRotation;
        [SerializeField] float fullScale = 3f;
        [SerializeField] float sweepDegrees = -120f;
        [SerializeField] bool measuresCurrent;
        public void Configure(Transform pointer, bool current)
        {
            needle = pointer;
            zeroRotation = pointer.localRotation;
            measuresCurrent = current;
        }
        void Awake() => SetValue(0);
        public void ShowCompletedCircuit() => SetValue(measuresCurrent ? .2f : 1f);
        public void SetValue(float value)
        {
            if (needle != null)
                needle.localRotation = zeroRotation * Quaternion.AngleAxis(
                    sweepDegrees * Mathf.Clamp01(value / Mathf.Max(.001f, fullScale)), Vector3.forward);
        }
    }
}
