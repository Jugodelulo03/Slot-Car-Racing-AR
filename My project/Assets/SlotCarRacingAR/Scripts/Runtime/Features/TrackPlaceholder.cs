using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// Visible track placeholder scaffold. Will be replaced by actual
    /// track rendering once marker detection provides a valid anchor.
    /// </summary>
    public sealed class TrackPlaceholder : MonoBehaviour
    {
        [SerializeField] private bool _showWhenUntracked;

        private Renderer[] _renderers;

        public bool HasLayout { get; private set; }

        public float WidthMeters { get; private set; }

        public float LengthMeters { get; private set; }

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            SetTrackingState(_showWhenUntracked);
        }

        private void Start()
        {
            UnityEngine.Debug.Log("[Track] Placeholder initialized. Waiting for anchor.");
        }

        /// <summary>
        /// Toggle the visible state of the placeholder based on marker tracking.
        /// </summary>
        public void SetTrackingState(bool isTracked)
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                return;
            }

            bool shouldRender = _showWhenUntracked || isTracked;
            for (int index = 0; index < _renderers.Length; index++)
            {
                _renderers[index].enabled = shouldRender;
            }
        }

        public void ApplySurfaceLayout(Vector3 center, Quaternion rotation, float widthMeters, float lengthMeters)
        {
            transform.SetPositionAndRotation(center, rotation);
            transform.localScale = new Vector3(
                Mathf.Max(0.05f, widthMeters),
                transform.localScale.y,
                Mathf.Max(0.05f, lengthMeters));

            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            HasLayout = true;
        }

        /// <summary>
        /// Sets layout when parented to an ARAnchor.
        /// Only updates local scale — position and rotation are
        /// handled by the parent hierarchy.
        /// </summary>
        public void ApplyLocalLayout(float widthMeters, float lengthMeters)
        {
            transform.localScale = new Vector3(
                Mathf.Max(0.05f, widthMeters),
                transform.localScale.y,
                Mathf.Max(0.05f, lengthMeters));

            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            HasLayout = true;
        }
    }
}
