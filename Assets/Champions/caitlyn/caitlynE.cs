using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class caitlynE : MonoBehaviour
{
    public GameObject autoAttack; // The net projectile prefab
    public GameObject cam; // Reference to the camera for aiming

    public float pushBackDistance = 5f; // Distance Caitlyn is pushed back
    public float pushBackSpeed = 20f; // Speed of the push-back movement
    public float cooldownTime = 3f; // Cooldown duration in seconds

    private bool isOnCooldown = false; // Tracks whether the ability is on cooldown
    private CharacterController controller; // Reference to the CharacterController

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("No CharacterController found on the GameObject.");
        }
    }

    void Update()
    {
        // Check for input and ensure the ability is not on cooldown
        if (Input.GetKeyDown(KeyCode.Alpha2) && !isOnCooldown)
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        // Start cooldown
        isOnCooldown = true;
        Invoke(nameof(ResetCooldown), cooldownTime);

        // Spawn the projectile
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;
        Instantiate(autoAttack, currentPosition, currentRotation);

        // Apply push-back movement
        StartCoroutine(PushBack());
    }

    IEnumerator PushBack()
    {
        float remainingDistance = pushBackDistance;

        // Calculate the direction opposite to where the camera is facing
        Vector3 pushBackDirection = -cam.transform.forward;

        while (remainingDistance > 0f)
        {
            // Calculate the step for this frame
            float step = pushBackSpeed * Time.deltaTime;

            // Clamp the step to the remaining distance
            step = Mathf.Min(step, remainingDistance);

            // Move Caitlyn in the push-back direction
            controller.Move(pushBackDirection * step);

            // Reduce the remaining distance
            remainingDistance -= step;

            yield return null; // Wait until the next frame
        }
    }

    void ResetCooldown()
    {
        isOnCooldown = false;
    }
}
