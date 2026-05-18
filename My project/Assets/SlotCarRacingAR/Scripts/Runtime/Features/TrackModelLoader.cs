using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// Loads a 3D track model prefab, scales it uniformly, and parents it
    /// under the ARAnchor so it sits on the detected surface.
    /// </summary>
    public sealed class TrackModelLoader : MonoBehaviour
    {
        private GameObject _instance;
        private Vector3 _prefabScale;
        private Quaternion _prefabRotation;
        private float _nativeMaxExtent;  // world-space max extent at prefab's native scale

        /// <summary>
        /// XZ bounding size of the model after scaling (world-space).
        /// Used to synchronize the racing line with the rendered model.
        /// </summary>
        public Vector3 RenderedBoundsSize { get; private set; }

        /// <summary>
        /// Y position of the top surface of the model in anchor-local space.
        /// The car should drive at this height.
        /// </summary>
        public float SurfaceY { get; private set; }

        /// <summary>
        /// Diagnostic log visible from ArDebugOverlay.
        /// </summary>
        public static string DiagnosticLog { get; private set; } = "not loaded yet";

        private static readonly System.Text.StringBuilder _diagBuf = new();

        public void Load(GameObject prefab, Transform parent, float targetSize, float heightOffset)
        {
            _diagBuf.Clear();

            if (prefab == null)
            {
                DiagnosticLog = "ERROR: no prefab";
                return;
            }

            if (_instance != null) Destroy(_instance);

            _diagBuf.AppendLine($"prefab: '{prefab.name}' children={prefab.transform.childCount}");

            _instance = Instantiate(prefab, parent, false);
            _instance.name = "TrackModel";
            _instance.SetActive(true);

            foreach (Transform t in _instance.GetComponentsInChildren<Transform>(true))
                t.gameObject.SetActive(true);

            MeshFilter[] meshFilters = _instance.GetComponentsInChildren<MeshFilter>(true);
            int totalVerts = 0;
            foreach (MeshFilter mf in meshFilters)
                if (mf.sharedMesh != null) totalVerts += mf.sharedMesh.vertexCount;

            Renderer[] allRenderers = _instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in allRenderers)
                r.enabled = true;

            // Log shader names (unique only)
            var shaderSet = new System.Collections.Generic.HashSet<string>();
            foreach (Renderer r in allRenderers)
                foreach (Material m in r.sharedMaterials)
                    if (m != null) shaderSet.Add(m.shader.name);

            _diagBuf.AppendLine($"meshes={meshFilters.Length} verts={totalVerts} renderers={allRenderers.Length}");
            _diagBuf.Append("shaders: ");
            foreach (string s in shaderSet) _diagBuf.Append(s).Append(", ");
            _diagBuf.AppendLine();

            FixAllMaterials(_instance);

            // Preserve prefab import rotation & scale (handles Z-up→Y-up, cm→m, etc.)
            _prefabRotation = _instance.transform.localRotation;
            _prefabScale = _instance.transform.localScale;
            _instance.transform.localPosition = Vector3.zero;

            // Measure bounds at prefab's native orientation/scale
            Bounds bounds = ComputeBounds(_instance);
            float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            _nativeMaxExtent = maxExtent;  // store for Rescale
            _diagBuf.AppendLine($"bounds: {bounds.size:F2} max={maxExtent:F2}");
            _diagBuf.AppendLine($"prefabScale={_prefabScale:F4} prefabRot={_prefabRotation.eulerAngles:F1}");
            _diagBuf.AppendLine($"targetSize={targetSize:F3}m");

            if (maxExtent > 0.0001f)
            {
                float scaleFactor = targetSize / maxExtent;
                _instance.transform.localScale = _prefabScale * scaleFactor;

                // Compute bounds in anchor-local space (not world AABB which inflates on rotation)
                RenderedBoundsSize = ComputeLocalBoundsSize(_instance, parent);

                // Center using world bounds (still needed for correct offset)
                Bounds scaledBounds = ComputeBounds(_instance);
                Vector3 localCenter = parent.InverseTransformPoint(scaledBounds.center);
                _instance.transform.localPosition = new Vector3(-localCenter.x, heightOffset, -localCenter.z);

                // Surface Y = base height + local model thickness
                SurfaceY = heightOffset + RenderedBoundsSize.y;

                _diagBuf.AppendLine($"scaleFactor={scaleFactor:F6}");
                _diagBuf.AppendLine($"localBoundsSize={RenderedBoundsSize:F4}");
                _diagBuf.AppendLine($"surfaceY={SurfaceY:F4}");
                _diagBuf.AppendLine($"localPos={_instance.transform.localPosition:F4}");
                _diagBuf.AppendLine($"worldPos={_instance.transform.position:F4}");
                _diagBuf.AppendLine("STATUS: OK");
            }
            else
            {
                _instance.transform.localPosition = new Vector3(0f, heightOffset, 0f);
                _diagBuf.AppendLine("STATUS: FAIL - zero bounds!");
            }

            DiagnosticLog = _diagBuf.ToString();
            UnityEngine.Debug.Log($"[TrackModelLoader]\n{DiagnosticLog}");
        }

        /// <summary>
        /// Re-scale and reposition the model to match a new target size / height.
        /// Called when the user adjusts Scale or Height sliders.
        /// </summary>
        public void Rescale(float targetSize, float heightOffset)
        {
            if (_instance == null || _nativeMaxExtent <= 0.0001f) return;

            // Use stored native extent — avoids world-space AABB issues from anchor rotation
            float scaleFactor = targetSize / _nativeMaxExtent;
            _instance.transform.localRotation = _prefabRotation;
            _instance.transform.localScale = _prefabScale * scaleFactor;
            _instance.transform.localPosition = Vector3.zero;

            // Compute bounds in anchor-local space
            Transform parent = _instance.transform.parent;
            RenderedBoundsSize = ComputeLocalBoundsSize(_instance, parent);

            // Center using world bounds
            Bounds scaledBounds = ComputeBounds(_instance);
            Vector3 localCenter = parent.InverseTransformPoint(scaledBounds.center);
            _instance.transform.localPosition = new Vector3(-localCenter.x, heightOffset, -localCenter.z);

            // Surface Y = base height + local model thickness
            SurfaceY = heightOffset + RenderedBoundsSize.y;

            UnityEngine.Debug.Log($"[TrackModelLoader.Rescale] targetSize={targetSize:F4} nativeMax={_nativeMaxExtent:F2} " +
                                  $"scaleFactor={scaleFactor:F6} localScale={_instance.transform.localScale:F6} " +
                                  $"localPos={_instance.transform.localPosition:F4}");
        }

        public void Unload()
        {
            if (_instance != null)
            {
                Destroy(_instance);
                _instance = null;
            }
        }

        public GameObject Instance => _instance;

        private static Bounds ComputeBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(obj.transform.position, Vector3.zero);

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        /// <summary>
        /// Computes the bounding size of obj in the parent's local coordinate system.
        /// Unlike world AABB, this is not inflated by the parent's rotation.
        /// </summary>
        private static Vector3 ComputeLocalBoundsSize(GameObject obj, Transform parent)
        {
            Bounds worldBounds = ComputeBounds(obj);
            Vector3 center = worldBounds.center;
            Vector3 ext = worldBounds.extents;

            Vector3 localMin = Vector3.one * float.MaxValue;
            Vector3 localMax = Vector3.one * float.MinValue;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = center + new Vector3(
                    (i & 1) == 0 ? -ext.x : ext.x,
                    (i & 2) == 0 ? -ext.y : ext.y,
                    (i & 4) == 0 ? -ext.z : ext.z);
                Vector3 local = parent.InverseTransformPoint(corner);
                localMin = Vector3.Min(localMin, local);
                localMax = Vector3.Max(localMax, local);
            }

            return localMax - localMin;
        }

        private static void FixAllMaterials(GameObject obj)
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null)
            {
                // Fallback to mobile shader
                standardShader = Shader.Find("Mobile/Diffuse");
                if (standardShader == null)
                {
                    UnityEngine.Debug.LogError("[TrackModelLoader] No usable shader found!");
                    return;
                }
            }

            int fixedCount = 0;
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                Material[] mats = rend.materials; // creates copies
                for (int i = 0; i < mats.Length; i++)
                {
                    Material oldMat = mats[i];
                    if (oldMat == null) continue;

                    // Skip if already using Standard (avoid unnecessary work)
                    if (oldMat.shader == standardShader) continue;

                    Material newMat = new Material(standardShader);
                    newMat.name = oldMat.name + "_Fixed";

                    // Try to preserve base color from various property names
                    Color baseColor = Color.white;
                    string[] colorProps = { "_Color", "_BaseColor", "baseColorFactor", "_BaseColorFactor" };
                    foreach (string prop in colorProps)
                    {
                        if (oldMat.HasProperty(prop))
                        {
                            baseColor = oldMat.GetColor(prop);
                            break;
                        }
                    }
                    newMat.color = baseColor;

                    // Prevent reflections (skybox causes dark blue at small scales)
                    newMat.SetFloat("_Metallic", 0f);
                    newMat.SetFloat("_Glossiness", 0f);

                    // Try to preserve base texture from various property names
                    string[] texProps = { "_MainTex", "_BaseMap", "_BaseColorTexture", "baseColorTexture", "_AlbedoMap" };
                    foreach (string prop in texProps)
                    {
                        if (oldMat.HasProperty(prop))
                        {
                            Texture tex = oldMat.GetTexture(prop);
                            if (tex != null)
                            {
                                newMat.mainTexture = tex;
                                break;
                            }
                        }
                    }

                    mats[i] = newMat;
                    fixedCount++;
                }
                rend.materials = mats;
            }

            UnityEngine.Debug.Log($"[TrackModelLoader] Replaced {fixedCount} materials → Standard shader.");
        }
    }
}
