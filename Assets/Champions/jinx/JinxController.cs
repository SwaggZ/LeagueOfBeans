using System.Collections;
using UnityEngine;

/// <summary>
/// Jinx-style marksman.
///   LMB  - fire the current weapon (hold to keep firing)
///          Minigun: fast, low damage, spins up with sustained fire
///          Rocket launcher: slow, splash damage
///   RMB  - swap weapon
///   Q    - throw three stun mines in a spread
///   E    - load (short channel, slowed) then fire a giant rocket across the map;
///          damage grows with distance travelled and with the target's missing health
///   Passive - getting a kill grants a short, decaying move speed boost
/// All Jinx damage goes through <see cref="DealDamage"/> so kills can be credited.
/// </summary>
public class JinxController : MonoBehaviour
{
    public enum Weapon { Minigun, Rockets }

    [Header("References")]
    public GameObject cam;
    public Transform firePoint;

    [Header("Optional prefabs (runtime placeholders are built when empty)")]
    public GameObject minigunBulletPrefab;
    public GameObject rocketPrefab;
    public GameObject minePrefab;
    public GameObject megaRocketPrefab;

    [Header("HUD Icons")]
    public Sprite minigunIcon;
    public Sprite rocketIcon;
    public Sprite mineIcon;
    public Sprite megaRocketIcon;

    [Header("LMB - Minigun")]
    public float minigunDamage = 8f;
    public float minigunFireInterval = 0.16f;
    [Tooltip("Fire interval once fully spun up.")]
    public float minigunMinFireInterval = 0.07f;
    [Tooltip("Seconds of continuous fire to reach full spin.")]
    public float minigunSpinUpTime = 1.5f;
    public float minigunBulletSpeed = 70f;
    public float minigunRange = 35f;
    public float minigunSpread = 1.2f; // degrees

    [Header("LMB - Rocket launcher")]
    public float rocketDamage = 28f;
    public float rocketFireInterval = 0.9f;
    public float rocketSpeed = 40f;
    public float rocketRange = 50f;
    public float rocketSplashRadius = 3f;

    [Header("RMB - Weapon swap")]
    public Weapon startingWeapon = Weapon.Minigun;
    public float swapCooldown = 0.6f;

    [Header("Q - Stun mines")]
    public int mineCount = 3;
    public float mineSpreadAngle = 18f;
    public float mineThrowForce = 14f;
    public float mineDamage = 20f;
    public float mineStunDuration = 1.5f;
    public float mineLifetime = 8f;
    public float mineCooldown = 14f;

    [Header("E - Mega rocket")]
    public float megaLoadTime = 1f;
    [Range(0f, 1f)] public float megaLoadSlow = 0.5f;
    public float megaStartSpeed = 25f;
    public float megaMaxSpeed = 75f;
    public float megaAcceleration = 60f;
    public float megaRange = 200f;
    public float megaBaseDamage = 60f;
    [Tooltip("Extra damage reached after travelling megaFullDamageDistance.")]
    public float megaDistanceBonus = 90f;
    public float megaFullDamageDistance = 60f;
    [Tooltip("Fraction of the target's missing health added as damage.")]
    public float megaMissingHealthRatio = 0.25f;
    public float megaSplashRadius = 5f;
    [Tooltip("Damage multiplier for enemies caught in the splash (not the direct hit).")]
    public float megaSplashMultiplier = 0.8f;
    public float megaCooldown = 30f;

    [Header("Passive - Get excited on kill")]
    public float killSpeedBonus = 0.35f;
    public float killSpeedDuration = 3f;

    public Weapon CurrentWeapon { get; private set; }

    private float _nextFireTime;
    private float _nextSwapTime;
    private float _spin;               // 0..1 minigun spin-up
    private bool _mineOnCooldown;
    private bool _megaOnCooldown;
    private bool _loadingMega;

    private CharacterControl _cc;
    private float _baseSpeed, _baseRunSpeed;
    private float _killBoostEnd = -1f;

    void Awake()
    {
        if (cam == null)
        {
            var c = GetComponentInChildren<Camera>(true);
            if (c != null) cam = c.gameObject;
        }
        if (firePoint == null) firePoint = cam != null ? cam.transform : transform;
        _cc = GetComponent<CharacterControl>();
        if (_cc != null) { _baseSpeed = _cc.speed; _baseRunSpeed = _cc.runSpeed; }
        CurrentWeapon = startingWeapon;
    }

    void Start() => RefreshIcons();

    void OnEnable() => RefreshIcons();

    void OnDisable()
    {
        // Leave the movement component as we found it.
        _loadingMega = false;
        _killBoostEnd = -1f;
        ApplySpeed();
    }

    void Update()
    {
        if (_loadingMega) { UpdateSpeedBoost(); return; } // channelling the mega rocket

        bool firing = Input.GetMouseButton(0);
        if (firing) TryFire();

        // Minigun spins up while firing and winds down otherwise.
        if (CurrentWeapon == Weapon.Minigun && firing)
            _spin = Mathf.Min(1f, _spin + Time.deltaTime / Mathf.Max(0.01f, minigunSpinUpTime));
        else
            _spin = Mathf.Max(0f, _spin - Time.deltaTime / 0.75f);

        if (Input.GetMouseButtonDown(1)) TrySwapWeapon();
        if (Input.GetKeyDown(KeyCode.Q)) TryThrowMines();
        if (Input.GetKeyDown(KeyCode.E)) TryMegaRocket();

        UpdateSpeedBoost();
    }

    // ------------------------------------------------------------------ LMB

    public void TryFire()
    {
        if (Time.time < _nextFireTime || cam == null) return;

        Vector3 pos = MuzzlePosition();
        if (CurrentWeapon == Weapon.Minigun)
        {
            Quaternion rot = cam.transform.rotation * Quaternion.Euler(
                Random.Range(-minigunSpread, minigunSpread), Random.Range(-minigunSpread, minigunSpread), 0f);
            var go = Spawn(minigunBulletPrefab, pos, rot, () => JinxVisuals.Bullet(new Color(1f, 0.45f, 0.85f), 0.07f, 0.35f));
            Get<JinxProjectile>(go).Init(this, minigunDamage, minigunBulletSpeed, minigunRange, 0f);

            float interval = Mathf.Lerp(minigunFireInterval, minigunMinFireInterval, _spin);
            _nextFireTime = Time.time + interval;
            Hud()?.StartCooldown(AbilityKey.LeftClick, interval);
        }
        else
        {
            var go = Spawn(rocketPrefab, pos, cam.transform.rotation, () => JinxVisuals.Bullet(new Color(0.3f, 0.75f, 1f), 0.18f, 0.6f));
            Get<JinxProjectile>(go).Init(this, rocketDamage, rocketSpeed, rocketRange, rocketSplashRadius);

            _nextFireTime = Time.time + rocketFireInterval;
            Hud()?.StartCooldown(AbilityKey.LeftClick, rocketFireInterval);
        }
    }

    // ------------------------------------------------------------------ RMB

    public void TrySwapWeapon()
    {
        if (Time.time < _nextSwapTime) return;
        CurrentWeapon = CurrentWeapon == Weapon.Minigun ? Weapon.Rockets : Weapon.Minigun;
        _spin = 0f;
        _nextSwapTime = Time.time + swapCooldown;
        _nextFireTime = Mathf.Max(_nextFireTime, Time.time + 0.15f); // brief swap delay
        RefreshIcons();
        Hud()?.StartCooldown(AbilityKey.RightClick, swapCooldown);

        var mods = ModifiersUIManager.Instance;
        if (mods != null)
            mods.AddOrUpdate("JinxWeapon", WeaponIcon(CurrentWeapon),
                CurrentWeapon == Weapon.Minigun ? "Minigun" : "Rocket Launcher", -1f, 0);
    }

    // ------------------------------------------------------------------ Q

    public void TryThrowMines()
    {
        if (_mineOnCooldown || cam == null) return;

        Vector3 origin = cam.transform.position + cam.transform.forward * 0.8f;
        int n = Mathf.Max(1, mineCount);
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : (i / (float)(n - 1)) * 2f - 1f; // -1..1
            Vector3 dir = Quaternion.AngleAxis(t * mineSpreadAngle, Vector3.up) * cam.transform.forward;
            dir = (dir + Vector3.up * 0.25f).normalized;

            var go = Spawn(minePrefab, origin, Quaternion.identity, JinxVisuals.Mine);
            Get<JinxMine>(go).Init(this, dir * mineThrowForce, mineDamage, mineStunDuration, mineLifetime);
        }

        StartCoroutine(Cooldown(mineCooldown, () => _mineOnCooldown = false));
        _mineOnCooldown = true;
        Hud()?.StartCooldown(AbilityKey.One, mineCooldown);
    }

    // ------------------------------------------------------------------ E

    public void TryMegaRocket()
    {
        if (_megaOnCooldown || _loadingMega || cam == null) return;
        StartCoroutine(MegaRocketRoutine());
    }

    IEnumerator MegaRocketRoutine()
    {
        _megaOnCooldown = true;
        _loadingMega = true;
        ApplySpeed();
        var mods = ModifiersUIManager.Instance;
        if (mods != null) mods.AddOrUpdate("JinxMegaLoad", megaRocketIcon, "Loading rocket...", megaLoadTime, 0);

        yield return new WaitForSeconds(megaLoadTime);

        _loadingMega = false;
        ApplySpeed();

        if (cam != null)
        {
            var go = Spawn(megaRocketPrefab, MuzzlePosition(), cam.transform.rotation, JinxVisuals.MegaRocket);
            Get<JinxMegaRocket>(go).Init(this);
        }

        Hud()?.StartCooldown(AbilityKey.Two, megaCooldown);
        yield return new WaitForSeconds(megaCooldown);
        _megaOnCooldown = false;
    }

    // ------------------------------------------------------------------ damage + passive

    /// <summary>Deals damage and credits Jinx with the kill if it was the killing blow.</summary>
    public bool DealDamage(GameObject target, float amount)
    {
        if (target == null || amount <= 0f) return false;
        var hp = target.GetComponentInParent<HealthSystem>();
        if (hp == null) return false;

        bool wasAlive = hp.GetCurrentHealth() > 0f;
        hp.TakeDamage(amount);
        if (wasAlive && hp.GetCurrentHealth() <= 0f)
        {
            OnKill(hp.gameObject);
            return true;
        }
        return false;
    }

    private void OnKill(GameObject victim)
    {
        Debug.Log($"Jinx: got {victim.name}! Speed boost.");
        _killBoostEnd = Time.time + killSpeedDuration;
        ApplySpeed();
        var mods = ModifiersUIManager.Instance;
        if (mods != null)
        {
            var lib = ModifiersIconLibrary.Instance;
            mods.AddOrUpdate("JinxExcited", lib != null ? lib.MOVESPEED : null, "Get Excited!", killSpeedDuration, 0);
        }
    }

    private void UpdateSpeedBoost()
    {
        if (_killBoostEnd < 0f) return;
        if (Time.time >= _killBoostEnd) _killBoostEnd = -1f;
        ApplySpeed();
    }

    // Boost decays linearly over its duration; mega-rocket loading slows on top.
    private void ApplySpeed()
    {
        if (_cc == null) return;
        float mult = 1f;
        if (_killBoostEnd > 0f)
        {
            float remaining = Mathf.Clamp01((_killBoostEnd - Time.time) / Mathf.Max(0.01f, killSpeedDuration));
            mult *= 1f + killSpeedBonus * remaining;
        }
        if (_loadingMega) mult *= 1f - megaLoadSlow;
        _cc.speed = _baseSpeed * mult;
        _cc.runSpeed = _baseRunSpeed * mult;
    }

    /// <summary>True when the collider belongs to Jinx herself.</summary>
    public bool IsSelf(Collider c) => c != null && c.transform.IsChildOf(transform);

    // ------------------------------------------------------------------ helpers

    private void RefreshIcons()
    {
        var hud = Hud();
        if (hud == null) return;
        var cur = WeaponIcon(CurrentWeapon);
        var other = WeaponIcon(CurrentWeapon == Weapon.Minigun ? Weapon.Rockets : Weapon.Minigun);
        if (cur != null) hud.SetAbilityIcon(AbilityKey.LeftClick, cur);
        if (other != null) hud.SetAbilityIcon(AbilityKey.RightClick, other);
        if (mineIcon != null) hud.SetAbilityIcon(AbilityKey.One, mineIcon);
        if (megaRocketIcon != null) hud.SetAbilityIcon(AbilityKey.Two, megaRocketIcon);
    }

    private Sprite WeaponIcon(Weapon w) => w == Weapon.Minigun ? minigunIcon : rocketIcon;

    private Vector3 MuzzlePosition()
    {
        Transform t = cam != null ? cam.transform : transform;
        Vector3 basePos = firePoint != null ? firePoint.position : t.position;
        return basePos + t.forward * 0.6f;
    }

    private static CooldownUIManager Hud() => CooldownUIManager.Instance;

    private static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, System.Func<GameObject> fallback)
    {
        if (prefab != null) return NetworkHelper.SpawnProjectile(prefab, pos, rot);
        var go = fallback();
        go.transform.SetPositionAndRotation(pos, rot);
        return go;
    }

    private static T Get<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private IEnumerator Cooldown(float seconds, System.Action done)
    {
        yield return new WaitForSeconds(seconds);
        done();
    }
}
