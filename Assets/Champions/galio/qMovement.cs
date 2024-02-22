using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class qMovement : MonoBehaviour
{
    private galioQ galioScript;
    private bool moveAlongFirstPath;
    private float zPosition;
    private float stepSize = 0.1f;
    public float speed = 1.0f; // Speed of movement

    public void InitializeMovement(galioQ script, bool moveFirst)
    {
        galioScript = script;
        moveAlongFirstPath = moveFirst;
        zPosition = moveFirst ? Mathf.Max(0f, 3.162f - 5f) : Mathf.Max(0f, 3.162f - 5f);
        transform.position = GetNextPosition();
    }

    private void Update()
    {
        transform.position = GetNextPosition();

        if (moveAlongFirstPath)
        {
            if (zPosition > Mathf.Min(6.324f, 3.162f + 5f))
            {
                Destroy(gameObject);
            }
        }
        else
        {
            if (zPosition > Mathf.Min(6.324f, 3.162f + 5f))
            {
                Destroy(gameObject);
            }
        }
    }

    private Vector3 GetNextPosition()
    {
        float x = moveAlongFirstPath ? 0.1f * Mathf.Pow(zPosition - 3.162f, 2) - 1f : -(0.1f * Mathf.Pow(zPosition - 3.162f, 2) - 1f);
        Vector3 nextPosition = new Vector3(x * galioScript.widthScaleFactor, 0, zPosition * galioScript.lengthScaleFactor);
        nextPosition = galioScript.transform.TransformPoint(nextPosition); // Transform local to global space
        zPosition += stepSize * speed; // Adjust step size based on speed
        return nextPosition;
    }
}