/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEngine.UI;

namespace MultiSet.Samples
{
    // A heading-up map of the floor the non-LiDAR scan has covered, centred on the user. Each
    // half-metre cell turns from amber to green as it is captured from more directions; the line is
    // the path walked and the ring marks the map origin, where the scan started.
    [RequireComponent(typeof(RawImage))]
    public class CoverageMiniMap : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Manager Reference")]
        [SerializeField] private NonLiDARMappingManager mappingManager;

        [Header("Map")]
        [SerializeField] private float metersAcross = 12f;
        [SerializeField] private int resolution = 192;
        [SerializeField] private float refreshInterval = 0.2f;
        [SerializeField] private GameObject legend;

        [Header("Colours")]
        [SerializeField] private Color background = new(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color fewDirections = new(0.95f, 0.62f, 0.07f, 0.85f);
        [SerializeField] private Color someDirections = new(0.77f, 0.84f, 0.24f, 0.88f);
        [SerializeField] private Color manyDirections = new(0.18f, 0.67f, 0.35f, 0.92f);
        [SerializeField] private Color pathColor = new(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color markerColor = Color.white;
        #endregion

        #region Private Fields
        private const int ManyHeadings = 5;
        private const int SomeHeadings = 3;

        private RawImage image;
        private Texture2D texture;
        private Color32[] pixels;
        private Color32 backgroundPixel, fewPixel, somePixel, manyPixel, pathPixel, markerPixel;
        private readonly Color32 clearPixel = new(0, 0, 0, 0);
        private readonly Color32 outlinePixel = new(0, 0, 0, 200);
        private bool visible;
        private float nextRefresh;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            image = GetComponent<RawImage>();
            resolution = Mathf.Clamp(resolution, 64, 512);
            texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "CoverageMiniMap",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            pixels = new Color32[resolution * resolution];
            image.texture = texture;

            backgroundPixel = background;
            fewPixel = fewDirections;
            somePixel = someDirections;
            manyPixel = manyDirections;
            pathPixel = pathColor;
            markerPixel = markerColor;
        }

        private void OnEnable()
        {
            if (mappingManager == null)
            {
                mappingManager = FindFirstObjectByType<NonLiDARMappingManager>();
            }

            if (mappingManager != null)
            {
                mappingManager.OnStateChanged += HandleStateChanged;
                SetVisible(mappingManager.IsRecording);
            }
            else
            {
                SetVisible(false);
            }
        }

        private void OnDisable()
        {
            if (mappingManager != null)
            {
                mappingManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }

        private void Update()
        {
            if (!visible || mappingManager == null || Time.unscaledTime < nextRefresh)
            {
                return;
            }

            nextRefresh = Time.unscaledTime + refreshInterval;
            Redraw();
        }
        #endregion

        #region Event Handlers
        private void HandleStateChanged(object sender, MappingStateChangedEventArgs e)
        {
            SetVisible(e.CurrentState == MappingState.Mapping);
        }
        #endregion

        #region Drawing
        private void SetVisible(bool show)
        {
            visible = show;
            image.enabled = show;
            if (legend != null)
            {
                legend.SetActive(show);
            }
            if (show)
            {
                nextRefresh = 0f;
            }
        }

        private void Redraw()
        {
            Vector3 centre = mappingManager.CameraPosition;
            float heading = CoverageEstimator.HeadingDegrees(mappingManager.CameraRotation) * Mathf.Deg2Rad;
            float sin = Mathf.Sin(heading);
            float cos = Mathf.Cos(heading);
            float metresPerPixel = metersAcross / resolution;
            float half = resolution * 0.5f;
            float radius = half - 1f;
            float cellSize = NonLiDARLimits.CoverageCellSize;

            // Heading-up: screen right is the camera's right on the floor, screen up is where it faces.
            int lastX = int.MinValue, lastZ = int.MinValue;
            Color32 lastCell = backgroundPixel;
            for (int py = 0; py < resolution; py++)
            {
                float sy = py + 0.5f - half;
                for (int px = 0; px < resolution; px++)
                {
                    float sx = px + 0.5f - half;
                    float distance = Mathf.Sqrt(sx * sx + sy * sy);
                    int index = py * resolution + px;
                    if (distance > radius)
                    {
                        pixels[index] = clearPixel;
                        continue;
                    }
                    if (distance > radius - 2f)
                    {
                        pixels[index] = markerPixel;
                        continue;
                    }

                    float mx = sx * metresPerPixel;
                    float my = sy * metresPerPixel;
                    int cellX = Mathf.FloorToInt((centre.x + mx * cos + my * sin) / cellSize);
                    int cellZ = Mathf.FloorToInt((centre.z - mx * sin + my * cos) / cellSize);
                    if (cellX != lastX || cellZ != lastZ)
                    {
                        lastX = cellX;
                        lastZ = cellZ;
                        lastCell = mappingManager.TryGetCoverageCell(cellX, cellZ, out CoverageCell cell)
                            ? ColourFor(cell.HeadingCount)
                            : backgroundPixel;
                    }
                    pixels[index] = lastCell;
                }
            }

            var path = mappingManager.CapturePath;
            for (int i = 1; i < path.Count; i++)
            {
                DrawLine(ToPixel(path[i - 1], centre, sin, cos, metresPerPixel, half),
                    ToPixel(path[i], centre, sin, cos, metresPerPixel, half), radius - 3f, half);
            }

            DrawRing(ToPixel(Vector3.zero, centre, sin, cos, metresPerPixel, half), 5f, radius - 3f, half);
            DrawPlayer(half);

            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        private Color32 ColourFor(int headings)
        {
            return headings >= ManyHeadings ? manyPixel : headings >= SomeHeadings ? somePixel : fewPixel;
        }

        private static Vector2 ToPixel(Vector3 world, Vector3 centre, float sin, float cos, float metresPerPixel, float half)
        {
            float dx = world.x - centre.x;
            float dz = world.z - centre.z;
            return new Vector2((dx * cos - dz * sin) / metresPerPixel + half, (dx * sin + dz * cos) / metresPerPixel + half);
        }

        private void DrawLine(Vector2 from, Vector2 to, float clipRadius, float half)
        {
            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y)));
            if (steps > resolution * 2)
            {
                return;
            }
            for (int s = 0; s <= steps; s++)
            {
                Vector2 point = steps == 0 ? from : Vector2.Lerp(from, to, (float)s / steps);
                Plot((int)point.x, (int)point.y, pathPixel, clipRadius, half);
                Plot((int)point.x + 1, (int)point.y, pathPixel, clipRadius, half);
            }
        }

        private void DrawRing(Vector2 centre, float ringRadius, float clipRadius, float half)
        {
            for (int angle = 0; angle < 48; angle++)
            {
                float a = angle * Mathf.PI * 2f / 48f;
                Plot((int)(centre.x + Mathf.Cos(a) * ringRadius), (int)(centre.y + Mathf.Sin(a) * ringRadius), markerPixel, clipRadius, half);
                Plot((int)(centre.x + Mathf.Cos(a) * (ringRadius - 1f)), (int)(centre.y + Mathf.Sin(a) * (ringRadius - 1f)), markerPixel, clipRadius, half);
            }
        }

        // An arrow at the centre pointing up: the user, facing the way the camera looks.
        private void DrawPlayer(float half)
        {
            FillTriangle(new Vector2(half, half + 11f), new Vector2(half - 8f, half - 7f), new Vector2(half + 8f, half - 7f), outlinePixel);
            FillTriangle(new Vector2(half, half + 8f), new Vector2(half - 5.5f, half - 5f), new Vector2(half + 5.5f, half - 5f), markerPixel);
        }

        private void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color32 colour)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
                    bool hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
                    bool hasPositive = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(hasNegative && hasPositive) && x >= 0 && y >= 0 && x < resolution && y < resolution)
                    {
                        pixels[y * resolution + x] = colour;
                    }
                }
            }
        }

        private static float Cross(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

        private void Plot(int x, int y, Color32 colour, float clipRadius, float half)
        {
            float dx = x + 0.5f - half, dy = y + 0.5f - half;
            if (dx * dx + dy * dy > clipRadius * clipRadius || x < 0 || y < 0 || x >= resolution || y >= resolution)
            {
                return;
            }
            pixels[y * resolution + x] = colour;
        }
        #endregion
    }
}
