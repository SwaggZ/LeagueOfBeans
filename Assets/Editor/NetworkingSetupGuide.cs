using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Master setup guide for the complete networking system.
/// Run this FIRST to understand what needs to be configured.
/// 
/// Usage: Tools → Networking Setup Guide
/// </summary>
public class NetworkingSetupGuide
{
    [MenuItem("Tools/Networking Setup Guide")]
    public static void ShowGuide()
    {
        string guide = @"
=== LEAGUEOFBEANS NETWORKING SETUP GUIDE ===

The game uses a unified scene architecture (no separate selection/gameplay scenes).
Selection UI and gameplay happen in the same scene (SampleScene) with no scene reloads.

SETUP ORDER (follow exactly):

1️⃣  MENU SCENE SETUP (Run First)
   ├─ Open: Assets/Scenes/Menu.unity
   ├─ Run: Tools → Setup Menu Networking (Menu Scene)
   │  (Creates NetworkManager + NetworkGameSession)
   └─ Status: Menu scene ready ✓

2️⃣  SAMPLESCENE SETUP (Run Second)
   ├─ Open: Assets/Scenes/SampleScene.unity
   ├─ Run: Tools → Setup Selection Screen
   │  (Creates UI Canvas, character buttons, preview camera)
   ├─ Run: Tools → Create Spawn Points 
   │  (Batch-adds spawn point markers to spawn locations)
   ├─ Tag enemies: Select all enemy GameObjects → Inspector → Tag: Enemy
   └─ Status: SampleScene ready ✓

3️⃣  VERIFY SETUP (Optional but recommended)
   ├─ Run: Tools → Check Network Setup Integrity
   │  (Shows status of all components)
   └─ Review console output for any ⚠ warnings

4️⃣  TEST GAMEPLAY
   ├─ Play Menu scene
   ├─ Click ""Host Server""
   ├─ Should load SampleScene with selection UI
   ├─ Select a character
   ├─ Click ""START""
   ├─ Player should spawn in scene
   └─ Check console for logs (search: ""NetworkGameSession"")


COMPONENT OVERVIEW:

NetworkManager (Menu Scene, persists):
  • FishNet's core networking component
  • Does NOT manually create
  • AUTO-created by Setup Menu Networking tool
  • Persists to SampleScene via DontDestroyOnLoad

NetworkGameSession (Menu Scene, on NetworkManager):
  • Owns character prefabs list
  • Handles server-side spawn logic
  • Syncs selection state across network
  • AUTO-configured by Setup Menu Networking tool
  • Character prefabs populated from Assets/Champions/

CharacterSelection (SampleScene):
  • UI controller for selection browsing
  • Detects NetworkGameSession at runtime
  • Submits selection when START button clicked
  • AUTO-created by Setup Selection Screen tool

SelectionCamera (SampleScene):
  • Renders preview RenderTexture during selection UI
  • Only active during selection (disabled after spawn)
  • AUTO-created by Setup Selection Screen tool

SpawnPointMarker (SampleScene, multiple):
  • Marks valid spawn locations
  • Safest location selected by spawn algorithm
  • AUTO-added to GameObjects by Create Spawn Points tool


KEY SETTINGS TO CUSTOMIZE:

CharacterSelection.cs (if modifying prefab paths):
  • Modify CHARACTER_NAMES array if using different names
  • Modify CHARACTER_IDS array for character ID mapping

NetworkGameSession.cs (spawn behavior):
  • autoStartWhenAllSelected: Spawn when all players select (default: true)
  • requireReadyToStart: Require explicit Ready signal (default: false)
  • enemyScanRadius: Radius for safest spawn calculation
  • spawnPointKey: Filter spawn points by key


DEBUGGING TIPS:

If players don't spawn:
  ✓ Check console for [NetworkGameSession] logs
  ✓ Verify characterPrefabs list populated in NetworkGameSession Inspector
  ✓ Verify spawn points exist (should see in Gizmos while playing)
  ✓ Check character ID matches prefab name

If NetworkGameSession not found:
  ✓ Open Menu scene and run: Setup Menu Networking
  ✓ Verify NetworkManager exists in Menu scene
  ✓ Check that it has NetworkGameSession component

If selection UI doesn't appear:
  ✓ Verify SetupSelectionScreen tool was run on SampleScene
  ✓ Check Canvas exists in hierarchy
  ✓ Check character button wiring (should log on click)

If enemy tag not working:
  ✓ Add 'Enemy' tag: Project Settings → Tags and Layers
  ✓ Manually tag enemy GameObjects


ARCHITECTURE NOTES:

Menu Scene:
  • Entry point (StartMenu scene in build settings)
  • Creates NetworkManager (auto-persists)
  • Hosts server when ""Host Server"" clicked
  • Transitions to SampleScene

SampleScene:
  • Unified scene for selection + gameplay
  • NetworkManager from Menu persists here
  • Selection UI renders in canvas overlay
  • Gameplay happens in same scene (no load)
  • After spawn, preview cameras disabled, gameplay enabled

Network Flow:
  Menu: HostServer() → StartConnection()
        ↓
  Server Starts → LoadGameplaySceneAsServer() SampleScene
        ↓
  CharacterSelection Ready → SELECT character → CLICK START
        ↓
  ClientSubmitSelection() RPC → ClientSetReady() RPC
        ↓
  Server: AllPlayersSelected + AllPlayersReady?
        ↓
  Server: StartGameplay() → SpawnAllPlayers()
        ↓
  Character Instantiated at SafestSpawnPoint


COMMON MISTAKES:

❌ Creating multiple NetworkManagers
   → Run Setup Menu Networking ONCE and only once

❌ Not tagging enemies as ""Enemy""
   → Spawn algorithm won't find optimal locations

❌ Running SampleScene directly (without Menu first)
   → NetworkManager won't persist, selection won't work
   → Always play through Menu or use Setup Integrity tool

❌ Not configuring character prefabs
   → Setup Menu Networking does this automatically
   → Verify in NetworkGameSession Inspector

❌ Putting spawn points under parent objects
   → Move them to root level or adjust key filter


NEXT STEPS:

1. Open Menu.unity
2. Run: Tools → Setup Menu Networking (Menu Scene)
3. Open SampleScene.unity
4. Run: Tools → Setup Selection Screen
5. Run: Tools → Create Spawn Points
6. Tag enemies
7. Run: Tools → Check Network Setup Integrity
8. Test: Play and click Host Server


Questions? Check the console logs!
All setup scripts log detailed info to help troubleshoot.
";

        EditorUtility.DisplayDialog("Networking Setup Guide", guide, "OK");
        Debug.Log(guide);
    }
}
