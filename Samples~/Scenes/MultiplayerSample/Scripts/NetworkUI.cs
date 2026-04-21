using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Optional: status text to show connection state")]
    public TextMeshProUGUI statusText;

    [Tooltip("TMP_InputField for the host IP address")]
    public TMP_InputField ipInputField;

    [Tooltip("TMP_InputField for the player display name")]
    public TMP_InputField nameInputField;

    [Tooltip("GameObject containing Start Host / Start Client buttons")]
    public GameObject connectPanel;

    [Tooltip("GameObject containing Disconnect button")]
    public GameObject disconnectPanel;

    [Header("MPC (MultipeerConnectivity)")]
    [Tooltip("Optional: MultiplayerManager for native iOS peer connections")]
    public MultiplayerManager multiplayerManager;

    [Header("Settings")]
    [Tooltip("Default IP address")]
    public string defaultIP = "192.168.1.7";

    [Tooltip("Default player name")]
    public string defaultName = "Player";

    void Start()
    {
        if (ipInputField != null)
        {
            ipInputField.text = PlayerPrefs.GetString("HostIP", defaultIP);
            ipInputField.contentType = TMP_InputField.ContentType.Standard;
            ipInputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Enter Host IP...";
            ipInputField.onEndEdit.AddListener(SaveIP);
        }

        if (nameInputField != null)
        {
            nameInputField.text = PlayerPrefs.GetString("PlayerName", defaultName);
            nameInputField.contentType = TMP_InputField.ContentType.Standard;
            nameInputField.characterLimit = 20;
            nameInputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Enter your name...";
            nameInputField.onEndEdit.AddListener(SaveName);
        }

        ShowConnectUI();
    }

    private void ApplyPlayerConfig()
    {
        string playerName = defaultName;
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
            playerName = nameInputField.text.Trim();

        NetworkARPlayer.LocalPlayerName = playerName;
    }

    public void StartHost()
    {
        ApplyPlayerConfig();
        SetTransportAddress("0.0.0.0");
        NetworkManager.Singleton.StartHost();
        SetStatus($"Hosting on port {GetTransport().ConnectionData.Port}...\nWaiting for client.");
        ShowDisconnectUI();

        // Also start MPC hosting for native iOS clients
        StartMPCHost();
    }

    public void StartClient()
    {
        ApplyPlayerConfig();
        string ip = ipInputField != null ? ipInputField.text.Trim() : defaultIP;
        if (string.IsNullOrEmpty(ip)) ip = defaultIP;

        SetTransportAddress(ip);
        NetworkManager.Singleton.StartClient();
        SetStatus($"Connecting to {ip}...");
        ShowDisconnectUI();
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.Shutdown();
        DisconnectMPC();
        SetStatus("Disconnected.");
        ShowConnectUI();
    }

    /// <summary>
    /// Start MPC hosting so native iOS clients can discover and connect.
    /// Can be called alongside StartHost() for dual-mode operation.
    /// </summary>
    public void StartMPCHost()
    {
        if (multiplayerManager == null) return;

        string name = defaultName;
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
            name = nameInputField.text.Trim();

        multiplayerManager.playerName = name;
        multiplayerManager.isHost = true;
        multiplayerManager.StartSession();
        Debug.Log($"[NetworkUI] MPC hosting started as {name}");
    }

    public void DisconnectMPC()
    {
        if (multiplayerManager != null && multiplayerManager.multipeerBridge != null)
            multiplayerManager.multipeerBridge.Disconnect();
    }

    private void ShowConnectUI()
    {
        if (connectPanel != null) connectPanel.SetActive(true);
        if (disconnectPanel != null) disconnectPanel.SetActive(false);
        if (ipInputField != null) ipInputField.gameObject.SetActive(true);
        if (nameInputField != null) nameInputField.gameObject.SetActive(true);
    }

    private void ShowDisconnectUI()
    {
        if (connectPanel != null) connectPanel.SetActive(false);
        if (disconnectPanel != null) disconnectPanel.SetActive(true);
        if (ipInputField != null) ipInputField.gameObject.SetActive(false);
        if (nameInputField != null) nameInputField.gameObject.SetActive(false);
    }

    private void SaveIP(string ip)
    {
        PlayerPrefs.SetString("HostIP", ip);
        PlayerPrefs.Save();
    }

    private void SaveName(string name)
    {
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
    }

    private void SetTransportAddress(string ip)
    {
        var transport = GetTransport();
        if (transport != null)
        {
            ushort port = transport.ConnectionData.Port;
            transport.SetConnectionData(ip, port);
        }
    }

    private UnityTransport GetTransport()
    {
        return NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    void Update()
    {
        if (statusText == null) return;
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClientsIds.Count > 1)
        {
            statusText.text = $"Connected — {NetworkManager.Singleton.ConnectedClientsIds.Count} players";
        }
        else if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient)
        {
            statusText.text = "Connected to host";
        }
    }
}
