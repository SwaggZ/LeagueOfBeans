using UnityEngine;

/// <summary>
/// Base class for input handling that's multiplayer-ready.
/// Separates input reading (client-only) from action execution (can be server-authoritative).
/// 
/// CURRENT BEHAVIOR (no Mirror):
/// - ReadInput() is called in Update
/// - If input detected, ExecuteAction() is called immediately
/// 
/// FISHNET MIGRATION:
/// - ReadInput() only runs on local player (if (IsOwner))
/// - ExecuteAction() becomes a [ServerRpc] that runs on server
/// - Server validates and executes, then [ObserversRpc] for effects
/// 
/// Example conversion pattern:
/// void Update()
/// {
///     if (!isLocalPlayer) return; // Only local player reads input
///     ReadInput();
/// }
/// 
/// [ServerRpc]
/// void CmdExecuteAction(ActionData data)
/// {
///     // Server validates and executes
///     if (!ValidateAction(data)) return;
///     ServerExecuteAction(data);
///     RpcShowEffects(data); // All clients see effects
/// }
/// </summary>
public abstract class InputHandler : MonoBehaviour
{
    private IInputAuthority _authority;

    protected virtual void Awake()
    {
        _authority = GetComponent<IInputAuthority>();
    }

    /// <summary>
    /// Called to check for input. Only the owning client should read input.
    /// FISHNET: Wrap in if (IsOwner) check
    /// </summary>
    protected abstract void ReadInput();
    
    /// <summary>
    /// Called to execute the action. This will become a [ServerRpc] in FishNet.
    /// </summary>
    protected abstract void ExecuteAction();
    
    protected virtual void Update()
    {
        if (_authority != null && !_authority.HasInputAuthority) return;

        ReadInput();
    }
}

/// <summary>
/// Stores ability input state in a network-friendly format.
/// Can be passed to Commands for server validation.
/// </summary>
[System.Serializable]
public struct AbilityInput
{
    public Vector3 aimDirection;
    public Vector3 targetPosition;
    public float timestamp;
    
    // For abilities that target an entity
    public uint targetNetId; // FISHNET: NetworkObject.ObjectId of target
    
    public static AbilityInput Create(Transform playerTransform, Camera cam)
    {
        return new AbilityInput
        {
            aimDirection = playerTransform.forward,
            targetPosition = GetMouseWorldPosition(cam, playerTransform.position),
            timestamp = Time.time
        };
    }
    
    public static AbilityInput Create(Transform playerTransform, Vector3 direction)
    {
        return new AbilityInput
        {
            aimDirection = direction,
            targetPosition = playerTransform.position + direction * 10f,
            timestamp = Time.time
        };
    }
    
    private static Vector3 GetMouseWorldPosition(Camera cam, Vector3 fallback)
    {
        if (cam == null) return fallback;
        
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return hit.point;
        }
        return fallback + cam.transform.forward * 20f;
    }
}
