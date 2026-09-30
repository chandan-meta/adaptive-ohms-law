#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using UnityEngine;
namespace AdaptiveOhmsLaw
{
    // Uses the installed offline English recognizer. The helper writes only one temporary result.
    public sealed class WindowsVoiceCapture : IDisposable
    {
        [Serializable] public class Result {public string text="",error="";public int code=0;}
        Process process;
        string resultPath,statusPath;
        public bool Listening=>!string.IsNullOrEmpty(statusPath)&&File.Exists(statusPath)&&process!=null&&!process.HasExited;
        public bool Finished=>process!=null&&process.HasExited;
        static string Data(string text)=>"([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('"+Convert.ToBase64String(Encoding.UTF8.GetBytes(text))+"')))";
        public void Start(string wavePath=null){
            Dispose();var id=Guid.NewGuid().ToString("N");resultPath=Path.Combine(Application.temporaryCachePath,"voice-"+id+".json");statusPath=resultPath+".status";
            var script=Path.Combine(Application.streamingAssetsPath,"WattsonSpeech/Recognize.ps1");
            string command="& ([ScriptBlock]::Create("+Data(File.ReadAllText(script))+")) -ResultPath "+Data(resultPath)+" -StatusPath "+Data(statusPath);
            if(!string.IsNullOrEmpty(wavePath))command+=" -WavePath "+Data(wavePath);
            process=Process.Start(new ProcessStartInfo{FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell/v1.0/powershell.exe"),Arguments="-NoProfile -NonInteractive -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes(command)),UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
        }
        public Result Read(){if(!File.Exists(resultPath))return new Result{error="HELPER_FAILED"};return JsonUtility.FromJson<Result>(File.ReadAllText(resultPath));}
        public void Dispose(){if(process!=null){try{if(!process.HasExited){process.Kill();process.WaitForExit(300);}}catch(InvalidOperationException){}process.Dispose();process=null;}Delete(resultPath);Delete(statusPath);resultPath=statusPath=null;}
        static void Delete(string path){if(!string.IsNullOrEmpty(path))try{File.Delete(path);}catch(IOException){} }
    }
}
#endif

