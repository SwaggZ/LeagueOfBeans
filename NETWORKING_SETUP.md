# LeagueOfBeans Complete Networking Setup Instructions

## Current Status
- ✅ **Code**: All networking code is configured and ready
- ✅ **Setup Tools**: Comprehensive editor tools created
- ❌ **Infrastructure**: Missing NetworkGameSession in proper location
- ⚠️ **Next Action**: Run setup tools in correct order

## Root Cause of Current Issue
The error `"[Selection] StartGame: Could not find NetworkGameSession!"` occurs because:
1. Menu scene has a NetworkManager that persists to SampleScene
2. **BUT** that NetworkManager is missing the `NetworkGameSession` component
3. When you click START, the code can't find NetworkGameSession to submit selection

## Complete Setup Process (Step by Step)

### Step 1: Setup Menu Scene (5 minutes)
This creates the persistent NetworkManager with the NetworkGameSession component.

```
1. In Unity Editor, open: Assets/Scenes/Menu.unity
2. In top menu, go to: Tools → Setup Menu Networking (Menu Scene)
3. A dialog will appear: "NetworkManager setup complete"
4. Check Console for confirmation messages
```

**What this does:**
- Creates NetworkManager if it doesn't exist
- Adds NetworkGameSession component to NetworkManager
- Populates character prefabs list (Ahri, Ashe, Galio, Caitlyn, Aphelios, Jhin, Lux)
- NetworkManager automatically persists to other scenes via DontDestroyOnLoad

### Step 2: Setup Selection Screen in SampleScene (3 minutes)
This creates the character selection UI that appears when the game loads.

```
1. In Unity Editor, open: Assets/Scenes/SampleScene.unity
2. In top menu, go to: Tools → Setup Selection Screen
3. A dialog will appear: "Selection screen created successfully"
4. Check Hierarchy - should see "SelectionCanvas" with all UI elements
```

**What this does:**
- Creates SelectionCanvas with character buttons
- Creates preview camera with RenderTexture
- Creates START button wired to CharacterSelection.StartGame()
- Finds/creates CharacterSelection component
- No scene reload - everything stays in SampleScene

### Step 3: Setup Spawn Points in SampleScene (2 minutes)
This marks valid locations where players can spawn.

```
1. Still in SampleScene (from Step 2)
2. In top menu, go to: Tools → Create Spawn Points
3. Select which GameObjects should be spawn locations when prompted
4. Check Hierarchy - selected objects should now have SpawnPointMarker components
```

**What this does:**
- Adds SpawnPointMarker components to GameObjects
- Server uses these to calculate safest spawn location
- Safest = fewest enemies nearby, then furthest from enemies
- If none found, defaults to Vector3.zero

### Step 4: Tag Enemy GameObjects (2 minutes)
Mark which GameObjects are enemies so spawn algorithm works correctly.

```
1. Still in SampleScene
2. Select each enemy GameObject in Hierarchy
3. In Inspector, find Tag dropdown (top-right of Name field)
4. Click Tag → Create New Tag → Name: "Enemy" → Save
5. Select each enemy GO again, set Tag to "Enemy"
```

**What this does:**
- Enables NetworkGameSession.SelectSpawnPoint() to find nearby enemies
- Algorithms avoids spawning near enemies
- Without this, spawn algorithm can't distinguish enemies from other colliders

### Step 5: Verify Setup (1 minute)
Check that everything is configured correctly.

```
1. Still in SampleScene
2. In top menu, go to: Tools → Check Network Setup Integrity
3. Review the Console output for any ❌ errors or ⚠️ warnings
4. If any issues: either fix them or rerun the setup tools
```

**This checks:**
- NetworkManager exists
- NetworkGameSession exists with character prefabs
- CharacterSelection component in scene
- SpawnPointMarkers exist
- Character prefab files are accessible
- Enemy tag exists

### Step 6: Test the Complete Flow (5 minutes)
Now test that everything works together.

```
1. In Menu scene, click Play button
2. Should load Menu with "Host Server" button
3. Click "Host Server"
   → Should load SampleScene
   → Should see character selection UI
   → Should see character preview
4. Click character button (e.g., "Lux")
   → Should see preview image change
5. Click "START" button
   → Check Console for [NetworkGameSession] logs
   → Should see player character spawn in scene
```

**Expected Console Output:**
```
Server started, loading gameplay scene with selection UI...
[Selection] Preview[6] luxPreview active=True
[Selection] StartGame: Found NetworkGameSession, submitting selection...
[NetworkGameSession] SubmitSelectionServerRpc: client 0 selected 'Lux'
[NetworkGameSession] SpawnAllPlayers called...
[NetworkGameSession] Processing client 0, selected character ID: 'Lux'
[NetworkGameSession] Spawning LuxController at (0.0, 0.0, 0.0) for client 0
```

## What Each Setup Tool Does

### SetupMenuNetworking
- **File**: Assets/Editor/SetupMenuNetworking.cs
- **Command**: Tools → Setup Menu Networking (Menu Scene)
- **Creates**:
  - NetworkManager (if missing) in Menu scene
  - NetworkGameSession component on it
  - Populates characterPrefabs list with all 7 characters
- **Why Important**: Menu's NetworkManager persists to SampleScene, so it MUST have NetworkGameSession

### SetupSelectionScreen
- **File**: Assets/Editor/SetupSelectionScreen.cs
- **Command**: Tools → Setup Selection Screen
- **Creates**:
  - SelectionCanvas with 1920x1080 layout
  - Left panel: 7 character buttons (wired to SelectByIndex)
  - Right panel: Preview camera image + info text
  - Bottom: START button (wired to CharacterSelection.StartGame)
- **Why Important**: UI is how players select characters and start game

### CreateSpawnPoints
- **File**: Assets/Editor/CreateSpawnPoints.cs
- **Command**: Tools → Create Spawn Points
- **Creates**:
  - SpawnPointMarker components on selected GameObjects
  - These mark valid player spawn locations
- **Why Important**: Server needs spawn points to determine where players appear

### NetworkingDiagnostics
- **File**: Assets/Editor/NetworkingDiagnostics.cs
- **Command**: Tools → Check Network Setup Integrity
- **Shows**:
  - Status of all networking components
  - Character prefab configuration
  - Spawn point count
  - Any missing components or configuration issues
- **Why Important**: Quickly identify setup problems before testing

### NetworkingSetupGuide
- **File**: Assets/Editor/NetworkingSetupGuide.cs
- **Command**: Tools → Networking Setup Guide
- **Shows**:
  - Complete architectural overview
  - Setup order and why it matters
  - Debugging tips
  - Common mistakes
- **Why Important**: Reference guide for understanding the system

## Code Architecture Overview

### Scene Layout
```
Menu.unity (Entry Point)
├─ NetworkManager (persists via DontDestroyOnLoad)
│  └─ NetworkGameSession (on same GO)
│     └─ characterPrefabs list populated
└─ UI: Host/Join buttons

SampleScene.unity (Unified Selection + Gameplay)
├─ (NetworkManager persists from Menu)
├─ SelectionCanvas (character UI)
│  ├─ CharacterListPanel (buttons)
│  ├─ PreviewPanel (RawImage + SelectionCamera)
│  ├─ START button (wired to CharacterSelection.StartGame)
│  └─ CharacterSelection (on NetworkManager or solo)
├─ SelectionCamera (renders preview to RenderTexture)
├─ GameplayObjects (enemies, obstacles, etc.)
└─ SpawnPointMarkers (mark valid spawn locations)
```

### Network Flow
```
User clicks "Host Server" in Menu
  ↓
FishNetNetworkController.HostServer()
  ↓
NetworkManager.ServerManager.StartConnection()
NetworkManager.ClientManager.StartConnection()
  ↓
OnServerConnectionState event fires
FishNetNetworkController.LoadGameplaySceneAsServer()
  ↓
SampleScene loads
NetworkManager from Menu persists to it
SelectionCanvas UI appears
  ↓
User selects character → clicks START
  ↓
CharacterSelection.StartGame()
Finds NetworkGameSession
Calls ClientSubmitSelection() RPC
Calls ClientSetReady() RPC
  ↓
Server receives RPC
NetworkGameSession.SubmitSelectionServerRpc() runs
Checks if AllPlayersSelected && AllPlayersReady
  ↓
Server: StartGameplay() → SpawnAllPlayers()
  ↓
For each client:
  1. Get selected character ID from SyncDictionary
  2. Find corresponding prefab in characterPrefabs list
  3. Calculate safest spawn point (fewest enemies)
  4. Instantiate character prefab at spawn point
  5. Server-spawn it with NetworkManager.ServerManager.Spawn()
  ↓
Character appears in SampleScene
(No scene reload - same scene as selection UI)
```

## Troubleshooting Guide

### Problem: "Could not find NetworkGameSession!"
**The error you're seeing now.**

**Solution:**
1. Open Menu.unity
2. Run: Tools → Setup Menu Networking (Menu Scene)
3. Verify in Inspector: Menu's NetworkManager has NetworkGameSession component
4. Verify NetworkGameSession.characterPrefabs has 7 entries

### Problem: No character prefabs showing in NetworkGameSession Inspector
**AfterSetup Menu Networking, characterPrefabs should be populated.**

**Solution:**
1. Check Assets/Champions/ folder exists
2. Verify character prefab naming (e.g., "AhriController.prefab")
3. Run diagnostic: Tools → Check Network Setup Integrity
4. Manually set characterPrefabs if needed

### Problem: Selection UI doesn't appear when game starts
**You clicked Host but no UI shows in SampleScene.**

**Solution:**
1. Open SampleScene.unity
2. Check Hierarchy for "SelectionCanvas" - if missing, run: Tools → Setup Selection Screen
3. Check Console for errors during scene load
4. Verify EventSystem exists in Hierarchy (for UI input)

### Problem: Player doesn't spawn after clicking START
**UI works, but character doesn't appear.**

**Solution:**
1. Check Console for [NetworkGameSession] logs
2. Look for: "could not find prefab" - character ID mismatch
3. Check characterPrefabs list populated correctly: Tools → Check Network Setup Integrity
4. Verify spawn points exist: should see count > 0
5. Check SpawnPointMarkers in Hierarchy (if 0, run: Tools → Create Spawn Points)

### Problem: Character prefab found but wrong one spawns
**Wrong character appears or control is wrong.**

**Solution:**
1. Check character ID matching:
   - Console shows: "selected character ID: 'Lux'"
   - characterPrefabs should have ID "Lux" → correct prefab
2. Verify prefab names:
   - Selection uses character name (e.g., "Lux")
   - Prefab list should match (id="Lux", prefab=LuxController.prefab)

### Problem: Second client can't join
**Works for host, fails when second player joins.**

**Solution:**
1. Verify characterPrefabs on all clients (auto-synced, but check)
2. Check spawn points (should have at least 2)
3. Verify both clients see same character selection UI
4. Check network connection logs

## Files Created/Modified

### New Files (Setup Tools)
- `Assets/Editor/CompleteNetworkingSetup.cs`
- `Assets/Editor/SetupMenuNetworking.cs`
- `Assets/Editor/NetworkingDiagnostics.cs`
- `Assets/Editor/NetworkingSetupGuide.cs`

### Existing Files (Already Modified)
- `Assets/Networking/FishNet/FishNetNetworkController.cs` - loads SampleScene on server start
- `Assets/Networking/FishNet/NetworkGameSession.cs` - handles selection sync and spawn
- `Assets/CharacterSelection.cs` - UI controller, detects NetworkGameSession
- `Assets/Editor/SetupSelectionScreen.cs` - creates UI (already exists)
- `Assets/Editor/CreateSpawnPoints.cs` - creates spawn markers (already exists)

## Next Steps After Setup

1. **Test single-player spawn** (what you'll do first)
2. **Test networking** - connect second client and verify both can spawn
3. **Fine-tune spawn algorithm** - adjust enemyScanRadius, preferences
4. **Add ability functionality** - make characters playable
5. **Implement game rules** - win conditions, scoring, etc.

## Quick Reference

- **Menu Setup**: Tools → Setup Menu Networking (Menu Scene)
- **SampleScene Setup**: Tools → Setup Selection Screen
- **Spawn Setup**: Tools → Create Spawn Points
- **Verify**: Tools → Check Network Setup Integrity
- **Help**: Tools → Networking Setup Guide

---

**Ready to start? Open Menu.unity and run: Tools → Setup Menu Networking (Menu Scene)**
