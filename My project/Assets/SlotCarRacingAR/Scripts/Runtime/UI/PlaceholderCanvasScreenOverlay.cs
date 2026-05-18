using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Forces scaffold canvases into a predictable screen-space configuration
    /// so placeholder buttons stay visible while the real HUD is not built yet.
    /// </summary>
    public sealed class PlaceholderCanvasScreenOverlay : MonoBehaviour
    {
        [SerializeField] private Vector2 _referenceResolution = new(1920f, 1080f);

        private void Awake()
        {
            if (TryGetComponent<Canvas>(out Canvas canvas))
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = false;
            }

            if (TryGetComponent<CanvasScaler>(out CanvasScaler scaler))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = _referenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
            }
        }
    }
}