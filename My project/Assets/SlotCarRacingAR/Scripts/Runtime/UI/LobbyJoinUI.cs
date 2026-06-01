using System.Collections.Generic;
using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Guest join screen: shows discovered LAN games and a manual IP fallback.
    /// </summary>
    public sealed class LobbyJoinUI : MonoBehaviour
    {
        private InputField _ipInput;
        private Button _connectButton;
        private Button _retryButton;
        private Button _backButton;
        private Text _statusText;
        private Text _scanningText;
        private GameObject _discoveryPanel;
        private GameObject _inputPanel;
        private GameObject _feedbackPanel;
        private GameObject _failureButtonPanel;
        private RectTransform _gameListContainer;
        private readonly Dictionary<string, GameObject> _discoveredEntries = new();

        public event System.Action<string, int> OnConnectClicked;
        public event System.Action OnRetryClicked;
        public event System.Action OnBackClicked;

        private void Awake()
        {
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_connectButton != null)
            {
                _connectButton.onClick.RemoveAllListeners();
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
            }
        }

        public void ResetToInput()
        {
            _discoveryPanel.SetActive(true);
            _inputPanel.SetActive(true);
            _feedbackPanel.SetActive(false);
            _failureButtonPanel.SetActive(false);

            string lastIp = PlayerPrefs.GetString("LastHostIP", "");
            if (!string.IsNullOrEmpty(lastIp))
            {
                _ipInput.text = lastIp;
            }

            ClearDiscoveredEntries();
            if (_scanningText != null)
            {
                _scanningText.gameObject.SetActive(true);
            }
        }

        public void ShowDiscoveredHost(string ip, int port, string hostName)
        {
            string key = ip + ":" + port;
            if (_discoveredEntries.ContainsKey(key))
            {
                return;
            }

            if (_scanningText != null)
            {
                _scanningText.gameObject.SetActive(false);
            }

            GameObject entry = CreateGameEntry(ip, port, hostName);
            _discoveredEntries[key] = entry;
        }

        public void UpdateState(SessionState state, string failureReason)
        {
            switch (state)
            {
                case SessionState.Joining:
                    _discoveryPanel.SetActive(false);
                    _inputPanel.SetActive(false);
                    _feedbackPanel.SetActive(true);
                    _failureButtonPanel.SetActive(false);
                    _statusText.text = "Conectando...";
                    _statusText.color = RetroUi.Yellow;
                    break;

                case SessionState.Connected:
                    _discoveryPanel.SetActive(false);
                    _inputPanel.SetActive(false);
                    _feedbackPanel.SetActive(true);
                    _failureButtonPanel.SetActive(false);
                    _statusText.text = "Conectado al host!";
                    _statusText.color = RetroUi.Green;
                    break;

                case SessionState.Failed:
                    _discoveryPanel.SetActive(false);
                    _inputPanel.SetActive(false);
                    _feedbackPanel.SetActive(true);
                    _failureButtonPanel.SetActive(true);
                    _statusText.text = string.IsNullOrEmpty(failureReason) ? "No se encontro la sesion." : failureReason;
                    _statusText.color = RetroUi.Red;
                    break;

                default:
                    ResetToInput();
                    break;
            }
        }

        private void BuildUI()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            RetroUi.CreateFullScreenBackground(root, "JoinBackground", true);

            RectTransform panel = RetroUi.CreatePanel(
                root,
                "JoinPanel",
                new Vector2(0.15f, 0.10f),
                new Vector2(0.85f, 0.88f),
                RetroUi.Teal,
                true);

            RetroUi.CreateText(
                panel,
                "Title",
                "UNIRSE\nA PARTIDA",
                new Vector2(0.20f, 0.78f),
                new Vector2(0.80f, 1.08f),
                58,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            RetroUi.CreateText(
                panel,
                "Instruction",
                "Elige una sala disponible en tu red local",
                new Vector2(0.10f, 0.66f),
                new Vector2(0.90f, 0.76f),
                26,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            RectTransform discoveryRect = RetroUi.CreatePanel(
                panel,
                "DiscoveryPanel",
                new Vector2(0.10f, 0.32f),
                new Vector2(0.90f, 0.67f),
                RetroUi.CreamLight,
                true);
            _discoveryPanel = discoveryRect.gameObject;

            RetroUi.CreateText(
                _discoveryPanel.transform,
                "DiscoveryTitle",
                "SALAS DISPONIBLES",
                new Vector2(0.04f, 0.76f),
                new Vector2(0.96f, 0.98f),
                28,
                RetroUi.Black,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);

            _scanningText = RetroUi.CreateText(
                _discoveryPanel.transform,
                "ScanningText",
                "Buscando partidas en la red local...",
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.74f),
                24,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            GameObject listObj = new GameObject("GameList");
            listObj.transform.SetParent(_discoveryPanel.transform, false);
            _gameListContainer = listObj.AddComponent<RectTransform>();
            _gameListContainer.anchorMin = new Vector2(0.03f, 0.06f);
            _gameListContainer.anchorMax = new Vector2(0.97f, 0.74f);
            _gameListContainer.offsetMin = Vector2.zero;
            _gameListContainer.offsetMax = Vector2.zero;

            _inputPanel = new GameObject("InputPanel");
            _inputPanel.transform.SetParent(panel, false);
            RectTransform inputRect = _inputPanel.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.12f, 0.08f);
            inputRect.anchorMax = new Vector2(0.88f, 0.29f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;

            RetroUi.CreateText(
                inputRect,
                "ManualLabel",
                "O ingresa la IP manualmente",
                new Vector2(0.02f, 0.70f),
                new Vector2(0.98f, 1.00f),
                22,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _ipInput = CreateInputField(
                inputRect,
                "IpInputField",
                "EJ. 192.168.1.20",
                new Vector2(0.02f, 0.12f),
                new Vector2(0.62f, 0.64f));

            _connectButton = RetroUi.CreateButton(
                inputRect,
                "ConnectButton",
                "Unirse",
                new Vector2(0.66f, 0.12f),
                new Vector2(0.98f, 0.64f),
                RetroUi.Yellow,
                RetroUi.White,
                30);
            _connectButton.onClick.AddListener(HandleConnect);

            RetroUi.CreateText(
                panel,
                "WifiHint",
                "Asegurate de estar en la misma red Wi-Fi o hotspot que el host",
                new Vector2(0.16f, 0.01f),
                new Vector2(0.84f, 0.07f),
                24,
                RetroUi.Black,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);

            _backButton = RetroUi.CreateButton(
                root,
                "BackButton",
                "Cancelar",
                new Vector2(0.59f, 0.02f),
                new Vector2(0.76f, 0.10f),
                RetroUi.Red,
                RetroUi.White,
                30);
            _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            _feedbackPanel = new GameObject("FeedbackPanel");
            _feedbackPanel.transform.SetParent(root, false);
            RectTransform feedbackRect = _feedbackPanel.AddComponent<RectTransform>();
            feedbackRect.anchorMin = new Vector2(0.20f, 0.38f);
            feedbackRect.anchorMax = new Vector2(0.80f, 0.62f);
            feedbackRect.offsetMin = Vector2.zero;
            feedbackRect.offsetMax = Vector2.zero;

            RectTransform statusPanel = RetroUi.CreatePanel(
                _feedbackPanel.transform,
                "StatusPanel",
                Vector2.zero,
                Vector2.one,
                RetroUi.TealDark,
                false);
            _statusText = RetroUi.CreateText(
                statusPanel,
                "StatusText",
                "",
                new Vector2(0.04f, 0.12f),
                new Vector2(0.96f, 0.88f),
                36,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _feedbackPanel.SetActive(false);

            _failureButtonPanel = new GameObject("FailureButtons");
            _failureButtonPanel.transform.SetParent(root, false);
            RectTransform failRect = _failureButtonPanel.AddComponent<RectTransform>();
            failRect.anchorMin = new Vector2(0.22f, 0.16f);
            failRect.anchorMax = new Vector2(0.78f, 0.28f);
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

            Button failBackButton = RetroUi.CreateButton(
                failRect,
                "FailBackButton",
                "Volver",
                new Vector2(0.52f, 0.10f),
                new Vector2(0.96f, 0.90f),
                RetroUi.Red,
                RetroUi.White,
                30);
            failBackButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            _failureButtonPanel.SetActive(false);

            string lastIp = PlayerPrefs.GetString("LastHostIP", "");
            if (!string.IsNullOrEmpty(lastIp))
            {
                _ipInput.text = lastIp;
            }
        }

        private GameObject CreateGameEntry(string ip, int port, string hostName)
        {
            GameObject entry = new GameObject("GameEntry_" + ip);
            entry.transform.SetParent(_gameListContainer, false);

            RectTransform entryRect = entry.AddComponent<RectTransform>();
            int index = _discoveredEntries.Count;
            float top = 1f - index * 0.34f;
            float bottom = top - 0.29f;
            entryRect.anchorMin = new Vector2(0.03f, Mathf.Max(0f, bottom));
            entryRect.anchorMax = new Vector2(0.97f, top);
            entryRect.offsetMin = Vector2.zero;
            entryRect.offsetMax = Vector2.zero;

            Image image = entry.AddComponent<Image>();
            image.color = RetroUi.Green;
            Outline outline = entry.AddComponent<Outline>();
            outline.effectColor = RetroUi.Black;
            outline.effectDistance = new Vector2(4f, -4f);

            Button entryButton = entry.AddComponent<Button>();
            entryButton.targetGraphic = image;
            ColorBlock colors = entryButton.colors;
            colors.normalColor = RetroUi.Green;
            colors.highlightedColor = Color.Lerp(RetroUi.Green, RetroUi.White, 0.12f);
            colors.pressedColor = Color.Lerp(RetroUi.Green, RetroUi.Black, 0.18f);
            colors.selectedColor = RetroUi.Green;
            entryButton.colors = colors;
            string capturedIp = ip;
            int capturedPort = port;
            entryButton.onClick.AddListener(() => OnConnectClicked?.Invoke(capturedIp, capturedPort));

            RetroUi.CreateText(
                entry.transform,
                "Label",
                "UNIRSE A " + hostName + "    " + ip + ":" + port,
                new Vector2(0.03f, 0f),
                new Vector2(0.97f, 1f),
                26,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            return entry;
        }

        private void ClearDiscoveredEntries()
        {
            foreach (var kvp in _discoveredEntries)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }

            _discoveredEntries.Clear();
        }

        private void HandleConnect()
        {
            string ip = _ipInput.text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                _ipInput.text = "";
                return;
            }

            OnConnectClicked?.Invoke(ip, 7777);
        }

        private static InputField CreateInputField(
            RectTransform parent,
            string name,
            string placeholder,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform fieldRect = RetroUi.CreatePanel(parent, name, anchorMin, anchorMax, RetroUi.CreamLight, true);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(fieldRect, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0f);
            textRect.anchorMax = new Vector2(0.95f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text inputText = textObj.AddComponent<Text>();
            inputText.font = RetroUi.Font;
            inputText.fontSize = 26;
            inputText.fontStyle = FontStyle.BoldAndItalic;
            inputText.color = RetroUi.Black;
            inputText.alignment = TextAnchor.MiddleCenter;
            inputText.supportRichText = false;

            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(fieldRect, false);
            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.05f, 0f);
            placeholderRect.anchorMax = new Vector2(0.95f, 1f);
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            Text placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.font = RetroUi.Font;
            placeholderText.fontSize = 26;
            placeholderText.fontStyle = FontStyle.BoldAndItalic;
            placeholderText.color = new Color(0.25f, 0.25f, 0.25f, 0.72f);
            placeholderText.alignment = TextAnchor.MiddleCenter;
            placeholderText.text = placeholder;

            InputField inputField = fieldRect.gameObject.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.contentType = InputField.ContentType.Standard;
            inputField.characterValidation = InputField.CharacterValidation.None;
            inputField.keyboardType = TouchScreenKeyboardType.DecimalPad;
            inputField.caretColor = RetroUi.Black;
            inputField.selectionColor = RetroUi.WithAlpha(RetroUi.Yellow, 0.35f);

            return inputField;
        }
    }
}
