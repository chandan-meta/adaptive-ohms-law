using UnityEngine;

namespace OhmsLaw
{
    // Learner preference (the HUD toggle): fewer flashes, no handheld camera noise, calmer particles. Effects read On.
    public static class ReducedMotion
    {
        public static bool On
        {
            get => PlayerPrefs.GetInt("ohm.reducedMotion", 0) == 1;
            set => PlayerPrefs.SetInt("ohm.reducedMotion", value ? 1 : 0);
        }
    }
}
