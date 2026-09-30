using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public class ResistanceGraphView : MonoBehaviour
    {
        [SerializeField] ResistanceGraphGraphic graphGraphic;
        [SerializeField] RectTransform graphRect;
        [SerializeField] Button[] pointButtons;
        [SerializeField] Text[] pointLabels;
        [SerializeField] Text instructionText;
        [SerializeField] Text slopeResultText;
        [SerializeField] GameObject slopePanel;
        [SerializeField] Button continueButton;

        public event Action<float> SlopeDetermined;
        public event Action ContinueClicked;

        readonly List<Vector2> dataPoints = new List<Vector2>
        {
            new Vector2(0.20f, 1.0f),
            new Vector2(0.40f, 2.0f),
            new Vector2(0.60f, 3.0f),
            new Vector2(0.80f, 4.0f),
            new Vector2(1.00f, 5.0f)
        };

        int selectedIndexA = -1;
        int selectedIndexB = -1;

        void Awake()
        {
            for (int i = 0; i < pointButtons.Length; i++)
            {
                int index = i;
                pointButtons[i].onClick.AddListener(() => OnPointClicked(index));
            }

            if (continueButton != null)
                continueButton.onClick.AddListener(() => ContinueClicked?.Invoke());
        }

        public void Initialize(List<ExperimentObservation> observations = null)
        {
            if (observations != null && observations.Count == pointButtons.Length)
            {
                dataPoints.Clear();
                foreach (var row in observations) dataPoints.Add(new Vector2(row.current, row.voltage));
                dataPoints.Sort((a, b) => a.x.CompareTo(b.x));
            }
            selectedIndexA = -1;
            selectedIndexB = -1;

            if (graphGraphic != null)
            {
                graphGraphic.points.Clear();
                graphGraphic.points.AddRange(dataPoints);
                graphGraphic.showSlopeTriangle = false;
                graphGraphic.Refresh();
            }

            if (slopePanel != null)
                slopePanel.SetActive(false);
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            if (instructionText != null)
                instructionText.text = "Select two well-separated points on the line to find the slope (ΔV / ΔI).";

            UpdatePointVisuals();
            PositionPointButtons();
        }

        void PositionPointButtons()
        {
            if (graphRect == null) return;
            float maxI = 1.20f;
            float maxV = 6.0f;

            for (int i = 0; i < pointButtons.Length && i < dataPoints.Count; i++)
            {
                var pt = dataPoints[i];
                var rt = (RectTransform)pointButtons[i].transform;
                rt.anchorMin = rt.anchorMax = new Vector2(pt.x / maxI, pt.y / maxV);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        void OnPointClicked(int index)
        {
            if (index < 0 || index >= dataPoints.Count) return;

            if (selectedIndexA < 0)
            {
                selectedIndexA = index;
                if (instructionText != null)
                    instructionText.text = "Point 1: (" + dataPoints[index].x.ToString("0.00") + " A, " + dataPoints[index].y.ToString("0.0") + " V). Now select a second point.";
                UpdatePointVisuals();
            }
            else if (selectedIndexA == index)
            {
                if (instructionText != null)
                    instructionText.text = "Choose a different point on the line to calculate the slope.";
            }
            else
            {
                selectedIndexB = index;
                CalculateSlope();
            }
        }

        void CalculateSlope()
        {
            var pA = dataPoints[selectedIndexA];
            var pB = dataPoints[selectedIndexB];

            // Order so pA has smaller current
            if (pA.x > pB.x)
            {
                var temp = pA; pA = pB; pB = temp;
            }

            float deltaV = pB.y - pA.y;
            float deltaI = pB.x - pA.x;
            float slope = deltaI > 0.0001f ? (deltaV / deltaI) : 5.0f;

            if (graphGraphic != null)
            {
                graphGraphic.showSlopeTriangle = true;
                graphGraphic.slopePointA = pA;
                graphGraphic.slopePointB = pB;
                graphGraphic.Refresh();
            }

            if (slopePanel != null)
                slopePanel.SetActive(true);

            if (slopeResultText != null)
            {
                slopeResultText.text = "ΔV = " + deltaV.ToString("0.0") + " V   |   ΔI = " + deltaI.ToString("0.00") + " A\n"
                    + "Slope = ΔV / ΔI = " + deltaV.ToString("0.0") + " V ÷ " + deltaI.ToString("0.00") + " A = " + slope.ToString("0.0") + " Ω";
            }

            if (instructionText != null)
                instructionText.text = "Slope = ΔV / ΔI gives resistance: 5.0 Ω! Notice it matches our table ratio.";

            if (continueButton != null)
                continueButton.gameObject.SetActive(true);

            UpdatePointVisuals();
            SlopeDetermined?.Invoke(slope);
        }

        void UpdatePointVisuals()
        {
            for (int i = 0; i < pointButtons.Length; i++)
            {
                var img = pointButtons[i].GetComponent<Image>();
                if (img == null) continue;

                if (i == selectedIndexA || i == selectedIndexB)
                    img.color = WarmLabTheme.Amber; // Selected pair
                else
                    img.color = WarmLabTheme.Sage;
                var label=pointButtons[i].GetComponentInChildren<Text>();
                if(label!=null) label.color=(i==selectedIndexA||i==selectedIndexB)?WarmLabTheme.Ink:WarmLabTheme.Ivory;
            }
        }

        public void ResetPoints()
        {
            Initialize();
        }
    }
}

