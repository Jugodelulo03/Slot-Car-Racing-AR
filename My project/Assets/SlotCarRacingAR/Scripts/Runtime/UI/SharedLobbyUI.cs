using UnityEngine;
using UnityEngine.UI;
using SlotCarRacingAR.Runtime.Infrastructure;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Shared lobby display: shows both players' connection status,
    /// role/color assignments, and next-step guidance.
    /// </summary>
    public sealed class SharedLobbyUI : MonoBehaviour
    {
        private static readonly Color Player1Color = new Color(0.90f, 0.22f, 0.21f); // #E53935
        private static readonly Color Player2Color = new Color(0.26f, 0.63f, 0.28f); // #43A047
        private static readonly Color AmberColor = new Color(1f, 0.843f, 0.25f);
        private static readonly Color DisconnectColor = new Color(0.95f, 0.3f, 0.3f);

        private Text _player1Status;
        private Text _player2Status;
        private Text _guidanceText;
        private Text _confirmationText;
        private Button _continueButton;
        private GameObject _continueButtonObj;
        private float _confirmationTimer;

        public event System.Action OnContinueClicked;

        private void Awake()
        {
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_continueButton != null) _continueButton.onClick.RemoveAllListeners();
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

            // Player 1 (Host)
            if (localRole == PlayerRole.Host)
            {
                _player1Status.text = "● Jugador 1 (Host)" + ipSuffix;
            }
            else
            {
                _player1Status.text = "● Jugador 1 (Host)";
            }
            _player1Status.color = Player1Color;

            if (playerCount >= 2)
            {
                if (localRole == PlayerRole.Guest)
                {
                    _player2Status.text = "● Jugador 2 (Guest)" + ipSuffix;
                }
                else
                {
                    _player2Status.text = "● Jugador 2 (Guest)";
                }
                _player2Status.color = Player2Color;
                _guidanceText.text = "Apunten ambos teléfonos al mismo\nmarcador en la mesa.";
                _guidanceText.color = Color.white;
                SetContinueVisible(true);
            }
            else
            {
                _player2Status.text = "○ Esperando jugador 2...";
                _player2Status.color = new Color(0.5f, 0.5f, 0.5f);
                _guidanceText.text = "Esperando que el segundo jugador se conecte.";
                _guidanceText.color = AmberColor;
                SetContinueVisible(false);
            }
        }

        public void ShowDisconnected()
        {
            _player2Status.text = "✖ Jugador 2 desconectado";
            _player2Status.color = DisconnectColor;
            _guidanceText.text = "Esperando reconexión...";
            _guidanceText.color = DisconnectColor;
            SetContinueVisible(false);
        }

        public void ShowConnectionConfirmation()
        {
            _confirmationText.text = "¡Jugador 2 conectado!";
            _confirmationText.color = Player2Color;
            _confirmationText.gameObject.SetActive(true);
            _confirmationTimer = 2.5f;
        }

        private void SetContinueVisible(bool visible)
        {
            if (_continueButtonObj != null)
                _continueButtonObj.SetActive(visible);
        }

        private void BuildUI()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) return;

            // ── Background ──
            GameObject bgObj = new GameObject("LobbyBg");
            bgObj.transform.SetParent(root, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            bgImage.raycastTarget = true;
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // ── Title ──
            CreateText(root, "LobbyTitle", "SALA DE ESPERA", 40, FontStyle.Bold, Color.white,
                new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.95f));

            // ── Player 1 status ──
            _player1Status = CreateText(root, "Player1Status", "● Jugador 1 (Host)", 30, FontStyle.Bold, Player1Color,
                new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.78f));

            // ── Player 2 status ──
            _player2Status = CreateText(root, "Player2Status", "○ Esperando jugador 2...", 30, FontStyle.Bold, new Color(0.5f, 0.5f, 0.5f),
                new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.65f));

            // ── Connection confirmation (hidden by default) ──
            _confirmationText = CreateText(root, "Confirmation", "", 26, FontStyle.Bold, Player2Color,
                new Vector2(0.2f, 0.44f), new Vector2(0.8f, 0.52f));
            _confirmationText.gameObject.SetActive(false);

            // ── Guidance text ──
            _guidanceText = CreateText(root, "Guidance", "Esperando que el segundo jugador se conecte.", 26, FontStyle.Normal, AmberColor,
                new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.44f));

            // ── Continue button (hidden until 2 players) ──
            _continueButtonObj = new GameObject("ContinueButton");
            _continueButtonObj.transform.SetParent(root, false);

            Image btnImage = _continueButtonObj.AddComponent<Image>();
            btnImage.color = AmberColor;

            _continueButton = _continueButtonObj.AddComponent<Button>();
            _continueButton.targetGraphic = btnImage;
            _continueButton.onClick.AddListener(() => OnContinueClicked?.Invoke());

            RectTransform btnRect = _continueButtonObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.25f, 0.08f);
            btnRect.anchorMax = new Vector2(0.75f, 0.24f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(_continueButtonObj.transform, false);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Continuar";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 32;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.12f, 0.12f, 0.12f);
            labelText.raycastTarget = false;
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            _continueButtonObj.SetActive(false);
        }

        private static Text CreateText(
            RectTransform parent,
            string name,
            string content,
            int fontSize,
            FontStyle style,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            Text text = obj.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return text;
        }
    }
}
