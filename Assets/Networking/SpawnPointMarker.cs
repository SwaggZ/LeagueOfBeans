using UnityEngine;

/// <summary>
/// Optional spawn point marker to avoid tag-based lookups.
/// </summary>
public class SpawnPointMarker : MonoBehaviour
{
    [Tooltip("Optional key used for lookup (can match spawnPointTag or spawnPointName).")]
    public string key;
}
