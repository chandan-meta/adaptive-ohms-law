using UnityEngine;

namespace OhmsLaw
{
    // Georg Ohm's silent work loop, built procedurally because his model ships without animation clips:
    // he leans over the desk, writes in his notebook, then checks the galvanometer and adjusts a wire.
    // The galvanometer needle (optional) takes a new deflection each time he adjusts the wire.
    public class OhmWorker : MonoBehaviour
    {
        public Transform notebook, galvanometer, wirePost;
        [Tooltip("Pivot of the galvanometer needle, rotated about its local Y. Optional.")] public Transform needle;
        public float lean = 22f, writeSeconds = 5.5f, checkSeconds = 3.5f, needleSwing = 38f;
        [Tooltip("Stops the work clock so he holds his current pose (the 'ohm.freeze' cue). Disabling the component would leave the T-pose instead.")]
        public bool frozen;

        Transform spine, chest, head, rightArm, rightForeArm, rightHand, leftArm, leftForeArm, leftHand;
        Transform[] bones;
        Quaternion[] rest;
        Vector3 leftGoal, lookGoal;
        float clock, needleAngle, needleTarget;
        int adjustedCycle = -1;

        // Readings per cycle, as fractions of the full swing; the loop runs through them in order.
        static readonly float[] Deflections = { 0.85f, 0.45f, 1f, 0.6f, 0.3f, 0.75f };

        void Awake()
        {
            spine = Find("Spine"); chest = Find("Spine2"); head = Find("Head");
            rightArm = Find("RightArm"); rightForeArm = Find("RightForeArm"); rightHand = Find("RightHand");
            leftArm = Find("LeftArm"); leftForeArm = Find("LeftForeArm"); leftHand = Find("LeftHand");
            bones = new[] { spine, chest, head, rightArm, rightForeArm, leftArm, leftForeArm };
            rest = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) rest[i] = bones[i].localRotation;
            leftGoal = galvanometer.position;
            lookGoal = notebook.position;
            needleAngle = needleTarget = needleSwing * 0.6f; // mid-experiment: the needle already shows a reading
        }

        Transform Find(string bone)
        {
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name.EndsWith(":" + bone) || t.name == bone) return t;
            Debug.LogError("OhmWorker: bone not found: " + bone, this);
            return transform;
        }

        void LateUpdate()
        {
            // Frozen: the clock stops and the goals stop moving, so the same pose is written every frame.
            float dt = frozen ? 0 : Time.deltaTime;
            clock += dt;
            for (int i = 0; i < bones.Length; i++) bones[i].localRotation = rest[i];

            float period = writeSeconds + checkSeconds;
            float cycle = clock % period;
            bool writing = cycle < writeSeconds;
            // While checking, the left hand alternates between the galvanometer and the wire post.
            bool adjusting = !writing && cycle - writeSeconds >= checkSeconds * 0.5f;
            Vector3 leftTarget = adjusting ? wirePost.position : galvanometer.position;
            leftGoal = Vector3.Lerp(leftGoal, leftTarget, 1 - Mathf.Exp(-3f * dt));
            lookGoal = Vector3.Lerp(lookGoal, writing ? notebook.position : leftGoal, 1 - Mathf.Exp(-2.5f * dt));

            // Lean over the desk, with a slow breath.
            float breath = Mathf.Sin(clock * 1.4f) * 1.2f;
            spine.Rotate(transform.right, lean * 0.5f + breath, Space.World);
            chest.Rotate(transform.right, lean * 0.5f, Space.World);

            // Turn the head most of the way from where the leaning body faces to what he is looking at.
            Vector3 facing = Quaternion.AngleAxis(lean, transform.right) * transform.forward;
            var aim = Quaternion.FromToRotation(facing, (lookGoal - head.position).normalized);
            head.rotation = Quaternion.Slerp(Quaternion.identity, aim, 0.5f) * head.rotation; // half-way keeps his face readable

            // The writing hand scribbles small loops on the page.
            Vector3 scribble = writing ? transform.right * (Mathf.Sin(clock * 9f) * 0.015f) + transform.forward * (Mathf.Sin(clock * 13f) * 0.008f) : Vector3.zero;
            Vector3 elbowOut = -transform.forward * 0.3f - transform.up * 0.15f;
            Reach(rightArm, rightForeArm, rightHand, notebook.position + scribble + transform.up * 0.03f, rightArm.position + transform.right * 0.35f + elbowOut);
            Reach(leftArm, leftForeArm, leftHand, leftGoal + transform.up * 0.04f, leftArm.position - transform.right * 0.35f + elbowOut);

            Needle(Mathf.FloorToInt(clock / period), adjusting, dt);
        }

        // A new deflection when he touches the wire, a quick overshoot that dies down, steady while he writes.
        void Needle(int cycleIndex, bool adjusting, float dt)
        {
            if (!needle) return;
            if (adjusting && cycleIndex != adjustedCycle)
            {
                adjustedCycle = cycleIndex;
                needleTarget = needleSwing * Deflections[cycleIndex % Deflections.Length];
            }
            needleAngle = Mathf.Lerp(needleAngle, needleTarget, 1 - Mathf.Exp(-(adjusting ? 6f : 2f) * dt));
            float quiver = Mathf.Sin(clock * 9f) * Mathf.Abs(needleTarget - needleAngle) * 0.35f;
            needle.localRotation = Quaternion.Euler(0, needleAngle + quiver, 0);
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
    }
}
