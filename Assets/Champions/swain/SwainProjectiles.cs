using UnityEngine;

/// <summary>RMB hand: flies straight; reports the first enemy hit (or a miss) back to Swain.</summary>
public class SwainHand : MonoBehaviour
{
    public float hitRadius = 0.45f;
    private SwainController _owner;
    private float _speed, _range, _travelled;
    private bool _done;

    public void Init(SwainController owner, float speed, float range)
    {
        _owner = owner; _speed = speed; _range = range;
    }

    void Update()
    {
        if (_done) return;
        if (_owner == null) { Destroy(gameObject); return; }
        float step = _speed * Time.deltaTime;

        if (AbilityHits.Cast(_owner.transform, transform.position, transform.forward, step, out var hit, hitRadius))
        {
            transform.position += transform.forward * hit.distance;
            _done = true;
            if (hit.collider.CompareTag("Enemy")) _owner.OnHandHit(ModifierUtils.ResolveTarget(hit.collider));
            else _owner.OnHandMissed();
            SwainVisuals.Burst(transform.position, 0.8f, SwainVisuals.Crimson);
            Destroy(gameObject);
            return;
        }

        transform.position += transform.forward * step;
        _travelled += step;
        if (_travelled >= _range)
        {
            _done = true;
            _owner.OnHandMissed();
            Destroy(gameObject);
        }
    }
}

/// <summary>Q eye: flies straight and bursts on the first thing it hits, or at max range.</summary>
public class SwainEye : MonoBehaviour
{
    public float hitRadius = 0.35f;
    private SwainController _owner;
    private float _travelled;
    private bool _done;

    public void Init(SwainController owner) => _owner = owner;

    void Update()
    {
        if (_done) return;
        if (_owner == null) { Destroy(gameObject); return; }
        float step = _owner.eyeSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, 360f * Time.deltaTime, Space.Self); // slow spin

        if (AbilityHits.Cast(_owner.transform, transform.position, transform.forward, step, out var hit, hitRadius))
        {
            transform.position += transform.forward * hit.distance;
            Burst();
            return;
        }

        transform.position += transform.forward * step;
        _travelled += step;
        if (_travelled >= _owner.eyeRange) Burst();
    }

    private void Burst()
    {
        _done = true;
        _owner.EyeBurst(transform.position);
        Destroy(gameObject);
    }
}
