using UnityEngine;

/// <summary>
/// Base class for input handling that's multiplayer-ready.
/// Separates input reading (client-only) from action execution (can be server-authoritative).
/// 
/// CURRENT BEHAVIOR (no Mirror):
/// - ReadInput() is called in Update
/// - If input detected, ExecuteAction() is called immediately
/// 
/// MIRROR MIGRATION:
/// - ReadInput() only runs on local player (if (isLocalPlayer))
/// - ExecuteAction() becomes a [Command] that runs on server
/// - Server validates and executes, then [ClientRpc] for effects
/// 
/// Example conversion pattern:
/// void Update()
/// {
///     if (!isLocalPlayer) return; // Only local player reads input
///     ReadInput();
/// }
/// 
/// [Command]
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
    /// <summary>
    /// Called to check for input. Only the owning client should read input.
    /// MIRROR: Wrap in if (isLocalPlayer) check
    /// </summary>
    protected abstract void ReadInput();
    
    /// <summary>
    /// Called to execute the action. This will become a [Command] in Mirror.
    /// </summary>
    protected abstract void ExecuteAction();
    
    protected virtual void Update()
    {
        // MIRROR: Add this check:
        // if (!isLocalPlayer) return;
        
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
    public uint targetNetId; // MIRROR: NetworkIdentity.netId of target
    
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
