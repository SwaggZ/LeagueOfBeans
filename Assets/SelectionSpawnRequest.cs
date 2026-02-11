using UnityEngine;
using FishNet.Managing;
using UnityEngine.SceneManagement;

// Persisted across scene load by CharacterSelection. Spawns the chosen prefab in the next scene, then destroys itself.
public class SelectionSpawnRequest : MonoBehaviour
{
    public GameObject prefab;

    [Tooltip("Optional explicit spawn point reference.")]
    public Transform spawnPointOverride;

    [Tooltip("Name of a Transform to use as spawn point if found in the target scene.")]
    public string spawnPointName = "PlayerSpawn";

    [Tooltip("Tag of a Transform to use as spawn point if found in the target scene.")]
    public string spawnPointTag = "PlayerSpawn";

    [SerializeField] private SpawnService spawnService;

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
        var networkManager = FindObjectOfType<NetworkManager>(true);
        if (networkManager != null && (networkManager.IsClientStarted || networkManager.IsServerStarted))
        {
            Cleanup();
            return;
        }

        if (prefab == null)
        {
            Debug.LogError("[SelectionSpawnRequest] No prefab assigned. Nothing to spawn.");
            Cleanup();
            return;
        }

        // Try to find a spawn point via explicit reference or SpawnPointMarker, then by name
        Transform spawn = spawnPointOverride;
        if (spawn == null)
        {
            var markers = FindObjectsOfType<SpawnPointMarker>(true);
            foreach (var marker in markers)
            {
                if (marker == null) continue;
                if (!string.IsNullOrEmpty(spawnPointTag) && marker.key == spawnPointTag)
                {
                    spawn = marker.transform;
                    break;
                }
                if (!string.IsNullOrEmpty(spawnPointName) && marker.key == spawnPointName)
                {
                    spawn = marker.transform;
                    break;
                }
            }
        }
        if (spawn == null && !string.IsNullOrEmpty(spawnPointName))
        {
            var named = GameObject.Find(spawnPointName);
            if (named != null) spawn = named.transform;
        }
        if (spawn == null && !string.IsNullOrEmpty(spawnPointTag))
        {
            // Legacy fallback for existing scenes that rely on tags
            var tagged = GameObject.FindGameObjectWithTag(spawnPointTag);
            if (tagged != null) spawn = tagged.transform;
        }

        Vector3 pos = spawn != null ? spawn.position : Vector3.zero;
        Quaternion rot = spawn != null ? spawn.rotation : Quaternion.identity;

        var spawner = spawnService != null ? spawnService : FindObjectOfType<SpawnService>(true);
        var player = spawner != null
            ? spawner.Spawn(prefab, pos, rot)
            : Instantiate(prefab, pos, rot);
        Debug.Log($"[SelectionSpawnRequest] Spawned '{prefab.name}' at {pos} in scene '{scene.name}'");

        // Ensure a cooldown HUD exists in gameplay scenes
        var cooldownUi = FindObjectOfType<CooldownUIManager>(true);
        if (cooldownUi == null)
        {
            new GameObject("CooldownUIManager").AddComponent<CooldownUIManager>();
        }

        // Ensure modifiers HUD exists
        var modifiersUi = FindObjectOfType<ModifiersUIManager>(true);
        if (modifiersUi == null)
        {
            new GameObject("ModifiersUIManager").AddComponent<ModifiersUIManager>();
        }

        // Ensure ModifiersIconLibrary exists so modifiers pull icons from it
        var iconLibrary = FindObjectOfType<ModifiersIconLibrary>(true);
        if (iconLibrary == null)
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
        cooldownUi = FindObjectOfType<CooldownUIManager>(true);
        if (loadout != null && cooldownUi != null)
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
