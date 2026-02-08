using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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

    void Awake()
    {
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

    void Start()
    {
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
        var e = GetCurrent();
        if (e == null)
        {
            Debug.LogError("[Selection] No current character selected.");
            return;
        }
        if (e.gameplayPrefab == null)
        {
            Debug.LogError($"[Selection] Selected '{(e.preview!=null?e.preview.name:e.id)}' has no gameplayPrefab assigned. Add PreviewBinding to preview or assign in CharacterSelection.");
            return;
        }

        // Carry over spawn request into the next scene
        var go = new GameObject("SelectionSpawnRequest");
        var req = go.AddComponent<SelectionSpawnRequest>();
        req.prefab = e.gameplayPrefab;
        req.spawnPointName = spawnPointName;
        req.spawnPointTag = spawnPointTag;
        DontDestroyOnLoad(go);

        Debug.Log($"[Selection] Starting game with '{e.gameplayPrefab.name}'. Loading scene {(string.IsNullOrEmpty(gameplaySceneName)?("#"+gameplaySceneIndex):gameplaySceneName)}");
        if (!string.IsNullOrEmpty(gameplaySceneName))
            SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(gameplaySceneIndex, LoadSceneMode.Single);
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
