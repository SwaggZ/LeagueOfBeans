using UnityEngine;
using FishNet.Object;

/// <summary>
/// NetworkBehaviour component for player prefabs that manages ownership-based control.
/// Attaches to player prefabs to enable/disable input and camera for the owning client only.
/// 
/// Attach this to your player prefab root alongside:
/// - NetworkObject (required by FishNet)
/// - CharacterControl (movement script)
/// - Character-specific controller (LuxController, AhriController, etc.)
/// 
/// This script will:
/// 1. Enable CharacterControl and character controller ONLY for the owning client
/// 2. Enable the player camera ONLY for the owning client
/// 3. Register the local player with LocalPlayerRef for easy access
/// 4. Disable input processing on non-owned instances (prevents gliding/unwanted movement)
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Components to Control")]
    [Tooltip("Movement controller (CharacterControl). Auto-detected if null.")]
    public CharacterControl movementController;
    
    [Tooltip("Character-specific ability controller (LuxController, AhriController, etc). Auto-detected if null.")]
    public MonoBehaviour abilityController;
    
    [Tooltip("Player camera. Auto-detected if null.")]
    public Camera playerCamera;
    
    [Tooltip("Camera GameObject. Auto-detected if null.")]
    public GameObject cameraObject;

    private void Awake()
    {
        // Auto-detect components if not assigned
        if (movementController == null)
            movementController = GetComponent<CharacterControl>();
        
        if (abilityController == null)
        {
            // Try to find character-specific controllers (only those that exist)
            var lux = GetComponent<LuxController>();
            var aphelios = GetComponent<ApheliosController>();
            var jhin = GetComponent<JhinController>();
            
            abilityController = lux ?? aphelios ?? (MonoBehaviour)jhin;
        }
        
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);
        
        if (cameraObject == null && playerCamera != null)
            cameraObject = playerCamera.gameObject;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Only enable control and camera for the owning client
        bool isOwner = base.IsOwner;
        
        Debug.Log($"[NetworkPlayerController] {gameObject.name} started on client. IsOwner={isOwner}, IsServer={base.IsServerStarted}");
        
        // Enable/disable movement controller
        if (movementController != null)
        {
            movementController.enabled = isOwner;
            Debug.Log($"[NetworkPlayerController] CharacterControl.enabled = {isOwner}");
        }
        
        // Enable/disable ability controller
        if (abilityController != null)
        {
            abilityController.enabled = isOwner;
            Debug.Log($"[NetworkPlayerController] {abilityController.GetType().Name}.enabled = {isOwner}");
        }
        
        // Enable/disable camera
        if (playerCamera != null)
        {
            playerCamera.enabled = isOwner;
            Debug.Log($"[NetworkPlayerController] Camera.enabled = {isOwner}");
        }
        
        if (cameraObject != null)
        {
            cameraObject.SetActive(isOwner);
            Debug.Log($"[NetworkPlayerController] Camera GameObject.SetActive = {isOwner}");
        }
        
        // Register with LocalPlayerRef if this is the local player
        if (isOwner)
        {
            LocalPlayerRef.Register(gameObject, true);
            Debug.Log($"[NetworkPlayerController] Registered {gameObject.name} as local player");
            
            // Create UI managers for local player only
            EnsureCooldownUIManager();
            EnsureModifiersUIManager();
            EnsureCrosshair();
        }
        else
        {
            LocalPlayerRef.Register(gameObject, false);
        }
    }
    
    private void EnsureCooldownUIManager()
    {
        var cooldownUi = FindObjectOfType<CooldownUIManager>(true);
        if (cooldownUi == null)
        {
            Debug.Log("[NetworkPlayerController] Creating CooldownUIManager for local player");
            new GameObject("CooldownUIManager").AddComponent<CooldownUIManager>();
            
            // Force visibility update after a short delay to ensure SelectionCanvas is hidden
            StartCoroutine(ForceHUDVisibilityAfterDelay());
        }
        else
        {
            Debug.Log("[NetworkPlayerController] CooldownUIManager already exists");
            StartCoroutine(ForceHUDVisibilityAfterDelay());
        }
    }
    
    private System.Collections.IEnumerator ForceHUDVisibilityAfterDelay()
    {
        // Wait for SelectionCanvas to be disabled
        yield return new WaitForSeconds(0.1f);
        
        var cooldownUi = FindObjectOfType<CooldownUIManager>(true);
        if (cooldownUi != null)
        {
            Debug.Log("[NetworkPlayerController] Force-showing CooldownUIManager after spawn delay");
            cooldownUi.ForceShowHUD();
        }
        
        var modifiersUi = FindObjectOfType<ModifiersUIManager>(true);
        if (modifiersUi != null)
        {
            Debug.Log("[NetworkPlayerController] Force-showing ModifiersUIManager after spawn delay");
            modifiersUi.ForceShowHUD();
        }
    }
    
    private void EnsureModifiersUIManager()
    {
        var modifiersUi = FindObjectOfType<ModifiersUIManager>(true);
        if (modifiersUi == null)
        {
            Debug.Log("[NetworkPlayerController] Creating ModifiersUIManager for local player");
            new GameObject("ModifiersUIManager").AddComponent<ModifiersUIManager>();
        }
        
        // Ensure icon library exists
        var iconLibrary = FindObjectOfType<ModifiersIconLibrary>(true);
        if (iconLibrary == null)
        {
            Debug.LogWarning("[NetworkPlayerController] No ModifiersIconLibrary found. Creating empty one.");
            new GameObject("ModifiersIconLibrary").AddComponent<ModifiersIconLibrary>();
        }
    }
    
    private void EnsureCrosshair()
    {
        var crosshair = FindObjectOfType<ReactiveCrosshair>(true);
        if (crosshair == null)
        {
            Debug.Log("[NetworkPlayerController] Creating ReactiveCrosshair for local player");
            var crossGO = new GameObject("ReactiveCrosshairHost");
            crossGO.AddComponent<ReactiveCrosshair>();
        }
        else
        {
            Debug.Log("[NetworkPlayerController] ReactiveCrosshair already exists");
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        
        // Unregister from LocalPlayerRef
        LocalPlayerRef.Unregister(gameObject);
        Debug.Log($"[NetworkPlayerController] Unregistered {gameObject.name}");
    }
}
