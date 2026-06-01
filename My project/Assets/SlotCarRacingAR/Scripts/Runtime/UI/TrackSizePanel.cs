using SlotCarRacingAR.Runtime.Features;
using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Runtime panel with sliders to adjust track scale and height offset.
    /// </summary>
    public sealed class TrackSizePanel : MonoBehaviour
    {
        private MarkerDetectionEntryPoint _markerDetection;
        private Canvas _canvas;
        private Slider _scaleSlider;
        private Slider _heightSlider;
        private Text _scaleLabel;
        private Text _heightLabel;

        public void Bind(MarkerDetectionEntryPoint markerDetection)
        {
            _markerDetection = markerDetection;
            if (_markerDetection != null && _markerDetection.TrackScale < MarkerDetectionEntryPoint.MinTrackScale)
            {
                _markerDetection.SetTrackScale(MarkerDetectionEntryPoint.DefaultTrackScale);
            }
        }

        public void SetAdjustmentsAvailable(bool available)
        {
            if (_canvas == null)
            {
                BuildUi();
            }

            _canvas.enabled = available;
            if (_scaleSlider != null)
            {
                _scaleSlider.interactable = available;
            }

            if (_heightSlider != null)
            {
                _heightSlider.interactable = available;
            }
        }

        private void OnEnable()
        {
            if (_canvas == null)
            {
                BuildUi();
            }

            _canvas.enabled = true;
            SyncSlidersFromDetection();
        }

        private void OnDisable()
        {
            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }
        }

        private void SyncSlidersFromDetection()
        {
            if (_markerDetection == null)
            {
                return;
            }

            if (_scaleSlider != null)
            {
                _scaleSlider.SetValueWithoutNotify(_markerDetection.TrackScale);
            }

            if (_heightSlider != null)
            {
                _heightSlider.SetValueWithoutNotify(_markerDetection.HeightOffsetMeters);
            }

            UpdateLabels();
        }

        private void BuildUi()
        {
            GameObject canvasObj = new GameObject("TrackSizeCanvas");
            canvasObj.transform.SetParent(transform, false);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 95;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform panel = RetroUi.CreatePanel(
                canvasObj.transform,
                "TrackSizePanel",
                new Vector2(0.84f, 0.36f),
                new Vector2(0.985f, 0.86f),
                RetroUi.WithAlpha(RetroUi.TealDark, 0.92f),
                true);

            RetroUi.CreateText(
                panel,
                "Title",
                "AJUSTE DE PISTA",
                new Vector2(0.06f, 0.86f),
                new Vector2(0.94f, 0.98f),
                16,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _scaleLabel = RetroUi.CreateText(
                panel,
                "ScaleLabel",
                "Escala",
                new Vector2(0.02f, 0.04f),
                new Vector2(0.48f, 0.22f),
                13,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _scaleLabel.resizeTextForBestFit = true;
            _scaleLabel.resizeTextMinSize = 9;
            _scaleLabel.resizeTextMaxSize = 13;

            _scaleSlider = CreateRetroSlider(
                panel,
                "ScaleSlider",
                new Vector2(0.10f, 0.25f),
                new Vector2(0.42f, 0.80f),
                MarkerDetectionEntryPoint.MinTrackScale,
                MarkerDetectionEntryPoint.MaxTrackScale,
                MarkerDetectionEntryPoint.DefaultTrackScale,
                true);
            _scaleSlider.onValueChanged.AddListener(OnScaleChanged);

            _heightLabel = RetroUi.CreateText(
                panel,
                "HeightLabel",
                "Altura",
                new Vector2(0.52f, 0.04f),
                new Vector2(0.98f, 0.22f),
                13,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _heightLabel.resizeTextForBestFit = true;
            _heightLabel.resizeTextMinSize = 9;
            _heightLabel.resizeTextMaxSize = 13;

            _heightSlider = CreateRetroSlider(
                panel,
                "HeightSlider",
                new Vector2(0.58f, 0.25f),
                new Vector2(0.90f, 0.80f),
                -0.05f,
                0.10f,
                0.015f,
                true);
            _heightSlider.onValueChanged.AddListener(OnHeightChanged);
        }

        private void OnScaleChanged(float value)
        {
            if (_markerDetection != null)
            {
                _markerDetection.SetTrackScale(value);
            }

            UpdateLabels();
        }

        private void OnHeightChanged(float value)
        {
            if (_markerDetection != null)
            {
                _markerDetection.SetHeightOffset(value);
            }

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (_scaleLabel != null && _scaleSlider != null)
            {
                float width = _scaleSlider.value * OvalTrackDefinition.DesignBoundingWidth;
                float length = _scaleSlider.value * OvalTrackDefinition.DesignBoundingHeight;
                _scaleLabel.text = $"ESCALA\n{_scaleSlider.value:F2}\n{width * 100f:F0}x{length * 100f:F0} CM";
            }

            if (_heightLabel != null && _heightSlider != null)
            {
                float centimeters = _heightSlider.value * 100f;
                _heightLabel.text = $"ALTURA\n{(centimeters >= 0 ? "+" : "")}{centimeters:F1} CM";
            }
        }

        private static Slider CreateRetroSlider(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float min,
            float max,
            float defaultValue,
            bool vertical = false)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = anchorMin;
            sliderRect.anchorMax = anchorMax;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = vertical ? new Vector2(0.36f, 0f) : new Vector2(0f, 0.30f);
            bgRect.anchorMax = vertical ? new Vector2(0.64f, 1f) : new Vector2(1f, 0.70f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            RetroUi.StyleImageAsPanel(bgImg, RetroUi.CreamLight, false, true);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = vertical ? new Vector2(0.36f, 0f) : new Vector2(0f, 0.30f);
            fillAreaRect.anchorMax = vertical ? new Vector2(0.64f, 1f) : new Vector2(1f, 0.70f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = vertical ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            RetroUi.StyleImageAsPanel(fillImg, RetroUi.Yellow, false, false);

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = vertical ? new Vector2(0f, 14f) : new Vector2(14f, 0f);
            handleAreaRect.offsetMax = vertical ? new Vector2(0f, -14f) : new Vector2(-14f, 0f);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30f, 30f);
            handleRect.anchorMin = vertical ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
            handleRect.anchorMax = vertical ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            Image handleImg = handleObj.AddComponent<Image>();
            RetroUi.StyleImageAsPanel(handleImg, RetroUi.Red, true, true);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.targetGraphic = handleImg;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            slider.wholeNumbers = false;
            slider.direction = vertical ? Slider.Direction.BottomToTop : Slider.Direction.LeftToRight;

            return slider;
        }
    }
}
