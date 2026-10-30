/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace MultiSet.Samples
{
    // Shows AR Foundation's live point cloud only while a non-LiDAR scan is recording.
    [RequireComponent(typeof(ARPointCloudParticleVisualizer))]
    public class FeaturePointVisibility : MonoBehaviour
    {
        #region Private Fields
        private ARPointCloudParticleVisualizer visualizer;
        private ParticleSystem particles;
        private NonLiDARMappingManager mappingManager;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            visualizer = GetComponent<ARPointCloudParticleVisualizer>();
            particles = GetComponent<ParticleSystem>();
            mappingManager = FindFirstObjectByType<NonLiDARMappingManager>();
        }

        private void OnEnable()
        {
            if (mappingManager == null)
            {
                return;
            }

            mappingManager.OnStateChanged += HandleStateChanged;
            SetVisible(mappingManager.IsRecording);
        }

        private void OnDisable()
        {
            if (mappingManager != null)
            {
                mappingManager.OnStateChanged -= HandleStateChanged;
            }
        }
        #endregion

        #region Event Handlers
        private void HandleStateChanged(object sender, MappingStateChangedEventArgs e)
        {
            SetVisible(e.CurrentState == MappingState.Mapping);
        }
        #endregion

        #region Helper Methods
        private void SetVisible(bool visible)
        {
            // The visualizer re-enables its renderer every frame, so toggle the visualizer itself.
            visualizer.enabled = visible;
            if (!visible)
            {
                particles.Clear();
            }
        }
        #endregion
    }
}
