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

// Ability bar HUD: 5 slots (LMB, RMB, Q, E, CTRL) with icon, key badge, radial cooldown and timer.
//
// There is exactly one global HUD manager (CooldownUIManager.Instance), living on its own root object.
// A CooldownUIManager placed on a champion prefab is a *loadout carrier*: it only stores that champion's
// icons (defaultAbilities / defaultDash) and forwards any API call to the global HUD. The local player's
// loadout is applied on spawn via CooldownUIManager.ApplyLoadoutFrom(character).
//
// Use: CooldownUIManager.Instance.StartCooldown(AbilityKey.One, durationSeconds);
[DisallowMultipleComponent]
public class CooldownUIManager : MonoBehaviour
{
    public static CooldownUIManager Instance { get; private set; }

    [Header("Layout")]
    public Vector2 anchorPos = new Vector2(0.5f, 0f); // bottom-center
    public Vector2 anchoredOffset = new Vector2(0f, 24f);
    public float slotSize = 88f;
    public float spacing = 6f;

    // Legacy appearance fields: kept so existing prefab data still deserializes. The HUD look is defined below.
    [Header("Appearance (legacy, unused)")]
    public Color slotBorderColor = new Color(1f, 1f, 1f, 0.9f);
    public Color missingIconColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public string missingIconText = "NO IMG";
    public Color cooldownMaskColor = new Color(0f, 0f, 0f, 0.6f);
    public Color keyLabelColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
    public Color timerTextColor = new Color(1f, 1f, 1f, 0.95f);

    [Header("Default Loadout (champion prefabs)")]
    [Tooltip("Icons/presence for this champion. Applied to the global HUD when the local player spawns as this champion.")]
    public List<AbilityEntry> defaultAbilities = new List<AbilityEntry>();
    public AbilityEntry defaultDash; // Applied to CTRL (CTRL is Dash)

    [Header("Advanced")]
    [Tooltip("If true, a manager on a non-root, non-character object still becomes the global HUD. Ignored on champions (always a loadout carrier).")]
    public bool forceStayOnThisObject = false;

    [Serializable]
    public class AbilityEntry
    {
        public AbilityKey key;
        public Sprite icon;
        public bool present = true; // set false if character doesn't have this ability
    }

    // ---- Look ----
    static readonly Color FrameReady    = UIStyle.Gold;
    static readonly Color FrameCooldown = UIStyle.GoldDim;
    static readonly Color SlotBack      = new Color(0.04f, 0.07f, 0.09f, 0.92f);
    static readonly Color PanelBack     = new Color(0.02f, 0.04f, 0.06f, 0.78f);
    static readonly Color PanelEdge     = new Color(0.78f, 0.67f, 0.43f, 0.55f);
    static readonly Color SweepColor    = new Color(0f, 0.02f, 0.05f, 0.72f);
    static readonly Color BadgeBack     = new Color(0.03f, 0.05f, 0.07f, 0.96f);
    static readonly Color BadgeText     = new Color(0.94f, 0.90f, 0.80f, 1f);
    static readonly Color EmptyIcon     = new Color(0.16f, 0.20f, 0.24f, 1f);
    static readonly Color FlashColor    = new Color(1f, 0.95f, 0.75f, 1f);
    const float FlashTime = 0.35f;

    static readonly AbilityKey[] Order = { AbilityKey.LeftClick, AbilityKey.RightClick, AbilityKey.One, AbilityKey.Two, AbilityKey.Ctrl };

    private bool _isCarrier;
    private RectTransform _rootRT;
    private RectTransform _barRT;
    private Image _panel;

    private class Slot
    {
        public AbilityKey key;
        public string label;
        public RectTransform root;
        public Image frame;
        public Image icon;
        public Image sweep;
        public Image flash;
        public Text timer;
        public Text emptyLabel;
        public Image badge;
        public Image badgeEdge;
        public Text badgeText;
        public bool present;
        public float cooldownEnd;
        public float cooldownDuration;
        public bool wasCooling;
        public float flashUntil;
    }

    private readonly Dictionary<AbilityKey, Slot> _slots = new Dictionary<AbilityKey, Slot>();
    private static Sprite _roundSprite;
    private static Sprite _whiteSprite;
    private static Font _font;

    // ------------------------------------------------------------------ lifecycle

    private void Awake()
    {
        bool attachedToCharacter = GetComponent<CharacterControl>() != null || GetComponentInParent<CharacterControl>() != null;
        bool isRoot = transform.parent == null;

        // A HUD on a champion would die with that champion (and remote champions would hijack it), so a
        // manager on a character is always a loadout carrier. forceStayOnThisObject only applies to
        // non-character child objects.
        if (attachedToCharacter || (!isRoot && !forceStayOnThisObject))
        {
            // Loadout carrier: never builds UI, forwards API calls to the global HUD.
            _isCarrier = true;
            EnsureInstance();
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureResources();
        BuildUI();

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateVisibilityForScene();

        if (defaultAbilities != null && defaultAbilities.Count > 0)
            ApplyLoadout(defaultAbilities, defaultDash);
    }

    private void OnDestroy()
    {
        if (_isCarrier) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    /// <summary>Returns the global HUD manager, creating it if needed.</summary>
    public static CooldownUIManager EnsureInstance()
    {
        if (Instance == null)
        {
            var host = new GameObject("CooldownUIManager");
            host.AddComponent<CooldownUIManager>(); // root object -> becomes Instance in Awake
        }
        return Instance;
    }

    /// <summary>
    /// Applies the ability loadout of a spawned champion to the HUD and clears all running timers.
    /// Prefers an AbilityIconLoadout component; falls back to the champion's CooldownUIManager carrier.
    /// </summary>
    public static void ApplyLoadoutFrom(GameObject character)
    {
        if (character == null) return;
        var hud = EnsureInstance();
        if (hud == null) return;

        var iconLoadout = character.GetComponentInChildren<AbilityIconLoadout>(true);
        if (iconLoadout != null)
        {
            iconLoadout.ApplyToHUD();
            return;
        }

        foreach (var carrier in character.GetComponentsInChildren<CooldownUIManager>(true))
        {
            if (carrier == hud) continue;
            hud.ApplyLoadout(carrier.defaultAbilities, carrier.defaultDash);
            return;
        }

        hud.ResetAllCooldowns();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => UpdateVisibilityForScene();

    private void UpdateVisibilityForScene()
    {
        GameObject selectionCanvas = GameObject.Find("SelectionCanvas");
        bool inSelection = (selectionCanvas != null && selectionCanvas.activeInHierarchy)
                           || SceneManager.GetActiveScene().name.ToLower().Contains("select");
        if (_rootRT != null) _rootRT.gameObject.SetActive(!inSelection);

        if (inSelection)
        {
            try { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; } catch { }
        }
    }

    // ------------------------------------------------------------------ resources

    private static void EnsureResources()
    {
        _whiteSprite = UIStyle.White;
        _roundSprite = UIStyle.Rounded;

        if (_font == null)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                try { _font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans" }, 14); } catch { }
            }
        }
    }

    // ------------------------------------------------------------------ build

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static Image AddImage(RectTransform rt, Sprite sprite, Color color, bool sliced = true)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Text AddText(RectTransform rt, int size, Color color, FontStyle style, TextAnchor anchor)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = _font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    private void BuildUI()
    {
        _rootRT = NewRect("HUD_Cooldowns", transform);
        Stretch(_rootRT);
        var canvas = _rootRT.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = _rootRT.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        // Backing panel (resized to fit the visible slots in LayoutBar)
        _barRT = NewRect("AbilityBar", _rootRT);
        _barRT.anchorMin = _barRT.anchorMax = anchorPos;
        _barRT.pivot = new Vector2(0.5f, 0f);
        _barRT.anchoredPosition = anchoredOffset;

        var edge = NewRect("PanelEdge", _barRT);
        Stretch(edge, -2f);
        AddImage(edge, _roundSprite, PanelEdge);
        var panelRT = NewRect("Panel", _barRT);
        Stretch(panelRT);
        _panel = AddImage(panelRT, _roundSprite, PanelBack);

        foreach (var key in Order) CreateSlot(key);
        LayoutBar();
    }

    private static string LabelFor(AbilityKey key) => UIStyle.KeyLabel(key);

    private float SizeFor(AbilityKey key) => key == AbilityKey.Ctrl ? Mathf.Round(Slot0 * 0.8f) : Slot0;
    private float Slot0 => Mathf.Max(slotSize, 88f);

    private void CreateSlot(AbilityKey key)
    {
        float size = SizeFor(key);
        var s = new Slot { key = key, label = LabelFor(key) };

        s.root = NewRect($"Slot_{key}", _barRT);
        s.root.anchorMin = s.root.anchorMax = new Vector2(0f, 0f);
        s.root.pivot = new Vector2(0.5f, 0f);
        s.root.sizeDelta = new Vector2(size, size);

        var frameRT = NewRect("Frame", s.root);
        Stretch(frameRT);
        s.frame = AddImage(frameRT, _roundSprite, FrameReady);

        var backRT = NewRect("Back", s.root);
        Stretch(backRT, 3f);
        AddImage(backRT, _roundSprite, SlotBack);

        var iconRT = NewRect("Icon", s.root);
        Stretch(iconRT, 5f);
        s.icon = AddImage(iconRT, _whiteSprite, EmptyIcon, sliced: false);
        s.icon.preserveAspect = true;

        var emptyRT = NewRect("EmptyLabel", s.root);
        Stretch(emptyRT);
        s.emptyLabel = AddText(emptyRT, Mathf.RoundToInt(size * 0.32f), new Color(1f, 1f, 1f, 0.35f), FontStyle.Bold, TextAnchor.MiddleCenter);
        s.emptyLabel.text = s.label;

        var sweepRT = NewRect("Sweep", s.root);
        Stretch(sweepRT, 5f);
        s.sweep = AddImage(sweepRT, _whiteSprite, SweepColor, sliced: false);
        s.sweep.type = Image.Type.Filled;
        s.sweep.fillMethod = Image.FillMethod.Radial360;
        s.sweep.fillOrigin = (int)Image.Origin360.Top;
        s.sweep.fillClockwise = false;
        s.sweep.fillAmount = 0f;

        var flashRT = NewRect("Flash", s.root);
        Stretch(flashRT);
        s.flash = AddImage(flashRT, _roundSprite, new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f));

        var timerRT = NewRect("Timer", s.root);
        Stretch(timerRT);
        s.timer = AddText(timerRT, Mathf.RoundToInt(size * 0.42f), Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
        s.timer.text = string.Empty;

        // Key badge, centered on the bottom edge (League-style)
        float badgeH = Mathf.Round(size * 0.30f);
        float badgeW = Mathf.Max(badgeH * 1.3f, s.label.Length * badgeH * 0.55f + 10f);
        var badgeEdge = NewRect("KeyBadgeEdge", s.root);
        badgeEdge.anchorMin = badgeEdge.anchorMax = new Vector2(0.5f, 0f);
        badgeEdge.pivot = new Vector2(0.5f, 0.5f);
        badgeEdge.sizeDelta = new Vector2(badgeW + 3f, badgeH + 3f);
        badgeEdge.anchoredPosition = new Vector2(0f, 1f);
        var edgeImg = AddImage(badgeEdge, _roundSprite, FrameReady);
        var badgeRT = NewRect("KeyBadge", badgeEdge);
        Stretch(badgeRT, 1.5f);
        s.badge = AddImage(badgeRT, _roundSprite, BadgeBack);
        var badgeTextRT = NewRect("Key", badgeRT);
        Stretch(badgeTextRT);
        s.badgeText = AddText(badgeTextRT, Mathf.RoundToInt(badgeH * 0.62f), BadgeText, FontStyle.Bold, TextAnchor.MiddleCenter);
        s.badgeText.text = s.label;
        s.badgeEdge = edgeImg; // follows the frame colour

        _slots[key] = s;
        SetAbilityIcon(key, null);
        SetSlotPresent(key, key != AbilityKey.Ctrl);
    }

    private void LayoutBar()
    {
        if (_barRT == null) return;
        float gap = Mathf.Max(spacing, 12f);
        const float padX = 16f, padTop = 12f, padBottom = 22f; // bottom room for key badges

        float width = 0f, height = 0f;
        int visible = 0;
        foreach (var key in Order)
        {
            if (!_slots.TryGetValue(key, out var s) || !s.present) continue;
            float size = SizeFor(key);
            width += (visible > 0 ? gap : 0f) + size;
            height = Mathf.Max(height, size);
            visible++;
        }

        _barRT.gameObject.SetActive(visible > 0);
        _barRT.sizeDelta = new Vector2(width + padX * 2f, height + padTop + padBottom);

        float x = padX;
        foreach (var key in Order)
        {
            if (!_slots.TryGetValue(key, out var s) || !s.present) continue;
            float size = SizeFor(key);
            s.root.anchoredPosition = new Vector2(x + size * 0.5f, padBottom + (height - size)); // align tops
            x += size + gap;
        }
    }

    // ------------------------------------------------------------------ update

    private void Update()
    {
        if (_isCarrier) return;
        float now = Time.unscaledTime;
        foreach (var s in _slots.Values)
        {
            bool cooling = s.cooldownEnd > now && s.cooldownDuration > 0f;
            if (cooling)
            {
                float remaining = s.cooldownEnd - now;
                s.sweep.fillAmount = Mathf.Clamp01(remaining / s.cooldownDuration);
                s.timer.text = remaining >= 1f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("0.0");
            }
            else
            {
                s.sweep.fillAmount = 0f;
                s.timer.text = string.Empty;
                if (s.wasCooling && s.cooldownDuration >= 0.75f) s.flashUntil = now + FlashTime; // "ready" ping
            }
            s.wasCooling = cooling;

            Color frame = cooling ? FrameCooldown : FrameReady;
            s.frame.color = frame;
            s.badgeEdge.color = frame;
            s.icon.color = s.icon.sprite == _whiteSprite ? EmptyIcon : (cooling ? new Color(0.55f, 0.55f, 0.6f, 1f) : Color.white);

            float f = s.flashUntil > now ? (s.flashUntil - now) / FlashTime : 0f;
            s.flash.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, f * 0.65f);
            s.root.localScale = Vector3.one * (1f + 0.08f * f);
        }
    }

    // ------------------------------------------------------------------ public API

    public void SetAbilityIcon(AbilityKey key, Sprite icon)
    {
        if (_isCarrier) { if (Instance != null) Instance.SetAbilityIcon(key, icon); return; }
        if (!_slots.TryGetValue(key, out var s)) return;
        s.icon.sprite = icon != null ? icon : _whiteSprite;
        s.icon.color = icon != null ? Color.white : EmptyIcon;
        s.emptyLabel.gameObject.SetActive(icon == null);
    }

    public void SetSlotPresent(AbilityKey key, bool present)
    {
        if (_isCarrier) { if (Instance != null) Instance.SetSlotPresent(key, present); return; }
        if (!_slots.TryGetValue(key, out var s)) return;
        if (s.present == present && s.root.gameObject.activeSelf == present) return;
        s.present = present;
        s.root.gameObject.SetActive(present);
        LayoutBar();
    }

    public void StartCooldown(AbilityKey key, float durationSeconds)
    {
        if (_isCarrier) { if (Instance != null) Instance.StartCooldown(key, durationSeconds); return; }
        if (!_slots.TryGetValue(key, out var s)) return;
        if (key == AbilityKey.Ctrl) SetSlotPresent(AbilityKey.Ctrl, true); // dash slot appears on first use
        durationSeconds = Mathf.Max(0.01f, durationSeconds);
        s.cooldownDuration = durationSeconds;
        s.cooldownEnd = Time.unscaledTime + durationSeconds;
        s.sweep.fillAmount = 1f;
        s.flashUntil = 0f;
    }

    public void StartCooldown(string keyName, float durationSeconds)
    {
        if (string.Equals(keyName, "dash", StringComparison.OrdinalIgnoreCase)) { StartCooldown(AbilityKey.Ctrl, durationSeconds); return; }
        if (string.Equals(keyName, "q", StringComparison.OrdinalIgnoreCase) || keyName == "1") { StartCooldown(AbilityKey.One, durationSeconds); return; }
        if (string.Equals(keyName, "e", StringComparison.OrdinalIgnoreCase) || keyName == "2") { StartCooldown(AbilityKey.Two, durationSeconds); return; }
        if (Enum.TryParse<AbilityKey>(keyName, true, out var k)) StartCooldown(k, durationSeconds);
    }

    public bool IsOnCooldown(AbilityKey key)
    {
        if (_isCarrier) return Instance != null && Instance.IsOnCooldown(key);
        return _slots.TryGetValue(key, out var s) && s.cooldownEnd > Time.unscaledTime;
    }

    /// <summary>Clears every running cooldown (e.g. after respawning or switching champion).</summary>
    public void ResetAllCooldowns()
    {
        if (_isCarrier) { if (Instance != null) Instance.ResetAllCooldowns(); return; }
        foreach (var s in _slots.Values)
        {
            s.cooldownEnd = 0f;
            s.cooldownDuration = 0f;
            s.wasCooling = false;
            s.flashUntil = 0f;
        }
    }

    public void SetAbilityAvailable(AbilityKey key, bool available)
    {
        if (_isCarrier) { if (Instance != null) Instance.SetAbilityAvailable(key, available); return; }
        if (!_slots.TryGetValue(key, out var s)) return;
        float a = available ? 1f : 0.4f;
        s.icon.canvasRenderer.SetAlpha(a);
        s.frame.canvasRenderer.SetAlpha(a);
        s.badgeText.canvasRenderer.SetAlpha(a);
    }

    /// <summary>Force the HUD to be visible. Useful when spawning in gameplay after hiding for selection.</summary>
    public void ForceShowHUD()
    {
        if (_isCarrier) { if (Instance != null) Instance.ForceShowHUD(); return; }
        if (_rootRT != null) _rootRT.gameObject.SetActive(true);
    }

    /// <summary>Hide the HUD.</summary>
    public void HideHUD()
    {
        if (_isCarrier) { if (Instance != null) Instance.HideHUD(); return; }
        if (_rootRT != null) _rootRT.gameObject.SetActive(false);
    }

    /// <summary>Applies a champion loadout: slot presence + icons, and clears all running cooldowns.</summary>
    public void ApplyLoadout(List<AbilityEntry> abilities, AbilityEntry dash)
    {
        if (_isCarrier) { if (Instance != null) Instance.ApplyLoadout(abilities, dash); return; }

        ResetAllCooldowns();
        foreach (var key in Order)
        {
            SetAbilityIcon(key, null);
            SetSlotPresent(key, key != AbilityKey.Ctrl);
        }

        if (abilities != null)
        {
            foreach (var a in abilities)
            {
                if (a == null) continue;
                SetSlotPresent(a.key, a.present);
                if (a.present) SetAbilityIcon(a.key, a.icon);
            }
        }

        if (dash != null)
        {
            SetSlotPresent(AbilityKey.Ctrl, dash.present);
            if (dash.present) SetAbilityIcon(AbilityKey.Ctrl, dash.icon);
        }
    }
}
