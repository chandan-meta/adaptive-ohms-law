using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace OhmsLaw.Editor
{
    // Exercise the real InputSystem UI bindings with temporary devices; no physical touchscreen is claimed.
    public static class CinematicPointerProbe
    {
        public static void Begin() => ModuleFlow.Instance.StartCoroutine(Run());

        static IEnumerator Run()
        {
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var mouse = InputSystem.AddDevice<Mouse>("Cinematic QA mouse");
            var touch = InputSystem.AddDevice<Touchscreen>("Cinematic QA touch");
            bool original = SubtitleUI.Enabled;
            string report = "Synthetic device input through InputSystemUIInputModule; physical touch hardware not tested.\n";
            try
            {
                Canvas.ForceUpdateCanvases();
                var rect = (RectTransform)LessonPanel.Instance.Controls.Find("UI_ControlButton CC");
                var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                yield return null;
                yield return null;
                bool mousePassed = SubtitleUI.Enabled != original;
                report += (mousePassed ? "PASS" : "FAIL") + " mouse click toggles the actual CC button\n";
                bool beforeTouch = SubtitleUI.Enabled;
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = point });
                yield return null;
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = point });
                yield return null;
                yield return null;
                report += (SubtitleUI.Enabled != beforeTouch ? "PASS" : "FAIL") + " touch tap toggles the actual CC button\n";
            }
            finally
            {
                SubtitleUI.Enabled = original;
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(touch);
                settings.backgroundBehavior = background;
                File.WriteAllText("Logs/CinematicPolish/pointer-input.txt", report);
            }
        }
    }
}
