using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartMovement : MonoBehaviour
{
    public float speed = 20f; // Speed of the heart projectile
    public float AutoTime = 3f; // Time before the projectile self-destructs
    public float damage = 40f; // Damage dealt by the heart projectile
    public float pullDuration = 2f; // Duration for pulling the enemy
    public float pullSpeed = 5f; // Speed at which the enemy moves towards the player

    private GameObject player; // Reference to the player
    private GameObject targetEnemy; // Reference to the enemy being pulled
    private float pullElapsedTime = 0f; // Tracks pull effect time
    private bool impacted = false; // Whether the projectile has impacted/expired

    void Start()
    {
        // Find the player in the scene
        player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Player object with tag 'Player' not found in the scene.");
        }

        // Schedule auto-expire
        Invoke(nameof(ImpactTimeout), AutoTime);
    }

    void Update()
    {
        if (!impacted)
        {
            // Move the projectile forward until we impact/expire
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

            // Perform a raycast to detect collisions manually
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            // Check for collision in front of the projectile
            if (Physics.Raycast(ray, out hit, speed * Time.deltaTime))
            {
                Debug.Log($"Heart collided with: {hit.collider.gameObject.name}, Tag: {hit.collider.gameObject.tag}");

                // Check if the object hit has a HealthSystem component
                HealthSystem healthSystem = hit.collider.gameObject.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    Debug.Log("Heart hit an object with HealthSystem. Applying damage.");
                    healthSystem.TakeDamage(damage); // Apply damage to the object

                    // Check if the object is an enemy
                    if (hit.collider.gameObject.CompareTag("Enemy"))
                    {
                        Debug.Log("Heart hit an enemy. Starting pull effect.");
                        targetEnemy = hit.collider.gameObject; // Set the enemy as the target for pulling
                        pullElapsedTime = 0f; // Reset pull timer
                    }
                    else
                    {
                        Debug.LogWarning("Object hit is not tagged as 'Enemy'.");
                    }
                }

                // On collision we impact immediately (stop movement & visuals)
                ImpactNow();
            }
        }

        // Handle pulling the enemy if a target is set
        if (targetEnemy != null && pullElapsedTime < pullDuration)
        {
            if (player == null)
            {
                Debug.LogWarning("Player object is null. Stopping pull effect.");
                targetEnemy = null;
                return;
            }

            // Move the enemy towards the player
            targetEnemy.transform.position = Vector3.MoveTowards(targetEnemy.transform.position, player.transform.position, pullSpeed * Time.deltaTime);
            Debug.Log($"Pulling enemy. New position: {targetEnemy.transform.position}");

            pullElapsedTime += Time.deltaTime;

            // Stop pulling when duration is reached
            if (pullElapsedTime >= pullDuration)
            {
                Debug.Log("Pull effect completed.");
                targetEnemy = null;
                // After pull completes, remove projectile completely
                Destroy(gameObject);
            }
        }
    }

    // Called by timeout to end the projectile gracefully
    void ImpactTimeout()
    {
        ImpactNow();
        // If we timed out without an enemy, just destroy entirely
        if (targetEnemy == null) Destroy(gameObject);
    }

    // Stop visuals and further movement/raycasting
    void ImpactNow()
    {
        if (impacted) return;
        impacted = true;
        foreach (Transform child in transform)
        {
            if (child != null) Destroy(child.gameObject);
        }
        Debug.Log("Heart impacted: visuals removed and movement stopped.");
    }
}
