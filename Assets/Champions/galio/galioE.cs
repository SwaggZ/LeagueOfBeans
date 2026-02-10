using System.Collections;
using UnityEngine;

public class galioE : MonoBehaviour
{
    [Header("Ability Settings")]
    public float knockupForce = 20f;
    public float knockupDistance = 5f;
    public float knockupRange = 8f;
    public float abilityCooldown = 6f;
    public float damage = 50f;

    private bool isOnCooldown = false;
    private CharacterControl characterControl;

    void Start()
    {
        characterControl = GetComponent<CharacterControl>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isOnCooldown)
        {
            Debug.Log("E key pressed. Activating ability...");
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        Debug.Log("Galio E: Knocking up enemies!");
        ApplyKnockupToEnemies();
        StartCoroutine(StartCooldown());
        
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.Two, abilityCooldown);
        }
    }

    void ApplyKnockupToEnemies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, knockupRange);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Player") || collider.CompareTag("Ally")) continue;
            if (collider.CompareTag("Enemy"))
            {
                // Try DummyController first
                DummyController dummyCtrl = collider.GetComponent<DummyController>();
                if (dummyCtrl != null)
                {
                    Vector3 knockDirection = (collider.transform.position - transform.position).normalized;
                    knockDirection.y = 1f; // Strong upward component
                    dummyCtrl.ApplyKnockback(knockDirection, knockupDistance, knockupForce);
                    
                    // Apply damage
                    HealthSystem enemyHealth = collider.GetComponent<HealthSystem>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(damage);
                    }
                    
                    Debug.Log($"Applied knockup to {collider.gameObject.name}");
                }
                else
                {
                    // Try CharacterControl for players
                    CharacterControl charCtrl = collider.GetComponent<CharacterControl>();
                    if (charCtrl != null)
                    {
                        Vector3 knockDirection = (collider.transform.position - transform.position).normalized;
                        knockDirection.y = 1f;
                        charCtrl.ApplyKnockback(knockDirection, knockupDistance, knockupForce, 0f);
                        
                        HealthSystem enemyHealth = collider.GetComponent<HealthSystem>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.TakeDamage(damage);
                        }
                    }
                }
            }
        }
    }

    IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        Debug.Log("E ability on cooldown.");
        yield return new WaitForSeconds(abilityCooldown);
        isOnCooldown = false;
        Debug.Log("E ability ready.");
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the knockup range in the Scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, knockupRange);
    }
}
