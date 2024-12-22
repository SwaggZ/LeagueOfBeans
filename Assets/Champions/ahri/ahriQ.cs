using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ahriQ : MonoBehaviour
{
    public Transform orbPrefab;
    public float cooldownTime = 5f; // Cooldown duration in seconds

    private Transform orbInstance;
    private Vector3 initialPosition;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;

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

        // Check for input to activate the ability
        if (Input.GetKeyDown(KeyCode.Alpha1) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        // Set initial position
        initialPosition = transform.position;

        // Spawn the orb
        orbInstance = Instantiate(orbPrefab, initialPosition, Quaternion.identity);

        // Start cooldown
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
    }
}