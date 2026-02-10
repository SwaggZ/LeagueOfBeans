using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JhinController : MonoBehaviour
{
    [Header("References")]
    public GameObject cam;
    public Transform firePoint;

    [Header("Projectile Prefabs")]
    public GameObject bulletPrefab;
    public GameObject specialBulletPrefab;
    public GameObject minePrefab;
    public GameObject bounceBulletPrefab;

    [Header("HUD Icons")]
    public Sprite bulletIcon;
    public Sprite empoweredBulletIcon;
    public Sprite reloadingIcon;
    public Sprite specialBulletIcon;
    public Sprite mineIcon;
    public Sprite bounceIcon;
    public Sprite reloadIcon;

    [Header("Ammo System (LMB)")]
    public int maxAmmo = 4;
    public int currentAmmo = 4;
    public float normalDamage = 30f;
    public float empoweredDamage = 80f; // Last bullet damage
    public float bulletSpeed = 50f;
    public float bulletMaxDistance = 40f;
    public float fireCooldown = 0.6f;
    public float empoweredFireCooldown = 1.2f; // Slower for last bullet
    private bool _fireOnCooldown = false;

    [Header("Special Bullet (RMB)")]
    public float specialBulletDamage = 40f;
    public float specialBulletSpeed = 60f;
    public float specialBulletMaxDistance = 50f;
    public float specialBulletCooldown = 8f;
    public float markWindow = 3f; // Stun enemies hit in last 3 seconds
    public float markStunDuration = 1.5f;
    public float moveSpeedBoost = 0.10f; // 10% move speed
    public float moveSpeedBoostDuration = 3f;
    private bool _specialOnCooldown = false;

    [Header("Land Mine (Q)")]
    public float mineDamage = 60f;
    public float mineAoeRadius = 4f;
    public float mineCooldown = 10f;
    public float mineLifetime = 30f;
    private bool _mineOnCooldown = false;

    [Header("Bouncing Bullet (E)")]
    public float bounceDamage = 25f;
    public float bounceSpeed = 35f;
    public float bounceArcSpeed = 25f; // Speed during arc bounces
    public float bounceMaxDistance = 60f;
    public int maxBounces = 4;
    public float bounceRadius = 10f; // Search radius for next target
    public float missingHealthDamageBonus = 0.5f; // 50% of missing HP as bonus damage
    public float bounceCooldown = 12f;
    private bool _bounceOnCooldown = false;

    [Header("Reload (R)")]
    public float reloadTime = 1.5f;
    private bool _isReloading = false;

    // Track recent hits for RMB stun
    private struct RecentHit { public Collider col; public float time; }
    private readonly List<RecentHit> _recentHits = new List<RecentHit>();

    void Awake()
    {
        if (cam == null)
        {
            var c = GetComponentInChildren<Camera>(true);
            if (c != null) cam = c.gameObject;
        }
        if (firePoint == null) firePoint = transform;

        currentAmmo = maxAmmo;
        UpdateAmmoUI();
        UpdateAbilityIcons();
    }

    void Start()
    {
        UpdateAmmoUI();
        UpdateAbilityIcons();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryShoot();
        if (Input.GetMouseButtonDown(1)) TrySpecialBullet();
        if (Input.GetKeyDown(KeyCode.Q)) TryPlaceMine();
        if (Input.GetKeyDown(KeyCode.E)) TryBouncingBullet();
        if (Input.GetKeyDown(KeyCode.R)) TryReload();
    }

    private void UpdateAbilityIcons()
    {
        if (CooldownUIManager.Instance == null) return;
        UpdateLmbIcon();
        if (specialBulletIcon != null) CooldownUIManager.Instance.SetAbilityIcon(AbilityKey.RightClick, specialBulletIcon);
        if (mineIcon != null) CooldownUIManager.Instance.SetAbilityIcon(AbilityKey.One, mineIcon);
        if (bounceIcon != null) CooldownUIManager.Instance.SetAbilityIcon(AbilityKey.Two, bounceIcon);
    }

    private void UpdateLmbIcon()
    {
        if (CooldownUIManager.Instance == null) return;
        
        Sprite icon;
        if (_isReloading)
        {
            icon = reloadingIcon != null ? reloadingIcon : bulletIcon;
        }
        else if (currentAmmo <= 0)
        {
            icon = reloadingIcon != null ? reloadingIcon : bulletIcon;
        }
        else if (currentAmmo == 1)
        {
            icon = empoweredBulletIcon != null ? empoweredBulletIcon : bulletIcon;
        }
        else
        {
            icon = bulletIcon;
        }
        
        if (icon != null) CooldownUIManager.Instance.SetAbilityIcon(AbilityKey.LeftClick, icon);
    }

    private void UpdateAmmoUI()
    {
        UpdateLmbIcon();
        
        if (ModifiersUIManager.Instance != null)
        {
            if (currentAmmo > 0)
            {
                Sprite icon = currentAmmo == 1 ? (empoweredBulletIcon != null ? empoweredBulletIcon : bulletIcon) : bulletIcon;
                string label = currentAmmo == 1 ? "FINAL BULLET!" : $"Ammo: {currentAmmo}/{maxAmmo}";
                ModifiersUIManager.Instance.AddOrUpdate("JhinAmmo", icon, label, -1f, 0);
            }
            else
            {
                Sprite icon = reloadingIcon != null ? reloadingIcon : bulletIcon;
                ModifiersUIManager.Instance.AddOrUpdate("JhinAmmo", icon, "EMPTY - Press R", -1f, 0);
            }
        }
    }

    #region LMB - Regular Shooting
    public void TryShoot()
    {
        if (_fireOnCooldown || _isReloading) return;
        if (currentAmmo <= 0)
        {
            Debug.Log("Jhin: Out of ammo! Press R to reload.");
            return;
        }
        if (cam == null) return;

        bool isLastBullet = (currentAmmo == 1);
        float damage = isLastBullet ? empoweredDamage : normalDamage;
        float cooldown = isLastBullet ? empoweredFireCooldown : fireCooldown;

        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        Quaternion rot = cam.transform.rotation;

        GameObject go = bulletPrefab != null
            ? Instantiate(bulletPrefab, pos, rot)
            : CreateRuntimeBullet(pos, rot, isLastBullet);

        var proj = go.GetComponent<JhinProjectile>();
        if (proj == null) proj = go.AddComponent<JhinProjectile>();
        proj.Init(this, damage, bulletSpeed, bulletMaxDistance, isLastBullet);

        currentAmmo--;
        UpdateAmmoUI();

        _fireOnCooldown = true;
        StartCoroutine(FireCooldown(cooldown));
        if (CooldownUIManager.Instance != null)
            CooldownUIManager.Instance.StartCooldown(AbilityKey.LeftClick, cooldown);
    }

    IEnumerator FireCooldown(float t)
    {
        yield return new WaitForSeconds(t);
        _fireOnCooldown = false;
    }
    #endregion

    #region RMB - Special Bullet (Stun Marked + Speed Boost)
    public void TrySpecialBullet()
    {
        if (_specialOnCooldown) return;
        if (cam == null) return;

        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        Quaternion rot = cam.transform.rotation;

        GameObject go = specialBulletPrefab != null
            ? Instantiate(specialBulletPrefab, pos, rot)
            : CreateRuntimeBullet(pos, rot, true);

        var proj = go.GetComponent<JhinProjectile>();
        if (proj == null) proj = go.AddComponent<JhinProjectile>();
        proj.Init(this, specialBulletDamage, specialBulletSpeed, specialBulletMaxDistance, true, isSpecial: true, markStunDuration);

        // Apply move speed boost to self
        ApplyMoveSpeedBoost();

        _specialOnCooldown = true;
        StartCoroutine(SpecialCooldown());
        if (CooldownUIManager.Instance != null)
            CooldownUIManager.Instance.StartCooldown(AbilityKey.RightClick, specialBulletCooldown);
    }

    IEnumerator SpecialCooldown()
    {
        yield return new WaitForSeconds(specialBulletCooldown);
        _specialOnCooldown = false;
    }

    public void RecordHit(Collider c)
    {
        if (c == null) return;
        CleanupOldHits();
        _recentHits.Add(new RecentHit { col = c, time = Time.time });

        // Apply mark status to enemy
        GameObject target = ModifierUtils.ResolveTarget(c);
        if (target != null)
        {
            var marker = target.GetComponent<JhinMarkStatus>();
            if (marker == null) marker = target.AddComponent<JhinMarkStatus>();
            marker.Apply(markWindow);
        }
    }

    private void CleanupOldHits()
    {
        float cutoff = Time.time - markWindow;
        _recentHits.RemoveAll(h => h.time < cutoff || h.col == null);
    }

    private void StunMarkedEnemies()
    {
        CleanupOldHits();
        HashSet<GameObject> stunned = new HashSet<GameObject>();

        foreach (var hit in _recentHits)
        {
            if (hit.col == null) continue;
            GameObject target = ModifierUtils.ResolveTarget(hit.col);
            if (target == null || stunned.Contains(target)) continue;

            var marker = target.GetComponent<JhinMarkStatus>();
            if (marker != null)
            {
                marker.StunFromMark(markStunDuration);
                stunned.Add(target);
            }
        }

        _recentHits.Clear();
        Debug.Log($"Jhin: Stunned {stunned.Count} marked enemies!");
    }

    private void ApplyMoveSpeedBoost()
    {
        var cc = GetComponent<CharacterControl>();
        if (cc != null)
        {
            StartCoroutine(MoveSpeedBoostRoutine(cc));
        }

        if (ModifiersUIManager.Instance != null)
        {
            Sprite icon = ModifiersIconLibrary.Instance != null ? ModifiersIconLibrary.Instance.MOVESPEED : null;
            ModifiersUIManager.Instance.AddOrUpdate("JhinSpeedBoost", icon, "Speed Boost", moveSpeedBoostDuration, 0);
        }
    }

    IEnumerator MoveSpeedBoostRoutine(CharacterControl cc)
    {
        float originalSpeed = cc.speed;
        cc.speed = originalSpeed * (1f + moveSpeedBoost);
        yield return new WaitForSeconds(moveSpeedBoostDuration);
        cc.speed = originalSpeed;

        if (ModifiersUIManager.Instance != null)
            ModifiersUIManager.Instance.Remove("JhinSpeedBoost");
    }
    #endregion

    #region Q - Land Mine
    public void TryPlaceMine()
    {
        if (_mineOnCooldown) return;
        if (cam == null) return;

        // Throw mine from camera position (like Caitlyn's W)
        Vector3 spawnPos = cam.transform.position;
        Quaternion spawnRot = cam.transform.rotation;

        GameObject go = minePrefab != null
            ? Instantiate(minePrefab, spawnPos, spawnRot)
            : CreateRuntimeMine(spawnPos, spawnRot);

        var mine = go.GetComponent<JhinMine>();
        if (mine == null) mine = go.AddComponent<JhinMine>();
        mine.Init(mineDamage, mineAoeRadius, mineLifetime, cam.transform.forward);

        _mineOnCooldown = true;
        StartCoroutine(MineCooldown());
        if (CooldownUIManager.Instance != null)
            CooldownUIManager.Instance.StartCooldown(AbilityKey.One, mineCooldown);
    }

    IEnumerator MineCooldown()
    {
        yield return new WaitForSeconds(mineCooldown);
        _mineOnCooldown = false;
    }
    #endregion

    #region E - Bouncing Bullet
    public void TryBouncingBullet()
    {
        if (_bounceOnCooldown) return;
        if (cam == null) return;

        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        Quaternion rot = cam.transform.rotation;

        GameObject go = bounceBulletPrefab != null
            ? Instantiate(bounceBulletPrefab, pos, rot)
            : CreateRuntimeBounceBullet(pos, rot);

        var proj = go.GetComponent<JhinBounceBullet>();
        if (proj == null) proj = go.AddComponent<JhinBounceBullet>();
        proj.Init(this, bounceDamage, bounceSpeed, bounceArcSpeed, bounceMaxDistance, maxBounces, bounceRadius, missingHealthDamageBonus);

        _bounceOnCooldown = true;
        StartCoroutine(BounceCooldown());
        if (CooldownUIManager.Instance != null)
            CooldownUIManager.Instance.StartCooldown(AbilityKey.Two, bounceCooldown);
    }

    IEnumerator BounceCooldown()
    {
        yield return new WaitForSeconds(bounceCooldown);
        _bounceOnCooldown = false;
    }
    #endregion

    #region R - Reload
    public void TryReload()
    {
        if (_isReloading) return;
        if (currentAmmo >= maxAmmo)
        {
            Debug.Log("Jhin: Ammo already full!");
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        UpdateLmbIcon();

        // Update the existing ammo modifier to show reloading
        if (ModifiersUIManager.Instance != null)
        {
            Sprite icon = reloadingIcon != null ? reloadingIcon : bulletIcon;
            ModifiersUIManager.Instance.AddOrUpdate("JhinAmmo", icon, "Reloading...", reloadTime, 0);
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        _isReloading = false;
        UpdateAmmoUI(); // This will update the modifier back to ammo count

        Debug.Log("Jhin: Reloaded!");
    }
    #endregion

    #region Runtime Prefab Creation
    private GameObject CreateRuntimeBullet(Vector3 pos, Quaternion rot, bool empowered)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = empowered ? "JhinEmpoweredBullet" : "JhinBullet";
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = empowered ? new Vector3(0.15f, 0.4f, 0.15f) : new Vector3(0.1f, 0.3f, 0.1f);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = empowered ? new Color(1f, 0.2f, 0.2f) : new Color(0.9f, 0.7f, 0.3f); // Red or gold
            mr.material = mat;
        }

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        return go;
    }

    private GameObject CreateRuntimeMine(Vector3 pos, Quaternion rot)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "JhinMine";
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.4f, 0.2f, 0.1f); // Brown
            mr.material = mat;
        }

        // Add rigidbody for physics throw (like Caitlyn's throwable)
        var rb = go.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        return go;
    }

    private GameObject CreateRuntimeBounceBullet(Vector3 pos, Quaternion rot)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "JhinBounceBullet";
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = Vector3.one * 0.25f;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.8f, 0.4f, 0.9f); // Purple
            mr.material = mat;
        }

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        return go;
    }
    #endregion
}
