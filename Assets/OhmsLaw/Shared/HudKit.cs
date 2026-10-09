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
                // The v3 set, authored on the 24-unit grid of the design's SVG icons (U converts), strokes ~2.3 units.
                "check" => p => Mathf.Min(Seg(p, U(5, 12.6f), U(9.4f, 17), 1.5f), Seg(p, U(9.4f, 17), U(19, 7.4f), 1.5f)),
                "target" => p => Mathf.Min(Vector2.Distance(p, U(12, 12)) - 3.2f / 24f, Mathf.Abs(Vector2.Distance(p, U(12, 12)) - 8f / 24f) - 1.1f / 24f),
                "lens" => p => Mathf.Min(Mathf.Abs(Vector2.Distance(p, U(10.5f, 10.5f)) - 6f / 24f) - 1.2f / 24f, Seg(p, U(15, 15), U(20, 20), 1.2f)),
                "x" => p => Mathf.Min(Seg(p, U(6.5f, 6.5f), U(17.5f, 17.5f), 1.3f), Seg(p, U(17.5f, 6.5f), U(6.5f, 17.5f), 1.3f)),
                "lock" => p => Mathf.Min(Mathf.Abs(Box(p, U(12, 15.5f), new Vector2(7f / 24f, 5f / 24f), 2.2f / 24f)) - 1.1f / 24f,
                                         Mathf.Min(Arc(p, U(12, 8), 3.8f / 24f, 2.2f / 24f, 0f, 180f), Mathf.Min(Seg(p, U(8.2f, 8), U(8.2f, 10.5f), 1.1f), Seg(p, U(15.8f, 8), U(15.8f, 10.5f), 1.1f)))),
                "list" => p => Mathf.Min(Mathf.Min(Seg(p, U(9, 6.5f), U(20, 6.5f), 1.1f), Mathf.Min(Seg(p, U(9, 12), U(20, 12), 1.1f), Seg(p, U(9, 17.5f), U(20, 17.5f), 1.1f))),
                                         Mathf.Min(Vector2.Distance(p, U(4.6f, 6.5f)), Mathf.Min(Vector2.Distance(p, U(4.6f, 12)), Vector2.Distance(p, U(4.6f, 17.5f)))) - 1.4f / 24f),
                "exit" => p => Mathf.Min(Mathf.Min(Seg(p, U(10, 4.5f), U(5, 4.5f), 1.1f), Mathf.Min(Seg(p, U(5, 4.5f), U(5, 19.5f), 1.1f), Seg(p, U(5, 19.5f), U(10, 19.5f), 1.1f))),
                                         Mathf.Min(Seg(p, U(14.5f, 8), U(18.5f, 12), 1.1f), Mathf.Min(Seg(p, U(18.5f, 12), U(14.5f, 16), 1.1f), Seg(p, U(18.5f, 12), U(9.5f, 12), 1.1f)))),
                "mic" => p => Mathf.Min(Mathf.Abs(Box(p, U(12, 8.75f), new Vector2(3f / 24f, 5.75f / 24f), 3f / 24f)) - 1.1f / 24f,
                                        Mathf.Min(Arc(p, U(12, 11.2f), 6.5f / 24f, 2.2f / 24f, 180f, 360f), Seg(p, U(12, 17.8f), U(12, 21), 1.1f))),
                "send" => p => Mathf.Min(Seg(p, U(5, 12), U(18, 12), 1.3f), Mathf.Min(Seg(p, U(12.5f, 6), U(18.5f, 12), 1.3f), Seg(p, U(18.5f, 12), U(12.5f, 18), 1.3f))),
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

        // A point on the design's 24-unit icon grid, in the unit square (y down, like the SVGs).
        static Vector2 U(float x, float y) => new Vector2(x / 24f, y / 24f);

        // A stroke from a to b with round caps; halfWidth in grid units.
        static float Seg(Vector2 p, Vector2 a, Vector2 b, float halfWidth)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t) - halfWidth / 24f;
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

    // Normal / hover / pressed / on / disabled / focused for a HUD button (design v3): a soft scale (1.03 hover, 0.97
    // pressed), a white wash on hover and press, the "on" fill for a toggle that is on (CC) or a choice that is current,
    // 36 % when not interactable, a 3 px spark ring while Tab has the keyboard focus on it. Colours are set per role
    // (control, primary, secondary ...); a tap on a disabled button calls disabledTap so the HUD can say why. Unscaled
    // time; no scale under reduced motion.
    public class HudButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Image fill, outline;
        public bool active;
        public Color fillColor = Color.clear, activeFill = new Color(1f, 1f, 1f, 0.14f), outlineColor = Color.clear, activeOutline = Color.clear;
        public float hoverScale = 1.03f, pressScale = 0.97f, corner = 18f;
        public System.Action disabledTap;

        Button button;
        CanvasGroup group;
        Image ring;
        bool hover, pressed;
        float scale = 1f, dim = 1f;

        void Awake()
        {
            button = GetComponent<Button>();
            if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; if (Interactable) UiSounds.Play("hover"); }
        public void OnPointerExit(PointerEventData e) { hover = false; pressed = false; }
        public void OnPointerDown(PointerEventData e) { pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        public void OnPointerClick(PointerEventData e) { if (!Interactable) disabledTap?.Invoke(); }

        bool Interactable => !button || button.interactable;

        // A wash toward white that also lifts a transparent fill (a bare control gets a faint white pill).
        static Color Wash(Color c, float k) => c.a < 0.3f ? new Color(1f, 1f, 1f, c.a + k) : Color.Lerp(c, new Color(1f, 1f, 1f, c.a), k);

        void Update()
        {
            bool on = Interactable;
            float target = !on ? 1f : pressed ? pressScale : hover ? hoverScale : 1f;
            if (ReducedMotion.On) target = 1f;
            float ease = 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
            scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.localScale = Vector3.one * scale;
            // Written only while dimming or easing back: otherwise the group's alpha belongs to whoever fades it (the chapter
            // chip, the choice rows), and writing it every frame would undo their fades.
            float to = on ? 1f : 0.36f;
            if (dim != to)
            {
                dim = Mathf.Abs(dim - to) < 0.01f ? to : Mathf.Lerp(dim, to, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
                group.alpha = dim;
            }
            if (fill)
            {
                var c = active ? activeFill : fillColor;
                if (hover && on) c = Wash(c, 0.08f);
                if (pressed && on) c = Wash(c, 0.12f);
                fill.color = Color.Lerp(fill.color, c, ease);
            }
            if (outline) outline.color = Color.Lerp(outline.color, active ? activeOutline : outlineColor, ease);

            bool focused = HudKeys.Focused(gameObject);
            if (focused && !ring)
            {
                var rect = UI.Stretch("Focus", transform);
                rect.offsetMin = new Vector2(-4f, -4f);
                rect.offsetMax = new Vector2(4f, 4f);
                rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                ring = UI.Outline(rect, LessonPanel.Spark, corner + 4f, 3f);
            }
            if (ring && ring.enabled != focused) ring.enabled = focused;
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
            source.outputAudioMixerGroup = AudioBus.Group("UI");
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
            float seconds = name switch { "hover" => 0.03f, "click" => 0.06f, "done" => 0.26f, "stamp" => 0.3f, "hint" => 0.12f, _ => 0.14f };
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            var noise = new System.Random(3);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate, u = i / (float)n;
                float f = name switch
                {
                    "hover" => 1900f,
                    "click" => Mathf.Lerp(1300f, 750f, u),
                    "open" => Mathf.Lerp(520f, 980f, u),
                    "done" => u < 0.45f ? 880f : 1318.5f,           // a rising fifth: the action landed
                    "stamp" => Mathf.Lerp(150f, 70f, u),             // a soft thud under the CASE CLOSED stamp
                    "hint" => 660f,
                    _ => Mathf.Lerp(980f, 520f, u), // close
                };
                float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 6f)) * Mathf.Pow(1f - u, name == "hover" ? 1f : 1.6f);
                float s = Mathf.Sin(2f * Mathf.PI * f * t);
                if (name == "stamp") s = 0.75f * s + 0.25f * (float)(noise.NextDouble() * 2 - 1) * (1f - u);
                data[i] = s * env * 0.5f;
            }
            var clip = AudioClip.Create("ui " + name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
