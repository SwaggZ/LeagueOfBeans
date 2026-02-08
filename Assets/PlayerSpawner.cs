using UnityEngine;

// Drop this in your gameplay scene. Assign the character prefabs array and an optional spawn point.
public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("Assign the character prefabs in the same order as your CharacterSelection.characters.")]
    public GameObject[] characterPrefabs;

    [Tooltip("Optional spawn transform. If null, spawns at (0,0,0).")]
    public Transform spawnPoint;

    private void Awake()
    {
        // If a SelectionSpawnRequest exists (new selection flow), skip default spawn to avoid duplicates.
        var pendingSelectionSpawn = FindObjectOfType<SelectionSpawnRequest>(true);
        if (pendingSelectionSpawn != null)
        {
            Debug.Log("[Spawner] SelectionSpawnRequest detected. Skipping default PlayerSpawner spawn.");
            return;
        }

        // If a player is already in the scene, do nothing (safety for additive loads).
        var existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            Debug.Log("[Spawner] Existing Player found in scene. Skipping default spawn.");
            return;
        }

        int idx = PlayerPrefs.GetInt("selectedCharacter", 0);
        string selName = PlayerPrefs.GetString("selectedCharacterName", string.Empty);
        Debug.Log($"[Spawner] Awake. PlayerPrefs.selectedCharacter={idx}, name='{selName}'. Prefab count={(characterPrefabs!=null?characterPrefabs.Length:0)}");

        // Ensure a cooldown HUD exists if we are running the gameplay scene directly
        if (CooldownUIManager.Instance == null)
        {
            new GameObject("CooldownUIManager").AddComponent<CooldownUIManager>();
        }
        if (ModifiersUIManager.Instance == null)
        {
            new GameObject("ModifiersUIManager").AddComponent<ModifiersUIManager>();
        }
        if (characterPrefabs == null || characterPrefabs.Length == 0)
        {
            Debug.LogError("PlayerSpawner: No characterPrefabs assigned.");
            return;
        }
        if (idx < 0 || idx >= characterPrefabs.Length)
        {
            Debug.LogWarning($"[Spawner] Index {idx} out of range. Clamping to 0");
            idx = 0;
        }

        GameObject prefab = characterPrefabs[idx];
        // If name is provided and does not match, attempt to resolve by name in the list
        if (!string.IsNullOrEmpty(selName) && prefab != null && prefab.name != selName)
        {
            Debug.LogWarning($"[Spawner] Index/name mismatch: prefab at index {idx} is '{prefab.name}', expected '{selName}'. Searching by name...");
            for (int i = 0; i < characterPrefabs.Length; i++)
            {
                if (characterPrefabs[i] != null && characterPrefabs[i].name == selName)
                {
                    idx = i;
                    prefab = characterPrefabs[i];
                    Debug.Log($"[Spawner] Resolved by name. Using index {idx} -> '{prefab.name}'");
                    break;
                }
            }
        }

        if (prefab == null && !string.IsNullOrEmpty(selName))
        {
            Debug.LogError($"[Spawner] Prefab at index {idx} is null and could not resolve by name '{selName}'.");
        }
        if (prefab == null)
        {
            Debug.LogError($"PlayerSpawner: Prefab at index {idx} is null.");
            return;
        }

        for (int i = 0; i < characterPrefabs.Length; i++)
        {
            Debug.Log($"[Spawner] Prefab[{i}]= {(characterPrefabs[i]!=null?characterPrefabs[i].name:"<null>")}");
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        GameObject player = Instantiate(prefab, pos, rot);
        Debug.Log($"[Spawner] Instantiated '{prefab.name}' at {pos}");

        // Ensure camera exists/enabled on spawned character
        Camera chosenCam = null;
        var cam = player.GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            cam.gameObject.SetActive(true);
            chosenCam = cam;
            Debug.Log($"[Spawner] Enabled child camera: {cam.name}");
        }
        else if (Camera.main != null)
        {
            // Ensure at least one camera is active
            Camera.main.gameObject.SetActive(true);
            chosenCam = Camera.main;
            Debug.LogWarning("[Spawner] No child Camera on player. Using existing MainCamera in scene.");
        }
        else
        {
            // Create a simple follow-at-parent camera as a last resort
            var camGO = new GameObject("PlayerCamera");
            camGO.tag = "MainCamera";
            var newCam = camGO.AddComponent<Camera>();
            camGO.transform.SetParent(player.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.6f, -3.5f);
            camGO.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            chosenCam = newCam;
            Debug.LogWarning("[Spawner] Created fallback PlayerCamera as no camera was found.");
        }

        EnsureSingleAudioListener(chosenCam);

        // Apply HUD icon loadout if available
        var loadout = player.GetComponentInChildren<AbilityIconLoadout>(true);
        if (loadout != null && CooldownUIManager.Instance != null)
        {
            loadout.ApplyToHUD();
        }

        // Ensure a reactive crosshair exists when directly running gameplay scene
        if (FindObjectOfType<ReactiveCrosshair>(true) == null)
        {
            var crossGO = new GameObject("ReactiveCrosshairHost");
            var cross = crossGO.AddComponent<ReactiveCrosshair>();
            cross.playerRoot = player.transform;
        }
    }

    private void EnsureSingleAudioListener(Camera activeCam)
    {
        if (activeCam == null) return;
        var activeListener = activeCam.GetComponent<AudioListener>();
        if (activeListener == null)
        {
            activeListener = activeCam.gameObject.AddComponent<AudioListener>();
        }
        activeListener.enabled = true;

        var all = Object.FindObjectsOfType<AudioListener>(true);
        foreach (var al in all)
        {
            if (al == null) continue;
            if (al == activeListener) continue;
            al.enabled = false;
        }
    }
}
