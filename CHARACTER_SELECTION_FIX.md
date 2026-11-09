# Character Selection Stuck Issue - Fix Summary

## Problem
With two players connected, both could pick a character and click START, but nobody spawned. Everyone stayed on the selection screen.

## Root Cause
`NetworkGameSession` is a plain `MonoBehaviour` that lives under the `NetworkManager`. FishNet does not allow `NetworkBehaviour`s on the NetworkManager object. Its methods named `...ServerRpc` (`SubmitSelectionServerRpc`, `SetReadyServerRpc`, `RequestRespawnServerRpc`) are **ordinary C# methods**, not FishNet RPCs.

When a remote client called them, the code ran locally, saw `_isServer == false` and returned. The server never learned about the client's selection or ready state, so `StartGameplay()` never ran.

## Current Solution: FishNet Broadcasts
Clients send FishNet **broadcasts**, which need no `NetworkObject`, and the server handles them in `NetworkGameSession`.

| Broadcast | Sent when | Server handler → logic |
|-----------|-----------|------------------------|
| `SelectionBroadcast { CharacterId }` | Client clicks START | `OnSelectionBroadcast` → `SubmitSelectionServerRpc` |
| `ReadyBroadcast { IsReady }` | Client clicks START (initial spawn) | `OnReadyBroadcast` → `SetReadyServerRpc` |
| `RespawnBroadcast` | Client picks a character after dying | `OnRespawnBroadcast` → `RequestRespawnServerRpc` |

- The structs and handlers live in `Assets/Networking/FishNet/NetworkGameSession.cs`. The handlers are registered when the server starts and unregistered when it stops.
- `CharacterSelection.StartGame()` calls the server methods directly on the host and sends broadcasts on a remote client.
- `NetworkGameSession.ClientSubmitSelection()` and `ClientSetReady()` follow the same rule, so they are safe to call from UI such as `LobbyReadyPanel`.

> **History:** An earlier fix used a separate `NetworkSessionBridge` NetworkBehaviour with real `[ServerRpc]`s. The broadcast approach replaced it, so the bridge script and its Menu.unity object have been removed.

## Flow
```
Client → CharacterSelection.StartGame()
      → ClientManager.Broadcast(SelectionBroadcast) + Broadcast(ReadyBroadcast)
Server → OnSelectionBroadcast / OnReadyBroadcast
      → SubmitSelectionServerRpc / SetReadyServerRpc (now running on the server)
      → player is spawned for that connection
```

## Expected Console Logs
Client:
```
[Selection] We are CLIENT - using FishNet Broadcasts
[Selection] INITIAL SPAWN: Submitting selection 'Ahri' and marking ready
[Selection] Selection and ready submitted via broadcasts
```
Server:
```
[NetworkGameSession] Broadcast handlers registered!
[NetworkGameSession] Received SelectionBroadcast from client 1: Ahri
[NetworkGameSession] Received ReadyBroadcast from client 1: ready=True
[NetworkGameSession] Client 1 is ready with selection 'Ahri'. Spawning immediately...
```

## Troubleshooting
- **No "Broadcast handlers registered!" on the server:** `NetworkGameSession` isn't under the NetworkManager, or the server never started. Run `Tools → Setup Menu Networking (Menu Scene)` in Menu.unity.
- **Client logs show broadcasts sent but the server logs nothing:** the client isn't actually connected (check authentication/password), or the server and client are on different builds.
- **"Could not find NetworkGameSession":** run `Tools → Check Network Setup Integrity`.

## Related Files
- `Assets/Networking/FishNet/NetworkGameSession.cs`: broadcast structs, handlers, session logic
- `Assets/CharacterSelection.cs`: selection UI and START handling
- `Assets/Editor/SetupMenuNetworking.cs`: automated Menu scene setup
- `Assets/Editor/NetworkingDiagnostics.cs`: setup validation
