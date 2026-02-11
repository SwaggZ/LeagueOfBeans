using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ServerListItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private Image lockIcon;
    [SerializeField] private Button joinButton;

    private LanServerInfo _info;
    private Action<LanServerInfo> _onJoin;

    public void Bind(LanServerInfo info, Action<LanServerInfo> onJoin)
    {
        _info = info;
        _onJoin = onJoin;

        if (nameText != null) nameText.text = info.name;
        if (pingText != null) pingText.text = info.pingMs >= 0 ? $"{info.pingMs} ms" : "...";
        if (playersText != null)
        {
            string max = info.maxPlayers > 0 ? info.maxPlayers.ToString() : "?";
            playersText.text = $"{info.playerCount}/{max}";
        }
        if (lockIcon != null) lockIcon.enabled = info.hasPassword;

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => _onJoin?.Invoke(_info));
        }
    }

    public void UpdatePing(int pingMs)
    {
        if (_info != null) _info.pingMs = pingMs;
        if (pingText != null) pingText.text = pingMs >= 0 ? $"{pingMs} ms" : "...";
    }
}
