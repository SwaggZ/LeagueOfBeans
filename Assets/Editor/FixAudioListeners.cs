using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class FixAudioListeners : EditorWindow
{
    [MenuItem("LeagueOfBeans/Fix Audio Listeners")]
    public static void ShowWindow()
    {
        GetWindow<FixAudioListeners>("Fix Audio Listeners");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Audio Listeners", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1. Find all Audio Listener components in the scene\n" +
            "2. Keep only one active (usually on Main Camera)\n" +
            "3. Remove or disable extra listeners\n" +
            "4. Save the scene",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Fix Audio Listeners", GUILayout.Height(30)))
        {
            FixListeners();
        }
    }

    private void FixListeners()
    {
        // Load SampleScene
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Error", "Could not load SampleScene", "OK");
            return;
        }
        
        // Find all Audio Listeners
        AudioListener[] listeners = GameObject.FindObjectsOfType<AudioListener>();
        
        if (listeners.Length <= 1)
        {
            EditorUtility.DisplayDialog(
                "Audio Listeners OK",
                $"Found {listeners.Length} audio listener(s). No action needed.",
                "OK"
            );
            return;
        }
        
        Debug.Log($"[FixAudioListeners] Found {listeners.Length} audio listeners");
        
        // Prioritize Main Camera, then Selection Camera
        AudioListener keepListener = null;
        List<AudioListener> removeListeners = new List<AudioListener>();
        
        foreach (var listener in listeners)
        {
            Debug.Log($"[FixAudioListeners] Found listener on: {listener.gameObject.name}");
            
            if (keepListener == null)
            {
                // Keep the first one we find on Main Camera
                if (listener.gameObject.name.Contains("Main Camera"))
                {
                    keepListener = listener;
                    continue;
                }
            }
            
            if (keepListener == null)
            {
                // If no Main Camera, keep first one
                keepListener = listener;
            }
            else
            {
                removeListeners.Add(listener);
            }
        }
        
        // Remove extra listeners
        int removedCount = 0;
        foreach (var listener in removeListeners)
        {
            Debug.Log($"[FixAudioListeners] Removing listener from: {listener.gameObject.name}");
            DestroyImmediate(listener);
            removedCount++;
        }
        
        // Mark scene as dirty and save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        EditorUtility.DisplayDialog(
            "Audio Listeners Fixed",
            $"Removed {removedCount} extra audio listeners.\n\n" +
            $"Kept listener on: {keepListener.gameObject.name}",
            "OK"
        );
        
        Debug.Log($"[FixAudioListeners] Complete! Removed {removedCount} listeners, kept 1 on {keepListener.gameObject.name}");
    }
}
