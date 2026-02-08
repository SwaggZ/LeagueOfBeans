using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeathWall : MonoBehaviour
{
    [Tooltip("Damage dealt instantly on touch (use a very large value for instant kill).")]
    public float damageAmount = 1_000_000f;

    [Tooltip("If true, only affects objects tagged 'Player'. When false, hits any HealthSystem.")]
    public bool onlyAffectPlayer = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnValidate()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (onlyAffectPlayer && !other.CompareTag("Player")) return;
        var hs = other.GetComponentInParent<HealthSystem>();
        if (hs == null && other.attachedRigidbody != null)
            hs = other.attachedRigidbody.GetComponent<HealthSystem>();
        if (hs != null)
        {
            hs.TakeDamage(damageAmount);
        }
    }
}
