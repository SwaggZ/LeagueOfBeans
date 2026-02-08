using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class asheQ : MonoBehaviour
{
    public GameObject autoAttack; // Prefab for the arrows
    public GameObject cam; // Reference to the camera for aiming
    public float cooldownTime = 3f; // Cooldown duration in seconds
    public int numberOfArrows = 9; // Number of arrows to fire
    public float spreadDiameter = 2f; // Diameter of the spread area

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
        if (Input.GetKeyDown(KeyCode.Mouse1) && !isOnCooldown)
        {
            StartCoroutine(RepeatWithDelay());
        }
    }

    void ActivateAbility()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        // Calculate random offsets within the spread diameter
        float randomX = Random.Range(-spreadDiameter / 2, spreadDiameter / 2);
        float randomY = Random.Range(-spreadDiameter / 2, spreadDiameter / 2);

        currentPosition.x += randomX;
        currentPosition.y += randomY;

        // Instantiate a new GameObject using the modified position and rotation
        Instantiate(autoAttack, currentPosition, currentRotation);
    }

    IEnumerator RepeatWithDelay()
    {
        isOnCooldown = true; // Start cooldown
        cooldownTimer = cooldownTime; // Set cooldown timer
        // Push cooldown to HUD (RightClick)
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.RightClick, cooldownTime);
        }

        for (int i = 0; i < numberOfArrows; i++) // Use the modular number of arrows
        {
            ActivateAbility();
            yield return new WaitForSeconds(0.025f); // Delay between shots
        }
    }
}
