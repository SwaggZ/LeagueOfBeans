using System;

[Serializable]
public class LanServerInfo
{
    public string name;
    public string address;
    public ushort port;
    public bool hasPassword;
    public int playerCount;
    public int maxPlayers;
    public int pingMs;
    public DateTime lastSeenUtc;
}
