#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AdaptiveOhmsLaw
{
    public sealed class MemoryProfileStore : IStudentProfileStore
    {
        string json;
        public StudentLearningProfile Load() => json == null ? new StudentLearningProfile() : JsonUtility.FromJson<StudentLearningProfile>(json);
        public void Save(StudentLearningProfile profile) => json = JsonUtility.ToJson(profile);
    }
    public class Scene06ValidationRunner : MonoBehaviour
    {
        readonly List<string> results = new List<string>();
        readonly List<string> errors = new List<string>();
        SceneFlowManager flow;
        void Awake() { DontDestroyOnLoad(gameObject); Application.runInBackground = true; Application.logMessageReceived += Log; }
        void Log(string message,string stack,LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }
        void Check(bool condition,string label)
        {
            results.Add((condition ? "PASS " : "FAIL ") + label);
            File.WriteAllLines("Logs/Scene06-validation.txt",results);
            if (!condition) throw new Exception("Scene 06 validation: " + label);
        }
        Button Button(string name) => FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(b=>b.name==name);
        void Click(string name)
        {
            var button = Button(name); Check(button.isActiveAndEnabled && button.interactable,"Enabled control: " + name);
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        IEnumerator MouseClick(string name)
        {
            var button = Button(name);
            var center = (Vector2)((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse,new MouseState { position = center }); yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState { position = center }.WithButton(MouseButton.Left)); yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState { position = center }); yield return null;
            InputSystem.RemoveDevice(mouse);
        }
        IEnumerator TouchTap(string name)
        {
            var button = Button(name);
            var center = (Vector2)((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
            var touch = InputSystem.AddDevice<Touchscreen>();
            InputSystem.QueueStateEvent(touch,new TouchState { touchId = 1,phase = UnityEngine.InputSystem.TouchPhase.Began,position = center }); yield return null;
            InputSystem.QueueStateEvent(touch,new TouchState { touchId = 1,phase = UnityEngine.InputSystem.TouchPhase.Ended,position = center }); yield return null;
            InputSystem.RemoveDevice(touch);
        }
        IEnumerator Ready()
        {
            float deadline = Time.realtimeSinceStartup + 20;
            while (Time.realtimeSinceStartup < deadline)
            {
                flow = FindFirstObjectByType<SceneFlowManager>();
                if (flow != null && flow.Stage == LearningStage.Interaction && flow.Questions.Record != null) yield break;
                yield return null;
            }
            Check(false,"Scene reaches interaction within 20 seconds");
        }
        IEnumerator Start()
        {
            yield return Ready();
            Check(FindFirstObjectByType<InputSystemUIInputModule>() != null,"Modern shared mouse/touch UI module");
            Check(flow.Profiles.Profile.concepts.Count == 10,"All ten concept IDs initialized");
            Check(flow.Profiles.Profile.Mastery(ConceptId.Voltage)==50,"Isolated test profile starts at 50");
            Check(FindFirstObjectByType<ChargeDriftVisualization>().Voltage==2,"Opening demonstrates 1 V to 2 V");
            ScreenCapture.CaptureScreenshot("Logs/Scene06-desktop.png");
            yield return new WaitForSeconds(.3f);
            yield return MouseClick("AnswerA"); yield return new WaitForSeconds(.15f);
            Check(flow.Questions.Record.finalAnswer==0,"Mouse device event reaches answer through UI raycast");
            Check(flow.Profiles.Profile.Mastery(ConceptId.Voltage)==60,"First correct answer awards +10");
            Check(flow.Profiles.Profile.Mastery(ConceptId.Resistance)==50,"Unrelated concept unchanged");
            Check(flow.Questions.Record.firstAnswer==0 && flow.Questions.Record.attempts.Count==1,"First answer and attempt recorded");
            yield return new WaitForSeconds(3.2f); int completed = 0; flow.OnScene06Completed += ()=>completed++;
            Click("Continue"); flow.Complete();
            Check(completed==1 && flow.Stage==LearningStage.NextActivity,"Completion event fires exactly once");
            Check(flow.Questions.Record.completed,"Completed activity saved");
            var saved = StudentProfileManager.ValidationStore.Load();
            Check(saved.completedActivities.Contains("Scene06Prediction") && saved.correctFirstAttempts==1,"Profile JSON roundtrip retains progress");
            var diskStore = new JsonStudentProfileStore(Path.GetFullPath("Logs/Scene06-test-profile.json"));
            diskStore.Save(saved); diskStore.Save(saved);
            Check(diskStore.Load().questions[0].finalAnswer==0,"Atomic local JSON save, replacement and reload");
            StudentProfileManager.ValidationStore = new MemoryProfileStore();
            SceneManager.LoadScene("Scene_06_AdaptivePrediction"); yield return null; yield return Ready();
            Click("AnswerB"); yield return new WaitForSeconds(5.3f);
            Check(flow.Profiles.Profile.supportLevel==SupportLevel.Guided,"First incorrect answer enables Guided support");
            Check(flow.Questions.Record.hintCount==1 && flow.Profiles.Profile.Mastery(ConceptId.Current)==48,"Hint recorded and small incorrect penalty");
            Click("TryAgain"); Click("AnswerC"); yield return new WaitForSeconds(5.3f);
            Check(flow.Profiles.Profile.supportLevel==SupportLevel.Supported,"Repeated mistakes enable Supported guidance");
            Check(flow.Questions.Record.firstAnswer==1 && flow.Questions.Record.finalAnswer==2,"Original prediction retained across retries");
            Click("AskWattson"); Check(flow.ChatUI.IsOpen,"Ask Wattson opens");
            float before = flow.Profiles.Profile.Mastery(ConceptId.Current);
            Click("Simplify");
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).First(t=>t.name=="Reply").text.Contains("one step"),"Contextual simplified response at support level 3");
            Check(flow.Profiles.Profile.Mastery(ConceptId.Current)==before,"Explanation has no mastery penalty");
            Click("CloseChat"); Check(!flow.ChatUI.IsOpen,"Chat closes");
            Click("TryAgain"); Click("AnswerA"); yield return new WaitForSeconds(3.2f);
            Check(flow.Profiles.Profile.Mastery(ConceptId.Current)==50,"Assisted answer awards smaller +4 gain");
            Check(flow.Questions.Record.attempts.Count==3 && flow.Questions.Record.hintCount==2,"All attempts and hints retained");
            Check(flow.Questions.Record.responseSeconds>0,"Response timing recorded"); Click("Continue");
            StudentProfileManager.ValidationStore = new MemoryProfileStore();
            SceneManager.LoadScene("Scene_06_AdaptivePrediction"); yield return null; yield return Ready();
            yield return TouchTap("AnswerC");
            Check(flow.Questions.Record.finalAnswer==2,"Touch device event reaches same answer path through UI raycast");
            yield return new WaitForSeconds(5.3f); Click("KeepPrediction"); yield return new WaitForSeconds(3.2f); Click("Continue");
            Check(flow.Questions.Record.finalAnswer==2 && flow.Questions.Record.completed && flow.Profiles.Profile.correctFirstAttempts==0,"Keep prediction completes without marking answer correct");
            StudentProfileManager.ValidationStore = new MemoryProfileStore();
            SceneManager.LoadScene("Scene_06_AdaptivePrediction"); yield return null; yield return Ready();
            foreach (var dimensions in new[] { new Vector2Int(1920,1200),new Vector2Int(1280,800),new Vector2Int(2560,1600),new Vector2Int(1440,1080) })
            {
                var validation = Type.GetType("AdaptiveOhmsLaw.Editor.Scene06Validation, Assembly-CSharp-Editor");
                validation.GetMethod("SetResolution").Invoke(null,new object[]{dimensions.x,dimensions.y});
                yield return new WaitForSeconds(.5f); Canvas.ForceUpdateCanvases();
                Check(Screen.width==dimensions.x && Screen.height==dimensions.y,"Game view resolution " + dimensions);
                foreach (var text in FindObjectsByType<Text>(FindObjectsSortMode.None))
                    Check(text.preferredHeight <= text.rectTransform.rect.height+2,"Question text fits: " + text.name + " @ " + dimensions);
                ScreenCapture.CaptureScreenshot("Logs/Scene06-question-"+dimensions.x+"x"+dimensions.y+".png");
                yield return new WaitForSeconds(.3f);
                Click("AskWattson"); Click("VoltageHelp");
                foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.IsActive()))
                {
                    var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                    Check(corners[0].x>=0 && corners[0].y>=0 && corners[2].x<=Screen.width+1 && corners[2].y<=Screen.height+1,"Button stays on screen: " + button.name + " @ " + dimensions);
                    if (button.transform.IsChildOf(Button("CloseChat").transform.parent))
                        Check(corners[2].y-corners[0].y>=44,"Comfortable touch height: " + button.name);
                }
                ScreenCapture.CaptureScreenshot("Logs/Scene06-chat-"+dimensions.x+"x"+dimensions.y+".png");
                yield return new WaitForSeconds(.3f); Click("CloseChat");
            }
            // Configurable scoring and bounds, including more challenging future questions.
            var learning = FindFirstObjectByType<AdaptiveLearningManager>();
            var hard = ScriptableObject.Instantiate(flow.Questions.Definition); hard.difficulty=3;
            float previous = flow.Profiles.Profile.Mastery(ConceptId.Current);
            var record = new QuestionRecord(); record.attempts.Add(new AnswerAttempt {correct=true});
            learning.RecordAnswer(hard,record,true);
            Check(flow.Profiles.Profile.Mastery(ConceptId.Current)==previous+14,"Higher difficulty uses configurable bonus");
            flow.Profiles.Profile.concepts.First(c=>c.concept==ConceptId.Current).mastery=99;
            learning.RecordAnswer(hard,record,true);
            Check(flow.Profiles.Profile.Mastery(ConceptId.Current)==100,"Mastery upper bound");
            flow.Profiles.Profile.concepts.First(c=>c.concept==ConceptId.Current).mastery=1;
            learning.RecordAnswer(hard,record,false);
            Check(flow.Profiles.Profile.Mastery(ConceptId.Current)==0,"Mastery lower bound"); Destroy(hard);
            Check(errors.Count==0,"No runtime errors during verification: " + string.Join("; ",errors));
            results.Add("COMPLETE: editor Play mode branch, storage, chat and resolution checks passed. Hardware touch/Android build remain device checks.");
            File.WriteAllLines("Logs/Scene06-validation.txt",results);
            UnityEditor.EditorApplication.isPlaying = false;
        }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
#endif
