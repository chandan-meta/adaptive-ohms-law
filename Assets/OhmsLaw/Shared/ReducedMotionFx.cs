using UnityEngine;

namespace OhmsLaw
{
    // On an effect that flashes, streaks or bursts (dive streaks, the field wave, the Ohm-niscope flash, ...): with
    // ReducedMotion.On it plays calmer, read every time the effect is shown: fewer particles and a dimmer start colour.
    public class ReducedMotionFx : MonoBehaviour
    {
        [Range(0, 1), Tooltip("Share of the particles (rate and bursts) kept with reduced motion; the start alpha drops halfway to it.")]
        public float calm = 0.3f;

        ParticleSystem[] systems;
        float[] rates;
        ParticleSystem.Burst[][] bursts;
        Color[] colours;

        void OnEnable()
        {
            if (systems == null)
            {
                systems = GetComponentsInChildren<ParticleSystem>(true);
                rates = new float[systems.Length];
                bursts = new ParticleSystem.Burst[systems.Length][];
                colours = new Color[systems.Length];
                for (int i = 0; i < systems.Length; i++)
                {
                    var emission = systems[i].emission;
                    rates[i] = emission.rateOverTimeMultiplier;
                    bursts[i] = new ParticleSystem.Burst[emission.burstCount];
                    emission.GetBursts(bursts[i]);
                    colours[i] = systems[i].main.startColor.color;
                }
            }
            bool on = ReducedMotion.On;
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (!ps) continue;
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = rates[i] * (on ? calm : 1f);
                var scaled = (ParticleSystem.Burst[])bursts[i].Clone();
                for (int b = 0; b < scaled.Length; b++)
                    if (on) scaled[b].count = Mathf.Max(1f, bursts[i][b].count.constant * calm);
                emission.SetBursts(scaled);
                var main = ps.main;
                if (main.startColor.mode != ParticleSystemGradientMode.Color) continue;
                var colour = colours[i];
                if (on) colour.a *= (1f + calm) * 0.5f;
                main.startColor = colour;
            }
        }
    }
}
