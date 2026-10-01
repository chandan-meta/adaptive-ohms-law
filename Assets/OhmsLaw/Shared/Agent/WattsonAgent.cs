using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace OhmsLaw
{
    // Wattson's live Q&A brain: question -> proxy chat (streamed) -> sentence-by-sentence speech in his own voice.
    // Lives on the persistent module root, so the conversation carries across scenes.
    public class WattsonAgent : MonoBehaviour
    {
        public enum State { Idle, Listening, Thinking, Talking }

        const int MaxTurns = 6, MaxQuestionLength = 300;
        static readonly Regex Sentence = new Regex(@"^\s*(.+?[.!?।…]+[""'’”)\]]*)(?=\s)", RegexOptions.Singleline);

        public IChatClient Chat;
        public ITextToSpeech Speech;
        public ISpeechToText Hearing;

        public State Current { get; private set; }
        public bool Offline { get; private set; }
        public bool Engaged { get; private set; }
        public IReadOnlyList<ChatTurn> History => history;

        public event Action<State> StateChanged;
        public event Action<string, string> BubbleChanged;              // role, text so far
        public event Action<string> Notice;                             // friendly status for the student
        public event Action<string[], Action<int>> LearnerQuestion;     // options, pick callback

        readonly List<ChatTurn> history = new List<ChatTurn>();
        CancellationTokenSource running;

        static ProxyConfig Config => ModuleFlow.Instance.proxy;
        static WattsonController Wattson => WattsonController.Active;
        static SceneAgentContext Context => Wattson ? Wattson.agentContext : null;

        void Awake()
        {
            Chat = new ProxyChatClient(Config);
            Speech = new ProxyTextToSpeech(Config);
            Hearing = new ProxySpeechToText(Config);
        }

        // The Ask panel opened or closed: Wattson turns to the student, or goes back to the cinematic.
        public void Engage(bool on)
        {
            Engaged = on;
            if (!on) Stop();
            ApplyPose();
        }

        public void Stop()
        {
            running?.Cancel();
            running = null;
            Mic.Stop();
            if (Wattson && Wattson.liveVoice) Wattson.liveVoice.Stop();
            if (SubtitleUI.Instance) SubtitleUI.Instance.Hide(this);
            SetState(State.Idle);
        }

        public async void Ask(string question)
        {
            question = (question ?? "").Trim();
            if (question.Length == 0) return;
            if (question.Length > MaxQuestionLength) question = question.Substring(0, MaxQuestionLength);
            Stop();
            var cancel = (running = new CancellationTokenSource()).Token;
            BubbleChanged?.Invoke("user", question);
            try
            {
                // While the proxy is down, the suggested questions answer from the local FAQ.
                var local = Offline && Context && Context.faq ? Context.faq.Find(question) : null;
                if (local)
                {
                    BubbleChanged?.Invoke("assistant", local.text);
                    await Say(local, cancel);
                }
                else
                {
                    SetState(State.Thinking);
                    string answer = await Answer(question, cancel);
                    Offline = false;
                    Remember(question, answer);
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception error)
            {
                if (cancel.IsCancellationRequested) return;
                Debug.LogWarning("Wattson proxy: " + error.Message);
                Offline = true;
                await Fallback(question, cancel);
            }
            if (!cancel.IsCancellationRequested) SetState(State.Idle);
        }

        async Awaitable Fallback(string question, CancellationToken cancel)
        {
            var line = Context && Context.faq ? Context.faq.Find(question) : null;
            if (!line) line = Config.offlineLine;
            if (!line) return;
            BubbleChanged?.Invoke("assistant", line.text);
            try { await Say(line, cancel); } catch (OperationCanceledException) { }
        }

        async Awaitable<string> Answer(string question, CancellationToken cancel)
        {
            var context = Context;
            var request = new ChatRequest
            {
                sceneId = context ? context.sceneId : "",
                sceneTitle = context ? context.sceneTitle : "",
                currentLine = SubtitleTrack.CurrentLine ? SubtitleTrack.CurrentLine.text : "",
                covered = context ? context.covered : new string[0],
                doNotSpoil = context ? context.doNotSpoil : new string[0],
                history = history.ToArray(),
                question = question,
            };

            var full = new StringBuilder();
            string unspoken = "";
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
                    full.Append(text);
                    unspoken += text;
                    BubbleChanged?.Invoke("assistant", full.ToString());
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
            cancel.ThrowIfCancellationRequested();
            if (failure != null && full.Length == 0) throw failure;
            return full.ToString();
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
                // A voice failure still leaves the answer readable in the bubble and subtitles.
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

        // Speaks a pre-written line: the offline message, a FAQ answer or an AskLearner prompt.
        async Awaitable Say(DialogueLine line, CancellationToken cancel)
        {
            SetState(State.Talking);
            await Voice(line.text, line.vo, line.fallbackDuration, cancel);
        }

        // ponytail: clips are queued a frame apart; schedule on two sources with PlayScheduled if a sample-exact join is needed
        async Awaitable Voice(string text, AudioClip clip, float silentSeconds, CancellationToken cancel)
        {
            var source = Wattson ? Wattson.liveVoice : null;
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
            // Memory only, last few turns, never written to disk.
            history.Add(new ChatTurn { role = "user", text = question });
            history.Add(new ChatTurn { role = "assistant", text = answer });
            while (history.Count > MaxTurns * 2) history.RemoveRange(0, 2);
        }

        // Push-to-talk: call on press and on release.
        public void StartListening()
        {
            Stop();
            string problem = Mic.Start();
            if (problem != null) { Notice?.Invoke(problem); return; }
            SetState(State.Listening);
        }

        public async void StopListening()
        {
            if (Current != State.Listening) return;
            byte[] wav = Mic.Stop();
            if (wav == null) { SetState(State.Idle); Notice?.Invoke("I didn't catch that. Hold the button while you speak."); return; }
            SetState(State.Thinking);
            var cancel = (running = new CancellationTokenSource()).Token;
            try
            {
                string heard = await Hearing.TranscribeAsync(wav, cancel);
                if (string.IsNullOrWhiteSpace(heard)) { SetState(State.Idle); Notice?.Invoke("I didn't catch that. Try again, or type your question."); return; }
                Ask(heard);
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                Debug.LogWarning("Wattson hearing: " + error.Message);
                Offline = true;
                await Fallback("", cancel);
                if (!cancel.IsCancellationRequested) SetState(State.Idle);
            }
        }

        // Scripted, storyboard-driven question for later scenes. Nothing here is generated by the model:
        // the question, the options and every response are authored lines.
        public async void AskLearner(DialogueLine questionLine, string[] options, int correctIndex, DialogueLine[] responsesPerOption, Action<int, bool> answered = null)
        {
            Stop();
            var cancel = (running = new CancellationTokenSource()).Token;
            try
            {
                BubbleChanged?.Invoke("assistant", questionLine.text);
                await Say(questionLine, cancel);
                SetState(State.Idle);
                LearnerQuestion?.Invoke(options, async index =>
                {
                    if (index < 0 || index >= responsesPerOption.Length) return;
                    Stop();
                    var reply = (running = new CancellationTokenSource()).Token;
                    answered?.Invoke(index, index == correctIndex);
                    try
                    {
                        BubbleChanged?.Invoke("assistant", responsesPerOption[index].text);
                        await Say(responsesPerOption[index], reply);
                        SetState(State.Idle);
                    }
                    catch (OperationCanceledException) { }
                });
            }
            catch (OperationCanceledException) { }
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
