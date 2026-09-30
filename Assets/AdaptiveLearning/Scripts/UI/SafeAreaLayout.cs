using UnityEngine;
namespace AdaptiveOhmsLaw
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaLayout : MonoBehaviour
    {
        Rect last; Vector2 size;
        void Update()
        {
            var safe = Screen.safeArea;
            if (last == safe && size == new Vector2(Screen.width, Screen.height)) return;
            last = safe; size = new Vector2(Screen.width, Screen.height);
            if (size.x <= 0 || size.y <= 0) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = safe.position / size; rect.anchorMax = (safe.position + safe.size) / size;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
