using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public class MeterDisplay : MonoBehaviour
    {
        [SerializeField] Text readoutText;
        [SerializeField] Text labelText;
        [SerializeField] Transform needle;
        [SerializeField] float maxScaleValue = 5f;
        [SerializeField] float minAngle = 45f;
        [SerializeField] float maxAngle = -45f;
        [SerializeField] string defaultUnit = "A";
        [SerializeField] string numberFormat = "F2";

        float targetValue;
        float currentValue;

        public float CurrentReading => currentValue;

        public void Configure(Text readout, Text label, Transform needleTransform = null, string unit = "A", float maxVal = 5f)
        {
            readoutText = readout;
            labelText = label;
            needle = needleTransform;
            defaultUnit = unit;
            maxScaleValue = maxVal;
            UpdateDisplay(0f);
        }

        public void SetReading(float value)
        {
            targetValue = Mathf.Max(0f, value);
            currentValue = targetValue;
            UpdateDisplay(currentValue);
        }

        void UpdateDisplay(float val)
        {
            if (readoutText != null)
                readoutText.text = val.ToString(numberFormat) + " " + defaultUnit;

            if (needle != null)
            {
                float t = Mathf.Clamp01(val / Mathf.Max(0.01f, maxScaleValue));
                float angle = Mathf.Lerp(minAngle, maxAngle, t);
                needle.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}
