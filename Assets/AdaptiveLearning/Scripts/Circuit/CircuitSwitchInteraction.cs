using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AdaptiveOhmsLaw
{
    public class CircuitSwitchInteraction : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Transform lever;
        [SerializeField] Vector3 openEuler = new Vector3(0, 0, 35f);
        [SerializeField] Vector3 closedEuler = Vector3.zero;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip clickSound;
        [SerializeField] SimpleCircuitModel circuit;

        public event Action<bool> SwitchToggled;
        public bool InteractionEnabled { get; set; } = true;

        void Start()
        {
            UpdateVisual(circuit != null && circuit.IsSwitchClosed);
        }

        public void Configure(Transform switchLever, SimpleCircuitModel model, AudioSource audioSource = null, AudioClip clickClip = null)
        {
            lever = switchLever;
            circuit = model;
            sfx = audioSource;
            clickSound = clickClip;
            UpdateVisual(circuit != null && circuit.IsSwitchClosed);
        }

        public void SetClosed(bool closed, bool playSound = true)
        {
            if (circuit != null)
                circuit.IsSwitchClosed = closed;
            UpdateVisual(closed);
            if (playSound && sfx != null && clickSound != null)
                sfx.PlayOneShot(clickSound);
            SwitchToggled?.Invoke(closed);
        }

        public void Toggle()
        {
            bool newState = circuit == null || !circuit.IsSwitchClosed;
            SetClosed(newState, true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (InteractionEnabled && eventData.button == PointerEventData.InputButton.Left) Toggle();
        }

        public void UpdateVisual(bool closed)
        {
            if (lever != null)
                lever.localRotation = Quaternion.Euler(closed ? closedEuler : openEuler);
        }
    }
}
