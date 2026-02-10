using UnityEngine;

public class ApheliosProjectile : MonoBehaviour
{
    private ApheliosController _owner;
    private ApheliosController.WeaponType _firedWeapon;
    private float _damage;
    private float _speed;
    private float _maxDistance;
    private float _lifesteal;
    private float _slowPct;
    private float _slowDur;
    private Vector3 _startPos;
    private float _burnDur;
    private float _burnDps;
    private bool _hitTwice;
    private int _bouncesRemaining;
    private float _bounceRadius;

    public void Init(ApheliosController owner,
                     ApheliosController.WeaponType firedWeapon,
                     float damage, float speed, float maxDistance,
                     float lifestealPercent, float slowPercent, float slowDuration,
                     float burnDuration = 0f, float burnDps = 2f,
                     bool hitTwice = false, int bounces = 0, float bounceRadius = 0f)
    {
        _owner = owner; _firedWeapon = firedWeapon; _damage = damage; _speed = speed; _maxDistance = maxDistance; _lifesteal = lifestealPercent; _slowPct = slowPercent; _slowDur = slowDuration; _burnDur = burnDuration; _burnDps = burnDps; _hitTwice = hitTwice; _bouncesRemaining = bounces; _bounceRadius = bounceRadius;
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
        var hp = col.GetComponent<HealthSystem>();
        // Damage application: single or double hit for empowered sniper
        int times = _hitTwice ? 2 : 1;
        for (int i = 0; i < times; i++)
        {
            if (hp != null)
            {
                hp.TakeDamage(_damage);
                if (_lifesteal > 0f && _owner != null)
                {
                    var selfHp = _owner.GetComponent<HealthSystem>();
                    if (selfHp != null) selfHp.Heal(_damage * _lifesteal);
                }
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

        // Orbs: apply marker status and track hit for ability stun
        if (_owner != null && _firedWeapon == ApheliosController.WeaponType.Orbs)
        {
            _owner.RecordOrbsHit(col);
            
            // Apply visual orb marker and weak slow - prefer adding to rigidbody's gameObject (root enemy)
            GameObject targetObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
            
            var orbMarker = targetObj.GetComponent<OrbsMarkerStatus>();
            if (orbMarker == null)
            {
                orbMarker = targetObj.AddComponent<OrbsMarkerStatus>();
            }
            orbMarker.Apply(_owner.orbsStunWindow);
        }

        // Flamethrower (Ability): reflect one shot away from the player (no further bounces)
        // Flamethrower ability: enemy hit emits a second wave continuing AWAY in the hit direction.
        // The second wave should NOT emit another wave.
        if (_firedWeapon == ApheliosController.WeaponType.Flamethrower && _bouncesRemaining > 0)
        {
            // "Hit direction" = the direction this projectile was moving when it hit
            Vector3 waveDir = transform.forward;
            if (waveDir.sqrMagnitude < 0.0001f) waveDir = (hitPoint - transform.position).normalized;

            Vector3 spawnPos = hitPoint + waveDir * 0.15f; // nudge forward so it doesn't instantly re-hit the same collider

            GameObject go = Instantiate(gameObject, spawnPos, Quaternion.LookRotation(waveDir));

            // Prevent immediate re-hit on the same enemy collider (if both have colliders)
            var newCol = go.GetComponent<Collider>();
            if (newCol != null && col != null)
                Physics.IgnoreCollision(newCol, col, true);

            var proj = go.GetComponent<ApheliosProjectile>();
            if (proj == null) proj = go.AddComponent<ApheliosProjectile>();

            // Second wave: bounces = 0 so it won't create another wave
            proj.Init(_owner, _firedWeapon,
                      _damage, _speed, _maxDistance,
                      _lifesteal, _slowPct, _slowDur,
                      _burnDur, _burnDps,
                      hitTwice: false,
                      bounces: 0,
                      bounceRadius: 0f);
        }

        Destroy(gameObject);
    }

    private Vector3 GetColliderCenter(Collider c)
    {
        if (c == null) return transform.position;
        return c.bounds.center;
    }
}
