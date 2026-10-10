using UnityEngine;

namespace OhmsLaw
{
    // Scene 9's schematic on a world board: a world-space canvas floating over the back of the bench, navy glass with a
    // cyan hairline, hosting the live CircuitSchematic (CircuitSchematic.CreateOn) so it stays in step with the rig while
    // the learner builds the circuit. The builder places the root (hidden) and the "show Schematic Board" cue reveals it;
    // the canvas is built on first enable, when the HUD theme is loaded.
    public class WorldSchematic : MonoBehaviour
    {
        [Tooltip("Board width in metres; the height follows the schematic's aspect.")] public float width = 1.4f;
        const float PadX = 40f, PadY = 20f;   // canvas px around the schematic
        static readonly Color Glass = new Color(0.031f, 0.102f, 0.169f, 0.72f), Hairline = new Color(0.25f, 0.85f, 1f, 0.45f);

        bool built;

        void OnEnable()
        {
            if (built) return;
            built = true;
            var px = CircuitSchematic.Size + new Vector2(PadX * 2f, PadY * 2f);
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<RectTransform>();
            canvas.SetParent(transform, false);
            var c = canvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = Camera.main;
            canvas.sizeDelta = px;
            canvas.localScale = Vector3.one * (width / px.x);
            UI.Panel(UI.Stretch("Glass", canvas), Glass, true, 28f).raycastTarget = false;
            UI.Outline(UI.Stretch("Edge", canvas), Hairline, 28f, 3f);
            var area = UI.Rect("Area", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, CircuitSchematic.Size);
            CircuitSchematic.CreateOn(area);
        }
    }
}
