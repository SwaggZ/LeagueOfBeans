using UnityEngine;
using UnityEngine.UI;

// Attach this to the same GameObject that has HealthSystem.
// It creates a small world-space health bar above the object at runtime.
[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    public HealthSystem healthSystem; // If left null, we'll find it on this GameObject

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
    private static readonly Vector2 _basePixels = new Vector2(100f, 12f); // base pixel size; scaled to world units via localScale

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

    void Awake()
    {
        if (healthSystem == null)
        {
            healthSystem = GetComponent<HealthSystem>();
        }

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
