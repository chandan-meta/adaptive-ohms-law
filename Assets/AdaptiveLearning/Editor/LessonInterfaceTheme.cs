using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class LessonInterfaceTheme
    {
        static LessonInterfaceTheme(){EditorApplication.update+=()=>{
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Logs/lesson-theme.req")){File.Delete("Logs/lesson-theme.req");try{Apply();File.WriteAllText("Logs/lesson-theme.txt","COMPLETE");}catch(Exception e){File.WriteAllText("Logs/lesson-theme.txt",e.ToString());Debug.LogException(e);}}
            if(File.Exists("Logs/wattson-connect.req")){File.Delete("Logs/wattson-connect.req");var settings=AssetDatabase.LoadAssetAtPath<WattsonServiceSettings>("Assets/AdaptiveLearning/ScriptableObjects/WattsonService.asset");settings.chatEndpoint="http://127.0.0.1:8787/chat";EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();}
        };}
        static Sprite Rounded(){
            const string path="Assets/AdaptiveLearning/Art/Interface/RoundedPanel.png";Directory.CreateDirectory(Path.GetDirectoryName(path));
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(Mathf.Abs(x-31.5f)-19.5f,0),dy=Mathf.Max(Mathf.Abs(y-31.5f)-19.5f,0);texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(12.5f-Mathf.Sqrt(dx*dx+dy*dy))));}
            texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=new Vector4(16,16,16,16);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite==null)throw new InvalidOperationException("Rounded interface sprite did not import.");return sprite;
        }
        [MenuItem("Adaptive Learning/Apply Shared Interface Theme")]
        public static void Apply(){var rounded=Rounded();
            foreach(var path in new[]{Scene06Builder.ScenePath,Scene07Builder.ScenePath,Scene08Builder.ScenePath}){
                EditorSceneManager.OpenScene(path);
                foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                    if(button.name.StartsWith("Plot_")||button.name=="VoltageKnob")continue;
                    var graphic=button.targetGraphic as Image;if(graphic==null)continue;graphic.sprite=rounded;graphic.type=Image.Type.Sliced;graphic.color=Color.white;
                    bool primary=button.name=="AskWattson"||button.name=="Send"||button.name=="Continue"||button.name=="RecordReading";
                    Color normal=primary?new Color(.035f,.32f,.36f):new Color(.10f,.17f,.23f);
                    if(button.name=="VoiceInput")normal=new Color(.36f,.22f,.09f);
                    var colors=button.colors;colors.normalColor=normal;colors.highlightedColor=Color.Lerp(normal,new Color(.2f,.63f,.66f),.35f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.05f,.45f,.49f);colors.disabledColor=new Color(.08f,.11f,.14f,.65f);colors.colorMultiplier=1;colors.fadeDuration=.12f;button.colors=colors;
                    foreach(var text in button.GetComponentsInChildren<Text>(true)){text.color=new Color(.94f,.97f,1);text.fontStyle=FontStyle.Normal;}
                }
                foreach(var panel in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                    if(panel.name=="ChatCard"||panel.name=="DialoguePanel"||panel.name=="ObservationTablePanel"||panel.name=="MeterUIPanel"||panel.name=="GraphPanel"||panel.name=="Completion"){
                        panel.sprite=rounded;panel.type=Image.Type.Sliced;panel.color=new Color(.025f,.052f,.077f,panel.name=="DialoguePanel"?.96f:1);
                    }
                }
                var title=LessonReviewTools.Find<Text>("Lesson");title.color=new Color(.36f,.85f,.86f);title.fontStyle=FontStyle.Bold;
                var objective=LessonReviewTools.Find<Text>("Objective");objective.fontSize=22;objective.resizeTextMaxSize=22;objective.resizeTextMinSize=19;objective.color=new Color(.88f,.93f,.98f);
                LessonReviewTools.Set(LessonReviewTools.Find<RectTransform>("ChatCard"),.035f,.075f,.595f,.93f);
                LessonReviewTools.Find<Text>("ChatTitle").text="Talk to Wattson";
                LessonReviewTools.Find<Text>("ChatTitle").fontStyle=FontStyle.Bold;
                var reply=LessonReviewTools.Find<Text>("Reply");reply.lineSpacing=1.15f;
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Scene06Builder.ScenePath);
        }
    }
}
