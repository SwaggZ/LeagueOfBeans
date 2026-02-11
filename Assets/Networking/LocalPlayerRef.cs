using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized reference holder for player objects. Avoids FindGameObjectWithTag calls.
/// 
/// USAGE:
/// - Call LocalPlayerRef.Register(gameObject) when player spawns
/// - Call LocalPlayerRef.Unregister(gameObject) when player despawns
/// - Use LocalPlayerRef.LocalPlayer to get the local player
/// - Use LocalPlayerRef.GetAllPlayers() for all players (multiplayer ready)
/// 
/// FISHNET MIGRATION NOTES:
/// - LocalPlayer will be set only for the local client's player (IsOwner)
/// - AllPlayers will contain all player GameObjects across the network
/// - Register should be called in OnStartClient / OnStartServer as appropriate
/// </summary>
public static class LocalPlayerRef
{
    private static GameObject _localPlayer;
    private static readonly List<GameObject> _allPlayers = new List<GameObject>();
    
    /// <summary>
    /// The local player's GameObject. Null if not spawned yet.
    /// With FishNet, this is the player with IsOwner = true.
    /// </summary>
    public static GameObject LocalPlayer => _localPlayer;
    
    /// <summary>
    /// The local player's Transform. Null if not spawned.
    /// </summary>
    public static Transform LocalTransform => _localPlayer != null ? _localPlayer.transform : null;
    
    /// <summary>
    /// Returns true if the local player exists.
    /// </summary>
    public static bool HasLocalPlayer => _localPlayer != null;
    
    /// <summary>
    /// Registers a player. Call this when a player GameObject spawns.
    /// For local player, set isLocal = true.
    /// 
    /// FISHNET: Call in OnStartClient for all, and OnStartServer/OnStartClient for owner as needed
    /// </summary>
    public static void Register(GameObject player, bool isLocal = true)
    {
        if (player == null) return;
        
        if (isLocal)
        {
            _localPlayer = player;
        }
        
        if (!_allPlayers.Contains(player))
        {
            _allPlayers.Add(player);
        }
        
        Debug.Log($"[LocalPlayerRef] Registered player: {player.name} (local={isLocal})");
    }
    
    /// <summary>
    /// Unregisters a player. Call when player despawns.
    /// </summary>
    public static void Unregister(GameObject player)
    {
        if (player == null) return;
        
        if (_localPlayer == player)
        {
            _localPlayer = null;
        }
        
        _allPlayers.Remove(player);
        Debug.Log($"[LocalPlayerRef] Unregistered player: {player?.name}");
    }
    
    /// <summary>
    /// Gets all registered players. Useful for multiplayer targeting.
    /// </summary>
    public static IReadOnlyList<GameObject> GetAllPlayers()
    {
        // Clean up any null references
        _allPlayers.RemoveAll(p => p == null);
        return _allPlayers;
    }
    
    /// <summary>
    /// Finds the closest player to a position. Returns null if no players registered.
    /// </summary>
    public static GameObject GetClosestPlayer(Vector3 position)
    {
        _allPlayers.RemoveAll(p => p == null);
        
        GameObject closest = null;
        float closestDist = float.MaxValue;
        
        foreach (var player in _allPlayers)
        {
            float dist = Vector3.Distance(position, player.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = player;
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// Gets all allies (players on same team). Without Mirror/teams, returns all players.
    /// </summary>
    public static List<GameObject> GetAllies()
    {
        _allPlayers.RemoveAll(p => p == null);
        
        // FISHNET: Filter by team/ConnectionId for teams
        return new List<GameObject>(_allPlayers);
    }
    
    /// <summary>
    /// Clears all references. Call on scene unload or disconnect.
    /// </summary>
    public static void Clear()
    {
        _localPlayer = null;
        _allPlayers.Clear();
        Debug.Log("[LocalPlayerRef] Cleared all player references");
    }
    
    /// <summary>
    /// Fallback that uses FindGameObjectWithTag if no player is registered.
    /// This ensures backwards compatibility while we migrate.
    /// </summary>
    public static GameObject GetLocalPlayerWithFallback()
    {
        if (_localPlayer != null) return _localPlayer;

        // Prefer PlayerRegistration if present on the player prefab
        var reg = Object.FindObjectOfType<PlayerRegistration>(true);
        if (reg != null)
        {
            Register(reg.gameObject, reg.isLocalPlayer);
            return reg.gameObject;
        }

        // Last-resort fallback: try CharacterControl root
        var cc = Object.FindObjectOfType<CharacterControl>(true);
        if (cc != null)
        {
            Register(cc.gameObject, true);
            return cc.gameObject;
        }

        return null;
    }
}
