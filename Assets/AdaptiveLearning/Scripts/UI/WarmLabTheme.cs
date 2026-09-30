using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    // Shared by authored UI and the chat controls created when a lesson starts.
    [CreateAssetMenu(menuName = "Adaptive Learning/Warm Lab Theme")]
    public sealed class WarmLabTheme : ScriptableObject
    {
        public Font body, heading, control;
        public Sprite rounded;
        public static readonly Color Ivory = new Color32(247, 243, 234, 255);
        public static readonly Color Ink = new Color32(38, 49, 46, 255);
        public static readonly Color Amber = new Color32(242, 181, 68, 255);
        public static readonly Color Sage = new Color32(66, 107, 85, 255);
        public static readonly Color Plum = new Color32(115, 91, 131, 255);
        public static readonly Color Paper = new Color32(232, 231, 218, 255);
        static WarmLabTheme cached;
        public static WarmLabTheme Current => cached != null ? cached : cached = Resources.Load<WarmLabTheme>("WarmLabTheme");

        public void Apply(Transform root)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                string n = image.name.ToLowerInvariant();
                if (image.GetComponentInParent<Button>() != null || image.color.a < .2f ||
                    n.Contains("fade") || n.Contains("highlight") || n.Contains("marker") ||
                    n.Contains("point") || n.Contains("line") || n.Contains("axis") || n.Contains("overlay")) continue;
                if (image.color.maxColorComponent < .65f)
                {
                    image.color = n.Contains("input") || n.Contains("scroll") || n.StartsWith("row") ? Paper : Ivory;
                    image.sprite = rounded; image.type = Image.Type.Sliced;
                }
            }
            foreach (var label in root.GetComponentsInChildren<Text>(true))
            {
                bool title = label.name.Contains("Title") || label.name == "Lesson";
                label.font = title ? heading : body;
                label.fontStyle = FontStyle.Normal;
                label.color = HasPaperBehind(label.transform) ? Ink : Ivory;
                if (label.name == "Speaker") label.color = Plum;
                if (label.name == "Progress") label.color = Sage;
            }
            foreach (var button in root.GetComponentsInChildren<Button>(true)) StyleButton(button);
            foreach (var outline in root.GetComponentsInChildren<Outline>(true))
                if (outline.GetComponent<Graphic>() != null) outline.enabled = false;
        }
        static bool HasPaperBehind(Transform node)
        {
            for (var parent = node.parent; parent != null; parent = parent.parent)
            {
                var image = parent.GetComponent<Image>();
                if (image != null && image.color.a > .5f) return image.color.grayscale > .6f;
            }
            return false;
        }
        public void StyleButton(Button button)
        {
            string n = button.name.ToLowerInvariant();
            if (n.StartsWith("plot_") || n.StartsWith("point_") || n == "voltageknob") return;
            bool companion = n.Contains("wattson") || n.Contains("voice");
            bool primary = n.Contains("continue") || n.Contains("next") || n.Contains("finish") || n == "send" || n.Contains("record") || n.Contains("check");
            bool secondary = n.Contains("skip") || n.Contains("replay") || n.Contains("closechat");
            var tint = companion ? Plum : primary ? Amber : secondary ? Ivory : Sage;
            if (button.targetGraphic is Image image) { image.sprite = rounded; image.type = Image.Type.Sliced; image.color = Color.white; }
            var colors = button.colors;
            colors.normalColor = tint;
            colors.highlightedColor = Color.Lerp(tint, Color.white, .13f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(tint, Ink, .22f);
            colors.disabledColor = primary || secondary ? Paper : Color.Lerp(tint, Ink, .45f);
            colors.colorMultiplier = 1; colors.fadeDuration = .12f; button.colors = colors;
            foreach (var label in button.GetComponentsInChildren<Text>(true))
            { label.font = control; label.fontStyle = FontStyle.Normal; label.color = primary || secondary ? Ink : Ivory; }
            foreach (var outline in button.GetComponentsInChildren<Outline>(true)) outline.enabled = false;
        }
    }
}
