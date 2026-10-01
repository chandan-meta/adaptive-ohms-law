using UnityEngine;

namespace OhmsLaw
{
    // Billboard for world text seen from several distances: faces the main camera (same convention as Face():
    // the label's +Z points away from the viewer) and scales with depth / refDepth, clamped, so a label tuned for
    // one shot keeps roughly the same screen size in the others. Scale is applied on top of the build-time scale.
    public class WorldLabel : MonoBehaviour
    {
        [Tooltip("Depth in metres the label's size was tuned for. 0 = face the camera without rescaling.")]
        public float refDepth;
        public float minScale = WorldText.BillboardMin, maxScale = WorldText.BillboardMax;

        Vector3 baseScale;
        bool cached;
        Camera cam;

        void LateUpdate()
        {
            if (!cam) cam = Camera.main;
            if (!cam) return;
            if (!cached) { baseScale = transform.localScale; cached = true; }
            var eye = cam.transform.position;
            var toLabel = transform.position - eye;
            if (toLabel.sqrMagnitude < 1e-6f) return;
            transform.rotation = Quaternion.LookRotation(toLabel);
            if (refDepth <= 0) return;
            float depth = Vector3.Dot(toLabel, cam.transform.forward);
            transform.localScale = baseScale * Mathf.Clamp(depth / refDepth, minScale, maxScale);
        }
    }
}
