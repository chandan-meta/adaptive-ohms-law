using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Captions for every spoken line, scripted or AI, in the HUD's caption bar (LessonPanel.Caption): the speaker in
    // small spark caps over the line in large warm white, centred. The bar hugs the line (at most 1080 px, two lines a
    // page; longer text pages through, each page coming up when the voice reaches its first word) and sits centred in
    // the part of the frame the dock leaves free, clear of Ask Wattson. It fades in and out (0.25 s); CC OFF hides it.
    public class SubtitleUI : MonoBehaviour
    {
        public static SubtitleUI Instance { get; private set; }

        const float PadX = 28f, SideClear = 48f, AvatarClear = 200f;

        RectTransform box;
        CanvasGroup group;
        TextMeshProUGUI speaker, body;
        Image bolt;   // v5: a small bolt before WATTSON
        LayoutElement bodySize;
        object owner;
        bool shown;
        string laidOut = "", speakerShown;
        float laidFree = -1f;

        // Read every frame by the HUD: cached, PlayerPrefs touched only on a change (as ReducedMotion).
        static int enabled = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => enabled = -1;

        public static bool Enabled
        {
            get
            {
                if (enabled < 0) enabled = PlayerPrefs.GetInt("ohm.subtitles", 1);
                return enabled == 1;
            }
            set { enabled = value ? 1 : 0; PlayerPrefs.SetInt("ohm.subtitles", enabled); if (Instance) Instance.Apply(); }
        }

        void Awake()
        {
            Instance = this;
            var panel = LessonPanel.Instance;
            box = panel.Caption;
            group = box.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)PadX, (int)PadX, 14, 17);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var who = LessonPanel.Row("Speaker", box, 6f, TextAnchor.MiddleCenter);
            LessonPanel.Fixed(who, LessonPanel.SpeakerFont * 1.25f);
            bolt = LessonPanel.Icon(who, "bolt", 14f, LessonPanel.Spark);
            speaker = UI.Label(who, "", LessonPanel.SpeakerFont, LessonPanel.Spark, TextAlignmentOptions.Center);
            speaker.characterSpacing = 8f;
            speaker.fontStyle = FontStyles.Bold;
            speaker.textWrappingMode = TextWrappingModes.NoWrap;
            body = UI.Label(box, "", LessonPanel.CaptionFont, LessonPanel.Paper, TextAlignmentOptions.Top);
            body.overflowMode = TextOverflowModes.Page;
            bodySize = LessonPanel.Fixed(body.rectTransform, LineHeight(1));
            Apply();
        }

        // Nunito Sans's line box is ~1.37 em: a page shorter than its lines shows nothing, so leave room.
        static float LineHeight(int lines) => LessonPanel.CaptionFont * 1.42f * lines + 6f;

        void Apply() => shown = Enabled && owner != null;

        void Update()
        {
            // While Ask Wattson is open only his answer is captioned: the lesson's paused line waits behind the conversation.
            bool visible = shown && (!AskWattsonUI.IsOpen || owner is WattsonAgent);
            float target = visible ? 1f : 0f;
            group.alpha = ReducedMotion.On ? target : Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / 0.25f);
            if (visible) Layout();
        }

        // Width from the line (one line if it fits, else two lines at the widest the frame allows), centred in the frame
        // the dock leaves free. Recomputed only when the text or the free width changes.
        void Layout()
        {
            var panel = LessonPanel.Instance;
            if (!panel) return;
            float full = panel.Root.rect.width, free = full - panel.Cover;
            if (body.text == laidOut && Mathf.Approximately(free, laidFree)) return;
            laidOut = body.text;
            laidFree = free;
            float clear = panel.Cover > 0f ? SideClear : AvatarClear;   // without a dock the bar stays clear of the avatar
            float maxBox = Mathf.Min(LessonPanel.CaptionMax, free - 2f * clear);
            float maxText = maxBox - 2f * PadX;
            float one = body.GetPreferredValues(body.text, 99999f, 0f).x;
            bool wraps = one > maxText;
            float textWidth = wraps ? maxText : Mathf.Ceil(one) + 4f;
            float speakerWidth = speaker.GetPreferredValues(speaker.text, 99999f, 0f).x + (bolt.gameObject.activeSelf ? 20f : 0f);
            bodySize.preferredHeight = bodySize.minHeight = LineHeight(wraps ? 2 : 1);
            box.sizeDelta = new Vector2(Mathf.Max(textWidth, speakerWidth) + 2f * PadX, box.sizeDelta.y);
            box.anchoredPosition = new Vector2(free / 2f, LessonPanel.BottomInset);
        }

        // progress (0..1) through the spoken line picks which two-line page is on screen: a page comes up once the
        // voice has covered the characters before it, so a 3-line text (pages of 2 + 1 lines) turns at two thirds.
        public void Show(object shownBy, string speakerName, string text, float progress = 0)
        {
            owner = shownBy;
            Apply();
            if (!Enabled) return;
            if (speakerName != speakerShown)   // the caps once per speaker, not every frame
            {
                speakerShown = speakerName;
                speaker.text = (speakerName ?? "").ToUpperInvariant();
                bolt.gameObject.SetActive(speaker.text.StartsWith("WATTSON"));
            }
            if (body.text != text)
            {
                body.text = text;
                body.pageToDisplay = 1;
                Layout();
                Canvas.ForceUpdateCanvases();
                body.ForceMeshUpdate();
            }
            var info = body.textInfo;
            int pages = Mathf.Max(1, info.pageCount);
            float spoken = Mathf.Clamp01(progress) * info.characterCount;
            int page = 1;
            for (int p = 1; p < pages && p < info.pageInfo.Length; p++)
                if (spoken >= info.pageInfo[p].firstCharacterIndex) page = p + 1;
            body.pageToDisplay = Mathf.Clamp(page, 1, pages);
        }

        // Only the caller that put the current line up may take it down.
        public void Hide(object shownBy)
        {
            if (!ReferenceEquals(owner, shownBy)) return;
            owner = null;
            Apply();
        }
    }
}
