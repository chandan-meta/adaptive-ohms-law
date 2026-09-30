#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw.Editor
{
    public static class Scene11ScientificAxesUpgrade
    {
        const string ScenePath = "Assets/AdaptiveLearning/Scenes/Scene_11_TurnDataIntoGraph.unity";

        [MenuItem("Adaptive Learning/Upgrade Scene 11 Scientific Axes")]
        public static void Apply()
        {
            Directory.CreateDirectory("Logs/ScientificAxesBackups");
            string backup = "Logs/ScientificAxesBackups/Scene_11_TurnDataIntoGraph.unity";
            if (!File.Exists(backup)) File.Copy(ScenePath, backup);

            var scene = EditorSceneManager.OpenScene(ScenePath);
            var texts = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Text>(true)).ToArray();
            Set(texts, "GraphTitle", "POTENTIAL DIFFERENCE AGAINST CURRENT");
            Set(texts, "XAxisTitle", "Current / A");
            Set(texts, "YAxisTitle", "Potential difference / V");
            for (int i = 0; i <= 3; i++)
            {
                Set(texts, "VoltageTick" + i, (i * .1f).ToString("0.00"));
                Set(texts, "CurrentTick" + i, i.ToString());
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/Scene11-scientific-axes.txt",
                "Scene 11 now plots current horizontally and potential difference vertically. " + DateTime.Now.ToString("u"));
        }

        static void Set(Text[] texts, string name, string value)
        {
            var text = texts.FirstOrDefault(x => x.name == name);
            if (text == null) throw new InvalidOperationException("Missing Scene 11 label: " + name);
            text.text = value;
            EditorUtility.SetDirty(text);
        }
    }
}
#endif
