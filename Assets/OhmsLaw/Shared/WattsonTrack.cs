using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace OhmsLaw
{
    // Timeline track of Wattson's gestures: what he does, where he looks and what he points at.
    [TrackColor(1f, 0.85f, 0.2f)]
    [TrackBindingType(typeof(WattsonController))]
    [TrackClipType(typeof(WattsonClip))]
    public class WattsonTrack : TrackAsset { }


    public class WattsonBehaviour : PlayableBehaviour
    {
        public WattsonController.Gesture gesture;
        public Transform lookAt, pointAt, moveTo;
        public bool gadgetOut;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var wattson = playerData as WattsonController;
            if (!wattson) return;
            wattson.gesture = gesture;
            wattson.gadgetOut = gadgetOut;
            wattson.lookTarget = lookAt;
            wattson.pointTarget = pointAt;
            wattson.MoveTo(moveTo);
        }
    }
}
