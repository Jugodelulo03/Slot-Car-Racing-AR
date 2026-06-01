using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SlotCarRacingAR.Runtime.UI;

namespace SlotCarRacingAR.Runtime.App
{
    /// <summary>
    /// Minimal lobby button placeholder that forwards directly to the lobby
    /// composition root so scene flow can be exercised before the real UI exists.
    /// </summary>
    public sealed class LobbyStartRaceButtonPlaceholder : MonoBehaviour, IPointerClickHandler
    {
        private LobbyCompositionRoot _lobbyCompositionRoot;

        private void Awake()
        {
            if (TryGetComponent<RectTransform>(out RectTransform rectTransform))
            {
                rectTransform.anchorMin = new Vector2(0.35f, 0.08f);
                rectTransform.anchorMax = new Vector2(0.65f, 0.2f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            if (!TryGetComponent<Image>(out Image image))
            {
                image = gameObject.AddComponent<Image>();
            }

            if (image != null)
            {
                RetroUi.StyleImageAsPanel(image, RetroUi.Red);
            }

            if (transform.Find("Label") == null)
            {
                RetroUi.CreateText(
                    transform,
                    "Label",
                    "CONTINUAR",
                    Vector2.zero,
                    Vector2.one,
                    30,
                    RetroUi.White,
                    TextAnchor.MiddleCenter,
                    FontStyle.BoldAndItalic);
            }
        }

        public void Bind(LobbyCompositionRoot lobbyCompositionRoot)
        {
            _lobbyCompositionRoot = lobbyCompositionRoot;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _lobbyCompositionRoot?.TransitionToRace();
        }
    }
}
