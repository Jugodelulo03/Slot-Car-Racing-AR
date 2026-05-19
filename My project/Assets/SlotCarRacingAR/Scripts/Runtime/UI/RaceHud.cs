using SlotCarRacingAR.Runtime.Features;
using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Simple HUD showing speed and lap count in the top-right corner.
    /// Creates its own Canvas at runtime — no scene setup required.
    /// Gets car data from MarkerDetectionEntryPoint (same source as debug overlay).
    /// </summary>
    public sealed class RaceHud : MonoBehaviour
    {
        [SerializeField] private MarkerDetectionEntryPoint _markerDetection;

        private Text _speedText;
        private Text _lapText;
        private Canvas _canvas;

        private void Start()
        {
            CreateHud();
        }

        private void Update()
        {
            CarPlaceholder car = _markerDetection != null ? _markerDetection.Car : null;
            if (car == null)
            {
                _speedText.text = "--";
                _lapText.text = "Lap 0";
                return;
            }

            float speedPercent = car.MaxSpeed > 0f ? (car.Speed / car.MaxSpeed) * 100f : 0f;
            _speedText.text = $"{speedPercent:F0}%";
            _lapText.text = $"Lap {car.LapCount + 1}";
        }

        private void CreateHud()
        {
            // Canvas
            GameObject canvasObj = new GameObject("RaceHudCanvas");
            canvasObj.transform.SetParent(transform, false);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Panel container (top-right)
            GameObject panel = new GameObject("HudPanel");
            panel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-20f, -20f);
            panelRect.sizeDelta = new Vector2(200f, 100f);

            // Background
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);

            // Speed text
            GameObject speedObj = new GameObject("SpeedText");
            speedObj.transform.SetParent(panel.transform, false);

            RectTransform speedRect = speedObj.AddComponent<RectTransform>();
            speedRect.anchorMin = new Vector2(0f, 0.5f);
            speedRect.anchorMax = new Vector2(1f, 1f);
            speedRect.offsetMin = new Vector2(10f, 5f);
            speedRect.offsetMax = new Vector2(-10f, -5f);

            _speedText = speedObj.AddComponent<Text>();
            _speedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _speedText.fontSize = 32;
            _speedText.color = Color.white;
            _speedText.alignment = TextAnchor.MiddleCenter;
            _speedText.text = "0%";

            // Lap text
            GameObject lapObj = new GameObject("LapText");
            lapObj.transform.SetParent(panel.transform, false);

            RectTransform lapRect = lapObj.AddComponent<RectTransform>();
            lapRect.anchorMin = new Vector2(0f, 0f);
            lapRect.anchorMax = new Vector2(1f, 0.5f);
            lapRect.offsetMin = new Vector2(10f, 5f);
            lapRect.offsetMax = new Vector2(-10f, -5f);

            _lapText = lapObj.AddComponent<Text>();
            _lapText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _lapText.fontSize = 26;
            _lapText.color = Color.green;
            _lapText.alignment = TextAnchor.MiddleCenter;
            _lapText.text = "Lap 1";
        }

        private void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }
    }
}
