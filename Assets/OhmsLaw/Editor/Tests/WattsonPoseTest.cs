using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Poses Wattson's real prefab (Wattson_Scene04) in edit mode through WattsonController.Simulate and checks the
    // performance: joint ranges his skin survives, the pointing arm and body turn (no arm folded across his chest), the
    // Transform's anticipation / action / settle timing, reactions, eased flight, blinks, no NaN. Renders a few pose
    // previews to Logs/Blockout/wattson_poses/. A second menu checks Georg Ohm's poses through OhmWorker.Simulate on a
    // stand-in of Scene 4's desk: never a T-pose, fingers curled, no hand or finger point below the desk in any mode, palms
    // down on the page, the lift-over arc when a hand changes props, a frozen pose that holds, and Attend().
    // One PASS / FAIL line per check in the Console (and Logs/ohm-build.txt through Tools/unity_request.sh).
    public static class WattsonPoseTest
    {
        const string Prefab = "Assets/OhmsLaw/Scene04/Prefabs/Wattson_Scene04.prefab";
        const string OhmPrefab = "Assets/OhmsLaw/Scene04/Prefabs/Ohm_Scene04.prefab";
        const string Shots = "Logs/Blockout/wattson_poses";
        const float Dt = 1f / 60f;
        static int passed, failed;

        static void Check(string name, bool ok, string detail = "")
        {
            if (ok) { passed++; Debug.Log("PASS " + name + (detail != "" ? " (" + detail + ")" : "")); }
            else { failed++; Debug.LogError("FAIL " + name + (detail != "" ? " — " + detail : "")); }
        }

        // ------------------------------------------------------------------ Wattson

        class Rig
        {
            public WattsonController w;
            public Transform root, chest, head, eyeL, eyeR;
            public Transform rSh, rEl, rWr, lSh, lEl, lWr, rTh, rKn, rAn, lTh, lKn, lAn;
            public Quaternion headRel, eyeRel;
            public Vector3 eyeScale = Vector3.one;
            public float armLength;
            public Transform cam, target, home, far;
        }

        [MenuItem("Ohm's Law/Tests/Wattson Pose")]
        public static void Run()
        {
            passed = failed = 0;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            Check("Wattson_Scene04 prefab loads", prefab);
            if (!prefab) { Summary("Wattson Pose"); return; }
            var preview = new PreviewRenderUtility();
            try
            {
                var go = preview.InstantiatePrefabInScene(prefab);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var r = Bind(go);
                Check("controller and rig bones found", r.w && r.chest && r.head && r.rWr && r.lWr && r.rKn && r.lKn,
                    "missing: " + Missing(r));
                if (!r.w || !r.rWr || !r.lWr) return;
                r.cam = Marker(preview, "Test Camera", new Vector3(0, 0.1f, 1.8f));
                r.home = Marker(preview, "Test Home", Vector3.zero);
                // 43 degrees to his left (his left is -x when he faces +z): the Scene 8 transformation case.
                r.target = Marker(preview, "Test Target", Quaternion.AngleAxis(-43f, Vector3.up) * Vector3.forward * 1.2f);
                r.far = Marker(preview, "Test Far", new Vector3(2f, 0.3f, 0.5f));
                r.w.lookTarget = r.cam;
                r.w.path = null;
                r.w.Warp(r.home);

                Idle(r, preview);
                PointBare(r, preview);
                PointGadget(r, preview);
                TransformGesture(r, preview);
                Reactions(r, preview);
                EveryGesture(r, preview);
                Flight(r);
                SnapGesture(r, preview);
                Guided(r);
                Steady(r); // last: ScriptedSpeech keeps him "talking" for the rest of an edit-mode run (the frame count stands still)
            }
            catch (System.Exception e) { Check("no exception", false, e.ToString()); }
            finally { preview.Cleanup(); }
            Summary("Wattson Pose");
        }

        static void Summary(string name)
        {
            string summary = $"{name}: {passed} passed, {failed} failed";
            if (failed == 0) Debug.Log("PASS " + summary); else Debug.LogError("FAIL " + summary);
        }

        static Transform Marker(PreviewRenderUtility preview, string name, Vector3 at)
        {
            var m = new GameObject(name);
            preview.AddSingleGO(m);
            m.transform.position = at;
            return m.transform;
        }

        static Rig Bind(GameObject go)
        {
            var all = go.GetComponentsInChildren<Transform>(true);
            Transform B(string n) { foreach (var t in all) if (t.name == n) return t; return null; }
            var r = new Rig
            {
                w = go.GetComponent<WattsonController>(), root = go.transform, chest = B("Chest_jnt"), head = B("head_jnt"), eyeL = B("eye_left"), eyeR = B("eye_right"),
                rSh = B("right_Sholder_jnt"), rEl = B("right_elbow_jnt"), rWr = B("right_wrist_jnt"),
                lSh = B("left_Sholder_jnt"), lEl = B("left_elbow_jnt"), lWr = B("left_wrist_jnt"),
                rTh = B("right_thigh_jnt"), rKn = B("right_knee_jnt"), rAn = B("right_ankle_jnt"),
                lTh = B("left_thigh_jnt"), lKn = B("left_knee_jnt"), lAn = B("left_ankle_jnt"),
            };
            if (r.chest && r.head) r.headRel = Quaternion.Inverse(r.chest.rotation) * r.head.rotation;
            if (r.head && r.eyeL) { r.eyeRel = Quaternion.Inverse(r.head.rotation) * r.eyeL.rotation; r.eyeScale = r.eyeL.localScale; }
            if (r.rSh && r.rEl && r.rWr) r.armLength = Vector3.Distance(r.rSh.position, r.rEl.position) + Vector3.Distance(r.rEl.position, r.rWr.position);
            return r;
        }

        static string Missing(Rig r)
        {
            var names = new List<string>();
            foreach (var f in typeof(Rig).GetFields())
                if (f.FieldType == typeof(Transform) && !(Object)(f.GetValue(r) as Transform) && f.Name != "cam" && f.Name != "target" && f.Name != "home" && f.Name != "far") names.Add(f.Name);
            if (!r.w) names.Add("WattsonController");
            return string.Join(", ", names);
        }

        static void Play(Rig r, WattsonController.Gesture g, bool gadget, Transform point, float seconds)
        {
            r.w.gesture = g;
            r.w.gadgetOut = gadget;
            r.w.pointTarget = point;
            for (float t = 0; t < seconds; t += Dt) r.w.Simulate(Dt);
        }

        static float Bend(Transform a, Transform b, Transform c) => Vector3.Angle(b.position - a.position, c.position - b.position);

        // Joint ranges from the skin tests (scratch/rig): head <= 20/12 (checked at 28 total), chest <= 10, knees <= 55,
        // elbows <= ~95 (checked at 110 to allow the IK clamp).
        static void Limits(Rig r, string when)
        {
            float elbowR = Bend(r.rSh, r.rEl, r.rWr), elbowL = Bend(r.lSh, r.lEl, r.lWr);
            float kneeR = r.rKn ? Bend(r.rTh, r.rKn, r.rAn) : 30, kneeL = r.lKn ? Bend(r.lTh, r.lKn, r.lAn) : 30;
            float headTurn = r.head && r.chest ? Quaternion.Angle(Quaternion.Inverse(r.chest.rotation) * r.head.rotation, r.headRel) : 0;
            float eyeTurn = r.eyeL && r.head ? Quaternion.Angle(Quaternion.Inverse(r.head.rotation) * r.eyeL.rotation, r.eyeRel) : 0;
            bool finite = Finite(r.root);
            Check($"{when}: no NaN in any bone", finite);
            Check($"{when}: elbows within 0..110 degrees", elbowR <= 110 && elbowL <= 110, $"R {elbowR:0} L {elbowL:0}");
            Check($"{when}: knees bent 5..62 degrees (relaxed, never straight)", kneeR >= 5 && kneeR <= 62 && kneeL >= 5 && kneeL <= 62, $"R {kneeR:0} L {kneeL:0}");
            Check($"{when}: head turn within 28 degrees of the chest (yaw 20 + pitch 12 + a reaction tilt)", headTurn <= 28, $"{headTurn:0.0}");
            Check($"{when}: eyes within 15 degrees of the head", eyeTurn <= 15, $"{eyeTurn:0.0}");
        }

        static bool Finite(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var q = t.localRotation;
                if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w) || float.IsNaN(t.position.x)) return false;
            }
            return true;
        }

        static Vector3 Local(Rig r, Vector3 world) => r.root.InverseTransformPoint(world);

        static void Idle(Rig r, PreviewRenderUtility preview)
        {
            Play(r, WattsonController.Gesture.Idle, false, null, 3f);
            Limits(r, "Idle");
            float elbow = Bend(r.rSh, r.rEl, r.rWr);
            Check("Idle: elbows softly bent (not a stick arm)", elbow >= 12, $"{elbow:0}");
            Check("Idle: hands hang below the shoulders", r.rWr.position.y < r.rSh.position.y && r.lWr.position.y < r.lSh.position.y);
            if (r.rKn)
            {
                // Thighs swing forward of straight down: the floating pose, not the bind's standing legs.
                Vector3 thigh = r.root.InverseTransformDirection(r.rKn.position - r.rTh.position).normalized;
                Check("Idle: thighs swing forward while floating", thigh.z > 0.12f, $"thigh dir z {thigh.z:0.00}");
            }
            // Life: the hand keeps moving a little (breathing, sway, weight shift) over three seconds.
            Vector3 a = Local(r, r.rWr.position);
            float travel = 0;
            for (float t = 0; t < 3f; t += Dt) { r.w.Simulate(Dt); var b = Local(r, r.rWr.position); travel += (b - a).magnitude; a = b; }
            Check("Idle: alive (the hand drifts with breath and sway)", travel > 0.02f * r.armLength, $"{travel / r.armLength:0.000} arm lengths");
            // Blink within 8 s.
            float minScale = 1;
            Vector3 rest = r.eyeScale;
            for (float t = 0; t < 8f; t += Dt)
            {
                r.w.Simulate(Dt);
                if (r.eyeL) minScale = Mathf.Min(minScale, Mathf.Min(r.eyeL.localScale.x / rest.x, Mathf.Min(r.eyeL.localScale.y / rest.y, r.eyeL.localScale.z / rest.z)));
            }
            Check("Idle: blinks (the pupils squash) within 8 s", !r.eyeL || minScale < 0.5f, $"min scale {minScale:0.00}");
            Shot(r, preview, "01_idle");
        }

        static void PointBare(Rig r, PreviewRenderUtility preview)
        {
            Play(r, WattsonController.Gesture.Point, false, r.target, 1.5f);
            Limits(r, "Point (bare hand, target on his left)");
            Check("Point bare: the LEFT arm points (the target's side)", r.w.PointingWithLeft);
            Vector3 toTarget = (r.target.position - r.lSh.position).normalized;
            float aim = Vector3.Dot((r.lWr.position - r.lSh.position).normalized, toTarget);
            Check("Point bare: the left arm aims at the target", aim > 0.85f, $"dot {aim:0.00}");
            Check("Point bare: the right hand stays down", r.rWr.position.y < r.rSh.position.y);
            Shot(r, preview, "02_point_bare_left");
        }

        static void PointGadget(Rig r, PreviewRenderUtility preview)
        {
            Play(r, WattsonController.Gesture.Idle, false, null, 0.5f);
            var turns = new List<float>();
            Quaternion last = r.root.rotation;
            r.w.gesture = WattsonController.Gesture.Point;
            r.w.gadgetOut = true;
            r.w.pointTarget = r.target;
            for (float t = 0; t < 2f; t += Dt) { r.w.Simulate(Dt); turns.Add(Quaternion.Angle(last, r.root.rotation)); last = r.root.rotation; }
            Limits(r, "Point (gadget, target 43 degrees to his left)");
            Check("Point gadget: the RIGHT (gadget) arm points", !r.w.PointingWithLeft);
            float maxStep = 0;
            foreach (var s in turns) maxStep = Mathf.Max(maxStep, s);
            Check("Point gadget: the body turns smoothly (no snap)", maxStep < 6f, $"max {maxStep:0.0} deg/frame");
            Vector3 face = Vector3.ProjectOnPlane(r.root.forward, Vector3.up), cam = Vector3.ProjectOnPlane(r.cam.position - r.root.position, Vector3.up);
            float turned = Vector3.SignedAngle(cam, face, Vector3.up);
            Check("Point gadget: he turns toward the target (left) so the right arm can reach it", turned < -18f, $"{turned:0} deg");
            // No fold: the right hand stays on his right half (x >= small negative) in his own frame.
            float x = Local(r, r.rWr.position).x - Local(r, r.chest ? r.chest.position : r.root.position).x;
            Check("Point gadget: the right arm does not fold across his chest", x > -0.3f * r.armLength / r.root.lossyScale.x, $"wrist x {x:0.000} (local)");
            float aim = Vector3.Dot((r.rWr.position - r.rSh.position).normalized, (r.target.position - r.rSh.position).normalized);
            Check("Point gadget: the right arm aims at the target", aim > 0.85f, $"dot {aim:0.00}");
            if (r.w.gadget)
            {
                float g = Vector3.Dot(r.w.gadget.forward, (r.target.position - r.w.gadget.position).normalized);
                Check("Point gadget: the Ohm-niscope points at the target", g > 0.8f, $"dot {g:0.00}");
            }
            Shot(r, preview, "03_point_gadget_left");
        }

        static void TransformGesture(Rig r, PreviewRenderUtility preview)
        {
            Play(r, WattsonController.Gesture.Point, true, r.target, 1.5f); // he is already turned, as in Scene 8
            r.w.gesture = WattsonController.Gesture.Transform;
            float t = 0, startDist = 0, windDist = 0, bestSpeedAt = -1, bestSpeed = 0, prev = 0;
            float hitDist = 0, hitAim = 0, endSpeed = 0;
            Vector3 lastLocal = Vector3.zero;
            Transform frame = r.w.visual ? r.w.visual : r.root;
            bool shotWind = false, shotHit = false;
            for (int i = 0; t < 1.6f; i++)
            {
                r.w.Simulate(Dt);
                t += Dt;
                float d = Vector3.Distance(r.rWr.position, r.target.position);
                if (i == 0) { startDist = d; prev = d; }
                float approach = (prev - d) / Dt;
                if (approach > bestSpeed) { bestSpeed = approach; bestSpeedAt = t; }
                prev = d;
                if (!shotWind && t >= 0.4f) { windDist = d; shotWind = true; Shot(r, preview, "04_transform_anticipation_0.40s"); }
                if (!shotHit && t >= 0.62f)
                {
                    shotHit = true;
                    hitDist = Vector3.Distance(r.rSh.position, r.rWr.position) / r.armLength;
                    hitAim = Vector3.Dot((r.rWr.position - r.rSh.position).normalized, (r.target.position - r.rSh.position).normalized);
                    Limits(r, "Transform action");
                    Shot(r, preview, "05_transform_action_0.62s");
                }
                // Hand relative to its shoulder in his body frame (the bob and sway are not "unsettled").
                var local = frame.InverseTransformDirection(r.rWr.position - r.rSh.position) / r.armLength;
                if (t > 1.45f) endSpeed = Mathf.Max(endSpeed, (local - lastLocal).magnitude / Dt);
                lastLocal = local;
            }
            Check("Transform: anticipation draws the gadget back (farther from the target at 0.40 s)", windDist > startDist + 0.03f * r.armLength, $"{startDist:0.000} -> {windDist:0.000}");
            Check("Transform: the thrust peaks at the action moment (0.55 s +- 0.1)", Mathf.Abs(bestSpeedAt - WattsonController.TransformActionDelay) <= 0.1f, $"fastest at {bestSpeedAt:0.00} s");
            Check("Transform: at 0.62 s the gadget arm is extended at the target", hitDist > 0.88f && hitAim > 0.85f, $"reach {hitDist:0.00}, dot {hitAim:0.00}");
            Check("Transform: settled by TransformSettleSeconds (+0.35 s)", endSpeed < 0.35f, $"hand speed {endSpeed:0.000} arm lengths/s");
            Shot(r, preview, "06_transform_settled");
        }

        static void Reactions(Rig r, PreviewRenderUtility preview)
        {
            Play(r, WattsonController.Gesture.Idle, false, null, 1f);
            r.w.React(WattsonController.Reaction.Correct);
            for (float t = 0; t < 0.45f; t += Dt) r.w.Simulate(Dt);
            bool up = r.rWr.position.y > r.rSh.position.y;
            Check("React Correct: a fist pump (hand above the shoulder)", up);
            Shot(r, preview, "07_react_correct");
            for (float t = 0; t < 1.6f; t += Dt) r.w.Simulate(Dt);
            Check("React Correct: back to the gesture after ReactionSeconds", r.rWr.position.y < r.rSh.position.y);
            r.w.React(WattsonController.Reaction.Incorrect);
            for (float t = 0; t < 0.5f; t += Dt) r.w.Simulate(Dt);
            Limits(r, "React Incorrect");
            Shot(r, preview, "08_react_incorrect");
            r.w.React(WattsonController.Reaction.Partial);
            for (float t = 0; t < 0.5f; t += Dt) r.w.Simulate(Dt);
            Limits(r, "React Partial");
            for (float t = 0; t < 1.5f; t += Dt) r.w.Simulate(Dt);
        }

        static void EveryGesture(Rig r, PreviewRenderUtility preview)
        {
            foreach (WattsonController.Gesture g in System.Enum.GetValues(typeof(WattsonController.Gesture)))
            {
                bool gadget = g == WattsonController.Gesture.Aim || g == WattsonController.Gesture.RaiseGadget || g == WattsonController.Gesture.Transform;
                Play(r, g, gadget, r.target, 1.5f);
                Limits(r, g.ToString());
                if (g == WattsonController.Gesture.Greet || g == WattsonController.Gesture.Thinking || g == WattsonController.Gesture.Aim || g == WattsonController.Gesture.Flick)
                    Shot(r, preview, "09_" + g.ToString().ToLowerInvariant());
            }
            // Talking (Ask Wattson without a voice clip): the mouth moves and the head stays in range. While the agent pose
            // holds he moves to the Ask spot beside the panel (viewport x 0.42, the middle band) and springs back to his
            // marker when it is handed back. The preview camera stands in for Camera.main.
            var askCam = preview.camera;
            askCam.transform.position = r.cam.position;
            askCam.transform.LookAt(r.root.position);
            askCam.aspect = 16f / 9f;
            r.w.SetAgentPose(WattsonController.Gesture.Talking, askCam.transform);
            Play(r, WattsonController.Gesture.Idle, false, null, 1.5f);
            Check("Talking: loudness drives the mouth", r.w.Loudness > 0.01f && (!r.w.mouth || r.w.mouth.GetBlendShapeWeight(r.w.mouthShape) > 1f), $"loudness {r.w.Loudness:0.00}");
            Limits(r, "Talking");
            Vector3 vp = askCam.WorldToViewportPoint(r.root.position);
            Check("Ask pose: he holds the spot in the free frame (viewport x 0.30 before the panel's projection shift, y 0.35-0.62)", Mathf.Abs(vp.x - 0.30f) < 0.04f && vp.y > 0.33f && vp.y < 0.64f, $"viewport {vp.x:0.00}, {vp.y:0.00}");
            r.w.SetAgentPose(null, null);
            Play(r, WattsonController.Gesture.Idle, false, null, 2f);
            float back = Vector3.Distance(r.root.position, r.home.position);
            Check("Ask pose handed back: he returns to his marker", back < 0.05f, $"{back:0.000} m off");
            askCam.ResetAspect();
        }

        static void Flight(Rig r)
        {
            Play(r, WattsonController.Gesture.Idle, false, null, 0.5f);
            r.w.Warp(r.home);
            r.w.MoveTo(r.far);
            float total = Vector3.Distance(r.home.position, r.far.position), arrived = -1, overshoot = 0, peak = 0, firstSpeed = -1;
            Vector3 last = r.root.position;
            for (float t = 0; t < 2.5f; t += Dt)
            {
                r.w.Simulate(Dt);
                float speed = (r.root.position - last).magnitude / Dt;
                last = r.root.position;
                if (firstSpeed < 0 && t >= 2 * Dt) firstSpeed = speed;
                peak = Mathf.Max(peak, speed);
                float along = Vector3.Dot(r.root.position - r.home.position, (r.far.position - r.home.position).normalized);
                overshoot = Mathf.Max(overshoot, along - total);
                if (arrived < 0 && Vector3.Distance(r.root.position, r.far.position) < 0.03f) arrived = t;
            }
            Check("Flight: eases in (slow first frames)", firstSpeed < 0.35f * peak, $"start {firstSpeed:0.00} m/s, peak {peak:0.00} m/s");
            Check("Flight: arrives within 1.6 s", arrived >= 0 && arrived <= 1.6f, $"{arrived:0.00} s for {total:0.0} m");
            Check("Flight: settles with a small overshoot (< 8 % of the move)", overshoot < 0.08f * total, $"{overshoot:0.000} m");
            r.w.Warp(r.home);
        }

        // The snap: refused within SnapCooldown of the last one, the free hand up in front of him for the click, back down
        // after SnapSeconds. Camera.main (the editor's open scene, if any) decides whether he is on screen; off screen every
        // snap is refused.
        static void SnapGesture(Rig r, PreviewRenderUtility preview)
        {
            Play(r, WattsonController.Gesture.Idle, false, null, 1f);
            var cam = Camera.main;
            Vector3 vp = cam ? cam.WorldToViewportPoint(r.root.position) : new Vector3(0.5f, 0.5f, 1f);
            bool visible = vp.z > 0 && vp.x > -0.02f && vp.x < 1.02f && vp.y > -0.02f && vp.y < 1.02f;
            bool first = r.w.Snap(), again = r.w.Snap();
            if (!visible) { Check("Snap: refused while he is off screen (Camera.main)", !first && !again); return; }
            Check("Snap: the first snap runs", first);
            Check("Snap: a second snap at once is refused", !again);
            for (float t = 0; t < WattsonController.SnapLead; t += Dt) r.w.Simulate(Dt);
            Check("Snap: the hand is up for the click (wrist near or above the shoulder)", r.rWr.position.y > r.rSh.position.y - 0.25f * r.armLength,
                $"wrist {(r.rWr.position.y - r.rSh.position.y) / r.armLength:0.00} arm lengths from the shoulder");
            Limits(r, "Snap click");
            Shot(r, preview, "10_snap_click");
            for (float t = 0; t < 0.4f; t += Dt) r.w.Simulate(Dt);
            Check("Snap: still refused before SnapCooldown (0.58 s after)", !r.w.Snap());
            for (float t = 0; t < WattsonController.SnapCooldown; t += Dt) r.w.Simulate(Dt);
            Check("Snap: the hand is back down after SnapSeconds", r.rWr.position.y < r.rSh.position.y);
            Check("Snap: allowed again after SnapCooldown", r.w.Snap());
            for (float t = 0; t < 1f; t += Dt) r.w.Simulate(Dt);
        }

        // WattsonGuide's overrides: guidePoint wins over the Timeline's point target (and makes him point, gadget away),
        // guideHome over his marker; cleared, he flies back to the marker.
        static void Guided(Rig r)
        {
            Play(r, WattsonController.Gesture.Idle, true, r.far, 0.5f);
            r.w.guidePoint = r.target;
            for (float t = 0; t < 1.5f; t += Dt) r.w.Simulate(Dt);
            Check("Guide: guidePoint makes the effective gesture Point", r.w.Effective == WattsonController.Gesture.Point, r.w.Effective.ToString());
            var arm = r.w.PointingWithLeft ? r.lSh : r.rSh;
            var wrist = r.w.PointingWithLeft ? r.lWr : r.rWr;
            float aim = Vector3.Dot((wrist.position - arm.position).normalized, (r.target.position - arm.position).normalized);
            Check("Guide: he aims at guidePoint, not the Timeline's target", aim > 0.85f, $"dot {aim:0.00}");
            Check("Guide: the gadget stays away while guided", !r.w.gadget || !r.w.gadget.gameObject.activeSelf);
            r.w.guideHome = r.far.position;
            for (float t = 0; t < 2f; t += Dt) r.w.Simulate(Dt);
            float there = Vector3.Distance(r.root.position, r.far.position);
            Check("Guide: guideHome wins over his marker", there < 0.05f, $"{there:0.000} m off");
            r.w.guidePoint = null;
            r.w.guideHome = null;
            r.w.pointTarget = null;
            r.w.gadgetOut = false;
            for (float t = 0; t < 2.5f; t += Dt) r.w.Simulate(Dt);
            float back = Vector3.Distance(r.root.position, r.home.position);
            Check("Guide cleared: he returns to his marker", back < 0.05f, $"{back:0.000} m off");
            Check("Guide cleared: the Timeline's gesture is back", r.w.Effective == WattsonController.Gesture.Idle);
        }

        // Steady: while a line plays the bob (and the rest of the idle life) quietens; breathing stays.
        static void Steady(Rig r)
        {
            static float Bob(Rig r, bool speaking)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                for (float t = 0; t < 4.5f; t += Dt)
                {
                    if (speaking) r.w.ScriptedSpeech(null, t);
                    r.w.Simulate(Dt);
                    float y = r.w.visual ? r.w.visual.localPosition.y : r.root.position.y;
                    lo = Mathf.Min(lo, y);
                    hi = Mathf.Max(hi, y);
                }
                return hi - lo;
            }
            Play(r, WattsonController.Gesture.Idle, false, null, 1.5f);
            float lively = Bob(r, false);
            Check("Steady: lively when nothing is said", r.w.Steadiness < 0.05f, $"steadiness {r.w.Steadiness:0.00}");
            for (float t = 0; t < 0.6f; t += Dt) { r.w.ScriptedSpeech(null, t); r.w.Simulate(Dt); }
            float quiet = Bob(r, true);
            Check("Steady: fully steady while a line plays", r.w.Steadiness > 0.95f, $"steadiness {r.w.Steadiness:0.00}");
            Check("Steady: the bob quietens while he speaks (under 40 %)", quiet < 0.4f * lively, $"{quiet:0.0000} vs {lively:0.0000}");
            Limits(r, "Steady (talking)");
        }

        // ------------------------------------------------------------------ previews

        static void Shot(Rig r, PreviewRenderUtility preview, string name)
        {
            try
            {
                Directory.CreateDirectory(Shots);
                var cam = preview.camera;
                cam.fieldOfView = 30f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 20f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.16f, 0.18f, 0.22f);
                // Three-quarter view from the front-left, where the camera marker is, framing his whole body.
                Vector3 centre = r.root.position + Vector3.up * 0.02f;
                cam.transform.position = centre + new Vector3(-0.55f, 0.15f, 1.75f);
                cam.transform.LookAt(centre);
                preview.lights[0].intensity = 1.2f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35, -30, 0);
                preview.lights[1].intensity = 0.6f;
                preview.BeginStaticPreview(new Rect(0, 0, 480, 480));
                preview.Render(true);
                var tex = preview.EndStaticPreview();
                File.WriteAllBytes(Path.Combine(Shots, name + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            catch (System.Exception e) { Debug.LogWarning("INFO pose preview " + name + " not rendered: " + e.Message); }
        }

        // ------------------------------------------------------------------ Ohm

        [MenuItem("Ohm's Law/Tests/Ohm Pose")]
        public static void RunOhm()
        {
            passed = failed = 0;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OhmPrefab);
            Check("Ohm_Scene04 prefab loads", prefab);
            if (!prefab) { Summary("Ohm Pose"); return; }
            var preview = new PreviewRenderUtility();
            try
            {
                var go = preview.InstantiatePrefabInScene(prefab);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var worker = go.GetComponent<OhmWorker>();
                Check("OhmWorker present", worker);
                if (!worker) return;
                var all = go.GetComponentsInChildren<Transform>(true);
                Transform B(string n) { foreach (var t in all) if (t.name.EndsWith(":" + n)) return t; return null; }
                Transform rArm = B("RightArm"), rHand = B("RightHand"), lHand = B("LeftHand"), rIndex1 = B("RightHandIndex1"), rIndex3 = B("RightHandIndex3"), rFore = B("RightForeArm"), lFore = B("LeftForeArm"), headBone = B("Head");
                Transform[][] rFingers = Fingers(B, "Right"), lFingers = Fingers(B, "Left");
                Check("Ohm: finger chains found on both hands", rFingers.Length > 0 && lFingers.Length > 0, $"R {rFingers.Length} L {lFingers.Length}");
                // The bind pose has the palms down: their normals in hand space, to read where the palms face once posed.
                Vector3 rPalm = rHand.InverseTransformDirection(Vector3.down), lPalm = lHand.InverseTransformDirection(Vector3.down);
                // Scene 4's desk around him (SceneBuilderBase.Lab): his feet 0.82 m below the top and 0.59 m behind its
                // centre; the notebook target (2 cm above the desk) 0.19 m to his right and 0.33 m ahead, the torsion knob
                // 0.19 m to his left, the wire post 0.10 m to his left and 0.43 m ahead. He looks at a point out in the room.
                Transform root = go.transform;
                Vector3 Desk(float right, float up, float ahead) => root.position + root.right * right + Vector3.up * up + root.forward * ahead;
                worker.notebook = Marker(preview, "Test Notebook Target", Desk(0.19f, 0.84f, 0.33f));
                worker.galvanometer = Marker(preview, "Test Galvanometer Target", Desk(-0.19f, 0.99f, 0.29f));
                worker.wirePost = Marker(preview, "Test Wire Target", Desk(-0.10f, 0.93f, 0.43f));
                worker.lookTarget = Marker(preview, "Test Look", Desk(0f, 1.5f, 1.5f));
                worker.Simulate(0f); // Awake's init and the first frame
                // Awake writes the pose: no T-pose on the first frame (hands well below the shoulders).
                Check("Ohm: Awake leaves no T-pose (hands below the shoulders)", rHand.position.y < rArm.position.y - 0.08f, $"drop {rArm.position.y - rHand.position.y:0.00} m");
                Check("Ohm: deskTop inferred 2 cm below the notebook target", Mathf.Abs(worker.deskTop - (worker.notebook.position.y - 0.02f)) < 1e-4f, $"{worker.deskTop:0.000} m");
                float plane = worker.deskTop + worker.contactClearance - 0.002f;
                foreach (OhmWorker.Mode mode in System.Enum.GetValues(typeof(OhmWorker.Mode)))
                {
                    worker.SetMode(mode);
                    for (float t = 0; t < 1.5f; t += Dt) worker.Simulate(Dt);
                    Check($"Ohm {mode}: no NaN", Finite(go.transform));
                    if (rIndex1 && rIndex3 && rFore)
                    {
                        float curl = Vector3.Angle(rHand.position - rFore.position, rIndex3.position - rIndex1.position);
                        Check($"Ohm {mode}: fingers keep a curl (not splayed straight)", curl > 8f, $"{curl:0} deg");
                    }
                    // The desk contact: over the next seconds (a whole work loop in Work: writing, the knob, the wire,
                    // writing again) no point of either hand goes below deskTop + contactClearance, 2 mm tolerance.
                    float seconds = mode == OhmWorker.Mode.Work ? 10f : 3f, low = float.MaxValue;
                    for (float t = 0; t < seconds; t += Dt)
                    {
                        worker.Simulate(Dt);
                        low = Mathf.Min(low, Mathf.Min(Lowest(rHand, rFingers), Lowest(lHand, lFingers)));
                    }
                    Check($"Ohm {mode}: no hand or finger point below the desk clearance over {seconds:0} s", low >= plane,
                        $"lowest point {(low - worker.deskTop) * 1000f:0.0} mm above the desk top, clearance {worker.contactClearance * 1000f:0} mm");
                    if (mode == OhmWorker.Mode.Work && rFore && lFore)
                    {
                        float rDown = PalmDown(rHand, rFore, rPalm), lDown = PalmDown(lHand, lFore, lPalm);
                        Check("Ohm Work: palms face the desk (as far as a twist about the forearm allows)", rDown > 0.8f && lDown > 0.8f, $"R {rDown:0.00} L {lDown:0.00} of the possible turn");
                    }
                }
                worker.SetMode(OhmWorker.Mode.Work);
                for (float t = 0; t < 2f; t += Dt) worker.Simulate(Dt);
                worker.Freeze();
                Vector3 held = rHand.position;
                for (int i = 0; i < 30; i++) worker.Simulate(0f);
                // The desk-contact passes re-solve the arm from the last frame's lowest point, so a frozen pose settles within a
                // fraction of a millimetre (0.25 mm measured) instead of being bit-identical: invisible on screen.
                Check("Ohm frozen: holds the pose on screen (under 0.5 mm of settling)", (rHand.position - held).magnitude < 5e-4f, $"moved {(rHand.position - held).magnitude * 1000f:0.000} mm");
                Check("Ohm frozen: not a T-pose", rHand.position.y < rArm.position.y - 0.05f);
                worker.Freeze(false);
                // The lift-over: through a whole loop the pen leaves the page and comes back, and the writing hand (the
                // notebook stands on his right) rises in an arc well above both of its resting heights on the way.
                for (float t = 0; t < 1f; t += Dt) worker.Simulate(Dt);
                float lowest = float.MaxValue, highest = float.MinValue;
                for (float t = 0; t < worker.writeSeconds + worker.checkSeconds; t += Dt)
                {
                    worker.Simulate(Dt);
                    lowest = Mathf.Min(lowest, rHand.position.y);
                    highest = Mathf.Max(highest, rHand.position.y);
                }
                Check("Ohm Work: the writing hand lifts over in an arc when it changes props", highest - lowest > 0.025f, $"wrist rises {(highest - lowest) * 1000f:0} mm over a loop");
                // Attention: Attend(someone) turns the head toward them, eased; Attend() gives it back to the work.
                if (headBone)
                {
                    worker.SetMode(OhmWorker.Mode.Present);
                    for (float t = 0; t < 1.5f; t += Dt) worker.Simulate(Dt);
                    Quaternion before = headBone.rotation;
                    worker.Attend(Marker(preview, "Test Attention", Desk(-1.2f, 1.4f, 0.3f))); // beside him, on his left
                    for (float t = 0; t < 1.5f; t += Dt) worker.Simulate(Dt);
                    float turned = Quaternion.Angle(before, headBone.rotation);
                    Check("Ohm Attend: the head turns toward whoever has his attention", turned > 12f, $"{turned:0} deg");
                    worker.Attend();
                    for (float t = 0; t < 1.5f; t += Dt) worker.Simulate(Dt);
                    float back = Quaternion.Angle(before, headBone.rotation);
                    Check("Ohm Attend(): the head returns to the work", back < 5f, $"{back:0.0} deg off");
                }
            }
            catch (System.Exception e) { Check("no exception", false, e.ToString()); }
            finally { preview.Cleanup(); }
            Summary("Ohm Pose");
        }

        // The three knuckle joints of each of Ohm's fingers (Mixamo names), collected as OhmWorker collects them.
        static Transform[][] Fingers(System.Func<string, Transform> bone, string side)
        {
            var fingers = new List<Transform[]>();
            foreach (var name in new[] { "Thumb", "Index", "Middle", "Ring", "Pinky" })
            {
                var joints = new[] { bone(side + "Hand" + name + "1"), bone(side + "Hand" + name + "2"), bone(side + "Hand" + name + "3") };
                if (joints[0] && joints[1] && joints[2]) fingers.Add(joints);
            }
            return fingers.ToArray();
        }

        // The lowest point of a hand as OhmWorker measures it: the wrist, every knuckle joint and each finger's tip (the
        // last joint carried on by its own segment).
        static float Lowest(Transform hand, Transform[][] fingers)
        {
            float low = hand.position.y;
            foreach (var joints in fingers)
            {
                foreach (var j in joints) low = Mathf.Min(low, j.position.y);
                low = Mathf.Min(low, (joints[2].position * 2f - joints[1].position).y);
            }
            return low;
        }

        // How far the palm (its bind-pose normal, in hand space) has turned toward the floor, as a fraction of what a
        // twist about the forearm's axis can give: 1 = as far down as it goes.
        static float PalmDown(Transform hand, Transform fore, Vector3 palmLocal)
        {
            Vector3 axis = (hand.position - fore.position).normalized;
            float best = Vector3.ProjectOnPlane(Vector3.down, axis).magnitude;
            return best > 1e-3f ? Vector3.Dot(hand.TransformDirection(palmLocal), Vector3.down) / best : 1f;
        }
    }
}
