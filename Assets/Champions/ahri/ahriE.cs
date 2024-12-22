using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ahriE : MonoBehaviour
{
    public GameObject Eheart; // Prefab for the heart projectile
    public GameObject cam; // Reference to the camera for direction
    public float cooldownTime = 3f; // Cooldown duration in seconds

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

        // Check for input to activate the ability if not on cooldown
        if (Input.GetKeyDown(KeyCode.Mouse1) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        // Instantiate the heart projectile at the current position and rotation
        GameObject newObject = Instantiate(Eheart, currentPosition, currentRotation);

        // Start cooldown timer
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
    }
}