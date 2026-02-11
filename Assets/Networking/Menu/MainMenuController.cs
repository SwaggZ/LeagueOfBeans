using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple menu flow: settings, logo, play button.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject serverBrowserPanel;

    public void ShowMain()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (serverBrowserPanel != null) serverBrowserPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (serverBrowserPanel != null) serverBrowserPanel.SetActive(false);
    }

    public void ShowServerBrowser()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (serverBrowserPanel != null) serverBrowserPanel.SetActive(true);
    }
}
