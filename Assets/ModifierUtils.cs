using UnityEngine;

// Shared utilities for applying and removing modifiers consistently.
public static class ModifierUtils
{
    public static GameObject ResolveTarget(Collider col)
    {
        if (col == null) return null;
        return col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
    }

    public static ModifierTracker GetTracker(GameObject target)
    {
        if (target == null) return null;
        var tracker = target.GetComponent<ModifierTracker>();
        if (tracker == null) tracker = target.GetComponentInParent<ModifierTracker>();
        return tracker;
    }

    public static void ApplyModifier(GameObject target, string id, Sprite icon, string label,
        float durationSeconds, int stacks = 0, bool includePlayerHud = true, bool includeEnemy = true)
    {
        if (target == null || string.IsNullOrEmpty(id)) return;

        Sprite resolvedIcon = icon;
        if (resolvedIcon == null && ModifiersIconLibrary.Instance != null)
        {
            resolvedIcon = ModifiersIconLibrary.Instance.Resolve(id, label);
        }

        if (includePlayerHud && target.CompareTag("Player") && ModifiersUIManager.Instance != null)
        {
            ModifiersUIManager.Instance.AddOrUpdate(id, resolvedIcon, label, durationSeconds, stacks);
        }

        if (includeEnemy && !target.CompareTag("Player"))
        {
            var tracker = GetTracker(target);
            if (tracker != null)
            {
                tracker.AddOrUpdate(id, resolvedIcon, durationSeconds, stacks);
            }
        }
    }

    public static void RemoveModifier(GameObject target, string id, bool removePlayerHud = true, bool removeEnemy = true)
    {
        if (target == null || string.IsNullOrEmpty(id)) return;

        if (removePlayerHud && target.CompareTag("Player") && ModifiersUIManager.Instance != null)
        {
            ModifiersUIManager.Instance.Remove(id);
        }

        if (removeEnemy && !target.CompareTag("Player"))
        {
            var tracker = GetTracker(target);
            if (tracker != null)
            {
                tracker.Remove(id);
            }
        }
    }
}
