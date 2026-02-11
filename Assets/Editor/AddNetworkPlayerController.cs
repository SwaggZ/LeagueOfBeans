using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to add NetworkPlayerController component to all character prefabs.
/// This enables proper network ownership checking so only the owning client processes input.
/// 
/// Fixes the "character gliding right through objects" issue by disabling input processing
/// on non-owned character instances.
/// </summary>
public class AddNetworkPlayerController : EditorWindow
{
    [MenuItem("LeagueOfBeans/Add NetworkPlayerController to Characters")]
    public static void ShowWindow()
    {
        GetWindow<AddNetworkPlayerController>("Add Network Player Controller");
    }

    private void OnGUI()
    {
        GUILayout.Label("Add NetworkPlayerController to Character Prefabs", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This will add the NetworkPlayerController component to all character prefabs.\n\n" +
            "NetworkPlayerController enables/disables:\n" +
            "- Character movement (CharacterControl)\n" +
            "- Ability controllers (LuxController, etc.)\n" +
            "- Camera\n\n" +
            "Only for the OWNING client, preventing uncontrolled movement.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("ADD NETWORK PLAYER CONTROLLER", GUILayout.Height(40)))
        {
            AddNetworkPlayerControllerToCharacters();
        }
    }

    private static void AddNetworkPlayerControllerToCharacters()
    {
        var characterNames = new Dictionary<string, string>
        {
            { "Ahri", "Assets/Champions/ahri/Ahri.prefab" },
            { "Ashe", "Assets/Champions/ashe/Ashe.prefab" },
            { "Galio", "Assets/Champions/galio/Galio.prefab" },
            { "Caitlyn", "Assets/Champions/caitlyn/Caitlyn.prefab" },
            { "Aphelios", "Assets/Champions/aphelios/Aphelios.prefab" },
            { "Jhin", "Assets/Champions/jhin/Jhin.prefab" },
            { "Lux", "Assets/Champions/lux/Lux.prefab" }
        };

        int successCount = 0;
        int alreadyHadCount = 0;
        int failCount = 0;

        foreach (var kvp in characterNames)
        {
            string characterName = kvp.Key;
            string prefabPath = kvp.Value;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[AddNetworkPlayerController] Could not load prefab at path: {prefabPath}");
                failCount++;
                continue;
            }

            // Open prefab for editing
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                // Check if NetworkPlayerController already exists
                var existingController = prefabContents.GetComponent<NetworkPlayerController>();
                if (existingController != null)
                {
                    Debug.Log($"[AddNetworkPlayerController] {characterName} already has NetworkPlayerController");
                    alreadyHadCount++;
                }
                else
                {
                    // Add NetworkPlayerController component
                    var controller = prefabContents.AddComponent<NetworkPlayerController>();
                    Debug.Log($"[AddNetworkPlayerController] Added NetworkPlayerController to {characterName}");
                    
                    // Auto-assign components if possible
                    controller.movementController = prefabContents.GetComponent<CharacterControl>();
                    controller.playerCamera = prefabContents.GetComponentInChildren<Camera>(true);
                    
                    if (controller.playerCamera != null)
                        controller.cameraObject = controller.playerCamera.gameObject;
                    
                    // Character-specific controllers will be auto-detected in NetworkPlayerController.Awake()
                    
                    successCount++;
                }

                // Save the prefab
                PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            }
            finally
            {
                // Always unload prefab contents
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"NetworkPlayerController Update Complete!\n\n" +
                        $"✓ Added to {successCount} prefabs\n" +
                        $"⚠ {alreadyHadCount} already had component\n" +
                        $"✗ {failCount} failed\n\n" +
                        $"Characters should now only process input when owned by the local player.";

        EditorUtility.DisplayDialog("Success", message, "OK");
        Debug.Log($"[AddNetworkPlayerController] {message}");
    }
}
