using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Scene 4 moments the Timeline triggers through signals: the words drifting back, the slow-motion raise,
    // the tap-the-wire hold before the dive, and the hand-off to Scene 5.
    public class Scene04Director : MonoBehaviour
    {
        [Tooltip("Wait for the student to tap the wire before the dive.")] public bool tapToEnter = true;
        [Tooltip("Dive anyway after this many seconds. 0 waits for the tap.")] public float autoAdvance = 6f;
        public Transform wire, words;
        public Vector3 wordsDrift = new Vector3(0, 0.3f, 0);
        public WattsonController wattson;
        public Niscope niscope;

        RectTransform tapIcon;
        Vector3 wordsHome;
        float holdStarted = -1;
        bool drifting;

        void Start() { wordsHome = words.position; }

        public void DriftWords() => drifting = true;

        public void SlowMotion() => StartCoroutine(Slow(1.3f));

        IEnumerator Slow(float seconds)
        {
            wattson.motionScale = niscope.timeScale = 0.3f;
            yield return new WaitForSeconds(seconds);
            wattson.motionScale = niscope.timeScale = 1f;
        }

        public void WaitForTap()
        {
            if (!tapToEnter || holdStarted >= 0) return;
            ModuleFlow.Instance.SetPaused(ModuleFlow.PauseReason.Hold, true);
            holdStarted = Time.unscaledTime;

            // A tap-hand over the wire, no text: pulsing ring, finger and palm.
            tapIcon = UI.Rect("Tap Wire", ModuleFlow.Instance.transform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            tapIcon.SetAsFirstSibling();
            var ring = tapIcon.gameObject.AddComponent<Image>();
            ring.sprite = UI.Theme ? UI.Theme.circle : null;
            ring.color = new Color(1, 1, 1, 0.22f);
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
            if (tapIcon) Destroy(tapIcon.gameObject);
            ModuleFlow.Instance.SetPaused(ModuleFlow.PauseReason.Hold, false);
        }

        public void Complete() => ModuleFlow.Instance.CompleteScene();

        void Update()
        {
            words.position = Vector3.Lerp(words.position, wordsHome + (drifting ? wordsDrift : Vector3.zero), 1 - Mathf.Exp(-2f * Time.deltaTime));
            if (holdStarted < 0) return;

            var cam = Camera.main;
            if (cam && tapIcon)
            {
                var canvas = ModuleFlow.Instance.GetComponent<Canvas>();
                tapIcon.anchoredPosition = cam.WorldToScreenPoint(wire.position) / canvas.scaleFactor;
                tapIcon.localScale = Vector3.one * (1 + 0.12f * Mathf.Sin(Time.unscaledTime * 5f));
            }
            // The Ask panel pauses the countdown, so a question never gets cut off by the dive.
            if (AskWattsonUI.IsOpen) holdStarted += Time.unscaledDeltaTime;
            else if (autoAdvance > 0 && Time.unscaledTime - holdStarted > autoAdvance) Dive();
        }

        void OnDestroy() { if (tapIcon) Destroy(tapIcon.gameObject); }
    }
}
