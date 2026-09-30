using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace AdaptiveOhmsLaw.Editor
{
    [InitializeOnLoad] public static class WattsonVoiceSetup
    {
        [Serializable] public class Entry { public string path,text; }
        [Serializable] public class Export { public List<Entry> entries=new List<Entry>(); }
        const string Folder="Assets/AdaptiveLearning/Audio/GeneratedVoice";
        static WattsonVoiceSetup(){EditorApplication.update+=()=>{
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Logs/voice-export.req")){File.Delete("Logs/voice-export.req");ExportLines();}
            if(File.Exists("Logs/voice-apply.req")){File.Delete("Logs/voice-apply.req");try{Apply();File.WriteAllText("Logs/voice-apply.txt","COMPLETE");}catch(Exception e){File.WriteAllText("Logs/voice-apply.txt",e.ToString());Debug.LogException(e);}}
        };}
        public static void ExportLines(){Directory.CreateDirectory(Folder);var export=new Export();foreach(var guid in AssetDatabase.FindAssets("t:DialogueSequence",new[]{"Assets/AdaptiveLearning"})){
            var data=AssetDatabase.LoadAssetAtPath<DialogueSequence>(AssetDatabase.GUIDToAssetPath(guid));
            foreach(var line in data.lines)export.entries.Add(new Entry{path=Folder+"/"+data.name+"_"+line.id+".wav",text=line.text});
        }File.WriteAllText("Logs/voice-lines.json",JsonUtility.ToJson(export,true));}
        [MenuItem("Adaptive Learning/Apply Wattson Voice")]
        public static void Apply(){AssetDatabase.Refresh();int count=0;
            foreach(var guid in AssetDatabase.FindAssets("t:DialogueSequence",new[]{"Assets/AdaptiveLearning"})){
                var data=AssetDatabase.LoadAssetAtPath<DialogueSequence>(AssetDatabase.GUIDToAssetPath(guid));
                foreach(var line in data.lines){string path=Folder+"/"+data.name+"_"+line.id+".wav";var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null)throw new InvalidOperationException("Missing voice: "+path);
                    var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.75f;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
                    line.audio=AssetDatabase.LoadAssetAtPath<AudioClip>(path);line.duration=Mathf.Max(line.duration,line.audio.length+.15f);count++;
                }EditorUtility.SetDirty(data);
            }
            foreach(var path in new[]{Scene06Builder.ScenePath,Scene07Builder.ScenePath,Scene08Builder.ScenePath}){
                EditorSceneManager.OpenScene(path);var ui=UnityEngine.Object.FindFirstObjectByType<WattsonChatUI>();var card=LessonReviewTools.Find<RectTransform>("ChatCard");
                var button=card.Find("VoiceInput")?.GetComponent<Button>();
                if(button==null){button=UnityEngine.Object.Instantiate(LessonReviewTools.Find<Button>("Send"),card);button.name="VoiceInput";}
                LessonReviewTools.Set((RectTransform)button.transform,.04f,.055f,.225f,.18f);button.GetComponentInChildren<Text>().text="MIC / HOLD";
                LessonReviewTools.Set(LessonReviewTools.Find<RectTransform>("QuickQuestions"),.04f,.27f,.96f,.36f);
                LessonReviewTools.Set(LessonReviewTools.Find<RectTransform>("ConversationScroll"),.04f,.39f,.96f,.815f);
                LessonReviewTools.Set(LessonReviewTools.Find<RectTransform>("MessageInput"),.245f,.055f,.755f,.18f);
                var status=card.Find("VoiceStatus")?.GetComponent<Text>();if(status==null)status=LessonReviewTools.Label("VoiceStatus",card,"Hold the microphone to talk • release to send",16,.04f,.008f,.96f,.05f);
                ui.ConfigureVoice(button,status);EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }AssetDatabase.SaveAssets();File.WriteAllText("Logs/voice-coverage.txt",count+" dialogue clips assigned across Scenes 6–8.");EditorSceneManager.OpenScene(Scene06Builder.ScenePath);
        }
    }
}

