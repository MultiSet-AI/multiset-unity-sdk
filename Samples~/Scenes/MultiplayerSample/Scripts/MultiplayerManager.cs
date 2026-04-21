using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages MultipeerConnectivity-based multiplayer sessions.
/// Handles sending/receiving poses via the native MPC plugin,
/// coordinate conversion between map-global and local AR space,
/// and remote player lifecycle.
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    [Header("Settings")]
    public string playerName = "UnityPlayer";
    public bool isHost = true;
    public float sendRate = 20f;

    [Tooltip("If true, apply left/right-handed coordinate flip. " +
             "Set to false when using AR Foundation (already right-handed).")]
    public bool applyHandednessConversion = false;

    [Header("References")]
    public MultisetMultipeerBridge multipeerBridge;

    [Tooltip("PlayerPose prefab (BoundingBox + NameText) to spawn for MPC remote players")]
    public GameObject playerVisualPrefab;

    // Set after MultiSet localization
    private Transform mapSpaceTransform;
    private bool isLocalized;

    // AR camera — auto-found via Camera.main
    private Camera cachedCamera;

    // Remote MPC players
    private Dictionary<string, MPCRemotePlayerController> remotePlayers
        = new Dictionary<string, MPCRemotePlayerController>();

    // Local player color (generated once per session)
    private Color localPlayerColor;

    private float sendTimer;
    private const float StaleTimeout = 10f;

    void Start()
    {
        localPlayerColor = RandomVibrantColor();

        MultisetMultipeerBridge.OnDataReceivedEvent += OnDataReceived;
        MultisetMultipeerBridge.OnPeerConnectedEvent += OnPeerConnected;
        MultisetMultipeerBridge.OnPeerStateChangedEvent += OnPeerStateChanged;
    }

    void OnDestroy()
    {
        MultisetMultipeerBridge.OnDataReceivedEvent -= OnDataReceived;
        MultisetMultipeerBridge.OnPeerConnectedEvent -= OnPeerConnected;
        MultisetMultipeerBridge.OnPeerStateChangedEvent -= OnPeerStateChanged;

        if (multipeerBridge != null)
            multipeerBridge.Disconnect();
    }

    /// <summary>
    /// Start the MPC session as host (advertiser) or client (browser).
    /// </summary>
    public void StartSession()
    {
        if (multipeerBridge == null)
        {
            Debug.LogError("[MPC Manager] multipeerBridge is not assigned!");
            return;
        }

        if (isHost)
            multipeerBridge.StartHosting(playerName);
        else
            multipeerBridge.StartBrowsing(playerName);
    }

    /// <summary>
    /// Called after successful MultiSet localization.
    /// Captures the map anchor transform for coordinate conversion.
    /// </summary>
    public void OnLocalizationSuccess(Transform gizmoTransform)
    {
        mapSpaceTransform = gizmoTransform;
        isLocalized = true;
        Debug.Log("[MPC Manager] Localized — pose sharing enabled");
    }

    void Update()
    {
        if (isLocalized && multipeerBridge != null && multipeerBridge.IsConnected)
        {
            sendTimer += Time.deltaTime;
            if (sendTimer >= 1f / sendRate)
            {
                sendTimer = 0f;
                SendCurrentPose();
            }
        }

        UpdateRemotePlayers();
    }

    #region Coordinate Conversion

    private PoseUpdate CreateGlobalPose(Camera cam)
    {
        Vector3 camPos = cam.transform.position;
        Quaternion camRot = cam.transform.rotation;

        // Convert camera pose from Unity world space to map-global space
        Vector3 globalPos = mapSpaceTransform.InverseTransformPoint(camPos);
        Quaternion globalRot = Quaternion.Inverse(mapSpaceTransform.rotation) * camRot;

        if (applyHandednessConversion)
        {
            // Unity left-handed → ARKit right-handed
            return new PoseUpdate
            {
                positionX = globalPos.x,
                positionY = globalPos.y,
                positionZ = -globalPos.z,
                rotationX = -globalRot.x,
                rotationY = -globalRot.y,
                rotationZ = globalRot.z,
                rotationW = globalRot.w,
                isLocalized = true
            };
        }

        return new PoseUpdate
        {
            positionX = globalPos.x,
            positionY = globalPos.y,
            positionZ = globalPos.z,
            rotationX = globalRot.x,
            rotationY = globalRot.y,
            rotationZ = globalRot.z,
            rotationW = globalRot.w,
            isLocalized = true
        };
    }

    private (Vector3 position, Quaternion rotation) ConvertGlobalToLocal(PoseUpdate pose)
    {
        Vector3 globalPos;
        Quaternion globalRot;

        if (applyHandednessConversion)
        {
            // ARKit right-handed → Unity left-handed
            globalPos = new Vector3(pose.positionX, pose.positionY, -pose.positionZ);
            globalRot = new Quaternion(-pose.rotationX, -pose.rotationY, pose.rotationZ, pose.rotationW);
        }
        else
        {
            globalPos = new Vector3(pose.positionX, pose.positionY, pose.positionZ);
            globalRot = new Quaternion(pose.rotationX, pose.rotationY, pose.rotationZ, pose.rotationW);
        }

        // Convert from map-global to Unity local (world) space
        Vector3 localPos = mapSpaceTransform.TransformPoint(globalPos);
        Quaternion localRot = mapSpaceTransform.rotation * globalRot;

        return (localPos, localRot);
    }

    #endregion

    #region Send

    private void SendCurrentPose()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;
        if (cachedCamera == null || mapSpaceTransform == null) return;

        PoseUpdate pose = CreateGlobalPose(cachedCamera);

        string payloadJson = JsonUtility.ToJson(pose);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        NetworkMessage message = new NetworkMessage
        {
            type = 1,
            senderID = playerName,
            payload = Convert.ToBase64String(payloadBytes)
        };

        string messageJson = JsonUtility.ToJson(message);
        byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);

        multipeerBridge.SendData(messageBytes, reliable: false);
    }

    private void SendPlayerInfo()
    {
        PlayerInfo info = new PlayerInfo
        {
            playerName = playerName,
            colorR = localPlayerColor.r,
            colorG = localPlayerColor.g,
            colorB = localPlayerColor.b
        };

        string payloadJson = JsonUtility.ToJson(info);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        NetworkMessage message = new NetworkMessage
        {
            type = 2,
            senderID = playerName,
            payload = Convert.ToBase64String(payloadBytes)
        };

        string messageJson = JsonUtility.ToJson(message);
        byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);

        multipeerBridge.SendData(messageBytes, reliable: true);
    }

    #endregion

    #region Receive

    private void OnDataReceived(string jsonString)
    {
        try
        {
            NetworkMessage message = JsonUtility.FromJson<NetworkMessage>(jsonString);
            byte[] payloadBytes = Convert.FromBase64String(message.payload);
            string payloadJson = Encoding.UTF8.GetString(payloadBytes);

            switch (message.type)
            {
                case 1:
                    PoseUpdate pose = JsonUtility.FromJson<PoseUpdate>(payloadJson);
                    HandlePoseUpdate(message.senderID, pose);
                    break;
                case 2:
                    PlayerInfo info = JsonUtility.FromJson<PlayerInfo>(payloadJson);
                    HandlePlayerInfo(message.senderID, info);
                    break;
                default:
                    Debug.LogWarning($"[MPC Manager] Unknown message type: {message.type}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MPC Manager] Failed to parse message: {e.Message}");
        }
    }

    private void HandlePoseUpdate(string senderID, PoseUpdate pose)
    {
        if (!isLocalized || mapSpaceTransform == null)
        {
            Debug.LogWarning($"[MPC Manager] Ignoring pose from {senderID} — isLocalized={isLocalized}, mapSpace={mapSpaceTransform != null}");
            return;
        }

        var player = GetOrCreatePlayer(senderID);
        player.isLocalized = pose.isLocalized;

        var (localPos, localRot) = ConvertGlobalToLocal(pose);
        player.SetTarget(localPos, localRot);
        Debug.Log($"[MPC Manager] Pose applied for {senderID} → local({localPos.x:F2},{localPos.y:F2},{localPos.z:F2}) active={player.gameObject.activeSelf}");
    }

    private void HandlePlayerInfo(string senderID, PlayerInfo info)
    {
        var player = GetOrCreatePlayer(senderID);
        player.playerName = info.playerName;
        player.playerColor = new Color(info.colorR, info.colorG, info.colorB);
        player.UpdateVisuals();
        Debug.Log($"[MPC Manager] Player info received: {info.playerName}");
    }

    private void OnPeerConnected(string peerName)
    {
        Debug.Log($"[MPC Manager] Peer connected: {peerName}");
        SendPlayerInfo();
    }

    private void OnPeerStateChanged(string peer, string state)
    {
        Debug.Log($"[MPC Manager] Peer {peer} state: {state}");

        if (state == "disconnected" && remotePlayers.ContainsKey(peer))
        {
            Destroy(remotePlayers[peer].gameObject);
            remotePlayers.Remove(peer);
        }
    }

    #endregion

    #region Remote Player Management

    private MPCRemotePlayerController GetOrCreatePlayer(string peerID)
    {
        if (remotePlayers.TryGetValue(peerID, out var existing))
            return existing;

        GameObject go;
        if (playerVisualPrefab != null)
        {
            Debug.Log($"[MPC Manager] Instantiating playerVisualPrefab for {peerID}");

            // Instantiate inactive to prevent Netcode components from initializing
            playerVisualPrefab.SetActive(false);
            go = Instantiate(playerVisualPrefab);
            playerVisualPrefab.SetActive(true);

            go.name = $"MPCRemotePlayer_{peerID}";

            // Remove Netcode components — MPC players are not part of Netcode
            var netPlayer = go.GetComponent<NetworkARPlayer>();
            if (netPlayer != null)
            {
                DestroyImmediate(netPlayer);
                Debug.Log("[MPC Manager] Stripped NetworkARPlayer from MPC player");
            }
            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                DestroyImmediate(netObj);
                Debug.Log("[MPC Manager] Stripped NetworkObject from MPC player");
            }

            go.SetActive(true);

            // Activate all child visuals (BoundingBox, Code, Player)
            foreach (Transform child in go.transform)
                child.gameObject.SetActive(true);

            Debug.Log($"[MPC Manager] MPC player '{go.name}' created with {go.transform.childCount} children, active={go.activeSelf}");
        }
        else
        {
            go = new GameObject($"MPCRemotePlayer_{peerID}");
            Debug.LogError("[MPC Manager] playerVisualPrefab is NOT assigned! Assign PlayerPose prefab in Inspector.");
        }

        var controller = go.AddComponent<MPCRemotePlayerController>();
        controller.peerID = peerID;
        controller.Initialize(peerID, Color.cyan); // Default until PlayerInfo arrives
        remotePlayers[peerID] = controller;
        return controller;
    }

    private void UpdateRemotePlayers()
    {
        if (!isLocalized) return;

        List<string> toRemove = null;

        foreach (var kvp in remotePlayers)
        {
            var player = kvp.Value;

            // Only check staleness if we've received at least one pose
            bool stale = player.lastUpdateTime > 0f && Time.time - player.lastUpdateTime > StaleTimeout;
            bool shouldShow = player.isLocalized && !stale;

            player.gameObject.SetActive(shouldShow);

            if (shouldShow)
                player.SmoothUpdate(Time.deltaTime);
        }

        if (toRemove != null)
        {
            foreach (var key in toRemove)
            {
                Destroy(remotePlayers[key].gameObject);
                remotePlayers.Remove(key);
            }
        }
    }

    /// <summary>
    /// Get all remote players for external display (e.g., PoseDisplayUI).
    /// </summary>
    public IReadOnlyDictionary<string, MPCRemotePlayerController> RemotePlayers => remotePlayers;

    public bool IsLocalized => isLocalized;

    #endregion

    private static Color RandomVibrantColor()
    {
        float hue = UnityEngine.Random.Range(0f, 1f);
        float saturation = UnityEngine.Random.Range(0.6f, 1f);
        float value = UnityEngine.Random.Range(0.7f, 1f);
        return Color.HSVToRGB(hue, saturation, value);
    }
}
