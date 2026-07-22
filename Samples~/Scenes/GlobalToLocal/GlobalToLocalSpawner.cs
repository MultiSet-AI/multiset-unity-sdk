using UnityEngine;
using MultiSet;
namespace MultiSet
{
    public class GlobalToLocalSpawner : MonoBehaviour
    {
        // Name of the child container created under mapSpace to hold spawned points.
        const string ContainerName = "GPS Points";

        [Header("Map Frame")]
        [Tooltip("Parent frame representing the map origin. Spawned points are placed under it.")]
        public GameObject mapSpace;

        [Header("Map Source")]
        [HideInInspector]
        [Tooltip("Optional map code override. If left empty, the code is auto-discovered from the " +
                 "scene's localization target (e.g. MapLocalizationManager).")]
        public string mapCode;

        [Header("Input")]
        [Tooltip("JSON TextAsset with a top-level 'gpsPoints' array of {latitude, longitude, altitude, name}.")]
        public TextAsset gpsJson;

        [Header("Point Visuals")]
        [Tooltip("Diameter of each spawned sphere in meters.")]
        public float pointDiameter = 0.3f;
        [Tooltip("Tint applied to spheres when no custom material is assigned.")]
        public Color pointColor = Color.red;
        [Tooltip("Optional material for the spheres. If empty, a single tinted material is created for the batch.")]
        public Material pointMaterial;

        [Header("Options")]
        [Tooltip("Re-center all points onto the map origin's horizontal plane, preserving the relative " +
                 "height differences between points (subtracts the average Y). Corrects the vertical " +
                 "datum offset — the map origin altitude is WGS-84 ellipsoidal while phone GPS altitude " +
                 "is usually MSL, which otherwise pushes every point off by the geoid height. " +
                 "When off, the raw altitude difference from the origin is used.")]
        public bool alignToOriginPlane = true;

        [Tooltip("Hard-flatten every point to Y = 0. Overrides 'Align To Origin Plane'. " +
                 "Use when GPS altitude should be ignored entirely.")]
        public bool flattenToGround = false;

        // ── Map origin fetched from the API (serialized so it persists across recompiles / scene saves) ──
        [HideInInspector] public bool hasGeoReference;
        [HideInInspector] public string resolvedMapCode;
        [HideInInspector] public string mapName;
        [HideInInspector] public double originLatitude;
        [HideInInspector] public double originLongitude;
        [HideInInspector] public double originAltitude;
        [HideInInspector] public double headingDeg;

        // Logic engine (SDK DLL). Non-serialized; recreated on demand.
        GlobalToLocalConverter _converter;
        GlobalToLocalConverter Converter => _converter ??= new GlobalToLocalConverter();

        /// <summary>
        /// Fetches the map's geo-reference via the SDK and stores it. Result is reported on
        /// <paramref name="onComplete"/> as (success, message) for the editor to surface.
        /// </summary>
        public void FetchMapOrigin(System.Action<bool, string> onComplete)
        {
            if (mapSpace == null)
            {
                const string msg = "mapSpace is not assigned.";
                Debug.LogError($"[GlobalToLocalSpawner] {msg}");
                onComplete?.Invoke(false, msg);
                return;
            }

            Converter.FetchMapOrigin(mapCode, (success, message, origin) =>
            {
                hasGeoReference = success && origin.isGeoReferenced;
                if (hasGeoReference)
                {
                    resolvedMapCode = origin.mapCode;
                    mapName = origin.mapName;
                    originLatitude = origin.latitude;
                    originLongitude = origin.longitude;
                    originAltitude = origin.altitude;
                    headingDeg = origin.heading;
                    MarkSceneDirty();
                }
                onComplete?.Invoke(success, message);
            });
        }

        /// <summary>
        /// Converts the assigned JSON via the SDK and spawns a sphere per point under mapSpace.
        /// Returns the number spawned; on failure returns 0 and sets <paramref name="error"/>.
        /// </summary>
        public int SpawnPoints(out string error)
        {
            error = "";

            if (mapSpace == null) { error = "mapSpace is not assigned."; Debug.LogError($"[GlobalToLocalSpawner] {error}"); return 0; }
            if (gpsJson == null) { error = "gpsJson TextAsset is not assigned."; Debug.LogError($"[GlobalToLocalSpawner] {error}"); return 0; }
            if (!hasGeoReference) { error = "Fetch a geo-referenced map origin first (use 'Fetch Map Origin')."; return 0; }

            GlobalToLocalConverter.ConvertedPoint[] points = Converter.Convert(
                gpsJson.text, originLatitude, originLongitude, originAltitude, headingDeg,
                alignToOriginPlane, flattenToGround, out error);

            if (points == null || points.Length == 0)
            {
                if (!string.IsNullOrEmpty(error)) Debug.LogError($"[GlobalToLocalSpawner] {error}");
                return 0;
            }

            // Start from a clean container so re-running does not accumulate duplicates.
            ClearPoints();
            Transform container = GetOrCreateContainer();

            Material batchMaterial = pointMaterial;

            foreach (GlobalToLocalConverter.ConvertedPoint pt in points)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = pt.name;

                // Markers only - drop the auto-added collider so points don't block raycasts.
                if (sphere.TryGetComponent(out Collider col)) DestroyImmediate(col);

                Renderer renderer = sphere.GetComponent<Renderer>();
                if (batchMaterial == null)
                {
                    // Clone the primitive's default material (keeps the active render pipeline shader)
                    // and tint it once for the whole batch.
                    batchMaterial = new Material(renderer.sharedMaterial) { name = "GPS Point Material", color = pointColor };
                }
                renderer.sharedMaterial = batchMaterial;

                sphere.transform.SetParent(container, false);
                sphere.transform.SetLocalPositionAndRotation(pt.localPosition, Quaternion.identity);
                sphere.transform.localScale = Vector3.one * pointDiameter;

#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCreatedObjectUndo(sphere, "Spawn GPS Point");
#endif
            }

            Debug.Log($"[GlobalToLocalSpawner] Spawned {points.Length} GPS point(s) under '{ContainerName}'.");
            MarkSceneDirty();
            return points.Length;
        }

        /// <summary>Destroys the spawned points container (and everything under it), if present.</summary>
        public void ClearPoints()
        {
            if (mapSpace == null) return;

            Transform existing = mapSpace.transform.Find(ContainerName);
            if (existing == null) return;

#if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(existing.gameObject);
#else
        DestroyImmediate(existing.gameObject);
#endif
            MarkSceneDirty();
        }

        Transform GetOrCreateContainer()
        {
            Transform existing = mapSpace.transform.Find(ContainerName);
            if (existing != null) return existing;

            GameObject container = new GameObject(ContainerName);
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(container, "Create GPS Points Container");
#endif
            container.transform.SetParent(mapSpace.transform, false);
            container.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            container.transform.localScale = Vector3.one;
            return container.transform;
        }

        void MarkSceneDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }
    }
}