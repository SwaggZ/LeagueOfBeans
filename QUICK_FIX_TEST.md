# Quick Fix Verification Guide

## What Was Fixed
Players getting stuck in character selection screen when two players connect.

## Quick Test (5 minutes)

### 1. Run Setup Tool
```
Unity Editor → Menu.unity → Tools → Setup Menu Networking (Menu Scene)
```
**Expected:** Dialog says "NetworkManager, NetworkGameSession, and NetworkSessionBridge ready"

### 2. Verify in Hierarchy
Open Menu.unity, check hierarchy:
```
NetworkManager/
  ├── NetworkGameSession
  └── NetworkSessionBridge ← NEW! Should have NetworkObject component
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
Build #1: Host Game → Select character → Click START (wait)
Build #2: Join Server → Select character → Click START
→ Both should spawn into gameplay ✅
```

## What to Look For

### ✅ Success Indicators
Console logs:
```
[NetworkSessionBridge] Found NetworkGameSession, bridge ready
[NetworkSessionBridge] Network started. IsServer=True
[Selection] Using NetworkSessionBridge for networked calls
[NetworkSessionBridge] Client sending selection: <CharacterName>
[NetworkSessionBridge] Server received selection from client X
[NetworkGameSession] AllPlayersReady → TRUE
[NetworkGameSession] Conditions met, starting gameplay!
```

### ❌ Failure Indicators  
```
[Selection] NetworkSessionBridge not found (using direct calls)
[NetworkSessionBridge] NetworkObject not spawned! Cannot send RPC
[Selection] StartGame: Could not find NetworkGameSession
```

## Console Command for Debug
When stuck, check console for:
1. `[NetworkSessionBridge]` messages - Are RPCs being sent/received?
2. `[NetworkGameSession] State:` - Are both clients marked ready?
3. `AllPlayersReady` - Did the check pass?

## If Still Broken

### Check 1: Bridge Exists?
```
Tools → Check Network Setup Integrity
Look for: "✓ NetworkSessionBridge found"
```

### Check 2: NetworkObject Spawned?
When playing, check console for:
```
[NetworkSessionBridge] NetworkObject ID: X, IsSpawned: True
```
If `IsSpawned: False` → Bridge not network-ready

### Check 3: Server Started?
Check console for:
```
[NetworkManager] Server started
[NetworkSessionBridge] Network started. IsServer=True
```

### Common Issues

**Issue:** "NetworkSessionBridge not found"
- **Fix:** Run setup tool, verify bridge GameObject exists

**Issue:** "NetworkObject not spawned"
- **Fix:** Ensure NetworkManager starts server before clients send selections
- **Advanced Fix:** May need to configure bridge as Global network object

**Issue:** "Selection sent but server never receives"
- **Fix:** Verify both client and server show NetworkSessionBridge logs
- **Check:** NetworkManager is same instance on both (persisted from Menu)

## Need More Help?
See full documentation: `CHARACTER_SELECTION_FIX.md`
