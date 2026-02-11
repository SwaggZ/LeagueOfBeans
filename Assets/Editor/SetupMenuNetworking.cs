using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing;
using System.Collections.Generic;

/// <summary>
/// Sets up the Menu scene for networking:
/// - Creates/ensures NetworkManager exists
/// - Adds NetworkGameSession to it
/// - Configures character prefabs and spawn settings
/// 
/// This script should be run on the Menu scene where the game starts.
/// The NetworkManager will persist across scenes via DontDestroyOnLoad.
/// </summary>
public class SetupMenuNetworking
{
    private static string[] CHARACTER_IDS = { "Ahri", "Ashe", "Galio", "Caitlyn", "Aphelios", "Jhin", "Lux" };

    [MenuItem("Tools/Setup Menu Networking (Menu Scene)")]
    public static void SetupMenu()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Menu"))
        {
            EditorUtility.DisplayDialog("Error", "Please open the Menu.unity scene before running this script!", "OK");
            return;
        }

        Debug.Log("[SetupMenuNetworking] Setting up Menu scene networking...");

        // Step 1: Find or create NetworkManager
        NetworkManager nm = Object.FindObjectOfType<NetworkManager>();
        if (nm == null)
        {
            GameObject nmGo = new GameObject("NetworkManager");
            nm = nmGo.AddComponent<NetworkManager>();
            Debug.Log("[SetupMenuNetworking] Created new NetworkManager in Menu scene");
        }
        else
        {
            Debug.Log("[SetupMenuNetworking] Found existing NetworkManager in Menu scene");
        }

        // Step 2: Add NetworkGameSession on a separate GameObject (NOT on NetworkManager)
        // NetworkBehaviour auto-adds NetworkObject, which is forbidden on NetworkManager
        NetworkGameSession ngs = Object.FindObjectOfType<NetworkGameSession>();
        if (ngs == null)
        {
            GameObject ngsGo = new GameObject("NetworkGameSession");
            ngsGo.transform.SetParent(nm.transform);
            ngs = ngsGo.AddComponent<NetworkGameSession>();
            Debug.Log("[SetupMenuNetworking] Created NetworkGameSession on separate GameObject");
        }
        else
        {
            Debug.Log("[SetupMenuNetworking] NetworkGameSession already exists");
        }

        // Step 3: Configure character prefabs
        if (ngs.characterPrefabs == null || ngs.characterPrefabs.Count == 0)
        {
            ngs.characterPrefabs = new List<NetworkGameSession.CharacterPrefabEntry>();
            
            foreach (var id in CHARACTER_IDS)
            {
                var entry = new NetworkGameSession.CharacterPrefabEntry();
                entry.id = id;
                
                // Try to find character prefab
                GameObject prefab = FindCharacterPrefab(id);
                if (prefab != null)
                {
                    entry.prefab = prefab;
                    ngs.characterPrefabs.Add(entry);
                    Debug.Log($"[SetupMenuNetworking] Added {id}");
                }
                else
                {
                    Debug.LogWarning($"[SetupMenuNetworking] Prefab notfound for {id}");
                }
            }
        }

        // Mark as modified
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.SetDirty(ngs);
        EditorUtility.SetDirty(nm);

        EditorUtility.DisplayDialog("Success",
            $"✓ Menu scene networking configured\n" +
            $"✓ NetworkManager and NetworkGameSession ready\n" +
            $"✓ {ngs.characterPrefabs.Count} characters registered\n\n" +
            $"NetworkManager will persist to SampleScene via DontDestroyOnLoad",
            "OK");

        Debug.Log("[SetupMenuNetworking] Menu networking setup complete!");
    }

    private static GameObject FindCharacterPrefab(string characterName)
    {
        // Try common naming patterns
        var patterns = new[]
        {
            $"Assets/Champions/{characterName}/{characterName}Controller.prefab",
            $"Assets/Champions/{characterName.ToLower()}_controller.prefab",
            $"Assets/Champions/{characterName}/{characterName}.prefab",
            $"Assets/Champions/{characterName}_Prefab.prefab"
        };

        foreach (var pattern in patterns)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pattern);
            if (prefab != null)
                return prefab;
        }

        // Fallback: search all prefabs
        string[] guids = AssetDatabase.FindAssets($"t:Prefab {characterName}");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return null;
    }
}

/// <summary>
/// Alternative: Setup SampleScene for direct testing (without Menu scene).
/// The setup creates a local NetworkManager instead of relying on persistence.
/// </summary>
public class SetupSampleSceneNetworking
{
    [MenuItem("Tools/Setup SampleScene Networking (Gameplay Scene)")]
    public static void SetupSampleScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.name.Contains("SampleScene"))
        {
            EditorUtility.DisplayDialog("Error", "Please open the SampleScene.unity scene before running this script!", "OK");
            return;
        }

        Debug.Log("[SetupSampleSceneNetworking] Setting up SampleScene...");

        // Note: In SampleScene, we typically DON'T create a local NetworkManager.
        // Instead, the one from Menu persists here.
        // However, for direct testing of the scene in editor, you might need a local one.

        NetworkManager nm = Object.FindObjectOfType<NetworkManager>();
        if (nm == null)
        {
            EditorUtility.DisplayDialog("Info",
                "No NetworkManager found.\n\n" +
                "When playing through Menu → Host:\n" +
                "  NetworkManager from Menu will persist here automatically.\n\n" +
                "For direct scene testing, you must either:\n" +
                "1) Run Menu scene first (recommended)\n" +
                "2) Or use Tools → Complete Networking Setup to create local instance",
                "OK");
            return;
        }

        NetworkGameSession ngs = nm.GetComponent<NetworkGameSession>();
        if (ngs == null)
        {
            ngs = nm.gameObject.AddComponent<NetworkGameSession>();
            Debug.Log("[SetupSampleSceneNetworking] Added NetworkGameSession");
        }

        // Check for spawn points
        var spawnMarkers = Object.FindObjectsOfType<SpawnPointMarker>();
        if (spawnMarkers.Length == 0)
        {
            EditorUtility.DisplayDialog("Warning",
                "No SpawnPointMarkers found in SampleScene!\n\n" +
                "Run: Tools → Create Spawn Points",
                "OK");
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.SetDirty(ngs);

        EditorUtility.DisplayDialog("Success",
            $"✓ SampleScene networking configured\n" +
            $"✓ SpawnPoints checked: {spawnMarkers.Length} found",
            "OK");
    }
}
