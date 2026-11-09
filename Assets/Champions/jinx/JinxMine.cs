using UnityEngine;

/// <summary>
/// Thrown stun mine. Lands, arms after a short delay, then snaps shut on the first enemy that
/// steps on it: damage + stun. Expires after its lifetime.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class JinxMine : MonoBehaviour
{
    public float armDelay = 0.5f;
    public float triggerRadius = 1.3f;

    private JinxController _owner;
    private float _damage, _stunDuration;
    private Rigidbody _rb;
    private bool _landed, _armed, _sprung;
    private float _landTime;
    private float _spawnTime;
    private Renderer[] _renderers;

    public void Init(JinxController owner, Vector3 velocity, float damage, float stunDuration, float lifetime)
    {
        _owner = owner;
        _damage = damage;
        _stunDuration = stunDuration;
        _spawnTime = Time.time;
        _rb = GetComponent<Rigidbody>();
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.linearVelocity = velocity;
        _renderers = GetComponentsInChildren<Renderer>();

        // Never collide with the thrower or with the other mines from the same throw.
        if (owner != null)
        {
            var mine = GetComponentsInChildren<Collider>();
            foreach (var oc in owner.GetComponentsInChildren<Collider>())
                foreach (var mc in mine) Physics.IgnoreCollision(oc, mc);
        }
        foreach (var other in FindObjectsByType<JinxMine>(FindObjectsSortMode.None))
        {
            if (other == this) continue;
            foreach (var a in GetComponentsInChildren<Collider>())
                foreach (var b in other.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(a, b);
        }

        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_landed) return;
        if (collision.collider.CompareTag("Enemy")) return; // keep falling; it arms once on the floor
        // Land on anything mostly horizontal (floor / top of a box).
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f) { Land(); return; }
        }
    }

    private void Land()
    {
        _landed = true;
        _landTime = Time.time;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (_sprung) return;

        // Fallback: if it has come to rest without a clean floor contact, treat it as landed.
        if (!_landed && _rb != null && !_rb.isKinematic && _rb.linearVelocity.sqrMagnitude < 0.01f && Time.time > _spawnTime + 0.3f)
            Land();

        if (_landed && !_armed && Time.time >= _landTime + armDelay)
        {
            _armed = true;
            Tint(new Color(1f, 0.25f, 0.55f)); // armed: bright pink
        }

        if (!_armed) return;
        foreach (var c in Physics.OverlapSphere(transform.position, triggerRadius, ~0, QueryTriggerInteraction.Collide))
        {
            if (!c.CompareTag("Enemy")) continue;
            var target = ModifierUtils.ResolveTarget(c);
            if (target == null) continue;
            Spring(target);
            return;
        }
    }

    private void Spring(GameObject target)
    {
        _sprung = true;
        if (_owner != null) _owner.DealDamage(target, _damage);

        var dc = target.GetComponentInParent<DummyController>();
        if (dc != null) dc.Stun(_stunDuration);
        else
        {
            var cc = target.GetComponentInParent<CharacterControl>();
            if (cc != null) cc.Stun(_stunDuration);
        }

        JinxVisuals.Blast(transform.position, 1.2f, new Color(1f, 0.3f, 0.6f));
        Destroy(gameObject);
    }

    private void Tint(Color c)
    {
        if (_renderers == null) return;
        foreach (var r in _renderers)
            if (r != null && r.material != null) r.material.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
