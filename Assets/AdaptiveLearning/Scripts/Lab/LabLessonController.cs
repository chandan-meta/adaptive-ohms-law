using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    public sealed class LabLessonController : MonoBehaviour
    {
        [SerializeField] LabLessonDefinition definition;
        [SerializeField] GameObject wattsonPrefab;
        [SerializeField] WattsonServiceSettings service;
        [SerializeField] LabSceneView view;
        [SerializeField] WattsonController wattson;
        WattsonChatUI chatUI;
        LabCameraOrbit cameraOrbit;
        StudentProfileManager profiles;
        CircuitAssemblyModel circuit=new CircuitAssemblyModel();
        CircuitAssemblyRecord record;
        int step,selected=-1;
        bool chatOpen;
        string recentMistake="";
        float activeSeconds;
        public CircuitAssemblyModel Circuit=>circuit;
        public void Configure(LabLessonDefinition data,GameObject companion,WattsonServiceSettings settings){definition=data;wattsonPrefab=companion;service=settings;}
        public void SetSceneObjects(LabSceneView authoredView,WattsonController companion){view=authoredView;wattson=companion;}
        void Start(){
            if(definition==null||wattsonPrefab==null){Debug.LogError("Lab lesson assets are missing.");return;}

            if(view==null||wattson==null){Debug.LogError("Open the authored lab scene: its view or Wattson reference is missing.");return;}view.Title.text=definition.title;
            
            var presenter=gameObject.AddComponent<DialoguePresenter>();var speaker=view.Progress;var audio=gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;presenter.Configure(speaker,view.Subtitle,audio);wattson.Bind(view.Camera.transform,presenter);
            profiles=gameObject.AddComponent<StudentProfileManager>();profiles.Initialize(50);profiles.Profile.circuitAssemblies ??= new System.Collections.Generic.List<CircuitAssemblyRecord>();
            if(definition.buildCircuit){record=new CircuitAssemblyRecord{startedUtc=DateTime.UtcNow.ToString("o")};profiles.Profile.circuitAssemblies.Add(record);}
            cameraOrbit=view.Camera.GetComponent<LabCameraOrbit>();if(cameraOrbit==null)cameraOrbit=view.Camera.gameObject.AddComponent<LabCameraOrbit>();cameraOrbit.Configure(view);
            BuildChat();
            view.Next.onClick.AddListener(Advance);view.Hint.onClick.AddListener(Hint);view.Key.onClick.AddListener(CloseCircuit);
            for(int i=0;i<view.Terminals.Length;i++){int index=i;view.Terminals[i].onClick.AddListener(()=>SelectTerminal(index));view.Terminals[i].interactable=definition.buildCircuit;}
            view.Key.interactable=definition.buildCircuit;view.Hint.gameObject.SetActive(definition.buildCircuit);
            if(definition.buildCircuit){view.Next.interactable=false;Say(definition.steps[0].text);UpdateProgress();}else ShowStep();
        }
        void Update(){if(record!=null&&!record.completed&&!chatOpen)activeSeconds+=Time.deltaTime;}
        void Say(string text){view.Subtitle.text=text;wattson.ReactToChat(text);}
        void ShowStep(){view.Highlight(new[]{1,3,2,0,4}[Mathf.Min(step,4)]);Say(definition.steps[step].text);view.Progress.text="Meet your instruments  •  "+(step+1)+" / "+definition.steps.Length;view.Next.GetComponentInChildren<Text>().text=step==definition.steps.Length-1?"BUILD THE CIRCUIT":"NEXT INSTRUMENT";}
        void Advance(){if(chatOpen)return;if(definition.buildCircuit&&!circuit.Closed)return;if(!definition.buildCircuit&&++step<definition.steps.Length){ShowStep();return;}Complete();wattson.StopVoice();SceneManager.LoadScene(definition.nextScene);}
        public void SelectTerminal(int index){
            if(!definition.buildCircuit||chatOpen||circuit.Closed)return;
            if(selected<0){selected=index;TintTerminal(view.Terminals[index],WarmLabTheme.Amber);view.Progress.text="Selected "+CircuitAssemblyModel.Terminals[index]+". Tap the other end of this lead.";return;}
            int first=selected;selected=-1;TintTerminal(view.Terminals[first],WarmLabTheme.Sage);
            if(first==index){UpdateProgress();return;}
            if(circuit.Connect(first,index,out var feedback)){view.Connect(first,index);record.connections=circuit.Count;Say(feedback);}
            else {record.mistakes++;recentMistake=feedback;Say(feedback);if(record.mistakes>=2)ShowHint();}
            SaveRecord();UpdateProgress();
        }
        static void TintTerminal(Button button,Color tint){button.image.color=Color.white;var colors=button.colors;colors.normalColor=tint;colors.selectedColor=tint;colors.highlightedColor=tint;button.colors=colors;foreach(var text in button.GetComponentsInChildren<Text>())text.color=tint==WarmLabTheme.Amber?WarmLabTheme.Ink:WarmLabTheme.Ivory;}
        void UpdateProgress(){view.Progress.text=circuit.Closed?"Investigation ready • next: collect measurements":circuit.Count+" / 6 leads connected • "+(record.mistakes>=2?"Guided support":"Tap two terminals to connect a lead");}
        void Hint(){if(chatOpen||!definition.buildCircuit)return;record.hints++;ShowHint();SaveRecord();}
        void ShowHint(){circuit.NextHint(out int a,out int b);if(a<0){Say("All connections are ready. Tap OPEN on the key to close the circuit.");return;}TintTerminal(view.Terminals[a],WarmLabTheme.Amber);TintTerminal(view.Terminals[b],WarmLabTheme.Amber);Say("Connect "+CircuitAssemblyModel.Terminals[a]+" to "+CircuitAssemblyModel.Terminals[b]+". The highlighted terminals are the ends of one lead.");}
        public void CloseCircuit(){if(chatOpen||!definition.buildCircuit)return;if(!circuit.Close()){Say("Keep the key open until all six leads are connected. Complete the main loop and the voltmeter branch first.");return;}view.CloseKeyVisual();view.Key.GetComponentInChildren<Text>().text="CLOSED";view.Key.interactable=false;view.Ammeter.text="I  0.20 A";view.Voltmeter.text="V  1.00 V";view.Next.interactable=true;view.Next.GetComponentInChildren<Text>().text="START EXPERIMENT";view.Hint.interactable=false;foreach(var b in view.Terminals)b.interactable=false;Say(definition.steps[1].text);Complete();UpdateProgress();}
        void SaveRecord(){if(record!=null)record.responseSeconds=activeSeconds;profiles.Save();}
        void Complete(){string id=definition.buildCircuit?"Scene09BuildCircuit":"Scene08OurLabIntroduction";if(!profiles.Profile.completedActivities.Contains(id))profiles.Profile.completedActivities.Add(id);if(record!=null){record.completed=true;record.completedUtc=DateTime.UtcNow.ToString("o");}SaveRecord();}
        void BuildChat(){
            var manager=gameObject.AddComponent<WattsonChatManager>();manager.SetService(service);manager.Configure(()=>new LearningContext{scene=gameObject.scene.name,activity=definition.buildCircuit?"Scene09BuildCircuit":"Scene08OurLabIntroduction",step=definition.buildCircuit?circuit.Count+" of 6 leads connected":definition.steps[Mathf.Min(step,definition.steps.Length-1)].text,concept=definition.buildCircuit?ConceptId.SeriesConnection:ConceptId.Ammeter,mastery=profiles.Profile.Mastery(ConceptId.SeriesConnection),support=record!=null&&record.mistakes>=2?SupportLevel.Guided:SupportLevel.Independent,recentMistake=recentMistake,hasCircuitMeasurements=circuit.Closed,switchClosed=circuit.Closed,sourceVoltage=1,circuitResistance=5,currentReading=circuit.Closed?.2f:0,resistorVoltageReading=circuit.Closed?1:0});
            var ask=view.Button("AskWattson",view.Canvas.transform,"ASK WATTSON",new Vector2(.80f,.91f),new Vector2(.965f,.98f));
            var panel=view.Area("ChatCard",view.Canvas.transform,new Vector2(.035f,.055f),new Vector2(.67f,.89f),new Color(.025f,.07f,.095f,.99f));
            view.Label("ChatTitle",panel,"Talk to Wattson",32,new Vector2(.04f,.90f),new Vector2(.7f,.98f));
            var close=view.Button("CloseChat",panel,"CLOSE",new Vector2(.78f,.90f),new Vector2(.96f,.98f));
            var context=view.Label("ChatContext",panel,"Lesson companion",18,new Vector2(.04f,.83f),new Vector2(.96f,.89f));
            var scrollArea=view.Area("ConversationScroll",panel,new Vector2(.04f,.39f),new Vector2(.96f,.815f),new Color(.02f,.055f,.07f));scrollArea.gameObject.AddComponent<RectMask2D>();var scroll=scrollArea.gameObject.AddComponent<ScrollRect>();scroll.viewport=scrollArea;scroll.horizontal=false;
            var content=view.Area("Content",scrollArea,new Vector2(0,1),Vector2.one);content.pivot=new Vector2(.5f,1);var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;layout.spacing=18;var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=content;
            var reply=view.Label("Reply",content,"",24,Vector2.zero,Vector2.one);
            var quick=view.Area("QuickQuestions",panel,new Vector2(.04f,.27f),new Vector2(.96f,.36f));var q1=view.Button("QuickHelp",quick,"What should I do next?",Vector2.zero,new Vector2(.48f,1));var q2=view.Button("QuickMeters",quick,"What does a voltmeter measure?",new Vector2(.52f,0),Vector2.one);
            var inputArea=view.Area("MessageInput",panel,new Vector2(.245f,.055f),new Vector2(.755f,.18f),new Color(.07f,.14f,.18f));var input=inputArea.gameObject.AddComponent<InputField>();input.characterLimit=1200;input.textComponent=view.Label("InputText",inputArea,"",22,new Vector2(.025f,.05f),new Vector2(.975f,.95f));input.placeholder=view.Label("Placeholder",inputArea,"Ask your question…",22,new Vector2(.025f,.05f),new Vector2(.975f,.95f));
            var send=view.Button("Send",panel,"SEND",new Vector2(.78f,.055f),new Vector2(.96f,.18f));var mic=view.Button("VoiceInput",panel,"MIC",new Vector2(.04f,.055f),new Vector2(.225f,.18f));var status=view.Label("VoiceStatus",panel,"Hold the microphone to talk • release to send",16,new Vector2(.04f,.005f),new Vector2(.96f,.05f));
            chatUI=gameObject.AddComponent<WattsonChatUI>();chatUI.Configure(panel.gameObject,ask,close,send,new[]{q1,q2},input,reply,context);chatUI.ConfigureConversation(scroll,wattson);chatUI.ConfigureVoice(mic,status);chatUI.Bind(manager);chatUI.VisibilityChanged+=open=>{chatOpen=open;cameraOrbit.SetSuspended(open);if(!open)wattson.ReactToChat(view.Subtitle.text);};manager.ExplanationRequested+=()=>{if(record!=null){record.hints++;SaveRecord();}};WarmLabTheme.Current?.Apply(panel);panel.gameObject.SetActive(false);
        }
    }
}



