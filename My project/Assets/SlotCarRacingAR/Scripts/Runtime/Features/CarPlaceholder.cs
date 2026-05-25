using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    public enum CarPaletteColor
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Purple = 3,
        Yellow = 4,
        Orange = 5,
        White = 6
    }

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
        private bool _externalRaceStateEnabled;
        private float _laneOffsetMeters;
        private Color _visualColor = Color.white;
        private bool _hasVisualColor;

        // Penalty states
        private enum CarState { Normal, SpinOut }
        private CarState _state = CarState.Normal;
        private float _spinOutTimer;
        private float _spinOutStartYaw;
        private Vector3 _spinOutTrackPos;   // position on track where spin-out started
        private Vector3 _spinOutDirection;  // forward direction at moment of spin-out

        private const float CubeSize = 1.5f;
        private const int PaletteTextureSize = 8;
        private const string BodyPaletteMeshName = "Car.001_palette_0";
        private static readonly Vector2Int[] BodyPaletteSourceCells =
        {
            new(3, 4),
            new(3, 5),
        };

        // --- Public diagnostics ---
        public float Speed => _currentSpeed;
        public float TrackProgress => _trackProgress;
        public int LapCount => _lapCount;
        public bool IsInCurve => _track != null && _track.IsCurveAtProgress(_trackProgress);
        public bool IsInPenalty => _state == CarState.SpinOut;
        public bool IsUnstable => false;
        public float MaxSpeed => _maxSpeedMetersPerSecond;
        public float AccelerationRate => _accelerationRate;
        public float BrakeRate => _brakeRate;
        public float SpinOutDuration => _spinOutDuration;
        public CurveDifficulty CurrentDifficulty => _track != null ? _track.GetDifficultyAtProgress(_trackProgress) : CurveDifficulty.Straight;
        public float CurrentSafeSpeed => GetSafeSpeed(CurrentDifficulty);
        public float CurveSpeedLimit => CurrentSafeSpeed; // backward compat
        public float TrackCurvePercentage => _track != null ? _track.CurvePercentage : 0f;
        public float TrackTotalLength => _track != null ? _track.TotalLength : 0f;
        public float CurrentCurvatureAngle => _track != null ? _track.CurvatureAngleAtProgress(_trackProgress) : 0f;
        public string StateLabel => _state.ToString();
        public OvalTrackDefinition Track => _track;
        public Transform VisualRoot => _sceneCarModel;

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
            ApplyVisualColor();
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

        public float GetSafeSpeedForDifficulty(CurveDifficulty difficulty)
        {
            return GetSafeSpeed(difficulty);
        }

        public void SetExternalRaceStateEnabled(bool enabled)
        {
            _externalRaceStateEnabled = enabled;
            if (enabled)
            {
                _accelerationHeld = false;
            }
        }

        public void SetLaneOffset(float laneOffsetMeters)
        {
            _laneOffsetMeters = laneOffsetMeters;
            UpdateCarTransform();
        }

        public void SetVisualColor(Color color)
        {
            _visualColor = color;
            _hasVisualColor = true;
            ApplyVisualColor();
        }

        public bool LoadVisualFromResource(string resourcePath, Transform transformTemplate = null)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                UnityEngine.Debug.LogWarning($"[Car] Could not load car visual from Resources path '{resourcePath}'.");
                return false;
            }

            Transform template = transformTemplate != null ? transformTemplate : _sceneCarModel;
            Vector3 localPosition = template != null ? template.localPosition : Vector3.zero;
            Quaternion localRotation = template != null ? template.localRotation : Quaternion.identity;
            Vector3 localScale = template != null ? template.localScale : Vector3.one;

            ClearVisualChildren();

            GameObject visual = Instantiate(prefab, transform, false);
            visual.name = prefab.name + "_PlayerVisual";
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = localRotation;
            visual.transform.localScale = localScale;
            _sceneCarModel = visual.transform;
            _sceneCarModel.gameObject.SetActive(true);

            foreach (Collider collider in _sceneCarModel.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }

            ApplyVisualColor();
            UnityEngine.Debug.Log($"[Car] Loaded Resources car visual '{resourcePath}'.");
            return true;
        }

        public bool CloneVisualFrom(CarPlaceholder source, CarPaletteColor paletteColor)
        {
            if (source == null || source._sceneCarModel == null)
            {
                return false;
            }

            ClearVisualChildren();

            GameObject clone = Instantiate(source._sceneCarModel.gameObject, transform, false);
            clone.name = source._sceneCarModel.name + "_PlayerVisual";
            _sceneCarModel = clone.transform;
            _sceneCarModel.gameObject.SetActive(true);

            foreach (Collider collider in _sceneCarModel.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }

            return ApplyPaletteColor(paletteColor);
        }

        public bool ApplyPaletteColor(CarPaletteColor paletteColor)
        {
            if (_sceneCarModel == null)
            {
                return false;
            }

            Vector2Int targetCell = GetBodyPaletteTargetCell(paletteColor);
            bool changedAnyMesh = false;

            MeshFilter[] meshFilters = _sceneCarModel.GetComponentsInChildren<MeshFilter>(true);
            int changedUvCount = 0;
            for (int meshFilterIndex = 0; meshFilterIndex < meshFilters.Length; meshFilterIndex++)
            {
                MeshFilter meshFilter = meshFilters[meshFilterIndex];
                Mesh sourceMesh = meshFilter.sharedMesh;
                if (sourceMesh == null || !IsBodyPaletteMesh(meshFilter, sourceMesh))
                {
                    continue;
                }

                Mesh mesh = Instantiate(sourceMesh);
                mesh.name = sourceMesh.name + "_" + paletteColor;
                Vector2[] uvs = mesh.uv;
                bool changedMesh = false;

                for (int uvIndex = 0; uvIndex < uvs.Length; uvIndex++)
                {
                    if (!TryGetPaletteCell(uvs[uvIndex], out Vector2Int sourceCell))
                    {
                        continue;
                    }

                    if (!IsBodySourceCell(sourceCell))
                    {
                        continue;
                    }

                    uvs[uvIndex] = MoveUvToPaletteCell(uvs[uvIndex], sourceCell, targetCell);
                    changedMesh = true;
                    changedUvCount++;
                }

                if (!changedMesh)
                {
                    Destroy(mesh);
                    continue;
                }

                mesh.uv = uvs;
                meshFilter.sharedMesh = mesh;
                changedAnyMesh = true;
            }

            if (changedAnyMesh)
            {
                UnityEngine.Debug.Log($"[Car] Applied palette color: {paletteColor}. ChangedUVs={changedUvCount}");
            }
            else
            {
                UnityEngine.Debug.LogWarning(
                    $"[Car] Palette color {paletteColor} did not change any UVs. " +
                    $"SceneModel={_sceneCarModel.name}, MeshFilters={meshFilters.Length}");
            }

            return changedAnyMesh;
        }

        public void ApplyAuthoritativeState(float progress, float speed, int lap, bool penaltyActive)
        {
            if (_track == null)
            {
                return;
            }

            _trackProgress = Mathf.Repeat(progress, 1f);
            _currentSpeed = Mathf.Max(0f, speed);
            _lapCount = Mathf.Max(0, lap);
            _state = penaltyActive ? CarState.SpinOut : CarState.Normal;

            UpdateCarTransform();

            if (penaltyActive)
            {
                ApplyPenaltyPresentation();
            }
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
            if (_externalRaceStateEnabled) return;

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
            _spinOutTrackPos = GetLanePosition(_trackProgress);
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

            Vector3 pos = GetLanePosition(_trackProgress);
            Vector3 fwd = _track.GetForwardAtProgress(_trackProgress);

            transform.localPosition = pos;
            if (fwd.sqrMagnitude > 0.001f)
            {
                transform.localRotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
        }

        private Vector3 GetLanePosition(float progress)
        {
            Vector3 pos = _track.GetPositionAtProgress(progress);
            Vector3 fwd = _track.GetForwardAtProgress(progress);
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            if (right.sqrMagnitude > 0.001f)
            {
                pos += right * _laneOffsetMeters;
            }

            return pos;
        }

        private void ApplyPenaltyPresentation()
        {
            Vector3 fwd = _track.GetForwardAtProgress(_trackProgress);
            if (fwd.sqrMagnitude > 0.001f)
            {
                float yaw = Mathf.Sin(Time.time * 18f) * 35f;
                transform.localRotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, yaw, 0f);
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
                ApplyVisualColor();
                UnityEngine.Debug.Log("[Car] Using scene-placed car model.");
                return;
            }

            // Clear any existing visual children
            ClearVisualChildren();

            CreateCubeFallback();
            ApplyVisualColor();
        }

        private void ClearVisualChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private static bool IsBodyPaletteMesh(MeshFilter meshFilter, Mesh mesh)
        {
            if (mesh.name.Contains(BodyPaletteMeshName) || meshFilter.name.Contains(BodyPaletteMeshName))
            {
                return true;
            }

            Renderer renderer = meshFilter.GetComponent<Renderer>();
            if (renderer == null)
            {
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material != null && material.name.ToLowerInvariant().Contains("palette"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetPaletteCell(Vector2 uv, out Vector2Int cell)
        {
            int x = Mathf.FloorToInt(uv.x * PaletteTextureSize);
            int y = Mathf.FloorToInt(uv.y * PaletteTextureSize);
            if (x < 0 || x >= PaletteTextureSize || y < 0 || y >= PaletteTextureSize)
            {
                cell = default;
                return false;
            }

            cell = new Vector2Int(x, y);
            return true;
        }

        private static bool IsBodySourceCell(Vector2Int cell)
        {
            for (int i = 0; i < BodyPaletteSourceCells.Length; i++)
            {
                if (BodyPaletteSourceCells[i] == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 MoveUvToPaletteCell(Vector2 uv, Vector2Int sourceCell, Vector2Int targetCell)
        {
            return new Vector2(
                (targetCell.x + 0.5f) / PaletteTextureSize,
                (targetCell.y + 0.5f) / PaletteTextureSize);
        }

        private static Vector2Int GetBodyPaletteTargetCell(CarPaletteColor paletteColor)
        {
            switch (paletteColor)
            {
                case CarPaletteColor.Green:
                    return new Vector2Int(5, 6);
                case CarPaletteColor.Blue:
                    return new Vector2Int(1, 6);
                case CarPaletteColor.Purple:
                    return new Vector2Int(5, 5);
                case CarPaletteColor.Yellow:
                    return new Vector2Int(5, 2);
                case CarPaletteColor.White:
                    return new Vector2Int(1, 0);
                case CarPaletteColor.Orange:
                    return new Vector2Int(1, 3);
                case CarPaletteColor.Red:
                default:
                    return new Vector2Int(3, 4);
            }
        }

        private void ApplyVisualColor()
        {
            if (!_hasVisualColor)
            {
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.materials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                    {
                        continue;
                    }

                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", _visualColor);
                    }
                    else if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", _visualColor);
                    }
                }
            }
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
