#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace AdaptiveOhmsLaw
{
    public class LessonContinuityRunner:MonoBehaviour
    {
        string outputPath=>UnityEditor.SessionState.GetBool("RotaryReview",false)?"Logs/Rotary-validation.txt":UnityEditor.SessionState.GetBool("GraphReview",false)?"Logs/Scene08-review-validation.txt":"Logs/Lesson-continuity-validation.txt";
        Touchscreen testTouch; Mouse testMouse;
        readonly List<InputDevice> suspendedPointers=new List<InputDevice>();
        InputSettings originalInputSettings,validationInputSettings;
        Touchscreen TouchDevice => testTouch!=null && testTouch.added ? testTouch : (testTouch=InputSystem.AddDevice<Touchscreen>());
        Mouse MouseDevice => testMouse!=null && testMouse.added ? testMouse : (testMouse=InputSystem.AddDevice<Mouse>());
        readonly List<string> results=new List<string>(),errors=new List<string>();
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;
            originalInputSettings=InputSystem.settings;validationInputSettings=Instantiate(originalInputSettings);
            validationInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            validationInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=validationInputSettings;
            // Real desktop motion must not replace the injected pointer halfway through a test drag.
            foreach(var device in InputSystem.devices.ToArray())if(device.enabled&&(device is Mouse||device is Touchscreen)){suspendedPointers.Add(device);InputSystem.DisableDevice(device);}
        }
        void Log(string message,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
            // Unity 6000.3 occasionally reports a SearchDatabase indexing exception
            // during batch startup. It is editor infrastructure, not lesson runtime.
            if((stack??"").Contains("UnityEditor.Search.SearchDatabase"))return;
            errors.Add(message);File.AppendAllText(outputPath,Environment.NewLine+"RUNTIME ERROR "+message);UnityEditor.EditorApplication.delayCall+=()=>UnityEditor.EditorApplication.isPlaying=false;
        }
        void Check(bool condition,string label)
        {
            results.Add((condition?"PASS ":"FAIL ")+label);File.WriteAllLines(outputPath,results);
            if(!condition){UnityEditor.EditorApplication.isPlaying=false;throw new Exception(label);}
        }
        Button B(string name)=>FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(b=>b.name==name);
        bool Ready(string name){var b=B(name);return b.isActiveAndEnabled&&b.interactable;}
        IEnumerator Until(Func<bool> predicate,string label,float timeout=30)
        {float deadline=Time.realtimeSinceStartup+timeout;while(!predicate()&&Time.realtimeSinceStartup<deadline)yield return null;Check(predicate(),label);}
        void Click(string name)
        {Check(Ready(name),"Enabled "+name);ExecuteEvents.Execute(B(name).gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);}
        IEnumerator Tap(string name,bool touch=false)
        {
            Check(Ready(name),(touch?"Touch ":"Mouse ")+name);var rt=(RectTransform)B(name).transform;var point=(Vector2)rt.TransformPoint(rt.rect.center);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);Check(hits.Count>0&&(hits[0].gameObject==B(name).gameObject||hits[0].gameObject.transform.IsChildOf(B(name).transform)),"Pointer reaches "+name+" ("+string.Join(",",hits.Select(h=>h.gameObject.name))+")");
            if(touch)
            {
                var device=TouchDevice;InputSystem.QueueStateEvent(device,new TouchState{touchId=1,position=point,phase=UnityEngine.InputSystem.TouchPhase.Began});yield return null;
                InputSystem.QueueStateEvent(device,new TouchState{touchId=1,position=point,phase=UnityEngine.InputSystem.TouchPhase.Ended});yield return null;
            }
            else
            {
                var device=MouseDevice;InputSystem.QueueStateEvent(device,new MouseState{position=point});yield return null;
                InputSystem.QueueStateEvent(device,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;InputSystem.QueueStateEvent(device,new MouseState{position=point});yield return null;
            }
            yield return null;
        }
        IEnumerator Rotate(float target,bool touch=false)
        {
            var knob=FindFirstObjectByType<RotaryVoltageControl>();Check(knob!=null&&knob.IsInteractable(),"Rotary control available");
            var rt=(RectTransform)knob.transform;var center=(Vector2)rt.TransformPoint(rt.rect.center);float radius=rt.rect.width*rt.lossyScale.x*.35f;
            float start=135-knob.Value*54,end=135-target*54;
            InputDevice device=touch?(InputDevice)TouchDevice:MouseDevice;
            int downs=0, drags=0, ups=0, pressedFrames=0;
            string moduleTrace="";
            var trace=knob.gameObject.AddComponent<EventTrigger>();
            foreach(var kind in new[]{EventTriggerType.PointerDown,EventTriggerType.Drag,EventTriggerType.PointerUp})
            {
                var entry=new EventTrigger.Entry{eventID=kind};
                entry.callback.AddListener(_=>{if(kind==EventTriggerType.PointerDown)downs++;else if(kind==EventTriggerType.Drag)drags++;else ups++;});
                trace.triggers.Add(entry);
            }
            var pointerField=typeof(RotaryVoltageControl).GetField("activePointer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            string beforePointer=Convert.ToString(pointerField.GetValue(knob));
            var startPoint = center + new Vector2(Mathf.Cos(start * Mathf.Deg2Rad), Mathf.Sin(start * Mathf.Deg2Rad)) * radius;
            var dialHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = startPoint }, dialHits);
            Check(dialHits.Count > 0 && (dialHits[0].gameObject == knob.gameObject || dialHits[0].gameObject.transform.IsChildOf(knob.transform)),
                "Pointer reaches rotary start for " + target + " V (" + string.Join(",", dialHits.Select(h => h.gameObject.name)) + ")");
            if (!touch)
            {
                if (testTouch != null && testTouch.added) InputSystem.ResetDevice(testTouch);
                InputSystem.ResetDevice(device);
                yield return null;
                // Allow the UI input module to switch back from its touch pointer
                // before injecting the new mouse press, as a real hover does.
                float angle = start * Mathf.Deg2Rad;
                InputSystem.QueueStateEvent(device, new MouseState { position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius });
                yield return null; yield return null;
            }
            for(int i=0;i<=24;i++){
                float angle=Mathf.Lerp(start,end,i/24f)*Mathf.Deg2Rad;var point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                if(touch)InputSystem.QueueStateEvent(device,new TouchState{touchId=1,position=point,phase=i==0?UnityEngine.InputSystem.TouchPhase.Began:UnityEngine.InputSystem.TouchPhase.Moved});
                else InputSystem.QueueStateEvent(device,new MouseState{position=point}.WithButton(MouseButton.Left));
                yield return null;
                if(!touch && MouseDevice.leftButton.isPressed)pressedFrames++;
                if(i==5)
                {
                    var module=EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    var hit=module.GetLastRaycastResult(device.deviceId);
                    moduleTrace=" moduleHit="+(hit.gameObject!=null?hit.gameObject.name:"none")+" screen="+hit.screenPosition+" raw="+(touch?Vector2.zero:MouseDevice.position.ReadValue())+" actions="+module.point.action.enabled+","+module.leftClick.action.enabled;
                }
            }
            var last=center+new Vector2(Mathf.Cos(end*Mathf.Deg2Rad),Mathf.Sin(end*Mathf.Deg2Rad))*radius;
            if(touch)InputSystem.QueueStateEvent(device,new TouchState{touchId=1,position=last,phase=UnityEngine.InputSystem.TouchPhase.Ended});else InputSystem.QueueStateEvent(device,new MouseState{position=last});
            yield return null;
            string pointerTrace=" down="+downs+" drag="+drags+" up="+ups+" capture="+beforePointer+" -> "+Convert.ToString(pointerField.GetValue(knob))+" pressedFrames="+pressedFrames+" deviceEnabled="+device.enabled+" focus="+EventSystem.current.isFocused+" background="+Application.runInBackground+moduleTrace;
            Destroy(trace);
            Check(Mathf.Abs(knob.Value-target)<.01f,(touch?"Touch":"Mouse")+" rotary reaches "+target+" V (actual "+knob.Value+pointerTrace+")");
        }
        IEnumerator Layout(string prefix)
        {
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1920,1200),new Vector2Int(1280,800),new Vector2Int(2560,1600),new Vector2Int(1440,1080)})
            {
                Type.GetType("AdaptiveOhmsLaw.Editor.Scene07Validation, Assembly-CSharp-Editor").GetMethod("SetResolution").Invoke(null,new object[]{size.x,size.y});
                yield return Until(()=>Screen.width==size.x&&Screen.height==size.y,prefix+" resolution "+size,12);yield return new WaitForSeconds(.2f);Canvas.ForceUpdateCanvases();
                foreach(var button in FindObjectsByType<Selectable>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled&&b.interactable))
                {
                    var rt=(RectTransform)button.transform;var corners=new Vector3[4];rt.GetWorldCorners(corners);
                    Check(corners[0].x>=0&&corners[0].y>=0&&corners[2].x<=Screen.width+1&&corners[2].y<=Screen.height+1,prefix+" control in screen "+button.name);
                    Check((corners[2].y-corners[0].y)>=44,prefix+" touch height "+button.name);
                }
                ScreenCapture.CaptureScreenshot("Logs/"+prefix+"-"+size.x+"x"+size.y+".png");yield return new WaitForEndOfFrame();
            }
        }
        IEnumerator Start()
        {
            if(UnityEditor.SessionState.GetBool("RotaryReview",false)){yield return RotaryReview();yield break;}
            if(UnityEditor.SessionState.GetBool("GraphReview",false)){yield return GraphReview();yield break;}
            SceneFlowManager f6=null;
            yield return Until(()=>{f6=FindFirstObjectByType<SceneFlowManager>();return f6!=null&&f6.Stage==LearningStage.Interaction;},"Scene 6 opening reaches question");
            yield return Layout("Scene06-immersive");Click("AnswerC");yield return Until(()=>Ready("KeepPrediction"),"Scene 6 curious hint decision");Click("KeepPrediction");yield return Until(()=>Ready("Continue"),"Scene 6 completion ready");Click("Continue");
            Scene07FlowManager f7=null;
            yield return Until(()=>{f7=FindFirstObjectByType<Scene07FlowManager>();return f7!=null&&f7.Stage==Scene07Stage.Experiment;},"Real Scene 6 to 7 transition",40);
            Check(f7.PriorPrediction==2,"Scene 7 recalls retained Scene 6 prediction");Check(f7.Profiles.Profile.completedActivities.Contains("Scene06Prediction"),"Scene 6 completion persisted across load");
            Check(!B("RecordReading").interactable,"Open circuit cannot record");yield return Layout("Scene07-reviewed");
            Click("ToggleSwitch");yield return Rotate(3);yield return Tap("RecordReading");Check(f7.Experiment.Table.RecordedCount==1,"Mouse recorded 3 V");yield return Rotate(1,true);yield return Tap("RecordReading",true);
            Check(f7.Experiment.Table.RecordedCount==2,"Out-of-order mouse and touch observations recorded");
            var session=f7.Profiles.Profile.experiments.Last().sessionId;
            yield return SceneManager.LoadSceneAsync("Scene_10_BecomeTheExperimenter");
            yield return Until(()=>{f7=FindFirstObjectByType<Scene07FlowManager>();return f7!=null&&f7.Stage==Scene07Stage.Experiment;},"Experiment resumes after reload");
            Check(f7.Experiment.Table.RecordedCount==2&&f7.Profiles.Profile.experiments.Last().sessionId==session,"Partial observations restored without a duplicate session");
            Check(f7.Experiment.Table.Observations[0].voltage==1,"Restored table sorted by voltage");Click("ToggleSwitch");yield return Rotate(2);Click("RecordReading");yield return Rotate(4);Click("RecordReading");yield return Rotate(5,true);Click("RecordReading");
            yield return Until(()=>f7.Stage==Scene07Stage.EvidenceQuestion,"Evidence question waits for ready controls");float master=f7.Profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship);Click("AnswerA");
            Check(f7.Profiles.Profile.Mastery(ConceptId.VoltageCurrentRelationship)==master+10,"First correct evidence awards configured mastery");
            Check(f7.Profiles.Profile.Mastery(ConceptId.Ammeter)==50,"Unassessed meter concept is not awarded mastery");
            yield return Until(()=>Ready("Continue"),"Scene 7 explanation finishes before continue");Click("Continue");
            Scene08FlowManager f8=null;
            yield return Until(()=>{f8=FindFirstObjectByType<Scene08FlowManager>();return f8!=null&&f8.Stage==Scene08Stage.Plotting;},"Real Scene 7 to 8 transition",40);
            Check(!f8.Record.sampleData&&f8.Record.sourceSessionId==session,"Graph uses saved experiment session");
            Check(f8.Record.observations.Count==5&&Mathf.Abs(f8.Record.observations[2].current-.6f)<.001f,"Quantitative readings survive scene transition");
            yield return Layout("Scene08-plotting");
            float graphMaster=f8.Profiles.Profile.Mastery(ConceptId.GraphInterpretation);
            Click("Plot_2_1");Check(f8.Record.plottingMistakes==1&&f8.Record.hints==1,"Misplaced point triggers a saved clue");Check(f8.Profiles.Profile.Mastery(ConceptId.GraphInterpretation)==graphMaster,"Motor placement has no mastery penalty");
            Click("Plot_3_1");Check(f8.Profiles.Profile.supportLevel==SupportLevel.Supported,"Repeated placement mistakes increase support");
            yield return Tap("AskWattson",true);Check(f8.ChatUI.IsOpen,"Graph chat opens by touch");Click("AmmeterHelp");yield return new WaitForSeconds(.2f);
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).First(t=>t.name=="Reply").text.Contains("horizontal axis"),"Graph chat knows the current axes and selected reading");
            Check(!B("Plot_1_1").interactable,"Chat pauses graph interaction");Click("CloseChat");
            yield return Tap("Plot_1_1");Check(f8.Record.plottedIndices.Count==1,"Mouse plots first point");
            yield return SceneManager.LoadSceneAsync("Scene_11_TurnDataIntoGraph");
            yield return Until(()=>{f8=FindFirstObjectByType<Scene08FlowManager>();return f8!=null&&f8.Stage==Scene08Stage.Plotting;},"Graph resumes after reload");
            Check(f8.Record.plottedIndices.Count==1,"Plotted point restored");yield return Tap("Plot_2_2",true);Check(f8.Record.plottedIndices.Count==2,"Touch plots second point");Click("Plot_3_3");Click("Plot_4_4");Click("Plot_5_5");
            yield return Until(()=>f8.Stage==Scene08Stage.Interpretation,"All points reveal interpretation question");
            Check(FindFirstObjectByType<GraphGridGraphic>().showTrend,"Trend line appears only after plotting");
            ScreenCapture.CaptureScreenshot("Logs/Scene08-pattern.png");yield return new WaitForEndOfFrame();
            Click("AnswerB");yield return Until(()=>Ready("TryAgain"),"Incorrect interpretation offers retry");
            Check(f8.Profiles.Profile.Mastery(ConceptId.GraphInterpretation)==graphMaster-2,"Incorrect interpretation has configured small loss");Click("TryAgain");Click("AnswerA");
            Check(f8.Profiles.Profile.Mastery(ConceptId.GraphInterpretation)==graphMaster+2,"Assisted interpretation has smaller gain");
            yield return Until(()=>Ready("Continue"),"Graph explanation completes");int completed=0;f8.OnScene08Completed+=()=>completed++;Click("Continue");f8.Complete();
            Check(completed==1&&f8.Record.completed,"Graph completion event fires exactly once");
            Check(f8.Profiles.Profile.completedActivities.Contains("Scene08Graph"),"Graph activity completion persisted");
            var store=new JsonStudentProfileStore(Path.GetFullPath("Logs/Lesson-test-profile.json"));store.Save(f8.Profiles.Profile);var loaded=store.Load();loaded.EnsureConcepts(50);
            Check(loaded.graphs.Last().plottedIndices.Count==5&&loaded.graphs.Last().completed,"JSON roundtrip includes graph evidence");
            Check(loaded.questions.Last().firstAnswer==1&&loaded.questions.Last().finalAnswer==0&&loaded.questions.Last().responseSeconds>0,"Graph answer history and response time saved");
            StudentProfileManager.ValidationStore=new MemoryProfileStore();yield return SceneManager.LoadSceneAsync("Scene_11_TurnDataIntoGraph");
            yield return Until(()=>{f8=FindFirstObjectByType<Scene08FlowManager>();return f8!=null&&f8.Stage==Scene08Stage.Plotting;},"Direct graph launch works");
            Check(f8.Record.sampleData,"Direct graph launch labels practice data");Click("Plot_1_1");Click("Plot_2_2");Click("Plot_3_3");Click("Plot_4_4");Click("Plot_5_5");yield return Until(()=>f8.Stage==Scene08Stage.Interpretation,"Practice graph question ready");Click("AnswerA");
            Check(f8.Profiles.Profile.Mastery(ConceptId.GraphInterpretation)==50,"Sample practice does not award assessed mastery");
            yield return Until(()=>Ready("Continue"),"Practice conclusion ready");Click("Continue");
            Check(errors.Count==0,"No runtime errors: "+string.Join("; ",errors));results.Add("COMPLETE: real 06→07→08 flow, persistence, graph branches, mouse/touch and five layouts verified.");File.WriteAllLines(outputPath,results);UnityEditor.EditorApplication.isPlaying=false;
        }
        IEnumerator GraphReview()
        {
            yield return null;
            var profile=new StudentLearningProfile();profile.EnsureConcepts(50);
            profile.experiments.Add(new ExperimentRecord{sessionId="graph-review-evidence",completed=true,fixedResistance=5,observations=Enumerable.Range(1,5).Select(v=>new ExperimentObservation{voltage=v,current=v/5f}).ToList()});
            StudentProfileManager.ValidationStore=new MemoryProfileStore();StudentProfileManager.ValidationStore.Save(profile);
            yield return SceneManager.LoadSceneAsync("Scene_11_TurnDataIntoGraph");Scene08FlowManager flow=null;
            yield return Until(()=>{flow=FindFirstObjectByType<Scene08FlowManager>();return flow!=null&&flow.Stage==Scene08Stage.Plotting;},"Graph opens with saved test evidence");
            var grid=FindFirstObjectByType<GraphGridGraphic>();Canvas.ForceUpdateCanvases();yield return null;
            var mesh=grid.canvasRenderer.GetMesh();Check(mesh.vertexCount>=32,"Graph grid actually produces visible mesh geometry");
            yield return Layout("Scene08-reviewed-plotting");
            yield return Tap("GraphReading2",true);Check(flow.Graph.Selected==2,"Touch selects a different data row");yield return Tap("Plot_3_3");Check(flow.Record.plottedIndices.Contains(2),"Selected third observation plots correctly");
            Click("GraphReading0");yield return Tap("Plot_1_1",true);Click("Plot_2_2");Click("Plot_4_4");Click("Plot_5_5");yield return Until(()=>flow.Stage==Scene08Stage.Interpretation,"Graph interpretation opens");
            Check(grid.showTrend&&grid.points.Count==5,"Graph trend contains five evidence points");Canvas.ForceUpdateCanvases();yield return null;
            mesh=grid.canvasRenderer.GetMesh();Check(mesh.vertexCount>=44,"Graph trend produces line geometry");
            yield return Layout("Scene08-reviewed-pattern");
            Click("AskWattson");Check(flow.ChatUI.IsOpen,"Chat opens over the final graph");Click("CurrentZero");yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot("Logs/Scene08-reviewed-chat.png");yield return new WaitForEndOfFrame();yield return Tap("CloseChat");Check(!flow.ChatUI.IsOpen,"Mouse can reach modal close above 3D viewport");
            // Asking an explanation must preserve mastery and use the assisted scoring rule.
            Check(flow.Profiles.Profile.Mastery(ConceptId.GraphInterpretation)==50,"Graph explanation request has no mastery penalty");Click("AnswerA");
            Check(flow.Profiles.Profile.Mastery(ConceptId.GraphInterpretation)==54,"Explanation-assisted correct answer awards +4");
            yield return Until(()=>Ready("Continue"),"Graph conclusion ready");Click("Continue");
            yield return SceneManager.LoadSceneAsync("Scene_11_TurnDataIntoGraph");
            yield return Until(()=>{flow=FindFirstObjectByType<Scene08FlowManager>();return flow!=null&&flow.Stage==Scene08Stage.Plotting;},"Completed graph can replay");
            Click("Plot_1_1");Click("Plot_2_2");Click("Plot_3_3");Click("Plot_4_4");Click("Plot_5_5");yield return Until(()=>flow.Stage==Scene08Stage.Interpretation,"Replay reaches question");Click("AnswerB");yield return Until(()=>Ready("KeepAnswer"),"Guided explanation is offered");Click("KeepAnswer");yield return Until(()=>Ready("Continue"),"Guided conclusion completes");Click("Continue");
            Check(flow.Record.conclusionOutcome=="GuidedCompleted","Guided outcome is saved honestly");Check(flow.Profiles.Profile.Mastery(ConceptId.GraphInterpretation)==54,"Replay does not inflate or subtract mastery");
            var fresh=new StudentLearningProfile();fresh.EnsureConcepts(50);fresh.experiments.Add(profile.experiments[0]);StudentProfileManager.ValidationStore=new MemoryProfileStore();StudentProfileManager.ValidationStore.Save(fresh);
            yield return SceneManager.LoadSceneAsync("Scene_11_TurnDataIntoGraph");
            yield return Until(()=>{flow=FindFirstObjectByType<Scene08FlowManager>();return flow!=null&&flow.Stage==Scene08Stage.Plotting;},"Fresh learner graph opens");
            Click("Plot_1_1");Click("Plot_2_2");Click("Plot_3_3");Click("Plot_4_4");Click("Plot_5_5");yield return Until(()=>flow.Stage==Scene08Stage.Interpretation,"Fresh learner question opens");Click("AnswerA");
            Check(flow.Profiles.Profile.Mastery(ConceptId.GraphInterpretation)==60,"Unassisted graph first answer awards +10");yield return Until(()=>Ready("Continue"),"Fresh conclusion ready");Click("Continue");
            Check(errors.Count==0,"No runtime errors: "+string.Join("; ",errors));results.Add("COMPLETE: final graph rendering, five aspect/resolution layouts, row selection, mouse/touch, chat, guided/replay and unassisted scoring verified.");File.WriteAllLines(outputPath,results);UnityEditor.EditorApplication.isPlaying=false;
            if (Application.isBatchMode) UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.Exit(0);
        }
        IEnumerator RotaryReview()
        {
            var flow=FindFirstObjectByType<Scene07FlowManager>();var knob=FindFirstObjectByType<RotaryVoltageControl>(FindObjectsInactive.Include);
            Check(!knob.IsInteractable(),"Opening disables rotary input");
            yield return Until(()=>{FindFirstObjectByType<DialoguePresenter>()?.SkipCurrentLine();return flow.Stage==Scene07Stage.Experiment;},"Experiment becomes interactive");
            Click("ToggleSwitch");yield return Rotate(3);yield return Tap("RecordReading");Check(flow.Experiment.Table.RecordedCount==1,"Mouse records after rotating");
            yield return Rotate(1,true);yield return Tap("RecordReading",true);Check(flow.Experiment.Table.RecordedCount==2,"Touch records after rotating");
            knob.OnMove(new AxisEventData(EventSystem.current){moveDir=MoveDirection.Right});Check(Mathf.Abs(knob.Value-1.1f)<.001f,"Keyboard fine adjustment");Check(!B("RecordReading").interactable,"Intermediate reading cannot be recorded");
            knob.OnScroll(new PointerEventData(EventSystem.current){scrollDelta=Vector2.up});Check(Mathf.Abs(knob.Value-1.2f)<.001f,"Scroll fine adjustment");
            Click("AskWattson");Check(!knob.IsInteractable(),"Chat disables rotary input");float held=knob.Value;knob.OnMove(new AxisEventData(EventSystem.current){moveDir=MoveDirection.Right});Check(knob.Value==held,"Disabled dial leaves voltage unchanged");Click("CloseChat");Check(knob.IsInteractable(),"Closing chat restores rotary input");
            yield return Rotate(0);Check(flow.Experiment.Circuit.Current==0,"Zero voltage produces zero current");yield return Rotate(3,true);Check(Mathf.Abs(flow.Experiment.Circuit.Current-.6f)<.001f,"Three volts produces 0.60 A");
            Check(Mathf.Abs(FindObjectsByType<MeterDisplay>(FindObjectsSortMode.None).First(m=>m.name=="Ammeter").CurrentReading-.6f)<.001f,"Physical ammeter follows rotary setting");
            yield return Layout("Scene07-rotary");yield return Rotate(2);yield return Tap("RecordReading",true);
            yield return Rotate(4);yield return Tap("RecordReading");yield return Rotate(5,true);yield return Tap("RecordReading",true);
            yield return Until(()=>flow.Stage==Scene07Stage.EvidenceQuestion,"Rotary experiment reaches evidence question");
            Check(errors.Count==0,"No runtime errors");results.Add("COMPLETE: rotary mouse/touch, keyboard/scroll, chat gating, circuit values and recording verified.");File.WriteAllLines(outputPath,results);UnityEditor.EditorApplication.isPlaying=false;
        }
        void OnDestroy(){Application.logMessageReceived-=Log;if(testTouch!=null&&testTouch.added)InputSystem.RemoveDevice(testTouch);if(testMouse!=null&&testMouse.added)InputSystem.RemoveDevice(testMouse);foreach(var device in suspendedPointers)if(device.added)InputSystem.EnableDevice(device);if(originalInputSettings!=null)InputSystem.settings=originalInputSettings;if(validationInputSettings!=null)Destroy(validationInputSettings);}
    }
}
#endif








