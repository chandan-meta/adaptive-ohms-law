using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace OhmsLaw
{
    [Serializable]
    public class SubtitleClip : PlayableAsset, ITimelineClipAsset
    {
        public DialogueLine line;
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<SubtitleBehaviour>.Create(graph);
            playable.GetBehaviour().line = line;
            return playable;
        }
    }
}
