using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoxFireMovement : MonoBehaviour
{
    private float rotationSpeed = 50f;
    float speed = 30f;
    public float damage = 20f; // Damage dealt by the fox fire

    public string targetTag = "Enemy";

    void Update()
    {
        GoToEnemy();
        DestroyIfNoTarget();
        DestroyFoxFire();
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    void GoToEnemy()
    {
        if (transform.parent == null)
        {
            GameObject closestEnemy = FindClosestObject();
            if (closestEnemy != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, closestEnemy.transform.position, speed * Time.deltaTime);

                // Apply damage when close enough
                if (Vector3.Distance(transform.position, closestEnemy.transform.position) < 0.1f)
                {
                    HealthSystem healthSystem = closestEnemy.GetComponent<HealthSystem>();
                    if (healthSystem != null)
                    {
                        healthSystem.TakeDamage(damage);
                        Debug.Log($"Fox fire dealt {damage} damage to {closestEnemy.name}.");
                    }
                    Destroy(gameObject); // Destroy fox fire after dealing damage
                }
            }
        }
    }

    public GameObject FindClosestObject()
    {
        GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(targetTag);

        if (objectsWithTag.Length == 0)
        {
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

    void DestroyIfNoTarget()
    {
        GameObject closestEnemy = FindClosestObject();
        if (closestEnemy == null)
        {
            Debug.Log("No target available. Destroying fox fire.");
            Destroy(gameObject);
        }
    }

    void DestroyFoxFire()
    {
        GameObject closestObj = FindClosestObject();
        if (closestObj != null && transform.position == closestObj.transform.position)
        {
            Destroy(gameObject);
        }
    }
}
