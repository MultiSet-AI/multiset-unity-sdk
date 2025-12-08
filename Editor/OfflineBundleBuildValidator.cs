/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace MultiSet
{
    /// <summary>
    /// Build preprocessor that validates offline bundles exist in StreamingAssets
    /// when OfflineBundleDownloadMode.Editor is selected.
    /// Stops the build process with a clear error message if bundles are missing.
    /// </summary>
    public class OfflineBundleBuildValidator : IPreprocessBuildWithReport
    {
        // Run early in the build process
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            // Get all scenes in build settings
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

            // Store current scene path to restore later
            string currentScenePath = SceneManager.GetActiveScene().path;

            try
            {
                foreach (EditorBuildSettingsScene buildScene in buildScenes)
                {
                    // Skip disabled scenes
                    if (!buildScene.enabled)
                        continue;

                    string scenePath = buildScene.path;

                    // Skip if scene doesn't exist
                    if (!File.Exists(scenePath))
                        continue;

                    // Open scene additively to check for OnDeviceLocalizationManager
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                    try
                    {
                        // Find all OnDeviceLocalizationManager components in the scene
                        GameObject[] rootObjects = scene.GetRootGameObjects();

                        foreach (GameObject rootObj in rootObjects)
                        {
                            OnDeviceLocalizationManager[] managers = rootObj.GetComponentsInChildren<OnDeviceLocalizationManager>(true);

                            foreach (OnDeviceLocalizationManager manager in managers)
                            {
                                ValidateManager(manager, scenePath, errors, warnings);
                            }
                        }
                    }
                    finally
                    {
                        // Close the additively loaded scene
                        if (scene.isLoaded && scene.path != currentScenePath)
                        {
                            EditorSceneManager.CloseScene(scene, true);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MultiSet Build Validator] Error during validation: {e.Message}");
            }

            // Show warnings
            foreach (string warning in warnings)
            {
                Debug.LogWarning(warning);
            }

            // If there are errors, stop the build
            if (errors.Count > 0)
            {
                string errorMessage = "[MultiSet Build Validator] Build stopped due to missing offline bundles!\n\n";
                errorMessage += string.Join("\n\n", errors);
                errorMessage += "\n\n--- HOW TO FIX ---\n";
                errorMessage += "1. Open the scene with OnDeviceLocalizationManager\n";
                errorMessage += "2. Select the GameObject with OnDeviceLocalizationManager component\n";
                errorMessage += "3. Click 'Get Map Or MapSet Status' button\n";
                errorMessage += "4. Click 'Download Offline Bundle & Metadata' button\n";
                errorMessage += "5. Wait for download to complete, then rebuild\n";
                errorMessage += "\nAlternatively, change 'Offline Bundle Download Mode' to 'Runtime' to download on device.";

                Debug.LogError(errorMessage);
                throw new BuildFailedException(errorMessage);
            }
        }

        private void ValidateManager(OnDeviceLocalizationManager manager, string scenePath, List<string> errors, List<string> warnings)
        {
            // Only validate if Editor mode is selected
            if (manager.offlineBundleDownloadMode != OfflineBundleDownloadMode.Editor)
            {
                return;
            }

            string mapOrMapsetCode = manager.mapOrMapsetCode;

            // Check if map/mapset code is provided
            if (string.IsNullOrWhiteSpace(mapOrMapsetCode))
            {
                warnings.Add($"[Scene: {Path.GetFileName(scenePath)}] OnDeviceLocalizationManager has no Map/MapSet code configured.");
                return;
            }

            string streamingAssetsPath = Application.dataPath + "/StreamingAssets";
            string mapDataPath = Path.Combine(streamingAssetsPath, "MapData", mapOrMapsetCode);

            // Check if the folder exists
            if (!Directory.Exists(mapDataPath))
            {
                errors.Add($"[Scene: {Path.GetFileName(scenePath)}]\n" +
                          $"  - Mode: Editor (pre-download)\n" +
                          $"  - {(manager.localizationType == LocalizationType.Map ? "Map Code" : "MapSet Code")}: {mapOrMapsetCode}\n" +
                          $"  - Expected Path: StreamingAssets/MapData/{mapOrMapsetCode}/\n" +
                          $"  - ERROR: Offline bundle folder not found!");
                return;
            }

            // Check for bundle files based on localization type
            if (manager.localizationType == LocalizationType.Map)
            {
                ValidateSingleMapBundle(mapOrMapsetCode, mapDataPath, scenePath, errors, warnings);
            }
            else
            {
                ValidateMapSetBundle(mapOrMapsetCode, mapDataPath, scenePath, errors, warnings);
            }
        }

        private void ValidateSingleMapBundle(string mapCode, string mapDataPath, string scenePath, List<string> errors, List<string> warnings)
        {
            string bundlePath = Path.Combine(mapDataPath, $"{mapCode}.bytes");
            string metadataPath = Path.Combine(mapDataPath, $"{mapCode}_metadata.bytes");

            bool bundleExists = File.Exists(bundlePath);
            bool metadataExists = File.Exists(metadataPath);

            if (!bundleExists)
            {
                errors.Add($"[Scene: {Path.GetFileName(scenePath)}]\n" +
                          $"  - Mode: Editor (pre-download)\n" +
                          $"  - Map Code: {mapCode}\n" +
                          $"  - ERROR: Offline bundle file not found!\n" +
                          $"  - Expected: StreamingAssets/MapData/{mapCode}/{mapCode}.bytes");
            }

            if (!metadataExists)
            {
                warnings.Add($"[Scene: {Path.GetFileName(scenePath)}] Metadata file not found for map '{mapCode}'. " +
                            $"Expected: StreamingAssets/MapData/{mapCode}/{mapCode}_metadata.bytes");
            }
        }

        private void ValidateMapSetBundle(string mapSetCode, string mapDataPath, string scenePath, List<string> errors, List<string> warnings)
        {
            // For MapSet, check if there are any .bytes files (excluding metadata)
            string[] bundleFiles = Directory.GetFiles(mapDataPath, "*.bytes");

            // Filter out metadata files
            List<string> actualBundleFiles = new List<string>();
            foreach (string file in bundleFiles)
            {
                string fileName = Path.GetFileName(file);
                if (!fileName.Contains("_metadata") && !fileName.Contains("license"))
                {
                    actualBundleFiles.Add(file);
                }
            }

            if (actualBundleFiles.Count == 0)
            {
                errors.Add($"[Scene: {Path.GetFileName(scenePath)}]\n" +
                          $"  - Mode: Editor (pre-download)\n" +
                          $"  - MapSet Code: {mapSetCode}\n" +
                          $"  - ERROR: No offline bundle files found in MapSet folder!\n" +
                          $"  - Expected: StreamingAssets/MapData/{mapSetCode}/*.bytes");
            }

            // Check for MapSet metadata
            string metadataPath = Path.Combine(mapDataPath, $"{mapSetCode}_metadata.bytes");
            if (!File.Exists(metadataPath))
            {
                warnings.Add($"[Scene: {Path.GetFileName(scenePath)}] MapSet metadata file not found for '{mapSetCode}'. " +
                            $"Expected: StreamingAssets/MapData/{mapSetCode}/{mapSetCode}_metadata.bytes");
            }
        }
    }
}
#endif
