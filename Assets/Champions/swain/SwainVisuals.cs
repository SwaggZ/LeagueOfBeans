using UnityEngine;

/// <summary>Placeholder visuals for Swain's abilities (lightning lines, hand, eye, aura, bursts).</summary>
public static class SwainVisuals
{
    public static readonly Color Violet = new Color(0.65f, 0.35f, 1f);
    public static readonly Color Crimson = new Color(0.9f, 0.1f, 0.2f);

    private static Shader _lit, _unlit;
    private static Shader Lit => _lit != null ? _lit : (_lit = Shader.Find("Universal Render Pipeline/Lit"));
    private static Shader Unlit => _unlit != null ? _unlit : (_unlit = Shader.Find("Universal Render Pipeline/Unlit"));

    public static Material Mat(Color c, float emission = 0f)
    {
        var m = new Material(Lit) { color = c };
        if (emission > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * emission); }
        return m;
    }

    /// <summary>URP unlit material set up for alpha blending.</summary>
    public static Material Transparent(Color c)
    {
        var m = new Material(Unlit);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.SetColor("_BaseColor", c);
        return m;
    }

    private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;
        return go;
    }

    /// <summary>A jagged bolt from a to b that fades out.</summary>
    public static void Lightning(Vector3 a, Vector3 b, Color color, float life, float width)
    {
        var go = new GameObject("SwainBolt");
        var lr = go.AddComponent<LineRenderer>();
        const int segments = 8;
        lr.positionCount = segments + 1;
        lr.material = Transparent(color);
        lr.widthMultiplier = width;
        lr.numCapVertices = 2;
        Vector3 d = b - a;
        Vector3 side = Vector3.Cross(d.normalized, Vector3.up);
        if (side.sqrMagnitude < 0.01f) side = Vector3.right;
        float jag = Mathf.Min(0.35f, d.magnitude * 0.05f);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = a + d * t;
            if (i > 0 && i < segments)
                p += side.normalized * Random.Range(-jag, jag) + Vector3.up * Random.Range(-jag, jag) * 0.6f;
            lr.SetPosition(i, p);
        }
        go.AddComponent<Fade>().Init(life, color, lr: lr);
    }

    /// <summary>Line used while the hand's recast is available; positions are set by the controller.</summary>
    public static LineRenderer Tether()
    {
        var go = new GameObject("SwainTether");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.material = Transparent(new Color(Crimson.r, Crimson.g, Crimson.b, 0.75f));
        lr.widthMultiplier = 0.06f;
        return lr;
    }

    public static GameObject Hand()
    {
        var root = new GameObject("SwainHand");
        var dark = Mat(new Color(0.12f, 0.05f, 0.08f));
        var glow = Mat(Crimson, 2f);
        Prim(PrimitiveType.Sphere, "Palm", root.transform, Vector3.zero, new Vector3(0.45f, 0.18f, 0.4f), dark);
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 0.11f;
            var f = Prim(PrimitiveType.Capsule, "Claw", root.transform, new Vector3(x, 0f, 0.3f), new Vector3(0.06f, 0.16f, 0.06f), dark);
            f.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            Prim(PrimitiveType.Sphere, "Tip", root.transform, new Vector3(x, -0.05f, 0.45f), Vector3.one * 0.06f, glow);
        }
        return root;
    }

    public static GameObject Eye()
    {
        var root = new GameObject("SwainEye");
        Prim(PrimitiveType.Sphere, "Ball", root.transform, Vector3.zero, Vector3.one * 0.4f, Mat(new Color(0.9f, 0.85f, 0.75f)));
        Prim(PrimitiveType.Sphere, "Iris", root.transform, new Vector3(0f, 0f, 0.15f), new Vector3(0.25f, 0.25f, 0.12f), Mat(Crimson, 2.5f));
        Prim(PrimitiveType.Sphere, "Pupil", root.transform, new Vector3(0f, 0f, 0.2f), new Vector3(0.06f, 0.18f, 0.05f), Mat(Color.black));
        return root;
    }

    /// <summary>Pulsing ground ring under Swain while in demon form. Parented to him.</summary>
    public static GameObject Aura(Transform owner, float radius)
    {
        var root = new GameObject("SwainDemonAura");
        root.transform.SetParent(owner, false);
        root.transform.localPosition = new Vector3(0f, -0.95f, 0f);
        Prim(PrimitiveType.Cylinder, "Disc", root.transform, Vector3.zero, new Vector3(radius * 2f, 0.01f, radius * 2f),
             Transparent(new Color(0.55f, 0.02f, 0.12f, 0.28f)));
        Prim(PrimitiveType.Cylinder, "Core", root.transform, new Vector3(0f, 0.01f, 0f), new Vector3(radius, 0.01f, radius),
             Transparent(new Color(0.9f, 0.1f, 0.25f, 0.22f)));
        root.AddComponent<Pulse>();
        return root;
    }

    /// <summary>Expanding, fading sphere.</summary>
    public static void Burst(Vector3 pos, float radius, Color color)
    {
        var go = Prim(PrimitiveType.Sphere, "SwainBurst", null, Vector3.zero, Vector3.one * 0.2f, Transparent(color));
        go.transform.position = pos;
        go.AddComponent<Fade>().Init(0.35f, color, growTo: radius * 2f);
    }

    private class Fade : MonoBehaviour
    {
        private float _life, _t, _grow;
        private Color _c;
        private LineRenderer _lr;
        private Material _m;

        public void Init(float life, Color c, LineRenderer lr = null, float growTo = 0f)
        {
            _life = life; _c = c; _lr = lr; _grow = growTo;
            _m = lr != null ? lr.material : GetComponent<Renderer>().material;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Mathf.Max(0.01f, _life));
            _m.SetColor("_BaseColor", new Color(_c.r, _c.g, _c.b, (_lr != null ? 1f : 0.55f) * (1f - k)));
            if (_grow > 0f) transform.localScale = Vector3.one * Mathf.Lerp(0.2f, _grow, 1f - (1f - k) * (1f - k));
            if (k >= 1f) Destroy(gameObject);
        }
    }

    private class Pulse : MonoBehaviour
    {
        void Update()
        {
            float s = 1f + Mathf.Sin(Time.time * 4f) * 0.04f;
            transform.localScale = new Vector3(s, 1f, s);
            transform.Rotate(0f, 30f * Time.deltaTime, 0f);
        }
    }
}
