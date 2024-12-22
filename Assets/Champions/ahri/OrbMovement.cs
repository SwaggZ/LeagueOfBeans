using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbMovement : MonoBehaviour
{
    public Transform player;
    private Transform cam;
    private bool isReturning = false;
    public float speed = 15f;
    public float BackSpeed = 20f;
    public float maxDistance = 30f; // Maximum distance the orb travels before returning
    public float damage = 50f; // Damage dealt by the orb
    public float damageCooldown = 0.4f; // Cooldown between consecutive damage to the same enemy

    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private Dictionary<GameObject, float> damageCooldownTimers = new Dictionary<GameObject, float>(); // Tracks cooldowns for each enemy

    void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (cam == null || player == null)
        {
            Debug.LogError("MainCamera or Player tag missing in the scene.");
        }

        initialPosition = transform.position;
        targetPosition = initialPosition + cam.forward * maxDistance; // Calculate the target position based on distance
    }

    void Update()
    {
        // Perform a raycast to detect collisions manually
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Check for collision in front of the projectile
        if (Physics.Raycast(ray, out hit, speed * Time.deltaTime))
        {
            Debug.Log($"Projectile collided with: {hit.collider.gameObject.name}, Tag: {hit.collider.gameObject.tag}");

            // Check if the object hit has a HealthSystem component
            HealthSystem healthSystem = hit.collider.gameObject.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                Debug.Log("Projectile hit an object with HealthSystem. Attempting to apply damage.");

                // Check if the enemy is off cooldown for damage
                if (!damageCooldownTimers.ContainsKey(hit.collider.gameObject) || Time.time >= damageCooldownTimers[hit.collider.gameObject])
                {
                    Debug.Log("Damage applied to enemy.");
                    healthSystem.TakeDamage(damage); // Apply damage to the object

                    // Set the cooldown timer for this enemy
                    damageCooldownTimers[hit.collider.gameObject] = Time.time + damageCooldown;
                }
                else
                {
                    Debug.Log("Enemy is on damage cooldown.");
                }
            }
        }

        if (!isReturning)
        {
            // Move towards the target position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            // Check if the orb reaches its target position
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isReturning = true;
            }
        }
        else
        {
            // Return to the player
            transform.position = Vector3.MoveTowards(transform.position, player.position, BackSpeed * Time.deltaTime);

            // Destroy the orb when it reaches the player
            if (Vector3.Distance(transform.position, player.position) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
