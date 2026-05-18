using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// Car entity that follows an OvalTrackDefinition spline.
    /// Accelerates when the player holds the button, decelerates on release.
    /// Represented as a cube until real car art exists.
    /// Penalizes speed in curves: if going too fast in a curve, applies braking.
    /// </summary>
    public sealed class CarPlaceholder : MonoBehaviour
    {
        [SerializeField] private float _maxSpeedMetersPerSecond = 0.6f;
        [SerializeField] private float _accelerationRate = 0.8f;
        [SerializeField] private float _brakeRate = 1.5f;
        [SerializeField] private float _curveSpeedLimit = 0.25f;
        [SerializeField] private float _curvePenaltyBrakeRate = 3f;

        private OvalTrackDefinition _track;
        private float _currentSpeed;
        private float _trackProgress; // 0..1 around the loop
        private int _lapCount;
        private bool _accelerationHeld;
        private bool _inCurvePenalty;
        private float _penaltyTimer;

        private const float PenaltyDuration = 0.6f;
        private const float CubeSize = 0.08f; // 8cm cube

        public float Speed => _currentSpeed;
        public float TrackProgress => _trackProgress;
        public int LapCount => _lapCount;
        public bool IsInCurve => _track != null && _track.IsCurveAtProgress(_trackProgress);
        public bool IsInPenalty => _inCurvePenalty;
        public float MaxSpeed => _maxSpeedMetersPerSecond;

        /// <summary>
        /// Initialize the car with a track to follow. Called by composition root.
        /// </summary>
        public void BindTrack(OvalTrackDefinition track)
        {
            _track = track;
            _trackProgress = 0f;
            _lapCount = 0;
            _currentSpeed = 0f;
            _inCurvePenalty = false;

            // Make the car a visible cube
            EnsureCubeVisual();
            UpdateCarTransform();

            UnityEngine.Debug.Log("[Car] Bound to oval track.");
        }

        /// <summary>
        /// Toggle held acceleration state from the input placeholder.
        /// </summary>
        public void SetAccelerationHeld(bool isPressed)
        {
            _accelerationHeld = isPressed;
        }

        /// <summary>
        /// Apply acceleration input to the car.
        /// </summary>
        public void Accelerate(float input)
        {
            _accelerationHeld = input > 0.5f;
        }

        private void Update()
        {
            if (_track == null) return;

            float dt = Time.deltaTime;

            // Handle curve penalty
            if (_inCurvePenalty)
            {
                _penaltyTimer -= dt;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _curvePenaltyBrakeRate * dt);
                if (_penaltyTimer <= 0f)
                {
                    _inCurvePenalty = false;
                }
            }
            else
            {
                // Normal speed control
                if (_accelerationHeld)
                {
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, _maxSpeedMetersPerSecond, _accelerationRate * dt);
                }
                else
                {
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _brakeRate * dt);
                }

                // Curve speed check
                if (_track.IsCurveAtProgress(_trackProgress) && _currentSpeed > _curveSpeedLimit)
                {
                    _inCurvePenalty = true;
                    _penaltyTimer = PenaltyDuration;
                    UnityEngine.Debug.Log($"[Car] Curve penalty! Speed {_currentSpeed:F2} > limit {_curveSpeedLimit:F2}");
                }
            }

            // Move along track
            if (_currentSpeed > 0f && _track.TotalLength > 0f)
            {
                float prevProgress = _trackProgress;
                float distanceThisFrame = _currentSpeed * dt;
                _trackProgress += distanceThisFrame / _track.TotalLength;

                // Lap detection
                if (_trackProgress >= 1f)
                {
                    _trackProgress -= 1f;
                    _lapCount++;
                    UnityEngine.Debug.Log($"[Car] Lap {_lapCount} completed!");
                }

                UpdateCarTransform();
            }
        }

        private void UpdateCarTransform()
        {
            if (_track == null) return;

            Vector3 pos = _track.GetPositionAtProgress(_trackProgress);
            Vector3 fwd = _track.GetForwardAtProgress(_trackProgress);

            transform.localPosition = pos;
            if (fwd.sqrMagnitude > 0.001f)
            {
                transform.localRotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
        }

        private void EnsureCubeVisual()
        {
            // Check if we already have a visual child
            if (transform.childCount > 0) return;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CarCubeVisual";
            cube.transform.SetParent(transform, false);
            cube.transform.localScale = Vector3.one * CubeSize;
            cube.transform.localPosition = new Vector3(0f, CubeSize * 0.5f, 0f); // Sit on top of track

            Collider col = cube.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = cube.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.red; // Player 1 = red (UX-DR19)
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
        }

        private void Start()
        {
            UnityEngine.Debug.Log("[Car] Placeholder initialized.");
        }
    }
}
