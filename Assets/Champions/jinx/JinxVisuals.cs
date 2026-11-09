using UnityEngine;

/// <summary>
/// Placeholder visuals for Jinx's projectiles, used when no prefab is assigned on the controller.
/// Swap in real prefabs/VFX later via the JinxController prefab fields.
/// </summary>
public static class JinxVisuals
{
    private static Shader _lit;
    private static Shader Lit => _lit != null ? _lit : (_lit = Shader.Find("Universal Render Pipeline/Lit"));

    public static Material Mat(Color c, float emission = 0f)
    {
        var m = new Material(Lit) { color = c };
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emission);
        }
        return m;
    }

    private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Color color, float emission, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = Mat(color, emission);
        return go;
    }

    /// <summary>A glowing capsule pointing along +Z.</summary>
    public static GameObject Bullet(Color color, float width, float length)
    {
        var root = new GameObject("JinxShot");
        var body = Prim(PrimitiveType.Capsule, "Body", root.transform, Vector3.zero, new Vector3(width, length * 0.5f, width), color, 2f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        return root;
    }

    /// <summary>Stun mine: dark base with a pink jaw dome. Physics body on the root.</summary>
    public static GameObject Mine()
    {
        var root = new GameObject("JinxMine");
        var col = root.AddComponent<SphereCollider>();
        col.radius = 0.3f;
        root.AddComponent<Rigidbody>();
        Prim(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, -0.15f, 0f), new Vector3(0.7f, 0.08f, 0.7f), new Color(0.15f, 0.15f, 0.2f), 0f);
        Prim(PrimitiveType.Sphere, "Jaw", root.transform, new Vector3(0f, -0.05f, 0f), new Vector3(0.55f, 0.3f, 0.55f), new Color(0.55f, 0.2f, 0.4f), 0.5f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Cube, "Tooth", root.transform, new Vector3(Mathf.Cos(a) * 0.27f, 0.05f, Mathf.Sin(a) * 0.27f),
                 new Vector3(0.06f, 0.12f, 0.06f), new Color(0.95f, 0.95f, 0.9f), 0f);
        }
        return root;
    }

    /// <summary>The big ultimate rocket.</summary>
    public static GameObject MegaRocket()
    {
        var root = new GameObject("JinxMegaRocket");
        var body = Prim(PrimitiveType.Capsule, "Body", root.transform, Vector3.zero, new Vector3(0.6f, 1.1f, 0.6f), new Color(0.25f, 0.55f, 1f), 0.6f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Prim(PrimitiveType.Sphere, "Nose", root.transform, new Vector3(0f, 0f, 1.05f), new Vector3(0.5f, 0.5f, 0.6f), new Color(1f, 0.3f, 0.65f), 1.5f);
        for (int i = 0; i < 4; i++)
        {
            var fin = Prim(PrimitiveType.Cube, "Fin", root.transform, Vector3.zero, new Vector3(0.05f, 0.7f, 0.5f), new Color(1f, 0.3f, 0.65f), 0.5f);
            fin.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            fin.transform.localPosition = fin.transform.localRotation * new Vector3(0f, 0.35f, -0.85f);
        }
        Prim(PrimitiveType.Sphere, "Exhaust", root.transform, new Vector3(0f, 0f, -1.25f), new Vector3(0.45f, 0.45f, 0.7f), new Color(1f, 0.6f, 0.2f), 4f);
        return root;
    }

    /// <summary>Brief expanding, fading explosion sphere.</summary>
    public static void Blast(Vector3 pos, float radius, Color color)
    {
        var go = Prim(PrimitiveType.Sphere, "JinxBlast", null, pos, Vector3.one * 0.2f, color, 2f);
        go.transform.position = pos;
        var fx = go.AddComponent<BlastFx>();
        fx.targetScale = radius * 2f;
    }

    private class BlastFx : MonoBehaviour
    {
        public float targetScale = 4f;
        public float life = 0.3f;
        private float _t;
        private Material _m;
        private Color _c;

        void Start()
        {
            _m = GetComponent<Renderer>().material;
            _m.SetFloat("_Surface", 1f); // transparent
            _m.SetFloat("_Blend", 0f);
            _m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _m.SetInt("_ZWrite", 0);
            _m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            _c = _m.color;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / life);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, targetScale, 1f - (1f - k) * (1f - k));
            _m.color = new Color(_c.r, _c.g, _c.b, 0.6f * (1f - k));
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
