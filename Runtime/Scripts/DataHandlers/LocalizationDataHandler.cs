using UnityEngine;

namespace MultiSet
{
    public class LocalizationDataHandler : MonoBehaviour
    {
        private SingleFrameLocalizationManager singleFrameLocalizationManager;
        private MapLocalizationManager mapLocalizationManager;

        void Start()
        {
            singleFrameLocalizationManager = FindFirstObjectByType<SingleFrameLocalizationManager>();
            mapLocalizationManager = FindFirstObjectByType<MapLocalizationManager>();

            // Subscribe to SingleFrameLocalizationManager
            if (singleFrameLocalizationManager != null)
            {
                singleFrameLocalizationManager.OnLocalizationWithResponse += OnSingleFrameLocalizationResponse;
            }

            // Subscribe to MapLocalizationManager
            if (mapLocalizationManager != null)
            {
                mapLocalizationManager.OnLocalizationWithResponse += OnMultiFrameLocalizationResponse;
            }
        }

        void OnSingleFrameLocalizationResponse(LocalizationSuccessResponse response)
        {
            Debug.Log($"SingleFrame Localization Response: {JsonUtility.ToJson(response)}");
            // Access response.position, response.rotation, response.confidence, response.mapCodes, etc.
        }

        void OnMultiFrameLocalizationResponse(LocalizationResponseMultiFrame response)
        {
            Debug.Log($"MultiFrame Localization Response: {JsonUtility.ToJson(response)}");
            // Access response.estimatedPose, response.trackingPose, response.confidence, response.mapCodes, etc.
        }

        void OnDestroy()
        {
            // Unsubscribe from SingleFrameLocalizationManager
            if (singleFrameLocalizationManager != null)
            {
                singleFrameLocalizationManager.OnLocalizationWithResponse -= OnSingleFrameLocalizationResponse;
            }

            // Unsubscribe from MapLocalizationManager
            if (mapLocalizationManager != null)
            {
                mapLocalizationManager.OnLocalizationWithResponse -= OnMultiFrameLocalizationResponse;
            }
        }
    }
}