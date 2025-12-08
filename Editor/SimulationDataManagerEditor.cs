/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace MultiSet
{
    [CustomEditor(typeof(SimulationDataManager))]
    public class SimulationDataManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SimulationDataManager simulation = (SimulationDataManager)target;

            GUILayout.Space(15);

            // Title with styling
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Simulation Data Manager", titleStyle);

            GUILayout.Space(10);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Space(5);

            // First button - Get simulation data list
            EditorGUILayout.LabelField("Step 1: Fetch Available Simulations", EditorStyles.miniBoldLabel);
            GUILayout.Space(5);

            GUIContent buttonContent = new GUIContent("Get Simulation Data List", "Fetch available simulation data from server.");

            if (simulation.isDownloading)
            {
                GUI.backgroundColor = new Color(1f, 0.8f, 0.2f); // Orange
                buttonContent.text = "Fetching Simulation Data...";
                GUI.enabled = false;

                // Progress indicator
                Rect progressRect = GUILayoutUtility.GetRect(0, 4);
                EditorGUI.ProgressBar(progressRect, Mathf.PingPong(Time.realtimeSinceStartup, 1f), "");
            }
            else
            {
                GUI.backgroundColor = new Color(0.6f, 0.8f, 1f); // Light blue
                buttonContent.text = "Get Simulation Data List";
                GUI.enabled = true;
            }

            if (GUILayout.Button(buttonContent, GUILayout.Height(35)))
            {
                if (!simulation.isDownloading)
                {
                    simulation.GetSimulationData();
                }
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            GUILayout.Space(15);

            // Show dropdown if simulation data is available
            if (simulation.simulationDataResponse != null &&
                simulation.simulationDataResponse.simulationData != null &&
                simulation.simulationDataResponse.simulationData.Count > 0)
            {
                EditorGUILayout.LabelField("Step 2: Select & Download Simulation", EditorStyles.miniBoldLabel);
                GUILayout.Space(5);

                EditorGUILayout.HelpBox($"Found {simulation.simulationDataResponse.simulationData.Count} simulation(s). ✓ indicates already downloaded.", MessageType.Info);
                GUILayout.Space(8);

                List<string> simulationNames = simulation.GetSimulationNames();
                string[] options = simulationNames.ToArray();

                // Add "Select Simulation..." as first option if nothing selected
                if (simulation.selectedSimulationIndex < 0)
                {
                    string[] optionsWithDefault = new string[options.Length + 1];
                    optionsWithDefault[0] = "Select Simulation...";
                    System.Array.Copy(options, 0, optionsWithDefault, 1, options.Length);
                    options = optionsWithDefault;

                    simulation.selectedSimulationIndex = EditorGUILayout.Popup("Available Simulations:", 0, options) - 1;
                }
                else
                {
                    simulation.selectedSimulationIndex = EditorGUILayout.Popup("Available Simulations:", simulation.selectedSimulationIndex, options);
                }

                GUILayout.Space(8);

                // Show alert if file already exists
                if (simulation.fileAlreadyExists && simulation.alertTimer > 0)
                {
                    EditorGUILayout.HelpBox(simulation.alertMessage, MessageType.Warning);
                    GUILayout.Space(5);
                }

                // Download button with enhanced states
                GUIContent downloadButtonContent = new GUIContent("Download Selected Simulation", "Download the selected simulation zip file and extract it.");
                string buttonText = "Download Selected Simulation";
                Color buttonColor = new Color(0.2f, 0.8f, 0.4f); // Green
                bool showProgress = false;

                if (simulation.isDownloadingFile)
                {
                    buttonText = "Downloading File...";
                    buttonColor = new Color(0.2f, 0.6f, 1f); // Blue
                    GUI.enabled = false;
                    showProgress = true;
                }
                else if (simulation.isExtractingFile)
                {
                    buttonText = "Extracting & Processing...";
                    buttonColor = new Color(1f, 0.6f, 0.2f); // Orange
                    GUI.enabled = false;
                    showProgress = true;
                }
                else if (simulation.selectedSimulationIndex < 0)
                {
                    buttonText = "Select a Simulation First";
                    buttonColor = Color.gray;
                    GUI.enabled = false;
                }
                else
                {
                    // Check if selected simulation is already downloaded
                    if (simulation.selectedSimulationIndex >= 0 && simulation.selectedSimulationIndex < simulation.simulationDataResponse.simulationData.Count)
                    {
                        var selectedSim = simulation.simulationDataResponse.simulationData[simulation.selectedSimulationIndex];
                        string dataDir = Path.Combine(Application.persistentDataPath, "SimulationData", selectedSim.simulationCode);
                        bool alreadyExists = Directory.Exists(dataDir) &&
                                           Directory.GetFiles(dataDir, "*.jpg").Length > 0 &&
                                           Directory.GetFiles(dataDir, "*.json").Length > 0;

                        if (alreadyExists)
                        {
                            buttonText = "Re-download Simulation";
                            buttonColor = new Color(1f, 0.8f, 0.2f); // Yellow-orange
                        }
                        else
                        {
                            buttonText = "Download Selected Simulation";
                            buttonColor = new Color(0.2f, 0.8f, 0.4f); // Green
                        }
                        GUI.enabled = true;
                    }
                    else
                    {
                        buttonText = "Select a Simulation First";
                        buttonColor = Color.gray;
                        GUI.enabled = false;
                    }
                }

                downloadButtonContent.text = buttonText;
                GUI.backgroundColor = buttonColor;

                if (GUILayout.Button(downloadButtonContent, GUILayout.Height(40)))
                {
                    if (simulation.selectedSimulationIndex < 0)
                    {
                        EditorUtility.DisplayDialog("No Selection", "Please select a simulation from the dropdown first.", "OK");
                    }
                    else if (!simulation.isDownloadingFile && !simulation.isExtractingFile)
                    {
                        simulation.DownloadSelectedSimulation();
                    }
                }

                // Progress bar for download/extract operations
                if (showProgress)
                {
                    GUILayout.Space(5);
                    Rect progressRect = GUILayoutUtility.GetRect(0, 6);
                    EditorGUI.ProgressBar(progressRect, Mathf.PingPong(Time.realtimeSinceStartup * 2, 1f), "");
                }

                GUI.enabled = true;
                GUI.backgroundColor = Color.white;

                GUILayout.Space(10);
                EditorGUILayout.EndVertical();
            }
            else if (!simulation.isDownloading)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Click 'Get Simulation Data List' to fetch available simulations from the server.", MessageType.Info);
                GUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }

            // Force repaint during operations for smooth UI updates
            if (simulation.isDownloading || simulation.isDownloadingFile || simulation.isExtractingFile || simulation.alertTimer > 0)
            {
                Repaint();
            }
        }
    }
}
#endif