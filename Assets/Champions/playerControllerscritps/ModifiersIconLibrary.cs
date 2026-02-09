using UnityEngine;
using UnityEngine.Serialization;

// Drag this onto a GameObject in your gameplay scene (e.g., GameManager)
// and assign sprites for common modifiers in the Inspector.
// Then code can reference ModifiersIconLibrary.Instance.* to fetch icons.
[DisallowMultipleComponent]
public class ModifiersIconLibrary : MonoBehaviour
{
    public static ModifiersIconLibrary Instance { get; private set; }

    [Header("Common Modifier Icons")]
    [Header("Expanded Library (match your PNG names)")]
    public Sprite ATTRACT;
    public Sprite DMGBURN;
    public Sprite DMGPOISON;
    public Sprite DMGRD;
    public Sprite HASTE;
    public Sprite KNOCKUP;
    public Sprite MOVESPEED;
    public Sprite SHIELD;
    public Sprite SLOWNESS;
    public Sprite STUN;

    // Back-compat soft aliases
    public Sprite burn => DMGBURN;
    public Sprite poison => DMGPOISON;
    public Sprite shield => SHIELD;
    public Sprite haste => HASTE;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Sprite Resolve(string id, string label)
    {
        string key = (id ?? string.Empty) + " " + (label ?? string.Empty);
        key = key.ToLowerInvariant();
        
        Sprite result = null;
        
        // Direct checks first
        if (key.Contains("dmgrd") || key.Contains("damage reduction") || key.Contains("dr")) result = DMGRD;
        else if (key.Contains("slow")) result = SLOWNESS;
        else if (key.Contains("burn")) result = DMGBURN;
        else if (key.Contains("poison")) result = DMGPOISON;
        else if (key.Contains("stun")) result = STUN;
        else if (key.Contains("knock")) result = KNOCKUP;
        else if (key.Contains("move") || key.Contains("speed")) result = MOVESPEED != null ? MOVESPEED : HASTE;
        else if (key.Contains("haste")) result = HASTE;
        else if (key.Contains("shield")) result = SHIELD;
        else if (key.Contains("attract") || key.Contains("pull")) result = ATTRACT;
        
        return result;
    }
}
