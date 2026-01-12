/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using TMPro;
using UnityEngine;

namespace MultiSet.Samples
{
    // This class demonstrates how to subscribe to MappingManager events and update UI.
    // use this as a reference to create custom UI implementations.
    public class MappingUIController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Manager Reference")]
        [SerializeField] private MappingManager mappingManager;

        [Header("Buttons")]
        [SerializeField] private GameObject startButton;
        [SerializeField] private GameObject stopButton;

        [Header("Panels")]
        [SerializeField] private GameObject mapNamePanel;
        [SerializeField]private GameObject draftDetailsPopup;
        [SerializeField] private GameObject uploadSuccessPopup;
        [SerializeField] private GameObject authAlertPopup;
        [SerializeField] private GameObject mappingAlertPopup;
        [SerializeField] private GameObject loader;

        [Header("Input")]
        [SerializeField] private TMP_InputField mapNameInput;
        [SerializeField] private GameObject mapNameError;

        [Header("Text")]
        [SerializeField] private TMP_Text uploadProgressText;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (mappingManager == null)
            {
                mappingManager = FindFirstObjectByType<MappingManager>();
            }

            if (mappingManager != null)
            {
                SubscribeToEvents();
            }
            else
            {
                Debug.LogError("MappingManager not found! Please assign it in the inspector.");
            }
        }

        private void OnDisable()
        {
            if (mappingManager != null)
            {
                UnsubscribeFromEvents();
            }
        }

        private void Start()
        {
            // Initialize UI state
            startButton?.SetActive(false);
            stopButton?.SetActive(false);
            HideAllPopups();
        }
        #endregion

        #region Event Subscription
        private void SubscribeToEvents()
        {
            mappingManager.OnStateChanged += HandleStateChanged;
            mappingManager.OnDeviceCapabilityChecked += HandleDeviceCapabilityChecked;
            mappingManager.OnUploadProgress += HandleUploadProgress;
            mappingManager.OnUploadCompleted += HandleUploadCompleted;
            mappingManager.OnError += HandleError;
            mappingManager.OnMapNameValidated += HandleMapNameValidated;
            mappingManager.OnAuthenticationChanged += HandleAuthenticationChanged;
            mappingManager.OnDraftSaved += HandleDraftSaved;
        }

        private void UnsubscribeFromEvents()
        {
            mappingManager.OnStateChanged -= HandleStateChanged;
            mappingManager.OnDeviceCapabilityChecked -= HandleDeviceCapabilityChecked;
            mappingManager.OnUploadProgress -= HandleUploadProgress;
            mappingManager.OnUploadCompleted -= HandleUploadCompleted;
            mappingManager.OnError -= HandleError;
            mappingManager.OnMapNameValidated -= HandleMapNameValidated;
            mappingManager.OnAuthenticationChanged -= HandleAuthenticationChanged;
            mappingManager.OnDraftSaved -= HandleDraftSaved;
        }
        #endregion

        #region Event Handlers
        private void HandleStateChanged(object sender, MappingStateChangedEventArgs e)
        {
            switch (e.CurrentState)
            {
                case MappingState.Initializing:
                    startButton?.SetActive(false);
                    stopButton?.SetActive(false);
                    break;

                case MappingState.Ready:
                    startButton?.SetActive(true);
                    stopButton?.SetActive(false);
                    loader?.SetActive(false);
                    break;

                case MappingState.Mapping:
                    startButton?.SetActive(false);
                    stopButton?.SetActive(true);
                    break;

                case MappingState.Processing:
                    startButton?.SetActive(false);
                    stopButton?.SetActive(false);
                    loader?.SetActive(true);
                    if (uploadProgressText != null && !string.IsNullOrEmpty(e.Message))
                    {
                        uploadProgressText.text = e.Message;
                    }
                    break;

                case MappingState.WaitingForMapName:
                    loader?.SetActive(false);
                    mapNamePanel?.SetActive(true);
                    break;

                case MappingState.Uploading:
                    loader?.SetActive(true);
                    mapNamePanel?.SetActive(false);
                    break;

                case MappingState.Completed:
                    loader?.SetActive(false);
                    mapNamePanel?.SetActive(false);
                    uploadSuccessPopup?.SetActive(true);
                    break;

                case MappingState.Error:
                    loader?.SetActive(false);
                    // Optionally show error toast
                    if (!string.IsNullOrEmpty(e.Message))
                    {
                        ToastManager.Instance?.ShowToast(e.Message);
                    }
                    break;
            }
        }

        private void HandleDeviceCapabilityChecked(object sender, DeviceCapabilityEventArgs e)
        {
            if (e.MeshingSupported)
            {
                startButton?.SetActive(true);
            }
            else
            {
                startButton?.SetActive(false);
                mappingAlertPopup?.SetActive(true);
            }
        }

        private void HandleUploadProgress(object sender, UploadProgressEventArgs e)
        {
            if (uploadProgressText != null)
            {
                uploadProgressText.text = $"Uploading: {e.ProgressPercent}%";
            }
        }

        private void HandleUploadCompleted(object sender, UploadCompletedEventArgs e)
        {
            loader?.SetActive(false);

            if (e.Success)
            {
                mapNamePanel?.SetActive(false);
                uploadSuccessPopup?.SetActive(true);
            }
            else
            {
                ToastManager.Instance?.ShowToast(e.ErrorMessage ?? "Upload failed");
            }
        }

        private void HandleError(object sender, MappingErrorEventArgs e)
        {
            loader?.SetActive(false);
            ToastManager.Instance?.ShowToast(e.ErrorMessage);
        }

        private void HandleMapNameValidated(object sender, MapNameValidationEventArgs e)
        {
            mapNameError?.SetActive(!e.IsValid);

            if (!e.IsValid)
            {
                var errorText = mapNameError?.GetComponentInChildren<TMP_Text>();
                if (errorText != null)
                {
                    errorText.text = e.ErrorMessage;
                }
            }
        }

        private void HandleAuthenticationChanged(object sender, AuthenticationEventArgs e)
        {
            if (!e.IsAuthenticated)
            {
                authAlertPopup?.SetActive(true);
            }
            else
            {
                authAlertPopup?.SetActive(false);
                startButton?.SetActive(mappingManager.IsMeshingSupported);
            }
        }

        private void HandleDraftSaved(object sender, DraftMapSelectedEventArgs e)
        {
            draftDetailsPopup?.SetActive(true);
        }
        #endregion

        #region Button Handlers

        // Called when Start Mapping button is clicked.
        public void OnStartMappingClicked()
        {
            mappingManager?.StartMapping();
        }


        // Called when Stop Mapping button is clicked.
        public void OnStopMappingClicked()
        {
            mappingManager?.StopMapping();
        }


        // Called when Upload button is clicked.
        public void OnUploadClicked()
        {
            if (mapNameInput != null)
            {
                mappingManager?.UploadMap(mapNameInput.text);
            }
        }


        // Called when Save as Draft button is clicked.
        public void OnSaveAsDraftClicked()
        {
            if (mapNameInput != null)
            {
                mappingManager?.SaveAsDraft(mapNameInput.text);
            }
        }


        // Called when Reload Scene button is clicked.
        public void OnReloadSceneClicked()
        {
            mappingManager?.ReloadScene();
        }

        public void OnResetArSessionClicked()
        {
            mappingManager?.ResetSession();
        }
        #endregion

        #region Helper Methods
        private void HideAllPopups()
        {
            mapNamePanel?.SetActive(false);
            draftDetailsPopup?.SetActive(false);
            uploadSuccessPopup?.SetActive(false);
            authAlertPopup?.SetActive(false);
            mappingAlertPopup?.SetActive(false);
            loader?.SetActive(false);
            mapNameError?.SetActive(false);
        }
        #endregion
    }
}
