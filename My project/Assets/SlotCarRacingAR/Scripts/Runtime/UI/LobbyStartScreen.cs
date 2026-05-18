using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Lobby start screen with two dominant actions: Create Match and Join Match.
    /// Built programmatically via UGUI. Provides onboarding guidance text.
    /// Respects landscape layout with top title, center guidance, bottom action zone.
    /// </summary>
    public sealed class LobbyStartScreen : MonoBehaviour
    {
        private Button _createMatchButton;
        private Button _joinMatchButton;

        public event System.Action OnCreateMatchClicked;
        public event System.Action OnJoinMatchClicked;

        private void Awake()
        {
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_createMatchButton != null)
                _createMatchButton.onClick.RemoveAllListeners();
            if (_joinMatchButton != null)
                _joinMatchButton.onClick.RemoveAllListeners();
        }

        private void BuildUI()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) return;

            // ── Background overlay ──
            CreateBackground(root);

            // ── Title area (top 20%) ──
            CreateTitle(root);

            // ── Onboarding guidance (center 30%-60%) ──
            CreateOnboardingGuidance(root);

            // ── Action buttons (bottom zone, 8%-38%) ──
            CreateActionButtons(root);
        }

        private static void CreateBackground(RectTransform parent)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(parent, false);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
            bgImage.raycastTarget = false;

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
        }

        private static void CreateTitle(RectTransform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "SLOT CAR RACING AR";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 48;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.raycastTarget = false;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.78f);
            titleRect.anchorMax = new Vector2(0.9f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
        }

        private void CreateOnboardingGuidance(RectTransform parent)
        {
            // Backplate for readability (UX-DR18)
            GameObject panelObj = new GameObject("OnboardingPanel");
            panelObj.transform.SetParent(parent, false);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.5f);
            panelBg.raycastTarget = false;

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.42f);
            panelRect.anchorMax = new Vector2(0.85f, 0.72f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Guidance text
            GameObject textObj = new GameObject("OnboardingText");
            textObj.transform.SetParent(panelObj.transform, false);

            Text guidanceText = textObj.AddComponent<Text>();
            guidanceText.text =
                "Ambos jugadores necesitan estar en la misma red Wi-Fi\n" +
                "y apuntar al mismo marcador sobre la mesa.\n\n" +
                "Regla: Mantén para acelerar, suelta en las curvas.";
            guidanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            guidanceText.fontSize = 26;
            guidanceText.alignment = TextAnchor.MiddleCenter;
            guidanceText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            guidanceText.raycastTarget = false;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void CreateActionButtons(RectTransform parent)
        {
            // ── Create Match button (primary CTA — amber, UX-DR19) ──
            _createMatchButton = CreateButton(
                parent,
                "CreateMatchButton",
                "Crear Partida",
                new Color(1f, 0.843f, 0.25f, 1f),   // Amber #FFD740
                new Color(0.12f, 0.12f, 0.12f, 1f),  // Dark text on amber
                new Vector2(0.08f, 0.10f),
                new Vector2(0.48f, 0.36f),
                36
            );
            _createMatchButton.onClick.AddListener(HandleCreateMatch);

            // ── Join Match button (secondary — lighter style) ──
            _joinMatchButton = CreateButton(
                parent,
                "JoinMatchButton",
                "Unirse",
                new Color(0.25f, 0.25f, 0.32f, 1f),  // Subtle dark
                Color.white,
                new Vector2(0.52f, 0.10f),
                new Vector2(0.92f, 0.36f),
                36
            );
            _joinMatchButton.onClick.AddListener(HandleJoinMatch);
        }

        private static Button CreateButton(
            RectTransform parent,
            string name,
            string label,
            Color bgColor,
            Color textColor,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int fontSize)
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

            // Button label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = fontSize;
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

        private void HandleCreateMatch()
        {
            UnityEngine.Debug.Log("[LobbyStartScreen] Create Match selected.");
            OnCreateMatchClicked?.Invoke();
        }

        private void HandleJoinMatch()
        {
            UnityEngine.Debug.Log("[LobbyStartScreen] Join Match selected.");
            OnJoinMatchClicked?.Invoke();
        }
    }
}
