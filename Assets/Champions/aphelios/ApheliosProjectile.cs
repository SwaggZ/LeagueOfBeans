using UnityEngine;

public class ApheliosProjectile : MonoBehaviour
{
    private ApheliosController _owner;
    private float _damage;
    private float _speed;
    private float _maxDistance;
    private float _lifesteal;
    private float _slowPct;
    private float _slowDur;
    private Vector3 _startPos;
    private float _burnDur;
    private float _burnDps;

    public void Init(ApheliosController owner, float damage, float speed, float maxDistance, float lifestealPercent, float slowPercent, float slowDuration, float burnDuration = 0f, float burnDps = 2f)
    {
        _owner = owner; _damage = damage; _speed = speed; _maxDistance = maxDistance; _lifesteal = lifestealPercent; _slowPct = slowPercent; _slowDur = slowDuration; _burnDur = burnDuration; _burnDps = burnDps;
        _startPos = transform.position;
    }

    void Update()
    {
        float step = _speed * Time.deltaTime;
        // move
        Vector3 nextPos = transform.position + transform.forward * step;
        // raycast to detect hit between positions
        if (Physics.Raycast(transform.position, transform.forward, out var hit, step))
        {
            OnHit(hit.collider);
            Destroy(gameObject);
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
        var hp = col.GetComponent<HealthSystem>();
        if (hp != null)
        {
            hp.TakeDamage(_damage);
            // lifesteal heals the shooter if he has HealthSystem
            if (_lifesteal > 0f && _owner != null)
            {
                var selfHp = _owner.GetComponent<HealthSystem>();
                if (selfHp != null) selfHp.Heal(_damage * _lifesteal);
            }
        }

        // Apply slow to CharacterControl via a helper
        if (_slowPct > 0f && _slowDur > 0f)
        {
            var slow = col.GetComponentInParent<SlowStatus>();
            if (slow == null && col.attachedRigidbody != null)
            {
                slow = col.attachedRigidbody.GetComponent<SlowStatus>();
            }
            if (slow == null)
            {
                slow = col.gameObject.AddComponent<SlowStatus>();
            }
            slow.Apply(_slowPct, _slowDur);
            // Do NOT push enemy status effects to the player's Modifiers HUD
        }

        // Burn (non-stackable refresh)
        if (_burnDur > 0f && _burnDps > 0f)
        {
            var burn = col.GetComponentInParent<BurnStatus>();
            if (burn == null && col.attachedRigidbody != null)
            {
                burn = col.attachedRigidbody.GetComponent<BurnStatus>();
            }
            if (burn == null)
            {
                burn = col.gameObject.AddComponent<BurnStatus>();
            }
            burn.Apply(_burnDur, _burnDps);
        }
    }
}
