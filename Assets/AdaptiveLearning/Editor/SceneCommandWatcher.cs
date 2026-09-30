using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class SceneCommandWatcher
    {
        static SceneCommandWatcher()
        {
            EditorApplication.update += CheckCommands;
        }

        static void CheckCommands()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Automatically build Scene 07 if not yet present
            if (!File.Exists(Scene07Builder.ScenePath))
            {
                try
                {
                    Scene07Builder.Build();
                    Scene07Builder.EnsureScene06Transition();
                }
                catch (Exception e)
                {
                    Debug.LogError("Error building Scene 07: " + e);
                }
            }

            // Command hooks for test automation
            if (File.Exists("Logs/scene07-build.req"))
            {
                try { File.Delete("Logs/scene07-build.req"); } catch { }
                Scene07Builder.Build();
                Scene07Builder.EnsureScene06Transition();
            }

            if (File.Exists("Logs/scene07-validate.req"))
            {
                try { File.Delete("Logs/scene07-validate.req"); } catch { }
                Scene07Validation.Run();
            }

            if (File.Exists("Logs/scene06-validate.req"))
            {
                try { File.Delete("Logs/scene06-validate.req"); } catch { }
                Scene06Validation.Run();
            }

            if (File.Exists("Logs/scene07-build-player.req"))
            {
                try { File.Delete("Logs/scene07-build-player.req"); } catch { }
                Scene07BuildPlayer.BuildWindows();
            }

            if (File.Exists("Logs/scene12-13-build.req"))
            {
                try { File.Delete("Logs/scene12-13-build.req"); } catch { }
                Scene12And13Builder.BuildAll();
            }

            if (File.Exists("Logs/scene12-13-validate.req"))
            {
                try { File.Delete("Logs/scene12-13-validate.req"); } catch { }
                Scene12And13Validation.Run();
            }
        }
    }
}
