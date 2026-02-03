using UnityEngine;

namespace MultiSet
{
    public class ObjectTrackingDataHandler : MonoBehaviour
    {
        private ObjectTrackingManager objectTrackingManager;

        void Start()
        {
            objectTrackingManager = FindFirstObjectByType<ObjectTrackingManager>();
            // Subscribe to ObjectTrackingManager
            if (objectTrackingManager != null)
            {
                objectTrackingManager.OnTrackingWithResponse += OnTrackingResponse;
            }
        }

        void OnTrackingResponse(ObjectTrackingResponse response)
        {
            Debug.Log($"Object Tracking Response: {JsonUtility.ToJson(response)}");
            // Access response.position, response.rotation, response.confidence, response.objectCodes, etc.
        }

        void OnDestroy()
        {
            // Unsubscribe from ObjectTrackingManager
            if (objectTrackingManager != null)
            {
                objectTrackingManager.OnTrackingWithResponse -= OnTrackingResponse;
            }
        }
    }
}