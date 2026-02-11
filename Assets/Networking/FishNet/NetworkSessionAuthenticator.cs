using FishNet.Authenticating;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Optional password authentication for hosted lobbies.
/// </summary>
public class NetworkSessionAuthenticator : HostAuthenticator
{
    public struct PasswordBroadcast : IBroadcast
    {
        public string Password;
    }

    public struct PasswordResponseBroadcast : IBroadcast
    {
        public bool Passed;
    }

    [SerializeField] private bool requirePassword;
    [SerializeField] private string serverPassword = string.Empty;

    private string _clientPassword = string.Empty;

    public void ConfigureServer(bool require, string password)
    {
        requirePassword = require;
        serverPassword = password ?? string.Empty;
    }

    public void ConfigureClient(string password)
    {
        _clientPassword = password ?? string.Empty;
    }

    public override event System.Action<NetworkConnection, bool> OnAuthenticationResult;

    public override void InitializeOnce(NetworkManager networkManager)
    {
        base.InitializeOnce(networkManager);

        NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        NetworkManager.ServerManager.RegisterBroadcast<PasswordBroadcast>(OnPasswordBroadcast, false);
        NetworkManager.ClientManager.RegisterBroadcast<PasswordResponseBroadcast>(OnPasswordResponse);
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState != LocalConnectionState.Started)
            return;

        if (TryAuthenticateAsClientHost())
            return;

        if (!requirePassword)
        {
            // No password required; still mark authenticated for server logic.
            PasswordBroadcast pb = new() { Password = string.Empty };
            NetworkManager.ClientManager.Broadcast(pb);
            return;
        }

        PasswordBroadcast broadcast = new() { Password = _clientPassword ?? string.Empty };
        NetworkManager.ClientManager.Broadcast(broadcast);
    }

    private void OnPasswordBroadcast(NetworkConnection conn, PasswordBroadcast pb, Channel channel)
    {
        if (conn.IsAuthenticated)
        {
            conn.Disconnect(true);
            return;
        }

        bool passed = !requirePassword || pb.Password == serverPassword;
        SendAuthResponse(conn, passed);
        OnAuthenticationResult?.Invoke(conn, passed);
    }

    private void OnPasswordResponse(PasswordResponseBroadcast rb, Channel channel)
    {
        NetworkManager.Log(rb.Passed ? "Authentication complete." : "Authentication failed.");
    }

    private void SendAuthResponse(NetworkConnection conn, bool passed)
    {
        PasswordResponseBroadcast rb = new() { Passed = passed };
        NetworkManager.ServerManager.Broadcast(conn, rb, false);
    }

    protected override void OnHostAuthenticationResult(NetworkConnection conn, bool authenticated)
    {
        SendAuthResponse(conn, authenticated);
        OnAuthenticationResult?.Invoke(conn, authenticated);
    }
}
