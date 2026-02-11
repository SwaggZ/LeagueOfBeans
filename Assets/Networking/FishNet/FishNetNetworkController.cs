using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Orchestrates host/join and scene flow with FishNet.
/// </summary>
[DisallowMultipleComponent]
public class FishNetNetworkController : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private SessionContext sessionContext;
    [SerializeField] private NetworkSessionAuthenticator authenticator;

    [Header("Scenes")]
    [SerializeField] private string selectionSceneName = "selection";
    [SerializeField] private string gameplaySceneName = "SampleScene";

    public string GameplaySceneName => gameplaySceneName;
    public string SelectionSceneName => selectionSceneName;

    private bool _isHosting = false;

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>(true);
        if (sessionContext == null) sessionContext = FindObjectOfType<SessionContext>(true);
        if (authenticator == null && networkManager != null)
            authenticator = networkManager.GetComponent<NetworkSessionAuthenticator>();
    }

    private void OnEnable()
    {
        if (networkManager != null)
        {
            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }
    }

    private void OnDisable()
    {
        if (networkManager != null)
        {
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        }
    }

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        // When hosting and server starts, load gameplay scene (which contains selection UI)
        if (_isHosting && args.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("Server started, loading gameplay scene with selection UI...");
            LoadGameplaySceneAsServer();
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        // If we're hosting and the client disconnects, stop the server
        if (_isHosting && args.ConnectionState == LocalConnectionState.Stopped)
        {
            Debug.Log("Host client disconnected, stopping server...");
            networkManager.ServerManager.StopConnection(true);
            _isHosting = false;
        }
    }

    public void HostServer()
    {
        if (networkManager == null) return;
        if (sessionContext == null) return;

        _isHosting = true;

        ApplyTransport(sessionContext.serverPort, "localhost");
        if (authenticator != null)
            authenticator.ConfigureServer(sessionContext.requirePassword, sessionContext.serverPassword);

        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();
    }

    public void JoinServer(string address, ushort port, string password)
    {
        if (networkManager == null) return;

        if (sessionContext != null)
            sessionContext.SetJoin(address, port, password);

        ApplyTransport(port, address);
        if (authenticator != null)
            authenticator.ConfigureClient(password);

        networkManager.ClientManager.StartConnection();
    }

    public void StopAll()
    {
        if (networkManager == null) return;
        networkManager.ClientManager.StopConnection();
        networkManager.ServerManager.StopConnection(true);
        _isHosting = false;
    }

    public void LoadSelectionSceneAsServer()
    {
        if (networkManager == null || !networkManager.IsServerStarted) return;

        SceneLoadData sld = new SceneLoadData(selectionSceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        networkManager.SceneManager.LoadGlobalScenes(sld);
    }

    public void LoadGameplaySceneAsServer()
    {
        if (networkManager == null || !networkManager.IsServerStarted) return;

        SceneLoadData sld = new SceneLoadData(gameplaySceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        networkManager.SceneManager.LoadGlobalScenes(sld);
    }

    private void ApplyTransport(ushort port, string address)
    {
        if (networkManager == null) return;
        Transport transport = networkManager.TransportManager.Transport;
        FishNetTransportConfigurator.Apply(transport, address, port);
    }
}
