using System.Collections.Generic;
using UnityEngine;

public class qMovement : MonoBehaviour
{
    public float damagePerSecond = 10f; // Damage applied per second
    public float tickRate = 0.5f; // Frequency of damage ticks (seconds)
    public float detectionRadius = 5f; // Radius of the tornado's detection area
    public float detectionDistance = 10f; // Maximum distance of the raycast
    public LayerMask enemyLayer; // Layer mask for detecting enemies

    private float tickTimer;

    void Update()
    {
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickRate)
        {
            CastRayAndDamageEnemies();
            tickTimer = 0f;
        }
    }

    private void CastRayAndDamageEnemies()
    {
        // Perform a spherecast to detect enemies within the tornado's path
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position, // Start position
            detectionRadius,    // Sphere radius
            transform.forward,  // Direction
            detectionDistance,  // Distance
            enemyLayer          // Layer mask to filter enemies
        );

        foreach (RaycastHit hit in hits)
        {
            // Skip allies
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Ally")) continue;
            if (!hit.collider.CompareTag("Enemy")) continue;

            HealthSystem healthSystem = hit.collider.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                Debug.Log($"Applying {damagePerSecond * tickRate} damage to {hit.collider.gameObject.name}.");
                healthSystem.TakeDamage(damagePerSecond * tickRate);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the spherecast in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius); // Start sphere
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * detectionDistance); // Forward direction
        Gizmos.DrawWireSphere(transform.position + transform.forward * detectionDistance, detectionRadius); // End sphere
    }
}
