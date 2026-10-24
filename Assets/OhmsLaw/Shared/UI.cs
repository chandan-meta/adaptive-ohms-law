using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Small helpers for the code-built module UI. Sizes are in 1920x1080 reference pixels; the type scale is the
    // "Screen UI" table in Docs/Polish/TYPOGRAPHY.md, the colours are STYLE_GUIDE.md section 8.
    public static class UI
    {
        public const float MinTarget = 88f;

        // Screen UI type scale (reference px).
        public const float ButtonFont = 34f, ScreenButtonFont = 36f;
        public const float SubtitleFont = 44f, SpeakerFont = 28f, SubtitleTop = 236f;
        public const float MCQQuestionFont = 40f, MCQOptionFont = 36f, MCQHintFont = 30f, MCQRow = 108f;
        public const float PromptFont = 42f, SliderTickFont = 30f;

        // HUD panels (subtitles, MCQ, prompt, battery) sit at this alpha so 3D text and particles never bleed through.
        public const float HudAlpha = 0.95f;

        public static ModulePalette Theme;

        public static Color PanelColor => Theme ? Theme.panel : new Color(0.07f, 0.09f, 0.13f, 0.82f);
        public static Color HudColor => Alpha(PanelColor, HudAlpha);
        public static Color AccentColor => Theme ? Theme.voltage : new Color(0.25f, 0.66f, 1f);
        // Fill for the blue accent buttons (Record, x2, prompt pill): the panel colour pulled toward voltage-blue.
        public static Color AccentFill => Alpha(Color.Lerp(PanelColor, AccentColor, 0.55f), HudAlpha);
        // Option tiles and other "raised" surfaces on a HUD panel.
        public static Color RaisedColor => Alpha(Color.Lerp(PanelColor, Color.white, 0.1f), 1f);

        public static Color Alpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        // corner: on-screen corner radius in reference px (the sliced sprite's border is rescaled to it; 16 = as drawn).
        public static Image Panel(RectTransform rect, Color color, bool round = true, float corner = 16f)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            if (round && Theme && Theme.roundedPanel)
            {
                image.sprite = Theme.roundedPanel;
                image.type = Image.Type.Sliced;
                float border = Mathf.Max(1f, Theme.roundedPanel.border.x);
                image.pixelsPerUnitMultiplier = border / Mathf.Max(1f, corner);
            }
            return image;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var label = Stretch("Label", parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (FigmaSkin.Available && FigmaSkin.BodyFont) label.font = FigmaSkin.BodyFont;
            else if (Theme && Theme.font) label.font = Theme.font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        static TMP_FontAsset display;
        static bool displayLooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { display = null; displayLooked = false; }

        // The display face (kids UI): Fredoka SDF, built by BlockoutAssets into Shared/Resources with the body font as its
        // fallback; the body font itself until that asset exists. Display text only: titles, hero plates, readouts, the stamp.
        public static TMP_FontAsset DisplayFont
        {
            get
            {
                if (FigmaSkin.Available && FigmaSkin.DisplayFont) return FigmaSkin.DisplayFont;
                if (!display && !displayLooked) { displayLooked = true; display = Resources.Load<TMP_FontAsset>("Fredoka SDF"); }
                return display ? display : Theme ? Theme.font : null;
            }
        }

        public static TextMeshProUGUI Display(TextMeshProUGUI label)
        {
            var font = DisplayFont;
            if (label && font) label.font = font;
            return label;
        }

        public static Button Button(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, string text, UnityAction onClick, float fontSize = ButtonFont)
        {
            var rect = Rect(name, parent, anchor, pivot, position, new Vector2(Mathf.Max(size.x, MinTarget), Mathf.Max(size.y, MinTarget)));
            Panel(rect, PanelColor);
            FigmaSkin.Apply(rect.GetComponent<Image>(), "SecondaryNormal");
            var button = rect.gameObject.AddComponent<Button>();
            if (onClick != null) button.onClick.AddListener(onClick);
            // Always present, even for "": callers fetch it with GetComponentInChildren<TextMeshProUGUI>() and set the text later.
            var label = Label(rect, text ?? "", fontSize, Color.white);
            label.rectTransform.offsetMin = new Vector2(14, 6);
            label.rectTransform.offsetMax = new Vector2(-14, -6);
            if (FigmaSkin.Available)
            {
                button.transition = Selectable.Transition.None;
                var hud = rect.gameObject.AddComponent<HudButton>();
                hud.fill = rect.GetComponent<Image>();
                hud.figmaStyle = "Secondary";
                hud.figmaForeground = new Graphic[] { label };
            }
            else Hover(button);
            return button;
        }

        // Hover / pressed feedback as a white wash over whatever colour the button's panel has. A ColorBlock can only
        // darken a tinted sprite, so the wash is its own stretched image whose alpha the Button drives. The panel on
        // the button's GameObject stays the Image callers recolour (GetComponent<Image>().color = ...).
        public static Image Hover(Button button)
        {
            var wash = Panel(Stretch("Hover", button.transform), Color.white);
            wash.raycastTarget = false;
            wash.canvasRenderer.SetColor(Color.clear); // already at "normal", so assigning the graphic below starts no tween
            // Selectable.Awake made the panel the target: detach it first, or the ColorBlock below cross-fades the panel to clear.
            button.targetGraphic = null;
            button.colors = new ColorBlock
            {
                normalColor = Color.clear,
                highlightedColor = new Color(1, 1, 1, 0.10f),
                pressedColor = new Color(1, 1, 1, 0.24f),
                selectedColor = Color.clear,
                disabledColor = Color.clear,
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
            button.targetGraphic = wash;
            return wash;
        }

        // Uses the fancy glyph only when the font really has it.
        public static string Glyph(string preferred, string fallback) =>
            Theme && Theme.font && Theme.font.HasCharacters(preferred) ? preferred : fallback;

        static readonly Dictionary<int, Sprite> circles = new Dictionary<int, Sprite>();

        // Crisp anti-aliased disc (hole 0) or ring (hole = inner radius as a fraction of the outer), built once per
        // hole size. The built-in Knob sprite is 32 px and goes soft at the 96-150 px the module draws.
        public static Sprite Circle(float hole = 0f)
        {
            int key = Mathf.RoundToInt(Mathf.Clamp01(hole) * 100f);
            if (circles.TryGetValue(key, out var sprite) && sprite) return sprite;
            const int size = 128;
            float outer = size * 0.5f - 1f, inner = outer * key / 100f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "UI Circle" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                    float a = Mathf.Clamp01(outer - d + 0.5f) * (inner > 0 ? Mathf.Clamp01(d - inner + 0.5f) : 1f); // 1 px soft edges
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key > 0 ? "Ring" : "Disc";
            circles[key] = sprite;
            return sprite;
        }

        static readonly Dictionary<int, Sprite> outlines = new Dictionary<int, Sprite>();

        // Rounded-rectangle outline with no fill, 9-sliced, scaled so its corner radius is `corner` and its line `thickness`
        // reference px. Callout frames draw it over their fill: a filled border panel under a translucent fill tints the fill
        // and shows through it while the card fades.
        public static Image Outline(RectTransform rect, Color color, float corner, float thickness)
        {
            const int r = 32, size = 2 * r + 2; // corner slices of r px around a 2 px stretchable centre
            int key = Mathf.RoundToInt(Mathf.Clamp(thickness / Mathf.Max(1f, corner), 0.02f, 1f) * 100f);
            if (!outlines.TryGetValue(key, out var sprite) || !sprite)
            {
                float t = r * key / 100f, half = size * 0.5f;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "UI Outline" };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        // Signed distance to the rounded rectangle (negative inside); the ring is -t..0 with 1 px soft edges.
                        float qx = Mathf.Abs(x + 0.5f - half) - (half - r), qy = Mathf.Abs(y + 0.5f - half) - (half - r);
                        float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                        float a = Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(d + t + 0.5f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.one * r);
                sprite.name = "Outline";
                outlines[key] = sprite;
            }
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.pixelsPerUnitMultiplier = r / Mathf.Max(1f, corner);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }

    // Fades a HUD element in when it appears and, after Dismiss(), out before destroying it. Unscaled time, so it
    // runs while the Timeline is held at a gate. Added by code only (never serialised), hence it may live in UI.cs.
    public class UIFade : MonoBehaviour
    {
        public float inSeconds = LessonPanel.PanelSeconds, outSeconds = 0.18f;   // exits faster than entries

        CanvasGroup group;
        bool dismissed;

        void Awake()
        {
            if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
        }

        public void Dismiss()
        {
            dismissed = true;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        void Update()
        {
            float target = dismissed ? 0f : 1f, seconds = dismissed ? outSeconds : inSeconds;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds));
            if (dismissed) { if (group.alpha <= 0f) Destroy(gameObject); }
            else if (!group.interactable && group.alpha >= 0.9f) group.interactable = true; // taps count once the panel is readable
        }
    }

    // Keeps a readout label in step with a value the slider mirrors (the slider itself is set with
    // SetValueWithoutNotify from code, which raises no event).
    public class SliderReadout : MonoBehaviour
    {
        public TMP_Text label;
        public System.Func<float> value;
        public string format = "0.0 V";

        void LateUpdate()
        {
            if (label && value != null) label.text = value().ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
