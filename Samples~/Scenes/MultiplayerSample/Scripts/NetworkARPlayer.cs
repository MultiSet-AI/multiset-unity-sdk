using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class NetworkARPlayer : NetworkBehaviour
{
    public Transform arCamera;
    private Transform mapSpace;

    // -- Pose sync --
    private NetworkVariable<Vector3> netPosition =
        new NetworkVariable<Vector3>(
            writePerm: NetworkVariableWritePermission.Owner);

    private NetworkVariable<Quaternion> netRotation =
        new NetworkVariable<Quaternion>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> netIsLocalized =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    private NetworkVariable<int> playerIndex =
        new NetworkVariable<int>(
            writePerm: NetworkVariableWritePermission.Server);

    // -- Player identity sync --
    private NetworkVariable<FixedString64Bytes> netPlayerName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    // Static field — set by NetworkUI before connecting
    public static string LocalPlayerName = "Player";

    private float sendInterval = 0.05f;
    private float lastSendTime;

    private static int nextPlayerIndex = 1;

    // References to child visual elements (BoundingBox + Code/NameText + Player humanoid) already in the prefab
    public TMP_Text nameText;
    private GameObject boundingBox;
    private GameObject codeContainer;
    private GameObject playerModel;
    private bool nameInitialized;
    private NetworkARPlayer cachedLocalPlayer;
    private Camera cachedCamera;

    // Occlusion raycast — MapMesh layer
    private int mapMeshLayerMask;
    private float occlusionCheckInterval = 0.2f;
    private float lastOcclusionCheckTime;
    private bool isOccluded;

    public Vector3 Position => netPosition.Value;
    public Quaternion Rotation => netRotation.Value;
    public int PlayerIndex => playerIndex.Value;
    public bool IsLocalized => netIsLocalized.Value;
    public string PlayerName => netPlayerName.Value.ToString();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            playerIndex.Value = nextPlayerIndex++;
        }

        // Cache references to child visual elements
        var bbTransform = transform.Find("BoundingBox");
        if (bbTransform != null) boundingBox = bbTransform.gameObject;
        var codeTransform = transform.Find("Code");
        if (codeTransform != null) codeContainer = codeTransform.gameObject;
        var playerTransform = transform.Find("Player");
        if (playerTransform != null) playerModel = playerTransform.gameObject;

        // Setup occlusion raycast layer mask
        int mapMeshLayer = LayerMask.NameToLayer("CollisionMesh");
        mapMeshLayerMask = mapMeshLayer >= 0 ? 1 << mapMeshLayer : 0;

        if (IsOwner)
        {
            // Hide visuals on the local player — we don't render ourselves
            SetVisualsActive(false);

            // Push local identity to network
            netPlayerName.Value = new FixedString64Bytes(LocalPlayerName);
        }
        else
        {
            UpdateNameText();
        }

        // Update name if it changes after spawn
        netPlayerName.OnValueChanged += OnNameChanged;
    }

    public override void OnNetworkDespawn()
    {
        netPlayerName.OnValueChanged -= OnNameChanged;

        base.OnNetworkDespawn();

        if (IsServer)
        {
            nextPlayerIndex = 1;
        }
    }

    private void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes curr)
    {
        if (!IsOwner)
            UpdateNameText();
    }

    /// <summary>
    /// Called by LocalizationSuccessDataHandler when localization succeeds on this device.
    /// </summary>
    public void SetLocalized(Transform mapSpaceTransform)
    {
        mapSpace = mapSpaceTransform;

        if (IsOwner)
        {
            netIsLocalized.Value = true;
        }
    }

    private void SetVisualsActive(bool active)
    {
        if (boundingBox != null) boundingBox.SetActive(active);
        if (codeContainer != null) codeContainer.SetActive(active);
        if (playerModel != null) playerModel.SetActive(active);
    }

    /// <summary>
    /// Shows BoundingBox+Name when visible, or Player humanoid when occluded by map mesh.
    /// </summary>
    private void ApplyOcclusionVisuals(bool occluded)
    {
        // BoundingBox + Name always visible
        if (boundingBox != null) boundingBox.SetActive(true);
        if (codeContainer != null) codeContainer.SetActive(true);
        // Humanoid skeleton only shown when occluded (behind wall)
        if (playerModel != null) playerModel.SetActive(occluded);
    }

    private void UpdateNameText()
    {
        if (nameText == null) return;

        string name = netPlayerName.Value.ToString();
        if (string.IsNullOrEmpty(name)) return;

        nameText.text = name;
        nameInitialized = true;
    }

    void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            UpdateOwner();
        }
        else
        {
            UpdateRemote();
        }
    }

    private void UpdateOwner()
    {
        if (arCamera == null)
        {
            var cam = Camera.main;
            if (cam != null) arCamera = cam.transform;
        }

        if (!netIsLocalized.Value || mapSpace == null || arCamera == null) return;

        if (Time.time - lastSendTime > sendInterval)
        {
            netPosition.Value = mapSpace.InverseTransformPoint(arCamera.position);
            netRotation.Value = Quaternion.Inverse(mapSpace.rotation) * arCamera.rotation;
            lastSendTime = Time.time;
        }
    }

    private void UpdateRemote()
    {
        // Remote player instances need mapSpace — grab from local player
        if (mapSpace == null)
        {
            if (cachedLocalPlayer == null)
                cachedLocalPlayer = GetLocalPlayer();
            if (cachedLocalPlayer != null && cachedLocalPlayer.mapSpace != null)
                mapSpace = cachedLocalPlayer.mapSpace;
        }

        bool shouldShow = netIsLocalized.Value && mapSpace != null;

        if (!shouldShow)
        {
            SetVisualsActive(false);
            return;
        }

        // Try initializing name if it wasn't available at spawn time
        if (!nameInitialized)
            UpdateNameText();

        // Convert map-global pose to local AR space
        Vector3 worldPos = mapSpace.TransformPoint(netPosition.Value);
        transform.position = Vector3.Lerp(transform.position, worldPos, Time.deltaTime * 15f);

        // Check occlusion at a reduced rate to save performance
        if (Time.time - lastOcclusionCheckTime > occlusionCheckInterval)
        {
            lastOcclusionCheckTime = Time.time;
            isOccluded = CheckOcclusion();
        }

        // Toggle visuals based on line-of-sight
        ApplyOcclusionVisuals(isOccluded);

        // Billboard the bounding box / name toward camera (only when visible)
        if (!isOccluded)
            BillboardToCamera();
    }

    /// <summary>
    /// Raycasts from camera to this remote player through the MapMesh layer.
    /// Returns true if the map mesh is blocking line of sight.
    /// </summary>
    private bool CheckOcclusion()
    {
        if (mapMeshLayerMask == 0) return false;

        if (cachedCamera == null)
            cachedCamera = Camera.main;
        if (cachedCamera == null) return false;

        Vector3 cameraPos = cachedCamera.transform.position;
        Vector3 playerPos = transform.position;
        Vector3 direction = playerPos - cameraPos;
        float distance = direction.magnitude;

        if (distance < 0.01f) return false;

        // Cast ray from camera toward remote player, only checking MapMesh layer
        return Physics.Raycast(cameraPos, direction, distance, mapMeshLayerMask);
    }

    private void BillboardToCamera()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;
        if (cachedCamera == null) return;

        Vector3 dirToCamera = cachedCamera.transform.position - transform.position;
        if (dirToCamera.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(-dirToCamera, Vector3.up);
    }

    private static NetworkARPlayer GetLocalPlayer()
    {
        var players = FindObjectsByType<NetworkARPlayer>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsOwner) return p;
        }
        return null;
    }
}
