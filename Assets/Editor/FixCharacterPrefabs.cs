using UnityEngine;
using UnityEditor;
using FishNet.Object;
using System.Collections.Generic;

public class FixCharacterPrefabs : EditorWindow
{
    [MenuItem("LeagueOfBeans/Fix Character Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<FixCharacterPrefabs>("Fix Character Prefabs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Character Prefabs", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1. Add NetworkObject components to all character prefabs\n" +
            "2. Configure NetworkObject settings for FishNet\n" +
            "3. Save all changes",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Fix All Character Prefabs", GUILayout.Height(30)))
        {
            FixPrefabs();
        }
    }

    private void FixPrefabs()
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
        int skippedCount = 0;
        
        foreach (var kvp in characterPaths)
        {
            string characterName = kvp.Key;
            string prefabPath = kvp.Value;
            
            // Load the prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogWarning($"Could not find prefab at: {prefabPath}");
                skippedCount++;
                continue;
            }
            
            // Check if it already has NetworkObject
            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                Debug.Log($"[FixPrefabs] {characterName} already has NetworkObject, skipping");
                skippedCount++;
                continue;
            }
            
            // Open the prefab for editing
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            
            // Add NetworkObject component
            netObj = prefabRoot.AddComponent<NetworkObject>();
            
            // Configure NetworkObject
            // Set default settings for a player-controlled object
            SerializedObject so = new SerializedObject(netObj);
            
            // Find and set IsNetworked to true (should be default but let's be explicit)
            SerializedProperty isNetworkedProp = so.FindProperty("_isNetworked");
            if (isNetworkedProp != null)
                isNetworkedProp.boolValue = true;
            
            // Find and set IsSpawnable to true
            SerializedProperty isSpawnableProp = so.FindProperty("_isSpawnable");
            if (isSpawnableProp != null)
                isSpawnableProp.boolValue = true;
            
            so.ApplyModifiedPropertiesWithoutUndo();
            
            // Save the prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            
            Debug.Log($"[FixPrefabs] Added NetworkObject to {characterName}");
            fixedCount++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog(
            "Character Prefabs Fixed",
            $"Successfully fixed {fixedCount} prefabs.\n" +
            $"Skipped {skippedCount} prefabs (already had NetworkObject).",
            "OK"
        );
        
        Debug.Log($"[FixPrefabs] Complete! Fixed {fixedCount}, Skipped {skippedCount}");
    }
}
