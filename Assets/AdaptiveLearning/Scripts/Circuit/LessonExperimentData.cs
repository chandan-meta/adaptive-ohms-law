using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdaptiveOhmsLaw
{
    // Shared protocol for the experiment, graph and resistance investigation.
    public static class LessonExperimentData
    {
        public const int ReadingCount = 5;
        public const float Resistance = 5f;
        public const float CurrentStep = 1f / Resistance;

        public static bool IsValid(List<ExperimentObservation> data) =>
            data != null && data.Count == ReadingCount && Enumerable.Range(1, ReadingCount).All(v =>
                data.Count(o => o != null && float.IsFinite(o.voltage) && float.IsFinite(o.current) &&
                    Mathf.Abs(o.voltage - v) < .01f && Mathf.Abs(o.current - v / Resistance) < .005f) == 1);
    }
}
