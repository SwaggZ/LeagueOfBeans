using UnityEngine;

/// <summary>
/// Non-static spawn/despawn entry point to ease FishNet migration.
/// </summary>
public class SpawnService : MonoBehaviour
{
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;
        return parent == null
            ? Instantiate(prefab, position, rotation)
            : Instantiate(prefab, position, rotation, parent);
    }

    public void Despawn(GameObject go)
    {
        if (go == null) return;
        Destroy(go);
    }

    public void Despawn(GameObject go, float delay)
    {
        if (go == null) return;
        Destroy(go, delay);
    }
}
