using MultiSet;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlobalToLocalSpawner))]
public class GlobalToLocalSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GlobalToLocalSpawner tool = (GlobalToLocalSpawner)target;

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Global → Local Spawner\n\n" +
            "1. Assign 'Map Space' (the map origin frame). Optionally set a 'Map Code' " +
            "(otherwise it's read from the scene's localization target).\n" +
            "2. Press 'Fetch Map Origin' to pull the map's coordinates + heading from the API. " +
            "Only geo-referenced maps (non-zero heading) can be converted.\n" +
            "3. Assign a GPS JSON TextAsset and press 'Spawn Points' to convert each point and " +
            "place a marker sphere under Map Space.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        DrawDefaultInspector();

        // ── Fetched map origin (read-only) ──
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Map Origin (from API)", EditorStyles.boldLabel);

        if (tool.hasGeoReference)
        {
            EditorGUILayout.HelpBox(
                $"Geo-referenced ✓  '{tool.mapName}' ({tool.resolvedMapCode})\n" +
                $"lat {tool.originLatitude:F8}, lon {tool.originLongitude:F8}\n" +
                $"alt {tool.originAltitude:F2} m, heading {tool.headingDeg:F3}°",
                MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No geo-referenced origin fetched yet. Press 'Fetch Map Origin'.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(4);
        using (new EditorGUI.DisabledScope(tool.mapSpace == null))
        {
            if (GUILayout.Button("Fetch Map Origin", GUILayout.Height(26)))
            {
                tool.FetchMapOrigin((success, message) =>
                {
                    EditorUtility.DisplayDialog(success ? "Map Origin" : "Cannot Convert", message, "OK");
                    Repaint();
                });
            }
        }

        // ── Spawn / clear ──
        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(tool.mapSpace == null || tool.gpsJson == null || !tool.hasGeoReference))
        {
            if (GUILayout.Button("Spawn Points", GUILayout.Height(30)))
            {
                int n = tool.SpawnPoints(out string error);
                if (n == 0 && !string.IsNullOrEmpty(error))
                    EditorUtility.DisplayDialog("Spawn Points", error, "OK");
            }
        }

        using (new EditorGUI.DisabledScope(tool.mapSpace == null))
        {
            if (GUILayout.Button("Clear Points"))
            {
                tool.ClearPoints();
            }
        }
    }
}
