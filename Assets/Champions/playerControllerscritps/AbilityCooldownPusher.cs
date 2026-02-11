using System;
using UnityEngine;

// Drop this on a character prefab to push cooldowns to the HUD
// without modifying existing ability scripts. It can:
// - Listen for input (LMB, RMB, Q, E) and start cooldowns with configured durations
// - Be triggered manually via Trigger(AbilityKey key, float duration) from Animation Events or other scripts
[DisallowMultipleComponent]
public class AbilityCooldownPusher : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public AbilityKey key;
        public float durationSeconds = 3f;
        public bool useInput = true; // If true, auto-detect input for this key (LMB/RMB/Q/E only)
    }

    [Header("Configure per-ability cooldowns")]
    public Entry[] abilities = new Entry[]
    {
        new Entry { key = AbilityKey.LeftClick, durationSeconds = 1f, useInput = true },
        new Entry { key = AbilityKey.RightClick, durationSeconds = 3f, useInput = true },
        new Entry { key = AbilityKey.One, durationSeconds = 5f, useInput = true }, // Q
        new Entry { key = AbilityKey.Two, durationSeconds = 12f, useInput = true }, // E
        new Entry { key = AbilityKey.Ctrl, durationSeconds = 8f, useInput = false }, // usually triggered explicitly when dash is used
    };

    private CooldownUIManager _cooldownUi;

    private void Awake()
    {
        _cooldownUi = FindObjectOfType<CooldownUIManager>(true);
    }

    void Update()
    {
        if (_cooldownUi == null)
        {
            _cooldownUi = FindObjectOfType<CooldownUIManager>(true);
            if (_cooldownUi == null) return;
        }
        if (abilities == null) return;

        foreach (var e in abilities)
        {
            if (e == null || !e.useInput) continue;

            // Only auto-handle LMB/RMB/Q/E to avoid false triggers on CTRL
            switch (e.key)
            {
                case AbilityKey.LeftClick:
                    if (Input.GetMouseButtonDown(0)) StartCooldownSafe(AbilityKey.LeftClick, e.durationSeconds);
                    break;
                case AbilityKey.RightClick:
                    if (Input.GetMouseButtonDown(1)) StartCooldownSafe(AbilityKey.RightClick, e.durationSeconds);
                    break;
                case AbilityKey.One:
                    if (Input.GetKeyDown(KeyCode.Q)) StartCooldownSafe(AbilityKey.One, e.durationSeconds);
                    break;
                case AbilityKey.Two:
                    if (Input.GetKeyDown(KeyCode.E)) StartCooldownSafe(AbilityKey.Two, e.durationSeconds);
                    break;
            }
        }
    }

    // Call this from animation events or other scripts when an ability is confirmed/consumed
    public void Trigger(AbilityKey key, float durationSeconds)
    {
        StartCooldownSafe(key, durationSeconds);
    }

    private void StartCooldownSafe(AbilityKey key, float durationSeconds)
    {
        if (_cooldownUi == null) return;
        if (durationSeconds <= 0f) return;
        _cooldownUi.StartCooldown(key, durationSeconds);
    }
}
