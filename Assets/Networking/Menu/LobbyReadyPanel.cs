using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LobbyReadyPanel : MonoBehaviour
{
    [SerializeField] private NetworkGameSession networkSession;
    [SerializeField] private Transform listRoot;
    [SerializeField] private LobbyReadyEntryUI entryPrefab;
    [SerializeField] private Button readyToggleButton;
    [SerializeField] private TMP_Text readyButtonText;

    private readonly Dictionary<int, LobbyReadyEntryUI> _entries = new Dictionary<int, LobbyReadyEntryUI>();
    private bool _ready;

    private void Awake()
    {
        if (networkSession == null) networkSession = FindObjectOfType<NetworkGameSession>(true);
        if (readyToggleButton != null)
            readyToggleButton.onClick.AddListener(ToggleReady);
    }

    private void OnEnable()
    {
        HookEvents(true);
        RefreshAll();
    }

    private void OnDisable()
    {
        HookEvents(false);
    }

    private void HookEvents(bool hook)
    {
        if (networkSession == null) return;

        // Regular dictionaries don't have OnChange events, so just call RefreshAll
        if (hook)
        {
            RefreshAll();
        }
    }

    private void OnSelectionChanged(int key)
    {
        RefreshEntry(key);
    }

    private void OnReadyChanged(int key)
    {
        RefreshEntry(key);
    }

    private void RefreshAll()
    {
        if (networkSession == null) return;

        // Clear old entries
        foreach (var entry in _entries.Values)
            if (entry != null) Destroy(entry.gameObject);
        _entries.Clear();

        // Refresh from Selected dictionary
        foreach (var kvp in networkSession.Selected)
            RefreshEntry(kvp.Key);

        // Refresh from Ready dictionary
        foreach (var kvp in networkSession.Ready)
            RefreshEntry(kvp.Key);
    }

    private void RefreshEntry(int clientId)
    {
        if (networkSession == null || listRoot == null || entryPrefab == null) return;

        if (!_entries.TryGetValue(clientId, out var entry))
        {
            entry = Instantiate(entryPrefab, listRoot);
            _entries[clientId] = entry;
        }

        networkSession.Selected.TryGetValue(clientId, out var selection);
        networkSession.Ready.TryGetValue(clientId, out var ready);

        string playerName = "Player " + clientId;
        entry.Bind(playerName, selection, ready);
    }

    private void ToggleReady()
    {
        _ready = !_ready;
        if (readyButtonText != null) readyButtonText.text = _ready ? "Unready" : "Ready";
        networkSession?.ClientSetReady(_ready);
    }
}
