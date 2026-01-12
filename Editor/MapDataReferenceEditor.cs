/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEditor;

namespace MultiSet
{
    [CustomEditor(typeof(MapDataReference))]
    public class MapDataReferenceEditor : Editor
    {
        private MapDataReference mapRef;

        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle sectionStyle;
        private GUIStyle warningStyle;
        private GUIStyle successStyle;

        private bool showIdentification = true;
        private bool showMapSetInfo = true;
        private bool showCurrentPose = true;
        private bool showOriginalPose = false;

        private void OnEnable()
        {
            mapRef = (MapDataReference)target;

            // Set up editor callback for DLL compatibility
            mapRef.onSetDirty = (obj) => EditorUtility.SetDirty(obj);
        }

        private void OnDisable()
        {
            // Clean up editor callback
            if (mapRef != null)
            {
                mapRef.onSetDirty = null;
            }
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();

            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Map Data Reference", headerStyle);
            EditorGUILayout.Space(5);

            // Status indicator
            if (mapRef.isUpdating)
            {
                EditorGUILayout.HelpBox("Updating pose to server...", MessageType.Info);
            }
            else if (mapRef.hasUnsavedChanges)
            {
                EditorGUILayout.HelpBox("This map has unsaved pose changes.", MessageType.Warning);
            }

            if (!string.IsNullOrEmpty(mapRef.lastUpdateStatus))
            {
                bool isSuccess = mapRef.lastUpdateStatus.Contains("successful");
                EditorGUILayout.HelpBox(mapRef.lastUpdateStatus, isSuccess ? MessageType.Info : MessageType.Error);
            }

            EditorGUILayout.Space(10);

            // Map Identification Section
            showIdentification = EditorGUILayout.BeginFoldoutHeaderGroup(showIdentification, "Map Identification");
            if (showIdentification)
            {
                EditorGUILayout.BeginVertical(sectionStyle);
                DrawReadOnlyField("Map Code:", mapRef.mapCode);
                DrawReadOnlyField("Map Name:", mapRef.mapName);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(5);

            // MapSet Information Section
            showMapSetInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showMapSetInfo, "MapSet Information");
            if (showMapSetInfo)
            {
                EditorGUILayout.BeginVertical(sectionStyle);
                DrawReadOnlyField("MapSet Code:", mapRef.mapSetCode);
                DrawReadOnlyField("MapSet Name:", mapRef.mapSetName);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(5);

            // Current Pose Section (Live from transform)
            showCurrentPose = EditorGUILayout.BeginFoldoutHeaderGroup(showCurrentPose, "Current Pose (From Transform)");
            if (showCurrentPose)
            {
                EditorGUILayout.BeginVertical(sectionStyle);

                EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"X: {mapRef.CurrentPosition.x:F6}");
                EditorGUILayout.LabelField($"Y: {mapRef.CurrentPosition.y:F6}");
                EditorGUILayout.LabelField($"Z: {mapRef.CurrentPosition.z:F6}");
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Rotation (Quaternion)", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"X: {mapRef.CurrentRotation.x:F6}");
                EditorGUILayout.LabelField($"Y: {mapRef.CurrentRotation.y:F6}");
                EditorGUILayout.LabelField($"Z: {mapRef.CurrentRotation.z:F6}");
                EditorGUILayout.LabelField($"W: {mapRef.CurrentRotation.w:F6}");
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(5);

                // Also show Euler angles for convenience
                Vector3 euler = mapRef.CurrentRotation.eulerAngles;
                EditorGUILayout.LabelField("Rotation (Euler)", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"X: {euler.x:F2}°");
                EditorGUILayout.LabelField($"Y: {euler.y:F2}°");
                EditorGUILayout.LabelField($"Z: {euler.z:F2}°");
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(5);

            // Original Pose Section
            showOriginalPose = EditorGUILayout.BeginFoldoutHeaderGroup(showOriginalPose, "Original Pose (From Server)");
            if (showOriginalPose)
            {
                EditorGUILayout.BeginVertical(sectionStyle);

                EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"X: {mapRef.OriginalPosition.x:F6}");
                EditorGUILayout.LabelField($"Y: {mapRef.OriginalPosition.y:F6}");
                EditorGUILayout.LabelField($"Z: {mapRef.OriginalPosition.z:F6}");
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Rotation (Quaternion)", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"X: {mapRef.OriginalRotation.x:F6}");
                EditorGUILayout.LabelField($"Y: {mapRef.OriginalRotation.y:F6}");
                EditorGUILayout.LabelField($"Z: {mapRef.OriginalRotation.z:F6}");
                EditorGUILayout.LabelField($"W: {mapRef.OriginalRotation.w:F6}");
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(15);

            // Pose Difference
            if (mapRef.hasUnsavedChanges)
            {
                EditorGUILayout.BeginVertical(sectionStyle);
                EditorGUILayout.LabelField("Pose Changes", EditorStyles.boldLabel);

                Vector3 posDiff = mapRef.CurrentPosition - mapRef.OriginalPosition;
                float angleDiff = Quaternion.Angle(mapRef.CurrentRotation, mapRef.OriginalRotation);

                EditorGUILayout.LabelField($"Position Delta: ({posDiff.x:F4}, {posDiff.y:F4}, {posDiff.z:F4})");
                EditorGUILayout.LabelField($"Rotation Delta: {angleDiff:F2}°");

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(10);
            }

            // Action Buttons
            EditorGUILayout.BeginHorizontal();

            // Update Pose Button
            GUI.enabled = mapRef.hasUnsavedChanges && !mapRef.isUpdating && !string.IsNullOrEmpty(mapRef.dataId);
            GUI.backgroundColor = mapRef.hasUnsavedChanges ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
            if (GUILayout.Button("Update Pose to Server", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog(
                    "Update Map Pose",
                    $"Are you sure you want to update the pose of '{mapRef.mapName}' on the server?",
                    "Update",
                    "Cancel"))
                {
                    UpdateMapPoseToServer();
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            // Reset to Original Button
            GUI.enabled = mapRef.hasUnsavedChanges && !mapRef.isUpdating;
            GUI.backgroundColor = mapRef.hasUnsavedChanges ? new Color(0.8f, 0.6f, 0.2f) : Color.white;
            if (GUILayout.Button("Reset to Original Pose", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset Pose",
                    $"Are you sure you want to reset '{mapRef.mapName}' to its original pose from the server?",
                    "Reset",
                    "Cancel"))
                {
                    mapRef.ResetToOriginalPose();
                    EditorUtility.SetDirty(mapRef);
                    SceneView.RepaintAll();
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Instructions
            EditorGUILayout.HelpBox(
                "Instructions:\n" +
                "1. Use Unity's Transform tools to adjust this map's position/rotation in the Scene view\n" +
                "2. Changes are detected automatically\n" +
                "3. Click 'Update Pose to Server' to save changes to the backend\n" +
                "4. Click 'Reset to Original Pose' to revert to the server values",
                MessageType.Info
            );

            serializedObject.ApplyModifiedProperties();

            // Continuously repaint while updating or if there are changes
            if (mapRef.isUpdating)
            {
                Repaint();
            }
        }

        private void UpdateMapPoseToServer()
        {
            if (string.IsNullOrEmpty(mapRef.dataId))
            {
                EditorUtility.DisplayDialog("Error", "Data ID is missing. Cannot update pose.", "OK");
                return;
            }

            mapRef.isUpdating = true;
            mapRef.lastUpdateStatus = "Updating...";
            EditorUtility.SetDirty(mapRef);

            // Create the payload using shared wrapper class
            RelativePoseWrapper payload = new RelativePoseWrapper
            {
                relativePose = mapRef.GetRelativePosePayload()
            };

            string jsonPayload = JsonUtility.ToJson(payload);

            // Make the API call
            MultiSetApiManager.UpdateMapSetData(mapRef.dataId, jsonPayload, (success, data, statusCode) =>
            {
                if (success)
                {
                    mapRef.OnPoseUpdateSuccess();

                    // Also update the MapSetData in parent manager if available
                    if (mapRef.parentManager != null && mapRef.parentManager.currentMapSet != null)
                    {
                        foreach (var mapSetData in mapRef.parentManager.currentMapSet.mapSetData)
                        {
                            if (mapSetData._id == mapRef.dataId)
                            {
                                mapSetData.relativePose = mapRef.GetRelativePosePayload();
                                break;
                            }
                        }
                    }

                    EditorUtility.DisplayDialog("Success", $"Pose for '{mapRef.mapName}' updated successfully!", "OK");
                }
                else
                {
                    // Use shared error parsing utility
                    string errorMsg = ApiErrorHelper.ParseErrorMessage(data);
                    Debug.LogError($"[MapDataReferenceEditor] Failed to update pose: {errorMsg} (code: {statusCode})");
                    mapRef.OnPoseUpdateFailed(errorMsg);
                    EditorUtility.DisplayDialog("Error", $"Failed to update pose:\n{errorMsg}", "OK");
                }

                // Repaint inspector
                Repaint();
            });
        }

        private void DrawReadOnlyField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, labelStyle, GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(value ?? "N/A", valueStyle, GUILayout.Height(18));
            EditorGUILayout.EndHorizontal();
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
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
    }
}
