using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OhmsLaw
{
    // Wattson beside a screen surface (the data stage, a hero plate, the law), pointing at one element on it: a table row,
    // a plotted point, an axis title. He stands on the surface's side nearest the element, his whole figure outside the
    // surface, the dock (while it shows) and the caption bar, at the height of the element so the arm reads straight
    // across; a thin beam runs from his fingertip to the surface's edge (never over its content) and an outline marks the
    // element (a ring round a small one, a bracket on the edge of a row facing him, an underline under an axis title).
    // Everything follows the layout every frame: a stage that slides, scales or reflows takes him and the marks with it.
    // The overrides go through WattsonController.guideHome / guidePoint, which win over the Timeline's marker and target;
    // Release() (and every scene entry, and the element or the surface going away) hands him back to the Timeline.
    [DefaultExecutionOrder(2000)] // after LessonPanel lays its surfaces out (Follow) and after Wattson has posed this frame
    public class WattsonGuide : MonoBehaviour
    {
        public enum Mark { Auto, Ring, Bracket, Underline, None }

        public static bool Active => instance && instance.beside;

        // Point at `target` (its point at `pivot`, normalized in its rect; zero means the centre) from beside `beside` (null:
        // the target's nearest ancestor surface named "UI_…", e.g. UI_Stage, UI_Hero, UI_Dock; else the target itself).
        public static void Point(RectTransform target, Vector2 pivot = default, RectTransform beside = null, Mark mark = Mark.Auto)
        {
            if (!target) { Release(); return; }
            var guide = Instance();
            guide.Begin(beside ? beside : Surface(target), target, pivot == Vector2.zero ? new Vector2(0.5f, 0.5f) : pivot, 0, mark);
        }

        // Stand beside `beside` without pointing (side -1 left, +1 right, 0 the side he is already nearer).
        public static void Stand(RectTransform beside, int side = 0)
        {
            if (!beside) { Release(); return; }
            Instance().Begin(beside, null, Vector2.zero, side, Mark.None);
        }

        // Back to the Timeline's gestures and markers: he flies back to his marker.
        public static void Release()
        {
            if (!instance) return;
            instance.End();
        }

        // ------------------------------------------------------------------ setup

        static WattsonGuide instance;
        static readonly Vector3[] corners = new Vector3[4];

        RectTransform beside, target, layer, beam, ring, bracket, underline;
        Image beamImage, ringImage, underlineImage;
        Image[] bracketImages;
        Vector2 pivot;
        Mark mark;
        int side, forcedSide, blockedSide; // blockedSide: the set covered him there, for this element (no flip back)
        readonly System.Collections.Generic.List<Renderer> occluders = new System.Collections.Generic.List<Renderer>();
        float depth, shown;
        Transform proxy;
        WattsonController held;
        Vector3 centreOffset; // his figure's centre from his transform, in world space (the bounds at the first hold)
        Vector2 halfSize;     // his figure's half width / height in metres (the bounds at the first hold)
        Canvas canvas;

        const float MinDepth = 1.6f, MaxDepth = 3.2f, Gap = 28f, EdgeMargin = 24f, BeamWidth = 3f;

        static WattsonGuide Instance()
        {
            if (instance) return instance;
            // On the HUD's root (it outlives every scene), so the menus and the white fade still draw over the marks.
            var panel = LessonPanel.Instance;
            var go = panel ? UI.Stretch("Wattson Guide", panel.Root).gameObject : new GameObject("Wattson Guide");
            if (!panel) DontDestroyOnLoad(go);
            instance = go.AddComponent<WattsonGuide>();
            return instance;
        }

        // The nearest ancestor that is one of the HUD's surfaces (named "UI_…"); the target itself when there is none.
        static RectTransform Surface(RectTransform target)
        {
            for (var t = target.parent; t; t = t.parent)
                if (t.name.StartsWith("UI_") && t is RectTransform rect) return rect;
            return target;
        }

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (!canvas) return;
            layer = (RectTransform)transform;
            beam = UI.Rect("Beam", layer, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, BeamWidth));
            beamImage = beam.gameObject.AddComponent<Image>();
            ring = UI.Rect("Ring", layer, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 60f);
            ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = UI.Circle(0.86f);
            underline = UI.Rect("Underline", layer, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(60f, BeamWidth));
            underlineImage = underline.gameObject.AddComponent<Image>();
            // The bracket: a bar along the edge facing him with two short arms reaching onto the row's ends.
            bracket = UI.Rect("Bracket", layer, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 60f));
            bracketImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var part = UI.Rect("Part", bracket, new Vector2(0.5f, i == 0 ? 0.5f : i == 1 ? 1f : 0f), new Vector2(0.5f, i == 0 ? 0.5f : i == 1 ? 1f : 0f), Vector2.zero, i == 0 ? new Vector2(BeamWidth, 0f) : new Vector2(14f, BeamWidth));
                if (i == 0) { part.anchorMin = new Vector2(0.5f, 0f); part.anchorMax = new Vector2(0.5f, 1f); part.sizeDelta = new Vector2(BeamWidth, 0f); }
                bracketImages[i] = part.gameObject.AddComponent<Image>();
            }
            foreach (var image in GetComponentsInChildren<Image>(true)) image.raycastTarget = false;
            Show(0f);
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
            if (proxy) Destroy(proxy.gameObject);
        }

        // Every scene starts with Wattson under the Timeline's control.
        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => End();

        void Begin(RectTransform surface, RectTransform element, Vector2 at, int forced, Mark kind)
        {
            var wattson = WattsonController.Active;
            var cam = Camera.main;
            if (!wattson || !cam) { End(); return; }
            if (held && held != wattson) Clear(held);
            if (held != wattson || !beside) Measure(wattson, cam);
            beside = surface;
            target = element;
            pivot = at;
            mark = kind;
            forcedSide = forced;
            if (forced != 0) side = forced;
            blockedSide = 0;
            held = wattson;
            // What could stand between the lens and him: the bench apparatus and the furniture he already steers round.
            occluders.Clear();
            if (CircuitRig.Active) occluders.AddRange(CircuitRig.Active.GetComponentsInChildren<Renderer>());
            foreach (var r in wattson.furniture) if (r) occluders.Add(r);
            occluders.RemoveAll(r => r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer);
            if (element)
            {
                if (!proxy) { proxy = new GameObject("Wattson Point Proxy").transform; DontDestroyOnLoad(proxy.gameObject); }
                held.guidePoint = proxy;
            }
            else held.guidePoint = null;
            Place(cam); // this frame, so the proxy is under the element before he first aims at it
        }

        void End()
        {
            if (held) Clear(held);
            held = null;
            beside = target = null;
            shown = 0f;
            Show(0f);
        }

        static void Clear(WattsonController wattson)
        {
            wattson.guidePoint = null;
            wattson.guideHome = null;
        }

        // His size and centre from his renderers in their current pose, and the depth he holds at (his own, clamped).
        void Measure(WattsonController wattson, Camera cam)
        {
            var bounds = new Bounds(wattson.transform.position, Vector3.one * 0.3f);
            bool any = false;
            foreach (var r in wattson.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            centreOffset = bounds.center - wattson.transform.position;
            // A pointing arm in the pose widens the bounds: his body is never wider than about two thirds of its height.
            halfSize = new Vector2(Mathf.Min(Mathf.Max(bounds.extents.x, bounds.extents.z), bounds.extents.y * 0.7f), bounds.extents.y);
            float now = Vector3.Dot(wattson.transform.position - cam.transform.position, cam.transform.forward);
            depth = Mathf.Clamp(now, MinDepth, MaxDepth);
        }

        // ------------------------------------------------------------------ every frame

        void LateUpdate()
        {
            if (!beside) return;
            var cam = Camera.main;
            if (!held || !cam || !beside.gameObject.activeInHierarchy || (target && !target.gameObject.activeInHierarchy) || held != WattsonController.Active)
            {
                End();
                return;
            }
            Place(cam);
        }

        void Place(Camera cam)
        {
            Rect area = ScreenRect(beside);
            Vector2 aim = target ? ScreenPoint(target, pivot) : area.center;

            // His figure on screen at the depth he holds: pixels per metre there (PanelFraming's lens shift moves, never
            // scales, the picture).
            Vector3 probe = cam.transform.position + cam.transform.forward * depth;
            float ppm = Mathf.Abs(cam.WorldToScreenPoint(probe + cam.transform.up).y - cam.WorldToScreenPoint(probe).y);
            Vector2 half = halfSize * ppm;
            // The arm that points toward the surface reaches a little past his body: room for it before the edge.
            float reach = target ? half.x * 0.9f : 0f;

            // Height: level with the element (the arm reads straight across), above the captions and under the top row.
            float floor = CaptionTop() + Gap + half.y, ceiling = Screen.height - LessonPanel.DockTop * Scale - half.y;
            float y = Mathf.Clamp(aim.y, Mathf.Min(floor, ceiling), Mathf.Max(floor, ceiling));

            // The side: forced, else nearest the element, with a dead zone round the surface's centre so he does not flip,
            // and never a side where the set has already covered him for this element.
            if (forcedSide == 0)
            {
                float rel = (aim.x - area.center.x) / Mathf.Max(1f, area.width);
                if (side == 0 || Mathf.Abs(rel) > 0.12f) side = rel < 0 ? -1 : 1;
                if (side == blockedSide) side = -side;
            }
            Rect dock = DockRect();
            float x = Spot(area, half, reach, side);
            if (forcedSide == 0)
            {
                float other = Spot(area, half, reach, -side);
                if (Hidden(cam, x, y, half))
                {
                    // The bench would stand in front of him there: the other side, pressed against the screen edge if it
                    // is a little short of room (the set never hides him).
                    if (!Hidden(cam, Mathf.Clamp(other, EdgeMargin + half.x, Screen.width - EdgeMargin - half.x), y, half)) { blockedSide = side; side = -side; x = other; }
                }
                // No room on that side (a wide surface, a narrow screen): the other side, if he fits and shows there.
                else if (!Fits(x, half, dock) && Fits(other, half, dock) && !Hidden(cam, other, y, half)) { side = -side; x = other; }
            }
            x = Mathf.Clamp(x, EdgeMargin + half.x, Screen.width - EdgeMargin - half.x);

            // His transform sits off his figure's centre (centreOffset): put the centre on the spot.
            Vector3 spot = cam.ScreenToWorldPoint(new Vector3(x, y, depth)) - centreOffset;
            held.guideHome = spot;
            if (proxy && target) proxy.position = cam.ScreenToWorldPoint(new Vector3(aim.x, aim.y, depth));

            Draw(cam, area, aim);
        }

        // Something of the set nearer the lens than he is covers a fifth or more of his figure on screen at (x, y).
        bool Hidden(Camera cam, float x, float y, Vector2 half)
        {
            var him = new Rect(x - half.x, y - half.y, half.x * 2f, half.y * 2f);
            float area = him.width * him.height;
            if (area <= 0f) return false;
            Vector3 eye = cam.transform.position, ahead = cam.transform.forward;
            foreach (var r in occluders)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                var b = r.bounds;
                float near = float.MaxValue;
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    near = Mathf.Min(near, Vector3.Dot(corner - eye, ahead));
                    var p = (Vector2)cam.WorldToScreenPoint(corner);
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
                if (near <= 0.05f || near >= depth - 0.05f) continue;
                float w = Mathf.Min(him.xMax, max.x) - Mathf.Max(him.xMin, min.x), h = Mathf.Min(him.yMax, max.y) - Mathf.Max(him.yMin, min.y);
                if (w > 0f && h > 0f && w * h >= 0.2f * area) return true;
            }
            return false;
        }

        // The screen x of his centre on `towards`'s side of the surface: body, arm and a gap clear of its edge.
        static float Spot(Rect area, Vector2 half, float reach, int towards) =>
            towards < 0 ? area.xMin - Gap - reach - half.x : area.xMax + Gap + reach + half.x;

        // Inside the screen, and clear of the dock while it shows (it may stand between the surface and the right edge).
        static bool Fits(float x, Vector2 half, Rect dock)
        {
            if (x - half.x < EdgeMargin || x + half.x > Screen.width - EdgeMargin) return false;
            return dock.width <= 0f || x + half.x < dock.xMin - Gap * 0.5f || x - half.x > dock.xMax;
        }

        float Scale => canvas ? canvas.scaleFactor : 1f;

        // The dock while it shows a card (alpha over 0.5 and something in its column); empty otherwise.
        static Rect DockRect()
        {
            var panel = LessonPanel.Instance;
            if (!panel || !panel.Dock || !panel.Dock.gameObject.activeInHierarchy) return Rect.zero;
            if (panel.Dock.TryGetComponent(out CanvasGroup group) && group.alpha <= 0.5f) return Rect.zero;
            bool card = false;
            if (panel.Column) foreach (Transform block in panel.Column) if (block.gameObject.activeSelf) { card = true; break; }
            return card ? ScreenRect(panel.Dock) : Rect.zero;
        }

        // The top of the caption bar when a line is on it (screen px); the bottom inset otherwise.
        float CaptionTop()
        {
            var panel = LessonPanel.Instance;
            if (panel && panel.Caption && panel.Caption.gameObject.activeInHierarchy) return ScreenRect(panel.Caption).yMax;
            return LessonPanel.BottomInset * Scale;
        }

        static Rect ScreenRect(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]), max = min;
            for (int i = 1; i < 4; i++)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static Vector2 ScreenPoint(RectTransform rect, Vector2 normalized)
        {
            var r = rect.rect;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(r.xMin + r.width * normalized.x, r.yMin + r.height * normalized.y, 0f)));
        }

        // ------------------------------------------------------------------ the beam and the marks

        void Draw(Camera cam, Rect area, Vector2 aim)
        {
            if (!layer) return;
            Above(beside);
            shown = Mathf.MoveTowards(shown, target ? 1f : 0f, Time.unscaledDeltaTime / 0.2f);
            Show(shown);
            if (!target || shown <= 0f) return;

            // Beam: fingertip to the surface's edge facing him, at the element's height (clamped inside the edge). Hidden
            // while the fingertip is off screen or already over the surface (mid-flight round it).
            Vector3 tipScreen = cam.WorldToScreenPoint(held.PointerTip);
            Vector2 from = tipScreen;
            Vector2 to = new Vector2(side < 0 ? area.xMin : area.xMax, Mathf.Clamp(aim.y, area.yMin + 8f, area.yMax - 8f));
            bool beamOn = tipScreen.z > 0 && !area.Contains(from) && (side < 0 ? from.x < to.x : from.x > to.x);
            beam.gameObject.SetActive(beamOn);
            if (beamOn) Line(beam, Local(from), Local(to));

            // The mark on the element: its kind, then its shape from the element's screen rect.
            Rect element = ScreenRect(target);
            Mark kind = mark;
            if (kind == Mark.Auto) kind = Mathf.Max(element.width, element.height) < 90f * Scale ? Mark.Ring : Mark.Bracket;
            ring.gameObject.SetActive(kind == Mark.Ring);
            bracket.gameObject.SetActive(kind == Mark.Bracket);
            underline.gameObject.SetActive(kind == Mark.Underline);
            float s = Scale;
            if (kind == Mark.Ring)
            {
                ring.anchoredPosition = Local(element.center);
                ring.sizeDelta = Vector2.one * (Mathf.Max(element.width, element.height) / s + 22f);
            }
            else if (kind == Mark.Bracket)
            {
                float edge = side < 0 ? element.xMin - 8f * s : element.xMax + 8f * s;
                bracket.anchoredPosition = Local(new Vector2(edge, element.center.y));
                bracket.sizeDelta = new Vector2(14f, element.height / s + 8f);
                // The arms reach onto the row from the facing side.
                for (int i = 1; i < 3; i++)
                {
                    var arm = (RectTransform)bracketImages[i].transform;
                    arm.pivot = new Vector2(side < 0 ? 0f : 1f, arm.pivot.y);
                    arm.anchorMin = arm.anchorMax = new Vector2(0.5f, i == 1 ? 1f : 0f);
                    arm.anchoredPosition = Vector2.zero;
                }
            }
            else if (kind == Mark.Underline)
            {
                underline.anchoredPosition = Local(new Vector2(element.center.x, element.yMin - 4f * s));
                underline.sizeDelta = new Vector2(element.width / s, BeamWidth);
            }
        }

        // The layer draws right over the surface it marks (outlines only, never a fill), and under anything opened later.
        void Above(RectTransform surface)
        {
            var parent = layer.parent;
            Transform top = surface;
            while (top && top.parent != parent) top = top.parent;
            if (!top || top == layer) return;
            int index = top.GetSiblingIndex(), now = layer.GetSiblingIndex();
            if (now != index + 1) layer.SetSiblingIndex(now < index ? index : index + 1);
        }

        void Show(float alpha)
        {
            if (!layer) return;
            Color c = UI.Alpha(LessonPanel.Spark, 0.85f * alpha);
            beamImage.color = UI.Alpha(LessonPanel.Spark, 0.6f * alpha);
            ringImage.color = underlineImage.color = c;
            foreach (var image in bracketImages) image.color = c;
            bool on = alpha > 0f;
            if (!on) { beam.gameObject.SetActive(false); ring.gameObject.SetActive(false); bracket.gameObject.SetActive(false); underline.gameObject.SetActive(false); }
        }

        // Screen px -> the layer's local px.
        Vector2 Local(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out var local);
            return local - layer.rect.min; // the layer's children anchor at its bottom-left corner
        }

        static void Line(RectTransform line, Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            line.anchoredPosition = from;
            line.sizeDelta = new Vector2(d.magnitude, BeamWidth);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
    }
}
