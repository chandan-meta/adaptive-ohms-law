using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace OhmsLaw
{
    // Floating world-space words, symbols and formulas, sized by capital-letter height in metres.
    // Text faces -Z, so it reads from a camera on its -Z side looking along +Z (Face / LookRotation(pos - cam)).
    // Sizes come from Docs/Polish/TYPOGRAPHY.md: builders pick a tier from Cap/Wrap instead of inventing numbers.
    public static class WorldText
    {
        public enum Look { Glow, Panel, Plain }   // Scene 4 HDR word / rounded dark backing / bare text

        // Cap heights in metres, each tuned for the shot that reads it (see the TYPOGRAPHY size table).
        public static class Cap
        {
            public const float PropLabel = 0.03f;       // prop name, Bench Close
            public const float Caption = 0.025f;        // meter caption
            public const float Reading = 0.04f;         // meter digits
            public const float Terminal = 0.04f;        // X / Y badges
            public const float PathLabel = 0.045f;
            public const float DataCard = 0.15f;        // R = V/I, V ∝ I, CONSTANT... on the Boards shot
            public const float Omega = 0.4f;
            public const float LetterCard = 0.26f;      // "V" with its name as a 36 % sub-line
            public const float LetterSub = LetterCard * 0.36f;
            public const float HeroFormula = 0.3f;      // V = IR
            public const float Title = 0.32f;           // OHM'S LAW
            public const float ConceptWord = 0.23f;     // VOLTAGE / CURRENT / RESISTANCE inside the wire
            public const float ConceptSub = 0.13f;      // Symbol / Unit sub-lines
            public const float Question = 0.4f;         // WHAT MAKES CHARGE FLOW?
            public const float Counter = 0.5f, CounterCaption = 0.14f;
            public const float ProseCard = 0.11f;       // S07 question, S09 reminder
            public const float ProseSub = 0.09f;        // "Your prediction: ..." under the reminder
            public const float SymbolPair = 0.095f;     // S07 "Voltage → V" / "Current → I"
            public const float Name = 0.04f;            // capsule / stand-in names
            public const float VfxTag = 0.035f;
            public const float Ink = 0.012f;            // notebook pages
        }

        // Screen targets (cap height in 1080p pixels) behind the Cap tiers; use CapAt() when a card is read from a
        // camera other than the one its tier was tuned for (the study's Boards camera is 2.7 m away, not 3.5 m).
        public static class Px
        {
            public const float Tick = 20f, Small = 24f, Medium = 28f, Large = 36f;
            public const float DataCard = 56f, ConceptWord = 64f, Hero = 115f, Title = 120f, Omega = 150f;
        }

        // Wrap widths in metres for the tiers that wrap.
        public static class Wrap
        {
            public const float PropLabel = 0.45f, Caption = 0.45f, PathLabel = 1.2f;
            public const float DataCard = 2.4f, Concept = 2.6f, ProseCard = 2.7f, Question = 6f, Ink = 0.22f;
        }

        // Nunito SDF face: pointSize 90, capLine 64, lineHeight 122.76. 3D TextMeshPro puts 0.1 world unit per fontSize em.
        public const float CapToFontSize = 10f * 90f / 64f;        // 14.0625
        public const float LineAdvancePerCap = 122.76f / 64f;       // 1.918 cap per line at lineSpacing 0
        public const float PadX = 0.8f, PadY = 0.5f;                // backing padding per side, in caps
        public const float CapsLineSpacing = -20f, ProseLineSpacing = 0f;
        public const float BillboardMin = 0.7f, BillboardMax = 2.5f;

        static readonly Regex Tags = new Regex("<[^>]+>");

        public static float FontSizeForCap(TMP_FontAsset f, float capMetres)
        {
            if (!f || f.faceInfo.capLine <= 0) return capMetres * CapToFontSize;
            var face = f.faceInfo;
            return capMetres * 10f * face.pointSize / (face.capLine * (face.scale > 0 ? face.scale : 1f));
        }

        // Baseline-to-baseline distance in metres; lineSpacing is TMP's em/100 unit.
        public static float LineAdvance(TMP_FontAsset f, float capMetres, float lineSpacing)
        {
            float perCap = LineAdvancePerCap, emPerCap = 90f / 64f;
            if (f && f.faceInfo.capLine > 0) { perCap = f.faceInfo.lineHeight / f.faceInfo.capLine; emPerCap = f.faceInfo.pointSize / f.faceInfo.capLine; }
            return capMetres * (perCap + lineSpacing * 0.01f * emPerCap);
        }

        // Cap height that renders `px` pixels tall (1080 rows) at `depth` metres from a camera with this vertical FOV.
        public static float CapForScreenPx(float px, float depth, float fovDeg) =>
            px * 2f * depth * Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad) / 1080f;

        // Same, for text at `position` seen by a camera at `cameraPos` looking at `cameraLook` (depth along the view axis).
        public static float CapAt(float px, Vector3 position, Vector3 cameraPos, Vector3 cameraLook, float fovDeg) =>
            CapForScreenPx(px, Mathf.Max(0.1f, Vector3.Dot(position - cameraPos, (cameraLook - cameraPos).normalized)), fovDeg);

        // No lowercase letters outside rich-text tags: formula and hero cards get the tight line spacing.
        public static bool AllCaps(string text)
        {
            foreach (char c in Tags.Replace(text ?? "", "")) if (char.IsLower(c)) return false;
            return true;
        }

        // "V\n<size=36%>Potential Difference</size>": a headline with smaller sub-lines, both tiers from Cap.
        public static string Sub(string headline, string sub, float cap, float subCap) =>
            headline + "\n<size=" + Mathf.RoundToInt(subCap / cap * 100f) + "%>" + sub + "</size>";

        public static Color PanelColor => UI.Theme
            ? new Color(UI.Theme.panel.r, UI.Theme.panel.g, UI.Theme.panel.b, 0.92f)
            : new Color(0.06f, 0.08f, 0.12f, 0.92f);

        // height = cap height in metres. width > 0 wraps at that width. backing != null adds the rounded panel
        // (RoundedPanel sprite, 9-sliced, or a quad with that material when the sprite is missing).
        // lineSpacing defaults to -20 for all-caps text and 0 for prose.
        public static TextMeshPro Card(string name, string text, Color color, Transform parent, Vector3 localPosition, float height = Cap.DataCard, Material backing = null, float width = 0, float? lineSpacing = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var label = go.AddComponent<TextMeshPro>();
            if (UI.Theme && UI.Theme.font) label.font = UI.Theme.font;
            label.text = text;
            label.color = color;
            label.fontSize = FontSizeForCap(label.font, height);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.lineSpacing = lineSpacing ?? (AllCaps(text) ? CapsLineSpacing : ProseLineSpacing);
            label.paragraphSpacing = 0;
            label.textWrappingMode = width > 0 ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            // Rect height from the real line count (wrapping decides it), plus a cap of breathing room.
            label.rectTransform.sizeDelta = new Vector2(width > 0 ? width : 20f, 50f);
            label.ForceMeshUpdate(true, true); // layout now, even if a parent is still inactive (needs an awake component)
            int lines = Mathf.Max(1, label.textInfo.lineCount);
            label.rectTransform.sizeDelta = new Vector2(label.rectTransform.sizeDelta.x, lines * LineAdvance(label.font, height, label.lineSpacing) + height);
            label.ForceMeshUpdate(true, true);
            if (backing) Backing(label, height, width, backing);
            return label;
        }

        static void Backing(TextMeshPro label, float cap, float wrap, Material material)
        {
            var bounds = label.textBounds;
            var size = new Vector2(Mathf.Max(0.02f, bounds.size.x + cap * 2f * PadX), Mathf.Max(0.02f, bounds.size.y + cap * 2f * PadY));
            if (wrap > 0) size.x = Mathf.Min(size.x, wrap + cap * 2f * PadX);
            var sprite = UI.Theme ? UI.Theme.roundedPanel : null;
            var color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : PanelColor;
            if (sprite)
            {
                // 9-sliced panel: the sprite's 16 px corners at 100 px/unit are 0.16 m, scaled to ~0.4 cap.
                var panel = new GameObject("Backing").AddComponent<SpriteRenderer>();
                panel.transform.SetParent(label.transform, false);
                float s = Mathf.Max(0.01f, 0.4f * cap / 0.16f);
                panel.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0.01f);
                panel.transform.localScale = Vector3.one * s;
                panel.sprite = sprite;
                panel.drawMode = SpriteDrawMode.Sliced;
                panel.size = size / s;
                panel.color = color;
                panel.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                if (pipeline && pipeline.default2DMaterial) panel.sharedMaterial = pipeline.default2DMaterial; // URP's unlit sprite shader
                return;
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "Backing";
            quad.transform.SetParent(label.transform, false);
            quad.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0.01f);
            quad.transform.localScale = new Vector3(size.x, size.y, 1);
            quad.GetComponent<Renderer>().sharedMaterial = material;
        }

        // Scene 4's word recipe: HDR face material (the colour lives in the material, so the vertex colour goes white)
        // and the PopIn with bob. The spark burst is the builder's job (it needs a Glow material asset).
        public static PopIn Glow(TextMeshPro label, Material faceMaterial)
        {
            if (faceMaterial) { label.fontSharedMaterial = faceMaterial; label.color = Color.white; }
            var pop = label.TryGetComponent(out PopIn existing) ? existing : label.gameObject.AddComponent<PopIn>();
            pop.duration = 0.55f;
            pop.overshoot = 1.45f;
            pop.bob = 0.025f;
            return pop;
        }

        // Faces the main camera every frame and scales with depth / refDepth (clamped 0.7-2.5) so a label tuned
        // for one shot keeps its screen size in the others. refDepth 0 = billboard only.
        public static WorldLabel Billboard(Component target, float refDepth)
        {
            var label = target.TryGetComponent(out WorldLabel existing) ? existing : target.gameObject.AddComponent<WorldLabel>();
            label.refDepth = refDepth;
            return label;
        }
    }
}
