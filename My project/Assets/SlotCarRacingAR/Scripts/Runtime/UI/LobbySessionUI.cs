using UnityEngine;
using UnityEngine.UI;
using SlotCarRacingAR.Runtime.Infrastructure;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Displays session status in the Lobby after "Create Match" is pressed.
    /// Shows creating, waiting, connected, or failed states with semantic colors.
    /// </summary>
    public sealed class LobbySessionUI : MonoBehaviour
    {
        private Text _statusText;
        private Text _detailText;
        private Button _retryButton;
        private Button _backButton;
        private GameObject _buttonPanel;

        public event System.Action OnRetryClicked;
        public event System.Action OnBackClicked;

        private void Awake()
        {
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_retryButton != null) _retryButton.onClick.RemoveAllListeners();
            if (_backButton != null) _backButton.onClick.RemoveAllListeners();
        }

        public void UpdateState(SessionState state, string ipAddress, string failureReason)
        {
            switch (state)
            {
                case SessionState.Creating:
                    _statusText.text = "Creando sesión...";
                    _statusText.color = new Color(1f, 0.843f, 0.25f); // amber
                    _detailText.text = "Iniciando servidor local.";
                    SetButtonsVisible(false);
                    break;

                case SessionState.WaitingForPlayer:
                    _statusText.text = "Esperando jugador 2";
                    _statusText.color = new Color(1f, 0.843f, 0.25f); // amber
                    _detailText.text =
                        "Tu IP: " + ipAddress + "\n\n" +
                        "El otro jugador debe estar en la misma red\n" +
                        "y seleccionar \"Unirse\" en su dispositivo.";
                    SetButtonsVisible(false);
                    break;

                case SessionState.Connected:
                    _statusText.text = "¡Jugador 2 conectado!";
                    _statusText.color = new Color(0.3f, 0.9f, 0.3f); // green
                    _detailText.text = "Preparando la carrera...";
                    SetButtonsVisible(false);
                    break;

                case SessionState.Failed:
                    _statusText.text = "Error de conexión";
                    _statusText.color = new Color(0.95f, 0.3f, 0.3f); // red
                    _detailText.text = failureReason;
                    SetButtonsVisible(true);
                    break;

                default:
                    gameObject.SetActive(false);
                    break;
            }
        }

        private void SetButtonsVisible(bool visible)
        {
            if (_buttonPanel != null)
                _buttonPanel.SetActive(visible);
        }

        private void BuildUI()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) return;

            // ── Background ──
            GameObject bgObj = new GameObject("SessionBg");
            bgObj.transform.SetParent(root, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            bgImage.raycastTarget = true; // block input to screen behind
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // ── Status text (center-top area) ──
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(root, false);
            _statusText = statusObj.AddComponent<Text>();
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.fontSize = 42;
            _statusText.fontStyle = FontStyle.Bold;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = Color.white;
            _statusText.raycastTarget = false;
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.60f);
            statusRect.anchorMax = new Vector2(0.9f, 0.80f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            // ── Detail text (center area) ──
            GameObject detailObj = new GameObject("DetailText");
            detailObj.transform.SetParent(root, false);
            _detailText = detailObj.AddComponent<Text>();
            _detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailText.fontSize = 26;
            _detailText.alignment = TextAnchor.MiddleCenter;
            _detailText.color = new Color(0.85f, 0.85f, 0.85f);
            _detailText.raycastTarget = false;
            RectTransform detailRect = detailObj.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.1f, 0.35f);
            detailRect.anchorMax = new Vector2(0.9f, 0.58f);
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = Vector2.zero;

            // ── Button panel (bottom zone — only visible on failure) ──
            _buttonPanel = new GameObject("ButtonPanel");
            _buttonPanel.transform.SetParent(root, false);
            RectTransform panelRect = _buttonPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.08f);
            panelRect.anchorMax = new Vector2(0.9f, 0.30f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Retry button (amber)
            _retryButton = CreateButton(
                panelRect,
                "RetryButton",
                "Reintentar",
                new Color(1f, 0.843f, 0.25f),
                new Color(0.12f, 0.12f, 0.12f),
                new Vector2(0.05f, 0.1f),
                new Vector2(0.47f, 0.9f)
            );
            _retryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());

            // Back button (subtle dark)
            _backButton = CreateButton(
                panelRect,
                "BackButton",
                "Volver",
                new Color(0.25f, 0.25f, 0.32f),
                Color.white,
                new Vector2(0.53f, 0.1f),
                new Vector2(0.95f, 0.9f)
            );
            _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            _buttonPanel.SetActive(false);
        }

        private static Button CreateButton(
            RectTransform parent,
            string name,
            string label,
            Color bgColor,
            Color textColor,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = bgColor;

            Button button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImage;

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = anchorMin;
            btnRect.anchorMax = anchorMax;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 30;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = textColor;
            labelText.raycastTarget = false;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}
