#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    public class WattsonChatValidationRunner : MonoBehaviour
    {
        readonly List<string> results=new List<string>();readonly List<string> errors=new List<string>();
        TcpListener testServer;
        async Task<string> ServeOnce()
        {
            using(var client=await testServer.AcceptTcpClientAsync())using(var stream=client.GetStream()){
                var bytes=new List<byte>();var one=new byte[1];
                while(bytes.Count<16000){if(await stream.ReadAsync(one,0,1)==0)throw new IOException("Incomplete request");bytes.Add(one[0]);int n=bytes.Count;if(n>=4&&bytes[n-4]==13&&bytes[n-3]==10&&bytes[n-2]==13&&bytes[n-1]==10)break;}
                string header=Encoding.ASCII.GetString(bytes.ToArray());int length=int.Parse(header.Split(new[]{"\r\n"},StringSplitOptions.None).First(l=>l.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase)).Split(':')[1]);
                var body=new byte[length];int read=0;while(read<length){int count=await stream.ReadAsync(body,read,length-read);if(count==0)throw new IOException("Incomplete body");read+=count;}
                var response=Encoding.UTF8.GetBytes("{\"reply\":\"Transport test reply.\"}");var responseHeader=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: "+response.Length+"\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(responseHeader,0,responseHeader.Length);await stream.WriteAsync(response,0,response.Length);return Encoding.UTF8.GetString(body);
            }
        }
        void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message);}
        void Check(bool value,string label){results.Add((value?"PASS ":"FAIL ")+label);File.WriteAllLines("Logs/Wattson-chat-validation.txt",results);if(!value){UnityEditor.EditorApplication.isPlaying=false;throw new Exception(label);}}
        T F<T>(string name)where T:Component=>FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.name==name);
        IEnumerator Until(Func<bool> predicate,string label){float deadline=Time.realtimeSinceStartup+25;while(!predicate()&&Time.realtimeSinceStartup<deadline)yield return null;Check(predicate(),label);}
        class FakeConversation : IWattsonConversationProvider
        {
            public int historyCount,delay;public LearningContext context;public bool fail,capacity;
            public Task<string> RespondAsync(string message,LearningContext context,CancellationToken token)=>RespondAsync(message,context,Array.Empty<WattsonChatTurn>(),token);
            public async Task<string> RespondAsync(string message,LearningContext value,IReadOnlyList<WattsonChatTurn> history,CancellationToken token){historyCount=history.Count;context=value;if(delay>0)await Task.Delay(delay,token);if(capacity)throw new WattsonServiceException("Google busy",true);if(fail)throw new InvalidOperationException("Simulated unavailable service");return "Test response: keep resistance fixed while comparing voltage and current.";}
        }
        IEnumerator Start()
        {
            var flow=FindFirstObjectByType<Scene08FlowManager>();yield return Until(()=>flow.Stage==Scene08Stage.Plotting,"Lesson ready with isolated profile");
            var manager=FindFirstObjectByType<WattsonChatManager>();var ui=flow.ChatUI;float mastery=flow.Profiles.Profile.Mastery(ConceptId.GraphInterpretation);
            ui.SetOpen(true);ui.Submit("What does an ammeter measure?");yield return null;
            Check(manager.History.Count==2,"Question and reply retained");Check(F<Text>("Reply").text.Contains("series"),"Offline reply displayed");Check(!F<Text>("Reply").supportRichText,"Reply markup rendered as plain text");
            Check(flow.Profiles.Profile.Mastery(ConceptId.GraphInterpretation)==mastery,"Chat does not reduce mastery");
            var speech=FindFirstObjectByType<WattsonSpeech>();
            yield return Until(()=>speech.Output.isPlaying,"Chat reply has audible speech playback");
            var samples=new float[1024];float peak=0;float audioDeadline=Time.realtimeSinceStartup+3;
            while(peak<.0001f&&Time.realtimeSinceStartup<audioDeadline){speech.Output.GetOutputData(samples,0);foreach(var sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));yield return null;}
            Check(peak>.0001f,"Voice produces nonzero audio output");
            Check(F<Button>("VoiceInput").interactable,"Microphone control available");
            Check(F<Button>("VoiceInput").GetComponent<HoldToTalk>()!=null,"Microphone uses pointer hold/release interaction");
            Check(F<RectTransform>("MicrophoneMonitor")!=null&&F<MicrophoneIcon>("MicIcon")!=null,"Device monitor and microphone icon present");
            var hold=F<Button>("VoiceInput").GetComponent<HoldToTalk>();var voice=FindFirstObjectByType<WattsonVoiceInput>();
            int voiceTurns=manager.History.Count;
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=901};
            hold.OnPointerDown(pointer);hold.OnPointerUp(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=902});
            if(Microphone.devices.Length>0)Check(voice.Listening,"A second pointer cannot release the recording");
            hold.OnPointerUp(pointer);yield return null;
            Check(!voice.Listening,"Releasing the initiating pointer ends capture");
            voice.Stop();Check(manager.History.Count==voiceTurns,"Short press never sends an empty question");
            hold.OnPointerDown(pointer);ui.SetOpen(false);yield return null;
            Check(!voice.Listening&&!voice.Processing,"Closing chat discards an active capture");ui.SetOpen(true);
            Check(VoiceDiagnostics.WindowsError(unchecked((int)0x80045509)).Contains("dictation is turned off"),"Speech privacy failure has an accurate recovery message");
            Check(VoiceDiagnostics.WindowsError(unchecked((int)0x80070005)).Contains("access was denied"),"Permission failure is distinct from a missing microphone");
            if(File.Exists("Logs/voice-input-smoke.req")){
                File.Delete("Logs/voice-input-smoke.req");var mic=FindFirstObjectByType<WattsonVoiceInput>();string status="";Action<string> capture=value=>status=value;mic.StatusChanged+=capture;
                mic.Listen();yield return new WaitForSecondsRealtime(3);File.WriteAllText("Logs/voice-input-smoke.txt","Detected devices: "+Microphone.devices.Length+"\nListening session: "+mic.Listening+"\nNative recognizer: "+mic.DiagnosticState+"\n"+status);mic.Stop();mic.StatusChanged-=capture;
            }
            var snapshot=manager.Context;var fake=new FakeConversation{delay=100};manager.Configure(()=>snapshot,fake);
            ui.Submit("Why does doubling it change the measurement?");yield return Until(()=>manager.History.Count==4,"Asynchronous follow-up completes");
            Check(fake.historyCount==2,"Provider receives prior conversation");Check(fake.context.activity=="Scene08Graph"&&fake.context.selectedVoltage>0,"Provider receives activity and selected reading");
            testServer=new TcpListener(IPAddress.Loopback,0);testServer.Start();var serverTask=ServeOnce();
            var settings=ScriptableObject.CreateInstance<WattsonServiceSettings>();settings.chatEndpoint="http://127.0.0.1:"+((IPEndPoint)testServer.LocalEndpoint).Port+"/chat";settings.timeoutSeconds=5;
            var networkTask=new HttpWattsonResponseProvider(settings).RespondAsync("Transport check",snapshot,manager.History,CancellationToken.None);
            yield return Until(()=>networkTask.IsCompleted&&serverTask.IsCompleted,"Local HTTP contract completes");
            Check(networkTask.Status==TaskStatus.RanToCompletion&&networkTask.Result=="Transport test reply.","HTTP JSON response parsed");
            Check(serverTask.Result.Contains("\"history\"")&&serverTask.Result.Contains("Scene08Graph")&&serverTask.Result.Contains("Transport check"),"HTTP request contains conversation and lesson context");
            testServer.Stop();testServer=null;Destroy(settings);
            fake.delay=3000;int before=manager.History.Count;ui.Submit("Cancel this pending question");ui.SetOpen(false);yield return new WaitForSeconds(.15f);
            Check(manager.History.Count==before,"Closing chat cancels pending answer without recording a stale turn");
            ui.SetOpen(true);fake.delay=0;fake.fail=true;F<InputField>("MessageInput").text="Keep my question if the service fails";ui.Submit(F<InputField>("MessageInput").text);yield return null;
            Check(F<InputField>("MessageInput").text.Contains("Keep my question"),"Failed request preserves input");Check(F<Button>("Send").interactable,"Failure restores send control");Check(manager.History.Count==before,"Failed response is not inserted into history");
            fake.fail=false;fake.capacity=true;ui.Submit("What is voltage?");yield return null;
            Check(manager.LastReplyWasOffline&&F<Text>("Reply").text.Contains("built-in lesson explanation"),"Google capacity fallback is clearly identified as a built-in response");
            manager.Configure(()=>snapshot,new LocalWattsonResponseProvider());
            for(int i=0;i<6;i++){ui.Submit(i%2==0?"What is voltage?":"What is current?");yield return null;}
            var scroll=F<ScrollRect>("ConversationScroll");Canvas.ForceUpdateCanvases();yield return null;
            Check(scroll.content.rect.height>scroll.viewport.rect.height,"Long conversation scrolls");
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1920,1200),new Vector2Int(1280,800),new Vector2Int(2560,1600),new Vector2Int(1440,1080)}){
                Type.GetType("AdaptiveOhmsLaw.Editor.Scene07Validation, Assembly-CSharp-Editor").GetMethod("SetResolution").Invoke(null,new object[]{size.x,size.y});yield return Until(()=>Screen.width==size.x&&Screen.height==size.y,"Chat resolution "+size);yield return new WaitForSeconds(.2f);
                foreach(var b in new[]{F<Button>("CloseChat"),F<Button>("Send"),F<Button>("VoiceInput")}){var corners=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(corners);Check(corners[0].x>=0&&corners[2].x<=Screen.width+1&&corners[2].y-corners[0].y>=44,"Chat control bounds/touch size "+b.name);}
                ScreenCapture.CaptureScreenshot("Logs/Wattson-chat-"+size.x+"x"+size.y+".png");yield return new WaitForEndOfFrame();
            }
            before=manager.History.Count;ui.SetOpen(false);yield return SceneManager.LoadSceneAsync("Scene_10_BecomeTheExperimenter");yield return null;
            manager=FindFirstObjectByType<WattsonChatManager>();Check(manager.History.Count==before,"Conversation survives activity transition");
            Check(errors.Count==0,"No runtime errors: "+string.Join(";",errors));results.Add("COMPLETE: conversation, context, cancellation, failure recovery, speech output, scrolling and five layouts. Physical microphone and Android hardware still need learner testing. Live AI requires a configured service.");File.WriteAllLines("Logs/Wattson-chat-validation.txt",results);UnityEditor.EditorApplication.isPlaying=false;
        }
        void OnDestroy(){testServer?.Stop();Application.logMessageReceived-=Log;}
    }
}
#endif

