using System.Collections.Generic;
using System.IO;
using FishNet.Managing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class SetupNetworkScenes
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string SelectionScenePath = "Assets/Scenes/selection.unity";
    private const string MainMenuPrefabPath = "Assets/Networking/Menu/Prefabs/MainMenuCanvas.prefab";
    private const string LobbyPanelPrefabPath = "Assets/Networking/Menu/Prefabs/LobbyReadyPanel.prefab";
    private const string ServerListItemPrefabPath = "Assets/Networking/Menu/Prefabs/ServerListItem.prefab";

    [MenuItem("Tools/Setup FishNet Menu and Lobby Scenes")]
    public static void SetupScenes()
    {
        var previousScene = SceneManager.GetActiveScene();

        EnsureMenuScene();
        EnsureSelectionScene();
        EnsureBuildSettings();

        if (previousScene.IsValid())
            EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureMenuScene()
    {
        Scene scene;
        if (File.Exists(MenuScenePath))
            scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        else
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var networkRoot = GameObject.Find("Network");
        if (networkRoot == null) networkRoot = new GameObject("Network");

        var networkManager = networkRoot.GetComponent<NetworkManager>();
        if (networkManager == null) networkManager = networkRoot.AddComponent<NetworkManager>();

        var sessionContext = networkRoot.GetComponent<SessionContext>();
        if (sessionContext == null) sessionContext = networkRoot.AddComponent<SessionContext>();

        var authenticator = networkRoot.GetComponent<NetworkSessionAuthenticator>();
        if (authenticator == null) authenticator = networkRoot.AddComponent<NetworkSessionAuthenticator>();

        var controller = networkRoot.GetComponent<FishNetNetworkController>();
        if (controller == null) controller = networkRoot.AddComponent<FishNetNetworkController>();

        SetSerializedField(controller, "networkManager", networkManager);
        SetSerializedField(controller, "sessionContext", sessionContext);
        SetSerializedField(controller, "authenticator", authenticator);
        SetSerializedField(controller, "selectionSceneName", "selection");
        SetSerializedField(controller, "gameplaySceneName", "SampleScene");

        var listenerObj = GameObject.Find("LanServerListener");
        if (listenerObj == null) listenerObj = new GameObject("LanServerListener");
        var listener = listenerObj.GetComponent<LanServerListener>();
        if (listener == null) listener = listenerObj.AddComponent<LanServerListener>();

        var broadcasterObj = GameObject.Find("LanServerBroadcaster");
        if (broadcasterObj == null) broadcasterObj = new GameObject("LanServerBroadcaster");
        var broadcaster = broadcasterObj.GetComponent<LanServerBroadcaster>();
        if (broadcaster == null) broadcaster = broadcasterObj.AddComponent<LanServerBroadcaster>();

        SetSerializedField(broadcaster, "networkManager", networkManager);
        SetSerializedField(broadcaster, "sessionContext", sessionContext);

        // Ensure EventSystem exists for UI input
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        var mainMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
        if (mainMenuPrefab != null && Object.FindObjectOfType<MainMenuController>(true) == null)
        {
            PrefabUtility.InstantiatePrefab(mainMenuPrefab, scene);
        }

        var serverListPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ServerListItemPrefabPath);
        var serverBrowser = Object.FindObjectOfType<ServerBrowserController>(true);
        if (serverBrowser != null)
        {
            SetSerializedField(serverBrowser, "networkController", controller);
            SetSerializedField(serverBrowser, "sessionContext", sessionContext);
            SetSerializedField(serverBrowser, "lanListener", listener);
            if (serverListPrefab != null)
                SetSerializedField(serverBrowser, "listItemPrefab", serverListPrefab.GetComponent<ServerListItemUI>());
        }

        if (string.IsNullOrEmpty(scene.path))
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        else
            EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureSelectionScene()
    {
        if (!File.Exists(SelectionScenePath))
            return;

        Scene scene = EditorSceneManager.OpenScene(SelectionScenePath, OpenSceneMode.Single);

        var session = Object.FindObjectOfType<NetworkGameSession>(true);
        if (session == null)
        {
            var go = new GameObject("NetworkGameSession");
            session = go.AddComponent<NetworkGameSession>();
        }
        session.gameplaySceneName = "SampleScene";
        session.autoStartWhenAllSelected = true;
        session.requireReadyToStart = true;

        var selection = Object.FindObjectOfType<CharacterSelection>(true);
        if (selection != null)
        {
            session.characterPrefabs = new List<NetworkGameSession.CharacterPrefabEntry>();
            foreach (var entry in selection.characters)
            {
                if (entry == null || entry.gameplayPrefab == null) continue;
                string id = !string.IsNullOrEmpty(entry.id) ? entry.id : entry.gameplayPrefab.name;
                session.characterPrefabs.Add(new NetworkGameSession.CharacterPrefabEntry
                {
                    id = id,
                    prefab = entry.gameplayPrefab
                });
            }
            EditorUtility.SetDirty(session);
        }

        // Ensure EventSystem exists for lobby UI input
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        var lobbyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPanelPrefabPath);
        if (lobbyPrefab != null && Object.FindObjectOfType<LobbyReadyPanel>(true) == null)
        {
            PrefabUtility.InstantiatePrefab(lobbyPrefab, scene);
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        AddSceneIfMissing(scenes, MenuScenePath);
        AddSceneIfMissing(scenes, SelectionScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddSceneIfMissing(List<EditorBuildSettingsScene> scenes, string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        foreach (var s in scenes)
        {
            if (s.path == path) return;
        }
        scenes.Add(new EditorBuildSettingsScene(path, true));
    }

    private static void SetSerializedField(Object target, string fieldName, Object value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null) return;
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    private static void SetSerializedField(Object target, string fieldName, string value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null) return;
        prop.stringValue = value;
        so.ApplyModifiedProperties();
    }
}
