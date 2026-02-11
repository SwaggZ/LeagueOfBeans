using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum AbilityKey
{
    LeftClick,
    RightClick,
    One,
    Two,
    Ctrl
}

// Drop this anywhere in the scene (or it will auto-create itself on first access).
// Shows 5 ability slots (LMB, RMB, Q, E, CTRL) with icons, cooldown radial and timer text.
// Use: CooldownUIManager.Instance.SetAbilityIcon(AbilityKey.One, mySprite);
//      CooldownUIManager.Instance.StartCooldown(AbilityKey.One, durationSeconds);
[DisallowMultipleComponent]
public class CooldownUIManager : MonoBehaviour
{
    public static CooldownUIManager Instance { get; private set; }

    [Header("Layout")]
    public Vector2 anchorPos = new Vector2(0.5f, 0f); // bottom-center
    public Vector2 anchoredOffset = new Vector2(0f, 24f);
    public float slotSize = 52f;
    public float spacing = 6f;

    [Header("Appearance")]
    public Color slotBorderColor = new Color(1f, 1f, 1f, 0.9f);
    public Color missingIconColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public string missingIconText = "NO IMG";
    public Color cooldownMaskColor = new Color(0f, 0f, 0f, 0.6f);
    public Color keyLabelColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
    public Color timerTextColor = new Color(1f, 1f, 1f, 0.95f);

    [Header("Default Loadout (Optional)")]
    [Tooltip("If provided, these icons/presence will be applied on Awake. You can also call ApplyLoadout at runtime per character.")]
    public List<AbilityEntry> defaultAbilities = new List<AbilityEntry>();
    public AbilityEntry defaultDash; // Applied to CTRL (CTRL is Dash)

    private Canvas _canvas;
    private RectTransform _rootRT;
    private RectTransform _barRT;

    private class Slot
    {
        public RectTransform root;
        public Image border;
        public Image icon;
        public Text keyLabel;
        public Image cooldownMask;
        public Text timerLabel;
        public bool hasIcon;
        public float cooldownEnd;
        public float cooldownDuration;
    }

    private Dictionary<AbilityKey, Slot> _slots = new Dictionary<AbilityKey, Slot>();
    private static Sprite _uiSprite;
    private static Font _defaultFont;

    [Header("Advanced")]
    [Tooltip("If true, the manager will NOT relocate to a dedicated root host even when attached to a character or child object. Not recommended unless you know you want per-character HUD lifetimes.")]
    public bool forceStayOnThisObject = false;

    [Serializable]
    public class AbilityEntry
    {
        public AbilityKey key;
        public Sprite icon;
        public bool present = true; // set false if character doesn't have this ability
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another manager already exists, remove only this component.
            // Never destroy the entire GameObject (could be a character preview in selection).
            Debug.Log("[CooldownUIManager] Duplicate detected on '" + gameObject.name + "'. Removing this component and keeping existing global manager.");
            Destroy(this);
            return;
        }
        // If this component was added to a character or any non-root object in a scene (like Character Select),
        // do NOT mark that object as DontDestroyOnLoad. Instead, spawn a dedicated root host for the manager.
        bool attachedToCharacter = GetComponent<CharacterControl>() != null || GetComponentInParent<CharacterControl>() != null;
        bool isRoot = transform.parent == null;

        if (!forceStayOnThisObject && (!isRoot || attachedToCharacter))
        {
            Debug.Log("[CooldownUIManager] Relocating from '" + gameObject.name + "' to a dedicated root host GameObject.");
            GameObject host = new GameObject("CooldownUIManager");
            var mgr = host.AddComponent<CooldownUIManager>();
            // Copy a few configurable fields if desired
            mgr.anchorPos = this.anchorPos;
            mgr.anchoredOffset = this.anchoredOffset;
            mgr.slotSize = this.slotSize;
            mgr.spacing = this.spacing;
            mgr.slotBorderColor = this.slotBorderColor;
            mgr.missingIconColor = this.missingIconColor;
            mgr.missingIconText = this.missingIconText;
            mgr.cooldownMaskColor = this.cooldownMaskColor;
            mgr.keyLabelColor = this.keyLabelColor;
            mgr.timerTextColor = this.timerTextColor;
            mgr.defaultAbilities = new List<AbilityEntry>(this.defaultAbilities);
            mgr.defaultDash = this.defaultDash;

            // Disable/destroy this component so it doesn't try to initialize on the character
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureResources();
        BuildUI();

        // Hide in Character Selection scene(s)
        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateVisibilityForScene();

        // Apply default loadout if configured
        if (defaultAbilities != null && defaultAbilities.Count > 0)
        {
            ApplyLoadout(defaultAbilities, defaultDash);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateVisibilityForScene();
    }

    private void UpdateVisibilityForScene()
    {
        // Check if SelectionCanvas is actually active, not just if CharacterSelection exists
        GameObject selectionCanvas = GameObject.Find("SelectionCanvas");
        bool inSelection = (selectionCanvas != null && selectionCanvas.activeInHierarchy)
                           || SceneManager.GetActiveScene().name.ToLower().Contains("select");
        if (_rootRT != null)
        {
            _rootRT.gameObject.SetActive(!inSelection);
        }

        // Ensure the cursor is free/visible in selection scenes
        if (inSelection)
        {
            try
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            catch { }
        }
    }

    private void EnsureResources()
    {
        if (_uiSprite == null)
        {
            // Create a simple 1x1 white sprite to avoid relying on built-in UI resources
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _uiSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            _uiSprite.name = "CooldownUI_WhiteSprite";
        }

        if (_defaultFont == null)
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_defaultFont == null)
            {
                try
                {
                    _defaultFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans" }, 14);
                }
                catch { }
            }
        }
    }

    private void BuildUI()
    {
        // Canvas root
        GameObject canvasGO = new GameObject("HUD_Cooldowns", typeof(RectTransform));
        _rootRT = canvasGO.GetComponent<RectTransform>();
        // Parent the UI under this manager so lifetime follows the manager
        _rootRT.SetParent(this.transform, false);
        _rootRT.anchorMin = new Vector2(0f, 0f);
        _rootRT.anchorMax = new Vector2(1f, 1f);
        _rootRT.offsetMin = Vector2.zero;
        _rootRT.offsetMax = Vector2.zero;

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // prefer matching height to keep bottom bar consistent
        canvasGO.AddComponent<GraphicRaycaster>();

        // No need for DontDestroyOnLoad on the canvas since it's a child of this manager

        // Bar panel
        GameObject barGO = new GameObject("CooldownBar", typeof(RectTransform));
        _barRT = barGO.GetComponent<RectTransform>();
        _barRT.SetParent(_rootRT, false);
        int slotCount = 5; // LMB, RMB, Q, E, CTRL (CTRL is Dash)
        _barRT.sizeDelta = new Vector2((slotSize * slotCount) + (spacing * (slotCount - 1)), slotSize);
        _barRT.anchorMin = anchorPos;
        _barRT.anchorMax = anchorPos;
        _barRT.pivot = new Vector2(0.5f, 0f);
        _barRT.anchoredPosition = anchoredOffset;

        // Build fixed order of slots
        CreateSlot(AbilityKey.LeftClick, "LMB", 0);
        CreateSlot(AbilityKey.RightClick, "RMB", 1);
        CreateSlot(AbilityKey.One, "Q", 2);
        CreateSlot(AbilityKey.Two, "E", 3);
        CreateSlot(AbilityKey.Ctrl, "CTRL", 4);
    }

    private void CreateSlot(AbilityKey key, string label, int index)
    {
        GameObject slotGO = new GameObject($"Slot_{key}", typeof(RectTransform));
        RectTransform slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.SetParent(_barRT, false);
        slotRT.sizeDelta = new Vector2(slotSize, slotSize);
        slotRT.anchorMin = new Vector2(0.5f, 0.5f);
        slotRT.anchorMax = new Vector2(0.5f, 0.5f);
        slotRT.pivot = new Vector2(0.5f, 0.5f);
        float startX = -((_barRT.sizeDelta.x - slotSize) * 0.5f);
        float x = startX + index * (slotSize + spacing) + slotSize * 0.5f;
        slotRT.anchoredPosition = new Vector2(x, slotSize * 0.5f);

        // Border/back
        GameObject borderGO = new GameObject("Border", typeof(RectTransform));
        RectTransform borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.SetParent(slotRT, false);
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = Vector2.zero;
        borderRT.offsetMax = Vector2.zero;
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.sprite = _uiSprite;
        borderImg.type = Image.Type.Sliced;
        borderImg.color = slotBorderColor;

        // Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.SetParent(slotRT, false);
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = _uiSprite; // placeholder default
        iconImg.color = missingIconColor;
        iconImg.type = Image.Type.Sliced;

        // Key label (bottom-left)
        GameObject keyGO = new GameObject("Key", typeof(RectTransform));
        RectTransform keyRT = keyGO.GetComponent<RectTransform>();
        keyRT.SetParent(slotRT, false);
        keyRT.anchorMin = new Vector2(0f, 0f);
        keyRT.anchorMax = new Vector2(0f, 0f);
        keyRT.pivot = new Vector2(0f, 0f);
        keyRT.anchoredPosition = new Vector2(4f, 4f);
        keyRT.sizeDelta = new Vector2(slotSize * 0.5f, slotSize * 0.25f);
        Text keyText = keyGO.AddComponent<Text>();
        keyText.font = _defaultFont;
        keyText.fontSize = Mathf.RoundToInt(slotSize * 0.25f);
        keyText.alignment = TextAnchor.LowerLeft;
        keyText.color = keyLabelColor;
        keyText.text = label;

        // Cooldown mask (radial fill over icon)
        GameObject maskGO = new GameObject("CooldownMask", typeof(RectTransform));
        RectTransform maskRT = maskGO.GetComponent<RectTransform>();
        maskRT.SetParent(iconRT, false);
        maskRT.anchorMin = Vector2.zero;
        maskRT.anchorMax = Vector2.one;
        maskRT.offsetMin = Vector2.zero;
        maskRT.offsetMax = Vector2.zero;
        Image maskImg = maskGO.AddComponent<Image>();
        maskImg.sprite = _uiSprite;
        maskImg.type = Image.Type.Filled;
        maskImg.fillMethod = Image.FillMethod.Radial360;
        maskImg.fillClockwise = false;
        maskImg.fillOrigin = (int)Image.Origin360.Top;
        maskImg.color = cooldownMaskColor;
        maskImg.fillAmount = 0f; // hidden until on cooldown

        // Timer text (centered)
        GameObject timerGO = new GameObject("Timer", typeof(RectTransform));
        RectTransform timerRT = timerGO.GetComponent<RectTransform>();
        timerRT.SetParent(slotRT, false);
        timerRT.anchorMin = Vector2.zero;
        timerRT.anchorMax = Vector2.one;
        timerRT.offsetMin = Vector2.zero;
        timerRT.offsetMax = Vector2.zero;
        Text timerText = timerGO.AddComponent<Text>();
        timerText.font = _defaultFont;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.fontSize = Mathf.RoundToInt(slotSize * 0.4f);
        timerText.color = timerTextColor;
        timerText.text = string.Empty;

        _slots[key] = new Slot
        {
            root = slotRT,
            border = borderImg,
            icon = iconImg,
            keyLabel = keyText,
            cooldownMask = maskImg,
            timerLabel = timerText,
            hasIcon = false,
            cooldownEnd = 0f,
            cooldownDuration = 0f
        };

        // Missing image text overlay if no icon assigned yet
        SetAbilityIcon(key, null);
        // Default present state: true for LMB/RMB/1/2, false for optional CTRL (Dash)
        bool defaultPresent = (key == AbilityKey.LeftClick || key == AbilityKey.RightClick || key == AbilityKey.One || key == AbilityKey.Two);
        if (key == AbilityKey.Ctrl)
        {
            SetSlotPresent(key, false);
        }
        else
        {
            SetSlotPresent(key, defaultPresent);
        }
    }

    private void Update()
    {
        float now = Time.unscaledTime; // UI should respect unscaled time
        foreach (var kv in _slots)
        {
            var s = kv.Value;
            if (s.cooldownEnd > now && s.cooldownDuration > 0f)
            {
                float remaining = Mathf.Max(0f, s.cooldownEnd - now);
                float pct = Mathf.Clamp01(remaining / s.cooldownDuration);
                s.cooldownMask.fillAmount = pct;
                s.timerLabel.text = remaining >= 1f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("0.0");
            }
            else
            {
                s.cooldownMask.fillAmount = 0f;
                s.timerLabel.text = string.Empty;
            }
        }
    }

    // Public API
    public void SetAbilityIcon(AbilityKey key, Sprite icon)
    {
        if (!_slots.TryGetValue(key, out var s)) return;

        if (icon == null)
        {
            s.icon.sprite = _uiSprite;
            s.icon.color = missingIconColor;
            // Add/ensure a small overlay text that says NO IMG
            EnsureMissingText(s);
            s.hasIcon = false;
        }
        else
        {
            s.icon.sprite = icon;
            s.icon.color = Color.white;
            RemoveMissingText(s);
            s.hasIcon = true;
        }
    }

    public void SetSlotPresent(AbilityKey key, bool present)
    {
        if (!_slots.TryGetValue(key, out var s)) return;
        s.root.gameObject.SetActive(present);
    }

    public void StartCooldown(AbilityKey key, float durationSeconds)
    {
        if (!_slots.TryGetValue(key, out var s)) return;
        // If this is CTRL (Dash), ensure the slot is visible when starting a cooldown
        if (key == AbilityKey.Ctrl)
        {
            SetSlotPresent(AbilityKey.Ctrl, true);
        }
        durationSeconds = Mathf.Max(0.01f, durationSeconds);
        s.cooldownDuration = durationSeconds;
        s.cooldownEnd = Time.unscaledTime + durationSeconds;
        s.cooldownMask.fillAmount = 1f; // start full
    }

    public void StartCooldown(string keyName, float durationSeconds)
    {
        // Accept synonyms: "Dash" maps to CTRL
        if (string.Equals(keyName, "dash", StringComparison.OrdinalIgnoreCase))
        {
            StartCooldown(AbilityKey.Ctrl, durationSeconds);
            return;
        }
        // Accept common key labels for Q/E mapping to internal One/Two
        if (string.Equals(keyName, "q", StringComparison.OrdinalIgnoreCase))
        {
            StartCooldown(AbilityKey.One, durationSeconds);
            return;
        }
        if (string.Equals(keyName, "e", StringComparison.OrdinalIgnoreCase))
        {
            StartCooldown(AbilityKey.Two, durationSeconds);
            return;
        }
        if (string.Equals(keyName, "1", StringComparison.OrdinalIgnoreCase))
        {
            StartCooldown(AbilityKey.One, durationSeconds);
            return;
        }
        if (string.Equals(keyName, "2", StringComparison.OrdinalIgnoreCase))
        {
            StartCooldown(AbilityKey.Two, durationSeconds);
            return;
        }
        if (Enum.TryParse<AbilityKey>(keyName, true, out var k))
        {
            StartCooldown(k, durationSeconds);
        }
    }

    public bool IsOnCooldown(AbilityKey key)
    {
        if (!_slots.TryGetValue(key, out var s)) return false;
        return s.cooldownEnd > Time.unscaledTime;
    }

    public void SetAbilityAvailable(AbilityKey key, bool available)
    {
        if (!_slots.TryGetValue(key, out var s)) return;
        float a = available ? 1f : 0.4f;
        s.icon.canvasRenderer.SetAlpha(a);
        s.border.canvasRenderer.SetAlpha(a);
        s.keyLabel.canvasRenderer.SetAlpha(a);
    }
    
    /// <summary>
    /// Force the HUD to be visible. Useful when spawning in gameplay after hiding for selection.
    /// </summary>
    public void ForceShowHUD()
    {
        if (_rootRT != null)
        {
            _rootRT.gameObject.SetActive(true);
            Debug.Log("[CooldownUIManager] HUD force-shown");
        }
    }
    
    /// <summary>
    /// Hide the HUD.
    /// </summary>
    public void HideHUD()
    {
        if (_rootRT != null)
        {
            _rootRT.gameObject.SetActive(false);
            Debug.Log("[CooldownUIManager] HUD hidden");
        }
    }

    private void EnsureMissingText(Slot s)
    {
        var t = s.root.Find("MissingText") as RectTransform;
        if (t == null)
        {
            GameObject mtGO = new GameObject("MissingText", typeof(RectTransform));
            RectTransform mtRT = mtGO.GetComponent<RectTransform>();
            mtRT.SetParent(s.root, false);
            mtRT.anchorMin = Vector2.zero;
            mtRT.anchorMax = Vector2.one;
            mtRT.offsetMin = Vector2.zero;
            mtRT.offsetMax = Vector2.zero;
            Text txt = mtGO.AddComponent<Text>();
            txt.font = _defaultFont;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = Mathf.RoundToInt(slotSize * 0.22f);
            txt.color = new Color(0.9f, 0.9f, 0.9f, 0.85f);
            txt.text = missingIconText;
        }
    }

    private void RemoveMissingText(Slot s)
    {
        var t = s.root.Find("MissingText");
        if (t != null) Destroy(t.gameObject);
    }

    // Loadout application for per-character compatibility
    public void ApplyLoadout(List<AbilityEntry> abilities, AbilityEntry dash)
    {
        // Reset to defaults: show main 4, hide optional
        SetSlotPresent(AbilityKey.LeftClick, true);
        SetSlotPresent(AbilityKey.RightClick, true);
        SetSlotPresent(AbilityKey.One, true);
        SetSlotPresent(AbilityKey.Two, true);
        SetSlotPresent(AbilityKey.Ctrl, false); // CTRL is Dash and is optional by default

        // Clear icons to placeholder initially
        foreach (var key in new[] { AbilityKey.LeftClick, AbilityKey.RightClick, AbilityKey.One, AbilityKey.Two, AbilityKey.Ctrl })
        {
            SetAbilityIcon(key, null);
        }

        if (abilities != null)
        {
            foreach (var a in abilities)
            {
                SetSlotPresent(a.key, a.present);
                if (a.present)
                {
                    SetAbilityIcon(a.key, a.icon);
                }
            }
        }

        if (dash != null)
        {
            // Apply dash to CTRL slot (CTRL is Dash)
            SetSlotPresent(AbilityKey.Ctrl, dash.present);
            if (dash.present)
                SetAbilityIcon(AbilityKey.Ctrl, dash.icon);
        }
    }
}
