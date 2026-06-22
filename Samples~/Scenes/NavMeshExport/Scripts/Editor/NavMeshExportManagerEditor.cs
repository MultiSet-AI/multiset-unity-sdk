/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NavMeshExportManager))]
public class NavMeshExportManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NavMeshExportManager generator = (NavMeshExportManager)target;

        EditorGUILayout.Space(10);

        // Show the effective map code being used
        EditorGUILayout.LabelField("Effective Map Code", generator.MapCode, EditorStyles.boldLabel);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Waypoint Generation", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Waypoints", GUILayout.Height(30)))
        {
            generator.GenerateNavigationData();
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Export Navigation Data", GUILayout.Height(40)))
        {
            if (generator.mapMesh == null)
            {
                EditorUtility.DisplayDialog(
                    "Map Mesh Required",
                    "Please assign the Map Mesh reference first.",
                    "OK"
                );
                return;
            }

            generator.ExportNavigationData();
            EditorUtility.DisplayDialog(
                "Export Complete",
                $"Navigation data exported!\n\nFile: {generator.MapCode}_navigation_data.json\n\n" +
                "Includes: bounds, POIs, waypoints, and pre-computed paths.",
                "OK"
            );
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Workflow:\n" +
            "1. Assign the Map Mesh and POIs Parent references\n" +
            "2. Map Code is automatically set from the mesh name\n" +
            "   (use 'Map Code Override' to specify a different name)\n" +
            "3. Ensure NavMesh is baked on the map mesh\n" +
            "4. Click 'Generate Waypoints' to create navigation grid\n" +
            "5. Verify waypoints in Scene view (cyan spheres)\n" +
            "6. Click 'Export Navigation Data' to save JSON\n\n" +
            "The exported JSON includes:\n" +
            "- Map bounds (center, size, min, max)\n" +
            "- POI data (id, name, description, type, positions)\n" +
            "- Waypoint positions and connections\n" +
            "- Pre-computed paths to each POI\n\n" +
            "Use this on Meta Ray-Ban glasses for audio navigation.",
            MessageType.Info
        );
    }
}
