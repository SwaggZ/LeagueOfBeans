using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Broadcasts LAN server info over UDP for a simple server list.
/// </summary>
[DisallowMultipleComponent]
public class LanServerBroadcaster : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private SessionContext sessionContext;
    [SerializeField] private int broadcastPort = 47777;
    [SerializeField] private float intervalSeconds = 1.5f;

    private UdpClient _udp;
    private float _nextSend;

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>(true);
        if (sessionContext == null) sessionContext = FindObjectOfType<SessionContext>(true);
    }

    private void OnEnable()
    {
        if (_udp == null)
        {
            _udp = new UdpClient();
            _udp.EnableBroadcast = true;
        }
    }

    private void OnDisable()
    {
        if (_udp != null)
        {
            _udp.Close();
            _udp = null;
        }
    }

    private void Update()
    {
        if (networkManager == null || !networkManager.IsServerStarted) return;
        if (Time.unscaledTime < _nextSend) return;
        _nextSend = Time.unscaledTime + intervalSeconds;

        string payload = BuildPayload();
        byte[] data = Encoding.UTF8.GetBytes(payload);
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
        try
        {
            _udp?.Send(data, data.Length, endpoint);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LanServerBroadcaster] Send failed: {ex.Message}");
        }
    }

    private string BuildPayload()
    {
        string name = sessionContext != null ? sessionContext.serverName : "Lobby";
        bool hasPassword = sessionContext != null && sessionContext.requirePassword;
        ushort port = sessionContext != null ? sessionContext.serverPort : (ushort)7770;
        int playerCount = networkManager != null ? networkManager.ServerManager.Clients.Count : 0;
        int maxPlayers = 0;
        return $"LOB|{Escape(name)}|{port}|{(hasPassword ? 1 : 0)}|{playerCount}|{maxPlayers}";
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("|", "/");
    }
}
