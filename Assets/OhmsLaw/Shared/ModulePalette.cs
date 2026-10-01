using TMPro;
using UnityEngine;

namespace OhmsLaw
{
    // Shared look for every scene: concept colours plus the UI font and sprites.
    [CreateAssetMenu(menuName = "Ohm's Law/Module Palette")]
    public class ModulePalette : ScriptableObject
    {
        public Color voltage = new Color32(0x3F, 0xA9, 0xFF, 255);
        public Color current = new Color32(0xFF, 0xB8, 0x30, 255);
        public Color resistance = new Color32(0xFF, 0x5A, 0x5F, 255);
        public Color charge = new Color32(0xD6, 0xFA, 0xFF, 255);
        public Color wattsonAccent = new Color32(0xBF, 0xF6, 0xFF, 255);
        public Color panel = new Color(0.07f, 0.09f, 0.13f, 0.82f);
        public TMP_FontAsset font;
        public Sprite roundedPanel, circle;
        public Texture2D wattsonFace;
    }
}
