using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared hit detection and effects for champion abilities: casts that ignore the caster,
/// damage/heal, stun, and pulls that work on both training dummies and player characters.
/// </summary>
public static class AbilityHits
{
    private static readonly RaycastHit[] _buffer = new RaycastHit[32];

    /// <summary>Nearest solid hit along a ray/sphere cast, ignoring the caster, players and trigger volumes.</summary>
    public static bool Cast(Transform caster, Vector3 origin, Vector3 dir, float distance, out RaycastHit best, float radius = 0f, bool skipEnemies = false)
    {
        int n = radius > 0f
            ? Physics.SphereCastNonAlloc(origin, radius, dir, _buffer, distance, ~0, QueryTriggerInteraction.Collide)
            : Physics.RaycastNonAlloc(origin, dir, _buffer, distance, ~0, QueryTriggerInteraction.Collide);

        best = default;
        float bestDist = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var h = _buffer[i];
            var c = h.collider;
            if (c == null) continue;
            if (caster != null && c.transform.IsChildOf(caster)) continue;
            if (c.CompareTag("Player")) continue;
            if (c.isTrigger && !c.CompareTag("Enemy")) continue;
            if (skipEnemies && c.CompareTag("Enemy")) continue;
            if (h.distance < bestDist) { bestDist = h.distance; best = h; }
        }
        if (bestDist == 0f) best.point = origin;
        return bestDist < float.MaxValue;
    }

    /// <summary>True if no wall/obstacle blocks the line from 'from' to the target (other enemies don't block).</summary>
    public static bool HasLineOfSight(Transform caster, Vector3 from, GameObject target)
    {
        var col = target.GetComponentInChildren<Collider>();
        Vector3 to = col != null ? col.bounds.center : target.transform.position;
        Vector3 d = to - from;
        if (!Cast(caster, from, d.normalized, d.magnitude, out var hit, 0f, skipEnemies: true)) return true;
        return hit.collider.transform.IsChildOf(target.transform);
    }

    /// <summary>All distinct enemy roots within radius of a point.</summary>
    public static List<GameObject> EnemiesInRadius(Vector3 point, float radius)
    {
        var result = new List<GameObject>();
        var seen = new HashSet<GameObject>();
        foreach (var c in Physics.OverlapSphere(point, radius, ~0, QueryTriggerInteraction.Collide))
        {
            if (!c.CompareTag("Enemy")) continue;
            var t = ModifierUtils.ResolveTarget(c);
            if (t != null && seen.Add(t)) result.Add(t);
        }
        return result;
    }

    public static HealthSystem Health(GameObject target) => target != null ? target.GetComponentInParent<HealthSystem>() : null;

    /// <summary>Damages the target; returns the health it actually lost (capped at what it had).</summary>
    public static float Damage(GameObject target, float amount)
    {
        var hp = Health(target);
        if (hp == null || amount <= 0f) return 0f;
        float before = Mathf.Max(0f, hp.GetCurrentHealth());
        hp.TakeDamage(amount);
        return Mathf.Clamp(before - Mathf.Max(0f, hp.GetCurrentHealth()), 0f, before);
    }

    public static void Heal(GameObject self, float amount)
    {
        var hp = Health(self);
        if (hp != null && amount > 0f) hp.Heal(amount);
    }

    public static void Stun(GameObject target, float duration)
    {
        var dc = target.GetComponentInParent<DummyController>();
        if (dc != null) { dc.Stun(duration); return; }
        var cc = target.GetComponentInParent<CharacterControl>();
        if (cc != null) cc.Stun(duration);
    }

    /// <summary>Drags the target horizontally toward 'toward', stopping 'stopDistance' short of it. Clears any stun first.</summary>
    public static void Pull(GameObject target, Vector3 toward, float stopDistance, float speed)
    {
        Vector3 d = toward - target.transform.position;
        d.y = 0f;
        float distance = d.magnitude - stopDistance;
        if (distance <= 0.05f) return;

        var dc = target.GetComponentInParent<DummyController>();
        if (dc != null)
        {
            if (dc.IsStunned()) dc.Stun(0.001f); // stunned targets ignore movement; end the stun so the pull plays
            dc.ApplyKnockback(d.normalized, distance, speed, 0f);
            return;
        }
        var cc = target.GetComponentInParent<CharacterControl>();
        if (cc != null)
        {
            cc.Stun(0.001f);
            cc.ApplyKnockback(d.normalized, distance, speed, 0f);
        }
    }
}
