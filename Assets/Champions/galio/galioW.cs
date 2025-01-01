using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class galioW : MonoBehaviour
{
    public GameObject tornadoPrefab; // Assign the tornado prefab in the inspector
    public float growDuration = 0.75f; // Time for the tornado to grow to full size
    public float pullDuration = 2f; // Duration of the pull effect
    public float pullSpeed = 10f; // Speed at which enemies are pulled
    public float pullRadius = 5f; // Radius of the tornado's pull effect
    public float healthBoost = 250f; // Max HP boost
    public float healthBoostDuration = 2f; // Duration of the health boost
    public float damage = 30f; // Single damage applied to enemies
    public float abilityCooldown = 5f; // Cooldown duration for the ability
    public Vector3 desiredTornadoScale = Vector3.one; // The desired tornado scale

    private GameObject currentTornado; // Reference to the active tornado prefab
    private bool isGrowing = false; // To track the tornado growth
    private float currentPullTime = 0f; // Timer for the pull effect
    private List<GameObject> pulledEnemies = new List<GameObject>(); // Enemies affected by the pull
    private bool isOnCooldown = false; // To track ability cooldown state

    private CharacterControl characterControl; // Reference to Galio's movement system
    private HealthSystem healthSystem; // Reference to Galio's health system

    void Start()
    {
        characterControl = GetComponent<CharacterControl>();
        healthSystem = GetComponent<HealthSystem>();

        if (characterControl == null)
        {
            Debug.LogError("CharacterControl component not found on Galio!");
        }

        if (healthSystem == null)
        {
            Debug.LogError("HealthSystem component not found on Galio!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && !isOnCooldown)
        {
            Debug.Log("Alpha1 key pressed. Activating ability...");
            ActivateAbility();
        }

        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            Debug.Log("Alpha1 key released. Ending ability...");
            EndAbility();
        }

        if (isGrowing)
        {
            GrowTornado();
        }

        if (currentTornado != null)
        {
            StickTornadoToGalio();
        }

        if (currentPullTime < pullDuration && pulledEnemies.Count > 0)
        {
            AttractEnemies();
        }
    }

    void ActivateAbility()
    {
        // Slow Galio
        if (characterControl != null)
        {
            characterControl.speed *= 0.5f;
            Debug.Log("Galio's speed slowed.");
        }

        // Spawn and start growing the tornado
        currentTornado = Instantiate(tornadoPrefab, transform.position, Quaternion.identity);
        if (currentTornado != null)
        {
            currentTornado.transform.localScale = Vector3.zero; // Start at scale 0
            isGrowing = true;
            Debug.Log("Tornado instantiated and growth started.");
        }
        else
        {
            Debug.LogError("Failed to instantiate tornado prefab!");
        }
    }

    void GrowTornado()
    {
        if (currentTornado != null)
        {
            float scaleIncreaseRate = 1f / growDuration * Time.deltaTime;
            Vector3 scaleIncrease = (desiredTornadoScale - Vector3.zero) * scaleIncreaseRate;
            currentTornado.transform.localScale += scaleIncrease;

            // Clamp to the desired scale
            if (currentTornado.transform.localScale.magnitude >= desiredTornadoScale.magnitude)
            {
                currentTornado.transform.localScale = desiredTornadoScale;
                isGrowing = false;
                Debug.Log("Tornado growth completed.");
            }
        }
        else
        {
            Debug.LogError("Tornado is null during growth!");
        }
    }

    void StickTornadoToGalio()
    {
        if (currentTornado != null)
        {
            currentTornado.transform.position = transform.position;
        }
    }

    void EndAbility()
    {
        // Reset Galio's speed
        if (characterControl != null)
        {
            characterControl.speed /= 0.5f; // Restore original speed
            Debug.Log("Galio's speed restored.");
        }

        // Detect enemies in the tornado's pull range
        Debug.Log($"Detecting enemies within pull radius: {pullRadius}");
        ApplyDamage();

        // Destroy the tornado prefab
        if (currentTornado != null)
        {
            Destroy(currentTornado);
            Debug.Log("Tornado destroyed.");
        }

        // Start pulling enemies
        currentPullTime = 0f;

        // Apply temporary max HP boost
        StartCoroutine(ApplyHealthBoost());

        // Start cooldown
        StartCoroutine(StartCooldown());
    }

    void ApplyDamage()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pullRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                HealthSystem enemyHealth = collider.GetComponent<HealthSystem>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                    Debug.Log($"Dealt {damage} damage to {collider.gameObject.name}. Current health: {enemyHealth.GetCurrentHealth()}");
                }
            }
        }
    }

    void AttractEnemies()
    {
        currentPullTime += Time.deltaTime;

        foreach (GameObject enemy in pulledEnemies)
        {
            if (enemy != null)
            {
                enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, transform.position, pullSpeed * Time.deltaTime);
                Debug.Log($"Pulling enemy {enemy.name} towards Galio. Current position: {enemy.transform.position}");
            }
        }

        if (currentPullTime >= pullDuration)
        {
            pulledEnemies.Clear();
            Debug.Log("Pull effect ended. Enemies cleared.");
        }
    }

    IEnumerator ApplyHealthBoost()
    {
        float originalMaxHealth = healthSystem.maxHealth;
        healthSystem.maxHealth += healthBoost;
        Debug.Log($"Galio's max health increased by {healthBoost}. Current max health: {healthSystem.maxHealth}");

        yield return new WaitForSeconds(healthBoostDuration);

        healthSystem.maxHealth = originalMaxHealth;
        Debug.Log("Galio's max health boost expired. Restored to original.");
    }

    IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        Debug.Log("Ability on cooldown.");
        yield return new WaitForSeconds(abilityCooldown);
        isOnCooldown = false;
        Debug.Log("Ability ready.");
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the pull range in the Scene view
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pullRadius);

        // Visualize the current tornado's desired scale in the Scene view
        if (currentTornado != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTornado.transform.position, desiredTornadoScale.x / 2);
        }
    }
}
