using System.Collections;
using UnityEngine;

/// <summary>
/// Swain-style battle mage.
///   LMB - Lightning: instant bolts in a wide, flat cone in front of him.
///   RMB - Grasping hand, two stages: the hand flies forward and stuns the first enemy it hits;
///         recast within the window to drag that enemy to him.
///   Q   - Eye: a projectile that bursts in an area, draining health from every enemy hit to heal him.
///   E   - Demon form: an aura drains nearby enemies and heals him for part of the damage. Ends after
///         5 seconds without hitting an enemy, or after 20 seconds at most.
/// </summary>
public class SwainController : MonoBehaviour
{
    [Header("References")]
    public GameObject cam;
    public Transform firePoint;

    [Header("HUD Icons")]
    public Sprite lightningIcon;
    public Sprite handIcon;
    [Tooltip("Optional: shown on RMB while the pull recast is available.")]
    public Sprite pullIcon;
    public Sprite eyeIcon;
    public Sprite demonIcon;

    [Header("LMB - Lightning cone")]
    public float boltDamage = 22f;
    public float boltRange = 9f;
    [Tooltip("Half of the cone's horizontal width, in degrees.")]
    public float boltHalfAngle = 30f;
    [Tooltip("How far above/below the aim line a target can be (keeps the cone flat).")]
    public float boltHalfHeight = 1.5f;
    public float boltCooldown = 1f;

    [Header("RMB - Grasping hand")]
    public float handDamage = 20f;
    public float handSpeed = 40f;
    public float handRange = 22f;
    public float handStunDuration = 1f;
    [Tooltip("Seconds after a hit during which RMB pulls the enemy.")]
    public float handRecastWindow = 3f;
    public float pullSpeed = 25f;
    [Tooltip("How far in front of Swain the pulled enemy stops.")]
    public float pullStopDistance = 2.5f;
    public float handCooldown = 10f;

    [Header("Q - Eye")]
    public float eyeDamage = 35f;
    public float eyeSpeed = 28f;
    public float eyeRange = 28f;
    public float eyeRadius = 3.5f;
    [Tooltip("Fraction of the damage dealt that heals Swain.")]
    public float eyeHealRatio = 0.6f;
    public float eyeCooldown = 7f;

    [Header("E - Demon form")]
    public float auraRadius = 5.5f;
    public float auraTickInterval = 0.5f;
    public float auraDamagePerTick = 8f;
    [Tooltip("Fraction of aura damage dealt that heals Swain.")]
    public float auraHealRatio = 0.35f;
    [Tooltip("Demon form ends if the aura hits nobody for this long.")]
    public float demonIdleTimeout = 5f;
    public float demonMaxDuration = 20f;
    public float demonCooldown = 30f;

    public bool InDemonForm => _demonActive;

    private float _nextBolt;
    private bool _eyeOnCooldown;

    private enum HandState { Ready, Flying, Tethered, Cooldown }
    private HandState _hand = HandState.Ready;
    private GameObject _tetherTarget;
    private float _tetherEnd;
    private LineRenderer _tether;

    private bool _demonActive, _demonOnCooldown;
    private float _demonStart, _demonLastHit, _nextAuraTick;
    private GameObject _aura;

    void Awake()
    {
        if (cam == null)
        {
            var c = GetComponentInChildren<Camera>(true);
            if (c != null) cam = c.gameObject;
        }
        if (firePoint == null) firePoint = cam != null ? cam.transform : transform;
    }

    void Start() => RefreshIcons();
    void OnEnable() => RefreshIcons();

    void OnDisable()
    {
        EndTether(startCooldown: false);
        if (_demonActive) EndDemonForm(startCooldown: false);
        _hand = HandState.Ready;
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) TryLightning();
        if (Input.GetMouseButtonDown(1)) TryHand();
        if (Input.GetKeyDown(KeyCode.Q)) TryEye();
        if (Input.GetKeyDown(KeyCode.E)) TryDemonForm();

        UpdateTether();
        UpdateDemonForm();
    }

    // ------------------------------------------------------------------ LMB

    public void TryLightning()
    {
        if (Time.time < _nextBolt || cam == null) return;
        _nextBolt = Time.time + boltCooldown;
        Hud()?.StartCooldown(AbilityKey.LeftClick, boltCooldown);

        Transform c = cam.transform;
        Vector3 origin = Muzzle();
        int hits = 0;
        foreach (var enemy in AbilityHits.EnemiesInRadius(c.position, boltRange + 1.5f))
        {
            Vector3 center = Center(enemy);
            Vector3 local = c.InverseTransformPoint(center);
            if (local.z <= 0.2f) continue;                                   // behind
            if (new Vector2(local.x, local.z).magnitude > boltRange) continue; // too far
            if (Mathf.Abs(local.y) > boltHalfHeight) continue;               // outside the flat band
            if (Mathf.Atan2(Mathf.Abs(local.x), local.z) * Mathf.Rad2Deg > boltHalfAngle) continue;
            if (!AbilityHits.HasLineOfSight(transform, c.position, enemy)) continue;

            AbilityHits.Damage(enemy, boltDamage);
            SwainVisuals.Lightning(origin, center, SwainVisuals.Violet, 0.18f, 0.06f);
            hits++;
        }

        // Fan of bolts showing the cone's shape (stopping at walls).
        const int fan = 5;
        for (int i = 0; i < fan; i++)
        {
            float a = Mathf.Lerp(-boltHalfAngle, boltHalfAngle, i / (float)(fan - 1));
            Vector3 dir = Quaternion.AngleAxis(a, c.up) * c.forward;
            float len = boltRange;
            if (AbilityHits.Cast(transform, origin, dir, boltRange, out var wall) && !wall.collider.CompareTag("Enemy"))
                len = wall.distance;
            SwainVisuals.Lightning(origin, origin + dir * len, SwainVisuals.Violet, 0.14f, 0.035f);
        }
    }

    // ------------------------------------------------------------------ RMB

    public void TryHand()
    {
        if (_hand == HandState.Tethered) { PullTethered(); return; }
        if (_hand != HandState.Ready || cam == null) return;

        _hand = HandState.Flying;
        var go = SwainVisuals.Hand();
        go.transform.SetPositionAndRotation(Muzzle(), cam.transform.rotation);
        go.AddComponent<SwainHand>().Init(this, handSpeed, handRange);
    }

    internal void OnHandHit(GameObject target)
    {
        AbilityHits.Damage(target, handDamage);
        if (target == null || AbilityHits.Health(target) == null || AbilityHits.Health(target).GetCurrentHealth() <= 0f)
        {
            StartHandCooldown();
            return;
        }
        AbilityHits.Stun(target, handStunDuration);

        _hand = HandState.Tethered;
        _tetherTarget = target;
        _tetherEnd = Time.time + handRecastWindow;
        _tether = SwainVisuals.Tether();
        if (pullIcon != null) Hud()?.SetAbilityIcon(AbilityKey.RightClick, pullIcon);
        ModifiersUIManager.Instance?.AddOrUpdate("SwainPull", pullIcon != null ? pullIcon : handIcon, "Recast RMB: pull", handRecastWindow, 0);
    }

    internal void OnHandMissed() => StartHandCooldown();

    private void PullTethered()
    {
        var target = _tetherTarget;
        EndTether(startCooldown: true);
        if (target == null) return;
        AbilityHits.Pull(target, transform.position, pullStopDistance, pullSpeed);
        SwainVisuals.Lightning(Muzzle(), Center(target), SwainVisuals.Crimson, 0.25f, 0.08f);
    }

    private void UpdateTether()
    {
        if (_hand != HandState.Tethered) return;
        bool lost = _tetherTarget == null || !_tetherTarget.activeInHierarchy
                    || AbilityHits.Health(_tetherTarget) == null || AbilityHits.Health(_tetherTarget).GetCurrentHealth() <= 0f
                    || Vector3.Distance(transform.position, _tetherTarget.transform.position) > handRange + 8f;
        if (lost || Time.time >= _tetherEnd) { EndTether(startCooldown: true); return; }
        if (_tether != null)
        {
            _tether.SetPosition(0, Muzzle() - Vector3.up * 0.3f);
            _tether.SetPosition(1, Center(_tetherTarget));
        }
    }

    private void EndTether(bool startCooldown)
    {
        if (_tether != null) Destroy(_tether.gameObject);
        _tether = null;
        _tetherTarget = null;
        ModifiersUIManager.Instance?.Remove("SwainPull");
        if (handIcon != null) Hud()?.SetAbilityIcon(AbilityKey.RightClick, handIcon);
        if (startCooldown && _hand == HandState.Tethered) StartHandCooldown();
    }

    private void StartHandCooldown()
    {
        _hand = HandState.Cooldown;
        Hud()?.StartCooldown(AbilityKey.RightClick, handCooldown);
        StartCoroutine(After(handCooldown, () => { if (_hand == HandState.Cooldown) _hand = HandState.Ready; }));
    }

    // ------------------------------------------------------------------ Q

    public void TryEye()
    {
        if (_eyeOnCooldown || cam == null) return;
        _eyeOnCooldown = true;
        Hud()?.StartCooldown(AbilityKey.One, eyeCooldown);
        StartCoroutine(After(eyeCooldown, () => _eyeOnCooldown = false));

        var go = SwainVisuals.Eye();
        go.transform.SetPositionAndRotation(Muzzle(), cam.transform.rotation);
        go.AddComponent<SwainEye>().Init(this);
    }

    internal void EyeBurst(Vector3 point)
    {
        float dealt = 0f;
        foreach (var enemy in AbilityHits.EnemiesInRadius(point, eyeRadius))
        {
            dealt += AbilityHits.Damage(enemy, eyeDamage);
            SwainVisuals.Lightning(Center(enemy), Muzzle() - Vector3.up * 0.4f, SwainVisuals.Crimson, 0.35f, 0.05f); // drain line
        }
        AbilityHits.Heal(gameObject, dealt * eyeHealRatio);
        SwainVisuals.Burst(point, eyeRadius, SwainVisuals.Crimson);
    }

    // ------------------------------------------------------------------ E

    public void TryDemonForm()
    {
        if (_demonActive || _demonOnCooldown) return;
        _demonActive = true;
        _demonStart = _demonLastHit = Time.time;
        _nextAuraTick = Time.time;
        _aura = SwainVisuals.Aura(transform, auraRadius);
        Hud()?.SetAbilityAvailable(AbilityKey.Two, false);
        ModifiersUIManager.Instance?.AddOrUpdate("SwainDemon", demonIcon, "Demon Form", demonMaxDuration, 0);
    }

    private void UpdateDemonForm()
    {
        if (!_demonActive) return;
        float now = Time.time;

        if (now >= _nextAuraTick)
        {
            _nextAuraTick = now + auraTickInterval;
            float dealt = 0f;
            foreach (var enemy in AbilityHits.EnemiesInRadius(transform.position, auraRadius))
            {
                float d = AbilityHits.Damage(enemy, auraDamagePerTick);
                if (d <= 0f) continue;
                dealt += d;
                SwainVisuals.Lightning(Center(enemy), transform.position, SwainVisuals.Crimson, 0.2f, 0.03f);
            }
            if (dealt > 0f)
            {
                _demonLastHit = now;
                AbilityHits.Heal(gameObject, dealt * auraHealRatio);
            }
        }

        if (now - _demonLastHit >= demonIdleTimeout || now - _demonStart >= demonMaxDuration)
            EndDemonForm(startCooldown: true);
    }

    private void EndDemonForm(bool startCooldown)
    {
        _demonActive = false;
        if (_aura != null) Destroy(_aura);
        _aura = null;
        ModifiersUIManager.Instance?.Remove("SwainDemon");
        var hud = Hud();
        hud?.SetAbilityAvailable(AbilityKey.Two, true);
        if (!startCooldown) return;
        _demonOnCooldown = true;
        hud?.StartCooldown(AbilityKey.Two, demonCooldown);
        StartCoroutine(After(demonCooldown, () => _demonOnCooldown = false));
    }

    // ------------------------------------------------------------------ helpers

    private void RefreshIcons()
    {
        var hud = Hud();
        if (hud == null) return;
        if (lightningIcon != null) hud.SetAbilityIcon(AbilityKey.LeftClick, lightningIcon);
        if (handIcon != null) hud.SetAbilityIcon(AbilityKey.RightClick, handIcon);
        if (eyeIcon != null) hud.SetAbilityIcon(AbilityKey.One, eyeIcon);
        if (demonIcon != null) hud.SetAbilityIcon(AbilityKey.Two, demonIcon);
    }

    internal Vector3 Muzzle()
    {
        Transform t = cam != null ? cam.transform : transform;
        Vector3 basePos = firePoint != null ? firePoint.position : t.position;
        return basePos + t.forward * 0.6f;
    }

    internal static Vector3 Center(GameObject go)
    {
        var col = go.GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : go.transform.position;
    }

    private static CooldownUIManager Hud() => CooldownUIManager.Instance;

    private IEnumerator After(float seconds, System.Action done)
    {
        yield return new WaitForSeconds(seconds);
        done();
    }
}
