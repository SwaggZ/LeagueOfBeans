using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class asheE : MonoBehaviour
{
    public GameObject autoAttack; // Prefab for the arrows
    public GameObject cam; // Reference to the camera for aiming
    public float cooldownTime = 5f; // Cooldown duration in seconds
    public int numberOfArrows = 9; // Number of arrows to shoot (including the center arrow)
    public float totalArcAngle = 80f; // Total arc angle for all arrows

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

        // Check for input to activate the ability
        if (Input.GetKeyDown(KeyCode.Q) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        Vector3 currentPosition = transform.position;
        Quaternion centerRotation = cam.transform.rotation;

        // Calculate the angle offset dynamically based on the total arc and number of arrows
        float angleOffset = totalArcAngle / (numberOfArrows - 1);

        // Instantiate the center arrow
        NetworkHelper.SpawnProjectile(autoAttack, currentPosition, centerRotation);

        // Calculate the number of arrows on each side of the center
        int sideArrows = (numberOfArrows - 1) / 2;

        // Create arrows with right and left angle offsets
        for (int i = 1; i <= sideArrows; i++)
        {
            // Right angle offset
            Quaternion rightRotation = centerRotation * Quaternion.Euler(0, angleOffset * i, 0);
            NetworkHelper.SpawnProjectile(autoAttack, currentPosition, rightRotation);

            // Left angle offset
            Quaternion leftRotation = centerRotation * Quaternion.Euler(0, -angleOffset * i, 0);
            NetworkHelper.SpawnProjectile(autoAttack, currentPosition, leftRotation);
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