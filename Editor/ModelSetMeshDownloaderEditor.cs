/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEditor;
using UnityEngine;

namespace MultiSet
{
    [CustomEditor(typeof(ModelsetMeshDownloader))]
    public class ModelSetMeshDownloaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ModelsetMeshDownloader mapMeshDownloader = (ModelsetMeshDownloader)target;

            GUILayout.Space(20);
            EditorGUILayout.HelpBox("Download Object Mesh in Editor Mode. Helps to setup AR Scene", MessageType.Info);

            GUIContent buttonContent = new GUIContent("Download Mesh", "This downloads the mesh file for the specified ModelSet.");
            GUILayout.Space(20);

            if (mapMeshDownloader.isDownloading)
            {
                GUI.backgroundColor = Color.green;
                buttonContent.text = "Downloading Mesh...";
            }
            else
            {
                GUI.backgroundColor = GUI.backgroundColor; // Reset to default color
                buttonContent.text = "Download Object Mesh";
            }

            if (GUILayout.Button(buttonContent, GUILayout.Height(30))) // Increase the height to 30
            {
                if (!mapMeshDownloader.isDownloading)
                {
                    mapMeshDownloader.DownloadMesh();
                }
            }
        }
    }
}