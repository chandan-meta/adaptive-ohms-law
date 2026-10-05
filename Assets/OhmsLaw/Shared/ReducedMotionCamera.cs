using Unity.Cinemachine;
using UnityEngine;

namespace OhmsLaw
{
    // On a virtual camera with handheld noise (the builders' Vcam() adds it next to the Perlin): no camera shake while
    // ReducedMotion.On, read every frame so the HUD's MOTION toggle takes effect mid-shot, the built amplitude back when it is off.
    public class ReducedMotionCamera : MonoBehaviour
    {
        CinemachineBasicMultiChannelPerlin perlin;
        float amplitude;

        void Awake()
        {
            perlin = GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (!perlin) { enabled = false; return; }
            amplitude = perlin.AmplitudeGain;
        }

        void Update() => perlin.AmplitudeGain = ReducedMotion.On ? 0f : amplitude; // before the brain's LateUpdate places the camera
    }
}
