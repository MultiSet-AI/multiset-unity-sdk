/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using TMPro;

namespace MultiSet
{
    public class GpsDebugUITMP : MonoBehaviour
    {
        [Header("TextMeshPro References")]
        [SerializeField] private TextMeshProUGUI latitudeText;
        [SerializeField] private TextMeshProUGUI longitudeText;
        [SerializeField] private TextMeshProUGUI altitudeText;
        [SerializeField] private TextMeshProUGUI headingText;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI combinedText; // Single text showing all info

        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.5f;

        [Header("Display Format")]
        [SerializeField] private bool showLabels = true;
        [SerializeField] private int decimalPlaces = 8;
        [SerializeField] private bool useCombinedDisplay = false;

        [Header("Visual Settings")]
        [SerializeField] private bool colorCodeStatus = true;
        [SerializeField] private bool showCompassDirection = true;

        private float lastUpdateTime;


        void Update()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;

                if (useCombinedDisplay && combinedText != null)
                    UpdateCombinedUI();
                else
                    UpdateSeparateUI();
            }
        }

        void UpdateSeparateUI()
        {
            if (GpsCoordinateHandler.Instance == null)
            {
                SetUnavailableText();
                return;
            }

            if (!GpsCoordinateHandler.Instance.isGpsOn)
            {
                SetGpsDisabledText();
                return;
            }

            GPSCoordinates coords = GpsCoordinateHandler.Instance.gpsCoordinates;

            // Latitude
            if (latitudeText != null)
            {
                latitudeText.text = showLabels
                    ? $"Lat: {coords.latitude.ToString($"F{decimalPlaces}")}°"
                    : coords.latitude.ToString($"F{decimalPlaces}");
            }

            // Longitude
            if (longitudeText != null)
            {
                longitudeText.text = showLabels
                    ? $"Lon: {coords.longitude.ToString($"F{decimalPlaces}")}°"
                    : coords.longitude.ToString($"F{decimalPlaces}");
            }

            // Altitude
            if (altitudeText != null)
            {
                altitudeText.text = showLabels
                    ? $"Alt: {coords.altitude:F2}m"
                    : $"{coords.altitude:F2}";
            }

            // Heading with compass direction
            if (headingText != null)
            {
                double heading = GpsCoordinateHandler.Instance.trueHeading;
                string compassDir = showCompassDirection ? $" ({GetCompassDirection(heading)})" : "";
                headingText.text = showLabels
                    ? $"Heading: {heading:F1}°{compassDir}"
                    : $"{heading:F1}°{compassDir}";
            }

            // Accuracy
            if (accuracyText != null)
            {
                float accuracy = Input.location.lastData.horizontalAccuracy;
                accuracyText.text = showLabels
                    ? $"Accuracy: ±{accuracy:F1}m"
                    : $"±{accuracy:F1}m";
            }

            // Status
            if (statusText != null)
            {
                string quality = GetGpsQuality();
                statusText.text = showLabels ? $"GPS: {quality}" : quality;

                if (colorCodeStatus)
                    statusText.color = GetQualityColor(quality);
            }
        }

        void UpdateCombinedUI()
        {
            if (combinedText == null) return;

            if (GpsCoordinateHandler.Instance == null)
            {
                combinedText.text = "<color=#808080>GPS Unavailable</color>";
                return;
            }

            if (!GpsCoordinateHandler.Instance.isGpsOn)
            {
                combinedText.text = "<color=#FFFF00>GPS Disabled</color>";
                return;
            }

            GPSCoordinates coords = GpsCoordinateHandler.Instance.gpsCoordinates;
            double heading = GpsCoordinateHandler.Instance.trueHeading;
            float accuracy = Input.location.lastData.horizontalAccuracy;
            string quality = GetGpsQuality();
            string qualityColor = ColorUtility.ToHtmlStringRGB(GetQualityColor(quality));
            string compassDir = GetCompassDirection(heading);

            combinedText.text = $"<b>GPS Data</b>\n" +
                $"Lat: {coords.latitude:F8}°\n" +
                $"Lon: {coords.longitude:F8}°\n" +
                $"Alt: {coords.altitude:F2}m\n" +
                $"Heading: {heading:F1}° ({compassDir})\n" +
                $"Accuracy: ±{accuracy:F1}m\n" +
                $"<color=#{qualityColor}>Status: {quality}</color>";
        }

        void SetUnavailableText()
        {
            if (latitudeText != null) latitudeText.text = showLabels ? "Lat: --" : "--";
            if (longitudeText != null) longitudeText.text = showLabels ? "Lon: --" : "--";
            if (altitudeText != null) altitudeText.text = showLabels ? "Alt: --" : "--";
            if (headingText != null) headingText.text = showLabels ? "Heading: --" : "--";
            if (accuracyText != null) accuracyText.text = showLabels ? "Accuracy: --" : "--";
            if (statusText != null)
            {
                statusText.text = "GPS Unavailable";
                statusText.color = Color.gray;
            }
        }

        void SetGpsDisabledText()
        {
            if (latitudeText != null) latitudeText.text = showLabels ? "Lat: --" : "--";
            if (longitudeText != null) longitudeText.text = showLabels ? "Lon: --" : "--";
            if (altitudeText != null) altitudeText.text = showLabels ? "Alt: --" : "--";
            if (headingText != null) headingText.text = showLabels ? "Heading: --" : "--";
            if (accuracyText != null) accuracyText.text = showLabels ? "Accuracy: --" : "--";
            if (statusText != null)
            {
                statusText.text = "GPS Disabled";
                statusText.color = Color.yellow;
            }
        }

        string GetGpsQuality()
        {
            if (GpsCoordinateHandler.Instance == null || !GpsCoordinateHandler.Instance.isGpsOn)
                return "No GPS";

            float accuracy = Input.location.lastData.horizontalAccuracy;

            if (accuracy < 5f) return "Excellent";
            else if (accuracy < 10f) return "Good";
            else if (accuracy < 20f) return "Fair";
            else if (accuracy < 50f) return "Poor";
            else return "Very Poor";
        }

        Color GetQualityColor(string quality)
        {
            switch (quality)
            {
                case "Excellent": return new Color(0f, 0.8f, 0f);
                case "Good": return new Color(0.5f, 0.8f, 0f);
                case "Fair": return Color.yellow;
                case "Poor": return new Color(1f, 0.5f, 0f);
                case "Very Poor": return Color.red;
                default: return Color.gray;
            }
        }

        string GetCompassDirection(double heading)
        {
            if (heading < 0) heading += 360;
            heading = heading % 360;

            if (heading >= 337.5 || heading < 22.5) return "N";
            else if (heading >= 22.5 && heading < 67.5) return "NE";
            else if (heading >= 67.5 && heading < 112.5) return "E";
            else if (heading >= 112.5 && heading < 157.5) return "SE";
            else if (heading >= 157.5 && heading < 202.5) return "S";
            else if (heading >= 202.5 && heading < 247.5) return "SW";
            else if (heading >= 247.5 && heading < 292.5) return "W";
            else return "NW";
        }

        public string GetFormattedGpsString()
        {
            if (GpsCoordinateHandler.Instance == null || !GpsCoordinateHandler.Instance.isGpsOn)
                return "GPS not available";

            GPSCoordinates coords = GpsCoordinateHandler.Instance.gpsCoordinates;
            double heading = GpsCoordinateHandler.Instance.trueHeading;
            string compassDir = GetCompassDirection(heading);

            return $"Lat: {coords.latitude:F8}, Lon: {coords.longitude:F8}, " +
                   $"Alt: {coords.altitude:F2}m, Heading: {heading:F2}° ({compassDir})";
        }

        public void CopyGpsToClipboard()
        {
            GUIUtility.systemCopyBuffer = GetFormattedGpsString();
            Debug.Log("GPS data copied: " + GetFormattedGpsString());
        }
    }
}