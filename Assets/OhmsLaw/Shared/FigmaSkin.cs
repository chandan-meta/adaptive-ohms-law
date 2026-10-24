using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Figma exports supply the artwork; the existing HUD keeps its layout and behavior.
    public static class FigmaSkin
    {
        const string Root = "FigmaUI/";
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static TMP_FontAsset bodyFont, displayFont;
        static bool bodyLoaded, displayLoaded;

        public static bool Available => Get("Panel");
        public static TMP_FontAsset BodyFont => LoadFont("Inter", ref bodyFont, ref bodyLoaded);
        public static TMP_FontAsset DisplayFont => LoadFont("SpaceGrotesk", ref displayFont, ref displayLoaded);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            sprites.Clear();
            bodyFont = displayFont = null;
            bodyLoaded = displayLoaded = false;
        }

        public static Sprite Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (!sprites.TryGetValue(name, out var sprite))
            {
                sprite = Resources.Load<Sprite>(Root + name);
                sprites[name] = sprite;
            }
            return sprite;
        }

        public static bool Apply(Image image, string name, bool sliced = true)
        {
            if (!image) return false;
            var sprite = Get(name);
            if (!sprite) return false;
            image.sprite = sprite;
            image.color = Color.white;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            if (image.pixelsPerUnitMultiplier != 1f) image.pixelsPerUnitMultiplier = 1f;
            image.preserveAspect = false;
            return true;
        }

        static TMP_FontAsset LoadFont(string name, ref TMP_FontAsset asset, ref bool loaded)
        {
            var fallback = UI.Theme ? UI.Theme.font : null;
            if (!loaded)
            {
                loaded = true;
                var source = Resources.Load<Font>(Root + "Fonts/" + name);
                if (source)
                {
                    asset = TMP_FontAsset.CreateFontAsset(source);
                    if (asset)
                    {
                        asset.name = name + " Figma UI (Dynamic)";
                        asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                        asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    }
                }
            }
            if (asset && fallback && fallback != asset && !asset.fallbackFontAssetTable.Contains(fallback))
                asset.fallbackFontAssetTable.Add(fallback);
            return asset ? asset : fallback;
        }
    }
}
