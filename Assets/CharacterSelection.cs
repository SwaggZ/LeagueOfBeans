using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FishNet.Managing;

// Clean and robust character selection controller
public class CharacterSelection : MonoBehaviour
{
    [System.Serializable]
    public class CharacterEntry
    {
        public string id; // e.g., "Ahri", "Ashe", "Caitlyn", "Galio"
        public GameObject preview; // scene preview object (enabled/disabled in selection scene)
        public GameObject gameplayPrefab; // prefab to spawn in gameplay scene

        [Header("Ability Names")]
        public string lmbName = "Left Click";
        public string rmbName = "Right Click";
        public string oneName = "1";
        public string twoName = "2";

        [Header("Ability Descriptions")]
        [TextArea] public string lmbDesc = "";
        [TextArea] public string rmbDesc = "";
        [TextArea] public string oneDesc = "";
        [TextArea] public string twoDesc = "";
    }

    [Header("Characters (configure in Inspector)")]
    public List<CharacterEntry> characters = new List<CharacterEntry>();

    [Header("UI References")] 
    public TMP_Text label;
    public TMP_Text lmbNameText;
    public TMP_Text rmbNameText;
    public TMP_Text oneNameText;
    public TMP_Text twoNameText;
    public TMP_Text lmbDescText;
    public TMP_Text rmbDescText;
    public TMP_Text oneDescText;
    public TMP_Text twoDescText;

    [Header("Target Scene")] 
    [Tooltip("If not empty, loads by name; otherwise uses scene index.")]
    public string gameplaySceneName = "";
    public int gameplaySceneIndex = 1;
    [Header("Spawn Point (in gameplay scene)")]
    public string spawnPointName = "PlayerSpawn";
    public string spawnPointTag = "PlayerSpawn";

    [Header("Defaults")] 
    public int selectedIndex = 0;

    [Header("Networking (Optional)")]
    public NetworkGameSession networkSession;
    
    // Cached references for hiding/showing UI
    private GameObject _cachedCanvas;
    private GameObject _cachedCamera;
    
    // Track if this is a respawn (vs initial spawn)
    private bool _isRespawnMode = false;

    void Awake()
    {
        // Ensure cursor is free/visible in selection
        try { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; } catch {}
        
        // Auto-detect NetworkGameSession if not assigned
        if (networkSession == null)
        {
            // Try direct find first
            networkSession = FindObjectOfType<NetworkGameSession>();
            if (networkSession != null)
                Debug.Log("[Selection] Auto-detected NetworkGameSession");
            else
                Debug.LogWarning("[Selection] Could not auto-detect NetworkGameSession at Awake");
        }
        
        // Note: We now use FishNet Broadcasts instead of NetworkSessionBridge
        Debug.Log("[Selection] Using FishNet Broadcasts for client-server communication");
        
        // Auto-fill gameplayPrefab from PreviewBinding if missing, then prefer gameplay prefab name for id
        for (int i = 0; i < characters.Count; i++)
        {
            var e = characters[i];
            if (e == null) continue;
            if (e.gameplayPrefab == null && e.preview != null)
            {
                var binding = e.preview.GetComponent<PreviewBinding>();
                if (binding != null && binding.gameplayPrefab != null)
                {
                    e.gameplayPrefab = binding.gameplayPrefab;
                    Debug.Log($"[Selection] Auto-bound gameplay prefab for '{e.preview.name}' -> {e.gameplayPrefab.name}");
                }
            }

            // Prefer gameplay prefab name for id when available; otherwise use preview name
            if (string.IsNullOrWhiteSpace(e.id))
            {
                if (e.gameplayPrefab != null)
                {
                    e.id = e.gameplayPrefab.name;
                    Debug.Log($"[Selection] Auto-set id for entry {i} to '{e.id}' from gameplay prefab name");
                }
                else if (e.preview != null)
                {
                    e.id = e.preview.name;
                    Debug.Log($"[Selection] Auto-set id for entry {i} to '{e.id}' from preview name");
                }
            }
        }
    }
    
    void OnEnable()
    {
        // Try to wire START button if not already wired
        WireStartButton();
    }
    
    private void WireStartButton()
    {
        // Find StartButton specifically
        GameObject startButtonGo = GameObject.Find("StartButton");
        if (startButtonGo == null)
        {
            Debug.LogWarning("[Selection] Could not find 'StartButton' GameObject");
            return;
        }
        
        Button startButton = startButtonGo.GetComponent<Button>();
        if (startButton == null)
        {
            Debug.LogError("[Selection] 'StartButton' found but has no Button component!");
            return;
        }
        
        Debug.Log("[Selection] Found StartButton, wiring to StartGame()");
        
        // Remove any existing listeners and add ours
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() => {
            Debug.Log("[Selection] === START BUTTON CLICKED ===");
            // Disable button immediately to prevent double-clicks
            startButton.interactable = false;
            StartGame();
        });
        
        Debug.Log("[Selection] StartButton wired successfully");
    }
    
    /// <summary>
    /// Re-enables the START button. Called when showing selection UI for respawn.
    /// </summary>
    private void ReenableStartButton()
    {
        GameObject startButtonGo = GameObject.Find("StartButton");
        if (startButtonGo != null)
        {
            Button startButton = startButtonGo.GetComponent<Button>();
            if (startButton != null)
            {
                startButton.interactable = true;
                Debug.Log("[Selection] START button re-enabled for respawn.");
            }
        }
    }

    void Start()
    {
        // Redo in Start in case other scripts toggled during scene load
        try { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; } catch {}
        // Enable only selected preview
        ApplySelection(Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, characters.Count - 1)));
    }

    public void SelectByIndex(int index)
    {
        ApplySelection(index);
    }

    // Convenience hooks for existing buttons (optional)
    public void Ahri()  => ApplySelectionById("Ahri");
    public void Ashe()  => ApplySelectionById("Ashe");
    public void Cait()  => ApplySelectionById("Caitlyn");
    public void Galio() => ApplySelectionById("Galio");
    public void Aphelios() => ApplySelectionById("Aphelios");
    public void Jhin() => ApplySelectionById("Jhin");
    public void Lux() => ApplySelectionById("Lux");

    private void ApplySelectionById(string id)
    {
        int idx = characters.FindIndex(c =>
            c != null && (
                (!string.IsNullOrEmpty(c.id) && string.Equals(c.id, id, System.StringComparison.OrdinalIgnoreCase)) ||
                (c.preview != null && string.Equals(c.preview.name, id, System.StringComparison.OrdinalIgnoreCase)) ||
                (c.gameplayPrefab != null && string.Equals(c.gameplayPrefab.name, id, System.StringComparison.OrdinalIgnoreCase))
            )
        );
        if (idx < 0) { Debug.LogWarning($"[Selection] Could not find id '{id}'. Falling back to index 0."); idx = 0; }
        ApplySelection(idx);
    }

    private void ApplySelection(int index)
    {
        if (characters == null || characters.Count == 0) return;
        index = Mathf.Clamp(index, 0, characters.Count - 1);
        selectedIndex = index;

        for (int i = 0; i < characters.Count; i++)
        {
            var e = characters[i];
            if (e != null && e.preview != null)
            {
                e.preview.SetActive(i == selectedIndex);
            }
        }

        UpdateUI();
        DumpPreviewStates();

        // Don't submit selection to network yet - only when StartGame is called
        // This allows browsing characters without triggering spawn
    }

    private void UpdateUI()
    {
        var e = GetCurrent();
        if (e == null) return;

        if (label != null)
            label.text = e.preview != null ? e.preview.name : (e.id ?? "<Character>");

        if (lmbNameText) lmbNameText.text = e.lmbName;
        if (rmbNameText) rmbNameText.text = e.rmbName;
        if (oneNameText) oneNameText.text = e.oneName;
        if (twoNameText) twoNameText.text = e.twoName;

        if (lmbDescText) lmbDescText.text = e.lmbDesc;
        if (rmbDescText) rmbDescText.text = e.rmbDesc;
        if (oneDescText) oneDescText.text = e.oneDesc;
        if (twoDescText) twoDescText.text = e.twoDesc;
    }

    public void StartGame()
    {
        Debug.Log("[Selection] ===== START GAME CALLED =====");
        
        var e = GetCurrent();
        if (e == null)
        {
            Debug.LogError("[Selection] No current character selected.");
            return;
        }
        Debug.Log($"[Selection] Current character: {(e.preview != null ? e.preview.name : e.id)}, gameplayPrefab: {(e.gameplayPrefab != null ? e.gameplayPrefab.name : "NULL")}");
        
        if (e.gameplayPrefab == null)
        {
            Debug.LogError($"[Selection] Selected '{(e.preview!=null?e.preview.name:e.id)}' has no gameplayPrefab assigned. Add PreviewBinding to preview or assign in CharacterSelection.");
            return;
        }

        // Try multiple ways to find NetworkGameSession
        if (networkSession == null)
        {
            Debug.Log("[Selection] networkSession is null, searching...");
            networkSession = FindObjectOfType<NetworkGameSession>();
            if (networkSession != null) Debug.Log("[Selection] Found NetworkGameSession via FindObjectOfType");
        }
        
        if (networkSession == null)
        {
            Debug.Log("[Selection] Still null, trying FindObjectOfType(true)...");
            networkSession = FindObjectOfType<NetworkGameSession>(true);
            if (networkSession != null) Debug.Log("[Selection] Found NetworkGameSession via FindObjectOfType(true)");
        }
        
        if (networkSession == null)
        {
            Debug.Log("[Selection] Still null, checking NetworkManager...");
            var nm = FindObjectOfType<NetworkManager>();
            if (nm == null)
                nm = FindObjectOfType<NetworkManager>(true);
            if (nm != null)
            {
                Debug.Log($"[Selection] Found NetworkManager: {nm.name}");
                // Check children for NetworkGameSession
                networkSession = nm.GetComponentInChildren<NetworkGameSession>();
                if (networkSession != null) Debug.Log("[Selection] Found NetworkGameSession in NetworkManager children");
            }
            else
            {
                Debug.LogError("[Selection] Could not find NetworkManager!");
            }
        }

        if (networkSession != null)
        {
            var nm = FindObjectOfType<NetworkManager>();
            Debug.Log($"[Selection] NetworkManager={nm?.name}, IsClientStarted={nm?.IsClientStarted}, IsServerStarted={nm?.IsServerStarted}");
            
            if (nm == null || !nm.IsClientStarted)
            {
                Debug.LogError("[Selection] NetworkManager not found or client not started!");
                return;
            }
            
            string characterId = !string.IsNullOrEmpty(e.id) ? e.id : e.gameplayPrefab.name;
            
            // Check if we're hosting (both server and client on same instance)
            bool isHost = nm.IsServerStarted && nm.IsClientStarted;
            
            // If we're the host, call server methods directly. Otherwise use broadcasts.
            if (isHost)
            {
                Debug.Log($"[Selection] We are HOST - calling server methods directly on NetworkGameSession");
                
                // Get the host's client connection (ClientId 0 is always the host)
                var hostConnection = nm.ServerManager.Clients.Count > 0 
                    ? nm.ServerManager.Clients[0] 
                    : null;
                
                if (hostConnection == null)
                {
                    Debug.LogError("[Selection] Could not find host connection!");
                    return;
                }
                
                Debug.Log($"[Selection] Using host connection: ClientId={hostConnection.ClientId}");
                
                if (_isRespawnMode)
                {
                    Debug.Log($"[Selection] RESPAWN MODE: Requesting respawn with '{characterId}'");
                    networkSession.SubmitSelectionServerRpc(characterId, hostConnection);
                    networkSession.RequestRespawnServerRpc(hostConnection);
                    _isRespawnMode = false;
                }
                else
                {
                    Debug.Log($"[Selection] INITIAL SPAWN: Submitting selection '{characterId}' and marking ready");
                    networkSession.SubmitSelectionServerRpc(characterId, hostConnection);
                    networkSession.SetReadyServerRpc(true, hostConnection);
                    Debug.Log("[Selection] Selection and ready submitted directly to server");
                }
            }
            else
            {
                Debug.Log($"[Selection] We are CLIENT - using FishNet Broadcasts");
                
                if (_isRespawnMode)
                {
                    Debug.Log($"[Selection] RESPAWN MODE: Requesting respawn with '{characterId}'");
                    nm.ClientManager.Broadcast(new SelectionBroadcast { CharacterId = characterId });
                    nm.ClientManager.Broadcast(new RespawnBroadcast());
                    _isRespawnMode = false;
                }
                else
                {
                    Debug.Log($"[Selection] INITIAL SPAWN: Submitting selection '{characterId}' and marking ready");
                    nm.ClientManager.Broadcast(new SelectionBroadcast { CharacterId = characterId });
                    nm.ClientManager.Broadcast(new ReadyBroadcast { IsReady = true });
                    Debug.Log("[Selection] Selection and ready submitted via broadcasts");
                }
            }
            
            // DON'T hide UI here - wait for server to spawn player
            // UI will be hidden in NetworkPlayerController.OnStartClient() when spawn actually happens
            return;
        }

        Debug.LogError("[Selection] StartGame: Could not find NetworkGameSession! Make sure NetworkGameSession exists in scene.");
    }
    
    public void HideSelectionUI()
    {
        Debug.Log("[Selection] Hiding selection UI and camera");
        
        // Cache and disable the selection canvas
        if (_cachedCanvas == null)
            _cachedCanvas = GameObject.Find("SelectionCanvas");
        
        if (_cachedCanvas != null)
        {
            _cachedCanvas.SetActive(false);
            Debug.Log("[Selection] Disabled SelectionCanvas");
        }
        else
        {
            Debug.LogWarning("[Selection] Could not find SelectionCanvas to hide");
        }
        
        // Cache and disable the selection camera
        if (_cachedCamera == null)
            _cachedCamera = GameObject.Find("SelectionCamera");
        
        if (_cachedCamera != null)
        {
            Camera cam = _cachedCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.enabled = false;
                Debug.Log("[Selection] Disabled SelectionCamera");
            }
            _cachedCamera.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[Selection] Could not find SelectionCamera to hide");
        }
        
        // Disable all character previews
        foreach (var character in characters)
        {
            if (character != null && character.preview != null)
            {
                character.preview.SetActive(false);
            }
        }
        
        // Re-enable cursor for gameplay
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log("[Selection] Selection UI hidden");
    }
    
    public void SetRespawnMode(bool isRespawn)
    {
        _isRespawnMode = isRespawn;
        Debug.Log($"[Selection] Respawn mode set to: {isRespawn}");
    }
    
    public void ShowSelectionUI()
    {
        Debug.Log("[Selection] Showing selection UI");
        
        if (_cachedCanvas != null)
        {
            _cachedCanvas.SetActive(true);
            Debug.Log("[Selection] Enabled SelectionCanvas");
        }
        
        if (_cachedCamera != null)
        {
            _cachedCamera.SetActive(true);
            Camera cam = _cachedCamera.GetComponent<Camera>();
            if (cam != null)
                cam.enabled = true;
            Debug.Log("[Selection] Enabled SelectionCamera");
        }
        
        // Re-enable the currently selected preview
        ApplySelection(selectedIndex);
        
        // Re-enable START button for respawn
        ReenableStartButton();
        
        // Ensure cursor is visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private CharacterEntry GetCurrent()
    {
        if (characters == null || characters.Count == 0) return null;
        int idx = Mathf.Clamp(selectedIndex, 0, characters.Count - 1);
        return characters[idx];
    }
    
    private void DumpPreviewStates()
    {
        if (characters == null) return;
        for (int i = 0; i < characters.Count; i++)
        {
            var e = characters[i];
            string name = e?.preview != null ? e.preview.name : (e?.id ?? "<null>");
            bool active = e?.preview != null && e.preview.activeSelf;
            Debug.Log($"[Selection] Preview[{i}] {name} active={active}");
        }
    }
}
