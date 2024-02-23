using UnityEngine;

public class qMovement : MonoBehaviour
{
    private galioQ galioScript;
    private bool moveAlongFirstPath;
    private bool moveAlongThirdPath = false;
    private Vector3 targetPosition; // To store player's position
    private float zPosition;
    private float stepSize = 0.1f;
    public float speed = 1.0f;
    public Vector3 playerPosition;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        playerPosition = galioScript.transform.position; 
    }

    // Updated method to include targetPosition
    public void InitializeMovement(galioQ script, bool moveFirst, bool moveThird, Vector3 targetPos)
    {
        galioScript = script;
        moveAlongFirstPath = moveFirst;
        moveAlongThirdPath = moveThird;
        targetPosition = targetPos; // Store the target position
        zPosition = moveFirst || moveThird ? Mathf.Max(0f, 3.162f - 5f) : Mathf.Max(0f, 3.162f - 5f);
        transform.position = GetNextPositionTowardsTarget(); // Use the new method
    }

    // New method for third path initialization
    public void InitializeMovementForThirdPath(galioQ script, float startZ, float endZ, Vector3 targetPos)
    {
        galioScript = script;
        moveAlongFirstPath = false; // Ensure the prefab isn't set to move along the first or second path
        moveAlongThirdPath = true; // Indicate that this prefab is moving along the third path
        zPosition = startZ; // Start from the beginning of the third gizmo
        float maxZPosition = endZ; // Define how far along the Z axis this object should move before being destroyed
    }


    private void Update()
{
    transform.position = GetNextPosition();

    // Adjust the condition to account for the third path movement
    if (moveAlongThirdPath)
    {
        if (zPosition > 7f) // Condition for the third path's endpoint
        {
            Destroy(gameObject);
        }
    }
    else if (zPosition > Mathf.Min(6.324f, 3.162f + 5f))
    {
        if (!moveAlongThirdPath) // Check if not already moving along the third path
        {
            // Assuming galioScript.transform.position is the player position you want to use
            galioScript.InstantiateThirdPrefab(playerPosition); // Now passing the player position
        }
        Destroy(gameObject);
    }
}

    private Vector3 GetNextPositionTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized; // Direction towards the target
        float step = speed * Time.deltaTime; // Calculate the step size based on speed
        return transform.position + direction * step; // Calculate the next position towards the target
    }

    private Vector3 GetNextPosition()
    {
        float x;
        if (moveAlongThirdPath)
        {
            x = 0; // For the third path, x is always 0
        }
        else
        {
            x = moveAlongFirstPath ? 0.1f * Mathf.Pow(zPosition - 3.162f, 2) - 1f : -(0.1f * Mathf.Pow(zPosition - 3.162f, 2) - 1f);
        }
        
        Vector3 nextPosition = new Vector3(x * galioScript.widthScaleFactor, 0, zPosition * galioScript.lengthScaleFactor);
        nextPosition = galioScript.transform.TransformPoint(nextPosition); // Transform local to global space
        zPosition += stepSize * speed; // Adjust step size based on speed
        return nextPosition;
    }
}