using TMPro;
using UnityEngine;

namespace OhmsLaw
{
    // Bottom-centre subtitles for every spoken line, scripted or AI. Two lines at a time; longer text pages through,
    // each page coming up when the voice reaches its first word.
    public class SubtitleUI : MonoBehaviour
    {
        public static SubtitleUI Instance { get; private set; }

        // Box spans y 40..236 with a 210 px left inset; other HUD elements (battery control) stay above it. The right inset
        // leaves ~50 px of air to the Ask Wattson button (x >= 1722), even while the spotlight cue scales it up.
        const float SideInset = 210f, RightSide = 250f, Bottom = 40f;
        RectTransform box;
        TextMeshProUGUI speaker, body;
        object owner;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("ohm.subtitles", 1) == 1;
            set { PlayerPrefs.SetInt("ohm.subtitles", value ? 1 : 0); if (Instance) Instance.box.gameObject.SetActive(value && Instance.owner != null); }
        }

        void Awake()
        {
            Instance = this;
            box = UI.Stretch("Subtitles", transform);
            box.anchorMax = new Vector2(1, 0);
            box.offsetMin = new Vector2(SideInset, Bottom);
            box.offsetMax = new Vector2(-RightSide, UI.SubtitleTop);
            UI.Panel(box, UI.HudColor, true, 22f).raycastTarget = false;

            speaker = UI.Label(box, "", UI.SpeakerFont, UI.Theme ? UI.Theme.wattsonAccent : Color.cyan, TextAlignmentOptions.TopLeft);
            speaker.rectTransform.offsetMin = new Vector2(36, 0);
            speaker.rectTransform.offsetMax = new Vector2(-36, -10);
            speaker.characterSpacing = 4f;

            // 196 px box - 10/44 insets = 142 px: two 60 px lines of the 44 px body fit, a third pages (TYPOGRAPHY.md).
            body = UI.Label(box, "", UI.SubtitleFont, Color.white, TextAlignmentOptions.Center);
            body.rectTransform.offsetMin = new Vector2(36, 10);
            body.rectTransform.offsetMax = new Vector2(-36, -44);
            body.overflowMode = TextOverflowModes.Page;
            box.gameObject.SetActive(false);
        }

        // progress (0..1) through the spoken line picks which two-line page is on screen: a page comes up once the
        // voice has covered the characters before it, so a 3-line text (pages of 2 + 1 lines) turns at two thirds.
        public void Show(object shownBy, string speakerName, string text, float progress = 0)
        {
            owner = shownBy;
            box.gameObject.SetActive(Enabled);
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
            box.gameObject.SetActive(false);
        }
    }
}
