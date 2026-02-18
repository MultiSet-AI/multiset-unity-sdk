using UnityEngine;

namespace MultiSet
{
    public class MappingDataHandler : MonoBehaviour
    {
        private MappingManager mappingManager;

        void Start()
        {
            mappingManager = FindFirstObjectByType<MappingManager>();

            if (mappingManager != null)
            {
                mappingManager.OnUploadCompleted += HandleUploadCompleted;
            }
        }

        void OnDestroy()
        {
            if (mappingManager != null)
            {
                mappingManager.OnUploadCompleted -= HandleUploadCompleted;
            }
        }

        private void HandleUploadCompleted(object sender, UploadCompletedEventArgs args)
        {
            if (args.Success)
            {
                string mapCode = args.MapCode;
                Debug.Log("Map created successfully! Map Code: " + mapCode);
            }
            else
            {
                Debug.LogError("Map upload failed: " + args.ErrorMessage);
            }
        }
    }
}