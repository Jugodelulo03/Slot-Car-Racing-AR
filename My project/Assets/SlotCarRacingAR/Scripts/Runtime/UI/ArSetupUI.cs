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
        private GameObject _rescanPanel;
        private Button _rescanButton;
        private GameObject _readySyncStrip;
        private readonly Image[] _readyPanelImages = new Image[SharedLobbyState.MaxPlayers + 1];
        private readonly Image[] _readyLights = new Image[SharedLobbyState.MaxPlayers + 1];
        private readonly Text[] _readyLabels = new Text[SharedLobbyState.MaxPlayers + 1];
        private Image _hostReadyPanelImage;
        private Image _guestReadyPanelImage;
        private GameObject _guidePanel;
        private Text _guideStepText;
        private Text _guideTitleText;
        private Text _guideBodyText;
        private Text _hostReadyLabel;
        private Text _guestReadyLabel;
        private Image _hostReadyLight;
        private Image _guestReadyLight;
        private float _toastTimer;
        private const float ToastDuration = 2.5f;
        private bool _localReady;

        public event Action<bool> OnReadyPressed;
        public event Action OnRescanPressed;

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

        private void OnDestroy()
        {
            if (_readyButton != null)
            {
                _readyButton.onClick.RemoveListener(HandleReadyButtonClicked);
            }

            if (_rescanButton != null)
            {
                _rescanButton.onClick.RemoveListener(HandleRescanButtonClicked);
            }
        }

        public void ShowScanning()
        {
            gameObject.SetActive(true);
            _scanningPanel.SetActive(true);
            _stabilityPanel.SetActive(false);
            _toastPanel.SetActive(false);
            _readyPanel.SetActive(false);
            SetRescanVisible(false);
            _localReady = false;
            ApplyReadyButtonVisual(false);
            SetGuide(
                "PASO 1/3",
                "APUNTA AL MARCADOR",
                "Pon el marcador plano sobre la mesa y encuadralo con la camara.",
                RetroUi.Yellow);
        }

        public void ShowMarkerDetected()
        {
            _scanningPanel.SetActive(false);
            _stabilityPanel.SetActive(true);
            SetRescanVisible(true);
            GameAudio.Play(GameSfx.MarkerFound);
            ShowToast("Marcador detectado", RetroUi.Green);
            SetGuide(
                "PASO 2/3",
                "MANTEN EL CELULAR QUIETO",
                "Espera a que la pista quede estable antes de confirmar.",
                RetroUi.Green);
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
                    SetRescanVisible(true);
                    SetGuide(
                        "PASO 2/3",
                        "MANTEN EL CELULAR QUIETO",
                        "Si la pista se mueve, baja un poco el celular y espera.",
                        RetroUi.Yellow);
                    break;
                case TrackStabilityState.Stable:
                    _stabilityText.text = "PISTA ESTABLE";
                    _stabilityText.color = RetroUi.Green;
                    GameAudio.Play(GameSfx.Ready);
                    ShowToast("Pista lista", RetroUi.Green);
                    _readyPanel.SetActive(true);
                    SetRescanVisible(true);
                    SetGuide(
                        "PASO 3/3",
                        "AJUSTA Y CONFIRMA",
                        "Usa escala y altura solo si la pista no encaja. Luego toca Confirmar.",
                        RetroUi.Green);
                    break;
                case TrackStabilityState.Scanning:
                    _stabilityPanel.SetActive(false);
                    _scanningPanel.SetActive(true);
                    _readyPanel.SetActive(false);
                    SetRescanVisible(false);
                    SetGuide(
                        "PASO 1/3",
                        "APUNTA AL MARCADOR",
                        "Busca el marcador impreso para crear la pista AR.",
                        RetroUi.Yellow);
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
            SetRescanVisible(true);
            GameAudio.Play(GameSfx.Error);
            SetGuide(
                "RECUPERA TRACKING",
                "VUELVE AL MARCADOR",
                "Apunta de nuevo a la pista y manten el celular quieto.",
                RetroUi.Red);
            _localReady = false;
        }

        public void UpdateReadySync(bool hostReady, bool guestReady)
        {
            _readySyncStrip.SetActive(true);
            ApplyReadyBadge(1, true, hostReady, "HOST");
            ApplyReadyBadge(2, true, guestReady, "P2");
            ApplyReadyBadge(3, false, false, "P3");
            ApplyReadyBadge(4, false, false, "P4");

            if (hostReady && guestReady)
            {
                SetRescanVisible(false);
                SetGuide(
                    "LISTOS",
                    "ARRANCA LA CARRERA",
                    "La pista queda bloqueada. Ya no se puede ajustar escala ni altura.",
                    RetroUi.Green);
            }
            else if (_localReady)
            {
                SetRescanVisible(true);
                SetGuide(
                    "ESPERANDO",
                    "FALTAN JUGADORES",
                    "Mantente apuntando a la pista mientras el resto confirma.",
                    RetroUi.Yellow);
            }
        }

        public void UpdateReadySync(SharedLobbyState sharedState)
        {
            if (sharedState == null)
            {
                UpdateReadySync(false, false);
                return;
            }

            _readySyncStrip.SetActive(true);
            for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
            {
                ApplyReadyBadge(playerId, sharedState.HasPlayer(playerId), sharedState.GetReady(playerId), GetPlayerLabel(playerId));
            }

            if (sharedState.AllReady)
            {
                SetRescanVisible(false);
                SetGuide(
                    "LISTOS",
                    "ARRANCA LA CARRERA",
                    "La pista queda bloqueada. Ya no se puede ajustar escala ni altura.",
                    RetroUi.Green);
            }
            else if (_localReady)
            {
                SetRescanVisible(true);
                SetGuide(
                    "ESPERANDO",
                    "FALTAN JUGADORES",
                    "Mantente apuntando a la pista mientras todos confirman.",
                    RetroUi.Yellow);
            }
        }

        public void RevokeReady()
        {
            _localReady = false;
            ApplyReadyButtonVisual(false);
            _readySyncStrip.SetActive(false);
            SetRescanVisible(true);
            SetGuide(
                "PASO 3/3",
                "CONFIRMA DE NUEVO",
                "El tracking cambio. Estabiliza la pista y vuelve a confirmar.",
                RetroUi.Yellow);
        }

        public void UpdateConnectionStatus(string status, Color color)
        {
            // Intentionally hidden in the race setup view to keep the track view clear.
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetGuide(string step, string title, string body, Color accent)
        {
            if (_guidePanel == null)
            {
                return;
            }

            _guidePanel.SetActive(true);
            _guideStepText.text = step;
            _guideStepText.color = accent;
            _guideTitleText.text = title;
            _guideBodyText.text = body;
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
                "Todos deben apuntar al mismo marcador",
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

            _guidePanel = RetroUi.CreatePanel(
                transform,
                "FirstRunGuide",
                new Vector2(0.03f, 0.16f),
                new Vector2(0.34f, 0.40f),
                RetroUi.WithAlpha(RetroUi.TealDark, 0.90f),
                false).gameObject;

            _guideStepText = RetroUi.CreateText(
                _guidePanel.transform,
                "GuideStep",
                "PASO 1/3",
                new Vector2(0.07f, 0.68f),
                new Vector2(0.93f, 0.94f),
                20,
                RetroUi.Yellow,
                TextAnchor.MiddleLeft,
                FontStyle.BoldAndItalic);

            _guideTitleText = RetroUi.CreateText(
                _guidePanel.transform,
                "GuideTitle",
                "APUNTA AL MARCADOR",
                new Vector2(0.07f, 0.43f),
                new Vector2(0.93f, 0.72f),
                25,
                RetroUi.White,
                TextAnchor.MiddleLeft,
                FontStyle.BoldAndItalic);

            _guideBodyText = RetroUi.CreateText(
                _guidePanel.transform,
                "GuideBody",
                "Pon el marcador plano sobre la mesa y encuadralo con la camara.",
                new Vector2(0.07f, 0.08f),
                new Vector2(0.93f, 0.42f),
                19,
                RetroUi.Cream,
                TextAnchor.MiddleLeft,
                FontStyle.BoldAndItalic);

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

            _rescanPanel = RetroUi.CreatePanel(
                transform,
                "RescanPanel",
                new Vector2(0.03f, 0.04f),
                new Vector2(0.23f, 0.13f),
                RetroUi.Yellow,
                true).gameObject;

            _rescanButton = _rescanPanel.AddComponent<Button>();
            _rescanButton.targetGraphic = _rescanPanel.GetComponent<Image>();
            ColorBlock rescanColors = _rescanButton.colors;
            rescanColors.normalColor = RetroUi.Yellow;
            rescanColors.highlightedColor = Color.Lerp(RetroUi.Yellow, RetroUi.White, 0.12f);
            rescanColors.pressedColor = Color.Lerp(RetroUi.Yellow, RetroUi.Black, 0.18f);
            rescanColors.selectedColor = RetroUi.Yellow;
            _rescanButton.colors = rescanColors;
            _rescanButton.onClick.AddListener(HandleRescanButtonClicked);
            _rescanPanel.AddComponent<RetroButtonPress>();

            Text rescanText = RetroUi.CreateText(
                _rescanPanel.transform,
                "RescanButtonText",
                "REESCANEAR",
                Vector2.zero,
                Vector2.one,
                22,
                RetroUi.TealDark,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);
            rescanText.resizeTextForBestFit = true;
            rescanText.resizeTextMinSize = 14;
            rescanText.resizeTextMaxSize = 22;
            _rescanPanel.SetActive(false);

            _readySyncStrip = new GameObject("ReadySyncStrip");
            _readySyncStrip.transform.SetParent(transform, false);
            RectTransform readySyncRect = _readySyncStrip.AddComponent<RectTransform>();
            RetroUi.Fill(readySyncRect);

            CreateReadyBadge(1, new Vector2(0.03f, 0.86f), new Vector2(0.25f, 0.97f), false);
            CreateReadyBadge(3, new Vector2(0.03f, 0.73f), new Vector2(0.25f, 0.84f), false);
            CreateReadyBadge(2, new Vector2(0.75f, 0.86f), new Vector2(0.97f, 0.97f), true);
            CreateReadyBadge(4, new Vector2(0.75f, 0.73f), new Vector2(0.97f, 0.84f), true);

            _hostReadyPanelImage = _readyPanelImages[1];
            _guestReadyPanelImage = _readyPanelImages[2];
            _hostReadyLight = _readyLights[1];
            _guestReadyLight = _readyLights[2];
            _hostReadyLabel = _readyLabels[1];
            _guestReadyLabel = _readyLabels[2];
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

        private void CreateReadyBadge(byte playerId, Vector2 anchorMin, Vector2 anchorMax, bool lightOnRight)
        {
            RectTransform readyPanel = RetroUi.CreatePanel(
                _readySyncStrip.transform,
                "ReadyPanel_P" + playerId,
                anchorMin,
                anchorMax,
                RetroUi.WithAlpha(RetroUi.TealDark, 0.92f),
                false);

            _readyPanelImages[playerId] = readyPanel.GetComponent<Image>();
            _readyLights[playerId] = RetroUi.CreateStatusLight(
                readyPanel,
                "ReadyLight_P" + playerId,
                lightOnRight ? new Vector2(0.77f, 0.17f) : new Vector2(0.08f, 0.17f),
                lightOnRight ? new Vector2(0.93f, 0.83f) : new Vector2(0.24f, 0.83f),
                RetroUi.Red);

            _readyLabels[playerId] = RetroUi.CreateText(
                readyPanel,
                "ReadyLabel_P" + playerId,
                GetPlayerLabel(playerId) + "\nESPERA",
                lightOnRight ? new Vector2(0.06f, 0.06f) : new Vector2(0.30f, 0.06f),
                lightOnRight ? new Vector2(0.72f, 0.94f) : new Vector2(0.94f, 0.94f),
                22,
                RetroUi.White,
                lightOnRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
                FontStyle.BoldAndItalic);
            _readyLabels[playerId].resizeTextForBestFit = true;
            _readyLabels[playerId].resizeTextMinSize = 14;
            _readyLabels[playerId].resizeTextMaxSize = 22;
        }

        private void ApplyReadyBadge(byte playerId, bool hasPlayer, bool ready, string label)
        {
            if (playerId < 1 || playerId > SharedLobbyState.MaxPlayers || _readyLabels[playerId] == null)
            {
                return;
            }

            GameObject badge = _readyLabels[playerId].transform.parent.gameObject;
            badge.SetActive(hasPlayer);

            _readyLabels[playerId].text = label + "\n" + (ready ? "LISTO" : "ESPERA");
            _readyLabels[playerId].color = ready ? RetroUi.Green : RetroUi.White;
            _readyLights[playerId].color = ready ? RetroUi.Green : RetroUi.Red;
            _readyPanelImages[playerId].color = RetroUi.WithAlpha(ready ? RetroUi.Teal : RetroUi.TealDark, 0.92f);
        }

        private static string GetPlayerLabel(byte playerId)
        {
            return playerId == 1 ? "HOST" : "P" + playerId;
        }

        private void HandleReadyButtonClicked()
        {
            _localReady = !_localReady;
            ApplyReadyButtonVisual(_localReady);

            if (_localReady)
            {
                GameAudio.Play(GameSfx.Ready);
                SetGuide(
                    "ESPERANDO",
                    "CONFIRMADO",
                    "Cuando todos esten listos aparecera el contador 3, 2, 1.",
                    RetroUi.Green);
            }
            else
            {
                SetGuide(
                    "PASO 3/3",
                    "AJUSTA Y CONFIRMA",
                    "Usa escala y altura solo si la pista no encaja. Luego toca Confirmar.",
                    RetroUi.Yellow);
            }

            OnReadyPressed?.Invoke(_localReady);
        }

        private void HandleRescanButtonClicked()
        {
            _localReady = false;
            ApplyReadyButtonVisual(false);
            SetRescanVisible(false);
            OnRescanPressed?.Invoke();
        }

        private void SetRescanVisible(bool visible)
        {
            if (_rescanPanel != null)
            {
                _rescanPanel.SetActive(visible);
            }
        }

        private void ApplyReadyButtonVisual(bool ready)
        {
            if (_readyButtonText != null)
            {
                _readyButtonText.text = "CONFIRMAR";
                _readyButtonText.color = ready ? RetroUi.Yellow : RetroUi.White;
            }

            Color fillColor = ready ? RetroUi.TealDark : RetroUi.Red;
            if (_readyButton != null)
            {
                Image buttonImage = _readyButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = fillColor;
                }

                ColorBlock readyColors = _readyButton.colors;
                readyColors.normalColor = fillColor;
                readyColors.highlightedColor = Color.Lerp(fillColor, RetroUi.White, 0.12f);
                readyColors.pressedColor = Color.Lerp(fillColor, RetroUi.Black, 0.18f);
                readyColors.selectedColor = fillColor;
                _readyButton.colors = readyColors;
            }
        }
    }
}
