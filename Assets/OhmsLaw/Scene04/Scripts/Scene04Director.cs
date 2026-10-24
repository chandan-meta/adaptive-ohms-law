using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Scene 4 moments the Timeline triggers through signals: the words drifting back, the slow-motion raise,
    // the tap-the-wire hold before the dive, the hand-off to Scene 5, and the beds. The music and the room tone are runtime
    // sources, not Timeline tracks: the wire-tap hold (and PAUSE, and the Ask panel) freeze the Timeline, and the music must
    // play on through the hold, the tap and the dive into Scene 5, which takes the playing bed over (AudioBus carry).
    public class Scene04Director : MonoBehaviour
    {
        public const string TapInstruction = "Click or tap the highlighted wire to explore what happens inside.";
        public const string TapGate = "S04_TapWire"; // the hold's gate id (SceneDirector.Gate): Enter, Ask Wattson and the play test see it

        [Tooltip("Wait for the student to tap the wire before the dive.")] public bool tapToEnter = true;
        [Tooltip("Dive anyway after this many seconds in a hands-free review run (ReviewMode.autoAnswer). 0 waits for the tap.")] public float autoAdvance = 4f;
        public Transform wire, words;
        public Vector3 wordsDrift = new Vector3(0, 0.3f, 0);
        public WattsonController wattson;
        public Niscope niscope;

        [Header("Beds")]
        [Tooltip("The crossfade pair, both named Music (Music buses): whichever plays at the dive is carried into Scene 5.")] public AudioSource music, musicTwin;
        [Tooltip("The room tone (an Sfx bus named Ambience), carried into Scene 5's WireHum crossfade.")] public AudioSource ambience;
        public AudioClip laboratory, theme, wireBed, roomTone;
        [Tooltip("Seconds into each clip where its bed enters (on the music's own downbeats).")] public float laboratoryIn = 11.13f, themeIn = 1.7f;
        public float laboratoryFade = 1.5f, themeFade = 2f, dipLevel = 0.4f, dipSeconds = 6f, wireFade = 3f, roomFade = 1f;
        AudioSource activeBed;
        Coroutine bedFade, roomFadeRun;

        RectTransform tapIcon;
        Vector3 wordsHome;
        float holdStarted = -1, idle;
        bool drifting;

        void Start()
        {
            // A bed carried INTO Scene 4 (Replay from the finale, a chapter jump) was taken over by a bus of the same name: it
            // goes out at once rather than playing under the title or doubling the laboratory theme.
            foreach (var bed in new[] { music, musicTwin })
                if (bed && bed.isPlaying) { activeBed = bed; bedFade = StartCoroutine(Crossfade(null, null, 0f, 0f, 1f)); }
            if (ambience && ambience.isPlaying) roomFadeRun = StartCoroutine(Room(null, 1f));
            if (wattson) wattson.gestureSounds = false; // Scene 4's Timeline carries Wattson's wave / gadget sounds at the exact frames
            wordsHome = words.position;
            if (LessonPanel.Instance && wattson && wattson.agentContext) LessonPanel.Instance.SetTitle(wattson.agentContext.sceneTitle);
        }

        public void DriftWords() => drifting = true;

        // ------------------------------------------------------------------ beds (signals)

        public void StartBeds()
        {
            Bed(laboratory, laboratoryIn, 1f, laboratoryFade);
            if (roomFadeRun != null) StopCoroutine(roomFadeRun);
            roomFadeRun = StartCoroutine(Room(roomTone, roomFade));
        }

        public void ThemeBed() => Bed(theme, themeIn, 1f, themeFade);
        public void DipBed() => Bed(activeBed ? activeBed.clip : theme, 0f, dipLevel, dipSeconds);
        public void WireBed() => Bed(wireBed ? wireBed : AudioLibrary.ClipOrNull("PadWire"), 0f, 1f, wireFade);

        // `clip` at `level` of the bus's built gain, entering `clipIn` seconds into the clip, over `seconds`. The same clip
        // as the playing bed only changes its level (the dip); another one starts on the idle source and rises while every
        // other playing bed lowers, equal power: never a dip to silence. A missing clip leaves the bed as it is.
        void Bed(AudioClip clip, float clipIn, float level, float seconds)
        {
            if (!clip || !music) return;
            if (bedFade != null) StopCoroutine(bedFade);
            AudioSource to = activeBed && activeBed.isPlaying && activeBed.clip == clip ? activeBed : activeBed == music && musicTwin ? musicTwin : music;
            bedFade = StartCoroutine(Crossfade(to, clip, clipIn, level, seconds));
        }

        IEnumerator Crossfade(AudioSource to, AudioClip clip, float clipIn, float level, float seconds)
        {
            var inBus = to ? to.GetComponent<AudioBus>() : null;
            if (to && (to.clip != clip || !to.isPlaying))
            {
                if (inBus) inBus.gain = 0f;
                to.clip = clip;
                to.loop = true;
                to.Play();
                if (clip && clip.length > 0f) to.time = Mathf.Repeat(clipIn, clip.length);
            }
            if (inBus) inBus.trim = AudioLibrary.GainOf(clip ? clip.name : "");
            float inFrom = inBus ? inBus.gain : 0f, inTo = inBus ? inBus.NominalGain * level : 0f;
            var outs = new List<(AudioBus bus, AudioSource source, float from)>();
            foreach (var bed in new[] { music, musicTwin })
                if (bed && bed != to && bed.isPlaying && bed.TryGetComponent(out AudioBus bus)) outs.Add((bus, bed, bus.gain));
            activeBed = to;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / Mathf.Max(0.05f, seconds))
            {
                float a = t * Mathf.PI * 0.5f;
                foreach (var o in outs) o.bus.gain = o.from * Mathf.Cos(a);
                if (inBus) inBus.gain = Mathf.Lerp(inFrom, inTo, Mathf.Sin(a));
                yield return null;
            }
            foreach (var o in outs) { o.source.Stop(); o.bus.gain = o.bus.NominalGain; }
            if (inBus) inBus.gain = inTo;
            bedFade = null;
        }

        // The room tone: whatever plays lowers first, then `clip` rises (null: it just goes out).
        IEnumerator Room(AudioClip clip, float seconds)
        {
            if (!ambience || !ambience.TryGetComponent(out AudioBus bus)) yield break;
            if (ambience.isPlaying && ambience.clip != clip)
            {
                float from = bus.gain;
                for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.5f) { bus.gain = from * (1 - t); yield return null; }
                ambience.Stop();
            }
            if (!clip) { bus.gain = bus.NominalGain; roomFadeRun = null; yield break; }
            if (!ambience.isPlaying) { bus.gain = 0f; ambience.clip = clip; ambience.loop = true; ambience.Play(); }
            bus.trim = AudioLibrary.GainOf(clip.name);
            float start = bus.gain;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / Mathf.Max(0.05f, seconds)) { bus.gain = Mathf.Lerp(start, bus.NominalGain, t); yield return null; }
            bus.gain = bus.NominalGain;
            roomFadeRun = null;
        }

        public void SlowMotion() => StartCoroutine(Slow(1.3f));

        IEnumerator Slow(float seconds)
        {
            wattson.motionScale = niscope.timeScale = 0.3f;
            yield return new WaitForSeconds(seconds);
            wattson.motionScale = niscope.timeScale = 1f;
        }

        // The hold: the instruction goes into the panel and stays until the wire is tapped (or Enter is pressed, as at every
        // gate); a large tap target follows the wire (the highlighted outline shows which wire). Only a hands-free review
        // run advances by itself.
        public void WaitForTap()
        {
            if (!tapToEnter || holdStarted >= 0) return;
            ModuleFlow.Instance.SetPaused(ModuleFlow.PauseReason.Hold, true);
            SceneDirector.Gate = TapGate;
            HudKeys.ConsumeAct(); // an Enter pressed before the hold does not answer it
            holdStarted = Time.unscaledTime;
            idle = 0f;
            if (LessonPanel.Instance) LessonPanel.Instance.Turn("Tap the glowing wire.", null, "Find out what happens inside it.");

            // A tap-hand over the wire: pulsing ring, finger and palm; the ring itself is the target (220 px, easy to hit).
            tapIcon = UI.Rect("Tap Wire", ModuleFlow.Instance.transform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 220));
            tapIcon.SetAsFirstSibling();
            var ring = tapIcon.gameObject.AddComponent<Image>();
            ring.sprite = UI.Circle();
            ring.color = new Color(1, 1, 1, 0.12f);
            var outline = UI.Stretch("Ring", tapIcon).gameObject.AddComponent<Image>();
            outline.sprite = UI.Circle(0.92f);
            outline.color = new Color(1, 1, 1, 0.85f);
            outline.raycastTarget = false;
            tapIcon.gameObject.AddComponent<Button>().onClick.AddListener(Dive);
            var finger = UI.Rect("Finger", tapIcon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), new Vector2(-6, -8), new Vector2(26, 62));
            UI.Panel(finger, Color.white).raycastTarget = false;
            var palm = UI.Rect("Palm", tapIcon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(6, 2), new Vector2(58, 44));
            UI.Panel(palm, Color.white).raycastTarget = false;
        }

        void Dive()
        {
            if (holdStarted < 0) return;
            holdStarted = -1;
            SceneDirector.Gate = null;
            if (tapIcon) Destroy(tapIcon.gameObject);
            if (LessonPanel.Instance) LessonPanel.Instance.Done("Into the wire.");
            ModuleFlow.Instance.SetPaused(ModuleFlow.PauseReason.Hold, false);
        }

        // No white: Scene 5 opens on this frame (its "Macro A" stands where SH11b Dive ends) and carries the dive on.
        public void Complete() => ModuleFlow.Instance.Cut();

        void Update()
        {
            words.position = Vector3.Lerp(words.position, wordsHome + (drifting ? wordsDrift : Vector3.zero), 1 - Mathf.Exp(-2f * Time.deltaTime));
            if (holdStarted < 0) return;

            var cam = Camera.main;
            if (cam && tapIcon)
            {
                var canvas = ModuleFlow.Instance.GetComponent<Canvas>();
                tapIcon.anchoredPosition = cam.WorldToScreenPoint(wire.position) / canvas.scaleFactor;
                tapIcon.localScale = Vector3.one * (1 + 0.1f * Mathf.Sin(Time.unscaledTime * 5f));
            }
            if (HudKeys.ConsumeAct()) { Dive(); return; }
            // An idle learner is nudged as at every gate: the turn card pulses with the hint sound.
            idle = HudKeys.Tapped || AskWattsonUI.IsOpen || ModuleFlow.UserPaused ? 0f : idle + Time.unscaledDeltaTime;
            if (idle >= SceneDirector.IdleSeconds && LessonPanel.Instance)
            {
                idle = 0f;
                LessonPanel.Pop(LessonPanel.Instance.Block("Instruction"));
                UiSounds.Play("hint");
            }
            // The Ask panel pauses the countdown, so a question never gets cut off by the dive.
            if (AskWattsonUI.IsOpen) holdStarted += Time.unscaledDeltaTime;
            else if (ReviewMode.autoAnswer && autoAdvance > 0 && Time.unscaledTime - holdStarted > autoAdvance) Dive();
        }

        void OnDestroy()
        {
            if (tapIcon) Destroy(tapIcon.gameObject);
            if (holdStarted >= 0 && SceneDirector.Gate == TapGate) SceneDirector.Gate = null; // left mid-hold (SKIP, a chapter jump)
        }
    }
}
