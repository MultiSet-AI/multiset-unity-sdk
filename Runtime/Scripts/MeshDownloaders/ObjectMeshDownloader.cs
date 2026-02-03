/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MultiSet
{
    public class ObjectMeshDownloader : MonoBehaviour
    {
        [Space(10)]
        [Tooltip("Drag and drop the ObjectSpace GameObject here.")]
        public GameObject m_objectSpace;
        private List<string> objectCodes = new List<string>();
        private int currentObjectIndex = 0;
        private string currentObjectCode => objectCodes[currentObjectIndex];
        private ModelSet m_object;
        private string m_savePath;

        [HideInInspector]
        public bool isDownloading = false;
        int loadedObjects = 0;

        public void DownloadMesh()
        {
            if (Application.isPlaying)
            {
                return;
            }

            loadedObjects = 0;
            currentObjectIndex = 0;
            isDownloading = true;

            MultisetSdkManager multisetSdkManager = FindFirstObjectByType<MultisetSdkManager>();
            ObjectTrackingManager objectTrackingManager = FindFirstObjectByType<ObjectTrackingManager>();
            if (objectTrackingManager != null)
            {
                objectCodes = objectTrackingManager.objectCodes
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .ToList();
            }
            else
            {
                isDownloading = false;
                Debug.LogError("ObjectTrackingManager not found in the scene!");
                return;
            }

            if (objectCodes.Count == 0)
            {
                isDownloading = false;
                Debug.LogError("No valid Object Codes found in ObjectTrackingManager!!");
                return;
            }

            MultiSetConfig config = Resources.Load<MultiSetConfig>("MultiSetConfig");
            if (config != null)
            {
                multisetSdkManager.clientId = config.clientId;
                multisetSdkManager.clientSecret = config.clientSecret;

                if (!string.IsNullOrWhiteSpace(multisetSdkManager.clientId) && !string.IsNullOrWhiteSpace(multisetSdkManager.clientSecret))
                {
                    // Subscribe to the AuthCallBack event
                    EventManager<EventData>.StartListening("AuthCallBack", OnAuthCallBack);

                    multisetSdkManager.AuthenticateMultiSetSDK();
                }
                else
                {
                    isDownloading = false;
                    Debug.LogError("Please enter valid credentials in MultiSetConfig!");
                }
            }
            else
            {
                isDownloading = false;
                Debug.LogError("MultiSetConfig not found!");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from the AuthCallBack event
            EventManager<EventData>.StopListening("AuthCallBack", OnAuthCallBack);
        }

        private void OnAuthCallBack(EventData eventData)
        {
            if (eventData.AuthSuccess)
            {
                ProcessNextObject();
            }
            else
            {
                isDownloading = false;
                Debug.LogError("Authentication failed!");
            }

            // Unsubscribe from the AuthCallBack event
            EventManager<EventData>.StopListening("AuthCallBack", OnAuthCallBack);
        }

        private void ProcessNextObject()
        {
            if (currentObjectIndex >= objectCodes.Count)
            {
                isDownloading = false;
#if UNITY_EDITOR
                EditorUtility.DisplayDialog("Object Mesh Ready", loadedObjects + " Mesh File(s) loaded in the scene.", "OK");
#endif
                return;
            }

            string code = currentObjectCode;

            // Check if the mesh is already present in the scene under m_objectSpace
            if (IsMeshAlreadyInScene(code))
            {
                Debug.Log("Mesh for object code " + code + " is already present in the scene. Skipping.");
                currentObjectIndex++;
                ProcessNextObject();
                return;
            }

            Debug.Log("Fetching Object data for: " + code);
            GetObjectDetails(code);
        }

        private bool IsMeshAlreadyInScene(string code)
        {
            if (m_objectSpace == null) return false;

            string prefabPath = Path.Combine("Assets/MultiSet/ModelData/", code + ".prefab");

#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return false;

            // Check for object code container under m_objectSpace
            Transform codeContainer = m_objectSpace.transform.Find(code);
            if (codeContainer == null) return false;

            // Check if the prefab instance exists under the container
            foreach (Transform child in codeContainer)
            {
                if (child.name == prefab.name || child.name == prefab.name + "(Clone)")
                {
                    return true;
                }
            }
#endif

            return false;
        }

        #region MODEL-SET-DATA
        private void GetObjectDetails(string modelSetCode)
        {
            MultiSetApiManager.GetObjectDetails(modelSetCode, ObjectDetailsCallback);
        }

        private void ObjectDetailsCallback(bool success, string data, long statusCode)
        {
            if (string.IsNullOrEmpty(data))
            {
                isDownloading = false;
                Debug.LogError("Error : Object Details Callback: Empty or null data received!");
                return;
            }

            if (success)
            {
                m_object = JsonUtility.FromJson<ModelSet>(data);
                DownloadGlbFileEditor(m_object);
            }
            else
            {
                isDownloading = false;
                Debug.LogError("Get Object Details failed!" + data);
            }
        }

        public void DownloadGlbFileEditor(ModelSet modelSet)
        {
            string code = currentObjectCode;
            string directoryPath = Path.Combine(Application.dataPath, "MultiSet/ModelData/" + code);
            string finalFilePath = Path.Combine("Assets/MultiSet/ModelData/" + code, code + ".glb");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            m_savePath = Path.Combine(directoryPath, code + ".glb");

            if (File.Exists(m_savePath))
            {
                ImportAndAttachGLB(finalFilePath);
                currentObjectIndex++;
                ProcessNextObject();
            }
            else
            {
                string _meshLink = modelSet.objectMesh.meshLink;
                if (!string.IsNullOrWhiteSpace(_meshLink))
                {
                    MultiSetApiManager.GetFileUrl(_meshLink, FileUrlCallbackEditor);
                }
                else
                {
                    Debug.LogWarning("No mesh link found for object code: " + code);
                    currentObjectIndex++;
                    ProcessNextObject();
                }
            }
        }

        private void FileUrlCallbackEditor(bool success, string data, long statusCode)
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogError("File URL Callback: Empty or null data received!");
                currentObjectIndex++;
                ProcessNextObject();
                return;
            }

            if (success)
            {
                string code = currentObjectCode;
                FileData meshUrl = JsonUtility.FromJson<FileData>(data);

                MultiSetHttpClient.DownloadFileAsync(meshUrl.url, (byte[] fileData) =>
                {
                    if (fileData != null)
                    {
                        try
                        {
                            File.WriteAllBytes(m_savePath, fileData);

                            string finalFilePath = Path.Combine("Assets/MultiSet/ModelData/" + code, code + ".glb");

                            // Refresh the Asset Database to make Unity recognize the new file
#if UNITY_EDITOR
                            AssetDatabase.Refresh();
#endif

                            if (File.Exists(m_savePath))
                            {
                                ImportAndAttachGLB(finalFilePath);
                            }
                            else
                            {
                                Debug.LogError("File not found at path: " + m_savePath);
                            }

                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Failed to save mesh file for " + code + ": " + e.Message);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to download mesh file for " + code);
                    }

                    currentObjectIndex++;
                    ProcessNextObject();
                });
            }
            else
            {
                ErrorJSON errorJSON = JsonUtility.FromJson<ErrorJSON>(data);
                Debug.LogError("Error : " + errorJSON.error);
                currentObjectIndex++;
                ProcessNextObject();
            }

        }

        private void ImportAndAttachGLB(string finalFilePath = null)
        {
            string glbPath = finalFilePath;
            string code = currentObjectCode;

            if (string.IsNullOrEmpty(glbPath))
            {
                Debug.LogError("GLB path cannot be empty!");
                return;
            }

            if (m_objectSpace == null)
            {
                Debug.LogError("ObjectSpace GameObject is not assigned!");
                return;
            }

#if UNITY_EDITOR

            GameObject importedObject = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);

            if (importedObject == null)
            {
                Debug.LogError("Failed to load GLB file. Ensure the file exists at the specified path.");
                return;
            }

            // Save as prefab
            string prefabPath = Path.Combine("Assets/MultiSet/ModelData/", code + ".prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(importedObject, prefabPath);

            // Find or create a container named after the object code under ObjectSpace
            Transform codeContainer = m_objectSpace.transform.Find(code);
            if (codeContainer == null)
            {
                GameObject containerGO = new GameObject(code);
                containerGO.transform.SetParent(m_objectSpace.transform, false);
                codeContainer = containerGO.transform;
            }

            // Check if mesh already exists under the code container
            bool alreadyExists = false;
            foreach (Transform child in codeContainer)
            {
                if (child.name == prefab.name || child.name == prefab.name + "(Clone)")
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                // Instantiate the prefab under the object code container
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance != null)
                {
                    instance.transform.SetParent(codeContainer, false);
                    instance.tag = "EditorOnly";

                    loadedObjects++;

                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instance.scene);
                }
                else
                {
                    Debug.LogError("Failed to instantiate the imported GLB object.");
                }
            }
            else
            {
                Debug.LogWarning("Object Mesh for " + code + " already exists in the hierarchy.");
            }

#endif
        }

        #endregion
       
    }
}