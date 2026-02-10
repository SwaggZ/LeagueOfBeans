using UnityEngine;
using UnityEngine.UI;

// Reactive crosshair with 4 flaps that open on fire and from movement, then settle.
// Drop this anywhere; it will auto-create a ScreenSpaceOverlay canvas and a crosshair UI.
// Public API: call OnShoot() whenever you fire to kick the crosshair open.
[DisallowMultipleComponent]
public class ReactiveCrosshair : MonoBehaviour
{
    [Header("Appearance")]
    public float baseGap = 12f;            // distance from center to each flap at rest
    public float flapLength = 18f;         // visible length of each flap
    public float flapThickness = 3f;       // thickness (height for L/R, width for U/D)
    public Color color = new Color(1f, 1f, 1f, 0.9f);

    [Header("Recoil/Open Behavior")]
    public float kickPerShot = 10f;        // how much additional gap per shot
    public float maxKick = 40f;            // clamp for accumulated recoil
    public float recoveryPerSecond = 35f;  // how quickly recoil decays back to 0

    [Header("Movement Influence")] 
    public Transform playerRoot;           // optional; will try to find Player tag when empty
    public float maxMoveSpeed = 8f;        // speed that yields full movementSpread
    public float movementSpreadMax = 12f;  // max extra spread from movement

    [Header("Input Listening")]
    public bool listenForLeftClick = true; // if true, RMB/LMB events will drive OnShoot automatically

    private RectTransform _canvasRT;
    private RectTransform _root;
    private RectTransform _up, _down, _left, _right;
    private float _recoil;                 // decays to 0
    private Sprite _white;

    void Awake()
    {
        EnsureCanvasAndUI();
        if (playerRoot == null)
        {
            var player = LocalPlayerRef.GetLocalPlayerWithFallback();
            if (player != null) playerRoot = player.transform;
        }
    }

    void Update()
    {
        if (listenForLeftClick && Input.GetMouseButtonDown(0))
            OnShoot();

        // Decay recoil
        _recoil = Mathf.MoveTowards(_recoil, 0f, recoveryPerSecond * Time.unscaledDeltaTime);

        // Movement-based spread
        float moveSpeed = GetPlayerSpeed();
        float moveFactor = Mathf.Clamp01(maxMoveSpeed > 0f ? (moveSpeed / maxMoveSpeed) : 0f);
        float moveSpread = movementSpreadMax * moveFactor;

        float spread = baseGap + moveSpread + _recoil;
        LayoutFlaps(spread);
    }

    public void OnShoot()
    {
        _recoil = Mathf.Min(_recoil + kickPerShot, maxKick);
    }

    private float GetPlayerSpeed()
    {
        if (playerRoot == null) return 0f;
        // Try CharacterController
        var cc = playerRoot.GetComponent<CharacterController>();
        if (cc != null) return cc.velocity.magnitude;
        // Try Rigidbody
        var rb = playerRoot.GetComponent<Rigidbody>();
        if (rb != null) return rb.velocity.magnitude;
        // As a last resort, sample change in position (very rough)
        return 0f;
    }

    private void EnsureCanvasAndUI()
    {
        if (_white == null)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            _white.name = "ReactiveCrosshair_White";
        }

        // Canvas root
        var canvasGO = new GameObject("HUD_Crosshair", typeof(RectTransform));
        _canvasRT = canvasGO.GetComponent<RectTransform>();
        _canvasRT.SetParent(this.transform, false);
        _canvasRT.anchorMin = Vector2.zero;
        _canvasRT.anchorMax = Vector2.one;
        _canvasRT.offsetMin = Vector2.zero;
        _canvasRT.offsetMax = Vector2.zero;
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Crosshair container (centered)
        var rootGO = new GameObject("Crosshair", typeof(RectTransform));
        _root = rootGO.GetComponent<RectTransform>();
        _root.SetParent(_canvasRT, false);
        _root.anchorMin = new Vector2(0.5f, 0.5f);
        _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0.5f);
        _root.anchoredPosition = Vector2.zero;
        _root.sizeDelta = new Vector2(200, 200);

        // Create 4 flaps
        _up = CreateFlap("Up");
        _down = CreateFlap("Down");
        _left = CreateFlap("Left");
        _right = CreateFlap("Right");

        LayoutFlaps(baseGap);
    }

    private RectTransform CreateFlap(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(_root, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        var img = go.AddComponent<Image>();
        img.sprite = _white;
        img.color = color;
        return rt;
    }

    private void LayoutFlaps(float spread)
    {
        if (_up == null) return;

        // Up
        _up.sizeDelta = new Vector2(flapThickness, flapLength);
        _up.anchoredPosition = new Vector2(0f, spread + flapLength * 0.5f);

        // Down
        _down.sizeDelta = new Vector2(flapThickness, flapLength);
        _down.anchoredPosition = new Vector2(0f, -spread - flapLength * 0.5f);

        // Left
        _left.sizeDelta = new Vector2(flapLength, flapThickness);
        _left.anchoredPosition = new Vector2(-spread - flapLength * 0.5f, 0f);

        // Right
        _right.sizeDelta = new Vector2(flapLength, flapThickness);
        _right.anchoredPosition = new Vector2(spread + flapLength * 0.5f, 0f);
    }
}
