using UnityEngine;
namespace AdaptiveOhmsLaw
{
    public class ChargeDriftVisualization : MonoBehaviour
    {
        [SerializeField] Transform[] charges;
        [SerializeField] Vector3 bounds = new Vector3(6, 2.5f, 10);
        [SerializeField] float driftPerVolt = .16f;
        Vector3[] origins;
        float voltage;
        public float Voltage => voltage;
        public void Configure(Transform[] particles) => charges = particles;
        void Awake()
        {
            origins = new Vector3[charges.Length];
            for (int i = 0; i < charges.Length; i++) origins[i] = charges[i].localPosition;
        }
        public void SetVoltage(float value) => voltage = Mathf.Max(0, value);
        public void HighlightCharges(bool enabled)
        {
            foreach (var charge in charges) charge.localScale = Vector3.one * (enabled ? .135f : .09f);
        }
        void Update()
        {
            // Bounded thermal motion + slow net electron drift; explanatory, not to scale.
            for (int i = 0; i < charges.Length; i++)
            {
                var p = origins[i]; p.x += voltage * driftPerVolt * Time.deltaTime;
                if (p.x > bounds.x) p.x -= bounds.x * 2;
                origins[i] = p;
                float t = Time.time * 1.7f + i * 2.399f;
                charges[i].localPosition = p + new Vector3(Mathf.Sin(t * 1.31f), Mathf.Sin(t), Mathf.Cos(t * .87f)) * .065f;
            }
        }
    }
}
