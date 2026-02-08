using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsheAutoMovement : MonoBehaviour
{
    public float speed = 10f; // Speed of the arrow
    public float AutoTime = 5f; // Time before the arrow self-destructs
    public float damage = 20f; // Damage dealt by the arrow
    public float homingRange = 10f; // Range within which the arrow can curve towards an enemy
    public float curveIntensity = 2f; // Intensity of the homing curve
    public bool isCurving = true; // Toggle for enabling/disabling the homing effect
    public bool isUlt = false; // Toggle for ultimate ability behavior
    public float ultRadius = 5f; // Radius of the area damage for ultimate
    public float maxUltDamage = 50f; // Maximum damage for enemies at the center of the area

    [Header("Forward-only homing")]
    [Tooltip("Center of the homing query is placed this many units in front of the arrow.")]
    public float forwardHomingOffset = 1.5f;

    [Tooltip("Radius of the small area *in front* of the arrow that can acquire a target.")]
    public float forwardHomingRadius = 2.0f;

    private GameObject targetEnemy; // The current target for homing

    void Start()
    {
        // Schedule the projectile to destroy itself after a set time
        Invoke("DestroySelf", AutoTime);
    }

    void Update()
    {
        if (isCurving)
        {
            FindClosestEnemy(); // Update the target enemy

            // Curve slightly towards the target enemy if one is found
            if (targetEnemy != null)
            {
                Vector3 directionToTarget = (targetEnemy.transform.position - transform.position).normalized;
                Vector3 newDirection = Vector3.Lerp(transform.forward, directionToTarget, curveIntensity * Time.deltaTime).normalized;
                transform.rotation = Quaternion.LookRotation(newDirection);
            }
        }

        // Move the arrow forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Perform a raycast to detect collisions manually
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Check for collision in front of the projectile
        if (Physics.Raycast(ray, out hit, speed * Time.deltaTime))
        {
            Debug.Log($"Projectile collided with: {hit.collider.gameObject.name}, Tag: {hit.collider.gameObject.tag}");

            // Check if the object hit has a HealthSystem component
            HealthSystem healthSystem = hit.collider.gameObject.GetComponent<HealthSystem>();

            if (isUlt)
            {
                if (hit.collider.CompareTag("Enemy") && healthSystem != null)
                {
                    Debug.Log("Ultimate arrow hit an enemy. Applying damage.");
                    healthSystem.TakeDamage(damage); // Apply direct hit damage to the enemy
                    ApplyAreaDamage();
                }
                else
                {
                    Debug.Log("Ultimate arrow hit an object. Applying area damage.");
                    ApplyAreaDamage();
                }
            }
            else
            {
                if (healthSystem != null)
                {
                    Debug.Log("Projectile hit an object with HealthSystem. Applying damage.");
                    healthSystem.TakeDamage(damage); // Apply damage to the object
                }
            }

            Destroy(gameObject);
        }
    }

    void FindClosestEnemy()
    {
        // Position the detection sphere slightly in front of the arrow
        Vector3 queryCenter = transform.position + transform.forward * forwardHomingOffset;

        // Get everything in that small forward sphere
        Collider[] hits = Physics.OverlapSphere(
            queryCenter,
            forwardHomingRadius,
            ~0, // everything
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = homingRange;
        targetEnemy = null;

        foreach (Collider hit in hits)
        {
            // ONLY enemies
            if (!hit.CompareTag("Enemy"))
                continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetEnemy = hit.gameObject;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!isCurving) return;
        Vector3 queryCenter = transform.position + transform.forward * forwardHomingOffset;
        Gizmos.DrawWireSphere(queryCenter, forwardHomingRadius);
    }

    void ApplyAreaDamage()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, ultRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                float distanceToCenter = Vector3.Distance(transform.position, collider.transform.position);
                float damageMultiplier = 1f - (distanceToCenter / ultRadius); // Reduce damage based on distance
                float finalDamage = Mathf.Max(maxUltDamage * damageMultiplier, 0f);

                HealthSystem healthSystem = collider.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    Debug.Log($"Applying {finalDamage} area damage to {collider.gameObject.name}.");
                    healthSystem.TakeDamage(finalDamage);
                }
            }
        }
    }

    void DestroySelf()
    {
        Destroy(gameObject); // Destroy the arrow after the timer
    }
}
