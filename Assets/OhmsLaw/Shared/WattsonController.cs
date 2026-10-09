using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

namespace OhmsLaw
{
    // Wattson's body, all procedural (his 52-joint FK rig ships without clips):
    //   flight     spring flight along a spline (Scene 4, as before) or, between markers, an eased move (a SmoothDamp lead
    //              the spring follows: eases in, leans and banks into the move, overshoots a touch and settles);
    //   life       breathing, bob, sway, weight shifts, bent dangling legs with secondary motion, blinks, eye darts;
    //   attention  the body turns toward what he points at (enough that the arm never folds across his chest), the
    //              head and eyes look at it, and back at the camera while he talks;
    //   gestures   each gesture is a hand goal per arm, solved with two-bone IK (shoulder, elbow, wrist) plus palm twist,
    //              wrist and finger curl; goals blend over ~0.2 s;
    //   talking    the mouth blendshape and glow follow his voice; nods and beat gestures ride on any gesture;
    //   reactions  React() layers a short correct / partial / incorrect reaction over the current gesture.
    // Joint ranges stay inside what his skin survives (measured in Blender, scratch/rig/evidence.json): head yaw <= 20,
    // pitch <= 12, chest <= 10, knees <= 55, elbows <= ~95 degrees. Eyes: no eyelid joints exist, so a blink squashes the
    // pupils (weighted to eye_left / eye_right) for 0.16 s; the eyeballs follow the head.
    public class WattsonController : MonoBehaviour
    {
        public enum Gesture { Idle, Greet, Point, Flick, Reach, RaiseGadget, Aim, Listening, Thinking, Talking, Transform } // append only: clips store the int
        // Transform: anticipation (he gathers, the gadget drawn back), the action (gadget thrust at the target)
        // TransformActionDelay seconds in, then a settle by TransformSettleSeconds into an aim he holds. Builders time the
        // transformation VFX cues to the action moment.
        public const float TransformActionDelay = 0.55f, TransformSettleSeconds = 1.1f;
        public enum Reaction { Correct, Partial, Incorrect }
        public const float ReactionSeconds = 1.4f;

        public static WattsonController Active { get; private set; }

        [Header("Scene")]
        public SceneAgentContext agentContext;
        public Transform lookTarget, pointTarget;
        public Gesture gesture;
        public bool gadgetOut;

        [Header("Flight")]
        public SplineContainer path;
        [Range(0, 1)] public float pathT;
        public float spring = 60f, damping = 9f, bobHeight = 0.12f, bobSpeed = 1.6f, bank = 14f;
        [Range(0.05f, 1f)] public float motionScale = 1f;
        [Tooltip("Marker moves: seconds of the eased lead the spring follows (ease in and out).")] public float moveTime = 0.22f;

        [Tooltip("Existing furniture meshes to clear during marker and Ask movements. Empty preserves introductory flight.")]
        public Renderer[] furniture = new Renderer[0];
        public Unity.Cinemachine.CinemachineCamera conversationCamera;
        float footClearance, bodyRadius, conversationFov;
        Unity.Cinemachine.CinemachineBrain conversationBrain;
        int conversationOverride = -1;
        float conversationPlacementAt = -1;

        [Header("Rig")]
        public Transform visual;
        public Transform rightShoulder, rightElbow, leftShoulder, leftElbow, gadget;
        public SkinnedMeshRenderer mouth;
        public int mouthShape;
        public Renderer[] glowRenderers;
        [ColorUsage(false, true)] public Color glowColor = Color.cyan;

        [Header("Voice and FX")]
        public AudioSource voice;
        public AudioSource liveVoice, sfx;
        public AudioClip chirp;
        public ParticleSystem sparks;

        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        const float PointTurnShare = 0.35f, AcrossLimit = 15f, MaxTurn = 75f;
        const float HeadYaw = 20f, HeadPitch = 12f, EyeYaw = 10f, EyePitch = 8f, ChestMax = 10f;
        // Where he holds while the Ask panel is open: viewport x 0.30, the middle of the part of the frame the lesson panel
        // leaves free (PanelFraming already shifts the view), in the middle band.
        const float AskViewportX = 0.30f, AskViewportYMin = 0.35f, AskViewportYMax = 0.62f;

        readonly float[] samples = new float[256];
        MaterialPropertyBlock block;
        Gesture? agentGesture;
        Transform agentLook;
        Vector3? askHome;
        Vector3 home, velocity, visualHome, chase, chaseVelocity, lastPosition;
        float loudness, rollTime = -1, nextRoll, pose, flash;
        bool pointLeft, ready, placed;
        AudioClip scriptedClip, unreadableClip;
        float scriptedTime;
        int scriptedFrame = -10;

        // procedural clock and timers (game time, so the Transform action stays in step with the Timeline)
        float clock, gestureStart, reactionStart = -100f, blinkAt = 2f, blinkStart = -1f, saccadeAt, shiftAt, shift, shiftTarget;
        Gesture shown;
        Reaction reaction;
        bool actionFired;
        Vector2 saccade, headAngles, eyeAngles;
        float headRoll, chestPitch, legDrag;

        // rig (found by name under the visual; any part may be missing, e.g. the placeholder sphere)
        Transform frameT, chest, head;
        Quaternion chestRest, headRest;
        readonly Eye[] eyes = new Eye[2];
        Arm right, left;
        readonly Leg[] legs = new Leg[2];

        class Eye { public Transform t; public Quaternion rest; public Vector3 scale; public int upAxis; }
        class Leg { public int side; public Transform thigh, knee, ankle, ball; public Quaternion thighRest, kneeRest, ankleRest; public float phase; }
        class Arm
        {
            public int side; // +1 his right, -1 his left (frame x)
            public Transform clav, upper, lower, hand;
            public Quaternion clavRest, upperRest, lowerRest, handRest;
            public float length;
            public Vector3 handAxis, palmAxis; // hand-local: along the hand, palm normal
            public Transform[][] fingers;
            public Quaternion[][] fingerRest;
            public Vector3[] curlAxis;         // hand-local, per finger
            public int thumb = -1, index = -1;
            public Goal now;                   // the blended goal on screen (frame space)
            public bool primed;
        }

        // A hand goal. Gesture tables write x as "outward" for either arm; Solve() turns it into frame space.
        struct Goal
        {
            public Vector3 dir;     // shoulder -> hand
            public float reach;     // fraction of the straight arm, 0.7 .. 0.98 (the elbow stays under ~95 degrees)
            public Vector3 hint;    // where the elbow goes
            public Vector3 palm;    // palm normal wanted (zero: keep the twist)
            public float curl;      // finger curl, 0 open .. 1 fist
            public float pointing;  // 1: the index finger stays straight
            public float aim;       // 1: the wrist turns the hand (and the gadget) to aimAt
            public Vector3 aimAt;
            public float wave;      // wrist wave, degrees
            public float droop;     // relaxed wrist flex, degrees
        }

        public Gesture Effective => agentGesture ?? gesture;
        public float Loudness => loudness;
        // Seconds since the current gesture began (editor tests and debugging).
        public float GestureTime => clock - gestureStart;
        public bool PointingWithLeft => pointLeft;

        void OnEnable() { Active = this; }
        void OnDisable() { EndConversationView(); if (Active == this) Active = null; }

        void Awake() => Init();

        void Init()
        {
            if (ready) return;
            ready = true;
            block = new MaterialPropertyBlock();
            home = transform.position;
            if (path) transform.position = path.EvaluatePosition(pathT);
            chase = lastPosition = transform.position;
            if (visual) visualHome = visual.localPosition;
            frameT = visual ? visual : transform;
            foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                footClearance = Mathf.Max(footClearance, transform.position.y - skin.bounds.min.y);
                bodyRadius = Mathf.Max(bodyRadius, Mathf.Min(skin.bounds.extents.x, skin.bounds.extents.z));
            }
            footClearance += bobHeight + 0.06f;

            var all = GetComponentsInChildren<Transform>(true);
            Transform Bone(string n)
            {
                foreach (var t in all) if (t.name == n) return t;
                return null;
            }
            chest = Bone("Chest_jnt");
            head = Bone("head_jnt");
            if (chest) chestRest = chest.localRotation;
            if (head) headRest = head.localRotation;
            for (int i = 0; i < 2; i++)
            {
                var t = Bone(i == 0 ? "eye_left" : "eye_right");
                if (!t) continue;
                eyes[i] = new Eye { t = t, rest = t.localRotation, scale = t.localScale, upAxis = UpAxis(t) };
            }
            right = MakeArm(Or(rightShoulder, Bone("right_Sholder_jnt")), Or(rightElbow, Bone("right_elbow_jnt")), Bone("right_wrist_jnt"), Bone("right_scapula_jnt"));
            left = MakeArm(Or(leftShoulder, Bone("left_Sholder_jnt")), Or(leftElbow, Bone("left_elbow_jnt")), Bone("left_wrist_jnt"), Bone("left_scapula_jnt"));
            for (int i = 0; i < 2; i++)
            {
                string s = i == 0 ? "right_" : "left_";
                Transform thigh = Bone(s + "thigh_jnt"), knee = Bone(s + "knee_jnt"), ankle = Bone(s + "ankle_jnt");
                if (!thigh || !knee || !ankle) continue;
                legs[i] = new Leg
                {
                    side = i == 0 ? 1 : -1, thigh = thigh, knee = knee, ankle = ankle, ball = Bone(s + "ball_roll_jnt"),
                    thighRest = thigh.localRotation, kneeRest = knee.localRotation, ankleRest = ankle.localRotation, phase = i * 2.1f,
                };
            }
            shown = Effective;
            blinkAt = Random.Range(1.5f, 4f);
        }

        static Transform Or(Transform a, Transform b) => a ? a : b;

        // The eye bone's local axis that points up at rest: the blink squashes along it.
        int UpAxis(Transform t)
        {
            int best = 1;
            float dot = 0;
            for (int i = 0; i < 3; i++)
            {
                Vector3 axis = Vector3.zero;
                axis[i] = 1;
                float d = Mathf.Abs(Vector3.Dot(t.TransformDirection(axis), frameT.up));
                if (d > dot) { dot = d; best = i; }
            }
            return best;
        }

        Arm MakeArm(Transform upper, Transform lower, Transform hand, Transform clav)
        {
            if (!upper || !lower || !hand) return null;
            var a = new Arm
            {
                upper = upper, lower = lower, hand = hand, clav = clav,
                upperRest = upper.localRotation, lowerRest = lower.localRotation, handRest = hand.localRotation,
                clavRest = clav ? clav.localRotation : Quaternion.identity,
                side = frameT.InverseTransformPoint(upper.position).x >= 0 ? 1 : -1,
                length = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position),
            };
            // Fingers: the wrist's children named like the rig's finger joints (the gadget hangs off the right wrist too).
            var chains = new System.Collections.Generic.List<Transform[]>();
            foreach (Transform c in hand)
            {
                if (!c.name.Contains("Fingr")) continue;
                var chain = new System.Collections.Generic.List<Transform>();
                for (var t = c; t; t = FirstFinger(t)) chain.Add(t);
                chains.Add(chain.ToArray());
            }
            a.fingers = chains.ToArray();
            a.fingerRest = new Quaternion[a.fingers.Length][];
            for (int f = 0; f < a.fingers.Length; f++)
            {
                a.fingerRest[f] = new Quaternion[a.fingers[f].Length];
                for (int j = 0; j < a.fingers[f].Length; j++) a.fingerRest[f][j] = a.fingers[f][j].localRotation;
            }
            Vector3 forearm = (hand.position - lower.position).normalized;
            Vector3 along = forearm;
            if (a.fingers.Length >= 3)
            {
                // The thumb is the finger reaching farthest forward (his facing) in the rest pose, whatever the rig calls
                // it (LB's finger numbers are not mirrored left/right); the index is the finger next to it, the little
                // finger the farthest. The palm faces the side the thumb hangs to.
                float most = float.MinValue;
                for (int f = 0; f < a.fingers.Length; f++)
                {
                    float ahead = Vector3.Dot(Tip(a.fingers[f]) - hand.position, frameT.forward);
                    if (ahead > most) { most = ahead; a.thumb = f; }
                }
                Vector3 thumbRoot = a.fingers[a.thumb][0].position, sum = Vector3.zero;
                int little = -1;
                float near = float.MaxValue, far = -1;
                for (int f = 0; f < a.fingers.Length; f++)
                {
                    if (f == a.thumb) continue;
                    float d = Vector3.Distance(a.fingers[f][0].position, thumbRoot);
                    if (d < near) { near = d; a.index = f; }
                    if (d > far) { far = d; little = f; }
                    sum += a.fingers[f][0].position - hand.position;
                }
                along = sum.normalized;
                Vector3 palm = Vector3.Cross(a.fingers[a.index][0].position - hand.position, a.fingers[little][0].position - hand.position).normalized;
                if (Vector3.Dot(palm, Tip(a.fingers[a.thumb]) - hand.position) < 0) palm = -palm;
                a.palmAxis = hand.InverseTransformDirection(palm);
                a.curlAxis = new Vector3[a.fingers.Length];
                for (int f = 0; f < a.fingers.Length; f++)
                {
                    Vector3 dir = Tip(a.fingers[f]) - a.fingers[f][0].position;
                    a.curlAxis[f] = hand.InverseTransformDirection(Vector3.Cross(dir, palm).normalized);
                }
            }
            else a.palmAxis = hand.InverseTransformDirection(-frameT.up);
            a.handAxis = hand.InverseTransformDirection(along);
            return a;
        }

        static Transform FirstFinger(Transform t)
        {
            foreach (Transform c in t) if (c.name.Contains("Fingr")) return c;
            return null;
        }

        static Vector3 Tip(Transform[] chain) =>
            chain.Length > 1 ? chain[chain.Length - 1].position : chain[0].position + (chain[0].position - chain[0].parent.position);

        // ------------------------------------------------------------------ API

        // Ask Wattson takes over the pose while the cinematic is paused; pass null to hand it back to the Timeline. While it
        // holds he also moves to the Ask spot beside the panel, computed once from the camera (the look transform when it
        // is one, else Camera.main) at his own depth. home, path and pathT stay untouched, so the Timeline's per-frame
        // MoveTo and the resumed flight behave exactly as before, and he springs back when the pose is handed back.
        public void SetAgentPose(Gesture? agentPose, Transform look)
        {
            agentGesture = agentPose;
            agentLook = look;
            if (agentPose == null) { askHome = null; EndConversationView(); return; }
            if (askHome.HasValue) return;
            if (conversationCamera && Camera.main && Camera.main.TryGetComponent(out conversationBrain))
            {
                // Reuse the scene's authored wide shot during a question; moving deeper from a macro shot can hide him behind the set.
                conversationFov = conversationCamera.Lens.FieldOfView;
                conversationCamera.Lens.FieldOfView = Mathf.Min(75, conversationFov * 1.15f);
                conversationOverride = conversationBrain.SetCameraOverride(-1, 10000, null, conversationCamera, 1, Time.unscaledDeltaTime);
                askHome = transform.position;
                conversationPlacementAt = Time.unscaledTime + 0.35f;
                return;
            }
            var cam = look ? look.GetComponent<Camera>() : null;
            if (!cam) cam = Camera.main;
            if (!cam) return;
            Vector3 vp = cam.WorldToViewportPoint(transform.position);
            if (vp.z <= 0) return; // behind the camera: leave him where he is
            askHome = cam.ViewportToWorldPoint(new Vector3(AskViewportX, Mathf.Clamp(vp.y, AskViewportYMin, AskViewportYMax), vp.z));
        }

        void EndConversationView()
        {
            conversationPlacementAt = -1;
            if (conversationOverride < 0) return;
            if (conversationBrain) conversationBrain.ReleaseCameraOverride(conversationOverride);
            if (conversationCamera) conversationCamera.Lens.FieldOfView = conversationFov;
            conversationOverride = -1;
        }

        // Blockout scenes fly him between markers instead of along a spline.
        public void MoveTo(Transform marker)
        {
            if (!marker) return;
            path = null;
            home = marker.position;
        }

        // Puts him on a marker at once, settled (under a fade or a cut), instead of springing there.
        public void Warp(Transform marker)
        {
            if (!marker) return;
            MoveTo(marker);
            transform.position = chase = lastPosition = home;
            velocity = chaseVelocity = Vector3.zero;
        }

        // Called by the subtitle track every frame a scripted line is on screen.
        public void ScriptedSpeech(AudioClip clip, float time)
        {
            scriptedClip = clip;
            scriptedTime = time;
            scriptedFrame = Time.frameCount;
        }

        // A short reaction layered over the current gesture: Correct = a fist pump and a hop, Partial = a so-so wobble
        // and a head tilt, Incorrect = a gentle palms-up shrug (encouraging, never mocking). Safe to call every answer.
        public void React(Reaction kind)
        {
            Init();
            reaction = kind;
            reactionStart = clock;
            blinkStart = clock; // a blink sells the beat
            if (kind == Reaction.Correct)
            {
                flash = Mathf.Max(flash, 0.6f);
                if (sparks) sparks.Emit(ReducedMotion.On ? 4 : 12);
            }
        }

        public void React(bool correct) => React(correct ? Reaction.Correct : Reaction.Incorrect);

        // Editor tests: one frame of flight and pose with an explicit time step (Update/LateUpdate do not run in edit mode).
        public void Simulate(float dt)
        {
            Init();
            Step(dt);
            PoseRig(dt);
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            if (conversationOverride >= 0 && conversationBrain)
                conversationBrain.SetCameraOverride(conversationOverride, 10000, null, conversationCamera, 1, Time.unscaledDeltaTime);
            // Let the authored wide shot and panel framing settle before choosing a spot at his existing depth.
            if (conversationPlacementAt >= 0 && Time.unscaledTime >= conversationPlacementAt && Camera.main)
            {
                var cam = Camera.main;
                var vp = cam.WorldToViewportPoint(transform.position);
                if (vp.z > 0)
                    askHome = cam.ViewportToWorldPoint(new Vector3(AskViewportX, Mathf.Clamp(vp.y, AskViewportYMin, AskViewportYMax), vp.z));
                conversationPlacementAt = -1;
            }
            Step(Time.deltaTime);
            FunTap();
        }

        void LateUpdate() => PoseRig(Time.deltaTime);

        // The Timeline may switch the gesture after Update: both Step and PoseRig notice, so no frame poses a new gesture
        // with the old gesture's clock.
        void TrackGesture()
        {
            var g = Effective;
            if (g == shown) return;
            shown = g;
            gestureStart = clock;
            actionFired = false;
            if (Random.value < 0.6f) blinkStart = clock; // people blink as they change what they do
        }

        void Step(float dt)
        {
            clock += dt;
            TrackGesture();
            var g = Effective;
            Fly(dt * motionScale);
            Talk(dt);
            if (gadget)
            {
                bool thinking = g == Gesture.Thinking;
                gadget.gameObject.SetActive(gadgetOut || thinking || g == Gesture.Transform);
                if (thinking) gadget.Rotate(0, 0, 540 * dt, Space.Self);
            }
            if (g == Gesture.Transform && !actionFired && GestureTime >= TransformActionDelay)
            {
                actionFired = true;
                flash = 1.2f;
                if (sparks) sparks.Emit(ReducedMotion.On ? 8 : 26);
            }
            flash = Mathf.MoveTowards(flash, 0, dt * 1.6f);
        }

        // Which arm leads: the gadget hand (right) for every gesture, except a bare-handed Point, which uses the arm on the
        // target's side. The side is taken from where he looks, not his turned body, and flips only past a dead zone.
        bool LeadIsRight()
        {
            if (gadgetOut || Effective != Gesture.Point || !pointTarget) { pointLeft = false; return true; }
            Transform look = agentLook ? agentLook : lookTarget;
            Vector3 ahead = Vector3.ProjectOnPlane(look ? look.position - transform.position : transform.forward, Vector3.up).normalized;
            float side = Vector3.Cross(ahead, Vector3.ProjectOnPlane(pointTarget.position - transform.position, Vector3.up).normalized).y;
            if (Mathf.Abs(side) > 0.1f) pointLeft = side < 0;
            return !pointLeft;
        }

        bool Aiming(Gesture g) => pointTarget && !agentLook && (g == Gesture.Point || g == Gesture.Aim || g == Gesture.Transform || g == Gesture.RaiseGadget);

        void Fly(float dt)
        {
            Vector3 target = askHome ?? (path ? (Vector3)path.EvaluatePosition(pathT) : home);
            if (!path || askHome.HasValue) target = ClearFurniture(transform.position, target);
            // Something else moved him (a director cue under a fade): he settles where he was put, no spring back.
            if (placed && (transform.position - lastPosition).sqrMagnitude > 1e-8f)
            {
                chase = transform.position;
                velocity = chaseVelocity = Vector3.zero;
            }
            placed = true;
            if (askHome.HasValue && ReducedMotion.On) { transform.position = chase = target; velocity = chaseVelocity = Vector3.zero; } // no glide to the Ask spot (read every frame: the toggle flips mid-shot)
            else if (path && !askHome.HasValue) { chase = target; chaseVelocity = Vector3.zero; } // the Timeline's flight is already eased
            else if (dt > 0) chase = Vector3.SmoothDamp(chase, target, ref chaseVelocity, Mathf.Max(0.05f, moveTime), Mathf.Infinity, dt);
            // Substeps prevent a scene-load hitch launching the spring through furniture.
            for (float left = furniture.Length > 0 ? Mathf.Min(dt, 0.2f) : dt; left > 0;)
            {
                float step = furniture.Length > 0 ? Mathf.Min(left, 1f / 90f) : left;
                velocity += (chase - transform.position) * (spring * step);
                velocity *= Mathf.Exp(-damping * step);
                Vector3 next = transform.position + velocity * step;
                if (!path || askHome.HasValue)
                {
                    Vector3 safe = ClearFurniture(transform.position, next);
                    if (safe.y > next.y) velocity.y = Mathf.Max(0, velocity.y);
                    next = safe;
                }
                transform.position = next;
                left -= step;
            }
            lastPosition = transform.position;

            var g = Effective;
            Transform look = agentLook ? agentLook : lookTarget;
            Vector3 forward = look ? look.position - transform.position : transform.forward;
            bool leadRight = LeadIsRight();
            if (Aiming(g))
            {
                // Turn toward the target: a three-quarter share for readability, and at least enough that the target sits
                // no more than AcrossLimit degrees across the pointing arm's side, so the arm never folds over his chest.
                Vector3 flat = Vector3.ProjectOnPlane(forward, Vector3.up), aim = Vector3.ProjectOnPlane(pointTarget.position - transform.position, Vector3.up);
                if (flat.sqrMagnitude > 1e-6f && aim.sqrMagnitude > 1e-6f)
                {
                    float a = Vector3.SignedAngle(flat, aim, Vector3.up); // + = to his right
                    float turn = leadRight ? Mathf.Min(a * PointTurnShare, a + AcrossLimit) : Mathf.Max(a * PointTurnShare, a - AcrossLimit);
                    forward = Quaternion.AngleAxis(Mathf.Clamp(turn, -MaxTurn, MaxTurn), Vector3.up) * flat + Vector3.up * forward.y;
                }
            }
            if (velocity.magnitude > 1.2f) forward = Vector3.Lerp(forward.normalized, velocity.normalized, 0.7f);
            forward.y *= 0.35f;
            if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
            float roll = Mathf.Clamp(-Vector3.Dot(velocity, transform.right) * bank, -35f, 35f);
            float pitch = Mathf.Clamp(Vector3.Dot(velocity, transform.forward) * 6f, -10f, 22f);
            var rotation = Quaternion.LookRotation(forward.normalized, Vector3.up) * Quaternion.Euler(pitch, 0, roll);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1 - Mathf.Exp(-7f * dt));

            if (!visual || visual == transform) return; // the placeholder sphere has no separate visual to bob
            float calm = ReducedMotion.On ? 0.35f : 1f;
            float barrel = 0;
            if (rollTime >= 0)
            {
                rollTime += dt / 0.55f;
                barrel = Mathf.SmoothStep(0, 360, rollTime);
                if (rollTime >= 1) rollTime = -1;
            }
            pose = Mathf.MoveTowards(pose, g == Gesture.Idle ? 0 : 1, dt * 3f);
            // Weight shifts: every few seconds he settles his weight onto a new side.
            if (clock >= shiftAt) { shiftTarget = Random.Range(-1f, 1f); shiftAt = clock + Random.Range(2.5f, 6f); }
            shift = Mathf.Lerp(shift, shiftTarget, 1 - Mathf.Exp(-1.5f * dt));
            float bow = g == Gesture.Greet ? 16f : g == Gesture.Thinking ? -10f : 0;
            float tilt = g == Gesture.Listening ? 12f : 0;
            float sway = (g == Gesture.Thinking ? Mathf.Sin(clock * 1.3f) * 14f : 0) + Mathf.Sin(clock * 0.55f) * 3f * calm;
            float idleRoll = (Mathf.Sin(clock * 0.9f) * 2f + shift * 3f) * calm;

            Vector3 offset = Vector3.up * (Mathf.Sin(clock * bobSpeed) * bobHeight);
            // Reactions: a hop for a correct answer, a small dip for a wrong one.
            float rt = clock - reactionStart;
            if (rt < ReactionSeconds)
            {
                if (reaction == Reaction.Correct) offset += Vector3.up * (Mathf.Sin(Mathf.PI * Mathf.Clamp01(rt / 0.42f)) * 0.3f * calm);
                else if (reaction == Reaction.Incorrect) offset += Vector3.down * (Mathf.Sin(Mathf.PI * Mathf.Clamp01(rt / 0.8f)) * 0.06f);
            }
            // Transform: he coils back and shivers with charge, lunges at the action moment, then settles.
            if (g == Gesture.Transform && pointTarget)
            {
                float t = GestureTime;
                float gather = Mathf.SmoothStep(0, 1, t / (TransformActionDelay - 0.1f)), thrust = Thrust(t), settle = Settle(t);
                Vector3 toward = transform.InverseTransformDirection(Vector3.ProjectOnPlane(pointTarget.position - transform.position, Vector3.up).normalized);
                offset += toward * ((thrust * (1 - settle) * 0.28f - gather * (1 - thrust) * 0.06f) * calm);
                offset += new Vector3(Mathf.PerlinNoise(clock * 30f, 1) - 0.5f, Mathf.PerlinNoise(1, clock * 30f) - 0.5f, 0) * (0.03f * gather * (1 - thrust) * calm);
                bow += (thrust * (1 - settle) * 12f - gather * (1 - thrust) * 9f) * calm;
            }
            visual.localPosition = visualHome + offset;
            visual.localRotation = Quaternion.Slerp(visual.localRotation,
                Quaternion.Euler(bow * pose, sway, tilt * pose + barrel + idleRoll), rollTime >= 0 ? 1 : 1 - Mathf.Exp(-6f * dt));
        }

        Vector3 ClearFurniture(Vector3 from, Vector3 to)
        {
            foreach (var surface in furniture)
            {
                if (!surface || !surface.enabled || !surface.gameObject.activeInHierarchy) continue;
                var box = surface.bounds;
                float top = box.max.y + footClearance;
                box.Expand(new Vector3(bodyRadius * 2f + 0.12f, 0, bodyRadius * 2f + 0.12f));
                // Two safe markers can straddle a table: check the swept footprint too.
                var a = new Vector3(from.x, box.center.y, from.z);
                var b = new Vector3(to.x, box.center.y, to.z);
                Vector3 direction = b - a;
                bool crossing = box.Contains(a) || box.Contains(b) ||
                    (direction.sqrMagnitude > 0.000001f && box.IntersectRay(new Ray(a, direction.normalized), out float distance) && distance <= direction.magnitude);
                if (crossing) to.y = Mathf.Max(to.y, top);
            }
            return to;
        }

        // Transform phases: 0 -> 1 thrust over 0.16 s, fastest exactly at the action moment; 0 -> 1 settle.
        static float Thrust(float t) { float x = Mathf.Clamp01((t - (TransformActionDelay - 0.08f)) / 0.16f); return x * x * (3 - 2 * x); }
        static float Settle(float t) => Mathf.SmoothStep(0, 1, (t - (TransformActionDelay + 0.2f)) / (TransformSettleSeconds - TransformActionDelay - 0.2f));

        void Talk(float dt)
        {
            float raw = 0;
            if (scriptedFrame >= Time.frameCount - 1) raw = ClipLoudness();
            else if (liveVoice && liveVoice.isPlaying) { liveVoice.GetOutputData(samples, 0); raw = Rms(); }
            else if (agentGesture == Gesture.Talking) raw = SilentTalk(); // answer shown without a voice clip
            loudness = Mathf.Lerp(loudness, Mathf.Clamp01(raw * 6f), 1 - Mathf.Exp(-18f * dt));

            if (mouth) mouth.SetBlendShapeWeight(mouthShape, loudness * 100f);
            if (glowRenderers == null || block == null) return;
            // He always glows a little, being made of electricity, and brightens as he speaks (and as he fires the gadget).
            block.SetColor(Emission, glowColor * (0.45f + loudness * 0.8f + flash * (ReducedMotion.On ? 0.3f : 1f)));
            foreach (var r in glowRenderers) if (r) r.SetPropertyBlock(block);
        }

        float ClipLoudness()
        {
            // No recorded voice yet: keep him animated so silent fallback timing still reads as talking.
            if (!scriptedClip || scriptedClip == unreadableClip) return SilentTalk();
            int channels = scriptedClip.channels;
            int offset = Mathf.Clamp((int)(scriptedTime * scriptedClip.frequency), 0, Mathf.Max(0, scriptedClip.samples - samples.Length / channels));
            if (!scriptedClip.GetData(samples, offset)) { unreadableClip = scriptedClip; return 0.1f; }
            return Rms();
        }

        float SilentTalk() => 0.06f + 0.08f * Mathf.PerlinNoise(clock * 9f, 0);

        float Rms()
        {
            float sum = 0;
            foreach (float s in samples) sum += s * s;
            return Mathf.Sqrt(sum / samples.Length);
        }

        // ------------------------------------------------------------------ pose

        void PoseRig(float dt)
        {
            TrackGesture();
            dt *= motionScale;
            var g = Effective;
            bool talking = loudness > 0.05f;
            if (g == Gesture.Idle && talking) g = Gesture.Talking;
            float calm = ReducedMotion.On ? 0.35f : 1f;
            float breath = Mathf.Sin(clock * 1.7f);
            float rt = clock - reactionStart, react = rt < ReactionSeconds
                ? Mathf.SmoothStep(0, 1, rt / 0.18f) * (1 - Mathf.SmoothStep(0, 1, (rt - 0.85f) / (ReactionSeconds - 0.85f))) : 0;

            // Back to the rest pose, then layer everything on top (world space: independent of the rig's bone axes).
            if (chest) chest.localRotation = chestRest;
            if (head) head.localRotation = headRest;
            foreach (var e in eyes) if (e != null) { e.t.localRotation = e.rest; e.t.localScale = e.scale; }
            ResetArm(right);
            ResetArm(left);
            foreach (var l in legs) if (l != null) { l.thigh.localRotation = l.thighRest; l.knee.localRotation = l.kneeRest; l.ankle.localRotation = l.ankleRest; }

            Chest(g, dt, breath, talking, react);
            Look(g, dt, talking, react);
            Legs(g, dt, calm);
            Arms(g, dt, breath, calm, react);
        }

        static void ResetArm(Arm a)
        {
            if (a == null) return;
            if (a.clav) a.clav.localRotation = a.clavRest;
            a.upper.localRotation = a.upperRest; a.lower.localRotation = a.lowerRest; a.hand.localRotation = a.handRest;
            for (int f = 0; f < a.fingers.Length; f++)
                for (int j = 0; j < a.fingers[f].Length; j++) a.fingers[f][j].localRotation = a.fingerRest[f][j];
        }

        static float Ease(float rate, float dt) => 1 - Mathf.Exp(-rate * dt);

        void Chest(Gesture g, float dt, float breath, bool talking, float react)
        {
            if (!chest) return;
            float t = GestureTime;
            float want = breath * 1.5f + (talking ? loudness * 4f + Mathf.Sin(clock * 6.5f) * loudness * 3f : 0);
            if (g == Gesture.Thinking) want -= 4f;
            if (g == Gesture.Transform) want += Thrust(t) * (1 - Settle(t)) * 8f - Mathf.SmoothStep(0, 1, t / 0.45f) * (1 - Thrust(t)) * 7f;
            if (reaction == Reaction.Incorrect) want -= 4f * react;
            chestPitch = Mathf.Lerp(chestPitch, Mathf.Clamp(want, -ChestMax, ChestMax), Ease(12f, dt));
            chest.rotation = Quaternion.AngleAxis(chestPitch, frameT.right) * chest.rotation;
        }

        // Head and eyes: at the camera (or whoever he talks to); at the point target while pointing, glancing back to the
        // camera while he speaks. Eyes take what the head's range cannot, plus small darts; blinks squash the pupils.
        void Look(Gesture g, float dt, bool talking, float react)
        {
            Transform cam = agentLook ? agentLook : lookTarget;
            if (!cam && Camera.main) cam = Camera.main.transform;
            Vector3 origin = head ? head.position : frameT.position + frameT.up;
            Vector3 gaze = cam ? cam.position : origin + frameT.forward;
            if (pointTarget && !agentLook && (g == Gesture.Point || g == Gesture.Aim || g == Gesture.Transform || g == Gesture.RaiseGadget || g == Gesture.Reach))
            {
                float share = g == Gesture.Reach ? 0 : talking ? (GestureTime < 1.2f ? 0.8f : 0.25f) : 0.85f;
                gaze = Vector3.Lerp(gaze, pointTarget.position, share);
            }
            Vector3 d = frameT.InverseTransformDirection(gaze - origin);
            if (d.sqrMagnitude < 1e-8f) d = Vector3.forward;
            d.Normalize();
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, pitch = -Mathf.Asin(Mathf.Clamp(d.y, -1, 1)) * Mathf.Rad2Deg - chestPitch;
            if (Mathf.Abs(yaw) > 120f) yaw = 0; // behind him: do not wring his neck
            float nod = talking ? Mathf.Sin(clock * 5.3f) * loudness * 10f : 0;
            float roll = g == Gesture.Listening ? 8f : g == Gesture.Thinking ? -6f : 0;
            float shake = 0;
            if (react > 0)
            {
                float rt = clock - reactionStart;
                if (reaction == Reaction.Correct) nod += Mathf.Sin(rt * 14f) * 6f * react;
                else if (reaction == Reaction.Partial) roll += 10f * react;
                else { roll -= 7f * react; shake = Mathf.Sin(rt * 15f) * 6f * react * (1 - rt / ReactionSeconds); }
            }
            Vector2 want = new Vector2(Mathf.Clamp(yaw + shake, -HeadYaw, HeadYaw), Mathf.Clamp(pitch + nod, -HeadPitch, HeadPitch));
            headAngles = Vector2.Lerp(headAngles, want, Ease(9f, dt));
            headRoll = Mathf.Lerp(headRoll, roll, Ease(6f, dt));
            Quaternion turn = Quaternion.AngleAxis(headAngles.x, frameT.up) * Quaternion.AngleAxis(headAngles.y, frameT.right) * Quaternion.AngleAxis(headRoll, frameT.forward);
            if (head) head.rotation = turn * head.rotation;

            // Eyes
            if (clock >= saccadeAt) { saccade = Random.insideUnitCircle * 2.5f; saccadeAt = clock + Random.Range(0.4f, 2.2f); }
            Vector2 eyeWant = new Vector2(Mathf.Clamp(yaw - headAngles.x + saccade.x, -EyeYaw, EyeYaw), Mathf.Clamp(pitch - headAngles.y + saccade.y, -EyePitch, EyePitch));
            eyeAngles = Vector2.Lerp(eyeAngles, eyeWant, Ease(25f, dt));
            if (clock >= blinkAt) { blinkStart = clock; blinkAt = clock + Random.Range(2.2f, 5.5f); }
            float b = Mathf.Clamp01((clock - blinkStart) / 0.16f), open = b >= 1 ? 1 : 1 - 0.88f * Mathf.Sin(Mathf.PI * b);
            Quaternion eyeTurn = Quaternion.AngleAxis(eyeAngles.x, turn * frameT.up) * Quaternion.AngleAxis(eyeAngles.y, turn * frameT.right);
            foreach (var e in eyes)
            {
                if (e == null) continue;
                e.t.rotation = eyeTurn * e.t.rotation;
                Vector3 s = e.scale;
                s[e.upAxis] *= open;
                e.t.localScale = s;
            }
        }

        // Bent, relaxed legs that dangle while he floats: a slow pendulum per leg, the weight shift tucks one leg, they
        // trail behind a move and swing on with it, tuck for the Transform gather and kick back on the thrust.
        void Legs(Gesture g, float dt, float calm)
        {
            if (legs[0] == null && legs[1] == null) return;
            float forwardSpeed = Vector3.Dot(velocity, frameT.forward), side = Vector3.Dot(velocity, frameT.right);
            legDrag = Mathf.Lerp(legDrag, Mathf.Clamp(forwardSpeed * 14f, -22f, 22f), Ease(4f, dt));
            float t = GestureTime, gather = 0, kick = 0;
            if (g == Gesture.Transform) { gather = Mathf.SmoothStep(0, 1, t / 0.45f) * (1 - Thrust(t)); kick = Thrust(t) * (1 - Settle(t)); }
            float rt = clock - reactionStart, hop = reaction == Reaction.Correct && rt < 0.6f ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(rt / 0.6f)) : 0;
            foreach (var l in legs)
            {
                if (l == null) continue;
                float swing = Mathf.Sin(clock * 1.25f + l.phase) * 5f * calm;
                float thigh = 22f + swing + shift * l.side * 6f * calm - legDrag + gather * 14f - kick * 12f + hop * 12f;
                float knee = 40f + Mathf.Sin(clock * 1.25f + l.phase + 0.8f) * 7f * calm + shift * l.side * 10f * calm + Mathf.Abs(legDrag) * 0.6f + gather * 18f + kick * 4f + hop * 18f;
                thigh = Mathf.Clamp(thigh, -25f, 40f);
                knee = Mathf.Clamp(knee, 8f, 55f);
                Vector3 thighDir = l.knee.position - l.thigh.position;
                Vector3 want = Vector3.RotateTowards(thighDir, thigh >= 0 ? frameT.forward : -frameT.forward, Mathf.Abs(thigh) * Mathf.Deg2Rad, 0);
                want = Vector3.RotateTowards(want, frameT.right * Mathf.Sign(side) * -1f, Mathf.Clamp(Mathf.Abs(side) * 6f, 0, 10f) * Mathf.Deg2Rad, 0);
                l.thigh.rotation = Quaternion.FromToRotation(thighDir, want) * l.thigh.rotation;
                Vector3 nowThigh = (l.knee.position - l.thigh.position).normalized, shin = l.ankle.position - l.knee.position;
                Vector3 shinWant = Vector3.RotateTowards(nowThigh, -frameT.forward, knee * Mathf.Deg2Rad, 0);
                l.knee.rotation = Quaternion.FromToRotation(shin, shinWant) * l.knee.rotation;
                if (l.ball)
                {
                    // Toes drop a little: floating feet point down.
                    Vector3 foot = l.ball.position - l.ankle.position, shinNow = l.ankle.position - l.knee.position;
                    l.ankle.rotation = Quaternion.FromToRotation(foot, Vector3.RotateTowards(foot, shinNow, 14f * Mathf.Deg2Rad, 0)) * l.ankle.rotation;
                }
            }
        }

        // ------------------------------------------------------------------ arms

        static readonly Goal Hang = new Goal { dir = new Vector3(0.3f, -0.92f, 0.16f), reach = 0.93f, hint = new Vector3(0.45f, -0.5f, -0.75f), palm = new Vector3(-1f, 0, 0.15f), curl = 0.35f, droop = 10f };

        void Arms(Gesture g, float dt, float breath, float calm, float react)
        {
            if (right == null && left == null) return;
            bool leadRight = LeadIsRight();
            Arm lead = leadRight ? right : left, other = leadRight ? left : right;
            if (lead == null) { lead = other; other = null; }
            float t = GestureTime;
            float beat = loudness > 0.05f ? Mathf.Clamp01(loudness * 3f) * 0.18f * calm : 0;
            Goal a = Hang, b = Hang;
            a.dir.y += breath * 0.015f;
            b.dir.y += breath * 0.015f;
            a.dir.z -= shift * 0.06f * calm;
            b.dir.z += shift * 0.06f * calm;
            bool aimed = pointTarget && (g == Gesture.Point || g == Gesture.Aim || g == Gesture.Transform);
            Vector3 target = pointTarget ? pointTarget.position : Vector3.zero;
            switch (g)
            {
                case Gesture.Talking:
                    a = Open(0.4f, -0.5f, 0.75f, Mathf.Sin(clock * 6.5f) * beat);
                    b = Open(0.4f, -0.5f, 0.75f, Mathf.Sin(clock * 6.5f + 1.9f) * beat);
                    break;
                case Gesture.Greet:
                    a = new Goal { dir = new Vector3(0.5f + Mathf.Sin(clock * 8f) * 0.1f * calm, 0.8f, 0.3f), reach = 0.86f, hint = new Vector3(1f, -0.45f, -0.2f), palm = new Vector3(0, 0.1f, 1f), curl = 0.05f, wave = 16f * calm };
                    break;
                case Gesture.Point:
                    if (aimed) a = Aimed(lead, target, 0.97f, gadgetOut ? 0.75f : 0.85f, gadgetOut ? 0 : 1);
                    else a = new Goal { dir = new Vector3(0.3f, 0.15f, 1f), reach = 0.96f, hint = new Vector3(0.5f, -1f, -0.2f), palm = new Vector3(0, -1, 0), curl = 0.85f, pointing = 1 };
                    b.dir = new Vector3(0.3f, -0.9f, -0.1f);
                    b.dir.y += Mathf.Sin(clock * 6.5f) * beat * 0.5f;
                    break;
                case Gesture.Flick:
                    a = b = new Goal { dir = new Vector3(0.8f, 0.45f, 0.4f), reach = 0.92f, hint = new Vector3(0.5f, -1f, -0.2f), palm = new Vector3(0, 1f, 0.3f), curl = 0.05f, droop = -10f };
                    break;
                case Gesture.Reach:
                    a = new Goal { dir = new Vector3(0.45f, -0.55f, -0.7f), reach = 0.9f, hint = new Vector3(1f, 0.1f, 0.2f), palm = new Vector3(0, 0, -1f), curl = 0.6f };
                    break;
                case Gesture.RaiseGadget:
                    a = new Goal { dir = new Vector3(0.25f, 1f, 0.3f), reach = 0.95f, hint = new Vector3(1f, -0.3f, -0.3f), palm = new Vector3(-1f, 0, 0), curl = 0.75f };
                    if (pointTarget && lead != null)
                    {
                        // Raised AT the target (Scene 8: the freeze is aimed at Ohm), not straight up; the hand stays on
                        // its own side so it never covers his face.
                        Vector3 at = Aimed(lead, target, 0.95f, 0.75f, 0).dir;
                        at.x = Mathf.Max(at.x, 0.15f);
                        a.dir = Vector3.Slerp(new Vector3(0.45f, 1f, 0.2f).normalized, at.normalized, 0.4f);
                        a.aim = 1; a.aimAt = target;
                    }
                    b.dir = new Vector3(0.4f, -0.7f, 0.2f);
                    break;
                case Gesture.Aim:
                    a = aimed ? Aimed(lead, target, 0.95f, 0.75f, 0) : new Goal { dir = new Vector3(0.2f, 0.1f, 1f), reach = 0.95f, hint = new Vector3(0.5f, -1f, -0.2f), palm = new Vector3(-1f, 0, 0), curl = 0.75f };
                    break; // the other hand supports the gadget arm: solved after it (see below)
                case Gesture.Listening:
                    b = new Goal { dir = new Vector3(0.3f, 0.9f, 0.2f), reach = 0.85f, hint = new Vector3(1f, -0.2f, 0), palm = new Vector3(-1f, 0, 0.2f), curl = 0.3f };
                    if (!leadRight) { var s = a; a = b; b = s; } // the listening hand is always his left
                    break;
                case Gesture.Thinking:
                    a = new Goal { dir = new Vector3(-0.35f, 0.5f, 0.8f), reach = 0.85f, hint = new Vector3(0.5f, -1f, 0.3f), palm = new Vector3(-0.5f, 0.6f, -0.3f), curl = 0.6f };
                    b = new Goal { dir = new Vector3(-0.55f, -0.45f, 0.7f), reach = 0.78f, hint = new Vector3(0.6f, -1f, 0), palm = new Vector3(0, 1f, 0), curl = 0.4f };
                    break;
                case Gesture.Transform:
                {
                    float gather = Mathf.SmoothStep(0, 1, t / (TransformActionDelay - 0.1f)), thrust = Thrust(t), settle = Settle(t);
                    var windUp = new Goal { dir = new Vector3(0.55f, 0.35f, -0.55f), reach = 0.72f, hint = new Vector3(1f, -0.6f, -0.3f), palm = new Vector3(-1f, 0, 0), curl = 0.85f };
                    Goal strike = pointTarget ? Aimed(lead, target, 0.98f, 0.85f, 0) : new Goal { dir = new Vector3(0.2f, 0.1f, 1f), reach = 0.98f, hint = new Vector3(0.5f, -1f, -0.2f), palm = new Vector3(-1f, 0, 0), curl = 0.85f };
                    Goal hold = strike;
                    hold.reach = 0.93f;
                    a = Mix(Mix(Mix(Hang, windUp, gather), strike, thrust), hold, settle);
                    var balance = new Goal { dir = new Vector3(0.35f, -0.2f, 0.85f), reach = 0.85f, hint = new Vector3(0.6f, -1f, 0), palm = new Vector3(0, 0, 1f), curl = 0.2f };
                    var swingBack = new Goal { dir = new Vector3(0.45f, -0.75f, -0.45f), reach = 0.9f, hint = new Vector3(0.5f, -0.5f, 0.6f), palm = new Vector3(-1f, 0, 0), curl = 0.4f };
                    b = Mix(Mix(Mix(Hang, balance, gather), swingBack, thrust), Hang, settle);
                    break;
                }
            }
            // Reactions ride over the gesture.
            if (react > 0)
            {
                float rt = clock - reactionStart;
                if (reaction == Reaction.Correct)
                    a = Mix(a, new Goal { dir = new Vector3(0.3f, 1f, 0.15f), reach = 0.8f + Mathf.Sin(rt * 18f) * 0.06f * calm, hint = new Vector3(1f, -0.4f, -0.2f), palm = new Vector3(-1f, 0, 0), curl = 1f }, react);
                else if (reaction == Reaction.Partial)
                    a = Mix(a, new Goal { dir = new Vector3(0.15f, -0.2f, 0.95f), reach = 0.7f, hint = new Vector3(0.7f, -1f, 0), palm = new Vector3(0, -1f, 0), curl = 0.15f, wave = 14f }, react);
                else
                {
                    var shrug = new Goal { dir = new Vector3(0.65f, -0.45f, 0.6f), reach = 0.78f, hint = new Vector3(0.5f, -1f, -0.2f), palm = new Vector3(0, 1f, 0.2f), curl = 0.1f };
                    a = Mix(a, shrug, react);
                    b = Mix(b, shrug, react);
                }
            }
            float rate = g == Gesture.Transform ? 30f : 10f;
            float shrugDeg = reaction == Reaction.Incorrect ? 10f * react : 0;
            if (lead != null) Solve(lead, a, Ease(rate, dt), breath, shrugDeg);
            if (other == null) return;
            if (g == Gesture.Aim && lead != null && lead.hand)
            {
                // The free hand cups the gadget arm's forearm, if it can reach without folding.
                Vector3 grip = Vector3.Lerp(lead.lower.position, lead.hand.position, 0.6f);
                float d = Vector3.Distance(other.upper.position, grip) / other.length;
                bool reachable = d < 0.97f && d > 0.7f;
                if (reachable)
                    b = new Goal { dir = frameT.InverseTransformDirection(grip - other.upper.position).normalized, reach = d, hint = new Vector3(0.6f * other.side, -1f, 0), palm = frameT.InverseTransformDirection(lead.hand.position - other.upper.position).normalized, curl = 0.55f, aim = 0 };
                else b.dir = new Vector3(0.35f, -0.85f, 0.1f);
                if (reachable) { SolveFrame(other, b, Ease(rate, dt), breath, shrugDeg); return; }
            }
            Solve(other, b, Ease(rate, dt), breath, shrugDeg);
        }

        // A talking beat: hands forward and open, palms up, rising with the voice.
        static Goal Open(float x, float y, float z, float lift) =>
            new Goal { dir = new Vector3(x, y + lift, z), reach = 0.82f, hint = new Vector3(0.6f, -0.8f, -0.1f), palm = new Vector3(-0.4f, 0.9f, 0.1f), curl = 0.2f };

        // Pointed at a world position, written in the table convention (x outward) so it mixes with table goals. The
        // direction never crosses more than 25 degrees over his chest, even while his body is still turning toward it.
        Goal Aimed(Arm arm, Vector3 target, float reach, float curl, float pointing)
        {
            var g = new Goal { dir = new Vector3(0.2f, 0.1f, 1f), reach = reach, hint = new Vector3(0.5f, -1f, -0.2f), palm = new Vector3(0, -1f, 0), curl = curl, pointing = pointing, aim = 1, aimAt = target };
            if (arm == null) return g;
            Vector3 d = frameT.InverseTransformDirection(target - arm.upper.position);
            if (d.sqrMagnitude < 1e-10f) return g;
            d.Normalize();
            d.x *= arm.side; // outward positive
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, flat = new Vector2(d.x, d.z).magnitude;
            if (yaw < -25f) { d.x = -Mathf.Sin(25f * Mathf.Deg2Rad) * flat; d.z = Mathf.Cos(25f * Mathf.Deg2Rad) * flat; }
            g.dir = d;
            return g;
        }

        static Goal Mix(Goal a, Goal b, float k)
        {
            if (k <= 0) return a;
            if (k >= 1) return b;
            return new Goal
            {
                dir = Vector3.Lerp(a.dir, b.dir, k), reach = Mathf.Lerp(a.reach, b.reach, k), hint = Vector3.Lerp(a.hint, b.hint, k),
                palm = Vector3.Lerp(a.palm, b.palm, k), curl = Mathf.Lerp(a.curl, b.curl, k), pointing = Mathf.Lerp(a.pointing, b.pointing, k),
                aim = Mathf.Lerp(a.aim, b.aim, k), aimAt = b.aim > 0 ? b.aimAt : a.aimAt, wave = Mathf.Lerp(a.wave, b.wave, k), droop = Mathf.Lerp(a.droop, b.droop, k),
            };
        }

        // Table goal (x = outward) -> frame space for this arm.
        void Solve(Arm arm, Goal g, float k, float breath, float shrug)
        {
            g.dir.x *= arm.side;
            g.hint.x *= arm.side;
            g.palm.x *= arm.side;
            SolveFrame(arm, g, k, breath, shrug);
        }

        void SolveFrame(Arm arm, Goal g, float k, float breath, float shrug)
        {
            if (g.dir.sqrMagnitude < 1e-6f) g.dir = new Vector3(0.3f * arm.side, -0.92f, 0.16f);
            if (!arm.primed) { arm.now = g; arm.primed = true; }
            var n = arm.now;
            n.dir = Vector3.Slerp(n.dir.normalized, g.dir.normalized, k);
            n.reach = Mathf.Lerp(n.reach, Mathf.Clamp(g.reach, 0.7f, 0.98f), k);
            n.hint = Vector3.Lerp(n.hint, g.hint, k);
            n.palm = Vector3.Lerp(n.palm, g.palm, k);
            n.curl = Mathf.Lerp(n.curl, g.curl, k);
            n.pointing = Mathf.Lerp(n.pointing, g.pointing, k);
            n.aim = Mathf.Lerp(n.aim, g.aim, k);
            n.aimAt = g.aim > 0 ? g.aimAt : n.aimAt;
            n.wave = Mathf.Lerp(n.wave, g.wave, k);
            n.droop = Mathf.Lerp(n.droop, g.droop, k);
            arm.now = n;

            // The collarbone follows a raised arm a little and lifts with the breath (and the shrug).
            if (arm.clav)
            {
                Vector3 bone = arm.upper.position - arm.clav.position;
                float lift = Mathf.Max(0, n.dir.y) * 8f + breath * 0.8f + shrug;
                arm.clav.rotation = Quaternion.FromToRotation(bone, Vector3.RotateTowards(bone, frameT.up, lift * Mathf.Deg2Rad, 0)) * arm.clav.rotation;
            }
            Vector3 shoulder = arm.upper.position;
            Vector3 handAt = shoulder + frameT.TransformDirection(n.dir.normalized) * (arm.length * n.reach);
            Vector3 elbowAt = shoulder + frameT.TransformDirection(n.hint) * arm.length;
            Limb.Reach(arm.upper, arm.lower, arm.hand, handAt, elbowAt);
            if (n.palm.sqrMagnitude > 1e-4f) Limb.Twist(arm.lower, arm.hand, arm.palmAxis, frameT.TransformDirection(n.palm), 0.8f);

            // Wrist: aimed hands (and the gadget, which hangs off the right wrist along the hand) turn to the target;
            // otherwise a relaxed flex toward the palm, and the greeting wave.
            Vector3 along = arm.hand.TransformDirection(arm.handAxis), palmW = arm.hand.TransformDirection(arm.palmAxis);
            if (n.aim > 0.01f)
            {
                Vector3 want = Vector3.RotateTowards(along, n.aimAt - arm.hand.position, 35f * Mathf.Deg2Rad * n.aim, 0);
                arm.hand.rotation = Quaternion.FromToRotation(along, want) * arm.hand.rotation;
            }
            else if (Mathf.Abs(n.droop) > 0.1f)
            {
                Vector3 want = Vector3.RotateTowards(along, n.droop > 0 ? palmW : -palmW, Mathf.Abs(n.droop) * Mathf.Deg2Rad, 0);
                arm.hand.rotation = Quaternion.FromToRotation(along, want) * arm.hand.rotation;
            }
            if (n.wave > 0.1f) arm.hand.rotation = Quaternion.AngleAxis(Mathf.Sin(clock * 9f) * n.wave, palmW) * arm.hand.rotation;

            // Fingers curl toward the palm; the index stays straight when pointing; the thumb curls less.
            if (arm.curlAxis == null) return;
            for (int f = 0; f < arm.fingers.Length; f++)
            {
                float c = n.curl * (f == arm.thumb ? 0.45f : f == arm.index ? 1 - n.pointing : 1f);
                if (c < 0.01f) continue;
                Vector3 axis = arm.hand.TransformDirection(arm.curlAxis[f]);
                var chain = arm.fingers[f];
                for (int j = 0; j < chain.Length; j++)
                    chain[j].rotation = Quaternion.AngleAxis(c * (j == 0 ? 55f : 70f), axis) * chain[j].rotation;
            }
        }

        // Fun tap: a barrel roll with sparks and a chirp, only while he is idle and silent.
        void FunTap()
        {
            var pointer = Pointer.current;
            var cam = Camera.main;
            if (pointer == null || !cam || !pointer.press.wasPressedThisFrame || Time.unscaledTime < nextRoll) return;
            if (Effective != Gesture.Idle || loudness > 0.05f || SubtitleTrack.CurrentLine) return;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            Vector3 screen = cam.WorldToScreenPoint(transform.position);
            float radius = Mathf.Max(60f, (cam.WorldToScreenPoint(transform.position + cam.transform.up * 0.3f) - screen).magnitude);
            if (screen.z <= 0 || ((Vector2)screen - pointer.position.ReadValue()).magnitude > radius) return;
            nextRoll = Time.unscaledTime + 2f;
            if (!ReducedMotion.On) rollTime = 0;
            if (sparks) sparks.Emit(28);
            if (sfx && chirp) sfx.PlayOneShot(chirp);
        }
    }

    // Two-bone limb helpers in world space, independent of how a rig's bone axes are oriented (Wattson and Ohm).
    public static class Limb
    {
        // Bends upper -> lower -> end so 'end' reaches 'target' (clamped to the limb's length), the middle joint pointing
        // toward 'hint'. Only rotations are written.
        public static void Reach(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 hint)
        {
            float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, end.position);
            if (a < 1e-6f || b < 1e-6f) return;
            Vector3 to = target - upper.position;
            float d = Mathf.Clamp(to.magnitude, Mathf.Max(Mathf.Abs(a - b), 0.02f * (a + b)), 0.999f * (a + b));
            Vector3 dir = to.sqrMagnitude > 1e-12f ? to.normalized : (lower.position - upper.position).normalized;
            float cos = (a * a + d * d - b * b) / (2 * a * d);
            float angle = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 axis = Vector3.Cross(dir, hint - upper.position);
            if (axis.sqrMagnitude < 1e-12f) axis = Vector3.Cross(dir, Vector3.up);
            if (axis.sqrMagnitude < 1e-12f) axis = Vector3.Cross(dir, Vector3.right);
            Vector3 elbow = upper.position + Quaternion.AngleAxis(angle, axis.normalized) * dir * a;
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(end.position - lower.position, upper.position + dir * d - lower.position) * lower.rotation;
        }

        // Turns the end bone's palm (palmLocal, end-local) toward 'normal' about the forearm axis, shared 60/40 between the
        // forearm and the wrist so neither twists far.
        public static void Twist(Transform lower, Transform end, Vector3 palmLocal, Vector3 normal, float weight = 1f, float limit = 120f)
        {
            Vector3 axis = end.position - lower.position;
            if (axis.sqrMagnitude < 1e-12f) return;
            axis.Normalize();
            Vector3 now = Vector3.ProjectOnPlane(end.TransformDirection(palmLocal), axis), want = Vector3.ProjectOnPlane(normal, axis);
            if (now.sqrMagnitude < 1e-4f || want.sqrMagnitude < 1e-4f) return;
            float angle = Mathf.Clamp(Vector3.SignedAngle(now, want, axis), -limit, limit) * weight;
            lower.Rotate(axis, angle * 0.6f, Space.World);
            end.Rotate(axis, angle * 0.4f, Space.World);
        }
    }
}
