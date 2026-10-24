using System.Globalization;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Explore Now: once the case is closed on Scene 13's bench, the case-closed card offers a small Ohm's law lab, all in
    // the learner's hands and never narrated. A card in the dock's place (the wide column the bench shot is framed
    // around): the battery slider (0-5 V in 0.5 V steps), the live V / I / R line, a short table and a V-I graph that fill
    // when the learner records a reading, R = V ÷ I to work out and check (with units; never at 0 V, where 0 ÷ 0 has no
    // answer), a second wire unlocked by a right answer (same nichrome, twice as long: 10 Ω), and one optional challenge,
    // "double the current". The bench answers every change: the needles and plates, the amber path's brightness, the
    // electrical hum. Back returns to the case-closed card with the rig as Scene 13 left it (5 V, 5 Ω).
    public class ExploreLab : MonoBehaviour
    {
        public static bool IsOpen => lab && lab.gameObject.activeSelf;

        static ExploreLab lab;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        const int Rows = 6;                                       // the table keeps the last six readings (oldest scrolls off)
        const float StepVolts = 0.5f, LongWire = 10f;
        const float TableWidth = 170f, GraphWidth = 354f, DataHeight = 204f, Line = 26f;
        const float PlotLeft = 50f, PlotBottom = 40f, PlotWidth = 290f, PlotHeight = 162f, MaxI = 1.2f, MaxV = 6f;
        const string ZeroNote = "At 0 V there is no current, and 0 ÷ 0 has no answer. Set a voltage first.";
        // Where Wattson waits while the learner works: behind and above the source (Scene 10's spot), clear of the card.
        static readonly Vector3 QuietSpot = new Vector3(-0.75f, 1.4f, 0.8f);

        RectTransform card, plot, live, wireRow;
        Slider slider;
        TextMeshProUGUI voltsValue, readings, feedback, note, status;
        Image feedbackIcon, statusIcon;
        TMP_InputField input;
        Button check;
        HudButton shortWire, longWire;
        readonly TextMeshProUGUI[,] cells = new TextMeshProUGUI[Rows, 2];
        readonly Image[] rowGlow = new Image[Rows], dots = new Image[Rows];
        readonly RectTransform[] lines = new RectTransform[2];   // V = R I for 5 Ω and for 10 Ω
        readonly TextMeshProUGUI[] legends = new TextMeshProUGUI[2];

        // The readings, oldest first.
        readonly float[] recV = new float[Rows], recI = new float[Rows], recR = new float[Rows];
        int recorded;

        CircuitRig rig;
        SceneDirector director;
        bool unlocked, challenging, zeroShown, readingsDirty;
        float shownV = -1f, shownI = -1f, shownR = -1f, targetAmps, startAmps;
        Transform quiet;

        // What Scene 13 had before the lab, put back by Close().
        float savedVolts, savedOverride;
        bool savedPath, savedReadouts, savedPlates;
        CinemachineCamera benchCam, finaleCam;
        CinemachineCamera[] cams = new CinemachineCamera[0];
        int[] savedPriorities = new int[0];

        // Play mode without a domain reload keeps statics (and ModuleFlow's static event keeps its listeners).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            lab = null;
            ModuleFlow.UnitySectionComplete -= Offer;
            ModuleFlow.UnitySectionComplete += Offer;
            SceneManager.activeSceneChanged -= Left;
            SceneManager.activeSceneChanged += Left;
        }

        // The case is closed with the bench on screen: the end card offers the lab. Not past the last scene (the white
        // "End of module" card covers the bench) and not without a rig.
        static void Offer()
        {
            var flow = ModuleFlow.Instance;
            if (CircuitRig.Active && !(flow && flow.Covered)) LessonPanel.ExploreAction = Open;
        }

        // Leaving the scene (a replay, a chapter jump): the card lived on the module canvas, so it goes with the scene.
        static void Left(Scene from, Scene to)
        {
            if (LessonPanel.ExploreAction == (System.Action)Open) LessonPanel.ExploreAction = null;
            if (lab) Destroy(lab.gameObject);
            lab = null;
        }

        public static void Open()
        {
            var rig = CircuitRig.Active;
            var panel = LessonPanel.Instance;
            if (!rig || !panel || IsOpen) return;
            if (!lab) lab = Build(panel);
            lab.Enter(rig);
        }

        public static void Close()
        {
            if (IsOpen) lab.Leave();
        }

        // ------------------------------------------------------------------ enter and leave

        void Enter(CircuitRig circuit)
        {
            rig = circuit;
            var panel = LessonPanel.Instance;
            director = FindFirstObjectByType<SceneDirector>();
            panel.Show("End", false);
            panel.HeroClear();
            if (director) { director.Run("law.clear"); director.Run("point.clear"); }

            // The finale is over: its Timeline holds the Finale shot (Hold extrapolation), so it is stopped to free the
            // camera, and the bench shot takes over by priority (the brain blends there).
            var timeline = director ? director.GetComponent<PlayableDirector>() : FindFirstObjectByType<PlayableDirector>();
            if (timeline && timeline.playableGraph.IsValid()) timeline.Stop();
            cams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            savedPriorities = new int[cams.Length];
            for (int i = 0; i < cams.Length; i++) savedPriorities[i] = cams[i].Priority.Value;
            benchCam = Cam("Bench Study");
            finaleCam = Cam("Finale");
            Lead(benchCam);

            var wattson = WattsonController.Active;
            if (wattson)
            {
                if (!quiet) quiet = new GameObject("Lab Quiet Spot").transform;   // in the active scene: it goes with it
                quiet.position = QuietSpot;
                wattson.gesture = WattsonController.Gesture.Idle;
                wattson.pointTarget = null;
                wattson.gadgetOut = false;
                wattson.lookTarget = Camera.main ? Camera.main.transform : null;
                wattson.MoveTo(quiet);
            }

            savedVolts = rig.state.voltage;
            savedOverride = rig.resistanceOverride;
            savedPath = rig.showPath;
            savedReadouts = rig.readoutsShown;
            var plates = rig.transform.Find("Meter Plates");
            savedPlates = plates && plates.gameObject.activeSelf;
            if (plates) plates.gameObject.SetActive(true);
            rig.showPath = true;
            rig.readoutsShown = true;
            rig.controlsOpen = true;
            rig.targetAmps = -1f;
            challenging = false;
            Status("", false);
            Feedback("", false);
            shownV = shownI = shownR = -1f;
            Wires();
            gameObject.SetActive(true);
            LessonPanel.FadeIn(card);
            UiSounds.Play("open");
        }

        void Leave()
        {
            if (rig)
            {
                rig.resistanceOverride = savedOverride;
                rig.SetVoltage(savedVolts);
                rig.showPath = savedPath;
                rig.readoutsShown = savedReadouts;
                rig.controlsOpen = false;
                var plates = rig.transform.Find("Meter Plates");
                if (plates) plates.gameObject.SetActive(savedPlates);
            }
            for (int i = 0; i < cams.Length; i++) if (cams[i]) cams[i].Priority = savedPriorities[i];
            Lead(finaleCam);   // back to the finale's composition (the timeline is stopped: priority decides)
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            gameObject.SetActive(false);
            if (LessonPanel.Instance) LessonPanel.Instance.Show("End", true);
            UiSounds.Play("close");
        }

        CinemachineCamera Cam(string shot)
        {
            foreach (var cam in cams) if (cam && (cam.name == "Cam " + shot || cam.name == shot)) return cam;
            return null;
        }

        void Lead(CinemachineCamera cam)
        {
            if (!cam) return;
            int top = 0;
            foreach (var c in cams) if (c && c != cam) top = Mathf.Max(top, c.Priority.Value);
            cam.Priority = top + 10;
        }

        // ------------------------------------------------------------------ the learner's controls

        void SetVolts(float volts)
        {
            if (!rig) return;
            float before = rig.state.voltage, applied = rig.SetVoltage(volts);
            if (!Mathf.Approximately(applied, before) && director) director.Play("VoltStep", 1f + 0.07f * (applied - 1f));
        }

        void Record()
        {
            if (!rig) return;
            if (recorded == Rows)
            {
                for (int i = 1; i < Rows; i++) { recV[i - 1] = recV[i]; recI[i - 1] = recI[i]; recR[i - 1] = recR[i]; }
                recorded--;
            }
            recV[recorded] = rig.Volts;
            recI[recorded] = rig.Amps;
            recR[recorded] = rig.Resistance;
            recorded++;
            Data();
            if (director) { director.Play("Record"); director.Play("Plot", 1f + 0.05f * recorded); }
        }

        void ClearReadings()
        {
            recorded = 0;
            Data();
        }

        void Check()
        {
            if (!rig) return;
            float v = rig.Volts, i = rig.Amps;
            if (i <= 0.0001f || v <= 0.0001f) { Feedback(ZeroNote, false, LessonPanel.Muted); return; }
            if (!float.TryParse((input.text ?? "").Trim().Replace(',', '.'), NumberStyles.Float, Inv, out float answer))
            {
                Feedback("Type a number for R, then Check.", false, LessonPanel.Muted);
                return;
            }
            float r = v / i, flipped = i / v;
            string sum = $"{v.ToString("0.0", Inv)} V ÷ {i.ToString("0.00", Inv)} A";
            if (Mathf.Abs(answer - r) <= 0.02f * r)
            {
                Feedback($"{sum} = {r.ToString("0.#", Inv)} Ω", true, LessonPanel.Success);
                UiSounds.Play("correct");
                if (director) director.Run("snap");
                Unlock();
            }
            else if (Mathf.Abs(answer - flipped) <= 0.02f * flipped)
            {
                Feedback("That's I ÷ V. Flip it: V ÷ I.", false, LessonPanel.Error);
                UiSounds.Play("wrong");
            }
            else
            {
                Feedback($"Not quite: {sum} = ? Try again.", false, LessonPanel.Error);
                UiSounds.Play("wrong");
            }
        }

        // A first right answer opens the second wire.
        void Unlock()
        {
            if (unlocked) return;
            unlocked = true;
            wireRow.gameObject.SetActive(true);
            Note("New: a second wire, the same nichrome but twice as long.");
            readingsDirty = true;   // the R reading drops "fixed"
        }

        void ChooseWire(float ohms)
        {
            if (!rig || Mathf.Approximately(rig.Resistance, ohms)) return;
            float was = rig.Resistance;
            rig.resistanceOverride = ohms;
            Note(ohms > was ? "Same voltage, twice the resistance: half the current." : "Same voltage, half the resistance: twice the current.");
            Wires();
            Data();
        }

        void Wires()
        {
            if (!rig || !shortWire) return;
            Choice(shortWire, Mathf.Approximately(rig.Resistance, ExperimentData.Main.resistance));
            Choice(longWire, Mathf.Approximately(rig.Resistance, LongWire));
        }

        static void Choice(HudButton button, bool on)
        {
            button.active = on;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.color = on ? LessonPanel.Ink : LessonPanel.Paper;
        }

        // Double the current: from what flows now, by raising the voltage or (once unlocked) halving the resistance.
        void StartChallenge()
        {
            if (!rig) return;
            float i = rig.Amps;
            float lowest = unlocked ? Mathf.Min(rig.Resistance, ExperimentData.Main.resistance) : rig.Resistance;
            float reachable = CircuitRig.MaxVolts / Mathf.Max(0.01f, lowest);
            challenging = false;
            if (i <= 0.0001f) { Status("Set a voltage first: there is no current to double yet.", false); return; }
            if (2f * i > reachable + 0.0001f) { Status("Lower the voltage first so there's room to double it.", false); return; }
            challenging = true;
            startAmps = i;
            targetAmps = 2f * i;
            Status($"Make it {targetAmps.ToString("0.00", Inv)} A (now {i.ToString("0.00", Inv)} A).", false);
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            if (!rig) { gameObject.SetActive(false); return; }
            float v = rig.Volts, i = rig.Amps, r = rig.Resistance;
            int step = Mathf.RoundToInt(rig.state.voltage / StepVolts);
            if (Mathf.RoundToInt(slider.value) != step) slider.SetValueWithoutNotify(step);

            bool moved = !Mathf.Approximately(v, shownV) || !Mathf.Approximately(i, shownI) || !Mathf.Approximately(r, shownR);
            if (moved || readingsDirty)
            {
                readingsDirty = false;
                voltsValue.text = rig.state.voltage.ToString("0.0", Inv) + " V";
                readings.text = $"<color=#{Hex(LessonPanel.VoltColor)}>V = {v.ToString("0.0", Inv)} V</color>     <color=#{Hex(LessonPanel.AmpColor)}>I = {i.ToString("0.00", Inv)} A</color>     <color=#{Hex(LessonPanel.OhmColor)}>R = {r.ToString("0.#", Inv)} Ω</color>{(unlocked ? "" : "<size=80%><color=#" + Hex(LessonPanel.Muted) + ">  fixed</color></size>")}";
            }
            if (moved)
            {
                bool rChanged = !Mathf.Approximately(r, shownR);
                shownV = v; shownI = i; shownR = r;
                // 0 V: nothing to divide. Otherwise the last verdict was about other numbers, so it goes.
                bool none = i <= 0.0001f || v <= 0.0001f;
                input.interactable = check.interactable = !none;
                if (none) { Feedback(ZeroNote, false, LessonPanel.Muted); zeroShown = true; }
                else if (zeroShown || feedback.text.Length > 0) { Feedback("", false); zeroShown = false; }
                if (rChanged) Lines();
                if (challenging)
                {
                    if (Mathf.Abs(i - targetAmps) < 0.01f)
                    {
                        challenging = false;
                        Status($"Case cracked! {startAmps.ToString("0.00", Inv)} A → {targetAmps.ToString("0.00", Inv)} A.", true);
                        UiSounds.Play("correct");
                        if (director) director.Run("snap");
                    }
                    else Status($"Make it {targetAmps.ToString("0.00", Inv)} A (now {i.ToString("0.00", Inv)} A).", false);
                }
            }

            // ← and → step the battery when the slider does not have the keyboard and nothing is being typed.
            var keys = Keyboard.current;
            var events = EventSystem.current;
            bool typing = input.isFocused, sliderHasKeys = events && events.currentSelectedGameObject == slider.gameObject;
            if (keys != null && !typing && !sliderHasKeys && !ModuleFlow.UserPaused && !AskWattsonUI.IsOpen)
            {
                if (keys.leftArrowKey.wasPressedThisFrame) SetVolts(rig.state.voltage - StepVolts);
                if (keys.rightArrowKey.wasPressedThisFrame) SetVolts(rig.state.voltage + StepVolts);
            }

            // The live ring eases to the reading, like the needles.
            var target = At(i, v);
            live.anchoredPosition = ReducedMotion.On ? target : Vector2.Lerp(live.anchoredPosition, target, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }

        // The card fits the band between the controls and the Ask button at any screen size (1366 x 768 included).
        void LateUpdate()
        {
            var panel = LessonPanel.Instance;
            if (!panel || !card) return;
            float band = panel.Root.rect.height - LessonPanel.DockTop - LessonPanel.DockBottom;
            card.localScale = Vector3.one * Mathf.Min(1f, band / Mathf.Max(1f, card.rect.height));
        }

        // ------------------------------------------------------------------ views

        // The table and the graph show the same readings, newest last; the newest row glows.
        void Data()
        {
            for (int row = 0; row < Rows; row++)
            {
                bool shown = row < recorded;
                cells[row, 0].text = shown ? recV[row].ToString("0.0", Inv) : "";
                cells[row, 1].text = shown ? recI[row].ToString("0.00", Inv) : "";
                rowGlow[row].color = shown && row == recorded - 1 ? UI.Alpha(LessonPanel.Spark, 0.16f) : Color.clear;
                dots[row].gameObject.SetActive(shown);
                if (!shown) continue;
                dots[row].rectTransform.anchoredPosition = At(recI[row], recV[row]);
                dots[row].color = Mathf.Approximately(recR[row], LongWire) ? Color.Lerp(LessonPanel.OhmColor, Color.white, 0.45f) : Color.white;
            }
            Lines();
        }

        // V = R I for each wire with two readings on the graph: the wire in the circuit bright, the other dim.
        void Lines()
        {
            for (int k = 0; k < 2; k++)
            {
                float ohms = k == 0 ? ExperimentData.Main.resistance : LongWire;
                int points = 0;
                for (int row = 0; row < recorded; row++) if (Mathf.Approximately(recR[row], ohms) && recV[row] > 0.0001f) points++;
                bool shown = points >= 2;
                lines[k].gameObject.SetActive(shown);
                legends[k].gameObject.SetActive(shown);
                if (!shown) continue;
                bool current = rig && Mathf.Approximately(rig.Resistance, ohms);
                var color = UI.Alpha(LessonPanel.OhmColor, current ? 0.95f : 0.35f);
                lines[k].GetComponent<Image>().color = color;
                legends[k].color = color;
            }
        }

        void Feedback(string text, bool right, Color? color = null)
        {
            feedback.text = text;
            feedback.color = color ?? LessonPanel.Muted;
            feedbackIcon.gameObject.SetActive(right && text.Length > 0);
        }

        void Status(string text, bool done)
        {
            status.text = text;
            status.color = done ? LessonPanel.Success : LessonPanel.Body;
            statusIcon.gameObject.SetActive(done);
        }

        void Note(string text)
        {
            note.text = text;
            note.gameObject.SetActive(text.Length > 0);
        }

        static Vector2 At(float amps, float volts) =>
            new Vector2(Mathf.Clamp01(amps / MaxI) * PlotWidth, Mathf.Clamp01(volts / MaxV) * PlotHeight);

        static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        // ------------------------------------------------------------------ build

        static ExploreLab Build(LessonPanel panel)
        {
            var card = UI.Rect("UI_ExploreLab", panel.Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-LessonPanel.Margin, -LessonPanel.DockTop), new Vector2(LessonPanel.WideWidth, 0));
            card.SetSiblingIndex(panel.Dock.GetSiblingIndex() + 1);   // over the dock, under the controls and the menus
            var stack = card.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset((int)LessonPanel.Pad, (int)LessonPanel.Pad, 18, 18);
            stack.spacing = 10f;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LessonPanel.Card(card);
            LessonPanel.Brackets(card, true);

            var lab = card.gameObject.AddComponent<ExploreLab>();
            lab.card = card;
            lab.BuildHead();
            lab.BuildSlider();
            lab.readings = Single(card, "", 20f, LessonPanel.Paper, 30f);
            lab.readings.richText = true;
            lab.BuildData();
            lab.BuildButtons();
            lab.BuildCalculate();
            lab.BuildWires();
            lab.BuildChallenge();
            card.gameObject.SetActive(false);
            return lab;
        }

        void BuildHead()
        {
            var head = LessonPanel.Row("Head", card, 10f);
            LessonPanel.Fixed(head, 44f);
            LessonPanel.Icon(head, "bolt", 22f, LessonPanel.Spark);
            var title = UI.Display(LessonPanel.Text(head, "Ohm's Law Lab", 26f, LessonPanel.Paper, TextAlignmentOptions.MidlineLeft));
            title.textWrappingMode = TextWrappingModes.NoWrap;
            Flex(head);
            LessonPanel.Pill("Back", head, "Back to the case", Close, LessonPanel.Kind.Ghost, "x", 44f, 17f);
        }

        void BuildSlider()
        {
            var head = LessonPanel.Row("Voltage Head", card, 10f);
            LessonPanel.Fixed(head, 24f);
            LessonPanel.Eyebrow(head, "Battery voltage", LessonPanel.VoltColor).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            voltsValue = LessonPanel.Text(head, "", 22f, LessonPanel.Paper, TextAlignmentOptions.MidlineRight);
            voltsValue.textWrappingMode = TextWrappingModes.NoWrap;

            const float height = 56f, inset = 20f, trackTop = 16f, trackHeight = 10f, knob = 38f;
            int steps = Mathf.RoundToInt(CircuitRig.MaxVolts / StepVolts);
            var row = UI.Rect("Slider", card, new Vector2(0, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, height));
            LessonPanel.Fixed(row, height);
            row.gameObject.AddComponent<Image>().color = Color.clear;   // the whole row is the hit area
            slider = row.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = steps;
            slider.wholeNumbers = true;
            var track = UI.Rect("Track", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(inset, -trackTop), new Vector2(-2f * inset, trackHeight));
            track.anchorMax = new Vector2(1, 1);
            var sliderTrack = UI.Panel(track, new Color(1f, 1f, 1f, 0.18f), true, trackHeight / 2f);
            FigmaSkin.Apply(sliderTrack, "SliderTrack");
            sliderTrack.raycastTarget = false;
            var fill = UI.Stretch("Fill", UI.Stretch("Fill Area", track));
            var sliderFill = UI.Panel(fill, LessonPanel.VoltColor, true, trackHeight / 2f);
            FigmaSkin.Apply(sliderFill, "SliderFill");
            sliderFill.raycastTarget = false;
            for (int volt = 0; volt <= (int)CircuitRig.MaxVolts; volt++)
            {
                var label = UI.Rect("Label " + volt, track, new Vector2(volt / CircuitRig.MaxVolts, 0), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(56, 20));
                var text = UI.Label(label, volt + " V", 15f, LessonPanel.Muted);
                text.fontStyle = FontStyles.Bold;
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }
            var area = UI.Stretch("Handle Area", row);
            area.offsetMin = new Vector2(inset, 0);
            area.offsetMax = new Vector2(-inset, 0);
            // The Slider stretches the handle across the row's height: knob - height keeps a knob-sized disc on the track.
            var handle = UI.Rect("Knob", area, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, height / 2f - trackTop - trackHeight / 2f), new Vector2(knob, knob - height));
            var disc = handle.gameObject.AddComponent<Image>();
            disc.sprite = UI.Circle();
            disc.color = LessonPanel.Paper;
            FigmaSkin.Apply(disc, "Icon-slider-thumb", false);
            var bolt = UI.Rect("Icon", handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20)).gameObject.AddComponent<Image>();
            bolt.sprite = Icons.Get("bolt");
            bolt.color = LessonPanel.Ink;
            bolt.raycastTarget = false;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = disc;
            slider.onValueChanged.AddListener(value => SetVolts(value * StepVolts));
        }

        void BuildData()
        {
            var row = LessonPanel.Row("Data", card, 12f, TextAnchor.UpperLeft);
            LessonPanel.Fixed(row, DataHeight);

            // The table: V and I, a row per reading.
            var table = Box("Table", row, TableWidth);
            UI.Panel(table, UI.Alpha(LessonPanel.Raised, 0.55f), true, 12f).raycastTarget = false;
            Cell(table, "V (V)", 0, -6f, LessonPanel.VoltColor, 17f);
            Cell(table, "I (A)", 1, -6f, LessonPanel.AmpColor, 17f);
            var rule = UI.Rect("Rule", table, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(TableWidth / 2f, -36f), new Vector2(TableWidth - 16f, 2f));
            UI.Panel(rule, new Color(1f, 1f, 1f, 0.3f), false).raycastTarget = false;
            for (int r = 0; r < Rows; r++)
            {
                float y = -40f - r * Line;
                var glow = UI.Rect("Row " + (r + 1), table, new Vector2(0, 1), new Vector2(0, 1), new Vector2(6f, y), new Vector2(TableWidth - 12f, Line - 2f));
                rowGlow[r] = UI.Panel(glow, Color.clear, true, 8f);
                rowGlow[r].raycastTarget = false;
                cells[r, 0] = Cell(table, "", 0, y, LessonPanel.Paper, 17f);
                cells[r, 1] = Cell(table, "", 1, y, LessonPanel.Paper, 17f);
            }

            // The graph: V up, I along, as in the lesson (slope = ΔV ÷ ΔI = R).
            var graph = Box("Graph", row, GraphWidth);
            UI.Panel(graph, UI.Alpha(LessonPanel.Raised, 0.35f), true, 12f).raycastTarget = false;
            plot = UI.Rect("Plot", graph, Vector2.zero, Vector2.zero, new Vector2(PlotLeft, PlotBottom), new Vector2(PlotWidth, PlotHeight));
            var grid = new Color(1f, 1f, 1f, 0.09f);
            for (int k = 1; k <= 6; k++)
            {
                Segment(plot, At(k * 0.2f, 0), At(k * 0.2f, MaxV), grid, 1.5f);
                Segment(plot, At(0, k), At(MaxI, k), grid, 1.5f);
            }
            Segment(plot, Vector2.zero, new Vector2(PlotWidth + 8f, 0), new Color(1f, 1f, 1f, 0.55f), 2.5f);
            Segment(plot, Vector2.zero, new Vector2(0, PlotHeight + 8f), new Color(1f, 1f, 1f, 0.55f), 2.5f);
            for (int k = 2; k <= 6; k += 2)
            {
                Tick(plot, (k * 0.2f).ToString("0.0", Inv), At(k * 0.2f, 0) + new Vector2(0, -12f));
                Tick(plot, k.ToString(Inv), At(0, k) + new Vector2(-16f, 0));
            }
            var iTitle = Tick(plot, "I (A)", new Vector2(PlotWidth / 2f, -30f));
            iTitle.color = LessonPanel.AmpColor;
            iTitle.fontSize = 16f;
            var vTitle = Tick(plot, "V (V)", new Vector2(-38f, PlotHeight / 2f));
            vTitle.color = LessonPanel.VoltColor;
            vTitle.fontSize = 16f;
            vTitle.rectTransform.parent.localRotation = Quaternion.Euler(0, 0, 90);
            for (int k = 0; k < 2; k++)
            {
                float ohms = k == 0 ? ExperimentData.Main.resistance : LongWire;
                float endI = Mathf.Min(MaxI, MaxV / ohms);
                lines[k] = Segment(plot, Vector2.zero, At(endI, endI * ohms), LessonPanel.OhmColor, 3f);
                // Beside the line's top end, never on it: left of the shallow 5 Ω line, right of the steep 10 Ω one.
                legends[k] = Tick(plot, ohms.ToString("0", Inv) + " Ω", At(endI, endI * ohms) + new Vector2(k == 0 ? -62f : 48f, -8f));
                legends[k].alignment = k == 0 ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                lines[k].gameObject.SetActive(false);
                legends[k].gameObject.SetActive(false);
            }
            for (int r = 0; r < Rows; r++)
            {
                var dot = UI.Rect("Point " + (r + 1), plot, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11f, 11f));
                dots[r] = dot.gameObject.AddComponent<Image>();
                dots[r].sprite = UI.Circle();
                dots[r].raycastTarget = false;
                dot.gameObject.SetActive(false);
            }
            live = UI.Rect("Live", plot, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
            var ring = live.gameObject.AddComponent<Image>();
            ring.sprite = UI.Circle(0.55f);
            ring.color = LessonPanel.AmpColor;
            ring.raycastTarget = false;
        }

        void BuildButtons()
        {
            var row = LessonPanel.Row("Record", card, 10f);
            LessonPanel.Fixed(row, 44f);
            LessonPanel.Pill("Record", row, "Record reading", Record, LessonPanel.Kind.Secondary, "check", 44f, 18f);
            LessonPanel.Pill("Clear", row, "Clear", ClearReadings, LessonPanel.Kind.Ghost, null, 44f, 17f);
        }

        void BuildCalculate()
        {
            var row = LessonPanel.Row("Calculate", card, 10f);
            LessonPanel.Fixed(row, 48f);
            var sum = LessonPanel.Text(row, $"<color=#{Hex(LessonPanel.OhmColor)}>R</color> = <color=#{Hex(LessonPanel.VoltColor)}>V</color> ÷ <color=#{Hex(LessonPanel.AmpColor)}>I</color> =", 21f, LessonPanel.Paper, TextAlignmentOptions.MidlineLeft);
            sum.textWrappingMode = TextWrappingModes.NoWrap;

            var field = UI.Rect("Answer", row, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116f, 44f));
            var size = field.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = size.minWidth = 116f;
            size.preferredHeight = size.minHeight = 44f;
            UI.Panel(field, LessonPanel.Raised, true, 22f);
            UI.Outline(UI.Stretch("Edge", field), LessonPanel.ControlLine, 22f, 1.5f);
            input = field.gameObject.AddComponent<TMP_InputField>();
            var area = UI.Stretch("Text Area", field);
            area.offsetMin = new Vector2(16, 6);
            area.offsetMax = new Vector2(-14, -6);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = UI.Label(area, "?", 21f, LessonPanel.Muted, TextAlignmentOptions.Center);
            var text = UI.Label(area, "", 21f, LessonPanel.Paper, TextAlignmentOptions.Center);
            placeholder.textWrappingMode = text.textWrappingMode = TextWrappingModes.NoWrap;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 6;
            input.onSubmit.AddListener(_ => Check());

            var unit = LessonPanel.Text(row, "Ω", 24f, LessonPanel.OhmColor, TextAlignmentOptions.MidlineLeft);
            unit.textWrappingMode = TextWrappingModes.NoWrap;
            Flex(row);
            check = LessonPanel.Pill("Check", row, "Check", Check, LessonPanel.Kind.Primary, null, 44f, 19f);

            var line = LessonPanel.Row("Feedback", card, 8f);
            LessonPanel.Fixed(line, 24f);
            feedbackIcon = LessonPanel.Icon(line, "check", 18f, LessonPanel.Success);
            feedback = LessonPanel.Text(line, "", 17f, LessonPanel.Muted, TextAlignmentOptions.MidlineLeft);
            feedback.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            feedback.enableAutoSizing = true;
            feedback.fontSizeMin = 15f;
            feedback.fontSizeMax = 17f;
            feedbackIcon.gameObject.SetActive(false);
        }

        void BuildWires()
        {
            wireRow = LessonPanel.Row("Wire", card, 10f);
            LessonPanel.Fixed(wireRow, 44f);
            LessonPanel.Eyebrow(wireRow, "Wire", LessonPanel.OhmColor);
            shortWire = LessonPanel.Pill("Wire XY", wireRow, "Wire XY · 5 Ω", () => ChooseWire(ExperimentData.Main.resistance), LessonPanel.Kind.Raised, null, 44f, 17f).GetComponent<HudButton>();
            longWire = LessonPanel.Pill("Long Wire", wireRow, "Twice as long · 10 Ω", () => ChooseWire(LongWire), LessonPanel.Kind.Raised, null, 44f, 17f).GetComponent<HudButton>();
            wireRow.gameObject.SetActive(false);
            note = Single(card, "", 16f, LessonPanel.Body, 22f);
            note.gameObject.SetActive(false);
        }

        void BuildChallenge()
        {
            var row = LessonPanel.Row("Challenge", card, 10f);
            LessonPanel.Fixed(row, 44f);
            LessonPanel.Pill("Challenge", row, "Challenge: double the current", StartChallenge, LessonPanel.Kind.Secondary, "bolt", 44f, 17f);
            var line = LessonPanel.Row("Challenge Status", card, 8f);
            LessonPanel.Fixed(line, 22f);
            statusIcon = LessonPanel.Icon(line, "check", 18f, LessonPanel.Success);
            status = LessonPanel.Text(line, "", 16f, LessonPanel.Body, TextAlignmentOptions.MidlineLeft);
            status.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            status.enableAutoSizing = true;
            status.fontSizeMin = 15f;
            status.fontSizeMax = 16f;
            statusIcon.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ helpers

        // A one-line label at a fixed height in the card's column.
        static TextMeshProUGUI Single(Transform parent, string text, float size, Color color, float height)
        {
            var label = LessonPanel.Text(parent, text, size, color, TextAlignmentOptions.MidlineLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 15f;
            label.fontSizeMax = size;
            LessonPanel.Fixed(label.rectTransform, height);
            return label;
        }

        static void Flex(Transform row)
        {
            var spacer = UI.Rect("Spacer", row, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        // A fixed-size holder in a row; its children are laid out in px from its top-left (table) or bottom-left (graph).
        static RectTransform Box(string name, Transform row, float width)
        {
            var box = UI.Rect(name, row, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(width, DataHeight));
            var size = box.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = size.minWidth = width;
            size.preferredHeight = size.minHeight = DataHeight;
            return box;
        }

        // A table cell: column 0 (V) or 1 (I), its top edge `y` px below the table's top.
        static TextMeshProUGUI Cell(RectTransform table, string text, int column, float y, Color color, float size)
        {
            float half = (TableWidth - 16f) / 2f;
            var rect = UI.Rect("Cell", table, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8f + column * half, y), new Vector2(half, Line));
            var label = UI.Label(rect, text, size, color);
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        static TextMeshProUGUI Tick(RectTransform parent, string text, Vector2 at)
        {
            var rect = UI.Rect("Text", parent, Vector2.zero, new Vector2(0.5f, 0.5f), at, new Vector2(80f, 20f));
            var label = UI.Label(rect, text, 15f, LessonPanel.Muted);
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        static RectTransform Segment(RectTransform parent, Vector2 from, Vector2 to, Color color, float thickness)
        {
            var rect = UI.Rect("Line", parent, Vector2.zero, new Vector2(0, 0.5f), from, new Vector2((to - from).magnitude, thickness));
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        // ------------------------------------------------------------------ the play test's run through the lab

        // The hands-free play test (Editor/BlockoutPlayTest.cs) opens the lab from the case-closed card and works it the way
        // a learner would, through the same calls the controls make, reporting each step to `check`: 0 V refuses the
        // division, I ÷ V is caught, the right R unlocks the second wire, 10 Ω halves the current at the same voltage, the
        // challenge is met by doubling the voltage, and Back puts the bench back as Scene 13 left it.
        public static System.Collections.IEnumerator SelfTest(System.Action<string, bool> check)
        {
            var open = LessonPanel.ExploreAction;
            check("the case-closed card offers Explore Now", open != null);
            if (open == null) yield break;
            open();
            yield return new WaitForSeconds(0.6f);
            check("Explore Now opens the lab with the controls live", IsOpen && lab.rig && lab.rig.controlsOpen);
            if (!IsOpen) yield break;
            var me = lab;
            me.SetVolts(0f);
            yield return new WaitForSeconds(0.4f);
            check("at 0 V the R = V ÷ I check is off and says why", !me.check.interactable && me.feedback.text.Contains("0 ÷ 0"));
            me.SetVolts(3f);
            yield return new WaitForSeconds(0.6f);
            check("3 V on 5 Ω reads 0.60 A", Mathf.Abs(me.rig.Amps - 0.6f) < 0.001f && me.check.interactable);
            me.Record();
            check("Record adds the reading to the table and the graph", me.recorded == 1 && Mathf.Approximately(me.recV[0], 3f));
            me.input.text = "0.2";
            me.Check();
            check("I ÷ V is caught with a targeted hint", me.feedback.text.Contains("Flip"));
            me.input.text = "5";
            me.Check();
            check("the right R (5 Ω, with units) unlocks the second wire", me.unlocked && me.wireRow.gameObject.activeSelf && me.feedback.text.Contains("Ω"));
            me.ChooseWire(LongWire);
            yield return new WaitForSeconds(0.3f);
            check("the 10 Ω wire halves the current at the same 3 V", Mathf.Abs(me.rig.Amps - 0.3f) < 0.001f);
            me.SetVolts(1.5f);
            yield return new WaitForSeconds(0.3f);
            me.StartChallenge();
            check("the challenge asks for twice 0.15 A", me.challenging && Mathf.Abs(me.targetAmps - 0.3f) < 0.001f);
            me.SetVolts(3f);
            yield return new WaitForSeconds(0.4f);
            check("doubling the voltage cracks the challenge", !me.challenging && me.status.text.StartsWith("Case cracked"));
            Close();
            yield return new WaitForSeconds(0.4f);
            var rig = CircuitRig.Active;
            check("Back closes the lab and puts the bench back (5 V, 5 Ω, controls closed)", !IsOpen && rig && Mathf.Approximately(rig.state.voltage, 5f) && rig.resistanceOverride <= 0f && !rig.controlsOpen);
        }
    }
}
