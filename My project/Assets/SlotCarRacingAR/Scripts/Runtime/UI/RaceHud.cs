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
        private GameObject _finishNoticePanel;
        private Text _finishNoticeText;
        private RetroUiAnimator _positionAnimator;
        private RetroUiAnimator _finishNoticeAnimator;
        private bool _visible;
        private float _lastObservedLocalFinishTime = -1f;
        private int _lastDisplayedPosition = -1;

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

            if (!visible)
            {
                _lastObservedLocalFinishTime = -1f;
                _lastDisplayedPosition = -1;
                if (_finishNoticePanel != null)
                {
                    _finishNoticePanel.SetActive(false);
                }
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
                UpdateFinishNotice(-1f, 0);
                return;
            }

            byte localPlayerId = _sharedState.LocalPlayerId;
            if (localPlayerId == 0)
            {
                localPlayerId = _sharedState.IsServer ? (byte)1 : (byte)2;
            }

            byte localLap = _sharedState.GetLap(localPlayerId);
            int displayLap = Mathf.Clamp(localLap + 1, 1, SharedLobbyState.RaceLapTarget);
            int position = _sharedState.GetRacePosition(localPlayerId);

            _lapText.text = "VUELTA\n" + displayLap + "/" + SharedLobbyState.RaceLapTarget;
            SetPositionDisplay(position);
            UpdateFinishNotice(_sharedState.GetFinishTime(localPlayerId), _sharedState.GetFinishRank(localPlayerId));
        }

        private void UpdateFinishNotice(float localFinishTime, int rank)
        {
            if (localFinishTime >= 0f)
            {
                ShowFinishNotice(rank, localFinishTime, _lastObservedLocalFinishTime < 0f);
            }
            else if (_finishNoticePanel != null)
            {
                _finishNoticePanel.SetActive(false);
            }

            _lastObservedLocalFinishTime = localFinishTime;
        }

        private void ShowFinishNotice(int rank, float finishTimeSeconds, bool playSfx)
        {
            if (_finishNoticePanel == null || _finishNoticeText == null)
            {
                return;
            }

            _finishNoticeText.text = FormatOrdinal(Mathf.Max(1, rank)) + "\n" + FormatTime(finishTimeSeconds);
            _finishNoticePanel.SetActive(true);
            if (playSfx)
            {
                _finishNoticeAnimator?.PlayPop(1.08f, 0.24f);
                GameAudio.Play(GameSfx.Ready);
            }
        }

        private void SetPositionDisplay(int position)
        {
            int clamped = Mathf.Clamp(position, 1, 4);
            _positionText.text = FormatOrdinal(clamped);
            if (_lastDisplayedPosition != clamped)
            {
                _positionAnimator?.PlayPop(1.08f, 0.18f);
                _lastDisplayedPosition = clamped;
            }

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

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f)
            {
                return "--:--";
            }

            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainder = seconds - minutes * 60f;
            return minutes.ToString("00") + ":" + remainder.ToString("00.00");
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
            _positionAnimator = RetroUiAnimator.Attach(positionPanel.gameObject);

            _finishNoticePanel = RetroUi.CreatePanel(
                _root.transform,
                "FinishNoticePanel",
                new Vector2(0.24f, 0.35f),
                new Vector2(0.76f, 0.66f),
                RetroUi.WithAlpha(RetroUi.TealDark, 0.94f),
                false).gameObject;

            _finishNoticeText = RetroUi.CreateText(
                _finishNoticePanel.transform,
                "FinishNoticeText",
                "LLEGASTE",
                Vector2.zero,
                Vector2.one,
                58,
                RetroUi.Cream,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _finishNoticeText.resizeTextForBestFit = true;
            _finishNoticeText.resizeTextMinSize = 42;
            _finishNoticeText.resizeTextMaxSize = 70;
            _finishNoticeAnimator = RetroUiAnimator.Attach(_finishNoticePanel);
            _finishNoticePanel.SetActive(false);
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
