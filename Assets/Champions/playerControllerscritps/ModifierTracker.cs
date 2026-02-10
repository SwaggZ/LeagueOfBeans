using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Tracks active modifiers on an entity (player or enemy).
/// Used by HealthBarUI to display modifier icons above health bars.
/// </summary>
[DisallowMultipleComponent]
public class ModifierTracker : MonoBehaviour
{
    private class Modifier
    {
        public string id;
        public Sprite sprite;
        public float endTime; // -1 = infinite
        public int stacks;
    }

    private List<Modifier> _modifiers = new List<Modifier>();

    /// <summary>
    /// Add or update a modifier on this entity
    /// </summary>
    public void AddOrUpdate(string id, Sprite sprite, float durationSeconds = -1f, int stacks = 0)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (sprite == null && ModifiersIconLibrary.Instance != null)
        {
            sprite = ModifiersIconLibrary.Instance.Resolve(id, string.Empty);
        }

        // Find existing modifier with this ID
        var existing = _modifiers.Find(m => m.id == id);
        if (existing != null)
        {
            existing.sprite = sprite;
            existing.endTime = durationSeconds >= 0f ? Time.time + durationSeconds : -1f;
            existing.stacks = stacks;
        }
        else
        {
            _modifiers.Add(new Modifier
            {
                id = id,
                sprite = sprite,
                endTime = durationSeconds >= 0f ? Time.time + durationSeconds : -1f,
                stacks = stacks
            });
        }
    }

    /// <summary>
    /// Remove a modifier by ID
    /// </summary>
    public void Remove(string id)
    {
        _modifiers.RemoveAll(m => m.id == id);
    }

    /// <summary>
    /// Get all active modifiers (auto-removes expired ones)
    /// </summary>
    public List<(Sprite sprite, float remainingTime, int stacks)> GetActiveModifiers()
    {
        float now = Time.time;
        
        // Remove expired modifiers
        _modifiers.RemoveAll(m => m.endTime >= 0f && m.endTime <= now);

        var result = new List<(Sprite, float, int)>();
        foreach (var mod in _modifiers)
        {
            float remaining = mod.endTime >= 0f ? (mod.endTime - now) : -1f;
            result.Add((mod.sprite, remaining, mod.stacks));
        }
        return result;
    }

    /// <summary>
    /// Clear all modifiers
    /// </summary>
    public void Clear()
    {
        _modifiers.Clear();
    }
}
