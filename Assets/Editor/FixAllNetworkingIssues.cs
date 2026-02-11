using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using FishNet.Object;
using System.Collections.Generic;

public class FixAllNetworkingIssues : EditorWindow
{
    [MenuItem("LeagueOfBeans/Fix All Networking Issues")]
    public static void ShowWindow()
    {
        GetWindow<FixAllNetworkingIssues>("Fix All Issues");
    }

    private Vector2 scrollPos;
    private string logOutput = "";

    private void OnGUI()
    {
        GUILayout.Label("Fix All Networking Issues", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool will automatically fix all known networking issues:\n\n" +
            "1. Add NetworkObject to character prefabs\n" +
            "2. Update CharacterSelection scene data\n" +
            "3. Fix spawn point keys\n" +
            "4. Remove duplicate audio listeners\n\n" +
            "Click the button below to run all fixes.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("FIX ALL ISSUES", GUILayout.Height(40)))
        {
            logOutput = "";
            FixAllIssues();
        }
        
        if (!string.IsNullOrEmpty(logOutput))
        {
            GUILayout.Space(10);
            GUILayout.Label("Results:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            EditorGUILayout.TextArea(logOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    private void Log(string message)
    {
        logOutput += message + "\n";
        Debug.Log(message);
    }

    private void FixAllIssues()
    {
        Log("=== STARTING AUTOMATIC FIXES ===\n");
        
        // Step 1: Fix character prefabs
        Log("Step 1: Adding NetworkObject to character prefabs...");
        int prefabsFixed = FixCharacterPrefabs();
        Log($"  ✓ Fixed {prefabsFixed} prefab(s)\n");
        
        // Step 2: Load SampleScene
        Log("Step 2: Loading SampleScene...");
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Log("  ✗ ERROR: Could not load SampleScene!");
            return;
        }
        Log("  ✓ Scene loaded\n");
        
        // Step 3: Update CharacterSelection data
        Log("Step 3: Updating CharacterSelection scene data...");
        int selectionEntriesUpdated = UpdateCharacterSelection(scene);
        Log($"  ✓ Updated {selectionEntriesUpdated} character entries\n");
        
        // Step 4: Fix spawn points
        Log("Step 4: Fixing spawn point keys...");
        int spawnPointsFixed = FixSpawnPoints(scene);
        Log($"  ✓ Fixed {spawnPointsFixed} spawn point(s)\n");
        
        // Step 5: Fix audio listeners
        Log("Step 5: Fixing audio listeners...");
        int listenersRemoved = FixAudioListeners(scene);
        Log($"  ✓ Removed {listenersRemoved} extra listener(s)\n");
        
        // Save scene and assets
        Log("Step 6: Saving changes...");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Log("  ✓ All changes saved\n");
        
        Log("=== ALL FIXES COMPLETE ===\n");
        Log("You can now test the game!");
        
        EditorUtility.DisplayDialog(
            "All Fixes Complete!",
            $"Successfully applied all fixes:\n\n" +
            $"• {prefabsFixed} character prefabs fixed\n" +
            $"• {selectionEntriesUpdated} character entries updated\n" +
            $"• {spawnPointsFixed} spawn points fixed\n" +
            $"• {listenersRemoved} audio listeners removed\n\n" +
            "You can now test the game!",
            "OK"
        );
    }

    private int FixCharacterPrefabs()
    {
        // Character names with folder name (lowercase) and prefab name (capitalized)
        var characterPaths = new Dictionary<string, string>()
        {
            { "Ahri", "Assets/Champions/ahri/Ahri.prefab" },
            { "Ashe", "Assets/Champions/ashe/Ashe.prefab" },
            { "Galio", "Assets/Champions/galio/Galio.prefab" },
            { "Caitlyn", "Assets/Champions/caitlyn/Caitlyn.prefab" },
            { "Aphelios", "Assets/Champions/aphelios/Aphelios.prefab" },
            { "Jhin", "Assets/Champions/jhin/Jhin.prefab" },
            { "Lux", "Assets/Champions/lux/Lux.prefab" }
        };

        int fixedCount = 0;
        
        foreach (var kvp in characterPaths)
        {
            string characterName = kvp.Key;
            string prefabPath = kvp.Value;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Log($"  ! Warning: Could not find prefab at {prefabPath}");
                continue;
            }
            
            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                // Already has NetworkObject
                continue;
            }
            
            // Open prefab for editing
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            
            // Add NetworkObject
            netObj = prefabRoot.AddComponent<NetworkObject>();
            
            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            
            Log($"  • Added NetworkObject to {characterName}");
            fixedCount++;
        }
        
        return fixedCount;
    }

    private int UpdateCharacterSelection(Scene scene)
    {
        CharacterSelection charSelection = GameObject.FindObjectOfType<CharacterSelection>();
        
        if (charSelection == null)
        {
            Log("  ! Warning: Could not find CharacterSelection component");
            return 0;
        }
        
        SerializedObject so = new SerializedObject(charSelection);
        SerializedProperty charactersProperty = so.FindProperty("characters");
        
        if (charactersProperty == null || !charactersProperty.isArray)
        {
            Log("  ! Warning: Could not find 'characters' array");
            return 0;
        }
        
        string[] characterIDs = new string[]
        {
            "Ahri", "Ashe", "Galio", "Caitlyn", "Aphelios", "Jhin", "Lux"
        };
        
        int updatedCount = 0;
        
        for (int i = 0; i < charactersProperty.arraySize && i < characterIDs.Length; i++)
        {
            SerializedProperty characterEntry = charactersProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = characterEntry.FindPropertyRelative("id");
            
            if (idProperty != null)
            {
                string oldID = idProperty.stringValue;
                string newID = characterIDs[i];
                
                if (oldID != newID)
                {
                    idProperty.stringValue = newID;
                    Log($"  • Updated entry {i}: '{oldID}' → '{newID}'");
                    updatedCount++;
                }
            }
        }
        
        so.ApplyModifiedProperties();
        return updatedCount;
    }

    private int FixSpawnPoints(Scene scene)
    {
        SpawnPointMarker[] markers = GameObject.FindObjectsOfType<SpawnPointMarker>();
        
        if (markers.Length == 0)
        {
            Log("  ! Warning: No SpawnPointMarker components found");
            return 0;
        }
        
        int updatedCount = 0;
        
        foreach (var marker in markers)
        {
            SerializedObject so = new SerializedObject(marker);
            SerializedProperty keyProperty = so.FindProperty("key");
            
            if (keyProperty != null)
            {
                string oldKey = keyProperty.stringValue;
                if (oldKey != "PlayerSpawn")
                {
                    keyProperty.stringValue = "PlayerSpawn";
                    so.ApplyModifiedProperties();
                    Log($"  • Updated '{marker.gameObject.name}': '{oldKey}' → 'PlayerSpawn'");
                    updatedCount++;
                }
            }
        }
        
        return updatedCount;
    }

    private int FixAudioListeners(Scene scene)
    {
        AudioListener[] listeners = GameObject.FindObjectsOfType<AudioListener>();
        
        if (listeners.Length <= 1)
        {
            return 0;
        }
        
        AudioListener keepListener = null;
        List<AudioListener> removeListeners = new List<AudioListener>();
        
        foreach (var listener in listeners)
        {
            if (keepListener == null && listener.gameObject.name.Contains("Main Camera"))
            {
                keepListener = listener;
                continue;
            }
            
            if (keepListener == null)
            {
                keepListener = listener;
            }
            else
            {
                removeListeners.Add(listener);
            }
        }
        
        foreach (var listener in removeListeners)
        {
            Log($"  • Removing listener from: {listener.gameObject.name}");
            DestroyImmediate(listener);
        }
        
        return removeListeners.Count;
    }
}
