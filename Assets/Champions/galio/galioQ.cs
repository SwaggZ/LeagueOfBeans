using System.Collections;
using UnityEngine;

public class galioQ : MonoBehaviour
{
    public float startZ = 0f;
    public float endZ = 10.954f; // Adjusted for the new function range
    public float extendedEndZ = 13f; // Adjusted for the flat section
    public float step = 0.1f; // How much z increases each step
    public GameObject cam;

    public GameObject prefabForOriginalPath; // Assign in inspector
    public float speedForOriginalPath = 5f; // Speed for moving along the original path

    public GameObject prefabForFlatPath; // Assign in inspector
    public float speedForFlatPath = 3f; // Speed for moving along the flat path

    public float cooldownDuration = 5f; // Cooldown duration in seconds
    private bool isOnCooldown = false; // Flag to track cooldown state

    private bool drawGizmo = false;
    private Vector3[] gizmoPoints;
    private Vector3[] gizmoPointsInverse;
    private Vector3[] gizmoPointsFlat; // Points for the flat section

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        // Trigger the ability logic
        CreateGizmoPoints();
        CreateGizmoPointsInverse();
        CreateGizmoPointsFlat();
        drawGizmo = true;

        StartPrefabMovement();

        // Start the cooldown
        StartCoroutine(StartCooldown());
    }

    IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        Debug.Log("Ability activated. Cooldown started.");
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;
        Debug.Log("Cooldown complete. Ability ready.");
    }

    void StartPrefabMovement()
    {
        // Instantiate and start moving prefabs along the original paths
        GameObject prefabInstance1 = Instantiate(prefabForOriginalPath, gizmoPoints[0], Quaternion.identity);
        StartCoroutine(MovePrefabAlongPath(prefabInstance1, gizmoPoints, speedForOriginalPath, false));

        GameObject prefabInstance2 = Instantiate(prefabForOriginalPath, gizmoPointsInverse[0], Quaternion.identity);
        StartCoroutine(MovePrefabAlongPath(prefabInstance2, gizmoPointsInverse, speedForOriginalPath, false));
    }

    IEnumerator MovePrefabAlongPath(GameObject prefab, Vector3[] path, float speed, bool isFlatPath)
    {
        foreach (Vector3 point in path)
        {
            while (prefab.transform.position != point)
            {
                prefab.transform.position = Vector3.MoveTowards(prefab.transform.position, point, speed * Time.deltaTime);
                yield return null;
            }
        }

        if (!isFlatPath)
        {
            Destroy(prefab); // Destroy the original prefab once it reaches the end

            if (path == gizmoPoints || path == gizmoPointsInverse)
            {
                // Instantiate and move prefab along the flat path
                GameObject flatPathPrefab = Instantiate(prefabForFlatPath, path[path.Length - 1], Quaternion.identity);
                StartCoroutine(MovePrefabAlongPath(flatPathPrefab, gizmoPointsFlat, speedForFlatPath, true));
            }
        }
        else
        {
            // Destroy the flat path prefab once it reaches the end
            Destroy(prefab);
        }
    }

    void CreateGizmoPoints()
    {
        int pointsCount = Mathf.CeilToInt((endZ - startZ) / step) + 1;
        gizmoPoints = new Vector3[pointsCount];

        Vector3 playerPosition = transform.position;
        Quaternion lookRotation = Quaternion.Euler(cam.transform.eulerAngles.x, transform.eulerAngles.y, 0);

        for (int i = 0; i < pointsCount; i++)
        {
            float z = (startZ + (step * i));
            float x = (0.1f * Mathf.Pow(z - 5.477f, 2) - 3);
            Vector3 point = new Vector3(x, 0, z);
            point = lookRotation * point;
            gizmoPoints[i] = playerPosition + point;
        }
    }

    void CreateGizmoPointsInverse()
    {
        int pointsCount = Mathf.CeilToInt((endZ - startZ) / step) + 1;
        gizmoPointsInverse = new Vector3[pointsCount];

        Vector3 playerPosition = transform.position;
        Quaternion lookRotation = Quaternion.Euler(cam.transform.eulerAngles.x, transform.eulerAngles.y, 0);

        for (int i = 0; i < pointsCount; i++)
        {
            float z = (startZ + (step * i));
            float x = -(0.1f * Mathf.Pow(z - 5.477f, 2) - 3);
            Vector3 point = new Vector3(x, 0, z);
            point = lookRotation * point;
            gizmoPointsInverse[i] = playerPosition + point;
        }
    }

    void CreateGizmoPointsFlat()
    {
        int pointsCount = Mathf.CeilToInt((extendedEndZ - endZ) / step) + 1;
        gizmoPointsFlat = new Vector3[pointsCount];

        Vector3 playerPosition = transform.position;
        Quaternion lookRotation = Quaternion.Euler(cam.transform.eulerAngles.x, transform.eulerAngles.y, 0);

        for (int i = 0; i < pointsCount; i++)
        {
            float z = (endZ + (step * i));
            float x = 0; // x remains constant, applying scale to z
            Vector3 point = new Vector3(x, 0, z);
            point = lookRotation * point;
            gizmoPointsFlat[i] = playerPosition + point;
        }
    }

    void OnDrawGizmos()
    {
        if (drawGizmo)
        {
            DrawGizmoLines(gizmoPoints, Color.red);
            DrawGizmoLines(gizmoPointsInverse, Color.blue);
            DrawGizmoLines(gizmoPointsFlat, Color.green);
        }
    }

    void DrawGizmoLines(Vector3[] points, Color color)
    {
        if (points != null)
        {
            Gizmos.color = color;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }
        }
    }
}
