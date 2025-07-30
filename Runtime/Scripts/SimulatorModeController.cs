/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace MultiSet
{
    public class SimulatorModeController : MonoBehaviour
    {
        [Space(10)]
        private bool simulatorMode = false;
        public float walkingSpeed = 4f;
        public float turningSpeed = 3f;

        [Header("Key Bindings")]
        public KeyCode moveForwardKey = KeyCode.W;
        public KeyCode moveBackwardKey = KeyCode.S;
        public KeyCode moveRightKey = KeyCode.D;
        public KeyCode moveLeftKey = KeyCode.A;
        public KeyCode moveUpwardKey = KeyCode.E;
        public KeyCode moveDownwardKey = KeyCode.Q;
        public KeyCode sprintKey = KeyCode.LeftShift;

        private GameObject simulatorCamera;

        private void Awake()
        {
#if UNITY_EDITOR
            // Automatically true when running in the Unity Editor
            simulatorMode = true;
#elif UNITY_ANDROID || UNITY_IOS
            // Automatically false on mobile platforms
            simulatorMode = false;
#else
            // Default behavior for other platforms
            simulatorMode = false;
#endif
        }

        private void Start()
        {
            if (!simulatorMode)
                return;

            //Find TrackedPoseDriver and disable it while in simulator mode
            TrackedPoseDriver[] trackedPoseDrivers = FindObjectsByType<TrackedPoseDriver>(FindObjectsSortMode.None);
            foreach (TrackedPoseDriver trackedPoseDriver in trackedPoseDrivers)
            {
                trackedPoseDriver.enabled = false;
            }
        }

        private void Update()
        {
            if (simulatorMode)
            {
                HandleMovement();
            }
        }

        private void HandleMovement()
        {
            float currentSpeed = walkingSpeed;

            // Sprint logic
            if (Input.GetKey(sprintKey))
            {
                currentSpeed *= 1.5f; // Increase speed by 50% while sprinting
            }

            // Forward and backward movement
            if (Input.GetKey(moveForwardKey))
            {
                transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
            }
            if (Input.GetKey(moveBackwardKey))
            {
                transform.Translate(Vector3.back * currentSpeed * Time.deltaTime);
            }

            // Right and left movement
            if (Input.GetKey(moveRightKey))
            {
                transform.Translate(Vector3.right * currentSpeed * Time.deltaTime);
            }
            if (Input.GetKey(moveLeftKey))
            {
                transform.Translate(Vector3.left * currentSpeed * Time.deltaTime);
            }

            // Upward and downward movement
            if (Input.GetKey(moveUpwardKey))
            {
                transform.Translate(Vector3.up * currentSpeed * Time.deltaTime);
            }
            if (Input.GetKey(moveDownwardKey))
            {
                transform.Translate(Vector3.down * currentSpeed * Time.deltaTime);
            }

            // Rotate the camera based on mouse movement
            if (Input.GetMouseButton(1))
            {
                float horizontal = Input.GetAxis("Mouse X") * turningSpeed;
                float vertical = -Input.GetAxis("Mouse Y") * turningSpeed;

                transform.Rotate(0, horizontal, 0, Space.World);
                transform.Rotate(vertical, 0, 0, Space.Self);
            }
        }
    }
}