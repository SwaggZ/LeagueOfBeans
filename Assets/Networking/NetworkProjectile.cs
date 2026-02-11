using UnityEngine;

/// <summary>
/// Base class for projectiles that will eventually need networking.
/// Inherit from this instead of MonoBehaviour for projectiles.
/// 
/// Currently extends MonoBehaviour. When FishNet is installed:
/// - Change to extend NetworkBehaviour
/// - Add SyncVar attributes to synced fields
/// - Movement updates will be handled by NetworkTransform or custom sync
/// 
/// FISHNET MIGRATION CHECKLIST:
/// [ ] Change base class to NetworkBehaviour
/// [ ] Add NetworkObject component requirement
/// [ ] Add NetworkTransform component for automatic position sync
/// [ ] Mark damage, speed, etc as SyncVars if needed server-authoritative values
/// [ ] Move collision/damage logic to server-only (if (IsServer))
/// [ ] Add ObserversRpc for visual effects that all clients should see
/// </summary>
public class NetworkProjectile : MonoBehaviour
{
    [Header("Network Settings")]
    [Tooltip("Owner connection ID. Will be used for authority in multiplayer.")]
    public int ownerConnectionId = -1;
    
    [Tooltip("Damage dealt by this projectile. Will be a SyncVar in FishNet.")]
    public float damage = 10f;
    
    [Tooltip("Movement speed. Will be a SyncVar in FishNet.")]
    public float speed = 20f;
    
    // FISHNET: These will become SyncVars
    // [SyncVar] public float damage;
    // [SyncVar] public float speed;

    private NetworkTransformProxy _netTransform;
    private DamageService _damageService;

    protected virtual void Awake()
    {
        _netTransform = GetComponent<NetworkTransformProxy>();
        _damageService = FindObjectOfType<DamageService>(true);
    }
    
    /// <summary>
    /// Override this to initialize projectile after spawn.
    /// Called by the spawner after setting initial values.
    /// FISHNET: This pattern remains the same, but may need ObserversRpc for client init
    /// </summary>
    public virtual void Initialize(GameObject owner, float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        
        if (owner != null)
        {
            // In singleplayer, owner is always the player
            // FISHNET: Get owner's NetworkObject.OwnerId
            ownerConnectionId = 0;
        }
    }
    
    /// <summary>
    /// Override this to handle projectile movement.
    /// FISHNET: Movement can be handled by NetworkTransform, or manual sync for prediction
    /// </summary>
    protected virtual void UpdateMovement()
    {
        // Override in derived class
    }
    
    /// <summary>
    /// Call this to safely destroy the projectile.
    /// Uses NetworkHelper for network-safe destruction.
    /// </summary>
    protected virtual void DestroySelf()
    {
        NetworkHelper.Despawn(gameObject);
    }
    
    /// <summary>
    /// Call this to safely destroy after a delay.
    /// </summary>
    protected virtual void DestroySelf(float delay)
    {
        NetworkHelper.Despawn(gameObject, delay);
    }
    
    /// <summary>
    /// Apply damage to a target. Override for custom damage logic.
    /// FISHNET: This should only run on server (if (IsServer))
    /// </summary>
    protected virtual void ApplyDamage(GameObject target, float amount)
    {
        // FISHNET: Wrap in if (IsServer)
        // {
        //     var health = target.GetComponent<Health>();
        //     health?.TakeDamage(amount);
        // }
        
        if (_damageService != null)
        {
            _damageService.DealDamage(target, amount, gameObject);
            return;
        }

        // Fallback to direct HealthSystem for singleplayer
        var hp = target.GetComponent<HealthSystem>();
        if (hp != null) hp.TakeDamage(amount);
    }

    protected bool CanSimulateLocally()
    {
        return _netTransform == null || _netTransform.CanSimulate;
    }

    protected void ApplyPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        if (_netTransform != null)
        {
            _netTransform.ApplyPositionAndRotation(position, rotation);
        }
        else
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }
    
    /// <summary>
    /// Checks if the target should be damaged (not ally/player).
    /// </summary>
    protected virtual bool ShouldDamage(Collider col)
    {
        if (col == null) return false;
        
        // Skip allies and player
        if (col.CompareTag("Player") || col.CompareTag("Ally"))
            return false;
        
        // Check parent tags too
        Transform root = col.transform.root;
        if (root != null && (root.CompareTag("Player") || root.CompareTag("Ally")))
            return false;
        
        // Damage enemies
        return col.CompareTag("Enemy");
    }
    
    /// <summary>
    /// Checks if target is an ally (player or ally-tagged).
    /// </summary>
    protected virtual bool IsAlly(Collider col)
    {
        if (col == null) return false;
        
        if (col.CompareTag("Player") || col.CompareTag("Ally"))
            return true;
        
        Transform root = col.transform.root;
        return root != null && (root.CompareTag("Player") || root.CompareTag("Ally"));
    }
    
    /// <summary>
    /// Checks if target is an enemy.
    /// </summary>
    protected virtual bool IsEnemy(Collider col)
    {
        if (col == null) return false;
        return col.CompareTag("Enemy");
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Editor helper to show network status.
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        // Draw direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
#endif
}
