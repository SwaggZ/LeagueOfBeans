using UnityEngine;

public class TornadoMovement : MonoBehaviour
{
    public float moveSpeed = 2.0f; // Speed of movement
    public float distanceThreshold = 5.0f; // Distance at which the tornado stops moving

    private Vector3 centerPoint;
    private bool moving = false;

    // Start is called before the first frame update
    void Start()
    {
        // Get the center point between the two objects
        GameObject[] objects = GameObject.FindGameObjectsWithTag("QObject");
        if (objects.Length >= 2)
        {
            centerPoint = (objects[0].transform.position + objects[1].transform.position) / 2f;
            moving = true;
        }
        else
        {
            Debug.LogError("Not enough objects with tag 'QObject' found!");
            moving = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (moving)
        {
            // Move the tornado away from the center point
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

            // Check if the tornado has reached the distance threshold
            if (Vector3.Distance(transform.position, centerPoint) >= distanceThreshold)
            {
                moving = false;
                // Optionally, you can destroy the tornado object here or perform any other desired action
            }
        }
    }
}
