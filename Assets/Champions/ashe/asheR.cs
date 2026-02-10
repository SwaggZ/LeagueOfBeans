using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class asheR : MonoBehaviour
{
    public GameObject ultimateProjectile; // Prefab for the ultimate ability projectile
    public GameObject cam; // Reference to the camera for aiming
    public float cooldownTime = 15f; // Cooldown duration in seconds

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
        if (Input.GetKeyDown(KeyCode.E) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        // Instantiate the ultimate projectile using the same position and rotation
        NetworkHelper.SpawnProjectile(ultimateProjectile, currentPosition, currentRotation);

        // Start cooldown
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
        // Push cooldown to HUD (key 2)
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.Two, cooldownTime);
        }
    }
}
