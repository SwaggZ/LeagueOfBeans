using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// NetworkBehaviour bridge that forwards client requests to NetworkGameSession on the server.
/// This is required because NetworkGameSession is a regular MonoBehaviour (attached to NetworkManager)
/// and cannot use FishNet RPCs directly.
/// 
/// IMPORTANT: This GameObject must have a NetworkObject component and be spawned as a network object.
/// It should be created in the Menu scene and persist via DontDestroyOnLoad.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkSessionBridge : NetworkBehaviour
{
    // Event fired when bridge is spawned and ready on a client
    public static event System.Action<NetworkSessionBridge> OnBridgeReady;
    
    private NetworkGameSession _session;
    
    private void Awake()
    {
        // Find NetworkGameSession (should be on NetworkManager or as sibling)
        _session = FindObjectOfType<NetworkGameSession>();
        if (_session == null)
        {
            Debug.LogError("[NetworkSessionBridge] Could not find NetworkGameSession! Make sure it exists in the scene.");
        }
        else
        {
            Debug.Log("[NetworkSessionBridge] Found NetworkGameSession, bridge ready.");
        }
    }
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Debug.Log($"[NetworkSessionBridge] Network started. IsServer={base.IsServerStarted}, IsClient={base.IsClientStarted}");
        
        // Verify NetworkObject is properly configured
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            Debug.Log($"[NetworkSessionBridge] NetworkObject ID: {netObj.ObjectId}, IsSpawned: {netObj.IsSpawned}");
        }
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[NetworkSessionBridge] OnStartClient called - bridge is now ready for client!");
        
        // Notify any listeners that the bridge is ready
        OnBridgeReady?.Invoke(this);
    }
    
    /// <summary>
    /// Client calls this to submit their character selection to the server.
    /// </summary>
    public void ClientSubmitSelection(string characterId)
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsSpawned)
        {
            Debug.LogError("[NetworkSessionBridge] ClientSubmitSelection: NetworkObject not spawned! Cannot send RPC.");
            return;
        }
        
        if (!base.IsClientStarted)
        {
            Debug.LogWarning("[NetworkSessionBridge] ClientSubmitSelection: Client not started");
            return;
        }
        
        Debug.Log($"[NetworkSessionBridge] Client sending selection: {characterId}");
        SubmitSelectionServerRpc(characterId);
    }
    
    /// <summary>
    /// Client calls this to set their ready state.
    /// </summary>
    public void ClientSetReady(bool isReady)
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsSpawned)
        {
            Debug.LogError("[NetworkSessionBridge] ClientSetReady: NetworkObject not spawned! Cannot send RPC.");
            return;
        }
        
        if (!base.IsClientStarted)
        {
            Debug.LogWarning("[NetworkSessionBridge] ClientSetReady: Client not started");
            return;
        }
        
        Debug.Log($"[NetworkSessionBridge] Client sending ready state: {isReady}");
        SetReadyServerRpc(isReady);
    }
    
    /// <summary>
    /// Client calls this to request respawn after death.
    /// </summary>
    public void ClientRequestRespawn()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsSpawned)
        {
            Debug.LogError("[NetworkSessionBridge] ClientRequestRespawn: NetworkObject not spawned! Cannot send RPC.");
            return;
        }
        
        if (!base.IsClientStarted)
        {
            Debug.LogWarning("[NetworkSessionBridge] ClientRequestRespawn: Client not started");
            return;
        }
        
        Debug.Log("[NetworkSessionBridge] Client requesting respawn");
        RequestRespawnServerRpc();
    }
    
    /// <summary>
    /// Server RPC: Receives character selection from client and forwards to NetworkGameSession.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SubmitSelectionServerRpc(string characterId, NetworkConnection conn = null)
    {
        if (_session == null)
        {
            Debug.LogError("[NetworkSessionBridge] SubmitSelectionServerRpc: NetworkGameSession is null!");
            return;
        }
        
        if (conn == null)
        {
            Debug.LogWarning("[NetworkSessionBridge] SubmitSelectionServerRpc: Connection is null!");
            return;
        }
        
        Debug.Log($"[NetworkSessionBridge] Server received selection from client {conn.ClientId}: {characterId}");
        _session.SubmitSelectionServerRpc(characterId, conn);
    }
    
    /// <summary>
    /// Server RPC: Receives ready state from client and forwards to NetworkGameSession.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(bool isReady, NetworkConnection conn = null)
    {
        if (_session == null)
        {
            Debug.LogError("[NetworkSessionBridge] SetReadyServerRpc: NetworkGameSession is null!");
            return;
        }
        
        if (conn == null)
        {
            Debug.LogWarning("[NetworkSessionBridge] SetReadyServerRpc: Connection is null!");
            return;
        }
        
        Debug.Log($"[NetworkSessionBridge] Server received ready state from client {conn.ClientId}: {isReady}");
        _session.SetReadyServerRpc(isReady, conn);
    }
    
    /// <summary>
    /// Server RPC: Receives respawn request from client and forwards to NetworkGameSession.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestRespawnServerRpc(NetworkConnection conn = null)
    {
        if (_session == null)
        {
            Debug.LogError("[NetworkSessionBridge] RequestRespawnServerRpc: NetworkGameSession is null!");
            return;
        }
        
        if (conn == null)
        {
            Debug.LogWarning("[NetworkSessionBridge] RequestRespawnServerRpc: Connection is null!");
            return;
        }
        
        Debug.Log($"[NetworkSessionBridge] Server received respawn request from client {conn.ClientId}");
        _session.RequestRespawnServerRpc(conn);
    }
}

