using UnityEngine;

public class TrapBehavior : MonoBehaviour
{
    public float AutoTime = 30f; // Time before the trap self-destructs
    private float damage = 50f; // Damage dealt by the trap
    public float detectionRadius = 1.5f; // Radius to detect enemies
    public float stunDuration = 2f; // Duration of the stun effect

    void Start()
    {
        // Schedule destruction if not triggered
        Invoke("DestroySelf", AutoTime);
    }

    public void SetDamage(float damageValue)
    {
        damage = damageValue;
    }

    void FixedUpdate()
    {
        CheckForEnemies();
    }

    void CheckForEnemies()
    {
        // Use OverlapSphere to detect enemies in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (Collider collider in colliders)
        {
            // Skip allies
            if (collider.CompareTag("Player") || collider.CompareTag("Ally")) continue;

            // Check if the detected object has the "Enemy" tag
            if (collider.CompareTag("Enemy"))
            {
                GameObject enemyObj = ModifierUtils.ResolveTarget(collider);

                // Apply damage if the object has a HealthSystem
                HealthSystem health = enemyObj.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    Debug.Log($"{enemyObj.name} took {damage} damage from the trap!");
                }

                // Apply stun if the object has a CharacterControl or DummyController component
                DummyController dummyController = enemyObj.GetComponent<DummyController>();
                if (dummyController != null)
                {
                    dummyController.Stun(stunDuration);
                    Debug.Log($"{enemyObj.name} is stunned for {stunDuration} seconds!");
                }
                else
                {
                    CharacterControl enemyController = enemyObj.GetComponent<CharacterControl>();
                    if (enemyController != null)
                    {
                        enemyController.Stun(stunDuration);
                        Debug.Log($"{enemyObj.name} is stunned for {stunDuration} seconds!");
                    }
                }

                // Destroy the trap after applying effects
                Destroy(gameObject);
                break; // Exit loop after triggering the trap
            }
        }
    }

    void DestroySelf()
    {
        Debug.Log($"Trap {gameObject.name} destroyed after {AutoTime} seconds.");
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Draw the detection radius in the scene view for debugging
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
