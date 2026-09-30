using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdaptiveOhmsLaw
{
    // Interactive equation builder with both drag-and-drop and tap-to-place support.
    public class EquationAssemblyView : MonoBehaviour
    {
        public enum TokenType { None, V, I, R }

        [System.Serializable]
        public class SlotUI
        {
            public RectTransform rect;
            public Text slotLabel;
            public Image background;
            public TokenType currentToken = TokenType.None;
            public TokenUI placedTokenUI;
        }

        [SerializeField] Text promptText;
        [SerializeField] Text initialFormulaText;
        [SerializeField] SlotUI[] slots; // 3 slots: [0] = [1] * [2]
        [SerializeField] TokenUI[] tokens; // 3 tokens: V, I, R
        [SerializeField] RectTransform tokenPool;
        [SerializeField] GameObject successBanner;
        [SerializeField] Text successBannerText;
        [SerializeField] Button clearButton;

        public event Action EquationAssembled;
        public event Action<bool> AttemptEvaluated;

        bool solved = false;
        int mistakes = 0;

        void Awake()
        {
            if (tokens == null || tokens.Length == 0 || Array.Exists(tokens, t => t == null))
            {
                tokens = GetComponentsInChildren<TokenUI>(true);
            }
            if (tokenPool == null)
            {
                tokenPool = (transform.Find("TokenPool") ?? transform.Find("SlotsArea/TokenPool")) as RectTransform;
            }

            if (tokens != null)
            {
                foreach (var tok in tokens)
                {
                    if (tok != null)
                    {
                        tok.view = this;
                        if (tok.originalParent == null)
                            tok.originalParent = tokenPool != null ? tokenPool : tok.transform.parent;

                    }
                }
            }

            if (clearButton == null)
                clearButton = transform.Find("ResetEq")?.GetComponent<Button>() ?? GetComponentInChildren<Button>(true);

            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(ResetEquation);
            }
        }

        public void Initialize()
        {
            solved = false;
            mistakes = 0;

            if (promptText != null)
                promptText.text = "Rearrange R = V / I to express Voltage V in terms of Current I and Resistance R.\nDrag or tap the symbols into the slots:";
            if (initialFormulaText != null)
                initialFormulaText.text = "Starting from:  R  =  V  /  I";
            if (successBanner != null)
                successBanner.SetActive(false);

            ResetEquation();
        }

        public void ResetEquation()
        {
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.currentToken = TokenType.None;
                    slot.placedTokenUI = null;
                    if (slot.slotLabel != null) slot.slotLabel.text = "?";
                }
            }

            foreach (var tok in tokens)
            {
                if (tok != null)
                {
                    tok.placedSlot = -1;
                    tok.transform.SetParent(tokenPool, false);
                    tok.gameObject.SetActive(true);
                }
            }

            if (successBanner != null && !solved)
                successBanner.SetActive(false);
        }

        public void OnTokenClicked(TokenUI token)
        {
            if (solved) return;

            // If already in a slot, remove it back to pool
            if (token.placedSlot >= 0)
            {
                RemoveFromSlot(token.placedSlot);
                return;
            }

            // Find first empty slot
            int emptySlot = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].currentToken == TokenType.None)
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot >= 0)
            {
                PlaceInSlot(token, emptySlot);
            }
        }

        public void OnTokenDropped(TokenUI token, PointerEventData eventData)
        {
            if (solved)
            {
                ReturnTokenToPool(token);
                return;
            }

            // Check if dropped over a slot
            int targetSlot = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(slots[i].rect, eventData.position, eventData.pressEventCamera))
                {
                    targetSlot = i;
                    break;
                }
            }

            if (targetSlot >= 0)
            {
                // If slot already occupied, evict current token
                if (slots[targetSlot].placedTokenUI != null)
                {
                    ReturnTokenToPool(slots[targetSlot].placedTokenUI);
                }
                PlaceInSlot(token, targetSlot);
            }
            else
            {
                ReturnTokenToPool(token);
            }
        }

        void PlaceInSlot(TokenUI token, int slotIndex)
        {
            slots[slotIndex].currentToken = token.tokenType;
            slots[slotIndex].placedTokenUI = token;
            token.placedSlot = slotIndex;
            token.transform.SetParent(slots[slotIndex].rect, false);
            token.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            CheckEquation();
        }

        public void RemoveFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length) return;
            var tok = slots[slotIndex].placedTokenUI;
            slots[slotIndex].currentToken = TokenType.None;
            slots[slotIndex].placedTokenUI = null;

            if (tok != null)
                ReturnTokenToPool(tok);
        }

        void ReturnTokenToPool(TokenUI token)
        {
            token.placedSlot = -1;
            token.transform.SetParent(tokenPool, false);
        }

        void CheckEquation()
        {
            // All 3 slots must be filled
            bool allFilled = true;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].currentToken == TokenType.None)
                {
                    allFilled = false;
                    break;
                }
            }

            if (!allFilled) return;

            // Correct equation: [0] = V, and ([1] == I && [2] == R) or ([1] == R && [2] == I)
            bool isCorrect = (slots[0].currentToken == TokenType.V) &&
                ((slots[1].currentToken == TokenType.I && slots[2].currentToken == TokenType.R) ||
                 (slots[1].currentToken == TokenType.R && slots[2].currentToken == TokenType.I));

            AttemptEvaluated?.Invoke(isCorrect);

            if (isCorrect)
            {
                solved = true;
                if (successBanner != null)
                {
                    successBanner.SetActive(true);
                    if (successBannerText != null)
                        successBannerText.text = "OHM'S LAW:  V  =  I  ×  R\nPotential Difference = Current × Resistance";
                }
                if (promptText != null)
                    promptText.text = "Well done! Voltage equals current multiplied by resistance.";

                EquationAssembled?.Invoke();
            }
            else
            {
                mistakes++;
                if (promptText != null)
                    promptText.text = "Not quite. Remember: R = V / I. Multiplying both sides by current I gives V = I × R.";
            }
        }
    }
}
