using UnityEngine;

/// <summary>
/// Centralized damage dealing helper. All damage should go through this.
/// 
/// FISHNET MIGRATION:
/// - DealDamage should only execute on server
/// - Use [ServerRpc] from player to request damage
/// - Server validates and applies damage, then syncs via SyncVar on target health
/// 
/// Pattern for FishNet:
/// 1. Client detects hit (collision)
/// 2. Client calls [ServerRpc] CmdRequestDamage(targetNetId, amount)
/// 3. Server validates (anti-cheat) and calls DamageDealer.DealDamage
/// 4. Target's health [SyncVar] updates, clients see new value
/// </summary>
public static class DamageDealer
{
    /// <summary>
    /// Deal damage to any valid target. Automatically finds the correct component.
    /// FISHNET: Wrap call site in if (IsServer) or call via ServerRpc
    /// </summary>
    public static bool DealDamage(GameObject target, float amount, GameObject source = null)
    {
        if (target == null) return false;
        
        // Skip if target is player/ally
        if (target.CompareTag("Player") || target.CompareTag("Ally"))
            return false;
        
        // Resolve actual target (might be a child collider)
        GameObject resolved = ModifierUtils.ResolveTarget(target.GetComponent<Collider>())?.gameObject ?? target;
        
        // Try HealthSystem (primary damage component)
        var hp = resolved.GetComponent<HealthSystem>();
        if (hp != null)
        {
            hp.TakeDamage(amount);
            LogDamage(source, resolved, amount, "HealthSystem");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Deal damage to a specific HealthSystem.
    /// FISHNET: Server-only
    /// </summary>
    public static void DealDamage(HealthSystem target, float amount, GameObject source = null)
    {
        if (target == null) return;
        target.TakeDamage(amount);
        LogDamage(source, target.gameObject, amount, "HealthSystem");
    }
    
    /// <summary>
    /// Deal AOE damage to all enemies in radius.
    /// FISHNET: Server-only
    /// </summary>
    public static int DealAoeDamage(Vector3 center, float radius, float damage, LayerMask enemyMask, GameObject source = null)
    {
        int hitCount = 0;
        Collider[] hits = Physics.OverlapSphere(center, radius, enemyMask, QueryTriggerInteraction.Collide);
        
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            
            // Skip allies
            if (hit.CompareTag("Player") || hit.CompareTag("Ally")) continue;
            
            Transform root = hit.transform.root;
            if (root != null && (root.CompareTag("Player") || root.CompareTag("Ally"))) continue;
            
            if (hit.CompareTag("Enemy"))
            {
                GameObject resolved = ModifierUtils.ResolveTarget(hit)?.gameObject ?? hit.gameObject;
                
                var hp = resolved.GetComponent<HealthSystem>();
                if (hp != null)
                {
                    hp.TakeDamage(damage);
                    hitCount++;
                    LogDamage(source, resolved, damage, "AOE");
                }
            }
        }
        
        return hitCount;
    }
    
    /// <summary>
    /// Validates if damage should be applied (anti-cheat checks for Mirror).
    /// FISHNET: Call this on server before applying damage
    /// </summary>
    public static bool ValidateDamage(GameObject source, GameObject target, float amount)
    {
        // Basic validation
        if (target == null) return false;
        if (amount <= 0) return false;
        
        // Don't damage allies
        if (target.CompareTag("Player") || target.CompareTag("Ally"))
            return false;
        
        // FISHNET: Add additional server-side validation:
        // - Check source is a valid player
        // - Check amount is within expected range for ability
        // - Check cooldowns haven't been bypassed
        // - Check distance isn't impossibly far
        
        return true;
    }
    
    private static void LogDamage(GameObject source, GameObject target, float amount, string type)
    {
        #if UNITY_EDITOR
        // Only log in editor for debugging, remove for release
        // Debug.Log($"[DamageDealer] {source?.name ?? "Unknown"} dealt {amount} damage to {target?.name} via {type}");
        #endif
    }
}
