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

    [Header("Burn (Flamethrower)")]
    public float burnDuration = 3f; // seconds
    public float burnDamagePerSecond = 2f;

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
    }

    void Start()
    {
        // Ensure UI gets updated after managers are spawned by the spawner
        ShowWeaponIndicator();
        UpdateLmbIcon();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryShoot();
        if (Input.GetKeyDown(KeyCode.Q)) CycleWeapon(); // switcher ability
    }

    public void CycleWeapon()
    {
        currentWeapon = (WeaponType)(((int)currentWeapon + 1) % 4);
        ShowWeaponIndicator();
        UpdateLmbIcon();
        // small switch cooldown on HUD (Q)
        if (CooldownUIManager.Instance != null) CooldownUIManager.Instance.StartCooldown(AbilityKey.One, 0.2f);
    }

    private void ShowWeaponIndicator()
    {
        if (ModifiersUIManager.Instance != null)
        {
            string label = currentWeapon.ToString();
            ModifiersUIManager.Instance.AddOrUpdate("ApheliosWeapon", null, label, -1f, 0);
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
                go = Instantiate(prefab, pos, rot);
            }
            else
            {
                // Runtime fallback projectile if no prefab assigned
                go = CreateRuntimeProjectile(pos, rot);
            }
            var proj = go.GetComponent<ApheliosProjectile>();
            if (proj == null) proj = go.AddComponent<ApheliosProjectile>();
            bool applyBurn = (currentWeapon == WeaponType.Flamethrower) && burnDuration > 0f && burnDamagePerSecond > 0f;
            proj.Init(this, stats.damage, stats.projectileSpeed, stats.maxDistance, stats.lifestealPercent, stats.slowPercent, stats.slowDuration, applyBurn ? burnDuration : 0f, burnDamagePerSecond);
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
}
