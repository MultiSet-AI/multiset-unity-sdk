/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEngine;
using UnityEngine.AI;

/**
 * Enables visualisation of the NavMesh inside the editor and in app.
 * Helps to debug navigation and place NavMeshObstacles in the Editor.
 */
public class ShowNavMesh : MonoBehaviour
{
    public static ShowNavMesh instance;
    [Tooltip("Color of the NavMesh surface visualization")]

    public Color surfaceColor = new Color(0, 1, 0, 0.3f); // Semi-transparent green

    [Tooltip("Height offset for the visualization mesh")]
    public float heightOffset = 0.01f;

    private MeshFilter meshFilter;

    private MeshRenderer meshRenderer;

    [Tooltip("Enable to see NavMesh")]
    public bool IsNavMeshShown = true;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Add MeshFilter and MeshRenderer components if they don't exist
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Create the NavMesh visualization
        GenerateNavMeshVisualization();
    }

    void Update()
    {
        SetNavMeshVisibility(IsNavMeshShown);
    }

    // set visibility of NavMesh
    public void SetNavMeshVisibility(bool isShown)
    {
        meshRenderer.enabled = isShown;
    }

    public void GenerateNavMeshVisualization()
    {
        SetNavMeshVisibility(IsNavMeshShown);

        // Create a new Mesh
        Mesh navMeshMesh = new Mesh();
        navMeshMesh.name = "NavMesh Surface Visualization";

        // Get NavMesh data
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        // Adjust vertices to add a small height offset
        Vector3[] vertices = new Vector3[triangulation.vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = triangulation.vertices[i] + Vector3.up * heightOffset;
        }

        // Set mesh data
        navMeshMesh.vertices = vertices;
        navMeshMesh.triangles = triangulation.indices;

        // Recalculate mesh normals and bounds
        navMeshMesh.RecalculateNormals();
        navMeshMesh.RecalculateBounds();

        // Assign the mesh to the MeshFilter
        meshFilter.mesh = navMeshMesh;

        // Create a material for visualization
        Material surfaceMaterial = new Material(Shader.Find("Standard"));
        surfaceMaterial.color = surfaceColor;
        surfaceMaterial.SetFloat("_Mode", 2); // Fade rendering mode
        surfaceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        surfaceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        surfaceMaterial.SetInt("_ZWrite", 0);
        surfaceMaterial.DisableKeyword("_ALPHATEST_ON");
        surfaceMaterial.EnableKeyword("_ALPHABLEND_ON");
        surfaceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        surfaceMaterial.renderQueue = 3000;

        // Assign material to MeshRenderer
        meshRenderer.material = surfaceMaterial;
    }
}