#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class LabValidation
    {
        static int stage,frames;
        static double deadline;
        static readonly System.Collections.Generic.List<string> results=new System.Collections.Generic.List<string>();
        static LabValidation(){EditorApplication.update+=Tick;}
        [MenuItem("Adaptive Learning/Validate Lab Scenes 08 and 09")]
        public static void Run(){stage=frames=0;deadline=0;results.Clear();Directory.CreateDirectory("Logs");SessionState.SetBool("LabValidation",true);SessionState.SetBool("LabValidationBatch",Application.isBatchMode);EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_08_OurLabIntroduction.unity");var authored=UnityEngine.Object.FindFirstObjectByType<LabSceneView>();if(authored==null||authored.Devices.Length!=5||authored.Devices.Any(d=>d==null)||authored.Camera.orthographic)throw new Exception("Editable 3D lab objects missing before Play");if(authored.Devices.Any(d=>!PrefabUtility.IsPartOfPrefabInstance(d.gameObject)))throw new Exception("Equipment must remain prefab instances");EditorApplication.isPlaying=true;}
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);results.Add("PASS "+text);File.WriteAllLines("Logs/Lab-validation.txt",results);}
        static void Tick(){
            if(!SessionState.GetBool("LabValidation",false))return;
            if(!EditorApplication.isPlaying)return;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+80;
            try{
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Runtime validation timeout");
                if(++frames<20)return;
                var controller=UnityEngine.Object.FindFirstObjectByType<LabLessonController>();if(controller==null)return;
                if(stage==0){
                    Check(SceneManager.GetActiveScene().name=="Scene_08_OurLabIntroduction","Scene 8 loads");
                    Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"One learner camera");
                    Check(UnityEngine.Object.FindFirstObjectByType<WattsonController>()!=null,"Wattson present");
                    var next=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="Continue");
                    Check(next.interactable,"Introduction navigation enabled");Capture("Scene08-Lab",1440,1080);
                    for(int i=0;i<5;i++)next.onClick.Invoke();stage=1;frames=0;return;
                }
                if(stage==1){
                    // Wait for the authored camera handoff before measuring the
                    // exploration home pose; a mid-cinematic pose is not home.
                    var director=UnityEngine.Object.FindFirstObjectByType<LessonCinematicDirector>();
                    if(director!=null&&(director.ShotActive||director.CameraTransitioning)){
                        if(director.ShotActive)director.SkipCurrentExplanation();
                        return;
                    }
                    Check(SceneManager.GetActiveScene().name=="Scene_09_BuildTheCircuit","Scene 8 continues to Scene 9");
                    var lab=UnityEngine.Object.FindFirstObjectByType<LabSceneView>();
                    Check(lab.Devices.All(d=>d.GetComponentsInChildren<MeshFilter>().Any(m=>AssetDatabase.GetAssetPath(m.sharedMesh).Contains("ImportedOhmsLab"))),"All five instruments use imported meshes");
                    Check(lab.Devices.SelectMany(d=>d.GetComponentsInChildren<Renderer>()).Where(r=>r.enabled).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader.name.StartsWith("Universal Render Pipeline/"))),"Visible equipment uses URP materials");
                    var pointers=lab.Devices.SelectMany(d=>d.GetComponentsInChildren<Transform>()).Where(t=>t.name.StartsWith("niddle_grp")).ToArray();
                    var zeroRotations=pointers.Select(t=>t.localRotation).ToArray();Check(pointers.Length==2,"Two imported meter pointers present");
                    Check(!controller.Circuit.Close(),"Incomplete circuit cannot close");
                    controller.SelectTerminal(0);controller.SelectTerminal(1);Check(controller.Circuit.Count==0,"Source short circuit rejected");
                    foreach(var pair in new[]{new[]{0,2},new[]{3,4},new[]{5,8},new[]{9,1},new[]{6,4},new[]{7,5}}){controller.SelectTerminal(pair[0]);controller.SelectTerminal(pair[1]);}
                    Check(controller.Circuit.Complete,"Six correct leads complete topology");
                    controller.CloseCircuit();Check(controller.Circuit.Closed,"Completed circuit closes");
                    Check(pointers.Select((t,i)=>Quaternion.Angle(t.localRotation,zeroRotations[i])>1).All(x=>x),"Both meter pointers respond to closing the circuit");
                    var profile=UnityEngine.Object.FindFirstObjectByType<StudentProfileManager>().Profile;
                    Check(profile.completedActivities.Contains("Scene09BuildCircuit"),"Completion saved");Check(profile.circuitAssemblies.Last().mistakes==1,"Connection mistake recorded");
                    var orbit=Camera.main.GetComponent<LabCameraOrbit>();Check(orbit!=null,"Camera orbit available");var home=Camera.main.transform.position;
                    var pad=UnityEngine.Object.FindFirstObjectByType<LabOrbitJoystick>();Canvas.ForceUpdateCanvases();
                    var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=4,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,pad.rectTransform.TransformPoint(pad.rectTransform.rect.center+Vector2.right*60))};
                    pad.OnPointerDown(pointer);Check(pad.Value.x>.5f,"Joystick accepts touch or mouse position");pad.OnPointerUp(pointer);Check(pad.Value==Vector2.zero,"Joystick stops on release");
                    orbit.Orbit(Vector2.right,1);Check(Vector3.Distance(home,Camera.main.transform.position)>1,"Joystick orbit changes table viewpoint");Capture("Scene09-Orbit",1920,1080);orbit.ResetView();Check(Vector3.Distance(home,Camera.main.transform.position)<.001f,"Reset view restores original composition");
                    var chat=UnityEngine.Object.FindFirstObjectByType<WattsonChatUI>();chat.SetOpen(true);Check(chat.IsOpen,"Ask Wattson opens");orbit.Orbit(Vector2.right,1);Check(Vector3.Distance(home,Camera.main.transform.position)<.001f,"Camera stays still while chatting");
                    Check(UnityEngine.Object.FindFirstObjectByType<MicrophoneIcon>()!=null,"Visible microphone icon exists");Capture("Wattson-Microphone",1920,1080);
                    chat.SetOpen(false);Check(!chat.IsOpen,"Ask Wattson closes");
                    Check(UnityEngine.Object.FindObjectsByType<HoldToTalk>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Shared hold-to-talk available");
                    Capture("Scene09-Lab",1440,1080);Capture("Scene09-Wide",1920,1080);Capture("Scene09-Tablet",1280,800);stage=2;frames=0;return;
                }
                if(stage==2){results.Add("COMPLETE");File.WriteAllLines("Logs/Lab-validation.txt",results);SessionState.SetBool("LabValidation",false);EditorApplication.isPlaying=false;if(SessionState.GetBool("LabValidationBatch",false))EditorApplication.Exit(0);}
            }catch(Exception e){File.AppendAllText("Logs/Lab-validation.txt","\nFAIL "+e);SessionState.SetBool("LabValidation",false);EditorApplication.isPlaying=false;if(SessionState.GetBool("LabValidationBatch",false))EditorApplication.Exit(1);}
        }
        static void Capture(string name,int width,int height){
            var camera=Camera.main;var canvas=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.renderMode==RenderMode.ScreenSpaceOverlay);
            var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes("Logs/"+name+".png",image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
        }
        [MenuItem("Adaptive Learning/Open Scene 08 Our Lab")]
        public static void Open8()=>EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_08_OurLabIntroduction.unity");
        [MenuItem("Adaptive Learning/Open Scene 09 Build Circuit")]
        public static void Open9()=>EditorSceneManager.OpenScene("Assets/AdaptiveLearning/Scenes/Scene_09_BuildTheCircuit.unity");
    }
}
#endif



