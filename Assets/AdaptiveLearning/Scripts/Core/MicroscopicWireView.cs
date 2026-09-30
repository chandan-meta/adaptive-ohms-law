using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    // Simplified microscopic visualization of Nichrome wire XY.
    // Shows fixed lattice ions and free electrons with rapid thermal motion plus slow net drift.
    public class MicroscopicWireView : MonoBehaviour
    {
        [SerializeField] RectTransform simulationContainer;
        [SerializeField] Text titleLabel;
        [SerializeField] Text captionLabel;
        [SerializeField] Text disclaimerLabel;
        [SerializeField] Button actionButton;
        [SerializeField] Text actionButtonText;
        [SerializeField] GameObject resistanceBadge;
        [SerializeField] Text resistanceSymbolText;
        [SerializeField] Button closeButton;

        public event Action InspectionCompleted;
        public event Action CloseRequested;

        struct Electron
        {
            public Vector2 pos;
            public Vector2 thermalVel;
            public float collisionCooldown;
        }

        readonly List<Vector2> latticeIons = new List<Vector2>();
        readonly List<Electron> electrons = new List<Electron>();
        readonly List<RectTransform> electronVisuals = new List<RectTransform>();
        readonly List<RectTransform> ionVisuals = new List<RectTransform>();
        readonly List<Vector2> trackedPath = new List<Vector2>();

        int trackedIndex = 0;
        bool isDrifting = true;
        bool actionTaken = false;
        float simWidth = 700f;
        float simHeight = 240f;
        const float driftSpeed = 35f; // Slow visible net drift
        const float thermalSpeed = 160f; // Rapid random motion
        const float ionRadius = 14f;
        const float electronRadius = 6f;

        void Awake()
        {
            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionButtonClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        public void Initialize()
        {
            actionTaken = false;
            if (resistanceBadge != null)
                resistanceBadge.SetActive(false);
            if (disclaimerLabel != null)
                disclaimerLabel.text = "SIMPLIFIED VISUALIZATION  •  Nichrome wire XY structure";
            if (captionLabel != null)
                captionLabel.text = "Charges undergo rapid random thermal motion. Under voltage, they drift with a small net velocity, colliding with lattice ions.";
            if (actionButtonText != null)
                actionButtonText.text = "FOLLOW ELECTRON DRIFT";

            SetupSimulation();
        }

        void SetupSimulation()
        {
            if (simulationContainer == null) return;

            // Clear existing visuals
            foreach (var t in electronVisuals) if (t != null) Destroy(t.gameObject);
            foreach (var t in ionVisuals) if (t != null) Destroy(t.gameObject);
            electronVisuals.Clear();
            ionVisuals.Clear();
            latticeIons.Clear();
            electrons.Clear();

            var rect = simulationContainer.rect;
            simWidth = Mathf.Max(300f, rect.width > 0 ? rect.width : 700f);
            simHeight = Mathf.Max(160f, rect.height > 0 ? rect.height : 240f);

            // Create lattice of metal ions (fixed positive ions)
            int cols = 8;
            int rows = 3;
            float stepX = simWidth / (cols + 1);
            float stepY = simHeight / (rows + 1);

            for (int r = 1; r <= rows; r++)
            {
                for (int c = 1; c <= cols; c++)
                {
                    float x = -simWidth * 0.5f + c * stepX + ((r % 2 == 0) ? stepX * 0.25f : -stepX * 0.25f);
                    float y = -simHeight * 0.5f + r * stepY;
                    var pos = new Vector2(x, y);
                    latticeIons.Add(pos);

                    var ionGo = new GameObject("Ion_" + c + "_" + r, typeof(RectTransform), typeof(Image));
                    ionGo.transform.SetParent(simulationContainer, false);
                    var rt = (RectTransform)ionGo.transform;
                    rt.sizeDelta = new Vector2(ionRadius * 2, ionRadius * 2);
                    rt.anchoredPosition = pos;
                    var img = ionGo.GetComponent<Image>();
                    img.color = new Color(0.85f, 0.48f, 0.22f, 0.95f); // Copper/Nichrome lattice ion
                    ionVisuals.Add(rt);
                }
            }

            // Create mobile charge carriers (electrons)
            int electronCount = 20;
            for (int i = 0; i < electronCount; i++)
            {
                float x = UnityEngine.Random.Range(-simWidth * 0.45f, simWidth * 0.45f);
                float y = UnityEngine.Random.Range(-simHeight * 0.4f, simHeight * 0.4f);
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * thermalSpeed;

                electrons.Add(new Electron { pos = new Vector2(x, y), thermalVel = vel, collisionCooldown = 0 });

                var eGo = new GameObject("Electron_" + i, typeof(RectTransform), typeof(Image));
                eGo.transform.SetParent(simulationContainer, false);
                var rt = (RectTransform)eGo.transform;
                rt.sizeDelta = new Vector2(electronRadius * 2, electronRadius * 2);
                rt.anchoredPosition = new Vector2(x, y);
                var img = eGo.GetComponent<Image>();
                img.color = (i == trackedIndex) ? new Color(1f, 0.92f, 0.2f) : new Color(0.2f, 0.9f, 0.95f);
                electronVisuals.Add(rt);
            }
        }

        void Update()
        {
            if (electrons.Count == 0) return;
            float dt = Time.deltaTime;
            if (dt <= 0 || dt > 0.1f) dt = 0.016f;

            float halfW = simWidth * 0.5f;
            float halfH = simHeight * 0.5f;

            for (int i = 0; i < electrons.Count; i++)
            {
                var e = electrons[i];
                e.collisionCooldown = Mathf.Max(0, e.collisionCooldown - dt);

                // Movement = thermal velocity + net drift along +X (towards positive potential)
                Vector2 totalVel = e.thermalVel;
                if (isDrifting)
                    totalVel.x += driftSpeed;

                e.pos += totalVel * dt;

                // Horizontal wrap around (continuous circuit)
                if (e.pos.x > halfW) e.pos.x = -halfW;
                else if (e.pos.x < -halfW) e.pos.x = halfW;

                // Vertical boundary reflection
                if (e.pos.y > halfH - electronRadius)
                {
                    e.pos.y = halfH - electronRadius;
                    e.thermalVel.y = -Mathf.Abs(e.thermalVel.y);
                }
                else if (e.pos.y < -halfH + electronRadius)
                {
                    e.pos.y = -halfH + electronRadius;
                    e.thermalVel.y = Mathf.Abs(e.thermalVel.y);
                }

                // Check collision with lattice ions
                if (e.collisionCooldown <= 0)
                {
                    foreach (var ion in latticeIons)
                    {
                        float distSq = (e.pos - ion).sqrMagnitude;
                        float minDist = ionRadius + electronRadius;
                        if (distSq < minDist * minDist)
                        {
                            // Scatter in a random direction
                            float scatterAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                            e.thermalVel = new Vector2(Mathf.Cos(scatterAngle), Mathf.Sin(scatterAngle)) * thermalSpeed;
                            e.collisionCooldown = 0.08f;
                            break;
                        }
                    }
                }

                electrons[i] = e;
                if (i < electronVisuals.Count && electronVisuals[i] != null)
                {
                    electronVisuals[i].anchoredPosition = e.pos;
                }
            }

            // Subtle vibration of lattice ions (thermal agitation)
            float t = Time.time * 8f;
            for (int i = 0; i < ionVisuals.Count; i++)
            {
                if (ionVisuals[i] != null && i < latticeIons.Count)
                {
                    float jitterX = Mathf.Sin(t + i * 1.7f) * 1.5f;
                    float jitterY = Mathf.Cos(t + i * 2.3f) * 1.5f;
                    ionVisuals[i].anchoredPosition = latticeIons[i] + new Vector2(jitterX, jitterY);
                }
            }
        }

        void OnActionButtonClicked()
        {
            if (!actionTaken)
            {
                actionTaken = true;
                if (captionLabel != null)
                    captionLabel.text = "Interactions within the material impede the directed flow of charge.\nThat opposition is called ELECTRICAL RESISTANCE (R).";
                if (resistanceBadge != null)
                    resistanceBadge.SetActive(true);
                if (actionButtonText != null)
                    actionButtonText.text = "RETURN TO BENCH & DISCOVER RATIO";
            }
            else
            {
                InspectionCompleted?.Invoke();
            }
        }
    }
}
