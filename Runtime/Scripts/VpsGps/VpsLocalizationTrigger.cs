/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using MultiSet;
using TMPro;
using UnityEngine;

namespace MultiSet
{
    public class VpsLocalizationTrigger : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The map mesh GameObject - used in Editor to calculate bounds (can be EditorOnly)")]
        public GameObject mapMesh;

        [Tooltip("The AR Camera GameObject (should have a SphereCollider with 'Is Trigger' enabled)")]
        public GameObject arCamera;

        [Header("Trigger Zone (Auto-Created)")]
        [Tooltip("Separate GameObject with BoxCollider for trigger detection (persists in build)")]
        public GameObject triggerZone;

        [Header("Box Collider Settings")]
        [Tooltip("Auto-fit BoxCollider to mesh bounds (recommended)")]
        public bool autoFitBoxCollider = true;

        [Tooltip("Include all child meshes when calculating bounds")]
        public bool includeChildMeshes = true;

        [Tooltip("Extra padding to add to BoxCollider size (meters)")]
        public float boundsPadding = 2f;

        private Vector3 boxColliderSize = new Vector3(10f, 5f, 10f);
        private Vector3 boxColliderCenter = Vector3.zero;

        private BoxCollider triggerZoneCollider;
        private SphereCollider arCameraSphereCollider;
        private ARCameraTriggerDetector arCameraTriggerDetector;
        private bool runtimeDetectionReady;
        private bool hasInitializedState;

        [Header("Stored Bounds (For Build)")]
        [HideInInspector]
        [Tooltip("Stored local position of trigger zone center relative to parent (calculated from mesh in Editor)")]
        public Vector3 storedLocalPosition;

        [Tooltip("Stored size of trigger zone (calculated from mesh in Editor)")]
        [HideInInspector]
        public Vector3 storedSize = new Vector3(10f, 5f, 10f);

        [Tooltip("Cooldown time between localization triggers (seconds)")]
        public float localizationCooldown = 5f;

        public TMP_Text currentPositionText; // Reference to the TMP text for current position

        private bool isInsideMapMesh = false;
        private float lastLocalizationTime = 0f;

        void Awake()
        {
            // disable the trigger zone at start;
            if (triggerZone != null)
            {
                triggerZoneCollider = triggerZone.GetComponent<BoxCollider>();
                triggerZone.SetActive(false);
            }

            runtimeDetectionReady = triggerZoneCollider != null && arCamera != null;
            UpdateButtonStates(false);
        }

        private void Start()
        {
            // Validate AR Camera reference
            if (arCamera == null)
            {
                Debug.LogError("VpsLocalizationTrigger: arCamera is not assigned!");
                return;
            }
        }

        public void SetupMeshColliderAndCameraCollider() // call this function after first Gps localization
        {
            if (arCamera == null)
            {
                Debug.LogError("VpsLocalizationTrigger: arCamera is not assigned!");
                return;
            }

            // Create or validate trigger zone GameObject
            ValidateTriggerZone();

            // Setup AR camera collider and trigger detector
            ValidateARCameraCollider();

            // Enable the trigger zone for detection
            if (triggerZone != null)
            {
                triggerZone.SetActive(true);
                Debug.Log("VpsLocalizationTrigger: Trigger zone enabled for detection");
            }

            runtimeDetectionReady = triggerZoneCollider != null && arCamera != null;
            hasInitializedState = false;

            if (runtimeDetectionReady)
            {
                CheckCameraInsideTriggerZone("Setup");
            }
        }

        private void ValidateTriggerZone()
        {
            // Find or create trigger zone GameObject
            if (triggerZone == null)
            {
                // Try to find existing trigger zone as child
                Transform existingTrigger = transform.Find("MapTriggerZone");
                if (existingTrigger != null)
                {
                    triggerZone = existingTrigger.gameObject;
                    Debug.Log("VpsLocalizationTrigger: Found existing trigger zone");
                }
                else
                {
                    // Create new trigger zone
                    triggerZone = new GameObject("MapTriggerZone");
                    triggerZone.transform.SetParent(this.transform);
                    Debug.Log("VpsLocalizationTrigger: Created new trigger zone GameObject");
                }
            }

            // Get or add BoxCollider
            BoxCollider boxCollider = triggerZone.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = triggerZone.AddComponent<BoxCollider>();
                Debug.Log("VpsLocalizationTrigger: Added BoxCollider to trigger zone");
            }

            triggerZoneCollider = boxCollider;

            // Set as trigger
            boxCollider.isTrigger = true;

            // In Editor: Calculate bounds from mapMesh if available
#if UNITY_EDITOR
            if (mapMesh != null && autoFitBoxCollider)
            {
                FitBoxColliderToMesh(boxCollider);

                // Store the LOCAL position and size for runtime (so it moves with parent MapSpace)
                storedLocalPosition = triggerZone.transform.localPosition;
                storedSize = boxCollider.size;

                Debug.Log($"VpsLocalizationTrigger: Calculated and stored bounds - Local Position: {storedLocalPosition}, Size: {storedSize}");
            }
            else if (!autoFitBoxCollider)
            {
                // Use manual settings
                triggerZone.transform.localPosition = storedLocalPosition;
                boxCollider.size = boxColliderSize;
                boxCollider.center = boxColliderCenter;
                storedSize = boxColliderSize;
                Debug.Log($"VpsLocalizationTrigger: Using manual BoxCollider size: {boxColliderSize}");
            }
#else
        // In Build: Use stored values (mapMesh may not exist)
        // Use LOCAL position so trigger zone moves with parent MapSpace after GPS localization
        triggerZone.transform.localPosition = storedLocalPosition;
        boxCollider.size = storedSize;
        boxCollider.center = Vector3.zero;
        Debug.Log($"VpsLocalizationTrigger: Using stored bounds in build - Local Position: {storedLocalPosition}, Size: {storedSize}");
#endif

            runtimeDetectionReady = triggerZoneCollider != null && arCamera != null;
        }


        private void FitBoxColliderToMesh(BoxCollider boxCollider)
        {
            if (mapMesh == null)
            {
                Debug.LogWarning("VpsLocalizationTrigger: mapMesh is null, cannot calculate bounds");
                return;
            }

            Bounds? combinedBounds = CalculateCombinedBounds();

            if (combinedBounds.HasValue)
            {
                Bounds bounds = combinedBounds.Value;

                // Position trigger zone at world bounds center
                triggerZone.transform.position = bounds.center;

                // Add padding to size
                Vector3 paddedSize = bounds.size + new Vector3(boundsPadding * 2, boundsPadding * 2, boundsPadding * 2);

                // Set BoxCollider size (center at 0,0,0 since we moved the GameObject)
                boxCollider.center = Vector3.zero;
                boxCollider.size = paddedSize;

                Debug.Log($"VpsLocalizationTrigger: Fitted BoxCollider - Center: {bounds.center}, Size: {paddedSize}");
            }
            else
            {
                Debug.LogWarning("VpsLocalizationTrigger: Could not calculate bounds. Using manual settings.");
                triggerZone.transform.localPosition = storedLocalPosition;
                boxCollider.size = boxColliderSize;
                boxCollider.center = boxColliderCenter;
            }
        }

        /// Calculate combined bounds of all meshes in the map mesh hierarchy
        private Bounds? CalculateCombinedBounds()
        {
            if (mapMesh == null)
                return null;

            Bounds? combinedBounds = null;

            if (includeChildMeshes)
            {
                // Get all renderers in children (includes parent)
                Renderer[] renderers = mapMesh.GetComponentsInChildren<Renderer>();

                if (renderers.Length > 0)
                {
                    Debug.Log($"VpsLocalizationTrigger: Found {renderers.Length} renderers in map mesh hierarchy");

                    foreach (Renderer renderer in renderers)
                    {
                        if (combinedBounds.HasValue)
                        {
                            Bounds temp = combinedBounds.Value;
                            temp.Encapsulate(renderer.bounds);
                            combinedBounds = temp;
                        }
                        else
                        {
                            combinedBounds = renderer.bounds;
                        }
                    }
                    return combinedBounds;
                }

                // Fallback: Try MeshFilters in children
                MeshFilter[] meshFilters = mapMesh.GetComponentsInChildren<MeshFilter>();
                if (meshFilters.Length > 0)
                {
                    Debug.Log($"VpsLocalizationTrigger: Found {meshFilters.Length} mesh filters in map mesh hierarchy");

                    foreach (MeshFilter meshFilter in meshFilters)
                    {
                        if (meshFilter.sharedMesh != null)
                        {
                            Bounds worldBounds = TransformBounds(meshFilter.sharedMesh.bounds, meshFilter.transform);

                            if (combinedBounds.HasValue)
                            {
                                Bounds temp = combinedBounds.Value;
                                temp.Encapsulate(worldBounds);
                                combinedBounds = temp;
                            }
                            else
                            {
                                combinedBounds = worldBounds;
                            }
                        }
                    }
                    return combinedBounds;
                }
            }
            else
            {
                // Only use the parent mesh
                Renderer renderer = mapMesh.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Debug.Log("VpsLocalizationTrigger: Using parent renderer bounds only");
                    return renderer.bounds;
                }

                MeshFilter meshFilter = mapMesh.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Debug.Log("VpsLocalizationTrigger: Using parent mesh filter bounds only");
                    return TransformBounds(meshFilter.sharedMesh.bounds, meshFilter.transform);
                }
            }

            return null;
        }

        /// Transform local bounds to world space
        private Bounds TransformBounds(Bounds localBounds, Transform transform)
        {
            Vector3 center = transform.TransformPoint(localBounds.center);

            // Transform all 8 corners of the bounds to find the world-space bounding box
            Vector3[] corners = new Vector3[8];
            Vector3 ext = localBounds.extents;
            corners[0] = transform.TransformPoint(localBounds.center + new Vector3(-ext.x, -ext.y, -ext.z));
            corners[1] = transform.TransformPoint(localBounds.center + new Vector3(-ext.x, -ext.y, ext.z));
            corners[2] = transform.TransformPoint(localBounds.center + new Vector3(-ext.x, ext.y, -ext.z));
            corners[3] = transform.TransformPoint(localBounds.center + new Vector3(-ext.x, ext.y, ext.z));
            corners[4] = transform.TransformPoint(localBounds.center + new Vector3(ext.x, -ext.y, -ext.z));
            corners[5] = transform.TransformPoint(localBounds.center + new Vector3(ext.x, -ext.y, ext.z));
            corners[6] = transform.TransformPoint(localBounds.center + new Vector3(ext.x, ext.y, -ext.z));
            corners[7] = transform.TransformPoint(localBounds.center + new Vector3(ext.x, ext.y, ext.z));

            // Find min and max of all corners
            Vector3 min = corners[0];
            Vector3 max = corners[0];

            for (int i = 1; i < 8; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }

            Bounds worldBounds = new Bounds();
            worldBounds.SetMinMax(min, max);
            return worldBounds;
        }

        private void ValidateARCameraCollider()
        {
            SphereCollider sphereCollider = arCamera.GetComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                Debug.Log("VpsLocalizationTrigger: Adding SphereCollider to arCamera");
                sphereCollider = arCamera.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.5f; // Default radius
            }

            if (!sphereCollider.isTrigger)
            {
                Debug.Log("VpsLocalizationTrigger: Setting arCamera SphereCollider as trigger");
                sphereCollider.isTrigger = true;
            }

            arCameraSphereCollider = sphereCollider;

            // Ensure arCamera has a Rigidbody (required for trigger detection)
            Rigidbody rb = arCamera.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.Log("VpsLocalizationTrigger: Adding Rigidbody to arCamera");
                rb = arCamera.AddComponent<Rigidbody>();
            }

            // Configure rigidbody for trigger detection without physics
            rb.isKinematic = true;
            rb.useGravity = false;

            // Add ARCameraTriggerDetector component to receive trigger events
            ARCameraTriggerDetector triggerDetector = arCamera.GetComponent<ARCameraTriggerDetector>();
            if (triggerDetector == null)
            {
                Debug.Log("VpsLocalizationTrigger: Adding ARCameraTriggerDetector to arCamera");
                triggerDetector = arCamera.AddComponent<ARCameraTriggerDetector>();
            }

            // Set reference back to this script
            triggerDetector.localizationTrigger = this;
            arCameraTriggerDetector = triggerDetector;
        }

        /// <summary>
        /// Called by ARCameraTriggerDetector when AR Camera enters a trigger
        /// </summary>
        public void OnARCameraEnteredTrigger(Collider other)
        {
            if (!IsTriggerZoneCollider(other))
            {
                Debug.Log($"VpsLocalizationTrigger: AR Camera triggered with non-trigger-zone object: {other.gameObject.name}");
                return;
            }

            Debug.Log("✓ VpsLocalizationTrigger: AR Camera ENTERED map mesh area via physics trigger");
            HandleInsideStateChange(true, "PhysicsTrigger");
        }

        /// <summary>
        /// Called by ARCameraTriggerDetector when AR Camera exits a trigger
        /// </summary>
        public void OnARCameraExitedTrigger(Collider other)
        {
            if (!IsTriggerZoneCollider(other))
            {
                Debug.Log($"VpsLocalizationTrigger: AR Camera trigger exit with non-trigger-zone object: {other.gameObject.name}");
                return;
            }

            Debug.Log("✓ VpsLocalizationTrigger: AR Camera EXITED map mesh area via physics trigger");
            HandleInsideStateChange(false, "PhysicsTrigger");
        }

        /// <summary>
        /// Trigger VPS localization if conditions are met
        /// </summary>
        public void TriggerVpsLocalization()
        {
            // Check cooldown
            if (Time.time - lastLocalizationTime < localizationCooldown)
            {
                Debug.Log($"VpsLocalizationTrigger: Localization on cooldown. Wait {localizationCooldown - (Time.time - lastLocalizationTime):F1}s");
                return;
            }

            lastLocalizationTime = Time.time;
        }

        /// <summary>
        /// Manually refresh the BoxCollider to fit the mesh bounds (useful for testing)
        /// </summary>
        [ContextMenu("Refresh BoxCollider")]
        public void RefreshBoxCollider()
        {
#if UNITY_EDITOR
            if (mapMesh != null)
            {
                ValidateTriggerZone();
                Debug.Log("VpsLocalizationTrigger: BoxCollider refreshed and bounds stored");
            }
            else
            {
                Debug.LogWarning("VpsLocalizationTrigger: Cannot refresh - mapMesh is null");
            }
#else
        Debug.Log("VpsLocalizationTrigger: Refresh BoxCollider only works in Editor");
#endif
        }

        /// <summary>
        /// Update button states based on whether inside or outside map mesh
        /// </summary>
        /// <param name="insideMapMesh">True if inside map mesh, false if outside</param>
        private void UpdateButtonStates(bool insideMapMesh)
        {
            if (currentPositionText != null)
            {
                currentPositionText.text = insideMapMesh ? "Near VPS Map" : "Away from VPS Map";
            }
        }

        private void Update()
        {
            if (!runtimeDetectionReady)
                return;

            if (triggerZone == null || triggerZoneCollider == null || arCamera == null)
                return;

            if (!triggerZone.activeInHierarchy)
            {
                hasInitializedState = false;
                return;
            }

            CheckCameraInsideTriggerZone("Update");
        }

        private void CheckCameraInsideTriggerZone(string source)
        {
            if (triggerZoneCollider == null || arCamera == null)
                return;

            if (triggerZone != null && !triggerZone.activeInHierarchy)
            {
                hasInitializedState = false;
                return;
            }

            bool inside = IsPointInsideTriggerZone(triggerZoneCollider, arCamera.transform.position);
            if (!hasInitializedState || inside != isInsideMapMesh)
            {
                HandleInsideStateChange(inside, source);
            }

            hasInitializedState = true;
        }

        private bool IsPointInsideTriggerZone(BoxCollider boxCollider, Vector3 worldPoint)
        {
            if (boxCollider.bounds.Contains(worldPoint))
                return true;

            Vector3 closestPoint = boxCollider.ClosestPoint(worldPoint);
            return (closestPoint - worldPoint).sqrMagnitude <= 0.0001f;
        }

        private bool IsTriggerZoneCollider(Collider other)
        {
            return triggerZoneCollider != null && other == triggerZoneCollider;
        }

        private void HandleInsideStateChange(bool inside, string source)
        {
            isInsideMapMesh = inside;

            string stateLabel = inside ? "ENTERED" : "EXITED";
            Debug.Log($"VpsLocalizationTrigger: {stateLabel} map mesh area ({source})");

            UpdateButtonStates(inside);
        }

        private void OnDrawGizmos()
        {
            // Visualize the AR camera trigger sphere
            if (arCamera != null)
            {
                Gizmos.color = isInsideMapMesh ? Color.green : Color.yellow;
                SphereCollider sphereCollider = arCamera.GetComponent<SphereCollider>();
                if (sphereCollider != null)
                {
                    Gizmos.DrawWireSphere(arCamera.transform.position, sphereCollider.radius);
                }
            }

            // Visualize the trigger zone BoxCollider
            if (triggerZone != null)
            {
                BoxCollider boxCollider = triggerZone.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    Gizmos.color = isInsideMapMesh ? new Color(0, 1, 0, 0.3f) : new Color(1, 1, 0, 0.3f);
                    Matrix4x4 oldMatrix = Gizmos.matrix;
                    Gizmos.matrix = triggerZone.transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                    Gizmos.matrix = oldMatrix;
                }
            }
            // Fallback: Visualize stored bounds if trigger zone doesn't exist yet
            else if (storedSize != Vector3.zero)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Orange for stored bounds
                Vector3 worldPos = transform.TransformPoint(storedLocalPosition);
                Gizmos.DrawWireCube(worldPos, storedSize);
            }
        }
    }
}
