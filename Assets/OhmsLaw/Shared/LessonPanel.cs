using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace OhmsLaw
{
    // The module's one HUD, built once by ModuleFlow and kept across scenes. Cinematic first: the 3D scene owns the
    // screen, and the interface sits around it in four places:
    //   UI_TopControls      top-right: PAUSE · REPLAY · CC · VOLUME · SKIP (icons + text, one compact bar)
    //   UI_SubtitlePanel    bottom: the speaker and the line being spoken (SubtitleUI fills it; CC hides it)
    //   UI_AskWattson       bottom-right: Wattson's round avatar, the shortcut to his conversation
    //   UI_Dock             right, under the controls: compact cards, only while a beat needs them (the current
    //                       instruction, a note, the prediction, readings, the counter, the schematic, the inset,
    //                       the table and graph, formulas, the end buttons); never a full-height panel
    // Sizes are 1920x1080 reference px (the canvas scales with the screen, match 0.5); anchors keep every piece in
    // its corner at any resolution. The dock reserves a strip only in scenes that declare one (SceneDirector.dockWidth),
    // and PanelFraming on the camera then composes the shot into the rest of the frame.
    public class LessonPanel : MonoBehaviour
    {
        public static LessonPanel Instance { get; private set; }

        // Layout (reference px).
        public const float NormalWidth = 480f, WideWidth = 580f, Margin = 32f, TopInset = 28f, BottomInset = 32f;
        public const float Pad = 16f, Gap = 10f, Corner = 16f;
        public const float ControlHeight = 46f, ButtonHeight = 56f, DockTop = TopInset + ControlHeight + 14f, DockBottom = 196f;
        public const float CaptionLeft = 120f, CaptionRight = 300f, AvatarSize = 92f;
        // Type scale.
        public const float ControlFont = 18f, InstructionFont = 22f, HeadingFont = 15f, CaptionFont = 32f, SpeakerFont = 20f;
        public const float InfoTitleFont = 20f, InfoFont = 19f, FormulaFont = 34f, RevealFont = 64f, RevealSubFont = 22f;

        // Block order in the dock (top to bottom). Names are what callers ask Block() for.
        public static readonly string[] Order = { "Instruction", "Info", "Volt", "Readings", "Counter", "Schematic", "Inset", "Table", "Graph", "Formula", "MCQ", "Spacer", "End" };
        static readonly HashSet<string> Bare = new HashSet<string> { "Table", "Graph", "Spacer" }; // draw their own frame

        public RectTransform Root { get; private set; }
        public RectTransform Dock { get; private set; }
        public RectTransform Column { get; private set; }
        public RectTransform Conversation { get; private set; }   // Ask Wattson builds its conversation here; shown while it is open
        public RectTransform Caption { get; private set; }        // SubtitleUI builds the speaker and text labels here
        public RectTransform AskRoot { get; private set; }        // avatar + label (the spotlight scales this)
        public Button AskButton { get; private set; }
        public bool Wide { get; private set; }

        // Colours of the design system.
        public static Color CardFill => new Color(0.03f, 0.05f, 0.09f, 0.82f);
        public static Color CardLine => UI.Alpha(AccentColor, 0.22f);
        public static Color AccentColor => UI.AccentColor;
        public static Color NoteColor => new Color32(0xFF, 0xB8, 0x30, 255);
        public static Color Muted => new Color(0.66f, 0.74f, 0.86f, 1f);
        static Color Dim => new Color(1, 1, 1, 0.62f);

        // Reference px the dock covers from the right edge (its width plus the margin) in a scene that reserves it.
        public float Cover => dockReserved > 0f || Wide ? shownWidth + Margin : 0f;
        // Fraction of the actual screen the dock covers right now (PanelFraming reads it every frame).
        public float ScreenShare
        {
            get
            {
                var canvas = GetComponentInParent<Canvas>();
                float scale = canvas ? canvas.scaleFactor : 1f;
                return Screen.width > 0 ? Mathf.Clamp01(Cover * scale / Screen.width) : 0f;
            }
        }

        readonly Dictionary<string, RectTransform> blocks = new Dictionary<string, RectTransform>();
        RectTransform instruction, info, formula, popup, catcher, pulseRing;
        TextMeshProUGUI heading, instructionText, infoTitle, infoBody, askLabel, pauseLabel, ccLabel, motionLabel;
        Image pauseIcon, ring, glow, avatarDisc;
        HudButton pauseButton, replayButton, ccButton, volumeButton, skipButton, askHud;
        Image[] dots;
        float width = NormalWidth, shownWidth = NormalWidth, dockReserved, pulseT;
        string instructionShown = "", infoKey = "";

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
            BuildControls();
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
        }

        // A rounded card: translucent navy fill with a hairline accent outline.
        public static void Card(RectTransform rect, float alpha = -1f, float corner = Corner)
        {
            UI.Panel(rect, alpha < 0 ? CardFill : UI.Alpha(CardFill, alpha), true, corner).raycastTarget = true;
            var edge = UI.Stretch("Edge", rect);
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UI.Outline(edge, CardLine, corner, 1.5f);
        }

        void BuildDock()
        {
            Dock = UI.Rect("UI_Dock", Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -DockTop), new Vector2(NormalWidth, 0));
            Dock.anchorMin = new Vector2(1, 0);
            Dock.offsetMin = new Vector2(-Margin - NormalWidth, DockBottom);
            Dock.offsetMax = new Vector2(-Margin, -DockTop);
            Column = Stack("Column", Dock, Gap);
            Column.anchorMin = Vector2.zero;
            Column.anchorMax = Vector2.one;
            Column.offsetMin = Column.offsetMax = Vector2.zero;
            var column = Column.GetComponent<VerticalLayoutGroup>();
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.UpperCenter;
            // The scene's heading over the cards (activity screens), a small muted caps label.
            var head = UI.Rect("Heading", Column, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 26));
            Fixed(head, 26f);
            UI.Panel(head, UI.Alpha(CardFill, 0.6f), true, 8f).raycastTarget = false; // a faint strip so the title reads over a bright wall
            heading = UI.Label(head, "", HeadingFont, Muted, TextAlignmentOptions.MidlineLeft);
            heading.rectTransform.offsetMin = new Vector2(12, 0);
            heading.characterSpacing = 5f;
            heading.fontStyle = FontStyles.Bold;
            head.gameObject.SetActive(false);
        }

        // A column of blocks (VerticalLayoutGroup); children report their preferred heights, so nested stacks size themselves.
        static RectTransform Stack(string name, Transform parent, float spacing, RectOffset padding = null)
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

        // A named card of the dock, created on first use at its place in Order (hidden until shown).
        public RectTransform Block(string name)
        {
            if (blocks.TryGetValue(name, out var block) && block) return block;
            block = UI.Rect(name, Column, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            // After the heading and after every block that comes before it in Order.
            int order = System.Array.IndexOf(Order, name);
            int index = 0;
            for (int i = 0; i < Column.childCount; i++)
            {
                var sibling = Column.GetChild(i);
                if (sibling == block) continue;
                int siblingOrder = System.Array.IndexOf(Order, sibling.name);
                if (siblingOrder < 0 || siblingOrder < order) index = i + 1;
            }
            block.SetSiblingIndex(index);
            var layout = block.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            if (!Bare.Contains(name))
            {
                layout.padding = new RectOffset((int)Pad, (int)Pad, 12, 12);
                Card(block);
            }
            block.gameObject.SetActive(false);
            blocks[name] = block;
            return block;
        }

        public void Show(string name, bool on)
        {
            var block = Block(name);
            if (block.gameObject.activeSelf == on) return;
            block.gameObject.SetActive(on);
            if (on) { Pop(block); FadeIn(block); }
        }

        // A card fades in over 0.25 s when it appears (instant under reduced motion); hiding is instant.
        public static void FadeIn(RectTransform rect)
        {
            if (!rect) return;
            if (!rect.TryGetComponent(out CardFade fade)) fade = rect.gameObject.AddComponent<CardFade>();
            fade.Restart();
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

        public bool Showing(string name) => blocks.TryGetValue(name, out var block) && block && block.gameObject.activeSelf;

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

        // Everything inside a block goes (the block itself and its card stay for the next use).
        public void Clear(string name)
        {
            var block = Block(name);
            for (int i = block.childCount - 1; i >= 0; i--)
            {
                var child = block.GetChild(i).gameObject;
                if (child.name == "Edge") continue;
                child.SetActive(false);
                Destroy(child);
            }
            Show(name, false);
        }

        // Width a view may use inside a card at the dock's normal or wide width.
        public static float Inner(bool wide) => (wide ? WideWidth : NormalWidth) - 2 * Pad;

        // ------------------------------------------------------------------ top controls

        void BuildControls()
        {
            var bar = UI.Rect("UI_TopControls", Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -TopInset), new Vector2(0, ControlHeight));
            var row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleRight;
            bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            pauseButton = Control(bar, "Pause", "pause", "PAUSE", TogglePause, out pauseLabel, out pauseIcon);
            replayButton = Control(bar, "Replay", "replay", "REPLAY", () =>
            {
                var flow = ModuleFlow.Instance;
                if (flow && !flow.IsPaused(ModuleFlow.PauseReason.Hold) && SubtitleTrack.LastLineStart >= 0) flow.JumpTo(SubtitleTrack.LastLineStart);
            }, out _, out _);
            ccButton = Control(bar, "CC", null, "CC ON", () => SubtitleUI.Enabled = !SubtitleUI.Enabled, out ccLabel, out _);
            volumeButton = Control(bar, "Volume", "volume", "VOLUME", TogglePopup, out _, out _);
            skipButton = Control(bar, "Skip", "skip", "SKIP", SkipSection, out _, out _);
            BuildPopup(bar);
        }

        // One control: icon (or the CC badge) and a caps label on a translucent navy pill with a hairline outline.
        static HudButton Control(Transform row, string name, string icon, string text, UnityAction onClick, out TextMeshProUGUI label, out Image iconImage)
        {
            var rect = UI.Rect("UI_ControlButton " + name, row, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(120, ControlHeight));
            Fixed(rect, ControlHeight);
            var fill = UI.Panel(rect, UI.Alpha(CardFill, 0.9f), true, 12f); // a little denser than the cards: bright scenery (a poster) must not read through the labels
            var edge = UI.Stretch("Edge", rect);
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; // frames the whole pill, not a slot in its row
            var outline = UI.Outline(edge, CardLine, 12f, 1.5f);
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { UiSounds.Play("click"); onClick(); });
            var hud = rect.gameObject.AddComponent<HudButton>();
            hud.fill = fill;
            hud.fillColor = UI.Alpha(CardFill, 0.9f);
            hud.outline = outline;
            var line = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            line.padding = new RectOffset(14, 16, 0, 0);
            line.spacing = 9f;
            line.childAlignment = TextAnchor.MiddleLeft;
            line.childControlWidth = line.childControlHeight = true;
            line.childForceExpandWidth = line.childForceExpandHeight = false;
            iconImage = null;
            if (icon != null)
            {
                var iconRect = UI.Rect("Icon", rect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(22, 22));
                var element = iconRect.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = element.minWidth = 22f;
                element.preferredHeight = element.minHeight = 22f;
                iconImage = iconRect.gameObject.AddComponent<Image>();
                iconImage.sprite = Icons.Get(icon);
                iconImage.color = Color.white;
                iconImage.raycastTarget = false;
            }
            else
            {
                // The CC badge: a small outlined box with CC inside.
                var badge = UI.Rect("Badge", rect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(30, 20));
                var element = badge.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = element.minWidth = 30f;
                element.preferredHeight = element.minHeight = 20f;
                UI.Outline(badge, Color.white, 5f, 1.6f);
                var cc = UI.Label(badge, "CC", 12f, Color.white);
                cc.fontStyle = FontStyles.Bold;
            }
            // The label is a direct child of the row, so its preferred text width sizes the button.
            label = UI.Label(rect, text, ControlFont, Color.white, TextAlignmentOptions.MidlineLeft);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 2f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return hud;
        }

        static void TogglePause()
        {
            var flow = ModuleFlow.Instance;
            if (flow) flow.SetPaused(ModuleFlow.PauseReason.User, !flow.IsPaused(ModuleFlow.PauseReason.User));
        }

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

        // ------------------------------------------------------------------ volume popup

        void BuildPopup(RectTransform bar)
        {
            catcher = UI.Stretch("Popup Catcher", Root);
            var catchImage = catcher.gameObject.AddComponent<Image>();
            catchImage.color = new Color(0f, 0f, 0f, 0.3f); // a faint scrim: the popup reads, a click anywhere on it closes the popup
            var tap = catcher.gameObject.AddComponent<EventTrigger>();
            On(tap, EventTriggerType.PointerClick, () => ShowPopup(false));
            catcher.gameObject.SetActive(false);

            popup = UI.Rect("UI_VolumePopup", Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -(TopInset + ControlHeight + 10f)), new Vector2(NormalWidth, 0));
            var stack = popup.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(18, 18, 14, 14);
            stack.spacing = 8f;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            popup.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Card(popup, 1f, 14f);
            foreach (var (name, kind) in new[] { ("Wattson", AudioBus.Kind.Voice), ("Effects", AudioBus.Kind.Sfx), ("Music", AudioBus.Kind.Music) })
                VolumeRow(popup, name, kind);
            var motion = UI.Button("Motion", popup, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 40), "", () => ReducedMotion.On = !ReducedMotion.On, 16f);
            motion.GetComponent<Image>().color = UI.Alpha(Color.white, 0.08f);
            Fixed((RectTransform)motion.transform, 40f);
            motionLabel = motion.GetComponentInChildren<TextMeshProUGUI>();
            motionLabel.fontStyle = FontStyles.Bold;
            motionLabel.characterSpacing = 2f;
            popup.gameObject.SetActive(false);
        }

        static void VolumeRow(Transform parent, string name, AudioBus.Kind kind)
        {
            var row = UI.Rect("Volume " + name, parent, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 34));
            Fixed(row, 34f);
            var label = UI.Label(UI.Rect("Name", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(100, 34)), name.ToUpperInvariant(), 15f, Muted, TextAlignmentOptions.MidlineLeft);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 2f;
            var rect = UI.Rect("Slider", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(104, 0), new Vector2(0, 34));
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(-104, 34);
            var slider = rect.gameObject.AddComponent<Slider>();
            var track = UI.Stretch("Track", rect);
            track.offsetMin = new Vector2(0, 14);
            track.offsetMax = new Vector2(0, -14);
            UI.Panel(track, new Color(1, 1, 1, 0.18f), true, 3f);
            var fill = UI.Stretch("Fill", track);
            UI.Panel(fill, AccentColor, true, 3f);
            var handleArea = UI.Stretch("Handle Area", rect);
            handleArea.offsetMin = new Vector2(11, 0);
            handleArea.offsetMax = new Vector2(-11, 0);
            var handle = UI.Rect("Handle", handleArea, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, -12)); // the Slider stretches the handle to the track's 34 px; -12 keeps a 22 px disc
            var knob = handle.gameObject.AddComponent<Image>();
            knob.sprite = UI.Circle();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.value = AudioBus.Get(kind);
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
            // Pivot first: changing it afterwards keeps anchoredPosition and shifts the bar sideways.
            Caption = UI.Rect("UI_SubtitlePanel", Root, new Vector2(0, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 100));
            Caption.anchorMax = new Vector2(1, 0);
            Caption.offsetMin = new Vector2(CaptionLeft, BottomInset);
            Caption.offsetMax = new Vector2(-CaptionRight, BottomInset + 100);
            Card(Caption, 0.7f);
        }

        void BuildAsk()
        {
            AskRoot = UI.Rect("UI_AskWattson", Root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-Margin - 8f, BottomInset - 6f), new Vector2(180, 156));
            var buttonRect = UI.Rect("Avatar", AskRoot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(AvatarSize + 16, AvatarSize + 16));
            AskButton = buttonRect.gameObject.AddComponent<Button>();
            AskButton.transition = Selectable.Transition.None;
            var hit = buttonRect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            askHud = buttonRect.gameObject.AddComponent<HudButton>();
            askHud.hoverScale = 1.05f;
            // Soft glow, the pulse ring (listening), the cyan ring, then the face in a circular mask.
            glow = UI.Rect("Glow", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (AvatarSize + 34)).gameObject.AddComponent<Image>();
            glow.sprite = UI.Circle();
            glow.color = UI.Alpha(AccentColor, 0.12f);
            glow.raycastTarget = false;
            pulseRing = UI.Rect("Pulse", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (AvatarSize + 6));
            var pulse = pulseRing.gameObject.AddComponent<Image>();
            pulse.sprite = UI.Circle(0.94f);
            pulse.color = Color.clear;
            pulse.raycastTarget = false;
            ring = UI.Rect("Ring", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (AvatarSize + 6)).gameObject.AddComponent<Image>();
            ring.sprite = UI.Circle(0.93f);
            ring.color = UI.Alpha(AccentColor, 0.85f);
            ring.raycastTarget = false;
            var disc = UI.Rect("Disc", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * AvatarSize);
            avatarDisc = disc.gameObject.AddComponent<Image>();
            avatarDisc.sprite = UI.Circle();
            avatarDisc.color = new Color(0.08f, 0.12f, 0.2f);
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
            var labelRect = UI.Rect("Label", AskRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(180, 44));
            askLabel = UI.Label(labelRect, "ASK\nWATTSON", 17f, Color.white, TextAlignmentOptions.Top);
            askLabel.fontStyle = FontStyles.Bold;
            askLabel.characterSpacing = 3f;
            askLabel.lineSpacing = -12f;
        }

        void BuildConversation()
        {
            Conversation = UI.Rect("UI_Conversation", Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -DockTop), new Vector2(WideWidth, 0));
            Conversation.anchorMin = new Vector2(1, 0);
            Conversation.offsetMin = new Vector2(-Margin - WideWidth, DockBottom);
            Conversation.offsetMax = new Vector2(-Margin, -DockTop);
            Card(Conversation, 0.9f);
            Conversation.gameObject.SetActive(false);
        }

        // Ask Wattson open: the conversation takes the dock's place; the avatar's label says how to come back.
        public void ShowConversation(bool open)
        {
            Dock.gameObject.SetActive(!open);
            Conversation.gameObject.SetActive(open);
            askLabel.text = open ? "CLOSE" : "ASK\nWATTSON";
            UiSounds.Play(open ? "open" : "close");
            if (open) Pop(Conversation);
        }

        // ------------------------------------------------------------------ instruction and info cards

        void BuildInstruction()
        {
            instruction = Block("Instruction");
            var layout = instruction.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 16, 12, 14);
            layout.spacing = 4f;
            var head = UI.Rect("Head", instruction, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 20));
            Fixed(head, 20f);
            var row = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var iconRect = UI.Rect("Icon", head, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(14, 14));
            var element = iconRect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = element.preferredHeight = element.minHeight = 14f;
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = Icons.Get("play");
            icon.color = AccentColor;
            icon.raycastTarget = false;
            var title = UI.Label(head, "YOUR TURN", HeadingFont, AccentColor, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 4f;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            instructionText = UI.Label(instruction, "", InstructionFont, Color.white, TextAlignmentOptions.TopLeft);
            instructionText.fontStyle = FontStyles.Bold;
        }

        void BuildInfo()
        {
            info = Block("Info");
            var layout = info.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 16, 11, 13);
            layout.spacing = 2f;
            infoTitle = UI.Label(info, "", InfoTitleFont, NoteColor, TextAlignmentOptions.TopLeft);
            infoTitle.fontStyle = FontStyles.Bold;
            infoBody = UI.Label(info, "", InfoFont, Color.white, TextAlignmentOptions.TopLeft);
        }

        // The one current instruction. Empty hides the card. The text stays until the next call.
        public void Instruction(string text)
        {
            text = text ?? "";
            if (text == instructionShown) return;
            instructionShown = text;
            instructionText.text = text;
            Show("Instruction", text.Length > 0);
        }

        public string CurrentInstruction => instructionShown;

        // A supporting note: a title and a line or two (what a callout used to say). One at a time; "" hides it.
        public void Info(string key, string noteTitle, string body)
        {
            infoKey = key ?? "";
            bool any = !string.IsNullOrEmpty(noteTitle) || !string.IsNullOrEmpty(body);
            infoTitle.text = noteTitle ?? "";
            infoBody.text = body ?? "";
            infoTitle.gameObject.SetActive(!string.IsNullOrEmpty(noteTitle));
            infoBody.gameObject.SetActive(!string.IsNullOrEmpty(body));
            Show("Info", any);
        }

        public void InfoClear(string key = null)
        {
            if (key != null && key != infoKey) return; // a later note replaced this one
            Info("", "", "");
        }

        public string CurrentInfo => Showing("Info") ? (infoTitle.text + " " + infoBody.text).Trim() : "";

        public void SetTitle(string text) { if (heading) heading.text = (text ?? "").ToUpperInvariant(); }

        // ------------------------------------------------------------------ formula reveals

        // Appends a line to the formula card (V ∝ I → V = kI → ...), each one popping in; the card shows itself.
        public TextMeshProUGUI Formula(string text, Color? color = null, float size = FormulaFont)
        {
            Show("Formula", true);
            var line = UI.Label(formula, text, size, color ?? Color.white, TextAlignmentOptions.Center);
            line.fontStyle = FontStyles.Bold;
            line.gameObject.AddComponent<FrameStamp>().frame = Time.frameCount; // a clear in the same frame keeps it (cue order at one Timeline time is not guaranteed)
            OneLine(line, size);
            Pop(line.rectTransform);
            return line;
        }

        // A hero reveal in the formula card: the big text (accent) over a small line.
        public void Reveal(string big, string small, Color? color = null)
        {
            Show("Formula", true);
            var hero = Formula(big, color ?? AccentColor, RevealFont);
            hero.characterSpacing = 6f;
            if (!string.IsNullOrEmpty(small))
            {
                var sub = UI.Label(formula, small, RevealSubFont, Dim, TextAlignmentOptions.Center);
                sub.gameObject.AddComponent<FrameStamp>().frame = Time.frameCount;
                OneLine(sub, RevealSubFont);
            }
        }

        // One line at a fixed height, shrinking its font to fit the width: a crowded column shrinks the data views
        // instead of squeezing text over text.
        public static void OneLine(TextMeshProUGUI label, float size)
        {
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(14f, size * 0.55f);
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

        // ------------------------------------------------------------------ end of the module

        void BuildEnd()
        {
            var end = Block("End");
            end.GetComponent<VerticalLayoutGroup>().spacing = 10f;
            var replay = Button("Replay", end, "REPLAY THE INVESTIGATION", () =>
            {
                var flow = ModuleFlow.Instance;
                if (flow && flow.scenes != null && flow.scenes.Length > 0) flow.JumpToScene(flow.scenes[0]);
            }, ButtonHeight, 20f);
            replay.GetComponent<Image>().color = UI.AccentFill;
            Button("Exit", end, "EXIT", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }, ButtonHeight, 20f);
        }

        public void End(bool on) => Show("End", on);

        // ------------------------------------------------------------------ data views

        ObservationTableUI table;
        GraphUI graph;
        public const float TableScale = 0.5f, GraphScale = 0.48f, MinViewShare = 0.7f;
        public bool HasViews => table || graph;

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
            var holder = UI.Rect("View", Block(block), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, canvas.y * scale));
            Fixed(holder, canvas.y * scale).minHeight = canvas.y * scale * MinViewShare; // a crowded column shrinks the views first, so no block ever overlaps another
            var rect = UI.Rect("Canvas", holder, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, canvas);
            rect.localScale = Vector3.one * scale;
            var fit = holder.gameObject.AddComponent<CanvasFit>();
            fit.canvas = rect; fit.size = canvas; fit.scale = scale;
            Show(block, true);
            return rect;
        }

        // A new scene: every card empties (the scene's cues rebuild what it needs), the dock narrows, and it reserves a
        // strip of the frame only when the scene asks for one (activity screens; 0 keeps the cinematic full-frame).
        public void ResetForScene(string sceneTitle, float dockWidth = 0f)
        {
            SetTitle(sceneTitle);
            foreach (var name in Order) if (name != "Spacer" && name != "Instruction" && name != "Info" && name != "Formula" && name != "End") Clear(name);
            table = null;
            graph = null;
            Instruction("");
            InfoClear();
            FormulaClear();
            End(false);
            Expand(false);
            dockReserved = dockWidth;
            ShowPopup(false);
        }

        // Wide during the table, graph and comparison explanations: the dock grows toward the centre, still docked right.
        public void Expand(bool wide)
        {
            Wide = wide;
            width = wide ? WideWidth : NormalWidth;
        }

        // A button at the dock's button height for the cards (options, replay, exit ...).
        public static Button Button(string name, Transform parent, string text, UnityAction onClick, float height = ButtonHeight, float font = 20f)
        {
            var button = UI.Button(name, parent, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, height), text, onClick, font);
            var fill = button.GetComponent<Image>();
            fill.color = UI.RaisedColor;
            var edge = UI.Stretch("Edge", button.transform);
            edge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var outline = UI.Outline(edge, UI.Alpha(AccentColor, 0.18f), 12f, 1.2f);
            var hud = button.gameObject.AddComponent<HudButton>();
            hud.fill = fill;
            hud.fillColor = UI.RaisedColor;
            hud.outline = outline;
            hud.outlineColor = UI.Alpha(AccentColor, 0.18f);
            button.onClick.AddListener(() => UiSounds.Play("click"));
            Fixed((RectTransform)button.transform, height);
            return button;
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
            bool held = flow && flow.IsPaused(ModuleFlow.PauseReason.Hold);
            pauseLabel.text = paused ? "RESUME" : "PAUSE";
            pauseIcon.sprite = Icons.Get(paused ? "play" : "pause");
            pauseButton.active = paused;
            ccLabel.text = SubtitleUI.Enabled ? "CC ON" : "CC OFF";
            ccButton.active = SubtitleUI.Enabled;
            volumeButton.active = popup.gameObject.activeSelf;
            motionLabel.text = ReducedMotion.On ? "MOTION: REDUCED" : "MOTION: FULL";
            bool canJump = flow && flow.Director && !held && !AskWattsonUI.IsOpen;
            var replay = replayButton.GetComponent<Button>();
            if (replay.interactable != canJump) replay.interactable = canJump;
            var skip = skipButton.GetComponent<Button>();
            if (skip.interactable != canJump) skip.interactable = canJump;

            // The dock's width eases between normal and wide; its heading shows over the cards of an activity screen.
            float target = width;
            shownWidth = ReducedMotion.On ? target : Mathf.Lerp(shownWidth, target, 1 - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            if (Mathf.Abs(shownWidth - target) < 0.5f) shownWidth = target;
            if (!Mathf.Approximately(Dock.offsetMin.x, -Margin - shownWidth))
            {
                Dock.offsetMin = new Vector2(-Margin - shownWidth, DockBottom);
                Conversation.offsetMin = new Vector2(-Margin - Mathf.Max(shownWidth, WideWidth), DockBottom);
            }
            bool anyCard = false;
            foreach (var block in blocks.Values) if (block && block.name != "Spacer" && block.gameObject.activeSelf) { anyCard = true; break; }
            bool showHeading = anyCard && dockReserved > 0f && heading.text.Length > 0;
            if (heading.transform.parent.gameObject.activeSelf != showHeading) heading.transform.parent.gameObject.SetActive(showHeading);

            AnimateAsk(flow);
        }

        // The avatar's states: idle (cyan ring, a slow breath while he can be asked), listening (an expanding ring),
        // thinking (three dots), speaking (the ring pulsing). Minimal under reduced motion.
        void AnimateAsk(ModuleFlow flow)
        {
            var agent = flow ? flow.Agent : null;
            var state = agent ? agent.Current : WattsonAgent.State.Idle;
            float t = Time.unscaledTime;
            bool calm = ReducedMotion.On;
            var accent = UI.Theme ? UI.Theme.wattsonAccent : Color.cyan;
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
                : state == WattsonAgent.State.Error ? 0.9f : 0.85f;
            var ringColor = state == WattsonAgent.State.Error ? NoteColor : accent;
            ring.color = UI.Alpha(ringColor, ringAlpha);

            bool thinking = state == WattsonAgent.State.Thinking;
            for (int i = 0; i < dots.Length; i++)
            {
                float a = thinking ? (calm ? 1f : 0.35f + 0.65f * Mathf.Max(0f, Mathf.Sin(t * 5f - i * 1.1f))) : 0f;
                dots[i].color = UI.Alpha(Color.white, a);
            }
        }
    }

    // Stamp of the frame a formula line was revealed in (see LessonPanel.FormulaClear).
    public class FrameStamp : MonoBehaviour { public int frame; }

    // Fade-in of a card that just appeared: alpha 0 -> 1 over 0.25 s of unscaled time (none under reduced motion).
    public class CardFade : MonoBehaviour
    {
        CanvasGroup group;
        float t = 1f;

        public void Restart()
        {
            if (!group && !TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
            t = ReducedMotion.On ? 1f : 0f;
            group.alpha = t;
        }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.25f);
            group.alpha = t;
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

    // Scale-in of a UI card: 0.96 -> 1 with a soft overshoot, unscaled time.
    public class UIPop : MonoBehaviour
    {
        float t = 1f;
        public void Restart() => t = 0f;

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.28f);
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
