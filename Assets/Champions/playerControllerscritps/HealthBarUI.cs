using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Attach this to the same GameObject that has HealthSystem.
// It creates a small world-space health bar above the object at runtime.
[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    public HealthSystem healthSystem; // If left null, we'll find it on this GameObject
    private ModifierTracker modifierTracker; // Optional; if present, displays modifiers above health bar

    [Header("Modifier Icons")]
    [Tooltip("Size of modifier icons in world units")]
    public float modifierIconSize = 0.3f;
    [Tooltip("Spacing between modifier icons")]
    public float modifierSpacing = 0.05f;
    [Tooltip("Vertical offset above the health bar")]
    public float modifierVerticalOffset = 0.25f;

    [Header("Bar Appearance")]
    [Tooltip("Vertical offset (in world units) above the GameObject where the bar is placed.")]
    public float verticalOffset = 2.0f;
    [Tooltip("Bar width in world units when scale is 1.")]
    public float barWidth = 1.6f;
    [Tooltip("Bar height in world units when scale is 1.")]
    public float barHeight = 0.18f;
    [Tooltip("Color of the background rectangle behind the health fill.")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [Tooltip("Color of the foreground (current health).")]
    public Color fillColor = new Color(0.2f, 1f, 0.2f, 0.95f);
    [Tooltip("Only show the bar when health is not full.")]
    public bool hideWhenFull = true;

    [Header("Billboarding")]
    [Tooltip("Keep the health bar facing the main camera.")]
    public bool billboardToCamera = true;

    private Canvas _canvas;
    private RectTransform _canvasRT;
    private Image _bgImage;
    private Image _fillImage;
    private static Sprite _defaultUISprite; // cached UI sprite (generated 1x1 white)
    private static Font _defaultFont;
    private static readonly Vector2 _basePixels = new Vector2(100f, 12f); // base pixel size; scaled to world units via localScale

    private GameObject _modifiersContainer; // Parent for modifier icons
    private List<Image> _modifierIconImages = new List<Image>(); // Reusable pool of modifier icon images
    private List<Text> _modifierStackLabels = new List<Text>(); // Reusable pool of stack labels

    private static Sprite GetDefaultUISprite()
    {
        if (_defaultUISprite == null)
        {
            // Create a 1x1 white sprite to avoid depending on built-in UI resources
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _defaultUISprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            _defaultUISprite.name = "HealthBarUI_WhiteSprite";
        }
        return _defaultUISprite;
    }

    private static Font GetDefaultFont()
    {
        if (_defaultFont == null)
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return _defaultFont;
    }

    void Awake()
    {
        if (healthSystem == null)
        {
            healthSystem = GetComponent<HealthSystem>();
        }
        
        // Optional modifier tracker
        modifierTracker = GetComponent<ModifierTracker>();

        // Create canvas holder
        GameObject canvasGO = new GameObject("HealthBar_Canvas", typeof(RectTransform));
        canvasGO.layer = gameObject.layer; // keep same layer
        canvasGO.transform.SetParent(transform, false);

        _canvasRT = canvasGO.GetComponent<RectTransform>();
        _canvasRT.sizeDelta = _basePixels; // use pixel size, scale to world size via localScale below
        _canvasRT.localPosition = new Vector3(0f, verticalOffset, 0f);
        _canvasRT.localRotation = Quaternion.identity;
        _canvasRT.localScale = new Vector3(barWidth / _basePixels.x, barHeight / _basePixels.y, 1f);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main; // optional; helps sorting
        canvasGO.AddComponent<GraphicRaycaster>(); // harmless even if unused

        // Background
        GameObject bgGO = new GameObject("BG", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        _bgImage = bgGO.AddComponent<Image>();
        _bgImage.color = backgroundColor;
        _bgImage.sprite = GetDefaultUISprite();
        _bgImage.type = Image.Type.Simple;
        _bgImage.raycastTarget = false;

        // Fill (use filled image type for easy percentage control)
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(bgGO.transform, false);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(2f, 2f);  // small padding
        fillRT.offsetMax = new Vector2(-2f, -2f);
        _fillImage = fillGO.AddComponent<Image>();
        _fillImage.color = fillColor;
        _fillImage.sprite = GetDefaultUISprite();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fillImage.fillAmount = 1f;
        _fillImage.raycastTarget = false;

        // Ensure fill renders above background
        _fillImage.transform.SetAsLastSibling();

        // Modifier icons container (positioned above the health bar)
        _modifiersContainer = new GameObject("ModifierIcons", typeof(RectTransform));
        _modifiersContainer.transform.SetParent(canvasGO.transform, false);
        RectTransform modRT = _modifiersContainer.GetComponent<RectTransform>();
        modRT.anchorMin = new Vector2(0.5f, 1f);
        modRT.anchorMax = new Vector2(0.5f, 1f);
        modRT.pivot = new Vector2(0.5f, 0f);
        modRT.anchoredPosition = new Vector2(0f, modifierVerticalOffset * 100f); // convert world units to pixels (rough)
        modRT.sizeDelta = new Vector2(_basePixels.x, _basePixels.y * 0.5f);

        UpdateFillImmediate();
        ApplyVisibility();
    }

    void LateUpdate()
    {
        if (healthSystem == null)
            return;

        // Update health fill each frame (cheap and robust)
        float max = Mathf.Max(healthSystem.maxHealth, 0.0001f);
        float current = Mathf.Clamp(healthSystem.GetCurrentHealth(), 0f, max);
        float pct = current / max;
        _fillImage.fillAmount = pct;

        if (hideWhenFull)
            _canvas.enabled = pct < 0.999f; // hide when basically full

        // Billboard so it faces camera
        if (billboardToCamera && Camera.main != null)
        {
            // Make the canvas face the camera while staying upright in world up
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude > 0.0001f)
                _canvasRT.rotation = Quaternion.LookRotation(camForward, Vector3.up);
        }

        // Keep offset and size in case properties changed at runtime
        _canvasRT.localPosition = new Vector3(0f, verticalOffset, 0f);
        _canvasRT.sizeDelta = _basePixels;
        _canvasRT.localScale = new Vector3(
            Mathf.Max(0.0001f, barWidth / _basePixels.x),
            Mathf.Max(0.0001f, barHeight / _basePixels.y),
            1f);
        _bgImage.color = backgroundColor;
        _fillImage.color = fillColor;

        // Update modifier icons if tracker exists
        if (modifierTracker != null && _modifiersContainer != null)
        {
            UpdateModifierIcons();
        }
    }

    private void UpdateModifierIcons()
    {
        var activeModifiers = modifierTracker.GetActiveModifiers();

        // Adjust number of display icons to match active modifiers
        while (_modifierIconImages.Count < activeModifiers.Count)
        {
            CreateModifierIcon();
        }
        while (_modifierIconImages.Count > activeModifiers.Count)
        {
            RemoveModifierIcon();
        }

        // Update each icon's sprite and position
        float totalWidth = activeModifiers.Count * modifierIconSize + (Mathf.Max(0, activeModifiers.Count - 1) * modifierSpacing);
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < activeModifiers.Count; i++)
        {
            var (sprite, remaining, stacks) = activeModifiers[i];
            var img = _modifierIconImages[i];
            var stackLabel = _modifierStackLabels[i];

            img.sprite = sprite;
            img.color = sprite != null ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            img.enabled = true;

            stackLabel.text = stacks > 1 ? stacks.ToString() : string.Empty;
            stackLabel.enabled = stacks > 1;

            // Position horizontally
            RectTransform rt = img.GetComponent<RectTransform>();
            float xPos = startX + i * (modifierIconSize + modifierSpacing);
            rt.anchoredPosition = new Vector2(xPos * 100f, 0f); // convert to pixels
        }
    }

    private void CreateModifierIcon()
    {
        GameObject iconGO = new GameObject("ModifierIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(_modifiersContainer.transform, false);

        RectTransform rt = iconGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(modifierIconSize * 100f, modifierIconSize * 100f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = iconGO.GetComponent<Image>();
        img.sprite = GetDefaultUISprite();
        img.color = Color.white;
        img.raycastTarget = false;

        GameObject stackGO = new GameObject("StackLabel", typeof(RectTransform), typeof(Text));
        stackGO.transform.SetParent(iconGO.transform, false);
        RectTransform srt = stackGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(1f, 0f);
        srt.anchorMax = new Vector2(1f, 0f);
        srt.pivot = new Vector2(1f, 0f);
        srt.anchoredPosition = new Vector2(-2f, 2f);
        srt.sizeDelta = new Vector2(modifierIconSize * 100f, modifierIconSize * 100f);

        Text stackText = stackGO.GetComponent<Text>();
        stackText.font = GetDefaultFont();
        stackText.fontSize = 12;
        stackText.color = Color.white;
        stackText.alignment = TextAnchor.LowerRight;
        stackText.text = string.Empty;
        stackText.raycastTarget = false;

        _modifierIconImages.Add(img);
        _modifierStackLabels.Add(stackText);
    }

    private void RemoveModifierIcon()
    {
        if (_modifierIconImages.Count > 0)
        {
            Image lastIcon = _modifierIconImages[_modifierIconImages.Count - 1];
            _modifierIconImages.RemoveAt(_modifierIconImages.Count - 1);
            Destroy(lastIcon.gameObject);
        }
        if (_modifierStackLabels.Count > 0)
        {
            _modifierStackLabels.RemoveAt(_modifierStackLabels.Count - 1);
        }
    }

    private void UpdateFillImmediate()
    {
        if (healthSystem == null)
            return;
        float max = Mathf.Max(healthSystem.maxHealth, 0.0001f);
        float current = Mathf.Clamp(healthSystem.GetCurrentHealth(), 0f, max);
        _fillImage.fillAmount = current / max;
    }

    private void ApplyVisibility()
    {
        if (_canvas == null)
            return;
        if (!hideWhenFull)
        {
            _canvas.enabled = true;
            return;
        }
        float max = 1f;
        float current = 1f;
        if (healthSystem != null)
        {
            max = Mathf.Max(healthSystem.maxHealth, 0.0001f);
            current = Mathf.Clamp(healthSystem.GetCurrentHealth(), 0f, max);
        }
        _canvas.enabled = (current / max) < 0.999f;
    }
}
