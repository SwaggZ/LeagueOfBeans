using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoMovement : MonoBehaviour
{
    public float speed = 10f; // Movement speed of the projectile
    public float AutoTime = 5f; // Time before the projectile self-destructs
    public float damage = 20f; // Damage dealt by the projectile

    void Start()
    {
        // Schedule the projectile to destroy itself after a set time
        Invoke("DestroySelf", AutoTime);
    }

    void Update()
    {
        // Move the projectile forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Perform a raycast to detect collisions manually
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Check for collision in front of the projectile
        if (Physics.Raycast(ray, out hit, speed * Time.deltaTime))
        {
            Debug.Log($"Projectile collided with: {hit.collider.gameObject.name}, Tag: {hit.collider.gameObject.tag}");

            // Skip allies
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Ally"))
            {
                return; // Pass through allies
            }
            if (!hit.collider.CompareTag("Enemy"))
            {
                NetworkHelper.Despawn(gameObject); // Destroy on non-enemy collision
                return;
            }

            // Check if the object hit has a HealthSystem component
            HealthSystem healthSystem = hit.collider.gameObject.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                Debug.Log("Projectile hit an enemy with HealthSystem. Applying damage.");
                healthSystem.TakeDamage(damage); // Apply damage to the enemy
            }

            // Destroy the projectile on any collision
            NetworkHelper.Despawn(gameObject);
        }
    }

    void DestroySelf()
    {
        NetworkHelper.Despawn(gameObject); // Destroy the projectile after the timer
    }
}