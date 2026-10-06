using System.Linq;
using UnityEngine;

namespace OhmsLaw
{
    // Georg Ohm's poses, built procedurally because his model ships without animation clips.
    //   Work     Scenes 4 and 7: leaning over the desk, writing, checking the galvanometer, adjusting the wire (a loop)
    //   Present  Scene 8: upright, both arms open low toward the apparatus, palms up, face to the camera
    //   Examine  Scene 7: a copper wire held in both hands at chest height, eyes on it
    // A mode change blends over ~1/3 s from a snapshot of the pose on screen; Freeze() holds whatever is on screen.
    // The pose is written in Awake too, so the model's T-pose never reaches a frame. Besides the spine, head and arms, the
    // neck shares the head turn, the collarbones follow each reach a little and the fingers always keep a curl (a pen
    // grip while writing, open palms presenting, a grip on the wire examining), so no pose has stiff splayed hands.
    // The hands never sink into the desk: in every mode the lowest point of a hand (wrist, finger joints, finger tips) is
    // kept contactClearance above deskTop by raising the wrist target, a hand changing props lifts over in an arc instead
    // of dragging across the desk, and the palms lie on the page while he works. Attend(someone) turns his head part-way
    // toward them in every mode (Wattson while he speaks); Attend() gives his eyes back to the work.
    // The galvanometer needle (optional) takes a new deflection with every reading and a full swing on Swing(), which
    // showing the "[VFX] galvanometer needle swing" object triggers.
    public class OhmWorker : MonoBehaviour
    {
        public enum Mode { Work, Present, Examine }

        [Tooltip("Wrist targets of the work loop. Each hand serves the props on its own side of him, so the hand nearest the notebook writes.")]
        public Transform notebook, galvanometer, wirePost;
        [Tooltip("World height of the desk top his hands work over. NaN (the default) infers it in Awake from the notebook target, which the builders place 2 cm above the desk.")]
        public float deskTop = float.NaN;
        [Tooltip("Metres kept between the lowest point of a hand (wrist, finger joints, finger tips) and the desk top.")]
        public float contactClearance = 0.012f;
        [Tooltip("Forward bend of the torso while working, degrees. With this model's 0.33 m arms, 22 reaches 0.3 m in front of his feet at desk height and 36 reaches 0.4 m.")]
        public float lean = 22f;
        public float writeSeconds = 5.5f, checkSeconds = 3.5f;

        public Mode mode = Mode.Work;
        [Tooltip("Stops the clock, the goals and the blend so he holds his current pose (the 'ohm.freeze' cue). Disabling the component would leave the T-pose instead.")]
        public bool frozen;
        [Tooltip("Mode blend rate: 3 = a third of a second.")] public float blendSpeed = 3f;
        [Tooltip("Present: what he looks at. The main camera when empty.")] public Transform lookTarget;
        [Tooltip("Someone to glance at (Wattson while he speaks); the head turns part-way toward it in every mode.")] public Transform attention;
        [Range(0, 1)] public float attentionWeight = 0.65f;
        [Tooltip("Examine: the copper wire he holds, its local X along the wire. Placed between his hands while examining, hidden otherwise.")]
        public Transform handWire;

        [Tooltip("Pivot of the galvanometer needle, rotated about its local Y. Optional.")] public Transform needle;
        [Tooltip("Full deflection in degrees. Readings alternate sides so every swing is visible.")] public float needleSwing = 35f;
        [Tooltip("The '[VFX] galvanometer needle swing' object: a full swing each time a cue shows it, repeated every swingRepeat seconds while it stays shown. Found by name in the SceneDirector's refs when empty.")]
        public GameObject swingCue;
        public string swingCueName = "[VFX] galvanometer needle swing";
        public float swingRepeat = 3f;

        Transform spine, chest, neck, head, rightClav, leftClav, rightArm, rightForeArm, rightHand, leftArm, leftForeArm, leftHand, cam;
        Transform[][] rightFingers, leftFingers; // [finger][joint], thumb first
        Vector3[] rightCurl, leftCurl;           // per finger, hand-local axis that curls it toward the palm
        Transform[] bones;
        Quaternion[] rest, from, pose;   // bind pose, the snapshot a blend starts from, what is on screen
        Vector3 rightPalm, leftPalm;     // palm normals in hand space, from the bind pose
        Mode shown, fromMode;            // the mode being shown (blended in) and the one before it
        float blend = 1f, clock;
        int writeSide;                   // +1 the right hand writes, -1 the left
        Vector3 rightGoal, leftGoal, lookGoal;
        Vector3 rightWant, leftWant;     // where each hand's current move ends: a new destination starts a new move
        float rightTotal, leftTotal;     // the length of each hand's current move, for the lift-over arc
        float rightLift, leftLift;       // the eased raise of each wrist target that keeps the hand off the desk
        Vector3 attendAt;                // where his attention last was (the glance eases out from there)
        float attend;                    // how much of the look the attention takes right now, eased toward attentionWeight
        int adjustedCycle = -1;
        float needleAngle, needleTarget, needleVelocity, cueSwingAt;
        bool cueOn;

        // Readings per cycle as fractions of the full swing, alternating sides; the loop runs through them in order.
        static readonly float[] Deflections = { 0.85f, -0.5f, 1f, -0.7f, 0.4f, -0.9f };
        // The height of the arc a hand rises in when it changes props (less for a move shorter than this).
        const float LiftOver = 0.07f;

        void Awake() => Init();

        // Finds the rig and writes the first pose. Runs once: Simulate() calls it too, for the pose test outside play mode.
        void Init()
        {
            if (bones != null) return;
            spine = Find("Spine"); chest = Find("Spine2"); head = Find("Head"); neck = Find("Neck", false);
            rightClav = Find("RightShoulder", false); leftClav = Find("LeftShoulder", false);
            rightArm = Find("RightArm"); rightForeArm = Find("RightForeArm"); rightHand = Find("RightHand");
            leftArm = Find("LeftArm"); leftForeArm = Find("LeftForeArm"); leftHand = Find("LeftHand");
            // The bind pose is a T-pose with the palms down.
            rightPalm = rightHand.InverseTransformDirection(Vector3.down);
            leftPalm = leftHand.InverseTransformDirection(Vector3.down);
            rightFingers = Fingers("Right", rightHand, out rightCurl);
            leftFingers = Fingers("Left", leftHand, out leftCurl);
            var list = new System.Collections.Generic.List<Transform> { spine, chest, head, rightArm, rightForeArm, rightHand, leftArm, leftForeArm, leftHand };
            foreach (var t in new[] { neck, rightClav, leftClav }) if (t) list.Add(t);
            foreach (var hand in new[] { rightFingers, leftFingers }) foreach (var finger in hand) list.AddRange(finger);
            bones = list.ToArray();
            rest = bones.Select(b => b.localRotation).ToArray();
            from = (Quaternion[])rest.Clone();
            pose = (Quaternion[])rest.Clone();
            if (float.IsNaN(deskTop)) deskTop = P(notebook).y - 0.02f; // the builders put the notebook target 2 cm above the desk
            writeSide = notebook ? Side(notebook) : 1;
            rightWant = rightGoal = Rest(1);
            leftWant = leftGoal = Rest(-1);
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

        Transform Find(string bone, bool required = true)
        {
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name.EndsWith(":" + bone) || t.name == bone) return t;
            if (!required) return null;
            Debug.LogError("OhmWorker: bone not found: " + bone, this);
            return transform;
        }

        // The three curling joints of each finger (Mixamo names; the 4th is an end marker), and the hand-local axis that
        // curls each finger toward the palm, measured in the bind pose (palms down).
        Transform[][] Fingers(string side, Transform hand, out Vector3[] curl)
        {
            var fingers = new System.Collections.Generic.List<Transform[]>();
            var axes = new System.Collections.Generic.List<Vector3>();
            foreach (var name in new[] { "Thumb", "Index", "Middle", "Ring", "Pinky" })
            {
                var joints = new[] { Find(side + "Hand" + name + "1", false), Find(side + "Hand" + name + "2", false), Find(side + "Hand" + name + "3", false) };
                if (joints.Any(j => !j)) continue;
                fingers.Add(joints);
                Vector3 along = joints[2].position - joints[0].position;
                axes.Add(hand.InverseTransformDirection(Vector3.Cross(along, Vector3.down).normalized));
            }
            curl = axes.ToArray();
            return fingers.ToArray();
        }

        void LateUpdate() => Pose(frozen ? 0f : Time.deltaTime);

        // One frame of the pose from outside play mode (the pose test), exactly as LateUpdate would write it; initialises
        // first when Awake has not run.
        public void Simulate(float dt) { Init(); Pose(frozen ? 0f : dt); }

        // Holds the pose on screen (the 'ohm.freeze' cue); Freeze(false) lets the clock and the blend run again.
        public void Freeze(bool hold = true) => frozen = hold;

        // Blends to a new pose; also releases a freeze, which would otherwise hold the blend at its start.
        public void SetMode(Mode next) { mode = next; frozen = false; }

        // His attention: Attend(wattson) while Wattson speaks turns his head part-way toward him in every mode (eased in
        // over ~1/4 s, so the glance never snaps); Attend() gives his eyes back to the work.
        public void Attend(Transform target) { attention = target; }
        public void Attend() => attention = null;

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
                case Mode.Present: Present(dt); break;
                default: Examine(dt); break;
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
            float rightArc = Travel(ref rightGoal, ref rightWant, ref rightTotal, Goal(1, active), dt);
            float leftArc = Travel(ref leftGoal, ref leftWant, ref leftTotal, Goal(-1, active), dt);
            lookGoal = Vector3.Lerp(lookGoal, P(active), Ease(2.5f, dt));

            // Lean over the desk, with a slow breath.
            float breath = Mathf.Sin(clock * 1.4f) * 1.2f;
            spine.Rotate(transform.right, lean * 0.5f + breath, Space.World);
            chest.Rotate(transform.right, lean * 0.5f, Space.World);
            // Head part-way to the work and a little toward the camera, so his face stays readable over the desk.
            if (!cam && Camera.main) cam = Camera.main.transform;
            Look(Glance(cam ? Vector3.Lerp(lookGoal, cam.position, 0.25f) : lookGoal, dt), 0.6f, lean);

            // The targets are wrist positions; the writing hand scribbles small loops on the page. The arcs lift a hand that
            // is changing props, the palms lie on the desk, and Hand() keeps every finger off its top.
            Vector3 scribble = writing ? transform.right * (Mathf.Sin(clock * 9f) * 0.015f) + transform.forward * (Mathf.Sin(clock * 13f) * 0.008f) : Vector3.zero;
            Vector3 elbowOut = -transform.forward * 0.3f - transform.up * 0.15f;
            Hand(1, rightGoal + Vector3.up * rightArc + (writeSide > 0 ? scribble : Vector3.zero), rightArm.position + transform.right * 0.35f + elbowOut, Vector3.down, writing && writeSide > 0 ? 0.6f : 0.35f, dt);
            Hand(-1, leftGoal + Vector3.up * leftArc + (writeSide < 0 ? scribble : Vector3.zero), leftArm.position - transform.right * 0.35f + elbowOut, Vector3.down, writing && writeSide < 0 ? 0.6f : 0.35f, dt);

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

        // Eases a hand's goal toward `next`. A destination more than 5 mm from the last starts a new move, and the height
        // returned lifts the hand in an arc over the first 60 % of it (a sine up to LiftOver, never taller than the move
        // is long), so it rises off one prop before it travels across the desk to the next; the last 40 % settles level.
        float Travel(ref Vector3 goal, ref Vector3 want, ref float total, Vector3 next, float dt)
        {
            if ((next - want).sqrMagnitude > 2.5e-5f) total = Vector3.Distance(goal, next);
            want = next;
            goal = Vector3.Lerp(goal, want, Ease(3f, dt));
            if (total < 1e-4f) return 0f;
            float progress = Mathf.Clamp01(1f - Vector3.Distance(goal, want) / total);
            return progress < 0.6f ? Mathf.Sin(progress / 0.6f * Mathf.PI) * Mathf.Min(LiftOver, total) : 0f;
        }

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

        void Present(float dt)
        {
            float breath = Mathf.Sin(clock * 1.1f);
            spine.Rotate(transform.right, 2f + breath * 1.2f, Space.World);
            chest.Rotate(transform.right, 2f, Space.World);
            if (!cam && Camera.main) cam = Camera.main.transform;
            Vector3 eye = lookTarget ? lookTarget.position : cam ? cam.position : head.position + transform.forward;
            Look(Glance(eye, dt), 0.85f, 4f);

            // Both arms open low toward the apparatus (94 % of his reach, so the elbows stay soft), elbows out and back,
            // palms turned up; the breath lifts the hands.
            Vector3 hands = transform.position + Vector3.up * (0.98f + breath * 0.01f) + transform.forward * 0.17f;
            foreach (int side in Sides)
            {
                Transform upper = side > 0 ? rightArm : leftArm;
                Hand(side, hands + transform.right * (side * 0.32f), upper.position + transform.right * (side * 0.3f) - transform.forward * 0.2f - Vector3.up * 0.25f, Vector3.up, 0.18f + breath * 0.03f, dt);
            }
        }

        // ------------------------------------------------------------------ Examine

        void Examine(float dt)
        {
            float breath = Mathf.Sin(clock * 1.2f);
            spine.Rotate(transform.right, 6f + breath, Space.World);
            chest.Rotate(transform.right, 6f, Space.World);
            // The wire sits at chest height a forearm's length in front of him, a hand's width either side of centre.
            Vector3 centre = transform.position + Vector3.up * (1.02f + breath * 0.005f) + transform.forward * 0.26f;
            Look(Glance(centre, dt), 0.8f, 12f);
            foreach (int side in Sides)
            {
                Transform upper = side > 0 ? rightArm : leftArm;
                Hand(side, centre + transform.right * (side * 0.17f), upper.position + transform.right * (side * 0.15f) - transform.forward * 0.05f - Vector3.up * 0.3f, Vector3.up, 0.7f, dt);
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
            if (neck)
            {
                // The neck takes 40 % of the turn, the head the rest (both about the same axis, so the parts add up).
                neck.rotation = Quaternion.Slerp(Quaternion.identity, aim, amount * 0.4f) * neck.rotation;
                head.rotation = Quaternion.Slerp(Quaternion.identity, aim, amount * 0.6f) * head.rotation;
            }
            else head.rotation = Quaternion.Slerp(Quaternion.identity, aim, amount) * head.rotation;
        }

        // Blends a look target toward whoever has his attention, by attentionWeight once the glance has eased in (~1/4 s);
        // it eases out the same way, from where they last were, when Attend() clears it.
        Vector3 Glance(Vector3 at, float dt)
        {
            if (attention) attendAt = attention.position;
            attend = Mathf.Lerp(attend, attention ? attentionWeight : 0f, Ease(4f, dt));
            return attend > 1e-3f ? Vector3.Lerp(at, attendAt, attend) : at;
        }

        void Arm(int side, Vector3 target, Vector3 elbowHint)
        {
            // The collarbone turns 15 % of the way toward the hand's goal (at most 12 degrees): the shoulder leads the
            // reach instead of the arm swinging from a fixed socket.
            Transform clav = side > 0 ? rightClav : leftClav, upper = side > 0 ? rightArm : leftArm;
            if (clav)
            {
                Vector3 bone = upper.position - clav.position, want = target - clav.position;
                float angle = Mathf.Min(Vector3.Angle(bone, want) * 0.15f, 12f);
                clav.rotation = Quaternion.FromToRotation(bone, Vector3.RotateTowards(bone, want, angle * Mathf.Deg2Rad, 0)) * clav.rotation;
            }
            Solve(side, target, elbowHint);
        }

        // The two-bone reach alone (no collarbone), which Hand() runs again when the desk contact raises the wrist target.
        void Solve(int side, Vector3 target, Vector3 elbowHint)
        {
            if (side > 0) Limb.Reach(rightArm, rightForeArm, rightHand, target, elbowHint);
            else Limb.Reach(leftArm, leftForeArm, leftHand, target, elbowHint);
        }

        // Places a hand: the arm reaches the wrist target, the palm turns to `palm`, the fingers curl, and the hand is kept
        // off the desk. The lowest of the wrist, every finger joint and every finger tip must clear deskTop by
        // contactClearance; while it does not, the wrist target is raised by the shortfall and the arm solved again. The
        // second pass scales the shortfall by how far the hand actually rose per metre of raise on the first (at full
        // stretch the arm only tilts up, so it rises less than the target), which lands it within a millimetre; a third
        // runs only if that one hit the per-pass cap. The raise is a per-side lift that eases toward the raise that would
        // put the lowest point exactly on the plane, so the hand comes off the desk, and the correction fades, smoothly
        // instead of popping; what the eased lift lacks on the way down, the passes make up within the frame.
        void Hand(int side, Vector3 target, Vector3 elbowHint, Vector3 palm, float curl, float dt)
        {
            float lift = side > 0 ? rightLift : leftLift;
            Arm(side, target + Vector3.up * lift, elbowHint);
            Palm(side, palm);
            Curl(side, curl);
            float plane = deskTop + contactClearance;
            if (float.IsNaN(plane)) return;
            float raised = lift, low = Lowest(side), gain = 1f;
            for (int i = 0; i < 3 && low < plane - 0.0005f; i++)
            {
                float step = Mathf.Min((plane - low) / gain, 0.5f);
                Solve(side, target + Vector3.up * (raised + step), elbowHint); // the curl is hand-relative, so it survives
                Palm(side, palm);
                float was = low;
                low = Lowest(side);
                gain = Mathf.Clamp((low - was) / step, 0.1f, 2f);
                raised += step;
            }
            float need = Mathf.Max(0f, raised + (plane - low) / gain);
            lift = Mathf.Lerp(lift, need, Ease(8f, dt));
            if (side > 0) rightLift = lift; else leftLift = lift;
        }

        // The lowest world point of a hand: the wrist, every finger joint and every finger tip.
        float Lowest(int side)
        {
            Transform hand = side > 0 ? rightHand : leftHand;
            var fingers = side > 0 ? rightFingers : leftFingers;
            float low = hand.position.y;
            for (int f = 0; f < fingers.Length; f++)
            {
                var joints = fingers[f];
                for (int j = 0; j < joints.Length; j++) low = Mathf.Min(low, joints[j].position.y);
                low = Mathf.Min(low, Tip(joints).y);
            }
            return low;
        }

        // A finger's tip: the chains end at the last knuckle, so its segment is carried on by its own length.
        static Vector3 Tip(Transform[] joints) => joints[joints.Length - 1].position * 2f - joints[joints.Length - 2].position;

        // Curls every finger toward the palm: 0 open, 1 a loose fist; the thumb follows at half strength.
        void Curl(int side, float amount)
        {
            Transform hand = side > 0 ? rightHand : leftHand;
            var fingers = side > 0 ? rightFingers : leftFingers;
            var axes = side > 0 ? rightCurl : leftCurl;
            for (int f = 0; f < fingers.Length; f++)
            {
                Vector3 axis = hand.TransformDirection(axes[f]);
                float c = amount * (f == 0 && fingers.Length == 5 ? 0.5f : 1f);
                for (int j = 0; j < 3; j++) fingers[f][j].rotation = Quaternion.AngleAxis(c * (j == 0 ? 50f : 65f), axis) * fingers[f][j].rotation;
            }
        }

        // Turns the palm toward `normal` about the forearm's axis: most of it at the forearm (supination), the rest at
        // the wrist, so neither twists grotesquely.
        void Palm(int side, Vector3 normal)
        {
            if (side > 0) Limb.Twist(rightForeArm, rightHand, rightPalm, normal, 1f, 180f);
            else Limb.Twist(leftForeArm, leftHand, leftPalm, normal, 1f, 180f);
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
