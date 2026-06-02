using UnityEngine;
using UnityEngine.EventSystems;
using SlotCarRacingAR.Runtime.Infrastructure;

namespace SlotCarRacingAR.Runtime.UI
{
    public sealed class RetroButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private static readonly Vector2 PressOffset = new Vector2(4f, -4f);

        private RectTransform _rectTransform;
        private Vector2 _restPosition;
        private RetroUiAnimator _animator;
        private bool _pressed;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _animator = RetroUiAnimator.Attach(gameObject);
            if (_rectTransform != null)
            {
                _restPosition = _rectTransform.anchoredPosition;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            GameAudio.Play(GameSfx.UiClick);
            SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetPressed(false);
        }

        private void OnDisable()
        {
            _pressed = false;
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _restPosition;
                _rectTransform.localScale = Vector3.one;
            }
        }

        private void SetPressed(bool pressed)
        {
            if (_pressed == pressed || _rectTransform == null)
            {
                return;
            }

            _pressed = pressed;
            _rectTransform.anchoredPosition = _restPosition + (pressed ? PressOffset : Vector2.zero);
            _rectTransform.localScale = pressed ? Vector3.one * 0.965f : Vector3.one;
            if (!pressed)
            {
                _animator?.PlayPressBounce();
            }
        }
    }
}
