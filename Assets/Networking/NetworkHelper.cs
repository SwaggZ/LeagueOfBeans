using UnityEngine;

/// <summary>
/// Wrapper for network-sensitive operations. Currently uses standard Unity APIs.
/// When Mirror is installed, this will be updated to use NetworkServer.Spawn / ClientRpc / etc.
/// 
/// MIRROR MIGRATION NOTES:
/// - SpawnProjectile() → will become [Command] + NetworkServer.Spawn()
/// - Despawn() → will become NetworkServer.Destroy()
/// - IsLocalPlayer() → will check NetworkIdentity.isLocalPlayer
/// - HasAuthority() → will check NetworkIdentity.hasAuthority
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// Spawns a projectile. In singleplayer, this just instantiates.
    /// In multiplayer (Mirror), this will be called from a Command and use NetworkServer.Spawn.
    /// </summary>
    public static GameObject SpawnProjectile(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[NetworkHelper] SpawnProjectile called with null prefab");
            return null;
        }
        
        GameObject go = Object.Instantiate(prefab, position, rotation);
        
        // MIRROR: Replace with NetworkServer.Spawn(go);
        // The caller should be a [Command] on the player's NetworkBehaviour
        
        return go;
    }
    
    /// <summary>
    /// Spawns a projectile at a transform's position/rotation.
    /// </summary>
    public static GameObject SpawnProjectile(GameObject prefab, Transform firePoint)
    {
        if (firePoint == null)
        {
            Debug.LogWarning("[NetworkHelper] SpawnProjectile called with null firePoint");
            return null;
        }
        return SpawnProjectile(prefab, firePoint.position, firePoint.rotation);
    }
    
    /// <summary>
    /// Despawns/destroys a networked object. In singleplayer, this just destroys.
    /// In multiplayer (Mirror), this will use NetworkServer.Destroy.
    /// </summary>
    public static void Despawn(GameObject go)
    {
        if (go == null) return;
        
        // MIRROR: Replace with NetworkServer.Destroy(go);
        // Only server should call this on networked objects
        
        Object.Destroy(go);
    }
    
    /// <summary>
    /// Despawns after a delay.
    /// </summary>
    public static void Despawn(GameObject go, float delay)
    {
        if (go == null) return;
        
        // MIRROR: For delayed network destroy, you may need a coroutine on server
        
        Object.Destroy(go, delay);
    }
    
    /// <summary>
    /// Checks if this is the local player. Without Mirror, always returns true if tagged "Player".
    /// With Mirror, will check NetworkIdentity.isLocalPlayer.
    /// </summary>
    public static bool IsLocalPlayer(GameObject go)
    {
        if (go == null) return false;
        
        // MIRROR: Replace with:
        // var netId = go.GetComponent<NetworkIdentity>();
        // return netId != null && netId.isLocalPlayer;
        
        return go.CompareTag("Player");
    }
    
    /// <summary>
    /// Checks if this object has authority (can be controlled). 
    /// Without Mirror, always true for player objects.
    /// With Mirror, checks NetworkIdentity.hasAuthority.
    /// </summary>
    public static bool HasAuthority(GameObject go)
    {
        if (go == null) return false;
        
        // MIRROR: Replace with:
        // var netId = go.GetComponent<NetworkIdentity>();
        // return netId != null && netId.hasAuthority;
        
        return go.CompareTag("Player");
    }
    
    /// <summary>
    /// Returns true if we're the server (or host). Without Mirror, always true.
    /// </summary>
    public static bool IsServer
    {
        get
        {
            // MIRROR: Replace with NetworkServer.active
            return true;
        }
    }
    
    /// <summary>
    /// Returns true if we're a client. Without Mirror, always true.
    /// </summary>
    public static bool IsClient
    {
        get
        {
            // MIRROR: Replace with NetworkClient.active
            return true;
        }
    }
}
