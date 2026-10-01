using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace OhmsLaw
{
    // Timeline marker where the cinematic stops and waits for the learner. SceneDirector holds the Timeline,
    // runs the interaction named by gateId (with its branch lines), then resumes.
    [Serializable, DisplayName("Ohm's Law/Interaction Gate")]
    public class InteractionGate : Marker, INotification, INotificationOptionProvider
    {
        public string gateId;
        [TextArea, Tooltip("What the learner does here. Written into Docs/FLOW.md.")] public string description;
        [Tooltip("Lines this gate may speak, depending on the answer.")] public DialogueLine[] lines = new DialogueLine[0];

        public PropertyName id => new PropertyName(gateId);
        public NotificationFlags flags => NotificationFlags.TriggerOnce; // never re-asked after a replay jump
    }
}
