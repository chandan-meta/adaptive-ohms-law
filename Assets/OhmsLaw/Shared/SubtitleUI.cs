using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Captions for every spoken line, scripted or AI, in the HUD's bottom bar (LessonPanel.Caption): the speaker in
    // small cyan caps, the line in large white text, two lines at a time; longer text pages through, each page coming
    // up when the voice reaches its first word. The bar fades in and out (0.25 s) and CC OFF hides it.
    public class SubtitleUI : MonoBehaviour
    {
        public static SubtitleUI Instance { get; private set; }

        RectTransform box;
        CanvasGroup group;
        TextMeshProUGUI speaker, body;
        object owner;
        bool shown;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("ohm.subtitles", 1) == 1;
            set { PlayerPrefs.SetInt("ohm.subtitles", value ? 1 : 0); if (Instance) Instance.Apply(); }
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
            layout.padding = new RectOffset(26, 26, 14, 16);
            layout.spacing = 4f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            speaker = UI.Label(box, "", LessonPanel.SpeakerFont, UI.Theme ? UI.Theme.wattsonAccent : Color.cyan, TextAlignmentOptions.TopLeft);
            speaker.characterSpacing = 4f;
            speaker.fontStyle = FontStyles.Bold;
            LessonPanel.Fixed(speaker.rectTransform, LessonPanel.SpeakerFont * 1.3f);
            body = UI.Label(box, "", LessonPanel.CaptionFont, new Color(0.98f, 0.97f, 0.94f), TextAlignmentOptions.TopLeft);
            body.overflowMode = TextOverflowModes.Page;
            // Two caption lines per page, whatever the bar's width: the label's height is fixed, the layout keeps it.
            LessonPanel.Fixed(body.rectTransform, LessonPanel.CaptionFont * 1.2f * 2f + 4f);
            Apply();
        }

        void Apply() => shown = Enabled && owner != null;

        void Update()
        {
            float target = shown ? 1f : 0f;
            group.alpha = ReducedMotion.On ? target : Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / 0.25f);
        }

        // progress (0..1) through the spoken line picks which two-line page is on screen: a page comes up once the
        // voice has covered the characters before it, so a 3-line text (pages of 2 + 1 lines) turns at two thirds.
        public void Show(object shownBy, string speakerName, string text, float progress = 0)
        {
            owner = shownBy;
            Apply();
            if (!Enabled) return;
            speaker.text = (speakerName ?? "").ToUpperInvariant();
            if (body.text != text)
            {
                body.text = text;
                body.pageToDisplay = 1;
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
