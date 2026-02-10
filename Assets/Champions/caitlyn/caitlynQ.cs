using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class caitlynQ : MonoBehaviour
{
    public GameObject autoAttack; // Prefab for the bullet
    public GameObject cam; // Reference to the camera
    public float cooldownTime = 1f; // Cooldown duration in seconds
    public float maxDistance = 30f; // Distance limit for Q projectile

    private bool isOnCooldown = false; // Tracks whether the ability is on cooldown

    void Update()
    {
        // Check for input or trigger to activate the ability
        if (Input.GetKeyDown(KeyCode.Mouse1) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        // Set the cooldown
        isOnCooldown = true;
        StartCoroutine(CooldownRoutine());

        // Push cooldown to HUD (RightClick)
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.RightClick, cooldownTime);
        }

        // Get the current position and rotation of the player
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        // Instantiate a new GameObject using the same position and rotation
        GameObject projectile = NetworkHelper.SpawnProjectile(autoAttack, currentPosition, currentRotation);
        var autoMove = projectile.GetComponent<caitlynAutoMovement>();
        if (autoMove != null)
        {
            autoMove.isPlane = false;
            autoMove.applyKnockback = false;
            autoMove.applyStun = false;
            autoMove.piercing = int.MaxValue;
            autoMove.maxDistance = maxDistance;
        }
    }

    IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false; // Cooldown complete
    }
}
