using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class BuildNetworkMenuPrefabs
{
    private const string PrefabRoot = "Assets/Networking/Menu/Prefabs";

    [MenuItem("Tools/Build Network UI Prefabs")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(PrefabRoot);

        string listItemPath = Path.Combine(PrefabRoot, "ServerListItem.prefab");
        string lobbyEntryPath = Path.Combine(PrefabRoot, "LobbyReadyEntry.prefab");
        string lobbyPanelPath = Path.Combine(PrefabRoot, "LobbyReadyPanel.prefab");
        string mainMenuPath = Path.Combine(PrefabRoot, "MainMenuCanvas.prefab");

        var listItemPrefab = BuildServerListItem(listItemPath);
        var lobbyEntryPrefab = BuildLobbyReadyEntry(lobbyEntryPath);
        var lobbyPanelPrefab = BuildLobbyReadyPanel(lobbyPanelPath, lobbyEntryPrefab);
        BuildMainMenu(mainMenuPath, listItemPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BuildNetworkMenuPrefabs] Prefabs created under Assets/Networking/Menu/Prefabs");
    }

    private static GameObject BuildServerListItem(string path)
    {
        var resources = new TMP_DefaultControls.Resources();
        GameObject root = new GameObject("ServerListItem", typeof(RectTransform));
        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        root.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var nameText = CreateTmpText("Name", root.transform, resources, "Lobby");
        var pingText = CreateTmpText("Ping", root.transform, resources, "0 ms");
        var playersText = CreateTmpText("Players", root.transform, resources, "0/0");

        var lockIconGo = new GameObject("Lock", typeof(RectTransform), typeof(Image));
        lockIconGo.transform.SetParent(root.transform, false);
        var lockImg = lockIconGo.GetComponent<Image>();
        lockImg.enabled = false;
        lockImg.color = new Color(1f, 1f, 1f, 0.85f);
        var lockRt = lockIconGo.GetComponent<RectTransform>();
        lockRt.sizeDelta = new Vector2(18f, 18f);

        var joinButtonGo = TMP_DefaultControls.CreateButton(resources);
        joinButtonGo.name = "JoinButton";
        joinButtonGo.transform.SetParent(root.transform, false);
        var joinText = joinButtonGo.GetComponentInChildren<TextMeshProUGUI>();
        if (joinText != null) joinText.text = "Join";

        var item = root.AddComponent<ServerListItemUI>();
        SetPrivateField(item, "nameText", nameText);
        SetPrivateField(item, "pingText", pingText);
        SetPrivateField(item, "playersText", playersText);
        SetPrivateField(item, "lockIcon", lockImg);
        SetPrivateField(item, "joinButton", joinButtonGo.GetComponent<Button>());

        return SavePrefab(root, path);
    }

    private static GameObject BuildLobbyReadyEntry(string path)
    {
        var resources = new TMP_DefaultControls.Resources();
        GameObject root = new GameObject("LobbyReadyEntry", typeof(RectTransform));
        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        var playerText = CreateTmpText("Player", root.transform, resources, "Player 0");
        var selectionText = CreateTmpText("Selection", root.transform, resources, "Selecting...");
        var readyText = CreateTmpText("Ready", root.transform, resources, "Not Ready");

        var entry = root.AddComponent<LobbyReadyEntryUI>();
        SetPrivateField(entry, "playerNameText", playerText);
        SetPrivateField(entry, "selectionText", selectionText);
        SetPrivateField(entry, "readyText", readyText);

        return SavePrefab(root, path);
    }

    private static GameObject BuildLobbyReadyPanel(string path, GameObject entryPrefab)
    {
        var resources = new TMP_DefaultControls.Resources();
        GameObject root = new GameObject("LobbyReadyPanel", typeof(RectTransform), typeof(Image));
        var bg = root.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.4f);

        var vertical = root.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = 8f;
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.padding = new RectOffset(12, 12, 12, 12);

        var header = CreateTmpText("Header", root.transform, resources, "Lobby");
        header.fontSize = 28;

        var listRootGo = new GameObject("List", typeof(RectTransform));
        listRootGo.transform.SetParent(root.transform, false);
        var listLayout = listRootGo.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 6f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childForceExpandWidth = false;
        listRootGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var readyButtonGo = TMP_DefaultControls.CreateButton(resources);
        readyButtonGo.name = "ReadyButton";
        readyButtonGo.transform.SetParent(root.transform, false);
        var readyText = readyButtonGo.GetComponentInChildren<TextMeshProUGUI>();
        if (readyText != null) readyText.text = "Ready";

        var panel = root.AddComponent<LobbyReadyPanel>();
        SetPrivateField(panel, "listRoot", listRootGo.transform);
        SetPrivateField(panel, "entryPrefab", entryPrefab.GetComponent<LobbyReadyEntryUI>());
        SetPrivateField(panel, "readyToggleButton", readyButtonGo.GetComponent<Button>());
        SetPrivateField(panel, "readyButtonText", readyText);

        return SavePrefab(root, path);
    }

    private static GameObject BuildMainMenu(string path, GameObject listItemPrefab)
    {
        var resources = new TMP_DefaultControls.Resources();
        GameObject canvasGo = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var canvasRt = canvasGo.GetComponent<RectTransform>();
        SetFullStretch(canvasRt);

        var mainPanel = CreatePanel("MainPanel", canvasGo.transform);
        var settingsPanel = CreatePanel("SettingsPanel", canvasGo.transform);
        var serverPanel = CreatePanel("ServerBrowserPanel", canvasGo.transform);

        var logo = CreateTmpText("Logo", mainPanel.transform, resources, "League of Beans");
        logo.fontSize = 48;
        AddLayoutSize(logo.gameObject, 520f, 80f);
        var playButton = TMP_DefaultControls.CreateButton(resources);
        playButton.name = "PlayButton";
        playButton.transform.SetParent(mainPanel.transform, false);
        var playText = playButton.GetComponentInChildren<TextMeshProUGUI>();
        if (playText != null) playText.text = "Play";
        AddLayoutSize(playButton, 260f, 54f);

        var settingsButton = TMP_DefaultControls.CreateButton(resources);
        settingsButton.name = "SettingsButton";
        settingsButton.transform.SetParent(mainPanel.transform, false);
        var settingsText = settingsButton.GetComponentInChildren<TextMeshProUGUI>();
        if (settingsText != null) settingsText.text = "Settings";
        AddLayoutSize(settingsButton, 260f, 54f);

        var settingsHeader = CreateTmpText("SettingsHeader", settingsPanel.transform, resources, "Settings");
        settingsHeader.fontSize = 36;
        AddLayoutSize(settingsHeader.gameObject, 520f, 60f);
        var backFromSettings = TMP_DefaultControls.CreateButton(resources);
        backFromSettings.name = "BackButton";
        backFromSettings.transform.SetParent(settingsPanel.transform, false);
        var backText = backFromSettings.GetComponentInChildren<TextMeshProUGUI>();
        if (backText != null) backText.text = "Back";
        AddLayoutSize(backFromSettings, 220f, 48f);

        var serverHeader = CreateTmpText("ServerHeader", serverPanel.transform, resources, "Servers");
        serverHeader.fontSize = 36;
        AddLayoutSize(serverHeader.gameObject, 520f, 60f);

        var listRootGo = new GameObject("ServerList", typeof(RectTransform));
        listRootGo.transform.SetParent(serverPanel.transform, false);
        var listLayout = listRootGo.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 6f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childForceExpandWidth = false;
        listRootGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        AddLayoutSize(listRootGo, 700f, 320f);

        var hostSection = CreateSection("HostSection", serverPanel.transform);
        var joinSection = CreateSection("JoinSection", serverPanel.transform);

        var serverNameInput = CreateInput("ServerName", hostSection.transform, resources, "Server name");
        var hostPortInput = CreateInput("HostPort", hostSection.transform, resources, "7770");
        var hostPasswordInput = CreateInput("HostPassword", hostSection.transform, resources, "Password (optional)");
        var hostButton = TMP_DefaultControls.CreateButton(resources);
        hostButton.name = "HostButton";
        hostButton.transform.SetParent(hostSection.transform, false);
        var hostText = hostButton.GetComponentInChildren<TextMeshProUGUI>();
        if (hostText != null) hostText.text = "Create Server";
        AddLayoutSize(hostButton, 220f, 44f);

        var directAddressInput = CreateInput("DirectAddress", joinSection.transform, resources, "localhost");
        var directPortInput = CreateInput("DirectPort", joinSection.transform, resources, "7770");
        var joinPasswordInput = CreateInput("JoinPassword", joinSection.transform, resources, "Password");
        var joinButton = TMP_DefaultControls.CreateButton(resources);
        joinButton.name = "JoinButton";
        joinButton.transform.SetParent(joinSection.transform, false);
        var joinText = joinButton.GetComponentInChildren<TextMeshProUGUI>();
        if (joinText != null) joinText.text = "Join";
        AddLayoutSize(joinButton, 220f, 44f);

        var backFromServer = TMP_DefaultControls.CreateButton(resources);
        backFromServer.name = "BackButton";
        backFromServer.transform.SetParent(serverPanel.transform, false);
        var backServerText = backFromServer.GetComponentInChildren<TextMeshProUGUI>();
        if (backServerText != null) backServerText.text = "Back";
        AddLayoutSize(backFromServer, 220f, 44f);

        var mainMenu = canvasGo.AddComponent<MainMenuController>();
        SetPrivateField(mainMenu, "mainPanel", mainPanel);
        SetPrivateField(mainMenu, "settingsPanel", settingsPanel);
        SetPrivateField(mainMenu, "serverBrowserPanel", serverPanel);

        var serverBrowser = canvasGo.AddComponent<ServerBrowserController>();
        SetPrivateField(serverBrowser, "listRoot", listRootGo.transform);
        SetPrivateField(serverBrowser, "listItemPrefab", listItemPrefab.GetComponent<ServerListItemUI>());
        SetPrivateField(serverBrowser, "serverNameInput", serverNameInput);
        SetPrivateField(serverBrowser, "hostPortInput", hostPortInput);
        SetPrivateField(serverBrowser, "hostPasswordInput", hostPasswordInput);
        SetPrivateField(serverBrowser, "hostPasswordInput", hostPasswordInput);
        SetPrivateField(serverBrowser, "directAddressInput", directAddressInput);
        SetPrivateField(serverBrowser, "directPortInput", directPortInput);
        SetPrivateField(serverBrowser, "joinPasswordInput", joinPasswordInput);

        // Wire buttons using UnityEventTools
        var playBtn = playButton.GetComponent<Button>();
        var settingsBtn = settingsButton.GetComponent<Button>();
        var backFromSettingsBtn = backFromSettings.GetComponent<Button>();
        var backFromServerBtn = backFromServer.GetComponent<Button>();
        var hostBtn = hostButton.GetComponent<Button>();
        var joinBtn = joinButton.GetComponent<Button>();

        UnityEventTools.AddPersistentListener(playBtn.onClick, mainMenu.ShowServerBrowser);
        UnityEventTools.AddPersistentListener(settingsBtn.onClick, mainMenu.ShowSettings);
        UnityEventTools.AddPersistentListener(backFromSettingsBtn.onClick, mainMenu.ShowMain);
        UnityEventTools.AddPersistentListener(backFromServerBtn.onClick, mainMenu.ShowMain);
        UnityEventTools.AddPersistentListener(hostBtn.onClick, serverBrowser.HostServer);
        UnityEventTools.AddPersistentListener(joinBtn.onClick, serverBrowser.JoinDirect);

        // Mark all button components dirty so events are serialized
        EditorUtility.SetDirty(playBtn);
        EditorUtility.SetDirty(settingsBtn);
        EditorUtility.SetDirty(backFromSettingsBtn);
        EditorUtility.SetDirty(backFromServerBtn);
        EditorUtility.SetDirty(hostBtn);
        EditorUtility.SetDirty(joinBtn);
        EditorUtility.SetDirty(mainMenu);
        EditorUtility.SetDirty(serverBrowser);

        settingsPanel.SetActive(false);
        serverPanel.SetActive(false);

        return SavePrefab(canvasGo, path);
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.35f);
        var rt = go.GetComponent<RectTransform>();
        SetFullStretch(rt);
        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        return go;
    }

    private static GameObject CreateSection(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        return go;
    }

    private static TextMeshProUGUI CreateTmpText(string name, Transform parent, TMP_DefaultControls.Resources resources, string text)
    {
        var go = TMP_DefaultControls.CreateText(resources);
        go.name = name;
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
        return tmp;
    }

    private static TMP_InputField CreateInput(string name, Transform parent, TMP_DefaultControls.Resources resources, string placeholder)
    {
        var go = TMP_DefaultControls.CreateInputField(resources);
        go.name = name;
        go.transform.SetParent(parent, false);
        var input = go.GetComponent<TMP_InputField>();
        if (input != null)
        {
            input.text = string.Empty;
            if (input.placeholder is TextMeshProUGUI p) p.text = placeholder;
        }
        return input;
    }

    private static Toggle CreateToggle(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent, false);
        var toggle = go.GetComponent<Toggle>();
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(bg.transform, false);
        toggle.targetGraphic = bg.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = false;
        AddLayoutSize(go, 220f, 36f);
        return toggle;
    }

    private static void SetFullStretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddLayoutSize(GameObject go, float width, float height)
    {
        if (go == null) return;
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void SetPrivateField(Object obj, string fieldName, Object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(obj, value);
    }
}
