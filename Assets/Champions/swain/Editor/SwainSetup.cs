// Menu: Tools/Swain/Build Champion - builds/updates Swain via ChampionScaffold (safe to re-run).
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SwainSetup
{
    [MenuItem("Tools/Swain/Build Champion")]
    public static void BuildFromMenu() => Debug.Log("[Swain] " + Build());

    public static string Build() => ChampionScaffold.Build(new ChampionScaffold.Config
    {
        Id = "Swain",
        Folder = "Assets/Champions/swain",
        Controller = typeof(SwainController),
        PlaceholderColor = new Color(0.45f, 0.1f, 0.15f),
        SelectMethod = cs => cs.Swain,
        LmbName = "Lightning",
        LmbDesc = "Unleash bolts in a wide, flat cone in front of you.",
        RmbName = "Grasping Hand",
        RmbDesc = "Launch a demonic hand that stuns the first enemy hit. Recast to drag them to you.",
        OneName = "Ravenous Eye",
        OneDesc = "Fire an eye that bursts on impact, draining health from every enemy caught to heal you.",
        TwoName = "Demon Form",
        TwoDesc = "Become a demon. Your aura drains nearby enemies and heals you. Ends after 5s without hitting anyone, or 20s at most.",
    });
}
#endif
