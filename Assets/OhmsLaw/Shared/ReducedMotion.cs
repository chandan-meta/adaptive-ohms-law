using UnityEngine;

namespace OhmsLaw
{
    // Learner preference (the HUD's MOTION toggle): fewer flashes, no handheld camera noise, calmer particles, no
    // sliding panels. Effects read On every frame, so the value is cached and PlayerPrefs is touched only on a change.
    public static class ReducedMotion
    {
        const string Key = "ohm.reducedMotion";
        static int cached = -1;

        public static bool On
        {
            get
            {
                if (cached < 0) cached = PlayerPrefs.GetInt(Key, 0);
                return cached == 1;
            }
            set
            {
                cached = value ? 1 : 0;
                PlayerPrefs.SetInt(Key, cached);
                PlayerPrefs.Save();
            }
        }
    }
}
