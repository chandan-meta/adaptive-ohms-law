using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad]
    public static class WarmLabVisualReview
    {
        static WarmLabVisualReview() { EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("WarmUIReview", false))
                new GameObject("Warm UI visual review").AddComponent<WarmLabVisualReviewRunner>();
        }; }
        public static void Run()
        {
            SessionState.SetBool("WarmUIReview", true);
            EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_08_OurLabIntroduction.unity");
            Scene07Validation.SetResolution(1920, 1080); EditorApplication.isPlaying = true;
        }
    }
    public sealed class WarmLabVisualReviewRunner : MonoBehaviour
    {
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); Application.runInBackground = true;
            Directory.CreateDirectory("Logs/WarmUI");
            foreach (var path in Directory.GetFiles("Assets/AdaptiveLearning/Scenes", "Scene_*.unity").OrderBy(p => p))
            {
                SceneManager.LoadScene(Path.GetFileNameWithoutExtension(path));
                yield return new WaitForSecondsRealtime(5);
                FindFirstObjectByType<LessonCinematicDirector>()?.SkipCurrentExplanation();
                yield return new WaitForSecondsRealtime(1);
                foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1920,1200), new Vector2Int(1440,1080) })
                {
                    Scene07Validation.SetResolution(size.x, size.y);
                    yield return new WaitForSecondsRealtime(.5f);
                    foreach(var viewport in FindObjectsByType<LearningViewport>(FindObjectsSortMode.None)) viewport.Refresh();
                    yield return null;
                    ScreenCapture.CaptureScreenshot("Logs/WarmUI/" + Path.GetFileNameWithoutExtension(path) + "-" + size.x + "x" + size.y + ".png");
                    yield return new WaitForSecondsRealtime(.3f);
                }
                if (SceneManager.GetActiveScene().name.Contains("08_"))
                {
                    var chat = FindFirstObjectByType<WattsonChatUI>(); chat.SetOpen(true);
                    yield return null;
                    ScreenCapture.CaptureScreenshot("Logs/WarmUI/Chat-1440x1080.png");
                    yield return new WaitForSecondsRealtime(.5f); chat.SetOpen(false);
                }
            }
            File.WriteAllText("Logs/WarmUI/Review.txt", "Captured all seven scene openings at 16:9, 16:10 and 4:3, plus chat. Inspect images before claiming visual pass.");
            SessionState.SetBool("WarmUIReview", false);
            EditorApplication.isPlaying = false; EditorApplication.Exit(0);
        }
    }
}
