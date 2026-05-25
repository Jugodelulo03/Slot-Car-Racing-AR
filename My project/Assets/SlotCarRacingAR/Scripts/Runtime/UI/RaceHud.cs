using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Minimal race HUD for the active quick-race loop.
    /// </summary>
    public sealed class RaceHud : MonoBehaviour
    {
        private SharedLobbyState _sharedState;
        private Canvas _canvas;
        private GameObject _root;
        private Text _speedText;
        private Text _lapText;
        private Text _positionText;
        private Text _messageText;
        private bool _visible;
        private float _maxSpeedMetersPerSecond = 0.25f;

        private void Start()
        {
            CreateHud();
            SetVisible(false);
        }

        public void Bind(SharedLobbyState sharedState)
        {
            _sharedState = sharedState;
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_root != null)
            {
                _root.SetActive(visible);
            }
        }

        public void SetMaxSpeed(float maxSpeedMetersPerSecond)
        {
            _maxSpeedMetersPerSecond = Mathf.Max(0.01f, maxSpeedMetersPerSecond);
        }

        private void Update()
        {
            if (_root == null || !_visible)
            {
                return;
            }

            if (_sharedState == null)
            {
                _speedText.text = "0%";
                _lapText.text = "VUELTA 1/" + SharedLobbyState.RaceLapTarget;
                _positionText.text = "POS 1/2";
                _messageText.text = "Manten para acelerar";
                return;
            }

            bool localIsHost = _sharedState.IsServer;
            float localProgress = localIsHost ? _sharedState.HostProgress.Value : _sharedState.GuestProgress.Value;
            float rivalProgress = localIsHost ? _sharedState.GuestProgress.Value : _sharedState.HostProgress.Value;
            float localSpeed = localIsHost ? _sharedState.HostSpeed.Value : _sharedState.GuestSpeed.Value;
            byte localLap = localIsHost ? _sharedState.HostLap.Value : _sharedState.GuestLap.Value;
            byte rivalLap = localIsHost ? _sharedState.GuestLap.Value : _sharedState.HostLap.Value;
            bool localPenalty = localIsHost ? _sharedState.HostPenaltyActive.Value : _sharedState.GuestPenaltyActive.Value;

            float speedPercent = Mathf.Clamp01(localSpeed / _maxSpeedMetersPerSecond) * 100f;
            int displayLap = Mathf.Clamp(localLap + 1, 1, SharedLobbyState.RaceLapTarget);
            int position = localLap + localProgress >= rivalLap + rivalProgress ? 1 : 2;

            _speedText.text = speedPercent.ToString("F0") + "%";
            _lapText.text = "VUELTA " + displayLap + "/" + SharedLobbyState.RaceLapTarget;
            _positionText.text = "POS " + position + "/2";

            if (_sharedState.Phase.Value == RacePhase.Finished)
            {
                byte localPlayer = localIsHost ? (byte)1 : (byte)2;
                _messageText.text = _sharedState.WinnerPlayerId.Value == localPlayer ? "GANASTE" : "RIVAL GANA";
                _messageText.color = _sharedState.WinnerPlayerId.Value == localPlayer
                    ? new Color(0.2f, 0.95f, 0.45f)
                    : new Color(1f, 0.65f, 0.25f);
            }
            else if (localPenalty)
            {
                _messageText.text = "Muy rapido en curva";
                _messageText.color = new Color(1f, 0.45f, 0.25f);
            }
            else
            {
                _messageText.text = "Manten para acelerar";
                _messageText.color = Color.white;
            }
        }

        private void CreateHud()
        {
            if (_canvas != null)
            {
                return;
            }

            GameObject canvasObj = new GameObject("RaceHudCanvas");
            canvasObj.transform.SetParent(transform, false);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            _root = new GameObject("RaceHudRoot");
            _root.transform.SetParent(canvasObj.transform, false);
            RectTransform rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject topPanel = CreatePanel("TopStatus", _root.transform, new Vector2(0.03f, 0.84f), new Vector2(0.56f, 0.96f));
            _lapText = CreateText("LapText", topPanel.transform, new Vector2(0f, 0f), new Vector2(0.34f, 1f), 30, Color.white);
            _positionText = CreateText("PositionText", topPanel.transform, new Vector2(0.34f, 0f), new Vector2(0.62f, 1f), 30, Color.white);
            _speedText = CreateText("SpeedText", topPanel.transform, new Vector2(0.62f, 0f), new Vector2(1f, 1f), 34, new Color(1f, 0.82f, 0.2f));

            GameObject messagePanel = CreatePanel("MessagePanel", _root.transform, new Vector2(0.22f, 0.04f), new Vector2(0.78f, 0.14f));
            _messageText = CreateText("MessageText", messagePanel.transform, Vector2.zero, Vector2.one, 32, Color.white);
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.68f);

            return panel;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(10f, 4f);
            rect.offsetMax = new Vector2(-10f, -4f);

            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }
        }
    }
}
