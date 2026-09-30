using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace AdaptiveOhmsLaw
{
    // Recorded clips are portable; device synthesis supplies speech for dynamic chat replies.
    public sealed class WattsonSpeech : MonoBehaviour
    {
        AudioSource source;
        Coroutine pending;
        AudioClip generated;
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void WattsonWebSpeak(string text);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void WattsonWebStopSpeech();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int WattsonWebSpeaking();
#endif
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        System.Diagnostics.Process process;
        string temporaryFile;
#endif
        public bool IsSpeaking => pending != null || (source != null && source.isPlaying)
#if UNITY_WEBGL && !UNITY_EDITOR
            || WattsonWebSpeaking() != 0
#endif
            ;
        public AudioSource Output => source;
        public event Action<string> Failed;
        void Awake() { source = gameObject.AddComponent<AudioSource>(); source.playOnAwake=false; source.spatialBlend=0; }
        public void Speak(string text)
        {
            Stop();
            if(string.IsNullOrWhiteSpace(text))return;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            pending=StartCoroutine(Synthesize(text));
#elif UNITY_ANDROID
            GetComponent<WattsonVoiceInput>()?.SpeakAndroid(text);
#elif UNITY_WEBGL
            WattsonWebSpeak(text);
#else
            Failed?.Invoke("Device speech is unavailable. Subtitles remain available.");
#endif
        }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        IEnumerator Synthesize(string text)
        {
            temporaryFile=Path.Combine(Application.temporaryCachePath,"wattson-"+Guid.NewGuid().ToString("N")+".wav");
            // Base64 data is never interpreted as PowerShell source.
            string data=Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            string path=Convert.ToBase64String(Encoding.UTF8.GetBytes(temporaryFile));
            string script="$ErrorActionPreference='Stop'; Add-Type -AssemblyName System.Speech; $s=New-Object System.Speech.Synthesis.SpeechSynthesizer; try {$s.Rate=0; $s.SetOutputToWaveFile([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('"+path+"'))); $s.Speak([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('"+data+"')))} finally {$s.Dispose()}";
            bool started=false;
            try { process=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell/v1.0/powershell.exe"),
                Arguments="-NoLogo -NoProfile -NonInteractive -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
                UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden }); started=process!=null;
            } catch(Exception) { Failed?.Invoke("Device voice could not start. Subtitles remain available."); }
            if(!started){pending=null;yield break;}
            float deadline=Time.realtimeSinceStartup+20;
            while(!process.HasExited && Time.realtimeSinceStartup<deadline)yield return null;
            if(!process.HasExited || process.ExitCode!=0 || !File.Exists(temporaryFile)) { Failed?.Invoke("Device voice is unavailable. Subtitles remain available."); Stop(); yield break; }
            using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(temporaryFile).AbsoluteUri,AudioType.WAV)) {
                yield return request.SendWebRequest();
                if(request.result==UnityWebRequest.Result.Success){generated=DownloadHandlerAudioClip.GetContent(request);source.clip=generated;source.Play();}
                else Failed?.Invoke("Voice audio could not be loaded.");
            }
            CleanupProcess();pending=null;
        }
        void CleanupProcess(){if(process!=null){try{if(!process.HasExited)process.Kill();}catch(InvalidOperationException){}process.Dispose();process=null;}if(!string.IsNullOrEmpty(temporaryFile)){try{File.Delete(temporaryFile);}catch(IOException){}temporaryFile=null;}}
#endif
        public void Stop()
        {
            if(pending!=null){StopCoroutine(pending);pending=null;} if(source!=null)source.Stop();
            if(generated!=null){Destroy(generated);generated=null;}
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            CleanupProcess();
#elif UNITY_ANDROID
            GetComponent<WattsonVoiceInput>()?.StopAndroidSpeech();
#elif UNITY_WEBGL
            WattsonWebStopSpeech();
#endif
        }
        void OnDisable()=>Stop();
    }
}

