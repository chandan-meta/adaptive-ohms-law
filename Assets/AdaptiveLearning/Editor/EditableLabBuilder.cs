using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace AdaptiveOhmsLaw.Editor
{
    public static class EditableLabBuilder
    {
        const string Root="Assets/AdaptiveLearning/";
        const string Folder=Root+"Prefabs/LabEquipment/";
        static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;if(parent!=null)t.SetParent(parent,false);return t;}
        static Material Mat(string name,Color color){string path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(m,path);}return m;}
        static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 scale,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=m;return o;}
        static GameObject Extract(string name){var src=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name==name);var copy=UnityEngine.Object.Instantiate(src.gameObject);copy.name=name;copy.transform.position=Vector3.zero;copy.transform.rotation=Quaternion.identity;
            foreach(var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))if(behaviour!=null&&behaviour.GetType().Namespace=="AdaptiveOhmsLaw")UnityEngine.Object.DestroyImmediate(behaviour);
            foreach(var t in copy.GetComponentsInChildren<Transform>().ToArray())if(t.name=="ApparatusLabel"||t.name=="DigitalReadout"||t.name=="SupplyReadout")UnityEngine.Object.DestroyImmediate(t.gameObject);
            var prefab=PrefabUtility.SaveAsPrefabAsset(copy,Folder+name+".prefab");UnityEngine.Object.DestroyImmediate(copy);return prefab;}
        // Explicit authoring utility; normal Play never rebuilds the scene.
        static bool testing;
        public static void BuildAndValidate(){testing=true;Build();LabValidation.Run();}
        public static void Build(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring scenes.");Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(Root+"Scenes/Scene_10_BecomeTheExperimenter.unity");
            string[] names={"VoltageSource","Ammeter","Resistor","Voltmeter","Switch","Tabletop"};var prefabs=names.Select(Extract).ToArray();
            // Adapt the existing wire/resistor geometry into the screenplay's straight test wire.
            var wire=PrefabUtility.LoadPrefabContents(Folder+"Resistor.prefab");foreach(var t in wire.GetComponentsInChildren<Transform>().ToArray())if(t!=wire.transform&&(t.name.StartsWith("Band")||t.name=="ApparatusLabel"))UnityEngine.Object.DestroyImmediate(t.gameObject);
            var body=wire.GetComponentsInChildren<Transform>().First(t=>t.name=="ResistorBody");body.localPosition=new Vector3(0,.25f,0);body.localRotation=Quaternion.Euler(0,0,90);body.localScale=new Vector3(.045f,.8f,.045f);
            var stand=wire.GetComponentsInChildren<Transform>().First(t=>t.name=="ResistorStand");stand.localScale=new Vector3(2,.12f,.55f);
            wire.name="NichromeWireXY";prefabs[2]=PrefabUtility.SaveAsPrefabAsset(wire,Folder+"NichromeWireXY.prefab");PrefabUtility.UnloadPrefabContents(wire);
            for(int scene=8;scene<=9;scene++)Author(scene,prefabs);
            AssetDatabase.SaveAssets();File.WriteAllText("Logs/EditableLab-build.txt","COMPLETE: Scene 8 and 9 saved with editable equipment, room, camera, controls and lead routes.");
            if(Application.isBatchMode&&!testing)EditorApplication.Exit(0);
        }
        static void Author(int number,GameObject[] prefabs){
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var systems=Group("LearningSystems");var controller=systems.gameObject.AddComponent<LabLessonController>();var view=systems.gameObject.AddComponent<LabSceneView>();view.Build();
            var environment=Group("Environment");var equipment=Group("Equipment - edit models here");var annotations=Group("Instrument labels");
            // Replace only the previous generated representations, retaining the shared interface.
            foreach(var device in view.Devices)UnityEngine.Object.DestroyImmediate(device.gameObject);
            foreach(var oldName in new[]{"Modern bench","Lab wall","Nichrome XY"}){var old=scene.GetRootGameObjects().FirstOrDefault(o=>o.name==oldName);if(old!=null)UnityEngine.Object.DestroyImmediate(old);}
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.renderMode==RenderMode.WorldSpace&&!view.Terminals.Any(b=>b.transform.IsChildOf(canvas.transform))&&!view.Key.transform.IsChildOf(canvas.transform)&&!view.Ammeter.transform.IsChildOf(canvas.transform)&&!view.Voltmeter.transform.IsChildOf(canvas.transform))UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            var table=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[5]);table.name="Workbench";table.transform.SetParent(environment);table.transform.position=new Vector3(0,-.2f,1.2f);table.transform.localScale=new Vector3(9,.4f,4.8f);
            var wood=Mat("LabWarmWood",new Color(.28f,.15f,.075f));table.GetComponent<Renderer>().sharedMaterial=wood;
            var steel=Mat("LabFrame",new Color(.075f,.1f,.12f));var wall=Mat("LabWall",new Color(.11f,.18f,.22f));var floor=Mat("LabFloor",new Color(.08f,.10f,.12f));
            foreach(float x in new[]{-3.9f,3.9f})foreach(float z in new[]{-.7f,3.1f})Cube("Bench leg",environment,new Vector3(x,-1.15f,z),new Vector3(.22f,1.8f,.22f),steel);
            Cube("Floor",environment,new Vector3(0,-2.1f,1),new Vector3(22,.2f,22),floor);Cube("Rear wall",environment,new Vector3(0,1.2f,6),new Vector3(18,6.5f,.2f),wall);Cube("Left wall",environment,new Vector3(-8,1.2f,0),new Vector3(.2f,6.5f,12),wall);
            for(int i=0;i<3;i++){Cube("Wall panel "+i,environment,new Vector3(-4+i*3,1.6f,5.85f),new Vector3(2.4f,1.9f,.1f),steel);Cube("Shelf "+i,environment,new Vector3(-4+i*3,.55f,5.5f),new Vector3(2.6f,.12f,.8f),wood);}
            var camera=view.Camera;camera.orthographic=false;camera.fieldOfView=46;camera.transform.position=new Vector3(0,5.8f,-8);camera.transform.LookAt(new Vector3(0,.2f,1.25f));camera.nearClipPlane=.1f;camera.farClipPlane=60;RenderSettings.fog=false;
            var light=UnityEngine.Object.FindFirstObjectByType<Light>();light.shadows=LightShadows.Soft;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-30,0);light.transform.SetParent(environment);
            var positions=new[]{new Vector3(-3,.4f,2.1f),new Vector3(-.6f,.48f,2.5f),new Vector3(1.8f,.12f,2.5f),new Vector3(1.6f,.48f,-.15f),new Vector3(-2.8f,.1f,-.15f)};
            view.Devices=new Transform[5];view.FocusMarkers=new GameObject[5];
            string[] labels={"DC SOURCE","AMMETER  /  I (A)","NICHROME WIRE  XY","VOLTMETER  /  V (V)","KEY / SWITCH"};
            for(int i=0;i<5;i++){var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);obj.transform.SetParent(equipment);obj.transform.position=positions[i];view.Devices[i]=obj.transform;
                var labelPos=positions[i]+new Vector3(0,.9f,0);if(i==3)labelPos=positions[i]+new Vector3(1.65f,.6f,0);if(i==4)labelPos=positions[i]+new Vector3(-1.45f,.5f,0);var label=WorldLabel(view,i==3?"VOLTMETER":i==4?"KEY":labels[i],annotations,labelPos,camera);view.FocusMarkers[i]=view.Area("Highlight",label.transform,new Vector2(0,0),Vector2.one,new Color(.1f,.7f,.7f,.2f)).gameObject;view.FocusMarkers[i].transform.SetAsFirstSibling();view.FocusMarkers[i].GetComponent<Image>().raycastTarget=false;view.FocusMarkers[i].SetActive(false);
            }
            MoveCanvas(view.Ammeter,view.Devices[1],new Vector3(0,-.27f,-.48f),new Vector2(.9f,.28f),Quaternion.identity,camera);view.Ammeter.fontSize=18;view.Ammeter.color=new Color(.02f,.08f,.1f);
            MoveCanvas(view.Voltmeter,view.Devices[3],new Vector3(0,-.27f,-.48f),new Vector2(.9f,.28f),Quaternion.identity,camera);view.Voltmeter.fontSize=18;view.Voltmeter.color=new Color(.02f,.08f,.1f);
            MoveCanvas(view.Key.GetComponentInChildren<Text>(),view.Devices[4],new Vector3(1.15f,.3f,0),new Vector2(1.1f,.48f),camera.transform.rotation,camera);
            int[] deviceMap={0,0,1,1,2,2,3,3,4,4};var anchors=new Transform[10];
            for(int i=0;i<10;i++){int d=deviceMap[i];float x=(i%2==0?-.43f:.43f);if(d==2)x=i==4?-.82f:.82f;
                var parent=view.Terminals[i].GetComponentInParent<Canvas>().transform;parent.SetParent(view.Devices[d],false);parent.localPosition=new Vector3(x,d==4?.15f:.1f,d==2?-.65f:d==4?-1.0f:-1.65f);parent.rotation=camera.transform.rotation;((RectTransform)parent).sizeDelta=new Vector2(78,68);parent.localScale=Vector3.one*.01f;
                var anchor=Group(CircuitAssemblyModel.Terminals[i]+" - wire anchor",view.Devices[d]);
                if(d==0){anchor.position=view.Devices[0].Find(i==0?"TerminalPos":"TerminalNeg").position;parent.localPosition=new Vector3(i==0?.45f:-.45f,.1f,-1.65f);}
                else {anchor.localPosition=new Vector3(x,d==2?.23f:.08f,d==2?0:-.48f);var post=UnityEngine.Object.Instantiate(view.Devices[0].Find("TerminalPos").gameObject,anchor);post.name="Binding post";post.transform.localPosition=Vector3.zero;post.transform.localScale=new Vector3(.12f,.07f,.12f);}
                anchors[i]=anchor;view.Terminals[i].GetComponentInChildren<Text>().fontSize=18;if(d==0)view.Terminals[i].GetComponentInChildren<Text>().text=i==0?"+":"−";
                if(number==8){view.Terminals[i].gameObject.SetActive(d==2);if(d==2)view.Terminals[i].image.color=Color.clear;}
            }
            var routes=Group("Circuit leads - editable waypoints");int[,] pairs={{0,2},{3,4},{5,8},{9,1},{6,4},{7,5}};view.WireRoutes=new LabWireRoute[6];
            var leadMat=Mat("LabLeadTeal",new Color(.08f,.62f,.68f));
            for(int i=0;i<6;i++){var root=Group("Lead "+CircuitAssemblyModel.Terminals[pairs[i,0]]+" to "+CircuitAssemblyModel.Terminals[pairs[i,1]],routes);var route=root.gameObject.AddComponent<LabWireRoute>();route.a=pairs[i,0];route.b=pairs[i,1];var p=anchors[route.a].position;var q=anchors[route.b].position;var dropA=Group("Route start on bench",root);dropA.position=new Vector3(p.x,.07f,p.z-.2f);var dropB=Group("Route end on bench",root);dropB.position=new Vector3(q.x,.07f,q.z-.2f);route.points=new[]{anchors[route.a],dropA,dropB,anchors[route.b]};route.line=root.gameObject.AddComponent<LineRenderer>();route.line.sharedMaterial=leadMat;route.line.startWidth=route.line.endWidth=.045f;route.line.numCapVertices=4;route.line.numCornerVertices=4;route.line.useWorldSpace=true;route.Show();route.line.enabled=false;view.WireRoutes[i]=route;}
            view.KeyLever=view.Devices[4].Find("LeverHinge");view.Key.gameObject.SetActive(number==9);
            var characters=Group("Characters");var wattson=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Wattson/Wattson.prefab"));wattson.transform.SetParent(characters);wattson.transform.position=new Vector3(3.7f,1.8f,2.7f);wattson.transform.LookAt(camera.transform);wattson.transform.localScale=Vector3.one*.85f;
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            string name=number==8?"Scene_08_OurLabIntroduction":"Scene_09_BuildTheCircuit";var data=AssetDatabase.LoadAssetAtPath<LabLessonDefinition>(Root+"ScriptableObjects/Dialogue/"+name+".asset");controller.Configure(data,AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Wattson/Wattson.prefab"),AssetDatabase.LoadAssetAtPath<WattsonServiceSettings>(Root+"ScriptableObjects/WattsonService.asset"));controller.SetSceneObjects(view,wattson.GetComponent<WattsonController>());view.Title.text=data.title;
            // Persist materials made by the shared editor UI factory so no transient references remain.
            foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var m in r.sharedMaterials)if(m!=null&&!AssetDatabase.Contains(m)){string path=AssetDatabase.GenerateUniqueAssetPath(Root+"Materials/LabAuthored.mat");AssetDatabase.CreateAsset(m,path);}
            EditorSceneManager.SaveScene(scene,Root+"Scenes/"+name+".unity");
        }
        static Canvas WorldLabel(LabSceneView view,string text,Transform parent,Vector3 pos,Camera camera){var c=new GameObject(text,typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.worldCamera=camera;c.transform.SetParent(parent);c.transform.position=pos;c.transform.rotation=camera.transform.rotation;c.transform.localScale=Vector3.one*.01f;((RectTransform)c.transform).sizeDelta=new Vector2(210,35);var t=view.Label("Label",c.transform,text,19,Vector2.zero,Vector2.one);t.alignment=TextAnchor.MiddleCenter;return c;}
        static void MoveCanvas(Text text,Transform parent,Vector3 pos,Vector2 size,Quaternion rotation,Camera camera){var c=text.GetComponentInParent<Canvas>();c.transform.SetParent(parent,false);c.transform.localPosition=pos;c.transform.rotation=rotation;c.transform.localScale=Vector3.one*.01f;((RectTransform)c.transform).sizeDelta=size*100;c.worldCamera=camera;}
    }
}




