using UnityEngine;
using System.Collections.Generic;

public class JhinBounceBullet : MonoBehaviour
{
    private JhinController _owner;
    private float _baseDamage;
    private float _speed;
    private float _arcSpeed;
    private float _maxDistance;
    private int _bouncesRemaining;
    private float _bounceRadius;
    private float _missingHealthBonus;
    private Vector3 _startPos;
    private HashSet<GameObject> _hitTargets = new HashSet<GameObject>();
    private Transform _currentTarget;
    private float _accumulatedBonus = 0f;

    // Arc movement variables
    private bool _isArcing = false;
    private Vector3 _arcStart;
    private Vector3 _arcEnd;
    private float _arcProgress = 0f;
    private float _arcHeight = 2f;
    private float _arcDuration;

    public void Init(JhinController owner, float damage, float speed, float arcSpeed, float maxDistance, int bounces, float bounceRadius, float missingHealthBonus)
    {
        _owner = owner;
        _baseDamage = damage;
        _speed = speed;
        _arcSpeed = arcSpeed;
        _maxDistance = maxDistance;
        _bouncesRemaining = bounces;
        _bounceRadius = bounceRadius;
        _missingHealthBonus = missingHealthBonus;
        _startPos = transform.position;
    }

    void Update()
    {
        // If arcing to next target, follow arc trajectory
        if (_isArcing && _currentTarget != null)
        {
            _arcProgress += Time.deltaTime / _arcDuration;
            
            if (_arcProgress >= 1f)
            {
                // Arrived at target
                _isArcing = false;
                transform.position = _currentTarget.position;
                
                // Check for hit
                Collider[] cols = Physics.OverlapSphere(transform.position, 0.5f);
                foreach (var col in cols)
                {
                    if (col.CompareTag("Enemy"))
                    {
                        OnHit(col, transform.position);
                        return;
                    }
                }
            }
            else
            {
                // Follow arc path
                Vector3 targetPos = _currentTarget.position;
                Vector3 flatPos = Vector3.Lerp(_arcStart, targetPos, _arcProgress);
                
                // Parabolic arc height
                float arcY = Mathf.Sin(_arcProgress * Mathf.PI) * _arcHeight;
                transform.position = new Vector3(flatPos.x, flatPos.y + arcY, flatPos.z);
                
                // Face movement direction
                Vector3 nextFlatPos = Vector3.Lerp(_arcStart, targetPos, Mathf.Min(_arcProgress + 0.1f, 1f));
                float nextArcY = Mathf.Sin(Mathf.Min(_arcProgress + 0.1f, 1f) * Mathf.PI) * _arcHeight;
                Vector3 nextPos = new Vector3(nextFlatPos.x, nextFlatPos.y + nextArcY, nextFlatPos.z);
                Vector3 dir = (nextPos - transform.position).normalized;
                if (dir != Vector3.zero) transform.forward = dir;
            }
            return;
        }

        // Normal straight-line movement
        float step = _speed * Time.deltaTime;
        Vector3 nextPosition = transform.position + transform.forward * step;

        // Raycast to detect hit
        if (Physics.Raycast(transform.position, transform.forward, out var hit, step))
        {
            OnHit(hit.collider, hit.point);
            return;
        }

        transform.position = nextPosition;

        // Check max distance
        if (Vector3.Distance(_startPos, transform.position) >= _maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnHit(Collider col, Vector3 hitPoint)
    {
        if (col == null) return;
        if (col.CompareTag("Player")) return;
        if (col.CompareTag("Ally"))
        {
            // Pass through allies without destroying (needs to bounce to enemies)
            transform.position = hitPoint + transform.forward * 0.1f;
            return;
        }
        if (!col.CompareTag("Enemy"))
        {
            // Pass through non-enemies
            transform.position = hitPoint + transform.forward * 0.1f;
            return;
        }

        GameObject target = ModifierUtils.ResolveTarget(col);
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Skip if already hit this target
        if (_hitTargets.Contains(target))
        {
            // Pass through, don't destroy
            transform.position = hitPoint + transform.forward * 0.1f;
            return;
        }

        // First target: base damage only
        // Subsequent targets: base + accumulated (3% missing HP from previous targets, capped per target) + 3% of current target's missing HP
        float damage = _baseDamage;
        var hp = target.GetComponent<HealthSystem>();
        if (hp != null)
        {
            bool isFirstHit = (_hitTargets.Count == 0);
            
            if (isFirstHit)
            {
                // First hit: just base damage
                damage = _baseDamage;
                hp.TakeDamage(damage);
                Debug.Log($"Jhin bounce bullet hit {target.name} (FIRST HIT) for {damage:F1} damage");
            }
            else
            {
                // Subsequent hits: base + accumulated from previous + current target's 3% missing HP
                float missingHealth = hp.maxHealth - hp.GetCurrentHealth();
                float currentBonus = missingHealth * _missingHealthBonus;
                
                // Cap current target bonus at 30 damage
                currentBonus = Mathf.Min(currentBonus, 30f);
                
                // Cap total accumulated bonus at 60 damage
                float cappedAccumulated = Mathf.Min(_accumulatedBonus, 60f);
                
                damage = _baseDamage + cappedAccumulated + currentBonus;
                hp.TakeDamage(damage);
                Debug.Log($"Jhin bounce bullet hit {target.name} for {damage:F1} damage (base: {_baseDamage}, accumulated: {cappedAccumulated:F1}/{_accumulatedBonus:F1}, current 3% missing: {currentBonus:F1})");
                
                // Accumulate this target's bonus for next bounce (capped at 30 per target)
                _accumulatedBonus += currentBonus;
            }
        }

        _hitTargets.Add(target);

        // Record hit for RMB marking
        if (_owner != null)
        {
            _owner.RecordHit(col);
        }

        _bouncesRemaining--;

        // Try to bounce to next target
        if (_bouncesRemaining > 0)
        {
            Transform nextTarget = FindNextTarget(hitPoint);
            if (nextTarget != null)
            {
                _currentTarget = nextTarget;
                
                // Start arc movement to next target
                _arcStart = hitPoint;
                _arcEnd = nextTarget.position;
                _arcProgress = 0f;
                _isArcing = true;
                
                // Calculate arc duration based on distance and arc speed
                float dist = Vector3.Distance(_arcStart, _arcEnd);
                _arcDuration = dist / _arcSpeed;
                _arcHeight = Mathf.Clamp(dist * 0.3f, 1f, 4f); // Arc height proportional to distance
                
                transform.position = hitPoint;
                _startPos = transform.position;
                Debug.Log($"Bounce bullet arcing to {nextTarget.name}. {_bouncesRemaining} bounces remaining.");
                return;
            }
        }

        Destroy(gameObject);
    }

    Transform FindNextTarget(Vector3 fromPos)
    {
        Collider[] nearby = Physics.OverlapSphere(fromPos, _bounceRadius);
        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in nearby)
        {
            if (!col.CompareTag("Enemy")) continue;

            GameObject target = ModifierUtils.ResolveTarget(col);
            if (target == null || _hitTargets.Contains(target)) continue;

            float dist = Vector3.Distance(fromPos, target.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = target.transform;
            }
        }

        return closest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _bounceRadius);
    }
}
