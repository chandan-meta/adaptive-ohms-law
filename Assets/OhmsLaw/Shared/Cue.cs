using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace OhmsLaw
{
    // Timeline marker that sets scene state, e.g. "wire.field 1" or "table.rows 3". SceneDirector.Run applies it.
    // Commands only set state, so replaying a stretch of Timeline re-applies them safely.
    [Serializable, DisplayName("Ohm's Law/Cue")]
    public class Cue : Marker, INotification, INotificationOptionProvider
    {
        public string command;
        public string note;

        public PropertyName id => new PropertyName(command);
        public NotificationFlags flags => NotificationFlags.Retroactive;
    }
}
