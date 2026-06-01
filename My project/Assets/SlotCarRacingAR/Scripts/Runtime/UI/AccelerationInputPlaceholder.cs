using SlotCarRacingAR.Runtime.Features;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Acceleration input placeholder scaffold. Provides a single button/touch
    /// input seam that later stories will replace with the full HUD.
    /// </summary>
    public sealed class AccelerationInputPlaceholder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private CarPlaceholderReference _carReference;

        private bool _isPressed;
        private Image _image;
        private Text _label;
        private readonly Color _normalColor = RetroUi.Teal;
        private readonly Color _pressedColor = RetroUi.Yellow;

        public event Action<bool> OnHoldChanged;

        private void Awake()
        {
            if (TryGetComponent<RectTransform>(out RectTransform rectTransform))
            {
                rectTransform.anchorMin = new Vector2(1f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 0f);
                rectTransform.pivot = new Vector2(1f, 0f);
                rectTransform.anchoredPosition = new Vector2(-42f, 42f);
                rectTransform.sizeDelta = new Vector2(220f, 220f);
            }

            if (!TryGetComponent<Image>(out Image image))
            {
                image = gameObject.AddComponent<Image>();
            }

            if (image != null)
            {
                _image = image;
                RetroUi.StyleImageAsCircle(_image, _normalColor);
            }

            BuildLabel();
        }

        /// <summary>
        /// Called by the composition root to inject the placeholder car dependency.
        /// </summary>
        public void Bind(CarPlaceholder carPlaceholder)
        {
            _carReference = new CarPlaceholderReference
            {
                Car = carPlaceholder,
            };

            ApplyState();
        }

        /// <summary>
        /// Called by the Unity UI event system when the button is pressed.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            SetPressed(true);
        }

        /// <summary>
        /// Called by the Unity UI event system when the button is released.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed(false);
        }

        /// <summary>
        /// Release acceleration if the pointer leaves the button bounds while pressed.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            SetPressed(false);
        }

        private void OnDisable()
        {
            SetPressed(false);
        }

        private void SetPressed(bool isPressed)
        {
            if (_isPressed == isPressed)
            {
                return;
            }

            _isPressed = isPressed;
            ApplyState();
        }

        private void ApplyState()
        {
            if (_image != null)
            {
                _image.color = _isPressed ? _pressedColor : _normalColor;
            }

            if (_label != null)
            {
                _label.color = _isPressed ? RetroUi.Black : RetroUi.Yellow;
            }

            OnHoldChanged?.Invoke(_isPressed);
            _carReference.Car?.SetAccelerationHeld(_isPressed);
        }

        private void BuildLabel()
        {
            if (transform.Find("AccelerationLabel") != null)
            {
                return;
            }

            _label = RetroUi.CreateText(
                transform,
                "AccelerationLabel",
                "ACELERAR",
                new Vector2(0.10f, 0.22f),
                new Vector2(0.90f, 0.78f),
                28,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 16;
            _label.resizeTextMaxSize = 28;
        }

        /// <summary>
        /// Thin serializable reference to avoid Find/Tag lookups.
        /// </summary>
        [System.Serializable]
        public struct CarPlaceholderReference
        {
            public CarPlaceholder Car;
        }
    }
}
