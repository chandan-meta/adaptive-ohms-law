using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    public class VoltageVisualization : MonoBehaviour
    {
        [SerializeField] ChargeDriftVisualization charges;
        [SerializeField] Text readout;
        [SerializeField] Transform highlight;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip activation;
        public float Voltage {get;private set;}
        public void Configure(ChargeDriftVisualization drift, Text label, Transform glow, AudioSource source)
        { charges = drift; readout = label; highlight = glow; sfx = source; }
        public void SetVoltage(float value)
        {
            Voltage=value;
            charges.SetVoltage(value); readout.text = "POTENTIAL DIFFERENCE    " + value.ToString("0") + " V";
            if (activation != null) sfx.PlayOneShot(activation);
        }
        public void ShowSupport(SupportLevel level)
        {
            highlight.gameObject.SetActive(true);
            highlight.localScale = new Vector3(2.1f, level == SupportLevel.Supported ? .12f : .055f, .55f);
            charges.HighlightCharges(level == SupportLevel.Supported);
        }
        public IEnumerator Demonstrate()
        {
            SetVoltage(1); highlight.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.5f); SetVoltage(2);
            yield return new WaitForSeconds(1.5f);
        }
    }
}
