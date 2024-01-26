using UnityEngine;

public class ThrowableCollisionHandler : MonoBehaviour
{
    public GameObject trapPrefab;
    public float detectionDistance = 1.0f;
    public float AutoTime = 10f;

    void Start()
    {
        Invoke("Destroy", AutoTime);
    }

    void Update()
    {
        // Check for collisions using OverlapSphere
        CheckForCollision();
    }

    void CheckForCollision()
    {
        // Create an OverlapSphere centered at the throwable GameObject's position
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionDistance);

        // Check each collider detected in the sphere
        foreach (Collider collider in colliders)
        {
            // Check if the collider belongs to an object on the "ground" layer
            if (collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                // Instantiate the bear trap at the throwable GameObject's position
                Instantiate(trapPrefab, transform.position, Quaternion.identity);

                // Destroy the throwable GameObject
                Destroy(gameObject);
                break; // Exit the loop if a collision is detected
            }
        }
    }

    void Destroy()
    {
        Destroy(gameObject);
    }
}
