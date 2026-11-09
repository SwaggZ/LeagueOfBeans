// Menu: Tools/Jinx/Build Champion - builds/updates Jinx via ChampionScaffold (safe to re-run).
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class JinxSetup
{
    [MenuItem("Tools/Jinx/Build Champion")]
    public static void BuildFromMenu() => Debug.Log("[Jinx] " + Build());

    public static string Build() => ChampionScaffold.Build(new ChampionScaffold.Config
    {
        Id = "Jinx",
        Folder = "Assets/Champions/jinx",
        Controller = typeof(JinxController),
        PlaceholderColor = new Color(0.35f, 0.55f, 1f),
        SelectMethod = cs => cs.Jinx,
        LmbName = "Minigun / Rockets",
        LmbDesc = "Hold to fire. Minigun spins up the longer you shoot; rockets hit slower but splash.",
        RmbName = "Swap Weapon",
        RmbDesc = "Switch between the minigun and the rocket launcher.",
        OneName = "Stun Mines",
        OneDesc = "Throw three mines. The first enemy to step on each is damaged and stunned.",
        TwoName = "Mega Rocket",
        TwoDesc = "Load up, then fire a giant rocket. Hits harder the further it flies and against wounded enemies.",
    });
}
#endif
