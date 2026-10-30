/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can't re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;

namespace MultiSet.Samples
{
    // Draws the non-LiDAR scan's coverage cloud as one particle system, instead of a GameObject
    // per point. The cloud is display-only; nothing drawn here is packaged.
    [RequireComponent(typeof(ParticleSystem))]
    public class CoveragePointRenderer : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Manager Reference")]
        [SerializeField] private NonLiDARMappingManager mappingManager;

        [Header("Appearance")]
        [SerializeField] private Material pointMaterial;
        [SerializeField] private Color pointColor = new(1f, 0.86f, 0.1f, 1f);
        [SerializeField] private float pointSize = 0.03f;
        // Fractions of the viewport: points near the camera stay dots, distant ones stay visible.
        [SerializeField] private float minScreenSize = 0.004f;
        [SerializeField] private float maxScreenSize = 0.018f;
        #endregion

        #region Private Fields
        private ParticleSystem particles;
        private ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[0];
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            particles = GetComponent<ParticleSystem>();
            ConfigureParticleSystem();
        }

        private void OnEnable()
        {
            if (mappingManager == null)
            {
                mappingManager = FindFirstObjectByType<NonLiDARMappingManager>();
            }

            if (mappingManager != null)
            {
                mappingManager.OnCoveragePointsChanged += HandleCoveragePointsChanged;
                mappingManager.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (mappingManager != null)
            {
                mappingManager.OnCoveragePointsChanged -= HandleCoveragePointsChanged;
                mappingManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Start()
        {
            // Points are in AR session space, which is the AR camera's parent space (its pose is local).
            var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            Camera arCamera = origin != null && origin.Camera != null ? origin.Camera : Camera.main;
            if (arCamera != null && arCamera.transform.parent != null)
            {
                transform.SetParent(arCamera.transform.parent, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }

            particles.Play();
        }
        #endregion

        #region Event Handlers
        private void HandleCoveragePointsChanged(object sender, CoveragePointsEventArgs e)
        {
            int count = e.Points.Count;
            if (buffer.Length < count)
            {
                buffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(count)];
            }

            for (int i = 0; i < count; i++)
            {
                buffer[i].position = e.Points[i];
                buffer[i].startColor = pointColor;
                buffer[i].startSize = pointSize;
                buffer[i].startLifetime = float.MaxValue;
                buffer[i].remainingLifetime = float.MaxValue;
            }

            particles.SetParticles(buffer, count);
        }

        private void HandleStateChanged(object sender, MappingStateChangedEventArgs e)
        {
            if (e.CurrentState == MappingState.Ready || e.CurrentState == MappingState.Mapping && e.PreviousState != MappingState.Mapping)
            {
                particles.Clear();
            }
        }
        #endregion

        #region Helper Methods
        private void ConfigureParticleSystem()
        {
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = NonLiDARLimits.PointCapacity;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.scalingMode = ParticleSystemScalingMode.Local;

            var emission = particles.emission;
            emission.enabled = false;

            var shape = particles.shape;
            shape.enabled = false;

            var particleRenderer = GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.minParticleSize = minScreenSize;
            particleRenderer.maxParticleSize = maxScreenSize;
            if (pointMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
                pointMaterial = new Material(shader) { color = pointColor };
            }
            particleRenderer.sharedMaterial = pointMaterial;
        }
        #endregion
    }
}
