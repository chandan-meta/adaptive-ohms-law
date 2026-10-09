using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace OhmsLaw
{
    // Records the Game view and the audio mix, in sync, for a review video. Time.captureFramerate pins the game clock to
    // one frame step per rendered frame (so a slow capture never drops anything), AudioRenderer hands over exactly that
    // frame's worth of the final mix, each frame streams raw into an ffmpeg process (video only, fast preset) and the
    // audio goes to a WAV beside it; the caller muxes the two afterwards. Runs in the editor and in players.
    public class GameplayRecorder : MonoBehaviour
    {
        public static GameplayRecorder Active { get; private set; }
        public int Frames { get; private set; }
        public string Error { get; private set; } = "";

        string dir;
        int fps, channels, sampleRate;
        Process ffmpeg;
        Stream pipe;
        FileStream wav;
        long audioBytes;
        byte[] frame;
        bool stopping;
        readonly StringBuilder log = new StringBuilder();

        public static GameplayRecorder Begin(string directory, int framesPerSecond = 30)
        {
            if (Active) return Active;
            Directory.CreateDirectory(directory);
            var go = new GameObject("Gameplay Recorder");
            DontDestroyOnLoad(go);
            var recorder = go.AddComponent<GameplayRecorder>();
            recorder.dir = directory;
            recorder.fps = framesPerSecond;
            recorder.Start();
            return recorder;
        }

        void Start()
        {
            if (Active == this) return;
            Active = this;
            Time.captureFramerate = fps;
            channels = AudioSettings.speakerMode switch { AudioSpeakerMode.Mono => 1, AudioSpeakerMode.Quad => 4, AudioSpeakerMode.Surround => 5, AudioSpeakerMode.Mode5point1 => 6, AudioSpeakerMode.Mode7point1 => 8, _ => 2 };
            sampleRate = AudioSettings.outputSampleRate;
            try
            {
                wav = new FileStream(Path.Combine(dir, "audio.wav"), FileMode.Create, FileAccess.Write);
                wav.Write(new byte[44], 0, 44); // header, filled in at the end
                AudioRenderer.Start();
            }
            catch (Exception e) { Error += "audio: " + e.Message + "\n"; wav?.Dispose(); wav = null; }
            StartCoroutine(Capture());
        }

        IEnumerator Capture()
        {
            var end = new WaitForEndOfFrame();
            while (!stopping)
            {
                yield return end;
                if (stopping) break;
                RenderAudio();
                CaptureFrame();
            }
        }

        // The samples the mix produced for this capture frame, as 16-bit PCM.
        void RenderAudio()
        {
            if (wav == null) return;
            int samples = AudioRenderer.GetSampleCountForCaptureFrame();
            if (samples <= 0) return;
            var buffer = new NativeArray<float>(samples * channels, Allocator.Temp);
            AudioRenderer.Render(buffer);
            var bytes = new byte[buffer.Length * 2];
            for (int i = 0; i < buffer.Length; i++)
            {
                short s = (short)Mathf.Clamp(Mathf.RoundToInt(buffer[i] * 32767f), short.MinValue, short.MaxValue);
                bytes[2 * i] = (byte)s;
                bytes[2 * i + 1] = (byte)(s >> 8);
            }
            buffer.Dispose();
            wav.Write(bytes, 0, bytes.Length);
            audioBytes += bytes.Length;
        }

        void CaptureFrame()
        {
            if (Error.Contains("video")) return;
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                if (ffmpeg == null) StartFfmpeg(tex.width, tex.height, tex.format);
                var data = tex.GetRawTextureData<byte>();
                if (frame == null || frame.Length != data.Length) frame = new byte[data.Length];
                data.CopyTo(frame);
                pipe.Write(frame, 0, frame.Length);
                Frames++;
            }
            catch (Exception e) { Error += "video: " + e.Message + "\n"; stopping = true; }
            finally { Destroy(tex); }
        }

        void StartFfmpeg(int width, int height, TextureFormat format)
        {
            string pixel = format switch { TextureFormat.RGB24 => "rgb24", TextureFormat.RGBA32 => "rgba", TextureFormat.ARGB32 => "argb", TextureFormat.BGRA32 => "bgra", _ => throw new Exception("capture format " + format) };
            // Unity's rows run bottom-up: vflip. Fast, near-lossless intermediate; the final encode happens after the run.
            string args = $"-y -loglevel error -nostats -f rawvideo -pix_fmt {pixel} -s {width}x{height} -framerate {fps} -i - -vf vflip -c:v libx264 -preset ultrafast -crf 16 -pix_fmt yuv420p -an \"{Path.Combine(dir, "video.mp4")}\"";
            ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo("ffmpeg", args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardError = true },
            };
            ffmpeg.ErrorDataReceived += (_, e) => { if (e.Data != null) log.AppendLine(e.Data); };
            ffmpeg.Start();
            ffmpeg.BeginErrorReadLine();
            pipe = ffmpeg.StandardInput.BaseStream;
            File.WriteAllText(Path.Combine(dir, "info.txt"), $"{width}x{height} {pixel} {fps} fps, audio {sampleRate} Hz x {channels}\n");
        }

        // Closes the streams: ffmpeg finishes video.mp4, the WAV gets its header. Call before leaving play mode.
        public void End()
        {
            if (stopping && ffmpeg == null) return;
            stopping = true;
            Time.captureFramerate = 0;
            try { if (wav != null) AudioRenderer.Stop(); } catch (Exception e) { Error += "audio stop: " + e.Message + "\n"; }
            if (wav != null)
            {
                int byteRate = sampleRate * channels * 2;
                wav.Seek(0, SeekOrigin.Begin);
                var header = new BinaryWriter(wav);
                header.Write(Encoding.ASCII.GetBytes("RIFF")); header.Write((int)(36 + audioBytes)); header.Write(Encoding.ASCII.GetBytes("WAVE"));
                header.Write(Encoding.ASCII.GetBytes("fmt ")); header.Write(16); header.Write((short)1); header.Write((short)channels);
                header.Write(sampleRate); header.Write(byteRate); header.Write((short)(channels * 2)); header.Write((short)16);
                header.Write(Encoding.ASCII.GetBytes("data")); header.Write((int)audioBytes);
                header.Flush();
                wav.Dispose();
                wav = null;
            }
            try { pipe?.Flush(); pipe?.Close(); ffmpeg?.WaitForExit(120000); } catch (Exception e) { Error += "ffmpeg close: " + e.Message + "\n"; }
            File.AppendAllText(Path.Combine(dir, "info.txt"), $"{Frames} frames = {Frames / (float)fps:0.0} s, audio {audioBytes / (float)(sampleRate * channels * 2):0.0} s\n{Error}{log}");
            ffmpeg = null;
            if (Active == this) Active = null;
        }

        void OnDestroy() { if (ffmpeg != null || wav != null) End(); }
    }
}
