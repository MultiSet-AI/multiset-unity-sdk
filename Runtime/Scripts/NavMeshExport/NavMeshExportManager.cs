/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Generates a grid of navigation waypoints on the NavMesh and pre-computes
/// paths from every waypoint to every POI. This data is exported for use
/// on Meta Ray-Ban glasses where runtime NavMesh is not available.
/// Combines functionality from DummyMapData - exports bounds, POIs, waypoints, and paths.
/// </summary>
public class NavMeshExportManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The map mesh containing the NavMesh")]
    public GameObject mapMesh;

    [Tooltip("Parent containing all POIs")]
    public GameObject poisParent;

    [Header("Grid Settings")]
    [Tooltip("Distance between waypoints in meters")]
    [Range(0.5f, 3f)]
    public float waypointSpacing = 1.0f;

    [Tooltip("Height above NavMesh to sample")]
    public float sampleHeight = 2f;

    [Tooltip("Max distance to find NavMesh point from sample position")]
    [Range(0.5f, 5f)]
    public float navMeshSampleDistance = 2f;

    [HideInInspector]
    public string mapCodeOverride = "";

    /// <summary>
    /// Returns the effective map code (override if set, otherwise mapMesh name)
    /// </summary>
    public string MapCode => string.IsNullOrWhiteSpace(mapCodeOverride) && mapMesh != null
        ? mapMesh.name
        : mapCodeOverride;

    [Header("Export Settings")]
    public string exportDirectory = "MultiSet/Scenes/Meta-Map-Navigation/ExportedData";

    [Header("Advanced Settings")]
    [Tooltip("Try sampling from multiple heights to find NavMesh on slopes or multi-level areas")]
    public bool multiHeightSampling = true;

    [Tooltip("Number of vertical samples to try when multiHeightSampling is enabled")]
    [Range(1, 5)]
    public int verticalSamples = 3;

    [Header("Debug Visualization")]
    public bool showWaypointsInScene = true;
    public Color waypointColor = Color.cyan;
    public float waypointGizmoSize = 0.15f;

    [Tooltip("Show sample points where NavMesh was not found (red X marks)")]
    public bool showFailedSamples = false;

    // Generated data
    [SerializeField] private List<Waypoint> generatedWaypoints = new();
    [SerializeField] private List<POIData> poiDestinations = new();
    [SerializeField] private MapBounds mapBounds;

    // Debug data - sample points where NavMesh wasn't found
    private List<Vector3> failedSamplePoints = new();

    // Lookup dictionaries for O(1) access (not serialized)
    private Dictionary<int, Waypoint> waypointLookup = new();
    private Dictionary<(int, int), List<Waypoint>> spatialHash = new();

    #region Data Structures

    [System.Serializable]
    public class Waypoint
    {
        public int id;
        public Vector3 position;
        public List<int> connectedWaypoints; // IDs of reachable neighbors

        public Waypoint(int id, Vector3 position)
        {
            this.id = id;
            this.position = position;
            this.connectedWaypoints = new List<int>();
        }
    }

    /// <summary>
    /// Extended POI data including local/world positions and description
    /// </summary>
    [System.Serializable]
    public class POIData
    {
        public int id;
        public string name;
        public string description;
        public string type;
        public Vector3 position;        // Local position relative to map
        public Vector3 worldPosition;   // World position (for reference)
        public int nearestWaypointId;
        public float arrivalRadius;     // For arrival detection
    }

    /// <summary>
    /// Map bounds information
    /// </summary>
    [System.Serializable]
    public class MapBounds
    {
        public Vector3 center;
        public Vector3 size;
        public Vector3 min;
        public Vector3 max;
    }

    [System.Serializable]
    public class NavigationPath
    {
        public int fromWaypointId;
        public int toPoiId;
        public List<int> waypointPath; // Sequence of waypoint IDs to follow
        public float totalDistance;
    }

    /// <summary>
    /// Complete exported navigation data including bounds, POIs, waypoints, and paths
    /// </summary>
    [System.Serializable]
    public class PrecomputedNavigationData
    {
        public string mapCode;
        public string exportedAt;
        public float waypointSpacing;
        public MapBounds bounds;
        public List<POIData> pois;
        public List<Waypoint> waypoints;
        public List<NavigationPath> paths;
    }

    #endregion

    /// <summary>
    /// Main method: Generates waypoints and computes all paths
    /// </summary>
    public void GenerateNavigationData()
    {
        generatedWaypoints.Clear();
        poiDestinations.Clear();
        waypointLookup.Clear();
        spatialHash.Clear();
        failedSamplePoints.Clear();
        mapBounds = null;

        // Validate references
        if (mapMesh == null)
        {
            Debug.LogError("NavigationWaypointGenerator: Map mesh is not assigned!");
            return;
        }

        if (poisParent == null)
        {
            Debug.LogError("NavigationWaypointGenerator: POIs parent is not assigned!");
            return;
        }

        Debug.Log($"Using map code: {MapCode}");

        // Step 1: Calculate map bounds
        CalculateMapBounds();

        // Step 2: Generate waypoint grid on NavMesh
        GenerateWaypointGrid();

        if (generatedWaypoints.Count == 0)
        {
            Debug.LogError("NavigationWaypointGenerator: No waypoints generated. Check if NavMesh is baked.");
            return;
        }

        // Step 3: Build lookup structures for performance
        BuildWaypointLookup();
        BuildSpatialHash();

        // Step 4: Connect neighboring waypoints
        ConnectWaypoints();

        // Step 5: Extract POI data
        ExtractPOIDestinations();

        if (poiDestinations.Count == 0)
        {
            Debug.LogWarning("NavigationWaypointGenerator: No POIs found. Check POIs parent has POI components.");
        }

        Debug.Log($"Generated {generatedWaypoints.Count} waypoints and {poiDestinations.Count} POI destinations");
    }

    /// <summary>
    /// Calculates the bounds of the map mesh
    /// </summary>
    private void CalculateMapBounds()
    {
        Bounds bounds = CalculateMeshBounds();

        mapBounds = new MapBounds
        {
            center = bounds.center,
            size = bounds.size,
            min = bounds.min,
            max = bounds.max
        };

        Debug.Log($"Map bounds calculated: Center={mapBounds.center}, Size={mapBounds.size}");
    }

    /// <summary>
    /// Builds dictionary for O(1) waypoint lookup by ID
    /// </summary>
    private void BuildWaypointLookup()
    {
        waypointLookup.Clear();
        foreach (var wp in generatedWaypoints)
        {
            waypointLookup[wp.id] = wp;
        }
    }

    /// <summary>
    /// Builds spatial hash for efficient nearby waypoint queries
    /// </summary>
    private void BuildSpatialHash()
    {
        spatialHash.Clear();
        float cellSize = waypointSpacing * 2f;

        foreach (var wp in generatedWaypoints)
        {
            var cell = GetSpatialCell(wp.position, cellSize);
            if (!spatialHash.ContainsKey(cell))
            {
                spatialHash[cell] = new List<Waypoint>();
            }
            spatialHash[cell].Add(wp);
        }
    }

    private (int, int) GetSpatialCell(Vector3 position, float cellSize)
    {
        return ((int)(position.x / cellSize), (int)(position.z / cellSize));
    }

    /// <summary>
    /// Gets waypoints in nearby spatial cells for efficient neighbor search
    /// </summary>
    private IEnumerable<Waypoint> GetNearbyWaypoints(Waypoint wp)
    {
        float cellSize = waypointSpacing * 2f;
        var cell = GetSpatialCell(wp.position, cellSize);

        // Check 3x3 grid of cells around the waypoint
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                var neighborCell = (cell.Item1 + dx, cell.Item2 + dz);
                if (spatialHash.TryGetValue(neighborCell, out var list))
                {
                    foreach (var other in list)
                    {
                        yield return other;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generates a grid of waypoints on the NavMesh surface
    /// </summary>
    private void GenerateWaypointGrid()
    {
        // Clear failed samples from previous run
        failedSamplePoints.Clear();

        // Get bounds of the map
        Bounds bounds = CalculateMeshBounds();
        Debug.Log($"Map bounds: Min={bounds.min}, Max={bounds.max}, Size={bounds.size}");

        int waypointId = 0;
        int sampledPoints = 0;
        int skippedTooClose = 0;
        int navMeshMisses = 0;

        // Calculate sample distance for NavMesh queries
        float sampleRadius = navMeshSampleDistance;

        // Use a spatial grid to track occupied positions for efficient "too close" check
        // Key: discretized grid position, Value: waypoint position
        float minSeparation = waypointSpacing * 0.5f;
        var occupiedPositions = new Dictionary<(int, int, int), Vector3>();

        // Prepare height sample positions for multi-height sampling
        List<float> heightOffsets = new List<float>();
        if (multiHeightSampling && verticalSamples > 1)
        {
            float heightRange = bounds.size.y + sampleHeight * 2;
            float heightStep = heightRange / (verticalSamples - 1);
            for (int i = 0; i < verticalSamples; i++)
            {
                heightOffsets.Add(bounds.min.y + i * heightStep);
            }
        }
        else
        {
            // Single sample from above
            heightOffsets.Add(bounds.max.y + sampleHeight);
        }

        // Iterate through grid
        for (float x = bounds.min.x; x <= bounds.max.x; x += waypointSpacing)
        {
            for (float z = bounds.min.z; z <= bounds.max.z; z += waypointSpacing)
            {
                sampledPoints++;
                bool foundNavMesh = false;

                // Try sampling from different heights
                foreach (float sampleY in heightOffsets)
                {
                    Vector3 samplePoint = new Vector3(x, sampleY, z);

                    // Try to find NavMesh point within sample distance
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(samplePoint, out hit, sampleRadius, NavMesh.AllAreas))
                    {
                        foundNavMesh = true;

                        // Use spatial hash for O(1) "too close" check
                        var cellKey = GetPositionCellKey(hit.position, minSeparation);

                        bool tooClose = false;
                        // Check current cell and neighbors
                        for (int dx = -1; dx <= 1 && !tooClose; dx++)
                        {
                            for (int dz = -1; dz <= 1 && !tooClose; dz++)
                            {
                                for (int dy = -1; dy <= 1 && !tooClose; dy++)
                                {
                                    var neighborKey = (cellKey.Item1 + dx, cellKey.Item2 + dy, cellKey.Item3 + dz);
                                    if (occupiedPositions.TryGetValue(neighborKey, out Vector3 existingPos))
                                    {
                                        if (Vector3.Distance(existingPos, hit.position) < minSeparation)
                                        {
                                            tooClose = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (!tooClose)
                        {
                            generatedWaypoints.Add(new Waypoint(waypointId++, hit.position));
                            occupiedPositions[cellKey] = hit.position;
                        }
                        else
                        {
                            skippedTooClose++;
                        }

                        // Found NavMesh at this grid position, move to next
                        break;
                    }
                }

                if (!foundNavMesh)
                {
                    navMeshMisses++;
                    // Store for debug visualization
                    if (showFailedSamples)
                    {
                        failedSamplePoints.Add(new Vector3(x, bounds.center.y, z));
                    }
                }
            }
        }

        Debug.Log($"Waypoint generation: {sampledPoints} points sampled, " +
                  $"{generatedWaypoints.Count} waypoints created, " +
                  $"{skippedTooClose} skipped (too close), " +
                  $"{navMeshMisses} no NavMesh found");

        // Diagnostic: if most points miss NavMesh, warn the user
        float missRate = (float)navMeshMisses / sampledPoints;
        if (missRate > 0.5f)
        {
            Debug.LogWarning($"High NavMesh miss rate ({missRate:P0}). Possible causes:\n" +
                           $"1. NavMesh is not baked - open Navigation window and bake\n" +
                           $"2. NavMesh Agent settings don't match the mesh - check agent radius/height\n" +
                           $"3. Mesh is not marked as Navigation Static\n" +
                           $"4. Try increasing navMeshSampleDistance (currently: {navMeshSampleDistance})");
        }
        else if (missRate > 0.2f)
        {
            Debug.Log($"NavMesh miss rate: {missRate:P0} - some areas not walkable, which is normal for areas with obstacles.");
        }
    }

    /// <summary>
    /// Gets a discrete cell key for spatial hashing
    /// </summary>
    private (int, int, int) GetPositionCellKey(Vector3 position, float cellSize)
    {
        return (
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    /// <summary>
    /// Connects waypoints that can reach each other via NavMesh.
    /// Uses spatial hashing for efficient neighbor lookup.
    /// </summary>
    private void ConnectWaypoints()
    {
        float connectionDistance = waypointSpacing * 1.5f; // Allow diagonal connections
        int connectionsCreated = 0;
        int pathsChecked = 0;

        foreach (var waypoint in generatedWaypoints)
        {
            // Clear existing connections to avoid duplicates on re-generation
            waypoint.connectedWaypoints.Clear();

            // Use spatial hash for efficient nearby waypoint lookup
            foreach (var other in GetNearbyWaypoints(waypoint))
            {
                if (waypoint.id == other.id) continue;

                // Skip if already connected (bidirectional check)
                if (waypoint.connectedWaypoints.Contains(other.id)) continue;

                float distance = Vector3.Distance(waypoint.position, other.position);
                if (distance <= connectionDistance)
                {
                    pathsChecked++;

                    // Verify path exists on NavMesh
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(waypoint.position, other.position, NavMesh.AllAreas, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            // Check path length is reasonable (not going around obstacles)
                            float pathLength = CalculatePathLength(path.corners);
                            if (pathLength <= distance * 1.5f)
                            {
                                waypoint.connectedWaypoints.Add(other.id);
                                connectionsCreated++;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"Waypoint connections: {pathsChecked} paths checked, {connectionsCreated} connections created");

        // Validate connectivity - warn if there are isolated waypoints
        int isolatedCount = generatedWaypoints.Count(w => w.connectedWaypoints.Count == 0);
        if (isolatedCount > 0)
        {
            Debug.LogWarning($"Found {isolatedCount} isolated waypoints with no connections. " +
                           "This may indicate disconnected NavMesh regions.");
        }
    }

    /// <summary>
    /// Extracts POI positions and finds their nearest waypoints
    /// </summary>
    private void ExtractPOIDestinations()
    {
        if (poisParent == null)
        {
            Debug.LogError("POIs parent not assigned!");
            return;
        }

        POI[] pois = poisParent.GetComponentsInChildren<POI>(true);
        Debug.Log($"Found {pois.Length} POIs in hierarchy");

        foreach (var poi in pois)
        {
            Vector3 worldPosition = poi.poiCollider != null
                ? poi.poiCollider.transform.position
                : poi.transform.position;

            // Calculate local position relative to map mesh
            Vector3 localPosition = mapMesh != null
                ? mapMesh.transform.InverseTransformPoint(worldPosition)
                : worldPosition;

            // Find nearest waypoint
            int nearestId = FindNearestWaypoint(worldPosition);

            poiDestinations.Add(new POIData
            {
                id = poi.identification,
                name = poi.poiName,
                description = poi.description,
                type = poi.type.ToString(),
                position = localPosition,
                worldPosition = worldPosition,
                nearestWaypointId = nearestId,
                arrivalRadius = GetColliderRadius(poi)
            });
        }

        Debug.Log($"Extracted {poiDestinations.Count} POIs");
    }

    /// <summary>
    /// Pre-computes paths from every waypoint to every POI using A*
    /// </summary>
    public List<NavigationPath> ComputeAllPaths()
    {
        // Validate data exists
        if (generatedWaypoints.Count == 0)
        {
            Debug.LogError("ComputeAllPaths: No waypoints generated. Call GenerateNavigationData() first.");
            return new List<NavigationPath>();
        }

        if (poiDestinations.Count == 0)
        {
            Debug.LogError("ComputeAllPaths: No POI destinations found. Check POIs parent reference.");
            return new List<NavigationPath>();
        }

        // Ensure lookup is built
        if (waypointLookup.Count == 0)
        {
            BuildWaypointLookup();
        }

        var allPaths = new List<NavigationPath>();
        int totalOperations = poiDestinations.Count * generatedWaypoints.Count;
        int completed = 0;
        int failedPaths = 0;

        foreach (var poi in poiDestinations)
        {
            int pathsToThisPoi = 0;
            int failedToThisPoi = 0;

            // Compute shortest path from every waypoint to this POI's nearest waypoint
            foreach (var startWaypoint in generatedWaypoints)
            {
                var path = FindShortestPath(startWaypoint.id, poi.nearestWaypointId);

                if (path != null && path.Count > 0)
                {
                    allPaths.Add(new NavigationPath
                    {
                        fromWaypointId = startWaypoint.id,
                        toPoiId = poi.id,
                        waypointPath = path,
                        totalDistance = CalculatePathDistance(path)
                    });
                    pathsToThisPoi++;
                }
                else
                {
                    failedPaths++;
                    failedToThisPoi++;
                }

                // Progress feedback every 500 operations
                completed++;
                if (completed % 500 == 0)
                {
                    float progress = (completed * 100f / totalOperations);
                    Debug.Log($"Computing paths: {completed}/{totalOperations} ({progress:F1}%)");
                }
            }

            Debug.Log($"Paths to '{poi.name}': {pathsToThisPoi} computed, {failedToThisPoi} failed");

            // Warn if many paths failed to this POI
            if (failedToThisPoi > generatedWaypoints.Count * 0.1f)
            {
                Debug.LogWarning($"POI '{poi.name}' has {failedToThisPoi} unreachable waypoints. " +
                               "Check if POI is in a disconnected NavMesh region.");
            }
        }

        Debug.Log($"Path computation complete: {allPaths.Count} paths created, {failedPaths} failed");

        if (failedPaths > 0)
        {
            Debug.LogWarning($"{failedPaths} paths could not be computed. " +
                           "This may indicate disconnected areas in the NavMesh.");
        }

        return allPaths;
    }

    /// <summary>
    /// A* pathfinding between waypoints.
    /// Uses dictionary lookup for O(1) waypoint access.
    /// </summary>
    private List<int> FindShortestPath(int startId, int endId)
    {
        // Same waypoint - return single-element path
        if (startId == endId)
        {
            return new List<int> { endId };
        }

        // Validate waypoints exist using O(1) lookup
        if (!waypointLookup.TryGetValue(startId, out Waypoint startWp) ||
            !waypointLookup.TryGetValue(endId, out Waypoint endWp))
        {
            return null;
        }

        // A* data structures
        var openSet = new SortedSet<(float fScore, int id)>(
            Comparer<(float, int)>.Create((a, b) =>
            {
                int cmp = a.Item1.CompareTo(b.Item1);
                return cmp != 0 ? cmp : a.Item2.CompareTo(b.Item2);
            }));

        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, float>();
        var inOpenSet = new HashSet<int>();

        // Initialize with start node
        gScore[startId] = 0;
        float hStart = Vector3.Distance(startWp.position, endWp.position);
        openSet.Add((hStart, startId));
        inOpenSet.Add(startId);

        while (openSet.Count > 0)
        {
            // Get node with lowest fScore
            var current = openSet.Min;
            openSet.Remove(current);
            int currentId = current.id;
            inOpenSet.Remove(currentId);

            // Reached destination - reconstruct path
            if (currentId == endId)
            {
                var path = new List<int>();
                int node = endId;
                while (cameFrom.ContainsKey(node))
                {
                    path.Add(node);
                    node = cameFrom[node];
                }
                path.Add(startId);
                path.Reverse();
                return path;
            }

            // Get current waypoint using O(1) lookup
            if (!waypointLookup.TryGetValue(currentId, out Waypoint currentWp))
            {
                continue;
            }

            // Explore neighbors
            foreach (int neighborId in currentWp.connectedWaypoints)
            {
                // Get neighbor using O(1) lookup
                if (!waypointLookup.TryGetValue(neighborId, out Waypoint neighborWp))
                {
                    continue;
                }

                float tentativeG = gScore[currentId] + Vector3.Distance(currentWp.position, neighborWp.position);

                if (!gScore.ContainsKey(neighborId) || tentativeG < gScore[neighborId])
                {
                    cameFrom[neighborId] = currentId;
                    gScore[neighborId] = tentativeG;
                    float fScoreValue = tentativeG + Vector3.Distance(neighborWp.position, endWp.position);

                    if (!inOpenSet.Contains(neighborId))
                    {
                        openSet.Add((fScoreValue, neighborId));
                        inOpenSet.Add(neighborId);
                    }
                }
            }
        }

        // No path found
        return null;
    }

    /// <summary>
    /// Exports all navigation data to JSON (bounds, POIs, waypoints, and paths)
    /// </summary>
    public void ExportNavigationData()
    {
        // Generate waypoints if not already done
        if (generatedWaypoints.Count == 0)
        {
            GenerateNavigationData();
        }

        // Validate we have data to export
        if (generatedWaypoints.Count == 0)
        {
            Debug.LogError("No waypoints to export. Generation may have failed.");
            return;
        }

        // Compute all paths
        var allPaths = ComputeAllPaths();

        // Create export data with all components (bounds, pois, waypoints, paths)
        var exportData = new PrecomputedNavigationData
        {
            mapCode = MapCode,
            exportedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            waypointSpacing = waypointSpacing,
            bounds = mapBounds,
            pois = poiDestinations,
            waypoints = generatedWaypoints,
            paths = allPaths
        };

        // Export to JSON
        string json = JsonUtility.ToJson(exportData, true);

        string fullPath = Path.Combine(Application.dataPath, exportDirectory);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        string fileName = $"{MapCode}_navigation_data.json";
        string filePath = Path.Combine(fullPath, fileName);

        File.WriteAllText(filePath, json);
        Debug.Log($"Navigation data exported to: {filePath}");
        Debug.Log($"Export summary: {mapBounds != null} bounds, {poiDestinations.Count} POIs, " +
                  $"{generatedWaypoints.Count} waypoints, {allPaths.Count} paths");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// Loads navigation data from JSON (for runtime use on glasses)
    /// </summary>
    public static PrecomputedNavigationData LoadFromJson(string jsonContent)
    {
        return JsonUtility.FromJson<PrecomputedNavigationData>(jsonContent);
    }

    /// <summary>
    /// Loads navigation data from StreamingAssets (for runtime use)
    /// </summary>
    public static PrecomputedNavigationData LoadFromStreamingAssets(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            return LoadFromJson(json);
        }

        Debug.LogError($"Navigation data file not found: {filePath}");
        return null;
    }

    #region Helper Methods

    private Bounds CalculateMeshBounds()
    {
        Renderer[] renderers = mapMesh.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return new Bounds(mapMesh.transform.position, Vector3.one * 10);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private int FindNearestWaypoint(Vector3 position)
    {
        int nearestId = -1;
        float nearestDistance = float.MaxValue;

        foreach (var waypoint in generatedWaypoints)
        {
            float distance = Vector3.Distance(waypoint.position, position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestId = waypoint.id;
            }
        }

        return nearestId;
    }

    private float CalculatePathLength(Vector3[] corners)
    {
        float length = 0;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            length += Vector3.Distance(corners[i], corners[i + 1]);
        }
        return length;
    }

    private float CalculatePathDistance(List<int> waypointIds)
    {
        float distance = 0;
        for (int i = 0; i < waypointIds.Count - 1; i++)
        {
            // Use O(1) dictionary lookup instead of O(n) Find
            if (waypointLookup.TryGetValue(waypointIds[i], out var wp1) &&
                waypointLookup.TryGetValue(waypointIds[i + 1], out var wp2))
            {
                distance += Vector3.Distance(wp1.position, wp2.position);
            }
        }
        return distance;
    }

    private float GetColliderRadius(POI poi)
    {
        if (poi.poiCollider == null) return 1.5f;

        SphereCollider sphere = poi.poiCollider.GetComponent<SphereCollider>();
        if (sphere != null) return sphere.radius;

        CapsuleCollider capsule = poi.poiCollider.GetComponent<CapsuleCollider>();
        if (capsule != null) return capsule.radius;

        BoxCollider box = poi.poiCollider.GetComponent<BoxCollider>();
        if (box != null) return Mathf.Min(box.size.x, box.size.z) / 2f;

        return 1.5f; // Default arrival radius
    }

    /// <summary>
    /// Gets POI by ID (for runtime navigation)
    /// </summary>
    public POIData GetPOIById(int id)
    {
        return poiDestinations.Find(p => p.id == id);
    }

    /// <summary>
    /// Gets all POIs (for UI list population)
    /// </summary>
    public List<POIData> GetAllPOIs()
    {
        return poiDestinations;
    }

    /// <summary>
    /// Gets all waypoints
    /// </summary>
    public List<Waypoint> GetAllWaypoints()
    {
        return generatedWaypoints;
    }

    /// <summary>
    /// Gets the map bounds
    /// </summary>
    public MapBounds GetMapBounds()
    {
        return mapBounds;
    }

    /// <summary>
    /// Converts local position to world position (when map is localized)
    /// </summary>
    public Vector3 LocalToWorldPosition(Vector3 localPosition)
    {
        if (mapMesh != null)
        {
            return mapMesh.transform.TransformPoint(localPosition);
        }
        return localPosition;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        // Draw failed sample points as red X marks
        if (showFailedSamples && failedSamplePoints != null && failedSamplePoints.Count > 0)
        {
            Gizmos.color = Color.red;
            float crossSize = waypointGizmoSize * 0.5f;
            foreach (var point in failedSamplePoints)
            {
                // Draw X mark
                Gizmos.DrawLine(point + new Vector3(-crossSize, 0, -crossSize),
                               point + new Vector3(crossSize, 0, crossSize));
                Gizmos.DrawLine(point + new Vector3(-crossSize, 0, crossSize),
                               point + new Vector3(crossSize, 0, -crossSize));
            }
        }

        if (!showWaypointsInScene || generatedWaypoints == null || generatedWaypoints.Count == 0) return;

        // Build lookup if needed for gizmo drawing
        if (waypointLookup == null || waypointLookup.Count != generatedWaypoints.Count)
        {
            waypointLookup = new Dictionary<int, Waypoint>();
            foreach (var wp in generatedWaypoints)
            {
                waypointLookup[wp.id] = wp;
            }
        }

        // Draw waypoints
        foreach (var waypoint in generatedWaypoints)
        {
            // Color waypoints based on connectivity
            if (waypoint.connectedWaypoints.Count == 0)
            {
                Gizmos.color = Color.red; // Isolated waypoints in red
            }
            else
            {
                Gizmos.color = waypointColor;
            }

            Gizmos.DrawSphere(waypoint.position, waypointGizmoSize);

            // Draw connections
            Gizmos.color = new Color(waypointColor.r, waypointColor.g, waypointColor.b, 0.3f);
            foreach (int connectedId in waypoint.connectedWaypoints)
            {
                // Use O(1) lookup
                if (waypointLookup.TryGetValue(connectedId, out var connected))
                {
                    Gizmos.DrawLine(waypoint.position, connected.position);
                }
            }
        }

        // Draw POI destinations
        if (poiDestinations != null)
        {
            foreach (var poi in poiDestinations)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(poi.worldPosition, poi.arrivalRadius);

                // Draw line to nearest waypoint
                if (waypointLookup.TryGetValue(poi.nearestWaypointId, out var nearestWp))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(poi.worldPosition, nearestWp.position);
                }
            }
        }

        // Draw map bounds if available
        if (mapBounds != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireCube(mapBounds.center, mapBounds.size);
        }
    }

    #endregion
}
