using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Managing;
using TMPro;

public class LoadCharacter : MonoBehaviour
{
    public GameObject[] characterPrefabs;
    public Transform spawnPoint;
    public TMP_Text label;
    [SerializeField] private SpawnService spawnService;

    // Start is called before the first frame update
    void Start()
    {
        var networkManager = FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
        if (networkManager != null && (networkManager.IsClientStarted || networkManager.IsServerStarted))
        {
            Debug.Log("[LoadCharacter] FishNet active. Skipping legacy spawn.");
            return;
        }

        // Legacy spawner: skip if new selection flow is active or a player already exists
        if (FindFirstObjectByType<SelectionSpawnRequest>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[LoadCharacter] SelectionSpawnRequest detected. Skipping legacy spawn.");
            return;
        }
        var existingPlayer = FindFirstObjectByType<PlayerRegistration>(FindObjectsInactive.Include);
        if (existingPlayer == null)
        {
            var existingControl = FindFirstObjectByType<CharacterControl>(FindObjectsInactive.Include);
            if (existingControl != null) existingPlayer = existingControl.GetComponent<PlayerRegistration>();
        }
        if (existingPlayer != null)
        {
            Debug.Log("[LoadCharacter] Player already present. Skipping legacy spawn.");
            return;
        }

        if (characterPrefabs == null || characterPrefabs.Length == 0)
        {
            Debug.LogError("[LoadCharacter] No characterPrefabs assigned.");
            return;
        }

        int selectedCharacter = Mathf.Clamp(PlayerPrefs.GetInt("selectedCharacter", 0), 0, characterPrefabs.Length - 1);
        GameObject prefab = characterPrefabs[selectedCharacter];
        if (prefab == null)
        {
            Debug.LogError($"[LoadCharacter] Prefab at index {selectedCharacter} is null.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        var spawner = spawnService != null ? spawnService : FindFirstObjectByType<SpawnService>(FindObjectsInactive.Include);
        var clone = spawner != null
            ? spawner.Spawn(prefab, pos, Quaternion.identity)
            : Instantiate(prefab, pos, Quaternion.identity);
        if (label != null) label.text = prefab.name;
    }
}
