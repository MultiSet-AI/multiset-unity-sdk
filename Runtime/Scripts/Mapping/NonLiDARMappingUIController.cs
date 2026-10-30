/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiSet.Samples
{
    // This class demonstrates how to subscribe to NonLiDARMappingManager events and update UI.
    // use this as a reference to create custom UI implementations.
    public class NonLiDARMappingUIController : MonoBehaviour
    {
        private enum DiscardContext
        {
            Packaging,
            MapName
        }

        #region Serialized Fields
        [Header("Manager Reference")]
        [SerializeField] private NonLiDARMappingManager mappingManager;

        [Header("Buttons")]
        [SerializeField] private GameObject startButton;
        [SerializeField] private Button finishButton;
        [SerializeField] private TMP_Text finishHintText;
        [SerializeField] private GameObject draftsMenu;

        [Header("Scan HUD")]
        [SerializeField] private GameObject scanHud;
        [SerializeField] private Image trackingPill;
        [SerializeField] private TMP_Text trackingPillText;
        [SerializeField] private GameObject guidanceBanner;
        [SerializeField] private TMP_Text guidanceText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private Color goodColor = new(0.18f, 0.67f, 0.35f, 0.9f);
        [SerializeField] private Color weakColor = new(0.95f, 0.62f, 0.07f, 0.9f);
        [SerializeField] private Color lostColor = new(0.85f, 0.19f, 0.19f, 0.9f);

        [Header("Timer")]
        [SerializeField] private GameObject timerRoot;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Slider timerSlider;

        [Header("Panels")]
        [SerializeField] private GameObject instructionsPopup;
        [SerializeField] private GameObject loader;
        [SerializeField] private TMP_Text loaderText;
        [SerializeField] private GameObject cancelPackagingButton;
        [SerializeField] private GameObject discardPrompt;
        [SerializeField] private Text discardPromptDetails;
        [SerializeField] private GameObject mapNamePanel;
        [SerializeField] private Text scanSummaryText;
        [SerializeField] private GameObject draftSavedPopup;
        [SerializeField] private GameObject uploadSuccessPopup;
        [SerializeField] private GameObject uploadFailedPopup;
        [SerializeField] private Text uploadFailedMessage;
        [SerializeField] private GameObject authAlertPopup;
        [SerializeField] private GameObject unsupportedPopup;
        [SerializeField] private Text unsupportedMessage;

        [Header("Input")]
        [SerializeField] private TMP_InputField mapNameInput;
        [SerializeField] private GameObject mapNameError;
        #endregion

        #region Private Fields
        private DiscardContext discardContext;
        private float nextDebugRefresh;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (mappingManager == null)
            {
                mappingManager = FindFirstObjectByType<NonLiDARMappingManager>();
            }

            if (mappingManager != null)
            {
                SubscribeToEvents();
            }
            else
            {
                Debug.LogError("NonLiDARMappingManager not found! Please assign it in the inspector.");
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
            HideAllPopups();
            Show(startButton, false);
            Show(finishButton, false);
            Show(timerRoot, false);
            Show(scanHud, true);
            SetText(finishHintText, "");
            SetText(statsText, "");
            Show(debugText, Debug.isDebugBuild);
            Show(instructionsPopup, true);

            if (mappingManager != null)
            {
                ApplyState(mappingManager.State, null, null);
            }
        }

        private void Update()
        {
            if (debugText == null || !Debug.isDebugBuild || mappingManager == null || Time.unscaledTime < nextDebugRefresh)
            {
                return;
            }

            nextDebugRefresh = Time.unscaledTime + 0.5f;
            debugText.text = $"camera {mappingManager.CameraConfiguration} · image misses {mappingManager.ImageTimestampMisses} · " +
                             $"{mappingManager.TrackingStatus} · points {mappingManager.CoveragePoints.Count}";
        }
        #endregion

        #region Event Subscription
        private void SubscribeToEvents()
        {
            mappingManager.OnStateChanged += HandleStateChanged;
            mappingManager.OnGuidanceChanged += HandleGuidanceChanged;
            mappingManager.OnScanProgress += HandleScanProgress;
            mappingManager.OnFinalMinute += HandleFinalMinute;
            mappingManager.OnPackagingProgress += HandlePackagingProgress;
            mappingManager.OnPackagingInterrupted += HandlePackagingInterrupted;
            mappingManager.OnMapNameValidated += HandleMapNameValidated;
            mappingManager.OnUploadProgress += HandleUploadProgress;
            mappingManager.OnError += HandleError;
        }

        private void UnsubscribeFromEvents()
        {
            mappingManager.OnStateChanged -= HandleStateChanged;
            mappingManager.OnGuidanceChanged -= HandleGuidanceChanged;
            mappingManager.OnScanProgress -= HandleScanProgress;
            mappingManager.OnFinalMinute -= HandleFinalMinute;
            mappingManager.OnPackagingProgress -= HandlePackagingProgress;
            mappingManager.OnPackagingInterrupted -= HandlePackagingInterrupted;
            mappingManager.OnMapNameValidated -= HandleMapNameValidated;
            mappingManager.OnUploadProgress -= HandleUploadProgress;
            mappingManager.OnError -= HandleError;
        }
        #endregion

        #region Event Handlers
        private void HandleStateChanged(object sender, MappingStateChangedEventArgs e)
        {
            ApplyState(e.CurrentState, e.PreviousState, e.Message);
        }

        private void ApplyState(MappingState state, MappingState? previousState, string message)
        {
            bool recording = state == MappingState.Mapping;
            Show(startButton, state == MappingState.Ready);
            Show(finishButton, recording);
            Show(timerRoot, recording);
            Show(timerSlider, recording);
            Show(draftsMenu, state == MappingState.Ready || state == MappingState.Initializing || state == MappingState.Error);
            if (!recording)
            {
                SetText(finishHintText, "");
            }

            switch (state)
            {
                case MappingState.Ready:
                    Show(loader, false);
                    SetText(statsText, "");
                    break;

                case MappingState.Mapping:
                    Show(instructionsPopup, false);
                    if (finishButton != null)
                    {
                        finishButton.interactable = false;
                    }
                    break;

                case MappingState.Processing:
                    ShowLoader(message, showCancel: true);
                    break;

                case MappingState.WaitingForMapName:
                    Show(loader, false);
                    if (scanSummaryText != null)
                    {
                        scanSummaryText.text = message;
                    }
                    Show(mapNameError, false);
                    Show(mapNamePanel, true);
                    break;

                case MappingState.Uploading:
                    Show(mapNamePanel, false);
                    Show(uploadFailedPopup, false);
                    ShowLoader(message, showCancel: false);
                    break;

                case MappingState.Completed:
                    Show(loader, false);
                    Show(uploadSuccessPopup, true);
                    break;

                case MappingState.Error:
                    Show(loader, false);
                    ShowError(previousState, message);
                    break;
            }
        }

        private void ShowError(MappingState? previousState, string message)
        {
            if (mappingManager != null && mappingManager.IsUnsupported)
            {
                if (unsupportedMessage != null)
                {
                    unsupportedMessage.text = message;
                }
                Show(unsupportedPopup, true);
                return;
            }

            if (previousState == MappingState.Uploading)
            {
                if (message == NonLiDARUploader.AuthenticationRequiredMessage)
                {
                    Show(authAlertPopup, true);
                    return;
                }

                if (uploadFailedMessage != null)
                {
                    uploadFailedMessage.text = $"{message}\nThe scan is saved in My Drafts.";
                }
                Show(uploadFailedPopup, true);
                return;
            }

            if (!string.IsNullOrEmpty(message))
            {
                ToastManager.Instance?.ShowToast(message);
            }
        }

        private void HandleGuidanceChanged(object sender, ScanGuidanceEventArgs e)
        {
            SetText(trackingPillText, e.Title);
            if (trackingPill != null)
            {
                trackingPill.color = e.Level switch
                {
                    TrackingLevel.Good => goodColor,
                    TrackingLevel.Weak => weakColor,
                    _ => lostColor
                };
            }

            SetText(guidanceText, e.Message);
            bool hasMessage = !string.IsNullOrEmpty(e.Message);
            if (guidanceBanner != null)
            {
                guidanceBanner.SetActive(hasMessage);
            }
            else
            {
                Show(guidanceText, hasMessage);
            }
        }

        private void HandleScanProgress(object sender, ScanProgressEventArgs e)
        {
            int elapsed = Mathf.FloorToInt(e.ElapsedSeconds);
            SetText(timerText, $"{elapsed / 60:00}:{elapsed % 60:00} / 10:00");
            if (timerSlider != null)
            {
                timerSlider.maxValue = NonLiDARLimits.MaximumDurationSeconds;
                timerSlider.value = e.ElapsedSeconds;
            }

            SetText(statsText, $"{e.KeptFrames} frames · {e.CoverageHint}");
            if (finishButton != null)
            {
                finishButton.interactable = e.CanFinish;
            }
            SetText(finishHintText, e.CanFinish ? "" : ScanGuidanceCopy.KeepGoing(e.SecondsUntilFinish));
        }

        private void HandleFinalMinute(object sender, EventArgs e)
        {
            ToastManager.Instance?.ShowToast(ScanGuidanceCopy.FinalMinute);
        }

        private void HandlePackagingProgress(object sender, PackagingProgressEventArgs e)
        {
            SetText(loaderText, $"Packaging scan: {e.ProgressPercent}%");
        }

        private void HandlePackagingInterrupted(object sender, PackagingInterruptedEventArgs e)
        {
            Show(loader, false);
            ShowDiscardPrompt(DiscardContext.Packaging, e.Message);
        }

        private void HandleMapNameValidated(object sender, MapNameValidationEventArgs e)
        {
            Show(mapNameError, !e.IsValid);

            if (!e.IsValid)
            {
                var errorText = mapNameError != null ? mapNameError.GetComponentInChildren<TMP_Text>() : null;
                if (errorText != null)
                {
                    errorText.text = e.ErrorMessage;
                }
            }
        }

        private void HandleUploadProgress(object sender, UploadProgressEventArgs e)
        {
            SetText(loaderText, $"Uploading: {e.ProgressPercent}%");
        }

        private void HandleError(object sender, MappingErrorEventArgs e)
        {
            ToastManager.Instance?.ShowToast(e.ErrorMessage);
        }
        #endregion

        #region Button Handlers
        public void OnStartClicked()
        {
            if (mappingManager != null)
            {
                mappingManager.StartRecording();
            }
        }

        public void OnFinishClicked()
        {
            if (mappingManager != null)
            {
                mappingManager.FinishRecording();
            }
        }

        public void OnCancelPackagingClicked()
        {
            if (mappingManager != null)
            {
                mappingManager.CancelPackaging();
            }
        }

        // Closing the name panel would strand the scan, so ask first.
        public void OnCloseMapNameClicked()
        {
            Show(mapNamePanel, false);
            ShowDiscardPrompt(DiscardContext.MapName, "Discard this scan? It has not been saved.");
        }

        public void OnDiscardClicked()
        {
            Show(discardPrompt, false);
            if (mappingManager != null)
            {
                mappingManager.DiscardRecording();
            }
        }

        public void OnKeepClicked()
        {
            Show(discardPrompt, false);
            if (discardContext == DiscardContext.Packaging)
            {
                ShowLoader("Preparing map data…", showCancel: true);
                if (mappingManager != null)
                {
                    mappingManager.RestartPackaging();
                }
            }
            else
            {
                Show(mapNamePanel, true);
            }
        }

        public void OnSubmitMapNameClicked()
        {
            if (mapNameInput != null && mappingManager != null)
            {
                mappingManager.SubmitMapName(mapNameInput.text);
            }
        }

        public void OnSaveAsDraftClicked()
        {
            if (mapNameInput == null || mappingManager == null)
            {
                return;
            }

            mappingManager.SaveAsDraft(mapNameInput.text);
            if (mappingManager.State == MappingState.Ready)
            {
                Show(mapNamePanel, false);
                Show(draftSavedPopup, true);
            }
        }

        public void OnRetryUploadClicked()
        {
            Show(uploadFailedPopup, false);
            if (mappingManager != null)
            {
                mappingManager.RetryUpload();
            }
        }

        public void OnReloadSceneClicked()
        {
            if (mappingManager != null)
            {
                mappingManager.ReloadScene();
            }
        }
        #endregion

        #region Helper Methods
        private void ShowLoader(string message, bool showCancel)
        {
            Show(loader, true);
            Show(cancelPackagingButton, showCancel);
            if (!string.IsNullOrEmpty(message))
            {
                SetText(loaderText, message);
            }
        }

        private void ShowDiscardPrompt(DiscardContext context, string message)
        {
            discardContext = context;
            if (discardPromptDetails != null)
            {
                discardPromptDetails.text = message;
            }
            Show(discardPrompt, true);
        }

        // Unassigned serialized fields are Unity "fake nulls", which ?. does not catch.
        private static void Show(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void Show(Component target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        private void HideAllPopups()
        {
            Show(loader, false);
            Show(discardPrompt, false);
            Show(mapNamePanel, false);
            Show(draftSavedPopup, false);
            Show(uploadSuccessPopup, false);
            Show(uploadFailedPopup, false);
            Show(authAlertPopup, false);
            Show(unsupportedPopup, false);
            Show(mapNameError, false);
        }
        #endregion
    }
}
