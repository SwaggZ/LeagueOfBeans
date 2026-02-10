using UnityEngine;

public class JhinProjectile : MonoBehaviour
{
    private JhinController _owner;
    private float _damage;
    private float _speed;
    private float _maxDistance;
    private bool _isEmpowered;
    private bool _isSpecial;
    private float _stunDuration;
    private Vector3 _startPos;

    public void Init(JhinController owner, float damage, float speed, float maxDistance, bool empowered, bool isSpecial = false, float stunDuration = 0f)
    {
        _owner = owner;
        _damage = damage;
        _speed = speed;
        _maxDistance = maxDistance;
        _isEmpowered = empowered;
        _isSpecial = isSpecial;
        _stunDuration = stunDuration;
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
            return;
        }

        transform.position = nextPos;

        if (Vector3.Distance(_startPos, transform.position) >= _maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnHit(Collider col, Vector3 hitPoint)
    {
        if (col == null) return;

        GameObject target = ModifierUtils.ResolveTarget(col);

        // Apply damage
        var hp = target.GetComponent<HealthSystem>();
        if (hp != null)
        {
            hp.TakeDamage(_damage);
            Debug.Log($"Jhin bullet hit {target.name} for {_damage} damage. Empowered: {_isEmpowered}");
        }

        // If special bullet, stun if enemy is marked
        if (_isSpecial)
        {
            var marker = target.GetComponent<JhinMarkStatus>();
            if (marker != null)
            {
                marker.StunFromMark(_stunDuration);
                Debug.Log($"Jhin RMB stunned marked enemy {target.name}!");
            }
        }

        // Record hit for marking
        if (_owner != null)
        {
            _owner.RecordHit(col);
        }

        Destroy(gameObject);
    }
}
