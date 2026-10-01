using System.Linq;
using UnityEngine;

namespace OhmsLaw
{
    // Georg Ohm's poses, built procedurally because his model ships without animation clips.
    //   Work     Scenes 4 and 7: leaning over the desk, writing, checking the galvanometer, adjusting the wire (a loop)
    //   Present  Scene 8: upright, both arms open low toward the apparatus, palms up, face to the camera
    //   Examine  Scene 7: a copper wire held in both hands at chest height, eyes on it
    // A mode change blends over ~1/3 s from a snapshot of the pose on screen; Freeze() holds whatever is on screen.
    // The pose is written in Awake too, so the model's T-pose never reaches a frame. The galvanometer needle
    // (optional) takes a new deflection with every reading and a full swing on Swing(), which showing the
    // "[VFX] galvanometer needle swing" object triggers.
    public class OhmWorker : MonoBehaviour
    {
        public enum Mode { Work, Present, Examine }

        [Tooltip("Wrist targets of the work loop. Each hand serves the props on its own side of him, so the hand nearest the notebook writes.")]
        public Transform notebook, galvanometer, wirePost;
        [Tooltip("Forward bend of the torso while working, degrees. With this model's 0.33 m arms, 22 reaches 0.3 m in front of his feet at desk height and 36 reaches 0.4 m.")]
        public float lean = 22f;
        public float writeSeconds = 5.5f, checkSeconds = 3.5f;

        public Mode mode = Mode.Work;
        [Tooltip("Stops the clock, the goals and the blend so he holds his current pose (the 'ohm.freeze' cue). Disabling the component would leave the T-pose instead.")]
        public bool frozen;
        [Tooltip("Mode blend rate: 3 = a third of a second.")] public float blendSpeed = 3f;
        [Tooltip("Present: what he looks at. The main camera when empty.")] public Transform lookTarget;
        [Tooltip("Examine: the copper wire he holds, its local X along the wire. Placed between his hands while examining, hidden otherwise.")]
        public Transform handWire;

        [Tooltip("Pivot of the galvanometer needle, rotated about its local Y. Optional.")] public Transform needle;
        [Tooltip("Full deflection in degrees. Readings alternate sides so every swing is visible.")] public float needleSwing = 35f;
        [Tooltip("The '[VFX] galvanometer needle swing' object: a full swing each time a cue shows it, repeated every swingRepeat seconds while it stays shown. Found by name in the SceneDirector's refs when empty.")]
        public GameObject swingCue;
        public string swingCueName = "[VFX] galvanometer needle swing";
        public float swingRepeat = 3f;

        Transform spine, chest, head, rightArm, rightForeArm, rightHand, leftArm, leftForeArm, leftHand, cam;
        Transform[] bones;
        Quaternion[] rest, from, pose;   // bind pose, the snapshot a blend starts from, what is on screen
        Vector3 rightPalm, leftPalm;     // palm normals in hand space, from the bind pose
        Mode shown, fromMode;            // the mode being shown (blended in) and the one before it
        float blend = 1f, clock;
        int writeSide;                   // +1 the right hand writes, -1 the left
        Vector3 rightGoal, leftGoal, lookGoal;
        int adjustedCycle = -1;
        float needleAngle, needleTarget, needleVelocity, cueSwingAt;
        bool cueOn;

        // Readings per cycle as fractions of the full swing, alternating sides; the loop runs through them in order.
        static readonly float[] Deflections = { 0.85f, -0.5f, 1f, -0.7f, 0.4f, -0.9f };

        void Awake()
        {
            spine = Find("Spine"); chest = Find("Spine2"); head = Find("Head");
            rightArm = Find("RightArm"); rightForeArm = Find("RightForeArm"); rightHand = Find("RightHand");
            leftArm = Find("LeftArm"); leftForeArm = Find("LeftForeArm"); leftHand = Find("LeftHand");
            bones = new[] { spine, chest, head, rightArm, rightForeArm, rightHand, leftArm, leftForeArm, leftHand };
            rest = bones.Select(b => b.localRotation).ToArray();
            from = (Quaternion[])rest.Clone();
            pose = (Quaternion[])rest.Clone();
            // The bind pose is a T-pose with the palms down.
            rightPalm = rightHand.InverseTransformDirection(Vector3.down);
            leftPalm = leftHand.InverseTransformDirection(Vector3.down);
            writeSide = notebook ? Side(notebook) : 1;
            rightGoal = Rest(1);
            leftGoal = Rest(-1);
            lookGoal = P(notebook);
            needleAngle = needleTarget = needleSwing * 0.6f; // mid-experiment: the needle already shows a reading
            shown = fromMode = mode;
            Pose(0f); // the first rendered frame is already posed
        }

        void Start()
        {
            if (Camera.main) cam = Camera.main.transform;
            if (swingCue || string.IsNullOrEmpty(swingCueName)) return;
            var director = FindFirstObjectByType<SceneDirector>(FindObjectsInactive.Include);
            if (director) swingCue = director.refs.FirstOrDefault(r => r && r.name == swingCueName);
        }

        Transform Find(string bone)
        {
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name.EndsWith(":" + bone) || t.name == bone) return t;
            Debug.LogError("OhmWorker: bone not found: " + bone, this);
            return transform;
        }

        void LateUpdate() => Pose(frozen ? 0f : Time.deltaTime);

        // Holds the pose on screen (the 'ohm.freeze' cue); Freeze(false) lets the clock and the blend run again.
        public void Freeze(bool hold = true) => frozen = hold;

        // Blends to a new pose; also releases a freeze, which would otherwise hold the blend at its start.
        public void SetMode(Mode next) { mode = next; frozen = false; }

        // A full sweep to the far side of the scale that overshoots and rings down, like a real galvanometer.
        public void Swing()
        {
            needleTarget = -Mathf.Sign(needleTarget == 0 ? 1 : needleTarget) * needleSwing;
            needleVelocity = (needleTarget - needleAngle) * 3f;
        }

        // ------------------------------------------------------------------ the frame

        void Pose(float dt)
        {
            clock += dt;
            if (mode != shown)
            {
                for (int i = 0; i < bones.Length; i++) from[i] = pose[i];
                fromMode = shown;
                shown = mode;
                blend = 0f;
            }
            blend = Mathf.Min(1f, blend + dt * blendSpeed);

            for (int i = 0; i < bones.Length; i++) bones[i].localRotation = rest[i];
            switch (shown)
            {
                case Mode.Work: Work(dt); break;
                case Mode.Present: Present(); break;
                default: Examine(); break;
            }

            float w = Mathf.SmoothStep(0f, 1f, blend);
            for (int i = 0; i < bones.Length; i++)
            {
                pose[i] = w >= 1f ? bones[i].localRotation : Quaternion.Slerp(from[i], bones[i].localRotation, w);
                bones[i].localRotation = pose[i];
            }
            HoldWire();
            Needle(dt);
            SwingCue();
        }

        // ------------------------------------------------------------------ Work

        void Work(float dt)
        {
            float period = writeSeconds + checkSeconds;
            float cycle = clock % period;
            bool writing = cycle < writeSeconds;
            bool adjusting = !writing && cycle - writeSeconds >= checkSeconds * 0.5f;
            Transform active = writing ? notebook : adjusting ? wirePost : galvanometer;
            rightGoal = Vector3.Lerp(rightGoal, Goal(1, active), Ease(3f, dt));
            leftGoal = Vector3.Lerp(leftGoal, Goal(-1, active), Ease(3f, dt));
            lookGoal = Vector3.Lerp(lookGoal, P(active), Ease(2.5f, dt));

            // Lean over the desk, with a slow breath.
            float breath = Mathf.Sin(clock * 1.4f) * 1.2f;
            spine.Rotate(transform.right, lean * 0.5f + breath, Space.World);
            chest.Rotate(transform.right, lean * 0.5f, Space.World);
            Look(lookGoal, 0.5f, lean); // half-way keeps his face readable

            // The targets are wrist positions; the writing hand scribbles small loops on the page.
            Vector3 scribble = writing ? transform.right * (Mathf.Sin(clock * 9f) * 0.015f) + transform.forward * (Mathf.Sin(clock * 13f) * 0.008f) : Vector3.zero;
            Vector3 elbowOut = -transform.forward * 0.3f - transform.up * 0.15f;
            Arm(1, rightGoal + (writeSide > 0 ? scribble : Vector3.zero), rightArm.position + transform.right * 0.35f + elbowOut);
            Arm(-1, leftGoal + (writeSide < 0 ? scribble : Vector3.zero), leftArm.position - transform.right * 0.35f + elbowOut);

            // A new deflection each time he touches the wire; it settles while he writes.
            int cycleIndex = Mathf.FloorToInt(clock / period);
            if (adjusting && cycleIndex != adjustedCycle)
            {
                adjustedCycle = cycleIndex;
                needleTarget = needleSwing * Deflections[cycleIndex % Deflections.Length];
                needleVelocity += (needleTarget - needleAngle) * 1.5f;
            }
        }

        // The hand on the active prop's side reaches for it; the other waits just short of its own prop.
        Vector3 Goal(int side, Transform active) => active && Side(active) == side ? P(active) : Rest(side);

        // Where a free hand waits: pulled back from the prop it serves toward him and down to the notebook's height (the
        // desk); the writing hand a little higher, pen lifted off the page.
        Vector3 Rest(int side)
        {
            Transform prop = side == writeSide ? notebook : Side(wirePost) == side ? wirePost : Side(galvanometer) == side ? galvanometer : notebook;
            Vector3 at = P(prop), back = Vector3.ProjectOnPlane(transform.position - at, Vector3.up).normalized;
            at += back * 0.08f;
            at.y = Mathf.Min(at.y, P(notebook).y) + (side == writeSide ? 0.03f : 0f);
            return at;
        }

        int Side(Transform t) => t && Vector3.Dot(t.position - transform.position, transform.right) < 0 ? -1 : 1;

        // A missing target becomes a point on the desk in front of him instead of a crash.
        Vector3 P(Transform t) => t ? t.position : transform.position + transform.forward * 0.4f + Vector3.up * 0.9f;

        // ------------------------------------------------------------------ Present

        void Present()
        {
            float breath = Mathf.Sin(clock * 1.1f);
            spine.Rotate(transform.right, 2f + breath * 1.2f, Space.World);
            chest.Rotate(transform.right, 2f, Space.World);
            if (!cam && Camera.main) cam = Camera.main.transform;
            Vector3 eye = lookTarget ? lookTarget.position : cam ? cam.position : head.position + transform.forward;
            Look(eye, 0.85f, 4f);

            // Both arms open low toward the apparatus (94 % of his reach, so the elbows stay soft), elbows out and back,
            // palms turned up; the breath lifts the hands.
            Vector3 hands = transform.position + Vector3.up * (0.98f + breath * 0.01f) + transform.forward * 0.17f;
            foreach (int side in Sides)
            {
                Transform upper = side > 0 ? rightArm : leftArm;
                Arm(side, hands + transform.right * (side * 0.32f), upper.position + transform.right * (side * 0.3f) - transform.forward * 0.2f - Vector3.up * 0.25f);
                Palm(side, Vector3.up);
            }
        }

        // ------------------------------------------------------------------ Examine

        void Examine()
        {
            float breath = Mathf.Sin(clock * 1.2f);
            spine.Rotate(transform.right, 6f + breath, Space.World);
            chest.Rotate(transform.right, 6f, Space.World);
            // The wire sits at chest height a forearm's length in front of him, a hand's width either side of centre.
            Vector3 centre = transform.position + Vector3.up * (1.02f + breath * 0.005f) + transform.forward * 0.26f;
            Look(centre, 0.8f, 12f);
            foreach (int side in Sides)
            {
                Transform upper = side > 0 ? rightArm : leftArm;
                Arm(side, centre + transform.right * (side * 0.17f), upper.position + transform.right * (side * 0.15f) - transform.forward * 0.05f - Vector3.up * 0.3f);
                Palm(side, Vector3.up);
            }
        }

        static readonly int[] Sides = { 1, -1 };

        // The held wire follows the hands; it appears half-way into the Examine blend and leaves half-way out.
        void HoldWire()
        {
            if (!handWire) return;
            bool held = shown == Mode.Examine ? blend > 0.5f : fromMode == Mode.Examine && blend < 0.5f;
            if (handWire.gameObject.activeSelf != held) handWire.gameObject.SetActive(held);
            if (!held) return;
            Vector3 a = Grip(rightForeArm, rightHand), b = Grip(leftForeArm, leftHand);
            handWire.position = (a + b) * 0.5f;
            handWire.right = (b - a).normalized;
        }

        // The palm, a little past the wrist along the forearm.
        static Vector3 Grip(Transform lower, Transform hand) => hand.position + (hand.position - lower.position).normalized * 0.07f;

        // ------------------------------------------------------------------ body parts

        // Turns the head part of the way from where the leaning body faces toward the target.
        void Look(Vector3 target, float amount, float bodyLean)
        {
            Vector3 facing = Quaternion.AngleAxis(bodyLean, transform.right) * transform.forward;
            var aim = Quaternion.FromToRotation(facing, (target - head.position).normalized);
            head.rotation = Quaternion.Slerp(Quaternion.identity, aim, amount) * head.rotation;
        }

        void Arm(int side, Vector3 target, Vector3 elbowHint)
        {
            if (side > 0) Reach(rightArm, rightForeArm, rightHand, target, elbowHint);
            else Reach(leftArm, leftForeArm, leftHand, target, elbowHint);
        }

        // Turns the palm toward `normal` about the forearm's axis: most of it at the forearm (supination), the rest at
        // the wrist, so neither twists grotesquely.
        void Palm(int side, Vector3 normal)
        {
            Transform lower = side > 0 ? rightForeArm : leftForeArm, hand = side > 0 ? rightHand : leftHand;
            Vector3 axis = (hand.position - lower.position).normalized;
            Vector3 now = Vector3.ProjectOnPlane(hand.TransformDirection(side > 0 ? rightPalm : leftPalm), axis);
            Vector3 want = Vector3.ProjectOnPlane(normal, axis);
            if (now.sqrMagnitude < 1e-4f || want.sqrMagnitude < 1e-4f) return;
            float angle = Vector3.SignedAngle(now, want, axis);
            lower.Rotate(axis, angle * 0.6f, Space.World);
            hand.Rotate(axis, angle * 0.4f, Space.World);
        }

        // Two-bone IK in world space, so it does not depend on how the rig's bone axes are oriented.
        static void Reach(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 elbowHint)
        {
            float upperLength = Vector3.Distance(upper.position, lower.position);
            float lowerLength = Vector3.Distance(lower.position, hand.position);
            Vector3 toTarget = target - upper.position;
            float distance = Mathf.Clamp(toTarget.magnitude, 0.01f, upperLength + lowerLength - 0.001f);
            Vector3 direction = toTarget.normalized;

            float cos = (upperLength * upperLength + distance * distance - lowerLength * lowerLength) / (2 * upperLength * distance);
            float angle = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 bendAxis = Vector3.Cross(direction, elbowHint - upper.position).normalized;
            Vector3 elbow = upper.position + Quaternion.AngleAxis(angle, bendAxis) * direction * upperLength;

            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, upper.position + direction * distance - lower.position) * lower.rotation;
        }

        static float Ease(float rate, float dt) => 1 - Mathf.Exp(-rate * dt);

        // ------------------------------------------------------------------ needle

        // A lightly damped spring: every new reading overshoots and settles, Swing() rings across the whole scale.
        void Needle(float dt)
        {
            if (!needle) return;
            if (dt > 0)
            {
                needleVelocity += (needleTarget - needleAngle) * 40f * dt;
                needleVelocity *= Mathf.Exp(-2.5f * dt);
                needleAngle += needleVelocity * dt;
            }
            needle.localRotation = Quaternion.Euler(0, needleAngle, 0);
        }

        // Showing the "[VFX] galvanometer needle swing" object swings the needle, again every swingRepeat seconds.
        void SwingCue()
        {
            bool on = swingCue && swingCue.activeInHierarchy;
            if (on && (!cueOn || clock >= cueSwingAt))
            {
                Swing();
                cueSwingAt = clock + swingRepeat;
            }
            cueOn = on;
        }
    }
}
