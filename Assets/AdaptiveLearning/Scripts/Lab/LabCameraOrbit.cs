using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    // Bounded orbit keeps the learner above the bench and away from walls.
    public sealed class LabCameraOrbit : MonoBehaviour
    {
        [SerializeField] Vector3 focus = new Vector3(0,.35f,1.4f);
        [SerializeField] float horizontalLimit=45, minElevation=20, maxElevation=60, speed=32;
        LabOrbitJoystick pad;
        CanvasGroup controls;
        Transform[] labels;
        Vector3 homePosition;
        Quaternion homeRotation;
        float homeYaw,homePitch,yaw,pitch,radius;
        bool suspended,ready;

        public bool IsSuspended => suspended;
        public bool IsReady => ready;

        System.Collections.IEnumerator Start()
        {
            // Scenes 12-13 author their tablet controls directly in the saved
            // hierarchy. Bind those controls without rebuilding their UI.
            yield return null;
            if(ready)yield break;
            var existingPad=FindFirstObjectByType<LabOrbitJoystick>(FindObjectsInactive.Include);
            var reset=System.Array.Find(FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None),
                b=>b.name=="ResetCamera"||b.name=="Reset camera");
            if(existingPad!=null)ConfigureExisting(existingPad,reset);
        }

        public void Configure(LabSceneView view)
        {
            if(ready)return;ready=true;
            InitializeHome();
            var root=new GameObject("Explore view controls",typeof(RectTransform),typeof(CanvasGroup));root.transform.SetParent(view.Canvas.transform,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=new Vector2(.025f,.46f);rect.pivot=new Vector2(0,.5f);rect.sizeDelta=new Vector2(150,230);rect.anchoredPosition=Vector2.zero;
            controls=root.GetComponent<CanvasGroup>();
            var label=view.Label("Explore label",rect,"LOOK AROUND",18,new Vector2(0,.88f),Vector2.one);label.alignment=TextAnchor.MiddleCenter;
            var go=new GameObject("View joystick",typeof(RectTransform),typeof(CanvasRenderer),typeof(LabOrbitJoystick));go.transform.SetParent(rect,false);
            var joystickRect=(RectTransform)go.transform;joystickRect.anchorMin=new Vector2(0,.22f);joystickRect.anchorMax=new Vector2(1,.88f);joystickRect.offsetMin=joystickRect.offsetMax=Vector2.zero;pad=go.GetComponent<LabOrbitJoystick>();pad.raycastTarget=true;
            var reset=view.Button("Reset camera",rect,"RESET VIEW",new Vector2(0,0),new Vector2(1,.21f));reset.GetComponentInChildren<Text>().fontSize=18;reset.onClick.AddListener(ResetView);WarmLabTheme.Current?.Apply(rect);
            var canvases=FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var list=new System.Collections.Generic.List<Transform>();foreach(var c in canvases)if(c.renderMode==RenderMode.WorldSpace)list.Add(c.transform);labels=list.ToArray();
        }
        public void ConfigureExisting(LabOrbitJoystick joystick,Button reset)
        {
            if(ready)return;ready=true;InitializeHome();pad=joystick;
            controls=joystick!=null?joystick.GetComponentInParent<CanvasGroup>():null;
            if (controls == null && joystick != null && joystick.transform.parent != null)
                controls=joystick.transform.parent.gameObject.AddComponent<CanvasGroup>();
            if(reset!=null){reset.onClick.RemoveListener(ResetView);reset.onClick.AddListener(ResetView);WarmLabTheme.Current?.StyleButton(reset);}
            var canvases=FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var list=new System.Collections.Generic.List<Transform>();foreach(var c in canvases)if(c.renderMode==RenderMode.WorldSpace)list.Add(c.transform);labels=list.ToArray();
        }
        void InitializeHome()
        {
            homePosition=transform.position;homeRotation=transform.rotation;
            var offset=homePosition-focus;radius=Mathf.Max(.01f,offset.magnitude);
            homeYaw=Mathf.Atan2(offset.x,offset.z)*Mathf.Rad2Deg;homePitch=Mathf.Asin(Mathf.Clamp(offset.y/radius,-1,1))*Mathf.Rad2Deg;
            yaw=homeYaw;pitch=homePitch;
        }
        public void SetSuspended(bool value){suspended=value;if(pad!=null){pad.ResetInput();pad.raycastTarget=!value;}if(controls!=null){controls.alpha=value?0:1;controls.blocksRaycasts=!value;controls.interactable=!value;}}
        public void ResetView(){if(!ready||suspended)return;yaw=homeYaw;pitch=homePitch;transform.SetPositionAndRotation(homePosition,homeRotation);pad?.ResetInput();FaceLabels();}
        void Update()
        {
            if(!ready||suspended)return;
            Vector2 movement=pad.Value;
            var selected=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            if(Keyboard.current!=null&&(selected==null||selected.GetComponent<InputField>()==null))
            {
                var keyboard=Keyboard.current;
                movement+=new Vector2((keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed?1:0),(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed?1:0));
                if(keyboard.homeKey.wasPressedThisFrame){ResetView();return;}
            }
            if(movement.sqrMagnitude<.01f)return;
            Orbit(movement,Time.unscaledDeltaTime);
        }
        public void Orbit(Vector2 movement,float seconds)
        {
            if(!ready||suspended||seconds<0)return;
            movement=Vector2.ClampMagnitude(movement,1);
            yaw=Mathf.Clamp(yaw+movement.x*speed*seconds,homeYaw-horizontalLimit,homeYaw+horizontalLimit);
            pitch=Mathf.Clamp(pitch+movement.y*speed*seconds,minElevation,maxElevation);
            float y=yaw*Mathf.Deg2Rad,p=pitch*Mathf.Deg2Rad;
            float viewingRadius=radius*(1+.22f*Mathf.Clamp01(Mathf.Abs(yaw-homeYaw)/horizontalLimit));
            transform.position=focus+viewingRadius*new Vector3(Mathf.Sin(y)*Mathf.Cos(p),Mathf.Sin(p),Mathf.Cos(y)*Mathf.Cos(p));
            transform.LookAt(focus);FaceLabels();
        }
        void FaceLabels(){if(labels==null)return;foreach(var label in labels)if(label!=null)label.rotation=transform.rotation;}
        void OnApplicationFocus(bool focused){if(!focused)pad?.ResetInput();}
    }
}
