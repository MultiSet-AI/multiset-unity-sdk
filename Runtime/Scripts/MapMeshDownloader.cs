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
    public class MapMeshDownloader : MonoBehaviour
    {
        // private MapLocalizationManager mapLocalizationManager;

        [Space(10)]
        [Tooltip("Drag and drop the MapSpace GameObject here.")]
        public GameObject m_mapSpace;

        private VpsMap m_vpsMap;
        private MapSet mapSet;
        private string mapOrMapsetCode;
        private string m_savePath;

        [HideInInspector]
        public bool isDownloading = false;
        int loadedMaps = 0;

        private bool itsMap = true;

        public void DownloadMesh()
        {
            if (Application.isPlaying)
            {
                return;
            }

            loadedMaps = 0;
            isDownloading = true;

            MultisetSdkManager multisetSdkManager = FindFirstObjectByType<MultisetSdkManager>();

            MapLocalizationManager mapLocalizationManager = FindFirstObjectByType<MapLocalizationManager>();
            SingleFrameLocalizationManager singleFrameLocalizationManager = FindFirstObjectByType<SingleFrameLocalizationManager>();

            if (mapLocalizationManager != null)
            {
                mapOrMapsetCode = mapLocalizationManager.mapOrMapsetCode;
                itsMap = mapLocalizationManager.localizationType == LocalizationType.Map;
            }
            else if (singleFrameLocalizationManager != null)
            {
                mapOrMapsetCode = singleFrameLocalizationManager.mapOrMapsetCode;
                itsMap = singleFrameLocalizationManager.localizationType == LocalizationType.Map;
            }

            if (string.IsNullOrWhiteSpace(mapOrMapsetCode))
            {
                isDownloading = false;
                Debug.LogError("Map or MapSet Code Missing in MapLocalizationManager!!");
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
                Debug.Log("Fetching Map data..");

                // Proceed with further actions after successful authentication
                if (itsMap)
                {
                    GetMapDetails(mapOrMapsetCode);
                }
                else
                {
                    GetMapSetDetails(mapOrMapsetCode);
                }
            }
            else
            {
                isDownloading = false;
                Debug.LogError("Authentication failed!");
            }

            // Unsubscribe from the AuthCallBack event
            EventManager<EventData>.StopListening("AuthCallBack", OnAuthCallBack);
        }

        #region MAP-DATA
        private void GetMapDetails(string mapIdOrCode)
        {
            MultiSetApiManager.GetMapDetails(mapIdOrCode, MapDetailsCallback);
        }

        private void MapDetailsCallback(bool success, string data, long statusCode)
        {
            if (string.IsNullOrEmpty(data))
            {
                isDownloading = false;
                Debug.LogError("Error : Map Details Callback: Empty or null data received!");
                return;
            }

            if (success)
            {
                m_vpsMap = JsonUtility.FromJson<VpsMap>(data);
                DownloadGlbFileEditor(m_vpsMap);
            }
            else
            {
                isDownloading = false;
                Debug.LogError("Get Map Details failed!" + data);
            }
        }

        public void DownloadGlbFileEditor(VpsMap vpsMap)
        {
            this.m_vpsMap = vpsMap;

            string directoryPath = Path.Combine(Application.dataPath, "MultiSet/MapData/" + mapOrMapsetCode);
            string finalFilePath = Path.Combine("Assets/MultiSet/MapData/" + mapOrMapsetCode, mapOrMapsetCode + ".glb");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            m_savePath = Path.Combine(directoryPath, mapOrMapsetCode + ".glb");

            if (File.Exists(m_savePath))
            {
                isDownloading = false;
                ImportAndAttachGLB(finalFilePath);
            }
            else
            {
                string _meshLink = m_vpsMap.mapMesh.texturedMesh.meshLink;
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

                            // string mapId = Util.GetMapId(meshUrl.url);
                            string finalFilePath = Path.Combine("Assets/MultiSet/MapData/" + mapOrMapsetCode, mapOrMapsetCode + ".glb");

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

            if (m_mapSpace == null)
            {
                Debug.LogError("MapSpace GameObject is not assigned!");
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
                Debug.LogWarning("Map Mesh with the name " + importedObject.name + " already exists in the hierarchy.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(importedObject) as GameObject;
            if (instance != null)
            {
                instance.transform.SetParent(m_mapSpace.transform, false);

                // Add EditorOnly Tag to the instantiated GameObject
                instance.tag = "EditorOnly";

                //save the gameObject as prefab 
                string prefabPath = Path.Combine("Assets/MultiSet/MapData/", mapOrMapsetCode + ".prefab");
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

            }
            else
            {
                Debug.LogError("Failed to instantiate the imported GLB object.");
            }

            //Show Default Unity Dialog
            EditorUtility.DisplayDialog("Map Mesh Ready", "Mesh File is loaded in the scene", "OK");

#endif
        }

        #endregion

        #region MAPSET-DATA

        private void GetMapSetDetails(string mapsetCode)
        {
            MultiSetApiManager.GetMapSetDetails(mapsetCode, MapSetDetailsCallback);
        }

        private void MapSetDetailsCallback(bool success, string data, long statusCode)
        {
            if (string.IsNullOrEmpty(data))
            {
                isDownloading = false;
                Debug.LogError("MapSet Details Callback: Empty or null data received.");
                return;
            }

            if (success)
            {
                MapSetResult mapSetResult = JsonUtility.FromJson<MapSetResult>(data);

                if (mapSetResult != null && mapSetResult.mapSet.mapSetData != null)
                {
                    this.mapSet = mapSetResult.mapSet;

                    GetMapSetMesh(mapSet);
                }
            }
            else
            {
                isDownloading = false;
                ErrorJSON errorJSON = JsonUtility.FromJson<ErrorJSON>(data);
                Debug.LogError($" Load MapSet Info Failed: " + errorJSON.error + "  code: " + statusCode);
            }
        }

        public void GetMapSetMesh(MapSet mapSet)
        {
            this.mapSet = mapSet;

            List<MapSetData> mapSetDataList = mapSet.mapSetData;

            foreach (MapSetData mapSetData in mapSetDataList)
            {
                string _mapCode = mapSetData.map.mapCode;
                string directoryPath = Path.Combine(Application.dataPath, "MultiSet/MapData/" + mapSet.mapSetCode);
                string finalFilePath = Path.Combine("Assets/MultiSet/MapData/" + mapSet.mapSetCode, _mapCode + ".glb");

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (File.Exists(finalFilePath))
                {
                    isDownloading = false;
                    ImportAndAttachGLBMapset(finalFilePath, mapSetData.map._id);
                }
                else
                {
                    string _meshLink = mapSetData.map.mapMesh.texturedMesh.meshLink;
                    MultiSetApiManager.GetFileUrl(_meshLink, FileUrlCallbackMapset);
                }
            }
        }

        private void FileUrlCallbackMapset(bool success, string data, long statusCode)
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
                            string mapId = Util.GetMapId(meshUrl.url);
                            string _mapCode = GetMapCodeFromMapSetData(mapId);

                            string finalFilePath = Path.Combine("Assets/MultiSet/MapData/" + mapSet.mapSetCode, _mapCode + ".glb");

                            string directoryPath = Path.Combine(Application.dataPath, "MultiSet/MapData/" + mapSet.mapSetCode);
                            m_savePath = Path.Combine(directoryPath, _mapCode + ".glb");

                            File.WriteAllBytes(m_savePath, fileData);

                            // Refresh the Asset Database to make Unity recognize the new file
#if UNITY_EDITOR
                            AssetDatabase.Refresh();
#endif

                            isDownloading = false;

                            if (File.Exists(finalFilePath))
                            {
                                ImportAndAttachGLBMapset(finalFilePath, mapId);
                            }
                            else
                            {
                                Debug.LogError("File not found at path: " + finalFilePath);
                            }

                        }
                        catch (Exception e)
                        {
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
                ErrorJSON errorJSON = JsonUtility.FromJson<ErrorJSON>(data);
                Debug.LogError("Error : " + JsonUtility.ToJson(errorJSON));
            }
        }

        private string GetMapCodeFromMapSetData(string mapId)
        {
            string mapCode = null;

            if (mapSet != null && mapSet.mapSetData != null)
            {
                foreach (MapSetData mapSetData in mapSet.mapSetData)
                {
                    if (mapSetData.map._id == mapId)
                    {
                        mapCode = mapSetData.map.mapCode;
                        break;
                    }
                }
            }

            return mapCode;
        }

        private void ImportAndAttachGLBMapset(string finalFilePath = null, string mapId = null)
        {
            string glbPath = finalFilePath;

            if (string.IsNullOrEmpty(glbPath))
            {
                Debug.LogError("GLB path cannot be empty!");
                return;
            }

            if (m_mapSpace == null)
            {
                Debug.LogError("Parent GameObject is not assigned!");
                return;
            }

#if UNITY_EDITOR
            
            GameObject importedObject = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);

            if (importedObject == null)
            {
                Debug.LogError("Failed to load GLB file. Ensure the file exists at the specified path.");
                return;
            }

            // Check if a GameObject with the same name already exists in the scene
            GameObject mapSetObject = GameObject.Find(this.mapSet.mapSetCode);
            if (mapSetObject == null)
            {
                mapSetObject = new GameObject(this.mapSet.mapSetCode);
                mapSetObject.transform.SetParent(m_mapSpace.transform, false);
                mapSetObject.tag = "EditorOnly";
            }

            // Check if a GameObject with the same name already exists in the scene
            GameObject existingObject = GameObject.Find(importedObject.name);
            if (existingObject != null)
            {
                Debug.LogWarning("Map Mesh with the name " + importedObject.name + " already exists in the hierarchy.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(importedObject) as GameObject;

            if (instance != null)
            {
                instance.transform.SetParent(mapSetObject.transform, false);

                Util.UpdateMeshPoseAndRotation(instance, mapSet, mapId);

                // Add EditorOnly Tag to the instantiated GameObject
                instance.tag = "EditorOnly";

                loadedMaps++;
            }
            else
            {
                Debug.LogError("Failed to instantiate the imported GLB object.");
            }

            if (loadedMaps == mapSet.mapSetData.Count)
            {
                //save the gameObject as prefab 
                string prefabPath = Path.Combine("Assets/MultiSet/MapData/", mapSet.mapSetCode + ".prefab");
                PrefabUtility.SaveAsPrefabAsset(mapSetObject, prefabPath);

                GameObject prefabInstance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)) as GameObject;
                prefabInstance.transform.SetParent(m_mapSpace.transform, false);
                DestroyImmediate(mapSetObject);

                //Show Default Unity Dialog
                EditorUtility.DisplayDialog("MapSet Mesh Ready", "MapSet Files are loaded in the scene", "OK");
            }
#endif
        }

        #endregion

    }
}