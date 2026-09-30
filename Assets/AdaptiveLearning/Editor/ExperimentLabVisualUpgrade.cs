#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw.Editor
{
    public static class ExperimentLabVisualUpgrade
    {
        const string Root = "Assets/AdaptiveLearning/";
        public static void FinishPresentation()
        {
            foreach (string name in new[] { "Scene_10_BecomeTheExperimenter", "Scene_11_TurnDataIntoGraph" })
            {
                var scene = EditorSceneManager.OpenScene(Root + "Scenes/" + name + ".unity");
                var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var binding = Object.FindFirstObjectByType<ImportedExperimentMeters>();
                binding.BindReadouts(texts.First(t => t.name == "AmmeterReadout"), texts.First(t => t.name == "VoltmeterReadout"));
                EditorUtility.SetDirty(binding);
                var knob = Object.FindFirstObjectByType<RotaryVoltageControl>(FindObjectsInactive.Include);
                if (knob != null) { var so = new SerializedObject(knob); so.FindProperty("physicalScaleMaximum").floatValue = 12; so.ApplyModifiedPropertiesWithoutUndo(); }
                if (knob != null)
                {
                    var help = knob.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == "KnobHelp");
                    if (help != null) help.text = "DRAG TO TURN\n0–5 V • snaps at each whole volt";
                    var template = knob.GetComponentsInChildren<Text>(true).First(t => t.name == "Setting0");
                    for (int i = 0; i <= 5; i++)
                    {
                        var tick = knob.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == "Setting" + i);
                        if (tick == null) tick = Object.Instantiate(template, template.transform.parent);
                        tick.name = "Setting" + i; tick.text = i.ToString();
                        float angle = (135 - i * 54) * Mathf.Deg2Rad;
                        var point = new Vector2(.5f + Mathf.Cos(angle) * .61f, .5f + Mathf.Sin(angle) * .61f);
                        tick.rectTransform.anchorMin = point - new Vector2(.07f,.06f); tick.rectTransform.anchorMax = point + new Vector2(.07f,.06f);
                        tick.rectTransform.offsetMin = tick.rectTransform.offsetMax = Vector2.zero;
                    }
                }
                if (name.StartsWith("Scene_11"))
                {
                    var lab = GameObject.Find("Shared laboratory - editable");
                    foreach (var child in lab.GetComponentsInChildren<Transform>())
                        if (child.name == "Floor" || child.name == "Rear wall" || child.name == "Left wall") child.gameObject.SetActive(false);
                    foreach (var light in lab.GetComponentsInChildren<Light>()) if (light.type == LightType.Point) light.enabled = false;
                }
                EditorSceneManager.SaveScene(scene);
            }
        }
        [MenuItem("Adaptive Learning/Apply Shared Lab To Experiment And Graph")]
        public static void Apply()
        {
            var source = EditorSceneManager.OpenScene(Root + "Scenes/Scene_12_DiscoverResistance.unity");
            var environment = source.GetRootGameObjects().First(g => g.name == "Environment");
            var labPrefab = PrefabUtility.SaveAsPrefabAsset(environment, Root + "Prefabs/Learning/SharedLaboratory.prefab");
            foreach (string name in new[] { "Scene_10_BecomeTheExperimenter", "Scene_11_TurnDataIntoGraph" })
            {
                var path = Root + "Scenes/" + name + ".unity";
                Directory.CreateDirectory("Logs/BeforeSharedLab");
                if (!File.Exists("Logs/BeforeSharedLab/" + name + ".unity")) File.Copy(path, "Logs/BeforeSharedLab/" + name + ".unity");
                var scene = EditorSceneManager.OpenScene(path);
                if (GameObject.Find("Shared laboratory - editable") != null) continue;
                var model = Object.FindFirstObjectByType<SimpleCircuitModel>();
                var station = model.transform.parent;
                foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (renderer.GetComponentInParent<WattsonController>() == null) renderer.enabled = false;
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (canvas.renderMode == RenderMode.WorldSpace) canvas.enabled = false;
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) light.enabled = false;
                var lab = (GameObject)PrefabUtility.InstantiatePrefab(labPrefab);
                lab.name = "Shared laboratory - editable"; lab.transform.SetParent(station, false);
                var equipment = new GameObject("Imported equipment - edit models here").transform;
                equipment.SetParent(station, false);
                string[] prefabs = { "DCSource", "Ammeter", "NichromeWireXY", "Voltmeter", "Switch" };
                Vector3[] positions = { new Vector3(-3,.015f,2.1f), new Vector3(-.6f,.015f,2.5f), new Vector3(1.8f,.015f,2.5f), new Vector3(1.6f,.015f,-.15f), new Vector3(-2.8f,.015f,-.15f) };
                var devices = new GameObject[5];
                for (int i = 0; i < 5; i++)
                {
                    devices[i] = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/ImportedLabEquipment/" + prefabs[i] + ".prefab"));
                    devices[i].transform.SetParent(equipment, false); devices[i].transform.localPosition = positions[i];
                }
                var meters = equipment.gameObject.AddComponent<ImportedExperimentMeters>();
                meters.Configure(model, devices[1].GetComponent<LabMeterVisual>(), devices[3].GetComponent<LabMeterVisual>());
                var voltageRange = new SerializedObject(devices[3].GetComponent<LabMeterVisual>());
                voltageRange.FindProperty("fullScale").floatValue = 6; voltageRange.ApplyModifiedPropertiesWithoutUndo();
                var oldMeters = model.GetComponentsInChildren<MeterDisplay>(true);
                foreach (var old in oldMeters)
                {
                    var serialized = new SerializedObject(old);
                    bool current = serialized.FindProperty("defaultUnit").stringValue == "A";
                    var device = devices[current ? 1 : 3];
                    var readout = Label(device.transform, current ? "Current reading" : "Voltage reading — dial ×2", current ? "0.00 A" : "0.00 V  (scale ×2)", new Vector3(0,.12f,-.86f));
                    serialized.FindProperty("readoutText").objectReferenceValue = readout;
                    serialized.FindProperty("needle").objectReferenceValue = null;
                    serialized.FindProperty("defaultUnit").stringValue = current ? "A" : "V  (scale ×2)";
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var button = devices[4].GetComponentsInChildren<Transform>().First(t => t.name == "Button");
                var switchInteraction = model.GetComponentInChildren<CircuitSwitchInteraction>();
                switchInteraction.transform.position = devices[4].transform.position;
                switchInteraction.Configure(button, model);
                var knob = Object.FindFirstObjectByType<RotaryVoltageControl>(FindObjectsInactive.Include);
                if (knob != null)
                {
                    var so = new SerializedObject(knob);
                    so.FindProperty("physicalKnob").objectReferenceValue = devices[0].GetComponentsInChildren<Transform>().First(t => t.name == "Round_Switch");
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                Vector3 sp = positions[0] + new Vector3(-.08f,.27f,-.7f), sn = positions[0] + new Vector3(-.58f,.27f,-.7f);
                Vector3 ap = positions[1] + new Vector3(.46f,.18f,-.63f), an = positions[1] + new Vector3(-.46f,.18f,-.63f);
                Vector3 x = positions[2] + new Vector3(-.86f,.12f,0), y = positions[2] + new Vector3(.86f,.12f,0);
                Vector3 vp = positions[3] + new Vector3(.46f,.18f,-.63f), vn = positions[3] + new Vector3(-.46f,.18f,-.63f);
                Vector3 kp = positions[4] + new Vector3(.71f,.27f,0), kn = positions[4] + new Vector3(-.71f,.27f,0);
                Lead(equipment, "Source to key", sp, kp, true); Lead(equipment,"Key to ammeter",kn,ap,true);
                Lead(equipment,"Ammeter to X",an,x,false); Lead(equipment,"Y to source",y,sn,false);
                Lead(equipment,"Voltmeter across X",vp,x,true); Lead(equipment,"Voltmeter across Y",vn,y,false);
                Label(devices[2].transform, "Wire XY label", "NICHROME WIRE  X — Y", new Vector3(0,.25f,-.2f));
                RenderSettings.fog = false;
                foreach (var focus in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name.StartsWith("Focus - ")))
                {
                    int index = focus.name.EndsWith("Source") ? 0 : focus.name.EndsWith("Ammeter") ? 1 : focus.name.EndsWith("WireXY") ? 2 : focus.name.EndsWith("Voltmeter") ? 3 : -1;
                    if (index >= 0) focus.position = devices[index].transform.TransformPoint(new Vector3(0,.5f,0));
                }
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        static Text Label(Transform parent, string name, string text, Vector3 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas)); go.transform.SetParent(parent,false);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace; go.transform.localPosition = position; go.transform.localScale = Vector3.one * .003f;
            ((RectTransform)go.transform).sizeDelta = new Vector2(480,70);
            var child = new GameObject("Readout", typeof(RectTransform),typeof(Text)); child.transform.SetParent(go.transform,false);
            var label = child.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 30; label.color = Color.white; label.text=text; label.alignment=TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin=Vector2.zero; label.rectTransform.anchorMax=Vector2.one; label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            return label;
        }
        static void Lead(Transform parent, string name, Vector3 a, Vector3 b, bool positive)
        {
            var go = new GameObject(name, typeof(LineRenderer)); go.transform.SetParent(parent,false);
            var line=go.GetComponent<LineRenderer>(); line.useWorldSpace=false; line.positionCount=4; line.startWidth=line.endWidth=.035f;
            line.numCornerVertices=5; line.numCapVertices=4;
            line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+(positive?"ReferenceRedLead":"ReferenceBlackLead")+".mat");
            line.SetPositions(new[]{a,new Vector3(a.x,.08f,a.z-.25f),new Vector3(b.x,.08f,b.z-.25f),b});
        }
    }
}
#endif
