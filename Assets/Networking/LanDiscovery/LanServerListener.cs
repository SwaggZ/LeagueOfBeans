using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// Listens for LAN server broadcasts.
/// </summary>
[DisallowMultipleComponent]
public class LanServerListener : MonoBehaviour
{
    public event Action<LanServerInfo> OnServerUpdated;

    [SerializeField] private int listenPort = 47777;
    [SerializeField] private float staleSeconds = 4f;

    private UdpClient _udp;
    private IPEndPoint _endpoint;
    private readonly ConcurrentQueue<LanServerInfo> _pending = new ConcurrentQueue<LanServerInfo>();
    private readonly Dictionary<string, LanServerInfo> _servers = new Dictionary<string, LanServerInfo>();

    private void OnEnable()
    {
        if (_udp != null) return;
        try
        {
            _endpoint = new IPEndPoint(IPAddress.Any, listenPort);
            _udp = new UdpClient(listenPort);
            _udp.BeginReceive(OnReceive, null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LanServerListener] Failed to bind UDP {listenPort}: {ex.Message}");
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
        while (_pending.TryDequeue(out var info))
        {
            string key = info.address + ":" + info.port;
            _servers[key] = info;
            OnServerUpdated?.Invoke(info);
        }

        CleanupStale();
    }

    public IReadOnlyCollection<LanServerInfo> GetServers()
    {
        return _servers.Values;
    }

    private void CleanupStale()
    {
        if (_servers.Count == 0) return;
        DateTime now = DateTime.UtcNow;
        List<string> remove = null;
        foreach (var kvp in _servers)
        {
            if ((now - kvp.Value.lastSeenUtc).TotalSeconds > staleSeconds)
            {
                if (remove == null) remove = new List<string>();
                remove.Add(kvp.Key);
            }
        }
        if (remove == null) return;
        foreach (var key in remove)
            _servers.Remove(key);
    }

    private void OnReceive(IAsyncResult ar)
    {
        try
        {
            if (_udp == null) return;
            byte[] data = _udp.EndReceive(ar, ref _endpoint);
            string msg = Encoding.UTF8.GetString(data);
            LanServerInfo info = Parse(msg, _endpoint.Address.ToString());
            if (info != null)
            {
                info.lastSeenUtc = DateTime.UtcNow;
                _pending.Enqueue(info);
            }
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LanServerListener] Receive failed: {ex.Message}");
        }
        finally
        {
            try { _udp?.BeginReceive(OnReceive, null); } catch { }
        }
    }

    private LanServerInfo Parse(string msg, string address)
    {
        if (string.IsNullOrEmpty(msg)) return null;
        string[] parts = msg.Split('|');
        if (parts.Length < 6) return null;
        if (parts[0] != "LOB") return null;

        string name = parts[1];
        if (!ushort.TryParse(parts[2], out var port)) return null;
        bool hasPassword = parts[3] == "1";
        int.TryParse(parts[4], out var playerCount);
        int.TryParse(parts[5], out var maxPlayers);

        return new LanServerInfo
        {
            name = name,
            address = address,
            port = port,
            hasPassword = hasPassword,
            playerCount = playerCount,
            maxPlayers = maxPlayers,
            pingMs = -1
        };
    }
}
