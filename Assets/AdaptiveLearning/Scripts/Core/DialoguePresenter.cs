using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    public class DialoguePresenter : MonoBehaviour
    {
        [SerializeField] Text speaker, subtitle;
        [SerializeField] AudioSource voice;
        public event Action<DialogueLine> LineStarted;
        public event Action<string> LineFinished;
        Coroutine running;
        DialogueLine currentLine;
        WattsonSpeech synthesizedVoice;
        public bool IsPresenting => running != null;
        public DialogueLine CurrentLine => currentLine;
        bool voicePaused;
        public void BindSpeech(WattsonSpeech speech) => synthesizedVoice = speech;
        public void PauseVoice(bool paused)
        {
            if (voicePaused == paused) return;
            voicePaused=paused;
            if (voice != null) { if(paused)voice.Pause();else voice.UnPause(); }
            if (currentLine != null && currentLine.audio == null)
            {
                if(paused)synthesizedVoice?.Stop();
                else synthesizedVoice?.Speak(currentLine.text);
            }
        }
        public void Configure(Text nameText, Text body, AudioSource source) { speaker = nameText; subtitle = body; voice = source; }
        public void Show(DialogueLine line)
        {
            if (line == null) return;
            if (running != null) StopCoroutine(running);
            currentLine = line;
            voice.Stop(); speaker.text = line.speaker; subtitle.text = line.text;
            if (line.audio != null) { voice.clip = line.audio; voice.Play();if(voicePaused)voice.Pause(); }
            LineStarted?.Invoke(line);
            if (line.audio == null && !voicePaused) synthesizedVoice?.Speak(line.text);
            running = StartCoroutine(EndLine(line));
        }
        IEnumerator EndLine(DialogueLine line)
        {
            float remaining=Mathf.Max(line.duration,line.audio!=null?line.audio.length:0);
            while(remaining>0 || voicePaused || (line.audio == null && synthesizedVoice != null && synthesizedVoice.IsSpeaking))
            {if(!voicePaused)remaining-=Time.deltaTime;yield return null;}
            LineFinished?.Invoke(line.nextEvent); running = null; currentLine = null;
        }

        public void SkipCurrentLine()
        {
            if (running == null) return;
            StopCoroutine(running); running = null;
            if (voice != null) voice.Stop();
            synthesizedVoice?.Stop();
            var line = currentLine; currentLine = null;
            LineFinished?.Invoke(line != null ? line.nextEvent : string.Empty);
        }

        public void ReplayCurrentLine()
        {
            var line = currentLine;
            if (line != null) Show(line);
        }
    }
}
