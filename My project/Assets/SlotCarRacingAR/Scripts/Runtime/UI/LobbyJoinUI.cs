using UnityEngine;
using UnityEngine.UI;
using SlotCarRacingAR.Runtime.Infrastructure;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Guest join screen: IP input field, Connect button, and status feedback.
    /// Built programmatically via UGUI.
    /// </summary>
    public sealed class LobbyJoinUI : MonoBehaviour
    {
        private InputField _ipInput;
        private Button _connectButton;
        private Button _retryButton;
        private Button _backButton;
        private Text _statusText;
        private GameObject _inputPanel;
        private GameObject _feedbackPanel;
        private GameObject _failureButtonPanel;

        public event System.Action<string> OnConnectClicked;
        public event System.Action OnRetryClicked;
        public event System.Action OnBackClicked;

        private void Awake()
        {
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_connectButton != null) _connectButton.onClick.RemoveAllListeners();
            if (_retryButton != null) _retryButton.onClick.RemoveAllListeners();
            if (_backButton != null) _backButton.onClick.RemoveAllListeners();
        }

        public void ResetToInput()
        {
            _inputPanel.SetActive(true);
            _feedbackPanel.SetActive(false);
            _failureButtonPanel.SetActive(false);

            // Pre-fill last used IP
            string lastIp = PlayerPrefs.GetString("LastHostIP", "");
            if (!string.IsNullOrEmpty(lastIp))
            {
                _ipInput.text = lastIp;
            }
        }

        public void UpdateState(SessionState state, string failureReason)
        {
            switch (state)
            {
                case SessionState.Joining:
                    _inputPanel.SetActive(false);
                    _feedbackPanel.SetActive(true);
                    _failureButtonPanel.SetActive(false);
                    _statusText.text = "Conectando...";
                    _statusText.color = new Color(1f, 0.843f, 0.25f); // amber
                    break;

                case SessionState.Connected:
                    _inputPanel.SetActive(false);
                    _feedbackPanel.SetActive(true);
                    _failureButtonPanel.SetActive(false);
                    _statusText.text = "¡Conectado al host!";
                    _statusText.color = new Color(0.3f, 0.9f, 0.3f); // green
                    break;

                case SessionState.Failed:
                    _inputPanel.SetActive(false);
                    _feedbackPanel.SetActive(true);
                    _failureButtonPanel.SetActive(true);
                    _statusText.text = failureReason;
                    _statusText.color = new Color(0.95f, 0.3f, 0.3f); // red
                    break;

                default:
                    ResetToInput();
                    break;
            }
        }

        private void BuildUI()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) return;

            // ── Background ──
            GameObject bgObj = new GameObject("JoinBg");
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
            GameObject titleObj = new GameObject("JoinTitle");
            titleObj.transform.SetParent(root, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "UNIRSE A PARTIDA";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 40;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.raycastTarget = false;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.78f);
            titleRect.anchorMax = new Vector2(0.9f, 0.92f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // ── Input panel (IP field + connect button) ──
            _inputPanel = new GameObject("InputPanel");
            _inputPanel.transform.SetParent(root, false);
            RectTransform inputPanelRect = _inputPanel.AddComponent<RectTransform>();
            inputPanelRect.anchorMin = new Vector2(0.15f, 0.30f);
            inputPanelRect.anchorMax = new Vector2(0.85f, 0.75f);
            inputPanelRect.offsetMin = Vector2.zero;
            inputPanelRect.offsetMax = Vector2.zero;

            // Instruction text
            GameObject instrObj = new GameObject("InstructionText");
            instrObj.transform.SetParent(_inputPanel.transform, false);
            Text instrText = instrObj.AddComponent<Text>();
            instrText.text = "Ingresa la IP del host\n(aparece en la pantalla del otro jugador)";
            instrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instrText.fontSize = 24;
            instrText.alignment = TextAnchor.MiddleCenter;
            instrText.color = new Color(0.8f, 0.8f, 0.8f);
            instrText.raycastTarget = false;
            RectTransform instrRect = instrObj.GetComponent<RectTransform>();
            instrRect.anchorMin = new Vector2(0f, 0.70f);
            instrRect.anchorMax = new Vector2(1f, 0.95f);
            instrRect.offsetMin = Vector2.zero;
            instrRect.offsetMax = Vector2.zero;

            // IP Input field
            _ipInput = CreateInputField(
                _inputPanel.GetComponent<RectTransform>(),
                "IpInputField",
                "192.168.x.x",
                new Vector2(0.05f, 0.38f),
                new Vector2(0.95f, 0.65f)
            );

            // Connect button (amber CTA)
            _connectButton = CreateButton(
                _inputPanel.GetComponent<RectTransform>(),
                "ConnectButton",
                "Conectar",
                new Color(1f, 0.843f, 0.25f),
                new Color(0.12f, 0.12f, 0.12f),
                new Vector2(0.2f, 0.05f),
                new Vector2(0.8f, 0.32f)
            );
            _connectButton.onClick.AddListener(HandleConnect);

            // ── Feedback panel (status text — shown during connecting/connected/failed) ──
            _feedbackPanel = new GameObject("FeedbackPanel");
            _feedbackPanel.transform.SetParent(root, false);
            RectTransform fbRect = _feedbackPanel.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.1f, 0.40f);
            fbRect.anchorMax = new Vector2(0.9f, 0.70f);
            fbRect.offsetMin = Vector2.zero;
            fbRect.offsetMax = Vector2.zero;

            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(_feedbackPanel.transform, false);
            _statusText = statusObj.AddComponent<Text>();
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.fontSize = 30;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = Color.white;
            _statusText.raycastTarget = false;
            RectTransform stRect = statusObj.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;

            _feedbackPanel.SetActive(false);

            // ── Failure buttons (retry + back) ──
            _failureButtonPanel = new GameObject("FailureButtons");
            _failureButtonPanel.transform.SetParent(root, false);
            RectTransform failRect = _failureButtonPanel.AddComponent<RectTransform>();
            failRect.anchorMin = new Vector2(0.1f, 0.08f);
            failRect.anchorMax = new Vector2(0.9f, 0.30f);
            failRect.offsetMin = Vector2.zero;
            failRect.offsetMax = Vector2.zero;

            _retryButton = CreateButton(
                failRect,
                "RetryButton",
                "Reintentar",
                new Color(1f, 0.843f, 0.25f),
                new Color(0.12f, 0.12f, 0.12f),
                new Vector2(0.05f, 0.1f),
                new Vector2(0.47f, 0.9f)
            );
            _retryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());

            _backButton = CreateButton(
                failRect,
                "BackButton",
                "Volver",
                new Color(0.25f, 0.25f, 0.32f),
                Color.white,
                new Vector2(0.53f, 0.1f),
                new Vector2(0.95f, 0.9f)
            );
            _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            _failureButtonPanel.SetActive(false);

            // Pre-fill saved IP
            string lastIp = PlayerPrefs.GetString("LastHostIP", "");
            if (!string.IsNullOrEmpty(lastIp))
            {
                _ipInput.text = lastIp;
            }
        }

        private void HandleConnect()
        {
            string ip = _ipInput.text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                _ipInput.text = "";
                return;
            }
            OnConnectClicked?.Invoke(ip);
        }

        private static InputField CreateInputField(
            RectTransform parent,
            string name,
            string placeholder,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject fieldObj = new GameObject(name);
            fieldObj.transform.SetParent(parent, false);

            Image fieldBg = fieldObj.AddComponent<Image>();
            fieldBg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

            RectTransform fieldRect = fieldObj.GetComponent<RectTransform>();
            fieldRect.anchorMin = anchorMin;
            fieldRect.anchorMax = anchorMax;
            fieldRect.offsetMin = Vector2.zero;
            fieldRect.offsetMax = Vector2.zero;

            // Text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(fieldObj.transform, false);
            Text inputText = textObj.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 28;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleCenter;
            inputText.supportRichText = false;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0f);
            textRect.anchorMax = new Vector2(0.95f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Placeholder child
            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(fieldObj.transform, false);
            Text phText = phObj.AddComponent<Text>();
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontSize = 28;
            phText.fontStyle = FontStyle.Italic;
            phText.color = new Color(0.5f, 0.5f, 0.5f);
            phText.alignment = TextAnchor.MiddleCenter;
            phText.text = placeholder;
            RectTransform phRect = phObj.GetComponent<RectTransform>();
            phRect.anchorMin = new Vector2(0.05f, 0f);
            phRect.anchorMax = new Vector2(0.95f, 1f);
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            InputField inputField = fieldObj.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = phText;
            inputField.contentType = InputField.ContentType.Standard;
            inputField.characterValidation = InputField.CharacterValidation.None;
            inputField.keyboardType = TouchScreenKeyboardType.DecimalPad;

            return inputField;
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
