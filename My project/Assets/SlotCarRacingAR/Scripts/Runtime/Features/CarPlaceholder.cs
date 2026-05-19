using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// Car entity that follows an OvalTrackDefinition spline.
    /// Graduated curve penalty system with 4 difficulty levels:
    ///   - Gentle: near-max speed OK, tiny speed loss
    ///   - Medium: must ease off accelerator
    ///   - Sharp: must brake before entering
    ///   - Hairpin: heavy braking required, spin-out if too fast
    /// Spin-out = car stops and does 2 full rotations on its axis.
    /// </summary>
    public sealed class CarPlaceholder : MonoBehaviour
    {
        [Header("Car Model")]
        [Tooltip("Scene-placed car model (child of this object). If assigned, uses as-is — no runtime scaling.")]
        [SerializeField] private Transform _sceneCarModel;
        [Tooltip("3D car model prefab (runtime instantiation). Only used if Scene Car Model is empty.")]
        [SerializeField] private GameObject _carModelPrefab;
        [Tooltip("Desired car length in meters (only for prefab path).")]
        [SerializeField] private float _carLengthMeters = 0.03f;

        [Header("Speed")]
        [SerializeField] private float _maxSpeedMetersPerSecond = 0.25f;
        [SerializeField] private float _accelerationRate = 0.3f;
        [SerializeField] private float _brakeRate = 0.6f;

        [Header("Curve Safe Speed (ratio of max speed — spin-out if exceeded)")]
        [Tooltip("Gentle curves: almost full speed OK.")]
        [SerializeField] [Range(0.5f, 1.0f)] private float _gentleSafeRatio = 0.90f;
        [Tooltip("Medium curves: spin-out above this ratio of max speed.")]
        [SerializeField] [Range(0.3f, 0.95f)] private float _mediumSafeRatio = 0.65f;
        [Tooltip("Sharp curves: spin-out above this ratio of max speed.")]
        [SerializeField] [Range(0.2f, 0.8f)] private float _sharpSafeRatio = 0.40f;
        [Tooltip("Hairpin curves: spin-out above this ratio of max speed.")]
        [SerializeField] [Range(0.1f, 0.6f)] private float _hairpinSafeRatio = 0.20f;

        [Header("Spin-Out Penalty")]
        [Tooltip("Number of full rotations during spin-out.")]
        [SerializeField] private int _spinOutRotations = 2;
        [Tooltip("Duration of spin-out in seconds.")]
        [SerializeField] private float _spinOutDuration = 1.5f;
        [Tooltip("How far the car slides off-track before returning (meters).")]
        [SerializeField] private float _spinOutSlideDistance = 0.03f;

        // Runtime state
        private OvalTrackDefinition _track;
        private float _currentSpeed;
        private float _trackProgress; // 0..1 around the loop
        private int _lapCount;
        private bool _accelerationHeld;

        // Penalty states
        private enum CarState { Normal, SpinOut }
        private CarState _state = CarState.Normal;
        private float _spinOutTimer;
        private float _spinOutStartYaw;
        private Vector3 _spinOutTrackPos;   // position on track where spin-out started
        private Vector3 _spinOutDirection;  // forward direction at moment of spin-out

        private const float CubeSize = 1.5f;

        // --- Public diagnostics ---
        public float Speed => _currentSpeed;
        public float TrackProgress => _trackProgress;
        public int LapCount => _lapCount;
        public bool IsInCurve => _track != null && _track.IsCurveAtProgress(_trackProgress);
        public bool IsInPenalty => _state == CarState.SpinOut;
        public bool IsUnstable => false;
        public float MaxSpeed => _maxSpeedMetersPerSecond;
        public CurveDifficulty CurrentDifficulty => _track != null ? _track.GetDifficultyAtProgress(_trackProgress) : CurveDifficulty.Straight;
        public float CurrentSafeSpeed => GetSafeSpeed(CurrentDifficulty);
        public float CurveSpeedLimit => CurrentSafeSpeed; // backward compat
        public float TrackCurvePercentage => _track != null ? _track.CurvePercentage : 0f;
        public float TrackTotalLength => _track != null ? _track.TotalLength : 0f;
        public float CurrentCurvatureAngle => _track != null ? _track.CurvatureAngleAtProgress(_trackProgress) : 0f;
        public string StateLabel => _state.ToString();

        /// <summary>
        /// Applies the same scale factor used for the track to the car.
        /// Called by MarkerDetectionEntryPoint so the car matches the track's AR size.
        /// </summary>
        public void ApplyTrackScaleFactor(float scaleFactor)
        {
            transform.localScale = Vector3.one * scaleFactor;
            UnityEngine.Debug.Log($"[Car] Applied track scale factor {scaleFactor:F6}.");
        }

        public void BindTrack(OvalTrackDefinition track)
        {
            _track = track;
            _trackProgress = 0f;
            _lapCount = 0;
            _currentSpeed = 0f;
            _state = CarState.Normal;
            _spinOutTimer = 0f;

            EnsureVisual();
            UpdateCarTransform();

            UnityEngine.Debug.Log("[Car] Bound to oval track.");
        }

        public void SetAccelerationHeld(bool isPressed)
        {
            _accelerationHeld = isPressed;
        }

        public void Accelerate(float input)
        {
            _accelerationHeld = input > 0.5f;
        }

        private float GetSafeSpeed(CurveDifficulty difficulty)
        {
            switch (difficulty)
            {
                case CurveDifficulty.Gentle: return _maxSpeedMetersPerSecond * _gentleSafeRatio;
                case CurveDifficulty.Medium: return _maxSpeedMetersPerSecond * _mediumSafeRatio;
                case CurveDifficulty.Sharp: return _maxSpeedMetersPerSecond * _sharpSafeRatio;
                case CurveDifficulty.Hairpin: return _maxSpeedMetersPerSecond * _hairpinSafeRatio;
                default: return _maxSpeedMetersPerSecond;
            }
        }

        private void Update()
        {
            if (_track == null) return;

            float dt = Time.deltaTime;

            switch (_state)
            {
                case CarState.SpinOut:
                    UpdateSpinOut(dt);
                    return; // don't move along track

                case CarState.Normal:
                    UpdateDriving(dt);
                    break;
            }

            // Move along track
            if (_currentSpeed > 0f && _track.TotalLength > 0f)
            {
                float distanceThisFrame = _currentSpeed * dt;
                _trackProgress += distanceThisFrame / _track.TotalLength;

                if (_trackProgress >= 1f)
                {
                    _trackProgress -= 1f;
                    _lapCount++;
                }

                UpdateCarTransform();
            }
        }

        private void UpdateDriving(float dt)
        {
            CurveDifficulty difficulty = _track.GetDifficultyAtProgress(_trackProgress);
            float safeSpeed = GetSafeSpeed(difficulty);

            // Acceleration / braking input
            if (_accelerationHeld)
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, _maxSpeedMetersPerSecond, _accelerationRate * dt);
            }
            else
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _brakeRate * dt);
            }

            // On straights and gentle curves: no penalty
            if (difficulty <= CurveDifficulty.Gentle)
            {
                return;
            }

            // Medium / Sharp / Hairpin: if over safe speed → spin-out immediately
            if (_currentSpeed > safeSpeed)
            {
                BeginSpinOut();
            }
        }

        private void BeginSpinOut()
        {
            _state = CarState.SpinOut;
            _spinOutTrackPos = _track.GetPositionAtProgress(_trackProgress);
            _spinOutDirection = _track.GetForwardAtProgress(_trackProgress);
            _spinOutStartYaw = transform.localEulerAngles.y;
            _spinOutTimer = _spinOutDuration;
            _currentSpeed = 0f;
            UnityEngine.Debug.Log($"[Car] SPIN-OUT! Difficulty={_track.GetDifficultyAtProgress(_trackProgress)} " +
                                  $"Angle={_track.CurvatureAngleAtProgress(_trackProgress):F1}°");
        }

        private void UpdateSpinOut(float dt)
        {
            _spinOutTimer -= dt;
            if (_spinOutTimer <= 0f)
            {
                // End spin-out: teleport back to track position
                _state = CarState.Normal;
                _spinOutTimer = 0f;
                UpdateCarTransform();
                return;
            }

            // progress01: 0 = just started → 1 = about to end
            float progress01 = 1f - (_spinOutTimer / _spinOutDuration);

            // Slide forward the whole time (ease-out)
            float slideOffset = Mathf.Sin(progress01 * Mathf.PI * 0.5f) * _spinOutSlideDistance;

            // Spin: full rotations over the duration
            float totalDegrees = _spinOutRotations * 360f;
            float currentYaw = _spinOutStartYaw + totalDegrees * progress01;

            Vector3 pos = _spinOutTrackPos + _spinOutDirection * slideOffset;
            transform.localPosition = pos;
            transform.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
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

        private void EnsureVisual()
        {
            // If a scene car model is assigned, use it — it scales with the parent via ApplyTrackScaleFactor
            if (_sceneCarModel != null)
            {
                // Destroy any runtime-created visuals (cubes from previous binds)
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = transform.GetChild(i);
                    if (child != _sceneCarModel)
                        Destroy(child.gameObject);
                }
                _sceneCarModel.gameObject.SetActive(true);
                UnityEngine.Debug.Log("[Car] Using scene-placed car model.");
                return;
            }

            // Clear any existing visual children
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            CreateCubeFallback();
        }

        private void LoadCarModel()
        {
            GameObject car = Instantiate(_carModelPrefab, transform, false);
            car.name = "CarModel";

            // Reset to identity so we measure the model's true shape
            car.transform.localPosition = Vector3.zero;
            car.transform.localRotation = Quaternion.identity;
            // Keep the import localScale (handles cm→m, Z-up→Y-up, etc.)
            Vector3 importScale = car.transform.localScale;

            // Measure bounds in parent-local space to avoid anchor scale interference
            Renderer[] renderers = car.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[Car] Model has no renderers, using cube fallback.");
                Destroy(car);
                CreateCubeFallback();
                return;
            }

            // Compute local-space bounds by transforming mesh vertices to car's local space
            MeshFilter[] meshFilters = car.GetComponentsInChildren<MeshFilter>(true);
            if (meshFilters.Length == 0)
            {
                Destroy(car);
                CreateCubeFallback();
                return;
            }

            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsInitialized = false;
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                Bounds meshBounds = mf.sharedMesh.bounds;
                // Transform mesh bounds corners into car's local space
                Vector3 meshCenter = mf.transform.localPosition + Vector3.Scale(meshBounds.center, mf.transform.localScale);
                Vector3 meshSize = Vector3.Scale(meshBounds.size, mf.transform.localScale);
                // Account for the child hierarchy up to car root
                Matrix4x4 childToCarLocal = car.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                // Transform all 8 corners
                Vector3 ext = meshBounds.extents;
                Vector3 center = meshBounds.center;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 pt = center + new Vector3(
                        (corner & 1) == 0 ? -ext.x : ext.x,
                        (corner & 2) == 0 ? -ext.y : ext.y,
                        (corner & 4) == 0 ? -ext.z : ext.z);
                    Vector3 worldPt = mf.transform.TransformPoint(pt);
                    Vector3 localPt = car.transform.InverseTransformPoint(worldPt);
                    if (!boundsInitialized)
                    {
                        localBounds = new Bounds(localPt, Vector3.zero);
                        boundsInitialized = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localPt);
                    }
                }
            }

            float maxExtent = Mathf.Max(localBounds.size.x, localBounds.size.y, localBounds.size.z);
            if (maxExtent < 0.0001f)
            {
                Destroy(car);
                CreateCubeFallback();
                return;
            }

            // Uniform scale: multiply import scale uniformly so longest axis = _carLengthMeters
            float scaleFactor = _carLengthMeters / maxExtent;
            car.transform.localScale = importScale * scaleFactor;

            // Center the model: offset by scaled bounds center, sit on track (bottom at Y=0)
            Vector3 scaledCenter = localBounds.center * scaleFactor;
            float scaledBottom = (localBounds.center.y - localBounds.extents.y) * scaleFactor;
            car.transform.localPosition = new Vector3(-scaledCenter.x, -scaledBottom, -scaledCenter.z);

            // Remove colliders (not needed for racing)
            foreach (Collider col in car.GetComponentsInChildren<Collider>())
                Destroy(col);

            // Fix materials (GLB imports often use shaders unavailable at runtime)
            FixMaterials(car);

            UnityEngine.Debug.Log($"[Car] Model loaded: '{_carModelPrefab.name}', importScale={importScale:F4}, " +
                                  $"localBounds={localBounds.size:F4}, scaleFactor={scaleFactor:F6}, " +
                                  $"final size ~{_carLengthMeters:F3}m");
        }

        private static void FixMaterials(GameObject obj)
        {
            Shader standard = Shader.Find("Standard");
            if (standard == null) return;

            foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    // Only fix if shader is missing/magenta or uses a non-built-in shader
                    if (mats[i].shader == null || mats[i].shader.name.Contains("Error") ||
                        mats[i].shader.name.Contains("glTF") || mats[i].shader.name.Contains("Universal"))
                    {
                        Color baseColor = mats[i].HasProperty("_BaseColor")
                            ? mats[i].GetColor("_BaseColor")
                            : mats[i].HasProperty("_Color")
                                ? mats[i].GetColor("_Color")
                                : Color.gray;
                        mats[i].shader = standard;
                        mats[i].SetColor("_Color", baseColor);
                        mats[i].SetFloat("_Metallic", 0.1f);
                        mats[i].SetFloat("_Glossiness", 0.3f);
                    }
                }
                r.materials = mats;
            }
        }

        private void CreateCubeFallback()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CarCubeVisual";
            cube.transform.SetParent(transform, false);
            cube.transform.localScale = Vector3.one * CubeSize;
            cube.transform.localPosition = new Vector3(0f, CubeSize * 0.5f, 0f);

            Collider col = cube.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = cube.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.red;
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
