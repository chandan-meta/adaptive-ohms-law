using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Scene 4 moments the Timeline triggers through signals: the words drifting back, the slow-motion raise,
    // the tap-the-wire hold before the dive, and the hand-off to Scene 5.
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

        RectTransform tapIcon;
        Vector3 wordsHome;
        float holdStarted = -1, idle;
        bool drifting;

        void Start()
        {
            if (wattson) wattson.gestureSounds = false; // Scene 4's Timeline carries Wattson's wave / gadget sounds at the exact frames
            wordsHome = words.position;
            if (LessonPanel.Instance && wattson && wattson.agentContext) LessonPanel.Instance.SetTitle(wattson.agentContext.sceneTitle);
        }

        public void DriftWords() => drifting = true;

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
