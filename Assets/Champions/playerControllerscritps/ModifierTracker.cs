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
    }

    private List<Modifier> _modifiers = new List<Modifier>();

    /// <summary>
    /// Add or update a modifier on this entity
    /// </summary>
    public void AddOrUpdate(string id, Sprite sprite, float durationSeconds = -1f)
    {
        if (string.IsNullOrEmpty(id)) return;

        // Find existing modifier with this ID
        var existing = _modifiers.Find(m => m.id == id);
        if (existing != null)
        {
            existing.sprite = sprite;
            existing.endTime = durationSeconds >= 0f ? Time.time + durationSeconds : -1f;
        }
        else
        {
            _modifiers.Add(new Modifier
            {
                id = id,
                sprite = sprite,
                endTime = durationSeconds >= 0f ? Time.time + durationSeconds : -1f
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
    public List<(Sprite sprite, float remainingTime)> GetActiveModifiers()
    {
        float now = Time.time;
        
        // Remove expired modifiers
        _modifiers.RemoveAll(m => m.endTime >= 0f && m.endTime <= now);

        var result = new List<(Sprite, float)>();
        foreach (var mod in _modifiers)
        {
            float remaining = mod.endTime >= 0f ? (mod.endTime - now) : -1f;
            result.Add((mod.sprite, remaining));
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
