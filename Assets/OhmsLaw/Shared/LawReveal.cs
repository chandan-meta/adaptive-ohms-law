using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OhmsLaw
{
    // The finale's one centred surface (Scene 13, "law.*" cues): the three clues V, I and R arrive in their slots, the
    // learner taps them into V = I × R (each tile flies into its place in the equation), OHM'S LAW lands above it, the
    // conditions and the two rearrangements join under it, and a live line shows the law holding while the bench sweeps
    // the voltage (V and I rise together, R stays put). It lives on the HUD root, not in the dock: LessonPanel.Follow
    // places it in the hero's band every frame and ResetForScene clears it. Layout is in fixed px (Size, y down from the
    // top-left), so the tiles can fly between the slots and the equation; the glass plate under it grows downward as
    // each part arrives, and the whole surface scales to fit the band (never under the captions or the controls).
    public class LawReveal : MonoBehaviour
    {
        public static LawReveal Instance { get; private set; }

        public static readonly Vector2 Size = new Vector2(1060f, 630f);
        static readonly string[] Sequence = { "V", "I", "R" };
        static readonly string[] Next = { "Start with V, the push.", "Next comes I, the flow.", "Last: R, the opposition." };
        // Layout px: the three slots, and where each symbol lands in the equation (the "=" and "×" between them).
        static readonly float[] SlotX = { 230f, 530f, 830f };
        const float SlotY = 230f, EquationY = 230f, HintY = 372f, ConditionsY = 368f, FormsY = 446f, DemoY = 528f, BarsY = 584f;
        static readonly Dictionary<string, float> GlyphX = new Dictionary<string, float> { ["V"] = 330f, ["="] = 430f, ["I"] = 530f, ["×"] = 630f, ["R"] = 730f };
        static readonly Vector2 TileSize = new Vector2(270f, 200f);
        const float SymbolFont = 120f, EquationFont = 150f, SymbolRise = 30f, BarLength = 460f;
        // Where the plate ends for each stage of the reveal (layout px from the top).
        const float PiecesBottom = 412f, FormsBottom = 498f, DemoBottom = 630f;

        class Tile
        {
            public string symbol;
            public RectTransform rect, symbolRect;
            public CanvasGroup body;
            public Image edge;
            public Button button;
            public Color color;
            public bool shown, placed, flying, hovered;
            public int slot;
        }

        class Tween { public float t, seconds, delay; public System.Action<float> step; public System.Action done; }

        RectTransform rect, plate, area, glow, equation, demoRow, bars;
        TextMeshProUGUI eyebrow, title, hint, conditions, vValue, iValue, rValue, rNote;
        TextMeshProUGUI[] forms, operators;
        Image vBar, iBar;
        readonly Dictionary<string, Tile> tiles = new Dictionary<string, Tile>();
        readonly List<Tween> tweens = new List<Tween>();
        readonly List<Image> slotFrames = new List<Image>();   // the empty places; they go once the equation is whole
        float plateBottom = PiecesBottom, plateShown = 0f;
        int placed;
        bool building, demoing;
        Coroutine sweep;

        // The equation's on-screen rect (WattsonGuide points at it: "point.law").
        public RectTransform EquationRect => equation;
        // The learner has built the law and the last tile has landed.
        public bool Built => placed >= Sequence.Length && !AnyFlying;
        bool AnyFlying { get { foreach (var tile in tiles.Values) if (tile.flying) return true; return false; } }

        static Color VoltColor => LessonPanel.VoltColor;
        static Color AmpColor => LessonPanel.AmpColor;
        static Color OhmColor => LessonPanel.OhmColor;
        static Color Of(string symbol) => symbol == "V" ? VoltColor : symbol == "I" ? AmpColor : OhmColor;

        // ------------------------------------------------------------------ lifetime

        public static LawReveal Ensure()
        {
            if (Instance) return Instance;
            var lesson = LessonPanel.Instance;
            if (!lesson) return null;
            // Over the dock, under the captions, the controls and the menus (as LessonPanel's own centred surfaces). The
            // name starts with "UI_": WattsonGuide stands beside the nearest such surface.
            var root = UI.Rect("UI_LawReveal", lesson.Root, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Size.x, Size.y));
            root.SetSiblingIndex(lesson.Dock.GetSiblingIndex() + 1);
            var reveal = root.gameObject.AddComponent<LawReveal>();
            reveal.rect = root;
            reveal.Build();
            return Instance = reveal;
        }

        // A new scene (LessonPanel.ResetForScene) or the end of the finale: gone at once.
        public static void Clear()
        {
            if (!Instance) return;
            Instance.gameObject.SetActive(false);
            Destroy(Instance.gameObject);
            Instance = null;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // LessonPanel.Follow: centred at x in the band [bottom, top] of the free frame (`free` wide), scaled to fit the
        // whole reveal's height so it never rescales as the parts arrive; the plate grows downward from the top.
        public void Place(float x, float top, float bottom, float free)
        {
            float band = Mathf.Max(120f, top - bottom);
            float scale = Mathf.Min(1f, (band - 16f) / Size.y, (free - 2f * LessonPanel.Margin) / Size.x);
            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = new Vector2(x, top - (band - Size.y * scale) / 2f);
        }

        void Build()
        {
            plate = UI.Rect("Plate", rect, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 0));
            plate.anchorMax = new Vector2(1, 1);
            plate.sizeDelta = new Vector2(0, plateBottom);
            LessonPanel.Card(plate, 0.95f, 26f);
            var frame = UI.Stretch("Frame", plate);
            UI.Outline(frame, UI.Alpha(LessonPanel.Accent, 0.8f), 26f, 2f);
            glow = UI.Stretch("Glow", plate);
            glow.SetAsFirstSibling();
            UI.Outline(glow, Color.clear, 34f, 3f).raycastTarget = false;
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            Animate(LessonPanel.PanelSeconds, k => group.alpha = k);

            area = UI.Rect("Area", rect, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Size);
            eyebrow = Draw.Label(area, "THE THREE CLUES", new Vector2(Size.x / 2f, 52f), 18f, LessonPanel.Muted, new Vector2(600, 30));
            eyebrow.characterSpacing = 8f;
            eyebrow.fontStyle = FontStyles.Bold;
            title = UI.Display(Draw.Label(area, "OHM'S LAW", new Vector2(Size.x / 2f, 70f), 78f, LessonPanel.Gold, new Vector2(900, 100)));
            title.characterSpacing = 12f;
            title.alpha = 0f;

            for (int i = 0; i < SlotX.Length; i++)
            {
                var slot = UI.Rect("Slot " + (i + 1), area, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(SlotX[i], -SlotY), TileSize);
                var outline = UI.Outline(slot, UI.Alpha(LessonPanel.Accent, 0.28f), 18f, 2f);
                outline.raycastTarget = false;
                slotFrames.Add(outline);
            }
            equation = UI.Rect("Equation", area, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(530f, -EquationY), new Vector2(560f, 170f));
            operators = new TextMeshProUGUI[2];
            int o = 0;
            foreach (var op in new[] { "=", "×" })
            {
                var label = UI.Display(Draw.Label(area, op, new Vector2(GlyphX[op], EquationY), EquationFont, LessonPanel.Paper, new Vector2(120, 170)));
                label.alpha = 0f;
                operators[o++] = label;
            }
            for (int i = 0; i < Sequence.Length; i++) tiles[Sequence[i]] = MakeTile(Sequence[i], i);

            hint = Draw.Label(area, "", new Vector2(Size.x / 2f, HintY), 22f, LessonPanel.Spark, new Vector2(900, 34));
            hint.fontStyle = FontStyles.Bold;
            conditions = Draw.Label(area, "for an ohmic conductor at constant temperature", new Vector2(Size.x / 2f, ConditionsY), 28f, LessonPanel.Body, new Vector2(900, 40));
            conditions.alpha = 0f;
            forms = new[]
            {
                UI.Display(Draw.Label(area, LessonPanel.Symbols("I = V ÷ R"), new Vector2(340f, FormsY), 58f, LessonPanel.Paper, new Vector2(360, 76))),
                UI.Display(Draw.Label(area, LessonPanel.Symbols("R = V ÷ I"), new Vector2(720f, FormsY), 58f, LessonPanel.Paper, new Vector2(360, 76))),
            };
            foreach (var form in forms) form.alpha = 0f;
            BuildDemo();
        }

        Tile MakeTile(string symbol, int slot)
        {
            var tile = new Tile { symbol = symbol, slot = slot, color = Of(symbol) };
            tile.rect = UI.Rect("Tile " + symbol, area, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(SlotX[slot], -SlotY), TileSize);
            var body = UI.Stretch("Body", tile.rect);
            tile.body = body.gameObject.AddComponent<CanvasGroup>();
            var fill = UI.Panel(body, LessonPanel.Raised, true, 18f);
            fill.raycastTarget = true;
            tile.edge = UI.Outline(UI.Stretch("Edge", body), UI.Alpha(tile.color, 0.65f), 18f, 2.5f);
            tile.edge.raycastTarget = false;
            var (name, unit) = symbol == "V" ? ("potential difference", "the push · volt (V)") : symbol == "I" ? ("current", "the flow · ampere (A)") : ("resistance", "the opposition · ohm (Ω)");
            var nameLabel = LessonPanel.Text(UI.Rect("Name", body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -44f), new Vector2(250, 32)), name, 22f, LessonPanel.Paper, TextAlignmentOptions.Center); // "potential difference" fits the tile with room
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            var unitLabel = LessonPanel.Text(UI.Rect("Unit", body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -74f), new Vector2(250, 28)), LessonPanel.Symbols(unit), 19f, LessonPanel.Muted, TextAlignmentOptions.Center, false);
            unitLabel.textWrappingMode = TextWrappingModes.NoWrap;
            tile.symbolRect = UI.Rect("Symbol", tile.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, SymbolRise), new Vector2(200, 150));
            var glyph = UI.Display(UI.Label(tile.symbolRect, symbol, SymbolFont, tile.color));
            glyph.textWrappingMode = TextWrappingModes.NoWrap;
            glyph.overflowMode = TextOverflowModes.Overflow;
            tile.button = tile.rect.gameObject.AddComponent<Button>();
            tile.button.transition = Selectable.Transition.None;
            tile.button.targetGraphic = fill;
            tile.button.onClick.AddListener(() => Tap(symbol));
            tile.button.interactable = false;
            var hover = tile.rect.gameObject.AddComponent<EventTrigger>();
            LessonPanel.On(hover, EventTriggerType.PointerEnter, () => tile.hovered = true);
            LessonPanel.On(hover, EventTriggerType.PointerExit, () => tile.hovered = false);
            tile.rect.gameObject.SetActive(false);
            return tile;
        }

        void BuildDemo()
        {
            demoRow = UI.Rect("Demo", area, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Size);
            var group = demoRow.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            vValue = UI.Display(Draw.Label(demoRow, "", new Vector2(GlyphX["V"], DemoY), 44f, VoltColor, new Vector2(220, 60)));
            Draw.Label(demoRow, "=", new Vector2(GlyphX["="], DemoY), 44f, LessonPanel.Paper, new Vector2(60, 60));
            iValue = UI.Display(Draw.Label(demoRow, "", new Vector2(GlyphX["I"] + 15f, DemoY), 44f, AmpColor, new Vector2(220, 60)));
            Draw.Label(demoRow, "×", new Vector2(GlyphX["×"] + 20f, DemoY), 44f, LessonPanel.Paper, new Vector2(60, 60));
            rValue = UI.Display(Draw.Label(demoRow, "", new Vector2(GlyphX["R"] + 30f, DemoY), 44f, OhmColor, new Vector2(200, 60)));
            // V and I as bars against their own full scales (5 V, and the current 5 V would drive): with R fixed they are
            // always the same length, which is "proportional" made visible.
            bars = UI.Rect("Bars", demoRow, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Size);
            vBar = Bar("V", BarsY, VoltColor);
            iBar = Bar("I", BarsY + 26f, AmpColor);
            rNote = Draw.Label(bars, "", new Vector2(880f, BarsY + 13f), 21f, LessonPanel.Body, new Vector2(220, 30));
            rNote.fontStyle = FontStyles.Bold;
        }

        Image Bar(string symbol, float y, Color color)
        {
            Draw.Label(bars, symbol, new Vector2(282f, y), 22f, color, new Vector2(30, 26)).fontStyle = FontStyles.Bold;
            var track = UI.Rect(symbol + " Track", bars, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(300f, -y), new Vector2(BarLength, 14f));
            UI.Panel(track, new Color(1, 1, 1, 0.12f), true, 7f).raycastTarget = false;
            var fill = UI.Rect("Fill", track, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0f, 14f));
            var image = UI.Panel(fill, color, true, 7f);
            image.raycastTarget = false;
            return image;
        }

        // ------------------------------------------------------------------ the cues

        // "law.piece V|I|R": that clue arrives in its slot with its name and unit.
        public void Piece(string symbol)
        {
            if (!tiles.TryGetValue(symbol, out var tile) || tile.shown) return;
            tile.shown = true;
            tile.rect.gameObject.SetActive(true);
            tile.rect.localScale = Vector3.one * 0.7f;
            var group = Group(tile.rect);
            group.alpha = 0f;
            Animate(ReducedMotion.On ? 0.01f : 0.32f, k =>
            {
                group.alpha = k;
                tile.rect.localScale = Vector3.one * (0.7f + 0.3f * k + (ReducedMotion.On ? 0f : 0.06f * Mathf.Sin(k * Mathf.PI)));
            });
        }

        // The BuildLaw gate (LawTiles): every clue in place, then shuffled to R · V · I so the build is a choice, the tiles
        // live, the hint under them.
        public void BeginBuild()
        {
            if (building || placed > 0) return;
            foreach (var symbol in Sequence) Piece(symbol);
            building = true;
            var shuffled = new[] { "R", "V", "I" };
            for (int i = 0; i < shuffled.Length; i++)
            {
                var tile = tiles[shuffled[i]];
                tile.slot = i;
                Vector2 from = tile.rect.anchoredPosition, to = new Vector2(SlotX[i], -SlotY);
                Animate(ReducedMotion.On ? 0.01f : 0.4f, k => tile.rect.anchoredPosition = Vector2.Lerp(from, to, k) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * 26f), null, 0.05f * i);
                tile.button.interactable = true;
            }
            hint.text = Next[0];
        }

        public void Tap(string symbol)
        {
            if (!building || placed >= Sequence.Length || !tiles.TryGetValue(symbol, out var tile) || tile.placed) return;
            if (symbol == Sequence[placed])
            {
                placed++;
                tile.placed = true;
                tile.button.interactable = false;
                UiSounds.Play("done");
                hint.text = placed >= Sequence.Length ? "That's the law." : Next[placed];
                Fly(tile, placed);
            }
            else
            {
                hint.text = Next[placed];
                UiSounds.Play("hint");
                Shake(tile);
            }
        }

        // The next correct tile (autoAnswer, Enter).
        public void Advance() { if (building && placed < Sequence.Length) Tap(Sequence[placed]); }

        // The tile's card fades away while its symbol flies to its place in the equation and grows to the equation's size;
        // the "=" lands with V, the "×" with I, and R completes it.
        void Fly(Tile tile, int order)
        {
            tile.flying = true;
            Vector2 from = tile.rect.anchoredPosition, to = new Vector2(GlyphX[tile.symbol], -EquationY);
            float lift = ReducedMotion.On ? 0f : 40f;
            Animate(ReducedMotion.On ? 0.12f : 0.45f, k =>
            {
                tile.rect.anchoredPosition = Vector2.Lerp(from, to, k) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * lift);
                tile.rect.localScale = Vector3.one;
                tile.body.alpha = 1f - k;
                tile.symbolRect.anchoredPosition = new Vector2(0f, SymbolRise * (1f - k));
                tile.symbolRect.localScale = Vector3.one * Mathf.Lerp(1f, EquationFont / SymbolFont, k);
            }, () =>
            {
                tile.flying = false;
                tile.body.blocksRaycasts = false;
                if (order <= operators.Length) PopIn(operators[order - 1]);
                if (order >= Sequence.Length)
                {
                    Flash(0.6f);
                    // The equation stands alone: the empty places it was built from fade out behind it.
                    Animate(0.3f, k => { foreach (var frame in slotFrames) if (frame) frame.color = UI.Alpha(LessonPanel.Accent, 0.28f * (1f - k)); });
                }
            });
        }

        // "law.name": OHM'S LAW above the equation (the eyebrow gives way), with one soft ring of light off the plate.
        public void Name()
        {
            Animate(0.25f, k => eyebrow.alpha = 1f - k);
            PopIn(title, 0.85f);
            Flash(1f);
        }

        // "law.conditions": the line that says when the law holds.
        public void Conditions()
        {
            hint.text = "";
            Rise(conditions, 0f);
        }

        // "law.forms": I = V ÷ R and R = V ÷ I slide in under the law, one after the other.
        public void Forms()
        {
            hint.text = "";
            plateBottom = Mathf.Max(plateBottom, FormsBottom);
            for (int i = 0; i < forms.Length; i++) Rise(forms[i], 0.15f * i);
        }

        // "law.demo [seconds]": the bench sweeps 1 → 5 V in whole volts over `seconds`; the live line under the law reads
        // the meters (V = I × R with this wire's R), V and I pulse together on every step and their bars grow in step.
        public void Demo(float seconds, System.Action<float> stepped)
        {
            hint.text = "";
            plateBottom = DemoBottom;
            demoing = true;
            var group = demoRow.GetComponent<CanvasGroup>();
            Animate(0.3f, k => group.alpha = k);
            if (sweep != null) StopCoroutine(sweep);
            sweep = StartCoroutine(Sweep(Mathf.Max(1f, seconds), stepped));
        }

        IEnumerator Sweep(float seconds, System.Action<float> stepped)
        {
            var rig = CircuitRig.Active;
            for (int volts = 1; volts <= 5; volts++)
            {
                if (rig) rig.SetVoltage(volts);
                stepped?.Invoke(volts);
                Pulse(vValue);
                Pulse(iValue);
                for (float t = 0; t < seconds / 5f; t += ModuleFlow.UserPaused ? 0f : Time.deltaTime) yield return null;
            }
            sweep = null;
        }

        // "law.clear": the surface shrinks a little and fades, then goes.
        public void Close()
        {
            var group = Group(rect);
            float from = group.alpha;
            Animate(ReducedMotion.On ? 0.01f : 0.35f, k =>
            {
                group.alpha = from * (1f - k);
                area.localScale = plate.localScale = Vector3.one * (1f - 0.08f * k);
            }, Clear);
        }

        // ------------------------------------------------------------------ every frame

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < tweens.Count; i++)
            {
                var tween = tweens[i];
                if (tween.delay > 0f) { tween.delay -= dt; continue; }
                tween.t = Mathf.Min(1f, tween.t + dt / Mathf.Max(0.01f, tween.seconds));
                float k = 1f - (1f - tween.t) * (1f - tween.t) * (1f - tween.t);   // ease out
                tween.step(k);
                if (tween.t < 1f) continue;
                tweens.RemoveAt(i--);
                tween.done?.Invoke();
            }

            // The plate grows to what is shown (pieces → forms → the live line).
            plateShown = Mathf.Lerp(plateShown <= 0f ? plateBottom : plateShown, plateBottom, ReducedMotion.On ? 1f : 1f - Mathf.Exp(-9f * dt));
            plate.sizeDelta = new Vector2(0f, plateShown);

            // Live tiles breathe their edge and lift under the pointer.
            foreach (var tile in tiles.Values)
            {
                if (!tile.shown || tile.placed) continue;
                bool live = tile.button.interactable;
                float breath = live && !ReducedMotion.On ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f + tile.slot) : 0f;
                tile.edge.color = UI.Alpha(live ? Color.Lerp(tile.color, LessonPanel.Glow, 0.35f * breath) : tile.color, live ? 0.7f + 0.3f * breath : 0.65f);
                if (!tile.flying && building) tile.rect.localScale = Vector3.Lerp(tile.rect.localScale, Vector3.one * (live && tile.hovered ? 1.05f : 1f), 1f - Mathf.Exp(-14f * dt));
            }

            if (demoing) Live();
        }

        // The live line and the bars from the bench: V = I × R, with R read off the meters (this wire's 5 Ω).
        void Live()
        {
            var rig = CircuitRig.Active;
            var data = ExperimentData.Main;
            float volts = rig ? rig.Volts : 5f, amps = rig ? rig.Amps : (data ? data.CurrentAt(5f) : 1f);
            float ohms = amps > 1e-4f ? volts / amps : data ? data.resistance : 5f;
            vValue.text = volts.ToString("0.0") + " V";
            iValue.text = amps.ToString("0.00") + " A";
            rValue.text = ohms.ToString("0.#") + " Ω";
            rNote.text = LessonPanel.Symbols("R stays " + ohms.ToString("0.#") + " Ω");
            float full = CircuitRig.MaxVolts, k = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
            SetBar(vBar, Mathf.Lerp(Width(vBar), BarLength * Mathf.Clamp01(volts / full), k));
            SetBar(iBar, Mathf.Lerp(Width(iBar), BarLength * Mathf.Clamp01(amps / Mathf.Max(1e-4f, full / ohms)), k));
        }

        static float Width(Image bar) => bar.rectTransform.sizeDelta.x;
        static void SetBar(Image bar, float width) => bar.rectTransform.sizeDelta = new Vector2(width, bar.rectTransform.sizeDelta.y);

        // ------------------------------------------------------------------ motion helpers

        void Animate(float seconds, System.Action<float> step, System.Action done = null, float delay = 0f)
        {
            tweens.Add(new Tween { seconds = seconds, step = step, done = done, delay = delay });
            if (seconds <= 0.011f && delay <= 0f) { step(1f); }
        }

        static CanvasGroup Group(Component c) => c.TryGetComponent(out CanvasGroup group) ? group : c.gameObject.AddComponent<CanvasGroup>();

        // A label appears with a small scale-in (from `from` x its size), its rect pivoting on its centre.
        void PopIn(TextMeshProUGUI label, float from = 0.6f)
        {
            var box = (RectTransform)label.transform.parent;
            Animate(ReducedMotion.On ? 0.01f : 0.3f, k =>
            {
                label.alpha = k;
                box.localScale = Vector3.one * (from + (1f - from) * k + (ReducedMotion.On ? 0f : 0.08f * Mathf.Sin(k * Mathf.PI)));
            });
        }

        // A line rises 24 px into place as it fades in.
        void Rise(TextMeshProUGUI label, float delay)
        {
            var box = (RectTransform)label.transform.parent;
            Vector2 home = box.anchoredPosition;
            label.alpha = 0f;
            Animate(ReducedMotion.On ? 0.01f : 0.35f, k =>
            {
                label.alpha = k;
                box.anchoredPosition = home + Vector2.down * (24f * (1f - k));
            }, null, delay);
        }

        void Pulse(TextMeshProUGUI label)
        {
            if (ReducedMotion.On) return;
            var box = (RectTransform)label.transform.parent;
            Animate(0.3f, k => box.localScale = Vector3.one * (1f + 0.16f * (1f - k)));
        }

        void Shake(Tile tile)
        {
            if (ReducedMotion.On || tile.flying) return;
            Vector2 home = new Vector2(SlotX[tile.slot], -SlotY);
            Animate(0.3f, k => tile.rect.anchoredPosition = home + Vector2.right * (Mathf.Sin(k * Mathf.PI * 6f) * 10f * (1f - k)));
        }

        // A ring of light leaves the plate's edge and fades (the hero plate's arrival, softer).
        void Flash(float strength)
        {
            var ring = glow.GetComponent<Image>();
            Animate(ReducedMotion.On ? 0.01f : 0.9f, k =>
            {
                float spread = 10f + 90f * k;
                glow.offsetMin = -Vector2.one * spread;
                glow.offsetMax = Vector2.one * spread;
                ring.color = UI.Alpha(LessonPanel.Glow, ReducedMotion.On ? 0f : 0.75f * strength * (1f - k));
            });
        }
    }
}
