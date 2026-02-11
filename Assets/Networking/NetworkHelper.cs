using UnityEngine;

/// <summary>
/// Wrapper for network-sensitive operations. Currently uses standard Unity APIs.
/// When FishNet is installed, this will be updated to use ServerManager.Spawn / ObserversRpc / etc.
/// 
/// FISHNET MIGRATION NOTES:
/// - SpawnProjectile() → will become ServerRpc + ServerManager.Spawn()
/// - Despawn() → will become ServerManager.Despawn()
/// - IsLocalPlayer() → will check NetworkObject.IsOwner or IsController
/// - HasAuthority() → will check NetworkObject.IsOwner
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// Spawns a projectile. In singleplayer, this just instantiates.
    /// In multiplayer (FishNet), this will be called from a ServerRpc and use ServerManager.Spawn.
    /// </summary>
    public static GameObject SpawnProjectile(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[NetworkHelper] SpawnProjectile called with null prefab");
            return null;
        }
        
        GameObject go = Object.Instantiate(prefab, position, rotation);
        
        // FISHNET: Replace with ServerManager.Spawn(go);
        // The caller should be a [ServerRpc] on the player's NetworkBehaviour
        
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
    /// In multiplayer (FishNet), this will use ServerManager.Despawn.
    /// </summary>
    public static void Despawn(GameObject go)
    {
        if (go == null) return;
        
        // FISHNET: Replace with ServerManager.Despawn(go);
        // Only server should call this on networked objects
        
        Object.Destroy(go);
    }
    
    /// <summary>
    /// Despawns after a delay.
    /// </summary>
    public static void Despawn(GameObject go, float delay)
    {
        if (go == null) return;
        
        // FISHNET: For delayed network despawn, you may need a coroutine on server
        
        Object.Destroy(go, delay);
    }
    
    /// <summary>
    /// Checks if this is the local player. Without FishNet, always returns true if tagged "Player".
    /// With FishNet, will check NetworkObject.IsOwner or IsController.
    /// </summary>
    public static bool IsLocalPlayer(GameObject go)
    {
        if (go == null) return false;
        
        // FISHNET: Replace with:
        // var netObj = go.GetComponent<NetworkObject>();
        // return netObj != null && (netObj.IsOwner || netObj.IsController);
        
        return go.CompareTag("Player");
    }
    
    /// <summary>
    /// Checks if this object has authority (can be controlled). 
    /// Without FishNet, always true for player objects.
    /// With FishNet, checks NetworkObject.IsOwner.
    /// </summary>
    public static bool HasAuthority(GameObject go)
    {
        if (go == null) return false;
        
        // FISHNET: Replace with:
        // var netObj = go.GetComponent<NetworkObject>();
        // return netObj != null && netObj.IsOwner;
        
        return go.CompareTag("Player");
    }
    
    /// <summary>
    /// Returns true if we're the server (or host). Without FishNet, always true.
    /// </summary>
    public static bool IsServer
    {
        get
        {
            // FISHNET: Replace with InstanceFinder.IsServer
            return true;
        }
    }
    
    /// <summary>
    /// Returns true if we're a client. Without FishNet, always true.
    /// </summary>
    public static bool IsClient
    {
        get
        {
            // FISHNET: Replace with InstanceFinder.IsClient
            return true;
        }
    }
}
