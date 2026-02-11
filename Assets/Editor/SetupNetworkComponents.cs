using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing;

/// <summary>
/// Editor tool to setup NetworkManager and NetworkGameSession in a scene.
/// Usage: Tools → Setup NetworkManager and NetworkGameSession
/// </summary>
public class SetupNetworkComponents
{
    [MenuItem("Tools/Setup NetworkManager and NetworkGameSession")]
    public static void SetupNetworkComponents_()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Error", "No active scene loaded!", "OK");
            return;
        }

        Debug.Log($"[SetupNetworkComponents] Setting up in scene: {activeScene.name}");

        // Check if NetworkManager already exists
        NetworkManager nm = Object.FindObjectOfType<NetworkManager>();
        if (nm == null)
        {
            // Create NetworkManager GameObject
            GameObject nmGo = new GameObject("NetworkManager");
            nm = nmGo.AddComponent<NetworkManager>();
            Debug.Log("[SetupNetworkComponents] Created new NetworkManager");
        }
        else
        {
            Debug.Log("[SetupNetworkComponents] Found existing NetworkManager");
        }

        // Check if NetworkGameSession already exists
        NetworkGameSession ngs = nm.GetComponent<NetworkGameSession>();
        if (ngs == null)
        {
            ngs = nm.gameObject.AddComponent<NetworkGameSession>();
            Debug.Log("[SetupNetworkComponents] Added NetworkGameSession to NetworkManager");
        }
        else
        {
            Debug.Log("[SetupNetworkComponents] NetworkGameSession already exists");
        }

        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.DisplayDialog("Success", $"NetworkManager and NetworkGameSession are now in {activeScene.name}!", "OK");
    }
}
