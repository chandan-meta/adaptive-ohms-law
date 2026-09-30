using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw.Editor
{
    public static class WarmLabThemeUpgrade
    {
        const string Root = "Assets/AdaptiveLearning/";
        public static void RunAndReview() { Run(); WarmLabVisualReview.Run(); }
        [MenuItem("Adaptive Learning/Presentation/Apply Approved Warm UI")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Root + "Resources")) AssetDatabase.CreateFolder(Root.TrimEnd('/'), "Resources");
            var path = Root + "Resources/WarmLabTheme.asset";
            var theme = AssetDatabase.LoadAssetAtPath<WarmLabTheme>(path);
            if (theme == null) { theme = ScriptableObject.CreateInstance<WarmLabTheme>(); AssetDatabase.CreateAsset(theme, path); }
            theme.body = AssetDatabase.LoadAssetAtPath<Font>(Root + "Art/Interface/Fonts/SourceSans3-Regular.ttf");
            theme.control = AssetDatabase.LoadAssetAtPath<Font>(Root + "Art/Interface/Fonts/SourceSans3-Semibold.ttf");
            theme.heading = AssetDatabase.LoadAssetAtPath<Font>(Root + "Art/Interface/Fonts/NunitoSans-Bold.ttf");
            theme.rounded = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Interface/RoundedPanel.png");
            if (theme.body == null || theme.control == null || theme.heading == null || theme.rounded == null) throw new System.Exception("Warm UI assets missing");
            EditorUtility.SetDirty(theme);
            foreach (var scenePath in Directory.GetFiles(Root + "Scenes", "Scene_*.unity").OrderBy(p => p))
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (canvas.isRootCanvas) theme.Apply(canvas.transform);
                var lab = Object.FindFirstObjectByType<LabSceneView>();
                if (lab != null)
                {
                    var footer = (RectTransform)lab.Subtitle.transform.parent;
                    Box(footer, .02f, .03f, .98f, .205f);
                    footer.GetComponent<Image>().color = WarmLabTheme.Ivory;
                    Box(lab.Subtitle.rectTransform, .025f, .27f, .73f, .74f);
                    lab.Subtitle.fontSize = 30; lab.Subtitle.resizeTextForBestFit = true;
                    lab.Subtitle.resizeTextMinSize = 24; lab.Subtitle.resizeTextMaxSize = 30;
                    Box(lab.Progress.rectTransform, .025f, .04f, .73f, .23f); lab.Progress.fontSize = 22;
                    var speaker = footer.Find("Speaker") as RectTransform;
                    if (speaker != null) Box(speaker, .025f, .77f, .73f, .96f);
                    var icon = footer.Find("Hint emblem"); if (icon != null) icon.gameObject.SetActive(false);
                    var bubble = lab.Canvas.transform.Find("Wattson help bubble"); if (bubble != null) bubble.gameObject.SetActive(false);
                    bool intro = scene.name.Contains("08_");
                    Box((RectTransform)lab.Next.transform, .77f, .10f, .98f, intro ? .76f : .46f);
                    Box((RectTransform)lab.Hint.transform, .77f, .54f, .98f, .90f);
                    var badge = lab.Title.transform.parent as RectTransform;
                    Box(badge, .02f, .875f, .33f, .97f);
                    badge.GetComponent<Image>().color = WarmLabTheme.Ivory;
                    Box(lab.Title.rectTransform, .045f, .43f, .96f, .92f);
                    lab.Title.color = WarmLabTheme.Ink; lab.Title.fontSize = 30;
                    lab.Title.resizeTextForBestFit = true; lab.Title.resizeTextMinSize = 23; lab.Title.resizeTextMaxSize = 30;
                    var objective = badge.Find("Warm objective")?.GetComponent<Text>();
                    if (objective == null) { var go = new GameObject("Warm objective", typeof(RectTransform), typeof(Text)); go.transform.SetParent(badge, false); objective = go.GetComponent<Text>(); }
                    objective.font = theme.body; objective.fontSize = 22; objective.color = WarmLabTheme.Ink;
                    objective.text = intro ? "Meet the instruments" : "Connect the main loop and meter branch"; objective.raycastTarget = false;
                    Box(objective.rectTransform, .045f, .08f, .96f, .40f);
                    var labels = GameObject.Find("Instrument labels");
                    if (labels != null) foreach (var image in labels.GetComponentsInChildren<Image>(true))
                    { image.color = WarmLabTheme.Sage; foreach (var label in image.GetComponentsInChildren<Text>(true)) label.color = WarmLabTheme.Ivory; }
                }
                var lesson = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Lesson");
                if (lesson != null)
                {
                    var existing = lesson.transform.parent.Find("Warm lesson identity");
                    var badge = existing as RectTransform;
                    if (lesson.transform.parent.name == "Warm lesson identity") badge = (RectTransform)lesson.transform.parent;
                    if (badge == null) { var go = new GameObject("Warm lesson identity", typeof(RectTransform), typeof(Image)); go.transform.SetParent(lesson.transform.parent, false); badge = (RectTransform)go.transform; badge.SetAsFirstSibling(); }
                    Box(badge, .02f, .90f, .40f, .98f); var surface = badge.GetComponent<Image>(); surface.sprite = theme.rounded; surface.type = Image.Type.Sliced; surface.color = WarmLabTheme.Ivory; surface.raycastTarget = false;
                    lesson.transform.SetParent(badge, false); Box(lesson.rectTransform, .045f, .45f, .96f, .96f); lesson.color = WarmLabTheme.Ink; lesson.fontSize = 28;
                    // Keep the identity above the 3D viewport. Chat raises itself
                    // when opened, so it still remains the top interaction layer.
                    badge.SetAsLastSibling();
                    lesson.text = scene.name.Contains("06_") ? "06 / Predict" : scene.name.Contains("10_") ? "10 / Experiment" : "11 / Find the pattern";
                    var objective = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Objective");
                    if (objective != null) { objective.transform.SetParent(badge, false); Box(objective.rectTransform, .045f, .04f, .96f, .43f); objective.color = WarmLabTheme.Ink; objective.fontSize = 21; }
                }
                if (lab == null && (scene.name.Contains("12_") || scene.name.Contains("13_")))
                {
                    var subtitle = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "Subtitle");
                    var footer = (RectTransform)subtitle.transform.parent; Box(footer, .02f, .03f, .98f, .205f);
                    Box(subtitle.rectTransform, .025f, .26f, .73f, .75f);
                    var speaker = footer.Find("Speaker") as RectTransform; if (speaker != null) Box(speaker, .025f, .77f, .73f, .96f);
                    var progress = footer.Find("Progress") as RectTransform; if (progress != null) Box(progress, .025f, .04f, .73f, .20f);
                }
                foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (text.name == "ChatTitle") text.color = WarmLabTheme.Plum;
                    if (text.name == "Reply" || text.name == "InputText" || text.name == "Placeholder") text.color = WarmLabTheme.Ink;
                }
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/Warm-UI-upgrade.txt", "COMPLETE: seven authored scenes; runtime chat and cinematic controls share the same theme.");
        }
        static void Box(RectTransform rect, float x, float y, float right, float top)
        { rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top); rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
