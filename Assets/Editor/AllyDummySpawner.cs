using UnityEngine;
using UnityEditor;

public class AllyDummySpawner
{
    [MenuItem("Tools/Spawn/Ally Dummy")]
    public static void SpawnAllyDummy()
    {
        // Find spawn position - in front of camera or at scene view
        Vector3 spawnPos = Vector3.zero;
        
        if (SceneView.lastActiveSceneView != null)
        {
            // Spawn in front of scene camera
            Camera sceneCam = SceneView.lastActiveSceneView.camera;
            spawnPos = sceneCam.transform.position + sceneCam.transform.forward * 5f;
            spawnPos.y = 0f; // Ground level
        }
        else if (Camera.main != null)
        {
            spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 5f;
            spawnPos.y = 0f;
        }

        CreateAllyDummy(spawnPos, "Ally Dummy");
    }

    [MenuItem("Tools/Spawn/Ally Dummy Group (3)")]
    public static void SpawnAllyDummyGroup()
    {
        Vector3 basePos = Vector3.zero;
        
        if (SceneView.lastActiveSceneView != null)
        {
            Camera sceneCam = SceneView.lastActiveSceneView.camera;
            basePos = sceneCam.transform.position + sceneCam.transform.forward * 5f;
            basePos.y = 0f;
        }

        CreateAllyDummy(basePos + Vector3.left * 3f, "Ally 1");
        CreateAllyDummy(basePos, "Ally 2");
        CreateAllyDummy(basePos + Vector3.right * 3f, "Ally 3");
    }

    public static GameObject CreateAllyDummy(Vector3 position, string name)
    {
        // Create capsule body
        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        dummy.name = name;
        dummy.tag = "Ally";
        dummy.transform.position = position;

        // Remove default collider (CharacterController will handle collision)
        var defaultCol = dummy.GetComponent<Collider>();
        if (defaultCol != null) Object.DestroyImmediate(defaultCol);

        // Style - green color for allies
        var mr = dummy.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.3f, 0.8f, 0.3f); // Green
            mr.sharedMaterial = mat;
        }

        // Add components
        var cc = dummy.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = Vector3.up;

        // Add trigger collider for physics detection (CharacterController doesn't work with OverlapSphere)
        var capsuleCollider = dummy.AddComponent<CapsuleCollider>();
        capsuleCollider.isTrigger = true;
        capsuleCollider.height = 2f;
        capsuleCollider.radius = 0.5f;
        capsuleCollider.center = Vector3.up;

        var hp = dummy.AddComponent<HealthSystem>();
        hp.maxHealth = 100f;

        var tracker = dummy.AddComponent<ModifierTracker>();

        // Create ground check
        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(dummy.transform);
        groundCheck.transform.localPosition = Vector3.zero;

        var ally = dummy.AddComponent<AllyDummy>();
        ally.allyName = name;
        ally.groundCheck = groundCheck.transform;
        // Set ground mask to "Ground" layer (layer 6 typically, or find by name)
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            ally.groundMask = 1 << groundLayer;
        }
        else
        {
            // Fallback: include default and ground-like layers
            ally.groundMask = LayerMask.GetMask("Default", "Ground", "ground");
        }

        // Register undo
        Undo.RegisterCreatedObjectUndo(dummy, "Spawn Ally Dummy");

        Debug.Log($"Spawned {name} at {position}");
        
        // Select the spawned object
        Selection.activeGameObject = dummy;

        return dummy;
    }
}
