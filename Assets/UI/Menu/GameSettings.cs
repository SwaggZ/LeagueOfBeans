using UnityEngine;

/// <summary>
/// Player-facing settings, persisted in PlayerPrefs and applied on startup.
/// Edited from the main menu's Settings panel.
/// </summary>
public static class GameSettings
{
    const string KeyVolume = "Settings.MasterVolume";
    const string KeySensitivity = "Settings.MouseSensitivity";
    const string KeyFullscreen = "Settings.Fullscreen";
    const string KeyQuality = "Settings.Quality";

    /// <summary>0..1, applied to AudioListener.volume.</summary>
    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(KeyVolume, 1f);
        set { PlayerPrefs.SetFloat(KeyVolume, Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); }
    }

    /// <summary>Multiplier on each champion camera's base sensitivity (1 = default).</summary>
    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(KeySensitivity, 1f);
        set => PlayerPrefs.SetFloat(KeySensitivity, Mathf.Clamp(value, 0.1f, 3f));
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        set { PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0); Screen.fullScreen = value; }
    }

    public static int Quality
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
        set
        {
            int q = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
            PlayerPrefs.SetInt(KeyQuality, q);
            QualitySettings.SetQualityLevel(q, true);
        }
    }

    public static void Save() => PlayerPrefs.Save();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyOnStartup()
    {
        AudioListener.volume = MasterVolume;
        if (PlayerPrefs.HasKey(KeyQuality)) QualitySettings.SetQualityLevel(Quality, true);
        if (PlayerPrefs.HasKey(KeyFullscreen) && !Application.isEditor) Screen.fullScreen = Fullscreen;
    }
}
