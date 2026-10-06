using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OhmsLaw
{
    // The one learning panel, docked at the right of the screen. The current instruction, Wattson's captions, the
    // supporting note, the playback controls, the voltage buttons, live readings, the counter, the schematic, the
    // table, the graph, the resistance inset, formula reveals and the Ask Wattson conversation all live here, and
    // nothing instructional sits anywhere else on the screen. Blocks are children of one column in a fixed order and
    // show only while they have something to say. Normally a third of the reference width; data views widen it toward
    // the centre (Expand). PanelFraming shifts the 3D camera so the action composes in the free part of the frame.
    // Built by code on the module canvas (ModuleFlow); the MonoBehaviours it adds live in this file by the project's
    // convention for runtime-only UI.
    public class LessonPanel : MonoBehaviour
    {
        public static LessonPanel Instance { get; private set; }

        public const float NormalWidth = 640f, WideWidth = 880f, Margin = 12f, Pad = 20f, Gap = 10f, Corner = 26f;
        public const float HeaderHeight = 72f, ControlHeight = 44f, ButtonHeight = 72f;
        public const float InstructionFont = 30f, CaptionFont = 28f, SpeakerFont = 18f, InfoTitleFont = 24f, InfoFont = 22f, ControlFont = 22f;
        public const float FormulaFont = 40f, RevealFont = 72f, RevealSubFont = 26f;
        const float SkipHold = 1f;

        // Block order in the column (top to bottom). Names are what callers ask Block() for.
        public static readonly string[] Order = { "Instruction", "Caption", "Info", "Volt", "Readings", "Counter", "Schematic", "Inset", "Table", "Graph", "Formula", "MCQ", "Spacer", "End" };

        public RectTransform Root { get; private set; }
        public RectTransform Column { get; private set; }
        public RectTransform Conversation { get; private set; }   // Ask Wattson builds its conversation here; shown while it is open
        public RectTransform Caption { get; private set; }        // SubtitleUI builds the speaker and text labels here
        public Button AskButton { get; private set; }
        public bool Wide { get; private set; }
        // Reference px the panel covers from the right edge (its width plus the margin), as it is now.
        public float Cover => shownWidth + Margin;
        // Fraction of the actual screen the panel covers right now (PanelFraming reads it every frame).
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
        RectTransform header, controls, settings, instruction, info, formula;
        TextMeshProUGUI title, instructionText, infoTitle, infoBody, askLabel, pauseLabel, subtitleLabel, motionLabel, skipLabel;
        Button replay;
        float width = NormalWidth, shownWidth = NormalWidth, skipPressed = -1f;
        string instructionShown = "", infoKey = "";

        public static Color AccentColor => UI.AccentColor;
        public static Color NoteColor => new Color32(0xFF, 0xB8, 0x30, 255); // the orange of the old callout titles
        static Color Dim => new Color(1, 1, 1, 0.62f);

        // ------------------------------------------------------------------ build

        public static LessonPanel Create(Transform safeArea)
        {
            var root = UI.Rect("Lesson Panel", safeArea, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-Margin, Margin), new Vector2(NormalWidth, 0));
            root.anchorMax = new Vector2(1, 1);
            root.offsetMin = new Vector2(-Margin - NormalWidth, Margin);
            root.offsetMax = new Vector2(-Margin, -Margin);
            var panel = root.gameObject.AddComponent<LessonPanel>();
            panel.Root = root;
            panel.Build();
            return panel;
        }

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Build()
        {
            UI.Panel(Root, UI.Alpha(UI.PanelColor, 0.985f), true, Corner).raycastTarget = true; // the panel swallows taps meant for the 3D scene behind it; near-opaque so chalk and lights behind it never bleed through the text

            // Header: title left, ASK WATTSON right. A fixed strip; the column and the conversation share the space under it.
            header = UI.Rect("Header", Root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(Pad, -Pad), new Vector2(0, HeaderHeight));
            header.anchorMax = new Vector2(1, 1);
            header.offsetMin = new Vector2(Pad, -Pad - HeaderHeight);
            header.offsetMax = new Vector2(-Pad, -Pad);
            var titleRect = UI.Rect("Title", header, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(300, HeaderHeight));
            titleRect.anchorMax = new Vector2(1, 0.5f);
            titleRect.sizeDelta = new Vector2(-244, HeaderHeight); // up to the ASK button
            title = UI.Label(titleRect, "", 20f, Dim, TextAlignmentOptions.MidlineLeft);
            title.characterSpacing = 4f;
            title.fontStyle = FontStyles.Bold;
            AskButton = UI.Button("Ask Wattson", header, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(232, HeaderHeight), "ASK WATTSON", null, 22f);
            AskButton.GetComponent<Image>().color = UI.AccentFill;
            askLabel = AskButton.GetComponentInChildren<TextMeshProUGUI>();
            askLabel.rectTransform.offsetMin = new Vector2(72, 6);
            askLabel.alignment = TextAlignmentOptions.MidlineLeft;
            askLabel.fontStyle = FontStyles.Bold;
            Face(AskButton.transform);

            var body = UI.Stretch("Body", Root);
            body.offsetMin = new Vector2(Pad, Pad);
            body.offsetMax = new Vector2(-Pad, -(Pad + HeaderHeight + Gap));

            Column = Stack("Column", body, Gap);
            Column.anchorMin = Vector2.zero;
            Column.anchorMax = Vector2.one;
            Column.offsetMin = Column.offsetMax = Vector2.zero;
            var column = Column.GetComponent<VerticalLayoutGroup>();
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.UpperCenter;

            Conversation = UI.Stretch("Conversation", body);
            Conversation.gameObject.SetActive(false);

            BuildControls();
            foreach (var name in Order) Block(name);
            BuildInstruction();
            Caption = Block("Caption");
            BuildInfo();
            formula = Stack("Lines", Block("Formula"), 6);
            formula.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            Block("Spacer").gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            Block("Spacer").gameObject.SetActive(true);
            BuildEnd();
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

        // A named block of the column, created on first use at its place in Order (hidden until shown).
        public RectTransform Block(string name)
        {
            if (blocks.TryGetValue(name, out var block) && block) return block;
            block = UI.Rect(name, Column, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            // After the controls (which are not in Order) and after every block that comes before it in Order.
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
            block.gameObject.SetActive(false);
            blocks[name] = block;
            return block;
        }

        public void Show(string name, bool on)
        {
            var block = Block(name);
            if (block.gameObject.activeSelf != on) block.gameObject.SetActive(on);
        }

        public bool Showing(string name) => blocks.TryGetValue(name, out var block) && block && block.gameObject.activeSelf;

        // Empties the block and returns a new child root for a view to build in (its own layout, padding and optional
        // panel image), so the next Clear() takes the whole view away, image and component included.
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
            if (panel) UI.Panel(home, UI.Alpha(Color.Lerp(UI.PanelColor, Color.white, 0.04f), 1f), true, 16f).raycastTarget = false;
            Show(name, true);
            return home;
        }

        // Everything inside a block goes (the block itself stays for the next use).
        public void Clear(string name)
        {
            var block = Block(name);
            for (int i = block.childCount - 1; i >= 0; i--) { var child = block.GetChild(i).gameObject; child.SetActive(false); Destroy(child); }
            Show(name, false);
        }

        // ------------------------------------------------------------------ header and controls

        void Face(Transform button)
        {
            var ring = UI.Rect("Ring", button, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(56, 56));
            var ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = UI.Circle();
            ringImage.color = UI.Theme ? UI.Theme.wattsonAccent : Color.cyan;
            ringImage.raycastTarget = false;
            var lens = UI.Rect("Lens", ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48, 48));
            var lensImage = lens.gameObject.AddComponent<Image>();
            lensImage.sprite = UI.Circle();
            lensImage.color = new Color(0.08f, 0.12f, 0.2f);
            lensImage.raycastTarget = false;
            lens.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (UI.Theme && UI.Theme.wattsonFace)
            {
                var face = UI.Stretch("Face", lens).gameObject.AddComponent<RawImage>();
                face.texture = UI.Theme.wattsonFace;
                face.raycastTarget = false;
            }
        }

        void BuildControls()
        {
            controls = UI.Rect("Controls", Column, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, ControlHeight));
            Fixed(controls, ControlHeight);
            var row = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            pauseLabel = Control(controls, "PAUSE", TogglePause);
            replay = Control(controls, "REPLAY", () => { if (SceneDirector.Gate == null && SubtitleTrack.LastLineStart >= 0 && ModuleFlow.Instance) ModuleFlow.Instance.JumpTo(SubtitleTrack.LastLineStart); }).GetComponentInParent<Button>();
            skipLabel = Control(controls, "SKIP", () => { if (ModuleFlow.Instance && ModuleFlow.Instance.debugMode) Skip(); });
            var hold = skipLabel.transform.parent.gameObject.AddComponent<EventTrigger>();
            On(hold, EventTriggerType.PointerDown, () => skipPressed = Time.unscaledTime);
            On(hold, EventTriggerType.PointerUp, () => skipPressed = -1f);
            Control(controls, "MORE", () => settings.gameObject.SetActive(!settings.gameObject.activeSelf));

            // Settings: captions, motion and the three volumes, under the controls while MORE is open.
            settings = Stack("Settings", Column, 8f);
            settings.SetSiblingIndex(controls.GetSiblingIndex() + 1);
            var toggles = UI.Rect("Toggles", settings, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, ControlHeight));
            Fixed(toggles, ControlHeight);
            var toggleRow = toggles.gameObject.AddComponent<HorizontalLayoutGroup>();
            toggleRow.spacing = 8f;
            toggleRow.childControlWidth = toggleRow.childControlHeight = true;
            toggleRow.childForceExpandWidth = toggleRow.childForceExpandHeight = true;
            subtitleLabel = Control(toggles, "CAPTIONS ON", () => SubtitleUI.Enabled = !SubtitleUI.Enabled);
            motionLabel = Control(toggles, "MOTION FULL", () => ReducedMotion.On = !ReducedMotion.On);
            foreach (var (name, kind) in new[] { ("Wattson", AudioBus.Kind.Voice), ("Effects", AudioBus.Kind.Sfx), ("Music", AudioBus.Kind.Music) })
                VolumeRow(settings, name, kind);
            settings.gameObject.SetActive(false);
        }

        static TextMeshProUGUI Control(Transform row, string text, UnityAction onClick)
        {
            var button = UI.Button(text, row, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(120, ControlHeight), text, onClick, ControlFont);
            button.GetComponent<Image>().color = UI.RaisedColor;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = ControlFont;
            return label;
        }

        static void VolumeRow(Transform parent, string name, AudioBus.Kind kind)
        {
            var row = UI.Rect("Volume " + name, parent, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 44));
            Fixed(row, 44f);
            var label = UI.Label(UI.Rect("Name", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(150, 44)), name, 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            label.rectTransform.offsetMin = new Vector2(8, 0);
            var rect = UI.Rect("Slider", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(160, 0), new Vector2(0, 44));
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(-170, 44);
            var slider = rect.gameObject.AddComponent<Slider>();
            var track = UI.Stretch("Track", rect);
            track.offsetMin = new Vector2(0, 18);
            track.offsetMax = new Vector2(0, -18);
            UI.Panel(track, new Color(1, 1, 1, 0.25f));
            var fill = UI.Stretch("Fill", track);
            UI.Panel(fill, UI.Theme ? UI.Theme.wattsonAccent : Color.cyan);
            var handleArea = UI.Stretch("Handle Area", rect);
            handleArea.offsetMin = new Vector2(18, 0);
            handleArea.offsetMax = new Vector2(-18, 0);
            var handle = UI.Rect("Handle", handleArea, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 0));
            var knob = handle.gameObject.AddComponent<Image>();
            knob.sprite = UI.Circle();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.value = AudioBus.Get(kind);
            slider.onValueChanged.AddListener(value => AudioBus.Set(kind, value));
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

        // Through the scene's director when there is one, so the hand-off state (camera pose, circuit) survives a skip.
        public static void Skip()
        {
            var flow = ModuleFlow.Instance;
            if (!flow) return;
            var director = flow.Director ? flow.Director.GetComponent<SceneDirector>() : null;
            if (director) director.Complete(); else flow.CompleteScene();
        }

        static void TogglePause()
        {
            var flow = ModuleFlow.Instance;
            if (flow) flow.SetPaused(ModuleFlow.PauseReason.User, !flow.IsPaused(ModuleFlow.PauseReason.User));
        }

        // ------------------------------------------------------------------ instruction, caption, info

        void BuildInstruction()
        {
            instruction = Block("Instruction");
            var layout = instruction.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 16, 12, 12);
            UI.Panel(instruction, UI.Alpha(Color.Lerp(UI.PanelColor, AccentColor, 0.18f), 1f), true, 18f).raycastTarget = false;
            var bar = UI.Rect("Bar", instruction, new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 10), new Vector2(6, 0));
            bar.anchorMax = new Vector2(0, 1);
            bar.offsetMin = new Vector2(8, 10);
            bar.offsetMax = new Vector2(14, -10);
            bar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UI.Panel(bar, AccentColor, true, 3f).raycastTarget = false;
            instructionText = UI.Label(instruction, "", InstructionFont, Color.white, TextAlignmentOptions.TopLeft);
            instructionText.fontStyle = FontStyles.Bold;
        }

        void BuildInfo()
        {
            info = Block("Info");
            var layout = info.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 10, 12);
            layout.spacing = 2f;
            UI.Panel(info, UI.Alpha(Color.Lerp(UI.PanelColor, Color.white, 0.06f), 1f), true, 16f).raycastTarget = false;
            infoTitle = UI.Label(info, "", InfoTitleFont, NoteColor, TextAlignmentOptions.TopLeft);
            infoTitle.fontStyle = FontStyles.Bold;
            infoBody = UI.Label(info, "", InfoFont, Color.white, TextAlignmentOptions.TopLeft);
        }

        // The one current instruction. Empty hides the block. The text stays until the next call.
        public void Instruction(string text)
        {
            text = text ?? "";
            if (text == instructionShown) return;
            instructionShown = text;
            instructionText.text = text;
            Show("Instruction", text.Length > 0);
            if (text.Length > 0) Pop(instruction);
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
            if (any) Pop(info);
        }

        public void InfoClear(string key = null)
        {
            if (key != null && key != infoKey) return; // a later note replaced this one
            Info("", "", "");
        }

        public string CurrentInfo => Showing("Info") ? (infoTitle.text + " " + infoBody.text).Trim() : "";

        public void SetTitle(string text) { if (title) title.text = (text ?? "").ToUpperInvariant(); }

        // ------------------------------------------------------------------ formula reveals

        // Appends a line to the formula block (V ∝ I → V = kI → ...), each one popping in; the block shows itself.
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

        // A hero reveal in the formula block: the big text (glowing accent) over a small line.
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

        // Empties the formula block, except lines revealed this very frame (a "formula.clear" cue placed at the same
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
            var replayButton = UI.Button("Replay", end, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, ButtonHeight), "REPLAY THE INVESTIGATION", () =>
            {
                var flow = ModuleFlow.Instance;
                if (flow && flow.scenes != null && flow.scenes.Length > 0) flow.JumpToScene(flow.scenes[0]);
            }, 24f);
            replayButton.GetComponent<Image>().color = UI.AccentFill;
            Fixed((RectTransform)replayButton.transform, ButtonHeight);
            var exit = UI.Button("Exit", end, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, ButtonHeight), "EXIT", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }, 24f);
            exit.GetComponent<Image>().color = UI.RaisedColor;
            Fixed((RectTransform)exit.transform, ButtonHeight);
        }

        public void End(bool on) => Show("End", on);

        // ------------------------------------------------------------------ data views

        ObservationTableUI table;
        GraphUI graph;
        public const float TableScale = 0.5f, GraphScale = 0.48f, MinViewShare = 0.7f;
        public bool HasViews => table || graph;

        // The observation table and the V-I graph, built inside the panel on first use (the panel widens for them) and
        // kept until the next scene entry resets the panel; their state comes from the scene's cues.
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

        // A new scene: every block empties (the scene's cues rebuild what it needs) and the panel narrows.
        public void ResetForScene(string sceneTitle)
        {
            SetTitle(sceneTitle);
            foreach (var name in Order) if (name != "Spacer" && name != "Caption" && name != "Instruction" && name != "Info" && name != "Formula" && name != "End") Clear(name);
            table = null;
            graph = null;
            Instruction("");
            InfoClear();
            FormulaClear();
            Show("Caption", false);
            End(false);
            Expand(false);
        }

        // ------------------------------------------------------------------ width and the conversation

        // Wide during table, graph and comparison explanations: the panel grows toward the centre, still docked right.
        public void Expand(bool wide)
        {
            Wide = wide;
            width = wide ? WideWidth : NormalWidth;
        }

        // Ask Wattson open: the column gives way to the conversation; the header stays.
        public void ShowConversation(bool open)
        {
            Column.gameObject.SetActive(!open);
            Conversation.gameObject.SetActive(open);
            askLabel.text = open ? "BACK TO\nLESSON" : "ASK WATTSON";
        }

        // A button at the panel's button height for the blocks (options, voltage, record ...).
        public static Button Button(string name, Transform parent, string text, UnityAction onClick, float height = ButtonHeight, float font = 26f)
        {
            var button = UI.Button(name, parent, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, height), text, onClick, font);
            button.GetComponent<Image>().color = UI.RaisedColor;
            Fixed((RectTransform)button.transform, height);
            return button;
        }

        // A short scale-in on a block that just changed (none under reduced motion).
        public static void Pop(RectTransform rect)
        {
            if (!rect || ReducedMotion.On) return;
            var pop = rect.GetComponent<UIPop>();
            if (!pop) pop = rect.gameObject.AddComponent<UIPop>();
            pop.Restart();
        }

        void Update()
        {
            var flow = ModuleFlow.Instance;
            bool paused = flow && flow.IsPaused(ModuleFlow.PauseReason.User);
            pauseLabel.text = paused ? "PLAY" : "PAUSE";
            subtitleLabel.text = SubtitleUI.Enabled ? "CAPTIONS ON" : "CAPTIONS OFF";
            motionLabel.text = ReducedMotion.On ? "MOTION REDUCED" : "MOTION FULL";
            if (replay.interactable != (SceneDirector.Gate == null)) replay.interactable = SceneDirector.Gate == null;
            skipLabel.text = skipPressed >= 0 ? "HOLD..." : "SKIP";
            if (skipPressed >= 0 && Time.unscaledTime - skipPressed > SkipHold) { skipPressed = -1; Skip(); }

            float target = width;
            shownWidth = ReducedMotion.On ? target : Mathf.Lerp(shownWidth, target, 1 - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            if (Mathf.Abs(shownWidth - target) < 0.5f) shownWidth = target;
            if (!Mathf.Approximately(Root.offsetMin.x, -Margin - shownWidth)) Root.offsetMin = new Vector2(-Margin - shownWidth, Margin);
        }
    }

    // Scale-in of a UI block: 0.96 -> 1 with a soft overshoot, unscaled time.
    // Keeps a fixed-px canvas scaled to the height its holder was given: when the column is crowded the layout
    // shrinks the holder toward its minimum and the canvas follows, keeping its aspect.
    public class FrameStamp : MonoBehaviour { public int frame; }

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

    // On the main camera: shifts the projection so the 3D view is centred in the part of the screen the panel leaves free.
    // Applied after Cinemachine has placed the camera for the frame, from the camera's own fov, aspect and clip planes, so
    // every shot composes into the free area without re-authoring the cameras; the shift follows the panel's width.
    [DefaultExecutionOrder(900)]
    public class PanelFraming : MonoBehaviour
    {
        Camera cam;
        int steppedFrame = -1;

        void Awake() => cam = GetComponent<Camera>();
        void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(CameraUpdated);
        void OnDisable() { CinemachineCore.CameraUpdatedEvent.RemoveListener(CameraUpdated); if (cam) cam.ResetProjectionMatrix(); }

        void CameraUpdated(CinemachineBrain brain) => Apply();
        void LateUpdate() { if (CinemachineBrain.ActiveBrainCount == 0 || Time.frameCount - steppedFrame > 1) Apply(); }

        void Apply()
        {
            if (!cam) return;
            steppedFrame = Time.frameCount;
            var panel = LessonPanel.Instance;
            float share = panel ? panel.ScreenShare : 0f;
            if (share <= 0.001f) { cam.ResetProjectionMatrix(); return; }
            // NDC x of the view axis = -m02: the free area's centre sits `share` left of the screen centre.
            var m = Matrix4x4.Perspective(cam.fieldOfView, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
            m[0, 2] = share;
            cam.projectionMatrix = m;
        }
    }
}
