using UnityEngine;

// Attach this to each preview character in the selection scene and assign the corresponding gameplay prefab.
[DisallowMultipleComponent]
public class PreviewBinding : MonoBehaviour
{
    public GameObject gameplayPrefab;
}
