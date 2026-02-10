using System.Collections.Generic;
using UnityEngine;

// Light carpet that slows enemies and can be detonated for damage
public class LuxCarpet : MonoBehaviour
{
    private LuxController _owner;
    private float _damage;
    private float _slowAmount;
    private float _radius;
    private float _duration;
    private float _spawnTime;
    private HashSet<GameObject> _slowedEnemies = new HashSet<GameObject>();
    private Dictionary<GameObject, SlowStatus> _appliedSlows = new Dictionary<GameObject, SlowStatus>();

    public void Init(LuxController owner, float damage, float slowAmount, float radius, float duration)
    {
        _owner = owner;
        _damage = damage;
        _slowAmount = slowAmount;
        _radius = radius;
        _duration = duration;
        _spawnTime = Time.time;

        // Notify owner
        if (_owner != null) _owner.OnCarpetCreated(this);

        Debug.Log($"Lux carpet active. Recast E to detonate or wait {_duration}s for auto-detonate.");
    }

    void Update()
    {
        // Check for enemies in radius
        CheckForEnemies();

        // Auto-detonate after duration
        if (Time.time > _spawnTime + _duration)
        {
            Debug.Log("Lux carpet auto-detonating!");
            Detonate();
        }
    }

    void CheckForEnemies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _radius);
        HashSet<GameObject> currentlyInside = new HashSet<GameObject>();

        foreach (var col in colliders)
        {
            if (!col.CompareTag("Enemy")) continue;

            GameObject target = ModifierUtils.ResolveTarget(col);
            if (target == null) continue;

            currentlyInside.Add(target);

            // Apply slow if not already slowed by this carpet
            if (!_slowedEnemies.Contains(target))
            {
                ApplySlow(target);
                _slowedEnemies.Add(target);
            }
        }

        // Remove slow from enemies that left the carpet
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var enemy in _slowedEnemies)
        {
            if (enemy == null || !currentlyInside.Contains(enemy))
            {
                RemoveSlow(enemy);
                toRemove.Add(enemy);
            }
        }

        foreach (var enemy in toRemove)
        {
            _slowedEnemies.Remove(enemy);
        }
    }

    void ApplySlow(GameObject target)
    {
        // Apply slow via DummyController or SlowStatus component
        var dc = target.GetComponent<DummyController>();
        if (dc != null)
        {
            // ApplySlow takes speed multiplier (0.6 = 40% slow) and duration
            // DummyController.ApplySlow already handles modifier display via ModifierUtils
            dc.ApplySlow(1f - _slowAmount, 999f); // Long duration, we manage removal
        }
        else
        {
            // For CharacterControl targets, use SlowStatus component
            var slow = target.GetComponent<SlowStatus>();
            if (slow == null) slow = target.AddComponent<SlowStatus>();
            slow.Apply(_slowAmount, 999f);
            _appliedSlows[target] = slow;
        }

        Debug.Log($"Lux carpet slowing {target.name}");
    }

    void RemoveSlow(GameObject target)
    {
        if (target == null) return;

        // Remove slow
        var dc = target.GetComponent<DummyController>();
        if (dc != null)
        {
            // DummyController.ClearSlow already handles modifier removal
            dc.ClearSlow();
        }
        else
        {
            // Remove SlowStatus component
            if (_appliedSlows.TryGetValue(target, out var slow) && slow != null)
            {
                Object.Destroy(slow);
                _appliedSlows.Remove(target);
            }
        }

        Debug.Log($"Lux carpet slow removed from {target.name}");
    }

    public void Detonate()
    {
        Debug.Log($"Lux carpet detonated! Dealing {_damage} damage.");

        // Find all enemies in radius and deal damage
        Collider[] colliders = Physics.OverlapSphere(transform.position, _radius);
        HashSet<GameObject> damaged = new HashSet<GameObject>();

        foreach (var col in colliders)
        {
            if (!col.CompareTag("Enemy")) continue;

            GameObject target = ModifierUtils.ResolveTarget(col);
            if (target == null || damaged.Contains(target)) continue;

            var hp = target.GetComponent<HealthSystem>();
            if (hp != null)
            {
                hp.TakeDamage(_damage);
                Debug.Log($"Lux carpet dealt {_damage} damage to {target.name}");
                damaged.Add(target);
            }
        }

        // Clear all slows
        foreach (var enemy in _slowedEnemies)
        {
            RemoveSlow(enemy);
        }
        _slowedEnemies.Clear();

        // Visual explosion
        CreateExplosionEffect();

        // Notify owner
        if (_owner != null) _owner.OnCarpetDestroyed();

        Destroy(gameObject);
    }

    void CreateExplosionEffect()
    {
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.name = "CarpetExplosion";
        explosion.transform.position = transform.position;
        explosion.transform.localScale = Vector3.one * _radius * 2.5f;

        var col = explosion.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var mr = explosion.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 1f, 0.6f, 0.4f); // Bright yellow flash
            mr.material = mat;
        }

        Destroy(explosion, 0.25f);
    }

    void OnDestroy()
    {
        // Clean up any remaining slows
        foreach (var enemy in _slowedEnemies)
        {
            RemoveSlow(enemy);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
