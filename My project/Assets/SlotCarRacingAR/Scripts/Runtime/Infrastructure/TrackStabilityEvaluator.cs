using System;
using UnityEngine;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    /// <summary>
    /// Evaluates spatial tracking stability by monitoring anchor pose drift over time.
    /// States: Scanning → Unstable → Stable.
    /// </summary>
    public sealed class TrackStabilityEvaluator : MonoBehaviour
    {
        private const float StabilityWindowSeconds = 2.0f;
        private const float PositionThresholdMeters = 0.005f; // 5mm drift tolerance
        private const float RotationThresholdDegrees = 1.5f;  // 1.5° rotation tolerance

        private Transform _anchorTransform;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private float _stableTimer;
        private bool _isEvaluating;

        public TrackStabilityState State { get; private set; } = TrackStabilityState.Scanning;

        /// <summary>Fired when stability state changes.</summary>
        public event Action<TrackStabilityState> OnStabilityChanged;

        /// <summary>Start evaluating stability of the given anchor transform.</summary>
        public void BeginEvaluation(Transform anchorTransform)
        {
            _anchorTransform = anchorTransform;
            _lastPosition = anchorTransform.position;
            _lastRotation = anchorTransform.rotation;
            _stableTimer = 0f;
            _isEvaluating = true;
            SetState(TrackStabilityState.Unstable);
        }

        /// <summary>Stop evaluating (e.g., tracking lost).</summary>
        public void StopEvaluation()
        {
            _isEvaluating = false;
            SetState(TrackStabilityState.Scanning);
        }

        private void Update()
        {
            if (!_isEvaluating || _anchorTransform == null) return;

            Vector3 currentPos = _anchorTransform.position;
            Quaternion currentRot = _anchorTransform.rotation;

            float posDrift = Vector3.Distance(currentPos, _lastPosition);
            float rotDrift = Quaternion.Angle(currentRot, _lastRotation);

            if (posDrift < PositionThresholdMeters && rotDrift < RotationThresholdDegrees)
            {
                _stableTimer += Time.deltaTime;
                if (_stableTimer >= StabilityWindowSeconds && State != TrackStabilityState.Stable)
                {
                    SetState(TrackStabilityState.Stable);
                }
            }
            else
            {
                // Drift detected — reset timer
                _stableTimer = 0f;
                _lastPosition = currentPos;
                _lastRotation = currentRot;
                if (State == TrackStabilityState.Stable)
                {
                    SetState(TrackStabilityState.Unstable);
                }
            }
        }

        private void SetState(TrackStabilityState newState)
        {
            if (State == newState) return;
            State = newState;
            UnityEngine.Debug.Log("[TrackStability] State → " + newState);
            OnStabilityChanged?.Invoke(newState);
        }
    }

    public enum TrackStabilityState
    {
        Scanning,
        Unstable,
        Stable
    }
}
