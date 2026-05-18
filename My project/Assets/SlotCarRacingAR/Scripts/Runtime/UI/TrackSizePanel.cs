using SlotCarRacingAR.Runtime.Features;
using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Runtime UI panel with a slider to adjust track scale (uniform)
    /// and a slider for height offset.
    /// Creates its own Canvas + sliders programmatically.
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
            if (_markerDetection == null) return;

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
            // Canvas
            GameObject canvasObj = new GameObject("TrackSizeCanvas");
            canvasObj.transform.SetParent(transform, false);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Panel background at bottom of screen
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.02f);
            panelRect.anchorMax = new Vector2(0.95f, 0.24f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.65f);

            // Title
            CreateLabel(panelObj.transform, "Title", "TRACK SIZE", 0.02f, 0.86f, 0.98f, 0.98f, 22, TextAnchor.MiddleCenter);

            // Scale slider row
            _scaleLabel = CreateLabel(panelObj.transform, "ScaleLabel", "Scale: 0.25", 0.02f, 0.52f, 0.35f, 0.84f, 18, TextAnchor.MiddleLeft);
            _scaleSlider = CreateSlider(panelObj.transform, "ScaleSlider", 0.36f, 0.52f, 0.98f, 0.84f, 0.10f, 1.0f, 0.25f);
            _scaleSlider.onValueChanged.AddListener(OnScaleChanged);

            // Height offset slider row
            _heightLabel = CreateLabel(panelObj.transform, "HeightLabel", "Height: +1.5cm", 0.02f, 0.05f, 0.35f, 0.48f, 18, TextAnchor.MiddleLeft);
            _heightSlider = CreateSlider(panelObj.transform, "HeightSlider", 0.36f, 0.05f, 0.98f, 0.48f, -0.05f, 0.10f, 0.015f);
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
            if (_scaleLabel != null)
            {
                float w = _scaleSlider.value * OvalTrackDefinition.DesignBoundingWidth;
                float l = _scaleSlider.value * OvalTrackDefinition.DesignBoundingHeight;
                _scaleLabel.text = $"Scale: {_scaleSlider.value:F2}\n{w * 100f:F0}×{l * 100f:F0}cm";
            }

            if (_heightLabel != null)
            {
                float cm = _heightSlider.value * 100f;
                _heightLabel.text = $"Height: {(cm >= 0 ? "+" : "")}{cm:F1}cm";
            }
        }

        private static Text CreateLabel(Transform parent, string name, string text,
            float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY,
            int fontSize, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text label = obj.AddComponent<Text>();
            label.text = text;
            label.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            return label;
        }

        private static Slider CreateSlider(Transform parent, string name,
            float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY,
            float min, float max, float defaultValue)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(anchorMinX, anchorMinY);
            sliderRect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.35f);
            bgRect.anchorMax = new Vector2(1f, 0.65f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.35f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.65f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.8f, 0.4f, 1f);

            // Handle area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30f, 0f);
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.color = Color.white;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.targetGraphic = handleImg;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }
    }
}
