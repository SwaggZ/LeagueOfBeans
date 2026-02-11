using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing;
using FishNet.Object;

/// <summary>
/// Fixes common NetworkManager configuration issues.
/// The NetworkManager itself should NOT have a NetworkObject component.
/// </summary>
public class FixNetworkManagerSetup
{
    [MenuItem("Tools/Fix NetworkManager (Remove NetworkObject)")]
    public static void FixNetworkManager()
    {
        var activeScene = SceneManager.GetActiveScene();
        Debug.Log($"[FixNetworkManagerSetup] Checking {activeScene.name} for NetworkManager issues...");

        var nm = Object.FindObjectOfType<NetworkManager>();
        if (nm == null)
        {
            EditorUtility.DisplayDialog("Info", "No NetworkManager found in this scene.", "OK");
            return;
        }

        // Check for and remove NetworkObject
        var networkObject = nm.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            Debug.LogWarning($"[FixNetworkManagerSetup] Found NetworkObject on NetworkManager! Removing...");
            Object.DestroyImmediate(networkObject);
            EditorUtility.SetDirty(nm.gameObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[FixNetworkManagerSetup] NetworkObject removed successfully");
            EditorUtility.DisplayDialog("Fixed", "✓ NetworkObject component removed from NetworkManager", "OK");
        }
        else
        {
            Debug.Log("[FixNetworkManagerSetup] No NetworkObject found on NetworkManager - OK");
            EditorUtility.DisplayDialog("OK", "NetworkManager is properly configured (no NetworkObject)", "OK");
        }
    }
}
