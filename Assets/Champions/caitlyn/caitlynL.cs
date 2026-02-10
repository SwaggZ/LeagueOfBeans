using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class caitlynL : MonoBehaviour
{
    public GameObject autoAttack; // Prefab for the bullet
    public GameObject cam; // Reference to the camera
    public float cooldownTime = 1f; // Cooldown duration in seconds
    public float maxDistance = 20f; // Distance limit for auto attack projectile

    private bool isOnCooldown = false; // Tracks whether the ability is on cooldown

    void Update()
    {
        // Check for input or trigger to activate the ability
        if (Input.GetKeyDown(KeyCode.Mouse0) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        // Set the cooldown
        isOnCooldown = true;
        StartCoroutine(CooldownRoutine());

        // Push cooldown to HUD (LeftClick)
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.LeftClick, cooldownTime);
        }

        // Get the current position and rotation of the player
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        // Instantiate a new GameObject using the same position and rotation
        GameObject projectile = NetworkHelper.SpawnProjectile(autoAttack, currentPosition, currentRotation);
        var autoMove = projectile.GetComponent<caitlynAutoMovement>();
        if (autoMove != null)
        {
            autoMove.piercing = 1;
            autoMove.maxDistance = maxDistance;
        }
    }

    IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false; // Cooldown complete
    }
}
