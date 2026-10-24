using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OhmsLaw
{
    // The HUD's screens around the lesson (design v3): where you are (the chapter banner and its chip), the pause menu
    // (resume, chapters, settings, exit), the title screen, plus the canvas's minimum scale and the keyboard shortcuts.
    // Everything is built once by code on the persistent module canvas, like the rest of the HUD (LessonPanel.cs).

    // The ten chapters as learners see them (the scene files keep their 04-13 names): title and one line of purpose.
    public static class Chapters
    {
        static readonly (string title, string purpose)[] list =
        {
            ("Meet Wattson", "Ohm's study, 1826. Your Christmas lights raised a question."),
            ("Journey Inside the Wire", "Shrink down and watch what moves when current flows."),
            ("Your Prediction", "Make a call before the evidence comes in."),
            ("Back to Georg Ohm", "See how Ohm hunted for a pattern."),
            ("From Ohm's Lab to Our Lab", "His 1826 apparatus becomes today's bench."),
            ("Build the Circuit", "Place the meters and close the switch."),
            ("Become the Experimenter", "Raise the voltage step by step and record the current."),
            ("The Pattern", "Turn five readings into one straight line."),
            ("Discover Resistance", "Find the number that stays the same."),
            ("Ohm's Law", "Put the three pieces together."),
        };

        public static int Count => ModuleFlow.Instance && ModuleFlow.Instance.scenes != null && ModuleFlow.Instance.scenes.Length > 0 ? ModuleFlow.Instance.scenes.Length : list.Length;
        public static int Current => ModuleFlow.Instance ? ModuleFlow.Instance.ChapterIndex : -1;   // 0-based, -1 outside the module
        public static string Title(int i) => i >= 0 && i < list.Length ? list[i].title : "";
        public static string Purpose(int i) => i >= 0 && i < list.Length ? list[i].purpose : "";
    }

    // Generated gradients (no texture assets): a horizontal fade and a top-left corner fade, used as vignettes behind
    // text laid over the 3D scene.
    public static class Vignettes
    {
        static Sprite left, corner, shine;

        public static Sprite Left => left ? left : left = Make("Vignette Left", 256, 4, (u, v) => u < 0.36f ? Mathf.Lerp(0.9f, 0.66f, u / 0.36f) : Mathf.Lerp(0.66f, 0f, Mathf.Clamp01((u - 0.36f) / 0.30f)));
        public static Sprite Corner => corner ? corner : corner = Make("Vignette Corner", 128, 128, (u, v) => { float d = Mathf.Sqrt(u * u + (1 - v) * (1 - v)); return 0.8f * Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) * 1.6f; });
        // v5: the soft white highlight along a glass card's top edge (white, fading down).
        public static Sprite Shine => shine ? shine : shine = Make("Glass Shine", 4, 32, (u, v) => 0.14f * v * v, true);

        static Sprite Make(string name, int w, int h, System.Func<float, float, float> alpha, bool white = false)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };
            var pixels = new Color32[w * h];
            byte tone = white ? (byte)255 : (byte)0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = new Color32(white ? tone : (byte)4, white ? tone : (byte)8, white ? tone : (byte)16, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha((x + 0.5f) / w, (y + 0.5f) / h)) * 255f));
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    // A 10-step progress rail: done (spark), now (paper, longer: shape, not colour alone), ahead (faint).
    public class Rail : MonoBehaviour
    {
        Image[] steps;
        bool small;

        public static Rail Create(Transform parent, bool small)
        {
            var rect = LessonPanel.Row("Rail", parent, small ? 4f : 6f);
            var rail = rect.gameObject.AddComponent<Rail>();
            rail.small = small;
            rail.steps = new Image[Chapters.Count];
            for (int i = 0; i < rail.steps.Length; i++)
            {
                var step = UI.Rect("Step " + (i + 1), rect, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                step.gameObject.AddComponent<LayoutElement>();
                rail.steps[i] = UI.Panel(step, Color.white, true, 3f);
                rail.steps[i].raycastTarget = false;
            }
            return rail;
        }

        public void Set(int now)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                bool isNow = i == now;
                var element = steps[i].GetComponent<LayoutElement>();
                element.preferredWidth = element.minWidth = small ? (isNow ? 26f : 14f) : (isNow ? 44f : 26f);
                element.preferredHeight = element.minHeight = small ? 4f : 5f;
                steps[i].color = i < now ? LessonPanel.Accent : isNow ? LessonPanel.Paper : new Color(1f, 1f, 1f, 0.24f);
            }
        }
    }

    // Where you are: on entering a chapter its title card lands top-left over the first shot (kicker, title, purpose,
    // the rail), holds about 4 s, then gives way to a small chip ("6 / 10", the rail, a chapters icon) that opens the
    // pause menu. Nothing here blocks the scene: a tap anywhere sends the title card away at once.
    public class ChapterBanner : MonoBehaviour
    {
        public static ChapterBanner Instance { get; private set; }

        RectTransform card, chip;
        CanvasGroup cardGroup, chipGroup;
        TextMeshProUGUI kicker, title, purpose, chipNumber;
        Rail cardRail, chipRail;
        float shownAt = -100f;
        int chapter = -1;
        bool titled, big;   // the big title card in cinematic chapters; in activity chapters the chip carries the title (the bench shots put Wattson top-left)

        void Awake()
        {
            Instance = this;
            var root = (RectTransform)transform;

            card = UI.Stretch("UI_ChapterTitle", root);
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            cardGroup.blocksRaycasts = false;
            var vignette = UI.Rect("Vignette", card, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1150, 470)).gameObject.AddComponent<Image>();
            vignette.sprite = Vignettes.Corner;
            vignette.raycastTarget = false;
            var block = LessonPanel.Stack("Block", card, 10f, new RectOffset(28, 32, 24, 24));
            block.anchorMin = block.anchorMax = new Vector2(0, 1);
            block.pivot = new Vector2(0, 1);
            block.anchoredPosition = new Vector2(LessonPanel.Margin, -LessonPanel.TopInset);
            block.sizeDelta = new Vector2(700, 0);
            var fitter = block.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            LessonPanel.Card(block, 1f, 22f);   // solid: a board or a poster behind never mixes with the title (4 % glass let lit chalk through)
            LessonPanel.Brackets(block, true);   // a title-bearing card
            kicker = LessonPanel.Eyebrow(block, "", LessonPanel.Spark);
            title = UI.Display(LessonPanel.Text(block, "", 54f, LessonPanel.Paper));   // Fredoka: the chapter title
            title.textWrappingMode = TextWrappingModes.NoWrap;
            purpose = LessonPanel.Text(block, "", 23f, UI.Alpha(LessonPanel.Paper, 0.9f), TextAlignmentOptions.TopLeft, false);
            cardRail = Rail.Create(block, false);
            LessonPanel.Fixed((RectTransform)cardRail.transform, 22f);

            chip = UI.Rect("UI_ChapterChip", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(LessonPanel.Margin, -LessonPanel.TopInset), new Vector2(0, LessonPanel.ControlHeight));
            chipGroup = chip.gameObject.AddComponent<CanvasGroup>();
            var fill = UI.Panel(chip, UI.Alpha(LessonPanel.Glass, LessonPanel.BarAlpha), true, LessonPanel.ControlHeight / 2f);
            FigmaSkin.Apply(fill, "Panel");
            fill.raycastTarget = true;
            var edge = UI.Stretch("Edge", chip);
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            if (!FigmaSkin.Available) UI.Outline(edge, LessonPanel.CardLine, LessonPanel.ControlHeight / 2f, 1.5f);
            var row = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(20, 18, 0, 0);
            row.spacing = 14f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            chip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            LessonPanel.Icon(chip, "bolt", 18f, LessonPanel.Spark);   // v5: the electric motif
            chipNumber = LessonPanel.Text(chip, "", 18f, LessonPanel.Paper, TextAlignmentOptions.MidlineLeft);
            chipNumber.textWrappingMode = TextWrappingModes.NoWrap;
            chipRail = Rail.Create(chip, true);
            LessonPanel.Icon(chip, "list", 22f, LessonPanel.Muted);
            var button = chip.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { UiSounds.Play("click"); PauseMenu.Open(); });
            var hud = chip.gameObject.AddComponent<HudButton>();
            hud.fill = fill;
            if (FigmaSkin.Available) hud.figmaStyle = "Panel";
            hud.fillColor = UI.Alpha(LessonPanel.Glass, LessonPanel.BarAlpha);
            hud.activeFill = hud.fillColor;
            hud.corner = LessonPanel.ControlHeight / 2f;
            Enter(-1);
        }

        // The chip's right edge in the HUD root's px (the top readings strip keeps clear of it); 0 while it is hidden.
        public static float ChipRight => Instance && Instance.chip && Instance.chip.gameObject.activeSelf ? Instance.chip.anchoredPosition.x + Instance.chip.rect.width : 0f;

        // A chapter starts (index 0-based; -1 outside the module hides the banner).
        public void Enter(int index)
        {
            chapter = index;
            if (index < 0) { card.gameObject.SetActive(false); chip.gameObject.SetActive(false); return; }
            kicker.text = $"CHAPTER {index + 1} OF {Chapters.Count}";
            title.text = Chapters.Title(index);
            purpose.text = Chapters.Purpose(index);
            big = LessonPanel.Instance && LessonPanel.Instance.Cinematic;
            ChipText(!big);
            cardRail.Set(index);
            chipRail.Set(index);
            shownAt = Time.unscaledTime;
            card.gameObject.SetActive(true);
            chip.gameObject.SetActive(true);
            cardGroup.alpha = 0f;
            chipGroup.alpha = 0f;
        }

        void ChipText(bool withTitle)
        {
            titled = withTitle;
            chipNumber.text = $"{chapter + 1} <color=#{ColorUtility.ToHtmlStringRGB(LessonPanel.Muted)}>/ {Chapters.Count}</color>" + (withTitle ? "   " + Chapters.Title(chapter) : "");
        }

        // The finale: every step done.
        public void Complete() { if (chapter >= 0) { chipRail.Set(Chapters.Count); cardRail.Set(Chapters.Count); } }

        void Update()
        {
            if (chapter < 0) return;
            bool hidden = TitleScreen.IsOpen;
            float t = Time.unscaledTime - shownAt;
            if (t < 4.5f && ((t > 0.5f && HudKeys.Tapped) || AskWattsonUI.IsOpen)) shownAt = Time.unscaledTime - 4.5f; // a tap (or Ask Wattson opening) sends the title away
            float cardAlpha = t < 0.6f ? t / 0.6f : t < 4.1f ? 1f : Mathf.Clamp01(1f - (t - 4.1f) / 0.4f);
            if (ReducedMotion.On) cardAlpha = t < 4.1f ? 1f : 0f;
            cardGroup.alpha = hidden || !big ? 0f : cardAlpha;
            // After the title card has gone (they share the corner); an activity chapter's chip shows at once, titled for ~4.5 s.
            float chipAlpha = big ? Mathf.Clamp01((t - 4.5f) / 0.3f) : Mathf.Clamp01(t / 0.4f);
            // The top readings strip shares the row: the chip folds to its number as soon as the strip shows.
            if (titled && (t > 4.5f || (LessonPanel.Instance && LessonPanel.Instance.TopStripShown))) ChipText(false);
            chipGroup.alpha = hidden ? 0f : chipAlpha;
            chipGroup.blocksRaycasts = !hidden && chipAlpha > 0.5f;
        }
    }

    // A labelled on/off switch row (the pause menu's settings): the whole row is the target.
    public class SwitchRow : MonoBehaviour
    {
        public System.Func<bool> get;
        public System.Action<bool> set;
        Image track;
        RectTransform knob;
        Image knobImage;

        public static SwitchRow Create(Transform parent, string label, System.Func<bool> get, System.Action<bool> set)
        {
            var row = LessonPanel.Row(label, parent, 12f);
            LessonPanel.Fixed(row, 48f);
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var text = LessonPanel.Text(row, label, 20f, LessonPanel.Paper, TextAlignmentOptions.MidlineLeft);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var trackRect = UI.Rect("Switch", row, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 36));
            var element = trackRect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = 64f;
            element.preferredHeight = element.minHeight = 36f;
            var view = row.gameObject.AddComponent<SwitchRow>();
            view.get = get;
            view.set = set;
            view.track = UI.Panel(trackRect, Color.white, true, 18f);
            view.track.raycastTarget = false;
            view.knob = UI.Rect("Knob", trackRect, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18, 0), new Vector2(28, 28));
            view.knobImage = view.knob.gameObject.AddComponent<Image>();
            view.knobImage.sprite = UI.Circle();
            view.knobImage.raycastTarget = false;
            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { UiSounds.Play("click"); view.set(!view.get()); });
            var hud = row.gameObject.AddComponent<HudButton>();   // the focus ring; no wash or scale on a settings row
            hud.hoverScale = hud.pressScale = 1f;
            hud.corner = 12f;
            return view;
        }

        void Update()
        {
            bool on = get != null && get();
            track.color = on ? LessonPanel.Accent : new Color(1f, 1f, 1f, 0.24f);
            knobImage.color = on ? LessonPanel.Ink : LessonPanel.Paper;
            float x = on ? 46f : 18f;
            knob.anchoredPosition = new Vector2(ReducedMotion.On ? x : Mathf.Lerp(knob.anchoredPosition.x, x, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime)), 0f);
        }
    }

    // The pause menu: the scene dims, Resume comes first, any chapter reached is one tap away (later ones stay locked
    // until reached; all open after the finale), and the settings: captions, larger text, reduced motion, the three
    // volumes. Exit asks once more, inline. Opened by PAUSE, the chapter chip, Esc or Space; Resume, Esc or Space close it.
    public class PauseMenu : MonoBehaviour
    {
        static PauseMenu instance;
        public static bool IsOpen => instance && instance.gameObject.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        RectTransform card, list;
        TextMeshProUGUI eyebrow, title, purpose, unlock, exitLabel;
        float exitArmedUntil;

        public static void Toggle() { if (IsOpen) Close(); else Open(); }

        public static void Open()
        {
            var panel = LessonPanel.Instance;
            if (!panel || TitleScreen.IsOpen || IsOpen) return;
            if (!instance) instance = Build(panel.Root);
            instance.Refresh();
            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
            if (ModuleFlow.Instance) ModuleFlow.Instance.SetPaused(ModuleFlow.PauseReason.User, true);
            if (panel.PopupOpen) panel.ClosePopup();
            UiSounds.Play("open");
            LessonPanel.Pop(instance.card);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            instance.gameObject.SetActive(false);
            if (ModuleFlow.Instance) ModuleFlow.Instance.SetPaused(ModuleFlow.PauseReason.User, false);
            UiSounds.Play("close");
        }

        static PauseMenu Build(RectTransform root)
        {
            var screen = UI.Stretch("UI_PauseMenu", root);
            var scrim = screen.gameObject.AddComponent<Image>();
            scrim.color = new Color(0.016f, 0.027f, 0.055f, 0.66f);
            var tap = screen.gameObject.AddComponent<EventTrigger>();
            LessonPanel.On(tap, EventTriggerType.PointerClick, Close);   // a tap outside the card resumes
            var menu = screen.gameObject.AddComponent<PauseMenu>();

            menu.card = UI.Rect("Card", screen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1140, 0));
            LessonPanel.Card(menu.card);
            menu.card.gameObject.AddComponent<EventTrigger>(); // clicks on the card stop here (no resume)
            var row = menu.card.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(40, 40, 36, 36);
            row.spacing = 44f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.UpperLeft;
            menu.card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Left: where you are, Resume, the chapters.
            var left = LessonPanel.Stack("Left", menu.card, 12f);
            Width(left, 656f);
            menu.eyebrow = LessonPanel.Eyebrow(left, "");
            menu.title = UI.Display(LessonPanel.Text(left, "", 42f, LessonPanel.Paper));   // Fredoka: the chapter title
            menu.purpose = LessonPanel.Text(left, "", 20f, LessonPanel.Muted, TextAlignmentOptions.TopLeft, false);
            var actions = LessonPanel.Row("Actions", left, 10f);
            LessonPanel.Fixed(actions, LessonPanel.ButtonHeight);
            LessonPanel.Pill("Resume", actions, "Resume", Close, LessonPanel.Kind.Primary, "play");
            LessonPanel.Pill("Restart", actions, "Restart chapter", () => { var f = ModuleFlow.Instance; int i = Chapters.Current; Close(); if (f && i >= 0) f.JumpToScene(f.scenes[i]); }, LessonPanel.Kind.Secondary, "replay");
            menu.list = UI.Rect("Chapters", left, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            var grid = menu.list.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(324, 56);
            grid.spacing = new Vector2(8, 8);
            grid.startAxis = GridLayoutGroup.Axis.Vertical;   // 1-5 down the first column, 6-10 down the second
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = Mathf.CeilToInt(Chapters.Count / 2f);
            LessonPanel.Fixed(menu.list, grid.constraintCount * 64f - 8f);
            menu.unlock = LessonPanel.Text(left, "Chapters unlock as you reach them. After the finale, all ten are open.", 17f, LessonPanel.Muted, TextAlignmentOptions.TopLeft, false);
            var keys = LessonPanel.Row("Keys", left, 18f);
            LessonPanel.Fixed(keys, 32f);
            foreach (var (key, does) in new[] { ("Space", "pause"), ("Esc", "menu"), ("R", "replay line"), ("C", "captions"), ("Enter", "do the step") })
            {
                var pair = LessonPanel.Row(key, keys, 6f);
                var cap = LessonPanel.Row("Key", pair, 0f, TextAnchor.MiddleCenter);
                cap.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(8, 8, 4, 4);
                var capEdge = UI.Stretch("Edge", cap);
                capEdge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                UI.Outline(capEdge, LessonPanel.ControlLine, 7f, 1.5f);
                LessonPanel.Text(cap, key, 15f, LessonPanel.Paper, TextAlignmentOptions.Center).textWrappingMode = TextWrappingModes.NoWrap;
                LessonPanel.Text(pair, does, 16f, LessonPanel.Muted, TextAlignmentOptions.MidlineLeft, false).textWrappingMode = TextWrappingModes.NoWrap;
            }

            // Right: settings, then Exit.
            var right = LessonPanel.Stack("Settings", menu.card, 14f);
            Width(right, 360f);
            LessonPanel.Eyebrow(right, "Settings");
            SwitchRow.Create(right, "Captions", () => SubtitleUI.Enabled, on => SubtitleUI.Enabled = on);
            SwitchRow.Create(right, "Larger text", () => HudScale.Larger, on => HudScale.Larger = on);
            SwitchRow.Create(right, "Reduce motion", () => ReducedMotion.On, on => ReducedMotion.On = on);
            foreach (var (name, kind) in new[] { ("Wattson", AudioBus.Kind.Voice), ("Effects", AudioBus.Kind.Sfx), ("Music", AudioBus.Kind.Music) })
                LessonPanel.VolumeRow(right, name, kind);
            var exit = LessonPanel.Pill("Exit", right, "Exit the module", null, LessonPanel.Kind.Ghost, "exit");
            menu.exitLabel = exit.GetComponentInChildren<TextMeshProUGUI>();
            exit.onClick.AddListener(menu.ExitPressed);
            screen.gameObject.SetActive(false);
            return menu;
        }

        static void Width(RectTransform rect, float width)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = width;
        }

        void ExitPressed()
        {
            if (Time.unscaledTime < exitArmedUntil) { ModuleFlow.Exit(); return; }
            exitArmedUntil = Time.unscaledTime + 3f;   // leaving asks once more, inline
            exitLabel.text = "Tap again to leave";
        }

        void Update()
        {
            if (exitArmedUntil > 0f && Time.unscaledTime >= exitArmedUntil) { exitArmedUntil = 0f; exitLabel.text = "Exit the module"; }
        }

        void Refresh()
        {
            int now = Chapters.Current, reached = Mathf.Max(ModuleFlow.Reached, now + 1);
            eyebrow.text = now >= 0 ? $"PAUSED · CHAPTER {now + 1} OF {Chapters.Count}" : "PAUSED";
            title.text = now >= 0 ? Chapters.Title(now) : "Paused";
            purpose.text = now >= 0 ? Chapters.Purpose(now) : "";
            foreach (Transform child in list) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            for (int i = 0; i < Chapters.Count; i++)
            {
                int index = i;
                bool done = i < now || (now < 0 && i < reached), isNow = i == now, open = i < reached || isNow;
                var button = LessonPanel.Pill("Chapter " + (i + 1), list, "", () =>
                {
                    var flow = ModuleFlow.Instance;
                    Close();
                    if (flow && index != Chapters.Current) flow.ReplayChapter(index);
                }, LessonPanel.Kind.Raised, null, 56f, 18f);
                var rect = (RectTransform)button.transform;
                // Badge (number), title (wraps to two lines, never shortened), state icon.
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                label.text = Chapters.Title(i);
                label.textWrappingMode = TextWrappingModes.Normal;
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                var badge = UI.Rect("Badge", rect, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34));
                badge.SetAsFirstSibling();
                var badgeSize = badge.gameObject.AddComponent<LayoutElement>();
                badgeSize.preferredWidth = badgeSize.minWidth = badgeSize.preferredHeight = badgeSize.minHeight = 34f;
                var disc = badge.gameObject.AddComponent<Image>();
                disc.sprite = UI.Circle();
                disc.color = done ? LessonPanel.Spark : new Color(1f, 1f, 1f, 0.1f);
                disc.raycastTarget = false;
                var number = UI.Label(badge, (i + 1).ToString(), 15f, done ? LessonPanel.Ink : LessonPanel.Paper);
                number.fontStyle = FontStyles.Bold;
                LessonPanel.Icon(rect, isNow ? "play" : open ? "replay" : "lock", 18f, isNow ? LessonPanel.Paper : LessonPanel.Muted);
                var hud = button.GetComponent<HudButton>();
                if (isNow) { hud.fillColor = LessonPanel.HoverFill; hud.outlineColor = hud.activeOutline = LessonPanel.Glow; }
                button.interactable = open;
                hud.disabledTap = () => { unlock.text = "Reach this chapter in the story first."; unlock.color = LessonPanel.Spark; };
            }
            unlock.text = "Chapters unlock as you reach them. After the finale, all ten are open.";
            unlock.color = LessonPanel.Muted;
        }
    }

    // The title screen, once per session over the first chapter's establishing shot: one clear action (Start the case),
    // Continue for a returning learner, captions and volume in the control bar. The Start tap is also what lets a
    // browser build play sound. The first chapter's Timeline holds on its first frame until then.
    public class TitleScreen : MonoBehaviour
    {
        static TitleScreen instance;
        static bool started;
        public static bool IsOpen => instance && instance.gameObject.activeSelf;

        // The editor enters play mode without a domain reload (Project Settings): statics would survive from the last run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { started = false; instance = null; }

        TextMeshProUGUI meta;
        GameObject continueButton;
        CanvasGroup fade;
        bool closing;
        WattsonController bannerWattson;
        UnityEngine.Splines.SplineContainer introPath;
        Transform introLook, bannerAnchor;
        Vector3 introPosition;
        Quaternion introRotation;
        Image curtain;
        int openedFrame, metaState = -1;
        RectTransform exportedBlock, exportedStart, exportedContinue;
        Image exportedBanner, exportedStartImage;
        Vector2 exportedCanvas;

        // A scene was entered: the title shows before the module's first chapter, once per session.
        public static void MaybeOpen(string sceneName)
        {
            var flow = ModuleFlow.Instance;
            var panel = LessonPanel.Instance;
            if (started || !flow || !panel || flow.scenes == null || flow.scenes.Length == 0 || flow.scenes[0] != sceneName) return;
            if (!instance) instance = Build(panel.Root);
            int reached = ModuleFlow.Reached;
            instance.continueButton.SetActive(reached > 1 && !ModuleFlow.Finished);   // a learner who left during the last chapter too
            instance.continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue · Chapter " + reached;
            instance.gameObject.SetActive(true);
            instance.closing = false;
            instance.fade.alpha = 0;
            instance.fade.interactable = true;
            instance.transform.Find("Block").gameObject.SetActive(true);
            instance.GetComponent<Image>().enabled = true;
            instance.curtain.color = Color.clear;
            instance.transform.SetAsFirstSibling();   // under the HUD: the control bar (CC, VOLUME) stays usable
            instance.openedFrame = Time.frameCount;
            flow.SetPaused(ModuleFlow.PauseReason.User, true);
            instance.StartCoroutine(instance.ComposeBanner());
        }

        System.Collections.IEnumerator ComposeBanner()
        {
            // Let the original camera settle at time zero before composing over its live laboratory.
            yield return null;
            yield return null;
            if (closing || !Camera.main) yield break;
            bannerWattson = FindFirstObjectByType<WattsonController>();
            if (!bannerWattson) yield break;
            introPath = bannerWattson.path;
            introLook = bannerWattson.lookTarget;
            introPosition = bannerWattson.transform.position;
            introRotation = bannerWattson.transform.rotation;
            bannerAnchor = new GameObject("Title presentation anchor").transform;
            bannerAnchor.position = Camera.main.ViewportToWorldPoint(new Vector3(0.77f, 0.40f, 2.5f));
            bannerWattson.Warp(bannerAnchor);
            bannerWattson.lookTarget = Camera.main.transform;
            var light = new GameObject("Banner warm fill").AddComponent<Light>();
            light.transform.SetParent(bannerAnchor, false);
            light.transform.localPosition = new Vector3(0, 0.5f, -0.5f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.55f);
            light.intensity = 0.35f;
            light.range = 1.8f;
            light.shadows = LightShadows.None;
        }

        void RestoreIntro()
        {
            if (bannerWattson && bannerAnchor)
            {
                bannerAnchor.position = introPosition;
                bannerWattson.Warp(bannerAnchor);
                bannerWattson.path = introPath;
                bannerWattson.lookTarget = introLook;
                bannerWattson.transform.rotation = introRotation;
            }
            if (bannerAnchor) Destroy(bannerAnchor.gameObject);
            bannerWattson = null;
        }

        void OnDisable() => RestoreIntro();

        // Exit in a browser (a page cannot close its tab): the title screen comes back over the first chapter.
        public static void Again()
        {
            var flow = ModuleFlow.Instance;
            if (!flow || flow.scenes == null || flow.scenes.Length == 0) return;
            started = false;
            flow.JumpToScene(flow.scenes[0]);
        }

        public static void StartCase()
        {
            if (!IsOpen || instance.closing) return;
            instance.StartCoroutine(instance.Begin(-1));
        }

        System.Collections.IEnumerator Begin(int chapter)
        {
            closing = true;
            fade.interactable = false;
            UiSounds.Play("start");   // UI_Confirm: the case begins (Start and Continue alike)
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.2f)
            {
                curtain.color = new Color(0.018f, 0.028f, 0.048f, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            curtain.color = new Color(0.018f, 0.028f, 0.048f, 1);
            RestoreIntro();
            transform.Find("Block").gameObject.SetActive(false);
            GetComponent<Image>().enabled = false;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.3f)
            {
                curtain.color = new Color(0.018f, 0.028f, 0.048f, 1 - Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            started = true;
            gameObject.SetActive(false);
            if (ModuleFlow.Instance) ModuleFlow.Instance.SetPaused(ModuleFlow.PauseReason.User, false);
            if (chapter >= 0) ModuleFlow.Instance.JumpToScene(ModuleFlow.Instance.scenes[chapter]);
            else if (ChapterBanner.Instance) ChapterBanner.Instance.Enter(0);
        }

        static void ContinueCase()
        {
            var flow = ModuleFlow.Instance;
            int reached = ModuleFlow.Reached;
            if (IsOpen && !instance.closing && flow && reached > 1 && reached <= flow.scenes.Length)
                instance.StartCoroutine(instance.Begin(reached - 1));
        }

        static TitleScreen Build(RectTransform root)
        {
            if (FigmaSkin.Available && FigmaSkin.Get("TitleBanner")) return BuildExported(root);
            var screen = UI.Stretch("UI_TitleScreen", root);
            var view = screen.gameObject.AddComponent<TitleScreen>();
            view.fade = screen.gameObject.AddComponent<CanvasGroup>();
            var shade = screen.gameObject.AddComponent<Image>();
            shade.sprite = Vignettes.Left;
            shade.color = Color.white;
            shade.raycastTarget = true;   // the scene underneath waits: nothing behind the title is clickable
            var block = LessonPanel.Stack("Block", screen, 22f);
            block.anchorMin = block.anchorMax = Vector2.zero;   // low on the left, over the floor: the opening shot keeps Ohm and his desk clear
            block.pivot = Vector2.zero;
            block.anchoredPosition = new Vector2(120, 96);
            block.sizeDelta = new Vector2(780, 0);
            block.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LessonPanel.Brackets(block, true, -14f);   // the title block: corner brackets just outside it
            LessonPanel.Eyebrow(block, "A Wattson investigation", LessonPanel.Spark, 18f).characterSpacing = 12f;
            var title = UI.Display(LessonPanel.Text(block, "OHM'S LAW", 110f, LessonPanel.Gold));   // Fredoka in gold, the reference's title
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.characterSpacing = -2f;
            LessonPanel.Fixed(title.rectTransform, 128f);
            // The bolt motif under the title: a thin electric-blue rule, the bolt, a fainter tail (the reference's pulse line).
            var motif = LessonPanel.Row("Motif", block, 8f);
            LessonPanel.Fixed(motif, 20f);
            Rule(motif, 150f, UI.Alpha(LessonPanel.Accent, 0.9f));
            LessonPanel.Icon(motif, "bolt", 18f, LessonPanel.Accent);
            Rule(motif, 90f, UI.Alpha(LessonPanel.Accent, 0.45f));
            var lead = LessonPanel.Text(block, "A Journey of Curiosity,\nCurrent and Discovery", 30f, UI.Alpha(LessonPanel.Paper, 0.96f), TextAlignmentOptions.TopLeft, false);
            lead.margin = new Vector4(0, 0, 120, 0);
            var actions = LessonPanel.Row("Actions", block, 14f);
            LessonPanel.Fixed(actions, 68f);
            LessonPanel.Pill("Start", actions, "START JOURNEY", StartCase, LessonPanel.Kind.Primary, "bolt", 68f, 25f);   // the one amber action
            LessonPanel.Text(block, "EXPLORE   ·   EXPERIMENT   ·   DISCOVER", 18f, LessonPanel.Spark);
            view.continueButton = LessonPanel.Pill("Continue", actions, "Continue", ContinueCase, LessonPanel.Kind.Secondary, null, 68f, 22f).gameObject;
            view.meta = LessonPanel.Text(block, "", 18f, LessonPanel.Muted, TextAlignmentOptions.TopLeft, false);
            view.curtain = UI.Stretch("Start transition", screen).gameObject.AddComponent<Image>();
            view.curtain.color = Color.clear;
            view.curtain.raycastTarget = false;
            screen.gameObject.SetActive(false);
            return view;
        }

        static TitleScreen BuildExported(RectTransform root)
        {
            var screen = UI.Stretch("UI_TitleScreen", root);
            var view = screen.gameObject.AddComponent<TitleScreen>();
            view.fade = screen.gameObject.AddComponent<CanvasGroup>();
            var shade = screen.gameObject.AddComponent<Image>();
            FigmaSkin.Apply(shade, "TitleShade", false);
            shade.raycastTarget = true;
            view.exportedBlock = UI.Rect("Block", screen, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(800, 504));
            view.exportedBanner = view.exportedBlock.gameObject.AddComponent<Image>();
            FigmaSkin.Apply(view.exportedBanner, "TitleBanner", false);
            view.exportedBanner.raycastTarget = false;
            view.exportedStart = UI.Rect("Start", view.exportedBlock, Vector2.up, Vector2.up, new Vector2(0, -326), new Vector2(336, 80));
            view.exportedStartImage = view.exportedStart.gameObject.AddComponent<Image>();
            FigmaSkin.Apply(view.exportedStartImage, "TitleStart", false);
            var start = view.exportedStart.gameObject.AddComponent<Button>();
            start.transition = Selectable.Transition.None;
            start.onClick.AddListener(StartCase);
            var feedback = view.exportedStart.gameObject.AddComponent<HudButton>();
            feedback.fill = view.exportedStartImage;
            feedback.figmaStyle = "Title";
            feedback.figmaState = "Start";
            var resume = LessonPanel.Pill("Continue", view.exportedBlock, "Continue", ContinueCase, LessonPanel.Kind.Secondary, null, 80, 20);
            view.continueButton = resume.gameObject;
            view.exportedContinue = (RectTransform)resume.transform;
            view.meta = UI.Label(UI.Rect("Metadata", view.exportedBlock, Vector2.up, Vector2.up, new Vector2(0, -480), new Vector2(800, 24)), "", 16, LessonPanel.Muted, TextAlignmentOptions.TopLeft);
            view.curtain = UI.Stretch("Start transition", screen).gameObject.AddComponent<Image>();
            view.curtain.color = Color.clear;
            view.curtain.raycastTarget = false;
            screen.gameObject.SetActive(false);
            return view;
        }

        void LayoutExported()
        {
            if (!exportedBlock) return;
            var canvas = ((RectTransform)transform).rect.size;
            if (canvas.x <= 0 || canvas.y <= 0 || canvas == exportedCanvas) return;
            exportedCanvas = canvas;
            bool tablet = canvas.x / canvas.y < 1.5f;
            var reference = tablet ? new Vector2(1024, 768) : new Vector2(1920, 1080);
            float scale = Mathf.Min(canvas.x / reference.x, canvas.y / reference.y);
            exportedBlock.sizeDelta = tablet ? new Vector2(560, 366) : new Vector2(800, 504);
            exportedBlock.localScale = Vector3.one * scale;
            exportedBlock.anchoredPosition = (tablet ? new Vector2(56, 106) : new Vector2(112, 126)) * scale;
            FigmaSkin.Apply(exportedBanner, tablet ? "TitleBannerTablet" : "TitleBanner", false);
            exportedStart.anchoredPosition = new Vector2(0, tablet ? -230 : -326);
            exportedStart.sizeDelta = tablet ? new Vector2(240, 60) : new Vector2(336, 80);
            FigmaSkin.Apply(exportedStartImage, tablet ? "TitleStartTablet" : "TitleStart", false);
            exportedStart.GetComponent<HudButton>().figmaState = tablet ? "StartTablet" : "Start";
            exportedContinue.anchorMin = exportedContinue.anchorMax = exportedContinue.pivot = Vector2.up;
            exportedContinue.anchoredPosition = new Vector2(tablet ? 256 : 352, tablet ? -230 : -326);
            exportedContinue.sizeDelta = tablet ? new Vector2(280, 60) : new Vector2(336, 80);
            exportedContinue.GetComponentInChildren<TextMeshProUGUI>().fontSize = tablet ? 16 : 20;
            meta.fontSize = tablet ? 13 : 16;
            meta.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, tablet ? -346 : -480);
            meta.rectTransform.parent.GetComponent<RectTransform>().sizeDelta = tablet ? new Vector2(560, 20) : new Vector2(800, 24);
        }

        // A 2 px rule of a fixed width in a row (the title's bolt motif).
        static void Rule(Transform row, float width, Color color)
        {
            var rect = UI.Rect("Rule", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(width, 2));
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = width;
            element.preferredHeight = element.minHeight = 2f;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        void Update()
        {
            LayoutExported();
            var flow = ModuleFlow.Instance;
            if (flow) flow.SetPaused(ModuleFlow.PauseReason.User, true);   // the Timeline may only have started this frame
            if (!closing) fade.alpha = Mathf.MoveTowards(fade.alpha, 1, Time.unscaledDeltaTime / 0.45f);
            int state = (SubtitleUI.Enabled ? 1 : 0) + (AudioBus.Get(AudioBus.Kind.Voice) > 0.01f ? 2 : 0);
            if (state != metaState)
            {
                metaState = state;
                meta.text = $"About 10 minutes  ·  Captions {((state & 1) != 0 ? "on" : "off")}  ·  Sound {((state & 2) != 0 ? "on" : "off")}";
            }
            if (ReviewMode.autoAnswer && Time.frameCount - openedFrame > 50) StartCase();   // hands-free review runs start by themselves (frames, not time: a scene load's hitch would skip the screen)
        }
    }

    // The canvas never shrinks below 75 % of the 1920 x 1080 design (school laptops at 1366 x 768 and 1280 x 720 get
    // a larger canvas instead of smaller text), and "Larger text" adds 15 %. CanvasScaler (scale with screen size,
    // match 0.5) gives sqrt(w/1920 * h/1080); scaling the reference resolution by natural / wanted sets the factor.
    public class HudScale : MonoBehaviour
    {
        public const float MinScale = 0.75f, LargerBy = 1.15f;
        const string Key = "ohm.largerText";
        static int larger = -1;   // read every frame: cached, PlayerPrefs touched only on a change

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => larger = -1;

        public static bool Larger
        {
            get
            {
                if (larger < 0) larger = PlayerPrefs.GetInt(Key, 0);
                return larger == 1;
            }
            set { larger = value ? 1 : 0; PlayerPrefs.SetInt(Key, larger); PlayerPrefs.Save(); }
        }

        CanvasScaler scaler;
        Vector2Int last;
        bool lastLarger;

        void Awake() => scaler = GetComponent<CanvasScaler>();

        void Update()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            bool larger = Larger;
            if (!scaler || screen.x <= 0 || screen.y <= 0 || (screen == last && larger == lastLarger)) return;
            last = screen;
            lastLarger = larger;
            float natural = Mathf.Sqrt(screen.x / 1920f * (screen.y / 1080f));
            float wanted = Mathf.Max(natural, MinScale) * (larger ? LargerBy : 1f);
            scaler.referenceResolution = new Vector2(1920f, 1080f) * (natural / wanted);
        }
    }

    // Keyboard and input for the HUD: Space pauses (or starts the case on the title), Esc opens / closes the menu (or
    // closes Ask Wattson and the volume popup first), R replays the line, C toggles captions, Enter acts on the current
    // turn (taps its target), Tab / Shift+Tab move a visible focus through the buttons in front (Enter then presses the
    // focused one instead). A focused text field keeps its keys; Ask Wattson keeps Space for push-to-talk.
    // AnyInput / Tapped are this frame's pointer, touch and key activity (the idle fold and the banner read them).
    [DefaultExecutionOrder(-50)]   // before the EventSystem: Enter is settled here before the UI module sees it
    public class HudKeys : MonoBehaviour
    {
        public static bool AnyInput { get; private set; }
        public static bool Tapped { get; private set; }
        static bool act;
        static GameObject focus;   // the button Tab moved to (a pointer tap ends keyboard focus)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { act = false; AnyInput = Tapped = false; focus = null; }

        // Enter during a gate: the gate treats it as the learner's action, once.
        public static bool ConsumeAct() { bool was = act; act = false; return was; }

        // The button shows the focus ring (HudButton).
        public static bool Focused(GameObject button) => focus == button && EventSystem.current && EventSystem.current.currentSelectedGameObject == button;

        void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            var touch = Touchscreen.current;
            Tapped = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (touch != null && touch.primaryTouch.press.wasPressedThisFrame);
            AnyInput = Tapped || (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.5f || mouse.scroll.ReadValue().sqrMagnitude > 0f))
                       || (keyboard != null && keyboard.anyKey.wasPressedThisFrame) || (touch != null && touch.primaryTouch.press.isPressed);
            if (Tapped) focus = null;
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (focus && selected && selected != focus) focus = selected;   // the arrow keys moved it on
            if (keyboard == null || Typing) return;
            var panel = LessonPanel.Instance;
            if (!panel) return;
            if (keyboard.tabKey.wasPressedThisFrame) Tab(panel, keyboard.shiftKey.isPressed);
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (panel.PopupOpen) panel.ClosePopup();
                else if (AskWattsonUI.IsOpen && AskWattsonUI.Instance) AskWattsonUI.Instance.Toggle();
                else if (!TitleScreen.IsOpen) PauseMenu.Toggle();
            }
            if (keyboard.spaceKey.wasPressedThisFrame && !AskWattsonUI.IsOpen)
            {
                if (TitleScreen.IsOpen) TitleScreen.StartCase(); else PauseMenu.Toggle();
            }
            if (keyboard.rKey.wasPressedThisFrame && !AskWattsonUI.IsOpen && !PauseMenu.IsOpen && !TitleScreen.IsOpen) panel.KeyReplay();
            if (keyboard.cKey.wasPressedThisFrame && !AskWattsonUI.IsOpen) SubtitleUI.Enabled = !SubtitleUI.Enabled;
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                // A button reached with Tab takes Enter (the UI module presses it). Any other selection is what a pointer
                // left behind: it is dropped, or the UI module would press that button too (PAUSE reopening the menu).
                var events = EventSystem.current;
                if (focus && focus.activeInHierarchy && events && events.currentSelectedGameObject == focus) return;
                if (events && events.currentSelectedGameObject) events.SetSelectedGameObject(null);
                if (TitleScreen.IsOpen) TitleScreen.StartCase();
                else if (PauseMenu.IsOpen) PauseMenu.Close();
                else if (SceneDirector.Gate != null && !AskWattsonUI.IsOpen) act = true;
            }
        }

        // The next (or previous) button of what is in front: the pause menu, the title screen (and the bar's CC and
        // VOLUME), Ask Wattson's conversation, else the whole HUD, in the order it was built.
        static void Tab(LessonPanel panel, bool back)
        {
            var events = EventSystem.current;
            if (!events) return;
            var buttons = new System.Collections.Generic.List<Selectable>();
            foreach (var button in panel.Root.GetComponentsInChildren<Selectable>())
            {
                var t = button.transform;
                bool front = PauseMenu.IsOpen ? button.GetComponentInParent<PauseMenu>() != null
                    : TitleScreen.IsOpen ? button.GetComponentInParent<TitleScreen>() != null || t.IsChildOf(panel.Controls)
                    : !AskWattsonUI.IsOpen || t.IsChildOf(panel.Conversation) || button == panel.AskButton;
                bool hidden = System.Array.Exists(button.GetComponentsInParent<CanvasGroup>(), g => g.alpha < 0.1f);   // a faded chip, a card stepped aside
                if (front && !hidden && button.IsInteractable()) buttons.Add(button);
            }
            if (buttons.Count == 0) return;
            int at = buttons.FindIndex(b => b.gameObject == events.currentSelectedGameObject);
            focus = buttons[at < 0 ? (back ? buttons.Count - 1 : 0) : (at + (back ? -1 : 1) + buttons.Count) % buttons.Count].gameObject;
            events.SetSelectedGameObject(focus);
        }

        static bool Typing
        {
            get
            {
                var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
                return selected && selected.TryGetComponent(out TMP_InputField field) && field.isFocused;
            }
        }
    }
}
