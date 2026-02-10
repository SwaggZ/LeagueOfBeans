using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class galioR : MonoBehaviour, IIncomingDamageModifier
{
    [Header("Ability Settings")]
    public float maxHeight = 10f; // Maximum height Galio lifts to
    public float liftDuration = 0.5f; // Time to lift up
    public float aimDuration = 3f; // Time player has to aim before auto-casting
    public float impactRadius = 8f; // Radius of the impact knockup effect
    public float knockupForce = 10f; // Force of the knockup effect
    public float knockupDuration = 0.5f; // Stun duration from knockup
    public string enemyTag = "Enemy"; // Tag to identify enemies
    public float abilityCooldown = 12f; // Cooldown duration for the ability

    private Vector3 startPosition; // Starting position before lift
    private bool isAiming = false; // Whether player is in aiming phase
    private bool isUltActive = false; // Block other abilities and reduce damage while true
    private bool isOnCooldown = false; // Whether ability is on cooldown
    private float aimTimer = 0f; // Timer for aim duration
    private CharacterControl characterControl; // Reference to movement system
    private HashSet<GameObject> _impactHitTargets = new HashSet<GameObject>();

    void Start()
    {
        characterControl = GetComponent<CharacterControl>();
        if (characterControl == null)
        {
            Debug.LogError("CharacterControl component not found on Galio!");
        }
    }

    void Update()
    {
        // Check for ability activation on 'E'
        if (Input.GetKeyDown(KeyCode.E) && !isOnCooldown)
        {
            StartCoroutine(CastAbility());
        }
    }

    IEnumerator CastAbility()
    {
        startPosition = transform.position;
        Debug.Log("[GalioR] CastAbility started");
        isUltActive = true; // block other abilities and enable DR
        // Show DR indicator (50%) while active
        ModifierUtils.ApplyModifier(gameObject, "GalioDR50", null, "50%", -1f, 0, includePlayerHud: true, includeEnemy: false);

        // Step 1: Lift phase - move upward to maxHeight
        // Disable CharacterControl gravity so Galio can stay aloft during the aim phase
        if (characterControl != null)
        {
            characterControl.SetGravityEnabled(false);
        }
        Debug.Log("[GalioR] Starting LIFT phase");
        yield return StartCoroutine(LiftUp());
        Debug.Log("[GalioR] LIFT phase complete!");

        // Step 2: Aiming phase - fixed duration to allow movement/aiming
        Debug.Log("[GalioR] Starting AIM phase (" + aimDuration + "s)");
        isAiming = true;
        float aimTimer = aimDuration;
        // Count down aim timer while allowing player movement each frame
        while (aimTimer > 0f)
        {
            aimTimer -= Time.deltaTime;
            yield return null;
        }
        isAiming = false;
        Debug.Log("[GalioR] AIM phase complete!");

        // Step 3: Landing and impact phase
        Debug.Log("[GalioR] Starting LAND and IMPACT phase");
        yield return StartCoroutine(LandAndImpact());

        // Re-enable CharacterControl gravity after landing so normal physics resume
        if (characterControl != null)
        {
            characterControl.SetGravityEnabled(true);
        }
        Debug.Log("[GalioR] LAND and IMPACT phase complete!");

        // Step 4: Start cooldown (ult phase ends here; unlock and DR off during cooldown)
        isUltActive = false;
        ModifierUtils.RemoveModifier(gameObject, "GalioDR50", removePlayerHud: true, removeEnemy: false);
        Debug.Log("[GalioR] Starting COOLDOWN phase");
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.Two, abilityCooldown);
        }
        yield return StartCoroutine(StartCooldown());
        Debug.Log("[GalioR] Ability fully complete!");
    }

    IEnumerator LiftUp()
    {
        Vector3 targetPosition = startPosition + Vector3.up * maxHeight;
        float elapsed = 0f;
        Debug.Log($"[GalioR] Lifting from {startPosition} to {targetPosition} over {liftDuration}s");

        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / liftDuration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        Debug.Log($"[GalioR] Lift complete! At height: {targetPosition.y}");
    }

    IEnumerator LandAndImpact()
    {
        // Wait for landing animation to complete
        yield return StartCoroutine(LandDown());

        // Once landed, apply knockup and damage
        ApplyImpactEffect();
    }

    IEnumerator LandDown()
    {
        // Land at current lateral position (x, z) but drop Y back to ground level
        Vector3 impactPos = new Vector3(transform.position.x, startPosition.y, transform.position.z);
        float elapsed = 0f;
        Debug.Log($"[GalioR] Landing from {transform.position} to {impactPos} over {liftDuration}s");

        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / liftDuration;
            transform.position = Vector3.Lerp(transform.position, impactPos, t);
            yield return null;
        }

        // Ensure final position is exact
        transform.position = impactPos;
        Debug.Log($"[GalioR] Land complete! At position: {impactPos}");
    }

    void ApplyImpactEffect()
    {
        // Perform sphere overlap at current position to find enemies (allowing for movement during aim)
        Collider[] hits = Physics.OverlapSphere(transform.position, impactRadius);

        _impactHitTargets.Clear();

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;

            // Resolve top-level gameobject for deduplication (handles multiple child colliders)
            GameObject target = hit.transform.root.gameObject;

            // Ignore self
            if (target == this.gameObject) continue;

            // Skip allies
            if (target.CompareTag("Player") || target.CompareTag("Ally")) continue;

            // Only hit enemies
            if (!target.CompareTag(enemyTag)) continue;

            if (_impactHitTargets.Contains(target)) continue;
            _impactHitTargets.Add(target);

            // Apply damage if HealthSystem exists on the target
            HealthSystem healthSystem = target.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(50f); // Base impact damage
            }

            // Try DummyController first (for enemies/dummies)
            DummyController dummyControl = target.GetComponent<DummyController>();
            if (dummyControl != null)
            {
                Vector3 knockupDir = new Vector3(0f, 1f, 0f); // Pure upward knockup
                dummyControl.ApplyKnockback(knockupDir, knockupForce, knockupForce, knockupDuration);
                Debug.Log($"Galio R knockup applied to {target.name} (DummyController)");
            }
            else
            {
                // Try CharacterControl on the target gameobject
                CharacterControl enemyControl = target.GetComponent<CharacterControl>();
                if (enemyControl != null)
                {
                    Vector3 knockupDir = new Vector3(0f, 1f, 0f); // Pure upward knockup
                    enemyControl.ApplyKnockback(knockupDir, knockupForce, knockupForce, knockupDuration);
                    Debug.Log($"Galio R knockup applied to {target.name}");
                }
                else
                {
                    // Fallback: apply force via rigidbody on the collider or parent
                    Rigidbody rb = hit.attachedRigidbody ?? target.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(Vector3.up * knockupForce, ForceMode.VelocityChange);
                        Debug.Log($"Galio R knockup (rigidbody) applied to {target.name}");
                    }
                    else
                    {
                        // Last resort: send message to the target
                        target.SendMessage("ApplyKnockup", knockupForce, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }

        Debug.Log($"Galio R impact! Knockup applied to enemies in radius {impactRadius}");
    }

    IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        Debug.Log($"Galio R on cooldown for {abilityCooldown} seconds.");
        yield return new WaitForSeconds(abilityCooldown);
        isOnCooldown = false;
        Debug.Log("Galio R ready!");
    }

    // IIncomingDamageModifier: 50% damage while ult is active
    public float GetIncomingDamageMultiplier()
    {
        return isUltActive ? 0.5f : 1f;
    }

    // Public read-only flag for other abilities to check
    public bool IsUltActive()
    {
        return isUltActive;
    }

    // Gizmo visualization
    void OnDrawGizmosSelected()
    {
        if (isAiming)
        {
            // During aiming, show impact radius at current position (where knockup will happen)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, impactRadius);
        }
    }
}
