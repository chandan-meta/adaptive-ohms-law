using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace OhmsLaw
{
    // Timeline track of scripted lines: shows the subtitle and drives Wattson's talking.
    [TrackColor(0.75f, 0.96f, 1f)]
    [TrackClipType(typeof(SubtitleClip))]
    public class SubtitleTrack : TrackAsset
    {
        public static DialogueLine CurrentLine { get; internal set; }
        // Timeline time where the most recent line began; used by "Replay line" and "Back to the case".
        public static double LastLineStart { get; internal set; } = -1;
        // Every scripted line as it starts, from the Timeline or from an interaction branch. Used by reviews and tests.
        public static event Action<DialogueLine> LineStarted;
        internal static void Started(DialogueLine line) => LineStarted?.Invoke(line);
    }


    public class SubtitleBehaviour : PlayableBehaviour
    {
        public DialogueLine line;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!line || !Application.isPlaying) return;
            SubtitleTrack.CurrentLine = line;
            SubtitleTrack.Started(line);
            if (playable.GetGraph().GetResolver() is PlayableDirector director)
                SubtitleTrack.LastLineStart = director.time - playable.GetTime();
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!line || !Application.isPlaying) return;
            if (SubtitleTrack.CurrentLine == line) SubtitleTrack.CurrentLine = null;
            if (SubtitleUI.Instance) SubtitleUI.Instance.Hide(line);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!line || !Application.isPlaying || info.effectiveSpeed <= 0) return;
            // Re-assert every frame so the line comes back after an Ask Wattson answer used the subtitle box.
            if (SubtitleUI.Instance) SubtitleUI.Instance.Show(line, line.speaker, line.text, (float)(playable.GetTime() / playable.GetDuration()));
            if (WattsonController.Active) WattsonController.Active.ScriptedSpeech(line.vo, (float)playable.GetTime());
        }
    }
}
