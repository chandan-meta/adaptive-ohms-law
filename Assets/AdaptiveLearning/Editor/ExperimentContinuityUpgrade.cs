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
    public static class ExperimentContinuityUpgrade
    {
        [MenuItem("Adaptive Learning/Upgrade Five Reading Continuity")]
        public static void Apply()
        {
            foreach (var name in new[] { "Scene_10_BecomeTheExperimenter", "Scene_11_TurnDataIntoGraph" })
            {
                string path = "Assets/AdaptiveLearning/Scenes/" + name + ".unity";
                Directory.CreateDirectory("Logs/ContinuityBackups");
                string backup = "Logs/ContinuityBackups/" + name + ".unity";
                if (!File.Exists(backup)) File.Copy(path, backup);
                var scene = EditorSceneManager.OpenScene(path);
                var circuit = UnityEngine.Object.FindFirstObjectByType<SimpleCircuitModel>(FindObjectsInactive.Include);
                circuit.Configure(LessonExperimentData.Resistance);
                foreach (var knob in UnityEngine.Object.FindObjectsByType<RotaryVoltageControl>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                { var so = new SerializedObject(knob); so.FindProperty("maximum").floatValue = 5; so.ApplyModifiedPropertiesWithoutUndo(); }
                foreach (var meter in UnityEngine.Object.FindObjectsByType<MeterDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                { var so = new SerializedObject(meter); so.FindProperty("maxScaleValue").floatValue = so.FindProperty("defaultUnit").stringValue == "A" ? 3 : 5; so.ApplyModifiedPropertiesWithoutUndo(); }
                var table = UnityEngine.Object.FindFirstObjectByType<ObservationTableUI>(FindObjectsInactive.Include);
                var tableData = new SerializedObject(table);
                ExpandTextColumn(tableData.FindProperty("rowVoltageTexts"), table.transform, false);
                ExpandTextColumn(tableData.FindProperty("rowCurrentTexts"), table.transform, true);
                tableData.ApplyModifiedPropertiesWithoutUndo();
                foreach (var label in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Text>(true)))
                {
                    label.text = label.text.Replace("10 Ω", "5 Ω");
                    if (label.text.StartsWith("YOUR CIRCUIT")) label.text = "YOUR CIRCUIT / SAME WIRE";
                    if (label.text.Contains(" / 3")) label.text = label.text.Replace(" / 3", " / 5");
                }
                foreach (var label in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TextMesh>(true)))
                    label.text = label.text.Replace("10 Ω", "5 Ω");
                var plot = UnityEngine.Object.FindFirstObjectByType<GraphPlotView>(FindObjectsInactive.Include);
                if (plot != null) UpgradeGraph(plot);
                EditorSceneManager.SaveScene(scene);
            }
            ChangeLine("Scene07Dialogue", "intro_same_resistor", "We will keep the same wire at constant temperature and change only the potential difference. Collect readings at one, two, three, four and five volts.");
            ChangeLine("Scene07Dialogue", "guide_guided", "Close the switch, then turn the knob to record readings at each whole volt from one to five volts.");
            ChangeLine("Scene07Dialogue", "readings_complete", "All five readings recorded. Notice the pattern in your observation table.");
            ChangeLine("Scene07Dialogue", "evidence_supported_clue", "At one volt, current is 0.20 amperes. At three volts, current is 0.60 amperes. The current increased.");
            ChangeLine("Scene07Dialogue", "evidence_retry", "Compare the five readings and choose the answer that fits our data.");
            ChangeLine("Scene08Dialogue", "sample", "We are using clearly marked practice readings from an ideal five-ohm circuit at constant temperature.");
            ChangeLine("Scene08Dialogue", "pattern", "Your five points form a straight line. In this ideal model, zero volts also gives zero current, so the line passes through the origin.");
            ChangeLine("Scene08Dialogue", "question_hint", "At one volt we measured 0.20 amperes. At two volts we measured 0.40 amperes. Both values doubled. What relationship fits?");
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/Continuity-upgrade.txt", "Saved five-reading experiment and graph. " + DateTime.Now.ToString("u"));
        }

        static void ExpandTextColumn(SerializedProperty array, Transform table, bool current)
        {
            var template = (Text)array.GetArrayElementAtIndex(0).objectReferenceValue;
            int oldCount = array.arraySize;
            array.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                var label = i < oldCount ? (Text)array.GetArrayElementAtIndex(i).objectReferenceValue : UnityEngine.Object.Instantiate(template, template.transform.parent);
                label.name = array.name + "_Reading" + (i + 1);
                var oldParent = label.transform.parent;
                label.transform.SetParent(table, false);
                if (oldParent != table) oldParent.gameObject.SetActive(false);
                label.gameObject.SetActive(true);
                var rect = label.rectTransform;
                var min = new Vector2(current ? .52f : .05f, .29f + (4 - i) * .085f);
                var max = new Vector2(current ? .96f : .49f, min.y + .08f);
                rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
                label.text = "—"; label.fontSize = 21; label.alignment = TextAnchor.MiddleCenter; label.resizeTextForBestFit = true; label.resizeTextMinSize = 14;
                array.GetArrayElementAtIndex(i).objectReferenceValue = label;
            }
        }

        static void UpgradeGraph(GraphPlotView plot)
        {
            var so = new SerializedObject(plot);
            var grid = (GraphGridGraphic)so.FindProperty("grid").objectReferenceValue;
            grid.maxCurrent = 1.2f; grid.maxVoltage = 6f;
            var targets = so.FindProperty("targets");
            var template = (Button)targets.GetArrayElementAtIndex(0).objectReferenceValue;
            var old = Enumerable.Range(0, targets.arraySize).Select(i => (Button)targets.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            targets.arraySize = 25;
            for (int i = 0; i < 25; i++)
            {
                var button = i < old.Length ? old[i] : UnityEngine.Object.Instantiate(template, template.transform.parent);
                button.name = "Plot_" + (i % 5 + 1) + "_" + (i / 5 + 1);
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = new Vector2((i % 5 + 1) / 6f, (i / 5 + 1) / 6f);
                rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(64, 64);
                targets.GetArrayElementAtIndex(i).objectReferenceValue = button;
            }
            var rows = so.FindProperty("rows");
            var rowTemplate = (Button)rows.GetArrayElementAtIndex(0).objectReferenceValue;
            int oldRows = rows.arraySize; rows.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                var button = i < oldRows ? (Button)rows.GetArrayElementAtIndex(i).objectReferenceValue : UnityEngine.Object.Instantiate(rowTemplate, rowTemplate.transform.parent);
                button.name = "GraphReading" + i;
                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(.04f + i * .123f, .30f); rect.anchorMax = new Vector2(.157f + i * .123f, .365f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var label = button.GetComponentInChildren<Text>(); label.fontSize = 21; label.resizeTextForBestFit = true; label.resizeTextMinSize = 15;
                rows.GetArrayElementAtIndex(i).objectReferenceValue = button;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var all = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int axis = 0; axis < 2; axis++)
            {
                string prefix = axis == 0 ? "VoltageTick" : "CurrentTick";
                var tickTemplate = all.First(t => t.name == prefix + "0");
                for (int i = 0; i <= 5; i++)
                {
                    var tick = all.FirstOrDefault(t => t.name == prefix + i) ?? UnityEngine.Object.Instantiate(tickTemplate, tickTemplate.transform.parent);
                    tick.name = prefix + i; tick.text = axis == 0 ? (i * .2f).ToString("0.0") : i.ToString();
                    var rect = tick.rectTransform;
                    rect.SetParent(grid.transform, false);
                    rect.anchorMin = rect.anchorMax = axis == 0 ? new Vector2(i / 6f, 0) : new Vector2(0, i / 6f);
                    rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(64, 32);
                    rect.anchoredPosition = axis == 0 ? new Vector2(0, -24) : new Vector2(-38, 0);
                    tick.alignment = TextAnchor.MiddleCenter; tick.fontSize = 18; tick.gameObject.SetActive(true);
                }
            }
        }

        static void ChangeLine(string asset, string id, string text)
        {
            var sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>("Assets/AdaptiveLearning/ScriptableObjects/Dialogue/" + asset + ".asset");
            var line = sequence.Get(id); line.text = text; line.audio = null;
            line.duration = Mathf.Max(4f, text.Split(' ').Length / 2.5f);
            EditorUtility.SetDirty(sequence);
        }
    }
}
#endif
