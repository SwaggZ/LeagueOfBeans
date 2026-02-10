# Mirror Networking Migration Guide

This document outlines the changes made to prepare the codebase for Mirror networking, and what steps remain when Mirror is actually installed.

## What's Already Done (Works Without Mirror)

### 1. NetworkHelper Class (`Assets/Networking/NetworkHelper.cs`)
A wrapper around common network operations:
- `NetworkHelper.SpawnProjectile()` - Currently uses `Instantiate()`, will become `NetworkServer.Spawn()`
- `NetworkHelper.Despawn()` - Currently uses `Destroy()`, will become `NetworkServer.Destroy()`
- `NetworkHelper.IsLocalPlayer()` - Currently checks tag, will check `NetworkIdentity.isLocalPlayer`
- `NetworkHelper.HasAuthority()` - Currently returns true for player, will check `NetworkIdentity.hasAuthority`
- `NetworkHelper.IsServer` / `NetworkHelper.IsClient` - Currently always true, will use Mirror properties

### 2. LocalPlayerRef Class (`Assets/Networking/LocalPlayerRef.cs`)
Centralized player reference management:
- Replaces `FindGameObjectWithTag("Player")` calls
- Supports multiple players (for multiplayer)
- Call `LocalPlayerRef.Register()` when player spawns
- Call `LocalPlayerRef.GetLocalPlayerWithFallback()` to get player reference

### 3. PlayerRegistration Component (`Assets/Networking/PlayerRegistration.cs`)
Add this to player prefabs to auto-register with LocalPlayerRef.
- Currently uses `Awake()`/`OnDestroy()`
- Will use `OnStartLocalPlayer()`/`OnStopClient()` with Mirror

### 4. NetworkProjectile Base Class (`Assets/Networking/NetworkProjectile.cs`)
Base class for projectiles that need networking:
- Override `Initialize()` for setup
- Use `DestroySelf()` instead of `Destroy()`
- Built-in ally/enemy detection helpers

### 5. DamageDealer Helper (`Assets/Networking/DamageDealer.cs`)
Centralized damage logic:
- `DamageDealer.DealDamage()` - Apply damage through one point
- `DamageDealer.DealAoeDamage()` - AOE damage in radius
- `DamageDealer.ValidateDamage()` - Will be used for server validation

### 6. InputHandler Base Class (`Assets/Networking/InputHandler.cs`)
Pattern for input handling:
- Separates input reading from action execution
- `AbilityInput` struct for serializing ability data to Commands

## Scripts Already Updated

The following scripts now use NetworkHelper instead of direct Instantiate/Destroy:

### Champions/jhin/
- `JhinController.cs` - All projectile spawning
- `JhinProjectile.cs` - Destroy on hit/timeout

### Champions/lux/
- `LuxController.cs` - All projectile spawning

### Champions/aphelios/
- `ApheliosController.cs` - All projectile spawning
- `ApheliosProjectile.cs` - Destroy and wave spawning

### Champions/ahri/
- `AutoMovement.cs` - Destroy on hit/timeout
- `OrbMovement.cs` - Uses LocalPlayerRef
- `FoxFireContainer.cs` - Uses LocalPlayerRef

### Champions/ashe/
- `asheL.cs`, `asheQ.cs`, `asheE.cs`, `asheR.cs` - All projectile spawning

### Champions/caitlyn/
- `caitlynL.cs`, `caitlynQ.cs`, `caitlynE.cs`, `caitlynW.cs` - Projectile spawning
- `ThrowableCollisionHandler.cs` - Trap spawning and destroy

### Champions/galio/
- `galioQ.cs` - Prefab spawning and destroy
- `galioW.cs` - Tornado spawning
- `galioRightClick.cs` - VFX spawning

### playerControllerscritps/
- `ReactiveCrosshair.cs` - Uses LocalPlayerRef

---

## When Mirror is Installed - Migration Steps

### Step 1: Update NetworkHelper.cs
```csharp
// Change SpawnProjectile to:
public static GameObject SpawnProjectile(GameObject prefab, Vector3 position, Quaternion rotation)
{
    if (!NetworkServer.active) 
    {
        Debug.LogWarning("SpawnProjectile called on client - use Command");
        return null;
    }
    
    GameObject go = Object.Instantiate(prefab, position, rotation);
    NetworkServer.Spawn(go);
    return go;
}

// Change Despawn to:
public static void Despawn(GameObject go)
{
    if (!NetworkServer.active) return;
    NetworkServer.Destroy(go);
}

// Update IsLocalPlayer:
public static bool IsLocalPlayer(GameObject go)
{
    var netId = go?.GetComponent<NetworkIdentity>();
    return netId != null && netId.isLocalPlayer;
}

// Update properties:
public static bool IsServer => NetworkServer.active;
public static bool IsClient => NetworkClient.active;
```

### Step 2: Update PlayerRegistration to NetworkBehaviour
```csharp
public class PlayerRegistration : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        LocalPlayerRef.Register(gameObject, true);
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer)
            LocalPlayerRef.Register(gameObject, false);
    }
    
    public override void OnStopClient()
    {
        base.OnStopClient();
        LocalPlayerRef.Unregister(gameObject);
    }
}
```

### Step 3: Convert Controllers to NetworkBehaviour

For each controller (JhinController, LuxController, etc.):

1. Change base class: `public class JhinController : NetworkBehaviour`
2. Add `[Command]` to ability methods that spawn projectiles
3. Move input reading to client-only section
4. Keep cooldown tracking on server

Example pattern:
```csharp
public class JhinController : NetworkBehaviour
{
    void Update()
    {
        if (!isLocalPlayer) return; // Only local player reads input
        
        if (Input.GetMouseButtonDown(0)) CmdTryShoot();
        // etc...
    }
    
    [Command]
    void CmdTryShoot()
    {
        // Server validates and spawns
        if (_fireOnCooldown) return;
        
        Vector3 pos = firePoint.position;
        Quaternion rot = transform.rotation; // Use synced rotation
        
        GameObject go = NetworkHelper.SpawnProjectile(bulletPrefab, pos, rot);
        // Initialize projectile...
        
        _fireOnCooldown = true;
        StartCoroutine(FireCooldown(cooldown));
    }
}
```

### Step 4: Add NetworkIdentity to Prefabs

Add these components to all spawned prefabs:
- `NetworkIdentity`
- `NetworkTransform` (for position sync)

### Step 5: Configure Mirror NetworkManager

1. Add `NetworkManager` to scene
2. Register all player prefabs
3. Register all spawnable prefabs (projectiles, effects)
4. Set up spawn points

### Step 6: Handle Health/Damage Sync

Character health should be a SyncVar:
```csharp
[SyncVar(hook = nameof(OnHealthChanged))]
public float health;

void OnHealthChanged(float oldHealth, float newHealth)
{
    // Update health bar UI
}

[Server]
public void TakeDamage(float amount)
{
    health -= amount;
    if (health <= 0) Die();
}
```

---

## Testing Checklist

After Mirror migration, test:
- [ ] Player spawns correctly on all clients
- [ ] Abilities fire and projectiles visible to all clients
- [ ] Damage is applied correctly (server-authoritative)
- [ ] Cooldowns work for all players
- [ ] Health bars sync across clients
- [ ] Status effects (stun, slow) sync correctly
- [ ] Player can't damage allies
- [ ] Reconnection works

## Notes

- Keep `ModifiersUIManager` and `CooldownUIManager` as singletons - they're client-only UI
- `ModifierUtils` can remain static - just ensure damage goes through server
- Consider adding prediction for responsive gameplay (client-side projectile with server validation)
