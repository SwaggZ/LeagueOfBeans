using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoxFireContainer : MonoBehaviour
{
    public GameObject foxFirePrefab;
    public int numberOfFoxFires = 3;
    public float orbitDistance = 3f; // Distance from the player
    public float rotationSpeed = 50f;
    public float containerLifetime = 5f; // Adjust this to change the container's lifetime

    public string targetTag = "Enemy";
    public float detectionDistance = 10f;

    private Transform player;
    private List<GameObject> foxFires = new List<GameObject>();

    private void Start()
    {
        var reg = FindObjectOfType<PlayerRegistration>(true);
        if (reg != null)
        {
            player = reg.transform;
        }
        else
        {
            var playerObj = LocalPlayerRef.GetLocalPlayerWithFallback();
            player = playerObj != null ? playerObj.transform : null;
        }

        // Follow the player's position and rotation
        if (player != null)
        {
            transform.position = player.position;
        }
        Invoke("DestroyContainer", containerLifetime);

        SummonFoxFires();
    }

    void Update()
    {
        // Follow the player's position and rotation
        if (player != null)
        {
            transform.position = player.position;
        }
        RotateContainer();
        EnemyDetected();
    }

    void SummonFoxFires()
    {
        // Clear existing fox fires
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        foxFires.Clear();

        // Spawn new fox fires
        for (int i = 0; i < numberOfFoxFires; i++)
        {
            float angle = i * 360f / numberOfFoxFires;
            Vector3 spawnPosition = transform.position + Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, orbitDistance);
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            GameObject foxFire = Instantiate(foxFirePrefab, spawnPosition, rotation, transform);
            foxFires.Add(foxFire);
            FoxFireMovement foxFireMovement = foxFire.GetComponent<FoxFireMovement>();
            foxFireMovement.SetRotationSpeed(rotationSpeed);
            foxFireMovement.SetDamage(20f); // Set damage value for each fox fire
        }
    }

    void RotateContainer()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void DestroyContainer()
    {
        Destroy(gameObject);
    }

    void EnemyDetected()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionDistance);

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag(targetTag))
            {
                // The object with the specified tag is within the detection distance
                Debug.Log("Target object detected: " + collider.gameObject.name);
                DetachAllFoxFires();
                DestroyContainer();
                break;
            }
        }
    }

    void DetachAllFoxFires()
    {
        foreach (GameObject foxFire in foxFires)
        {
            if (foxFire != null && foxFire.transform.parent != null)
            {
                foxFire.transform.SetParent(null); // Detach from the parent
            }
        }
    }
}