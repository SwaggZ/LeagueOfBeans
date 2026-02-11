using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FishNet.Managing;
using System.Collections.Generic;

/// <summary>
/// Editor tool to automatically create a character selection screen in the current scene.
/// Generates Canvas UI, character buttons, preview camera, and wires CharacterSelection component.
/// Usage: Tools → Setup Selection Screen
/// </summary>
public class SetupSelectionScreen
{
    private const string CHARACTER_NAMES = "Ahri,Ashe,Galio,Caitlyn,Aphelios,Jhin,Lux";
    private const string CHARACTER_IDS = "Ahri,Ashe,Galio,Caitlyn,Aphelios,Jhin,Lux"; // Must match NetworkGameSession prefab IDs!

    [MenuItem("Tools/Setup Selection Screen")]
    public static void CreateSelectionScreen()
    {
        // Get or create active scene
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Error", "No active scene loaded!", "OK");
            return;
        }

        Debug.Log("[SetupSelectionScreen] Creating selection screen in scene: " + activeScene.name);

        // Check if SelectionCanvas already exists
        GameObject canvasGo = GameObject.Find("SelectionCanvas");
        bool isNewCanvas = false;
        if (canvasGo == null)
        {
            // Create root Canvas
            canvasGo = new GameObject("SelectionCanvas");
            isNewCanvas = true;
        }
        else
        {
            Debug.Log("[SetupSelectionScreen] Found existing SelectionCanvas, updating configuration...");
        }

        // Setup Canvas (only add components if new)
        if (isNewCanvas)
        {
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            Debug.Log("[SetupSelectionScreen] Created Canvas");
        }
        else
        {
            // Ensure Canvas is properly configured
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("[SetupSelectionScreen] Updated existing Canvas");
        }

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        if (canvasRect == null) canvasRect = canvasGo.AddComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // Get or create background panel
        GameObject bgPanel = canvasGo.transform.Find("Background")?.gameObject;
        if (bgPanel == null)
        {
            bgPanel = CreatePanel("Background", canvasGo.transform, new Color(0.1f, 0.1f, 0.1f, 0.8f));
        }
        else
        {
            UpdatePanelColor(bgPanel, new Color(0.1f, 0.1f, 0.1f, 0.8f));
        }
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Create or get left panel for character list
        GameObject leftPanel = canvasGo.transform.Find("CharacterListPanel")?.gameObject;
        if (leftPanel == null)
        {
            leftPanel = CreatePanel("CharacterListPanel", canvasGo.transform, new Color(0.15f, 0.15f, 0.15f, 1f));
        }
        else
        {
            UpdatePanelColor(leftPanel, new Color(0.15f, 0.15f, 0.15f, 1f));
        }
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.25f, 1);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;

        // Create or get right panel for preview and info
        GameObject rightPanel = canvasGo.transform.Find("PreviewPanel")?.gameObject;
        if (rightPanel == null)
        {
            rightPanel = CreatePanel("PreviewPanel", canvasGo.transform, new Color(0.2f, 0.2f, 0.2f, 1f));
        }
        else
        {
            UpdatePanelColor(rightPanel, new Color(0.2f, 0.2f, 0.2f, 1f));
        }
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.25f, 0);
        rightRect.anchorMax = Vector2.one;
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;

        Debug.Log("[SetupSelectionScreen] Created or updated panels");

        // Create or update character buttons
        string[] names = CHARACTER_NAMES.Split(',');
        string[] ids = CHARACTER_IDS.Split(',');
        
        GameObject buttonContainer = leftPanel.transform.Find("ButtonContainer")?.gameObject;
        if (buttonContainer == null)
        {
            buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(leftPanel.transform, false);
            RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(10, 10);
            containerRect.offsetMax = new Vector2(-10, -10);

            VerticalLayoutGroup vlg = buttonContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.childForceExpandHeight = true;
            vlg.childForceExpandWidth = false;
            Debug.Log("[SetupSelectionScreen] Created new button container");
        }
        else
        {
            // Clear old buttons
            foreach (Transform child in buttonContainer.transform)
            {
                Object.DestroyImmediate(child.gameObject);
            }
            Debug.Log("[SetupSelectionScreen] Cleared existing buttons");
        }

        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i].Trim();
            string id = ids[i].Trim();
            CreateCharacterButton(buttonContainer.transform, name, id, i);
        }

        Debug.Log("[SetupSelectionScreen] Created character buttons");

        // Create preview area (RawImage for camera render)
        GameObject previewImageGo = CreatePanel("PreviewImage", rightPanel.transform, new Color(0.3f, 0.3f, 0.3f, 1f));
        RectTransform previewRect = previewImageGo.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.05f, 0.4f);
        previewRect.anchorMax = new Vector2(0.95f, 0.95f);
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;

        Image panelImage = previewImageGo.GetComponent<Image>();
        Object.DestroyImmediate(panelImage);
        RawImage previewRawImage = previewImageGo.AddComponent<RawImage>();
        previewRawImage.color = Color.white;

        // Create info panel
        GameObject infoPanel = CreatePanel("InfoPanel", rightPanel.transform, new Color(0.25f, 0.25f, 0.25f, 1f));
        RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.05f, 0.05f);
        infoRect.anchorMax = new Vector2(0.95f, 0.35f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;

        // Create character name label
        GameObject labelGo = new GameObject("CharacterNameLabel");
        labelGo.transform.SetParent(infoPanel.transform, false);
        TextMeshProUGUI labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = "Select a Character";
        labelText.alignment = TextAlignmentOptions.TopLeft;
        labelText.fontSize = 36;
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, -40);
        labelRect.offsetMax = new Vector2(-10, -10);

        // Create ability descriptions
        GameObject desc1Go = CreateAbilityText("LMBDesc", infoPanel.transform, "LMB: Base Attack");
        GameObject desc2Go = CreateAbilityText("RMBDesc", infoPanel.transform, "RMB: Ability 1");
        GameObject desc3Go = CreateAbilityText("OneDesc", infoPanel.transform, "1: Ability 2");
        GameObject desc4Go = CreateAbilityText("TwoDesc", infoPanel.transform, "2: Ability 3");

        Debug.Log("[SetupSelectionScreen] Created preview and info panels");

        // Create Start button (will wire it later after CharacterSelection is set up)
        GameObject startButtonGo = CreateButton("StartButton", rightPanel.transform, "START", new Color(0.2f, 0.8f, 0.2f, 1f));
        RectTransform startRect = startButtonGo.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.3f, 0.01f);
        startRect.anchorMax = new Vector2(0.7f, 0.04f);
        startRect.offsetMin = Vector2.zero;
        startRect.offsetMax = Vector2.zero;

        Button startButton = startButtonGo.GetComponent<Button>();
        // Clear any existing listeners (in case we're updating)
        startButton.onClick.RemoveAllListeners();
        
        Debug.Log("[SetupSelectionScreen] Created Start button (wiring deferred)");

        // Create preview camera
        GameObject cameraGo = new GameObject("SelectionCamera");
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        camera.cullingMask = LayerMask.GetMask("Default"); // Only render default layer
        camera.depth = -1; // Render before main camera

        // Create render texture and assign to camera
        RenderTexture rt = new RenderTexture(512, 512, 24);
        camera.targetTexture = rt;
        previewRawImage.texture = rt;

        // Position preview camera
        cameraGo.transform.position = new Vector3(0, 1.5f, 3);
        cameraGo.transform.LookAt(Vector3.zero + Vector3.up);

        Debug.Log("[SetupSelectionScreen] Created preview camera");

        // Get or create CharacterSelection component on NetworkManager
        CharacterSelection charSelection = Object.FindObjectOfType<CharacterSelection>();
        if (charSelection == null)
        {
            // Try to find NetworkManager
            NetworkManager nm = Object.FindObjectOfType<NetworkManager>();
            if (nm != null)
            {
                charSelection = nm.GetComponent<CharacterSelection>();
                if (charSelection == null)
                    charSelection = nm.gameObject.AddComponent<CharacterSelection>();
            }
            else
            {
                // Create a new GameObject for CharacterSelection
                GameObject csGo = new GameObject("CharacterSelectionManager");
                charSelection = csGo.AddComponent<CharacterSelection>();
            }
        }

        // Wire up CharacterSelection references
        charSelection.label = labelText;
        charSelection.lmbNameText = desc1Go.GetComponent<TextMeshProUGUI>();
        charSelection.rmbNameText = desc2Go.GetComponent<TextMeshProUGUI>();
        charSelection.oneNameText = desc3Go.GetComponent<TextMeshProUGUI>();
        charSelection.twoNameText = desc4Go.GetComponent<TextMeshProUGUI>();

        // Find or create ability description fields
        charSelection.lmbDescText = CreateAbilityDescPanel("LMBDescPanel", infoPanel.transform, 0).GetComponent<TextMeshProUGUI>();
        charSelection.rmbDescText = CreateAbilityDescPanel("RMBDescPanel", infoPanel.transform, 1).GetComponent<TextMeshProUGUI>();
        charSelection.oneDescText = CreateAbilityDescPanel("OneDescPanel", infoPanel.transform, 2).GetComponent<TextMeshProUGUI>();
        charSelection.twoDescText = CreateAbilityDescPanel("TwoDescPanel", infoPanel.transform, 3).GetComponent<TextMeshProUGUI>();

        // AUTO-CONFIGURE CHARACTER ENTRIES with proper IDs
        // This is important so that character IDs match the prefab list in NetworkGameSession
        if (charSelection.characters == null || charSelection.characters.Count == 0)
        {
            charSelection.characters = new List<CharacterSelection.CharacterEntry>();
            
            // Find preview GameObjects and create entries for each character
            var previewContainer = buttonContainer.transform.parent; // Left panel
            var previews = new[] { "AhriPreview", "AshePreview", "CaitPreview", "GalioPreview", "ApheliosPreview", "JhinPreview", "luxPreview" };
            var characterNames = new[] { "Ahri", "Ashe", "Caitlyn", "Galio", "Aphelios", "Jhin", "Lux" };
            
            for (int i = 0; i < characterNames.Length; i++)
            {
                var entry = new CharacterSelection.CharacterEntry();
                entry.id = characterNames[i]; // ID should match NetworkGameSession prefab IDs!
                
                // Find preview GameObject in scene
                var previewGo = Object.FindObjectOfType<Transform>()?.root.Find(previews[i])?.gameObject;
                if (previewGo == null)
                    previewGo = GameObject.Find(previews[i]);
                
                entry.preview = previewGo;
                
                // Try to find gameplay prefab
                string prefabPath = $"Assets/Champions/{characterNames[i]}/{characterNames[i]}Controller.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                entry.gameplayPrefab = prefab;
                
                charSelection.characters.Add(entry);
                Debug.Log($"[SetupSelectionScreen] Added character entry: {characterNames[i]} (preview: {(previewGo != null ? previewGo.name : "NOT FOUND")})");
            }
        }

        Debug.Log("[SetupSelectionScreen] Configured CharacterSelection component");

        // NOW wire the START button to CharacterSelection.StartGame()
        startButton.onClick.AddListener(() => charSelection.StartGame());
        Debug.Log("[SetupSelectionScreen] Wired START button to CharacterSelection.StartGame()");

        EditorUtility.DisplayDialog("Success", "Selection screen created successfully!\n\nConfiguration complete:\n✓ 7 character buttons\n✓ Preview camera\n✓ Character entries properly configured\n✓ START button wired", "OK");

        Debug.Log("[SetupSelectionScreen] Selection screen setup complete");
    }

    private static GameObject CreateCharacterButton(Transform parent, string characterName, string characterId, int index)
    {
        GameObject buttonGo = new GameObject(characterName + "Button");
        buttonGo.transform.SetParent(parent, false);

        Button button = buttonGo.AddComponent<Button>();
        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        buttonImage.raycastTarget = true;

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 60);
        LayoutElement layoutElem = buttonGo.AddComponent<LayoutElement>();
        layoutElem.preferredHeight = 60;

        // Button text
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = characterName;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 28;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Button colors
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        button.colors = colors;

        // Add click handler to CharacterSelection
        CharacterSelection charSelection = Object.FindObjectOfType<CharacterSelection>();
        if (charSelection != null)
        {
            int charIndex = index;
            button.onClick.AddListener(() => charSelection.SelectByIndex(charIndex));
        }

        return buttonGo;
    }

    private static GameObject CreateButton(string name, Transform parent, string buttonText, Color color)
    {
        GameObject buttonGo = new GameObject(name);
        buttonGo.transform.SetParent(parent, false);

        Image image = buttonGo.AddComponent<Image>();
        image.color = color;

        Button button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        button.colors = colors;

        // Text
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = buttonText;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 32;
        text.fontStyle = FontStyles.Bold;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonGo;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panelGo = new GameObject(name);
        panelGo.transform.SetParent(parent, false);

        Image image = panelGo.AddComponent<Image>();
        image.color = color;

        RectTransform rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return panelGo;
    }

    private static GameObject CreateAbilityText(string name, Transform parent, string text)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = go.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 20;
        textComponent.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 30);
        rect.anchoredPosition = Vector2.zero;

        return go;
    }

    private static GameObject CreateAbilityDescPanel(string name, Transform parent, int slotIndex)
    {
        GameObject panelGo = new GameObject(name);
        panelGo.transform.SetParent(parent, false);

        Image image = panelGo.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        TextMeshProUGUI text = panelGo.AddComponent<TextMeshProUGUI>();
        text.text = $"Slot {slotIndex}: Description";
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.wordWrappingRatios = 0.5f;

        LayoutElement layout = panelGo.AddComponent<LayoutElement>();
        layout.preferredHeight = 30;

        RectTransform rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(-20, 30);
        rect.anchoredPosition = new Vector2(0, -10 - (slotIndex * 35));

        return panelGo;
    }

    private static void UpdatePanelColor(GameObject panelGo, Color color)
    {
        Image image = panelGo.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }
}
