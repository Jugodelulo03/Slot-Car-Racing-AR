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
        private readonly Color _normalColor = new Color(1f, 0.45f, 0.15f, 0.75f);
        private readonly Color _pressedColor = new Color(1f, 0.75f, 0.18f, 0.95f);

        public event Action<bool> OnHoldChanged;

        private void Awake()
        {
            if (TryGetComponent<RectTransform>(out RectTransform rectTransform))
            {
                rectTransform.anchorMin = new Vector2(0.82f, 0.08f);
                rectTransform.anchorMax = new Vector2(0.97f, 0.32f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            if (TryGetComponent<Image>(out Image image))
            {
                _image = image;
                _image.color = _normalColor;
            }
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

            OnHoldChanged?.Invoke(_isPressed);
            _carReference.Car?.SetAccelerationHeld(_isPressed);
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
