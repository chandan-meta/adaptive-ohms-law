using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    // ------------------------------------------------------------------ the battery slider

    // v5: the battery slider, 0-5 V in whole volts, in the HUD's bottom-centre strip (LessonPanel.VoltStrip); the class
    // keeps the name and fields of the five buttons it replaces, and the director wires it the same way. The ticks carry
    // the step states: done (a check, muted: its row is recorded), now (paper: the source is there), next (spark, pulsing:
    // the one to set), later (36 %: above the cap, or not the learner's turn; a tap says why). The track takes a tap, the
    // bolt knob a drag, the keyboard ← / →. Every request goes through `set` (whole volts, the cap: LearnerVoltage).
    public class VoltageButtons : MonoBehaviour
    {
        public System.Func<float, float> set;   // request -> applied
        public System.Func<bool> open;          // the learner may change the voltage now
        public System.Func<float> current, max;

        const int Steps = 5;
        const float RowHeight = 78f, Knob = 40f, TrackInset = 24f, TrackTop = 18f, TrackHeight = 10f;

        Slider slider;
        Image track;
        CanvasGroup knobGroup;
        TextMeshProUGUI value;
        readonly Image[] ticks = new Image[Steps + 1], checks = new Image[Steps + 1];
        readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[Steps + 1];
        float hintAt = -9f;
        bool live;   // open and not paused, this frame

        public static VoltageButtons Create()
        {
            var lesson = LessonPanel.Instance;
            var root = lesson.VoltStrip();
            var view = root.gameObject.AddComponent<VoltageButtons>();
            var head = LessonPanel.Row("Head", root, 10f);
            LessonPanel.Fixed(head, 24f);
            LessonPanel.Icon(head, "bolt", 18f, LessonPanel.Spark);
            LessonPanel.Eyebrow(head, "Set the battery", LessonPanel.Spark).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            view.value = LessonPanel.Text(head, "", 22f, LessonPanel.Paper, TextAlignmentOptions.MidlineRight);
            view.value.textWrappingMode = TextWrappingModes.NoWrap;

            // The slider row: the track and its fill, a tick and a label per volt, the bolt knob.
            var row = UI.Rect("Slider", root, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, RowHeight));
            LessonPanel.Fixed(row, RowHeight);
            row.gameObject.AddComponent<Image>().color = Color.clear;   // the row is the hit area: a tap on the track sets too
            view.slider = row.gameObject.AddComponent<Slider>();
            view.slider.transition = Selectable.Transition.None;
            view.slider.minValue = 0f;
            view.slider.maxValue = Steps;
            view.slider.wholeNumbers = true;
            var trackRect = UI.Rect("Track", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(TrackInset, -TrackTop), new Vector2(-2f * TrackInset, TrackHeight));
            trackRect.anchorMax = new Vector2(1, 1);
            view.track = UI.Panel(trackRect, new Color(1f, 1f, 1f, 0.16f), true, TrackHeight / 2f);
            FigmaSkin.Apply(view.track, "SliderTrack");
            view.track.raycastTarget = false;
            var fillRect = UI.Stretch("Fill", UI.Stretch("Fill Area", trackRect));
            var sliderFill = UI.Panel(fillRect, LessonPanel.VoltColor, true, TrackHeight / 2f);
            FigmaSkin.Apply(sliderFill, "SliderFill");
            sliderFill.raycastTarget = false;
            for (int i = 0; i <= Steps; i++)
            {
                float u = (float)i / Steps;
                var tick = UI.Rect("Tick " + i, trackRect, new Vector2(u, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
                view.ticks[i] = tick.gameObject.AddComponent<Image>();
                view.ticks[i].sprite = UI.Circle();
                view.ticks[i].raycastTarget = false;
                view.checks[i] = LessonPanel.Icon(tick, "check", 12f, LessonPanel.Ink);
                view.checks[i].rectTransform.anchorMin = view.checks[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                var label = UI.Rect("Label " + i, trackRect, new Vector2(u, 0), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(60, 22));
                view.labels[i] = UI.Label(label, i + " V", 16f, LessonPanel.Muted);
                view.labels[i].fontStyle = FontStyles.Bold;
                view.labels[i].textWrappingMode = TextWrappingModes.NoWrap;
            }
            var handleArea = UI.Stretch("Handle Area", row);
            handleArea.offsetMin = new Vector2(TrackInset, 0);
            handleArea.offsetMax = new Vector2(-TrackInset, 0);
            // The Slider stretches the handle across the row's height: Knob - RowHeight keeps a Knob px disc, centred on the track.
            var handle = UI.Rect("Knob", handleArea, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, RowHeight / 2f - TrackTop - TrackHeight / 2f), new Vector2(Knob, Knob - RowHeight));
            var knob = handle.gameObject.AddComponent<Image>();
            knob.sprite = UI.Circle();
            knob.color = LessonPanel.Paper;
            FigmaSkin.Apply(knob, "Icon-slider-thumb", false);
            var bolt = UI.Rect("Icon", handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22)).gameObject.AddComponent<Image>();
            bolt.sprite = Icons.Get("bolt");
            bolt.color = LessonPanel.Ink;
            bolt.raycastTarget = false;
            view.knobGroup = handle.gameObject.AddComponent<CanvasGroup>();
            view.slider.fillRect = fillRect;
            view.slider.handleRect = handle;
            view.slider.targetGraphic = knob;
            view.slider.onValueChanged.AddListener(view.Changed);
            // A tap while it is not the learner's turn says so (the slider itself is off then).
            var tap = root.gameObject.AddComponent<EventTrigger>();
            LessonPanel.On(tap, EventTriggerType.PointerClick, () => { if (!view.live) view.Hint("Wait for your turn."); });
            return view;
        }

        // The slider's rule (pure, for the HUD self-check): the whole-volt value to apply for a request, or NaN when
        // nothing changes, with the hint that says why not (not the learner's turn, or above the cap).
        public static float Rule(float request, float now, float cap, bool live, out string hint)
        {
            hint = null;
            if (!live) { hint = "Wait for your turn."; return float.NaN; }
            if (request > cap + 0.001f) { hint = "One step at a time."; return float.NaN; }
            float want = Mathf.Clamp(Mathf.Round(request), 0f, Steps);
            return Mathf.Approximately(want, now) ? float.NaN : want;
        }

        // A value from the knob, the track, the arrows.
        void Changed(float volts)
        {
            float now = current != null ? current() : 0f, cap = max != null ? max() : Steps;
            float want = Rule(volts, now, cap, live, out string hint);
            if (hint != null) { slider.SetValueWithoutNotify(Mathf.Min(now, cap)); Hint(hint); return; }
            if (!float.IsNaN(want) && set != null) set(want);
        }

        void Hint(string text)
        {
            if (Time.unscaledTime - hintAt < 1f) return;   // a drag past the cap asks every frame
            hintAt = Time.unscaledTime;
            if (LessonPanel.Instance) LessonPanel.Instance.ShowHint(text);
        }

        void Update()
        {
            float now = current != null ? current() : 0f, cap = max != null ? max() : Steps;
            live = open != null && open() && !ModuleFlow.UserPaused;
            if (slider.interactable != live) slider.interactable = live;
            if (!Mathf.Approximately(slider.value, now)) slider.SetValueWithoutNotify(now);
            // Whole volts read "2 V"; the key's 0.5 V reads "0.5 V" (rounded to "1 V" it sat over a knob at 0).
            value.text = now.ToString(Mathf.Approximately(now, Mathf.Round(now)) ? "0" : "0.0", CultureInfo.InvariantCulture) + " V";
            float pulse = ReducedMotion.On ? 1f : 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
            track.color = FigmaSkin.Available ? Color.white : new Color(1f, 1f, 1f, live ? 0.2f : 0.12f);
            knobGroup.alpha = live ? 1f : 0.7f;
            for (int i = 0; i <= Steps; i++)
            {
                bool selected = Mathf.Approximately(now, i), above = i > cap + 0.001f;
                bool recorded = i > 0 && !selected && LearnerSession.tableRows >= i;
                bool next = live && !selected && !above && Mathf.Approximately(i, cap);
                ticks[i].color = selected ? LessonPanel.Paper : next ? UI.Alpha(LessonPanel.Spark, pulse) : recorded ? UI.Alpha(LessonPanel.Spark, 0.85f) : new Color(1f, 1f, 1f, above ? 0.36f : 0.55f);
                labels[i].color = selected ? LessonPanel.Paper : next ? LessonPanel.Spark : above ? new Color(1f, 1f, 1f, 0.36f) : LessonPanel.Muted;
                if (checks[i].gameObject.activeSelf != recorded) checks[i].gameObject.SetActive(recorded);
            }
            // ← and → step the battery while it is the learner's turn (the Slider steps itself while it holds the selection).
            var keys = Keyboard.current;
            var events = EventSystem.current;
            if (keys == null || !live || AskWattsonUI.IsOpen || (events && events.currentSelectedGameObject == slider.gameObject)) return;
            if (keys.leftArrowKey.wasPressedThisFrame && now > 0.5f) Changed(Mathf.Round(now) - 1f);
            if (keys.rightArrowKey.wasPressedThisFrame && now < Steps - 0.5f) Changed(Mathf.Round(now) + 1f);
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
        const float Top = 120f, SourceX = 70f, AmmeterX = 185f, X = 300f, Y = 420f, KeyX = 540f, Ring = 48f;

        Image ammeterRing, voltmeterRing, voltLeadX, voltLeadY, voltTopX, voltTopY, lever;
        TextMeshProUGUI ammeterLabel, voltmeterLabel, note;
        TextMeshProUGUI[] ammeterSigns, voltmeterSigns;
        readonly System.Collections.Generic.List<Image> loop = new System.Collections.Generic.List<Image>();
        bool shownKey;
        float pulse;

        public static CircuitSchematic Create()
        {
            var lesson = LessonPanel.Instance;
            var block = lesson.Fresh("Schematic", new RectOffset(12, 12, 8, 8));
            float scale = (LessonPanel.Inner(false) - 24f) / Size.x;
            var area = Draw.Box(block, Size, scale);
            lesson.Show("Schematic", true);
            return CreateOn(area);
        }

        // v5: the same live schematic on any RectTransform (a world-space board's canvas, Scene 9): the drawing is laid
        // out in Size px from the area's top-left corner, so give the area that size and scale it to fit.
        public static CircuitSchematic CreateOn(RectTransform area)
        {
            var view = area.gameObject.AddComponent<CircuitSchematic>();
            view.Build(area);
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
            L(new Vector2(SourceX, wireY), new Vector2(AmmeterX - Ring / 2, wireY));
            // Ammeter in series on the top wire. Conventional current runs source + → ammeter → X → Y → key → source −, so
            // each meter's + terminal is the one toward the source's + side: the ammeter's left, the voltmeter's X side.
            ammeterRing = Draw.Disc(area, new Vector2(AmmeterX, wireY), Ring, dim, 0.78f);
            ammeterLabel = Draw.Label(area, "A", new Vector2(AmmeterX, wireY), 24, dim, new Vector2(40, 40));
            ammeterSigns = Signs(area, new Vector2(AmmeterX - 30, wireY - 22), new Vector2(AmmeterX + 30, wireY - 22), dim);
            L(new Vector2(AmmeterX + Ring / 2, wireY), new Vector2(X, wireY));
            // The nichrome wire between X and Y, thicker, in the alloy's tone.
            Draw.Bar(area, new Vector2(X, wireY), new Vector2(Y, wireY), 10, new Color(0.78f, 0.72f, 0.58f));
            Draw.Disc(area, new Vector2(X, wireY), 14, chalk);
            Draw.Disc(area, new Vector2(Y, wireY), 14, chalk);
            // X and Y under the wire's ends, just outside the voltmeter's leads (where the bench's badges sit); the name above.
            Draw.Label(area, "X", new Vector2(X - 22, wireY + 22), 26, chalk, new Vector2(30, 28));
            Draw.Label(area, "Y", new Vector2(Y + 22, wireY + 22), 26, chalk, new Vector2(30, 28));
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
            // Voltmeter in parallel across XY, under the wire (between the wire and the return). Its leads stop at the ring,
            // as the ammeter's do: a wire drawn straight through the V would read as the meter shorted out.
            float voltX = (X + Y) / 2;
            voltLeadX = Draw.Bar(area, new Vector2(X, wireY), new Vector2(X, Top), Thin, dim);
            voltLeadY = Draw.Bar(area, new Vector2(Y, wireY), new Vector2(Y, Top), Thin, dim);
            voltTopX = Draw.Bar(area, new Vector2(X, Top), new Vector2(voltX - Ring / 2, Top), Thin, dim);
            voltTopY = Draw.Bar(area, new Vector2(voltX + Ring / 2, Top), new Vector2(Y, Top), Thin, dim);
            voltmeterRing = Draw.Disc(area, new Vector2(voltX, Top), Ring, dim, 0.78f);
            voltmeterLabel = Draw.Label(area, "V", new Vector2(voltX, Top), 24, dim, new Vector2(40, 40));
            voltmeterSigns = Signs(area, new Vector2(voltX - 42, Top - 16), new Vector2(voltX + 42, Top - 16), dim);
            note = Draw.Label(area, "Conventional current is drawn + → − (electrons drift the other way)", new Vector2(300, 218), 21, UI.Alpha(amber, 0f), new Vector2(600, 28));
        }

        // A meter's terminal marks: + at `plus`, − at `minus`.
        static TextMeshProUGUI[] Signs(RectTransform area, Vector2 plus, Vector2 minus, Color color) => new[]
        {
            Draw.Label(area, "+", plus, 20, color, new Vector2(24, 24)),
            Draw.Label(area, UI.Glyph("−", "-"), minus, 20, color, new Vector2(24, 24)),
        };

        static void Tint(TextMeshProUGUI[] signs, Color color) { foreach (var sign in signs) sign.color = color; }

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
            Tint(ammeterSigns, ammeterRing.color);
            voltmeterRing.color = rig.voltmeterPlaced ? volt : voltmeterNext ? Color.Lerp(dim, volt, pulse) : dim;
            voltmeterLabel.color = voltmeterRing.color;
            Tint(voltmeterSigns, voltmeterRing.color);
            Color leads = rig.voltmeterPlaced ? volt : voltmeterNext ? Color.Lerp(dim, volt, pulse * 0.6f) : dim;
            voltLeadX.color = leads;
            voltLeadY.color = leads;
            voltTopX.color = voltTopY.color = leads;
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

    // ------------------------------------------------------------------ the row -> point link

    // A small glowing spark that travels from a table row to its point on the graph (the "link" cue, and every row Scene
    // 10 records): one reading, shown twice, visibly the same. A dot with a short fading trail on the HUD root, over
    // everything, along a shallow arc; `landed` runs when it arrives (the point pops in or lights then). When either end is
    // out of sight (a card stepped aside, a view not built) or under reduced motion it lands at once.
    public class LinkSpark : MonoBehaviour
    {
        public const float Seconds = 0.45f;
        const int Trail = 6;
        const float Head = 22f, HaloSize = 58f;

        RectTransform from, to, root, head, halo;
        Image[] trail;
        Vector2[] past;
        System.Action landed;
        float t;

        public static void Fly(RectTransform from, RectTransform to, System.Action landed)
        {
            var lesson = LessonPanel.Instance;
            // The target may be a point not plotted yet (Scene 10: it pops in as the spark lands): its plot must show.
            if (!lesson || ReducedMotion.On || !Visible(from) || !to || !Visible((RectTransform)to.parent)) { landed?.Invoke(); return; }
            var spark = UI.Rect("Link Spark", lesson.Root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<LinkSpark>();
            spark.transform.SetAsLastSibling();
            spark.from = from;
            spark.to = to;
            spark.root = lesson.Root;
            spark.landed = landed;
            spark.Build();
        }

        // Shown, and not a card that stepped aside (LessonPanel.Crowd: alpha 0 and out of the layout).
        static bool Visible(RectTransform rect)
        {
            if (!rect || !rect.gameObject.activeInHierarchy) return false;
            foreach (var group in rect.GetComponentsInParent<CanvasGroup>())
                if (group.alpha <= 0.01f && group.TryGetComponent(out LayoutElement element) && element.ignoreLayout) return false;
            return true;
        }

        void Build()
        {
            var colour = LessonPanel.Spark;
            trail = new Image[Trail];
            past = new Vector2[Trail];
            for (int i = Trail - 1; i >= 0; i--)
            {
                float k = 1f - (i + 1f) / (Trail + 1f);
                trail[i] = Dot("Trail", Head * (0.45f + 0.45f * k), UI.Alpha(colour, 0.45f * k));
            }
            halo = Dot("Halo", HaloSize, UI.Alpha(colour, 0.22f)).rectTransform;
            head = Dot("Head", Head, colour).rectTransform;
            var start = At(from, new Vector2(1f, 0.5f));
            for (int i = 0; i < Trail; i++) past[i] = start;
            Place(start);
        }

        Image Dot(string name, float size, Color colour)
        {
            var dot = UI.Rect(name, transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size).gameObject.AddComponent<Image>();
            dot.sprite = UI.Circle();
            dot.color = colour;
            dot.raycastTarget = false;
            return dot;
        }

        // A point of `rect` (pivot-style fraction of its rect) in the HUD root's local space, followed every frame (the
        // stage and the dock still ease while it flies).
        Vector2 At(RectTransform rect, Vector2 fraction)
        {
            var r = rect.rect;
            return root.InverseTransformPoint(rect.TransformPoint(r.min + Vector2.Scale(r.size, fraction)));
        }

        void Place(Vector2 at)
        {
            for (int i = Trail - 1; i > 0; i--) past[i] = past[i - 1];
            past[0] = at;
            head.localPosition = halo.localPosition = at;
            for (int i = 0; i < Trail; i++) trail[i].rectTransform.localPosition = past[i];
        }

        void Update()
        {
            if (!from || !to) { Land(); return; }
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Seconds);
            float e = t * t * (3f - 2f * t);
            Vector2 a = At(from, new Vector2(1f, 0.5f)), b = At(to, new Vector2(0.5f, 0.5f));
            // A shallow arc above the straight path: it reads as a hand-off, not a laser.
            Vector2 lift = new Vector2(-(b - a).y, (b - a).x).normalized * ((b - a).magnitude * 0.18f);
            if (lift.y < 0f) lift = -lift;
            Place(Vector2.Lerp(a, b, e) + lift * (4f * e * (1f - e)));
            halo.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
            if (t >= 1f) Land();
        }

        void Land()
        {
            var done = landed;
            landed = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
            done?.Invoke();
        }
    }

    // ------------------------------------------------------------------ build the law (Scene 13)

    // The BuildLaw gate's handle on the finale surface (LawReveal): the clues V, I and R sit there as tiles (shuffled to
    // R · V · I); the learner taps V, then I, then R and each flies into its place in V = I × R. A wrong tile says which
    // one comes next and shakes; nothing is marked wrong. Complete once the last tile has landed.
    public class LawTiles : MonoBehaviour
    {
        public bool Complete => !LawReveal.Instance || LawReveal.Instance.Built;

        public static LawTiles Create()
        {
            var reveal = LawReveal.Ensure();
            if (!reveal) return new GameObject("Law Tiles (no HUD)").AddComponent<LawTiles>();
            reveal.BeginBuild();
            return reveal.TryGetComponent(out LawTiles tiles) ? tiles : reveal.gameObject.AddComponent<LawTiles>();
        }

        public void Tap(string symbol) { if (LawReveal.Instance) LawReveal.Instance.Tap(symbol); }

        // The next correct tile, for autoAnswer and the Enter key.
        public void Advance() { if (LawReveal.Instance) LawReveal.Instance.Advance(); }
    }
}
