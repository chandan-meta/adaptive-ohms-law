using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace OhmsLaw
{
    // The module's one HUD (design v3, "Case File": Docs/Polish/HUD_DESIGN.md), built once by ModuleFlow and kept across
    // scenes. The 3D scene owns the screen; the interface sits around it:
    //   UI_TopControls      top-right: one capsule, PAUSE · REPLAY · CC · VOLUME · SKIP (folds to icons when idle in a
    //                       cinematic chapter; a tap on a disabled control says why)
    //   UI_SubtitlePanel    bottom, centred in the free frame: the speaker and the line (SubtitleUI fills it; CC hides it)
    //   UI_AskWattson       bottom-right: Wattson's round avatar, the shortcut to his conversation
    //   UI_Dock             right, under the controls: cards only while a beat needs them (the turn, a clue, the steps,
    //                       readings, the counter, the schematic, the inset, the table and graph, formulas, the finale)
    //   the chapter banner, the pause menu and the title screen live in HudScreens.cs
    //   v5 "Live Wire" surfaces, each centred in the free frame: UI_Hero (a reveal plate), UI_TopReadings (the live V / I
    //                       strip at the top), UI_Stage (the table, graph and formula cards in front of the 3D view),
    //                       UI_Popup3D (a card showing a RenderTexture the learner can turn), UI_VoltageStrip (the battery slider)
    // Sizes are 1920x1080 reference px; HudScale keeps the canvas from shrinking below 75 %. Colour means something:
    // the chrome is two-tone navy glass with an electric-blue edge and glow (the kids UI, Docs/Polish/KIDS_UI), amber is the
    // one primary action, Wattson's spark marks guidance and "done", green / red only ever mean right / wrong, and
    // blue / amber / red only ever mean V, I, R. Display text (titles, hero plates, readouts, the stamp) is Fredoka
    // (UI.DisplayFont); everything else Nunito Sans.
    public class LessonPanel : MonoBehaviour
    {
        public static LessonPanel Instance { get; private set; }

        // Layout (reference px).
        public const float NormalWidth = 480f, WideWidth = 580f, Margin = 32f, TopInset = 28f, BottomInset = 40f;
        public const float Pad = 22f, Gap = 12f, Corner = 18f;
        public const float ControlHeight = 56f, ControlItem = 44f, ButtonHeight = 60f, DockTop = TopInset + ControlHeight + 20f, DockBottom = 196f;
        public const float CaptionMax = 1080f, AvatarSize = 92f;
        // Type scale (v3 floor: nothing under 15 px at 1080p).
        public const float ControlFont = 16f, InstructionFont = 28f, HintFont = 20f, HeadingFont = 16f, CaptionFont = 32f, SpeakerFont = 15f;
        public const float InfoTitleFont = 24f, InfoFont = 21f, FormulaFont = 34f, RevealFont = 64f, RevealSubFont = 22f;
        // ponytail: StageScale 1.1, not the brief's 1.2: the stage shows the dock-sized table and graph cards side by side
        // (1153 px before scaling), and 1.2 would overrun the 1408 px free frame; bigger needs taller view holders on the stage.
        public const float HeroFont = 120f, HeroSubFont = 26f, StageScale = 1.1f, Popup3DWidth = 980f, Popup3DView = 440f, VoltStripWidth = 720f, VoltStripBottom = 212f;
        // Motion (DESIGN_SPEC §6): panels enter over 0.22 s easing out, buttons settle in 0.14 s; exits are faster; fades
        // only under reduced motion.
        public const float PanelSeconds = 0.22f, ButtonSeconds = 0.14f;

        // Block order in the dock (top to bottom). Names are what callers ask Block() for. A question (MCQ) sits right
        // under its turn card, above the data it asks about.
        public static readonly string[] Order = { "Instruction", "Info", "Volt", "Readings", "Counter", "Schematic", "Inset", "MCQ", "Table", "Graph", "Formula", "Spacer", "End" };
        static readonly HashSet<string> Bare = new HashSet<string> { "Spacer" }; // draws no card
        static readonly HashSet<string> Keep = new HashSet<string> { "Edge", "Plate", "Shine", "Glow", "Halo", "Brackets" }; // a card's own layers, kept by Clear()
        static readonly HashSet<string> StageBlocks = new HashSet<string> { "Table", "Graph", "Formula" };

        public RectTransform Root { get; private set; }
        public RectTransform Dock { get; private set; }
        public RectTransform Column { get; private set; }
        public RectTransform Conversation { get; private set; }   // Ask Wattson builds its conversation here; shown while it is open
        public RectTransform Caption { get; private set; }        // SubtitleUI builds the speaker and text labels here
        public RectTransform AskRoot { get; private set; }        // avatar + label (the spotlight scales this)
        public RectTransform Controls { get; private set; }
        public Button AskButton { get; private set; }
        public bool Wide { get; private set; }
        public string Title { get; private set; } = "";

        // Colour roles (kids UI, Docs/Polish/KIDS_UI/DESIGN_SPEC.md §1): ink on every bright fill, text on navy.
        public static readonly Color Ink = new Color32(0x15, 0x19, 0x1D, 0xFF);
        public static readonly Color Paper = new Color32(0xF5, 0xF0, 0xE6, 0xFF);
        public static readonly Color Body = new Color32(0xF5, 0xF0, 0xE6, 0xFF);
        public static readonly Color Muted = new Color32(0xC1, 0xBA, 0xB0, 0xFF);
        public static readonly Color Raised = new Color32(0x30, 0x37, 0x3B, 0xFF);
        public static readonly Color HoverFill = new Color32(0x34, 0x39, 0x3D, 0xFF);
        // Electric blue for edges, chips, selected rows and progress; cyan glow for focus and hover; amber / gold for the one
        // primary action; green / red only ever mean right / wrong (each with a word and an icon, never colour alone).
        public static readonly Color Accent = new Color32(0xFF, 0xC4, 0x5C, 0xFF);
        public static readonly Color Glow = new Color32(0xFF, 0xF9, 0xEC, 0xFF);
        public static readonly Color Amber = new Color32(0xFF, 0xC4, 0x5C, 0xFF);
        public static readonly Color Gold = new Color32(0xFF, 0xD0, 0x70, 0xFF);
        public static readonly Color Success = new Color32(0x57, 0xE7, 0xB5, 0xFF);
        public static readonly Color Error = new Color32(0xFF, 0xB5, 0xB5, 0xFF);
        // Glass: cards are two-tone navy, a panel-navy rim (Glass at GlassAlpha) around a deep-navy plate, with a soft top
        // highlight, a 1.5 px electric-blue edge and a faint blue glow outside it. The project renders in linear colour
        // space, where UI alpha blends in linear light (0.8 of navy over a white bench reads as a mid grey, and a tenth of a
        // bright poster still ghosts through), so every card carries the inset plate (PlateAlpha) under its content: ~5 % of
        // the scene reaches the text, paper text keeps ~8:1 and muted text ~4.5:1 over the lab's white bench and the THINK
        // poster, while the 4 px rim still shows the scene. The bars (controls, chip, hint pill) have no plate: BarAlpha.
        public static readonly Color Glass = new Color32(0x29, 0x2C, 0x2F, 0xFF);
        public static readonly Color Deep = new Color32(0x19, 0x1D, 0x21, 0xFF);
        public const float GlassAlpha = 0.80f, PlateAlpha = 0.75f, BarAlpha = 0.93f, EdgeWidth = 1.5f, GlowSpread = 8f;
        public static Color CardFill => UI.Alpha(Glass, GlassAlpha);
        public static Color CardLine => new Color32(0x74, 0x63, 0x4C, 0xFF);
        public static Color ControlLine => UI.Alpha(Accent, 0.45f);   // interactive borders: 3:1 on the plate
        public static Color Spark => FigmaSkin.Available ? Accent : UI.Theme ? UI.Theme.wattsonAccent : Accent;
        public static Color AccentColor => Accent;   // guidance chrome (was voltage blue: blue now only ever means V)
        public static Color VoltColor => FigmaSkin.Available ? new Color32(0x78, 0xC4, 0xFF, 255) : UI.Theme ? UI.Theme.voltage : new Color(0.25f, 0.66f, 1f);
        public static Color AmpColor => FigmaSkin.Available ? Amber : UI.Theme ? UI.Theme.current : new Color(1f, 0.72f, 0.19f);
        public static Color OhmColor => FigmaSkin.Available ? new Color32(0xFF, 0x85, 0x85, 255) : UI.Theme ? UI.Theme.resistance : new Color(1f, 0.35f, 0.37f);
        static Color Dim => new Color(1, 1, 1, 0.62f);

        // Reference px the dock covers from the right edge (its width plus the margin) in a scene that reserves it.
        public float Cover => dockReserved > 0f || Wide ? shownWidth + Margin : 0f;
        // What the 3D view is framed around. A scene that reserves the strip frames around the reserved width, not the eased
        // one: a table or graph widening the dock overlaps a little more of the shot instead of sliding the whole picture
        // (that broke the match-cuts between the bench chapters).
        float FrameTarget => dockReserved > 0f ? dockReserved + Margin : Cover;
        // Fraction of the actual screen the 3D view is framed around (PanelFraming reads it every frame). After a match-cut
        // into a chapter that reserves another strip (8 -> 9: none -> the dock) it eases over ~0.5 s instead of jumping the
        // picture sideways; behind a white fade, or with reduced motion, it is there at once.
        public float ScreenShare
        {
            get
            {
                var canvas = GetComponentInParent<Canvas>();
                float scale = canvas ? canvas.scaleFactor : 1f;
                return Screen.width > 0 ? Mathf.Clamp01(framed * scale / Screen.width) : 0f;
            }
        }
        // A cinematic chapter (no dock reserved): the controls fold to icons when idle.
        public bool Cinematic => dockReserved <= 0f && !Wide;

        readonly Dictionary<string, RectTransform> blocks = new Dictionary<string, RectTransform>();
        readonly List<GameObject> controlWords = new List<GameObject>();
        readonly HashSet<string> aside = new HashSet<string>();
        RectTransform instruction, info, formula, popup, catcher, pulseRing, hintPill, badge;
        RectTransform hero, heroGlow, topStrip, stage, stageRow, popup3D, voltStrip;
        CanvasGroup dockGroup;
        TextMeshProUGUI heroBig, heroSmall, popupTitle, popupCaption;
        RawImage popupView;
        CurrentEdge turnCurrent;
        float heroT = 9f;
        TextMeshProUGUI instructionText, turnEyebrow, turnHint, infoEyebrow, infoTitle, infoBody, askLabel, pauseLabel, ccLabel, hintText;
        Image pauseIcon, ring, glow, avatarDisc, barFill, badgeFill, badgeIcon, turnEdge;
        HudButton pauseButton, replayButton, ccButton, volumeButton, skipButton, askHud;
        CardHeight turnHeight;
        GameObject[] barOnlyInGame;
        Image[] dots;
        float width = NormalWidth, shownWidth = NormalWidth, dockReserved, framed, pulseT, doneUntil, hintUntil, lastInput, fittedWidth;
        string instructionShown = "", turnKey = "", infoKey = "", attention;
        bool folded, stepDone, holding, turnLaidOut, captionsShown = true;
        int fittedLines = -1;

        // ------------------------------------------------------------------ build

        public static LessonPanel Create(Transform safeArea)
        {
            var root = UI.Stretch("Lesson HUD", safeArea);
            var panel = root.gameObject.AddComponent<LessonPanel>();
            panel.Root = root;
            UiSounds.Attach(root.gameObject);
            panel.Build();
            return panel;
        }

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Build()
        {
            BuildDock();
            BuildCaption();
            BuildAsk();
            BuildConversation();
            foreach (var name in Order) Block(name);
            BuildInstruction();
            BuildInfo();
            formula = Stack("Lines", Block("Formula"), 6);
            formula.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            Block("Spacer").gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            Block("Spacer").gameObject.SetActive(true);
            BuildEnd();
            BuildControls();   // after the dock and the caption: the bar (and its popup) draw over them
            BuildHint();
        }

        // A rounded glass card (kids UI): a panel-navy rim around an inset deep-navy plate (two-tone), a soft top highlight,
        // a 1.5 px electric-blue edge and a two-step blue glow outside it (two outline bands, no blur). `alpha` overrides
        // the rim's opacity (1 = solid).
        public static void Card(RectTransform rect, float alpha = -1f, float corner = Corner)
        {
            if (FigmaSkin.Available)
            {
                var image = rect.gameObject.AddComponent<Image>();
                FigmaSkin.Apply(image, "Panel");
                image.raycastTarget = true;
                var exportedEdge = UI.Stretch("Edge", rect);
                exportedEdge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var focus = exportedEdge.gameObject.AddComponent<Image>();
                FigmaSkin.Apply(focus, "Focus");
                focus.raycastTarget = false;
                focus.enabled = false;
                return;
            }
            UI.Panel(rect, alpha < 0 ? CardFill : UI.Alpha(Glass, alpha), true, corner).raycastTarget = true;
            var plate = UI.Stretch("Plate", rect);
            plate.offsetMin = new Vector2(4f, 4f);
            plate.offsetMax = new Vector2(-4f, -4f);
            plate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UI.Panel(plate, UI.Alpha(Deep, PlateAlpha), true, Mathf.Max(4f, corner - 4f)).raycastTarget = false;
            var shine = UI.Rect("Shine", rect, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0, -1f), new Vector2(-2f * corner, 26f));
            shine.anchorMax = new Vector2(1, 1);
            shine.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var gloss = shine.gameObject.AddComponent<Image>();
            gloss.sprite = Vignettes.Shine;
            gloss.color = Color.white;
            gloss.raycastTarget = false;
            var edge = UI.Stretch("Edge", rect);
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UI.Outline(edge, CardLine, corner, EdgeWidth);
            // The glow: a band out to GlowSpread at 7 % under a nearer band to half that at 10 %, so it falls off in two steps.
            Band("Glow", rect, corner, GlowSpread, UI.Alpha(Accent, 0.07f));
            Band("Halo", rect, corner, GlowSpread / 2f, UI.Alpha(Accent, 0.10f));
        }

        // An outline band from a rect's edge outward by `spread` px (its own layer, outside the layout).
        public static Image Band(string name, RectTransform rect, float corner, float spread, Color color)
        {
            var band = UI.Stretch(name, rect);
            band.offsetMin = new Vector2(-spread, -spread);
            band.offsetMax = new Vector2(spread, spread);
            band.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return UI.Outline(band, color, corner + spread, spread);
        }

        public const float BracketLength = 18f, BracketStroke = 2f, BracketInset = 9f;

        // Small electric-blue corner brackets (the reference's title frames) on a title-bearing card: four L marks, each two
        // strokes BracketLength long, `inset` px inside the corners (negative: outside them, around a bare block). On or off;
        // the layer stays with the card (Clear() keeps it).
        public static void Brackets(RectTransform card, bool on, float inset = BracketInset)
        {
            if (FigmaSkin.Available) return;
            var marks = card.Find("Brackets");
            if (!marks && !on) return;
            if (!marks)
            {
                marks = UI.Stretch("Brackets", card);
                marks.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                for (int i = 0; i < 4; i++)
                {
                    var anchor = new Vector2(i % 2, i / 2);   // bottom-left, bottom-right, top-left, top-right
                    var at = new Vector2(anchor.x == 0 ? inset : -inset, anchor.y == 0 ? inset : -inset);
                    // Pivot on the corner: each stroke grows inward from the corner along its edge.
                    foreach (var (name, size) in new[] { ("Across " + i, new Vector2(BracketLength, BracketStroke)), ("Up " + i, new Vector2(BracketStroke, BracketLength)) })
                    {
                        var stroke = UI.Rect(name, marks, anchor, anchor, at, size).gameObject.AddComponent<Image>();
                        stroke.color = UI.Alpha(Accent, 0.9f);
                        stroke.raycastTarget = false;
                    }
                }
            }
            marks.gameObject.SetActive(on);
        }

        void BuildDock()
        {
            Dock = UI.Rect("UI_Dock", Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -DockTop), new Vector2(NormalWidth, 0));
            Dock.anchorMin = new Vector2(1, 0);
            Dock.offsetMin = new Vector2(-Margin - NormalWidth, DockBottom);
            Dock.offsetMax = new Vector2(-Margin, -DockTop);
            dockGroup = Dock.gameObject.AddComponent<CanvasGroup>();   // fades out while the data stage holds the centre
            Column = Stack("Column", Dock, Gap);
            Column.anchorMin = Vector2.zero;
            Column.anchorMax = Vector2.one;
            Column.offsetMin = Column.offsetMax = Vector2.zero;
            var column = Column.GetComponent<VerticalLayoutGroup>();
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.UpperCenter;
        }

        // A column of blocks (VerticalLayoutGroup); children report their preferred heights, so nested stacks size themselves.
        public static RectTransform Stack(string name, Transform parent, float spacing, RectOffset padding = null)
        {
            var rect = UI.Rect(name, parent, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = Vector2.zero;
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        // A row (HorizontalLayoutGroup) whose children keep their preferred widths, vertically centred.
        public static RectTransform Row(string name, Transform parent, float spacing, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rect = UI.Rect(name, parent, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return rect;
        }

        // A named card of the dock, created on first use at its place in Order (hidden until shown).
        public RectTransform Block(string name)
        {
            if (blocks.TryGetValue(name, out var block) && block) return block;
            block = UI.Rect(name, Column, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            Place(block);
            var layout = block.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            if (!Bare.Contains(name))
            {
                layout.padding = new RectOffset((int)Pad, (int)Pad, 18, 18);
                Card(block);
                block.gameObject.AddComponent<CardHeight>();
            }
            block.gameObject.SetActive(false);
            blocks[name] = block;
            return block;
        }

        // Puts a block in the column at its place in Order (a block comes back from the stage or the top strip this way).
        void Place(RectTransform block)
        {
            if (block.parent != Column) block.SetParent(Column, false);
            int order = System.Array.IndexOf(Order, block.name);
            int index = 0;
            for (int i = 0; i < Column.childCount; i++)
            {
                var sibling = Column.GetChild(i);
                if (sibling == block) continue;
                int siblingOrder = System.Array.IndexOf(Order, sibling.name);
                if (siblingOrder < 0 || siblingOrder < order) index = i + 1;
            }
            block.SetSiblingIndex(index);
        }

        public void Show(string name, bool on)
        {
            var block = Block(name);
            if (block.gameObject.activeSelf == on) return;
            block.gameObject.SetActive(on);
            if (on) { Pop(block); FadeIn(block); }
        }

        // A card fades in over PanelSeconds (0.22 s, ease-out) when it appears (instant under reduced motion); hiding is instant.
        public static void FadeIn(RectTransform rect, float delay = 0f)
        {
            if (!rect) return;
            if (!rect.TryGetComponent(out CardFade fade)) fade = rect.gameObject.AddComponent<CardFade>();
            fade.Restart(delay);
        }

        // A card's edge glows spark (pulsing) while the learner should act on it (Scene 11's graph); "" or null ends it.
        public void Attention(string name, bool on)
        {
            if (!on && attention == name) { SetEdge(name, CardLine); attention = null; }
            else if (on) attention = name;
        }

        void SetEdge(string name, Color color)
        {
            var edge = Block(name).Find("Edge");
            if (edge && edge.TryGetComponent(out Image image))
            {
                image.color = FigmaSkin.Available ? Color.white : color;
                if (FigmaSkin.Available) image.enabled = color != CardLine;
            }
        }

        // ------------------------------------------------------------------ play-test hooks

        public bool PopupOpen => popup && popup.gameObject.activeSelf;
        public void ClosePopup() => ShowPopup(false);

        // Presses one of the top controls exactly as a learner's tap would.
        public void Press(string control)
        {
            var hud = control switch { "pause" => pauseButton, "replay" => replayButton, "cc" => ccButton, "volume" => volumeButton, "skip" => skipButton, _ => null };
            if (hud && hud.TryGetComponent(out Button button) && button.interactable) button.onClick.Invoke();
        }

        // "Volt" is the battery strip (v5): shown while VoltageButtons has built its slider there.
        public bool Showing(string name) => name == "Volt" ? voltStrip && voltStrip.gameObject.activeSelf
            : blocks.TryGetValue(name, out var block) && block && block.gameObject.activeSelf;

        // Empties the block and returns a new child root for a view to build in (its own layout and padding), so the
        // next Clear() takes the whole view away. The card itself is the block's.
        public RectTransform Fresh(string name, RectOffset padding = null, float spacing = 0f, bool panel = true)
        {
            Clear(name);
            var block = Block(name);
            var home = UI.Rect("Root", block, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            var layout = home.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding ?? new RectOffset();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Show(name, true);
            return home;
        }

        // Everything inside a block goes (the block itself and its card stay for the next use, its corner brackets off).
        // "Volt" empties the battery strip.
        public void Clear(string name)
        {
            if (name == "Volt" && voltStrip) { Empty(voltStrip); voltStrip.gameObject.SetActive(false); }
            Empty(Block(name));
            Brackets(Block(name), false);
            Show(name, false);
        }

        static void Empty(RectTransform card)
        {
            for (int i = card.childCount - 1; i >= 0; i--)
            {
                var child = card.GetChild(i).gameObject;
                if (Keep.Contains(child.name)) continue;
                child.SetActive(false);
                Destroy(child);
            }
        }

        // Width a view may use inside a card at the dock's normal or wide width.
        public static float Inner(bool wide) => (wide ? WideWidth : NormalWidth) - 2 * Pad;

        // ------------------------------------------------------------------ type and buttons

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, bool bold = true)
        {
            var label = UI.Label(parent, text, size, color, align);
            if (bold) label.fontStyle = FontStyles.Bold;
            return label;
        }

        // Caps label (eyebrows, keys): heavy, tracked.
        public static TextMeshProUGUI Eyebrow(Transform parent, string text, Color? color = null, float size = HeadingFont)
        {
            var label = Text(parent, (text ?? "").ToUpperInvariant(), size, color ?? Muted, TextAlignmentOptions.MidlineLeft);
            label.characterSpacing = 6f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            Fixed(label.rectTransform, size * 1.25f);
            return label;
        }

        public static Image Icon(Transform parent, string name, float size, Color color)
        {
            var rect = UI.Rect("Icon " + name, parent, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = element.preferredHeight = element.minHeight = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Icons.Get(name);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public enum Kind { Primary, Secondary, Ghost, Raised }

        // A pill button of the kids UI: Primary (amber with ink text and a soft amber glow, one per view; gold on hover),
        // Secondary (navy with the electric-blue edge), Ghost (text), Raised (a card's option row, blue-edged). Icon
        // optional; the word is always there.
        public static Button Pill(string name, Transform parent, string text, UnityAction onClick, Kind kind = Kind.Secondary, string icon = null, float height = ButtonHeight, float font = 20f)
        {
            var rect = UI.Rect(name, parent, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, height));
            Fixed(rect, height);
            float corner = kind == Kind.Raised ? 14f : height / 2f;
            Color fillColor = kind == Kind.Primary ? Amber : kind == Kind.Secondary ? UI.Alpha(Glass, 0.92f) : kind == Kind.Raised ? Raised : Color.clear;
            Color ink = kind == Kind.Primary ? Ink : kind == Kind.Ghost ? Muted : Paper;
            var fill = UI.Panel(rect, fillColor, true, corner);
            fill.raycastTarget = true;
            bool exported = kind != Kind.Ghost && FigmaSkin.Apply(fill, kind + "Normal");
            Image outline = null;
            if (!exported && (kind == Kind.Secondary || kind == Kind.Raised))
            {
                var edge = UI.Stretch("Edge", rect);
                edge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                outline = UI.Outline(edge, kind == Kind.Raised ? UI.Alpha(Accent, 0.35f) : ControlLine, corner, EdgeWidth);
            }
            if (!exported && kind == Kind.Primary)
            {
                // The gloss along the top and the glow outside: the reference's glossy amber action.
                var shine = UI.Rect("Shine", rect, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0, -1f), new Vector2(-corner, height * 0.45f));
                shine.anchorMax = new Vector2(1, 1);
                shine.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var gloss = shine.gameObject.AddComponent<Image>();
                gloss.sprite = Vignettes.Shine;
                gloss.color = Color.white;
                gloss.raycastTarget = false;
                Band("Glow", rect, corner, 6f, UI.Alpha(Amber, 0.18f));
            }
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(onClick);
            button.onClick.AddListener(() => UiSounds.Play("click"));
            var hud = rect.gameObject.AddComponent<HudButton>();
            if (exported) hud.figmaStyle = kind.ToString();
            hud.fill = fill;
            hud.fillColor = fillColor;
            hud.corner = corner;
            hud.activeFill = Accent;   // a Raised option that is chosen turns electric blue (MCQPanel sets the ink text)
            if (kind == Kind.Primary) hud.hoverFill = Gold;
            hud.outline = outline;
            hud.outlineColor = kind == Kind.Raised ? UI.Alpha(Accent, 0.35f) : ControlLine;
            hud.activeOutline = Accent;
            var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            int padX = kind == Kind.Ghost ? 14 : kind == Kind.Raised ? 16 : 28;
            row.padding = new RectOffset(padX, padX, 0, 0);
            row.spacing = 10f;
            row.childAlignment = kind == Kind.Raised ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var iconGraphic = icon != null ? Icon(rect, icon, 22f, ink) : null;
            var label = Text(rect, text, font, ink, TextAlignmentOptions.MidlineLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (exported) hud.figmaForeground = new Graphic[] { label, iconGraphic };
            return button;
        }

        // A full-width option row for the cards (kept for MCQPanel and older callers): a Raised pill.
        public static Button Button(string name, Transform parent, string text, UnityAction onClick, float height = ButtonHeight, float font = 20f)
        {
            var button = Pill(name, parent, text, onClick, Kind.Raised, null, height, font);
            button.GetComponentInChildren<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.Normal;
            return button;
        }

        // ------------------------------------------------------------------ top controls

        void BuildControls()
        {
            Controls = UI.Rect("UI_TopControls", Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -TopInset), new Vector2(0, ControlHeight));
            barFill = UI.Panel(Controls, UI.Alpha(Glass, BarAlpha), true, ControlHeight / 2f);
            bool exportedBar = FigmaSkin.Apply(barFill, "Panel");
            barFill.raycastTarget = true;
            var edge = UI.Stretch("Edge", Controls);
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            if (!exportedBar) UI.Outline(edge, CardLine, ControlHeight / 2f, 1.5f);
            var row = Controls.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(6, 6, 6, 6);
            row.spacing = 4f;   // 4 + the 1 px rule + 4: 9 px between hit areas
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleRight;
            Controls.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            pauseButton = Control(Controls, "Pause", "pause", "PAUSE", () => PauseMenu.Toggle(), out pauseLabel, out pauseIcon);
            var s1 = Rule(Controls);
            replayButton = Control(Controls, "Replay", "replay", "REPLAY", ReplayLine, out _, out _);
            var s2 = Rule(Controls);
            ccButton = Control(Controls, "CC", null, "CAPTIONS", () => SubtitleUI.Enabled = !SubtitleUI.Enabled, out ccLabel, out _);
            Rule(Controls);
            volumeButton = Control(Controls, "Volume", "volume", "VOLUME", TogglePopup, out _, out _);
            var s4 = Rule(Controls);
            skipButton = Control(Controls, "Skip", "skip", "SKIP", SkipSection, out _, out _);
            // On the title screen only CC and VOLUME make sense.
            barOnlyInGame = new[] { pauseButton.gameObject, s1, replayButton.gameObject, s2, s4, skipButton.gameObject };
            replayButton.disabledTap = skipButton.disabledTap = () => ShowHint(Held);
            BuildPopup();
        }

        // Why REPLAY and SKIP do nothing right now: a held gate either waits for the learner's step or for Wattson's line.
        string Held => AskWattsonUI.IsOpen ? "Close Ask Wattson first" : instructionShown.Length > 0 ? "Finish this step first" : "One moment, Wattson is talking";

        static GameObject Rule(Transform row)
        {
            var rect = UI.Rect("Rule", row, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1, 22));
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = 1f;
            element.preferredHeight = element.minHeight = 22f;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.14f);
            image.raycastTarget = false;
            return rect.gameObject;
        }

        // One control: icon (or the CC badge) and a caps word, a bare pill inside the capsule (white wash on hover, "on" fill).
        HudButton Control(Transform row, string name, string icon, string text, UnityAction onClick, out TextMeshProUGUI label, out Image iconImage)
        {
            var rect = UI.Rect("UI_ControlButton " + name, row, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(100, ControlItem));
            Fixed(rect, ControlItem);
            var fill = UI.Panel(rect, Color.clear, true, ControlItem / 2f);
            bool exportedControl = FigmaSkin.Apply(fill, "SecondaryNormal");
            fill.raycastTarget = true;
            fill.raycastPadding = new Vector4(-4f, -6f, -4f, -6f);   // the hit area: the capsule's full 56 px and half of each gap
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { UiSounds.Play("click"); onClick(); });
            var hud = rect.gameObject.AddComponent<HudButton>();
            if (exportedControl) hud.figmaStyle = "Secondary";
            hud.fill = fill;
            hud.fillColor = Color.clear;
            hud.corner = ControlItem / 2f;
            hud.activeFill = new Color(1f, 1f, 1f, 0.14f);
            var line = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            line.padding = new RectOffset(14, 14, 0, 0);
            line.spacing = 9f;
            line.childAlignment = TextAnchor.MiddleLeft;
            line.childControlWidth = line.childControlHeight = true;
            line.childForceExpandWidth = line.childForceExpandHeight = false;
            iconImage = null;
            if (icon != null) iconImage = Icon(rect, icon, 20f, Paper);
            else if (FigmaSkin.Available) iconImage = Icon(rect, "captions", 24f, Paper);
            else
            {
                // The CC badge: a small outlined box with CC inside.
                var badgeRect = UI.Rect("Badge", rect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(28, 19));
                var element = badgeRect.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = element.minWidth = 28f;
                element.preferredHeight = element.minHeight = 19f;
                UI.Outline(badgeRect, Paper, 5f, 1.6f);
                var cc = UI.Label(badgeRect, "CC", 11f, Paper);
                cc.fontStyle = FontStyles.Bold;
            }
            // The word is a direct child of the row, so its preferred width sizes the control; idle folds it away.
            label = UI.Label(rect, text, ControlFont, Paper, TextAlignmentOptions.MidlineLeft);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 4f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            controlWords.Add(label.gameObject);
            if (exportedControl) hud.figmaForeground = new Graphic[] { label, iconImage };
            return hud;
        }

        static void ReplayLine()
        {
            var flow = ModuleFlow.Instance;
            if (flow && !flow.IsPaused(ModuleFlow.PauseReason.Hold) && SubtitleTrack.LastLineStart >= 0) flow.JumpTo(SubtitleTrack.LastLineStart);
        }

        public bool CanJump
        {
            get
            {
                var flow = ModuleFlow.Instance;
                return flow && flow.Director && !flow.IsPaused(ModuleFlow.PauseReason.Hold) && !AskWattsonUI.IsOpen && !PauseMenu.IsOpen && !TitleScreen.IsOpen;
            }
        }

        // R on the keyboard: the same as REPLAY, when REPLAY may act.
        public void KeyReplay() { if (CanJump) { UiSounds.Play("click"); ReplayLine(); } else ShowHint(Held); }

        // SKIP: to the start of the next spoken line of this scene's Timeline, or into the next interaction gate if one
        // comes first (never over it: a gate is a required action), or, past the last line, on to the next scene. The
        // cues in between are retroactive, so the scene's state is exactly what it would be having played through.
        public static void SkipSection()
        {
            var flow = ModuleFlow.Instance;
            if (!flow || !flow.Director || flow.IsPaused(ModuleFlow.PauseReason.Hold)) return;
            double now = flow.Director.time, next = double.MaxValue, gate = double.MaxValue;
            if (flow.Director.playableAsset is TimelineAsset timeline)
            {
                foreach (var track in timeline.GetOutputTracks().OfType<SubtitleTrack>())
                    foreach (var clip in track.GetClips())
                        if (clip.start > now + 0.15 && clip.start < next) next = clip.start;
                if (timeline.markerTrack)
                    foreach (var marker in timeline.markerTrack.GetMarkers())
                        if (marker is InteractionGate && marker.time > now && marker.time < gate) gate = marker.time;
            }
            if (next < double.MaxValue && next < gate) { flow.JumpTo(next - 0.02); return; }
            if (gate < double.MaxValue) { flow.JumpTo(gate - 0.05); return; }
            Skip();
        }

        // Through the scene's director when there is one, so the hand-off state (camera pose, circuit) survives a skip.
        public static void Skip()
        {
            var flow = ModuleFlow.Instance;
            if (!flow) return;
            var director = flow.Director ? flow.Director.GetComponent<SceneDirector>() : null;
            if (director) director.Complete(); else flow.CompleteScene();
        }

        // ------------------------------------------------------------------ hint pill (why a control did nothing)

        void BuildHint()
        {
            hintPill = UI.Rect("UI_Hint", Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -(TopInset + ControlHeight + 10f)), new Vector2(0, 40));
            var hintSurface = UI.Panel(hintPill, UI.Alpha(Glass, BarAlpha), true, 20f);
            FigmaSkin.Apply(hintSurface, "Panel");
            hintSurface.raycastTarget = false;
            var edge = UI.Stretch("Edge", hintPill);
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            if (!FigmaSkin.Available) UI.Outline(edge, CardLine, 20f, EdgeWidth);
            var row = hintPill.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(16, 18, 0, 0);
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            hintPill.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Icon(hintPill, "target", 18f, Spark);
            hintText = Text(hintPill, "", 17f, Paper, TextAlignmentOptions.MidlineLeft);
            hintText.textWrappingMode = TextWrappingModes.NoWrap;
            hintPill.gameObject.SetActive(false);
        }

        public void ShowHint(string text)
        {
            hintText.text = text;
            hintPill.gameObject.SetActive(true);
            hintPill.SetAsLastSibling();
            hintUntil = Time.unscaledTime + 2.5f;
            UiSounds.Play("hint");
            FadeIn(hintPill);
        }

        // ------------------------------------------------------------------ volume popup

        void BuildPopup()
        {
            catcher = UI.Stretch("Popup Catcher", Root);
            var catchImage = catcher.gameObject.AddComponent<Image>();
            catchImage.color = new Color(0f, 0f, 0f, 0.3f); // a faint scrim: the popup reads, a click anywhere on it closes the popup
            var tap = catcher.gameObject.AddComponent<EventTrigger>();
            On(tap, EventTriggerType.PointerClick, () => ShowPopup(false));
            catcher.gameObject.SetActive(false);

            popup = UI.Rect("UI_VolumePopup", Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -(TopInset + ControlHeight + 10f)), new Vector2(NormalWidth, 0));
            var stack = popup.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(22, 22, 18, 18);
            stack.spacing = 10f;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            popup.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Card(popup, -1f, 18f);
            foreach (var (name, kind) in new[] { ("Wattson", AudioBus.Kind.Voice), ("Effects", AudioBus.Kind.Sfx), ("Music", AudioBus.Kind.Music) })
                VolumeRow(popup, name, kind);
            SwitchRow.Create(popup, "Reduce motion", () => ReducedMotion.On, on => ReducedMotion.On = on);   // the pause menu's switch
            popup.gameObject.SetActive(false);
        }

        // A labelled volume slider (the popup and the pause menu): 48 px tall so the thumb is an easy target.
        public static void VolumeRow(Transform parent, string name, AudioBus.Kind kind)
        {
            var row = UI.Rect("Volume " + name, parent, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 48));
            Fixed(row, 48f);
            var label = UI.Label(UI.Rect("Name", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(110, 48)), name, 18f, Paper, TextAlignmentOptions.MidlineLeft);
            label.fontStyle = FontStyles.Bold;
            var rect = UI.Rect("Slider", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(114, 0), new Vector2(0, 48));
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(-114, 48);
            var slider = rect.gameObject.AddComponent<Slider>();
            var track = UI.Stretch("Track", rect);
            track.offsetMin = new Vector2(0, 20);
            track.offsetMax = new Vector2(0, -20);
            UI.Panel(track, new Color(1, 1, 1, 0.16f), true, 4f);
            var fill = UI.Stretch("Fill", track);
            UI.Panel(fill, Accent, true, 4f);
            var handleArea = UI.Stretch("Handle Area", rect);
            handleArea.offsetMin = new Vector2(15, 0);
            handleArea.offsetMax = new Vector2(-15, 0);
            var handle = UI.Rect("Handle", handleArea, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, -18)); // the Slider stretches the handle to the 48 px row; -18 keeps a 30 px disc (the row is the hit area)
            var knob = handle.gameObject.AddComponent<Image>();
            knob.sprite = UI.Circle();
            knob.color = Paper;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.value = AudioBus.Get(kind);
            var sync = rect.gameObject.AddComponent<VolumeSync>();   // the popup and the pause menu each have one: both read the bus when shown
            sync.kind = kind;
            sync.slider = slider;
            slider.onValueChanged.AddListener(value => AudioBus.Set(kind, value));
        }

        void TogglePopup() => ShowPopup(!popup.gameObject.activeSelf);

        void ShowPopup(bool on)
        {
            if (popup.gameObject.activeSelf == on) return;
            popup.gameObject.SetActive(on);
            catcher.gameObject.SetActive(on);
            catcher.SetAsLastSibling();
            popup.SetAsLastSibling();
            UiSounds.Play(on ? "open" : "close");
            if (on) Pop(popup);
        }

        // ------------------------------------------------------------------ caption bar and Ask Wattson

        void BuildCaption()
        {
            // Bottom-left anchor, centre-bottom pivot: SubtitleUI moves it to the centre of the free frame and sizes it to
            // the line (pivot set first: changing it afterwards keeps anchoredPosition and shifts the bar).
            Caption = UI.Rect("UI_SubtitlePanel", Root, Vector2.zero, new Vector2(0.5f, 0), new Vector2(960, BottomInset), new Vector2(400, 100));
            Card(Caption);
        }

        void BuildAsk()
        {
            AskRoot = UI.Rect("UI_AskWattson", Root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-Margin + 6f, 26f), new Vector2(160, 150));
            var buttonRect = UI.Rect("Avatar", AskRoot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(AvatarSize + 16, AvatarSize + 16));
            AskButton = buttonRect.gameObject.AddComponent<Button>();
            AskButton.transition = Selectable.Transition.None;
            var hit = buttonRect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            askHud = buttonRect.gameObject.AddComponent<HudButton>();
            askHud.hoverScale = 1.05f;
            askHud.corner = (AvatarSize + 16) / 2f;   // the focus ring is a circle
            // Soft glow, the pulse ring (listening), the spark ring, then the face in a circular mask.
            glow = UI.Rect("Glow", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (AvatarSize + 34)).gameObject.AddComponent<Image>();
            glow.sprite = UI.Circle();
            glow.color = UI.Alpha(Spark, 0.12f);
            glow.raycastTarget = false;
            pulseRing = UI.Rect("Pulse", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (AvatarSize + 6));
            var pulse = pulseRing.gameObject.AddComponent<Image>();
            pulse.sprite = UI.Circle(0.94f);
            pulse.color = Color.clear;
            pulse.raycastTarget = false;
            ring = UI.Rect("Ring", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (AvatarSize + 6)).gameObject.AddComponent<Image>();
            ring.sprite = UI.Circle(0.93f);
            ring.color = UI.Alpha(Spark, 0.9f);
            ring.raycastTarget = false;
            var disc = UI.Rect("Disc", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * AvatarSize);
            avatarDisc = disc.gameObject.AddComponent<Image>();
            avatarDisc.sprite = UI.Circle();
            avatarDisc.color = Ink;
            avatarDisc.raycastTarget = false;
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (UI.Theme && UI.Theme.wattsonFace)
            {
                var face = UI.Stretch("Face", disc).gameObject.AddComponent<RawImage>();
                face.texture = UI.Theme.wattsonFace;
                face.raycastTarget = false;
            }
            // Thinking: three dots along the bottom of the avatar.
            dots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var dot = UI.Rect("Dot " + i, buttonRect, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 16f, 6f), new Vector2(9, 9));
                dots[i] = dot.gameObject.AddComponent<Image>();
                dots[i].sprite = UI.Circle();
                dots[i].color = Color.clear;
                dots[i].raycastTarget = false;
            }
            // v5: a bolt badge at the ring's top-right, the electric motif.
            var boltBadge = UI.Rect("Bolt", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(AvatarSize * 0.36f, AvatarSize * 0.36f), new Vector2(26, 26));
            var boltDisc = boltBadge.gameObject.AddComponent<Image>();
            boltDisc.sprite = UI.Circle();
            boltDisc.color = Spark;
            boltDisc.raycastTarget = false;
            var boltIcon = UI.Rect("Icon", boltBadge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16)).gameObject.AddComponent<Image>();
            boltIcon.sprite = Icons.Get("bolt");
            boltIcon.color = Ink;
            boltIcon.raycastTarget = false;
            var labelRect = UI.Rect("Label", AskRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(160, 40));
            askLabel = UI.Label(labelRect, "ASK\nWATTSON", 15f, Paper, TextAlignmentOptions.Top);
            askLabel.fontStyle = FontStyles.Bold;
            askLabel.characterSpacing = 6f;
            askLabel.lineSpacing = -10f;
        }

        void BuildConversation()
        {
            Conversation = UI.Rect("UI_Conversation", Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -DockTop), new Vector2(WideWidth, 0));
            Conversation.anchorMin = new Vector2(1, 0);
            Conversation.offsetMin = new Vector2(-Margin - WideWidth, DockBottom);
            Conversation.offsetMax = new Vector2(-Margin, -DockTop);
            Card(Conversation);
            Conversation.gameObject.SetActive(false);
        }

        // Ask Wattson open: the conversation takes the dock's place, the lesson's caption steps away; the avatar's label
        // says how to come back.
        public void ShowConversation(bool open)
        {
            Dock.gameObject.SetActive(!open);
            Conversation.gameObject.SetActive(open);
            askLabel.text = open ? "CLOSE" : "ASK\nWATTSON";
            UiSounds.Play(open ? "open" : "close");
            if (open) Pop(Conversation);
        }

        // ------------------------------------------------------------------ the turn card (what to do now) and clues

        void BuildInstruction()
        {
            instruction = Block("Instruction");
            turnHeight = instruction.GetComponent<CardHeight>();
            turnEdge = instruction.Find("Edge").GetComponent<Image>();
            if (!FigmaSkin.Available) turnCurrent = CurrentEdge.Add(instruction, Corner);
            var row = Row("Turn", instruction, 16f);
            badge = UI.Rect("Badge", row, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60));
            var element = badge.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = element.preferredHeight = element.minHeight = 60f;
            badgeFill = badge.gameObject.AddComponent<Image>();
            badgeFill.sprite = UI.Circle();
            badgeFill.raycastTarget = false;
            badgeIcon = UI.Rect("Icon", badge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30)).gameObject.AddComponent<Image>();
            badgeIcon.raycastTarget = false;
            var column = Stack("Text", row, 4f);
            column.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            turnEyebrow = Eyebrow(column, "YOUR TURN", Spark);
            instructionText = Text(column, "", InstructionFont, Paper);
            turnHint = Text(column, "", HintFont, Muted, TextAlignmentOptions.TopLeft, false);
            TurnStyle(false);
        }

        // Waiting (spark target), done (spark check) or listening between turns (muted speaker).
        void TurnStyle(bool done, bool listening = false)
        {
            stepDone = done;
            badgeFill.color = done ? Spark : listening ? new Color(1f, 1f, 1f, 0.08f) : UI.Alpha(Spark, 0.14f);
            badgeIcon.sprite = Icons.Get(done ? "check" : listening ? "volume" : "bolt");
            badgeIcon.color = done ? Ink : listening ? Muted : Spark;
            turnEdge.color = FigmaSkin.Available ? Color.white : done || listening ? CardLine : UI.Alpha(Accent, 0.9f);
            if (turnCurrent) turnCurrent.enabled = !done && !listening;
            turnEyebrow.color = listening ? Muted : Spark;
        }

        void BuildInfo()
        {
            info = Block("Info");
            info.GetComponent<VerticalLayoutGroup>().spacing = 6f;
            var head = Row("Eyebrow", info, 8f);
            Fixed(head, 20f);
            Icon(head, "lens", 16f, Spark);
            infoEyebrow = Text(head, "CLUE", HeadingFont, Spark, TextAlignmentOptions.MidlineLeft);
            infoEyebrow.characterSpacing = 6f;
            infoTitle = Text(info, "", InfoTitleFont, Paper);
            infoBody = Text(info, "", InfoFont, Body, TextAlignmentOptions.TopLeft, false);
        }

        // The one current turn: what to do (big), the step ("2 OF 3"), a hint (small). Empty text hides the card, unless
        // a Done() confirmation is still showing (it finishes first). The text stays until the next call.
        public void Instruction(string text) => Turn(text);

        public void Turn(string text, string step = null, string hint = null)
        {
            text = text ?? "";
            string key = text + "|" + step + "|" + hint;
            if (key == turnKey) return;
            turnKey = key;
            instructionShown = text;
            if (text.Length == 0)
            {
                if (doneUntil <= 0f) Settle();
                return;
            }
            doneUntil = 0f;
            TurnStyle(false);
            turnEyebrow.text = string.IsNullOrEmpty(step) ? "YOUR TURN" : "YOUR TURN · " + step.ToUpperInvariant();
            instructionText.text = NoWidow(text);
            turnHint.text = NoWidow(hint);
            turnHint.gameObject.SetActive(!string.IsNullOrEmpty(hint));
            bool wasShown = Showing("Instruction");
            Show("Instruction", true);
            if (wasShown) Pop(instruction); // a new step in the same card still pulses once
        }

        // The action landed: the card flips to a check for ~0.9 s with a chime, then leaves (unless a new turn replaces it).
        public void Done(string text)
        {
            instructionShown = "";
            turnKey = "";
            TurnStyle(true);
            turnEyebrow.text = "DONE";
            instructionText.text = NoWidow(text);
            turnHint.text = "";
            turnHint.gameObject.SetActive(false);
            Show("Instruction", true);
            Pop(instruction);
            doneUntil = Time.unscaledTime + 0.9f;
            UiSounds.Play("done");
        }

        // No turn now: the card leaves, unless an activity's controls sit under it (the voltage buttons, the choices). Then
        // it keeps its place, so they never move as they become tappable: a done step stays done until the next turn,
        // otherwise the card says to listen.
        void Settle()
        {
            if (!holding) { Show("Instruction", false); return; }
            if (stepDone && Showing("Instruction")) return;
            TurnStyle(false, true);
            turnEyebrow.text = "NEXT STEP";
            instructionText.text = "Listen to Wattson.";
            turnHint.text = "He'll give you the next step.";
            turnHint.gameObject.SetActive(true);
            Show("Instruction", true);
        }

        // The last two words share a line, so a wrapped card, clue or hint never ends on one word alone.
        static string NoWidow(string text)
        {
            int last = (text ?? "").TrimEnd().LastIndexOf(' ');
            return last > 0 ? text.Substring(0, last) + "\u00A0" + text.Substring(last + 1) : text ?? "";
        }

        public string CurrentInstruction => instructionShown;

        // A supporting note, shown as a clue card: a title and a line or two (what a callout used to say). One at a time;
        // "" hides it. Quantity words take their colour (voltage blue, current amber, resistance red).
        public void Info(string key, string noteTitle, string body)
        {
            infoKey = key ?? "";
            bool any = !string.IsNullOrEmpty(noteTitle) || !string.IsNullOrEmpty(body);
            infoEyebrow.text = infoKey == "reminder" ? "YOUR PREDICTION" : "CLUE";
            infoTitle.text = NoWidow(noteTitle);
            infoBody.text = NoWidow(Quantities(body ?? ""));
            infoTitle.gameObject.SetActive(!string.IsNullOrEmpty(noteTitle));
            infoBody.gameObject.SetActive(!string.IsNullOrEmpty(body));
            Show("Info", any);
        }

        public void InfoClear(string key = null)
        {
            if (key != null && key != infoKey) return; // a later note replaced this one
            Info("", "", "");
        }

        public string CurrentInfo => Showing("Info") ? Regex.Replace(infoTitle.text + " " + infoBody.text, "<.*?>", "").Replace('\u00A0', ' ').Trim() : "";

        static readonly Regex QuantityWords = new Regex(@"\b(potential difference|voltage|current|resistance)\b", RegexOptions.IgnoreCase);

        // Rich-text colour on the quantity words, so the words connect to the meters, the table and the graph.
        public static string Quantities(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Contains("<color")) return text;
            return QuantityWords.Replace(text, m =>
            {
                string word = m.Value.ToLowerInvariant();
                var color = word == "current" ? AmpColor : word == "resistance" ? OhmColor : VoltColor;
                return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{m.Value}</color>";
            });
        }

        // The chapter's title (the banner and the pause menu read it).
        public void SetTitle(string text) => Title = text ?? "";

        // ------------------------------------------------------------------ formula reveals

        // Appends a line to the formula card (V ∝ I → V = kI → ...), each one popping in; the card shows itself. V, I, R
        // and Ω take their colours, and a glossary line ("V — the push: ...") sets its symbol in a column, left-aligned,
        // so the rows line up.
        public TextMeshProUGUI Formula(string text, Color? color = null, float size = FormulaFont)
        {
            Show("Formula", true);
            var row = GlossaryRow.Match(text ?? "");
            var line = UI.Label(formula, Symbols(row.Success ? row.Groups[1].Value + "<pos=1.4em>" + row.Groups[2].Value : text),
                size, color ?? Paper, row.Success ? TextAlignmentOptions.Left : TextAlignmentOptions.Center);
            line.fontStyle = FontStyles.Bold;
            line.gameObject.AddComponent<FrameStamp>().frame = Time.frameCount; // a clear in the same frame keeps it (cue order at one Timeline time is not guaranteed)
            OneLine(line, size);
            line.enableAutoSizing = false;   // FitFormula sizes the card's lines together
            FitFormula();
            Pop(line.rectTransform);
            return line;
        }

        // A hero reveal in the formula card: the big text (paper) over a small line.
        public void Reveal(string big, string small, Color? color = null)
        {
            Show("Formula", true);
            var hero = Formula(big, color ?? Paper, RevealFont);
            hero.characterSpacing = 6f;
            if (!string.IsNullOrEmpty(small))
            {
                var sub = UI.Label(formula, Symbols(small), RevealSubFont, Dim, TextAlignmentOptions.Center);
                sub.gameObject.AddComponent<FrameStamp>().frame = Time.frameCount;
                OneLine(sub, RevealSubFont);
                sub.enableAutoSizing = false;
            }
            fittedLines = -1;   // the hero's tracking changed its width
            FitFormula();
        }

        static readonly Regex GlossaryRow = new Regex(@"^\s*([VIR])\s+—\s+(.+)$");
        static readonly Regex Symbol = new Regex(@"(?<![\p{L}\d])(Δ?[VIR]|Ω)(?!\p{L})|(?<=\d\s)A(?!\p{L})");

        // Rich-text colour on the quantity symbols and their units (V, ΔV blue; I, ΔI, A amber; R, Ω red), as on the
        // case-closed card.
        public static string Symbols(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            return Symbol.Replace(text, m =>
            {
                char c = m.Value[m.Value.Length - 1];
                var color = c == 'V' ? VoltColor : c == 'I' || c == 'A' ? AmpColor : OhmColor;
                return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{m.Value}</color>";
            });
        }

        // The formula card's lines share one size per kind (lines, the hero, its small line): the widest sets it, so a
        // derivation or the glossary never comes out in three sizes. Recomputed when lines come or go or the dock eases.
        void FitFormula()
        {
            float room = formula.rect.width;
            if (room <= 0f || (formula.childCount == fittedLines && Mathf.Approximately(room, fittedWidth))) return;
            fittedLines = formula.childCount;
            fittedWidth = room;
            var fit = new Dictionary<float, float>();
            foreach (Transform child in formula)
                if (child.gameObject.activeSelf && child.TryGetComponent(out TextMeshProUGUI line))
                {
                    float size = line.fontSizeMax, wide = line.GetPreferredValues(line.text, 99999f, 0f).x * size / Mathf.Max(1f, line.fontSize);
                    float fits = wide > room ? Mathf.Max(line.fontSizeMin, size * room / wide) : size;
                    fit[size] = fit.TryGetValue(size, out float other) ? Mathf.Min(other, fits) : fits;
                }
            foreach (Transform child in formula)
                if (child.gameObject.activeSelf && child.TryGetComponent(out TextMeshProUGUI line)) line.fontSize = fit[line.fontSizeMax];
        }

        // One line at a fixed height, shrinking its font to fit the width: a crowded column shrinks the data views
        // instead of squeezing text over text.
        public static void OneLine(TextMeshProUGUI label, float size)
        {
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(15f, size * 0.55f);
            Fixed(label.rectTransform, size * 1.3f);
        }

        // Empties the formula card, except lines revealed this very frame (a "formula.clear" cue placed at the same
        // Timeline time as the reveal it makes room for may fire after it).
        public void FormulaClear()
        {
            bool kept = false;
            for (int i = formula.childCount - 1; i >= 0; i--)
            {
                var child = formula.GetChild(i).gameObject;
                if (child.TryGetComponent(out FrameStamp stamp) && stamp.frame == Time.frameCount) { kept = true; continue; }
                child.SetActive(false);
                Destroy(child);
            }
            Show("Formula", kept);
        }

        // ------------------------------------------------------------------ the finale: case closed

        void BuildEnd()
        {
            var end = Block("End");
            Brackets(end, true);   // a title-bearing card
            var layout = end.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset((int)Pad, (int)Pad, 22, 22);

            // The stamp: a gold-edged box, tilted, hugging its word (Fredoka, the display face).
            var stampRow = Row("Stamp Row", end, 0f);
            Fixed(stampRow, 72f);
            var stamp = UI.Rect("Stamp", stampRow, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(372, 64));
            var stampSize = stamp.gameObject.AddComponent<LayoutElement>();
            stampSize.preferredWidth = stampSize.minWidth = 372f;
            stampSize.preferredHeight = stampSize.minHeight = 64f;
            UI.Outline(stamp, Gold, 12f, 4f);
            var stampWord = UI.Display(UI.Label(stamp, "CASE CLOSED", 40f, Gold));
            stampWord.fontStyle = FontStyles.Bold;
            stampWord.characterSpacing = 14f;
            stampWord.textWrappingMode = TextWrappingModes.NoWrap;
            stamp.localRotation = Quaternion.Euler(0, 0, 4f);

            Eyebrow(end, "What you found");
            var tiles = Row("V I R", end, 10f, TextAnchor.UpperLeft);
            var tileRow = tiles.GetComponent<HorizontalLayoutGroup>();
            tileRow.childForceExpandWidth = tileRow.childForceExpandHeight = true;   // equal tiles: the tallest sets the row
            foreach (var (symbol, color, name, unit) in new[] { ("V", VoltColor, "Potential difference", "the push · volts (V)"), ("I", AmpColor, "Current", "the flow · amperes (A)"), ("R", OhmColor, "Resistance", "the opposition · ohms (Ω)") })
            {
                var tile = Stack(symbol, tiles, 6f, new RectOffset(14, 14, 14, 14));
                tile.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                UI.Panel(tile, Raised, true, 14f).raycastTarget = false;
                var big = Text(tile, symbol, 44f, color);
                Fixed(big.rectTransform, 48f);
                Text(tile, name, 17f, Paper);
                Text(tile, unit, 15f, Muted, TextAlignmentOptions.TopLeft, false);
            }
            var law = UI.Display(Text(end, $"<color=#{ColorUtility.ToHtmlStringRGB(VoltColor)}>V</color> = <color=#{ColorUtility.ToHtmlStringRGB(AmpColor)}>I</color> × <color=#{ColorUtility.ToHtmlStringRGB(OhmColor)}>R</color>", 80f, Paper, TextAlignmentOptions.Center));
            OneLine(law, 80f);
            var data = ExperimentData.Main;
            string calc = data && data.Count > 0
                ? $"Your nichrome wire: {data.voltages[data.Count - 1]:0.0} V ÷ {data.currents[data.Count - 1]:0.00} A = {data.resistance:0.0} Ω"
                : "R = V ÷ I";
            var calcLabel = Text(end, calc, 19f, Muted, TextAlignmentOptions.Center, false);
            Fixed(calcLabel.rectTransform, 26f);
            // The one primary action after the case closes: the hands-on lab (shown only while one is offered); the replay
            // steps down to secondary.
            explore = Pill("Explore", end, "Explore Now", () => ExploreAction?.Invoke(), Kind.Primary, "bolt");
            explore.gameObject.SetActive(false);
            Pill("Replay", end, "Replay the investigation", () =>
            {
                var flow = ModuleFlow.Instance;
                if (flow && flow.scenes != null && flow.scenes.Length > 0) flow.JumpToScene(flow.scenes[0]);
            }, Kind.Secondary, "replay");
            var more = Row("More", end, 10f);
            Fixed(more, ButtonHeight);
            Pill("Chapters", more, "Chapters", () => PauseMenu.Open(), Kind.Secondary, "list");
            Pill("Exit", more, "Exit", ModuleFlow.Exit, Kind.Ghost, "exit");
        }

        // What the case-closed card's "Explore Now" opens (ExploreLab sets it where a bench can host the lab); null hides it.
        public static System.Action ExploreAction;
        Button explore;

        // The finale: the case-closed card takes the dock (the law it states replaces the formula and readings cards).
        public void End(bool on)
        {
            if (on)
            {
                if (explore) explore.gameObject.SetActive(ExploreAction != null);
                FormulaClear();
                Show("Readings", false);
                Show("Info", false);
                Instruction("");
                UiSounds.Play("stamp");   // the normal dock width: widening it would push the finale's framing left
            }
            Show("End", on);
        }

        // ------------------------------------------------------------------ data views

        ObservationTableUI table;
        GraphUI graph;
        // A crowded column shrinks the views, down to MinViewShare of their size; below YieldShare their text would fall
        // under the 15 px floor, so the cards in Yielders step aside first (in that order), as they do when the cards
        // alone would run past the dock: the live readings (the meters say the same), then the formula card, then the
        // graph (the table holds the same numbers: a quick check under the data needs the room). They come back when
        // there is room.
        public const float TableScale = 0.49f, GraphScale = 0.48f, MinViewShare = 0.6f, YieldShare = 0.7f;
        static readonly string[] Yielders = { "Readings", "Formula", "Graph" };
        public bool HasViews => table || graph;
        public bool SteppedAside(string name) => aside.Contains(name);

        // The observation table and the V-I graph, built inside the dock on first use (the dock widens for them) and
        // kept until the next scene entry resets the HUD; their state comes from the scene's cues.
        public ObservationTableUI Table
        {
            get
            {
                if (table) return table;
                table = View("Table", ObservationTableUI.Canvas, TableScale).gameObject.AddComponent<ObservationTableUI>();
                table.units = true;
                return table;
            }
        }

        public GraphUI Graph
        {
            get
            {
                if (graph) return graph;
                graph = View("Graph", GraphUI.Canvas, GraphScale).gameObject.AddComponent<GraphUI>();
                return graph;
            }
        }

        // A fixed-size canvas scaled into a block: the holder takes the scaled height in the column, the canvas keeps its
        // own pixel layout (the table and graph classes lay themselves out in canvas px).
        public RectTransform View(string block, Vector2 canvas, float scale)
        {
            Expand(true);
            Block(block).GetComponent<CardHeight>().enabled = false;   // the one kind of card that may shrink
            var holder = UI.Rect("View", Block(block), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, canvas.y * scale));
            Fixed(holder, canvas.y * scale).minHeight = canvas.y * scale * MinViewShare; // a crowded column shrinks the views first, so no block ever overlaps another
            var rect = UI.Rect("Canvas", holder, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, canvas);
            rect.localScale = Vector3.one * scale;
            var fit = holder.gameObject.AddComponent<CanvasFit>();
            fit.canvas = rect; fit.size = canvas; fit.scale = scale;
            Show(block, true);
            return rect;
        }

        // ------------------------------------------------------------------ v5 surfaces: hero, top readings, stage, 3D pop-up, battery strip

        public bool ReadingsOnTop { get; private set; }
        public bool Staged { get; private set; }
        public bool Popup3DOpen => popup3D && popup3D.gameObject.activeSelf;
        // The centred surfaces on screen (WattsonGuide stands beside them); null until first shown.
        public RectTransform StageRect => stage;
        public RectTransform HeroRect => hero;
        // While staged at least this much of the screen stays free on each side of the stage (Wattson stands there).
        public const float StageSide = 300f;
        TextMeshProUGUI stageCaption;
        // The 3D pop-up forwards pointer drags over its view here (area C turns the sample with them).
        public System.Action<Vector2> Popup3DDrag;

        // A block that is in the dock column where Crowd and the readings rule expect it (not moved to the stage or the top).
        public bool InDock(string name) => Showing(name) && !(Staged && StageBlocks.Contains(name)) && !(ReadingsOnTop && name == "Readings");
        // The top readings strip is on screen with its values (the chapter chip folds to its number beside it).
        public bool TopStripShown => topStrip && topStrip.gameObject.activeSelf && topStrip.rect.width > 1f;

        // A surface centred in the part of the frame the dock leaves free (Follow places it every frame): a column that
        // sizes itself to its content, over the dock and under the captions, the controls and the menus.
        RectTransform Surface(string name, float width, float pivotY, bool fitWidth = false, bool card = true, float glass = -1f)
        {
            var rect = UI.Rect(name, Root, Vector2.zero, new Vector2(0.5f, pivotY), Vector2.zero, new Vector2(width, 0));
            rect.SetSiblingIndex(Dock.GetSiblingIndex() + 1);
            var stack = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = card ? new RectOffset((int)Pad, (int)Pad, 18, 18) : new RectOffset();
            stack.spacing = Gap;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            var fit = rect.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (fitWidth) fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (card) Card(rect, glass);
            rect.gameObject.SetActive(false);
            return rect;
        }

        // "hero <big>|<small>": a centre-screen reveal plate: `big` at 120 px (V, I, R, Ω in their colours), `small` under it,
        // glass with a cyan frame and a bolt, popping in with a spark pulse; stays until HeroClear or the next scene. Never
        // takes input. Nearly solid glass: the bench's bright meter readouts behind the big glyphs never mix with them.
        public void Hero(string big, string small)
        {
            if (!hero)
            {
                hero = Surface("UI_Hero", 0f, 0.5f, fitWidth: true, glass: 0.95f);
                hero.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(60, 60, 30, 34);
                if (!FigmaSkin.Available)
                {
                    // The legacy arrival ring; imported artwork already includes its border.
                    heroGlow = UI.Rect("Glow", hero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    heroGlow.SetAsFirstSibling();
                    heroGlow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    UI.Outline(heroGlow, Color.clear, Corner + 8f, 3f);
                    var frame = UI.Stretch("Frame", hero);
                    frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    UI.Outline(frame, UI.Alpha(Accent, 0.8f), Corner, 2f);
                }
                var boltBadge = UI.Rect("Bolt", hero, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(22, -22), new Vector2(40, 40));
                boltBadge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var disc = boltBadge.gameObject.AddComponent<Image>();
                disc.sprite = UI.Circle();
                disc.color = Spark;
                var bolt = UI.Rect("Icon", boltBadge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24)).gameObject.AddComponent<Image>();
                bolt.sprite = Icons.Get("bolt");
                bolt.color = Ink;
                heroBig = UI.Display(Text(hero, "", HeroFont, Paper, TextAlignmentOptions.Center, false));   // Fredoka: display type
                heroBig.characterSpacing = 4f;
                heroBig.textWrappingMode = TextWrappingModes.NoWrap;
                heroSmall = Text(hero, "", HeroSubFont, Body, TextAlignmentOptions.Center, false);
                heroSmall.textWrappingMode = TextWrappingModes.NoWrap;
                foreach (var graphic in hero.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            }
            heroBig.text = Symbols(big ?? "");
            heroSmall.text = Symbols(small ?? "");
            heroSmall.gameObject.SetActive(!string.IsNullOrEmpty(small));
            hero.gameObject.SetActive(true);
            heroT = 0f;
            Pop(hero);
            FadeIn(hero);
        }

        public void HeroClear() { if (hero) hero.gameObject.SetActive(false); }

        // "readings.top 1|0": the Readings block moves to a strip at the top-centre of the free frame (LiveReadingsPanel
        // builds larger there) and back to the dock; while it is on top the dock's column leaves it alone.
        public void TopReadings(bool on)
        {
            if (ReadingsOnTop == on) return;
            ReadingsOnTop = on;
            var block = Block("Readings");
            if (on)
            {
                if (!topStrip) topStrip = Surface("UI_TopReadings", 0f, 1f, fitWidth: true, card: false);
                topStrip.gameObject.SetActive(true);
                block.SetParent(topStrip, false);
            }
            else
            {
                if (topStrip) topStrip.gameObject.SetActive(false);
                Place(block);
            }
        }

        // "stage 1|0": the Table, Graph and Formula blocks move to one wide glass plate in the centre of the screen (Table
        // left, Graph right, Formula under, the plate at StageScale, never closer than StageSide to either edge so Wattson
        // can stand beside it) and back to the dock. The table comes first, the graph a beat later. The dock (the turn card,
        // the clue) fades out under the stage while it shows; its reservation is untouched, so the camera never jumps.
        public void Stage(bool on)
        {
            if (Staged == on) return;
            Staged = on;
            if (on)
            {
                if (!stage)
                {
                    stage = Surface("UI_Stage", 0f, 0.5f, fitWidth: true);
                    stage.localScale = Vector3.one * StageScale;
                    stageRow = Row("Views", stage, Gap, TextAnchor.UpperCenter);
                }
                stage.gameObject.SetActive(true);
                Block("Table").SetParent(stageRow, false);
                Block("Graph").SetParent(stageRow, false);
                Block("Formula").SetParent(stage, false);
                Width(Block("Table"), ObservationTableUI.Canvas.x * TableScale + 2f * Pad);
                Width(Block("Graph"), GraphUI.Canvas.x * GraphScale + 2f * Pad);
                StageCaption(true);
                FadeIn(stage);   // no Pop: the plate keeps its StageScale
                FadeIn(Block("Table"));
                FadeIn(Block("Graph"), StageStagger);
            }
            else
            {
                StageCaption(false);
                if (stage) stage.gameObject.SetActive(false);
                Width(Block("Table"), -1f);
                Width(Block("Graph"), -1f);
                Place(Block("Table"));
                Place(Block("Graph"));
                Place(Block("Formula"));
            }
            fittedLines = -1;   // the formula lines refit to their new width
        }

        // The graph follows the table onto the stage this much later, so the eye takes one in, then the other.
        const float StageStagger = 0.35f;

        // Under the staged graph: what its slope is with V up the side and I along the bottom, and what it would be the other
        // way round, so the slope is never read as R off an I-against-V plot.
        void StageCaption(bool on)
        {
            if (!graph) return;
            if (on && !stageCaption)
            {
                stageCaption = Text(Block("Graph"), Symbols("slope = ΔV ÷ ΔI = R\n(an I-against-V plot would give 1/R)"), 18f, Muted, TextAlignmentOptions.Center, false);
                Fixed(stageCaption.rectTransform, 50f);
            }
            if (stageCaption) stageCaption.gameObject.SetActive(on);
        }

        static void Width(RectTransform block, float width)
        {
            if (!block.TryGetComponent(out LayoutElement element)) element = block.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = width;
        }

        // A centre card (~980 x 560) showing a RenderTexture with a title row and a caption; a drag over the view goes to
        // Popup3DDrag. The view keeps the texture's aspect (it crops the top and bottom of a wider render).
        public void Popup3D(RenderTexture rt, string title, string caption)
        {
            if (!popup3D)
            {
                popup3D = Surface("UI_Popup3D", Popup3DWidth, 0.5f);
                var head = Row("Head", popup3D, 12f);
                Fixed(head, 48f);
                Icon(head, "bolt", 22f, Spark);
                popupTitle = Text(head, "", InfoTitleFont, Paper, TextAlignmentOptions.MidlineLeft);
                popupTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                Pill("Close", head, "Close", Popup3DClose, Kind.Secondary, "x", 48f, 17f);
                var view = UI.Rect("View", popup3D, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, Popup3DView));
                Fixed(view, Popup3DView);
                popupView = view.gameObject.AddComponent<RawImage>();
                popupView.raycastTarget = true;
                var drag = view.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
                entry.callback.AddListener(data => Popup3DDrag?.Invoke(((PointerEventData)data).delta));
                drag.triggers.Add(entry);
                popupCaption = Text(popup3D, "", 19f, Muted, TextAlignmentOptions.Center, false);
            }
            popupView.texture = rt;
            float viewAspect = (Popup3DWidth - 2f * Pad) / Popup3DView, texAspect = rt ? (float)rt.width / rt.height : viewAspect;
            float kx = Mathf.Min(1f, viewAspect / texAspect), ky = Mathf.Min(1f, texAspect / viewAspect);
            popupView.uvRect = new Rect((1f - kx) / 2f, (1f - ky) / 2f, kx, ky);
            popupTitle.text = title ?? "";
            popupCaption.text = caption ?? "";
            popupCaption.gameObject.SetActive(!string.IsNullOrEmpty(caption));
            popup3D.gameObject.SetActive(true);
            popup3D.SetAsLastSibling();
            UiSounds.Play("open");
            Pop(popup3D);
            FadeIn(popup3D);
        }

        public void Popup3DClose()
        {
            if (!Popup3DOpen) return;
            popup3D.gameObject.SetActive(false);
            popupView.texture = null;
            UiSounds.Play("close");
        }

        // The battery strip: VoltageButtons builds its slider in the root this returns (bottom-centre of the free frame,
        // above the captions). Clear("Volt") empties it and hides the strip; Showing("Volt") reads it.
        public RectTransform VoltStrip()
        {
            if (!voltStrip) voltStrip = Surface("UI_VoltageStrip", VoltStripWidth, 0f);
            Empty(voltStrip);
            var home = UI.Rect("Root", voltStrip, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            var layout = home.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            voltStrip.gameObject.SetActive(true);
            Pop(voltStrip);
            FadeIn(voltStrip);
            return home;
        }

        // The centred surfaces follow the free frame (the dock's width eases), and the hero's ring pulses out on arrival.
        // The readings strip rides the top row, centred but never left of the chapter chip (which folds to its number while
        // the strip shows); the hero, the stage and the pop-up share the band under that row (and under the strip) and
        // over the dock's bottom edge.
        void Follow()
        {
            float free = Root.rect.width - Cover, height = Root.rect.height, stripTop = height - TopInset;
            if (topStrip && topStrip.gameObject.activeSelf)
            {
                // Between the chip and the controls; a row lower, centred, where they leave too little room (4:3 screens).
                float w = topStrip.rect.width, x = Mathf.Max(free / 2f, ChapterBanner.ChipRight + Gap + w / 2f);
                if (x + w / 2f > Root.rect.width - Margin - Controls.rect.width - Gap) { x = free / 2f; stripTop = height - DockTop; }
                topStrip.anchoredPosition = new Vector2(x, stripTop);
            }
            float top = TopStripShown ? stripTop - topStrip.rect.height - Gap : height - DockTop, midY = (DockBottom + top) / 2f;
            if (hero && hero.gameObject.activeSelf)
            {
                hero.anchoredPosition = new Vector2(free / 2f, midY + 40f);
                heroT += Time.unscaledDeltaTime;
                float k = ReducedMotion.On ? 0f : 1f - Mathf.Clamp01(heroT / 0.9f);
                if (heroGlow)
                {
                    heroGlow.GetComponent<Image>().color = UI.Alpha(Glow, 0.8f * k);
                    heroGlow.sizeDelta = hero.rect.size + Vector2.one * (16f + 110f * (1f - k * k));
                }
            }
            if (stage && stage.gameObject.activeSelf)
            {
                // Centred on the whole screen (the dock fades out under it), leaving StageSide free on each side for Wattson.
                float screen = Root.rect.width;
                float fit = StageFit(top, DockBottom, stage.rect.height, out float y);
                stage.localScale = Vector3.one * Mathf.Min(fit, (screen - 2f * StageSide) / Mathf.Max(1f, stage.rect.width));
                stage.anchoredPosition = new Vector2(screen / 2f, y);
            }
            if (LawReveal.Instance) LawReveal.Instance.Place(Root.rect.width / 2f, top, DockBottom, Root.rect.width - 2f * StageSide); // centred on the whole screen, room for Wattson either side
            if (popup3D && popup3D.gameObject.activeSelf) popup3D.anchoredPosition = new Vector2(free / 2f, midY);
            if (voltStrip && voltStrip.gameObject.activeSelf) voltStrip.anchoredPosition = new Vector2(free / 2f, VoltStripBottom);
        }

        // The data stage in a band (reference px, y up; `height` its unscaled layout height): StageScale, or smaller when
        // the band is shorter, and low in the band (80 px under its middle), so Wattson's top-left spot stays clear.
        public static float StageFit(float top, float bottom, float height, out float y)
        {
            float scale = StageScale * Mathf.Min(1f, (top - bottom) / Mathf.Max(1f, height * StageScale)), half = height * scale / 2f;
            y = Mathf.Clamp((top + bottom) / 2f - 80f, bottom + half, Mathf.Max(bottom + half, top - half));
            return scale;
        }

        // A new scene: every card empties (the scene's cues rebuild what it needs), the dock narrows, and it reserves a
        // strip of the frame only when the scene asks for one (activity screens; 0 keeps the cinematic full-frame).
        public void ResetForScene(string sceneTitle, float dockWidth = 0f)
        {
            SetTitle(sceneTitle);
            HeroClear();
            LawReveal.Clear();
            Popup3DClose();
            Popup3DDrag = null;
            TopReadings(false);
            Stage(false);
            foreach (var name in Order) if (name != "Spacer" && name != "Instruction" && name != "Info" && name != "Formula" && name != "End") Clear(name);
            table = null;
            graph = null;
            doneUntil = 0f;
            holding = false;
            turnKey = null;   // the card goes, whatever it showed
            Instruction("");
            InfoClear();
            FormulaClear();
            End(false);
            Expand(false);
            attention = null;
            dockReserved = dockWidth;
            if (!ModuleFlow.Instance || ModuleFlow.Instance.Covered) framed = FrameTarget;   // behind the white: no slide
            ShowPopup(false);
            lastInput = Time.unscaledTime;
        }

        // Wide during the table, graph and comparison explanations: the dock grows toward the centre, still docked right.
        public void Expand(bool wide)
        {
            Wide = wide;
            width = wide ? WideWidth : NormalWidth;
        }

        // Pins a column child to one height: preferred and minimum set, no flexible share of the spare space.
        public static LayoutElement Fixed(RectTransform rect, float height)
        {
            var element = rect.GetComponent<LayoutElement>();
            if (!element) element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 0f;
            return element;
        }

        public static void On(EventTrigger trigger, EventTriggerType type, UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        // A short scale-in on a card that just changed (none under reduced motion).
        public static void Pop(RectTransform rect)
        {
            if (!rect || ReducedMotion.On) return;
            var pop = rect.GetComponent<UIPop>();
            if (!pop) pop = rect.gameObject.AddComponent<UIPop>();
            pop.Restart();
        }

        // ------------------------------------------------------------------ every frame

        void Update()
        {
            var flow = ModuleFlow.Instance;
            bool paused = flow && flow.IsPaused(ModuleFlow.PauseReason.User);
            bool menu = PauseMenu.IsOpen, title = TitleScreen.IsOpen;
            pauseLabel.text = paused || menu ? "RESUME" : "PAUSE";
            pauseIcon.sprite = Icons.Get(paused || menu ? "play" : "pause");
            pauseButton.active = paused || menu;
            if (captionsShown != SubtitleUI.Enabled)
            {
                // Off: the same word struck through and no "on" fill, so the bar never changes width under the pointer.
                captionsShown = SubtitleUI.Enabled;
                ccLabel.fontStyle = captionsShown ? FontStyles.Bold : FontStyles.Bold | FontStyles.Strikethrough;
            }
            ccButton.active = captionsShown;
            volumeButton.active = popup.gameObject.activeSelf;
            bool canJump = CanJump;
            var replay = replayButton.GetComponent<Button>();
            if (replay.interactable != canJump) replay.interactable = canJump;
            var skip = skipButton.GetComponent<Button>();
            if (skip.interactable != canJump) skip.interactable = canJump;
            bool compact = title || (flow && flow.Ended);   // title and finale: only CC and VOLUME mean something
            foreach (var go in barOnlyInGame) if (go.activeSelf == compact) go.SetActive(!compact);

            // Idle in a cinematic chapter (no pointer or key for 3 s): the bar folds to its icons at full contrast; any
            // input brings the words back. Never while something is open or paused.
            if (HudKeys.AnyInput) lastInput = Time.unscaledTime;
            bool idle = Cinematic && !paused && !menu && !title && !PopupOpen && !AskWattsonUI.IsOpen && Time.unscaledTime - lastInput > 3f;
            if (idle != folded)
            {
                folded = idle;
                foreach (var word in controlWords) word.SetActive(!idle);
                barFill.color = FigmaSkin.Available ? Color.white : UI.Alpha(Glass, idle ? 0.78f : BarAlpha);
            }

            // The turn card's confirmation leaves after its moment (see Settle: not while an activity's controls sit under
            // it, and then it never gets shorter); the hint pill after 2.5 s.
            bool hold = Showing("Volt") || Showing("MCQ");
            bool expired = doneUntil > 0f && Time.unscaledTime > doneUntil;
            if (expired) doneUntil = 0f;
            bool changed = hold != holding;
            holding = hold;
            if ((expired || changed) && instructionShown.Length == 0 && doneUntil <= 0f) Settle();
            bool turnShown = Showing("Instruction");
            float floor = !holding || !turnShown ? 0f : turnLaidOut ? Mathf.Max(turnHeight.floor, instruction.rect.height) : turnHeight.floor;
            turnLaidOut = turnShown;   // a card shown this frame still has the height of its last layout
            if (!Mathf.Approximately(floor, turnHeight.floor)) { turnHeight.floor = floor; LayoutRebuilder.MarkLayoutForRebuild(instruction); }
            if (hintUntil > 0f && Time.unscaledTime > hintUntil) { hintUntil = 0f; hintPill.gameObject.SetActive(false); }
            if (attention != null) SetEdge(attention, UI.Alpha(Glow, ReducedMotion.On ? 0.9f : 0.5f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f)));

            // The dock's width eases between normal and wide.
            float target = width;
            shownWidth = ReducedMotion.On ? target : Mathf.Lerp(shownWidth, target, 1 - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            if (Mathf.Abs(shownWidth - target) < 0.5f) shownWidth = target;
            float frame = FrameTarget;
            framed = ReducedMotion.On ? frame : Mathf.Lerp(framed, frame, 1 - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            if (Mathf.Abs(framed - frame) < 0.5f) framed = frame;
            // The dock steps out of sight (and out of reach) under the data stage and the finale's law, and back after (the
            // law surface carries its own hint while the learner builds it).
            bool centre = Staged || LawReveal.Instance;
            float dockAlpha = centre ? 0f : 1f;
            dockGroup.alpha = ReducedMotion.On ? dockAlpha : Mathf.MoveTowards(dockGroup.alpha, dockAlpha, Time.unscaledDeltaTime / 0.25f);
            dockGroup.blocksRaycasts = dockGroup.interactable = !centre;
            if (!Mathf.Approximately(Dock.offsetMin.x, -Margin - shownWidth))
            {
                Dock.offsetMin = new Vector2(-Margin - shownWidth, DockBottom);
                Conversation.offsetMin = new Vector2(-Margin - Mathf.Max(shownWidth, WideWidth), DockBottom);
            }

            AnimateAsk(flow);
        }

        // After the cards' own Updates (their entrance fades), so a card that stepped aside stays out of sight.
        void LateUpdate()
        {
            FitFormula();
            Crowd();
            Follow();
        }

        // Which Yielders step aside this frame, from the cards' preferred heights (not from the last layout, so the
        // choice never flickers): the views' share if every shown card kept its place, then one yielder at a time.
        void Crowd()
        {
            aside.Clear();
            float views = (table && InDock("Table") ? ObservationTableUI.Canvas.y * TableScale : 0f) + (graph && InDock("Graph") ? GraphUI.Canvas.y * GraphScale : 0f);
            float need = -Gap;
            for (int i = 0; i < Column.childCount; i++)
            {
                var child = (RectTransform)Column.GetChild(i);
                if (child.gameObject.activeSelf && !OwnWay(child)) need += LayoutUtility.GetPreferredHeight(child) + Gap;
            }
            foreach (var name in Yielders)
            {
                if (need - views * (1f - YieldShare) <= Column.rect.height) break;
                if (!InDock(name) || OwnWay(Block(name))) continue;
                need -= LayoutUtility.GetPreferredHeight(Block(name)) + Gap;
                if (name == "Graph" && graph) views -= GraphUI.Canvas.y * GraphScale;
                aside.Add(name);
            }
            // The readings card steps aside by itself (LiveReadingsPanel); the formula and graph cards here.
            StepAside(Block("Formula"), aside.Contains("Formula"));
            StepAside(Block("Graph"), aside.Contains("Graph"));
        }

        // Out of the column's layout and out of sight (it keeps its state), or back.
        static void StepAside(RectTransform card, bool away)
        {
            if (!card.TryGetComponent(out LayoutElement element)) element = card.gameObject.AddComponent<LayoutElement>();
            if (!card.TryGetComponent(out CanvasGroup group)) group = card.gameObject.AddComponent<CanvasGroup>();
            if (element.ignoreLayout != away) { element.ignoreLayout = away; group.alpha = 1f; group.blocksRaycasts = !away; }
            if (away && group.alpha > 0f) group.alpha = 0f;
        }

        // The readings card is out of the column for its own reasons while the data table is up or the meters' plates
        // are hidden (LiveReadingsPanel).
        bool OwnWay(RectTransform block) => block.name == "Readings" && (InDock("Table") || !(CircuitRig.Active && CircuitRig.Active.readoutsShown));

        // The avatar's states: idle (spark ring, a slow breath while he can be asked), listening (an expanding ring),
        // thinking (three dots), speaking (the ring pulsing), offline or error (a muted ring). Minimal under reduced motion.
        void AnimateAsk(ModuleFlow flow)
        {
            var agent = flow ? flow.Agent : null;
            var state = agent ? agent.Current : WattsonAgent.State.Idle;
            float t = Time.unscaledTime;
            bool calm = ReducedMotion.On;
            var accent = Spark;
            bool open = AskWattsonUI.IsOpen;
            float breath = !open && state == WattsonAgent.State.Idle && !calm ? 0.5f + 0.5f * Mathf.Sin(t * 2.2f) : 0f;
            glow.color = UI.Alpha(accent, 0.10f + 0.08f * breath);
            glow.transform.localScale = Vector3.one * (1f + 0.04f * breath);

            var pulse = pulseRing.GetComponent<Image>();
            if (state == WattsonAgent.State.Listening && !calm)
            {
                pulseT = (pulseT + Time.unscaledDeltaTime / 1.1f) % 1f;
                pulseRing.localScale = Vector3.one * (1f + 0.55f * pulseT);
                pulse.color = UI.Alpha(accent, 0.7f * (1f - pulseT));
            }
            else { pulseRing.localScale = Vector3.one; pulse.color = state == WattsonAgent.State.Listening ? UI.Alpha(accent, 0.5f) : Color.clear; }

            float ringAlpha = state == WattsonAgent.State.Talking ? (calm ? 1f : 0.65f + 0.35f * Mathf.Sin(t * 7f))
                : state == WattsonAgent.State.Listening ? 1f
                : state == WattsonAgent.State.Error ? 0.9f : 0.9f;
            ring.color = UI.Alpha(state == WattsonAgent.State.Error ? Muted : accent, ringAlpha);

            bool thinking = state == WattsonAgent.State.Thinking;
            for (int i = 0; i < dots.Length; i++)
            {
                float a = thinking ? (calm ? 1f : 0.35f + 0.65f * Mathf.Max(0f, Mathf.Sin(t * 5f - i * 1.1f))) : 0f;
                dots[i].color = UI.Alpha(accent, a);
            }
        }
    }

    // Stamp of the frame a formula line was revealed in (see LessonPanel.FormulaClear).
    public class FrameStamp : MonoBehaviour { public int frame; }

    // A volume slider takes the bus's value each time it is shown (another slider may have moved it meanwhile).
    public class VolumeSync : MonoBehaviour
    {
        public AudioBus.Kind kind;
        public Slider slider;
        void OnEnable() { if (slider) slider.SetValueWithoutNotify(AudioBus.Get(kind)); }
    }

    // A dock card's height in the column: never less than its content needs (TextMeshPro reports no minimum height, so
    // a crowded column squeezed text over text); the data views shrink instead (their blocks turn this off). `floor`
    // keeps the turn card from getting shorter while an activity's controls sit under it.
    public class CardHeight : MonoBehaviour, ILayoutElement
    {
        public float floor;
        LayoutGroup group;

        float Content => group || TryGetComponent(out group) ? Mathf.Max(floor, group.preferredHeight) : -1f;
        public float minHeight => Content;
        public float preferredHeight => Content;
        public float flexibleHeight => -1f;
        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public int layoutPriority => 1;
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
    }

    // Entrance of a card that just appeared: alpha 0 -> 1 over LessonPanel.PanelSeconds of unscaled time, decelerating
    // (none under reduced motion: it simply appears), optionally after a wait (the stage's graph follows its table).
    public class CardFade : MonoBehaviour
    {
        CanvasGroup group;
        float t = 1f, wait;

        public void Restart(float delay = 0f)
        {
            if (!group && !TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
            t = ReducedMotion.On ? 1f : 0f;
            wait = ReducedMotion.On ? 0f : delay;
            group.alpha = t;
        }

        void Update()
        {
            if (t >= 1f) return;
            if (wait > 0f) { wait -= Time.unscaledDeltaTime; group.alpha = 0f; return; }
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / LessonPanel.PanelSeconds);
            group.alpha = 1f - (1f - t) * (1f - t);
        }
    }

    // Keeps a fixed-px canvas scaled to the height its holder was given: when the column is crowded the layout
    // shrinks the holder toward its minimum and the canvas follows, keeping its aspect.
    public class CanvasFit : MonoBehaviour
    {
        public RectTransform canvas;
        public Vector2 size;
        public float scale;

        void LateUpdate()
        {
            float height = ((RectTransform)transform).rect.height;
            if (height <= 0f || !canvas) return;
            float s = Mathf.Min(scale, height / size.y);
            if (!Mathf.Approximately(canvas.localScale.x, s)) canvas.localScale = Vector3.one * s;
        }
    }

    // Scale-in of a UI card: 0.96 -> 1 with a soft overshoot over LessonPanel.PanelSeconds, unscaled time.
    public class UIPop : MonoBehaviour
    {
        float t = 1f;
        public void Restart() => t = 0f;

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / LessonPanel.PanelSeconds);
            float s = 0.96f + 0.04f * (1f + Mathf.Sin(t * Mathf.PI * 0.5f) * (1f + 0.35f * Mathf.Sin(t * Mathf.PI) * (1 - t)));
            transform.localScale = Vector3.one * (t >= 1f ? 1f : Mathf.Min(1.03f, s));
        }
    }

    // On the main camera: shifts the projection so the 3D view is centred in the part of the screen the dock leaves
    // free (nothing when the scene reserves no dock), and on screens narrower than 16:9 (4:3 tablets) keeps the shots'
    // 16:9 horizontal field of view (Unity keeps the vertical one, which crops the edges of compositions authored
    // for 16:9). Applied after Cinemachine has placed the camera for the frame, from the camera's own fov, aspect and
    // clip planes, so every shot composes without re-authoring the cameras; the shift follows the dock's width.
    [DefaultExecutionOrder(900)]
    public class PanelFraming : MonoBehaviour
    {
        const float ReferenceAspect = 16f / 9f;
        Camera cam;
        int steppedFrame = -1;

        void Awake() => cam = GetComponent<Camera>();
        void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(CameraUpdated);
        void OnDisable() { CinemachineCore.CameraUpdatedEvent.RemoveListener(CameraUpdated); if (cam) cam.ResetProjectionMatrix(); }

        void CameraUpdated(CinemachineBrain brain) => Apply();

        // Without a brain this frame (no Cinemachine in the scene), once per frame from LateUpdate.
        void LateUpdate() { if (CinemachineBrain.ActiveBrainCount == 0 || Time.frameCount - steppedFrame > 1) Apply(); }

        void Apply()
        {
            steppedFrame = Time.frameCount;
            if (!cam) return;
            var panel = LessonPanel.Instance;
            float share = panel ? panel.ScreenShare : 0f;
            float fov = cam.fieldOfView, aspect = cam.aspect;
            bool narrow = aspect < ReferenceAspect - 0.01f;
            if (narrow)
            {
                float horizontal = 2f * Mathf.Atan(Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * ReferenceAspect);
                fov = 2f * Mathf.Atan(Mathf.Tan(horizontal * 0.5f) / aspect) * Mathf.Rad2Deg;
            }
            if (share <= 0.001f && !narrow) { cam.ResetProjectionMatrix(); return; }
            var m = Matrix4x4.Perspective(fov, aspect, cam.nearClipPlane, cam.farClipPlane);
            m[0, 2] = share;
            cam.projectionMatrix = m;
        }
    }
}
