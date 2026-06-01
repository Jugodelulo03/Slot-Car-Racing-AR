using SlotCarRacingAR.Runtime.Infrastructure;
using SlotCarRacingAR.Runtime.Features;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Race HUD for the active race loop.
    /// </summary>
    public sealed class RaceHud : MonoBehaviour
    {
        private SharedLobbyState _sharedState;
        private Canvas _canvas;
        private GameObject _root;
        private Text _lapText;
        private Text _positionText;
        private Image _positionBackground;
        private bool _visible;

        private void Start()
        {
            CreateHud();
            SetVisible(false);
        }

        public void Bind(SharedLobbyState sharedState)
        {
            _sharedState = sharedState;
        }

        public void BindLocalCar(CarPlaceholder localCar)
        {
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
        }

        private void Update()
        {
            if (_root == null || !_visible)
            {
                return;
            }

            if (_sharedState == null)
            {
                _lapText.text = "VUELTA\n1/" + SharedLobbyState.RaceLapTarget;
                SetPositionDisplay(1);
                return;
            }

            bool localIsHost = _sharedState.IsServer;
            float localProgress = localIsHost ? _sharedState.HostProgress.Value : _sharedState.GuestProgress.Value;
            float rivalProgress = localIsHost ? _sharedState.GuestProgress.Value : _sharedState.HostProgress.Value;
            byte localLap = localIsHost ? _sharedState.HostLap.Value : _sharedState.GuestLap.Value;
            byte rivalLap = localIsHost ? _sharedState.GuestLap.Value : _sharedState.HostLap.Value;

            int displayLap = Mathf.Clamp(localLap + 1, 1, SharedLobbyState.RaceLapTarget);
            int position = ResolveLocalPosition(localIsHost, localLap, localProgress, rivalLap, rivalProgress);

            _lapText.text = "VUELTA\n" + displayLap + "/" + SharedLobbyState.RaceLapTarget;
            SetPositionDisplay(position);
        }

        private int ResolveLocalPosition(bool localIsHost, byte localLap, float localProgress, byte rivalLap, float rivalProgress)
        {
            float localFinishTime = localIsHost ? _sharedState.HostFinishTimeSeconds.Value : _sharedState.GuestFinishTimeSeconds.Value;
            float rivalFinishTime = localIsHost ? _sharedState.GuestFinishTimeSeconds.Value : _sharedState.HostFinishTimeSeconds.Value;
            bool localFinished = localFinishTime >= 0f;
            bool rivalFinished = rivalFinishTime >= 0f;

            if (localFinished && rivalFinished)
            {
                return localFinishTime <= rivalFinishTime ? 1 : 2;
            }

            if (localFinished)
            {
                return 1;
            }

            if (rivalFinished)
            {
                return 2;
            }

            return localLap + localProgress >= rivalLap + rivalProgress ? 1 : 2;
        }

        private void SetPositionDisplay(int position)
        {
            int clamped = Mathf.Clamp(position, 1, 4);
            _positionText.text = FormatOrdinal(clamped);

            if (_positionBackground != null)
            {
                _positionBackground.color = GetPositionColor(clamped);
            }
        }

        private static string FormatOrdinal(int position)
        {
            switch (position)
            {
                case 1:
                    return "1ro";
                case 2:
                    return "2do";
                case 3:
                    return "3ro";
                case 4:
                    return "4to";
                default:
                    return position.ToString() + "to";
            }
        }

        private static Color GetPositionColor(int position)
        {
            switch (position)
            {
                case 1:
                    return new Color(1.00f, 0.73f, 0.10f);
                case 2:
                    return new Color(0.78f, 0.82f, 0.86f);
                case 3:
                    return new Color(0.72f, 0.39f, 0.16f);
                default:
                    return RetroUi.TealDark;
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
            RetroUi.Fill(rootRect);

            RectTransform lapPanel = RetroUi.CreatePanel(
                _root.transform,
                "LapPanel",
                new Vector2(0.03f, 0.82f),
                new Vector2(0.16f, 0.96f),
                RetroUi.Teal,
                false);
            _lapText = RetroUi.CreateText(
                lapPanel,
                "LapText",
                "VUELTA\n1/" + SharedLobbyState.RaceLapTarget,
                Vector2.zero,
                Vector2.one,
                32,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            RectTransform positionPanel = RetroUi.CreatePanel(
                _root.transform,
                "PositionPanel",
                new Vector2(0.78f, 0.74f),
                new Vector2(0.98f, 0.98f),
                GetPositionColor(1),
                false);
            _positionBackground = positionPanel.GetComponent<Image>();

            _positionText = RetroUi.CreateText(
                positionPanel,
                "PositionText",
                "1ro",
                Vector2.zero,
                Vector2.one,
                92,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _positionText.resizeTextForBestFit = true;
            _positionText.resizeTextMinSize = 54;
            _positionText.resizeTextMaxSize = 100;

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
