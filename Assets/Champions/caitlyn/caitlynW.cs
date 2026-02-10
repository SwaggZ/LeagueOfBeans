using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class caitlynW : MonoBehaviour
{
    public GameObject throwablePrefab; // The throwable object prefab
    public GameObject cam; // The camera for aiming
    public float baseThrowForce = 10f; // Base throw force
    public float maxThrowForce = 50f; // Maximum throw force
    public float cooldownTime = 3f; // Cooldown duration in seconds

    private bool isOnCooldown = false; // Tracks cooldown state
    private bool isCharging = false; // Tracks if the throw is being charged
    private float chargeStartTime; // Time when the charge started
    private int throwLevel; // Current throw level
    private int maxThrowLevel; // Maximum throw level (based on maxThrowForce)

    void Start()
    {
        // Calculate the maximum throw level based on maxThrowForce
        maxThrowLevel = Mathf.FloorToInt(maxThrowForce / 10f);
    }

    void Update()
    {
        // Handle cooldown logic
        if (isOnCooldown)
        {
            return; // Skip ability if on cooldown
        }

        // Check for input to charge the throw
        if (Input.GetKeyDown(KeyCode.Q))
        {
            StartCharging();
        }

        // Check for input release to throw
        if (Input.GetKeyUp(KeyCode.Q) && isCharging)
        {
            Throw();
        }

        // Update throw level while charging
        if (isCharging)
        {
            UpdateThrowLevel();
        }
    }

    void StartCharging()
    {
        isCharging = true;
        chargeStartTime = Time.time; // Record the time when charging starts
        throwLevel = 1; // Start at the base throw level
    }

    void UpdateThrowLevel()
    {
        float elapsedTime = Time.time - chargeStartTime; // Calculate the charge duration
        throwLevel = Mathf.Clamp(Mathf.FloorToInt(elapsedTime) + 1, 1, maxThrowLevel); // Update throw level
        Debug.Log("Throw " + throwLevel);
    }

    void Throw()
    {
        isCharging = false;
        isOnCooldown = true; // Start cooldown
        Invoke(nameof(ResetCooldown), cooldownTime);

        // Push cooldown to HUD (key 1)
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.One, cooldownTime);
        }

        // Calculate the throw force based on the throw level
        float throwForce = baseThrowForce + (throwLevel - 1) * 10f;

        // Get the current position and rotation for the throwable
        Vector3 currentPosition = cam != null ? cam.transform.position : transform.position;
        Quaternion currentRotation = cam != null ? cam.transform.rotation : transform.rotation;

        // Instantiate the throwable and add force to it
        GameObject throwable = NetworkHelper.SpawnProjectile(throwablePrefab, currentPosition, currentRotation);
        Rigidbody rb = throwable.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(cam.transform.forward * throwForce, ForceMode.Impulse);
        }

        Debug.Log($"Throwable thrown with force: {throwForce} (Level: {throwLevel})");
    }

    void ResetCooldown()
    {
        isOnCooldown = false;
    }
}
