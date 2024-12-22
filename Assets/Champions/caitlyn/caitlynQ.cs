using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class caitlynQ : MonoBehaviour
{
    public GameObject autoAttack; // Prefab for the bullet
    public GameObject cam; // Reference to the camera
    public float cooldownTime = 1f; // Cooldown duration in seconds

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

        // Get the current position and rotation of the player
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        // Instantiate a new GameObject using the same position and rotation
        Instantiate(autoAttack, currentPosition, currentRotation);
    }

    IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false; // Cooldown complete
    }
}
