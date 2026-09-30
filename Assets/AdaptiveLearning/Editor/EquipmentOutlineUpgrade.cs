#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdaptiveOhmsLaw.Editor
{
    // Bakes editable, collider-free presentation meshes; original apparatus is untouched.
    [InitializeOnLoad]
    public static class EquipmentOutlineUpgrade
    {
        const string Folder = "Assets/AdaptiveLearning/Art/Interface/EquipmentOutlines";
        static EquipmentOutlineUpgrade()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("OutlineReview", false))
                    new GameObject("Outline review").AddComponent<EquipmentOutlineReview>();
            };
        }
        [MenuItem("Adaptive Learning/Presentation/Apply Orange Equipment Outlines")]
        public static void Run()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var materials = new List<Material>();
            materials.Add(Material("Mask", 3000, 0, 0, true));
            materials.Add(Material("Soft outer glow", 3001, 11, .07f));
            materials.Add(Material("Middle glow", 3002, 8, .12f));
            materials.Add(Material("Inner glow", 3003, 5, .25f));
            materials.Add(Material("Orange edge", 3004, 2.5f, 1));
            var report = new List<string>();
            foreach (var path in Directory.GetFiles("Assets/AdaptiveLearning/Scenes", "Scene_*.unity").OrderBy(x=>x))
            {
                if (path.Contains("06_")) continue;
                var scene = EditorSceneManager.OpenScene(path);
                var all = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
                var director = all.Select(x=>x.GetComponent<LessonCinematicDirector>()).FirstOrDefault(x=>x);
                if (!director) continue;
                var serialized = new SerializedObject(director);
                var targets = serialized.FindProperty("focusTargets");
                for (int i=0; i<targets.arraySize; i++)
                {
                    var entry = targets.GetArrayElementAtIndex(i);
                    var subject = (CinematicSubject)entry.FindPropertyRelative("subject").enumValueIndex;
                    if (subject == CinematicSubject.Graph || subject == CinematicSubject.Learner) continue;
                    var old = entry.FindPropertyRelative("highlight").objectReferenceValue as GameObject;
                    if (old) UnityEngine.Object.DestroyImmediate(old);
                    string[] names = subject == CinematicSubject.Source ? new[]{"DCSource", "VoltageSource"} :
                        subject == CinematicSubject.Ammeter ? new[]{"Ammeter"} :
                        subject == CinematicSubject.Voltmeter ? new[]{"Voltmeter"} : new[]{"NichromeWireXY"};
                    var equipment = all.FirstOrDefault(t=>t && names.Contains(t.name) && !(t is RectTransform) && t.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled && r.gameObject.activeInHierarchy));
                    if (!equipment) { report.Add(scene.name+" / "+subject+": no apparatus model"); entry.FindPropertyRelative("highlight").objectReferenceValue=null; continue; }
                    var filters = equipment.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.gameObject.activeInHierarchy && f.sharedMesh && f.GetComponent<MeshRenderer>() && f.GetComponent<MeshRenderer>().enabled &&
                        !f.name.ToLowerInvariant().Contains("needle") && !f.name.ToLowerInvariant().Contains("niddle") && !f.name.ToLowerInvariant().Contains("glass")).ToArray();
                    var group = new GameObject("Orange outline - " + subject); group.transform.SetParent(equipment,false);
                    int count = 0;
                    foreach(var filter in filters)
                    {
                        var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                        mesh.name = scene.name+"-"+subject+"-"+count;
                        var matrix = group.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                        var vertices = mesh.vertices;
                        for(int v=0;v<vertices.Length;v++) vertices[v]=matrix.MultiplyPoint3x4(vertices[v]);
                        mesh.vertices=vertices; mesh.RecalculateNormals();
                        // Weld only the outline normals: preserves original hard edges and instrument faces.
                        var normals=mesh.normals; var sums=new Dictionary<Vector3,Vector3>();
                        for(int v=0;v<vertices.Length;v++) { sums.TryGetValue(vertices[v],out var sum); sums[vertices[v]]=sum+normals[v]; }
                        for(int v=0;v<vertices.Length;v++) normals[v]=sums[vertices[v]].normalized;
                        mesh.normals=normals; mesh.RecalculateBounds();
                        string meshPath=Folder+"/"+mesh.name+".asset";
                        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(existing) { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing; }
                        else AssetDatabase.CreateAsset(mesh,meshPath);
                        foreach(var material in materials)
                        {
                            var shell=new GameObject(filter.name+" - "+material.name,typeof(MeshFilter),typeof(MeshRenderer));
                            shell.transform.SetParent(group.transform,false); shell.GetComponent<MeshFilter>().sharedMesh=mesh;
                            var renderer=shell.GetComponent<MeshRenderer>(); renderer.sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();
                            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
                            renderer.lightProbeUsage=LightProbeUsage.Off; renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
                        }
                        count++;
                    }
                    group.SetActive(false);
                    entry.FindPropertyRelative("highlight").objectReferenceValue=group;
                    report.Add(scene.name+" / "+subject+": "+count+" outlined meshes");
                }
                serialized.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets(); Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/Equipment-outline-upgrade.txt",report);
        }
        static Material Material(string name,int queue,float width,float alpha,bool mask=false)
        {
            string path=Folder+"/"+name+".mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(Shader.Find("AdaptiveLearning/EquipmentOutline")); AssetDatabase.CreateAsset(material,path); }
            material.renderQueue=queue; material.SetFloat("_Width",width); material.SetColor("_Color",new Color(1,.32f,.015f,alpha));
            material.SetFloat("_StencilComp",mask?8:6); material.SetFloat("_StencilPass",mask?2:0); material.SetFloat("_ColorMask",mask?0:15);
            EditorUtility.SetDirty(material); return material;
        }
        public static void RunAndBuild() { Run(); WebLessonBuild.Run(); }
        public static void RunAndReview()
        {
            Run(); SessionState.SetBool("OutlineReview",true);
            // Do not activate the existing validation runner; it is registered only by its own entry point scene.
            SessionState.SetBool("CinematicValidation",false);
            StudentProfileManager.ValidationStore = new MemoryProfileStore();
            EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_08_OurLabIntroduction.unity");
            Scene07Validation.SetResolution(1440,810); EditorApplication.isPlaying=true;
        }
    }
    public sealed class EquipmentOutlineReview : MonoBehaviour
    {
        IEnumerator Start()
        {
            Application.runInBackground=true;
            yield return new WaitForSecondsRealtime(5);
            var director=FindFirstObjectByType<LessonCinematicDirector>(); director.SkipCurrentExplanation();
            yield return new WaitForSecondsRealtime(2);
            var homePosition=Camera.main.transform.position; var homeRotation=Camera.main.transform.rotation;
            director.PresentSpokenText("The ammeter measures current.");
            yield return new WaitForSecondsRealtime(2);
            Camera.main.transform.SetPositionAndRotation(homePosition,homeRotation);
            yield return null;
            ScreenCapture.CaptureScreenshot("Logs/Orange-outline-ammeter.png");
            yield return new WaitForSecondsRealtime(1);
            director.SkipCurrentExplanation(); yield return new WaitForSecondsRealtime(2);
            ScreenCapture.CaptureScreenshot("Logs/Orange-outline-off.png");
            yield return new WaitForSecondsRealtime(1);
            SessionState.SetBool("OutlineReview",false); EditorApplication.isPlaying=false; EditorApplication.Exit(0);
        }
    }
}
#endif
