using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing;

/// <summary>
/// Removes NetworkManager and networking objects from SampleScene.
/// The NetworkManager from Menu scene will persist here, so we don't need a local one.
/// </summary>
public class CleanupSampleScene
{
    [MenuItem("Tools/Cleanup SampleScene (Remove NetworkManager)")]
    public static void RemoveNetworkManager()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.name.Contains("SampleScene"))
        {
            EditorUtility.DisplayDialog("Error", "Please open SampleScene.unity before running this tool!", "OK");
            return;
        }

        Debug.Log("[CleanupSampleScene] Removing NetworkManager from SampleScene...");

        // Find and destroy NetworkManager in this scene
        var networkManagers = Object.FindObjectsOfType<NetworkManager>();
        int removed = 0;

        foreach (var nm in networkManagers)
        {
            // Only remove if it's in the active scene (not DontDestroyOnLoad)
            if (nm.gameObject.scene == activeScene)
            {
                Debug.Log($"[CleanupSampleScene] Removing NetworkManager: {nm.gameObject.name}");
                Object.DestroyImmediate(nm.gameObject);
                removed++;
            }
        }

        // Also remove any orphaned NetworkGameSession or NetworkSessionBridge
        var networkSessions = Object.FindObjectsOfType<NetworkGameSession>();
        foreach (var ngs in networkSessions)
        {
            if (ngs.gameObject.scene == activeScene)
            {
                Debug.Log($"[CleanupSampleScene] Removing NetworkGameSession: {ngs.gameObject.name}");
                Object.DestroyImmediate(ngs.gameObject);
                removed++;
            }
        }

        var bridges = Object.FindObjectsOfType<NetworkSessionBridge>();
        foreach (var bridge in bridges)
        {
            if (bridge.gameObject.scene == activeScene)
            {
                Debug.Log($"[CleanupSampleScene] Removing NetworkSessionBridge: {bridge.gameObject.name}");
                Object.DestroyImmediate(bridge.gameObject);
                removed++;
            }
        }

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"[CleanupSampleScene] ✓ Removed {removed} networking object(s)");
            EditorUtility.DisplayDialog("Success",
                $"✓ Removed {removed} networking object(s) from SampleScene\n\n" +
                "The NetworkManager from Menu scene will persist here automatically via DontDestroyOnLoad.\n\n" +
                "Save the scene (Ctrl+S) to apply changes.",
                "OK");
        }
        else
        {
            Debug.Log("[CleanupSampleScene] ✓ No NetworkManager found in SampleScene (already clean)");
            EditorUtility.DisplayDialog("Info",
                "SampleScene is already clean - no local NetworkManager found.\n\n" +
                "The NetworkManager from Menu scene will persist here automatically.",
                "OK");
        }
    }
}
