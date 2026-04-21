using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// C# bridge to the native iOS MultipeerConnectivity plugin.
/// Must be attached to a GameObject named exactly "MultisetMultipeerReceiver"
/// for UnitySendMessage callbacks to work.
/// </summary>
public class MultisetMultipeerBridge : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void _MultisetStartHosting(string displayName);
    [DllImport("__Internal")] private static extern void _MultisetStartBrowsing(string displayName);
    [DllImport("__Internal")] private static extern void _MultisetSendData(byte[] data, int length, bool reliable);
    [DllImport("__Internal")] private static extern void _MultisetDisconnect();
    [DllImport("__Internal")] private static extern bool _MultisetIsConnected();
    [DllImport("__Internal")] private static extern int _MultisetConnectedPeerCount();
#endif

    public static event Action<string> OnPeerConnectedEvent;
    public static event Action<string, string> OnPeerStateChangedEvent;
    public static event Action<string> OnDataReceivedEvent;

    public bool IsConnected
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _MultisetIsConnected();
#else
            return false;
#endif
        }
    }

    public int ConnectedPeerCount
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _MultisetConnectedPeerCount();
#else
            return 0;
#endif
        }
    }

    public void StartHosting(string displayName)
    {
#if UNITY_IOS && !UNITY_EDITOR
        _MultisetStartHosting(displayName);
#endif
        Debug.Log($"[MPC] StartHosting: {displayName}");
    }

    public void StartBrowsing(string displayName)
    {
#if UNITY_IOS && !UNITY_EDITOR
        _MultisetStartBrowsing(displayName);
#endif
        Debug.Log($"[MPC] StartBrowsing: {displayName}");
    }

    public void SendData(byte[] data, bool reliable)
    {
#if UNITY_IOS && !UNITY_EDITOR
        _MultisetSendData(data, data.Length, reliable);
#endif
    }

    public void Disconnect()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _MultisetDisconnect();
#endif
        Debug.Log("[MPC] Disconnected");
    }

    // Called by UnitySendMessage from native plugin
    void OnPeerConnected(string peerName)
    {
        Debug.Log($"[MPC] Peer connected: {peerName}");
        OnPeerConnectedEvent?.Invoke(peerName);
    }

    void OnPeerStateChanged(string json)
    {
        Debug.Log($"[MPC] Peer state: {json}");
        var data = JsonUtility.FromJson<PeerStateData>(json);
        OnPeerStateChangedEvent?.Invoke(data.peer, data.state);
    }

    void OnDataReceived(string jsonString)
    {
        OnDataReceivedEvent?.Invoke(jsonString);
    }

    [Serializable]
    private class PeerStateData
    {
        public string peer;
        public string state;
    }
}
