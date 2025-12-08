/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace MultiSet
{
    [CustomEditor(typeof(OnDeviceLocalizationManager))]
    public class OnDeviceLocalizationManagerEditor : Editor
    {
        private VpsMap vpsMap;
        private MapSet mapSet;
        private bool isMapDataLoaded = false;
        private bool isDownloadingBundle = false;
        private bool isDownloadingMetadata = false;
        private bool isFetchingStatus = false;
        private string statusMessage = "";
        private string offlineBundleStatus = "";
        private string downloadProgress = "";
        private bool showNotActiveAlert = false;

        // MapSet download tracking
        private List<VpsMap> mapsToDownload = new List<VpsMap>();
        private int currentDownloadIndex = 0;
        private int totalMapsToDownload = 0;
        private bool isDownloadingMapSet = false;

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            OnDeviceLocalizationManager manager = (OnDeviceLocalizationManager)target;

            // Only show editor download UI if Editor mode is selected
            if (manager.offlineBundleDownloadMode == OfflineBundleDownloadMode.Editor)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("Offline Bundle Management", EditorStyles.boldLabel);
                EditorGUILayout.Space(10);

                // Combined button to get status and download offline bundles & metadata
                GUI.enabled = !isFetchingStatus && !isDownloadingBundle && !isDownloadingMetadata && !isDownloadingMapSet;

                if (GUILayout.Button("Get Map or MapSet offline Bundles & Metadata", GUILayout.Height(30)))
                {
                    // Reset states
                    showNotActiveAlert = false;

                    if (manager.localizationType == LocalizationType.Map)
                    {
                        manager.mapCode = manager.mapOrMapsetCode;
                        manager.mapSetCode = string.Empty;
                    }
                    else
                    {
                        manager.mapCode = string.Empty;
                        manager.mapSetCode = manager.mapOrMapsetCode;
                    }

                    GetMapOrMapSetStatus(manager);
                }
                GUI.enabled = true;

                // Show alert when offline bundles are not active
                if (showNotActiveAlert)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox("Offline Bundle is not active or not available for this map/mapset. Please ensure the offline bundle is enabled and processed on the server before downloading.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox("Runtime download mode is selected. Offline bundles will be downloaded at runtime on the device.", MessageType.Info);
                EditorGUILayout.Space(10);
            }

            // Display status message
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(10);
                MessageType messageType = MessageType.Info;
                if (statusMessage.StartsWith("Error:"))
                {
                    messageType = MessageType.Error;
                }
                else if (statusMessage.Contains("successfully") || statusMessage.Contains("complete"))
                {
                    messageType = MessageType.Info;
                }
                EditorGUILayout.HelpBox(statusMessage, messageType);
            }

            // Show download progress
            if (!string.IsNullOrEmpty(downloadProgress))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Progress:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(downloadProgress);
            }

            // Display offline bundle status info when loaded
            if (isMapDataLoaded && !string.IsNullOrEmpty(offlineBundleStatus) && !isFetchingStatus)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Offline Bundle Status:", EditorStyles.boldLabel);

                // Color the status based on active/inactive
                GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
                if (offlineBundleStatus.ToLower() == "active")
                {
                    statusStyle.normal.textColor = new Color(0.2f, 0.7f, 0.2f);
                }
                else
                {
                    statusStyle.normal.textColor = new Color(0.8f, 0.4f, 0.1f);
                }
                EditorGUILayout.LabelField(offlineBundleStatus, statusStyle);

                // Show MapSet info if applicable
                if (mapSet != null && mapSet.mapSetData != null)
                {
                    EditorGUILayout.Space(5);
                    int totalMaps = mapSet.mapSetData.Count;
                    int activeMaps = mapsToDownload.Count;
                    EditorGUILayout.HelpBox($"MapSet contains {totalMaps} map(s). {activeMaps} map(s) have active offline bundles.", MessageType.Info);
                }
            }

            EditorGUILayout.Space(10);
        }

        private void GetMapOrMapSetStatus(OnDeviceLocalizationManager manager)
        {
            isFetchingStatus = true;
            statusMessage = "Fetching map/MapSet details...";
            downloadProgress = "";
            isMapDataLoaded = false;
            offlineBundleStatus = "";
            vpsMap = null;
            mapSet = null;
            mapsToDownload.Clear();
            Repaint();

            // Check if mapCode is provided
            if (!string.IsNullOrEmpty(manager.mapCode))
            {
                MultiSetApiManager.GetMapDetails(manager.mapCode, (success, data, statusCode) =>
                {
                    MapDetailsCallback(success, data, statusCode, manager);
                });
            }
            // Check if mapSetCode is provided
            else if (!string.IsNullOrEmpty(manager.mapSetCode))
            {
                MultiSetApiManager.GetMapSetDetails(manager.mapSetCode, (success, data, statusCode) =>
                {
                    MapSetDetailsCallback(success, data, statusCode, manager);
                });
            }
            else
            {
                isFetchingStatus = false;
                statusMessage = "Error: Please provide either a Map Code or MapSet Code!";
                Debug.LogError("Map or MapSet Code is missing!");
                Repaint();
            }
        }

        private void MapDetailsCallback(bool success, string data, long statusCode, OnDeviceLocalizationManager manager)
        {
            isFetchingStatus = false;

            if (string.IsNullOrEmpty(data))
            {
                statusMessage = "Error: Empty or null data received from API!";
                Debug.LogError("Map Details Callback: Empty or null data received!");
                Repaint();
                return;
            }

            if (success)
            {
                try
                {
                    vpsMap = JsonUtility.FromJson<VpsMap>(data);
                    isMapDataLoaded = true;
                    offlineBundleStatus = vpsMap.offlineBundleStatus ?? "Not Available";

                    // Check if offline bundle is active and auto-download
                    if (offlineBundleStatus.ToLower() == "active")
                    {
                        showNotActiveAlert = false;
                        statusMessage = $"Map '{vpsMap.mapName}' has active offline bundle. Starting download...";
                        Repaint();

                        // Auto-download the bundle and metadata
                        DownloadOfflineBundle(manager);
                    }
                    else
                    {
                        showNotActiveAlert = true;
                        statusMessage = $"Map '{vpsMap.mapName}' loaded. Offline bundle status: {offlineBundleStatus}";
                    }
                }
                catch (Exception e)
                {
                    statusMessage = $"Error parsing map data: {e.Message}";
                    Debug.LogError($"Error parsing map data: {e.Message}");
                }
            }
            else
            {
                statusMessage = $"Error: Failed to get map details. Status Code: {statusCode}";
                Debug.LogError($"Get Map Details failed! Status Code: {statusCode}");
            }

            Repaint();
        }

        private void MapSetDetailsCallback(bool success, string data, long statusCode, OnDeviceLocalizationManager manager)
        {
            isFetchingStatus = false;

            if (string.IsNullOrEmpty(data))
            {
                statusMessage = "Error: Empty or null data received from API!";
                Debug.LogError("MapSet Details Callback: Empty or null data received!");
                Repaint();
                return;
            }

            if (success)
            {
                try
                {
                    MapSetResult mapSetResult = JsonUtility.FromJson<MapSetResult>(data);
                    mapSet = mapSetResult.mapSet;
                    isMapDataLoaded = true;

                    // Clear previous download list
                    mapsToDownload.Clear();

                    // For MapSet, we'll check the first map's offline bundle status
                    if (mapSet.mapSetData != null && mapSet.mapSetData.Count > 0)
                    {
                        vpsMap = mapSet.mapSetData[0].map;
                        offlineBundleStatus = vpsMap.offlineBundleStatus ?? "Not Available";
                        int totalMaps = mapSet.mapSetData.Count;

                        // Build list of maps with active offline bundles
                        foreach (var mapSetData in mapSet.mapSetData)
                        {
                            if (mapSetData.map != null)
                            {
                                string bundleStatus = mapSetData.map.offlineBundleStatus ?? "Not Available";
                                if (bundleStatus.ToLower() == "active")
                                {
                                    mapsToDownload.Add(mapSetData.map);
                                }
                            }
                        }

                        if (mapsToDownload.Count > 0)
                        {
                            offlineBundleStatus = "active";
                            showNotActiveAlert = false;
                            statusMessage = $"MapSet '{mapSet.name}' has {mapsToDownload.Count}/{totalMaps} map(s) with active offline bundles. Starting download...";
                            Repaint();

                            // Auto-download all bundles and metadata
                            DownloadMapSetBundles(manager);
                        }
                        else
                        {
                            showNotActiveAlert = true;
                            offlineBundleStatus = "Not Active";
                            statusMessage = $"MapSet '{mapSet.name}' loaded. No maps have active offline bundles.";
                        }
                    }
                    else
                    {
                        showNotActiveAlert = true;
                        statusMessage = "MapSet loaded but contains no maps!";
                        Debug.LogWarning("MapSet loaded but contains no maps!");
                    }
                }
                catch (Exception e)
                {
                    statusMessage = $"Error parsing MapSet data: {e.Message}";
                    Debug.LogError($"Error parsing MapSet data: {e.Message}");
                }
            }
            else
            {
                statusMessage = $"Error: Failed to get MapSet details. Status Code: {statusCode}";
                Debug.LogError($"Get MapSet Details failed! Status Code: {statusCode}");
            }

            Repaint();
        }

        private void DownloadMapSetBundles(OnDeviceLocalizationManager manager)
        {
            if (mapsToDownload.Count == 0)
            {
                statusMessage = "Error: No maps with active offline bundles found in this MapSet!";
                Debug.LogError("No maps to download!");
                Repaint();
                return;
            }

            isDownloadingMapSet = true;
            currentDownloadIndex = 0;
            totalMapsToDownload = mapsToDownload.Count;

            statusMessage = $"Starting download of {totalMapsToDownload} map(s)...";
            Repaint();

            // Start downloading the first map
            DownloadNextMapInSet(manager);
        }

        private void DownloadNextMapInSet(OnDeviceLocalizationManager manager)
        {
            if (currentDownloadIndex >= mapsToDownload.Count)
            {
                // All map bundles downloaded, now download MapSet metadata
                downloadProgress = "Downloading MapSet metadata...";
                Repaint();
                DownloadMapSetMetadataFile(manager);
                return;
            }

            VpsMap currentMap = mapsToDownload[currentDownloadIndex];
            downloadProgress = $"Downloading map {currentDownloadIndex + 1}/{totalMapsToDownload}: {currentMap.mapName}...";
            Repaint();

            // Download bundle for this map
            DownloadMapSetOfflineBundle(manager, currentMap);
        }

        private void DownloadMapSetOfflineBundle(OnDeviceLocalizationManager manager, VpsMap mapToDownload)
        {
            if (mapToDownload == null || string.IsNullOrEmpty(mapToDownload.offlineBundle))
            {
                Debug.LogError($"Map has no offline bundle key! Skipping...");
                // Move to next map
                currentDownloadIndex++;
                DownloadNextMapInSet(manager);
                return;
            }

            // Check if offline bundle file already exists
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            string mapDataPath = Path.Combine(streamingAssetsPath, "MapData", manager.mapSetCode);
            string existingFilePath = Path.Combine(mapDataPath, $"{mapToDownload.mapCode}.bytes");

            if (File.Exists(existingFilePath))
            {
                // Bundle exists, move to next map
                currentDownloadIndex++;
                DownloadNextMapInSet(manager);
                return;
            }

            MultiSetApiManager.GetFileUrl(mapToDownload.offlineBundle, (success, data, statusCode) =>
            {
                MapSetBundleUrlCallback(success, data, statusCode, manager, mapToDownload);
            });
        }

        private void MapSetBundleUrlCallback(bool success, string data, long statusCode, OnDeviceLocalizationManager manager, VpsMap mapToDownload)
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogError($"Failed to get file URL for {mapToDownload.mapCode}");
                // Move to next map
                currentDownloadIndex++;
                DownloadNextMapInSet(manager);
                return;
            }

            if (success)
            {
                try
                {
                    FileData fileData = JsonUtility.FromJson<FileData>(data);

                    // Create StreamingAssets/MapData/{mapSetCode} path
                    string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                    if (!Directory.Exists(streamingAssetsPath))
                    {
                        Directory.CreateDirectory(streamingAssetsPath);
                    }

                    string mapDataPath = Path.Combine(streamingAssetsPath, "MapData", manager.mapSetCode);
                    if (!Directory.Exists(mapDataPath))
                    {
                        Directory.CreateDirectory(mapDataPath);
                    }

                    string savePath = Path.Combine(mapDataPath, $"{mapToDownload.mapCode}.bytes");

                    // Download file
                    MultiSetHttpClient.DownloadFileAsync(fileData.url, (byte[] fileBytes) =>
                    {
                        if (fileBytes != null && fileBytes.Length > 0)
                        {
                            try
                            {
                                File.WriteAllBytes(savePath, fileBytes);
                                // Refresh the Asset Database
                                AssetDatabase.Refresh();

                                // Move to next map (no individual map metadata needed)
                                currentDownloadIndex++;
                                DownloadNextMapInSet(manager);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Failed to save offline bundle file for {mapToDownload.mapCode}: {e.Message}");
                                currentDownloadIndex++;
                                DownloadNextMapInSet(manager);
                            }
                        }
                        else
                        {
                            Debug.LogError($"Failed to download offline bundle file for {mapToDownload.mapCode}");
                            currentDownloadIndex++;
                            DownloadNextMapInSet(manager);
                        }
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing file URL for {mapToDownload.mapCode}: {e.Message}");
                    currentDownloadIndex++;
                    DownloadNextMapInSet(manager);
                }
            }
            else
            {
                Debug.LogError($"Failed to get file URL for {mapToDownload.mapCode}. Status Code: {statusCode}");
                currentDownloadIndex++;
                DownloadNextMapInSet(manager);
            }
        }

        private void DownloadMapSetMetadataFile(OnDeviceLocalizationManager manager)
        {
            string accessTokenJson = PlayerPrefs.GetString(PlayerPrefsKey.AccessToken);
            if (string.IsNullOrEmpty(accessTokenJson))
            {
                Debug.LogError("Authentication token not found!");
                isDownloadingMapSet = false;
                statusMessage = "Error: Authentication token not found!";
                downloadProgress = "";
                Repaint();
                return;
            }

            AccessToken accessToken = JsonUtility.FromJson<AccessToken>(accessTokenJson);
            string metadataUrl = $"{API.baseUrl}/v1/vps/map-set/process-offline-metadata/{manager.mapSetCode}";

            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken.token}");

            var downloadTask = httpClient.PostAsync(metadataUrl, null);
            downloadTask.ContinueWith(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    var response = task.Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var readTask = response.Content.ReadAsByteArrayAsync();
                        readTask.ContinueWith(readTaskResult =>
                        {
                            if (readTaskResult.IsCompleted && !readTaskResult.IsFaulted)
                            {
                                byte[] fileBytes = readTaskResult.Result;

                                // Save to StreamingAssets/MapData/{mapSetCode}
                                string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                                if (!Directory.Exists(streamingAssetsPath))
                                {
                                    Directory.CreateDirectory(streamingAssetsPath);
                                }

                                string mapDataPath = Path.Combine(streamingAssetsPath, "MapData", manager.mapSetCode);
                                if (!Directory.Exists(mapDataPath))
                                {
                                    Directory.CreateDirectory(mapDataPath);
                                }

                                string metadataFileName = $"{manager.mapSetCode}_metadata.bytes";
                                string savePath = Path.Combine(mapDataPath, metadataFileName);

                                try
                                {
                                    File.WriteAllBytes(savePath, fileBytes);

                                    // Update on main thread
                                    EditorApplication.delayCall += () =>
                                    {
                                        AssetDatabase.Refresh();

                                        // All done!
                                        isDownloadingMapSet = false;
                                        statusMessage = $"All {totalMapsToDownload} map(s) and MapSet metadata downloaded successfully!";
                                        downloadProgress = "Download complete!";
                                        Repaint();
                                    };
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"Failed to save MapSet metadata file: {e.Message}");
                                    EditorApplication.delayCall += () =>
                                    {
                                        isDownloadingMapSet = false;
                                        statusMessage = $"Error saving MapSet metadata: {e.Message}";
                                        downloadProgress = "";
                                        Repaint();
                                    };
                                }
                            }
                            else
                            {
                                Debug.LogError($"Failed to read MapSet metadata content");
                                EditorApplication.delayCall += () =>
                                {
                                    isDownloadingMapSet = false;
                                    statusMessage = "Error: Failed to read MapSet metadata content";
                                    downloadProgress = "";
                                    Repaint();
                                };
                            }
                        });
                    }
                    else
                    {
                        Debug.LogError($"MapSet metadata download failed with status: {response.StatusCode}");
                        EditorApplication.delayCall += () =>
                        {
                            isDownloadingMapSet = false;
                            statusMessage = $"Error: MapSet metadata download failed! Status: {response.StatusCode}";
                            downloadProgress = "";
                            Repaint();
                        };
                    }
                }
                else
                {
                    Debug.LogError($"MapSet metadata download request failed: {task.Exception?.Message}");
                    EditorApplication.delayCall += () =>
                    {
                        isDownloadingMapSet = false;
                        statusMessage = "Error: MapSet metadata download request failed!";
                        downloadProgress = "";
                        Repaint();
                    };
                }
            });
        }

        private void DownloadOfflineBundle(OnDeviceLocalizationManager manager)
        {
            if (vpsMap == null || string.IsNullOrEmpty(vpsMap.offlineBundle))
            {
                statusMessage = "Error: No offline bundle key available!";
                Debug.LogError("No offline bundle key available!");
                Repaint();
                return;
            }

            // Check if offline bundle file already exists
            string mapCode = !string.IsNullOrEmpty(manager.mapCode) ? manager.mapCode : vpsMap.mapCode;
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            string mapDataPath = Path.Combine(streamingAssetsPath, "MapData", mapCode);
            string existingFilePath = Path.Combine(mapDataPath, $"{mapCode}.bytes");

            if (File.Exists(existingFilePath))
            {
                downloadProgress = $"Bundle exists, downloading metadata...";
                Repaint();

                // Bundle exists, but still download metadata
                DownloadOfflineMapMetadata(manager);
                return;
            }

            isDownloadingBundle = true;
            downloadProgress = "Fetching download URL...";
            Repaint();

            MultiSetApiManager.GetFileUrl(vpsMap.offlineBundle, OfflineBundleUrlCallback);
        }

        private void OfflineBundleUrlCallback(bool success, string data, long statusCode)
        {
            if (string.IsNullOrEmpty(data))
            {
                isDownloadingBundle = false;
                downloadProgress = "";
                statusMessage = "Error: Empty or null data received from file URL API!";
                Debug.LogError("File URL Callback: Empty or null data received!");
                Repaint();
                return;
            }

            if (success)
            {
                try
                {
                    FileData fileData = JsonUtility.FromJson<FileData>(data);

                    downloadProgress = "Downloading offline bundle...";
                    Repaint();

                    // Determine the save path
                    OnDeviceLocalizationManager manager = (OnDeviceLocalizationManager)target;
                    string mapCode = !string.IsNullOrEmpty(manager.mapCode) ? manager.mapCode : vpsMap.mapCode;

                    // Create StreamingAssets/MapData/{mapCode} path
                    string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                    if (!Directory.Exists(streamingAssetsPath))
                    {
                        Directory.CreateDirectory(streamingAssetsPath);
                    }

                    string mapDataPath = Path.Combine(streamingAssetsPath, "MapData", mapCode);
                    if (!Directory.Exists(mapDataPath))
                    {
                        Directory.CreateDirectory(mapDataPath);
                    }

                    string savePath = Path.Combine(mapDataPath, $"{mapCode}.bytes");

                    // Download file
                    MultiSetHttpClient.DownloadFileAsync(fileData.url, (byte[] fileBytes) =>
                    {
                        if (fileBytes != null && fileBytes.Length > 0)
                        {
                            try
                            {
                                File.WriteAllBytes(savePath, fileBytes);

                                isDownloadingBundle = false;
                                downloadProgress = $"Bundle downloaded! Now downloading metadata...";
                                statusMessage = $"Offline bundle downloaded successfully! ({fileBytes.Length / 1024} KB)";

                                // Refresh the Asset Database to make Unity recognize the new file
                                AssetDatabase.Refresh();

                                Repaint();

                                // Automatically download metadata after bundle download completes
                                OnDeviceLocalizationManager mgr = (OnDeviceLocalizationManager)target;
                                DownloadOfflineMapMetadata(mgr);
                            }
                            catch (Exception e)
                            {
                                isDownloadingBundle = false;
                                downloadProgress = "";
                                statusMessage = $"Error saving file: {e.Message}";
                                Debug.LogError($"Failed to save offline bundle file: {e.Message}");
                                Repaint();
                            }
                        }
                        else
                        {
                            isDownloadingBundle = false;
                            downloadProgress = "";
                            statusMessage = "Error: Failed to download offline bundle file!";
                            Debug.LogError("Failed to download offline bundle file.");
                            Repaint();
                        }
                    });
                }
                catch (Exception e)
                {
                    isDownloadingBundle = false;
                    downloadProgress = "";
                    statusMessage = $"Error: {e.Message}";
                    Debug.LogError($"Error parsing file URL: {e.Message}");
                    Repaint();
                }
            }
            else
            {
                isDownloadingBundle = false;
                downloadProgress = "";

                try
                {
                    ErrorJSON errorJSON = JsonUtility.FromJson<ErrorJSON>(data);
                    statusMessage = $"Error: {errorJSON.error}";
                    Debug.LogError($"Error getting file URL: {errorJSON.error}");
                }
                catch
                {
                    statusMessage = $"Failed to get file URL. Status Code: {statusCode}";
                    Debug.LogError($"Failed to get file URL. Status Code: {statusCode}");
                }

                Repaint();
            }
        }

        private void DownloadOfflineMapMetadata(OnDeviceLocalizationManager manager)
        {
            if (vpsMap == null)
            {
                statusMessage = "Error: No map data available!";
                Debug.LogError("No map data available!");
                Repaint();
                return;
            }

            string mapCode = !string.IsNullOrEmpty(manager.mapCode) ? manager.mapCode : vpsMap.mapCode;

            if (string.IsNullOrEmpty(mapCode))
            {
                statusMessage = "Error: Map code is missing!";
                Debug.LogError("Map code is missing!");
                Repaint();
                return;
            }

            // Always download metadata file (no file existence check - it's a small file)
            isDownloadingMetadata = true;
            statusMessage = "Downloading offline map metadata...";
            Repaint();

            // Create the POST request endpoint
            string metadataEndpoint = $"/v1/vps/map/process-offline-metadata/{mapCode}";

            // Make the API request
            MultiSetApiManager.ApiRequest(Method.POST, metadataEndpoint, "", (bool success, string data, long statusCode) =>
            {
                if (success)
                {
                    // The response is binary data (bytes), not JSON
                    // We need to download it directly
                    DownloadMetadataFile(mapCode);
                }
                else
                {
                    isDownloadingMetadata = false;
                    statusMessage = $"Error: Failed to process metadata request. Status Code: {statusCode}";
                    downloadProgress = "";
                    Debug.LogError($"Failed to process metadata request. Status Code: {statusCode}");
                    Repaint();
                }
            }, true);
        }

        private void DownloadMetadataFile(string mapCode)
        {
            // Get the token for authorization
            string accessTokenJson = PlayerPrefs.GetString(PlayerPrefsKey.AccessToken);
            if (string.IsNullOrEmpty(accessTokenJson))
            {
                isDownloadingMetadata = false;
                statusMessage = "Error: Authentication token not found!";
                downloadProgress = "";
                Debug.LogError("Authentication token not found!");
                Repaint();
                return;
            }

            AccessToken accessToken = JsonUtility.FromJson<AccessToken>(accessTokenJson);
            string metadataUrl = $"{API.baseUrl}/v1/vps/map/process-offline-metadata/{mapCode}";

            // Download the metadata file using HTTP client
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken.token}");

            var downloadTask = httpClient.PostAsync(metadataUrl, null);
            downloadTask.ContinueWith(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    var response = task.Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var readTask = response.Content.ReadAsByteArrayAsync();
                        readTask.ContinueWith(readTaskResult =>
                        {
                            if (readTaskResult.IsCompleted && !readTaskResult.IsFaulted)
                            {
                                byte[] fileBytes = readTaskResult.Result;

                                // Save to StreamingAssets/MapData/{mapCode}
                                string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                                if (!Directory.Exists(streamingAssetsPath))
                                {
                                    Directory.CreateDirectory(streamingAssetsPath);
                                }

                                string mapDataPath = Path.Combine(streamingAssetsPath, "MapData", mapCode);
                                if (!Directory.Exists(mapDataPath))
                                {
                                    Directory.CreateDirectory(mapDataPath);
                                }

                                string metadataFileName = $"{mapCode}_metadata.bytes";
                                string savePath = Path.Combine(mapDataPath, metadataFileName);

                                try
                                {
                                    File.WriteAllBytes(savePath, fileBytes);

                                    // Update on main thread
                                    EditorApplication.delayCall += () =>
                                    {
                                        isDownloadingMetadata = false;
                                        statusMessage = $"Metadata downloaded successfully! ({fileBytes.Length / 1024} KB)";
                                        downloadProgress = "Download complete!";
                                        AssetDatabase.Refresh();
                                        Repaint();
                                    };
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"Failed to save metadata file: {e.Message}");
                                    EditorApplication.delayCall += () =>
                                    {
                                        isDownloadingMetadata = false;
                                        statusMessage = $"Error saving metadata: {e.Message}";
                                        downloadProgress = "";
                                        Repaint();
                                    };
                                }
                            }
                            else
                            {
                                Debug.LogError($"Failed to read metadata content: {readTaskResult.Exception?.Message}");
                                EditorApplication.delayCall += () =>
                                {
                                    isDownloadingMetadata = false;
                                    statusMessage = "Error: Failed to read metadata content!";
                                    downloadProgress = "";
                                    Repaint();
                                };
                            }
                        });
                    }
                    else
                    {
                        Debug.LogError($"Metadata download failed with status: {response.StatusCode}");
                        EditorApplication.delayCall += () =>
                        {
                            isDownloadingMetadata = false;
                            statusMessage = $"Error: Metadata download failed! Status: {response.StatusCode}";
                            downloadProgress = "";
                            Repaint();
                        };
                    }
                }
                else
                {
                    Debug.LogError($"Metadata download request failed: {task.Exception?.Message}");
                    EditorApplication.delayCall += () =>
                    {
                        isDownloadingMetadata = false;
                        statusMessage = $"Error: Metadata download request failed!";
                        downloadProgress = "";
                        Repaint();
                    };
                }
            });
        }
    }
}
#endif
