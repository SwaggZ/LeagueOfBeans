// Creates Jhin projectile/ability prefabs via menu commands so you don't have to hand-edit YAML GUIDs.
// Menu: Tools/Jhin/Create Prefabs
// - Generates prefabs (Bullet, SpecialBullet, Mine, BounceBullet) under Assets/Champions/jhin/Prefabs
// - Each prefab contains a visual mesh and the appropriate component
// - If a JhinController is selected, assigns the generated prefabs to it
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class JhinPrefabBuilder
{
    private const string PrefabFolder = "Assets/Champions/jhin/Prefabs";

    [MenuItem("Tools/Jhin/Create Prefabs")]
    public static void CreatePrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();
        }

        var bullet = CreateBulletPrefab("Projectile_Bullet", new Color(0.9f, 0.7f, 0.3f), false);
        var specialBullet = CreateBulletPrefab("Projectile_SpecialBullet", new Color(1f, 0.2f, 0.2f), true);
        var mine = CreateMinePrefab("Ability_Mine", new Color(0.4f, 0.2f, 0.1f));
        var bounceBullet = CreateBounceBulletPrefab("Projectile_BounceBullet", new Color(0.8f, 0.4f, 0.9f));

        // Try to assign to selected JhinController
        var selected = Selection.activeGameObject;
        if (selected != null)
        {
            var ctrl = selected.GetComponent<JhinController>();
            if (ctrl != null)
            {
                Undo.RecordObject(ctrl, "Assign Jhin Prefabs");
                ctrl.bulletPrefab = bullet;
                ctrl.specialBulletPrefab = specialBullet;
                ctrl.minePrefab = mine;
                ctrl.bounceBulletPrefab = bounceBullet;
                EditorUtility.SetDirty(ctrl);
                Debug.Log("[Jhin] Assigned generated prefabs to selected JhinController.");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Jhin] Prefabs created at " + PrefabFolder);
    }

    private static GameObject CreateBulletPrefab(string name, Color tint, bool empowered)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint;
            mr.sharedMaterial = mat;
        }
        
        // Empowered bullets are larger
        go.transform.localScale = empowered 
            ? new Vector3(0.15f, 0.4f, 0.15f) 
            : new Vector3(0.1f, 0.3f, 0.1f);

        go.AddComponent<JhinProjectile>();

        string path = Path.Combine(PrefabFolder, name + ".prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateMinePrefab(string name, Color tint)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        
        // Add rigidbody for physics throw (like Caitlyn's throwable)

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint;
            mr.sharedMaterial = mat;
        }
        
        go.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);

        var rb = go.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        go.AddComponent<JhinMine>();

        string path = Path.Combine(PrefabFolder, name + ".prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateBounceBulletPrefab(string name, Color tint)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint;
            mr.sharedMaterial = mat;
        }
        
        go.transform.localScale = Vector3.one * 0.25f;

        go.AddComponent<JhinBounceBullet>();

        string path = Path.Combine(PrefabFolder, name + ".prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
#endif
