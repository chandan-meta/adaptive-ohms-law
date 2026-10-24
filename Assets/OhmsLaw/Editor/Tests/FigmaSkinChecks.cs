using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OhmsLaw.Editor
{
    public static class FigmaSkinChecks
    {
        const string ReportPath = "Logs/FigmaUI/runtime-binding-checks.txt";
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Ohm's Law/Tests/Figma imported skin")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Run Figma skin checks outside Play mode.");
            var report = new StringBuilder("Figma sprite bindings in an isolated preview scene\n");
            int passed = 0;
            void Check(string name, bool ok)
            {
                report.AppendLine((ok ? "PASS " : "FAIL ") + name);
                if (!ok) throw new InvalidOperationException(name);
                passed++;
            }

            var previousTheme = UI.Theme;
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject root = null;
            try
            {
                UI.Theme = AssetDatabase.FindAssets("t:ModulePalette").Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<ModulePalette>).FirstOrDefault(palette => palette && palette.font);
                Check("Existing module palette and fallback font load without rebuilding", UI.Theme && UI.Theme.font);
                Check("Figma skin is available", FigmaSkin.Available);
                foreach (string name in new[]
                {
                    "Panel", "PrimaryNormal", "PrimaryHover", "PrimaryPressed", "PrimaryDisabled",
                    "SecondaryNormal", "SecondaryHover", "SecondaryPressed", "SecondaryDisabled",
                    "RaisedNormal", "RaisedSelected", "RaisedCorrect", "RaisedIncorrect", "Focus",
                    "MeasurementVoltage", "MeasurementCurrent", "MeasurementResistance",
                    "TitleBanner", "TitleBannerTablet", "TitleShade", "TitleStart", "TitleStartTablet"
                })
                    Check("Export loads: " + name, FigmaSkin.Get(name));
                foreach (string path in Directory.GetFiles("Assets/OhmsLaw/Shared/Resources/FigmaUI", "Icon-*.png"))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    Check("Icon uses the exported sprite: " + name, Icons.Get(name.Substring(5)) == FigmaSkin.Get(name));
                }

                var body = FigmaSkin.BodyFont;
                var display = FigmaSkin.DisplayFont;
                Check("Inter body font loads", body && body.name.StartsWith("Inter", StringComparison.Ordinal));
                Check("Space Grotesk display font loads", display && display.name.StartsWith("SpaceGrotesk", StringComparison.Ordinal));
                Check("Body font retains the module's scientific fallback", body.fallbackFontAssetTable.Contains(UI.Theme.font));
                Check("Body and fallback render lesson glyphs including Ω",
                    body.HasCharacters("Ohm’s Law 0.60 A 3.0 V 5 Ω V = I × R ΔV ÷ ΔI →", out uint[] missing, true, true));
                Check("Display and fallback render scientific units", display.HasCharacter('Ω', true, true));

                root = new GameObject("Figma skin verification", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(root, scene);
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = new Vector2(1920, 1080);
                var card = UI.Rect("Card check", rect, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(400, 260));
                LessonPanel.Card(card);
                var panel = card.GetComponent<Image>();
                Check("Card uses the direct Panel export", panel && panel.sprite == FigmaSkin.Get("Panel"));
                Check("Card preserves white artwork tint and raycast target", panel.color == Color.white && panel.raycastTarget);
                Check("Card has a separate inactive focus Edge", card.Find("Edge") && !card.Find("Edge").GetComponent<Image>().enabled);
                Check("Card omits procedural shine and glow", !card.Find("Shine") && !card.Find("Glow") && !card.Find("Halo") && !card.Find("Plate"));

                int dirtyVertices = 0;
                UnityEngine.Events.UnityAction onDirtyVertices = () => dirtyVertices++;
                panel.RegisterDirtyVerticesCallback(onDirtyVertices);
                Canvas.ForceUpdateCanvases();
                dirtyVertices = 0;
                FigmaSkin.Apply(panel, "Panel");
                Check("Unchanged Figma artwork does not schedule another mesh rebuild", dirtyVertices == 0);
                panel.UnregisterDirtyVerticesCallback(onDirtyVertices);

                foreach (var kind in new[] { LessonPanel.Kind.Primary, LessonPanel.Kind.Secondary, LessonPanel.Kind.Raised })
                {
                    int clicks = 0;
                    var button = LessonPanel.Pill("Button " + kind, rect, "Record reading", () => clicks++, kind);
                    ((RectTransform)button.transform).sizeDelta = new Vector2(280, 64);
                    var image = button.GetComponent<Image>();
                    var feedback = button.GetComponent<HudButton>();
                    Check(kind + " button uses its exported normal art", image && image.sprite == FigmaSkin.Get(kind + "Normal"));
                    Check(kind + " button keeps a white interactive image", image.color == Color.white && image.raycastTarget);
                    button.onClick.Invoke();
                    Check(kind + " actual Button invokes its original callback once", clicks == 1);
                    Invoke(feedback, "Awake");
                    foreach (string state in kind == LessonPanel.Kind.Raised
                        ? new[] { "Normal", "Selected", "Correct", "Incorrect" }
                        : new[] { "Normal", "Hover", "Pressed", "Disabled" })
                    {
                        feedback.figmaState = state;
                        Invoke(feedback, "Update");
                        Check(kind + " " + state + " selects the direct PNG", image.sprite == FigmaSkin.Get(kind + state) && image.color == Color.white);
                    }
                    feedback.figmaState = "Normal";
                    Invoke(feedback, "Update");
                    var normalSprite = image.sprite;
                    image.color = Color.magenta;
                    Invoke(feedback, "Update");
                    Check(kind + " cached artwork still restores an external tint", image.sprite == normalSprite && image.color == Color.white);
                    image.sprite = FigmaSkin.Get("Panel");
                    Invoke(feedback, "Update");
                    Check(kind + " cached artwork restores a displaced sprite", image.sprite == normalSprite);
                    image.RegisterDirtyVerticesCallback(onDirtyVertices);
                    Canvas.ForceUpdateCanvases();
                    dirtyVertices = 0;
                    Invoke(feedback, "Update");
                    Check(kind + " idle feedback does not rebuild its image mesh", dirtyVertices == 0);
                    image.UnregisterDirtyVerticesCallback(onDirtyVertices);
                    if (kind == LessonPanel.Kind.Raised)
                    {
                        feedback.figmaState = null;
                        feedback.active = true;
                        Invoke(feedback, "Update");
                        Check("Selected wire choice uses the exported gold action", image.sprite == FigmaSkin.Get("PrimaryNormal"));
                    }
                    else
                    {
                        feedback.figmaState = null;
                        if (kind == LessonPanel.Kind.Secondary)
                        {
                            feedback.active = true;
                            Invoke(feedback, "Update");
                            Check("Active secondary toggle uses the exported gold action", image.sprite == FigmaSkin.Get("PrimaryNormal"));
                            Check("Active secondary foreground stays readable", feedback.figmaForeground.All(graphic => !graphic || graphic.color == LessonPanel.Ink));
                            feedback.active = false;
                            Invoke(feedback, "Update");
                            Check("Secondary toggle restores its normal artwork and foreground", image.sprite == FigmaSkin.Get("SecondaryNormal")
                                && feedback.figmaForeground.All(graphic => !graphic || graphic.color == LessonPanel.Paper));
                        }
                        button.interactable = false;
                        Invoke(feedback, "Update");
                        Check(kind + " disabled state follows Button.interactable", image.sprite == FigmaSkin.Get(kind + "Disabled"));
                    }
                }

                var build = typeof(TitleScreen).GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic);
                Check("Existing title screen factory is available", build != null);
                var title = (TitleScreen)build.Invoke(null, new object[] { rect });
                Check("Title shade is the direct PNG", title.GetComponent<Image>().sprite == FigmaSkin.Get("TitleShade"));
                Invoke(title, "LayoutExported");
                var block = title.transform.Find("Block");
                var start = block.Find("Start").GetComponent<Button>();
                Check("Desktop title banner is the direct PNG", block.GetComponent<Image>().sprite == FigmaSkin.Get("TitleBanner"));
                Check("Desktop start button is the direct PNG", start.GetComponent<Image>().sprite == FigmaSkin.Get("TitleStart"));
                Check("Start retains a real raycastable Button and feedback", start.GetComponent<Image>().raycastTarget && start.GetComponent<HudButton>());
                Check("Static title lettering is not recreated in TMP", !title.GetComponentsInChildren<TMP_Text>(true)
                    .Any(text => text.text.IndexOf("OHM", StringComparison.OrdinalIgnoreCase) >= 0 || text.text.IndexOf("START JOURNEY", StringComparison.OrdinalIgnoreCase) >= 0));
                rect.sizeDelta = new Vector2(1024, 768);
                Invoke(title, "LayoutExported");
                Check("Tablet title selects its exported banner", block.GetComponent<Image>().sprite == FigmaSkin.Get("TitleBannerTablet"));
                Check("Tablet title selects its exported start button", start.GetComponent<Image>().sprite == FigmaSkin.Get("TitleStartTablet"));
                report.AppendLine($"RESULT PASS ({passed} checks)");
                Debug.Log(report.ToString());
            }
            catch (Exception error)
            {
                report.AppendLine("RESULT FAIL: " + error.GetBaseException().Message);
                throw;
            }
            finally
            {
                if (root) UnityEngine.Object.DestroyImmediate(root);
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                UI.Theme = previousTheme;
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, report.ToString());
            }
        }

        static void Invoke(object target, string method)
        {
            var callback = target.GetType().GetMethod(method, PrivateInstance);
            if (callback == null) throw new MissingMethodException(target.GetType().Name, method);
            callback.Invoke(target, null);
        }
    }
}
