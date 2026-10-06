using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Captions for every spoken line, scripted or AI, in the lesson panel's Caption block: the speaker and the text.
    // Two lines at a time; longer text pages through, each page coming up when the voice reaches its first word.
    public class SubtitleUI : MonoBehaviour
    {
        public static SubtitleUI Instance { get; private set; }

        RectTransform box;
        TextMeshProUGUI speaker, body;
        object owner;

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
            var layout = box.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 8, 10);
            layout.spacing = 0f;
            UI.Panel(box, UI.Alpha(Color.Lerp(UI.PanelColor, Color.black, 0.35f), 1f), true, 16f).raycastTarget = false;
            speaker = UI.Label(box, "", LessonPanel.SpeakerFont, UI.Theme ? UI.Theme.wattsonAccent : Color.cyan, TextAlignmentOptions.TopLeft);
            speaker.characterSpacing = 4f;
            speaker.fontStyle = FontStyles.Bold;
            LessonPanel.Fixed(speaker.rectTransform, LessonPanel.SpeakerFont * 1.4f); // never squeezed under the text
            body = UI.Label(box, "", LessonPanel.CaptionFont, Color.white, TextAlignmentOptions.TopLeft);
            body.overflowMode = TextOverflowModes.Page;
            // Two caption lines per page, whatever the panel width: the label's height is fixed, the layout keeps it.
            LessonPanel.Fixed(body.rectTransform, LessonPanel.CaptionFont * 1.25f * 2f + 6f);
            Apply();
        }

        void Apply() { if (box) box.gameObject.SetActive(Enabled && owner != null); }

        // progress (0..1) through the spoken line picks which two-line page is on screen: a page comes up once the
        // voice has covered the characters before it, so a 3-line text (pages of 2 + 1 lines) turns at two thirds.
        public void Show(object shownBy, string speakerName, string text, float progress = 0)
        {
            owner = shownBy;
            Apply();
            if (!Enabled) return;
            speaker.text = speakerName;
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
