/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace MultiSet
{
    [CustomEditor(typeof(MultisetSdkManager))]
    public class MultisetSdkManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            string version = MultisetSdkManager.version;
            EditorGUILayout.LabelField("SDK Version : ", version, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This is the current MultiSet SDK version.", MessageType.Info);
            EditorGUILayout.Space();

            DrawDefaultInspector();

            GUILayout.Space(20);

            if (GUILayout.Button("Open MultiSet Configuration"))
            {
                OpenMultiSetConfig();
            }
            
            GUILayout.Space(20);

            Texture2D logo = new Texture2D(2, 2);
            logo.LoadImage(Base64ToImageConverter.LoadWatermark());

            if (logo != null)
            {
                // GUILayout.Label(logo, GUILayout.Width(64), GUILayout.Height(64));

                float width = EditorGUIUtility.currentViewWidth / 2; // 40 for padding
                float aspect = (float)logo.height / logo.width;
                float height = width * aspect;

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(logo, GUILayout.Width(width), GUILayout.Height(height));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

        }

        private void OpenMultiSetConfig()
        {
            MultiSetConfig config = Resources.Load<MultiSetConfig>("MultiSetConfig"); // Ensure the name matches

            if (config != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(config);

                if (!string.IsNullOrEmpty(assetPath))
                {
                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                    else
                    {
                        Debug.LogError("Failed to load MultiSetConfig asset.");
                    }
                }
                else
                {
                    Debug.LogError("Asset path for MultiSetConfig not found.");
                }
            }
            else
            {
                Debug.LogError("MultiSetConfig asset not found in Resources folder.");
            }
        }
    }
}
#endif
