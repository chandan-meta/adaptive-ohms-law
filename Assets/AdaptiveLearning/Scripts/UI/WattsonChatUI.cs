using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace AdaptiveOhmsLaw
{
    public class WattsonChatUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button open, close, send;
        [SerializeField] Button[] suggestions;
        [SerializeField] InputField input;
        [SerializeField] Text reply, context;
        [SerializeField] ScrollRect conversation;
        [SerializeField] WattsonController companion;
        [SerializeField] Button microphone;
        [SerializeField] Text voiceStatus;
        WattsonVoiceInput voiceInput;
        public void ConfigureVoice(Button button,Text status){microphone=button;voiceStatus=status;}
        readonly List<GameObject> previousTurns=new List<GameObject>();
        WattsonChatManager manager;
        GameObject previousSelection;
        CancellationTokenSource pending;
        public bool IsOpen => panel.activeSelf;
        public event Action<bool> VisibilityChanged;
        public void Configure(GameObject window, Button ask, Button dismiss, Button submit, Button[] quick, InputField field, Text body, Text contextLabel)
        { panel = window; open = ask; close = dismiss; send = submit; suggestions = quick; input = field; reply = body; context = contextLabel; }
        public void ConfigureConversation(ScrollRect scroll,WattsonController wattson){conversation=scroll;companion=wattson;}
        public void Bind(WattsonChatManager chat)
        {
            if(companion!=null&&microphone!=null){
                voiceInput=companion.GetComponent<WattsonVoiceInput>();if(voiceInput==null)voiceInput=companion.gameObject.AddComponent<WattsonVoiceInput>();
                voiceInput.Recognized+=OnRecognized;voiceInput.StatusChanged+=VoiceStatus;
                companion.Speech.Failed+=VoiceStatus;
                microphone.gameObject.AddComponent<HoldToTalk>().Bind(voiceInput);
            }
            WarmLabTheme.Current?.Apply(panel.transform); WarmLabTheme.Current?.StyleButton(open);
            manager=chat;open.onClick.AddListener(()=>SetOpen(true));close.onClick.AddListener(()=>SetOpen(false));send.onClick.AddListener(()=>Submit(input.text));
            input.onEndEdit.AddListener(text=>{
                if(Keyboard.current!=null&&(Keyboard.current.enterKey.wasPressedThisFrame||Keyboard.current.numpadEnterKey.wasPressedThisFrame))Submit(text);
            });
            foreach(var button in suggestions){var message=button.GetComponentInChildren<Text>().text;button.onClick.AddListener(()=>Submit(message));}
        }
        public void SetOpen(bool value)
        {
            if(value&&EventSystem.current!=null)previousSelection=EventSystem.current.currentSelectedGameObject;
            if(!value){pending?.Cancel();voiceInput?.Stop();companion?.StopChatReaction();companion?.ResumeLessonVoice();}
            if(value)panel.transform.SetAsLastSibling();
            panel.SetActive(value);VisibilityChanged?.Invoke(value);
            if(value){RenderHistory();UpdateContext();companion?.FaceLearner();companion?.StopVoice();VoiceStatus("Hold the microphone to talk • release to send");}
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(value?input.gameObject:previousSelection);
        }
        void UpdateContext()
        {
            string activity=manager.Context?.activity;
            string lesson=activity=="Scene08Graph"?"Graph evidence":activity=="Scene07OhmsExperiment"?"Circuit experiment":"Lesson companion";
            context.text=lesson+"  •  "+(manager.LastReplyWasOffline?"Offline lesson guide":manager.LastReplyUsedSearch?"AI conversation · Web checked":manager.UsesConnectedService?"AI conversation · Search available":"Offline lesson guide");
        }
        void AppendTurn(string role,string text)
        {
            if(conversation==null)return;
            var go=new GameObject("ChatTurn",typeof(RectTransform));go.transform.SetParent(conversation.content,false);
            var label=go.AddComponent<Text>();label.font=reply.font;label.fontSize=reply.fontSize;label.supportRichText=false;label.alignment=TextAnchor.UpperLeft;
            label.color=role=="user"?WarmLabTheme.Plum:WarmLabTheme.Ink;label.text=(role=="user"?"YOU":"WATTSON")+"\n"+text;label.raycastTarget=false;
            previousTurns.Add(go);
        }
        void RenderHistory(string pendingQuestion=null,string status=null)
        {
            foreach(var turn in previousTurns){turn.SetActive(false);Destroy(turn);}previousTurns.Clear();
            var history=manager.History;
            int end=history.Count-(pendingQuestion==null&&status==null&&history.Count>0?1:0);
            for(int i=0;i<end;i++){AppendTurn(history[i].role,history[i].text);AppendSources(history[i]);}
            if(pendingQuestion!=null)AppendTurn("user",pendingQuestion);
            reply.text=status??(history.Count>0?"WATTSON\n"+history[history.Count-1].text:"WATTSON\nAsk me a question about electricity or this activity. Asking for help never lowers your mastery.");
            reply.transform.SetAsLastSibling();
            if(pendingQuestion==null&&status==null&&history.Count>0)AppendSources(history[history.Count-1]);
            if(conversation!=null){Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(conversation.content);conversation.verticalNormalizedPosition=0;}
        }
        void AppendSources(WattsonChatTurn turn)
        {
            if(conversation==null||string.IsNullOrEmpty(turn.sourcesUrl))return;
            if(!Uri.TryCreate(turn.sourcesUrl,UriKind.Absolute,out var uri)||(uri.Scheme!="https"&&!(uri.Scheme=="http"&&uri.IsLoopback)))return;
            var go=new GameObject("Google Search sources",typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement));go.transform.SetParent(conversation.content,false);
            go.GetComponent<Image>().color=WarmLabTheme.Sage;go.GetComponent<LayoutElement>().preferredHeight=48;
            go.GetComponent<Button>().onClick.AddListener(()=>Application.OpenURL(turn.sourcesUrl));
            var label=new GameObject("Label",typeof(RectTransform),typeof(Text));label.transform.SetParent(go.transform,false);var rect=(RectTransform)label.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var text=label.GetComponent<Text>();text.font=reply.font;text.fontSize=18;text.text="Google Search · View sources and search suggestions";text.color=WarmLabTheme.Ivory;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;previousTurns.Add(go);
        }
        void VoiceStatus(string message){bool detailed=message.Length>140;if(voiceStatus!=null)voiceStatus.text=detailed?"Voice needs attention — see the steps above. You can still type.":message;if(detailed&&manager!=null&&IsOpen)RenderHistory(null,"VOICE HELP\n"+message);}
        void OnRecognized(string text){if(!IsOpen)return;input.text=text;Submit(text);}
        void SetBusy(bool busy){send.interactable=!busy;if(microphone!=null)microphone.interactable=!busy;foreach(var button in suggestions)button.interactable=!busy;}
        public async void Submit(string message)
        {
            if(string.IsNullOrWhiteSpace(message)||pending!=null||manager==null||!IsOpen)return;
            voiceInput?.Stop();companion?.StopVoice();
            var request=new CancellationTokenSource();pending=request;SetBusy(true);
            try{
                RenderHistory(message,"WATTSON\nThinking… I can check the web when needed.");
                string answer=await manager.SendAsync(message,request.Token);
                if(this==null||!IsOpen)return;
                input.text="";RenderHistory();UpdateContext();companion?.ReactToChat(answer);
            }
            catch(OperationCanceledException){if(this!=null&&IsOpen)RenderHistory();}
            catch(WattsonServiceException error){if(this!=null&&IsOpen){context.text="Wattson · Reply service unavailable";RenderHistory(null,"WATTSON\n"+error.Message+" Your question is still in the input box.");companion?.ReactToChat(error.Message);}}
            catch(Exception){if(this!=null&&IsOpen)RenderHistory(null,"WATTSON\nI couldn't reach the reply service. Your question is still in the input box—please try again.");}
            finally{request.Dispose();if(ReferenceEquals(pending,request))pending=null;if(this!=null)SetBusy(false);}
        }
        void OnDestroy(){pending?.Cancel();if(voiceInput!=null){voiceInput.Recognized-=OnRecognized;voiceInput.StatusChanged-=VoiceStatus;}if(companion!=null&&companion.Speech!=null)companion.Speech.Failed-=VoiceStatus;}
    }
}

