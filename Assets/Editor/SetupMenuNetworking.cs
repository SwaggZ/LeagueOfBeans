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

    private static void VerifyNetworkManagerComponents(NetworkManager nm)
    {
        if (nm == null) return;

        Debug.Log("[SetupMenuNetworking] Verifying and adding FishNet components...");

        // Add required FishNet components if missing
        System.Type[] requiredComponents = new System.Type[] {
            typeof(FishNet.Managing.Transporting.TransportManager),
            typeof(FishNet.Managing.Server.ServerManager),
            typeof(FishNet.Managing.Client.ClientManager),
            typeof(FishNet.Managing.Timing.TimeManager),
            typeof(FishNet.Managing.Scened.SceneManager),
            typeof(FishNet.Managing.Observing.ObserverManager),
        };

        int added = 0;
        foreach (var compType in requiredComponents)
        {
            var comp = nm.GetComponent(compType);
            if (comp == null)
            {
                nm.gameObject.AddComponent(compType);
                added++;
                Debug.Log($"[SetupMenuNetworking] Added {compType.Name}");
            }
        }

        // Add Transport (Tugboat) if missing
        var transport = nm.GetComponent<FishNet.Transporting.Tugboat.Tugboat>();
        if (transport == null)
        {
            nm.gameObject.AddComponent<FishNet.Transporting.Tugboat.Tugboat>();
            added++;
            Debug.Log("[SetupMenuNetworking] Added Tugboat Transport");
        }

        if (added > 0)
        {
            Debug.Log($"[SetupMenuNetworking] ✓ Added {added} missing FishNet components");
        }
        else
        {
            Debug.Log("[SetupMenuNetworking] ✓ All required FishNet components already present");
        }
        
        EditorUtility.SetDirty(nm.gameObject);
    }

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
        
        // Add all required FishNet components
        VerifyNetworkManagerComponents(nm);

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
        
        // Step 2.5: Add NetworkSessionBridge (NetworkBehaviour that can use RPCs)
        NetworkSessionBridge bridge = Object.FindObjectOfType<NetworkSessionBridge>();
        if (bridge == null)
        {
            GameObject bridgeGo = new GameObject("NetworkSessionBridge");
            bridgeGo.transform.SetParent(nm.transform);
            bridge = bridgeGo.AddComponent<NetworkSessionBridge>();
            // NetworkSessionBridge will auto-add NetworkObject component
            Debug.Log("[SetupMenuNetworking] Created NetworkSessionBridge on separate GameObject");
        }
        else
        {
            Debug.Log("[SetupMenuNetworking] NetworkSessionBridge already exists");
        }

        // Step 2.6: Add FishNetNetworkController (orchestrates host/join)
        FishNetNetworkController controller = Object.FindObjectOfType<FishNetNetworkController>();
        if (controller == null)
        {
            GameObject controllerGo = new GameObject("FishNetNetworkController");
            controllerGo.transform.SetParent(nm.transform);
            controller = controllerGo.AddComponent<FishNetNetworkController>();
            Debug.Log("[SetupMenuNetworking] Created FishNetNetworkController");
        }
        else
        {
            Debug.Log("[SetupMenuNetworking] FishNetNetworkController already exists");
        }

        // Step 2.7: Add SessionContext (stores server settings)
        SessionContext sessionContext = Object.FindObjectOfType<SessionContext>();
        if (sessionContext == null)
        {
            GameObject sessionGo = new GameObject("SessionContext");
            sessionGo.transform.SetParent(nm.transform);
            sessionContext = sessionGo.AddComponent<SessionContext>();
            Debug.Log("[SetupMenuNetworking] Created SessionContext");
        }
        else
        {
            Debug.Log("[SetupMenuNetworking] SessionContext already exists");
        }

        // Step 2.8: Add NetworkSessionAuthenticator (handles passwords)
        NetworkSessionAuthenticator auth = nm.GetComponent<NetworkSessionAuthenticator>();
        if (auth == null)
        {
            auth = nm.gameObject.AddComponent<NetworkSessionAuthenticator>();
            Debug.Log("[SetupMenuNetworking] Added NetworkSessionAuthenticator to NetworkManager");
        }
        else
        {
            Debug.Log("[SetupMenuNetworking] NetworkSessionAuthenticator already exists");
        }

        // Step 2.9: Wire up FishNetNetworkController references using reflection
        // (Unity doesn't serialize public fields immediately, so we use SerializedObject)
        var controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("networkManager").objectReferenceValue = nm;
        controllerSO.FindProperty("sessionContext").objectReferenceValue = sessionContext;
        controllerSO.FindProperty("authenticator").objectReferenceValue = auth;
        controllerSO.ApplyModifiedProperties();
        Debug.Log("[SetupMenuNetworking] Wired up FishNetNetworkController references");

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
            $"✓ NetworkManager with all FishNet components\n" +
            $"✓ NetworkGameSession, NetworkSessionBridge, FishNetNetworkController ready\n" +
            $"✓ SessionContext and NetworkSessionAuthenticator configured\n" +
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
