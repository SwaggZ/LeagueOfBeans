using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing;
using System.Collections.Generic;

/// <summary>
/// Comprehensive setup tool for the complete networking and gameplay system.
/// Configures NetworkManager, NetworkGameSession, character prefabs, and spawn points.
/// Usage: Tools → Complete Networking Setup
/// </summary>
public class CompleteNetworkingSetup
{
    private static string[] CHARACTER_NAMES = { "Ahri", "Ashe", "Galio", "Caitlyn", "Aphelios", "Jhin", "Lux" };
    private static string[] CHARACTER_IDS = { "0", "1", "2", "3", "4", "5", "6" };

    [MenuItem("Tools/Complete Networking Setup")]
    public static void RunCompleteSetup()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Error", "No active scene loaded!", "OK");
            return;
        }

        Debug.Log($"[CompleteNetworkingSetup] Starting complete setup in scene: {activeScene.name}");

        // Step 1: Find or create NetworkManager
        NetworkManager nm = Object.FindObjectOfType<NetworkManager>();
        if (nm == null)
        {
            GameObject nmGo = new GameObject("NetworkManager");
            nm = nmGo.AddComponent<NetworkManager>();
            Debug.Log("[CompleteNetworkingSetup] Created new NetworkManager");
        }
        else
        {
            Debug.Log("[CompleteNetworkingSetup] Found existing NetworkManager");
        }

        // Step 2: Add NetworkGameSession on a separate GameObject (NOT on NetworkManager)
        // NetworkBehaviour auto-adds NetworkObject, which is forbidden on NetworkManager
        NetworkGameSession ngs = Object.FindObjectOfType<NetworkGameSession>();
        if (ngs == null)
        {
            GameObject ngsGo = new GameObject("NetworkGameSession");
            ngsGo.transform.SetParent(nm.transform);
            ngs = ngsGo.AddComponent<NetworkGameSession>();
            Debug.Log("[CompleteNetworkingSetup] Created NetworkGameSession on separate GameObject");
        }
        else
        {
            Debug.Log("[CompleteNetworkingSetup] NetworkGameSession already exists");
        }

        // Step 3: Configure character prefabs on NetworkGameSession
        if (ngs.characterPrefabs == null || ngs.characterPrefabs.Count == 0)
        {
            ngs.characterPrefabs = new List<NetworkGameSession.CharacterPrefabEntry>();
            
            for (int i = 0; i < CHARACTER_IDS.Length; i++)
            {
                var entry = new NetworkGameSession.CharacterPrefabEntry();
                entry.id = CHARACTER_IDS[i];
                
                // Try to find character prefab by name
                string prefabPath = $"Assets/Champions/{CHARACTER_NAMES[i]}/{CHARACTER_NAMES[i]}Controller.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab == null)
                {
                    // Try alternate naming
                    prefabPath = $"Assets/Champions/{CHARACTER_NAMES[i].ToLower()}_controller.prefab";
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                }
                
                if (prefab == null)
                {
                    Debug.LogWarning($"[CompleteNetworkingSetup] Could not find prefab for {CHARACTER_NAMES[i]}. Skipping.");
                    continue;
                }
                
                entry.prefab = prefab;
                ngs.characterPrefabs.Add(entry);
                Debug.Log($"[CompleteNetworkingSetup] Added {CHARACTER_NAMES[i]} (ID: {CHARACTER_IDS[i]})");
            }

            if (ngs.characterPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("Warning", "No character prefabs found! Make sure they exist in Assets/Champions/", "OK");
            }
        }

        // Step 4: Configure spawn points
        var spawnMarkers = Object.FindObjectsOfType<SpawnPointMarker>();
        if (spawnMarkers.Length == 0)
        {
            EditorUtility.DisplayDialog("Warning", "No SpawnPointMarker components found in scene! Use Tools → Create Spawn Points to add them.", "OK");
        }
        else
        {
            Debug.Log($"[CompleteNetworkingSetup] Found {spawnMarkers.Length} spawn points");
        }

        // Step 5: Check for enemy tags
        var allTags = UnityEditorInternal.InternalEditorUtility.tags;
        bool hasEnemyTag = System.Array.Exists(allTags, tag => tag == "Enemy");
        
        if (!hasEnemyTag)
        {
            Debug.LogWarning("[CompleteNetworkingSetup] 'Enemy' tag not found! Add it in Project Settings → Tags and assign to enemy GameObjects for optimal spawn point selection.");
        }
        else
        {
            Debug.Log("[CompleteNetworkingSetup] 'Enemy' tag exists");
        }

        // Mark scene as modified
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.SetDirty(ngs);
        EditorUtility.SetDirty(nm);

        EditorUtility.DisplayDialog("Success", 
            $"✓ NetworkManager setup complete\n" +
            $"✓ NetworkGameSession configured\n" +
            $"✓ {ngs.characterPrefabs.Count} characters registered\n" +
            $"✓ {spawnMarkers.Length} spawn points found\n\n" +
            $"Scene is ready for networking gameplay!", 
            "OK");

        Debug.Log("[CompleteNetworkingSetup] Setup complete!");
    }
}
