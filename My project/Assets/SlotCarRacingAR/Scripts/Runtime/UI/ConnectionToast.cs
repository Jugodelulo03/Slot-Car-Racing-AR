using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Small non-blocking network notice. It never captures input and auto-hides.
    /// </summary>
    public sealed class ConnectionToast : MonoBehaviour
    {
        private const float DefaultDurationSeconds = 3.0f;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private GameObject _panel;
        private Text _messageText;
        private RetroUiAnimator _panelAnimator;
        private float _hideAt;

        private void Awake()
        {
            BuildUI();
            HideImmediate();
        }

        private void Update()
        {
            if (_panel == null || !_panel.activeSelf)
            {
                return;
            }

            float remaining = _hideAt - Time.unscaledTime;
            if (remaining <= 0f)
            {
                HideImmediate();
                return;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = remaining < 0.35f ? Mathf.Clamp01(remaining / 0.35f) : 1f;
            }
        }

        public void ShowPlayerDisconnected(byte oldCount, byte newCount)
        {
            string message = oldCount > newCount + 1
                ? "Jugadores desconectados"
                : "Jugador desconectado";
            Show(message, RetroUi.Red);
        }

        public void Show(string message, Color accentColor, float durationSeconds = DefaultDurationSeconds)
        {
            if (_panel == null || _messageText == null)
            {
                BuildUI();
            }

            _messageText.text = message;
            _messageText.color = RetroUi.White;

            Image panelImage = _panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = RetroUi.WithAlpha(Color.Lerp(RetroUi.TealDark, accentColor, 0.35f), 0.94f);
            }

            _hideAt = Time.unscaledTime + Mathf.Max(0.75f, durationSeconds);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            _panel.SetActive(true);
            _panelAnimator?.PlaySlideIn(new Vector2(0f, 44f), 0.22f);
            _panelAnimator?.PlayFadeIn(0.16f);
        }

        private void HideImmediate()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void BuildUI()
        {
            if (_canvas != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("ConnectionToastCanvas");
            canvasObject.transform.SetParent(transform, false);

            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 115;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            RectTransform panelRect = RetroUi.CreatePanel(
                canvasObject.transform,
                "ConnectionToastPanel",
                new Vector2(0.35f, 0.895f),
                new Vector2(0.65f, 0.965f),
                RetroUi.WithAlpha(RetroUi.RedDark, 0.94f),
                false,
                true,
                true);
            _panel = panelRect.gameObject;
            _panelAnimator = RetroUiAnimator.Attach(_panel);

            Image panelImage = _panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.raycastTarget = false;
            }

            _messageText = RetroUi.CreateText(
                panelRect,
                "ConnectionToastText",
                "Jugador desconectado",
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.92f),
                28,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _messageText.resizeTextForBestFit = true;
            _messageText.resizeTextMinSize = 18;
            _messageText.resizeTextMaxSize = 30;
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }
        }
    }
}
