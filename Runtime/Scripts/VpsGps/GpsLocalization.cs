/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MultiSet
{
    [Serializable]
    public class GpsLocalizationEvent : UnityEvent<bool, string> { }

    public class GpsLocalization : MonoBehaviour
    {
        [Header("Manual GPS Input (for Editor Testing)")]
        public double latitude;
        public double longitude;
        public float altitude;
        public double cameraHeadingDeg;

        [Header("GPS Sampling Settings")]
        [Tooltip("Number of GPS and heading samples to average for stable localization")]
        public int sampleCount = 40;

        [Tooltip("Interval between samples in seconds")]
        public float sampleInterval = 0.1f;

        [Tooltip("Minimum horizontal accuracy required (meters) - samples with worse accuracy will be filtered")]
        public float minAccuracyThreshold = 20f;

        [Tooltip("Wait time for GPS to stabilize before starting sampling (seconds)")]
        public float gpsWarmupTime = 2f;

        [Header("Events")]
        public GpsLocalizationEvent onLocalizationComplete;
        public UnityEvent onSamplingStarted;
        public UnityEvent onSamplingCompleted;

        public MapLocalizationManager mapLocalizationManager;
        public GameObject mapSpace;

        // Averaged GPS data
        private double averagedLatitude;
        private double averagedLongitude;
        private double averagedAltitude;
        private double averagedHeading;
        private float averagedAccuracy;

        private bool isCollectingSamples = false;
        private bool hasSampledData = false;

        // GPS quality metrics
        public float lastHorizontalAccuracy { get; private set; }
        public int validSamplesCollected { get; private set; }
        public int totalSamplesAttempted { get; private set; }
        private VpsMap vpsMap;


        private Vector3 camPos;
        private Quaternion camRot;
        public bool editorTest = false;
        public Camera arCamera; // Reference to AR camera


        void Start()
        {
            GpsCoordinateHandler.Instance?.EnableGpsHandler();

            mapLocalizationManager = FindFirstObjectByType<MapLocalizationManager>();
            if (mapLocalizationManager == null)
            {
                Debug.LogError("MapLocalizationManager not found in the scene.");
            }

            // Initialize GPS Handler
            InitializeGpsHandler();
        }


        private void InitializeGpsHandler()
        {
            // Check if GpsCoordinateHandler exists in scene
            if (GpsCoordinateHandler.Instance == null)
            {
                Debug.LogWarning("GpsCoordinateHandler not found in scene. Creating one...");
                GameObject gpsHandlerObj = new GameObject("GpsCoordinateHandler");
                gpsHandlerObj.AddComponent<GpsCoordinateHandler>();
            }

            // Enable GPS
            Debug.Log("Enabling GPS Handler...");
            GpsCoordinateHandler.Instance.EnableGpsHandler();

            // Wait a moment and check status
            StartCoroutine(CheckGpsStatus());
        }

        private IEnumerator CheckGpsStatus()
        {
            yield return new WaitForSeconds(2f);

            if (GpsCoordinateHandler.Instance.isGpsOn)
            {
                Debug.Log("GPS is now active!");
            }
            else
            {
                Debug.LogWarning("GPS failed to start. Check permissions and location services.");
            }
        }


        private void OnEnable()
        {
            EventManager<EventData>.StartListening("AuthCallBack", AuthCallBack);
        }

        private void OnDisable()
        {
            EventManager<EventData>.StopListening("AuthCallBack", AuthCallBack);
        }

        private void AuthCallBack(EventData @event)
        {
            if (@event.AuthSuccess)
            {
                Debug.Log("Auth Success. for GPS Localization");

                var mapCodeOrMapSetCode = mapLocalizationManager.mapOrMapsetCode;
                GetMapDetails(mapCodeOrMapSetCode);

            }
            else
            {
                Debug.LogError("Auth Failed!");

                ToastManager.Instance.ShowToast("Auth Failed!");
            }
        }

        private void GetMapDetails(string code)
        {
            MultiSetApiManager.GetMapDetails(code, MapDetailsCallback);
        }

        private void MapDetailsCallback(bool success, string data, long statusCode)
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogError("Error : Map Details Callback: Empty or null data received!");
                return;
            }

            if (success)
            {
                vpsMap = JsonUtility.FromJson<VpsMap>(data);
                Debug.Log("Map Location: " + vpsMap.location.coordinates[1] + ", " + vpsMap.location.coordinates[0] + ", " + vpsMap.location.coordinates[2]);
                Debug.Log("Map Heading: " + vpsMap.heading);
            }
            else
            {
                Debug.LogError("Get Map Details failed!");
            }
        }

        /// <summary>
        /// Starts collecting GPS and heading samples for averaging
        /// </summary>
        public void StartGpsSampling()
        {
            if (isCollectingSamples)
            {
                Debug.LogWarning("GPS sampling already in progress.");
                return;
            }

            if (GpsCoordinateHandler.Instance == null)
            {
                Debug.LogError("GpsCoordinateHandler instance not found. Cannot start GPS sampling.");
                return;
            }

            if (!GpsCoordinateHandler.Instance.isGpsOn)
            {
                Debug.LogError("GPS is not enabled. Please enable GPS first.");
                return;
            }

            StartCoroutine(CollectGpsSamples());
        }

        /// <summary>
        /// Stops any ongoing sampling or continuous tracking
        /// </summary>
        public void StopGpsLocalization()
        {
            StopAllCoroutines();
            isCollectingSamples = false;
            Debug.Log("GPS localization stopped.");
        }

        /// <summary>
        /// Gets current GPS signal quality
        /// </summary>
        public string GetGpsQualityStatus()
        {
            if (GpsCoordinateHandler.Instance == null || !GpsCoordinateHandler.Instance.isGpsOn)
                return "GPS not available";

            float accuracy = Input.location.lastData.horizontalAccuracy;

            if (accuracy < 5f)
                return "Excellent";
            else if (accuracy < 10f)
                return "Good";
            else if (accuracy < 20f)
                return "Fair";
            else if (accuracy < 50f)
                return "Poor";
            else
                return "Very Poor";
        }

        /// <summary>
        /// Collects and averages GPS and heading samples with accuracy filtering
        /// </summary>
        private IEnumerator CollectGpsSamples()
        {
            isCollectingSamples = true;
            hasSampledData = false;
            validSamplesCollected = 0;
            totalSamplesAttempted = 0;

            List<double> latitudes = new List<double>();
            List<double> longitudes = new List<double>();
            List<double> altitudes = new List<double>();
            List<double> headings = new List<double>();
            List<float> accuracies = new List<float>();

            Debug.Log($"GPS Warmup: Waiting {gpsWarmupTime}s for GPS to stabilize...");
            yield return new WaitForSeconds(gpsWarmupTime);

            Debug.Log($"Starting GPS sample collection: {sampleCount} samples with {sampleInterval}s interval");
            Debug.Log($"GPS Quality: {GetGpsQualityStatus()}");

            onSamplingStarted?.Invoke();

            for (int i = 0; i < sampleCount; i++)
            {
                totalSamplesAttempted++;
                GPSCoordinates currentData = GpsCoordinateHandler.Instance.gpsCoordinates;
                float horizontalAccuracy = Input.location.lastData.horizontalAccuracy;
                lastHorizontalAccuracy = horizontalAccuracy;

                // Check if GPS data is valid and meets accuracy threshold
                if (currentData.IsValid())
                {
                    if (horizontalAccuracy <= minAccuracyThreshold)
                    {
                        latitudes.Add(currentData.latitude);
                        longitudes.Add(currentData.longitude);
                        altitudes.Add(currentData.altitude);
                        headings.Add(currentData.trueHeading);
                        accuracies.Add(horizontalAccuracy);
                        validSamplesCollected++;

                        Debug.Log($"Sample {i + 1}/{sampleCount} [VALID] - Lat: {currentData.latitude:F8}, Lon: {currentData.longitude:F8}, Alt: {currentData.altitude:F2}, Heading: {currentData.trueHeading:F2}, Accuracy: {horizontalAccuracy:F2}m");
                    }
                    else
                    {
                        Debug.LogWarning($"Sample {i + 1}/{sampleCount} [FILTERED] - Accuracy too low: {horizontalAccuracy:F2}m (threshold: {minAccuracyThreshold}m)");
                    }
                }
                else
                {
                    Debug.LogWarning($"Sample {i + 1}/{sampleCount} [INVALID] - GPS data not valid");
                }

                yield return new WaitForSeconds(sampleInterval);
            }

            // Check if we have enough valid samples (at least 50% of requested)
            int minRequiredSamples = Mathf.Max(5, sampleCount / 2);
            if (latitudes.Count < minRequiredSamples)
            {
                Debug.LogError($"Insufficient valid GPS samples: {latitudes.Count}/{sampleCount} collected (minimum {minRequiredSamples} required). GPS quality may be too poor.");
                isCollectingSamples = false;
                onSamplingCompleted?.Invoke();
                yield break;
            }

            // Calculate averages
            averagedLatitude = CalculateAverage(latitudes);
            averagedLongitude = CalculateAverage(longitudes);
            averagedAltitude = CalculateAverage(altitudes);
            averagedHeading = CalculateCircularAverage(headings);
            averagedAccuracy = (float)CalculateAverage(accuracies.ConvertAll(x => (double)x));

            hasSampledData = true;
            isCollectingSamples = false;

            Debug.Log($"GPS Sampling Complete! {validSamplesCollected}/{totalSamplesAttempted} valid samples:");
            Debug.Log($"Averaged GPS - Lat: {averagedLatitude:F8}, Lon: {averagedLongitude:F8}, Alt: {averagedAltitude:F2}, Heading: {averagedHeading:F2}, Avg Accuracy: {averagedAccuracy:F2}m");

            onSamplingCompleted?.Invoke();
        }

        /// <summary>
        /// Calculates simple average of a list of values
        /// </summary>
        private double CalculateAverage(List<double> values)
        {
            double sum = 0;
            foreach (double value in values)
            {
                sum += value;
            }
            return sum / values.Count;
        }

        /// <summary>
        /// Calculates circular average for heading values (handles 0/360 wraparound)
        /// </summary>
        private double CalculateCircularAverage(List<double> headings)
        {
            double sumSin = 0;
            double sumCos = 0;

            foreach (double heading in headings)
            {
                double radians = heading * Mathf.Deg2Rad;
                sumSin += Math.Sin(radians);
                sumCos += Math.Cos(radians);
            }

            double avgSin = sumSin / headings.Count;
            double avgCos = sumCos / headings.Count;

            double avgRadians = Math.Atan2(avgSin, avgCos);
            double avgDegrees = avgRadians * Mathf.Rad2Deg;

            // Normalize to 0-360 range
            if (avgDegrees < 0)
            {
                avgDegrees += 360;
            }

            return avgDegrees;
        }


        /// <summary>
        /// Combined method to start sampling and then compute local pose
        /// </summary>
        public void LocalizeFromGps() // call this function on Localize button press
        {
            camPos = arCamera.transform.position;
            camRot = arCamera.transform.rotation;

            if (editorTest)
            {
                GetLocalPoseFromGps();
            }
            else
            {
                StartCoroutine(LocalizeFromGpsCoroutine());
            }
        }

        private IEnumerator LocalizeFromGpsCoroutine()
        {
            Debug.Log("Starting GPS-based localization...");

            // Wait for map details if not available
            yield return new WaitUntil(() => vpsMap != null);

            // Start GPS sampling
            StartGpsSampling();

            // Wait for sampling to complete
            yield return new WaitUntil(() => !isCollectingSamples);

            if (hasSampledData)
            {
                // Compute and apply localization
                GetLocalPoseFromGps();
                Debug.Log("GPS-based localization complete!");
            }
            else
            {
                Debug.LogError("GPS-based localization failed - no valid samples collected.");
            }
        }


        public void GetLocalPoseFromGps()
        {
            if (vpsMap == null)
            {
                Debug.LogError("VPS Map is null. Cannot compute local pose.");
                onLocalizationComplete?.Invoke(false, "VPS Map data not available");
                return;
            }

            if (!editorTest && !hasSampledData)
            {
                Debug.LogError("No GPS samples collected yet. Please call StartGpsSampling() first.");
                onLocalizationComplete?.Invoke(false, "No GPS samples available");
                return;
            }

            try
            {
                if (editorTest)
                {
                    averagedLatitude = latitude;
                    averagedLongitude = longitude;
                    averagedAltitude = altitude;
                    averagedHeading = cameraHeadingDeg;
                }

                averagedAltitude = altitude; // use map altitude for better stability

                // Use map data from API
                double mapOriginLat = vpsMap.location.coordinates[1];
                double mapOriginLon = vpsMap.location.coordinates[0];
                double mapOriginAlt = vpsMap.location.coordinates[2];
                double mapHeadingFromApi = vpsMap.heading;

                // Get GeoTransformer to convert GPS to local position
                GeoTransformer geoTransformer = new GeoTransformer(mapOriginLat, mapOriginLon, mapOriginAlt, mapHeadingFromApi);

                // Get user's position in map space
                Vector3 userPositionInMapSpace = geoTransformer.GlobalToLocal(averagedLatitude, averagedLongitude, averagedAltitude);

                float gpsHeading = (float)averagedHeading;
                float mapHeading = (float)mapHeadingFromApi;

                // Calculate what the camera's Y rotation would be in map's local coordinate system
                float cameraYInMapSpace = gpsHeading - mapHeading;

                // Build query pose with the corrected rotation
                // Use only Y rotation (yaw) for stability on flat ground
                Quaternion queryRotation = Quaternion.Euler(0f, cameraYInMapSpace, 0f);
                Matrix4x4 queryPose = Matrix4x4.TRS(userPositionInMapSpace, queryRotation, Vector3.one);

                // Camera pose in world space (use only Y rotation for stability)
                Quaternion cameraRotationYOnly = Quaternion.Euler(0f, camRot.eulerAngles.y, 0f);
                Matrix4x4 cameraPose = Matrix4x4.TRS(camPos, cameraRotationYOnly, Vector3.one);

                // Calculate MapSpace transform
                Matrix4x4 resultPose = cameraPose * queryPose.inverse;
                Quaternion resultRotation = resultPose.rotation;

                // Apply rotation and position
                mapSpace.transform.rotation = resultRotation;

                Vector3 resultPosition = new Vector3(resultPose.m03, resultPose.m13, resultPose.m23);
                mapSpace.transform.position = resultPosition;

                string successMessage = $"Success: {userPositionInMapSpace}, Acc: {averagedAccuracy:F2}m";
                onLocalizationComplete?.Invoke(true, successMessage);

                ToastManager.Instance.ShowToast(successMessage);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during GPS localization: {ex.Message}");
                onLocalizationComplete?.Invoke(false, $"Localization error: {ex.Message}");
                ToastManager.Instance.ShowToast($"Localization error: {ex.Message}");
            }
        }


        /// <summary>
        /// Gets the current averaged GPS coordinates (after sampling)
        /// </summary>
        public (double latitude, double longitude, double altitude, double heading, float accuracy) GetAveragedGpsData()
        {
            if (!hasSampledData)
            {
                Debug.LogWarning("No GPS data available. Call StartGpsSampling() first.");
                return (0, 0, 0, 0, 0);
            }

            return (averagedLatitude, averagedLongitude, averagedAltitude, averagedHeading, averagedAccuracy);
        }

        /// <summary>
        /// Checks if GPS localization is ready to use
        /// </summary>
        public bool IsLocalizationReady()
        {
            return hasSampledData && vpsMap != null && mapSpace != null;
        }
    }
}