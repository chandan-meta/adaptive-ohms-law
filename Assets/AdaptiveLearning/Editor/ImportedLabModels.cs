using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace AdaptiveOhmsLaw.Editor
{
    public static class ImportedLabModels
    {
        const string Root="Assets/AdaptiveLearning/Art/ImportedOhmsLab/";
        const string Prefabs="Assets/AdaptiveLearning/Prefabs/ImportedLabEquipment/";
        public static void FinishLayout()
        {
            foreach(string scene in new[]{"Scene_08_OurLabIntroduction","Scene_09_BuildTheCircuit"})
            {
                var opened=EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/"+scene+".unity");
                var view=Object.FindFirstObjectByType<LabSceneView>();
                foreach(int d in new[]{1,3})
                {
                    var text=d==1?view.Ammeter:view.Voltmeter;var canvas=text.GetComponentInParent<Canvas>();
                    canvas.transform.position=new Vector3(view.Devices[d].position.x,.95f,view.Devices[d].position.z-1.30f);
                    text.fontSize=14;text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
                }
                view.FocusMarkers[0].transform.parent.position=new Vector3(-3,1.85f,2.1f);
                EditorSceneManager.SaveScene(opened);
            }
            LabValidation.Run();
        }
        static Bounds BoundsOf(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>();
            var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;
        }
        static Material Convert(Material old)
        {
            if(old==null)return null;
            string path=Root+"URP/"+old.name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=old.name;
            var texture=old.HasProperty("_MainTex")?old.GetTexture("_MainTex"):null;
            m.SetTexture("_BaseMap",texture);
            m.SetColor("_BaseColor",old.HasProperty("_Color")?old.GetColor("_Color"):Color.white);
            m.SetFloat("_Smoothness",.28f);m.SetFloat("_Metallic",old.name.Contains("Chrome")?.65f:.05f);
            if(old.HasProperty("_BumpMap"))m.SetTexture("_BumpMap",old.GetTexture("_BumpMap"));
            if(m.GetTexture("_BumpMap")!=null)m.EnableKeyword("_NORMALMAP");
            m.enableInstancing=true;AssetDatabase.CreateAsset(m,path);return m;
        }
        static GameObject Make(GameObject source,string name,string[] children,float width,float yaw=0)
        {
            var root=new GameObject(name);var visual=new GameObject("Imported model - editable meshes");visual.transform.SetParent(root.transform,false);
            foreach(string child in children){var t=source.GetComponentsInChildren<Transform>().Single(x=>x.name==child);var copy=Object.Instantiate(t.gameObject,visual.transform,true);copy.name=child;}
            if(name=="NichromeWireXY")
            {
                var rod=visual.transform.Find("Rod_");var left=visual.transform.Find("Wire_Connector_03");var right=visual.transform.Find("Wire_Connector_02");
                rod.position=new Vector3((left.position.x+right.position.x)*.5f,left.position.y+.035f,(left.position.z+right.position.z)*.5f);
            }
            foreach(var r in root.GetComponentsInChildren<Renderer>())
            {
                // The legacy glass shader is opaque in URP. Preserve the glass mesh but hide it so the dial remains legible.
                if(r.name.StartsWith("Glass")){r.enabled=false;continue;}
                r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();
            }
            visual.transform.rotation=Quaternion.Euler(0,yaw,0);
            var b=BoundsOf(root);visual.transform.localScale=Vector3.one*(width/b.size.x);
            b=BoundsOf(root);visual.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
            if(name=="Ammeter"||name=="Voltmeter")root.AddComponent<LabMeterVisual>().Configure(root.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("niddle_grp")),name=="Ammeter");
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+name+".prefab");Object.DestroyImmediate(root);return prefab;
        }
        public static void ReplaceAndValidate()
        {
            Directory.CreateDirectory(Prefabs);Directory.CreateDirectory(Root+"URP");AssetDatabase.Refresh();
            foreach(string name in new[]{"Ammeter_Diff","VoltMeter_Diff"})
            {
                var old=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+name+".mat");var material=Convert(old);
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"OhmsLaw_circuit_Final.fbm/"+name+(name.StartsWith("Ammeter")?".png":".jpg")));
                EditorUtility.SetDirty(material);
            }
            var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"OhmsLaw_circuit_Final.fbx"));
            var supply=Make(source,"DCSource",new[]{"Power_Supply_Grp"},1.9f);
            var ammeter=Make(source,"Ammeter",new[]{"Ammeter_Grp"},1.15f);
            var voltmeter=Make(source,"Voltmeter",new[]{"Voltmeter_Grp"},1.15f);
            var key=Make(source,"Switch",new[]{"Switch_Grp"},1.5f);
            Object.DestroyImmediate(source);
            source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Ohmslaw_v02.fbx"));
            var wire=Make(source,"NichromeWireXY",new[]{"Rod_","Wire_Connector_02","Wire_Connector_03"},2f);
            Object.DestroyImmediate(source);
            source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Table.fbx"));
            var table=Make(source,"Workbench",new[]{"Wood","Legs"},9);
            Object.DestroyImmediate(source);
            var models=new[]{supply,ammeter,wire,voltmeter,key};
            foreach(string scene in new[]{"Scene_08_OurLabIntroduction","Scene_09_BuildTheCircuit"})
            {
                var opened=EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/"+scene+".unity");
                var view=Object.FindFirstObjectByType<LabSceneView>();
                view.Devices[2].position+=Vector3.forward*.7f;
                for(int d=0;d<5;d++)
                {
                    var device=view.Devices[d];
                    foreach(var r in device.GetComponentsInChildren<Renderer>())r.enabled=false;
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(models[d]);model.transform.SetParent(device,false);
                    model.transform.position=new Vector3(device.position.x,.015f,device.position.z);
                    if(d==4)
                    {
                        var button=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="Button");
                        var hinge=new GameObject("Switch interaction pivot").transform;hinge.SetParent(button.parent,false);hinge.position=button.position;
                        button.SetParent(hinge,true);view.KeyLever=hinge;hinge.localRotation=Quaternion.Euler(0,0,-30);
                    }
                    // Keep the lesson's exact digital reading below the authentic printed dial.
                    if(d==1||d==3)
                    {
                        var text=d==1?view.Ammeter:view.Voltmeter;var canvas=text.GetComponentInParent<Canvas>();
                        canvas.transform.position=model.transform.position+new Vector3(0,.53f,-.95f);canvas.transform.rotation=view.Camera.transform.rotation;
                        text.color=Color.white;text.fontSize=20;
                    }
                    for(int i=0;i<10;i++)
                    {
                        var anchor=device.Find(CircuitAssemblyModel.Terminals[i]+" - wire anchor");if(anchor==null)continue;
                        float x=(i%2==0?-.38f:.38f),y=.15f,z=-.48f;
                        if(d==0){x=i==0?-.08f:-.58f;y=.27f;z=-.70f;}
                        if(d==1||d==3){x=i%2==0?.46f:-.46f;y=.18f;z=-.63f;}
                        if(d==2){x=i==4?-.86f:.86f;y=.12f;z=0;}
                        if(d==4){x=i==8?.71f:-.71f;y=.27f;z=0;}
                        anchor.position=model.transform.position+new Vector3(x,y,z);
                    }
                }
                view.FocusMarkers[0].transform.parent.position+=new Vector3(0,.45f,-.9f);
                foreach(var route in view.WireRoutes){var a=route.points[0].position;var b=route.points[3].position;route.points[1].position=new Vector3(a.x,.06f,a.z-.2f);route.points[2].position=new Vector3(b.x,.06f,b.z-.2f);route.Show();route.line.enabled=false;}
                var oldTable=opened.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>()).First(t=>t.name=="Workbench");
                var env=oldTable.parent;foreach(var r in oldTable.GetComponentsInChildren<Renderer>())r.enabled=false;
                foreach(var t in env.GetComponentsInChildren<Transform>().Where(t=>t.name=="Bench leg")){var r=t.GetComponent<Renderer>();if(r!=null)r.enabled=false;}
                var bench=(GameObject)PrefabUtility.InstantiatePrefab(table);bench.name="Imported workbench - editable";bench.transform.SetParent(env,false);
                var bounds=BoundsOf(bench);bench.transform.localScale=new Vector3(1,2.05f/bounds.size.y,4.8f/bounds.size.z);bounds=BoundsOf(bench);bench.transform.position+=new Vector3(-bounds.center.x,-bounds.max.y,1.2f-bounds.center.z);
                EditorSceneManager.SaveScene(opened);
            }
            AssetDatabase.SaveAssets();LabValidation.Run();
        }
        public static void Inspect()
        {
            var report=new StringBuilder();
            foreach(var path in Directory.GetFiles(Root,"*.fbx"))
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var obj=Object.Instantiate(asset);
                report.AppendLine("MODEL "+path);
                foreach(var t in obj.GetComponentsInChildren<Transform>())
                {
                    var r=t.GetComponent<Renderer>();
                    report.AppendLine(t.name+" parent="+(t.parent==null?"ROOT":t.parent.name)+" pos="+t.position+" rot="+t.eulerAngles+" scale="+t.lossyScale+(r==null?"":" bounds="+r.bounds+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m==null?"NULL":m.name))));
                }
                Object.DestroyImmediate(obj);
            }
            File.WriteAllText("Logs/Imported-model-inspection.txt",report.ToString());
            EditorApplication.Exit(0);
        }
    }
}
