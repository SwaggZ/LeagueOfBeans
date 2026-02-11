using UnityEngine;

/// <summary>
/// Holds session settings and player selection across scene loads.
/// Avoids static references; locate via FindObjectOfType.
/// </summary>
[DisallowMultipleComponent]
public class SessionContext : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverName = "Lobby";
    public bool requirePassword = false;
    public string serverPassword = string.Empty;
    public ushort serverPort = 7770;

    [Header("Join Settings")]
    public string joinAddress = "localhost";
    public ushort joinPort = 7770;
    public string joinPassword = string.Empty;

    [Header("Selection")]
    public string selectedCharacterId = string.Empty;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void SetServer(string name, ushort port, bool requirePass, string password)
    {
        serverName = name;
        serverPort = port;
        requirePassword = requirePass;
        serverPassword = password ?? string.Empty;
    }

    public void SetJoin(string address, ushort port, string password)
    {
        joinAddress = address;
        joinPort = port;
        joinPassword = password ?? string.Empty;
    }

    public void SetSelection(string characterId)
    {
        selectedCharacterId = characterId ?? string.Empty;
    }
}
