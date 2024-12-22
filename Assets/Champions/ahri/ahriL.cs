using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ahriL : MonoBehaviour
{
    public GameObject autoAttack; // Prefab for the auto attack projectile
    public GameObject cam; // Reference to the camera for determining attack direction
    public float cooldownTime = 0.6f; // Cooldown duration in seconds, set for a basic attack speed similar to Overwatch

    private bool isOnCooldown = false; // Tracks whether the ability is on cooldown
    private float cooldownTimer = 0f; // Tracks remaining cooldown time

    void Update()
    {
        // Handle cooldown logic
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime; // Decrease timer as time passes

            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false; // Cooldown complete
            }
        }

        // Check if Mouse0 is held and not on cooldown
        if (Input.GetKey(KeyCode.Mouse0) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        // Capture current position and rotation for spawning the auto attack
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        // Instantiate the auto attack projectile at the current position and rotation
        GameObject newObject = Instantiate(autoAttack, currentPosition, currentRotation);

        // Start cooldown timer
        isOnCooldown = true;
        cooldownTimer = cooldownTime; // Reset the timer to the full cooldown duration
    }
}
