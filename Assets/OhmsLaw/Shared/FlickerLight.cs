using UnityEngine;

namespace OhmsLaw
{
    public class FlickerLight : MonoBehaviour
    {
        public float speed = 7f, amount = 0.3f;
        Light target;
        float baseIntensity, seed;

        void Awake() { target = GetComponent<Light>(); baseIntensity = target.intensity; seed = Random.value * 100f; }
        void Update() { target.intensity = baseIntensity * (1 - amount + 2 * amount * Mathf.PerlinNoise(Time.time * speed, seed)); }
    }
}
