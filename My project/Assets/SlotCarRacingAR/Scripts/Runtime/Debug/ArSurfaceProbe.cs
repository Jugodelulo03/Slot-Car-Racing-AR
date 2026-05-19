using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SlotCarRacingAR.Runtime.Debug
{
    [DisallowMultipleComponent]
    public sealed class ArSurfaceProbe : MonoBehaviour
    {
        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>(8);

        [SerializeField] private Camera _arCamera;
        [SerializeField] private ARPlaneManager _arPlaneManager;
        [SerializeField] private ARRaycastManager _arRaycastManager;

        private GameObject _probeRoot;
        private bool _hasPlacement;
        private Pose _lastHitPose;
        private float _lastHitDistanceMeters = -1f;
        private float _lastHitTime = -1f;

        public bool HasPlacement => _hasPlacement;
        public Vector3 LastHitPosition => _lastHitPose.position;
        public Vector3 LastHitNormal => _lastHitPose.rotation * Vector3.up;
        public float LastHitDistanceMeters => _lastHitDistanceMeters;
        public float LastHitAgeSeconds => _lastHitTime < 0f ? -1f : Time.unscaledTime - _lastHitTime;

        public int PlaneCount => CountPlanes(trackingOnly: false);

        public int TrackingPlaneCount => CountPlanes(trackingOnly: true);

        public void Bind(Camera arCamera, ARPlaneManager arPlaneManager, ARRaycastManager arRaycastManager)
        {
            _arCamera = arCamera;
            _arPlaneManager = arPlaneManager;
            _arRaycastManager = arRaycastManager;
        }

        private void OnDisable()
        {
            if (_probeRoot != null)
            {
                _probeRoot.SetActive(false);
            }
        }

        private void Update()
        {
            ResolveMissingReferences();

            if (_arCamera == null || _arPlaneManager == null || _arRaycastManager == null || !_arPlaneManager.enabled || !_arRaycastManager.enabled)
            {
                return;
            }

            Vector2 screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!_arRaycastManager.Raycast(screenPoint, Hits, TrackableType.PlaneWithinPolygon))
            {
                return;
            }

            ARRaycastHit hit = Hits[0];
            Pose targetPose = hit.pose;
            Vector3 up = targetPose.up;
            Vector3 forward = Vector3.ProjectOnPlane(_arCamera.transform.forward, up);
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, up);
            }

            Quaternion targetRotation = forward.sqrMagnitude < 0.001f
                ? Quaternion.LookRotation(Vector3.forward, up)
                : Quaternion.LookRotation(forward.normalized, up);

            // Probe visual disabled — data is still available via properties

            _hasPlacement = true;
            _lastHitPose = targetPose;
            _lastHitDistanceMeters = hit.distance;
            _lastHitTime = Time.unscaledTime;
        }

        private void ResolveMissingReferences()
        {
            _arCamera ??= GetComponentInChildren<Camera>(true);
            _arPlaneManager ??= GetComponentInChildren<ARPlaneManager>(true);
            _arRaycastManager ??= GetComponentInChildren<ARRaycastManager>(true);
        }

        private int CountPlanes(bool trackingOnly)
        {
            if (_arPlaneManager == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ARPlane plane in _arPlaneManager.trackables)
            {
                if (!trackingOnly || plane.trackingState == TrackingState.Tracking)
                {
                    count++;
                }
            }

            return count;
        }

        private void EnsureProbeVisual()
        {
            if (_probeRoot != null)
            {
                return;
            }

            _probeRoot = new GameObject("AR Surface Probe");
            _probeRoot.transform.SetParent(null, false);
            _probeRoot.SetActive(false);

            CreateProbePart("Base", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.08f, 0.0035f, 0.08f), new Color(0.96f, 0.52f, 0.16f));
            CreateProbePart("Post", PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0f), new Vector3(0.012f, 0.045f, 0.012f), new Color(0.12f, 0.78f, 0.92f));
            CreateProbePart("Cap", PrimitiveType.Sphere, new Vector3(0f, 0.095f, 0f), new Vector3(0.03f, 0.03f, 0.03f), new Color(1f, 0.95f, 0.92f));
        }

        private void CreateProbePart(string partName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = partName;
            part.transform.SetParent(_probeRoot.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }
}