using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Displays host session status after "Create Match" is pressed.
    /// </summary>
    public sealed class LobbySessionUI : MonoBehaviour
    {
        private Text _statusText;
        private Text _detailText;
        private Text _sessionCodeText;
        private Text _networkText;
        private Image _networkLight;
        private Button _retryButton;
        private Button _backButton;
        private Button _cancelButton;
        private GameObject _failureButtonPanel;
        private GameObject _cancelButtonObj;

        public event System.Action OnRetryClicked;
        public event System.Action OnBackClicked;

        private void Awake()
        {
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
            }
        }

        public void UpdateState(SessionState state, string ipAddress, string failureReason)
        {
            switch (state)
            {
                case SessionState.Creating:
                    _statusText.text = "CREANDO SESION";
                    _statusText.color = RetroUi.Yellow;
                    _detailText.text = "Iniciando servidor local...";
                    _sessionCodeText.text = "--";
                    _networkText.text = "Preparando red local";
                    _networkLight.color = RetroUi.Yellow;
                    SetFailureButtonsVisible(false);
                    SetCancelVisible(true);
                    break;

                case SessionState.WaitingForPlayer:
                    _statusText.text = "ESPERANDO RIVAL";
                    _statusText.color = RetroUi.Yellow;
                    _detailText.text = "Pide a tu companero que abra la app y se una.";
                    _sessionCodeText.text = string.IsNullOrWhiteSpace(ipAddress) ? "SIN IP" : ipAddress;
                    _networkText.text = "Conectado a red local";
                    _networkLight.color = RetroUi.Green;
                    SetFailureButtonsVisible(false);
                    SetCancelVisible(true);
                    break;

                case SessionState.Connected:
                    _statusText.text = "RIVAL CONECTADO";
                    _statusText.color = RetroUi.Green;
                    _detailText.text = "Preparando la carrera...";
                    _sessionCodeText.text = string.IsNullOrWhiteSpace(ipAddress) ? "OK" : ipAddress;
                    _networkText.text = "Conexion lista";
                    _networkLight.color = RetroUi.Green;
                    SetFailureButtonsVisible(false);
                    SetCancelVisible(false);
                    break;

                case SessionState.Failed:
                    _statusText.text = "ERROR DE CONEXION";
                    _statusText.color = RetroUi.Red;
                    _detailText.text = string.IsNullOrWhiteSpace(failureReason) ? "No se pudo crear la sesion." : failureReason;
                    _sessionCodeText.text = "ERROR";
                    _networkText.text = "Revisa Wi-Fi o hotspot";
                    _networkLight.color = RetroUi.Red;
                    SetFailureButtonsVisible(true);
                    SetCancelVisible(false);
                    break;

                default:
                    gameObject.SetActive(false);
                    break;
            }
        }

        private void SetFailureButtonsVisible(bool visible)
        {
            if (_failureButtonPanel != null)
            {
                _failureButtonPanel.SetActive(visible);
            }
        }

        private void SetCancelVisible(bool visible)
        {
            if (_cancelButtonObj != null)
            {
                _cancelButtonObj.SetActive(visible);
            }
        }

        private void BuildUI()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            RetroUi.CreateFullScreenBackground(root, "SessionBackground", true);

            RectTransform panel = RetroUi.CreatePanel(
                root,
                "HostSessionPanel",
                new Vector2(0.06f, 0.12f),
                new Vector2(0.94f, 0.88f),
                RetroUi.Teal,
                true);

            RetroUi.CreateLogo(
                panel,
                "Face2RaceLogo",
                new Vector2(0.08f, 0.74f),
                new Vector2(0.40f, 1.00f));

            RectTransform codePlate = RetroUi.CreatePanel(
                panel,
                "CodePlate",
                new Vector2(0.10f, 0.48f),
                new Vector2(0.42f, 0.74f),
                RetroUi.CreamLight,
                false);

            RetroUi.CreateText(
                codePlate,
                "CodeLabel",
                "SESION ACTIVA",
                new Vector2(0.10f, 0.62f),
                new Vector2(0.90f, 0.96f),
                24,
                RetroUi.Black,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);

            _sessionCodeText = RetroUi.CreateText(
                codePlate,
                "SessionCode",
                "--",
                new Vector2(0.05f, 0.02f),
                new Vector2(0.95f, 0.66f),
                44,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _sessionCodeText.resizeTextForBestFit = true;
            _sessionCodeText.resizeTextMinSize = 22;
            _sessionCodeText.resizeTextMaxSize = 44;

            RectTransform networkPlate = RetroUi.CreatePanel(
                panel,
                "NetworkPlate",
                new Vector2(0.10f, 0.37f),
                new Vector2(0.44f, 0.47f),
                RetroUi.WithAlpha(RetroUi.TealDark, 0.90f),
                false);

            _networkLight = RetroUi.CreateStatusLight(
                networkPlate,
                "NetworkLight",
                new Vector2(0.05f, 0.18f),
                new Vector2(0.13f, 0.82f),
                RetroUi.Yellow);

            _networkText = RetroUi.CreateText(
                networkPlate,
                "NetworkText",
                "Preparando red local",
                new Vector2(0.14f, 0.02f),
                new Vector2(0.95f, 0.98f),
                24,
                RetroUi.White,
                TextAnchor.MiddleLeft,
                FontStyle.BoldAndItalic);

            RectTransform statusCard = RetroUi.CreatePanel(
                panel,
                "StatusCard",
                new Vector2(0.08f, 0.12f),
                new Vector2(0.44f, 0.34f),
                RetroUi.CreamLight,
                false);

            _statusText = RetroUi.CreateText(
                statusCard,
                "StatusText",
                "CREANDO SESION",
                new Vector2(0.08f, 0.52f),
                new Vector2(0.92f, 0.94f),
                30,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _detailText = RetroUi.CreateText(
                statusCard,
                "DetailText",
                "Iniciando servidor local...",
                new Vector2(0.08f, 0.08f),
                new Vector2(0.92f, 0.50f),
                24,
                RetroUi.Black,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);

            RetroUi.CreateText(
                panel,
                "MarkerPrompt",
                "COLOCA EL MARCADOR\nSOBRE LA MESA",
                new Vector2(0.55f, 0.48f),
                new Vector2(0.91f, 0.72f),
                36,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            RetroUi.CreateText(
                panel,
                "MarkerHint",
                "Ambos deben apuntar al mismo marcador",
                new Vector2(0.55f, 0.32f),
                new Vector2(0.91f, 0.46f),
                26,
                RetroUi.Cream,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _cancelButton = RetroUi.CreateButton(
                root,
                "CancelButton",
                "Cancelar",
                new Vector2(0.58f, 0.03f),
                new Vector2(0.76f, 0.12f),
                RetroUi.Red,
                RetroUi.White,
                30);
            _cancelButtonObj = _cancelButton.gameObject;
            _cancelButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            _failureButtonPanel = new GameObject("FailureButtons");
            _failureButtonPanel.transform.SetParent(root, false);
            RectTransform failRect = _failureButtonPanel.AddComponent<RectTransform>();
            failRect.anchorMin = new Vector2(0.22f, 0.03f);
            failRect.anchorMax = new Vector2(0.78f, 0.15f);
            failRect.offsetMin = Vector2.zero;
            failRect.offsetMax = Vector2.zero;

            _retryButton = RetroUi.CreateButton(
                failRect,
                "RetryButton",
                "Reintentar",
                new Vector2(0.04f, 0.10f),
                new Vector2(0.48f, 0.90f),
                RetroUi.Yellow,
                RetroUi.White,
                30);
            _retryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());

            _backButton = RetroUi.CreateButton(
                failRect,
                "BackButton",
                "Volver",
                new Vector2(0.52f, 0.10f),
                new Vector2(0.96f, 0.90f),
                RetroUi.Red,
                RetroUi.White,
                30);
            _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            SetFailureButtonsVisible(false);
        }
    }
}
