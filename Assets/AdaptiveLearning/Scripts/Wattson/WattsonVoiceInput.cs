using System;
using System.IO;
using UnityEngine;
namespace AdaptiveOhmsLaw
{
    public sealed class WattsonVoiceInput : MonoBehaviour
    {
        public event Action<string> Recognized, StatusChanged;
        public bool Listening { get; private set; }
        public bool Processing { get; private set; }
        public float Level { get; private set; }
        public string DeviceName { get; private set; } = "Microphone idle";
        public string DiagnosticState => Listening ? "Capturing" : Processing ? "Transcribing" : "Stopped";
        float deadline;
        int deviceIndex=-1;
        public void NextDevice()
        {
            if(Listening||Processing)return;
#if UNITY_WEBGL && !UNITY_EDITOR
            StatusChanged?.Invoke("Choose your microphone in the browser's site settings.");
#else
            var devices=Microphone.devices;if(devices.Length>0){deviceIndex=(deviceIndex+1)%devices.Length;DeviceName=devices[deviceIndex];}
#endif
        }
        bool held;
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void WattsonWebListen(string target);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void WattsonWebRelease();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void WattsonWebCancel();
        string browserResult;
        void Awake(){gameObject.name="WattsonVoice-"+GetInstanceID();}
#endif
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        WindowsVoiceCapture recognizer;
        AudioClip recording;
        string wavePath;
        readonly float[] meter = new float[256];
        bool receiving;
#elif UNITY_ANDROID
        AndroidJavaObject bridge;
        bool awaitingPermission;
        string queuedResult;
        void Awake(){gameObject.name="WattsonVoice-"+GetInstanceID();using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))bridge=new AndroidJavaObject("com.adaptiveohmslaw.WattsonVoice",player.GetStatic<AndroidJavaObject>("currentActivity"),gameObject.name);}
        public void SpeakAndroid(string text)=>bridge?.Call("speak",text);
        public void StopAndroidSpeech()=>bridge?.Call("stopSpeech");
#endif
        public void Listen()
        {
            if(Listening || Processing)return;
            held=true; GetComponent<WattsonSpeech>()?.Stop();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try {
                var devices=Microphone.devices;
                if(devices.Length==0){Fail(VoiceDiagnostics.NoMicrophone);return;}
                deviceIndex=Mathf.Clamp(deviceIndex,0,devices.Length-1);DeviceName=devices[deviceIndex];
                recording=Microphone.Start(DeviceName,false,30,16000);
                if(recording==null){Fail("Microphone could not open. Check Windows Sound > Input and microphone access.");return;}
                Listening=true; receiving=false;
                StatusChanged?.Invoke("Opening microphone… keep holding.");
            }catch(Exception error){Fail(VoiceDiagnostics.WindowsError(error.HResult));return;}
#elif UNITY_ANDROID
            if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone)){
                awaitingPermission=true;
                var callbacks=new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted+=permission=>{if(this!=null&&awaitingPermission){awaitingPermission=false;held=false;StatusChanged?.Invoke("Permission granted. Hold the microphone to speak.");}};
                callbacks.PermissionDenied+=permission=>{if(this!=null){awaitingPermission=false;Fail("Allow Microphone in Android app permissions, then hold the mic again.");}};
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone,callbacks);return;
            }
            DeviceName="Android speech-service input (system selected)";
            queuedResult=null; Listening=true;bridge.Call("listen");StatusChanged?.Invoke("Opening microphone… keep holding.");
#elif UNITY_WEBGL
            browserResult=null;DeviceName="Browser default microphone";Listening=true;
            StatusChanged?.Invoke("Waiting for browser microphone permission…");
            WattsonWebListen(gameObject.name);
#else
            Fail("Voice input is unavailable on this platform. Type your question.");return;
#endif
            deadline=Time.realtimeSinceStartup+30;
        }
        public void Release()
        {
            if(!held)return;held=false;
            if(!Listening)return;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try {
                int frames=Microphone.GetPosition(DeviceName);
                Microphone.End(DeviceName);Listening=false;Level=0;
                if(recording==null || frames<recording.frequency/4){Fail("Recording too short. Hold the mic while you ask your question.");return;}
                var samples=new float[frames*recording.channels];recording.GetData(samples,0);
                float peak=0;foreach(float sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));
                if(peak<.0001f){Fail("This microphone recorded silence. Tap the microphone panel to choose another input and try again.");return;}
                wavePath=Path.Combine(Application.temporaryCachePath,"wattson-recording-"+Guid.NewGuid().ToString("N")+".wav");
                WriteWave(wavePath,samples,recording.frequency,recording.channels);
                Destroy(recording);recording=null;
                recognizer=new WindowsVoiceCapture();recognizer.Start(wavePath);
                Processing=true;deadline=Time.realtimeSinceStartup+30;
                StatusChanged?.Invoke("Transcribing your recording…");
            }catch(Exception error){Fail("Recording could not be processed: "+error.GetType().Name+". Try again or type your question.");}
#elif UNITY_ANDROID
            Listening=false;Processing=true;Level=0;deadline=Time.realtimeSinceStartup+20;
            StatusChanged?.Invoke("Transcribing your question…");
            if(!string.IsNullOrEmpty(queuedResult)){var text=queuedResult;Stop();Recognized?.Invoke(text);}else bridge?.Call("finishListening");
#elif UNITY_WEBGL
            Listening=false;Processing=true;deadline=Time.realtimeSinceStartup+20;
            StatusChanged?.Invoke("Transcribing your question…");
            if(!string.IsNullOrEmpty(browserResult)){var text=browserResult;Stop();Recognized?.Invoke(text);}else WattsonWebRelease();
#endif
        }
        public void Stop()
        {
            held=false;Listening=false;Processing=false;Level=0;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if(recording!=null){Microphone.End(DeviceName);Destroy(recording);recording=null;}
            recognizer?.Dispose();recognizer=null;
            if(!string.IsNullOrEmpty(wavePath)){try{File.Delete(wavePath);}catch(IOException){}wavePath=null;}
#elif UNITY_ANDROID
            awaitingPermission=false;queuedResult=null;bridge?.Call("stopListening");
#elif UNITY_WEBGL
            browserResult=null;WattsonWebCancel();
#endif
            StatusChanged?.Invoke("Hold the microphone to talk • release to send");
        }
        void Fail(string message){Stop();StatusChanged?.Invoke(message);}
        public void VoiceResult(string text){
            if(!Listening&&!Processing)return;
#if UNITY_WEBGL && !UNITY_EDITOR
            if(held){browserResult=text;StatusChanged?.Invoke("Question heard • release to send");return;}
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            if(held){queuedResult=text;StatusChanged?.Invoke("Question heard • release to send");return;}
#endif
            Stop();if(!string.IsNullOrWhiteSpace(text))Recognized?.Invoke(text);
        }
        public void VoiceError(string text)=>Fail(text);
        public void VoiceReady(string unused){if(Listening)StatusChanged?.Invoke("Microphone ready • speak, then release to send");}
        public void VoiceLevel(string value){if(Listening&&float.TryParse(value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var rms))Level=Mathf.Clamp01((rms+2)/12);}
        void Update()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if(Listening&&recording!=null){
                int position=Microphone.GetPosition(DeviceName);
                if(position>=meter.Length){
                    recording.GetData(meter,position-meter.Length);float peak=0;foreach(float sample in meter)peak=Mathf.Max(peak,Mathf.Abs(sample));Level=Mathf.Clamp01(peak*5);
                    if(!receiving){receiving=true;StatusChanged?.Invoke("Recording • speak, then release to send");}
                }
                if(!receiving&&Time.realtimeSinceStartup>deadline-26){Fail("No audio samples from this microphone. Check Windows Sound > Input, select a working device, then restart the app.");}
            }
            if(Processing&&recognizer!=null&&recognizer.Finished){
                var result=recognizer.Read();Stop();
                if(!string.IsNullOrWhiteSpace(result.text))Recognized?.Invoke(result.text);
                else Fail(result.error=="NO_SPEECH"?"No clear speech detected. Check the level meter moves as you speak, then hold the mic and try again.":result.error=="NO_ENGLISH_RECOGNIZER"?"Install an English Windows speech language, then restart the app.":"Speech recognition failed ("+result.error+"). Try again or type your question.");
            }
#endif
            if((Listening||Processing)&&Time.realtimeSinceStartup>deadline)Fail("Voice session timed out. Hold the microphone to try again.");
        }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        static void WriteWave(string path,float[] samples,int rate,int channels){
            using(var writer=new BinaryWriter(File.Create(path))){
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);foreach(float sample in samples)writer.Write((short)(Mathf.Clamp(sample,-1,1)*32767));
            }
        }
#endif
        void OnApplicationFocus(bool focus){if(!focus)Stop();}
        void OnDisable()=>Stop();
        void OnDestroy(){Stop();
#if UNITY_ANDROID && !UNITY_EDITOR
            bridge?.Call("dispose");bridge?.Dispose();bridge=null;
#endif
        }
    }
}




