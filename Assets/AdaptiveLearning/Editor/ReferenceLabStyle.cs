using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace AdaptiveOhmsLaw.Editor
{
    public static class ReferenceLabStyle
    {
        const string Root="Assets/AdaptiveLearning/";
        static Color Cyan=new Color(.03f,.85f,.95f);
        static void Box(RectTransform r,float x,float y,float w,float h){r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(w,h);r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Skin(Image image,Sprite sprite,Color edge){image.sprite=sprite;image.type=Image.Type.Sliced;image.color=new Color(.012f,.055f,.09f,.96f);var outline=image.GetComponent<Outline>()??image.gameObject.AddComponent<Outline>();outline.effectColor=edge;outline.effectDistance=new Vector2(1.6f,-1.6f);}
        static Material Material(string name,Color color,bool glow=false){string p=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.3f);if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2);}EditorUtility.SetDirty(m);return m;}
        static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 size,Material mat){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent);o.transform.localPosition=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
        static void Sign(string text,Transform parent,Vector3 position,float size,Color color){var o=new GameObject(text,typeof(TextMesh));o.transform.SetParent(parent,false);o.transform.localPosition=position;var t=o.GetComponent<TextMesh>();t.text=text;t.fontSize=70;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=color;}
        public static void ApplyAndValidate(){
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Interface/RoundedPanel.png");
            var wood=Material("ReferenceWarmWood",new Color(.64f,.32f,.13f));
            var texture=new Texture2D(512,128,TextureFormat.RGB24,false);for(int y=0;y<128;y++)for(int x=0;x<512;x++){float noise=Mathf.PerlinNoise(x*.004f,y*.10f);texture.SetPixel(x,y,Color.Lerp(new Color(.48f,.28f,.13f),new Color(.64f,.40f,.22f),noise));}texture.Apply();string tp=Root+"Art/Interface/BenchWood.png";File.WriteAllBytes(tp,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(tp);wood.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tp));wood.SetColor("_BaseColor",Color.white);
            foreach(string name in new[]{"Scene_08_OurLabIntroduction","Scene_09_BuildTheCircuit"}){
                EditorSceneManager.OpenScene(Root+"Scenes/"+name+".unity");var view=Object.FindFirstObjectByType<LabSceneView>();view.PanelSprite=sprite;
                view.Camera.transform.position=new Vector3(0,4.5f,-6.7f);view.Camera.transform.LookAt(new Vector3(0,.35f,1.45f));view.Camera.usePhysicalProperties=true;view.Camera.sensorSize=new Vector2(36,20.25f);view.Camera.focalLength=24.4f;view.Camera.gateFit=Camera.GateFitMode.Horizontal;
                var env=GameObject.Find("Environment").transform;
                foreach(var r in env.GetComponentsInChildren<Renderer>())if(r.name=="Workbench"||r.name.StartsWith("Shelf"))r.sharedMaterial=wood;
                var amber=Material("ReferenceAmberLight",new Color(1,.48f,.08f),true);var navy=Material("ReferenceNavy",new Color(.02f,.05f,.08f));
                Cube("Bench front light strip",env,new Vector3(0,-.04f,-1.19f),new Vector3(9,.018f,.02f),amber);
                Cube("Bench back light strip",env,new Vector3(0,.035f,3.5f),new Vector3(9,.025f,.02f),amber);
                for(int i=0;i<3;i++)Cube("Shelf light "+i,env,new Vector3(-4+i*3,.50f,5.1f),new Vector3(2.6f,.025f,.025f),amber);
                var warm=new GameObject("Warm tabletop pool",typeof(Light)).GetComponent<Light>();warm.transform.SetParent(env);warm.transform.position=new Vector3(0,3,1);warm.type=LightType.Point;warm.range=7;warm.intensity=2.5f;warm.color=new Color(1,.65f,.32f);warm.shadows=LightShadows.None;
                var keyLight=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);keyLight.color=new Color(1,.87f,.72f);keyLight.intensity=1.7f;
                var fill=new GameObject("Cool lab fill",typeof(Light)).GetComponent<Light>();fill.transform.SetParent(env);fill.type=LightType.Directional;fill.shadows=LightShadows.None;fill.intensity=.45f;fill.color=new Color(.5f,.75f,1);fill.transform.rotation=Quaternion.Euler(40,135,0);
                Cube("Circuit blueprint",env,new Vector3(-5.8f,1.25f,5.8f),new Vector3(1.7f,2.1f,.04f),Material("ReferenceBlueprint",new Color(.025f,.11f,.25f)));
                Sign("SIMPLE\nELECTRICAL\nCIRCUIT",env,new Vector3(-5.8f,1.25f,5.74f),.028f,new Color(.5f,.8f,1));
                for(int i=0;i<3;i++){var book=Cube("Reference book "+i,env,new Vector3(4.0f,.12f+i*.22f,.5f),new Vector3(.85f,.2f,1.2f),i==1?wood:navy);Sign(i==0?"CIRCUITS":i==1?"ELECTRICITY":"EXPERIMENTS",book.transform,new Vector3(0,0,-.51f),.007f,Color.white);}
                var footer=view.Subtitle.transform.parent.GetComponent<RectTransform>();Box(footer,.025f,.025f,.975f,.205f);Skin(footer.GetComponent<Image>(),sprite,new Color(.12f,.55f,.7f));
                Box(view.Subtitle.rectTransform,.095f,.25f,.755f,.73f);view.Subtitle.fontSize=25;Box(view.Progress.rectTransform,.095f,.02f,.75f,.2f);view.Progress.fontSize=17;
                var speaker=footer.Find("Speaker") as RectTransform;Box(speaker,.095f,.77f,.73f,.96f);
                var hintIcon=view.Label("Hint emblem",footer,"?",44,new Vector2(.018f,.3f),new Vector2(.075f,.75f));hintIcon.font=view.Subtitle.font;hintIcon.alignment=TextAnchor.MiddleCenter;hintIcon.color=Cyan;
                var bubble=view.Area("Wattson help bubble",view.Canvas.transform,new Vector2(.80f,.75f),new Vector2(.975f,.865f),Color.white);Skin(bubble.GetComponent<Image>(),sprite,new Color(.65f,.88f,1));var bubbleText=view.Label("Help message",bubble,"Need help?\\nAsk me anything!".Replace("\\n","\n"),22,new Vector2(.07f,.08f),new Vector2(.93f,.92f));bubbleText.font=view.Subtitle.font;
                var header=view.Area("Lesson badge",view.Canvas.transform,new Vector2(.025f,.91f),new Vector2(.30f,.98f),Color.white);Skin(header.GetComponent<Image>(),sprite,Cyan);view.Title.transform.SetParent(header,false);Box(view.Title.rectTransform,.045f,0,.97f,1);view.Title.fontSize=27;view.Title.fontStyle=FontStyle.Bold;view.Title.color=Color.white;
                foreach(var button in view.Terminals.Concat(new[]{view.Key,view.Hint,view.Next})){Skin(button.image,sprite,button==view.Key?new Color(1,.55f,.1f):Cyan);var label=button.GetComponentInChildren<Text>();label.fontStyle=FontStyle.Bold;var c=button.colors;c.normalColor=Color.white;c.disabledColor=new Color(.6f,.65f,.7f,.8f);button.colors=c;}
                foreach(var button in view.Terminals){var c=button.GetComponentInParent<Canvas>();((RectTransform)c.transform).sizeDelta=new Vector2(75,48);c.transform.rotation=view.Camera.transform.rotation;
                    int index=System.Array.IndexOf(view.Terminals,button);float inset=index>=6?.15f:.08f;
                    var visual=view.Area("Compact visual - full button remains touch target",button.transform,new Vector2(inset,inset),new Vector2(1-inset,1-inset),Color.white);Skin(visual.GetComponent<Image>(),sprite,Cyan);
                    var label=button.GetComponentInChildren<Text>();label.transform.SetParent(visual,false);Box(label.rectTransform,.03f,0,.97f,1);label.fontSize=17;
                    Object.DestroyImmediate(button.GetComponent<Outline>());button.GetComponent<Image>().color=Color.clear;button.targetGraphic=visual.GetComponent<Image>();
                }
                var keyCanvas=view.Key.GetComponentInParent<Canvas>();keyCanvas.transform.rotation=view.Camera.transform.rotation;view.Key.GetComponentInChildren<Text>().color=new Color(1,.69f,.25f);
                foreach(var labelCanvas in GameObject.Find("Instrument labels").GetComponentsInChildren<Canvas>()){labelCanvas.transform.rotation=view.Camera.transform.rotation;if(labelCanvas.name!="KEY"&&labelCanvas.name!="VOLTMETER"){var panel=labelCanvas.gameObject.AddComponent<Image>();Skin(panel,sprite,new Color(.03f,.3f,.4f,.5f));panel.raycastTarget=false;}}
                var red=Material("ReferenceRedLead",new Color(.7f,.025f,.018f));var black=Material("ReferenceBlackLead",new Color(.018f,.018f,.025f));foreach(var route in view.WireRoutes)route.line.sharedMaterial=route.a==0||route.a==6?red:black;
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
            EditorUtility.SetDirty(wood);AssetDatabase.SaveAssets();LabValidation.Run();
        }
    }
}



