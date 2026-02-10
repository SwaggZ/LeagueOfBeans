using UnityEngine;
using System.Collections.Generic;

// Works like Caitlyn's throwable - thrown with physics, lands on Ground layer, then detects enemies
public class JhinMine : MonoBehaviour
{
    private float _damage;
    private float _aoeRadius;
    private float _lifetime;
    private bool _landed = false;
    private bool _armed = false;
    private float _armDelay = 0.5f; // Time after landing before mine can trigger
    private float _landTime;
    private float _detectionRadius = 1.2f;
    private float _throwForce = 15f;
    private Rigidbody _rb;

    public void Init(float damage, float aoeRadius, float lifetime, Vector3 throwDirection)
    {
        _damage = damage;
        _aoeRadius = aoeRadius;
        _lifetime = lifetime;

        // Apply throw force (like Caitlyn's throwable)
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.AddForce(throwDirection * _throwForce, ForceMode.Impulse);
        }

        // Schedule self-destruct
        Invoke(nameof(DestroySelf), lifetime);
    }

    void FixedUpdate()
    {
        // Check for ground collision using OverlapSphere (like Caitlyn's ThrowableCollisionHandler)
        if (!_landed)
        {
            CheckGroundCollision();
        }
    }

    void Update()
    {
        // Arm after landing + delay
        if (_landed && !_armed && Time.time > _landTime + _armDelay)
        {
            _armed = true;
            Debug.Log("Jhin mine armed!");
        }

        if (_armed)
        {
            CheckForEnemies();
        }
    }

    void CheckGroundCollision()
    {
        // Detect ground using OverlapSphere (same as Caitlyn's ThrowableCollisionHandler)
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f);

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                HandleLanding();
                break;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Also check collision enter (like Caitlyn's ThrowableCollisionHandler)
        if (!_landed && collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            HandleLanding();
        }
    }

    void HandleLanding()
    {
        _landed = true;
        _landTime = Time.time;

        // Stop movement (like Caitlyn's throwable)
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        Debug.Log("Jhin mine landed!");
    }

    void CheckForEnemies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRadius);

        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                Explode();
                return;
            }
        }
    }

    void Explode()
    {
        Debug.Log($"Jhin mine exploded! Dealing {_damage} damage in {_aoeRadius} radius.");

        // Find all enemies in AOE radius
        Collider[] hits = Physics.OverlapSphere(transform.position, _aoeRadius);
        HashSet<GameObject> damaged = new HashSet<GameObject>();

        foreach (var col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;

            GameObject target = ModifierUtils.ResolveTarget(col);
            if (target == null || damaged.Contains(target)) continue;

            var hp = target.GetComponent<HealthSystem>();
            if (hp != null)
            {
                // Damage falloff based on distance
                float dist = Vector3.Distance(transform.position, target.transform.position);
                float falloff = 1f - (dist / _aoeRadius) * 0.5f; // 50% damage at max range
                float actualDamage = _damage * Mathf.Max(0.5f, falloff);

                hp.TakeDamage(actualDamage);
                Debug.Log($"Mine dealt {actualDamage} damage to {target.name}");
                damaged.Add(target);
            }

            // Apply knockback away from mine
            var dc = target.GetComponent<DummyController>();
            if (dc != null)
            {
                Vector3 knockDir = (target.transform.position - transform.position).normalized;
                dc.ApplyKnockback(knockDir, 8f, 3f);
            }
        }

        // Visual effect placeholder - create an expanding sphere
        CreateExplosionEffect();

        Destroy(gameObject);
    }

    void CreateExplosionEffect()
    {
        // Create a brief visual explosion
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.name = "MineExplosion";
        explosion.transform.position = transform.position;
        explosion.transform.localScale = Vector3.one * _aoeRadius * 2f;

        var col = explosion.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var mr = explosion.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 0.5f, 0.1f, 0.5f); // Orange
            mr.material = mat;
        }

        Destroy(explosion, 0.3f);
    }

    void DestroySelf()
    {
        Debug.Log("Jhin mine expired.");
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _aoeRadius);
    }
}
