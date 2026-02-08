using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class asheL : MonoBehaviour
{
    public GameObject autoAttack; // Prefab for the arrow
    public GameObject cam; // Reference to the camera for aiming
    public float cooldownTime = 1f; // Cooldown duration in seconds

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
        if (Input.GetKeyDown(KeyCode.Mouse0) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        // Instantiate a new GameObject using the same position and rotation
        Instantiate(autoAttack, currentPosition, currentRotation);

        // Start cooldown
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
        // Push cooldown to HUD (LeftClick)
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.LeftClick, cooldownTime);
        }
    }
}
