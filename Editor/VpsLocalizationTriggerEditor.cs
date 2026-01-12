/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEditor;

namespace MultiSet
{
    [CustomEditor(typeof(VpsLocalizationTrigger))]
    public class VpsLocalizationTriggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            // Add some space
            EditorGUILayout.Space(10);

            // Get reference to the target script
            VpsLocalizationTrigger trigger = (VpsLocalizationTrigger)target;

            // Create a styled button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30
            };

            // Add a button to refresh the BoxCollider
            if (GUILayout.Button("Refresh BoxCollider & Store Bounds", buttonStyle))
            {
                trigger.RefreshBoxCollider();

                // Mark the scene as dirty so Unity knows to save changes
                EditorUtility.SetDirty(trigger);
                if (trigger.gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(trigger.gameObject.scene);
                }
            }

            // Show helpful info
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Click the button above to calculate bounds from the map mesh and store them for the build. " +
                "This ensures the trigger zone present in build to trigger GPS and VPS localization.",
                MessageType.Info
            );

            // Show current stored bounds info
            if (trigger.storedSize != Vector3.zero)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Stored Bounds Info:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Position: {trigger.storedLocalPosition}");
                EditorGUILayout.LabelField($"Size: {trigger.storedSize}");
            }
        }
    }
}