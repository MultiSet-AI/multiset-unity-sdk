using UnityEngine;

/// <summary>
/// Configures map mesh children for occlusion raycasting:
/// 1. Assigns all children to the MapMesh layer
/// 2. Adds MeshColliders for Physics.Raycast
/// 3. Excludes MapMesh layer from the camera's culling mask so the mesh
///    never renders (no depth writes, no visual occlusion of player visuals)
/// Attach this to the Map Space GameObject in the scene.
/// </summary>
public class MapMeshColliderSetup : MonoBehaviour
{
    private const string MapMeshLayerName = "CollisionMesh";
    private int mapMeshLayer = -1;
    private bool cameraConfigured;

    void Start()
    {
        mapMeshLayer = LayerMask.NameToLayer(MapMeshLayerName);
        if (mapMeshLayer < 0)
            Debug.LogError($"[MapMeshColliderSetup] Layer '{MapMeshLayerName}' not found!");
    }

    void LateUpdate()
    {
        if (mapMeshLayer < 0) return;

        // Exclude MapMesh layer from the camera so it never renders
        // (no depth writes, no visual occlusion — but colliders still work for raycasts)
        if (!cameraConfigured)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.cullingMask &= ~(1 << mapMeshLayer);
                cameraConfigured = true;
                Debug.Log($"[MapMeshColliderSetup] Excluded layer '{MapMeshLayerName}' from camera culling mask");
            }
        }

        // Assign layer + add colliders to any new mesh children (loaded async by SDK)
        foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;

            if (mf.gameObject.layer != mapMeshLayer)
            {
                mf.gameObject.layer = mapMeshLayer;
                Debug.Log($"[MapMeshColliderSetup] Set layer on '{mf.gameObject.name}'");
            }

            if (mf.GetComponent<MeshCollider>() == null)
            {
                var col = mf.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = mf.sharedMesh;
                Debug.Log($"[MapMeshColliderSetup] Added MeshCollider on '{mf.gameObject.name}'");
            }
        }

        // Also set layer on any renderers (SkinnedMesh, etc.) that don't have MeshFilter
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.gameObject.layer != mapMeshLayer)
                r.gameObject.layer = mapMeshLayer;
        }
    }
}
