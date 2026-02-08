// Creates Aphelios projectile prefabs via menu commands so you don't have to hand-edit YAML GUIDs.
// Menu: Tools/Aphelios/Create Projectile Prefabs
// - Generates 4 prefabs (Sniper/Scythe/Orbs/Flamethrower) under Assets/Champions/aphelios/Prefabs
// - Each prefab contains a small visual sphere and the ApheliosProjectile component
// - If an ApheliosController is selected, assigns the generated prefabs to it
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class ApheliosPrefabBuilder
{
    private const string PrefabFolder = "Assets/Champions/aphelios/Prefabs";

    [MenuItem("Tools/Aphelios/Create Projectile Prefabs")]
    public static void CreateProjectiles()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();
        }

        var sniper = CreateProjectilePrefab("Projectile_Sniper", new Color(0.8f, 0.9f, 1f), 0.12f);
        var scythe = CreateProjectilePrefab("Projectile_Scythe", new Color(0.7f, 1f, 0.7f), 0.14f);
        var orbs = CreateProjectilePrefab("Projectile_Orbs", new Color(0.7f, 0.8f, 1f), 0.16f);
        var flame = CreateProjectilePrefab("Projectile_Flamethrower", new Color(1f, 0.6f, 0.2f), 0.18f);

        // Try to assign to selected ApheliosController
        var selected = Selection.activeGameObject;
        if (selected != null)
        {
            var ctrl = selected.GetComponent<ApheliosController>();
            if (ctrl != null)
            {
                Undo.RecordObject(ctrl, "Assign Aphelios Prefabs");
                ctrl.sniperProjectile = sniper;
                ctrl.scytheProjectile = scythe;
                ctrl.orbsProjectile = orbs;
                ctrl.flameProjectile = flame;
                EditorUtility.SetDirty(ctrl);
                Debug.Log("[Aphelios] Assigned generated projectiles to selected ApheliosController.");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Aphelios] Projectile prefabs created at " + PrefabFolder);
    }

    private static GameObject CreateProjectilePrefab(string name, Color tint, float scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // use raycast in script

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = tint;
            mr.sharedMaterial = mat;
        }
        go.transform.localScale = Vector3.one * scale;

        var proj = go.AddComponent<ApheliosProjectile>();

        string path = Path.Combine(PrefabFolder, name + ".prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
#endif
