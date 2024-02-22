using UnityEngine;

public class galioQ : MonoBehaviour
{
    public GameObject targetObject;
    public GameObject prefab1; // Prefab for the first object
    public GameObject prefab2; // Prefab for the second object
    public float widthScaleFactor = 1f;
    public float lengthScaleFactor = 1f; // New variable to control scale on Z axis
    public float QSpeed = 1.5f;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Check for left mouse button click
        {
            // Instantiate the first object and set it to move along the first path
        GameObject obj1 = Instantiate(prefab1, transform.position, Quaternion.identity);
        obj1.GetComponent<qMovement>().InitializeMovement(this, true);

        // Instantiate the second object and set it to move along the second path
        GameObject obj2 = Instantiate(prefab2, transform.position, Quaternion.identity);
        obj2.GetComponent<qMovement>().InitializeMovement(this, false);
        }
    }

    private void OnDrawGizmos()
    {
        if (targetObject == null)
            return;

        float stepSize = 0.1f;
        float startX = Mathf.Max(0f, 3.162f - 5f); // Adjusted to start from 0 if the initial value is less than 0
        float endX = Mathf.Min(6.324f, 3.162f + 5f); // Adjusted to end at 6.324 if the final value is greater than 6.324

        Vector3 prevPoint = targetObject.transform.position;
        for (float z = startX; z <= endX; z += stepSize)
        {
            float x = 0.1f * Mathf.Pow(z - 3.162f, 2) - 1f;
            Vector3 currentPoint = new Vector3(x * widthScaleFactor, 0, z * lengthScaleFactor); // Scale the point on Z axis
            currentPoint = transform.TransformPoint(currentPoint); // Transform local to global space
            if (x != startX)
            {
                Gizmos.DrawLine(prevPoint, currentPoint);
            }
            prevPoint = currentPoint;
        }

        if (targetObject == null)
            return;

        float stepSize2 = 0.1f;
        float startX2 = Mathf.Max(0f, 3.162f - 5f); // Adjusted to start from 0 if the initial value is less than 0
        float endX2 = Mathf.Min(6.324f, 3.162f + 5f); // Adjusted to end at 6.324 if the final value is greater than 6.324

        Vector3 prevPoint2 = targetObject.transform.position;
        for (float z2 = startX2; z2 <= endX2; z2 += stepSize2)
        {
            float x2 = -(0.1f * Mathf.Pow(z2 - 3.162f, 2) - 1f);
            Vector3 currentPoint2 = new Vector3(x2 * widthScaleFactor, 0, z2 * lengthScaleFactor); // Scale the point on Z axis
            currentPoint2 = transform.TransformPoint(currentPoint2); // Transform local to global space
            if (x2 != startX2)
            {
                Gizmos.DrawLine(prevPoint2, currentPoint2);
            }
            prevPoint2 = currentPoint2;
        }
    }
}