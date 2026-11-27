/// <summary>
/// フレームレートおよび垂直同期(VSync)設定の永続化先を抽象化するリポジトリインターフェース (DIP / SRP)。
/// </summary>
public interface IFrameRateSettingsRepository
{
    (int TargetFps, bool IsVSyncEnabled) Load(int defaultTargetFps, bool defaultVSync);
    void Save(int targetFps, bool isVSyncEnabled);
}

/// <summary>
/// PlayerPrefs を用いたフレームレート設定リポジトリ実装。
/// </summary>
public sealed class PlayerPrefsFrameRateRepository : IFrameRateSettingsRepository
{
    private const string PrefKeyTargetFps = "Settings_TargetFPS";
    private const string PrefKeyVSync = "Settings_VSyncCount";

    public (int TargetFps, bool IsVSyncEnabled) Load(int defaultTargetFps, bool defaultVSync)
    {
        int fps = UnityEngine.PlayerPrefs.GetInt(PrefKeyTargetFps, defaultTargetFps);
        bool vsync = UnityEngine.PlayerPrefs.GetInt(PrefKeyVSync, defaultVSync ? 1 : 0) > 0;
        return (fps, vsync);
    }

    public void Save(int targetFps, bool isVSyncEnabled)
    {
        UnityEngine.PlayerPrefs.SetInt(PrefKeyTargetFps, targetFps);
        UnityEngine.PlayerPrefs.SetInt(PrefKeyVSync, isVSyncEnabled ? 1 : 0);
        UnityEngine.PlayerPrefs.Save();
    }
}
