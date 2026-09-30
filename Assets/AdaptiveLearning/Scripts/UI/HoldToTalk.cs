using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace AdaptiveOhmsLaw
{
    // Pointer capture belongs to the initiating finger/mouse; dismissal always cancels.
    public sealed class HoldToTalk : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        WattsonVoiceInput voice;
        Button button;
        int? pointer;
        Text device;
        Image level;
        MicrophoneIcon micIcon;
        public void Bind(WattsonVoiceInput input){voice=input;button=GetComponent<Button>();
            var card=transform.parent;
            Layout(card.Find("QuickQuestions") as RectTransform,.27f,.36f);Layout(card.Find("ConversationScroll") as RectTransform,.39f,.815f);
            var panel=new GameObject("MicrophoneMonitor",typeof(RectTransform),typeof(Image));panel.transform.SetParent(card,false);
            var rect=(RectTransform)panel.transform;rect.anchorMin=new Vector2(.04f,.19f);rect.anchorMax=new Vector2(.96f,.255f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            panel.AddComponent<Button>().onClick.AddListener(()=>voice.NextDevice());voice.NextDevice();
#endif
            panel.GetComponent<Image>().color=WarmLabTheme.Sage;
            var label=new GameObject("Device",typeof(RectTransform),typeof(Text));label.transform.SetParent(panel.transform,false);
            rect=(RectTransform)label.transform;rect.anchorMin=new Vector2(.02f,.3f);rect.anchorMax=new Vector2(.98f,1);rect.offsetMin=rect.offsetMax=Vector2.zero;
            device=label.GetComponent<Text>();device.font=GetComponentInChildren<Text>().font;device.fontSize=16;device.resizeTextForBestFit=true;device.resizeTextMinSize=12;device.resizeTextMaxSize=16;device.color=Color.white;device.alignment=TextAnchor.MiddleLeft;device.raycastTarget=false;
            var bar=new GameObject("LiveLevel",typeof(RectTransform),typeof(Image));bar.transform.SetParent(panel.transform,false);level=bar.GetComponent<Image>();level.color=WarmLabTheme.Amber;level.raycastTarget=false;
            GetComponentInChildren<Text>().text="";
            var icon=new GameObject("MicIcon",typeof(RectTransform),typeof(CanvasRenderer),typeof(MicrophoneIcon));icon.transform.SetParent(transform,false);rect=(RectTransform)icon.transform;rect.anchorMin=new Vector2(.20f,.23f);rect.anchorMax=new Vector2(.80f,.94f);rect.offsetMin=rect.offsetMax=Vector2.zero;micIcon=icon.GetComponent<MicrophoneIcon>();micIcon.color=Color.white;micIcon.raycastTarget=false;
            var caption=new GameObject("Hold to talk label",typeof(RectTransform),typeof(Text));caption.transform.SetParent(transform,false);rect=(RectTransform)caption.transform;rect.anchorMin=new Vector2(0,.02f);rect.anchorMax=new Vector2(1,.25f);rect.offsetMin=rect.offsetMax=Vector2.zero;var text=caption.GetComponent<Text>();text.font=device.font;text.fontSize=14;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;text.text="HOLD TO TALK";
            Update();
        }
        static void Layout(RectTransform r,float bottom,float top){if(r==null)return;r.anchorMin=new Vector2(.04f,bottom);r.anchorMax=new Vector2(.96f,top);r.offsetMin=r.offsetMax=Vector2.zero;}
        public void OnPointerDown(PointerEventData e){if(pointer.HasValue||!button.IsInteractable()||voice.Processing||e.button!=PointerEventData.InputButton.Left)return;pointer=e.pointerId;voice.Listen();}
        public void OnPointerUp(PointerEventData e){if(pointer!=e.pointerId)return;pointer=null;voice.Release();}
        void Update(){if(voice==null||device==null)return;if(micIcon!=null)micIcon.color=voice.Listening?new Color(1,.35f,.25f):Color.white;device.text=voice.DeviceName+"  •  "+(voice.Listening?(voice.Level>.015f?"Signal detected":"Quiet / no signal"):voice.Processing?"Transcribing":"Tap here to change mic");var rect=(RectTransform)level.transform;rect.anchorMin=new Vector2(.02f,.1f);rect.anchorMax=new Vector2(.02f+.96f*voice.Level,.23f);rect.offsetMin=rect.offsetMax=Vector2.zero;}
        void OnDisable(){pointer=null;voice?.Stop();}
    }
}



