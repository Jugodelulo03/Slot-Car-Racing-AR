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
        private readonly Text[] _playerStatusLabels = new Text[SharedLobbyState.MaxPlayers + 1];
        private readonly Image[] _playerStatusLights = new Image[SharedLobbyState.MaxPlayers + 1];
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
            for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
            {
                bool connected = playerId <= playerCount;
                string label = GetPlayerLabel(playerId);
                if (connected)
                {
                    bool showIp = localRole == PlayerRole.Host && playerId == 1;
                    SetPlayerStatus(playerId, label + " CONECTADO" + (showIp ? ipSuffix : ""), RetroUi.Green, RetroUi.Green);
                }
                else
                {
                    SetPlayerStatus(playerId, "P" + playerId + " ESPERANDO", RetroUi.Yellow, RetroUi.Yellow);
                }
            }

            if (playerCount >= 2)
            {
                _guidanceText.text = playerCount + " jugadores conectados. Pueden empezar ahora o esperar hasta 4.";
                _guidanceText.color = RetroUi.White;
                SetContinueVisible(true);
            }
            else
            {
                _guidanceText.text = "Comparte el codigo o espera a que tus rivales aparezcan en la red local.";
                _guidanceText.color = RetroUi.Yellow;
                SetContinueVisible(false);
            }
        }

        public void UpdatePlayerSlots(SharedLobbyState sharedState, PlayerRole localRole, string localIp = "")
        {
            if (sharedState == null)
            {
                UpdatePlayerCount(0, localRole, localIp);
                return;
            }

            string ipSuffix = string.IsNullOrEmpty(localIp) ? "" : "  [" + localIp + "]";
            byte connectedCount = sharedState.PlayerCount.Value;
            for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
            {
                bool connected = sharedState.HasPlayer(playerId);
                if (connected)
                {
                    bool showIp = localRole == PlayerRole.Host && playerId == 1;
                    SetPlayerStatus(playerId, GetPlayerLabel(playerId) + " CONECTADO" + (showIp ? ipSuffix : ""), RetroUi.Green, RetroUi.Green);
                }
                else
                {
                    SetPlayerStatus(playerId, "P" + playerId + " ESPERANDO", RetroUi.Yellow, RetroUi.Yellow);
                }
            }

            if (connectedCount >= 2)
            {
                _guidanceText.text = connectedCount + " jugadores conectados. Pueden empezar ahora o esperar hasta 4.";
                _guidanceText.color = RetroUi.White;
                SetContinueVisible(true);
            }
            else
            {
                _guidanceText.text = "Comparte el codigo o espera a que tus rivales aparezcan en la red local.";
                _guidanceText.color = RetroUi.Yellow;
                SetContinueVisible(false);
            }
        }

        public void ShowDisconnected()
        {
            _player2Status.text = "RIVAL DESCONECTADO";
            _player2Status.color = RetroUi.Red;
            if (_guestLight != null)
            {
                _guestLight.color = RetroUi.Red;
            }

            _guidanceText.text = "Esperando reconexion o nuevos jugadores...";
            _guidanceText.color = RetroUi.Red;
            SetContinueVisible(false);
        }

        public void ShowConnectionConfirmation()
        {
            _confirmationText.text = "Jugador conectado!";
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
                new Vector2(0.08f, 0.18f),
                new Vector2(0.46f, 0.52f),
                RetroUi.CreamLight,
                false);

            CreatePlayerStatusRow(statusCard, 1, 0.75f, "HOST CONECTADO", RetroUi.Green);
            CreatePlayerStatusRow(statusCard, 2, 0.51f, "P2 ESPERANDO", RetroUi.Yellow);
            CreatePlayerStatusRow(statusCard, 3, 0.27f, "P3 ESPERANDO", RetroUi.Yellow);
            CreatePlayerStatusRow(statusCard, 4, 0.03f, "P4 ESPERANDO", RetroUi.Yellow);

            _player1Status = _playerStatusLabels[1];
            _player2Status = _playerStatusLabels[2];
            _guestLight = _playerStatusLights[2];

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

        private void CreatePlayerStatusRow(RectTransform parent, byte playerId, float yMin, string text, Color color)
        {
            _playerStatusLights[playerId] = RetroUi.CreateStatusLight(
                parent,
                "Player" + playerId + "Light",
                new Vector2(0.06f, yMin + 0.035f),
                new Vector2(0.16f, yMin + 0.185f),
                color);

            _playerStatusLabels[playerId] = RetroUi.CreateText(
                parent,
                "Player" + playerId + "Status",
                text,
                new Vector2(0.19f, yMin),
                new Vector2(0.94f, yMin + 0.22f),
                23,
                color,
                TextAnchor.MiddleLeft,
                FontStyle.BoldAndItalic);
            _playerStatusLabels[playerId].resizeTextForBestFit = true;
            _playerStatusLabels[playerId].resizeTextMinSize = 15;
            _playerStatusLabels[playerId].resizeTextMaxSize = 23;
        }

        private void SetPlayerStatus(byte playerId, string text, Color textColor, Color lightColor)
        {
            if (playerId < 1 || playerId > SharedLobbyState.MaxPlayers)
            {
                return;
            }

            if (_playerStatusLabels[playerId] != null)
            {
                _playerStatusLabels[playerId].text = text;
                _playerStatusLabels[playerId].color = textColor;
            }

            if (_playerStatusLights[playerId] != null)
            {
                _playerStatusLights[playerId].color = lightColor;
            }
        }

        private static string GetPlayerLabel(byte playerId)
        {
            return playerId == 1 ? "HOST" : "PLAYER " + playerId;
        }
    }
}
