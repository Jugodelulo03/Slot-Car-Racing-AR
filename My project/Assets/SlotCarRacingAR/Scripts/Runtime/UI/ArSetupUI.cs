using System;
using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// AR setup screen UI: scanning, stability feedback, ready button, and ready-sync strip.
    /// </summary>
    public sealed class ArSetupUI : MonoBehaviour
    {
        private GameObject _scanningPanel;
        private Text _scanningText;
        private Text _scanningSubText;
        private GameObject _toastPanel;
        private Text _toastText;
        private GameObject _stabilityPanel;
        private Text _stabilityText;
        private GameObject _readyPanel;
        private Button _readyButton;
        private Text _readyButtonText;
        private GameObject _readySyncStrip;
        private Text _hostReadyLabel;
        private Text _guestReadyLabel;
        private Image _hostReadyLight;
        private Image _guestReadyLight;
        private float _toastTimer;
        private const float ToastDuration = 2.5f;
        private bool _localReady;

        public event Action<bool> OnReadyPressed;

        private void Awake()
        {
            BuildUI();
        }

        private void Update()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f)
                {
                    _toastPanel.SetActive(false);
                }
            }
        }

        public void ShowScanning()
        {
            gameObject.SetActive(true);
            _scanningPanel.SetActive(true);
            _stabilityPanel.SetActive(false);
            _toastPanel.SetActive(false);
            _readyPanel.SetActive(false);
        }

        public void ShowMarkerDetected()
        {
            _scanningPanel.SetActive(false);
            _stabilityPanel.SetActive(true);
            ShowToast("Marcador detectado", RetroUi.Green);
            UpdateStability(TrackStabilityState.Unstable);
        }

        public void UpdateStability(TrackStabilityState state)
        {
            _stabilityPanel.SetActive(true);
            switch (state)
            {
                case TrackStabilityState.Unstable:
                    _stabilityText.text = "PISTA ESTABILIZANDOSE";
                    _stabilityText.color = RetroUi.Yellow;
                    _readyPanel.SetActive(false);
                    break;
                case TrackStabilityState.Stable:
                    _stabilityText.text = "PISTA ESTABLE";
                    _stabilityText.color = RetroUi.Green;
                    ShowToast("Pista lista", RetroUi.Green);
                    _readyPanel.SetActive(true);
                    break;
                case TrackStabilityState.Scanning:
                    _stabilityPanel.SetActive(false);
                    _scanningPanel.SetActive(true);
                    _readyPanel.SetActive(false);
                    break;
            }
        }

        public void ShowTrackingLost()
        {
            _stabilityPanel.SetActive(true);
            _stabilityText.text = "TRACKING PERDIDO";
            _stabilityText.color = RetroUi.Red;
            _readyPanel.SetActive(false);
            _readySyncStrip.SetActive(false);
            _localReady = false;
        }

        public void UpdateReadySync(bool hostReady, bool guestReady)
        {
            _readySyncStrip.SetActive(true);
            _hostReadyLabel.text = hostReady ? "HOST\nLISTO" : "HOST\nESPERA";
            _guestReadyLabel.text = guestReady ? "INVITADO\nLISTO" : "INVITADO\nESPERA";
            _hostReadyLabel.color = hostReady ? RetroUi.Green : RetroUi.White;
            _guestReadyLabel.color = guestReady ? RetroUi.Green : RetroUi.White;
            if (_hostReadyLight != null)
            {
                _hostReadyLight.color = hostReady ? RetroUi.Green : RetroUi.Red;
            }

            if (_guestReadyLight != null)
            {
                _guestReadyLight.color = guestReady ? RetroUi.Green : RetroUi.Red;
            }
        }

        public void RevokeReady()
        {
            _localReady = false;
            _readyButtonText.text = "CONFIRMAR";
            _readyButtonText.color = RetroUi.White;
            _readySyncStrip.SetActive(false);
        }

        public void UpdateConnectionStatus(string status, Color color)
        {
            // Intentionally hidden in the race setup view to keep the track view clear.
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ShowToast(string message, Color color)
        {
            _toastPanel.SetActive(true);
            _toastText.text = message.ToUpperInvariant();
            _toastText.color = color;
            _toastTimer = ToastDuration;
        }

        private void BuildUI()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            _scanningPanel = RetroUi.CreatePanel(
                transform,
                "ScanningPanel",
                new Vector2(0.04f, 0.67f),
                new Vector2(0.96f, 0.95f),
                RetroUi.WithAlpha(RetroUi.Teal, 0.86f),
                false).gameObject;

            RetroUi.CreateLogo(
                _scanningPanel.transform,
                "Face2RaceLogo",
                new Vector2(0.04f, 0.48f),
                new Vector2(0.36f, 0.96f));

            _scanningText = RetroUi.CreateText(
                _scanningPanel.transform,
                "ScanningText",
                "COLOCA EL MARCADOR SOBRE LA MESA",
                new Vector2(0.43f, 0.46f),
                new Vector2(0.93f, 0.88f),
                34,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _scanningSubText = RetroUi.CreateText(
                _scanningPanel.transform,
                "ScanningSubText",
                "Ambos deben apuntar al mismo marcador",
                new Vector2(0.46f, 0.16f),
                new Vector2(0.90f, 0.42f),
                26,
                RetroUi.Cream,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _stabilityPanel = RetroUi.CreatePanel(
                transform,
                "StabilityPanel",
                new Vector2(0.34f, 0.77f),
                new Vector2(0.66f, 0.95f),
                RetroUi.WithAlpha(RetroUi.Cream, 0.94f),
                false).gameObject;

            RetroUi.CreateText(
                _stabilityPanel.transform,
                "ReadyTitle",
                "TODO LISTO PARA CORRER?",
                new Vector2(0.06f, 0.62f),
                new Vector2(0.94f, 0.94f),
                23,
                RetroUi.Black,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);

            _stabilityText = RetroUi.CreateText(
                _stabilityPanel.transform,
                "StabilityText",
                "",
                new Vector2(0.08f, 0.12f),
                new Vector2(0.92f, 0.58f),
                27,
                RetroUi.Green,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _stabilityPanel.SetActive(false);

            _readyPanel = RetroUi.CreatePanel(
                transform,
                "ReadyPanel",
                new Vector2(0.28f, 0.02f),
                new Vector2(0.72f, 0.13f),
                RetroUi.Red,
                true).gameObject;

            _readyButton = _readyPanel.AddComponent<Button>();
            _readyButton.targetGraphic = _readyPanel.GetComponent<Image>();
            ColorBlock readyColors = _readyButton.colors;
            readyColors.normalColor = RetroUi.Red;
            readyColors.highlightedColor = Color.Lerp(RetroUi.Red, RetroUi.White, 0.12f);
            readyColors.pressedColor = Color.Lerp(RetroUi.Red, RetroUi.Black, 0.18f);
            readyColors.selectedColor = RetroUi.Red;
            _readyButton.colors = readyColors;
            _readyButton.onClick.AddListener(HandleReadyButtonClicked);
            _readyPanel.AddComponent<RetroButtonPress>();

            _readyButtonText = RetroUi.CreateText(
                _readyPanel.transform,
                "ReadyButtonText",
                "CONFIRMAR",
                Vector2.zero,
                Vector2.one,
                31,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _readyPanel.SetActive(false);

            _readySyncStrip = RetroUi.CreatePanel(
                transform,
                "ReadySyncStrip",
                new Vector2(0.02f, 0.84f),
                new Vector2(0.98f, 0.98f),
                RetroUi.WithAlpha(RetroUi.Teal, 0.92f),
                false).gameObject;

            _hostReadyLight = RetroUi.CreateStatusLight(_readySyncStrip.transform, "HostLight", new Vector2(0.05f, 0.18f), new Vector2(0.10f, 0.82f), RetroUi.Red);
            _hostReadyLabel = RetroUi.CreateText(
                _readySyncStrip.transform,
                "HostReadyLabel",
                "HOST\nESPERA",
                new Vector2(0.12f, 0.05f),
                new Vector2(0.40f, 0.95f),
                30,
                RetroUi.White,
                TextAnchor.MiddleLeft,
                FontStyle.BoldAndItalic);

            _guestReadyLight = RetroUi.CreateStatusLight(_readySyncStrip.transform, "GuestLight", new Vector2(0.90f, 0.18f), new Vector2(0.95f, 0.82f), RetroUi.Red);
            _guestReadyLabel = RetroUi.CreateText(
                _readySyncStrip.transform,
                "GuestReadyLabel",
                "INVITADO\nESPERA",
                new Vector2(0.60f, 0.05f),
                new Vector2(0.88f, 0.95f),
                30,
                RetroUi.White,
                TextAnchor.MiddleRight,
                FontStyle.BoldAndItalic);
            _readySyncStrip.SetActive(false);

            _toastPanel = RetroUi.CreatePanel(
                transform,
                "ToastPanel",
                new Vector2(0.31f, 0.26f),
                new Vector2(0.69f, 0.36f),
                RetroUi.WithAlpha(RetroUi.TealDark, 0.92f),
                false).gameObject;
            _toastText = RetroUi.CreateText(
                _toastPanel.transform,
                "ToastText",
                "",
                Vector2.zero,
                Vector2.one,
                28,
                RetroUi.Green,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _toastPanel.SetActive(false);
        }

        private void HandleReadyButtonClicked()
        {
            _localReady = !_localReady;
            _readyButtonText.text = "CONFIRMAR";
            _readyButtonText.color = _localReady ? RetroUi.Yellow : RetroUi.White;

            Image buttonImage = _readyButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = _localReady ? RetroUi.TealDark : RetroUi.Red;
            }

            ColorBlock readyColors = _readyButton.colors;
            Color fillColor = _localReady ? RetroUi.TealDark : RetroUi.Red;
            readyColors.normalColor = fillColor;
            readyColors.highlightedColor = Color.Lerp(fillColor, RetroUi.White, 0.12f);
            readyColors.pressedColor = Color.Lerp(fillColor, RetroUi.Black, 0.18f);
            readyColors.selectedColor = fillColor;
            _readyButton.colors = readyColors;

            OnReadyPressed?.Invoke(_localReady);
        }
    }
}
