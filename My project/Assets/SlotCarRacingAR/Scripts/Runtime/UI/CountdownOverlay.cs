using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Full-screen countdown overlay.
    /// </summary>
    public sealed class CountdownOverlay : MonoBehaviour
    {
        private Text _countdownText;
        private GameObject _panel;

        public event Action OnCountdownComplete;

        private void Awake()
        {
            BuildUI();
            _panel.SetActive(false);
        }

        public void Show(byte value)
        {
            _panel.SetActive(true);

            if (value == 0)
            {
                _countdownText.text = "GO!";
                _countdownText.color = RetroUi.Green;
                _countdownText.fontSize = 150;
                OnCountdownComplete?.Invoke();
                return;
            }

            _countdownText.text = value.ToString();
            _countdownText.color = RetroUi.Yellow;
            _countdownText.fontSize = 230;
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        private void BuildUI()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _panel = new GameObject("CountdownPanel");
            _panel.transform.SetParent(transform, false);
            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            RetroUi.Fill(panelRect);

            RetroUi.CreateFullScreenBackground(_panel.transform, "CountdownBackground", false);

            RectTransform card = RetroUi.CreatePanel(
                _panel.transform,
                "CountdownCard",
                new Vector2(0.20f, 0.25f),
                new Vector2(0.80f, 0.72f),
                RetroUi.Teal,
                false);

            RetroUi.CreateText(
                card,
                "CountdownLabel",
                "LA CARRERA EMPIEZA EN",
                new Vector2(0.08f, 0.70f),
                new Vector2(0.92f, 0.92f),
                36,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _countdownText = RetroUi.CreateText(
                card,
                "CountdownText",
                "",
                new Vector2(0.18f, 0.02f),
                new Vector2(0.82f, 0.72f),
                230,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            _countdownText.resizeTextForBestFit = true;
            _countdownText.resizeTextMinSize = 90;
            _countdownText.resizeTextMaxSize = 240;
        }
    }
}
