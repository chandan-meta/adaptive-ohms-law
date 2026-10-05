using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Screen-space callouts (Docs/Polish/ART_DIRECTION.md section 4): object callouts with a leader line to a world
    // target, compact "[VFX]" panels while an effect plays, and concept panels. They live on the module canvas, so
    // their size is the same at every depth and in every shot. Builders attach a CalloutDef to the target; the cues
    // "callout show <id>" / "callout hide <id>" / "callout hideall" drive CalloutLayer, which places the panels so
    // they never cover the top-right HUD, the subtitle band or each other. Everything here is added by code at
    // runtime (never serialised), which is why these MonoBehaviours may share a file.

    // One panel on the layer. Subclasses build the visuals and measure `size`; the layer owns placement and fading.
    // Child rects use anchor and pivot (0, 1), so a layout-space point (x right, y down from the top-left corner)
    // is anchoredPosition (x, -y).
    public abstract class Callout : MonoBehaviour
    {
        public const float Corner = 14f, Border = 2f, FadeSeconds = 0.25f;
        public static readonly Color Orange = new Color32(0xFF, 0xB8, 0x30, 255), Navy = new Color(0x1B / 255f, 0x22 / 255f, 0x33 / 255f, 0.95f);
        public static readonly Color Magenta = new Color32(0xE0, 0x40, 0xFB, 255), VfxFill = new Color(0x14 / 255f, 0x10 / 255f, 0x1F / 255f, 0.95f);
        public static readonly Color Cyan = new Color32(0x4F, 0xD1, 0xFF, 255), DeepBlue = new Color(0x0F / 255f, 0x1B / 255f, 0x33 / 255f, 0.92f);

        public string id;
        public CalloutDef def;                      // text source and world target; null for a cue-driven VFX panel
        public bool hasDef;
        public CalloutDef.Anchor anchor = CalloutDef.Anchor.TopLeft;
        public Vector2 aboveOffset = new Vector2(0, 90);
        public GameObject source;                   // VFX: the effect object; the panel hides when it is deactivated
        public bool hasSource, hiding, placed;
        public Vector2 size, position;              // measured once when built; layout-space top-left, eased by the layer
        public Scene scene;

        protected CalloutLayer layer;
        protected RectTransform card;
        CanvasGroup group;

        public void Init(CalloutLayer layer, string id, CalloutDef def, string title, string body)
        {
            this.layer = layer;
            this.id = id;
            this.def = def;
            hasDef = def != null;
            if (def) { anchor = def.anchor; aboveOffset = def.aboveOffset; }
            scene = def ? def.gameObject.scene : SceneManager.GetActiveScene();
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            card = UI.Rect("Panel", transform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            Build(title, body, def ? def.diagram : CalloutDef.Diagram.None);
            card.sizeDelta = size;
        }

        protected abstract void Build(string title, string body, CalloutDef.Diagram diagram);

        public void Place(Vector2 topLeft) => card.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);

        // marker: the target in layout space, or null when it is behind the camera or off the frame.
        public virtual void Track(Vector2? marker) { }

        // True once a hidden panel has faded out completely.
        public bool Fade(float unscaledDelta)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, hiding ? 0f : 1f, unscaledDelta / FadeSeconds);
            return hiding && group.alpha <= 0f;
        }

        // Rounded fill (the card's own image) under an outline-only border, both before the text in the hierarchy so the
        // text draws over them. Nothing coloured sits beneath the translucent fill, so it stays dark while the card fades.
        protected void Frame(Color border, Color fill) => Draw.Bordered(card, Corner, border, fill);

        // A label at a layout-space position inside `parent`, sized to its measured text.
        protected static TextMeshProUGUI Label(RectTransform parent, string name, string text, float font, Color color, Vector2 topLeft, Vector2 size, TextOverflowModes overflow = TextOverflowModes.Overflow)
        {
            var label = UI.Label(UI.Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(topLeft.x, -topLeft.y), size), text, font, color, TextAlignmentOptions.TopLeft);
            label.overflowMode = overflow;
            return label;
        }

        // Preferred size of `text` at `font` px: on one line when width is 0, wrapped to `width` otherwise.
        protected Vector2 Measure(float font, string text, float width = 0)
        {
            var probe = layer.Probe;
            probe.fontSize = font;
            var size = width > 0 ? probe.GetPreferredValues(text, width, 0) : probe.GetPreferredValues(text);
            return new Vector2(Mathf.Ceil(size.x), Mathf.Ceil(size.y));
        }

        protected float LineHeight(float font) => Measure(font, "Ag").y;

        // Title and wrapped body stacked at the top-left, as the object and concept panels share. Bodies longer than
        // maxLines are cut with an ellipsis. Returns the text block's width and the panel height it needs.
        protected Vector2 TextBlock(string title, string body, float titleFont, float bodyFont, int maxLines, float padX, float padTop, float padBottom, float gap, float minWidth, float maxTextWidth)
        {
            var titleSize = Measure(titleFont, title);
            var bodySize = string.IsNullOrEmpty(body) ? Vector2.zero : Measure(bodyFont, body);
            float textWidth = Mathf.Clamp(Mathf.Max(Mathf.Max(titleSize.x, bodySize.x), minWidth - 2 * padX), 0, maxTextWidth);
            if (titleSize.x > textWidth) titleSize = Measure(titleFont, title, textWidth);
            var overflow = TextOverflowModes.Overflow;
            if (bodySize.x > textWidth)
            {
                bodySize = Measure(bodyFont, body, textWidth);
                float line = LineHeight(bodyFont);
                if (Mathf.RoundToInt(bodySize.y / line) > maxLines) { bodySize.y = line * maxLines; overflow = TextOverflowModes.Ellipsis; }
            }
            Label(card, "Title", title, titleFont, Orange, new Vector2(padX, padTop), new Vector2(textWidth, titleSize.y));
            float bottom = padTop + titleSize.y;
            if (bodySize.y > 0)
            {
                Label(card, "Body", body, bodyFont, Color.white, new Vector2(padX, bottom + gap), new Vector2(textWidth, bodySize.y), overflow);
                bottom += gap + bodySize.y;
            }
            return new Vector2(textWidth, bottom + padBottom);
        }
    }

    // Dark navy panel, orange title (40 px: 28 px caps, the prop-name tier), white description (30 px: the 21 px cap
    // floor of TYPOGRAPHY.md, two lines at most), a 2 px orange leader to a 12 px marker dot on the world target.
    public class ObjectCallout : Callout
    {
        public const float TitleFont = 40f, BodyFont = 30f, MaxWidth = 580f;
        const float PadX = 24f, PadTop = 12f, PadBottom = 16f, TitleGap = 2f, MinWidth = 220f, LeaderWidth = 2f, Marker = 12f, Rim = 18f;
        const int MaxLines = 2;

        Image leader, dot, rim;

        protected override void Build(string title, string body, CalloutDef.Diagram diagram)
        {
            Frame(Orange, Navy);
            var block = TextBlock(title, body, TitleFont, BodyFont, MaxLines, PadX, PadTop, PadBottom, TitleGap, MinWidth, MaxWidth - 2 * PadX);
            size = new Vector2(block.x + 2 * PadX, block.y);

            // Leader beneath the card (never across its text), marker dot over it with a dark rim for contrast.
            leader = Draw.Bar(transform, Vector2.zero, Vector2.right, LeaderWidth, Orange);
            leader.name = "Leader";
            leader.transform.SetAsFirstSibling();
            rim = Draw.Disc(transform, Vector2.zero, Rim, new Color(0.08f, 0.1f, 0.14f, 0.7f));
            rim.name = "Marker Rim";
            dot = Draw.Disc(transform, Vector2.zero, Marker, Orange);
            dot.name = "Marker";
            Track(null);
        }

        public override void Track(Vector2? marker)
        {
            var start = Vector2.zero;
            bool show = marker.HasValue && LeaderStart(new Rect(position, size), marker.Value, out start);
            leader.enabled = dot.enabled = rim.enabled = show;
            if (!show) return;
            var m = marker.Value;
            Draw.Set(leader, start, m, LeaderWidth);
            dot.rectTransform.anchoredPosition = rim.rectTransform.anchoredPosition = new Vector2(m.x, -m.y);
        }

        // Nearest point on the card's edge to the marker, kept off the rounded corners. False when the marker is
        // under the card (nothing to draw).
        static bool LeaderStart(Rect card, Vector2 marker, out Vector2 start)
        {
            start = Vector2.zero;
            if (card.Contains(marker)) return false;
            float x = Mathf.Clamp(marker.x, card.xMin + Corner, card.xMax - Corner);
            float y = Mathf.Clamp(marker.y, card.yMin + Corner, card.yMax - Corner);
            if (marker.y > card.yMax) start = new Vector2(x, card.yMax);
            else if (marker.y < card.yMin) start = new Vector2(x, card.yMin);
            else start = new Vector2(marker.x > card.xMax ? card.xMax : card.xMin, y);
            return true;
        }
    }

    // Compact magenta-outlined panel: a waveform glyph, "[VFX]" in magenta and the effect's name in white. Shown
    // while the effect plays - by the cue that shows the "[VFX] ..." object, or by a CalloutDef of kind Vfx.
    public class VfxPanel : Callout
    {
        public const float TextFont = 32f, MaxWidth = 560f;
        const float PadX = 20f, PadY = 14f, GlyphWidth = 44f, GlyphHeight = 28f, GlyphGap = 14f;
        static readonly Regex Hyphenated = new Regex(@"\S+-\S+");

        protected override void Build(string title, string body, CalloutDef.Diagram diagram)
        {
            Frame(Magenta, VfxFill);
            // Display only: a hyphenated word ("Ohm-niscope", "time-freeze") never breaks at its hyphen; the id keeps the plain name.
            string text = "<color=#E040FB>[VFX]</color> " + Hyphenated.Replace(title, "<nobr>$0</nobr>");
            float maxText = MaxWidth - 2 * PadX - GlyphWidth - GlyphGap;
            var textSize = Measure(TextFont, text);
            if (textSize.x > maxText) textSize = Measure(TextFont, text, maxText);
            float textLeft = PadX + GlyphWidth + GlyphGap;
            float height = Mathf.Max(textSize.y, GlyphHeight) + 2 * PadY;
            Label(card, "Text", text, TextFont, Color.white, new Vector2(textLeft, (height - textSize.y) / 2), textSize);
            size = new Vector2(textLeft + textSize.x + PadX, height);

            // Waveform glyph: flat, spike up, spike down, flat.
            var glyph = UI.Rect("Glyph", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(PadX, -(height - GlyphHeight) / 2), new Vector2(GlyphWidth, GlyphHeight));
            float mid = GlyphHeight / 2;
            Vector2[] points = { new Vector2(0, mid), new Vector2(11, mid), new Vector2(17, 2), new Vector2(25, GlyphHeight - 2), new Vector2(31, mid), new Vector2(GlyphWidth, mid) };
            for (int i = 1; i < points.Length; i++) Draw.Bar(glyph, points[i - 1], points[i], 3f, Magenta);
        }
    }

    // Dark-blue panel with a cyan edge: orange title (44 px), white body (32 px, three lines at most) and an
    // optional cyan diagram (a cell with field arrows, or a conductor with three charges) at the right. No leader.
    public class ConceptPanel : Callout
    {
        public const float TitleFont = 44f, BodyFont = 32f;
        // The diagrams are drawn in a 150 x 110 box and shown at DiagramScale (180 x 132 px).
        const float PadX = 26f, PadTop = 16f, PadBottom = 20f, TitleGap = 6f, MinWidth = 360f, MaxTextWidth = 460f, DiagramWidth = 150f, DiagramHeight = 110f, DiagramScale = 1.2f, DiagramGap = 24f;
        const int MaxLines = 3;

        protected override void Build(string title, string body, CalloutDef.Diagram diagram)
        {
            Frame(Cyan, DeepBlue);
            var block = TextBlock(title, body, TitleFont, BodyFont, MaxLines, PadX, PadTop, PadBottom, TitleGap, MinWidth, MaxTextWidth);
            bool drawn = diagram != CalloutDef.Diagram.None;
            float w = DiagramWidth * DiagramScale, h = DiagramHeight * DiagramScale;
            size = new Vector2(block.x + 2 * PadX + (drawn ? DiagramGap + w : 0), Mathf.Max(block.y, drawn ? h + PadTop + PadBottom : 0));
            if (!drawn) return;
            var box = UI.Rect("Diagram", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(size.x - PadX - w, -(size.y - h) / 2), new Vector2(DiagramWidth, DiagramHeight));
            box.localScale = Vector3.one * DiagramScale;
            if (diagram == CalloutDef.Diagram.Battery) Battery(box); else Tube(box);
        }

        // A cell (+ above, − below) and three field arrows pointing right.
        static void Battery(RectTransform box)
        {
            const float cellX = 14f, cellW = 40f, cellTop = 26f, cellH = 58f;
            Draw.Outline(box, new Rect(cellX, cellTop, cellW, cellH), 8f, Cyan, DeepBlue);
            Draw.Bar(box, new Vector2(cellX + 8, cellTop + cellH / 2), new Vector2(cellX + cellW - 8, cellTop + cellH / 2), 2f, Cyan);
            Label(box, "Plus", "+", 26f, Cyan, new Vector2(cellX, -2), new Vector2(cellW, 26)).alignment = TextAlignmentOptions.Center;
            Label(box, "Minus", UI.Glyph("−", "-"), 26f, Cyan, new Vector2(cellX, cellTop + cellH - 2), new Vector2(cellW, 26)).alignment = TextAlignmentOptions.Center;
            for (int i = 0; i < 3; i++) Draw.Arrow(box, new Vector2(70, 30 + i * 25), new Vector2(DiagramWidth - 8, 30 + i * 25), 3f, 11f, Cyan);
        }

        // A conductor (capsule outline) with three cyan charges, each with a dark "−" bar and a short motion streak.
        static void Tube(RectTransform box)
        {
            const float tubeH = 56f, tubeY = (DiagramHeight - tubeH) / 2, charge = 18f;
            Draw.Outline(box, new Rect(4, tubeY, DiagramWidth - 8, tubeH), tubeH / 2, Cyan, DeepBlue);
            for (int i = 0; i < 3; i++)
            {
                var centre = new Vector2(34 + i * 41, DiagramHeight / 2);
                Draw.Bar(box, centre + new Vector2(-24, 0), centre + new Vector2(-13, 0), 2f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f));
                Draw.Disc(box, centre, charge, Cyan);
                Draw.Bar(box, centre + new Vector2(-5, 0), centre + new Vector2(5, 0), 2.5f, new Color(0.06f, 0.1f, 0.2f));
            }
        }
    }

    // Line art from UI Images, in layout space (x right, y down from the parent's top-left corner).
    static class Draw
    {
        static readonly Vector2 TopLeft = new Vector2(0, 1);

        public static Image Bar(Transform parent, Vector2 from, Vector2 to, float thickness, Color color)
        {
            var image = UI.Rect("Bar", parent, TopLeft, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            Set(image, from, to, thickness);
            return image;
        }

        public static void Set(Image bar, Vector2 from, Vector2 to, float thickness)
        {
            var d = to - from;
            bar.rectTransform.anchoredPosition = new Vector2(from.x, -from.y);
            bar.rectTransform.sizeDelta = new Vector2(d.magnitude, thickness);
            bar.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg);
        }

        public static Image Disc(Transform parent, Vector2 centre, float diameter, Color color)
        {
            var image = UI.Rect("Disc", parent, TopLeft, new Vector2(0.5f, 0.5f), new Vector2(centre.x, -centre.y), Vector2.one * diameter).gameObject.AddComponent<Image>();
            image.sprite = UI.Circle();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // Shaft plus two head strokes meeting at `to`.
        public static void Arrow(Transform parent, Vector2 from, Vector2 to, float thickness, float head, Color color)
        {
            Bar(parent, from, to, thickness, color);
            var dir = (to - from).normalized;
            var back = to - dir * head;
            var side = new Vector2(-dir.y, dir.x) * head * 0.7f;
            Bar(parent, to + dir * thickness * 0.5f, back + side, thickness, color);
            Bar(parent, to + dir * thickness * 0.5f, back - side, thickness, color);
        }

        // Rounded outline around a fill, at a layout-space rect inside `parent`.
        public static RectTransform Outline(Transform parent, Rect r, float corner, Color border, Color fill)
        {
            var rect = UI.Rect("Outline", parent, TopLeft, TopLeft, new Vector2(r.x, -r.y), r.size);
            Bordered(rect, corner, border, fill);
            return rect;
        }

        // The fill on `rect` itself, an outline-only ring (callout border width) over it: no border colour under the fill.
        public static void Bordered(RectTransform rect, float corner, Color border, Color fill)
        {
            UI.Panel(rect, fill, true, corner).raycastTarget = false;
            UI.Outline(UI.Stretch("Border", rect), border, corner, Callout.Border);
        }
    }

    // The canvas layer under the module HUD: created on first use beneath ModuleFlow's SafeArea children (so the
    // subtitles, controls and Ask Wattson draw over it) and kept across scenes; callouts die with their scene.
    // Layout runs when the CinemachineBrain reports the camera placed for the frame, so markers never lag the shot.
    public class CalloutLayer : MonoBehaviour
    {
        public const string Prefix = "[VFX] ";
        // Reserved regions in reference px: the top-right controls and the subtitle band (ART_DIRECTION section 4).
        public const float HudWidth = 900f, HudHeight = 130f, SubtitleBand = 240f;
        public const float Margin = 36f, Gap = 16f, Ease = 14f;
        // HUD elements SceneDirector and MCQPanel put on the SafeArea while they are up; callouts move out from under them.
        static readonly string[] HudSiblings = { "Prompt", "MCQ Panel", "Voltage Control", "Record", "Times Two" };

        static CalloutLayer instance;

        // Null until a ModuleFlow exists (a scene played on its own has no module canvas).
        public static CalloutLayer Instance
        {
            get
            {
                if (instance) return instance;
                var flow = ModuleFlow.Instance;
                var safe = flow ? flow.transform.Find("SafeArea") : null;
                if (!safe) return null;
                var rect = UI.Stretch("Callouts", safe);
                rect.SetAsFirstSibling();
                return instance = rect.gameObject.AddComponent<CalloutLayer>();
            }
        }

        // Disabled label the panels measure their text with (TMP needs an awake component to measure).
        public TextMeshProUGUI Probe { get; private set; }

        readonly List<Callout> callouts = new List<Callout>();
        readonly Dictionary<string, CalloutDef> defs = new Dictionary<string, CalloutDef>();
        readonly List<Rect> blocked = new List<Rect>();
        readonly Vector3[] corners = new Vector3[4];
        RectTransform rect, safeArea;
        Canvas canvas;
        int steppedFrame = -1;

        void Awake()
        {
            rect = (RectTransform)transform;
            safeArea = transform.parent as RectTransform;
            canvas = GetComponentInParent<Canvas>();
            var group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            Probe = UI.Label(UI.Rect("Measure", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), "", ObjectCallout.BodyFont, Color.white, TextAlignmentOptions.TopLeft);
            Probe.enabled = false;
            SceneManager.sceneUnloaded += SceneUnloaded;
        }

        void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(CameraUpdated);
        void OnDisable() => CinemachineCore.CameraUpdatedEvent.RemoveListener(CameraUpdated);

        void OnDestroy()
        {
            SceneManager.sceneUnloaded -= SceneUnloaded;
            if (instance == this) instance = null;
        }

        // The brain has placed the camera for this frame: lay out now. Without a brain (or one that stopped
        // updating) LateUpdate drives the layer instead.
        void CameraUpdated(CinemachineBrain brain) => Step();
        void LateUpdate() { if (CinemachineBrain.ActiveBrainCount == 0 || Time.frameCount - steppedFrame > 1) Step(); }

        // The scene's defs are gone: its panels fade (cue-driven VFX panels included); those whose def or effect
        // object was destroyed go at once in the next Step.
        void SceneUnloaded(Scene unloaded)
        {
            defs.Clear();
            foreach (var c in callouts) if (c && c.scene == unloaded) c.hiding = true;
        }

        // ------------------------------------------------------------------ API

        public void Show(string id)
        {
            var shown = Find(id);
            if (shown) { shown.hiding = false; return; } // no duplicates; a panel on its way out comes back
            var def = Def(id);
            if (!def) { Debug.LogWarning($"Callout '{id}' has no CalloutDef in the loaded scenes"); return; }
            if (def.kind == CalloutDef.Kind.Concept) Add<ConceptPanel>(id, def, def.title, def.description);
            else if (def.kind == CalloutDef.Kind.Object) Add<ObjectCallout>(id, def, def.title, def.description);
            else
            {
                var panel = Add<VfxPanel>(id, def, def.title, "");
                panel.source = def.gameObject; // shown while the effect object is active
                panel.hasSource = true;
            }
        }

        public void Hide(string id)
        {
            var shown = Find(id);
            if (shown) shown.hiding = true;
        }

        public void HideAll()
        {
            foreach (var c in callouts) if (c) c.hiding = true;
        }

        // A "[VFX] ..." object was shown or hidden by a cue: mirror it with a panel carrying the same text (minus the
        // prefix). `source` is the effect object when the scene has one; the panel then also hides when the effect
        // is deactivated or destroyed, and a CalloutDef on it (if a builder added one) sets the panel's anchor.
        public void Vfx(string name, bool on, GameObject source = null)
        {
            string text = name.StartsWith(Prefix) ? name.Substring(Prefix.Length) : name;
            string id = "vfx:" + text;
            if (!on) { Hide(id); return; }
            var shown = Find(id);
            if (shown) { shown.hiding = false; return; }
            var panel = Add<VfxPanel>(id, null, text, "");
            panel.source = source;
            panel.hasSource = source != null;
            if (source && source.TryGetComponent(out CalloutDef placement)) { panel.anchor = placement.anchor; panel.aboveOffset = placement.aboveOffset; }
        }

        // ------------------------------------------------------------------ internals

        Callout Find(string id)
        {
            foreach (var c in callouts) if (c && c.id == id) return c;
            return null;
        }

        // Defs are looked up by id across the loaded scenes (inactive targets included) and cached until a scene
        // unloads; a miss rescans once, so defs added at runtime are found too.
        CalloutDef Def(string id)
        {
            if (defs.TryGetValue(id, out var def) && def) return def;
            defs.Clear();
            foreach (var d in FindObjectsByType<CalloutDef>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(d.id)) defs[d.id] = d;
            return defs.TryGetValue(id, out def) && def ? def : null;
        }

        T Add<T>(string id, CalloutDef def, string title, string body) where T : Callout
        {
            var callout = UI.Stretch(id, transform).gameObject.AddComponent<T>();
            callout.Init(this, id, def, title, body);
            callouts.Add(callout);
            return callout;
        }

        // One pass per frame: cull dead panels, place every callout clear of the reserved regions and of each other
        // (VFX panels first, so they keep the top slot the references give them and object callouts make way), draw
        // the leaders, run the fades.
        void Step()
        {
            if (steppedFrame == Time.frameCount) return;
            steppedFrame = Time.frameCount;
            float dt = Time.unscaledDeltaTime;
            var frame = rect.rect.size;
            var cam = Camera.main;

            // Its def or effect object was destroyed (the scene is gone): no fade, just go.
            for (int i = callouts.Count - 1; i >= 0; i--)
            {
                var c = callouts[i];
                if (c && !(c.hasDef && !c.def) && !(c.hasSource && !c.source)) continue;
                if (c) Destroy(c.gameObject);
                callouts.RemoveAt(i);
            }

            Blocked(frame);
            bool topLeftTaken = callouts.Exists(c => !(c is VfxPanel) && c.anchor == CalloutDef.Anchor.TopLeft && !c.hiding);
            for (int pass = 0; pass < 2; pass++)
                foreach (var c in callouts)
                {
                    if ((c is VfxPanel) != (pass == 0)) continue;
                    if (c.hasSource && !c.source.activeInHierarchy) c.hiding = true; // the effect ended
                    bool wantsMarker = c is ObjectCallout || c.anchor == CalloutDef.Anchor.AboveTarget;
                    Vector2? marker = c.def && wantsMarker ? Marker(cam, c.def.WorldPoint, frame) : null;
                    var target = Resolve(c, Slot(c, frame, marker, topLeftTaken), frame);
                    c.position = c.placed ? Vector2.Lerp(c.position, target, 1 - Mathf.Exp(-Ease * dt)) : target;
                    c.placed = true;
                    c.Place(c.position);
                    c.Track(marker);
                    if (!c.hiding) blocked.Add(new Rect(c.position, c.size)); // a panel on its way out does not push the next one aside
                }

            for (int i = callouts.Count - 1; i >= 0; i--)
                if (callouts[i].Fade(dt)) { Destroy(callouts[i].gameObject); callouts.RemoveAt(i); }
        }

        // Reserved regions this frame: the HUD corner, the subtitle band and whichever prompt, MCQ or battery control
        // is up. Callouts are appended as they are placed, so later ones stack clear of earlier ones.
        void Blocked(Vector2 frame)
        {
            blocked.Clear();
            blocked.Add(new Rect(frame.x - HudWidth, 0, HudWidth, HudHeight));
            blocked.Add(new Rect(0, frame.y - SubtitleBand, frame.x, SubtitleBand));
            if (!safeArea) return;
            for (int i = 0; i < safeArea.childCount; i++)
            {
                var child = safeArea.GetChild(i) as RectTransform;
                if (!child || !child.gameObject.activeSelf || System.Array.IndexOf(HudSiblings, child.name) < 0) continue;
                child.GetWorldCorners(corners);
                var min = rect.InverseTransformPoint(corners[0]);
                var max = rect.InverseTransformPoint(corners[2]);
                blocked.Add(new Rect(min.x + frame.x / 2, frame.y / 2 - max.y, max.x - min.x, max.y - min.y));
            }
        }

        // The world target in layout space; null behind the camera or outside the frame.
        Vector2? Marker(Camera cam, Vector3 world, Vector2 frame)
        {
            if (!cam) return null;
            var screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0) return null;
            var eventCamera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, eventCamera, out var local)) return null;
            var p = new Vector2(local.x + frame.x / 2, frame.y / 2 - local.y);
            return p.x < 0 || p.y < 0 || p.x > frame.x || p.y > frame.y ? null : p;
        }

        // The anchor's home position for this panel. Top anchors start at the margin (Resolve pushes them below the
        // HUD when they reach into it); mid anchors centre in the band between the HUD and the subtitles.
        Vector2 Slot(Callout c, Vector2 frame, Vector2? marker, bool topLeftTaken)
        {
            var anchor = c.anchor;
            if (c is VfxPanel && anchor == CalloutDef.Anchor.TopLeft && topLeftTaken) anchor = CalloutDef.Anchor.TopCentre;
            float left = Margin, right = frame.x - Margin - c.size.x, centre = (frame.x - c.size.x) / 2;
            float top = Margin, bottom = frame.y - SubtitleBand - Gap - c.size.y, mid = (HudHeight + frame.y - SubtitleBand - c.size.y) / 2;
            switch (anchor)
            {
                case CalloutDef.Anchor.TopLeft: return new Vector2(left, top);
                case CalloutDef.Anchor.TopCentre: return new Vector2(centre, top);
                case CalloutDef.Anchor.TopRight: return new Vector2(right, top);
                case CalloutDef.Anchor.MidLeft: return new Vector2(left, mid);
                case CalloutDef.Anchor.MidRight: return new Vector2(right, mid);
                case CalloutDef.Anchor.BottomLeft: return new Vector2(left, bottom);
                case CalloutDef.Anchor.BottomCentre: return new Vector2(centre, bottom);
                case CalloutDef.Anchor.BottomRight: return new Vector2(right, bottom);
                default:
                    if (!marker.HasValue) return c.placed ? c.position : new Vector2(centre, top);
                    var m = marker.Value;
                    float x = Mathf.Clamp(m.x - c.size.x / 2 + c.aboveOffset.x, left, right);
                    float y = m.y - c.aboveOffset.y - c.size.y;
                    if (y < Margin) y = m.y + c.aboveOffset.y; // no room above the target: below it
                    return new Vector2(x, y);
            }
        }

        // Pushes the panel out of every reserved region and earlier callout: top and mid anchors stack downward,
        // bottom anchors upward, AboveTarget goes up while there is room and down otherwise.
        Vector2 Resolve(Callout c, Vector2 topLeft, Vector2 frame)
        {
            var r = new Rect(topLeft, c.size);
            bool upward = c.anchor == CalloutDef.Anchor.BottomLeft || c.anchor == CalloutDef.Anchor.BottomCentre || c.anchor == CalloutDef.Anchor.BottomRight;
            bool above = c.anchor == CalloutDef.Anchor.AboveTarget;
            for (int pass = 0; pass < 12; pass++)
            {
                bool moved = false;
                foreach (var o in blocked)
                {
                    if (!r.Overlaps(o)) continue;
                    float up = o.yMin - Gap - r.height, down = o.yMax + Gap;
                    r.y = upward || (above && up >= Margin) ? up : down;
                    moved = true;
                }
                if (!moved) break;
            }
            r.x = Mathf.Clamp(r.x, Margin, Mathf.Max(Margin, frame.x - Margin - r.width));
            return r.position;
        }
    }
}
