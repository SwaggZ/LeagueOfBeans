using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing;
using System.Collections.Generic;

/// <summary>
/// Comprehensive diagnostics and debugging tool for the networking setup.
/// Shows the current state and helps identify issues.
///
/// Usage: Tools → Check Network Setup Integrity
/// </summary>
public class NetworkingDiagnostics
{
    [MenuItem("Tools/Check Network Setup Integrity")]
    public static void CheckSetup()
    {
        Debug.Log("\n=== NETWORKING SETUP DIAGNOSTICS ===\n");

        var activeScene = SceneManager.GetActiveScene();
        Debug.Log($"📍 Active Scene: {activeScene.name}");

        CheckNetworkManager();
        CheckNetworkGameSession();
        CheckCharacterSelection();
        CheckSpawnPoints();
        CheckCharacterPrefabs();
        CheckTags();

        Debug.Log("\n=== END DIAGNOSTICS ===\n");

        // Also show summary in dialog
        ShowSummary(activeScene.name);
    }

    private static void CheckNetworkManager()
    {
        Debug.Log("\n--- NetworkManager Check ---");
        
        var allManagers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        Debug.Log($"Found {allManagers.Length} NetworkManager instance(s)");

        foreach (var nm in allManagers)
        {
            Debug.Log($"  • {nm.gameObject.name} (active: {nm.gameObject.activeSelf})");
            Debug.Log($"    - IsServer: {nm.IsServerStarted}");
            Debug.Log($"    - IsClient: {nm.IsClientStarted}");
            Debug.Log($"    - Clients: {(nm.IsServerStarted ? nm.ServerManager.Clients.Count : "N/A")}");
            
            // Check persistence (if in DontDestroyOnLoad scene)
            var unityObject = nm.gameObject;
            Scene objScene = unityObject.scene;
            if (objScene.name == "DontDestroyOnLoad")
                Debug.Log($"    - ✓ Will persist across scenes (in DontDestroyOnLoad)");
            else
                Debug.Log($"    - ℹ In scene: {objScene.name}");
        }

        if (allManagers.Length == 0)
            Debug.LogWarning("❌ NO NetworkManager found in scene!");
    }

    private static void CheckNetworkGameSession()
    {
        Debug.Log("\n--- NetworkGameSession Check ---");

        var sessions = Object.FindObjectsByType<NetworkGameSession>(FindObjectsSortMode.None);
        Debug.Log($"Found {sessions.Length} NetworkGameSession instance(s)");

        foreach (var session in sessions)
        {
            Debug.Log($"  • On: {session.gameObject.name}");
            Debug.Log($"    - CharacterPrefabs: {session.characterPrefabs.Count}");
            
            if (session.characterPrefabs.Count > 0)
            {
                foreach (var entry in session.characterPrefabs)
                {
                    string prefabStatus = entry.prefab != null ? "✓" : "❌";
                    Debug.Log($"      {prefabStatus} ID '{entry.id}' -> {(entry.prefab != null ? entry.prefab.name : "NULL")}");
                }
            }
            else
            {
                Debug.LogWarning("    ⚠ No character prefabs configured!");
            }

            Debug.Log($"    - Spawn Point Key: '{session.spawnPointKey}'");
            Debug.Log($"    - Auto Start: {session.autoStartWhenAllSelected}");
            Debug.Log($"    - Require Ready: {session.requireReadyToStart}");
        }

        if (sessions.Length == 0)
            Debug.LogError("❌ NO NetworkGameSession found!");
        else if (sessions.Length > 1)
            Debug.LogWarning("⚠ Multiple NetworkGameSession instances - this might cause issues!");
    }

    private static void CheckCharacterSelection()
    {
        Debug.Log("\n--- CharacterSelection Check ---");

        var selector = Object.FindFirstObjectByType<CharacterSelection>();
        if (selector == null)
        {
            Debug.LogWarning("❌ No CharacterSelection found in scene");
            return;
        }

        Debug.Log($"  • Found on: {selector.gameObject.name}");
        Debug.Log($"    - Characters configured: {selector.characters.Count}");

        foreach (var c in selector.characters)
        {
            if (c == null) continue;
            Debug.Log($"      • ID: '{c.id}'");
            Debug.Log($"        Preview: {(c.preview != null ? c.preview.name : "❌ NULL")}");
            Debug.Log($"        Gameplay Prefab: {(c.gameplayPrefab != null ? c.gameplayPrefab.name : "❌ NULL")}");
        }

        var ngs = selector.networkSession;
        if (ngs != null)
            Debug.Log($"    - NetworkGameSession: ✓ Found and assigned");
        else
            Debug.LogWarning("    - NetworkGameSession: ❌ NOT assigned (will auto-detect at runtime)");

        // Selection/ready requests now travel via FishNet broadcasts (see NetworkGameSession),
        // so no extra RPC component is needed on CharacterSelection.
    }

    private static void CheckSpawnPoints()
    {
        Debug.Log("\n--- SpawnPoint Check ---");

        var spawnMarkers = Object.FindObjectsByType<SpawnPointMarker>(FindObjectsSortMode.None);
        Debug.Log($"Found {spawnMarkers.Length} SpawnPointMarker(s)");

        if (spawnMarkers.Length == 0)
        {
            Debug.LogWarning("❌ No spawn points configured! Run: Tools → Create Spawn Points");
            return;
        }

        foreach (var sp in spawnMarkers)
        {
            Debug.Log($"  • {sp.gameObject.name}");
            Debug.Log($"    - Position: {sp.transform.position}");
            Debug.Log($"    - Key: '{sp.key}'");
        }
    }

    private static void CheckCharacterPrefabs()
    {
        Debug.Log("\n--- Character Prefab Check ---");

        var candidates = new[] { "Ahri", "Ashe", "Galio", "Caitlyn", "Aphelios", "Jhin", "Lux" };
        int found = 0;

        foreach (var name in candidates)
        {
            var patterns = new[]
            {
                $"Assets/Champions/{name}/{name}Controller.prefab",
                $"Assets/Champions/{name.ToLower()}_controller.prefab",
                $"Assets/Champions/{name}/{name}.prefab",
            };

            bool foundThisOne = false;
            foreach (var pattern in patterns)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pattern);
                if (prefab != null)
                {
                    Debug.Log($"  ✓ {name}: {pattern}");
                    foundThisOne = true;
                    found++;
                    break;
                }
            }

            if (!foundThisOne)
                Debug.LogWarning($"  ❌ {name}: NOT FOUND");
        }

        Debug.Log($"Summary: {found}/{candidates.Length} character prefabs found");
    }

    private static void CheckTags()
    {
        Debug.Log("\n--- Tags Check ---");

        var allTags = UnityEditorInternal.InternalEditorUtility.tags;
        bool hasEnemyTag = System.Array.Exists(allTags, tag => tag == "Enemy");

        if (hasEnemyTag)
        {
            Debug.Log("  ✓ 'Enemy' tag exists");
            
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Debug.Log($"    - {enemies.Length} GameObjects tagged as Enemy");
        }
        else
        {
            Debug.LogWarning("  ❌ 'Enemy' tag not found - add it in Project Settings → Tags");
        }
    }

    private static void ShowSummary(string sceneName)
    {
        var nm = Object.FindFirstObjectByType<NetworkManager>();
        var ngs = Object.FindFirstObjectByType<NetworkGameSession>();
        var selector = Object.FindFirstObjectByType<CharacterSelection>();
        var spawnMarkers = Object.FindObjectsByType<SpawnPointMarker>(FindObjectsSortMode.None);

        string summary = $"Scene: {sceneName}\n\n";

        if (nm != null) summary += "✓ NetworkManager\n"; else summary += "❌ NetworkManager MISSING\n";
        if (ngs != null) summary += "✓ NetworkGameSession\n"; else summary += "❌ NetworkGameSession MISSING\n";
        if (selector != null) summary += "✓ CharacterSelection\n"; else summary += "❌ CharacterSelection MISSING\n";
        summary += $"{spawnMarkers.Length} SpawnPoints\n";

        if (nm != null && ngs != null && selector != null && spawnMarkers.Length > 0)
            summary += "\n✓ All essential components configured!";
        else
            summary += "\n⚠ Some components missing - see Console for details";

        EditorUtility.DisplayDialog("Network Setup Summary", summary, "OK");
    }
}
