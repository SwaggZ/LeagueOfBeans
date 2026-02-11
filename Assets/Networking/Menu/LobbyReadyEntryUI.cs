using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LobbyReadyEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text selectionText;
    [SerializeField] private TMP_Text readyText;

    public void Bind(string playerName, string selection, bool ready)
    {
        if (playerNameText != null) playerNameText.text = playerName;
        if (selectionText != null) selectionText.text = string.IsNullOrEmpty(selection) ? "Selecting..." : selection;
        if (readyText != null) readyText.text = ready ? "Ready" : "Not Ready";
    }
}
