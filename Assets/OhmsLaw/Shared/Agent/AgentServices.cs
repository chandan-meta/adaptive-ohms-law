using System;
using System.Globalization;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace OhmsLaw
{
    [Serializable] public class ChatTurn { public string role, text; }

    [Serializable]
    public class ChatRequest
    {
        public string sceneId, sceneTitle, currentLine, question;
        public string[] covered, doNotSpoil;
        public ChatTurn[] history;
        // Authoritative live context (WOW_PLAN.md section 6), plain text: the scene, activity, bench readings, recorded rows,
        // whether resistance has been taught; and LearnerSession.Summary(). The model must ground answers in these.
        public string state, learner;
    }

    public class AgentException : Exception { public AgentException(string message) : base(message) { } }

    public interface IChatClient { Awaitable StreamAsync(ChatRequest request, Action<string> onText, CancellationToken cancel); }
    public interface ITextToSpeech { Awaitable<AudioClip> SpeakAsync(string text, CancellationToken cancel); }
    public interface ISpeechToText { Awaitable<string> TranscribeAsync(byte[] wav, CancellationToken cancel); }

    static class Proxy
    {
        // Seconds the proxy asked us to wait at its last 429 (its Retry-After header; 60 when it sent none).
        public static float RetryAfterSeconds { get; private set; } = 60f;

        public static UnityWebRequest Post(ProxyConfig config, string path, byte[] body, string contentType)
        {
            // A build without a hosted proxy is simply offline (no request, no wait).
            if (!config || string.IsNullOrEmpty(config.BaseUrl)) throw new AgentException("not_configured");
            var request = new UnityWebRequest(config.BaseUrl + path, "POST") { uploadHandler = new UploadHandlerRaw(body), timeout = config.requestTimeout };
            request.SetRequestHeader("Content-Type", contentType);
            request.SetRequestHeader("X-Session-Id", ProxyConfig.SessionId);
            return request;
        }

        public static async Awaitable Send(UnityWebRequest request, CancellationToken cancel, Func<bool> stalled = null)
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                if (cancel.IsCancellationRequested || (stalled != null && stalled())) { request.Abort(); break; }
                await Awaitable.NextFrameAsync();
            }
            cancel.ThrowIfCancellationRequested();
            if (request.result == UnityWebRequest.Result.Success) return;
            if (request.responseCode != 429) throw new AgentException("proxy_unreachable: " + request.error);
            RetryAfterSeconds = float.TryParse(request.GetResponseHeader("Retry-After"), NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds) && seconds > 0 ? seconds : 60f;
            throw new AgentException("rate_limited");
        }
    }

    // POST /chat, answered as server-sent events: data: {"t":"words"} ... data: {"done":true}
    public class ProxyChatClient : IChatClient
    {
        readonly ProxyConfig config;
        public ProxyChatClient(ProxyConfig config) { this.config = config; }

        public async Awaitable StreamAsync(ChatRequest body, Action<string> onText, CancellationToken cancel)
        {
            using var request = Proxy.Post(config, "/chat", Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)), "application/json");
            var stream = new SseHandler(onText);
            request.downloadHandler = stream;
            float start = Time.realtimeSinceStartup;
            await Proxy.Send(request, cancel, () => !stream.Started && Time.realtimeSinceStartup - start > config.firstReplyTimeout);
            if (stream.Error != null) throw new AgentException(stream.Error);
            if (!stream.Started) throw new AgentException("empty_reply");
        }

        class SseHandler : DownloadHandlerScript
        {
            [Serializable] class Chunk { public string t, error; }

            readonly Action<string> onText;
            readonly Decoder decoder = Encoding.UTF8.GetDecoder(); // keeps split multi-byte characters intact
            string pending = "";
            public bool Started;
            public string Error;

            public SseHandler(Action<string> onText) : base(new byte[4096]) { this.onText = onText; }

            protected override bool ReceiveData(byte[] data, int length)
            {
                var chars = new char[decoder.GetCharCount(data, 0, length)];
                decoder.GetChars(data, 0, length, chars, 0);
                pending += new string(chars);
                int end;
                while ((end = pending.IndexOf('\n')) >= 0)
                {
                    string line = pending.Substring(0, end).TrimEnd('\r');
                    pending = pending.Substring(end + 1);
                    if (!line.StartsWith("data: ")) continue;
                    var chunk = JsonUtility.FromJson<Chunk>(line.Substring(6));
                    if (!string.IsNullOrEmpty(chunk.error)) Error = chunk.error;
                    else if (!string.IsNullOrEmpty(chunk.t)) { Started = true; onText(chunk.t); }
                }
                return true;
            }
        }
    }

    // POST /tts {text} -> mp3
    public class ProxyTextToSpeech : ITextToSpeech
    {
        [Serializable] class Body { public string text; }

        readonly ProxyConfig config;
        public ProxyTextToSpeech(ProxyConfig config) { this.config = config; }

        public static byte[] Json(string text) =>
            Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Body { text = PronunciationLexicon.Apply(text) }));

        public async Awaitable<AudioClip> SpeakAsync(string text, CancellationToken cancel)
        {
            using var request = Proxy.Post(config, "/tts", Json(text), "application/json");
            request.downloadHandler = new DownloadHandlerAudioClip(request.url, AudioType.MPEG);
            await Proxy.Send(request, cancel);
            return DownloadHandlerAudioClip.GetContent(request);
        }
    }

    // POST /stt (16 kHz mono WAV) -> {"text":"..."}
    public class ProxySpeechToText : ISpeechToText
    {
        [Serializable] class Reply { public string text; }

        readonly ProxyConfig config;
        public ProxySpeechToText(ProxyConfig config) { this.config = config; }

        public async Awaitable<string> TranscribeAsync(byte[] wav, CancellationToken cancel)
        {
            using var request = Proxy.Post(config, "/stt", wav, "audio/wav");
            request.downloadHandler = new DownloadHandlerBuffer();
            await Proxy.Send(request, cancel);
            return JsonUtility.FromJson<Reply>(request.downloadHandler.text)?.text ?? "";
        }
    }

    // Push-to-talk capture: Unity Microphone -> mono 16 kHz WAV bytes.
    public static class Mic
    {
        const int Hz = 16000, MaxSeconds = 15;
        static AudioClip clip;

#if UNITY_WEBGL && !UNITY_EDITOR
        public static string Start() => "Voice questions are not available here. You can still type.";
        public static byte[] Stop() => null;
#else
        // Returns null when recording started, otherwise a friendly message to show the student.
        public static string Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
                return "Allow the microphone, then hold to talk again. You can still type.";
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
                return "Allow the microphone, then hold to talk again. You can still type.";
            }
#endif
            if (Microphone.devices.Length == 0) return "I can't find a microphone. You can still type your question.";
            clip = Microphone.Start(null, false, MaxSeconds, Hz);
            return clip ? null : "The microphone didn't start. You can still type your question.";
        }

        // Returns null when nothing useful was recorded.
        public static byte[] Stop()
        {
            if (!clip) return null;
            // A hold longer than MaxSeconds ends the (non-looping) recording by itself and GetPosition is 0 again: keep
            // what was recorded (the whole clip) instead of dropping the question.
            int recorded = Microphone.IsRecording(null) ? Microphone.GetPosition(null) : clip.samples;
            Microphone.End(null);
            byte[] wav = null;
            if (recorded > Hz / 4)
            {
                var samples = new float[recorded];
                clip.GetData(samples, 0);
                wav = Wav.Encode(samples, Hz);
            }
            UnityEngine.Object.Destroy(clip);
            clip = null;
            return wav;
        }
#endif
    }

    public static class Wav
    {
        // Mono 16-bit PCM WAV.
        public static byte[] Encode(float[] samples, int hz)
        {
            int dataBytes = samples.Length * 2;
            var bytes = new byte[44 + dataBytes];
            void Text(int at, string s) => Encoding.ASCII.GetBytes(s, 0, 4, bytes, at);
            void Int(int at, int v) => BitConverter.GetBytes(v).CopyTo(bytes, at);
            void Short(int at, short v) => BitConverter.GetBytes(v).CopyTo(bytes, at);
            Text(0, "RIFF"); Int(4, 36 + dataBytes); Text(8, "WAVE");
            Text(12, "fmt "); Int(16, 16); Short(20, 1); Short(22, 1); Int(24, hz); Int(28, hz * 2); Short(32, 2); Short(34, 16);
            Text(36, "data"); Int(40, dataBytes);
            for (int i = 0; i < samples.Length; i++)
            {
                short v = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                bytes[44 + i * 2] = (byte)v;
                bytes[45 + i * 2] = (byte)(v >> 8);
            }
            return bytes;
        }
    }
}
