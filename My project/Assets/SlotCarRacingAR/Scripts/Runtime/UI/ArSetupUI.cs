using System;
using UnityEngine;
using UnityEngine.UI;
using SlotCarRacingAR.Runtime.Infrastructure;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// AR setup screen UI: shows scanning guidance, stability feedback, ready button, and ready-sync strip.
    /// Respects the protected AR zone (central 60% width x 55% height).
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
        private GameObject _connectionIndicator;
        private Text _connectionText;
        private float _toastTimer;
        private const float ToastDuration = 2.5f;
        private bool _localReady;

        /// <summary>Fired when local player presses Ready (true) or cancels (false).</summary>
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

        /// <summary>Show scanning guidance. Call when AR setup begins.</summary>
        public void ShowScanning()
        {
            gameObject.SetActive(true);
            _scanningPanel.SetActive(true);
            _stabilityPanel.SetActive(false);
            _toastPanel.SetActive(false);
        }

        /// <summary>Marker detected — show confirmation toast, switch to stability panel.</summary>
        public void ShowMarkerDetected()
        {
            _scanningPanel.SetActive(false);
            _stabilityPanel.SetActive(true);
            ShowToast("✓ Marcador detectado — evaluando estabilidad", new Color(0.2f, 0.9f, 0.6f));
            UpdateStability(TrackStabilityState.Unstable);
        }

        /// <summary>Update the stability status display.</summary>
        public void UpdateStability(TrackStabilityState state)
        {
            _stabilityPanel.SetActive(true);
            switch (state)
            {
                case TrackStabilityState.Unstable:
                    _stabilityText.text = "⟳ Estabilizando pista... mantén quieto el teléfono";
                    _stabilityText.color = new Color(1f, 0.843f, 0.25f); // amber
                    _readyPanel.SetActive(false);
                    break;
                case TrackStabilityState.Stable:
                    _stabilityText.text = "✓ Pista estable";
                    _stabilityText.color = new Color(0.2f, 0.9f, 0.4f); // green
                    ShowToast("✓ Pista lista", new Color(0.2f, 0.9f, 0.4f));
                    _readyPanel.SetActive(true);
                    break;
                case TrackStabilityState.Scanning:
                    _stabilityPanel.SetActive(false);
                    _scanningPanel.SetActive(true);
                    _readyPanel.SetActive(false);
                    break;
            }
        }

        /// <summary>Show tracking lost recovery message.</summary>
        public void ShowTrackingLost()
        {
            _stabilityPanel.SetActive(true);
            _stabilityText.text = "⚠ Tracking perdido — apunta al marcador de nuevo";
            _stabilityText.color = new Color(0.95f, 0.3f, 0.3f); // red
            _readyPanel.SetActive(false);
            _readySyncStrip.SetActive(false);
            _localReady = false;
        }

        /// <summary>Update the ready-sync strip with both players' readiness.</summary>
        public void UpdateReadySync(bool hostReady, bool guestReady)
        {
            _readySyncStrip.SetActive(true);
            _hostReadyLabel.text = hostReady ? "● Host LISTO" : "○ Host esperando";
            _hostReadyLabel.color = hostReady ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
            _guestReadyLabel.text = guestReady ? "● Guest LISTO" : "○ Guest esperando";
            _guestReadyLabel.color = guestReady ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
        }

        /// <summary>Revoke local ready state (e.g. after tracking lost).</summary>
        public void RevokeReady()
        {
            _localReady = false;
            _readyButtonText.text = "LISTO";
            _readyButtonText.color = Color.white;
            _readySyncStrip.SetActive(false);
        }

        /// <summary>Update the connection status indicator.</summary>
        public void UpdateConnectionStatus(string status, Color color)
        {
            if (_connectionIndicator != null)
            {
                _connectionIndicator.SetActive(true);
                _connectionText.text = status;
                _connectionText.color = color;
            }
        }

        /// <summary>Hide all UI.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ShowToast(string message, Color color)
        {
            _toastPanel.SetActive(true);
            _toastText.text = message;
            _toastText.color = color;
            _toastTimer = ToastDuration;
        }

        private void BuildUI()
        {
            // Canvas setup — screen space overlay, on top of camera feed
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // ── Scanning Panel (top band, outside protected AR zone) ──
            _scanningPanel = new GameObject("ScanningPanel");
            _scanningPanel.transform.SetParent(transform, false);
            RectTransform scanRect = _scanningPanel.AddComponent<RectTransform>();
            // Top band: top 15% of screen
            scanRect.anchorMin = new Vector2(0.1f, 0.78f);
            scanRect.anchorMax = new Vector2(0.9f, 0.95f);
            scanRect.offsetMin = Vector2.zero;
            scanRect.offsetMax = Vector2.zero;

            Image scanBg = _scanningPanel.AddComponent<Image>();
            scanBg.color = new Color(0f, 0f, 0f, 0.7f);

            // Scanning text
            GameObject scanTextObj = new GameObject("ScanningText");
            scanTextObj.transform.SetParent(_scanningPanel.transform, false);
            RectTransform textRect = scanTextObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 5f);
            textRect.offsetMax = new Vector2(-20f, -5f);

            _scanningText = scanTextObj.AddComponent<Text>();
            _scanningText.text = "Apunten ambos teléfonos al marcador en la mesa";
            _scanningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _scanningText.fontSize = 36;
            _scanningText.alignment = TextAnchor.MiddleCenter;
            _scanningText.color = Color.white;
            _scanningText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _scanningText.verticalOverflow = VerticalWrapMode.Truncate;

            // Animated dots via secondary text
            GameObject dotsObj = new GameObject("DotsHint");
            dotsObj.transform.SetParent(_scanningPanel.transform, false);
            RectTransform dotsRect = dotsObj.AddComponent<RectTransform>();
            dotsRect.anchorMin = new Vector2(0.3f, 0f);
            dotsRect.anchorMax = new Vector2(0.7f, 0.3f);
            dotsRect.offsetMin = Vector2.zero;
            dotsRect.offsetMax = Vector2.zero;

            Text dotsText = dotsObj.AddComponent<Text>();
            dotsText.text = "Buscando marcador...";
            dotsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dotsText.fontSize = 24;
            dotsText.alignment = TextAnchor.MiddleCenter;
            dotsText.color = new Color(1f, 0.843f, 0.25f); // amber

            // ── Toast Panel (bottom band, outside protected AR zone) ──
            _toastPanel = new GameObject("ToastPanel");
            _toastPanel.transform.SetParent(transform, false);
            RectTransform toastRect = _toastPanel.AddComponent<RectTransform>();
            // Bottom band: bottom 12% of screen
            toastRect.anchorMin = new Vector2(0.15f, 0.05f);
            toastRect.anchorMax = new Vector2(0.85f, 0.15f);
            toastRect.offsetMin = Vector2.zero;
            toastRect.offsetMax = Vector2.zero;

            Image toastBg = _toastPanel.AddComponent<Image>();
            toastBg.color = new Color(0f, 0.1f, 0.05f, 0.85f);

            // Toast text
            GameObject toastTextObj = new GameObject("ToastText");
            toastTextObj.transform.SetParent(_toastPanel.transform, false);
            RectTransform toastTextRect = toastTextObj.AddComponent<RectTransform>();
            toastTextRect.anchorMin = Vector2.zero;
            toastTextRect.anchorMax = Vector2.one;
            toastTextRect.offsetMin = new Vector2(10f, 5f);
            toastTextRect.offsetMax = new Vector2(-10f, -5f);

            _toastText = toastTextObj.AddComponent<Text>();
            _toastText.text = "";
            _toastText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _toastText.fontSize = 32;
            _toastText.alignment = TextAnchor.MiddleCenter;
            _toastText.color = new Color(0.2f, 0.9f, 0.6f);

            _toastPanel.SetActive(false);

            // ── Stability Panel (top band, same position as scanning) ──
            _stabilityPanel = new GameObject("StabilityPanel");
            _stabilityPanel.transform.SetParent(transform, false);
            RectTransform stabRect = _stabilityPanel.AddComponent<RectTransform>();
            stabRect.anchorMin = new Vector2(0.1f, 0.78f);
            stabRect.anchorMax = new Vector2(0.9f, 0.95f);
            stabRect.offsetMin = Vector2.zero;
            stabRect.offsetMax = Vector2.zero;

            Image stabBg = _stabilityPanel.AddComponent<Image>();
            stabBg.color = new Color(0f, 0f, 0f, 0.7f);

            GameObject stabTextObj = new GameObject("StabilityText");
            stabTextObj.transform.SetParent(_stabilityPanel.transform, false);
            RectTransform stabTextRect = stabTextObj.AddComponent<RectTransform>();
            stabTextRect.anchorMin = Vector2.zero;
            stabTextRect.anchorMax = Vector2.one;
            stabTextRect.offsetMin = new Vector2(20f, 5f);
            stabTextRect.offsetMax = new Vector2(-20f, -5f);

            _stabilityText = stabTextObj.AddComponent<Text>();
            _stabilityText.text = "";
            _stabilityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _stabilityText.fontSize = 32;
            _stabilityText.alignment = TextAnchor.MiddleCenter;
            _stabilityText.color = Color.white;
            _stabilityText.horizontalOverflow = HorizontalWrapMode.Wrap;

            _stabilityPanel.SetActive(false);

            // ── Ready Panel (center-bottom, above toast) ──
            _readyPanel = new GameObject("ReadyPanel");
            _readyPanel.transform.SetParent(transform, false);
            RectTransform readyRect = _readyPanel.AddComponent<RectTransform>();
            readyRect.anchorMin = new Vector2(0.3f, 0.17f);
            readyRect.anchorMax = new Vector2(0.7f, 0.30f);
            readyRect.offsetMin = Vector2.zero;
            readyRect.offsetMax = Vector2.zero;

            // Ready button
            GameObject btnObj = new GameObject("ReadyButton");
            btnObj.transform.SetParent(_readyPanel.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.1f, 0.6f, 0.3f, 0.9f);

            _readyButton = btnObj.AddComponent<Button>();
            _readyButton.targetGraphic = btnBg;
            _readyButton.onClick.AddListener(HandleReadyButtonClicked);

            GameObject btnTextObj = new GameObject("ButtonText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            _readyButtonText = btnTextObj.AddComponent<Text>();
            _readyButtonText.text = "LISTO";
            _readyButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _readyButtonText.fontSize = 42;
            _readyButtonText.alignment = TextAnchor.MiddleCenter;
            _readyButtonText.color = Color.white;
            _readyButtonText.fontStyle = FontStyle.Bold;

            _readyPanel.SetActive(false);

            // ── Ready-Sync Strip (just below stability panel) ──
            _readySyncStrip = new GameObject("ReadySyncStrip");
            _readySyncStrip.transform.SetParent(transform, false);
            RectTransform syncRect = _readySyncStrip.AddComponent<RectTransform>();
            syncRect.anchorMin = new Vector2(0.1f, 0.70f);
            syncRect.anchorMax = new Vector2(0.9f, 0.78f);
            syncRect.offsetMin = Vector2.zero;
            syncRect.offsetMax = Vector2.zero;

            Image syncBg = _readySyncStrip.AddComponent<Image>();
            syncBg.color = new Color(0f, 0f, 0f, 0.6f);

            // Host label (left half)
            GameObject hostLabelObj = new GameObject("HostReadyLabel");
            hostLabelObj.transform.SetParent(_readySyncStrip.transform, false);
            RectTransform hostRect = hostLabelObj.AddComponent<RectTransform>();
            hostRect.anchorMin = new Vector2(0f, 0f);
            hostRect.anchorMax = new Vector2(0.5f, 1f);
            hostRect.offsetMin = new Vector2(10f, 0f);
            hostRect.offsetMax = new Vector2(-5f, 0f);

            _hostReadyLabel = hostLabelObj.AddComponent<Text>();
            _hostReadyLabel.text = "○ Host esperando";
            _hostReadyLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _hostReadyLabel.fontSize = 28;
            _hostReadyLabel.alignment = TextAnchor.MiddleCenter;
            _hostReadyLabel.color = new Color(0.6f, 0.6f, 0.6f);

            // Guest label (right half)
            GameObject guestLabelObj = new GameObject("GuestReadyLabel");
            guestLabelObj.transform.SetParent(_readySyncStrip.transform, false);
            RectTransform guestRect = guestLabelObj.AddComponent<RectTransform>();
            guestRect.anchorMin = new Vector2(0.5f, 0f);
            guestRect.anchorMax = new Vector2(1f, 1f);
            guestRect.offsetMin = new Vector2(5f, 0f);
            guestRect.offsetMax = new Vector2(-10f, 0f);

            _guestReadyLabel = guestLabelObj.AddComponent<Text>();
            _guestReadyLabel.text = "○ Guest esperando";
            _guestReadyLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _guestReadyLabel.fontSize = 28;
            _guestReadyLabel.alignment = TextAnchor.MiddleCenter;
            _guestReadyLabel.color = new Color(0.6f, 0.6f, 0.6f);

            _readySyncStrip.SetActive(false);

            // ── Connection Status Indicator (top-right corner) ──
            _connectionIndicator = new GameObject("ConnectionIndicator");
            _connectionIndicator.transform.SetParent(transform, false);
            RectTransform connRect = _connectionIndicator.AddComponent<RectTransform>();
            connRect.anchorMin = new Vector2(0.6f, 0.95f);
            connRect.anchorMax = new Vector2(0.98f, 1.0f);
            connRect.offsetMin = Vector2.zero;
            connRect.offsetMax = Vector2.zero;

            GameObject connTextObj = new GameObject("ConnectionText");
            connTextObj.transform.SetParent(_connectionIndicator.transform, false);
            RectTransform connTextRect = connTextObj.AddComponent<RectTransform>();
            connTextRect.anchorMin = Vector2.zero;
            connTextRect.anchorMax = Vector2.one;
            connTextRect.offsetMin = new Vector2(5f, 0f);
            connTextRect.offsetMax = new Vector2(-5f, 0f);

            _connectionText = connTextObj.AddComponent<Text>();
            _connectionText.text = "● Conectando...";
            _connectionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _connectionText.fontSize = 22;
            _connectionText.alignment = TextAnchor.MiddleRight;
            _connectionText.color = new Color(1f, 0.843f, 0.25f);

            _connectionIndicator.SetActive(true);
        }

        private void HandleReadyButtonClicked()
        {
            _localReady = !_localReady;
            _readyButtonText.text = _localReady ? "CANCELAR" : "LISTO";
            _readyButtonText.color = _localReady ? new Color(0.95f, 0.6f, 0.3f) : Color.white;

            Image btnBg = _readyButton.GetComponent<Image>();
            if (btnBg != null)
            {
                btnBg.color = _localReady
                    ? new Color(0.5f, 0.3f, 0.1f, 0.9f)
                    : new Color(0.1f, 0.6f, 0.3f, 0.9f);
            }

            OnReadyPressed?.Invoke(_localReady);
        }
    }
}
