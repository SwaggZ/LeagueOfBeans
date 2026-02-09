using UnityEngine;
using UnityEngine.SceneManagement;

// Persisted across scene load by CharacterSelection. Spawns the chosen prefab in the next scene, then destroys itself.
public class SelectionSpawnRequest : MonoBehaviour
{
    public GameObject prefab;

    [Tooltip("Name of a Transform to use as spawn point if found in the target scene.")]
    public string spawnPointName = "PlayerSpawn";

    [Tooltip("Tag of a Transform to use as spawn point if found in the target scene.")]
    public string spawnPointTag = "PlayerSpawn";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (prefab == null)
        {
            Debug.LogError("[SelectionSpawnRequest] No prefab assigned. Nothing to spawn.");
            Cleanup();
            return;
        }

        // Try to find a spawn point by tag, then by name
        Transform spawn = null;
        if (!string.IsNullOrEmpty(spawnPointTag))
        {
            var tagged = GameObject.FindGameObjectWithTag(spawnPointTag);
            if (tagged != null) spawn = tagged.transform;
        }
        if (spawn == null && !string.IsNullOrEmpty(spawnPointName))
        {
            var named = GameObject.Find(spawnPointName);
            if (named != null) spawn = named.transform;
        }

        Vector3 pos = spawn != null ? spawn.position : Vector3.zero;
        Quaternion rot = spawn != null ? spawn.rotation : Quaternion.identity;

        var player = Instantiate(prefab, pos, rot);
        Debug.Log($"[SelectionSpawnRequest] Spawned '{prefab.name}' at {pos} in scene '{scene.name}'");

        // Ensure a cooldown HUD exists in gameplay scenes
        if (CooldownUIManager.Instance == null)
        {
            new GameObject("CooldownUIManager").AddComponent<CooldownUIManager>();
        }

        // Ensure modifiers HUD exists
        if (ModifiersUIManager.Instance == null)
        {
            new GameObject("ModifiersUIManager").AddComponent<ModifiersUIManager>();
        }

        // Ensure ModifiersIconLibrary exists so modifiers pull icons from it
        if (ModifiersIconLibrary.Instance == null)
        {
            var libInScene = FindObjectOfType<ModifiersIconLibrary>(true);
            if (libInScene == null)
            {
                Debug.LogWarning("[SelectionSpawnRequest] No ModifiersIconLibrary found. Creating an empty one; assign sprites in your persistent scene to avoid NO IMG placeholders.");
                new GameObject("ModifiersIconLibrary").AddComponent<ModifiersIconLibrary>();
            }
        }

        // Ensure a camera is available
        Camera chosenCam = null;
        var cam = player.GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            cam.gameObject.SetActive(true);
            chosenCam = cam;
            Debug.Log($"[SelectionSpawnRequest] Enabled child camera: {cam.name}");
        }
        else if (Camera.main != null)
        {
            Camera.main.gameObject.SetActive(true);
            chosenCam = Camera.main;
            Debug.LogWarning("[SelectionSpawnRequest] No child camera on player. Using existing MainCamera in scene.");
        }
        else
        {
            var camGO = new GameObject("PlayerCamera");
            camGO.tag = "MainCamera";
            var newCam = camGO.AddComponent<Camera>();
            camGO.transform.SetParent(player.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.6f, -3.5f);
            camGO.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            chosenCam = newCam;
            Debug.LogWarning("[SelectionSpawnRequest] Created fallback PlayerCamera as no camera was found.");
        }

        EnsureSingleAudioListener(chosenCam);

        // Apply ability icon loadout from the spawned character if present
        var loadout = player.GetComponentInChildren<AbilityIconLoadout>(true);
        if (loadout != null && CooldownUIManager.Instance != null)
        {
            loadout.ApplyToHUD();
        }

        // Ensure a reactive crosshair exists in the gameplay scene
        if (FindObjectOfType<ReactiveCrosshair>(true) == null)
        {
            var crossGO = new GameObject("ReactiveCrosshairHost");
            var cross = crossGO.AddComponent<ReactiveCrosshair>();
            cross.playerRoot = player.transform;
        }

        Cleanup();
    }

    private void Cleanup()
    {
        Destroy(gameObject);
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
