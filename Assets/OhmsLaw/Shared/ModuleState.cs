using System.Collections.Generic;
using UnityEngine;

namespace OhmsLaw
{
    // Reviewer switches. Static, so they last for the whole play session and reset when Play mode restarts.
    public static class ReviewMode
    {
        // Gates answer themselves: correct option, parts placed, slider set, key closed. The module plays hands-free.
        public static bool autoAnswer;
        public static float autoDelay = 3f;

        static float scale = 1f;
        public static float timeScale
        {
            get => scale;
            set { scale = value; Time.timeScale = value; }
        }
        // skipToScene is ModuleFlow.JumpToScene, reachable from the Module Player and the F1 overlay.
    }

    // What the learner did, for later scenes to read back. Kept for the session only, never written to disk.
    public static class LearnerSession
    {
        public static string prediction = "";        // Scene 6: "A" increase, "B" decrease, "C" stay the same
        public static string s10PatternAnswer = "";   // Scene 10 pattern question, final answer letter
        public static int s10PatternAttempts;
        public static string slopeAnswer = "";        // Scene 12 slope, the option text chosen last
        public static readonly List<string> checkpoint = new List<string>(); // Scene 13, "AQ1 A correct", ...
        public static int tableRows;                  // observation table rows recorded so far

        public static string PredictionText =>
            prediction == "A" ? "Increase" : prediction == "B" ? "Decrease" : prediction == "C" ? "Stay the same" : "";

        public static string Letter(int index) => index < 0 ? "" : ((char)('A' + index)).ToString();
    }

    // Carried across a scene change so the next scene can start on the same frame (match-cuts).
    public static class SceneHandoffState
    {
        public static string fromScene = "";
        public static bool hasCamera;
        public static Vector3 cameraPosition;
        public static Quaternion cameraRotation;
        public static float fieldOfView = 40f;
        public static CircuitState circuit;
        public static bool fieldOn;
        public static float driftSpeed;
        public static Vector3[] cardPositions = new Vector3[0];
        public static int tableRows;
        public static bool graphShown;

        public static void SaveCamera()
        {
            var cam = Camera.main;
            if (!cam) return;
            hasCamera = true;
            cameraPosition = cam.transform.position;
            cameraRotation = cam.transform.rotation;
            fieldOfView = cam.fieldOfView;
        }
    }
}
