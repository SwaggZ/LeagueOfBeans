using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Shows LAN servers and handles host/join.
/// </summary>
[DisallowMultipleComponent]
public class ServerBrowserController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private FishNetNetworkController networkController;
    [SerializeField] private SessionContext sessionContext;
    [SerializeField] private LanServerListener lanListener;

    [Header("List UI")]
    [SerializeField] private Transform listRoot;
    [SerializeField] private ServerListItemUI listItemPrefab;

    [Header("Host UI")]
    [SerializeField] private TMP_InputField serverNameInput;
    [SerializeField] private TMP_InputField hostPortInput;
    [SerializeField] private TMP_InputField hostPasswordInput;

    [Header("Join UI")]
    [SerializeField] private TMP_InputField directAddressInput;
    [SerializeField] private TMP_InputField directPortInput;
    [SerializeField] private TMP_InputField joinPasswordInput;

    private readonly Dictionary<string, ServerListItemUI> _entries = new Dictionary<string, ServerListItemUI>();

    private void Awake()
    {
        if (networkController == null) networkController = FindObjectOfType<FishNetNetworkController>(true);
        if (sessionContext == null) sessionContext = FindObjectOfType<SessionContext>(true);
        if (lanListener == null) lanListener = FindObjectOfType<LanServerListener>(true);
    }

    private void OnEnable()
    {
        if (lanListener != null)
            lanListener.OnServerUpdated += HandleServerUpdated;
    }

    private void OnDisable()
    {
        if (lanListener != null)
            lanListener.OnServerUpdated -= HandleServerUpdated;
    }

    public void HostServer()
    {
        string name = serverNameInput != null ? serverNameInput.text : "Lobby";
        ushort port = ReadPort(hostPortInput, 7770);
        string password = hostPasswordInput != null ? hostPasswordInput.text : string.Empty;
        bool requirePassword = !string.IsNullOrEmpty(password);

        if (sessionContext != null)
            sessionContext.SetServer(name, port, requirePassword, password);

        networkController?.HostServer();
    }

    public void JoinSelected(LanServerInfo info)
    {
        string password = joinPasswordInput != null ? joinPasswordInput.text : string.Empty;
        networkController?.JoinServer(info.address, info.port, password);
    }

    public void JoinDirect()
    {
        string address = directAddressInput != null ? directAddressInput.text : "localhost";
        ushort port = ReadPort(directPortInput, 7770);
        string password = joinPasswordInput != null ? joinPasswordInput.text : string.Empty;
        networkController?.JoinServer(address, port, password);
    }

    private void HandleServerUpdated(LanServerInfo info)
    {
        if (listRoot == null || listItemPrefab == null) return;

        string key = info.address + ":" + info.port;
        if (!_entries.TryGetValue(key, out var entry))
        {
            entry = Instantiate(listItemPrefab, listRoot);
            _entries[key] = entry;
        }
        entry.Bind(info, JoinSelected);
        StartCoroutine(PingRoutine(info, entry));
    }

    private IEnumerator PingRoutine(LanServerInfo info, ServerListItemUI entry)
    {
        if (string.IsNullOrEmpty(info.address)) yield break;

        var ping = new Ping(info.address);
        float start = Time.unscaledTime;

        while (!ping.isDone && Time.unscaledTime - start < 1.2f)
            yield return null;

        int ms = ping.isDone ? ping.time : -1;
        entry.UpdatePing(ms);
    }

    private static ushort ReadPort(TMP_InputField input, ushort fallback)
    {
        if (input == null || string.IsNullOrEmpty(input.text)) return fallback;
        if (ushort.TryParse(input.text, out var port)) return port;
        return fallback;
    }
}
