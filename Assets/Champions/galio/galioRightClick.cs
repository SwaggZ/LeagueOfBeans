using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class galioRightClick : MonoBehaviour
{
    [Header("Smash Settings")]
    public float impactRadius = 6f;
    public float damage = 50f;
    public float knockupForce = 10f;   // Upward power
    public float knockupDuration = 0.5f; // Stun/air time for CharacterControl
    public string enemyTag = "Enemy";

    [Header("Cooldown")]
    public float abilityCooldown = 5f;
    private bool isOnCooldown = false;

    [Header("VFX (optional)")]
    public GameObject smashVfxPrefab;

    void Update()
    {
        var r = GetComponent<galioR>();
        bool ultActive = r != null && r.IsUltActive();
        if (Input.GetMouseButtonDown(1) && !isOnCooldown && !ultActive)
        {
            StartCoroutine(DoSmash());
        }
    }

    IEnumerator DoSmash()
    {
        // Play VFX
        if (smashVfxPrefab != null)
        {
            Instantiate(smashVfxPrefab, transform.position, Quaternion.identity);
        }

        // Apply area knockup and damage
        Collider[] hits = Physics.OverlapSphere(transform.position, impactRadius);
        HashSet<GameObject> hitSet = new HashSet<GameObject>();
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            GameObject target = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.transform.root.gameObject;
            if (target == gameObject) continue;
            if (!target.CompareTag(enemyTag)) continue;
            if (hitSet.Contains(target)) continue;
            hitSet.Add(target);

            // Try via CharacterControl first
            var cc = target.GetComponent<CharacterControl>();
            if (cc != null)
            {
                cc.ApplyKnockback(Vector3.up, knockupForce, knockupForce, knockupDuration);
            }
            else
            {
                // Fallback to rigidbody force
                var rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(Vector3.up * knockupForce, ForceMode.VelocityChange);
                }
            }

            // Damage
            var hp = target.GetComponent<HealthSystem>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
        }

        // Start cooldown + HUD
        isOnCooldown = true;
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.RightClick, abilityCooldown);
        }
        yield return new WaitForSeconds(abilityCooldown);
        isOnCooldown = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}
