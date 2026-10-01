using UnityEngine;

namespace OhmsLaw
{
    // Text and placement of one screen-space callout (see Callouts.cs), on the GameObject it points at - or on an
    // empty node for a concept panel. Builders attach it (SceneBuilderBase.Callout / Concept); the cues
    // "callout show <id>" and "callout hide <id>" find it by id. Its own file, so scenes can serialise it.
    public class CalloutDef : MonoBehaviour
    {
        public enum Kind { Object, Vfx, Concept }
        public enum Anchor { TopLeft, TopCentre, TopRight, MidLeft, MidRight, BottomLeft, BottomCentre, BottomRight, AboveTarget }
        public enum Diagram { None, Battery, Tube }

        [Tooltip("Name the cues use: callout show <id>.")] public string id;
        public string title;
        [TextArea] public string description;
        public Anchor anchor = Anchor.TopLeft;
        public Kind kind = Kind.Object;
        [Tooltip("Concept panels: the small cyan diagram at the right.")] public Diagram diagram = Diagram.None;
        [Tooltip("Marker dot at the centre of this object's renderer bounds (children included) rather than at its pivot.")] public bool atBoundsCentre = true;
        [Tooltip("World offset of the marker dot from that point, in metres.")] public Vector3 offset;
        [Tooltip("AboveTarget: the panel's bottom-centre relative to the marker, in reference px (y up).")] public Vector2 aboveOffset = new Vector2(0, 90);

        Renderer[] renderers;

        // Where the marker dot sits. Particle, line and trail renderers are skipped: sparks and the world-space
        // leads would pull the centre off the instrument.
        public Vector3 WorldPoint
        {
            get
            {
                if (!atBoundsCentre) return transform.position + offset;
                if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
                var bounds = new Bounds();
                bool any = false;
                foreach (var r in renderers)
                {
                    if (!r || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                    if (any) bounds.Encapsulate(r.bounds); else { bounds = r.bounds; any = true; }
                }
                return (any ? bounds.center : transform.position) + offset;
            }
        }
    }
}
