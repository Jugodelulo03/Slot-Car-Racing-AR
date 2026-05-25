using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Full-screen countdown overlay: displays 3, 2, 1, GO! centered on screen.
    /// </summary>
    public sealed class CountdownOverlay : MonoBehaviour
    {
        private Text _countdownText;
        private GameObject _panel;

        /// <summary>Fired when countdown finishes (GO! displayed).</summary>
        public event Action OnCountdownComplete;

        private void Awake()
        {
            BuildUI();
            _panel.SetActive(false);
        }

        /// <summary>Show the overlay and display a countdown number.</summary>
        public void Show(byte value)
        {
            _panel.SetActive(true);

            if (value == 0)
            {
                _countdownText.text = "GO!";
                _countdownText.color = new Color(0.2f, 1f, 0.4f);
                _countdownText.fontSize = 200;
                OnCountdownComplete?.Invoke();
            }
            else
            {
                _countdownText.text = value.ToString();
                _countdownText.color = Color.white;
                _countdownText.fontSize = 240;
            }
        }

        /// <summary>Hide the overlay after GO! has been shown briefly.</summary>
        public void Hide()
        {
            _panel.SetActive(false);
        }

        private void BuildUI()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // Above AR setup UI

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Full-screen semi-transparent panel
            _panel = new GameObject("CountdownPanel");
            _panel.transform.SetParent(transform, false);
            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);

            // Centered countdown number
            GameObject textObj = new GameObject("CountdownText");
            textObj.transform.SetParent(_panel.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.2f, 0.3f);
            textRect.anchorMax = new Vector2(0.8f, 0.7f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _countdownText = textObj.AddComponent<Text>();
            _countdownText.text = "";
            _countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _countdownText.fontSize = 240;
            _countdownText.alignment = TextAnchor.MiddleCenter;
            _countdownText.color = Color.white;
            _countdownText.fontStyle = FontStyle.Bold;
        }
    }
}
