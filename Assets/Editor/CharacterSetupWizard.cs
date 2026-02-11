using UnityEngine;
using UnityEditor;
using System.IO;
using FishNet.Object;

public class CharacterSetupWizard : EditorWindow
{
    private string characterName = "";
    private Texture2D characterTexture;
    
    [MenuItem("Tools/Character Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<CharacterSetupWizard>("Character Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Character Setup Wizard", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        characterName = EditorGUILayout.TextField("Character Name:", characterName);
        characterTexture = (Texture2D)EditorGUILayout.ObjectField("Character Texture:", characterTexture, typeof(Texture2D), false);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Create Character", GUILayout.Height(40)))
        {
            if (string.IsNullOrWhiteSpace(characterName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a character name!", "OK");
                return;
            }
            
            if (characterTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a character texture!", "OK");
                return;
            }
            
            CreateCharacter();
        }
    }
    
    void CreateCharacter()
    {
        string safeName = characterName.Trim().Replace(" ", "");
        string folderPath = $"Assets/Champions/{safeName}";
        
        // Create folder structure
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Champions", safeName);
        }
        
        // Create material
        Material mat = CreateMaterial(folderPath, safeName);
        
        // Create gameplay prefab
        GameObject gameplayPrefab = CreateGameplayPrefab(safeName, mat);
        string gameplayPrefabPath = $"{folderPath}/{safeName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(gameplayPrefab, gameplayPrefabPath);
        DestroyImmediate(gameplayPrefab);
        
        // Create preview prefab
        GameObject previewPrefab = CreatePreviewPrefab(safeName, mat);
        string previewPrefabPath = $"{folderPath}/{safeName}Preview.prefab";
        PrefabUtility.SaveAsPrefabAsset(previewPrefab, previewPrefabPath);
        DestroyImmediate(previewPrefab);
        
        // Create controller script
        CreateControllerScript(folderPath, safeName);
        
        // Add to CharacterSelection
        AddToCharacterSelection(gameplayPrefabPath, previewPrefabPath, safeName);
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", 
            $"Character '{safeName}' created successfully!\n\n" +
            $"Folder: {folderPath}\n" +
            $"Gameplay Prefab: {gameplayPrefabPath}\n" +
            $"Preview Prefab: {previewPrefabPath}\n\n" +
            $"Next steps:\n" +
            "1. Wait for scripts to compile\n" +
            "2. Add the controller component to the prefab\n" +
            "3. Implement abilities in the controller script\n" +
            "4. Configure ability icons and stats", 
            "OK");
        
        // Select the gameplay prefab
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(gameplayPrefabPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
    
    Material CreateMaterial(string folderPath, string characterName)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = $"{characterName}_Mat";
        
        if (characterTexture != null)
        {
            mat.mainTexture = characterTexture;
            mat.SetTexture("_BaseMap", characterTexture);
        }
        
        string matPath = $"{folderPath}/{characterName}_Mat.mat";
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }
    
    GameObject CreateGameplayPrefab(string characterName, Material material)
    {
        // Create root GameObject
        GameObject character = new GameObject(characterName);
        
        // Add FishNet networking
        var netObj = character.AddComponent<NetworkObject>();
        var netController = character.AddComponent<NetworkPlayerController>();
        
        // Add core components
        character.AddComponent<CharacterControl>();
        character.AddComponent<HealthSystem>();
        character.tag = "Player";
        
        // Add CharacterController for movement
        var charController = character.AddComponent<CharacterController>();
        charController.radius = 0.5f;
        charController.height = 2f;
        charController.center = new Vector3(0, 1, 0);
        
        // Create visual model
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        model.name = "Model";
        model.transform.SetParent(character.transform);
        model.transform.localPosition = new Vector3(0, 1, 0);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        
        // Remove the default collider (we use CharacterController)
        DestroyImmediate(model.GetComponent<Collider>());
        
        // Apply material
        var renderer = model.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
        
        // Create camera
        GameObject cameraObj = new GameObject("Camera");
        cameraObj.transform.SetParent(character.transform);
        cameraObj.transform.localPosition = new Vector3(0, 1.6f, -3.5f);
        cameraObj.transform.localRotation = Quaternion.Euler(10, 0, 0);
        var cam = cameraObj.AddComponent<Camera>();
        cam.fieldOfView = 60;
        cameraObj.AddComponent<AudioListener>();
        
        // Create fire point
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(character.transform);
        firePoint.transform.localPosition = new Vector3(0, 1.6f, 0);
        firePoint.transform.localRotation = Quaternion.identity;
        
        return character;
    }
    
    GameObject CreatePreviewPrefab(string characterName, Material material)
    {
        GameObject preview = new GameObject($"{characterName}Preview");
        
        // Create visual model (same as gameplay but no controllers)
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        model.name = "Model";
        model.transform.SetParent(preview.transform);
        model.transform.localPosition = new Vector3(0, 1, 0);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        
        // Apply material
        var renderer = model.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
        
        return preview;
    }
    
    void CreateControllerScript(string folderPath, string characterName)
    {
        string scriptContent = $@"using System.Collections;
using UnityEngine;

public class {characterName}Controller : MonoBehaviour
{{
    [Header(""References"")]
    public GameObject cam;
    public Transform firePoint;

    [Header(""Projectile Prefabs"")]
    // Assign your ability prefabs here

    [Header(""HUD Icons"")]
    // Assign your ability icons here
    public Sprite lmbIcon;
    public Sprite rmbIcon;
    public Sprite qIcon;
    public Sprite eIcon;

    [Header(""LMB - Basic Attack"")]
    public float lmbDamage = 40f;
    public float lmbCooldown = 0.5f;
    private bool _lmbOnCooldown = false;

    [Header(""RMB - Ability 1"")]
    public float rmbCooldown = 8f;
    private bool _rmbOnCooldown = false;

    [Header(""Q - Ability 2"")]
    public float qCooldown = 10f;
    private bool _qOnCooldown = false;

    [Header(""E - Ability 3"")]
    public float eCooldown = 12f;
    private bool _eOnCooldown = false;

    private CooldownUIManager _cooldownUi;

    void Awake()
    {{
        if (cam == null)
        {{
            var c = GetComponentInChildren<Camera>(true);
            if (c != null) cam = c.gameObject;
        }}
        if (firePoint == null) firePoint = transform;
        _cooldownUi = FindObjectOfType<CooldownUIManager>(true);
    }}

    void Start()
    {{
        UpdateAbilityIcons();
    }}

    void Update()
    {{
        if (Input.GetMouseButtonDown(0)) TryLMB();
        if (Input.GetMouseButtonDown(1)) TryRMB();
        if (Input.GetKeyDown(KeyCode.Q)) TryQ();
        if (Input.GetKeyDown(KeyCode.E)) TryE();
    }}

    private void UpdateAbilityIcons()
    {{
        var cooldownUi = ResolveCooldownUi();
        if (cooldownUi == null) return;
        if (lmbIcon != null) cooldownUi.SetAbilityIcon(AbilityKey.LeftClick, lmbIcon);
        if (rmbIcon != null) cooldownUi.SetAbilityIcon(AbilityKey.RightClick, rmbIcon);
        if (qIcon != null) cooldownUi.SetAbilityIcon(AbilityKey.One, qIcon);
        if (eIcon != null) cooldownUi.SetAbilityIcon(AbilityKey.Two, eIcon);
    }}

    private CooldownUIManager ResolveCooldownUi()
    {{
        if (_cooldownUi == null) _cooldownUi = FindObjectOfType<CooldownUIManager>(true);
        return _cooldownUi != null ? _cooldownUi : CooldownUIManager.Instance;
    }}

    #region LMB - Basic Attack
    public void TryLMB()
    {{
        if (_lmbOnCooldown) return;
        if (cam == null) return;

        // TODO: Implement LMB ability
        Debug.Log(""{characterName} LMB fired!"");

        _lmbOnCooldown = true;
        StartCoroutine(LMBCooldown());
        var cooldownUi = ResolveCooldownUi();
        if (cooldownUi != null)
            cooldownUi.StartCooldown(AbilityKey.LeftClick, lmbCooldown);
    }}

    IEnumerator LMBCooldown()
    {{
        yield return new WaitForSeconds(lmbCooldown);
        _lmbOnCooldown = false;
    }}
    #endregion

    #region RMB - Ability 1
    public void TryRMB()
    {{
        if (_rmbOnCooldown) return;
        if (cam == null) return;

        // TODO: Implement RMB ability
        Debug.Log(""{characterName} RMB used!"");

        _rmbOnCooldown = true;
        StartCoroutine(RMBCooldown());
        var cooldownUi = ResolveCooldownUi();
        if (cooldownUi != null)
            cooldownUi.StartCooldown(AbilityKey.RightClick, rmbCooldown);
    }}

    IEnumerator RMBCooldown()
    {{
        yield return new WaitForSeconds(rmbCooldown);
        _rmbOnCooldown = false;
    }}
    #endregion

    #region Q - Ability 2
    public void TryQ()
    {{
        if (_qOnCooldown) return;
        if (cam == null) return;

        // TODO: Implement Q ability
        Debug.Log(""{characterName} Q used!"");

        _qOnCooldown = true;
        StartCoroutine(QCooldown());
        var cooldownUi = ResolveCooldownUi();
        if (cooldownUi != null)
            cooldownUi.StartCooldown(AbilityKey.One, qCooldown);
    }}

    IEnumerator QCooldown()
    {{
        yield return new WaitForSeconds(qCooldown);
        _qOnCooldown = false;
    }}
    #endregion

    #region E - Ability 3
    public void TryE()
    {{
        if (_eOnCooldown) return;
        if (cam == null) return;

        // TODO: Implement E ability
        Debug.Log(""{characterName} E used!"");

        _eOnCooldown = true;
        StartCoroutine(ECooldown());
        var cooldownUi = ResolveCooldownUi();
        if (cooldownUi != null)
            cooldownUi.StartCooldown(AbilityKey.Two, eCooldown);
    }}

    IEnumerator ECooldown()
    {{
        yield return new WaitForSeconds(eCooldown);
        _eOnCooldown = false;
    }}
    #endregion
}}
";

        string scriptPath = $"{folderPath}/{characterName}Controller.cs";
        File.WriteAllText(scriptPath, scriptContent);
        AssetDatabase.ImportAsset(scriptPath);
    }
    
    void AddToCharacterSelection(string gameplayPrefabPath, string previewPrefabPath, string characterName)
    {
        // Find CharacterSelection in the scene
        CharacterSelection selection = FindObjectOfType<CharacterSelection>(true);
        
        if (selection == null)
        {
            Debug.LogWarning("CharacterSelection not found in scene. Character not added to selection list.");
            return;
        }
        
        // Load prefabs
        GameObject gameplayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gameplayPrefabPath);
        GameObject previewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(previewPrefabPath);
        
        if (gameplayPrefab == null || previewPrefab == null)
        {
            Debug.LogWarning("Could not load prefabs. Character not added to selection list.");
            return;
        }
        
        // Create new entry
        var entry = new CharacterSelection.CharacterEntry
        {
            id = characterName,
            preview = previewPrefab,
            gameplayPrefab = gameplayPrefab,
            lmbName = "Basic Attack",
            rmbName = "Ability 1",
            oneName = "Q Ability",
            twoName = "E Ability",
            lmbDesc = "TODO: Describe LMB ability",
            rmbDesc = "TODO: Describe RMB ability",
            oneDesc = "TODO: Describe Q ability",
            twoDesc = "TODO: Describe E ability"
        };
        
        // Add to list
        selection.characters.Add(entry);
        
        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);
        
        Debug.Log($"Added {characterName} to CharacterSelection");
    }
}
