/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEditor;
using UnityEngine;

namespace MultiSet
{
    [CustomEditor(typeof(MapPointInspector))]
    public class MapPointInspectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MapPointInspector inspector = (MapPointInspector)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Map Point Inspector\n\n" +
                "Use this component to pick a point on the map mesh in Play mode " +
                "and read its floor height (Y) plus full position in MapSpace coordinates. " +
                "Useful when authoring hint poses, spawn points, or measuring vertical offsets.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Assign 'Map Space' — the root transform whose local space is the map's coordinate frame.\n" +
                "2. Assign 'Map Mesh' — the downloaded mesh GameObject (typically a child of Map Space).\n" +
                "3. MeshColliders are added automatically to descendants of 'Map Mesh' that don't have one.\n" +
                "4. Enter Play mode and move the mouse over the mesh — the cursor disc and readout label follow the surface.",
                MessageType.None);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Play-Mode Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Hover the mouse over the Map mesh to see the cursor and the height/position readout.\n" +
                "• Press the configured 'Copy Pose Key' (default: C) to copy the current cursor pose " +
                "(X, Y, Z and floor height) to the system clipboard.",
                MessageType.None);

            // Warn about missing required references.
            if (inspector.mapSpace == null || inspector.mapMesh == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Map Space and Map Mesh must both be assigned before entering Play mode.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(8);
            DrawDefaultInspector();

            // Live readout + manual copy button in Play mode.
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Live Cursor Readout", EditorStyles.boldLabel);

                if (inspector.IsHitting)
                {
                    Vector3 p = inspector.LocalHitPosition;
                    EditorGUILayout.LabelField("Floor Height (Y)", $"{p.y:F4} m");
                    EditorGUILayout.LabelField("Position (MapSpace)",
                        $"X: {p.x:F4}   Y: {p.y:F4}   Z: {p.z:F4}");

                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("Copy Current Pose To Clipboard", GUILayout.Height(28)))
                    {
                        inspector.CopyCurrentPoseToClipboard();
                    }

                    Repaint();
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Move the mouse over the Map Mesh in the Game view to see the live cursor pose here.",
                        MessageType.None);
                    Repaint();
                }
            }
        }
    }
}
