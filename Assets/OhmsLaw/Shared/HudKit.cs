using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Vector-style icons for the HUD, rasterised once from signed-distance shapes (64 px, 2 px soft edge), so they stay
    // crisp at the 22-28 px they are drawn at and need no icon font or texture asset.
    public static class Icons
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        const int Size = 64;

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var sprite) && sprite) return sprite;
            System.Func<Vector2, float> field = name switch
            {
                "pause" => p => Mathf.Min(Box(p, new Vector2(0.31f, 0.5f), new Vector2(0.09f, 0.32f), 0.04f), Box(p, new Vector2(0.69f, 0.5f), new Vector2(0.09f, 0.32f), 0.04f)),
                "play" => p => Tri(p, new Vector2(0.3f, 0.16f), new Vector2(0.3f, 0.84f), new Vector2(0.86f, 0.5f)),
                "replay" => p => Mathf.Min(Arc(p, new Vector2(0.5f, 0.5f), 0.32f, 0.11f, 50f, 330f), Tri(p, new Vector2(0.62f, 0.12f), new Vector2(0.94f, 0.24f), new Vector2(0.66f, 0.42f))),
                "skip" => p => Mathf.Min(Tri(p, new Vector2(0.1f, 0.2f), new Vector2(0.1f, 0.8f), new Vector2(0.5f, 0.5f)), Tri(p, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.8f), new Vector2(0.9f, 0.5f))),
                "volume" => p => Mathf.Min(Mathf.Min(Box(p, new Vector2(0.2f, 0.5f), new Vector2(0.1f, 0.13f), 0.02f), Tri(p, new Vector2(0.22f, 0.36f), new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.86f))),
                                           Mathf.Min(Tri(p, new Vector2(0.22f, 0.64f), new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.86f)),
                                                     Mathf.Min(Arc(p, new Vector2(0.5f, 0.5f), 0.24f, 0.07f, -40f, 40f), Arc(p, new Vector2(0.5f, 0.5f), 0.38f, 0.07f, -45f, 45f)))),
                "dot" => p => Vector2.Distance(p, new Vector2(0.5f, 0.5f)) - 0.4f,
                _ => p => Box(p, new Vector2(0.5f, 0.5f), new Vector2(0.3f, 0.3f), 0.08f),
            };
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Icon " + name };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    // 2x2 supersampling of the distance field, 1.5 px soft edge; y flipped so the shapes read with y down.
                    float a = 0f;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            var p = new Vector2((x + 0.25f + 0.5f * sx) / Size, 1f - (y + 0.25f + 0.5f * sy) / Size);
                            a += Mathf.Clamp01(0.5f - field(p) * Size / 1.5f);
                        }
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 0.25f * 255f));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "Icon " + name;
            cache[name] = sprite;
            return sprite;
        }

        // Signed distances in the unit square (negative inside).
        static float Box(Vector2 p, Vector2 centre, Vector2 half, float round)
        {
            var q = new Vector2(Mathf.Abs(p.x - centre.x), Mathf.Abs(p.y - centre.y)) - half + Vector2.one * round;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - round;
        }

        // Either winding: the sign of the triangle's area orients the edges so that inside is negative.
        static float Tri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s = Mathf.Sign(Cross(b - a, c - a));
            return Mathf.Max(Edge(p, a, b, s), Mathf.Max(Edge(p, b, c, s), Edge(p, c, a, s)));
        }

        static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

        // Signed distance to the line a->b: negative on the triangle's inner side (given its winding s).
        static float Edge(Vector2 p, Vector2 a, Vector2 b, float s) => -s * Cross(b - a, p - a) / Mathf.Max(1e-5f, (b - a).magnitude);

        static float Arc(Vector2 p, Vector2 centre, float radius, float thickness, float fromDeg, float toDeg)
        {
            var d = p - centre;
            float ring = Mathf.Abs(d.magnitude - radius) - thickness * 0.5f;
            float angle = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg; // 0 = right, 90 = up (y down in the field)
            if (angle < 0) angle += 360f;
            float a = fromDeg < 0 ? fromDeg + 360f : fromDeg, b = toDeg < 0 ? toDeg + 360f : toDeg;
            bool inside = a <= b ? angle >= a && angle <= b : angle >= a || angle <= b;
            if (inside) return ring;
            // Outside the sector: distance to the nearer end cap (round caps).
            Vector2 End(float deg) => centre + new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), -Mathf.Sin(deg * Mathf.Deg2Rad)) * radius;
            return Mathf.Min(Vector2.Distance(p, End(a)), Vector2.Distance(p, End(b))) - thickness * 0.5f;
        }

    }

    // Normal / hover / pressed / active / disabled for a HUD button: a soft scale (1.03 hover, 0.97 pressed), the fill
    // and outline brightening, the whole button dimmed when not interactable. Unscaled time; no scale under reduced motion.
    public class HudButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image fill, outline;
        public bool active;                 // a toggle that is on (CC ON): accent tint
        public Color fillColor = new Color(0.03f, 0.05f, 0.09f, 0.72f), outlineColor = new Color(0.27f, 0.63f, 1f, 0.35f);
        public float hoverScale = 1.03f, pressScale = 0.97f;

        Button button;
        CanvasGroup group;
        bool hover, pressed;
        float scale = 1f;

        void Awake()
        {
            button = GetComponent<Button>();
            if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; if (Interactable) UiSounds.Play("hover"); }
        public void OnPointerExit(PointerEventData e) { hover = false; pressed = false; }
        public void OnPointerDown(PointerEventData e) { pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }

        bool Interactable => !button || button.interactable;

        void Update()
        {
            bool on = Interactable;
            float target = !on ? 1f : pressed ? pressScale : hover ? hoverScale : 1f;
            if (ReducedMotion.On) target = 1f;
            scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.localScale = Vector3.one * scale;
            group.alpha = Mathf.Lerp(group.alpha, on ? 1f : 0.45f, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            var accent = UI.AccentColor;
            if (fill)
            {
                var c = active ? Color.Lerp(fillColor, accent, 0.28f) : fillColor;
                if (hover && on) c = Color.Lerp(c, Color.white, 0.07f);
                if (pressed && on) c = Color.Lerp(c, Color.white, 0.12f);
                fill.color = Color.Lerp(fill.color, c, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            }
            if (outline)
            {
                var c = active ? UI.Alpha(accent, 0.8f) : outlineColor;
                if (hover && on) c = UI.Alpha(accent, 0.75f);
                outline.color = Color.Lerp(outline.color, c, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            }
        }
    }

    // Very quiet interface sounds (hover tick, click, open, close), synthesised once at runtime: no asset, always under
    // the narration, on the effects bus so the volume setting covers them.
    public static class UiSounds
    {
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioSource source;
        static float lastHover;

        public static void Attach(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            var bus = host.AddComponent<AudioBus>();
            bus.kind = AudioBus.Kind.Sfx;
            bus.gain = 0.35f;
        }

        public static void Play(string name)
        {
            if (!source) return;
            if (name == "hover") { if (Time.unscaledTime - lastHover < 0.08f) return; lastHover = Time.unscaledTime; }
            if (!clips.TryGetValue(name, out var clip)) clips[name] = clip = Make(name);
            source.PlayOneShot(clip, name == "hover" ? 0.35f : 0.8f);
        }

        static AudioClip Make(string name)
        {
            const int rate = 44100;
            float seconds = name == "hover" ? 0.03f : name == "click" ? 0.06f : 0.14f;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate, u = i / (float)n;
                float f = name switch
                {
                    "hover" => 1900f,
                    "click" => Mathf.Lerp(1300f, 750f, u),
                    "open" => Mathf.Lerp(520f, 980f, u),
                    _ => Mathf.Lerp(980f, 520f, u), // close
                };
                float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 6f)) * Mathf.Pow(1f - u, name == "hover" ? 1f : 1.6f);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.5f;
            }
            var clip = AudioClip.Create("ui " + name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
