using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class WattsonChatUpgrade
    {
        static WattsonChatUpgrade(){EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("WattsonChatValidation",false))new GameObject("Wattson conversation validation").AddComponent<WattsonChatValidationRunner>();
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool("WattsonChatValidation",false);
        };EditorApplication.update+=()=>{
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Logs/wattson-chat-validate.req")){File.Delete("Logs/wattson-chat-validate.req");Validate();return;}
            if(!File.Exists("Logs/wattson-chat-upgrade.req"))return;
            File.Delete("Logs/wattson-chat-upgrade.req");try{Apply();File.WriteAllText("Logs/wattson-chat-upgrade.txt","COMPLETE");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/wattson-chat-upgrade.txt",e.ToString());}
        };}
        [MenuItem("Adaptive Learning/Validate Wattson Conversation")]
        public static void Validate(){EditorSceneManager.OpenScene(Scene08Builder.ScenePath);SessionState.SetBool("WattsonChatValidation",true);Scene07Validation.SetResolution(1920,1080);EditorApplication.isPlaying=true;}
        static T F<T>(string name)where T:Component=>LessonReviewTools.Find<T>(name);
        static void Move(string name,float x,float y,float r,float t)=>LessonReviewTools.Set(F<RectTransform>(name),x,y,r,t);
        [MenuItem("Adaptive Learning/Upgrade Wattson Conversation UI")]
        public static void Apply()
        {
            const string configPath="Assets/AdaptiveLearning/ScriptableObjects/WattsonService.asset";
            var settings=AssetDatabase.LoadAssetAtPath<WattsonServiceSettings>(configPath);
            if(settings==null){settings=ScriptableObject.CreateInstance<WattsonServiceSettings>();AssetDatabase.CreateAsset(settings,configPath);}
            foreach(var path in new[]{Scene06Builder.ScenePath,Scene07Builder.ScenePath,Scene08Builder.ScenePath}){
                EditorSceneManager.OpenScene(path);
                var chat=UnityEngine.Object.FindFirstObjectByType<WattsonChatManager>();chat.SetService(settings);
                var ui=UnityEngine.Object.FindFirstObjectByType<WattsonChatUI>();
                Move("ChatCard",.035f,.065f,.63f,.94f);F<Image>("WattsonChatPanel").color=new Color(0,0,0,.35f);
                F<Image>("ChatCard").color=new Color(.018f,.047f,.065f,1);
                F<Text>("ChatTitle").text="TALK TO WATTSON";F<Text>("ChatTitle").fontSize=27;
                Move("ChatTitle",.04f,.90f,.73f,.975f);Move("CloseChat",.80f,.90f,.965f,.975f);Move("Context",.04f,.835f,.96f,.885f);
                F<Text>("Context").text="Lesson companion • Offline lesson guide";F<Text>("Context").fontSize=18;
                Move("QuickQuestions",.04f,.205f,.96f,.295f);Move("MessageInput",.04f,.055f,.755f,.18f);Move("Send",.78f,.055f,.96f,.18f);
                F<Button>("Send").GetComponentInChildren<Text>().text="SEND";F<InputField>("MessageInput").characterLimit=1200;
                F<Text>("Placeholder").text="Ask your own question…";
                foreach(var label in F<Transform>("QuickQuestions").GetComponentsInChildren<Text>()){label.fontSize=18;label.resizeTextMaxSize=18;label.resizeTextMinSize=14;}
                var card=F<Transform>("ChatCard");var scroll=card.Find("ConversationScroll")?.GetComponent<ScrollRect>();
                if(scroll==null){
                    var area=LessonReviewTools.Area("ConversationScroll",card,.04f,.325f,.96f,.815f);
                    area.gameObject.AddComponent<Image>().color=new Color(.025f,.07f,.09f);area.gameObject.AddComponent<RectMask2D>();
                    scroll=area.gameObject.AddComponent<ScrollRect>();scroll.viewport=area;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
                    var content=LessonReviewTools.Area("Messages",area,0,1,1,1);content.pivot=new Vector2(.5f,1);
                    var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(18,18,18,18);layout.spacing=24;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
                    content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=content;
                    F<Text>("Reply").transform.SetParent(content,false);
                }
                var reply=F<Text>("Reply");reply.fontSize=22;reply.resizeTextForBestFit=false;reply.supportRichText=false;reply.alignment=TextAnchor.UpperLeft;reply.text="WATTSON\nWhat would you like to explore?";
                ui.ConfigureConversation(scroll,UnityEngine.Object.FindFirstObjectByType<WattsonController>());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Scene08Builder.ScenePath);
        }
    }
}
