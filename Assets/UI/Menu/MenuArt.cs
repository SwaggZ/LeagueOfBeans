using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Art used by the main menu (logo + featured champion splashes). Lives in a Resources folder
/// (Resources/MenuArt.asset) so the runtime MainMenuStyler can find it without scene wiring.
/// </summary>
[CreateAssetMenu(menuName = "League of Beans/Menu Art", fileName = "MenuArt")]
public class MenuArt : ScriptableObject
{
    [System.Serializable]
    public class Featured
    {
        public string championName;
        public Sprite splash;
        [Tooltip("Horizontal point of the image to keep in frame (0 = left edge, 1 = right edge), e.g. the champion's face.")]
        [Range(0f, 1f)] public float focusX = 0.5f;
    }

    public Sprite wordmark;
    public Sprite emblem;
    public List<Featured> featured = new List<Featured>();

    private static MenuArt _cached;
    public static MenuArt Load()
    {
        if (_cached == null) _cached = Resources.Load<MenuArt>("MenuArt");
        return _cached;
    }
}
