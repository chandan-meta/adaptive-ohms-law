using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace OhmsLaw
{
    [Serializable]
    public class WattsonClip : PlayableAsset, ITimelineClipAsset
    {
        public WattsonController.Gesture gesture;
        public ExposedReference<Transform> lookAt, pointAt, moveTo;
        public bool gadgetOut;
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<WattsonBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.gesture = gesture;
            behaviour.gadgetOut = gadgetOut;
            behaviour.lookAt = lookAt.Resolve(graph.GetResolver());
            behaviour.pointAt = pointAt.Resolve(graph.GetResolver());
            behaviour.moveTo = moveTo.Resolve(graph.GetResolver());
            return playable;
        }
    }
}
