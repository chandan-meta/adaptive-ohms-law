using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OhmsLaw
{
    // Wattson's live companion. A question (typed, or heard by speech-to-text) goes to the lab controls first
    // (WattsonActions: no network, so it works offline and never waits on latency); anything else goes to the proxy chat
    // with the live module state, streamed and spoken sentence by sentence in his voice. Offline, saved FAQ answers and
    // the app's guidance are labelled as such, never passed off as live AI. Lives on the persistent module root, so the
    // conversation carries across scenes; an AI answer never outlives the moment it was asked in (scene or activity).
    public class WattsonAgent : MonoBehaviour
    {
        public enum State { Idle, Listening, Thinking, Talking, Error }
        // Who a message is from, shown as its label: the learner (typed, or as speech recognition heard it), Wattson's
        // live AI answer, a saved offline answer, or the app's own lab-controls confirmation.
        public enum Speaker { You, YouHeard, Wattson, Offline, LabControls }

        public class Message
        {
            public Speaker speaker;
            public string text;
            public bool FromLearner => speaker == Speaker.You || speaker == Speaker.YouHeard;
        }

        // MaxState / MaxLearner / MaxTurnText are the proxy's own caps (index.js MAX_STATE, MAX_LEARNER, history turns).
        const int MaxTurns = 6, MaxQuestionLength = 300, MaxLog = 40, RecentLines = 3, MaxState = 1500, MaxLearner = 800, MaxTurnText = 800;
        const float NarrationWait = 6f, ConfirmVoiceWait = 4f;
        static readonly Regex Sentence = new Regex(@"^\s*(.+?[.!?।…]+[""'’”)\]]*)(?=\s)", RegexOptions.Singleline);

        public IChatClient Chat;
        public ITextToSpeech Speech;
        public ISpeechToText Hearing;

        public State Current { get; private set; }
        public bool Offline { get; private set; }            // the chat proxy did not answer last time
        public bool VoiceInputDown { get; private set; }     // speech-to-text failed last time; typing still works
        public string Problem { get; private set; } = "";    // what went wrong, shown while State.Error
        public string Note { get; private set; } = "";       // a calm status line shown while Idle
        public bool Engaged { get; private set; }
        public IReadOnlyList<ChatTurn> History => history;
        public IReadOnlyList<Message> Log => log;

        public event Action<State> StateChanged;
        public event Action<Message> MessageAdded, MessageChanged;

        readonly List<ChatTurn> history = new List<ChatTurn>();
        readonly List<Message> log = new List<Message>();
        readonly List<string> recentLines = new List<string>();
        CancellationTokenSource running;
        bool runningIsAction;   // a lab-controls confirmation stays valid when the activity it completed ends
        int momentScene;        // the scene and activity the running answer belongs to
        string momentGate;

        static ProxyConfig Config => ModuleFlow.Instance ? ModuleFlow.Instance.proxy : null;
        static WattsonController Wattson => WattsonController.Active;
        static SceneAgentContext Context => Wattson ? Wattson.agentContext : null;
        static WattsonActions.LiveBench Live => WattsonActions.LiveBench.Instance;

        // An on-screen question, a prediction or a target activity is running: every answer is hint-only.
        public static bool InActivity => WattsonActions.ActivityRule(Live, OnScreenQuestion().Length > 0).Length > 0;

        // A saved FAQ answer (suggested question chip, offline answer) may be offered now: never during an activity, never
        // naming the ohm before it is taught.
        public static bool Offered(string question, DialogueLine answer) =>
            answer && WattsonActions.SavedAnswerAllowed(Live, InActivity, question, answer.text);

        void Awake()
        {
            Chat = new ProxyChatClient(Config);
            Speech = new ProxyTextToSpeech(Config);
            Hearing = new ProxySpeechToText(Config);
            SubtitleTrack.LineStarted += OnLine;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SubtitleTrack.LineStarted -= OnLine;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // The last few scripted lines, so "what did you mean by...?" can be answered.
        void OnLine(DialogueLine line)
        {
            if (!line || string.IsNullOrEmpty(line.text)) return;
            recentLines.Add(line.text);
            while (recentLines.Count > RecentLines) recentLines.RemoveAt(0);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            recentLines.Clear();
            Stop();
        }

        // The learner moved on (scene change, an activity opened or closed): a stale AI answer stops. A lab-controls
        // confirmation keeps going, since its own action is often what completed the activity.
        void Update()
        {
            int scene = SceneManager.GetActiveScene().handle;
            if (scene == momentScene && SceneDirector.Gate == momentGate) return;
            momentScene = scene;
            momentGate = SceneDirector.Gate;
            if (running == null || runningIsAction) return;
            Stop();
            Note = "The lesson moved on, so I stopped that answer. Ask again if you still want it.";
        }

        // The Ask panel opened or closed: Wattson turns to the student, or goes back to the cinematic.
        public void Engage(bool on)
        {
            Engaged = on;
            Note = "";
            if (!on || Current == State.Error) Stop();
            ApplyPose();
        }

        public void Stop()
        {
            running?.Cancel();
            running = null;
            runningIsAction = false;
            Mic.Stop();
            if (Wattson && Wattson.liveVoice) Wattson.liveVoice.Stop();
            if (SubtitleUI.Instance) SubtitleUI.Instance.Hide(this);
            SetState(State.Idle);
        }

        public async void Ask(string question) => await AskAsync(question, false);

        async Awaitable AskAsync(string question, bool heard)
        {
            question = (question ?? "").Trim();
            if (question.Length == 0) return;
            if (question.Length > MaxQuestionLength) question = question.Substring(0, MaxQuestionLength);
            Stop(); // a new question interrupts the old answer
            Note = "";
            var cancel = (running = new CancellationTokenSource()).Token;
            Add(heard ? Speaker.YouHeard : Speaker.You, question);
            try
            {
                if (WattsonActions.TryHandle(question, out string done))
                {
                    runningIsAction = true;
                    Add(Speaker.LabControls, done);
                    await Confirm(done, cancel);
                }
                else
                {
                    // While the proxy is down, the suggested questions answer from the local FAQ without retrying it.
                    var saved = Offline ? Saved(question) : null;
                    if (saved) await SayOffline(saved, cancel);
                    else
                    {
                        SetState(State.Thinking);
                        string answer = await Answer(question, cancel);
                        Offline = false;
                        Remember(question, answer);
                    }
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception error)
            {
                if (cancel.IsCancellationRequested) return;
                Debug.LogWarning("Wattson proxy: " + error.Message);
                Offline = true;
                if (error.Message == "rate_limited" || error.Message == "busy") Note = "My radio needs a short rest: lots of questions just now. Saved answers still work.";
                try { await Fallback(question, cancel); } catch (OperationCanceledException) { return; }
            }
            if (cancel.IsCancellationRequested) return;
            running = null;
            runningIsAction = false;
            SetState(State.Idle);
        }

        // The app's lab-controls confirmation in Wattson's voice. The action has already happened, so a slow or missing
        // voice never holds it up: after a short wait the text is shown and read silently instead.
        async Awaitable Confirm(string text, CancellationToken cancel)
        {
            SetState(State.Talking);
            AudioClip clip = null;
            if (!Offline)
            {
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancel);
                wait.CancelAfter(TimeSpan.FromSeconds(ConfirmVoiceWait));
                try { clip = await Speech.SpeakAsync(WattsonActions.Spoken(text), wait.Token); }
                catch (OperationCanceledException) { cancel.ThrowIfCancellationRequested(); }
                catch (Exception error) { Debug.LogWarning("Wattson voice: " + error.Message); }
            }
            try { await Voice(text, clip, ReadingSeconds(text), cancel); }
            finally { if (clip) Destroy(clip); }
        }

        // No live answer: the saved FAQ answer when the question matches one, otherwise the offline line plus the app's
        // guidance from the real bench state. Both are labelled "Offline answer". During an activity there is no saved
        // answer, and the offline line ("Try one of these clues!") gives way to a note that the puzzle is the learner's.
        async Awaitable Fallback(string question, CancellationToken cancel)
        {
            var saved = Saved(question);
            if (saved) { await SayOffline(saved, cancel); return; }
            var line = Config ? Config.offlineLine : null;
            string guidance = WattsonActions.Guidance();
            if (InActivity)
            {
                line = null;
                guidance = WattsonActions.OfflinePuzzle + (Live.Present ? " " + guidance : "");
            }
            Add(Speaker.Offline, ((line ? line.text + " " : "") + guidance).Trim());
            SetState(State.Talking);
            if (line) await Voice(line.text, line.vo, line.fallbackDuration, cancel);
            await Voice(guidance, null, ReadingSeconds(guidance), cancel);
        }

        static DialogueLine Saved(string question)
        {
            var saved = Context && Context.faq ? Context.faq.Find(question) : null;
            return Offered(question, saved) ? saved : null;
        }

        async Awaitable SayOffline(DialogueLine line, CancellationToken cancel)
        {
            Add(Speaker.Offline, line.text);
            SetState(State.Talking);
            await Voice(line.text, line.vo, line.fallbackDuration, cancel);
        }

        async Awaitable<string> Answer(string question, CancellationToken cancel)
        {
            var context = Context;
            var speaking = SceneDirector.Speaking ? SceneDirector.Speaking : SubtitleTrack.CurrentLine;
            var request = new ChatRequest
            {
                sceneId = context ? context.sceneId : "",
                sceneTitle = context ? context.sceneTitle : "",
                currentLine = speaking ? speaking.text : "",
                covered = context ? Covered(context.covered) : new string[0],
                doNotSpoil = context ? context.doNotSpoil : new string[0],
                history = history.ToArray(),
                question = question,
                state = DescribeState(),
                learner = Cap(LearnerSession.Summary().Replace("\n", "; "), MaxLearner),
            };

            var full = new StringBuilder();
            string unspoken = "";
            Message message = null;
            var sentences = new List<string>();
            var clips = new List<AudioClip>();
            bool streamed = false;

            // Speech starts at the first complete sentence while the rest is still streaming in.
            var fetching = FetchSpeech(sentences, clips, () => streamed, cancel);
            var playing = PlaySpeech(sentences, clips, () => streamed, cancel);
            Exception failure = null;
            try
            {
                await Chat.StreamAsync(request, text =>
                {
                    if (cancel.IsCancellationRequested) return; // a late chunk never grows an answer the learner moved past
                    full.Append(text);
                    unspoken += text;
                    if (message == null) message = Add(Speaker.Wattson, full.ToString());
                    else { message.text = full.ToString(); MessageChanged?.Invoke(message); }
                    for (var match = Sentence.Match(unspoken); match.Success; match = Sentence.Match(unspoken))
                    {
                        sentences.Add(match.Groups[1].Value.Trim());
                        unspoken = unspoken.Substring(match.Length);
                    }
                }, cancel);
                if (unspoken.Trim().Length > 0) sentences.Add(unspoken.Trim());
            }
            catch (Exception error) { failure = error; }
            streamed = true;
            // Let whatever was already said finish, even if the stream broke part-way.
            try { await fetching; } catch (OperationCanceledException) { }
            try { await playing; } catch (OperationCanceledException) { }
            foreach (var clip in clips) if (clip) Destroy(clip); // fetched but never played (interrupted)
            cancel.ThrowIfCancellationRequested();
            if (failure != null && full.Length == 0) throw failure;
            return full.ToString();
        }

        // A scene's covered facts are written for the whole scene: Scene 12's name the ohm before S12_L14 has, Scene 10's
        // quote 2 V → 0.4 A and "the current increases" before row 2 is recorded. WattsonActions.FactAllowed keeps those out.
        static string[] Covered(string[] facts) =>
            facts == null ? new string[0] : Array.FindAll(facts, fact => WattsonActions.FactAllowed(Live, fact));

        // The live state the AI must ground its answer in (WOW_PLAN.md section 6): never invent a reading. The app's rules
        // ("[App rule: ...]", which the proxy prompt follows) come right after the scene, so the length cap never cuts them;
        // the recent scripted lines come last.
        string DescribeState()
        {
            var parts = new List<string>();
            var context = Context;
            var live = Live;
            string sceneId = context ? context.sceneId : SceneManager.GetActiveScene().name;
            parts.Add("Scene " + sceneId + (context && !string.IsNullOrEmpty(context.sceneTitle) ? " (" + context.sceneTitle + ")" : ""));
            string question = OnScreenQuestion();
            parts.Add(WattsonActions.Rule("the app itself changes the voltage when the learner asks; never say you changed, set or measured anything"));
            string activity = WattsonActions.ActivityRule(live, question.Length > 0);
            if (activity.Length > 0) parts.Add(activity);
            string oneVolt = WattsonActions.OneVoltRule(live);
            if (oneVolt.Length > 0) parts.Add(oneVolt);
            parts.Add(WattsonActions.ResistanceState(live));

            parts.Add(SceneDirector.Gate != null ? "Learner activity in progress: " + SceneDirector.Gate : "No learner activity right now; the story waits while the learner talks to you");
            string prompt = ScreenText("SafeArea/Prompt");
            if (prompt.Length > 0) parts.Add("On-screen instruction: " + prompt);
            if (question.Length > 0) parts.Add("On-screen question the learner is answering: " + question);

            var rig = CircuitRig.Active;
            if (rig)
            {
                var bench = new StringBuilder("Bench: source set to " + WattsonActions.Volts(rig.state.voltage) + " V, key " + (rig.state.keyClosed ? "closed" : "open"));
                bench.Append(rig.ammeterPlaced ? ", ammeter reads " + WattsonActions.Amps(rig.Amps) + " A" : ", ammeter not connected yet");
                bench.Append(rig.voltmeterPlaced ? ", voltmeter reads " + WattsonActions.Volts(rig.Volts) + " V" : ", voltmeter not connected yet");
                bench.Append(rig.controlsOpen ? ". The learner may change the voltage now (battery slider, or by asking you; whole volts 0-" + WattsonActions.Volts(SceneDirector.ControlsMax) + " V)" : ". The voltage controls are locked right now");
                if (rig.targetAmps >= 0) bench.Append(". Target activity: the learner must make the ammeter read " + WattsonActions.Amps(rig.targetAmps) + " A by choosing the voltage");
                if (!Mathf.Approximately(rig.PreviousVoltage, rig.state.voltage)) bench.Append(". Previous source setting: " + WattsonActions.Volts(rig.PreviousVoltage) + " V");
                parts.Add(bench.ToString());
                if (WattsonActions.LastChange.Length > 0 && Mathf.Approximately((float)WattsonActions.LastChangeVolts, rig.state.voltage)) parts.Add(WattsonActions.LastChange.TrimEnd('.'));
            }
            else parts.Add("Bench: there is no circuit in this scene");

            var data = ExperimentData.Main;
            int rows = data ? Mathf.Min(LearnerSession.tableRows, data.Count) : 0;
            if (rows > 0)
            {
                var recorded = new List<string>();
                for (int i = 0; i < rows; i++) recorded.Add(WattsonActions.Volts(data.voltages[i]) + " V, " + WattsonActions.Amps(data.currents[i]) + " A");
                parts.Add("Observation table rows recorded: " + string.Join("; ", recorded));
            }
            else parts.Add("No observation table rows recorded yet");
            if (recentLines.Count > 0) parts.Add("Wattson's recent scripted lines: " + string.Join(" / ", recentLines));

            string state = string.Join(". ", parts) + ".";
            return Cap(state, MaxState);
        }

        static string Cap(string text, int max) => text.Length > max ? text.Substring(0, max) : text;

        static string ScreenText(string path)
        {
            var flow = ModuleFlow.Instance;
            var found = flow ? flow.transform.Find(path) : null;
            var label = found ? found.GetComponentInChildren<TMP_Text>() : null;
            return label ? label.text.Trim() : "";
        }

        // The MCQ on screen: its question and options (MCQPanel: a "Label" child, then "Option A", "Option B", ...).
        static string OnScreenQuestion()
        {
            var flow = ModuleFlow.Instance;
            var panel = flow ? flow.transform.Find("SafeArea/MCQ Panel") : null;
            if (!panel) return "";
            var parts = new List<string>();
            foreach (Transform child in panel)
            {
                var label = child.GetComponentInChildren<TMP_Text>();
                if (!label || string.IsNullOrWhiteSpace(label.text)) continue;
                if (child.name.StartsWith("Option ")) parts.Add(child.name.Substring(7) + ". " + label.text.Trim());
                else if (child.name == "Label") parts.Insert(0, label.text.Trim());
            }
            return string.Join(" ", parts);
        }

        async Awaitable FetchSpeech(List<string> sentences, List<AudioClip> clips, Func<bool> streamed, CancellationToken cancel)
        {
            for (int i = 0; ; i++)
            {
                while (i >= sentences.Count)
                {
                    if (streamed()) return;
                    await Awaitable.NextFrameAsync(cancel);
                }
                AudioClip clip = null;
                // A voice failure still leaves the answer readable in the panel and the subtitles.
                try { clip = await Speech.SpeakAsync(sentences[i], cancel); }
                catch (AgentException error) { Debug.LogWarning("Wattson voice: " + error.Message); }
                clips.Add(clip);
            }
        }

        async Awaitable PlaySpeech(List<string> sentences, List<AudioClip> clips, Func<bool> streamed, CancellationToken cancel)
        {
            for (int i = 0; ; i++)
            {
                while (i >= clips.Count)
                {
                    if (streamed() && i >= sentences.Count) return;
                    await Awaitable.NextFrameAsync(cancel);
                }
                SetState(State.Talking);
                try { await Voice(sentences[i], clips[i], ReadingSeconds(sentences[i]), cancel); }
                finally { if (clips[i]) Destroy(clips[i]); }
            }
        }

        // ponytail: clips are queued a frame apart; schedule on two sources with PlayScheduled if a sample-exact join is needed
        async Awaitable Voice(string text, AudioClip clip, float silentSeconds, CancellationToken cancel)
        {
            var source = Wattson ? Wattson.liveVoice : null;
            // One voice at a time (ModuleFlow registers every voice source with VoiceChannel): wait for narration that is
            // still playing, capped so a stuck source cannot mute him.
            for (float waited = 0; waited < NarrationWait && VoiceChannel.Busy(source); waited += Time.unscaledDeltaTime)
                await Awaitable.NextFrameAsync(cancel);
            float length = clip && source ? clip.length : silentSeconds;
            if (clip && source) { source.clip = clip; source.Play(); }
            try
            {
                for (float t = 0; t < length && (!clip || !source || source.isPlaying || t < 0.1f); t += Time.unscaledDeltaTime)
                {
                    if (SubtitleUI.Instance) SubtitleUI.Instance.Show(this, "WATTSON", text, t / length);
                    await Awaitable.NextFrameAsync(cancel);
                }
            }
            finally { if (SubtitleUI.Instance) SubtitleUI.Instance.Hide(this); }
        }

        static float ReadingSeconds(string text) => Mathf.Clamp(text.Length * 0.06f, 1.5f, 8f);

        void Remember(string question, string answer)
        {
            // Memory only, last few turns, never written to disk. Lab-controls turns stay out: the live state carries them.
            history.Add(new ChatTurn { role = "user", text = question });
            history.Add(new ChatTurn { role = "assistant", text = Cap(answer, MaxTurnText) }); // the proxy keeps no more of a turn
            while (history.Count > MaxTurns * 2) history.RemoveRange(0, 2);
        }

        Message Add(Speaker speaker, string text)
        {
            var message = new Message { speaker = speaker, text = text };
            log.Add(message);
            if (log.Count > MaxLog) log.RemoveAt(0);
            MessageAdded?.Invoke(message);
            return message;
        }

        // Push-to-talk: call on press and on release.
        public void StartListening()
        {
            Stop(); // talking over Wattson cuts him off
            Note = "";
            string problem = Mic.Start();
            if (problem != null) { Fail(problem); return; }
            SetState(State.Listening);
        }

        public async void StopListening()
        {
            if (Current != State.Listening) return;
            byte[] wav = Mic.Stop();
            if (wav == null) { Fail("I didn't catch that. Hold the button while you speak, or type your question."); return; }
            SetState(State.Thinking);
            var cancel = (running = new CancellationTokenSource()).Token;
            runningIsAction = false;
            string heard;
            try { heard = await Hearing.TranscribeAsync(wav, cancel); }
            catch (OperationCanceledException) { return; }
            catch (Exception error)
            {
                if (cancel.IsCancellationRequested) return;
                Debug.LogWarning("Wattson hearing: " + error.Message);
                // Only voice input is down: the chat, the lab controls and typing carry on as before.
                running = null;
                VoiceInputDown = true;
                Fail(error.Message == "rate_limited"
                    ? "Voice input is resting for a few minutes. Please type your question."
                    : "Voice input needs the Wattson server, which isn't answering. Please type instead.");
                return;
            }
            if (cancel.IsCancellationRequested) return;
            running = null;
            VoiceInputDown = false;
            if (string.IsNullOrWhiteSpace(heard)) { Fail("I didn't catch that. Try again, or type your question."); return; }
            await AskAsync(heard, true);
        }

        void Fail(string problem)
        {
            Problem = problem;
            if (Current == State.Error) StateChanged?.Invoke(State.Error);
            else SetState(State.Error);
        }

        void SetState(State state)
        {
            if (Current == state) return;
            Current = state;
            ApplyPose();
            StateChanged?.Invoke(state);
        }

        void ApplyPose()
        {
            if (!Wattson) return;
            if (!Engaged) { Wattson.SetAgentPose(null, null); return; }
            var cam = Camera.main;
            Wattson.SetAgentPose(
                Current == State.Thinking ? WattsonController.Gesture.Thinking :
                Current == State.Talking ? WattsonController.Gesture.Talking : WattsonController.Gesture.Listening,
                cam ? cam.transform : null);
        }
    }
}
