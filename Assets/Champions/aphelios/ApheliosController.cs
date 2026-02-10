using System.Collections;
using UnityEngine;

public class ApheliosController : MonoBehaviour
{
    public enum WeaponType { Sniper, Scythe, Orbs, Flamethrower }

    [Header("References")]
    public GameObject cam; // aim source
    public Transform firePoint; // optional; fallback to transform

    [Header("Projectile Prefabs")] // simple forward projectiles
    public GameObject sniperProjectile;
    public GameObject scytheProjectile;
    public GameObject orbsProjectile;
    public GameObject flameProjectile;

    [Header("HUD Icons (LMB)")]
    public Sprite sniperIcon;
    public Sprite scytheIcon;
    public Sprite orbsIcon;
    public Sprite flamethrowerIcon;

    [Header("HUD Icons (RMB Ability)")]
    public Sprite sniperAbilityIcon;
    public Sprite scytheAbilityIcon;
    public Sprite orbsAbilityIcon;
    public Sprite flamethrowerAbilityIcon;

    [System.Serializable]
    public class WeaponStats
    {
        public float damage = 20f;
        public float cooldown = 0.8f;
        public float projectileSpeed = 40f;
        public float maxDistance = 30f;
        [Header("Pattern")] public int pellets = 1; // for flamethrower spread
        public float spreadAngle = 0f; // degrees total cone
        [Header("On-Hit Modifiers")] public float lifestealPercent = 0f; // 0.2 = 20%
        public float slowPercent = 0f; // 0.3 = 30%
        public float slowDuration = 0f;
    }

    [Header("Weapon Stats")] // tuned defaults
    public WeaponStats sniper = new WeaponStats { damage = 60f, cooldown = 1.8f, projectileSpeed = 90f, maxDistance = 120f };
    public WeaponStats scythe = new WeaponStats { damage = 20f, cooldown = 0.35f, projectileSpeed = 30f, maxDistance = 18f, lifestealPercent = 0.25f };
    public WeaponStats orbs = new WeaponStats { damage = 28f, cooldown = 0.6f, projectileSpeed = 25f, maxDistance = 16f, slowPercent = 0.35f, slowDuration = 1.8f };
    public WeaponStats flamethrower = new WeaponStats { damage = 18f, cooldown = 0.5f, projectileSpeed = 22f, maxDistance = 12f, pellets = 7, spreadAngle = 18f };

    [Header("State")]
    public WeaponType currentWeapon = WeaponType.Sniper;
    private bool _onCooldown = false;

    [Header("Ability (RMB) - General")]
    public float sniperAbilityCooldown = 8f;
    public float scytheAbilityCooldown = 10f;
    public float orbsAbilityCooldown = 12f;
    public float flamethrowerAbilityCooldown = 10f;
    private bool _abilityOnCooldown = false;

    [Header("Ability (RMB) - Sniper")] 
    public int sniperEmpoweredShots = 0; // when > 0, next sniper shots hit twice

    [Header("Ability (RMB) - Orbs")] 
    public float orbsStunWindow = 3f; // seconds back in time to search
    public float orbsStunDuration = 1.5f;

    [Header("Ability (RMB) - Scythe")]
    public float scytheAuraDuration = 3f;
    public float scytheAuraRadius = 6f;
    public float scytheAuraTickInterval = 0.3f;
    public float scytheAuraDamagePerTick = 10f;

    [Header("Ability (RMB) - Flamethrower")] 
    public int flameBounceCount = 2; // number of times a projectile can bounce
    public float flameBounceRadius = 8f; // search radius to find next target
    public float flameBounceWindow = 4f; // time window after cast where shots can bounce
    private bool _flameBounceActive = false;
    [Header("Ability (RMB) - Flamethrower Tuning")] 
    public int flameAbilityPellets = 9; // wider volley: default more pellets than base
    public float flameAbilitySpreadAngle = 28f; // degrees, wider than base spread
    public float flameAbilitySpeedMult = 1.15f; // slightly faster
    public float flameAbilityRangeMult = 1.5f; // go further than base

    [Header("Burn (Flamethrower)")]
    public float burnDuration = 3f; // seconds
    public float burnDamagePerSecond = 2f;

    // Track recent orbs hits for the stun recall
    private struct RecentHit { public Collider col; public float time; }
    private readonly System.Collections.Generic.List<RecentHit> _recentOrbsHits = new System.Collections.Generic.List<RecentHit>();

    void Awake()
    {
        if (cam == null)
        {
            var c = GetComponentInChildren<Camera>(true);
            if (c != null) cam = c.gameObject;
        }
        if (firePoint == null) firePoint = this.transform;
        // Pick a random starting weapon from the cycle
        currentWeapon = (WeaponType)Random.Range(0, 4);
        // UI managers might not exist yet in Awake (spawn order). We'll also update in Start.
        ShowWeaponIndicator();
        UpdateLmbIcon();
        UpdateAbilityIcon();
    }

    void Start()
    {
        // Ensure UI gets updated after managers are spawned by the spawner
        ShowWeaponIndicator();
        UpdateLmbIcon();
        UpdateAbilityIcon();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryShoot();
        if (Input.GetKeyDown(KeyCode.Q)) CycleWeapon(); // switcher ability
        if (Input.GetMouseButtonDown(1)) TryUseAbility(); // weapon ability on RMB
    }

    public void CycleWeapon()
    {
        currentWeapon = (WeaponType)(((int)currentWeapon + 1) % 4);
        ShowWeaponIndicator();
        UpdateLmbIcon();
        UpdateAbilityIcon();
        // small switch cooldown on HUD (Q)
        if (CooldownUIManager.Instance != null) CooldownUIManager.Instance.StartCooldown(AbilityKey.One, 0.2f);
    }

    private void ShowWeaponIndicator()
    {
        // No longer show weapon name at the top; ensure it is removed if present
        if (ModifiersUIManager.Instance != null)
        {
            ModifiersUIManager.Instance.Remove("ApheliosWeapon");
        }
    }

    private void UpdateLmbIcon()
    {
        if (CooldownUIManager.Instance == null) return;
        Sprite icon = null;
        switch (currentWeapon)
        {
            case WeaponType.Sniper: icon = sniperIcon; break;
            case WeaponType.Scythe: icon = scytheIcon; break;
            case WeaponType.Orbs: icon = orbsIcon; break;
            case WeaponType.Flamethrower: icon = flamethrowerIcon; break;
        }
        if (icon != null)
        {
            CooldownUIManager.Instance.SetAbilityIcon(AbilityKey.LeftClick, icon);
        }
    }

    private void UpdateAbilityIcon()
    {
        if (CooldownUIManager.Instance == null) return;
        Sprite icon = null;
        switch (currentWeapon)
        {
            case WeaponType.Sniper: icon = sniperAbilityIcon != null ? sniperAbilityIcon : sniperIcon; break;
            case WeaponType.Scythe: icon = scytheAbilityIcon != null ? scytheAbilityIcon : scytheIcon; break;
            case WeaponType.Orbs: icon = orbsAbilityIcon != null ? orbsAbilityIcon : orbsIcon; break;
            case WeaponType.Flamethrower: icon = flamethrowerAbilityIcon != null ? flamethrowerAbilityIcon : flamethrowerIcon; break;
        }
        if (icon != null)
        {
            CooldownUIManager.Instance.SetAbilityIcon(AbilityKey.RightClick, icon);
        }
    }

    private WeaponStats GetStats()
    {
        switch (currentWeapon)
        {
            case WeaponType.Scythe: return scythe;
            case WeaponType.Orbs: return orbs;
            case WeaponType.Flamethrower: return flamethrower;
            default: return sniper;
        }
    }

    private GameObject GetProjectile()
    {
        switch (currentWeapon)
        {
            case WeaponType.Scythe: return scytheProjectile != null ? scytheProjectile : sniperProjectile;
            case WeaponType.Orbs: return orbsProjectile != null ? orbsProjectile : sniperProjectile;
            case WeaponType.Flamethrower: return flameProjectile != null ? flameProjectile : sniperProjectile;
            default: return sniperProjectile;
        }
    }

    public void TryShoot()
    {
        if (_onCooldown) return;
        var stats = GetStats();
        var prefab = GetProjectile();
        if (cam == null) return;

        // spawn pattern
        Quaternion baseRot = cam.transform.rotation;
        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        int pellets = Mathf.Max(1, stats.pellets);
        for (int i = 0; i < pellets; i++)
        {
            Quaternion rot = baseRot;
            if (pellets > 1 || stats.spreadAngle > 0f)
            {
                float half = stats.spreadAngle * 0.5f;
                float yaw = Random.Range(-half, half);
                float pitch = Random.Range(-half, half) * 0.25f; // mostly horizontal
                rot = baseRot * Quaternion.Euler(pitch, yaw, 0f);
            }
            GameObject go;
            if (prefab != null)
            {
                go = NetworkHelper.SpawnProjectile(prefab, pos, rot);
            }
            else
            {
                // Runtime fallback projectile if no prefab assigned
                go = CreateRuntimeProjectile(pos, rot);
            }
            var proj = go.GetComponent<ApheliosProjectile>();
            if (proj == null) proj = go.AddComponent<ApheliosProjectile>();
            bool applyBurn = (currentWeapon == WeaponType.Flamethrower) && burnDuration > 0f && burnDamagePerSecond > 0f;
            bool hitTwice = false;
            int bounces = 0;
            float bounceRadius = 0f;
            // Apply weapon-ability modifiers captured at fire time
            if (currentWeapon == WeaponType.Sniper && sniperEmpoweredShots > 0)
            {
                hitTwice = true;
                sniperEmpoweredShots--;
                // Remove any HUD indicator if count reaches 0
                if (sniperEmpoweredShots <= 0)
                {
                    if (ModifiersUIManager.Instance != null)
                    {
                        ModifiersUIManager.Instance.Remove("ApheliosSniperEmpower");
                    }
                }
            }
            if (currentWeapon == WeaponType.Flamethrower && _flameBounceActive)
            {
                bounces = Mathf.Max(0, flameBounceCount);
                bounceRadius = Mathf.Max(0.1f, flameBounceRadius);
            }
            proj.Init(this, currentWeapon, stats.damage, stats.projectileSpeed, stats.maxDistance,
                      stats.lifestealPercent, stats.slowPercent, stats.slowDuration,
                      applyBurn ? burnDuration : 0f, burnDamagePerSecond,
                      hitTwice, bounces, bounceRadius);
        }

        // start weapon-specific cooldown + HUD
        _onCooldown = true;
        if (CooldownUIManager.Instance != null) CooldownUIManager.Instance.StartCooldown(AbilityKey.LeftClick, stats.cooldown);
        StartCoroutine(Cooldown(stats.cooldown));
    }

    IEnumerator Cooldown(float t)
    {
        yield return new WaitForSeconds(t);
        _onCooldown = false;
    }

    IEnumerator AbilityCooldown(float t)
    {
        yield return new WaitForSeconds(t);
        _abilityOnCooldown = false;
    }

    private GameObject CreateRuntimeProjectile(Vector3 position, Quaternion rotation)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = position;
        go.transform.rotation = rotation;
        // Visual size and tint per weapon
        float scale = 0.14f;
        Color tint = Color.white;
        switch (currentWeapon)
        {
            case WeaponType.Sniper: tint = new Color(0.8f, 0.9f, 1f); scale = 0.12f; break;
            case WeaponType.Scythe: tint = new Color(0.7f, 1f, 0.7f); scale = 0.14f; break;
            case WeaponType.Orbs: tint = new Color(0.7f, 0.8f, 1f); scale = 0.16f; break;
            case WeaponType.Flamethrower: tint = new Color(1f, 0.6f, 0.2f); scale = 0.18f; break;
        }
        go.transform.localScale = Vector3.one * scale;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint;
            mr.material = mat;
        }
        // Remove collider; projectile uses raycast
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return go;
    }

    // Ability (RMB)
    public void TryUseAbility()
    {
        if (_abilityOnCooldown) return;
        float cd = 8f;
        switch (currentWeapon)
        {
            case WeaponType.Sniper:
                sniperEmpoweredShots = Mathf.Max(sniperEmpoweredShots, 1);
                if (ModifiersUIManager.Instance != null)
                {
                    ModifiersUIManager.Instance.AddOrUpdate("ApheliosSniperEmpower", sniperIcon, "Empowered Shot", -1f, sniperEmpoweredShots);
                }
                cd = sniperAbilityCooldown;
                break;

            case WeaponType.Orbs:
                StunRecentOrbsHits();
                cd = orbsAbilityCooldown;
                break;

            case WeaponType.Scythe:
                StartCoroutine(RunScytheAura());
                cd = scytheAbilityCooldown;
                break;

            case WeaponType.Flamethrower:
                // Fire a flamethrower volley where each hit causes a single reflected shot away from the player
                FireFlamethrowerAbilityVolley();
                cd = flamethrowerAbilityCooldown;
                break;
        }

        _abilityOnCooldown = true;
        if (CooldownUIManager.Instance != null)
        {
            CooldownUIManager.Instance.StartCooldown(AbilityKey.RightClick, cd);
        }
        StartCoroutine(AbilityCooldown(cd));
    }

    private void FireFlamethrowerAbilityVolley()
    {
        var stats = flamethrower; // explicit for clarity
        var prefab = flameProjectile != null ? flameProjectile : GetProjectile();
        if (cam == null) return;

        Quaternion baseRot = cam.transform.rotation;
        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        // Ability-specific overrides to make it wider and further
        int pellets = Mathf.Max(1, flameAbilityPellets > 0 ? flameAbilityPellets : stats.pellets);
        float spread = flameAbilitySpreadAngle > 0f ? flameAbilitySpreadAngle : stats.spreadAngle;
        float speed = Mathf.Max(0.1f, stats.projectileSpeed * Mathf.Max(0.01f, flameAbilitySpeedMult));
        float maxDist = Mathf.Max(0.1f, stats.maxDistance * Mathf.Max(0.01f, flameAbilityRangeMult));
        for (int i = 0; i < pellets; i++)
        {
            Quaternion rot = baseRot;
            if (pellets > 1 || spread > 0f)
            {
                float half = spread * 0.5f;
                float yaw = Random.Range(-half, half);
                float pitch = Random.Range(-half, half) * 0.25f;
                rot = baseRot * Quaternion.Euler(pitch, yaw, 0f);
            }
            GameObject go = prefab != null ? NetworkHelper.SpawnProjectile(prefab, pos, rot) : CreateRuntimeProjectile(pos, rot);
            var proj = go.GetComponent<ApheliosProjectile>();
            if (proj == null) proj = go.AddComponent<ApheliosProjectile>();
            bool applyBurn = burnDuration > 0f && burnDamagePerSecond > 0f;
            // Set bounces=1 to create exactly one reflected shot away from the player on first hit
            proj.Init(this, WeaponType.Flamethrower, stats.damage, speed, maxDist,
                      stats.lifestealPercent, stats.slowPercent, stats.slowDuration,
                      applyBurn ? burnDuration : 0f, burnDamagePerSecond,
                      false, 1, flamethrowerAbilityCooldown > 0 ? flameBounceRadius : flameBounceRadius);
        }
    }

    // Record an orbs hit (called by projectile)
    public void RecordOrbsHit(Collider c)
    {
        if (c == null) return;
        _recentOrbsHits.Add(new RecentHit { col = c, time = Time.time });
        CleanupOldOrbsHits();
    }

    private void CleanupOldOrbsHits()
    {
        float cutoff = Time.time - orbsStunWindow;
        for (int i = _recentOrbsHits.Count - 1; i >= 0; i--)
        {
            if (_recentOrbsHits[i].col == null || _recentOrbsHits[i].time < cutoff)
                _recentOrbsHits.RemoveAt(i);
        }
    }

    private void StunRecentOrbsHits()
    {
        CleanupOldOrbsHits();
        foreach (var h in _recentOrbsHits)
        {
            if (h.col == null) continue;
            
            // Try to find OrbsMarkerStatus on the hit target and stun via it
            var orbMarker = h.col.GetComponentInParent<OrbsMarkerStatus>();
            if (orbMarker == null && h.col.attachedRigidbody != null)
            {
                orbMarker = h.col.attachedRigidbody.GetComponent<OrbsMarkerStatus>();
            }
            
            if (orbMarker != null)
            {
                orbMarker.StunFromOrbs(orbsStunDuration);
            }
            else
            {
                // Fallback: if no marker (shouldn't happen), try to stun via CharacterControl
                var cc = h.col.GetComponentInParent<CharacterControl>();
                if (cc == null && h.col.attachedRigidbody != null)
                {
                    cc = h.col.attachedRigidbody.GetComponent<CharacterControl>();
                }
                if (cc != null)
                {
                    cc.Stun(orbsStunDuration);
                }
            }
        }
        _recentOrbsHits.Clear();
    }

    private IEnumerator RunScytheAura()
    {
        float end = Time.time + scytheAuraDuration;
        var selfHp = GetComponent<HealthSystem>();
        // Apply temporary 10% move speed increase while aura is active
        var cc = GetComponent<CharacterControl>();
        float originalSpeed = 0f;
        float originalRunSpeed = 0f;
        if (cc != null)
        {
            originalSpeed = cc.speed;
            originalRunSpeed = cc.runSpeed;
            cc.speed = originalSpeed * 1.10f;
            cc.runSpeed = originalRunSpeed * 1.10f;
        }
        Sprite icon = ModifiersIconLibrary.Instance != null
            ? (ModifiersIconLibrary.Instance.MOVESPEED ?? ModifiersIconLibrary.Instance.HASTE)
            : null;
        if (ModifiersUIManager.Instance != null)
        {
            ModifiersUIManager.Instance.AddOrUpdate("ApheliosScytheHaste", icon, "+10% Move Speed", scytheAuraDuration, 0);
        }
        while (Time.time < end)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, scytheAuraRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null) continue;
                if (col.transform.IsChildOf(transform)) continue; // ignore self
                if (col.CompareTag("Player") || col.CompareTag("Ally")) continue; // Skip allies
                if (!col.CompareTag("Enemy")) continue; // Only damage enemies
                var hp = col.GetComponent<HealthSystem>();
                if (hp == null) continue;
                hp.TakeDamage(scytheAuraDamagePerTick);
                if (selfHp != null && scythe.lifestealPercent > 0f)
                {
                    selfHp.Heal(scytheAuraDamagePerTick * scythe.lifestealPercent);
                }
            }
            yield return new WaitForSeconds(Mathf.Max(0.05f, scytheAuraTickInterval));
        }
        // Revert move speed and remove haste indicator
        if (cc != null)
        {
            cc.speed = originalSpeed;
            cc.runSpeed = originalRunSpeed;
        }
        if (ModifiersUIManager.Instance != null)
        {
            ModifiersUIManager.Instance.Remove("ApheliosScytheHaste");
        }
    }

    private IEnumerator EnableFlameBounceWindow()
    {
        _flameBounceActive = true;
        yield return new WaitForSeconds(flamethrowerAbilityCooldown > 0 ? Mathf.Min(flameBounceWindow, flamethrowerAbilityCooldown) : flameBounceWindow);
        _flameBounceActive = false;
    }
}
