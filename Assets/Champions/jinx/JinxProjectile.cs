using UnityEngine;

/// <summary>
/// Straight-flying Jinx shot. With splashRadius 0 it is a minigun bullet (single target);
/// with a splash radius it is a rocket that explodes on any impact or at max range.
/// </summary>
public class JinxProjectile : MonoBehaviour
{
    private JinxController _owner;
    private float _damage, _speed, _maxDistance, _splashRadius;
    private float _travelled;
    private bool _done;

    public void Init(JinxController owner, float damage, float speed, float maxDistance, float splashRadius)
    {
        _owner = owner;
        _damage = damage;
        _speed = speed;
        _maxDistance = maxDistance;
        _splashRadius = splashRadius;
    }

    void Update()
    {
        if (_done) return;
        float step = _speed * Time.deltaTime;

        if (JinxHits.Cast(_owner, transform.position, transform.forward, step, out var hit))
        {
            transform.position = hit.point;
            Impact(hit.collider);
            return;
        }

        transform.position += transform.forward * step;
        _travelled += step;
        if (_travelled >= _maxDistance)
        {
            if (_splashRadius > 0f) JinxHits.Explode(_owner, transform.position, _splashRadius, _damage, null, 1f, new Color(0.3f, 0.75f, 1f));
            Finish();
        }
    }

    private void Impact(Collider col)
    {
        GameObject direct = col.CompareTag("Enemy") ? ModifierUtils.ResolveTarget(col) : null;

        if (_splashRadius > 0f)
        {
            JinxHits.Explode(_owner, transform.position, _splashRadius, _damage, direct, 1f, new Color(0.3f, 0.75f, 1f));
        }
        else if (direct != null && _owner != null)
        {
            _owner.DealDamage(direct, _damage);
        }
        Finish();
    }

    private void Finish()
    {
        _done = true;
        NetworkHelper.Despawn(gameObject);
    }
}
