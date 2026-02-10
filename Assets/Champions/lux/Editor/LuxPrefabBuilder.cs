using UnityEngine;
using UnityEditor;
using System.IO;

public class LuxPrefabBuilder
{
    private const string PrefabFolder = "Assets/Champions/lux/Prefabs";

    [MenuItem("Tools/Lux/Create Prefabs")]
    public static void CreateAllPrefabs()
    {
        if (!Directory.Exists(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
        }

        // Create all ability prefabs
        var lightOrb = CreateLightOrbPrefab("Ability_LightOrb", new Color(1f, 0.95f, 0.6f));
        var wand = CreateWandPrefab("Ability_Wand", new Color(0.9f, 0.8f, 1f));
        var stunOrb = CreateStunOrbPrefab("Ability_StunOrb", new Color(1f, 0.8f, 0.9f));
        var carpetThrowable = CreateCarpetThrowablePrefab("Ability_CarpetThrowable", new Color(1f, 1f, 0.5f));

        AssetDatabase.Refresh();

        Debug.Log($"Lux prefabs created in {PrefabFolder}:");
        Debug.Log($"  - LightOrb: {lightOrb.name}");
        Debug.Log($"  - Wand: {wand.name}");
        Debug.Log($"  - StunOrb: {stunOrb.name}");
        Debug.Log($"  - CarpetThrowable: {carpetThrowable.name}");
    }

    private static GameObject CreateLightOrbPrefab(string name, Color tint)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint;
            mat.SetFloat("_Smoothness", 1f);
            mr.sharedMaterial = mat;
        }

        go.transform.localScale = Vector3.one * 0.3f;
        go.AddComponent<LuxLightOrb>();

        string path = Path.Combine(PrefabFolder, name + ".prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateWandPrefab(string name, Color tint)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint;
            mat.SetFloat("_Smoothness", 1f);
            mr.sharedMaterial = mat;
        }

        go.transform.localScale = new Vector3(0.15f, 0.6f, 0.15f);
        go.AddComponent<LuxWand>();

        string path = Path.Combine(PrefabFolder, name + ".prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateStunOrbPrefab(string name, Color tint)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint;
            mat.SetFloat("_Smoothness", 1f);
            mr.sharedMaterial = mat;
        }

        go.transform.localScale = Vector3.one * 0.4f;
        go.AddComponent<LuxStunOrb>();

        string path = Path.Combine(PrefabFolder, name + ".prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateCarpetThrowablePrefab(string name, Color tint)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint;
            mat.SetFloat("_Smoothness", 1f);
            mr.sharedMaterial = mat;
        }

        go.transform.localScale = Vector3.one * 0.35f;

        // Add rigidbody for physics throw
        var rb = go.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        go.AddComponent<LuxCarpetThrowable>();

        string path = Path.Combine(PrefabFolder, name + ".prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
