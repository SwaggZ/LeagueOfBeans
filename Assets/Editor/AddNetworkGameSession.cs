using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing;

/// <summary>
/// Editor tool to add NetworkGameSession component to SampleScene.
/// Usage: Tools → Add NetworkGameSession to Scene
/// </summary>
public class AddNetworkGameSession
{
    [MenuItem("Tools/Add NetworkGameSession to Scene")]
    public static void AddNetworkGameSessionToScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Error", "No active scene loaded!", "OK");
            return;
        }

        Debug.Log($"[AddNetworkGameSession] Adding to scene: {activeScene.name}");

        // Try to find NetworkManager first
        NetworkManager nm = Object.FindObjectOfType<NetworkManager>();
        if (nm == null)
        {
            EditorUtility.DisplayDialog("Error", "NetworkManager not found in scene! Make sure it exists before adding NetworkGameSession.", "OK");
            return;
        }

        // Check if NetworkGameSession already exists
        if (nm.GetComponent<NetworkGameSession>() != null)
        {
            EditorUtility.DisplayDialog("Info", "NetworkGameSession already exists on NetworkManager!", "OK");
            return;
        }

        // Add NetworkGameSession component
        NetworkGameSession ngs = nm.gameObject.AddComponent<NetworkGameSession>();
        Debug.Log($"[AddNetworkGameSession] Added NetworkGameSession to {nm.gameObject.name}");

        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog("Success", "NetworkGameSession added to NetworkManager!", "OK");
    }
}
