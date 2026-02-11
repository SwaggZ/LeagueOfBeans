using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class FixSpawnPoints : EditorWindow
{
    [MenuItem("LeagueOfBeans/Fix Spawn Points")]
    public static void ShowWindow()
    {
        GetWindow<FixSpawnPoints>("Fix Spawn Points");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Spawn Points", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1. Find all SpawnPointMarker components in SampleScene\n" +
            "2. Set their 'key' property to 'PlayerSpawn'\n" +
            "3. Save the scene\n\n" +
            "This fixes the issue where spawn points don't pass the filter.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Fix All Spawn Points", GUILayout.Height(30)))
        {
            FixAllSpawnPoints();
        }
    }

    private void FixAllSpawnPoints()
    {
        // Load SampleScene
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Error", "Could not load SampleScene", "OK");
            return;
        }
        
        // Find all SpawnPointMarker components
        SpawnPointMarker[] markers = GameObject.FindObjectsOfType<SpawnPointMarker>();
        
        if (markers.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Spawn Points",
                "No SpawnPointMarker components found in scene.\n\n" +
                "Use Tools → Create Spawn Points to add them first.",
                "OK"
            );
            return;
        }
        
        int updatedCount = 0;
        
        foreach (var marker in markers)
        {
            SerializedObject so = new SerializedObject(marker);
            SerializedProperty keyProperty = so.FindProperty("key");
            
            if (keyProperty != null)
            {
                string oldKey = keyProperty.stringValue;
                keyProperty.stringValue = "PlayerSpawn";
                so.ApplyModifiedProperties();
                
                Debug.Log($"[FixSpawnPoints] Updated '{marker.gameObject.name}': '{oldKey}' → 'PlayerSpawn'");
                updatedCount++;
            }
        }
        
        // Mark scene as dirty and save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        EditorUtility.DisplayDialog(
            "Spawn Points Fixed",
            $"Updated {updatedCount} spawn point markers.\n\n" +
            "All markers now have key = 'PlayerSpawn'",
            "OK"
        );
        
        Debug.Log($"[FixSpawnPoints] Complete! Updated {updatedCount} markers");
    }
}
