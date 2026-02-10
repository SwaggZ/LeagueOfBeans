using UnityEngine;

/// <summary>
/// Attach this to player prefabs to auto-register with LocalPlayerRef.
/// This component replaces the need for FindGameObjectWithTag("Player").
/// 
/// MIRROR MIGRATION:
/// - Change base class to NetworkBehaviour
/// - Override OnStartLocalPlayer() to register local player
/// - Override OnStartClient() to register remote players
/// - Override OnStopClient() to unregister
/// 
/// Example Mirror conversion:
/// public class PlayerRegistration : NetworkBehaviour
/// {
///     public override void OnStartLocalPlayer() { LocalPlayerRef.Register(gameObject, true); }
///     public override void OnStartClient() { LocalPlayerRef.Register(gameObject, false); }
///     public override void OnStopClient() { LocalPlayerRef.Unregister(gameObject); }
/// }
/// </summary>
public class PlayerRegistration : MonoBehaviour
{
    [Tooltip("Set true for the local (controlled) player. In multiplayer, this is set automatically by Mirror.")]
    public bool isLocalPlayer = true;
    
    void Awake()
    {
        // Register this player
        LocalPlayerRef.Register(gameObject, isLocalPlayer);
    }
    
    void OnDestroy()
    {
        // Unregister when destroyed
        LocalPlayerRef.Unregister(gameObject);
    }
    
    // MIRROR: These methods will replace Awake/OnDestroy:
    //
    // public override void OnStartLocalPlayer()
    // {
    //     base.OnStartLocalPlayer();
    //     isLocalPlayer = true;
    //     LocalPlayerRef.Register(gameObject, true);
    // }
    //
    // public override void OnStartClient()
    // {
    //     base.OnStartClient();
    //     if (!isLocalPlayer) // remote players
    //     {
    //         LocalPlayerRef.Register(gameObject, false);
    //     }
    // }
    //
    // public override void OnStopClient()
    // {
    //     base.OnStopClient();
    //     LocalPlayerRef.Unregister(gameObject);
    // }
}
