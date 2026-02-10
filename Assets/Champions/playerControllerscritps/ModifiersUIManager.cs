using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Lightweight status/modifier HUD: shows a horizontal row of icons with optional label, stack, and timer.
// API:
//   ModifiersUIManager.Instance.AddOrUpdate(id, icon, label, durationSeconds, stacks)
//   ModifiersUIManager.Instance.Remove(id)
//   ModifiersUIManager.Instance.Clear()
[DisallowMultipleComponent]
public class ModifiersUIManager : MonoBehaviour
{
    public static ModifiersUIManager Instance { get; private set; }

    [Header("Layout")]
    public Vector2 anchor = new Vector2(0f, 1f); // top-left
    public Vector2 anchoredOffset = new Vector2(12f, -12f);
    public float slotSize = 150f; // enlarged default
    public float spacing = 6f;

    [Header("Appearance")]
    public bool showBorder = false; // allow borderless icons
    public Color borderColor = new Color(1f, 1f, 1f, 0.85f);
    public Color labelColor = new Color(1f, 1f, 1f, 0.95f);
    public Color timerColor = new Color(1f, 1f, 1f, 0.9f);
    [Tooltip("Unified text size for label, stacks and timer.")]
    public int textSize = 40;

    [Header("Icon Border (Inset)")]
    [Range(0f, 0.45f)]
    public float iconInset = 0.05f; // 5% inset → thinner border look

    private RectTransform _rootRT;
    private RectTransform _barRT;
    private static Sprite _uiSprite;
    private static Font _font;

    private class Slot
    {
        public string id;
        public RectTransform root;
        public Image border;
        public Image icon;
        public Text label;
        public Text stackLabel;
        public Text timer;
        public float endTime; // < 0 = infinite
        public int stacks;
    }

    private readonly List<Slot> _order = new List<Slot>();
    private readonly Dictionary<string, Slot> _byId = new Dictionary<string, Slot>();

    void Awake()
    {
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
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateVisibilityForScene();
    }

    private void UpdateVisibilityForScene()
    {
        bool inSelection = FindObjectOfType<CharacterSelection>(true) != null
                           || SceneManager.GetActiveScene().name.ToLower().Contains("select");
        if (_rootRT != null)
            _rootRT.gameObject.SetActive(!inSelection);
    }

    private void EnsureResources()
    {
        if (_uiSprite == null)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _uiSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        }
        if (_font == null)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("HUD_Modifiers", typeof(RectTransform));
        _rootRT = canvasGO.GetComponent<RectTransform>();
        _rootRT.SetParent(this.transform, false);
        _rootRT.anchorMin = new Vector2(0f, 0f);
        _rootRT.anchorMax = new Vector2(1f, 1f);
        _rootRT.offsetMin = Vector2.zero;
        _rootRT.offsetMax = Vector2.zero;
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // match height so the top bar stays proportionate
        canvasGO.AddComponent<GraphicRaycaster>();

        var barGO = new GameObject("Bar", typeof(RectTransform));
        _barRT = barGO.GetComponent<RectTransform>();
        _barRT.SetParent(_rootRT, false);
        _barRT.pivot = new Vector2(0f, 1f);
        _barRT.anchorMin = anchor;
        _barRT.anchorMax = anchor;
        _barRT.anchoredPosition = anchoredOffset;
        _barRT.sizeDelta = new Vector2(800f, slotSize);
    }

    private Slot CreateSlot(string id)
    {
        var slotGO = new GameObject(id, typeof(RectTransform));
        var rt = slotGO.GetComponent<RectTransform>();
        rt.SetParent(_barRT, false);
        rt.sizeDelta = new Vector2(slotSize, slotSize);

        var borderGO = new GameObject("Border", typeof(RectTransform));
        var brt = borderGO.GetComponent<RectTransform>();
        brt.SetParent(rt, false);
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        var border = borderGO.AddComponent<Image>();
        border.sprite = _uiSprite; border.type = Image.Type.Sliced; border.color = borderColor; border.enabled = showBorder;

        var iconGO = new GameObject("Icon", typeof(RectTransform));
        var irt = iconGO.GetComponent<RectTransform>();
        irt.SetParent(rt, false);
        float inset = Mathf.Clamp01(iconInset);
        irt.anchorMin = new Vector2(inset, inset); irt.anchorMax = new Vector2(1f - inset, 1f - inset); irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;
        var icon = iconGO.AddComponent<Image>();
        icon.sprite = _uiSprite; icon.color = Color.white;

        var lblGO = new GameObject("Label", typeof(RectTransform));
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.SetParent(rt, false);
        // Place label INSIDE the slot at the bottom so it never gets culled off-screen
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(1f, 0f);
        lrt.pivot = new Vector2(0.5f, 0f);
        int labelHeight = Mathf.Max(12, textSize + 2);
        lrt.anchoredPosition = new Vector2(0f, 0f);
        // stretch horizontally: sizeDelta.x = 0 so it matches parent width
        lrt.sizeDelta = new Vector2(0f, labelHeight);
        var label = lblGO.AddComponent<Text>();
        label.font = _font;
        label.color = labelColor;
        label.alignment = TextAnchor.LowerCenter; // anchor text to the bottom of the slot
        label.fontSize = Mathf.Max(8, textSize);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.text = string.Empty;
        label.raycastTarget = false;
        // Ensure label renders above other children (like the timer)
        lblGO.transform.SetAsLastSibling();

        var stkGO = new GameObject("Stacks", typeof(RectTransform));
        var srt = stkGO.GetComponent<RectTransform>();
        srt.SetParent(rt, false);
        srt.anchorMin = new Vector2(1f, 0f); srt.anchorMax = new Vector2(1f, 0f); srt.pivot = new Vector2(1f, 0f);
        srt.anchoredPosition = new Vector2(-2f, 2f); srt.sizeDelta = new Vector2(slotSize * 0.5f, slotSize * 0.5f);
        var stacks = stkGO.AddComponent<Text>(); stacks.font = _font; stacks.color = labelColor; stacks.alignment = TextAnchor.LowerRight; stacks.fontSize = Mathf.Max(8, textSize); stacks.text = string.Empty;

        var timGO = new GameObject("Timer", typeof(RectTransform));
        var trt = timGO.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        // Position timer at the top-right corner to avoid overlapping the label
        trt.anchorMin = new Vector2(1f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(1f, 1f);
        int timerH = Mathf.Max(12, textSize);
        trt.sizeDelta = new Vector2(Mathf.Max(24, slotSize * 0.5f), timerH);
        trt.anchoredPosition = new Vector2(-2f, -2f);
        var timer = timGO.AddComponent<Text>(); timer.font = _font; timer.color = timerColor; timer.alignment = TextAnchor.UpperRight; timer.fontSize = Mathf.Max(8, textSize); timer.text = string.Empty; timer.raycastTarget = false;

        // Ensure label is on top of all elements
        lblGO.transform.SetAsLastSibling();

        return new Slot { id = id, root = rt, border = border, icon = icon, label = label, stackLabel = stacks, timer = timer, endTime = -1f, stacks = 0 };
    }

    private void Layout()
    {
        for (int i = 0; i < _order.Count; i++)
        {
            var s = _order[i];
            s.root.anchoredPosition = new Vector2(i * (slotSize + spacing), 0f);
        }
    }

    public void AddOrUpdate(string id, Sprite icon, string label = "", float durationSeconds = -1f, int stacks = 0)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (icon == null && ModifiersIconLibrary.Instance != null)
        {
            icon = ModifiersIconLibrary.Instance.Resolve(id, label);
        }
        if (!_byId.TryGetValue(id, out var s))
        {
            s = CreateSlot(id);
            _order.Add(s);
            _byId[id] = s;
            Layout();
        }
        if (icon == null)
        {
            var lib = ModifiersIconLibrary.Instance != null ? ModifiersIconLibrary.Instance : GameObject.FindObjectOfType<ModifiersIconLibrary>(true);
            if (lib != null)
            {
                icon = lib.Resolve(id, label);
                // Fallback: if Resolve returned null but the library has assigned sprites, use the first available one
                if (icon == null && Application.isPlaying)
                {
                    icon = lib.DMGRD ?? lib.SHIELD ?? lib.STUN ?? lib.SLOWNESS ?? lib.HASTE;
                    if (icon != null)
                    {
                        Debug.LogWarning($"[ModifiersUI] No exact match for '{id}' (label: '{label}'). Using fallback sprite. Assign the correct sprite in ModifiersIconLibrary.");
                    }
                }
            }
        }

        if (icon != null)
        {
            s.icon.sprite = icon;
            s.icon.color = Color.white;
            s.icon.preserveAspect = true;
            s.icon.enabled = true;
            RemoveMissingText(s);
        }
        else
        {
            // Show a neutral placeholder so it's obvious an icon is missing
            s.icon.sprite = _uiSprite;
            s.icon.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            s.icon.preserveAspect = true;
            s.icon.enabled = true;
            EnsureMissingText(s);
            if (Application.isPlaying)
            {
                Debug.LogWarning($"[ModifiersUI] Missing icon for '{id}'. Assign sprites in ModifiersIconLibrary inspector (check SLOWNESS, SHIELD, STUN, HASTE, etc.)");
            }
        }
        s.label.text = label ?? string.Empty;
        s.stacks = Mathf.Max(0, stacks);
        s.stackLabel.text = s.stacks > 1 ? s.stacks.ToString() : string.Empty;
        s.endTime = durationSeconds >= 0f ? (Time.unscaledTime + durationSeconds) : -1f;
        s.timer.text = string.Empty;
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
            var txt = mtGO.AddComponent<Text>();
            txt.font = _font;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = Mathf.Max(10, textSize - 4);
            txt.color = new Color(0.9f, 0.9f, 0.9f, 0.85f);
            txt.text = "NO IMG";
            txt.raycastTarget = false;
            mtGO.transform.SetAsLastSibling();
        }
    }

    private void RemoveMissingText(Slot s)
    {
        var t = s.root.Find("MissingText");
        if (t != null) Destroy(t.gameObject);
    }

    public void Remove(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (_byId.TryGetValue(id, out var s))
        {
            _order.Remove(s);
            _byId.Remove(id);
            if (s.root != null) Destroy(s.root.gameObject);
            Layout();
        }
    }

    public void Clear()
    {
        foreach (var s in _order)
            if (s.root != null) Destroy(s.root.gameObject);
        _order.Clear();
        _byId.Clear();
    }

    void Update()
    {
        float now = Time.unscaledTime;
        // Update timers and auto-remove expired
        for (int i = _order.Count - 1; i >= 0; i--)
        {
            var s = _order[i];
            if (s.endTime >= 0f)
            {
                float remaining = s.endTime - now;
                if (remaining <= 0f)
                {
                    Remove(s.id);
                    continue;
                }
                s.timer.text = remaining >= 1f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("0.0");
            }
            else
            {
                s.timer.text = string.Empty;
            }
        }
    }
}
