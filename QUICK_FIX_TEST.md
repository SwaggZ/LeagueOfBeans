# Quick Fix Verification Guide

## What Was Fixed
Players got stuck on the character selection screen when two players connected. Client selections now reach the server through FishNet broadcasts. See `CHARACTER_SELECTION_FIX.md`.

## Quick Test (5 minutes)

### 1. Run Setup Tool
```
Unity Editor → Menu.unity → Tools → Setup Menu Networking (Menu Scene)
```
**Expected:** Dialog says "NetworkGameSession, FishNetNetworkController ready"

### 2. Verify in Hierarchy
Open Menu.unity and check the hierarchy:
```
NetworkManager/
  └── NetworkGameSession
```

### 3. Test Locally (Single Editor)
```
1. Menu.unity → Play
2. Click "Host Game"
3. Select character → Click START
4. Should spawn into gameplay ✅
```

### 4. Test Multiplayer (Built Builds)
```
Build #1: Host Game → Select character → Click START
Build #2: Join Server → Select character → Click START
→ Both should spawn into gameplay ✅
```

## What to Look For

### ✅ Success Indicators
Client console:
```
[Selection] We are CLIENT - using FishNet Broadcasts
[Selection] Selection and ready submitted via broadcasts
```
Host/server console:
```
[NetworkGameSession] Broadcast handlers registered!
[NetworkGameSession] Received SelectionBroadcast from client X: <CharacterName>
[NetworkGameSession] Received ReadyBroadcast from client X: ready=True
[NetworkGameSession] Client X is ready with selection '<CharacterName>'. Spawning immediately...
```

### ❌ Failure Indicators
```
[Selection] StartGame: Could not find NetworkGameSession
[NetworkGameSession] SubmitSelectionServerRpc called on non-server!
```
The second line means something called the server method directly on a remote client instead of sending a broadcast.

## If Still Broken

### Check 1: Setup Integrity
```
Tools → Check Network Setup Integrity
```

### Check 2: Server Registered Handlers?
The host console must show `Broadcast handlers registered!` after hosting. If it doesn't, `NetworkGameSession` isn't under the persisted NetworkManager.

### Check 3: Same NetworkManager
The NetworkManager must be the one carried over from Menu.unity via DontDestroyOnLoad, not a second one placed in SampleScene.

## Need More Help?
See full documentation: `CHARACTER_SELECTION_FIX.md`
