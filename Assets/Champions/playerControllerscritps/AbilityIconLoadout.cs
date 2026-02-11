using System.Collections.Generic;
using UnityEngine;

// Put this on a gameplay character prefab and assign sprites/presence.
// On spawn, SelectionSpawnRequest/PlayerSpawner will call ApplyToHUD automatically
// to populate the Cooldown UI icons and visibility.
[DisallowMultipleComponent]
public class AbilityIconLoadout : MonoBehaviour
{
    [Header("Presence")]
    public bool leftClickPresent = true;
    public bool rightClickPresent = true;
    public bool onePresent = true;
    public bool twoPresent = true;
    public bool ctrlPresent = false; // usually optional dash

    [Header("Icons")]
    public Sprite leftClickIcon;
    public Sprite rightClickIcon;
    public Sprite oneIcon;
    public Sprite twoIcon;
    public Sprite ctrlIcon; // dash

    public void ApplyToHUD()
    {
        var cooldownUi = FindObjectOfType<CooldownUIManager>(true);
        if (cooldownUi == null) return;

        var list = new List<CooldownUIManager.AbilityEntry>
        {
            new CooldownUIManager.AbilityEntry{ key = AbilityKey.LeftClick, icon = leftClickIcon, present = leftClickPresent },
            new CooldownUIManager.AbilityEntry{ key = AbilityKey.RightClick, icon = rightClickIcon, present = rightClickPresent },
            new CooldownUIManager.AbilityEntry{ key = AbilityKey.One, icon = oneIcon, present = onePresent },
            new CooldownUIManager.AbilityEntry{ key = AbilityKey.Two, icon = twoIcon, present = twoPresent },
        };

        var dash = new CooldownUIManager.AbilityEntry{ key = AbilityKey.Ctrl, icon = ctrlIcon, present = ctrlPresent };
        cooldownUi.ApplyLoadout(list, dash);
    }
}
