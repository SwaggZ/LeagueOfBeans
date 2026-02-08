using UnityEngine;

// Drag this onto a GameObject in your gameplay scene (e.g., GameManager)
// and assign sprites for common modifiers in the Inspector.
// Then code can reference ModifiersIconLibrary.Instance.* to fetch icons.
[DisallowMultipleComponent]
public class ModifiersIconLibrary : MonoBehaviour
{
    public static ModifiersIconLibrary Instance { get; private set; }

    [Header("Common Modifier Icons")]
    public Sprite damageReduction; // e.g., your DMGRD sprite
    public Sprite burn;
    public Sprite poison;
    public Sprite shield;
    public Sprite haste;

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
}
