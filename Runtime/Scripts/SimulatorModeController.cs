/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace MultiSet
{
    public class SimulatorModeController : MonoBehaviour
    {
        [Space(10)]
        private bool simulatorMode = false;
        public float walkingSpeed = 4f;
        public float turningSpeed = 3f;
        public float mouseSensitivity = 2f;

        [Header("Input Actions")]
        public InputAction moveAction;
        public InputAction lookAction;
        public InputAction sprintAction;
        public InputAction verticalMoveAction;

        private GameObject simulatorCamera;
        private Vector2 currentLookInput;
        private bool isRightMousePressed = false;

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

            // Initialize input actions if in simulator mode
            if (simulatorMode)
            {
                SetupInputActions();
            }
        }

        private void SetupInputActions()
        {
            // Movement (WASD)
            moveAction = new InputAction("Move", InputActionType.Value, "<Keyboard>/w");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            // Vertical movement (Q/E)
            verticalMoveAction = new InputAction("VerticalMove", InputActionType.Value);
            verticalMoveAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/e")
                .With("Negative", "<Keyboard>/q");

            // Mouse look
            lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");

            // Sprint
            sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");

            // Mouse button for camera rotation
            var rightMouseAction = new InputAction("RightMouse", InputActionType.Button, "<Mouse>/rightButton");
            rightMouseAction.performed += ctx => isRightMousePressed = true;
            rightMouseAction.canceled += ctx => isRightMousePressed = false;
            rightMouseAction.Enable();
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

        private void OnEnable()
        {
            if (simulatorMode)
            {
                moveAction?.Enable();
                lookAction?.Enable();
                sprintAction?.Enable();
                verticalMoveAction?.Enable();
            }
        }

        private void OnDisable()
        {
            if (simulatorMode)
            {
                moveAction?.Disable();
                lookAction?.Disable();
                sprintAction?.Disable();
                verticalMoveAction?.Disable();
            }
        }

        private void Update()
        {
            if (simulatorMode)
            {
                HandleMovement();
                HandleRotation();
            }
        }

        private void HandleMovement()
        {
            float currentSpeed = walkingSpeed;

            // Sprint logic
            if (sprintAction.IsPressed())
            {
                currentSpeed *= 1.5f; // Increase speed by 50% while sprinting
            }

            // Get movement input
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            float verticalInput = verticalMoveAction.ReadValue<float>();

            // Apply movement
            Vector3 movement = new Vector3(moveInput.x, verticalInput, moveInput.y) * currentSpeed * Time.deltaTime;
            transform.Translate(movement);
        }

        private void HandleRotation()
        {
            // Only rotate when right mouse button is held
            if (isRightMousePressed)
            {
                Vector2 lookInput = lookAction.ReadValue<Vector2>();
                
                float horizontal = lookInput.x * mouseSensitivity * turningSpeed * Time.deltaTime;
                float vertical = -lookInput.y * mouseSensitivity * turningSpeed * Time.deltaTime;

                transform.Rotate(0, horizontal, 0, Space.World);
                transform.Rotate(vertical, 0, 0, Space.Self);
            }
        }

        private void OnDestroy()
        {
            // Clean up input actions
            moveAction?.Dispose();
            lookAction?.Dispose();
            sprintAction?.Dispose();
            verticalMoveAction?.Dispose();
        }
    }
}