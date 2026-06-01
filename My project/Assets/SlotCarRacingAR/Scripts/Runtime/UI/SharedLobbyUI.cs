using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Shared lobby display: shows both players' connection status and next step.
    /// </summary>
    public sealed class SharedLobbyUI : MonoBehaviour
    {
        private Text _player1Status;
        private Text _player2Status;
        private Text _guidanceText;
        private Text _confirmationText;
        private Button _continueButton;
        private GameObject _continueButtonObj;
        private Image _guestLight;
        private float _confirmationTimer;

        public event System.Action OnContinueClicked;

        private void Awake()
        {
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveAllListeners();
            }
        }

        private void Update()
        {
            if (_confirmationTimer > 0f)
            {
                _confirmationTimer -= Time.deltaTime;
                if (_confirmationTimer <= 0f)
                {
                    _confirmationText.gameObject.SetActive(false);
                }
            }
        }

        public void UpdatePlayerCount(byte playerCount, PlayerRole localRole, string localIp = "")
        {
            string ipSuffix = string.IsNullOrEmpty(localIp) ? "" : "  [" + localIp + "]";
            _player1Status.text = localRole == PlayerRole.Host
                ? "HOST CONECTADO" + ipSuffix
                : "HOST CONECTADO";

            if (playerCount >= 2)
            {
                _player2Status.text = localRole == PlayerRole.Guest
                    ? "INVITADO CONECTADO" + ipSuffix
                    : "INVITADO CONECTADO";
                _player2Status.color = RetroUi.Green;
                if (_guestLight != null)
                {
                    _guestLight.color = RetroUi.Green;
                }

                _guidanceText.text = "Coloca el marcador sobre la mesa. Ambos deben apuntar al mismo marcador.";
                _guidanceText.color = RetroUi.White;
                SetContinueVisible(true);
            }
            else
            {
                _player2Status.text = "ESPERANDO RIVAL";
                _player2Status.color = RetroUi.Yellow;
                if (_guestLight != null)
                {
                    _guestLight.color = RetroUi.Yellow;
                }

                _guidanceText.text = "Comparte el codigo o espera a que tu rival aparezca en la red local.";
                _guidanceText.color = RetroUi.Yellow;
                SetContinueVisible(false);
            }
        }

        public void ShowDisconnected()
        {
            _player2Status.text = "INVITADO DESCONECTADO";
            _player2Status.color = RetroUi.Red;
            if (_guestLight != null)
            {
                _guestLight.color = RetroUi.Red;
            }

            _guidanceText.text = "Esperando reconexion...";
            _guidanceText.color = RetroUi.Red;
            SetContinueVisible(false);
        }

        public void ShowConnectionConfirmation()
        {
            _confirmationText.text = "Jugador 2 conectado!";
            _confirmationText.color = RetroUi.Green;
            _confirmationText.gameObject.SetActive(true);
            _confirmationTimer = 2.5f;
        }

        private void SetContinueVisible(bool visible)
        {
            if (_continueButtonObj != null)
            {
                _continueButtonObj.SetActive(visible);
            }
        }

        private void BuildUI()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            RetroUi.CreateFullScreenBackground(root, "SharedLobbyBackground", true);

            RectTransform panel = RetroUi.CreatePanel(
                root,
                "SessionPanel",
                new Vector2(0.07f, 0.12f),
                new Vector2(0.93f, 0.86f),
                RetroUi.Teal,
                true);

            RetroUi.CreateLogo(
                panel,
                "Face2RaceLogo",
                new Vector2(0.08f, 0.74f),
                new Vector2(0.42f, 1.00f));

            RetroUi.CreateText(
                panel,
                "SessionTitle",
                "SESION ACTIVA",
                new Vector2(0.08f, 0.61f),
                new Vector2(0.42f, 0.72f),
                28,
                RetroUi.Black,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);

            _confirmationText = RetroUi.CreateText(
                panel,
                "Confirmation",
                "",
                new Vector2(0.10f, 0.49f),
                new Vector2(0.43f, 0.59f),
                28,
                RetroUi.Green,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _confirmationText.gameObject.SetActive(false);

            RectTransform statusCard = RetroUi.CreatePanel(
                panel,
                "StatusCard",
                new Vector2(0.08f, 0.23f),
                new Vector2(0.44f, 0.49f),
                RetroUi.CreamLight,
                false);

            RetroUi.CreateStatusLight(statusCard, "HostLight", new Vector2(0.06f, 0.58f), new Vector2(0.17f, 0.82f), RetroUi.Red);
            _guestLight = RetroUi.CreateStatusLight(statusCard, "GuestLight", new Vector2(0.06f, 0.18f), new Vector2(0.17f, 0.42f), RetroUi.Yellow);

            _player1Status = RetroUi.CreateText(
                statusCard,
                "Player1Status",
                "HOST CONECTADO",
                new Vector2(0.18f, 0.52f),
                new Vector2(0.94f, 0.88f),
                26,
                RetroUi.Red,
                TextAnchor.MiddleLeft,
                FontStyle.BoldAndItalic);

            _player2Status = RetroUi.CreateText(
                statusCard,
                "Player2Status",
                "ESPERANDO RIVAL",
                new Vector2(0.18f, 0.12f),
                new Vector2(0.94f, 0.48f),
                26,
                RetroUi.Yellow,
                TextAnchor.MiddleLeft,
                FontStyle.BoldAndItalic);

            RetroUi.CreateText(
                panel,
                "MarkerPrompt",
                "COLOCA EL MARCADOR\nSOBRE LA MESA",
                new Vector2(0.55f, 0.48f),
                new Vector2(0.91f, 0.71f),
                34,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _guidanceText = RetroUi.CreateText(
                panel,
                "Guidance",
                "Comparte el codigo o espera a tu rival.",
                new Vector2(0.53f, 0.28f),
                new Vector2(0.93f, 0.45f),
                28,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _continueButton = RetroUi.CreateButton(
                root,
                "ContinueButton",
                "Continuar",
                new Vector2(0.32f, 0.04f),
                new Vector2(0.68f, 0.15f),
                RetroUi.Red,
                RetroUi.White,
                34);
            _continueButtonObj = _continueButton.gameObject;
            _continueButton.onClick.AddListener(() => OnContinueClicked?.Invoke());
            _continueButtonObj.SetActive(false);
        }
    }
}
