using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace OhmsLaw.Editor
{
    // The one voice bake for every scene: Scene 4, the blockouts 5-13 and the test scene, proposed lines included.
    // - With the Wattson proxy running and an ElevenLabs key set: each line goes through /tts and lands as "<id>.mp3" in its
    //   scene's Audio/VO (lines under Shared, such as Agent_Offline, go to Scene 4's). That is the real voice; BlockoutAssets.FindVo prefers it.
    // - Without it: the lines go through edge-tts (Microsoft's free neural voices, EdgeVoice at the end of this file; python and
    //   internet, no key), one batched run per scene, and land as the same "<id>.mp3". Scratch clips those lines had are removed.
    // - Without edge-tts either: a line with no clip gets "scratch_<id>.wav" from the clearest local voice, so timing still works.
    // - A voice actor's "<id>.wav" / "<id>.ogg" is never touched.
    // Text-to-speech gets PronunciationLexicon.ForSpeech(text); subtitles never change. Each clip remembers the words and the
    // voice it was made from (importer userData), so a re-bake redoes only lines whose words or voice settings changed, and
    // it resumes where a rate limit stopped it. Scenes that got new clips are rebuilt so the Timeline follows the clip lengths.
    public static class BakeBlockoutVO
    {
        const string Scene04 = "Scene04";
        const string ScratchVoice = "local:winrt-mark-1.0"; // change to re-make every scratch clip
        enum Result { Ok, Failed, RateLimited }

        [MenuItem("Ohm's Law/Bake VO/All Scenes")] public static void All() => Run(SceneIds());
        [MenuItem("Ohm's Law/Bake VO/Scene 04")] static void S04() => Bake(Scene04);
        [MenuItem("Ohm's Law/Bake VO/Scene 05")] static void S05() => Bake("Scene05");
        [MenuItem("Ohm's Law/Bake VO/Scene 06")] static void S06() => Bake("Scene06");
        [MenuItem("Ohm's Law/Bake VO/Scene 07")] static void S07() => Bake("Scene07");
        [MenuItem("Ohm's Law/Bake VO/Scene 08")] static void S08() => Bake("Scene08");
        [MenuItem("Ohm's Law/Bake VO/Scene 09")] static void S09() => Bake("Scene09");
        [MenuItem("Ohm's Law/Bake VO/Scene 10")] static void S10() => Bake("Scene10");
        [MenuItem("Ohm's Law/Bake VO/Scene 11")] static void S11() => Bake("Scene11");
        [MenuItem("Ohm's Law/Bake VO/Scene 12")] static void S12() => Bake("Scene12");
        [MenuItem("Ohm's Law/Bake VO/Scene 13")] static void S13() => Bake("Scene13");
        [MenuItem("Ohm's Law/Bake VO/Test Scene")] static void Test() => Bake("Scene00Test");

        public static void Bake(string sceneId) => Run(new[] { sceneId });
        public static void Bake(SceneBuilderBase builder) => Run(new[] { builder.SceneId });

        static IEnumerable<string> SceneIds() => new[] { Scene04 }.Concat(BlockoutAssets.Builders().Select(b => b.SceneId)).OrderBy(id => id);

        static void Run(IEnumerable<string> sceneIds)
        {
            AssetDatabase.Refresh(); // picks up clips renamed or dropped in outside Unity
            var config = AssetDatabase.LoadAssetAtPath<ProxyConfig>(BlockoutAssets.Shared + "/Agent/ProxyConfig.asset");
            string voice = config ? ProxyVoice(config) : null; // null: no proxy with an ElevenLabs key, so the edge voice speaks
            bool edge = voice == null;
            if (edge) voice = EdgeVoice.Signature;
            edgeUnavailable = false;
            Debug.Log(edge
                ? $"Bake VO: no proxy with an ElevenLabs key; neural voice through edge-tts ({voice}). For the real one see Server/wattson-proxy/README.md."
                : $"Bake VO: real voice through {config.BaseUrl} ({voice}).");
            var rebuild = new List<string>();
            bool stoppedEarly = false;
            try
            {
                foreach (var sceneId in sceneIds)
                {
                    var (changed, stopped) = BakeScene(sceneId, config, voice);
                    if (changed) rebuild.Add(sceneId);
                    if (stopped) { stoppedEarly = true; break; }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.SaveAssets();
            foreach (var sceneId in rebuild) Rebuild(sceneId);
            if (!stoppedEarly) RemoveScratch(sceneIds); // a stopped bake may hold clips its lines are not pointed at yet; the next full pass cleans up
        }

        static (bool changed, bool stopped) BakeScene(string sceneId, ProxyConfig config, string voice)
        {
            string folder = BlockoutAssets.Root + "/" + sceneId, vo = folder + "/Audio/VO";
            var search = (sceneId == Scene04 ? new[] { folder, BlockoutAssets.Shared } : new[] { folder }).Where(AssetDatabase.IsValidFolder).ToArray();
            var lines = search.Length == 0 ? new DialogueLine[0] : AssetDatabase.FindAssets("t:DialogueLine", search)
                .Select(guid => AssetDatabase.LoadAssetAtPath<DialogueLine>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(line => line && !string.IsNullOrEmpty(line.id) && !string.IsNullOrWhiteSpace(line.Spoken))
                .GroupBy(line => line.id).Select(group => group.First()).OrderBy(line => line.id, StringComparer.Ordinal).ToArray();
            if (lines.Length == 0) { Debug.LogWarning(sceneId + ": no dialogue lines yet. Build the scene first."); return (false, false); }
            Directory.CreateDirectory(vo);
            var byId = lines.ToDictionary(line => line.id);

            // The edge voice speaks the whole scene in one run first; the loop below then finds those clips in place.
            bool edge = EdgeVoice.Is(voice);
            var fresh = new HashSet<string>(StringComparer.Ordinal); // ids the edge batch just gave a new mp3
            if (edge && !edgeUnavailable && !EdgeBatch(sceneId, vo, voice, lines, fresh))
            {
                edgeUnavailable = true;
                Debug.LogWarning("Bake VO: edge-tts made no clip (it needs python with the edge-tts package on PATH, and internet). " +
                                 "Lines without a clip get the local scratch voice for the rest of this bake.");
            }
            if (edge && edgeUnavailable) voice = null; // the scratch path below

            int real = 0, scratch = 0, actor = 0, failed = 0, made = 0;
            bool changed = false, stopped = false;
            for (int i = 0; i < lines.Length && !stopped; i++)
            {
                var line = lines[i];
                if (EditorUtility.DisplayCancelableProgressBar("Bake VO " + sceneId, line.id, i / (float)lines.Length)) { stopped = true; break; }
                string speech = PronunciationLexicon.ForSpeech(line.Spoken);
                string mp3 = vo + "/" + line.id + ".mp3";

                if (File.Exists(vo + "/" + line.id + ".wav") || File.Exists(vo + "/" + line.id + ".ogg")) actor++;
                else if (voice != null)
                {
                    string stamp = Stamp(speech, VoiceFor(voice, speech));
                    if (fresh.Contains(line.id)) { real++; made++; }
                    else if (File.Exists(mp3) && StampOf(mp3) == stamp) real++;
                    else if (edge) failed++; // edge_bake.py reported it; see the warning above
                    else
                    {
                        var body = new TtsBody { text = speech, previous_text = Neighbour(byId, line.id, -1), next_text = Neighbour(byId, line.id, +1) };
                        var result = ProxyTts(config, body, mp3, out string retryAfter);
                        if (result == Result.RateLimited)
                        {
                            int minutes = int.TryParse(retryAfter, out int seconds) ? Mathf.CeilToInt(seconds / 60f) : 10;
                            Debug.LogWarning($"Bake VO stopped at {line.id}: the proxy's /tts limit is reached. Run the bake again in about {minutes} min; " +
                                             "it continues from here. For a first full bake, set TTS_LIMIT=400 in Server/wattson-proxy/.env and restart the proxy.");
                            stopped = true;
                        }
                        else if (result == Result.Failed) failed++;
                        else { Import(mp3, stamp); real++; made++; }
                    }
                }
                else if (File.Exists(mp3))
                {
                    real++;
                    if (!StampOf(mp3).StartsWith(TextHash(speech) + "@"))
                        Debug.LogWarning($"{line.id}: its words changed since the real voice was baked. Re-bake with the proxy running, or the clip and subtitle differ.");
                }
                else
                {
                    string path = vo + "/scratch_" + line.id + ".wav", stamp = Stamp(speech, ScratchVoice);
                    if (File.Exists(path) && StampOf(path) == stamp) scratch++;
                    else if (OsTts(speech, path)) { Import(path, stamp); scratch++; made++; }
                    else failed++;
                }

                // Point the line at its best clip now: Scene 4's builder keeps whatever clip a line already has.
                var clip = BlockoutAssets.FindVo(folder, line.id);
                if (clip && clip != line.vo) { line.vo = clip; EditorUtility.SetDirty(line); changed = true; }
            }
            Debug.Log($"{sceneId} VO: {real} real, {actor} voice actor, {scratch} scratch (local voice), {failed} failed; {made} clip(s) made" +
                      (stopped ? ", stopped early." : ".") + (made > 0 || changed ? " Rebuilding the scene to re-time the Timeline." : ""));
            return (changed || made > 0, stopped);
        }

        static void Rebuild(string sceneId)
        {
            if (sceneId == Scene04) { Scene04Builder.Build(); return; }
            var builder = BlockoutAssets.Builders().FirstOrDefault(b => b.SceneId == sceneId);
            if (builder != null) builder.Build();
        }

        // Ohm's Law/Bake VO/Remove scratch clips: every scratch_<id>.wav with a real <id>.mp3 beside it goes (its .meta too), so no
        // old-voice clip lingers once a line is spoken for real. Run does the same for the scenes it baked.
        [MenuItem("Ohm's Law/Bake VO/Remove scratch clips")]
        public static void RemoveScratch() => RemoveScratch(Directory.GetDirectories(BlockoutAssets.Root).Select(Path.GetFileName));

        static void RemoveScratch(IEnumerable<string> sceneIds)
        {
            int count = 0;
            foreach (var vo in sceneIds.Select(id => BlockoutAssets.Root + "/" + id + "/Audio/VO").Where(Directory.Exists))
                foreach (var wav in Directory.GetFiles(vo, "scratch_*.wav").Select(path => path.Replace('\\', '/')))
                {
                    string id = Path.GetFileNameWithoutExtension(wav).Substring("scratch_".Length);
                    if (File.Exists(vo + "/" + id + ".mp3") && AssetDatabase.DeleteAsset(wav)) count++;
                }
            Debug.Log($"Removed {count} scratch clip(s) whose line now has a real voice.");
        }

        // ------------------------------------------------------------------ clip stamps

        static string TextHash(string speech) => Hash128.Compute(speech).ToString();
        static string Stamp(string speech, string voice) => TextHash(speech) + "@" + Hash128.Compute(voice);
        // The edge voice is modulated per line (EdgeVoice.Modulation), so the stamp carries the line's own rate and pitch.
        static string VoiceFor(string voice, string speech) => EdgeVoice.Is(voice) ? voice + "|" + string.Join("|", EdgeVoice.Modulation(speech)) : voice;
        static string StampOf(string path)
        {
            var importer = AssetImporter.GetAtPath(path); // a UnityEngine.Object: test it with its bool, never ?. / ??
            return importer ? importer.userData ?? "" : "";
        }

        static void Import(string path, string stamp)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad; // the talking glow reads the waveform
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.userData = stamp;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------ proxy

        [Serializable] class Health { public bool tts; public string ttsVoice; }
        [Serializable] class TtsBody { public string text, previous_text, next_text; }

        // The proxy's voice signature when it can speak (ElevenLabs key set), else null.
        static string ProxyVoice(ProxyConfig config)
        {
            try
            {
                using var request = UnityWebRequest.Get(config.BaseUrl + "/health");
                request.timeout = 3;
                var op = request.SendWebRequest();
                while (!op.isDone) Thread.Sleep(10);
                if (request.result != UnityWebRequest.Result.Success) return null;
                var health = JsonUtility.FromJson<Health>(request.downloadHandler.text);
                if (health == null || !health.tts) { Debug.LogWarning("The Wattson proxy is running without ELEVENLABS_API_KEY."); return null; }
                return string.IsNullOrEmpty(health.ttsVoice) ? "elevenlabs" : health.ttsVoice;
            }
            catch { return null; }
        }

        // The numbered storyboard neighbours (S05_L06 <- S05_L07 -> S05_L08), sent so a sentence split over two clips keeps one melody.
        static readonly Regex Numbered = new Regex(@"^(.*_L)(\d+)$");

        static string Neighbour(Dictionary<string, DialogueLine> byId, string id, int step)
        {
            var match = Numbered.Match(id);
            if (!match.Success) return "";
            string digits = match.Groups[2].Value;
            string other = match.Groups[1].Value + (int.Parse(digits) + step).ToString(new string('0', digits.Length));
            if (!byId.TryGetValue(other, out var line)) return "";
            string speech = PronunciationLexicon.ForSpeech(line.Spoken);
            return speech.Length > 300 ? speech.Substring(0, 300) : speech;
        }

        static Result ProxyTts(ProxyConfig config, TtsBody body, string path, out string retryAfter)
        {
            retryAfter = null;
            using var request = new UnityWebRequest(config.BaseUrl + "/tts", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body))),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 90,
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Session-Id", ProxyConfig.SessionId);
            var op = request.SendWebRequest();
            while (!op.isDone) Thread.Sleep(20);
            if (request.responseCode == 429) { retryAfter = request.GetResponseHeader("Retry-After"); return Result.RateLimited; }
            var data = request.downloadHandler.data;
            if (request.result != UnityWebRequest.Result.Success || data == null || data.Length < 512)
            {
                Debug.LogWarning($"Proxy TTS failed ({request.responseCode} {request.error}): {request.downloadHandler.text}");
                return Result.Failed;
            }
            File.WriteAllBytes(path, data);
            return Result.Ok;
        }

        // ------------------------------------------------------------------ edge-tts (neural voice, no key)

        [Serializable] class EdgeLine { public string id, text, @out, rate, pitch; }
        static bool edgeUnavailable; // set for the rest of a Run once edge_bake.py could not make a single clip

        // One edge-tts run for every line of the scene whose mp3 is missing or stale: the manifest goes to the temp folder,
        // Tools/edge_bake.py speaks the lines four at a time, and each clip it reports OK is imported with its stamp and its id
        // added to "fresh". False only when the script failed without making anything, so the caller falls back to the scratch voice.
        static bool EdgeBatch(string sceneId, string vo, string voice, DialogueLine[] lines, HashSet<string> fresh)
        {
            var pending = new List<(string id, string speech, string mp3)>();
            foreach (var line in lines)
            {
                if (File.Exists(vo + "/" + line.id + ".wav") || File.Exists(vo + "/" + line.id + ".ogg")) continue; // voice actor
                string speech = PronunciationLexicon.ForSpeech(line.Spoken), mp3 = vo + "/" + line.id + ".mp3";
                if (!File.Exists(mp3) || StampOf(mp3) != Stamp(speech, VoiceFor(voice, speech))) pending.Add((line.id, speech, mp3));
            }
            if (pending.Count == 0) return true;

            EditorUtility.DisplayProgressBar("Bake VO " + sceneId, $"edge-tts: speaking {pending.Count} line(s) as {EdgeVoice.Name}...", 0f);
            string manifest = Path.Combine(Path.GetTempPath(), "ohm_edge_" + sceneId + ".json");
            File.WriteAllText(manifest, "[" + string.Join(",", pending.Select(p => { var (rate, pitch) = EdgeVoice.Modulation(p.speech); return JsonUtility.ToJson(new EdgeLine { id = p.id, text = p.speech, @out = Path.GetFullPath(p.mp3), rate = rate, pitch = pitch }); })) + "]",
                              new UTF8Encoding(false));
            string script = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Tools", "edge_bake.py");
            var output = new StringBuilder();
            bool ok;
            try { ok = Run("python", $"\"{script}\" \"{manifest}\" --voice={EdgeVoice.Name} --rate={EdgeVoice.Rate} --pitch={EdgeVoice.Pitch}", 600000, output); }
            catch (Exception e) { Debug.LogWarning("edge-tts could not start: " + e.Message); ok = false; }

            var spoken = new HashSet<string>(StringComparer.Ordinal);
            foreach (var raw in output.ToString().Split('\n'))
            {
                string text = raw.Trim();
                if (text.StartsWith("OK ")) spoken.Add(text.Substring(3));
                else if (text.StartsWith("FAIL ")) Debug.LogWarning("edge-tts failed for " + text.Substring(5));
            }
            EditorUtility.DisplayProgressBar("Bake VO " + sceneId, $"edge-tts: importing {spoken.Count} clip(s)", 0f);
            foreach (var (id, speech, mp3) in pending)
                if (spoken.Contains(id) && File.Exists(mp3)) { Import(mp3, Stamp(speech, VoiceFor(voice, speech))); fresh.Add(id); }
            return ok || fresh.Count > 0;
        }

        // ------------------------------------------------------------------ local scratch voice

        // Windows: the OneCore voice "Microsoft Mark" (WinRT), the clearest and youngest-sounding male voice that ships with
        // Windows, falling back to System.Speech's default voice. macOS: say; Linux: espeak.
        const string WindowsScript = @"param([string]$out, [string]$textFile)
$text = [System.IO.File]::ReadAllText($textFile, [System.Text.Encoding]::UTF8)
try {
  Add-Type -AssemblyName System.Runtime.WindowsRuntime
  $null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Media.SpeechSynthesis, ContentType = WindowsRuntime]
  $null = [Windows.Storage.Streams.DataReader, Windows.Storage.Streams, ContentType = WindowsRuntime]
  $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
  function Await($op, [Type]$type) { $task = $asTask.MakeGenericMethod($type).Invoke($null, @($op)); $null = $task.Wait(-1); $task.Result }
  $synth = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
  $voice = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices | Where-Object { $_.DisplayName -match 'Mark' } | Select-Object -First 1
  if ($voice) { $synth.Voice = $voice }
  $stream = Await ($synth.SynthesizeTextToStreamAsync($text)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
  $reader = New-Object Windows.Storage.Streams.DataReader($stream.GetInputStreamAt(0))
  $size = [uint32]$stream.Size
  $null = Await ($reader.LoadAsync($size)) ([uint32])
  $bytes = New-Object byte[] $size
  $reader.ReadBytes($bytes)
  [System.IO.File]::WriteAllBytes($out, $bytes)
  exit 0
} catch { }
Add-Type -AssemblyName System.Speech
$s = New-Object System.Speech.Synthesis.SpeechSynthesizer
$s.SetOutputToWaveFile($out)
$s.Speak($text)
$s.Dispose()
";

        static bool OsTts(string text, string path)
        {
            string full = Path.GetFullPath(path);
            try
            {
#if UNITY_EDITOR_WIN
                // Written outside the project first, so a failure keeps the clip the line already has.
                string script = Path.Combine(Path.GetTempPath(), "ohm_tts.ps1"), textFile = Path.Combine(Path.GetTempPath(), "ohm_tts.txt");
                string wav = Path.Combine(Path.GetTempPath(), "ohm_tts.wav");
                File.WriteAllText(script, WindowsScript, new UTF8Encoding(true));
                File.WriteAllText(textFile, text, new UTF8Encoding(false));
                if (File.Exists(wav)) File.Delete(wav);
                if (!Run("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" \"{wav}\" \"{textFile}\"") || !File.Exists(wav) || new FileInfo(wav).Length < 1000) return false;
                File.Copy(wav, full, true);
                return true;
#elif UNITY_EDITOR_OSX
                string aiff = full + ".aiff";
                if (!Run("say", $"-o \"{aiff}\" \"{text.Replace("\"", "'")}\"")) return false;
                bool ok = Run("afconvert", $"-f WAVE -d LEI16 \"{aiff}\" \"{full}\"");
                File.Delete(aiff);
                return ok && File.Exists(full);
#else
                return Run("espeak", $"-w \"{full}\" \"{text.Replace("\"", "'")}\"") && File.Exists(full);
#endif
            }
            catch (Exception e) { Debug.LogWarning("Local text-to-speech unavailable: " + e.Message); return false; }
        }

        // Runs a tool to completion, a minute by default; "output" collects its standard output when given.
        static bool Run(string file, string args, int timeoutMs = 60000, StringBuilder output = null)
        {
            var info = new ProcessStartInfo(file, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = output != null };
            if (output != null) info.StandardOutputEncoding = Encoding.UTF8;
            using var process = Process.Start(info);
            if (process == null) return false;
            var error = Task.Run(() => process.StandardError.ReadToEnd()); // both pipes drain while we wait, so neither fills and stalls the tool
            var stdout = output != null ? Task.Run(() => process.StandardOutput.ReadToEnd()) : null;
            bool exited = process.WaitForExit(timeoutMs);
            if (!exited) { try { process.Kill(); } catch { } }
            if (stdout != null) output.Append(stdout.Result);
            string err = error.Result; // both pipes close with the process, killed or not
            if (!exited) { Debug.LogWarning($"{file} did not finish within {timeoutMs / 1000} s and was stopped."); return false; }
            if (process.ExitCode != 0) Debug.LogWarning($"{file} failed: {err}");
            return process.ExitCode == 0;
        }

        // ------------------------------------------------------------------ self-check

        // Ohm's Law/Tests/Voice Pipeline: lexicon, clip lookup and placement, and ducking. Logs PASS / FAIL lines.
        [MenuItem("Ohm's Law/Tests/Voice Pipeline")]
        public static void SelfCheck()
        {
            int fails = 0;
            void Check(bool ok, string what) { if (!ok) fails++; Debug.Log((ok ? "PASS " : "FAIL ") + what); }

            foreach (var (input, expected) in new[]
            {
                ("At 1 V the ammeter read 0.20 A. What will it read at 2 V?", "At 1 volt the ammeter read 0.20 amperes. What will it read at 2 volts?"),
                ("1 Ω = 1 V / 1 A", "1 ohm equals 1 volt divided by 1 ampere"),
                ("3.0 ÷ 0.60 = ?", "3.0 divided by 0.60 equals?"),
                ("Slope = ΔV / ΔI", "Slope equals delta V divided by delta I"),
                ("electrons drift from the − end toward the + end", "electrons drift from the negative end toward the positive end"),
                ("Georg Ohm and the nichrome wire XY", "Gay-ork Ohm and the Nye-krome wire X Y"),
                ("In 1826, Ohm kept changing one condition. Back to 1826 you go.", "In eighteen twenty-six, Ohm kept changing one condition. Back to eighteen twenty-six you go."),
                ("born in 1789, published in 1827; 1800 and 1805", "born in seventeen eighty-nine, published in eighteen twenty-seven; eighteen hundred and eighteen oh five"),
                ("0.1826 A, 1000 and 1200 Ω", "0.1826 amperes, 1000 and 1200 ohms"),
            })
                Check(PronunciationLexicon.Apply(input) == expected, $"lexicon \"{input}\" -> \"{PronunciationLexicon.Apply(input)}\"");
            Check(PronunciationLexicon.ForSpeech("...we call that electric current.") == "we call that, electric current.", "delivery: continuation and reveal beat");

            var lines = AssetDatabase.FindAssets("t:DialogueLine", new[] { BlockoutAssets.Root })
                .Select(guid => AssetDatabase.LoadAssetAtPath<DialogueLine>(AssetDatabase.GUIDToAssetPath(guid))).Where(line => line).ToArray();
            var symbols = new Regex("[Ω∝÷×→−=Δ∆]|\\bV ?/ ?I\\b");
            var leftovers = lines.Where(line => symbols.IsMatch(PronunciationLexicon.ForSpeech(line.Spoken) ?? "")).Select(line => line.id).ToArray();
            Check(leftovers.Length == 0, $"{lines.Length} lines: no symbol left in the spoken text {string.Join(", ", leftovers)}");
            var unstable = lines.Where(line => { var once = PronunciationLexicon.ForSpeech(line.Spoken); return PronunciationLexicon.ForSpeech(once) != once; }).Select(line => line.id).ToArray();
            Check(unstable.Length == 0, $"spoken text is stable when applied twice {string.Join(", ", unstable)}");

            string SceneFolder(DialogueLine line)
            {
                var parts = AssetDatabase.GetAssetPath(line).Split('/');
                return parts.Length > 2 && parts[2].StartsWith("Scene") ? BlockoutAssets.Root + "/" + parts[2] : BlockoutAssets.Root + "/" + Scene04;
            }
            var misplaced = lines.Where(line => line.vo && !AssetDatabase.GetAssetPath(line.vo).StartsWith(SceneFolder(line) + "/Audio/VO/")).Select(line => line.id).ToArray();
            Check(misplaced.Length == 0, $"every clip lives in its own scene's Audio/VO {string.Join(", ", misplaced)}");
            var notPreferred = lines.Where(line => File.Exists(SceneFolder(line) + "/Audio/VO/" + line.id + ".mp3") && line.vo != BlockoutAssets.FindVo(SceneFolder(line), line.id))
                .Select(line => line.id).ToArray();
            Check(notPreferred.Length == 0, $"a baked <id>.mp3 is the clip in use {string.Join(", ", notPreferred)}");
            var unprefixed = Directory.GetFiles(BlockoutAssets.Root + "/" + Scene04 + "/Audio/VO", "*.wav").Select(Path.GetFileName).Where(name => !name.StartsWith("scratch_")).ToArray();
            Check(unprefixed.Length == 0, $"Scene 4's old Windows-voice clips are marked scratch {string.Join(", ", unprefixed)}");
            Debug.Log("INFO clips: " + string.Join(" · ", lines.GroupBy(SceneFolder).OrderBy(g => g.Key).Select(g =>
                $"{Path.GetFileName(g.Key)} {g.Count(l => l.vo && !BlockoutAssets.IsScratch(l.vo))} real / {g.Count(l => BlockoutAssets.IsScratch(l.vo))} scratch / {g.Count(l => !l.vo)} silent")));

            // Ducking: music and effects drop while a voice speaks, the voice is untouched, everything comes back afterwards,
            // and a bus that rewrites its volume every frame does not get ducked twice.
            var made = new List<GameObject>();
            AudioSource Source(string name, float volume)
            {
                var go = EditorUtility.CreateGameObjectWithHideFlags("VoiceCheck " + name, HideFlags.HideAndDontSave, typeof(AudioSource));
                made.Add(go);
                var source = go.GetComponent<AudioSource>();
                source.volume = volume;
                return source;
            }
            try
            {
                var music = Source("music", 0.8f);
                music.gameObject.AddComponent<AudioBus>().kind = AudioBus.Kind.Music;
                var sfx = Source("sfx", 1f);
                var voice = Source("voice", 1f);
                VoiceChannel.Register(voice);
                var ducker = Source("ducker", 1f).gameObject.AddComponent<AudioDucker>();
                var all = new[] { music, sfx, voice };
                ducker.Step(1f, true, all);
                Check(Mathf.Approximately(music.volume, 0.8f * ducker.music) && Mathf.Approximately(sfx.volume, ducker.effects) && voice.volume == 1f,
                      $"ducked while speaking: music {music.volume:0.00}, effects {sfx.volume:0.00}, voice {voice.volume:0.00}");
                music.volume = 0.8f; // what AudioBus.Update writes every frame
                ducker.Step(0.02f, true, all);
                Check(Mathf.Approximately(music.volume, 0.8f * ducker.music), $"no double ducking under a bus: music {music.volume:0.00}");
                ducker.Step(1f, false, all);
                Check(Mathf.Approximately(music.volume, 0.8f) && Mathf.Approximately(sfx.volume, 1f), $"restored after the voice: music {music.volume:0.00}, effects {sfx.volume:0.00}");
                Check(!VoiceChannel.Busy(), "VoiceChannel is not busy when no voice plays");
            }
            finally { foreach (var go in made) UnityEngine.Object.DestroyImmediate(go); }

            Debug.Log(fails == 0 ? "PASS Voice Pipeline: all checks passed" : $"FAIL Voice Pipeline: {fails} check(s) failed");
        }
    }

    // Wattson's voice (2026-10-10): Microsoft's neural "Ryan" (en-GB, a warm, lively detective) through the free edge-tts endpoint,
    // brightened a little, until an ElevenLabs key exists. Modulation(): each line gets its own rate and pitch from its shape, so
    // questions lift, exclamations and short beats quicken, and long explanations slow down. Changing any constant changes the
    // signature, so the next bake redoes every edge clip.
    public static class EdgeVoice
    {
        public const string Name = "en-GB-RyanNeural", Rate = "+8%", Pitch = "+6Hz";
        const int BaseRate = 8, BasePitch = 6;
        public static string Signature => "edge:" + Name + "|" + Rate + "|" + Pitch;
        // Per-line modulation: (rate, pitch) strings edge-tts accepts. ponytail: four rules on punctuation and length; SSML prosody per phrase if a voice director wants more.
        public static (string rate, string pitch) Modulation(string speech)
        {
            string t = (speech ?? "").Trim();
            int rate = BaseRate, pitch = BasePitch;
            if (t.EndsWith("?")) { pitch += 5; rate -= 2; }                       // a question lifts and leaves room
            else if (t.EndsWith("!")) { rate += 6; pitch += 3; }                   // an exclamation quickens
            else if (t.Length < 32) { rate += 4; pitch += 2; }                     // a short beat is snappy
            if (t.Length > 110) rate -= 5;                                         // a long explanation slows down
            if (t.Contains("...")) rate -= 2;                                      // a pause written in
            return ((rate >= 0 ? "+" : "") + rate + "%", (pitch >= 0 ? "+" : "") + pitch + "Hz");
        }
        public static bool Is(string voice) => voice != null && voice.StartsWith("edge:");
    }
}
