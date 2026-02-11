using UnityEngine;

/// <summary>
/// Non-static wrapper for damage calls to ease migration to networking.
/// </summary>
public class DamageService : MonoBehaviour
{
    public bool DealDamage(GameObject target, float amount, GameObject source = null)
    {
        return DamageDealer.DealDamage(target, amount, source);
    }

    public void DealDamage(HealthSystem target, float amount, GameObject source = null)
    {
        DamageDealer.DealDamage(target, amount, source);
    }

    public int DealAoeDamage(Vector3 center, float radius, float damage, LayerMask enemyMask, GameObject source = null)
    {
        return DamageDealer.DealAoeDamage(center, radius, damage, enemyMask, source);
    }

    public bool ValidateDamage(GameObject source, GameObject target, float amount)
    {
        return DamageDealer.ValidateDamage(source, target, amount);
    }
}
