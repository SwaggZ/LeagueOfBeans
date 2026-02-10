using System.Collections.Generic;
using UnityEngine;

// Wand travels forward and back (like Ahri's orb), allies hit gain decaying shield
public class LuxWand : MonoBehaviour
{
    private LuxController _owner;
    private Transform _player;
    private Transform _cam;
    private float _shieldAmount;
    private float _shieldDuration;
    private float _speed;
    private float _returnSpeed;
    private float _maxDistance;

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private bool _isReturning = false;
    private HashSet<GameObject> _hitForward = new HashSet<GameObject>();
    private HashSet<GameObject> _hitReturn = new HashSet<GameObject>();
    private float _shieldCooldown = 0.4f;
    private Dictionary<GameObject, float> _shieldCooldownTimers = new Dictionary<GameObject, float>();

    public void Init(LuxController owner, Transform player, Transform cam, float shieldAmount, float shieldDuration, float speed, float returnSpeed, float maxDistance)
    {
        _owner = owner;
        _player = player;
        _cam = cam;
        _shieldAmount = shieldAmount;
        _shieldDuration = shieldDuration;
        _speed = speed;
        _returnSpeed = returnSpeed;
        _maxDistance = maxDistance;

        _startPos = transform.position;
        _targetPos = _startPos + cam.forward * maxDistance;
    }

    void Update()
    {
        // Raycast for ally detection
        CheckForAllies();

        if (!_isReturning)
        {
            // Move towards target position
            transform.position = Vector3.MoveTowards(transform.position, _targetPos, _speed * Time.deltaTime);

            // Check if reached target
            if (Vector3.Distance(transform.position, _targetPos) < 0.1f)
            {
                _isReturning = true;
                if (_owner != null) _owner.OnWandReturning();
            }
        }
        else
        {
            // Return to player
            if (_player != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, _player.position, _returnSpeed * Time.deltaTime);

                // Destroy when reaching player
                if (Vector3.Distance(transform.position, _player.position) < 0.5f)
                {
                    if (_owner != null) _owner.OnWandComplete();
                    Destroy(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void CheckForAllies()
    {
        // OverlapSphere to find nearby allies - explicitly include triggers
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1.5f, ~0, QueryTriggerInteraction.Collide);

        foreach (var col in colliders)
        {
            // Get the gameObject - check both collider's object and root
            GameObject target = col.gameObject;
            
            // Check for player or ally tag on the collider's gameObject or parent
            bool isAllyOrPlayer = target.CompareTag("Player") || target.CompareTag("Ally");
            
            // Also check parent/root if not found
            if (!isAllyOrPlayer && target.transform.parent != null)
            {
                var parent = target.transform.root.gameObject;
                if (parent.CompareTag("Player") || parent.CompareTag("Ally"))
                {
                    target = parent;
                    isAllyOrPlayer = true;
                }
            }
            
            if (!isAllyOrPlayer) continue;

            // Ensure ally has required components for shield display
            if (target.CompareTag("Ally"))
            {
                if (target.GetComponent<ModifierTracker>() == null)
                    target.AddComponent<ModifierTracker>();
                if (target.GetComponent<HealthSystem>() == null)
                    target.AddComponent<HealthSystem>();
            }
            
            // Track hits separately for forward and return
            HashSet<GameObject> hitSet = _isReturning ? _hitReturn : _hitForward;

            if (hitSet.Contains(target)) continue;

            // Check cooldown
            if (_shieldCooldownTimers.ContainsKey(target) && Time.time < _shieldCooldownTimers[target])
                continue;

            // Apply shield
            ApplyShield(target);
            hitSet.Add(target);
            _shieldCooldownTimers[target] = Time.time + _shieldCooldown;

            Debug.Log($"Lux wand applied shield to {target.name} ({(_isReturning ? "return" : "forward")})");
        }
    }

    void ApplyShield(GameObject target)
    {
        // Try to apply shield via ShieldStatus component
        var shield = target.GetComponent<LuxShieldStatus>();
        if (shield == null) shield = target.AddComponent<LuxShieldStatus>();
        shield.Apply(_shieldAmount, _shieldDuration);

        // Show modifier icon
        Sprite icon = ModifiersIconLibrary.Instance != null ? ModifiersIconLibrary.Instance.SHIELD : null;
        
        // Show on player HUD if it's the player
        if (target.CompareTag("Player") && ModifiersUIManager.Instance != null)
        {
            ModifiersUIManager.Instance.AddOrUpdate("LuxShield", icon, $"Shield: {_shieldAmount:F0}", _shieldDuration, 0);
        }
        
        // Show on ally/enemy health bar
        var tracker = target.GetComponent<ModifierTracker>();
        if (tracker != null)
        {
            tracker.AddOrUpdate("LuxShield", icon, _shieldDuration, 0);
        }
    }
}

// Decaying shield status component
public class LuxShieldStatus : MonoBehaviour
{
    private float _shieldAmount;
    private float _duration;
    private float _startTime;
    private float _initialAmount;
    private HealthSystem _hp;

    public void Apply(float amount, float duration)
    {
        _hp = GetComponent<HealthSystem>();
        
        // Stack shields if already have one
        _shieldAmount += amount;
        _initialAmount = _shieldAmount;
        _duration = duration;
        _startTime = Time.time;

        Debug.Log($"Shield applied: {_shieldAmount} for {_duration}s");
    }

    void Update()
    {
        if (_shieldAmount <= 0)
        {
            Destroy(this);
            return;
        }

        // Decay shield over time
        float elapsed = Time.time - _startTime;
        if (elapsed >= _duration)
        {
            _shieldAmount = 0;
            Destroy(this);
            return;
        }

        // Linear decay
        float remaining = 1f - (elapsed / _duration);
        _shieldAmount = _initialAmount * remaining;
    }

    // Called by HealthSystem to absorb damage
    public float AbsorbDamage(float damage)
    {
        if (_shieldAmount <= 0) return damage;

        if (damage <= _shieldAmount)
        {
            _shieldAmount -= damage;
            Debug.Log($"Shield absorbed {damage} damage. Shield remaining: {_shieldAmount}");
            return 0;
        }
        else
        {
            float overflow = damage - _shieldAmount;
            Debug.Log($"Shield absorbed {_shieldAmount} damage. {overflow} damage passes through.");
            _shieldAmount = 0;
            return overflow;
        }
    }

    public float GetCurrentShield() => _shieldAmount;
}
