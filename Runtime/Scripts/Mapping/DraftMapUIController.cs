/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MultiSet.Samples
{

    // This class demonstrates how to subscribe to DraftMapManager events and update UI.
    // use this as a reference to create custom UI implementations.

    public class DraftMapUIController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Manager Reference")]
        [SerializeField] private DraftMapManager draftMapManager;

        [Header("List UI")]
        [SerializeField] private GameObject draftMapListHolder;
        [SerializeField] private GameObject draftMapCardPrefab;
        [SerializeField] private GameObject draftMenu;

        [Header("Popups")]
        [SerializeField] private GameObject draftMapMenuPopup;
        [SerializeField] private GameObject uploadSuccessPopup;
        [SerializeField] private GameObject loader;

        [Header("Input")]
        [SerializeField] private TMP_InputField mapNameInput;
        [SerializeField] private GameObject mapNameError;

        [Header("Text")]
        [SerializeField] private TMP_Text uploadProgressText;
        #endregion

        #region Private Fields
        private List<GameObject> draftMapCards = new List<GameObject>();
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (draftMapManager == null)
            {
                draftMapManager = FindFirstObjectByType<DraftMapManager>();
            }

            if (draftMapManager != null)
            {
                SubscribeToEvents();
            }
            else
            {
                Debug.LogError("DraftMapManager not found! Please assign it in the inspector.");
            }

            EventManager<DraftMap>.StartListening("DraftMapSelection", ViewDraftMap);
        }

        private void OnDisable()
        {
            if (draftMapManager != null)
            {
                UnsubscribeFromEvents();
            }

            EventManager<DraftMap>.StopListening("DraftMapSelection", ViewDraftMap);
        }

        private void ViewDraftMap(DraftMap map)
        {
            mapNameInput.text = map.mapName.Trim();
            draftMapMenuPopup.SetActive(true);

            if (draftMapManager != null)
            {
                draftMapManager.SelectDraft(map);
            }
        }

        #endregion

        #region Event Subscription
        private void SubscribeToEvents()
        {
            draftMapManager.OnDraftListLoaded += HandleDraftListLoaded;
            draftMapManager.OnDraftSelected += HandleDraftSelected;
            draftMapManager.OnUploadProgress += HandleUploadProgress;
            draftMapManager.OnUploadCompleted += HandleUploadCompleted;
            draftMapManager.OnError += HandleError;
            draftMapManager.OnMapNameValidated += HandleMapNameValidated;
            draftMapManager.OnAuthenticationRequired += HandleAuthenticationRequired;
            draftMapManager.OnDraftDeleted += HandleDraftDeleted;
            draftMapManager.OnProcessingStateChanged += HandleProcessingStateChanged;
        }

        private void UnsubscribeFromEvents()
        {
            draftMapManager.OnDraftListLoaded -= HandleDraftListLoaded;
            draftMapManager.OnDraftSelected -= HandleDraftSelected;
            draftMapManager.OnUploadProgress -= HandleUploadProgress;
            draftMapManager.OnUploadCompleted -= HandleUploadCompleted;
            draftMapManager.OnError -= HandleError;
            draftMapManager.OnMapNameValidated -= HandleMapNameValidated;
            draftMapManager.OnAuthenticationRequired -= HandleAuthenticationRequired;
            draftMapManager.OnDraftDeleted -= HandleDraftDeleted;
            draftMapManager.OnProcessingStateChanged -= HandleProcessingStateChanged;
        }
        #endregion

        #region Event Handlers
        private void HandleDraftListLoaded(object sender, DraftMapListEventArgs e)
        {
            // Clear existing cards
            ClearDraftCards();

            // Update menu visibility
            draftMenu?.SetActive(e.HasDrafts);

            if (!e.HasDrafts)
            {
                return;
            }

            // Create cards for each draft
            foreach (var draftMap in e.DraftMaps)
            {
                CreateDraftCard(draftMap);
            }
        }

        private void HandleDraftSelected(object sender, DraftMapSelectedEventArgs e)
        {
            if (mapNameInput != null)
            {
                mapNameInput.text = e.SelectedDraft.mapName;
            }

            draftMapMenuPopup?.SetActive(true);
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
                draftMapMenuPopup?.SetActive(false);
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

        private void HandleAuthenticationRequired(object sender, AuthenticationEventArgs e)
        {
            loader?.SetActive(false);
            ToastManager.Instance?.ShowToast(e.Message ?? "Authentication required");
        }

        private void HandleDraftDeleted(object sender, string draftId)
        {
            // Refresh the list
            draftMapManager?.LoadDraftList();
        }

        private void HandleProcessingStateChanged(object sender, bool isProcessing)
        {
            loader?.SetActive(isProcessing);
        }
        #endregion

        #region UI Creation
        private void CreateDraftCard(DraftMap draftMap)
        {
            if (draftMapCardPrefab == null || draftMapListHolder == null)
            {
                Debug.LogError("draftMapCardPrefab or draftMapListHolder is not assigned in the Inspector.");
                return;
            }

            var cardObj = Instantiate(draftMapCardPrefab);
            draftMapCards.Add(cardObj);

            cardObj.SetActive(true);
            cardObj.transform.SetParent(draftMapListHolder.transform, false);
            cardObj.transform.localScale = Vector3.one;
            cardObj.name = draftMap.mapName;

            // Get creation date
            string creationDate = draftMapManager.GetDraftCreationDate(draftMap.id);

            // Setup the card component
            var draftMapCard = cardObj.GetComponent<DraftMapCard>();
            if (draftMapCard != null)
            {
                draftMapCard.draftMap = draftMap;
                draftMapCard.SetDraftMapCard(draftMap, creationDate);
            }
        }

        private void ClearDraftCards()
        {
            foreach (var card in draftMapCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            draftMapCards.Clear();
        }
        #endregion

        #region Button Handlers

        // Called when Upload button is clicked.
        public void OnUploadClicked()
        {
            if (mapNameInput != null)
            {
                mapNameError?.SetActive(false);
                draftMapManager?.UploadSelectedDraft(mapNameInput.text);
            }
        }


        // Called when Delete button is clicked.
        public void OnDeleteClicked()
        {
            draftMapManager?.DeleteSelectedDraftAndReload();
        }


        // Called when Close popup button is clicked.
        public void OnClosePopupClicked()
        {
            draftMapMenuPopup?.SetActive(false);
        }


        // Refresh the draft list.
        public void RefreshDraftList()
        {
            draftMapManager?.LoadDraftList();
        }
        #endregion
    }
}
