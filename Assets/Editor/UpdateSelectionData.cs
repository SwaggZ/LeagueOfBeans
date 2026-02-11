using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class UpdateSelectionData : EditorWindow
{
    [MenuItem("LeagueOfBeans/Update Selection Scene Data")]
    public static void ShowWindow()
    {
        GetWindow<UpdateSelectionData>("Update Selection Data");
    }

    private void OnGUI()
    {
        GUILayout.Label("Update Selection Scene Data", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1. Find CharacterSelection component in SampleScene\n" +
            "2. Update character entries with proper IDs (names not numbers)\n" +
            "3. Save the scene\n\n" +
            "This fixes the issue where character ID '6' is sent instead of 'Lux'.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Update Scene Data", GUILayout.Height(30)))
        {
            UpdateSceneData();
        }
    }

    private void UpdateSceneData()
    {
        // Load SampleScene
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Error", "Could not load SampleScene", "OK");
            return;
        }
        
        // Find CharacterSelection component
        CharacterSelection charSelection = GameObject.FindObjectOfType<CharacterSelection>();
        
        if (charSelection == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find CharacterSelection component in scene", "OK");
            return;
        }
        
        // Create SerializedObject to edit the component
        SerializedObject so = new SerializedObject(charSelection);
        SerializedProperty charactersProperty = so.FindProperty("characters");
        
        if (charactersProperty == null || !charactersProperty.isArray)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 'characters' array in CharacterSelection", "OK");
            return;
        }
        
        // Define the proper character IDs
        string[] characterIDs = new string[]
        {
            "Ahri", "Ashe", "Galio", "Caitlyn", "Aphelios", "Jhin", "Lux"
        };
        
        int updatedCount = 0;
        
        // Update each character entry's ID
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
                    Debug.Log($"[UpdateSelection] Updated character {i}: '{oldID}' → '{newID}'");
                    updatedCount++;
                }
            }
        }
        
        // Apply changes
        so.ApplyModifiedProperties();
        
        // Mark scene as dirty and save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        EditorUtility.DisplayDialog(
            "Update Complete",
            $"Updated {updatedCount} character entries in SampleScene.\n\n" +
            "Character IDs are now:\n" +
            string.Join(", ", characterIDs),
            "OK"
        );
        
        Debug.Log($"[UpdateSelection] Complete! Updated {updatedCount} entries");
    }
}
