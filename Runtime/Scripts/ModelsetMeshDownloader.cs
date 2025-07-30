/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
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
    public class ModelsetMeshDownloader : MonoBehaviour
    {
        [Space(10)]
        [Tooltip("Drag and drop the ObjectSpace GameObject here.")]
        public GameObject m_objectSpace;
        private string modelSetCode;
        private ModelSet m_modelSet;
        private string m_savePath;

        [HideInInspector]
        public bool isDownloading = false;
        int loadedModelSets = 0;

        public void DownloadMesh()
        {
            if (Application.isPlaying)
            {
                return;
            }

            loadedModelSets = 0;
            isDownloading = true;

            MultisetSdkManager multisetSdkManager = FindFirstObjectByType<MultisetSdkManager>();
            ModelsetTrackingManager modelsetTrackingManager = FindFirstObjectByType<ModelsetTrackingManager>();
            if (modelsetTrackingManager != null)
            {
                modelSetCode = modelsetTrackingManager.modelsetCode;
            }
            else
            {
                isDownloading = false;
                Debug.LogError("ModelSetTrackingManager not found in the scene!");
                return;
            }

            if (string.IsNullOrWhiteSpace(modelSetCode))
            {
                isDownloading = false;
                Debug.LogError("ModelSet Code Missing in ModelSetTrackingManager!!");
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
                Debug.Log("Fetching ModelSet data..");

                GetModelSetDetails(modelSetCode);
            }
            else
            {
                isDownloading = false;
                Debug.LogError("Authentication failed!");
            }

            // Unsubscribe from the AuthCallBack event
            EventManager<EventData>.StopListening("AuthCallBack", OnAuthCallBack);
        }

        #region MODEL-SET-DATA
        private void GetModelSetDetails(string modelSetCode)
        {
            MultiSetApiManager.GetModelSetDetails(modelSetCode, ModelSetDetailsCallback);
        }

        private void ModelSetDetailsCallback(bool success, string data, long statusCode)
        {
            if (string.IsNullOrEmpty(data))
            {
                isDownloading = false;
                Debug.LogError("Error : ModelSet Details Callback: Empty or null data received!");
                return;
            }

            if (success)
            {
                m_modelSet = JsonUtility.FromJson<ModelSet>(data);
                DownloadGlbFileEditor(m_modelSet);
            }
            else
            {
                isDownloading = false;
                Debug.LogError("Get ModelSet Details failed!" + data);
            }
        }

        public void DownloadGlbFileEditor(ModelSet modelSet)
        {
            string directoryPath = Path.Combine(Application.dataPath, "MultiSet/ModelData/" + modelSetCode);
            string finalFilePath = Path.Combine("Assets/MultiSet/ModelData/" + modelSetCode, modelSetCode + ".glb");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            m_savePath = Path.Combine(directoryPath, modelSetCode + ".glb");

            if (File.Exists(m_savePath))
            {
                isDownloading = false;
                ImportAndAttachGLB(finalFilePath);
            }
            else
            {
                string _meshLink = modelSet.objectMesh.meshLink;
                if (!string.IsNullOrWhiteSpace(_meshLink))
                {
                    MultiSetApiManager.GetFileUrl(_meshLink, FileUrlCallbackEditor);
                }
            }
        }

        private void FileUrlCallbackEditor(bool success, string data, long statusCode)
        {
            if (string.IsNullOrEmpty(data))
            {
                isDownloading = false;
                Debug.LogError("File URL Callback: Empty or null data received!");
                return;
            }

            if (success)
            {
                FileData meshUrl = JsonUtility.FromJson<FileData>(data);

                MultiSetHttpClient.DownloadFileAsync(meshUrl.url, (byte[] fileData) =>
                {
                    if (fileData != null)
                    {
                        try
                        {
                            File.WriteAllBytes(m_savePath, fileData);

                            string finalFilePath = Path.Combine("Assets/MultiSet/ModelData/" + modelSetCode, modelSetCode + ".glb");

                            // Refresh the Asset Database to make Unity recognize the new file
#if UNITY_EDITOR
                            AssetDatabase.Refresh();
#endif

                            isDownloading = false;

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
                            isDownloading = false;
                            Debug.LogError("Failed to save mesh file: " + e.Message);
                        }
                    }
                    else
                    {
                        isDownloading = false;
                        Debug.LogError("Failed to download mesh file.");
                    }
                });
            }
            else
            {
                isDownloading = false;
                ErrorJSON errorJSON = JsonUtility.FromJson<ErrorJSON>(data);
                Debug.LogError("Error : " + errorJSON.error);
            }

        }

        private void ImportAndAttachGLB(string finalFilePath = null)
        {
            string glbPath = finalFilePath;

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

            // Check if a GameObject with the same name already exists in the hierarchy
            GameObject existingObject = GameObject.Find(importedObject.name);
            if (existingObject != null)
            {
                Debug.LogWarning("Object Mesh with the name " + importedObject.name + " already exists in the hierarchy.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(importedObject) as GameObject;
            if (instance != null)
            {
                instance.transform.SetParent(m_objectSpace.transform, false);

                // Add EditorOnly Tag to the instantiated GameObject
                instance.tag = "EditorOnly";

                //save the gameObject as prefab 
                string prefabPath = Path.Combine("Assets/MultiSet/ModelData/", modelSetCode + ".prefab");
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

            }
            else
            {
                Debug.LogError("Failed to instantiate the imported GLB object.");
            }

            //Show Default Unity Dialog
            EditorUtility.DisplayDialog("Object Mesh Ready", "Mesh File is loaded in the scene", "OK");

#endif
        }

        #endregion
       
    }
}