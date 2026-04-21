using Unity.Netcode;
using UnityEngine;

namespace MultiSet
{
    public class LocalizationSuccessDataHandler : MonoBehaviour
    {
        [SerializeField] private Transform mapSpace;

        [Tooltip("Optional: MPC MultiplayerManager to notify on localization success")]
        [SerializeField] private MultiplayerManager multiplayerManager;

        private SingleFrameLocalizationManager singleFrameLocalizationManager;
        private MapLocalizationManager mapLocalizationManager;

        void Start()
        {
            singleFrameLocalizationManager = FindFirstObjectByType<SingleFrameLocalizationManager>();
            mapLocalizationManager = FindFirstObjectByType<MapLocalizationManager>();

            if (singleFrameLocalizationManager != null)
            {
                singleFrameLocalizationManager.OnLocalizationWithResponse += OnSingleFrameLocalizationResponse;
            }

            if (mapLocalizationManager != null)
            {
                mapLocalizationManager.OnLocalizationWithResponse += OnMultiFrameLocalizationResponse;
            }
        }

        void OnSingleFrameLocalizationResponse(LocalizationSuccessResponse response)
        {
            Debug.Log($"SingleFrame Localization Response: {JsonUtility.ToJson(response)}");
            NotifyLocalPlayer();
        }

        void OnMultiFrameLocalizationResponse(LocalizationResponseMultiFrame response)
        {
            Debug.Log($"MultiFrame Localization Response: {JsonUtility.ToJson(response)}");
            NotifyLocalPlayer();
        }

        private void NotifyLocalPlayer()
        {
            if (mapSpace == null)
            {
                Debug.LogError("LocalizationSuccessDataHandler: mapSpace is not assigned!");
                return;
            }

            var players = FindObjectsByType<NetworkARPlayer>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    player.SetLocalized(mapSpace);
                    Debug.Log($"Player {player.PlayerIndex} localized — now syncing in map space.");
                    break;
                }
            }

            // Notify MPC MultiplayerManager if assigned
            if (multiplayerManager != null)
            {
                multiplayerManager.OnLocalizationSuccess(mapSpace);
            }
        }

        void OnDestroy()
        {
            if (singleFrameLocalizationManager != null)
            {
                singleFrameLocalizationManager.OnLocalizationWithResponse -= OnSingleFrameLocalizationResponse;
            }

            if (mapLocalizationManager != null)
            {
                mapLocalizationManager.OnLocalizationWithResponse -= OnMultiFrameLocalizationResponse;
            }
        }
    }
}
