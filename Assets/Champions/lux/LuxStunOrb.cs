using System.Collections.Generic;
using UnityEngine;

// Stun orb that stuns the first N enemies hit and deals damage
public class LuxStunOrb : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private float _maxDistance;
    private float _stunDuration;
    private int _maxTargets;
    private Vector3 _startPos;
    private HashSet<GameObject> _hitTargets = new HashSet<GameObject>();

    public void Init(float damage, float speed, float maxDistance, float stunDuration, int maxTargets)
    {
        _damage = damage;
        _speed = speed;
        _maxDistance = maxDistance;
        _stunDuration = stunDuration;
        _maxTargets = maxTargets;
        _startPos = transform.position;
    }

    void Update()
    {
        float step = _speed * Time.deltaTime;
        Vector3 nextPos = transform.position + transform.forward * step;

        // Raycast to detect hit
        if (Physics.Raycast(transform.position, transform.forward, out var hit, step))
        {
            OnHit(hit.collider, hit.point);
            // Don't return immediately - continue through to hit more targets
        }

        transform.position = nextPos;

        // Destroy after max distance or max targets hit
        if (Vector3.Distance(_startPos, transform.position) >= _maxDistance || _hitTargets.Count >= _maxTargets)
        {
            Destroy(gameObject);
        }
    }

    void OnHit(Collider col, Vector3 hitPoint)
    {
        if (col == null) return;
        if (col.CompareTag("Player")) return;
        if (col.CompareTag("Ally")) return; // Don't damage allies
        if (!col.CompareTag("Enemy")) return; // Only damage enemies

        GameObject target = ModifierUtils.ResolveTarget(col);
        if (target == null) return;

        // Skip if already hit
        if (_hitTargets.Contains(target)) return;

        // Check if we've already hit max targets
        if (_hitTargets.Count >= _maxTargets) return;

        _hitTargets.Add(target);

        // Apply damage
        var hp = target.GetComponent<HealthSystem>();
        if (hp != null)
        {
            hp.TakeDamage(_damage);
            Debug.Log($"Lux stun orb hit {target.name} for {_damage} damage.");
        }

        // Apply stun
        var dc = target.GetComponent<DummyController>();
        if (dc != null)
        {
            dc.Stun(_stunDuration);
            Debug.Log($"Lux stun orb stunned {target.name} for {_stunDuration}s!");
        }
        else
        {
            var cc = target.GetComponent<CharacterControl>();
            if (cc != null)
            {
                cc.Stun(_stunDuration);
                Debug.Log($"Lux stun orb stunned {target.name} for {_stunDuration}s!");
            }
        }

        // Pass through to hit more targets (up to max)
        transform.position = hitPoint + transform.forward * 0.2f;

        Debug.Log($"Lux stun orb: {_hitTargets.Count}/{_maxTargets} targets hit.");
    }
}
