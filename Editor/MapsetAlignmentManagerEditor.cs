/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEditor;

namespace MultiSet
{
    [CustomEditor(typeof(MapsetAlignmentManager))]
    public class MapsetAlignmentManagerEditor : Editor
    {
        private MapsetAlignmentManager manager;
        private bool showMapsList = false;
        private Vector2 mapsScrollPosition;

        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle sectionStyle;

        private void OnEnable()
        {
            manager = (MapsetAlignmentManager)target;

            // Subscribe to events (use += to allow multiple listeners)
            manager.onMapSetInfoLoaded += OnMapSetInfoLoaded;
            manager.onMeshesDownloaded += OnMeshesDownloaded;
            manager.onAlignmentSaved += OnAlignmentSaved;

            // Set up editor callbacks for DLL compatibility
            // These callbacks allow the DLL code to invoke Unity Editor APIs
            manager.onRefreshAssetDatabase = () => AssetDatabase.Refresh();
            manager.onSetDirty = (obj) => EditorUtility.SetDirty(obj);
            manager.onLoadAssetAtPath = (path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);
            manager.onInstantiatePrefab = (prefab) => PrefabUtility.InstantiatePrefab(prefab);
            manager.onSaveAsPrefab = (go, path) => PrefabUtility.SaveAsPrefabAsset(go, path);
            manager.onMarkSceneDirty = (scene) => UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        private void OnDisable()
        {
            // Unsubscribe from events and callbacks
            if (manager != null)
            {
                // Use -= to properly unsubscribe
                manager.onMapSetInfoLoaded -= OnMapSetInfoLoaded;
                manager.onMeshesDownloaded -= OnMeshesDownloaded;
                manager.onAlignmentSaved -= OnAlignmentSaved;

                // Clean up editor callbacks
                manager.onRefreshAssetDatabase = null;
                manager.onSetDirty = null;
                manager.onLoadAssetAtPath = null;
                manager.onInstantiatePrefab = null;
                manager.onSaveAsPrefab = null;
                manager.onMarkSceneDirty = null;
            }
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();

            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("MapSet Alignment Tool", headerStyle);
            EditorGUILayout.Space(10);

            // MapSpace field
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mapSpace"), new GUIContent("Map Space GameObject"));
            EditorGUILayout.Space(5);

            // MapSet Code field
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mapsetCode"), new GUIContent("MapSet Code"));

            // Validation warning
            if (string.IsNullOrEmpty(manager.mapsetCode))
            {
                EditorGUILayout.HelpBox("Please enter a MapSet Code to continue.", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Download MapSet Data Button
            GUI.enabled = !string.IsNullOrEmpty(manager.mapsetCode) && !manager.isLoadingMapSetInfo && !manager.isDownloadingMeshes;
            if (GUILayout.Button("Download MapSet Data", GUILayout.Height(35)))
            {
                manager.GetMapSetInfo();
            }
            GUI.enabled = true;

            // Loading status
            if (manager.isLoadingMapSetInfo || manager.isDownloadingMeshes)
            {
                EditorGUILayout.HelpBox(manager.loadingStatus, MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Display MapSet Details if available
            if (manager.currentMapSet != null)
            {
                DrawMapSetDetails();
            }

            serializedObject.ApplyModifiedProperties();

            // Repaint if loading or downloading
            if (manager.isLoadingMapSetInfo || manager.isDownloadingMeshes)
            {
                Repaint();
            }
        }

        private void DrawMapSetDetails()
        {
            EditorGUILayout.BeginVertical(sectionStyle);

            EditorGUILayout.LabelField("MapSet Details", headerStyle);
            EditorGUILayout.Space(5);

            // MapSet Name
            DrawDetailRow("Name:", manager.currentMapSet.name);

            // MapSet Code
            DrawDetailRow("Code:", manager.currentMapSet.mapSetCode);

            // Status
            DrawDetailRow("Status:", manager.currentMapSet.status ?? "N/A");

            // Total Maps
            int totalMaps = manager.currentMapSet.mapSetData?.Count ?? 0;
            DrawDetailRow("Total Maps:", totalMaps.ToString());

            EditorGUILayout.Space(10);

            // Maps List Section
            if (totalMaps > 0 && manager.currentMapSet.mapSetData != null)
            {
                showMapsList = EditorGUILayout.Foldout(showMapsList, "Maps List", true, EditorStyles.foldoutHeader);

                if (showMapsList)
                {
                    EditorGUILayout.Space(5);
                    mapsScrollPosition = EditorGUILayout.BeginScrollView(mapsScrollPosition, GUILayout.MaxHeight(200));

                    for (int i = 0; i < manager.currentMapSet.mapSetData.Count; i++)
                    {
                        var mapData = manager.currentMapSet.mapSetData[i];
                        if (mapData != null)
                        {
                            DrawMapItem(i, mapData);
                        }
                    }

                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.Space(10);

            // Downloading status with progress bar
            if (manager.isDownloadingMeshes)
            {
                float progress = totalMaps > 0 ? (float)manager.loadedMapMeshes.Count / totalMaps : 0f;
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(),
                    progress,
                    $"Downloading: {manager.loadedMapMeshes.Count}/{totalMaps}"
                );
            }

            EditorGUILayout.Space(15);

            // Alignment Section
            if (manager.loadedMapMeshes.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "MapSet meshes are now loaded in the scene. You can adjust the transform values of each map in the Scene view.\n\n" +
                    "Instructions:\n" +
                    "1. Select individual map objects in the Hierarchy (children of MapSpace)\n" +
                    "2. Use Unity's transform tools (Move, Rotate, Scale) to align the maps\n" +
                    "3. Select a map to see its details and update its pose individually\n" +
                    "4. Or use 'Update All Modified Poses' to save all changes at once",
                    MessageType.Info
                );

                EditorGUILayout.Space(10);

                // Loaded meshes info
                DrawDetailRow("Loaded Meshes:", manager.loadedMapMeshes.Count.ToString());

                // Count modified maps
                int modifiedCount = 0;
                foreach (GameObject mapObject in manager.loadedMapMeshes)
                {
                    if (mapObject != null)
                    {
                        MapDataReference mapRef = mapObject.GetComponent<MapDataReference>();
                        if (mapRef != null && mapRef.hasUnsavedChanges)
                        {
                            modifiedCount++;
                        }
                    }
                }

                if (modifiedCount > 0)
                {
                    DrawDetailRow("Modified Maps:", modifiedCount.ToString());
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox($"{modifiedCount} map(s) have unsaved pose changes.", MessageType.Warning);
                }

                EditorGUILayout.Space(10);

                // Update All Modified Poses Button
                GUI.enabled = modifiedCount > 0;
                GUI.backgroundColor = modifiedCount > 0 ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
                if (GUILayout.Button($"Update All Modified Poses ({modifiedCount})", GUILayout.Height(40)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Update All Modified Poses",
                        $"Are you sure you want to update the poses of {modifiedCount} modified map(s) to the server?",
                        "Update All",
                        "Cancel"))
                    {
                        manager.UpdateAllModifiedPoses(
                            (completed, total) =>
                            {
                                EditorUtility.DisplayProgressBar("Updating Poses", $"Updating map {completed}/{total}...", (float)completed / total);
                            },
                            (success, message) =>
                            {
                                EditorUtility.ClearProgressBar();
                                if (success)
                                {
                                    EditorUtility.DisplayDialog("Success", message, "OK");
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Error", message, "OK");
                                }
                                Repaint();
                            }
                        );
                    }
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMapItem(int index, MapSetData mapData)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Map {index + 1}", EditorStyles.boldLabel, GUILayout.Width(60));
            if (mapData.map != null)
            {
                EditorGUILayout.LabelField(mapData.map.mapName ?? "Unnamed", EditorStyles.label);
            }
            EditorGUILayout.EndHorizontal();

            if (mapData.map != null)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Code:", GUILayout.Width(60));
                EditorGUILayout.LabelField(mapData.map.mapCode ?? "N/A", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Status:", GUILayout.Width(60));
                EditorGUILayout.LabelField(mapData.map.status ?? "N/A", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Order:", GUILayout.Width(60));
                EditorGUILayout.LabelField(mapData.order.ToString(), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                // Relative Pose info
                if (mapData.relativePose != null)
                {
                    EditorGUILayout.LabelField("Relative Pose:", EditorStyles.miniBoldLabel);
                    if (mapData.relativePose.position != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Position:", GUILayout.Width(80));
                        EditorGUILayout.LabelField(
                            $"({mapData.relativePose.position.x:F2}, {mapData.relativePose.position.y:F2}, {mapData.relativePose.position.z:F2})",
                            EditorStyles.miniLabel
                        );
                        EditorGUILayout.EndHorizontal();
                    }
                    if (mapData.relativePose.rotation != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Rotation:", GUILayout.Width(80));
                        EditorGUILayout.LabelField(
                            $"({mapData.relativePose.rotation.qx:F2}, {mapData.relativePose.rotation.qy:F2}, {mapData.relativePose.rotation.qz:F2}, {mapData.relativePose.rotation.qw:F2})",
                            EditorStyles.miniLabel
                        );
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private void DrawDetailRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, labelStyle, GUILayout.Width(120));
            EditorGUILayout.LabelField(value, valueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold
                };
            }

            if (valueStyle == null)
            {
                valueStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true
                };
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        private void OnMapSetInfoLoaded(bool success, string message)
        {
            if (success)
            {
                // Check if mapset is active
                if (manager.currentMapSet != null && manager.currentMapSet.status == "active")
                {
                    // Check if MapSpace is assigned before auto-downloading
                    if (manager.mapSpace == null)
                    {
                        EditorUtility.DisplayDialog("MapSpace Required",
                            "MapSet data loaded successfully!\n\nPlease assign a MapSpace GameObject to download and view the map meshes.",
                            "OK");
                    }
                    else
                    {
                        // Auto-download meshes since mapset is active
                        manager.ViewMapSet();
                    }
                }
                else
                {
                    // MapSet is not active
                    string status = manager.currentMapSet?.status ?? "unknown";
                    EditorUtility.DisplayDialog("MapSet Not Active",
                        $"The MapSet '{manager.currentMapSet?.name ?? manager.mapsetCode}' is not active.\n\nCurrent status: {status}\n\nOnly active MapSets can be used for alignment.",
                        "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load MapSet information:\n{message}", "OK");
            }
            Repaint();
        }

        private void OnMeshesDownloaded(bool success, string message)
        {
            if (success)
            {
                EditorUtility.DisplayDialog("Success", "All meshes downloaded successfully!\n\nYou can now align the maps in the Scene view.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Failed to download meshes:\n{message}", "OK");
            }
            Repaint();
        }

        private void OnAlignmentSaved(bool success, string message)
        {
            if (success)
            {
                EditorUtility.DisplayDialog("Success", "Alignment saved successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Failed to save alignment:\n{message}", "OK");
            }
            Repaint();
        }
    }
}
