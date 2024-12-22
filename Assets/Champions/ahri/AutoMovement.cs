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

            // Check if the object hit has a HealthSystem component
            HealthSystem healthSystem = hit.collider.gameObject.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                Debug.Log("Projectile hit an object with HealthSystem. Applying damage.");
                healthSystem.TakeDamage(damage); // Apply damage to the object
            }

            // Destroy the projectile on any collision
            Destroy(gameObject);
        }
    }

    void DestroySelf()
    {
        Destroy(gameObject); // Destroy the projectile after the timer
    }
}