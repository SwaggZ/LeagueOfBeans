using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LuxController : MonoBehaviour
{
    [Header("References")]
    public GameObject cam;
    public Transform firePoint;

    [Header("Projectile Prefabs")]
    public GameObject lightOrbPrefab;
    public GameObject wandPrefab;
    public GameObject stunOrbPrefab;
    public GameObject carpetThrowablePrefab;

    [Header("HUD Icons")]
    public Sprite lightOrbIcon;
    public Sprite wandForwardIcon;
    public Sprite wandReturnIcon;
    public Sprite stunOrbIcon;
    public Sprite carpetThrowIcon;
    public Sprite carpetDetonateIcon;

    [Header("Light Orb (LMB)")]
    public float lightOrbDamage = 40f;
    public float lightOrbSpeed = 45f;
    public float lightOrbMaxDistance = 50f;
    public float lightOrbCooldown = 0.5f;
    private bool _lightOrbOnCooldown = false;

    [Header("Wand Shield (RMB)")]
    public float wandShieldAmount = 60f;
    public float wandShieldDuration = 3f;
    public float wandSpeed = 20f;
    public float wandReturnSpeed = 25f;
    public float wandMaxDistance = 35f;
    public float wandCooldown = 10f;
    private bool _wandOnCooldown = false;
    private LuxWand _activeWand;

    [Header("Stun Orb (Q)")]
    public float stunOrbDamage = 30f;
    public float stunOrbSpeed = 30f;
    public float stunOrbMaxDistance = 40f;
    public float stunDuration = 1.5f;
    public int maxStunTargets = 2;
    public float stunOrbCooldown = 12f;
    private bool _stunOrbOnCooldown = false;

    [Header("Light Carpet (E)")]
    public float carpetDamage = 80f;
    public float carpetSlowAmount = 0.4f; // 40% slow
    public float carpetRadius = 5f;
    public float carpetDuration = 5f;
    public float carpetThrowForce = 15f;
    public float carpetCooldown = 14f;
    private bool _carpetOnCooldown = false;
    private LuxCarpet _activeCarpet;

    private CooldownUIManager _cooldownUi;

    void Awake()
    {
        if (cam == null)
        {
            var c = GetComponentInChildren<Camera>(true);
            if (c != null) cam = c.gameObject;
        }
        if (firePoint == null) firePoint = transform;
        _cooldownUi = FindObjectOfType<CooldownUIManager>(true);
    }

    void Start()
    {
        UpdateAbilityIcons();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryLightOrb();
        if (Input.GetMouseButtonDown(1)) TryWand();
        if (Input.GetKeyDown(KeyCode.Q)) TryStunOrb();
        if (Input.GetKeyDown(KeyCode.E)) TryCarpet();
    }

    private void UpdateAbilityIcons()
    {
        var cooldownUi = ResolveCooldownUi();
        if (cooldownUi == null) return;
        if (lightOrbIcon != null) cooldownUi.SetAbilityIcon(AbilityKey.LeftClick, lightOrbIcon);
        if (wandForwardIcon != null) cooldownUi.SetAbilityIcon(AbilityKey.RightClick, wandForwardIcon);
        if (stunOrbIcon != null) cooldownUi.SetAbilityIcon(AbilityKey.One, stunOrbIcon);
        if (carpetThrowIcon != null) cooldownUi.SetAbilityIcon(AbilityKey.Two, carpetThrowIcon);
    }

    #region LMB - Light Orb (Damage Only)
    public void TryLightOrb()
    {
        if (_lightOrbOnCooldown) return;
        if (cam == null) return;

        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        Quaternion rot = cam.transform.rotation;

        GameObject go = lightOrbPrefab != null
            ? NetworkHelper.SpawnProjectile(lightOrbPrefab, pos, rot)
            : CreateRuntimeLightOrb(pos, rot);

        var orb = go.GetComponent<LuxLightOrb>();
        if (orb == null) orb = go.AddComponent<LuxLightOrb>();
        orb.Init(lightOrbDamage, lightOrbSpeed, lightOrbMaxDistance);

        _lightOrbOnCooldown = true;
        StartCoroutine(LightOrbCooldown());
        var cooldownUi = ResolveCooldownUi();
        if (cooldownUi != null)
            cooldownUi.StartCooldown(AbilityKey.LeftClick, lightOrbCooldown);
    }

    IEnumerator LightOrbCooldown()
    {
        yield return new WaitForSeconds(lightOrbCooldown);
        _lightOrbOnCooldown = false;
    }
    #endregion

    #region RMB - Wand Shield (Forward and Back)
    public void TryWand()
    {
        if (_wandOnCooldown) return;
        if (cam == null) return;

        Vector3 pos = firePoint != null ? firePoint.position : transform.position;

        GameObject go = wandPrefab != null
            ? NetworkHelper.SpawnProjectile(wandPrefab, pos, Quaternion.identity)
            : CreateRuntimeWand(pos);

        var wand = go.GetComponent<LuxWand>();
        if (wand == null) wand = go.AddComponent<LuxWand>();
        wand.Init(this, transform, cam.transform, wandShieldAmount, wandShieldDuration, wandSpeed, wandReturnSpeed, wandMaxDistance);
        _activeWand = wand;

        // Set forward icon
        var cooldownUi = ResolveCooldownUi();
        if (wandForwardIcon != null && cooldownUi != null)
            cooldownUi.SetAbilityIcon(AbilityKey.RightClick, wandForwardIcon);

        _wandOnCooldown = true;
        StartCoroutine(WandCooldown());
        cooldownUi = ResolveCooldownUi();
        if (cooldownUi != null)
            cooldownUi.StartCooldown(AbilityKey.RightClick, wandCooldown);
    }

    public void OnWandReturning()
    {
        // Switch to return icon
        var cooldownUi = ResolveCooldownUi();
        if (wandReturnIcon != null && cooldownUi != null)
            cooldownUi.SetAbilityIcon(AbilityKey.RightClick, wandReturnIcon);
    }

    public void OnWandComplete()
    {
        _activeWand = null;
        // Reset to forward icon for next use
        var cooldownUi = ResolveCooldownUi();
        if (wandForwardIcon != null && cooldownUi != null)
            cooldownUi.SetAbilityIcon(AbilityKey.RightClick, wandForwardIcon);
    }

    IEnumerator WandCooldown()
    {
        yield return new WaitForSeconds(wandCooldown);
        _wandOnCooldown = false;
    }
    #endregion

    #region Q - Stun Orb (Stuns first 2 enemies)
    public void TryStunOrb()
    {
        if (_stunOrbOnCooldown) return;
        if (cam == null) return;

        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        Quaternion rot = cam.transform.rotation;

        GameObject go = stunOrbPrefab != null
            ? NetworkHelper.SpawnProjectile(stunOrbPrefab, pos, rot)
            : CreateRuntimeStunOrb(pos, rot);

        var orb = go.GetComponent<LuxStunOrb>();
        if (orb == null) orb = go.AddComponent<LuxStunOrb>();
        orb.Init(stunOrbDamage, stunOrbSpeed, stunOrbMaxDistance, stunDuration, maxStunTargets);

        _stunOrbOnCooldown = true;
        StartCoroutine(StunOrbCooldown());
        var cooldownUi = ResolveCooldownUi();
        if (cooldownUi != null)
            cooldownUi.StartCooldown(AbilityKey.One, stunOrbCooldown);
    }

    IEnumerator StunOrbCooldown()
    {
        yield return new WaitForSeconds(stunOrbCooldown);
        _stunOrbOnCooldown = false;
    }
    #endregion

    #region E - Light Carpet (Throw, Slow, Recast to Detonate)
    public void TryCarpet()
    {
        // If carpet is active, detonate it
        if (_activeCarpet != null)
        {
            _activeCarpet.Detonate();
            _activeCarpet = null;

            // Reset icon to throw
            var cooldownUi = ResolveCooldownUi();
            if (carpetThrowIcon != null && cooldownUi != null)
                cooldownUi.SetAbilityIcon(AbilityKey.Two, carpetThrowIcon);
            return;
        }

        if (_carpetOnCooldown) return;
        if (cam == null) return;

        // Throw carpet (like Jhin's mine)
        Vector3 spawnPos = cam.transform.position;
        Quaternion spawnRot = cam.transform.rotation;

        GameObject go = carpetThrowablePrefab != null
            ? NetworkHelper.SpawnProjectile(carpetThrowablePrefab, spawnPos, spawnRot)
            : CreateRuntimeCarpetThrowable(spawnPos, spawnRot);

        var throwable = go.GetComponent<LuxCarpetThrowable>();
        if (throwable == null) throwable = go.AddComponent<LuxCarpetThrowable>();
        throwable.Init(this, carpetDamage, carpetSlowAmount, carpetRadius, carpetDuration, carpetThrowForce, cam.transform.forward);

        // Switch to detonate icon
        var cooldownUi2 = ResolveCooldownUi();
        if (carpetDetonateIcon != null && cooldownUi2 != null)
            cooldownUi2.SetAbilityIcon(AbilityKey.Two, carpetDetonateIcon);
    }

    public void OnCarpetCreated(LuxCarpet carpet)
    {
        _activeCarpet = carpet;
    }

    public void OnCarpetDestroyed()
    {
        _activeCarpet = null;
        
        // Reset icon and start cooldown
        var cooldownUi = ResolveCooldownUi();
        if (carpetThrowIcon != null && cooldownUi != null)
            cooldownUi.SetAbilityIcon(AbilityKey.Two, carpetThrowIcon);

        _carpetOnCooldown = true;
        StartCoroutine(CarpetCooldown());
        cooldownUi = ResolveCooldownUi();
        if (cooldownUi != null)
            cooldownUi.StartCooldown(AbilityKey.Two, carpetCooldown);
    }

    private CooldownUIManager ResolveCooldownUi()
    {
        if (_cooldownUi == null)
        {
            _cooldownUi = FindObjectOfType<CooldownUIManager>(true);
        }
        return _cooldownUi;
    }

    IEnumerator CarpetCooldown()
    {
        yield return new WaitForSeconds(carpetCooldown);
        _carpetOnCooldown = false;
    }
    #endregion

    #region Runtime Prefab Creation
    private GameObject CreateRuntimeLightOrb(Vector3 pos, Quaternion rot)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "LuxLightOrb";
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = Vector3.one * 0.3f;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 0.95f, 0.6f); // Light yellow
            mat.SetFloat("_Smoothness", 1f);
            mr.material = mat;
        }

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        return go;
    }

    private GameObject CreateRuntimeWand(Vector3 pos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "LuxWand";
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.15f, 0.6f, 0.15f);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.9f, 0.8f, 1f); // Light purple/pink
            mat.SetFloat("_Smoothness", 1f);
            mr.material = mat;
        }

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        return go;
    }

    private GameObject CreateRuntimeStunOrb(Vector3 pos, Quaternion rot)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "LuxStunOrb";
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = Vector3.one * 0.4f;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 0.8f, 0.9f); // Pink
            mat.SetFloat("_Smoothness", 1f);
            mr.material = mat;
        }

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        return go;
    }

    private GameObject CreateRuntimeCarpetThrowable(Vector3 pos, Quaternion rot)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "LuxCarpetThrowable";
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = Vector3.one * 0.35f;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 1f, 0.5f); // Bright yellow
            mat.SetFloat("_Smoothness", 1f);
            mr.material = mat;
        }

        var rb = go.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        return go;
    }
    #endregion
}
