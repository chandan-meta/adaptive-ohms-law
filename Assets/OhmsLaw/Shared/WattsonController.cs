using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

namespace OhmsLaw
{
    // Wattson's body: spring flight along a spline, procedural gestures, and talking driven by audio loudness.
    public class WattsonController : MonoBehaviour
    {
        public enum Gesture { Idle, Greet, Point, Flick, Reach, RaiseGadget, Aim, Listening, Thinking, Talking, Transform } // append only: clips store the int
        // Transform: anticipation (gathers, gadget drawn back), the action (gadget thrust at the target) TransformActionDelay
        // seconds in, then a settle. Builders time the transformation VFX cues to the action moment.
        public const float TransformActionDelay = 0.55f, TransformSettleSeconds = 1.1f;

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
        const float PointTurn = 30f; // degrees he turns toward what he points at
        readonly float[] samples = new float[256];
        MaterialPropertyBlock block;
        Gesture? agentGesture;
        Transform agentLook;
        Vector3 home, velocity, visualHome, rightArm, leftArm, restRight, restLeft;
        Quaternion restRightRotation, restLeftRotation;
        float loudness, rollTime = -1, nextRoll, pose;
        bool pointLeft;
        AudioClip scriptedClip, unreadableClip;
        float scriptedTime;
        int scriptedFrame = -10;

        public Gesture Effective => agentGesture ?? gesture;
        public float Loudness => loudness;

        void OnEnable() { Active = this; }
        void OnDisable() { if (Active == this) Active = null; }

        void Awake()
        {
            block = new MaterialPropertyBlock();
            home = transform.position;
            if (path) transform.position = path.EvaluatePosition(pathT);
            if (visual) visualHome = visual.localPosition;
            if (rightShoulder && rightElbow)
            {
                restRightRotation = rightShoulder.localRotation;
                rightArm = restRight = transform.InverseTransformDirection((rightElbow.position - rightShoulder.position).normalized);
            }
            if (leftShoulder && leftElbow)
            {
                restLeftRotation = leftShoulder.localRotation;
                leftArm = restLeft = transform.InverseTransformDirection((leftElbow.position - leftShoulder.position).normalized);
            }
        }

        // Ask Wattson takes over the pose while the cinematic is paused; pass null to hand it back to the Timeline.
        public void SetAgentPose(Gesture? agentPose, Transform look)
        {
            agentGesture = agentPose;
            agentLook = look;
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
            transform.position = home;
            velocity = Vector3.zero;
        }

        // Called by the subtitle track every frame a scripted line is on screen.
        public void ScriptedSpeech(AudioClip clip, float time)
        {
            scriptedClip = clip;
            scriptedTime = time;
            scriptedFrame = Time.frameCount;
        }

        void Update()
        {
            float dt = Time.deltaTime * motionScale;
            Fly(dt);
            Talk(dt);
            FunTap();
            if (gadget)
            {
                bool thinking = Effective == Gesture.Thinking;
                gadget.gameObject.SetActive(gadgetOut || thinking);
                if (thinking) gadget.Rotate(0, 0, 540 * Time.deltaTime, Space.Self);
            }
        }

        void Fly(float dt)
        {
            Vector3 target = path ? (Vector3)path.EvaluatePosition(pathT) : home;
            velocity += (target - transform.position) * (spring * dt);
            velocity *= Mathf.Exp(-damping * dt);
            transform.position += velocity * dt;

            Transform look = agentLook ? agentLook : lookTarget;
            Vector3 forward = look ? look.position - transform.position : transform.forward;
            // Pointing, he turns part of the way toward the target so the arm reads in three-quarter view instead of
            // folding across his body; the turn eases back with the next gesture.
            if (Effective == Gesture.Point && pointTarget && !agentLook)
            {
                Vector3 flat = Vector3.ProjectOnPlane(forward, Vector3.up), aim = Vector3.ProjectOnPlane(pointTarget.position - transform.position, Vector3.up);
                if (flat.sqrMagnitude > 1e-4f && aim.sqrMagnitude > 1e-4f) forward = Vector3.RotateTowards(flat, aim, PointTurn * Mathf.Deg2Rad, 0f) + Vector3.up * forward.y;
            }
            if (velocity.magnitude > 1.2f) forward = Vector3.Lerp(forward.normalized, velocity.normalized, 0.7f);
            forward.y *= 0.35f;
            if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
            float roll = -Vector3.Dot(velocity, transform.right) * bank;
            float pitch = Mathf.Clamp(Vector3.Dot(velocity, transform.forward) * 6f, -10f, 22f);
            var rotation = Quaternion.LookRotation(forward.normalized, Vector3.up) * Quaternion.Euler(pitch, 0, roll);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1 - Mathf.Exp(-7f * dt));

            if (!visual) return;
            float barrel = 0;
            if (rollTime >= 0)
            {
                rollTime += Time.deltaTime / 0.55f;
                barrel = Mathf.SmoothStep(0, 360, rollTime);
                if (rollTime >= 1) rollTime = -1;
            }
            var g = Effective;
            pose = Mathf.MoveTowards(pose, g == Gesture.Idle ? 0 : 1, dt * 3f);
            float bow = g == Gesture.Greet ? 16f : g == Gesture.Thinking ? -10f : 0;
            float tilt = g == Gesture.Listening ? 12f : 0;
            float sway = g == Gesture.Thinking ? Mathf.Sin(Time.time * 1.3f) * 14f : 0;
            visual.localPosition = visualHome + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            visual.localRotation = Quaternion.Slerp(visual.localRotation,
                Quaternion.Euler(bow * pose, sway, tilt * pose + barrel), rollTime >= 0 ? 1 : 1 - Mathf.Exp(-6f * dt));
        }

        void Talk(float dt)
        {
            float raw = 0;
            if (scriptedFrame >= Time.frameCount - 1) raw = ClipLoudness();
            else if (liveVoice && liveVoice.isPlaying) { liveVoice.GetOutputData(samples, 0); raw = Rms(); }
            else if (agentGesture == Gesture.Talking) raw = SilentTalk(); // answer shown without a voice clip
            loudness = Mathf.Lerp(loudness, Mathf.Clamp01(raw * 6f), 1 - Mathf.Exp(-18f * Time.deltaTime));

            if (mouth) mouth.SetBlendShapeWeight(mouthShape, loudness * 100f);
            if (glowRenderers == null) return;
            // He always glows a little, being made of electricity, and brightens as he speaks.
            block.SetColor(Emission, glowColor * (0.45f + loudness * 0.8f));
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

        static float SilentTalk() => 0.06f + 0.08f * Mathf.PerlinNoise(Time.time * 9f, 0);

        float Rms()
        {
            float sum = 0;
            foreach (float s in samples) sum += s * s;
            return Mathf.Sqrt(sum / samples.Length);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime * motionScale;
            var g = Effective;
            if (g == Gesture.Idle && loudness > 0.05f) g = Gesture.Talking;
            Vector3 right = restRight, left = restLeft;
            float beat = Mathf.Sin(Time.time * 7f) * 0.12f * Mathf.Clamp01(loudness * 3f);
            switch (g)
            {
                case Gesture.Greet: right = new Vector3(0.35f, 0.9f, 0.35f); break;
                case Gesture.Point:
                    // The arm on the target's side points (facing the lens, a screen-right target is on his left); with the
                    // gadget out the right hand holds it, so that arm aims. The side is taken from where he looks, not his
                    // turned body, and flips only past a dead zone, so arms never swap for a target near his line of sight.
                    if (!pointTarget) pointLeft = false;
                    else
                    {
                        Transform look = agentLook ? agentLook : lookTarget;
                        Vector3 ahead = Vector3.ProjectOnPlane(look ? look.position - transform.position : transform.forward, Vector3.up).normalized;
                        float side = Vector3.Cross(ahead, Vector3.ProjectOnPlane(pointTarget.position - transform.position, Vector3.up).normalized).y;
                        if (Mathf.Abs(side) > 0.1f) pointLeft = side < 0;
                    }
                    if (pointLeft && !gadgetOut) left = ToTarget(leftShoulder, new Vector3(-0.2f, 0.2f, 1f));
                    else right = ToTarget(rightShoulder, new Vector3(0.2f, 0.2f, 1f));
                    break;
                case Gesture.Flick: right = new Vector3(0.8f, 0.55f, 0.3f); left = new Vector3(-0.8f, 0.55f, 0.3f); break;
                case Gesture.Reach: right = new Vector3(0.3f, -0.3f, -0.9f); break;
                case Gesture.RaiseGadget: right = new Vector3(0.25f, 1f, 0.3f); break;
                case Gesture.Aim: right = ToTarget(rightShoulder, Vector3.forward); left = Vector3.Lerp(restLeft, right, 0.5f); break;
                case Gesture.Listening: left = new Vector3(-0.5f, 0.8f, 0.2f); break;
                case Gesture.Thinking: right = new Vector3(0.3f, 0.45f, 0.85f); break;
                case Gesture.Talking:
                    right = restRight + new Vector3(0.3f, 0.3f + beat, 0.45f);
                    left = restLeft + new Vector3(-0.3f, 0.3f - beat, 0.45f);
                    break;
            }
            Pose(rightShoulder, rightElbow, restRightRotation, ref rightArm, right, dt);
            Pose(leftShoulder, leftElbow, restLeftRotation, ref leftArm, left, dt);
        }

        Vector3 ToTarget(Transform from, Vector3 fallback) =>
            pointTarget && from ? transform.InverseTransformDirection(pointTarget.position - from.position) : fallback;

        // Swings the whole arm from its rest pose so the shoulder-to-elbow bone points along 'wanted' (root space).
        void Pose(Transform shoulder, Transform elbow, Quaternion rest, ref Vector3 current, Vector3 wanted, float dt)
        {
            if (!shoulder || !elbow) return;
            current = Vector3.Slerp(current, wanted.normalized, 1 - Mathf.Exp(-9f * dt));
            shoulder.localRotation = rest;
            shoulder.rotation = Quaternion.FromToRotation(elbow.position - shoulder.position, transform.TransformDirection(current)) * shoulder.rotation;
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
            rollTime = 0;
            if (sparks) sparks.Emit(28);
            if (sfx && chirp) sfx.PlayOneShot(chirp);
        }
    }
}
