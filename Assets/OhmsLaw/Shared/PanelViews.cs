using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OhmsLaw
{
    // The lesson panel's small views, all built by code at runtime (hence one file): the voltage buttons, the charge
    // counter for the wire's cross-section, the live circuit schematic, the resistance comparison inset, and the
    // UI line-art helpers they draw with.

    // Line art from UI Images, in layout space (x right, y down from the parent's top-left corner).
    public static class Draw
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

        public static Image Disc(Transform parent, Vector2 centre, float diameter, Color color, float hole = 0f)
        {
            var image = UI.Rect("Disc", parent, TopLeft, new Vector2(0.5f, 0.5f), new Vector2(centre.x, -centre.y), Vector2.one * diameter).gameObject.AddComponent<Image>();
            image.sprite = UI.Circle(hole);
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

        public static TextMeshProUGUI Label(Transform parent, string text, Vector2 centre, float font, Color color, Vector2? size = null)
        {
            var rect = UI.Rect("Label", parent, TopLeft, new Vector2(0.5f, 0.5f), new Vector2(centre.x, -centre.y), size ?? new Vector2(200, 40));
            var label = UI.Label(rect, text, font, color);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        // A fixed-size drawing area scaled to the block's width: the box keeps its own px layout, the holder takes the
        // scaled height in the column.
        public static RectTransform Box(RectTransform block, Vector2 size, float scale)
        {
            var holder = UI.Rect("Box", block, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, size.y * scale));
            LessonPanel.Fixed(holder, size.y * scale);
            var rect = UI.Rect("Area", holder, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, size);
            rect.localScale = Vector3.one * scale;
            return rect;
        }
    }

    // ------------------------------------------------------------------ voltage buttons

    // Five equal steps, 1 V to 5 V, in the Volt block. The director wires them to the rig. Each reads its state: done (a
    // check over its left edge, muted: its row is recorded), now (paper and ink at full alpha in every state: the source is
    // there), next (spark outline, pulsing: the one to tap), later (36 %: above the cap, or not the learner's turn; a tap
    // says why). The step count ("3 OF 5") is the turn card's.
    public class VoltageButtons : MonoBehaviour
    {
        public System.Func<float, float> set;   // request -> applied
        public System.Func<bool> open;          // the learner may change the voltage now
        public System.Func<float> current, max;

        readonly Button[] buttons = new Button[5];
        readonly HudButton[] huds = new HudButton[5];
        readonly TextMeshProUGUI[] words = new TextMeshProUGUI[5];
        readonly Image[] checks = new Image[5];

        public static VoltageButtons Create()
        {
            var lesson = LessonPanel.Instance;
            var block = lesson.Fresh("Volt", null, 12f);
            var view = block.gameObject.AddComponent<VoltageButtons>();
            LessonPanel.Eyebrow(block, "Set the battery");
            var row = LessonPanel.Row("Row", block, 8f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            LessonPanel.Fixed(row, 62f);
            for (int i = 0; i < 5; i++)
            {
                float volts = i + 1;
                var button = LessonPanel.Pill(volts + " V", row, volts.ToString("0") + " V", () => { if (view.set != null && (view.open == null || view.open())) view.set(volts); }, LessonPanel.Kind.Raised, null, 62f, 21f);
                var size = button.GetComponent<LayoutElement>();
                size.flexibleWidth = 1f;
                size.preferredWidth = 0f; // the row shares its width equally, whatever the label
                var line = button.GetComponent<HorizontalLayoutGroup>();
                line.childAlignment = TextAnchor.MiddleCenter;
                line.padding = new RectOffset(6, 6, 0, 0);
                line.spacing = 6f;
                view.checks[i] = LessonPanel.Icon(button.transform, "check", 16f, LessonPanel.Spark);
                view.checks[i].GetComponent<LayoutElement>().ignoreLayout = true; // over the left edge: the label stays centred
                view.checks[i].rectTransform.anchoredPosition = new Vector2(16f, 0f);
                view.buttons[i] = button;
                view.huds[i] = button.GetComponent<HudButton>();
                view.huds[i].activeOutline = LessonPanel.Paper;
                view.huds[i].disabledTap = () => lesson.ShowHint(view.open != null && view.open() ? "One step at a time." : "Wait for your turn.");
                view.words[i] = button.GetComponentInChildren<TextMeshProUGUI>();
            }
            lesson.Show("Volt", true);
            return view;
        }

        void Update()
        {
            float now = current != null ? current() : 0f, cap = max != null ? max() : 5f;
            bool enabled = open != null && open() && !ModuleFlow.UserPaused;
            float pulse = ReducedMotion.On ? 1f : 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
            for (int i = 0; i < 5; i++)
            {
                float volts = i + 1;
                bool selected = Mathf.Approximately(now, volts), above = volts > cap + 0.001f;
                bool recorded = !selected && LearnerSession.tableRows >= volts;
                bool next = enabled && !selected && !above && Mathf.Approximately(volts, cap);
                buttons[i].interactable = (enabled && !above) || selected; // the source's own value never dims
                huds[i].active = selected;
                huds[i].outlineColor = next ? UI.Alpha(LessonPanel.Spark, pulse) : LessonPanel.ControlLine;
                words[i].color = selected ? LessonPanel.Ink : recorded ? LessonPanel.Muted : LessonPanel.Paper;
                if (checks[i].gameObject.activeSelf != recorded) checks[i].gameObject.SetActive(recorded);
            }
        }
    }

    // ------------------------------------------------------------------ the charge counter

    // The wire's cross-section counter in the Counter block: the charges that have crossed the ring this second
    // (1, 2, 3 ...), a ring that fills over the second, the last completed second's count, and what that means.
    public class WireCounterView : MonoBehaviour
    {
        public WireInterior wire;

        TextMeshProUGUI count, perSecond, meaning;
        Image ring;
        int shownTally = -1, shownLast = -1;
        float flash;

        public static WireCounterView Create(WireInterior wire)
        {
            var lesson = LessonPanel.Instance;
            var block = lesson.Fresh("Counter", null, 6f);
            var view = block.gameObject.AddComponent<WireCounterView>();
            view.wire = wire;

            LessonPanel.Eyebrow(block, "Charge through the section");

            var row = UI.Rect("Row", block, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 110));
            LessonPanel.Fixed(row, 110f);
            var track = UI.Rect("Track", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(100, 100));
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.sprite = UI.Circle(0.82f);
            trackImage.color = new Color(1, 1, 1, 0.18f);
            trackImage.raycastTarget = false;
            var fill = UI.Stretch("Fill", track);
            view.ring = fill.gameObject.AddComponent<Image>();
            view.ring.sprite = UI.Circle(0.82f);
            view.ring.color = UI.Theme ? UI.Theme.charge : Color.cyan;
            view.ring.type = Image.Type.Filled;
            view.ring.fillMethod = Image.FillMethod.Radial360;
            view.ring.fillOrigin = (int)Image.Origin360.Top;
            view.ring.fillClockwise = true;
            view.ring.fillAmount = 0f;
            view.ring.raycastTarget = false;
            view.count = UI.Label(track, "0", 48f, UI.Theme ? UI.Theme.current : Color.yellow);
            view.count.fontStyle = FontStyles.Bold;
            var text = UI.Rect("Text", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(124, 0), new Vector2(0, 100));
            text.anchorMax = new Vector2(1, 0.5f);
            text.sizeDelta = new Vector2(-124, 100);
            view.perSecond = UI.Label(text, "", 24f, Color.white, TextAlignmentOptions.MidlineLeft);
            view.perSecond.rectTransform.offsetMax = new Vector2(0, -2);
            view.perSecond.rectTransform.offsetMin = new Vector2(0, 42);
            view.meaning = UI.Label(text, LessonPanel.Quantities("charge ÷ time = current (I)"), 22f, LessonPanel.Paper, TextAlignmentOptions.MidlineLeft);
            view.meaning.fontStyle = FontStyles.Bold;
            view.meaning.rectTransform.offsetMax = new Vector2(0, -56);
            view.meaning.rectTransform.offsetMin = new Vector2(0, 0);

            UI.Label(block, "Simplified: a few representative electrons, slowed down so you can count them. The real wire carries billions per second.", 17f, LessonPanel.Muted, TextAlignmentOptions.TopLeft);
            lesson.Show("Counter", true);
            return view;
        }

        public void Hit() => flash = 1f;

        void Update()
        {
            if (!wire) return;
            bool counting = wire.counting;
            ring.fillAmount = counting ? wire.SecondProgress : 0f;
            int tally = counting ? wire.Tally : 0;
            if (tally != shownTally)
            {
                shownTally = tally;
                count.text = tally.ToString(CultureInfo.InvariantCulture);
                if (tally > 0) flash = 1f;
            }
            int last = counting && wire.Shown ? wire.LastSecond : -1;
            if (last != shownLast)
            {
                shownLast = last;
                perSecond.text = last < 0 ? "counting the first second..." : $"last second: <b>{last}</b> charges";
            }
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 2.5f);
            count.transform.localScale = Vector3.one * (1f + (ReducedMotion.On ? 0f : 0.18f * flash));
        }
    }

    // ------------------------------------------------------------------ circuit schematic

    // A clean schematic of the bench circuit in the Schematic block, kept in step with CircuitRig.Active: the source,
    // the ammeter in series, the nichrome wire XY, the key, and the voltmeter in parallel across XY. Parts not yet
    // placed are dim outlines; the slot the learner should fill pulses; once the key closes the loop lights in the
    // conventional-current direction, labelled as such.
    public class CircuitSchematic : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(600, 250);
        const float Loop = 6f, Thin = 4f;
        static readonly Vector2 A = new Vector2(70, 170), B = new Vector2(540, 170);   // loop bottom corners
        const float Top = 120f, SourceX = 70f, AmmeterX = 185f, X = 300f, Y = 420f, KeyX = 540f, VoltY = 48f;

        Image ammeterRing, voltmeterRing, voltLeadX, voltLeadY, voltTop, lever;
        TextMeshProUGUI ammeterLabel, voltmeterLabel, note;
        readonly System.Collections.Generic.List<Image> loop = new System.Collections.Generic.List<Image>();
        bool shownKey;
        float pulse;

        public static CircuitSchematic Create()
        {
            var lesson = LessonPanel.Instance;
            var block = lesson.Fresh("Schematic", new RectOffset(12, 12, 8, 8));
            float scale = (LessonPanel.Inner(false) - 24f) / Size.x;
            var area = Draw.Box(block, Size, scale);
            var view = area.gameObject.AddComponent<CircuitSchematic>();
            view.Build(area);
            lesson.Show("Schematic", true);
            return view;
        }

        void Build(RectTransform area)
        {
            var chalk = new Color(1, 1, 1, 0.85f);
            var dim = new Color(1, 1, 1, 0.28f);
            Color volt = UI.Theme ? UI.Theme.voltage : Color.cyan, amber = UI.Theme ? UI.Theme.current : Color.yellow;

            // Layout space, y down. The loop: the cell on the left side, the ammeter and the wire along the top edge
            // (y 40), the key on the right side, the return along the bottom (y 170). The voltmeter hangs under the wire.
            const float wireY = 40f, cellPlus = Top - 8f, cellMinus = Top + 8f;
            Image L(Vector2 from, Vector2 to, float w = Loop) { var bar = Draw.Bar(area, from, to, w, chalk); loop.Add(bar); return bar; }
            L(new Vector2(SourceX, cellMinus), A);                       // − plate down to the bottom-left corner
            L(new Vector2(SourceX, cellPlus), new Vector2(SourceX, wireY)); // + plate up to the top wire
            Draw.Bar(area, new Vector2(SourceX - 22, cellPlus), new Vector2(SourceX + 22, cellPlus), 5, chalk);   // long plate: +
            Draw.Bar(area, new Vector2(SourceX - 11, cellMinus), new Vector2(SourceX + 11, cellMinus), 8, chalk); // short plate: −
            Draw.Label(area, "+", new Vector2(SourceX - 36, cellPlus - 6), 26, chalk, new Vector2(30, 30));
            Draw.Label(area, UI.Glyph("−", "-"), new Vector2(SourceX - 36, cellMinus + 6), 26, chalk, new Vector2(30, 30));
            Draw.Label(area, "source", new Vector2(SourceX + 56, Top + 34), 26, new Color(1, 1, 1, 0.7f), new Vector2(90, 28));
            L(new Vector2(SourceX, wireY), new Vector2(AmmeterX - 24, wireY));
            // Ammeter in series on the top wire.
            ammeterRing = Draw.Disc(area, new Vector2(AmmeterX, wireY), 48, dim, 0.78f);
            ammeterLabel = Draw.Label(area, "A", new Vector2(AmmeterX, wireY), 24, dim, new Vector2(40, 40));
            L(new Vector2(AmmeterX + 24, wireY), new Vector2(X, wireY));
            // The nichrome wire between X and Y, thicker, in the alloy's tone.
            Draw.Bar(area, new Vector2(X, wireY), new Vector2(Y, wireY), 10, new Color(0.78f, 0.72f, 0.58f));
            Draw.Disc(area, new Vector2(X, wireY), 14, chalk);
            Draw.Disc(area, new Vector2(Y, wireY), 14, chalk);
            Draw.Label(area, "X", new Vector2(X - 26, wireY - 24), 26, chalk, new Vector2(30, 28));
            Draw.Label(area, "Y", new Vector2(Y + 26, wireY - 24), 26, chalk, new Vector2(30, 28));
            Draw.Label(area, "nichrome wire", new Vector2((X + Y) / 2, wireY - 24), 26, new Color(1, 1, 1, 0.7f), new Vector2(220, 28));
            L(new Vector2(Y, wireY), new Vector2(KeyX, wireY));
            // Key on the right side: a gap whose lever swings shut.
            L(new Vector2(KeyX, wireY), new Vector2(KeyX, 95));
            Draw.Disc(area, new Vector2(KeyX, 95), 12, chalk);
            Draw.Disc(area, new Vector2(KeyX, 150), 12, chalk);
            lever = Draw.Bar(area, new Vector2(KeyX, 150), new Vector2(KeyX + 30, 102), Loop, chalk);
            L(new Vector2(KeyX, 150), B);
            Draw.Label(area, "key", new Vector2(KeyX + 40, 128), 26, new Color(1, 1, 1, 0.7f), new Vector2(70, 28));
            L(B, A);
            // Voltmeter in parallel across XY, under the wire (between the wire and the return).
            voltLeadX = Draw.Bar(area, new Vector2(X, wireY), new Vector2(X, Top), Thin, dim);
            voltLeadY = Draw.Bar(area, new Vector2(Y, wireY), new Vector2(Y, Top), Thin, dim);
            voltTop = Draw.Bar(area, new Vector2(X, Top), new Vector2(Y, Top), Thin, dim);
            voltmeterRing = Draw.Disc(area, new Vector2((X + Y) / 2, Top), 48, dim, 0.78f);
            voltmeterLabel = Draw.Label(area, "V", new Vector2((X + Y) / 2, Top), 24, dim, new Vector2(40, 40));
            note = Draw.Label(area, "Conventional current is drawn + → − (electrons drift the other way)", new Vector2(300, 218), 21, UI.Alpha(amber, 0f), new Vector2(600, 28));
        }

        void Update()
        {
            var rig = CircuitRig.Active;
            if (!rig) return;
            Color volt = UI.Theme ? UI.Theme.voltage : Color.cyan, amber = UI.Theme ? UI.Theme.current : Color.yellow;
            var dim = new Color(1, 1, 1, 0.28f);
            pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.5f);
            bool ammeterNext = rig.draggable == "ammeter" && !rig.ammeterPlaced, voltmeterNext = rig.draggable == "voltmeter" && !rig.voltmeterPlaced;
            ammeterRing.color = rig.ammeterPlaced ? amber : ammeterNext ? Color.Lerp(dim, amber, pulse) : dim;
            ammeterLabel.color = ammeterRing.color;
            voltmeterRing.color = rig.voltmeterPlaced ? volt : voltmeterNext ? Color.Lerp(dim, volt, pulse) : dim;
            voltmeterLabel.color = voltmeterRing.color;
            Color leads = rig.voltmeterPlaced ? volt : voltmeterNext ? Color.Lerp(dim, volt, pulse * 0.6f) : dim;
            voltLeadX.color = leads;
            voltLeadY.color = leads;
            voltTop.color = leads;
            bool closed = rig.state.keyClosed;
            if (closed != shownKey)
            {
                shownKey = closed;
                Draw.Set(lever, new Vector2(KeyX, 150), closed ? new Vector2(KeyX, 95) : new Vector2(KeyX + 30, 102), Loop);
            }
            // The whole loop warms to amber once the circuit is complete: that is the conventional-current path.
            bool live = closed && rig.ammeterPlaced;
            var chalk = new Color(1, 1, 1, 0.85f);
            foreach (var bar in loop) if (bar) bar.color = Color.Lerp(bar.color, live ? amber : chalk, 1 - Mathf.Exp(-6f * Time.deltaTime));
            note.color = UI.Alpha(amber, live ? 0.9f : 0f);
        }
    }

    // ------------------------------------------------------------------ resistance inset

    // Magnified comparison in the Inset block: two conductors of the same length and thickness at the same voltage,
    // copper above and nichrome below. Electrons drift along both; in the nichrome one they scatter off more ions
    // and make less headway, so fewer cross each second. Nothing is destroyed or blocked: the opposition only
    // limits how fast the charge gets through. Each strip has a bar showing how much current it lets through.
    public class ResistanceInset : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(820, 330);
        const int Dots = 14, Ions = 9;
        const float StripX = 150f, StripW = 480f, StripH = 60f, CopperY = 100f, NichromeY = 220f;

        class Strip
        {
            public RectTransform group, area;
            public Image[] dots;
            public float[] x, y, vy;
            public float speed, scatter;
            public Image bar;
            public TextMeshProUGUI amps;
        }

        Strip copper, nichrome;
        RectTransform home;
        float clock;
        System.Random random = new System.Random(7);

        public static ResistanceInset Create()
        {
            var lesson = LessonPanel.Instance;
            var block = lesson.Fresh("Inset", null, 10f);
            lesson.Expand(true);
            float scale = (LessonPanel.Inner(true) - 4f) / Size.x;
            var area = Draw.Box(block, Size, scale);
            var view = area.gameObject.AddComponent<ResistanceInset>();
            view.home = block;
            view.Build(area);
            lesson.Show("Inset", true);
            return view;
        }

        void Build(RectTransform area)
        {
            var dim = new Color(1, 1, 1, 0.7f);
            Draw.Label(area, "MAGNIFIED, NOT TO SCALE · same voltage, length, thickness and temperature", new Vector2(Size.x / 2, 22), 23, dim, new Vector2(810, 30)).characterSpacing = 1f;
            copper = BuildStrip(area, "Copper", new Color(0.86f, 0.5f, 0.3f), CopperY, speed: 150f, scatter: 0.25f, ions: 0, "MORE current", 1.0f);
            nichrome = BuildStrip(area, "Nichrome", new Color(0.58f, 0.6f, 0.52f), NichromeY, speed: 60f, scatter: 1f, ions: Ions, "LESS current", 0.42f);
            Draw.Label(nichrome.group, "More opposition = more resistance. The charges still get through, just more slowly.", new Vector2(Size.x / 2, 302), 23, dim, new Vector2(810, 30));
        }

        // Scene 12's swap: copper alone first, then the learner brings nichrome in beside it (same voltage, less current).
        public void ShowNichrome(bool on)
        {
            nichrome.group.gameObject.SetActive(on);
            if (on) LessonPanel.Pop(nichrome.group);
        }

        // The swap button under the comparison: the turn's one action, so primary. The caller removes it once the swap is
        // done, however it came (a tap, Enter, autoAnswer).
        public Button OfferSwap(System.Action swapped)
        {
            var button = LessonPanel.Pill("Swap", home, "Swap in nichrome", () => swapped(), LessonPanel.Kind.Primary, null, 56f, 20f);
            button.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            return button;
        }

        Strip BuildStrip(RectTransform area, string name, Color tint, float y, float speed, float scatter, int ions, string verdict, float share)
        {
            var strip = new Strip { speed = speed, scatter = scatter };
            // Everything of one strip under one group, so the swap shows or hides it whole (same coordinates as the area).
            strip.group = UI.Stretch(name + " Group", area);
            area = strip.group;
            Draw.Label(area, name, new Vector2(78, y), 30, Color.white, new Vector2(150, 36)).fontStyle = FontStyles.Bold;
            var body = UI.Rect(name + " Strip", area, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(StripX, -y), new Vector2(StripW, StripH));
            UI.Panel(body, UI.Alpha(tint, 0.22f), true, StripH / 2).raycastTarget = false;
            UI.Outline(UI.Stretch("Edge", body), UI.Alpha(tint, 0.9f), StripH / 2, 3f);
            strip.area = body;
            var ionTint = new Color(0.7f, 0.72f, 0.66f, 0.9f);
            for (int i = 0; i < ions; i++)
            {
                float ix = 30 + i * (StripW - 60) / (ions - 1), iy = StripH / 2 + (i % 2 == 0 ? -12 : 12);
                var ion = UI.Rect("Ion", body, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(ix, -iy), new Vector2(16, 16)).gameObject.AddComponent<Image>();
                ion.sprite = UI.Circle();
                ion.color = ionTint;
                ion.raycastTarget = false;
            }
            strip.dots = new Image[Dots];
            strip.x = new float[Dots];
            strip.y = new float[Dots];
            strip.vy = new float[Dots];
            var charge = UI.Theme ? UI.Theme.charge : Color.cyan;
            for (int i = 0; i < Dots; i++)
            {
                strip.x[i] = (float)random.NextDouble() * StripW;
                strip.y[i] = StripH / 2 + ((float)random.NextDouble() - 0.5f) * (StripH - 24);
                var dot = UI.Rect("Electron", body, new Vector2(0, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14)).gameObject.AddComponent<Image>();
                dot.sprite = UI.Circle();
                dot.color = charge;
                dot.raycastTarget = false;
                strip.dots[i] = dot;
            }
            Draw.Arrow(area, new Vector2(StripX + 8, y + StripH / 2 + 16), new Vector2(StripX + 60, y + StripH / 2 + 16), 3, 10, UI.Alpha(charge, 0.8f));
            Draw.Label(area, "electron drift", new Vector2(StripX + 130, y + StripH / 2 + 17), 21, UI.Alpha(charge, 0.8f), new Vector2(170, 26));
            // The current bar: how much gets through per second, as a share of the copper strip's.
            var track = UI.Rect(name + " Bar", area, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(StripX + StripW + 24, -y - 14), new Vector2(150, 16)); // below the verdict (y is down in this canvas)
            UI.Panel(track, new Color(1, 1, 1, 0.15f), true, 9f).raycastTarget = false;
            var fill = UI.Rect("Fill", track, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(150 * share, 16));
            strip.bar = UI.Panel(fill, UI.Theme ? UI.Theme.current : Color.yellow, true, 9f);
            strip.bar.raycastTarget = false;
            strip.amps = Draw.Label(area, verdict, new Vector2(StripX + StripW + 99, y - 12), 25, UI.Theme ? UI.Theme.current : Color.yellow, new Vector2(180, 30));
            strip.amps.fontStyle = FontStyles.Bold;
            return strip;
        }

        void Update()
        {
            float dt = ModuleFlow.UserPaused ? 0f : Time.deltaTime;
            clock += dt;
            Step(copper, dt);
            Step(nichrome, dt);
        }

        void Step(Strip strip, float dt)
        {
            bool calm = ReducedMotion.On;
            for (int i = 0; i < Dots; i++)
            {
                // Drift right; the random walk is stronger where the scattering is, and it costs headway.
                float jitter = strip.scatter * (calm ? 40f : 90f);
                if ((float)random.NextDouble() < dt * 6f * strip.scatter) strip.vy[i] = ((float)random.NextDouble() - 0.5f) * 2f * jitter;
                strip.y[i] += strip.vy[i] * dt;
                strip.vy[i] *= Mathf.Exp(-3f * dt);
                if (strip.y[i] < 12) { strip.y[i] = 12; strip.vy[i] = Mathf.Abs(strip.vy[i]); }
                if (strip.y[i] > StripH - 12) { strip.y[i] = StripH - 12; strip.vy[i] = -Mathf.Abs(strip.vy[i]); }
                float wobble = Mathf.Sin(clock * 7f + i * 1.3f) * strip.scatter * 18f;
                strip.x[i] += (strip.speed + wobble) * dt;
                if (strip.x[i] > StripW) strip.x[i] -= StripW;
                strip.dots[i].rectTransform.anchoredPosition = new Vector2(strip.x[i], -strip.y[i]);
            }
        }
    }

    // ------------------------------------------------------------------ build the law (Scene 13)

    // Three tiles, R · V · I (shuffled on screen), and the law building above them: the learner taps V, then I, then R and
    // "V = I × R" fills in, each symbol in its colour. A wrong tile says which one comes next; nothing is marked wrong.
    public class LawTiles : MonoBehaviour
    {
        static readonly string[] Sequence = { "V", "I", "R" };
        static readonly string[] Next = { "Start with V, the push.", "Next comes I, the flow.", "Last: R, the opposition." };

        readonly System.Collections.Generic.Dictionary<string, Button> tiles = new System.Collections.Generic.Dictionary<string, Button>();
        TextMeshProUGUI line, hint;
        int placed;

        public bool Complete => placed >= Sequence.Length;

        public static LawTiles Create()
        {
            var lesson = LessonPanel.Instance;
            var block = lesson.Fresh("MCQ", null, 12f);
            var view = block.gameObject.AddComponent<LawTiles>();
            LessonPanel.Eyebrow(block, "Build the law");
            view.line = LessonPanel.Text(block, "", 56f, LessonPanel.Paper, TextAlignmentOptions.Center);
            LessonPanel.OneLine(view.line, 56f);
            var row = LessonPanel.Row("Tiles", block, 10f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            LessonPanel.Fixed(row, 88f);
            foreach (var symbol in new[] { "R", "V", "I" })
            {
                string s = symbol;
                var color = s == "V" ? LessonPanel.VoltColor : s == "I" ? LessonPanel.AmpColor : LessonPanel.OhmColor;
                var tile = LessonPanel.Pill(s, row, s, () => view.Tap(s), LessonPanel.Kind.Raised, null, 88f, 48f);
                tile.GetComponent<LayoutElement>().flexibleWidth = 1f;
                tile.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                tile.GetComponentInChildren<TextMeshProUGUI>().color = color;
                view.tiles[s] = tile;
            }
            view.hint = LessonPanel.Text(block, Next[0], 17f, LessonPanel.Spark);
            view.Refresh();
            lesson.Show("MCQ", true);
            return view;
        }

        public void Tap(string symbol)
        {
            if (Complete) return;
            if (symbol == Sequence[placed])
            {
                tiles[symbol].interactable = false;
                placed++;
                UiSounds.Play("done");
                hint.text = Complete ? "That's the law." : Next[placed];
                LessonPanel.Pop(line.rectTransform);
            }
            else
            {
                hint.text = Next[placed];
                UiSounds.Play("hint");
            }
            Refresh();
        }

        // The next correct tile, for autoAnswer and the Enter key.
        public void Advance() { if (!Complete) Tap(Sequence[placed]); }

        void Refresh()
        {
            string Coloured(string s, Color c) => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{s}</color>";
            string v = Coloured("V", LessonPanel.VoltColor), i = Coloured("I", LessonPanel.AmpColor), r = Coloured("R", LessonPanel.OhmColor);
            const string dim = "<alpha=#38>";
            line.text = placed switch
            {
                0 => dim + "V = I × R",
                1 => v + dim + " = I × R",
                2 => v + " = " + i + dim + " × R",
                _ => v + " = " + i + " × " + r,
            };
        }
    }
}
