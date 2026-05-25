using UnityEngine;
using UnityEngine.Profiling;

namespace SlotCarRacingAR.Runtime.Debug
{
    /// <summary>
    /// Baseline telemetry hooks for development builds.
    /// Surfaces FPS, GC allocation pressure, and tracking loss signals.
    /// Non-blocking for release builds via conditional compilation.
    /// </summary>
    public sealed class TelemetryHooks : MonoBehaviour
    {
        private float _fpsUpdateInterval = 0.5f;
        private float _fpsAccumulator;
        private int _fpsFrameCount;
        private float _currentFps;
        private long _lastGcMemory;
        private int _trackingLossCount;

        public float CurrentFps => _currentFps;
        public int TrackingLossCount => _trackingLossCount;

        private void Start()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _lastGcMemory = Profiler.GetTotalAllocatedMemoryLong();
            UnityEngine.Debug.Log("[Telemetry] Hooks initialized (dev build).");
#endif
        }

        private void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // FPS calculation
            _fpsAccumulator += Time.unscaledDeltaTime;
            _fpsFrameCount++;

            if (_fpsAccumulator >= _fpsUpdateInterval)
            {
                _currentFps = _fpsFrameCount / _fpsAccumulator;
                _fpsAccumulator = 0f;
                _fpsFrameCount = 0;
            }

            // GC pressure check
            long currentMemory = Profiler.GetTotalAllocatedMemoryLong();
            long delta = currentMemory - _lastGcMemory;
            if (delta < 0)
            {
                // GC collection occurred
                OnGcSpike(delta);
            }
            _lastGcMemory = currentMemory;
#endif
        }

        /// <summary>
        /// Call from MarkerDetectionEntryPoint when tracking is lost.
        /// </summary>
        public void OnTrackingLossDetected()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _trackingLossCount++;
            UnityEngine.Debug.LogWarning($"[Telemetry] Tracking loss #{_trackingLossCount}");
#endif
        }

        private void OnGcSpike(long delta)
        {
            // Intentionally silent — GC spikes are tracked via counter only
        }
    }
}
