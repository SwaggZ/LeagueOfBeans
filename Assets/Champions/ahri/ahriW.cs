using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ahriW : MonoBehaviour
{
    public GameObject foxFireContainerPrefab;
    private GameObject foxFireContainer;
    public float cooldownTime = 5f; // Cooldown duration in seconds

    private bool isOnCooldown = false; // Tracks whether the ability is on cooldown
    private float cooldownTimer = 0f; // Tracks remaining cooldown time

    void Update()
    {
        // Handle cooldown logic
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false; // Cooldown complete
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && !isOnCooldown)
        {
            CreateFoxFireContainer();
        }
    }

    void CreateFoxFireContainer()
    {
        foxFireContainer = Instantiate(foxFireContainerPrefab, transform.position, Quaternion.identity);
        isOnCooldown = true; // Start cooldown
        cooldownTimer = cooldownTime;

        // Destroy the fox fire container after its lifetime
        Destroy(foxFireContainer, 5f); // Adjust the lifetime duration as needed
    }
}