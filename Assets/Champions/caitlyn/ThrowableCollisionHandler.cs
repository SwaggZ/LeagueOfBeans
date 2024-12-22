using UnityEngine;

public class ThrowableCollisionHandler : MonoBehaviour
{
    public GameObject trapPrefab; // The trap to instantiate
    public float detectionRadius = 0.5f; // Radius for collision detection
    public float AutoTime = 10f; // Lifetime of the throwable before self-destruction
    public float damage = 50f; // Damage dealt when the trap is triggered

    private Rigidbody rb; // Rigidbody component

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Ensure continuous dynamic collision detection for high-speed impacts
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // Schedule destruction if the throwable doesn't collide
        Invoke("DestroySelf", AutoTime);
    }

    void FixedUpdate()
    {
        CheckImmediateCollision();
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.collider);
    }

    void CheckImmediateCollision()
    {
        // Detect collisions using OverlapSphere
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                HandleCollision(collider);
                break;
            }
        }
    }

    void HandleCollision(Collider collider)
    {
        // Stop the throwable's movement
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Instantiate the trap
        InstantiateTrap();

        // Log the collision for debugging
        Debug.Log($"Trap collided with: {collider.gameObject.name}");

        // Destroy the throwable object
        Destroy(gameObject);
    }

    void InstantiateTrap()
    {
        Instantiate(trapPrefab, transform.position, Quaternion.identity);
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
