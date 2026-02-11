using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class CreateSpawnPoints
{
    private const string MenuPath = "Tools/Create Spawn Points";

    [MenuItem(MenuPath)]
    public static void CreateSpawnPointsFromSelection()
    {
        var selection = Selection.gameObjects;
        if (selection.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select GameObjects to convert to spawn points.", "OK");
            return;
        }

        List<GameObject> created = new List<GameObject>();
        
        foreach (var go in selection)
        {
            if (go == null) continue;
            
            var marker = go.GetComponent<SpawnPointMarker>();
            if (marker == null)
            {
                marker = go.AddComponent<SpawnPointMarker>();
                if (string.IsNullOrEmpty(marker.key))
                    marker.key = go.name;
                created.Add(go);
                EditorUtility.SetDirty(go);
                Debug.Log($"Added SpawnPointMarker to {go.name} with key '{marker.key}'");
            }
        }

        EditorUtility.DisplayDialog("Success", $"Added SpawnPointMarker to {created.Count} object(s).", "OK");
    }

    [MenuItem(MenuPath, validate = true)]
    private static bool ValidateCreateSpawnPoints()
    {
        return Selection.gameObjects.Length > 0;
    }
}
