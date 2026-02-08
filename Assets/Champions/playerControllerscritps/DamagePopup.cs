using UnityEngine;

// Simple floating damage text using TextMesh (no UI Canvas needed)
public class DamagePopup : MonoBehaviour
{
    [Header("Motion")]
    public float lifetime = 1.0f;
    public float riseSpeed = 2.5f; // a bit faster rise
    public Vector2 randomHorizontal = new Vector2(0.2f, 0.2f);

    [Header("Appearance")]
    public int baseFontSize = 48; // a bit smaller
    public float baseCharacterSize = 0.08f; // slightly smaller world-space size
    public Color baseColor = new Color(1f, 0.93f, 0.3f, 1f); // Fortnite-like yellow

    private TextMesh _textMesh;
    private MeshRenderer _meshRenderer;
    private float _elapsed;
    private Vector3 _drift;
    private Color _color;
    private Camera _cam;

    void Awake()
    {
        _textMesh = gameObject.AddComponent<TextMesh>();
        _meshRenderer = GetComponent<MeshRenderer>();

        // Configure TextMesh
        // Use the supported built-in runtime font in newer Unity versions
        Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtin == null)
        {
            // Fallback to an OS font if builtin is unavailable
            try
            {
                string[] candidates = { "Arial", "Helvetica", "Liberation Sans" };
                builtin = Font.CreateDynamicFontFromOSFont(candidates, baseFontSize);
            }
            catch { /* ignore */ }
        }
        _textMesh.font = builtin;
        _textMesh.alignment = TextAlignment.Center;
        _textMesh.anchor = TextAnchor.MiddleCenter;
        _textMesh.fontSize = baseFontSize;
        _textMesh.characterSize = baseCharacterSize;
        _textMesh.richText = true;
        _textMesh.text = "";

        if (_meshRenderer != null)
        {
            _meshRenderer.sortingOrder = 5000; // draw on top of most things
        }
    }

    public void Init(string text, Color color, Camera cam, bool crit = false)
    {
        _textMesh.text = text;
        _color = color;
        _cam = cam;
        _elapsed = 0f;

        // Random slight drift
        _drift = new Vector3(
            Random.Range(-randomHorizontal.x, randomHorizontal.x),
            Random.Range(0.05f, 0.15f),
            Random.Range(-randomHorizontal.y, randomHorizontal.y));

        // Critical hit style: bigger, more saturated
        if (crit)
        {
            transform.localScale *= 1.25f;
            _color = new Color(1f, 0.5f, 0.2f, 1f); // orange-red
            _textMesh.fontSize = Mathf.RoundToInt(baseFontSize * 1.2f);
        }

        ApplyColor(1f);
    }

    public void Init(float amount, bool crit = false)
    {
        Color c = baseColor;
        Init(Mathf.RoundToInt(amount).ToString(), c, Camera.main, crit);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / lifetime);

        // Billboard toward camera (keep upright)
        if (_cam != null)
        {
            Vector3 toCam = _cam.transform.position - transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
        }

        // Move up and drift
        transform.position += (Vector3.up * riseSpeed + _drift) * Time.deltaTime;

        // Fade out
        float alpha = 1f - t;
        ApplyColor(alpha);

        if (_elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyColor(float alpha)
    {
        Color c = _color;
        c.a = alpha;
        _textMesh.color = c;
    }
}
