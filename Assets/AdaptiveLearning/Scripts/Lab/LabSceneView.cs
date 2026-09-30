using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    // Shared responsive view for the instrument introduction and circuit assembly.
    public sealed class LabSceneView : MonoBehaviour
    {
        public Canvas Canvas;
        public Sprite PanelSprite;
        public Text Subtitle,Title,Progress;
        public Button Next,Hint,Key;
        public Button[] Terminals;
        public Transform[] Devices;
        public Vector3[] Points;
        public Text Ammeter,Voltmeter;
        public Camera Camera;
        public Transform KeyLever;
        public void CloseKeyVisual(){if(KeyLever!=null)KeyLever.localRotation=Quaternion.identity;foreach(var device in Devices)foreach(var meter in device.GetComponentsInChildren<LabMeterVisual>())meter.ShowCompletedCircuit();}
        public readonly Color Teal=new Color(.18f,.83f,.82f);
        Font font;
        Material metal;
        [SerializeField] Material wire;
        public LabWireRoute[] WireRoutes;
        public GameObject[] FocusMarkers;
        void Awake(){font=WarmLabTheme.Current != null ? WarmLabTheme.Current.body : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");}
        public RectTransform Area(string name,Transform parent,Vector2 min,Vector2 max,Color? color=null){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;if(color.HasValue)go.AddComponent<Image>().color=color.Value;return r;}
        public Text Label(string name,Transform parent,string text,int size,Vector2 min,Vector2 max){var r=Area(name,parent,min,max);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.supportRichText=false;return t;}
        public Button Button(string name,Transform parent,string text,Vector2 min,Vector2 max){var r=Area(name,parent,min,max,new Color(.055f,.25f,.31f));var b=r.gameObject.AddComponent<Button>();if(PanelSprite!=null){var image=r.GetComponent<Image>();image.sprite=PanelSprite;image.type=Image.Type.Sliced;image.color=new Color(.015f,.10f,.15f,.97f);var edge=r.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.05f,.83f,.95f,.8f);edge.effectDistance=new Vector2(1.5f,-1.5f);}var t=Label(name+"Label",r,text,22,new Vector2(.06f,0),new Vector2(.94f,1));t.alignment=TextAnchor.MiddleCenter;WarmLabTheme.Current?.StyleButton(b);return b;}
        GameObject Shape(string name,Vector3 position,Vector3 scale,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;if(Application.isPlaying)Object.Destroy(go.GetComponent<Collider>());else Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        Material Material(Color c){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=c;return m;}
        public void Build(){
            font=WarmLabTheme.Current != null ? WarmLabTheme.Current.body : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");metal=Material(new Color(.13f,.23f,.29f));wire=Material(Teal);
            Camera=new GameObject("LearnerCamera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();Camera.tag="MainCamera";Camera.transform.position=new Vector3(0,.35f,-12);Camera.orthographic=true;Camera.orthographicSize=4.7f;Camera.backgroundColor=new Color(.025f,.055f,.075f);Camera.clearFlags=CameraClearFlags.SolidColor;
            var light=new GameObject("SoftLabLight",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(25,-30,0);light.shadows=LightShadows.None;RenderSettings.ambientLight=Color.gray;
            Shape("Modern bench",new Vector3(0,-1.8f,.6f),new Vector3(10,.25f,2),metal);
            var wall=new Material(Shader.Find("Universal Render Pipeline/Unlit"));wall.SetColor("_BaseColor",new Color(.025f,.055f,.075f));Shape("Lab wall",new Vector3(0,1,2),new Vector3(20,12,.2f),wall);
            Devices=new Transform[5];Vector3[] positions={new Vector3(-3,1.8f,0),new Vector3(-.8f,2.1f,0),new Vector3(1.7f,2.1f,0),new Vector3(1.7f,-.1f,0),new Vector3(-3,-.1f,0)};
            string[] names={"DC SOURCE","AMMETER","NICHROME WIRE XY","VOLTMETER","KEY / SWITCH"};
            for(int i=0;i<5;i++){Devices[i]=Shape(names[i],positions[i],new Vector3(i==2?2:1.65f,.85f,.45f),metal).transform;WorldLabel(names[i],positions[i]+new Vector3(0,.7f,-.3f),2.4f);}
            Shape("Nichrome XY",positions[2]+new Vector3(0,0,-.3f),new Vector3(1.5f,.045f,.045f),Material(new Color(.7f,.72f,.74f)));
            Ammeter=WorldLabel("I   — A",positions[1]+new Vector3(0,0,-.3f),1.3f);Voltmeter=WorldLabel("V   — V",positions[3]+new Vector3(0,0,-.3f),1.3f);
            WorldLabel("1 V",positions[0]+new Vector3(0,0,-.3f),1.3f);
            Canvas=new GameObject("LabInterface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();Canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=Canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            Title=Label("LessonTitle",Canvas.transform,"",28,new Vector2(.035f,.91f),new Vector2(.74f,.98f));Title.color=Teal;
            var bottom=Area("WattsonDialogue",Canvas.transform,new Vector2(.035f,.035f),new Vector2(.965f,.24f),new Color(.025f,.085f,.11f,.97f));
            Label("Speaker",bottom,"WATTSON",18,new Vector2(.025f,.78f),new Vector2(.7f,.98f)).color=Teal;
            Subtitle=Label("Subtitle",bottom,"",26,new Vector2(.025f,.21f),new Vector2(.76f,.8f));
            Progress=Label("Progress",bottom,"",18,new Vector2(.025f,.01f),new Vector2(.73f,.2f));
            Next=Button("Continue",bottom,"CONTINUE",new Vector2(.80f,.1f),new Vector2(.98f,.48f));Hint=Button("Hint",bottom,"ASK FOR A CLUE",new Vector2(.80f,.55f),new Vector2(.98f,.93f));
            Points=new[]{positions[0]+new Vector3(-.45f,-.8f,-.5f),positions[0]+new Vector3(.45f,-.8f,-.5f),positions[1]+new Vector3(-.45f,-.8f,-.5f),positions[1]+new Vector3(.45f,-.8f,-.5f),positions[2]+new Vector3(-.7f,-.8f,-.5f),positions[2]+new Vector3(.7f,-.8f,-.5f),positions[3]+new Vector3(-.45f,-.8f,-.5f),positions[3]+new Vector3(.45f,-.8f,-.5f),positions[4]+new Vector3(-.45f,-.8f,-.5f),positions[4]+new Vector3(.45f,-.8f,-.5f)};
            var swap=Points[0];Points[0]=Points[1];Points[1]=swap;swap=Points[8];Points[8]=Points[9];Points[9]=swap;
            Terminals=new Button[10];for(int i=0;i<10;i++){var c=WorldCanvas("Terminal "+i,Points[i],.73f,.62f);Terminals[i]=Button("Terminal "+i,c.transform,CircuitAssemblyModel.Terminals[i],Vector2.zero,Vector2.one);Terminals[i].GetComponentInChildren<Text>().fontSize=18;}
            var keyCanvas=WorldCanvas("Key control",positions[4]+new Vector3(0,0,-.35f),1.35f,.62f);Key=Button("Close key",keyCanvas.transform,"OPEN",Vector2.zero,Vector2.one);
        }
        Canvas WorldCanvas(string name,Vector3 pos,float width,float height){var c=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster)).GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.worldCamera=Camera;var r=(RectTransform)c.transform;r.sizeDelta=new Vector2(width*100,height*100);r.position=pos;r.localScale=Vector3.one*.01f;return c;}
        Text WorldLabel(string text,Vector3 pos,float width){var c=WorldCanvas(text,pos,width,.42f);var t=Label("Label",c.transform,text,20,Vector2.zero,Vector2.one);t.alignment=TextAnchor.MiddleCenter;return t;}
        public void Connect(int a,int b){foreach(var route in WireRoutes)if((route.a==a&&route.b==b)||(route.a==b&&route.b==a)){route.Show();return;}Debug.LogError("Missing circuit lead route.");}
        public void Highlight(int index){for(int i=0;i<FocusMarkers.Length;i++)FocusMarkers[i].SetActive(i==index);}
    }
}


