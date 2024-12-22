using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class caitlynAutoMovement : MonoBehaviour
{
    public float speed = 50f; // Speed of the bullet
    public float AutoTime = 5f; // Time before the bullet self-destructs
    public float damage = 20f; // Damage dealt by the bullet
    public bool isPlane = false; // Determines if the bullet should apply knockback and stun
    public float knockbackDistance = 5f; // Distance the enemy moves backward
    public float knockbackSpeed = 20f; // Speed of the knockback movement
    public float stunDuration = 2f; // Duration of the stun effect
    public int piercing = 1; // Number of enemies the bullet can pierce through

    private int enemiesHit = 0; // Tracks how many enemies have been hit

    void Start()
    {
        Invoke("DestroySelf", AutoTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, speed * Time.deltaTime))
        {
            Debug.Log($"Bullet collided with: {hit.collider.gameObject.name}");

            HealthSystem healthSystem = hit.collider.gameObject.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(damage);
                enemiesHit++; // Increment the hit counter
                Debug.Log($"Enemy hit: {hit.collider.gameObject.name}. Total hits: {enemiesHit}");
            }

            if (isPlane)
            {
                CharacterControl enemyController = hit.collider.gameObject.GetComponent<CharacterControl>();
                if (enemyController != null)
                {
                    Vector3 knockbackDir = hit.collider.transform.position - transform.position;
                    enemyController.ApplyKnockback(knockbackDir, knockbackDistance, knockbackSpeed, stunDuration);
                }
            }

            // Destroy the bullet if it has hit the maximum number of enemies
            if (enemiesHit >= piercing)
            {
                Destroy(gameObject);
            }
        }
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
