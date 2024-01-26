using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoxFireMovement : MonoBehaviour
{
    private float rotationSpeed = 50f;

    float speed = 30f;

    public string targetTag = "Enemy";

    void Update()
    {
        GoToEnemy();
        Destroy();
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    void GoToEnemy()
    {
        if (transform.parent == null)
        {
            transform.position = Vector3.MoveTowards(transform.position, FindClosestObject().transform.position, speed * Time.deltaTime);
        }
    }

    public GameObject FindClosestObject()
    {
        GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(targetTag);

        if (objectsWithTag.Length == 0)
        {
            // No objects with the specified tag found
            return null;
        }

        Transform thisTransform = transform;
        GameObject closestObject = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (GameObject obj in objectsWithTag)
        {
            Vector3 objectPosition = obj.transform.position;
            float distanceSqr = (objectPosition - thisTransform.position).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestObject = obj;
            }
        }

        return closestObject;
    }

    void Destroy()
    {
        GameObject closestObj = FindClosestObject();
        if(transform.position == closestObj.transform.position)
        {
            Destroy(gameObject);
        }
    }
}