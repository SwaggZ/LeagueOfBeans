using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Tracks character selections and spawns players in gameplay.
/// Regular MonoBehaviour (not NetworkBehaviour) to avoid adding NetworkObject to NetworkManager.
/// </summary>
public class NetworkGameSession : MonoBehaviour
{
    [System.Serializable]
    public class CharacterPrefabEntry
    {
        public string id;
        public GameObject prefab;
    }

    [Header("Prefabs")]
    public List<CharacterPrefabEntry> characterPrefabs = new List<CharacterPrefabEntry>();

    [Header("Spawn")]
    public string spawnPointKey = "PlayerSpawn";
    public float enemyScanRadius = 14f;
    public LayerMask enemyMask;
    public bool autoStartWhenAllSelected = true;
    public bool requireReadyToStart = false;

    [Header("Scene Names")]
    public string gameplaySceneName = "SampleScene";

    // We can't use SyncDictionary with regular MonoBehaviour, so we'll use regular Dictionary
    // and sync via events or direct method calls
    private Dictionary<int, string> selected = new Dictionary<int, string>();
    private Dictionary<int, bool> ready = new Dictionary<int, bool>();
    
    public Dictionary<int, string> Selected => selected;
    public Dictionary<int, bool> Ready => ready;

    private NetworkManager _cachedNetworkManager;
    
    // Store connection info for server-side checks
    private bool _isServer = false;
    private bool _isClient = false;
    
    // Spawn tracking to prevent duplicates
    private HashSet<int> _spawnedClients = new HashSet<int>();
    private bool _gameplayStarted = false;
    
    /// <summary>
    /// Allow a client to respawn after death. Called from Character Selection.
    /// </summary>
    public void RequestRespawn(int clientId)
    {
        if (!_isServer)
        {
            Debug.LogWarning("[NetworkGameSession] RequestRespawn called on non-server.");
            return;
        }
        
        if (_cachedNetworkManager == null)
        {
            Debug.LogError("[NetworkGameSession] RequestRespawn: NetworkManager is null!");
            return;
        }
        
        // Remove from spawned tracking to allow respawn
        if (_spawnedClients.Contains(clientId))
        {
            _spawnedClients.Remove(clientId);
            Debug.Log($"[NetworkGameSession] Client {clientId} cleared for respawn.");
        }
        
        // Get connection and spawn the player again
        if (_cachedNetworkManager.ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn))
        {
            if (selected.ContainsKey(clientId))
            {
                SpawnPlayerForConnection(conn, selected[clientId]);
            }
            else
            {
                Debug.LogWarning($"[NetworkGameSession] Cannot respawn client {clientId} - no character selection found.");
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkGameSession] Cannot respawn client {clientId} - connection not found.");
        }
    }

    private void OnEnable()
    {
        // Aggressively find and cache NetworkManager
        if (_cachedNetworkManager == null)
        {
            _cachedNetworkManager = GetComponentInParent<NetworkManager>();
            if (_cachedNetworkManager == null)
                _cachedNetworkManager = FindObjectOfType<NetworkManager>();
            
            if (_cachedNetworkManager != null)
            {
                Debug.Log($"[NetworkGameSession] Cached NetworkManager: {_cachedNetworkManager.gameObject.name}");
                
                // Register for server/client state changes
                _cachedNetworkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
                _cachedNetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
                
                _isServer = _cachedNetworkManager.IsServerStarted;
                _isClient = _cachedNetworkManager.IsClientStarted;
            }
            else
                Debug.LogWarning("[NetworkGameSession] Could not find NetworkManager!");
        }
    }

    private void OnDisable()
    {
        if (_cachedNetworkManager != null)
        {
            _cachedNetworkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            _cachedNetworkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    private void OnServerConnectionState(FishNet.Transporting.ServerConnectionStateArgs args)
    {
        _isServer = (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Started);
        if (_isServer)
            Debug.Log("[NetworkGameSession] Server started");
    }

    private void OnClientConnectionState(FishNet.Transporting.ClientConnectionStateArgs args)
    {
        _isClient = (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Started);
        if (_isClient)
            Debug.Log("[NetworkGameSession] Client started");
    }

    public void SubmitSelectionServerRpc(string characterId, NetworkConnection conn = null)
    {
        // Server method - processes character selection
        if (!_isServer)
        {
            Debug.LogWarning("[NetworkGameSession] SubmitSelectionServerRpc called on non-server!");
            return;
        }
        
        if (conn == null)
        {
            Debug.LogWarning("[NetworkGameSession] SubmitSelectionServerRpc called with null connection");
            return;
        }

        Debug.Log($"[NetworkGameSession] SubmitSelectionServerRpc: client {conn.ClientId} selected '{characterId}'");
        selected[conn.ClientId] = characterId ?? string.Empty;
        ready[conn.ClientId] = false;

        bool allSelected = AllPlayersSelected();
        bool allReady = AllPlayersReady();
        Debug.Log($"[NetworkGameSession] State after selection: AllSelected={allSelected}, AllReady={allReady}, autoStart={autoStartWhenAllSelected}, requireReady={requireReadyToStart}, gameplayStarted={_gameplayStarted}");

        if (autoStartWhenAllSelected && allSelected && (!requireReadyToStart || allReady) && !_gameplayStarted)
        {
            Debug.Log("[NetworkGameSession] Conditions met, starting gameplay!");
            StartGameplay();
        }
    }

    public void SetReadyServerRpc(bool isReady, NetworkConnection conn = null)
    {
        // Server method - processes ready state
        if (!_isServer)
        {
            Debug.LogWarning("[NetworkGameSession] SetReadyServerRpc called on non-server!");
            return;
        }
        
        if (conn == null)
        {
            Debug.LogWarning("[NetworkGameSession] SetReadyServerRpc called with null connection");
            return;
        }

        Debug.Log($"[NetworkGameSession] SetReadyServerRpc: client {conn.ClientId} ready={isReady}");
        ready[conn.ClientId] = isReady;

        bool allSelected = AllPlayersSelected();
        bool allReady = AllPlayersReady();
        Debug.Log($"[NetworkGameSession] State: AllSelected={allSelected}, AllReady={allReady}, autoStart={autoStartWhenAllSelected}, requireReady={requireReadyToStart}, gameplayStarted={_gameplayStarted}");

        if (autoStartWhenAllSelected && allSelected && (!requireReadyToStart || allReady) && !_gameplayStarted)
        {
            Debug.Log("[NetworkGameSession] Conditions met, starting gameplay!");
            StartGameplay();
        }
    }

    public void RequestStartServerRpc(NetworkConnection conn = null)
    {
        // Server method - manual start request
        if (!_isServer) return;
        StartGameplay();
    }
    
    /// <summary>
    /// ServerRpc for respawning a player after death.
    /// </summary>
    public void RequestRespawnServerRpc(NetworkConnection conn = null)
    {
        if (!_isServer)
        {
            Debug.LogWarning("[NetworkGameSession] RequestRespawnServerRpc called on non-server!");
            return;
        }
        
        if (conn == null)
        {
            Debug.LogWarning("[NetworkGameSession] RequestRespawnServerRpc called with null connection");
            return;
        }
        
        Debug.Log($"[NetworkGameSession] RequestRespawnServerRpc: Respawning client {conn.ClientId}");
        RequestRespawn(conn.ClientId);
    }

    public void ClientSubmitSelection(string characterId)
    {
        if (_cachedNetworkManager == null)
            _cachedNetworkManager = GetComponentInParent<NetworkManager>() ?? FindObjectOfType<NetworkManager>();
            
        if (_cachedNetworkManager == null)
        {
            Debug.LogError("[NetworkGameSession] ClientSubmitSelection: Could not find NetworkManager!");
            return;
        }
        
        if (!_cachedNetworkManager.IsClientStarted) 
        {
            Debug.LogWarning("[NetworkGameSession] ClientSubmitSelection: Client not started");
            return;
        }
        
        // Get local client connection
        var conn = _cachedNetworkManager.ClientManager.Connection;
        if (conn == null)
        {
            Debug.LogError("[NetworkGameSession] ClientSubmitSelection: No client connection!");
            return;
        }
        
        Debug.Log($"[NetworkGameSession] ClientSubmitSelection calling RPC with ID: {characterId}, clientId: {conn.ClientId}");
        try
        {
            SubmitSelectionServerRpc(characterId, conn);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkGameSession] ClientSubmitSelection RPC failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void ClientRequestStart()
    {
        if (_cachedNetworkManager == null)
            _cachedNetworkManager = GetComponentInParent<NetworkManager>() ?? FindObjectOfType<NetworkManager>();
            
        if (_cachedNetworkManager == null)
        {
            Debug.LogError("[NetworkGameSession] ClientRequestStart: Could not find NetworkManager!");
            return;
        }
        
        if (!_cachedNetworkManager.IsClientStarted)
        {
            Debug.LogWarning("[NetworkGameSession] ClientRequestStart: Client not started");
            return;
        }
        
        RequestStartServerRpc();
    }

    public void ClientSetReady(bool isReady)
    {
        if (_cachedNetworkManager == null)
            _cachedNetworkManager = GetComponentInParent<NetworkManager>() ?? FindObjectOfType<NetworkManager>();
            
        if (_cachedNetworkManager == null)
        {
            Debug.LogError("[NetworkGameSession] ClientSetReady: Could not find NetworkManager!");
            return;
        }
        
        if (!_cachedNetworkManager.IsClientStarted)
        {
            Debug.LogWarning("[NetworkGameSession] ClientSetReady: Client not started");
            return;
        }
        
        var conn = _cachedNetworkManager.ClientManager.Connection;
        if (conn == null)
        {
            Debug.LogError("[NetworkGameSession] ClientSetReady: No client connection!");
            return;
        }
        
        Debug.Log($"[NetworkGameSession] ClientSetReady calling RPC with: {isReady}");
        try
        {
            SetReadyServerRpc(isReady, conn);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkGameSession] ClientSetReady RPC failed: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// Client calls this to request respawn after death.
    /// </summary>
    public void ClientRequestRespawn()
    {
        if (_cachedNetworkManager == null)
            _cachedNetworkManager = GetComponentInParent<NetworkManager>() ?? FindObjectOfType<NetworkManager>();
            
        if (_cachedNetworkManager == null)
        {
            Debug.LogError("[NetworkGameSession] ClientRequestRespawn: Could not find NetworkManager!");
            return;
        }
        
        if (!_cachedNetworkManager.IsClientStarted)
        {
            Debug.LogWarning("[NetworkGameSession] ClientRequestRespawn: Client not started");
            return;
        }
        
        var conn = _cachedNetworkManager.ClientManager.Connection;
        if (conn == null)
        {
            Debug.LogError("[NetworkGameSession] ClientRequestRespawn: No client connection!");
            return;
        }
        
        Debug.Log($"[NetworkGameSession] ClientRequestRespawn calling RPC for clientId: {conn.ClientId}");
        try
        {
            RequestRespawnServerRpc(conn);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkGameSession] ClientRequestRespawn RPC failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private bool AllPlayersSelected()
    {
        if (_cachedNetworkManager == null) return false;

        string clientStates = "";
        foreach (var kvp in _cachedNetworkManager.ServerManager.Clients)
        {
            int clientId = kvp.Value.ClientId;
            selected.TryGetValue(clientId, out var id);
            clientStates += $"[Client {clientId}: {(string.IsNullOrEmpty(id) ? "NOT SELECTED" : id)}] ";
            if (string.IsNullOrEmpty(id))
                return false;
        }
        Debug.Log($"[NetworkGameSession] AllPlayersSelected: {clientStates} → TRUE");
        return true;
    }

    private bool AllPlayersReady()
    {
        if (_cachedNetworkManager == null) return false;

        string clientStates = "";
        foreach (var kvp in _cachedNetworkManager.ServerManager.Clients)
        {
            int clientId = kvp.Value.ClientId;
            ready.TryGetValue(clientId, out var isReady);
            clientStates += $"[Client {clientId}: {(isReady ? "READY" : "NOT READY")}] ";
            if (!isReady)
                return false;
        }
        Debug.Log($"[NetworkGameSession] AllPlayersReady: {clientStates} → TRUE");
        return true;
    }

    private void StartGameplay()
    {
        // Prevent starting gameplay multiple times
        if (_gameplayStarted)
        {
            Debug.LogWarning("[NetworkGameSession] StartGameplay: Already started! Ignoring duplicate call.");
            return;
        }
        
        _gameplayStarted = true;
        
        if (_cachedNetworkManager == null)
        {
            Debug.LogError("[NetworkGameSession] StartGameplay: NetworkManager is null!");
            return;
        }

        if (!_cachedNetworkManager.IsServerStarted)
        {
            Debug.LogError("[NetworkGameSession] StartGameplay: Server not started!");
            return;
        }

        Debug.Log("[NetworkGameSession] StartGameplay: All conditions met, spawning players immediately...");
        // Players are already in SampleScene (used for both selection and gameplay)
        // Spawn them immediately without scene loading
        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        if (_cachedNetworkManager == null) 
        {
            Debug.LogError("[NetworkGameSession] SpawnAllPlayers: NetworkManager is null!");
            return;
        }

        Debug.Log($"[NetworkGameSession] SpawnAllPlayers called. Server: {_cachedNetworkManager.IsServerStarted}, Clients: {_cachedNetworkManager.ServerManager.Clients.Count}");
        Debug.Log($"[NetworkGameSession] Available prefabs in characterPrefabs: {characterPrefabs.Count}");
        foreach (var entry in characterPrefabs)
        {
            if (entry != null)
                Debug.Log($"  - ID: '{entry.id}' -> Prefab: {(entry.prefab != null ? entry.prefab.name : "NULL")}");
        }

        foreach (var kvp in _cachedNetworkManager.ServerManager.Clients)
        {
            NetworkConnection conn = kvp.Value;
            if (conn == null) continue;
            
            // Skip if already spawned
            if (_spawnedClients.Contains(conn.ClientId))
            {
                Debug.Log($"[NetworkGameSession] Client {conn.ClientId} already spawned, skipping.");
                continue;
            }

            string characterId = selected.TryGetValue(conn.ClientId, out var id) ? id : string.Empty;
            Debug.Log($"[NetworkGameSession] Processing client {conn.ClientId}, selected character ID: '{characterId}'");
            
            SpawnPlayerForConnection(conn, characterId);
        }
    }
    
    /// <summary>
    /// Spawns a character for a specific connection.
    /// </summary>
    private void SpawnPlayerForConnection(NetworkConnection conn, string characterId)
    {
        if (conn == null || _cachedNetworkManager == null) return;
        
        GameObject prefab = ResolvePrefab(characterId);
        if (prefab == null)
        {
            Debug.LogWarning($"[NetworkGameSession] No prefab found for character '{characterId}' (clientId: {conn.ClientId})");
            return;
        }

        // Verify prefab has NetworkObject
        var prefabNetObj = prefab.GetComponent<FishNet.Object.NetworkObject>();
        if (prefabNetObj == null)
        {
            Debug.LogError($"[NetworkGameSession] Prefab '{prefab.name}' is missing NetworkObject component! Cannot spawn over network. Add NetworkObject component to the prefab.");
            return;
        }

        Transform spawnPoint = SelectSpawnPoint();
        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        Debug.Log($"[NetworkGameSession] Spawning {prefab.name} at {pos} for client {conn.ClientId}. Spawn point: {(spawnPoint != null ? spawnPoint.name : "NONE (using zero)")}");

        GameObject instance = Instantiate(prefab, pos, rot);
        
        // Verify instance has NetworkObject
        var instanceNetObj = instance.GetComponent<FishNet.Object.NetworkObject>();
        if (instanceNetObj == null)
        {
            Debug.LogError($"[NetworkGameSession] Instantiated object '{instance.name}' is missing NetworkObject component! Destroying...");
            Destroy(instance);
            return;
        }

        Debug.Log($"[NetworkGameSession] Calling ServerManager.Spawn for {instance.name} (ClientId: {conn.ClientId})");
        _cachedNetworkManager.ServerManager.Spawn(instance, conn);
        
        // Mark as spawned
        _spawnedClients.Add(conn.ClientId);
        Debug.Log($"[NetworkGameSession] Successfully spawned {instance.name} for client {conn.ClientId}");
    }

    private GameObject ResolvePrefab(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return characterPrefabs.Count > 0 ? characterPrefabs[0].prefab : null;

        foreach (var entry in characterPrefabs)
        {
            if (entry != null && entry.prefab != null && entry.id == characterId)
                return entry.prefab;
        }

        return characterPrefabs.Count > 0 ? characterPrefabs[0].prefab : null;
    }

    private Transform SelectSpawnPoint()
    {
        var spawnPoints = FindObjectsOfType<SpawnPointMarker>(true);
        Debug.Log($"[NetworkGameSession] Found {spawnPoints.Length} SpawnPointMarker(s)");

        List<Transform> candidates = new List<Transform>();
        foreach (var sp in spawnPoints)
        {
            if (sp == null) continue;
            if (string.IsNullOrEmpty(spawnPointKey) || sp.key == spawnPointKey || string.IsNullOrEmpty(sp.key))
                candidates.Add(sp.transform);
        }

        Debug.Log($"[NetworkGameSession] {candidates.Count} candidate spawn points after filtering (key='{spawnPointKey}')");

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[NetworkGameSession] No spawn points found!");
            return null;
        }

        Transform best = candidates[0];
        int bestCount = int.MaxValue;
        float bestDist = -1f;

        foreach (var point in candidates)
        {
            int count = CountEnemies(point.position, enemyScanRadius);
            float dist = DistanceToClosestEnemy(point.position, enemyScanRadius);
            Debug.Log($"[NetworkGameSession] Spawn '{point.name}': {count} enemies, {dist:F1}m to closest");

            if (count < bestCount || (count == bestCount && dist > bestDist))
            {
                bestCount = count;
                bestDist = dist;
                best = point;
            }
        }

        Debug.Log($"[NetworkGameSession] Selected spawn point: {best.name}");
        return best;
    }

    private int CountEnemies(Vector3 position, float radius)
    {
        Collider[] hits = enemyMask.value != 0
            ? Physics.OverlapSphere(position, radius, enemyMask)
            : Physics.OverlapSphere(position, radius);

        int count = 0;
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.CompareTag("Enemy") || hit.transform.root.CompareTag("Enemy"))
                count++;
        }
        return count;
    }

    private float DistanceToClosestEnemy(Vector3 position, float radius)
    {
        Collider[] hits = enemyMask.value != 0
            ? Physics.OverlapSphere(position, radius, enemyMask)
            : Physics.OverlapSphere(position, radius);

        float best = float.PositiveInfinity;
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!hit.CompareTag("Enemy") && !hit.transform.root.CompareTag("Enemy")) continue;

            float d = Vector3.Distance(position, hit.transform.position);
            if (d < best) best = d;
        }

        return float.IsInfinity(best) ? radius : best;
    }
}
