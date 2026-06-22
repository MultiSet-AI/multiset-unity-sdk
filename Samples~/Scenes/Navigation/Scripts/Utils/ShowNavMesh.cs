using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

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

        // Create a material for visualization based on render pipeline
        Material surfaceMaterial = CreateNavMeshMaterial();

        // Assign material to MeshRenderer
        meshRenderer.material = surfaceMaterial;
    }

    private Material CreateNavMeshMaterial()
    {
        Material surfaceMaterial;

        RenderPipelineAsset currentRP = GraphicsSettings.currentRenderPipeline;

        if (currentRP != null && currentRP.GetType().Name.Contains("UniversalRenderPipelineAsset"))
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Sprites/Default");
            }

            surfaceMaterial = new Material(urpShader);
            surfaceMaterial.color = surfaceColor;

            if (urpShader.name.Contains("Unlit"))
            {
                surfaceMaterial.SetFloat("_Surface", 1); // Transparent
                surfaceMaterial.SetFloat("_Blend", 0); // Alpha
                surfaceMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                surfaceMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                surfaceMaterial.SetFloat("_ZWrite", 0);
                surfaceMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                surfaceMaterial.EnableKeyword("_ALPHABLEND_ON");
            }
        }
        else
        {
            surfaceMaterial = new Material(Shader.Find("Standard"));
            surfaceMaterial.color = surfaceColor;
            surfaceMaterial.SetFloat("_Mode", 2);
            surfaceMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            surfaceMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            surfaceMaterial.SetInt("_ZWrite", 0);
            surfaceMaterial.DisableKeyword("_ALPHATEST_ON");
            surfaceMaterial.EnableKeyword("_ALPHABLEND_ON");
            surfaceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        surfaceMaterial.renderQueue = 3000;
        return surfaceMaterial;
    }
}