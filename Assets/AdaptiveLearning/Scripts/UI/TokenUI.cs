using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    public class TokenUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public EquationAssemblyView.TokenType tokenType;
        public Text label;
        public Image background;
        [HideInInspector] public EquationAssemblyView view;
        [HideInInspector] public Transform originalParent;
        [HideInInspector] public Vector2 originalPos;
        [HideInInspector] public int placedSlot = -1;

        Canvas rootCanvas;
        RectTransform rectTransform;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();
            view = GetComponentInParent<EquationAssemblyView>(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            if (view == null) view = GetComponentInParent<EquationAssemblyView>(true) ?? FindFirstObjectByType<EquationAssemblyView>(FindObjectsInactive.Include);
            view?.OnTokenClicked(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (placedSlot >= 0)
            {
                view?.RemoveFromSlot(placedSlot);
            }
            originalParent = transform.parent;
            originalPos = rectTransform.anchoredPosition;
            transform.SetParent(rootCanvas.transform, true);
            if (background != null) background.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (background != null) background.raycastTarget = true;
            view?.OnTokenDropped(this, eventData);
        }
    }
}
