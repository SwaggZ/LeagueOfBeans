using UnityEngine;

/// <summary>
/// Jinx's giant rocket. Accelerates as it flies; explodes on the first enemy or wall it hits.
/// Damage grows with distance travelled, plus a share of each target's missing health.
/// </summary>
public class JinxMegaRocket : MonoBehaviour
{
    public float hitRadius = 0.6f;

    private JinxController _owner;
    private float _speed, _travelled;
    private bool _done;

    public void Init(JinxController owner)
    {
        _owner = owner;
        _speed = owner != null ? owner.megaStartSpeed : 25f;
    }

    void Update()
    {
        if (_done || _owner == null) { if (!_done) Destroy(gameObject); return; }

        _speed = Mathf.Min(_owner.megaMaxSpeed, _speed + _owner.megaAcceleration * Time.deltaTime);
        float step = _speed * Time.deltaTime;

        if (JinxHits.Cast(_owner, transform.position, transform.forward, step, out var hit, hitRadius))
        {
            _travelled += hit.distance;
            transform.position += transform.forward * hit.distance;
            Detonate(hit.collider.CompareTag("Enemy") ? ModifierUtils.ResolveTarget(hit.collider) : null);
            return;
        }

        transform.position += transform.forward * step;
        _travelled += step;
        if (_travelled >= _owner.megaRange) Detonate(null);
    }

    private void Detonate(GameObject direct)
    {
        _done = true;
        var o = _owner;
        float distanceT = Mathf.Clamp01(_travelled / Mathf.Max(1f, o.megaFullDamageDistance));
        float damage = o.megaBaseDamage + o.megaDistanceBonus * distanceT;

        int hits = JinxHits.Explode(o, transform.position, o.megaSplashRadius, damage, direct,
            o.megaSplashMultiplier, new Color(1f, 0.35f, 0.7f), MissingHealthBonus);
        Debug.Log($"Jinx mega rocket: {damage:0} base dmg after {_travelled:0}m, hit {hits} enemies.");
        NetworkHelper.Despawn(gameObject);
    }

    private float MissingHealthBonus(GameObject target)
    {
        var hp = target.GetComponentInParent<HealthSystem>();
        if (hp == null) return 0f;
        float missing = Mathf.Max(0f, hp.maxHealth - hp.GetCurrentHealth());
        return missing * _owner.megaMissingHealthRatio;
    }
}
