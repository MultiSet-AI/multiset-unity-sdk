/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;

namespace MultiSet
{
    /// <summary>
    /// Helper script that attaches to the AR Camera to detect trigger collisions.
    /// This script receives OnTriggerEnter/Exit events and notifies VpsLocalizationTrigger.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ARCameraTriggerDetector : MonoBehaviour
    {
        [Tooltip("Reference to the VpsLocalizationTrigger script")]
        public VpsLocalizationTrigger localizationTrigger;

        private void OnTriggerEnter(Collider other)
        {
            if (localizationTrigger != null)
            {
                localizationTrigger.OnARCameraEnteredTrigger(other);
            }
            else
            {
                Debug.LogWarning("ARCameraTriggerDetector: localizationTrigger reference is null!");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (localizationTrigger != null)
            {
                localizationTrigger.OnARCameraExitedTrigger(other);
            }
            else
            {
                Debug.LogWarning("ARCameraTriggerDetector: localizationTrigger reference is null!");
            }
        }
    }
}
