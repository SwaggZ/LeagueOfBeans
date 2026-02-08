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
        if (Input.GetKeyDown(KeyCode.Q) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        // Set initial position
        Vector3 spawnOffset = Camera.main != null ? Camera.main.transform.forward * 1.0f : transform.forward * 1.0f;
        initialPosition = transform.position + spawnOffset;

        // Spawn the orb
        orbInstance = Instantiate(orbPrefab, initialPosition, Quaternion.identity);

        // ahriQ.cs
        OrbMovement orbMove = orbInstance.GetComponent<OrbMovement>();
        if (orbMove != null)
        {
            orbMove.Init(transform); // Ahri becomes the owner
        }

        // Start cooldown
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
        // Push cooldown to HUD (key 1)
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.One, cooldownTime);
        }
    }
}