using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadCharacter : MonoBehaviour
{
    public GameObject[] characterPrefabs;
    public Transform spawnPoint;
    public TMP_Text label;

    // Start is called before the first frame update
    void Start()
    {
        // Legacy spawner: skip if new selection flow is active or a player already exists
        if (FindObjectOfType<SelectionSpawnRequest>(true) != null)
        {
            Debug.Log("[LoadCharacter] SelectionSpawnRequest detected. Skipping legacy spawn.");
            return;
        }
        if (GameObject.FindGameObjectWithTag("Player") != null)
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
        var clone = Instantiate(prefab, pos, Quaternion.identity);
        if (label != null) label.text = prefab.name;
    }
}
