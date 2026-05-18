using System;
using System.IO;
using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SlotCarRacingAR.Editor
{
    /// <summary>
    /// Generates a single high-feature-density marker image for track anchor detection.
    /// The marker uses procedural noise, geometric shapes, and varied textures to maximize
    /// ARCore natural feature detection at distance.
    /// </summary>
    internal static class TrackedImageSetupUtility
    {
        private const string MarkerTextureAssetPath = "Assets/SlotCarRacingAR/Data/MarkerProfiles/TrackAnchorMarker.png";
        private const string ReferenceImageLibraryAssetPath = "Assets/SlotCarRacingAR/Data/MarkerProfiles/DevelopmentMarkerLibrary.asset";
        private const string RaceScenePath = "Assets/SlotCarRacingAR/Scenes/Race.unity";
        private const int MarkerResolution = 1024;
        private const int MaxMovingImages = 1;
        private const string MarkerName = "track-anchor";

        // Physical size 20cm — larger print = better detection at distance
        private static readonly Vector2 MarkerSizeMeters = new Vector2(0.20f, 0.20f);

        [MenuItem("Slot Car Racing AR/Setup/Create Development Marker Assets")]
        private static void CreateDevelopmentMarkerAssets()
        {
            EnsureAssetFolderExists();

            Texture2D markerTexture = CreateOrUpdateMarkerTexture();

            XRReferenceImageLibrary library = CreateOrUpdateReferenceImageLibrary(markerTexture);
            AssignReferenceImageLibraryToRaceScene(library);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = library;
            EditorUtility.FocusProjectWindow();

            UnityEngine.Debug.Log($"[TrackedImageSetup] Track anchor marker generated and assigned to '{ReferenceImageLibraryAssetPath}'.");
        }

        private static void EnsureAssetFolderExists()
        {
            string absoluteTexturePath = GetAbsoluteAssetPath(MarkerTextureAssetPath);
            string directoryPath = Path.GetDirectoryName(absoluteTexturePath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new InvalidOperationException("Could not resolve the marker asset directory path.");
            }

            Directory.CreateDirectory(directoryPath);
        }

        private static Texture2D CreateOrUpdateMarkerTexture()
        {
            byte[] markerBytes = BuildHighFeatureMarkerPng(MarkerResolution);
            string absoluteTexturePath = GetAbsoluteAssetPath(MarkerTextureAssetPath);

            File.WriteAllBytes(absoluteTexturePath, markerBytes);
            AssetDatabase.ImportAsset(MarkerTextureAssetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(MarkerTextureAssetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not import marker texture at '{MarkerTextureAssetPath}'.");
            }

            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            Texture2D markerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerTextureAssetPath);
            if (markerTexture == null)
            {
                throw new InvalidOperationException($"Could not load marker texture at '{MarkerTextureAssetPath}'.");
            }

            return markerTexture;
        }

        private static XRReferenceImageLibrary CreateOrUpdateReferenceImageLibrary(Texture2D markerTexture)
        {
            XRReferenceImageLibrary library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(ReferenceImageLibraryAssetPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
                AssetDatabase.CreateAsset(library, ReferenceImageLibraryAssetPath);
            }

            while (library.count > 0)
            {
                library.RemoveAt(library.count - 1);
            }

            library.Add();
            library.SetName(0, MarkerName);
            library.SetSpecifySize(0, true);
            library.SetSize(0, MarkerSizeMeters);
            library.SetTexture(0, markerTexture, true);

            EditorUtility.SetDirty(library);
            return library;
        }

        private static void AssignReferenceImageLibraryToRaceScene(XRReferenceImageLibrary library)
        {
            Scene raceScene = SceneManager.GetSceneByPath(RaceScenePath);
            bool openedForSetup = !raceScene.isLoaded;

            if (openedForSetup)
            {
                raceScene = EditorSceneManager.OpenScene(RaceScenePath, OpenSceneMode.Additive);
            }

            ARTrackedImageManager trackedImageManager = FindInScene<ARTrackedImageManager>(raceScene);
            if (trackedImageManager == null)
            {
                throw new InvalidOperationException("Race scene does not contain an ARTrackedImageManager.");
            }

            trackedImageManager.referenceLibrary = library;
            trackedImageManager.requestedMaxNumberOfMovingImages = MaxMovingImages;
            EditorUtility.SetDirty(trackedImageManager);

            MarkerDetectionEntryPoint markerDetectionEntryPoint = FindInScene<MarkerDetectionEntryPoint>(raceScene);
            if (markerDetectionEntryPoint != null)
            {
                SerializedObject serializedObject = new SerializedObject(markerDetectionEntryPoint);
                serializedObject.FindProperty("_trackedImageManager").objectReferenceValue = trackedImageManager;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(markerDetectionEntryPoint);
            }

            EditorSceneManager.SaveScene(raceScene);

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(raceScene, true);
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                T component = rootObject.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>
        /// Generates a highly feature-rich marker image using multiple layers of procedural noise,
        /// geometric shapes, and varied intensity patterns. ARCore's natural feature detection
        /// works best with images that have many unique keypoints at multiple scales.
        /// </summary>
        private static byte[] BuildHighFeatureMarkerPng(int size)
        {
            Color32[] pixels = new Color32[size * size];
            // Deterministic seed for reproducibility
            const int seed = 42;

            // Layer 1: Multi-octave value noise base (creates natural-looking texture)
            FillWithValueNoise(pixels, size, seed, octaves: 6);

            // Layer 2: High-contrast geometric shapes scattered across image
            DrawScatteredShapes(pixels, size, seed);

            // Layer 3: Grid of varied-size dots (provides features at multiple scales)
            DrawFeatureGrid(pixels, size, seed);

            // Layer 4: Asymmetric frame border (orientation cue + strong edges)
            DrawAsymmetricFrame(pixels, size);

            // Layer 5: Central distinctive logo area (large-scale feature)
            DrawCenterLogo(pixels, size);

            // Layer 6: Corner markers for additional orientation features
            DrawCornerPatterns(pixels, size);

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            byte[] encoded = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return encoded;
        }

        private static void FillWithValueNoise(Color32[] pixels, int size, int seed, int octaves)
        {
            float[] noise = new float[size * size];
            float maxValue = 0f;

            for (int octave = 0; octave < octaves; octave++)
            {
                float frequency = Mathf.Pow(2f, octave + 2);
                float amplitude = 1f / (octave + 1);

                int offsetX = Hash(seed + octave * 7) % 1000;
                int offsetY = Hash(seed + octave * 13) % 1000;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float sampleX = (x + offsetX) * frequency / size;
                        float sampleY = (y + offsetY) * frequency / size;

                        float value = PerlinNoise(sampleX, sampleY) * amplitude;
                        noise[y * size + x] += value;
                    }
                }

                maxValue += amplitude;
            }

            // Normalize and apply contrast stretching
            for (int i = 0; i < pixels.Length; i++)
            {
                float normalized = (noise[i] / maxValue) * 0.7f + 0.15f; // range [0.15, 0.85]
                // Apply S-curve for more contrast
                normalized = normalized * normalized * (3f - 2f * normalized);
                byte gray = (byte)Mathf.Clamp((int)(normalized * 255f), 0, 255);
                pixels[i] = new Color32(gray, gray, gray, 255);
            }
        }

        private static void DrawScatteredShapes(Color32[] pixels, int size, int seed)
        {
            int shapeCount = 35;
            int margin = size / 8;

            for (int i = 0; i < shapeCount; i++)
            {
                int h = Hash(seed + i * 31 + 100);
                int cx = margin + (h % (size - margin * 2));
                int cy = margin + (Hash(seed + i * 47 + 200) % (size - margin * 2));
                int shapeSize = 12 + (Hash(seed + i * 17 + 300) % 40);
                byte intensity = (byte)((Hash(seed + i * 23 + 400) % 2 == 0) ? 20 : 235);
                Color32 color = new Color32(intensity, intensity, intensity, 255);

                int shapeType = Hash(seed + i * 53) % 4;
                switch (shapeType)
                {
                    case 0: // Filled circle
                        FillCircle(pixels, size, cx, cy, shapeSize / 2, color);
                        break;
                    case 1: // Filled rectangle
                        int w = shapeSize + (Hash(seed + i * 61) % shapeSize);
                        FillRect(pixels, size, cx - w / 2, cy - shapeSize / 2, w, shapeSize, color);
                        break;
                    case 2: // Ring
                        FillCircle(pixels, size, cx, cy, shapeSize / 2, color);
                        byte inner = (byte)(255 - intensity);
                        FillCircle(pixels, size, cx, cy, shapeSize / 3, new Color32(inner, inner, inner, 255));
                        break;
                    case 3: // Cross
                        int arm = shapeSize / 5;
                        FillRect(pixels, size, cx - shapeSize / 2, cy - arm, shapeSize, arm * 2, color);
                        FillRect(pixels, size, cx - arm, cy - shapeSize / 2, arm * 2, shapeSize, color);
                        break;
                }
            }
        }

        private static void DrawFeatureGrid(Color32[] pixels, int size, int seed)
        {
            int gridStep = size / 16;
            int margin = size / 10;

            for (int gy = 0; gy < 14; gy++)
            {
                for (int gx = 0; gx < 14; gx++)
                {
                    int h = Hash(seed + gx * 7 + gy * 113 + 500);
                    if (h % 3 == 0) continue; // skip some for irregularity

                    int cx = margin + gx * gridStep + (h % 6) - 3;
                    int cy = margin + gy * gridStep + (Hash(seed + gx + gy * 3) % 6) - 3;
                    int dotRadius = 2 + (h % 5);

                    byte intensity = (byte)(h % 2 == 0 ? 10 : 245);
                    FillCircle(pixels, size, cx, cy, dotRadius, new Color32(intensity, intensity, intensity, 255));
                }
            }
        }

        private static void DrawAsymmetricFrame(Color32[] pixels, int size)
        {
            Color32 black = new Color32(0, 0, 0, 255);
            Color32 white = new Color32(255, 255, 255, 255);
            int borderWidth = size / 16;

            // Thick black border on top and left
            FillRect(pixels, size, 0, size - borderWidth, size, borderWidth, black); // top
            FillRect(pixels, size, 0, 0, borderWidth, size, black); // left

            // Thinner border on bottom and right (asymmetry helps orientation)
            int thinBorder = borderWidth / 2;
            FillRect(pixels, size, 0, 0, size, thinBorder, black); // bottom
            FillRect(pixels, size, size - thinBorder, 0, thinBorder, size, black); // right

            // White quiet zone just inside frame
            int quietZone = 4;
            FillRect(pixels, size, borderWidth, size - borderWidth - quietZone, size - borderWidth - thinBorder, quietZone, white);
            FillRect(pixels, size, borderWidth, thinBorder, quietZone, size - borderWidth - thinBorder, white);
        }

        private static void DrawCenterLogo(Color32[] pixels, int size)
        {
            Color32 black = new Color32(0, 0, 0, 255);
            Color32 white = new Color32(255, 255, 255, 255);

            int cx = size / 2;
            int cy = size / 2;
            int logoRadius = size / 7;

            // White circle background
            FillCircle(pixels, size, cx, cy, logoRadius + 6, white);
            // Black ring
            FillCircle(pixels, size, cx, cy, logoRadius, black);
            FillCircle(pixels, size, cx, cy, logoRadius - 8, white);
            // Inner cross
            int barThickness = Math.Max(6, logoRadius / 4);
            FillRect(pixels, size, cx - logoRadius + 12, cy - barThickness / 2, (logoRadius - 12) * 2, barThickness, black);
            FillRect(pixels, size, cx - barThickness / 2, cy - logoRadius + 12, barThickness, (logoRadius - 12) * 2, black);
            // Center dot
            FillCircle(pixels, size, cx, cy, barThickness, black);
            // Asymmetric notch (top-right of center)
            FillRect(pixels, size, cx + logoRadius / 3, cy + logoRadius / 3, barThickness * 2, barThickness, black);
        }

        private static void DrawCornerPatterns(Color32[] pixels, int size)
        {
            Color32 black = new Color32(0, 0, 0, 255);
            Color32 white = new Color32(255, 255, 255, 255);
            int cornerSize = size / 8;
            int inset = size / 12;

            // Top-left: concentric squares
            int tlX = inset;
            int tlY = size - inset - cornerSize;
            FillRect(pixels, size, tlX, tlY, cornerSize, cornerSize, black);
            FillRect(pixels, size, tlX + 8, tlY + 8, cornerSize - 16, cornerSize - 16, white);
            FillRect(pixels, size, tlX + 18, tlY + 18, cornerSize - 36, cornerSize - 36, black);
            FillRect(pixels, size, tlX + 26, tlY + 26, cornerSize - 52, cornerSize - 52, white);

            // Top-right: diagonal stripes
            int trX = size - inset - cornerSize;
            int trY = size - inset - cornerSize;
            for (int stripe = 0; stripe < cornerSize * 2; stripe += 12)
            {
                for (int d = 0; d < 6; d++)
                {
                    int px = trX + stripe + d - cornerSize;
                    for (int py = trY; py < trY + cornerSize; py++)
                    {
                        int localX = px + (py - trY);
                        if (localX >= trX && localX < trX + cornerSize && py >= 0 && py < size)
                        {
                            pixels[py * size + localX] = black;
                        }
                    }
                }
            }

            // Bottom-left: checkerboard
            int blX = inset;
            int blY = inset;
            int checkSize = 8;
            for (int cy = 0; cy < cornerSize; cy += checkSize)
            {
                for (int ccx = 0; ccx < cornerSize; ccx += checkSize)
                {
                    bool isBlack = ((cy / checkSize) + (ccx / checkSize)) % 2 == 0;
                    if (isBlack)
                    {
                        FillRect(pixels, size, blX + ccx, blY + cy,
                            Math.Min(checkSize, cornerSize - ccx), Math.Min(checkSize, cornerSize - cy), black);
                    }
                }
            }

            // Bottom-right: filled triangle pointing up
            int brX = size - inset - cornerSize;
            int brY = inset;
            for (int row = 0; row < cornerSize; row++)
            {
                int rowWidth = (row * cornerSize) / cornerSize;
                int startX = brX + (cornerSize - rowWidth) / 2;
                for (int px = startX; px < startX + rowWidth; px++)
                {
                    int py = brY + row;
                    if (px >= 0 && px < size && py >= 0 && py < size)
                    {
                        pixels[py * size + px] = black;
                    }
                }
            }
        }

        // Simple integer hash for deterministic pseudo-random values
        private static int Hash(int value)
        {
            value = ((value >> 16) ^ value) * 0x45d9f3b;
            value = ((value >> 16) ^ value) * 0x45d9f3b;
            value = (value >> 16) ^ value;
            return Math.Abs(value);
        }

        // Simple Perlin-like noise using interpolated gradient
        private static float PerlinNoise(float x, float y)
        {
            int xi = (int)Mathf.Floor(x);
            int yi = (int)Mathf.Floor(y);
            float xf = x - xi;
            float yf = y - yi;

            float u = xf * xf * (3f - 2f * xf);
            float v = yf * yf * (3f - 2f * yf);

            float n00 = GradientDot(xi, yi, xf, yf);
            float n10 = GradientDot(xi + 1, yi, xf - 1f, yf);
            float n01 = GradientDot(xi, yi + 1, xf, yf - 1f);
            float n11 = GradientDot(xi + 1, yi + 1, xf - 1f, yf - 1f);

            float nx0 = Mathf.Lerp(n00, n10, u);
            float nx1 = Mathf.Lerp(n01, n11, u);
            return (Mathf.Lerp(nx0, nx1, v) + 1f) * 0.5f; // normalize to [0,1]
        }

        private static float GradientDot(int ix, int iy, float dx, float dy)
        {
            int h = Hash(ix + Hash(iy * 127)) & 3;
            switch (h)
            {
                case 0: return dx + dy;
                case 1: return -dx + dy;
                case 2: return dx - dy;
                default: return -dx - dy;
            }
        }

        private static void FillCircle(Color32[] pixels, int textureSize, int centerX, int centerY, int radius, Color32 color)
        {
            int radiusSquared = radius * radius;
            int minY = Math.Max(0, centerY - radius);
            int maxY = Math.Min(textureSize - 1, centerY + radius);
            int minX = Math.Max(0, centerX - radius);
            int maxX = Math.Min(textureSize - 1, centerX + radius);

            for (int y = minY; y <= maxY; y++)
            {
                int deltaY = y - centerY;
                int rowOffset = y * textureSize;
                for (int x = minX; x <= maxX; x++)
                {
                    int deltaX = x - centerX;
                    if ((deltaX * deltaX) + (deltaY * deltaY) <= radiusSquared)
                    {
                        pixels[rowOffset + x] = color;
                    }
                }
            }
        }

        private static void FillRect(Color32[] pixels, int textureSize, int startX, int startY, int width, int height, Color32 color)
        {
            int clampedStartX = Math.Max(0, startX);
            int clampedStartY = Math.Max(0, startY);
            int clampedEndX = Math.Min(startX + width, textureSize);
            int clampedEndY = Math.Min(startY + height, textureSize);

            for (int y = clampedStartY; y < clampedEndY; y++)
            {
                int rowOffset = y * textureSize;
                for (int x = clampedStartX; x < clampedEndX; x++)
                {
                    pixels[rowOffset + x] = color;
                }
            }
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            string projectRelativePath = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, projectRelativePath);
        }
    }
}