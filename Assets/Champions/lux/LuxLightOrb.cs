using UnityEngine;

// Simple light orb that deals damage only - no modifiers
public class LuxLightOrb : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private float _maxDistance;
    private Vector3 _startPos;

    public void Init(float damage, float speed, float maxDistance)
    {
        _damage = damage;
        _speed = speed;
        _maxDistance = maxDistance;
        _startPos = transform.position;
    }

    void Update()
    {
        float step = _speed * Time.deltaTime;
        Vector3 nextPos = transform.position + transform.forward * step;

        // Raycast to detect hit
        if (Physics.Raycast(transform.position, transform.forward, out var hit, step))
        {
            OnHit(hit.collider);
            return;
        }

        transform.position = nextPos;

        if (Vector3.Distance(_startPos, transform.position) >= _maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnHit(Collider col)
    {
        if (col == null) return;
        if (col.CompareTag("Player") || col.CompareTag("Ally"))
        {
            Destroy(gameObject); // Destroy on ally contact without dealing damage
            return;
        }
        if (!col.CompareTag("Enemy")) return; // Only damage enemies

        GameObject target = ModifierUtils.ResolveTarget(col);
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Apply damage only
        var hp = target.GetComponent<HealthSystem>();
        if (hp != null)
        {
            hp.TakeDamage(_damage);
            Debug.Log($"Lux light orb hit {target.name} for {_damage} damage.");
        }

        Destroy(gameObject);
    }
}
