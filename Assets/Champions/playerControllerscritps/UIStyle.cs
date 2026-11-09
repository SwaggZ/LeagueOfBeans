using UnityEngine;

/// <summary>Shared look for runtime-built UI (ability HUD, champion select): palette + generated sprites.</summary>
public static class UIStyle
{
    // Palette (dark slate + League-style gold)
    public static readonly Color Gold        = new Color(0.78f, 0.67f, 0.43f, 1f);
    public static readonly Color GoldBright  = new Color(0.94f, 0.84f, 0.58f, 1f);
    public static readonly Color GoldDim     = new Color(0.32f, 0.30f, 0.27f, 1f);
    public static readonly Color Cream       = new Color(0.94f, 0.90f, 0.80f, 1f);
    public static readonly Color Ink         = new Color(0.02f, 0.04f, 0.06f, 1f);
    public static readonly Color Slate       = new Color(0.04f, 0.07f, 0.09f, 0.94f);
    public static readonly Color SlateLight  = new Color(0.09f, 0.13f, 0.16f, 0.96f);
    public static readonly Color Teal        = new Color(0.04f, 0.78f, 0.73f, 1f);

    static Sprite _white, _rounded;

    /// <summary>1x1 white sprite (for fills, radial sweeps).</summary>
    public static Sprite White
    {
        get
        {
            if (_white == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
                _white.name = "UI_White";
            }
            return _white;
        }
    }

    /// <summary>Anti-aliased rounded rectangle, 9-sliced (use Image.Type.Sliced).</summary>
    public static Sprite Rounded
    {
        get
        {
            if (_rounded == null)
            {
                const int size = 32, radius = 8;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f)));
                }
                tex.Apply();
                _rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                                         SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                _rounded.name = "UI_Rounded";
            }
            return _rounded;
        }
    }

    public static string KeyLabel(AbilityKey key)
    {
        switch (key)
        {
            case AbilityKey.LeftClick: return "LMB";
            case AbilityKey.RightClick: return "RMB";
            case AbilityKey.One: return "Q";
            case AbilityKey.Two: return "E";
            default: return "CTRL";
        }
    }
}
