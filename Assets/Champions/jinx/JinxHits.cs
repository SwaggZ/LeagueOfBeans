using System.Collections.Generic;
using UnityEngine;

/// <summary>Hit detection and area damage shared by Jinx's projectiles.</summary>
public static class JinxHits
{
    private static readonly RaycastHit[] _buffer = new RaycastHit[16];

    /// <summary>
    /// Nearest solid hit along the ray, ignoring Jinx herself, other players and trigger volumes.
    /// </summary>
    public static bool Cast(JinxController owner, Vector3 origin, Vector3 dir, float distance, out RaycastHit best, float radius = 0f)
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
            if (owner != null && owner.IsSelf(c)) continue;
            if (c.CompareTag("Player")) continue;
            if (c.isTrigger && !c.CompareTag("Enemy")) continue; // pass through trigger volumes (mines, pickups)
            if (c.GetComponentInParent<JinxMine>() != null) continue;
            if (h.distance < bestDist) { bestDist = h.distance; best = h; }
        }
        if (bestDist == 0f) best.point = origin; // already overlapping at the start of the cast
        return bestDist < float.MaxValue;
    }

    /// <summary>
    /// Damages every enemy within radius. The direct target takes full damage, others take
    /// damage * splashMultiplier. Returns how many enemies were hit.
    /// </summary>
    public static int Explode(JinxController owner, Vector3 point, float radius, float damage, GameObject direct,
                              float splashMultiplier, Color color, System.Func<GameObject, float> extraDamage = null)
    {
        var hit = new HashSet<GameObject>();
        if (direct != null)
        {
            hit.Add(direct);
            float d = damage + (extraDamage != null ? extraDamage(direct) : 0f);
            if (owner != null) owner.DealDamage(direct, d);
        }

        foreach (var c in Physics.OverlapSphere(point, radius, ~0, QueryTriggerInteraction.Collide))
        {
            if (!c.CompareTag("Enemy")) continue;
            var target = ModifierUtils.ResolveTarget(c);
            if (target == null || !hit.Add(target)) continue;
            float d = damage * splashMultiplier + (extraDamage != null ? extraDamage(target) * splashMultiplier : 0f);
            if (owner != null) owner.DealDamage(target, d);
        }

        JinxVisuals.Blast(point, radius, color);
        return hit.Count;
    }
}
