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

    void Start()
    {
        // Find the player in the scene
        player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Player object with tag 'Player' not found in the scene.");
        }

        // Schedule the projectile to destroy its child objects after a set time
        Invoke("DestroyChildObjects", AutoTime);
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

            // Destroy only the child objects on collision
            DestroyChildObjects();
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
            }
        }
    }

    void DestroyChildObjects()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("Child objects of heart destroyed.");
    }
}
