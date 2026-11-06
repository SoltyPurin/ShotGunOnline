using System;
using UnityEngine;

/// <summary>
/// ゲーム全体のフレームレートおよびVSyncを一括管理するマネージャー。
/// PlayerPrefsに保存し、ゲーム起動時やシーン遷移時にも自動で設定を維持する。
/// </summary>
public static class FrameRateManager
{
    private const string PREF_KEY_TARGET_FPS = "Settings_TargetFPS";
    private const string PREF_KEY_VSYNC = "Settings_VSyncCount";

    public enum FrameRateMode
    {
        VSync = -2,
        Unlimited = -1,
        FPS_30 = 30,
        FPS_60 = 60,
        FPS_90 = 90,
        FPS_120 = 120,
        FPS_144 = 144,
        FPS_240 = 240
    }

    public static readonly FrameRateMode[] AvailableModes = new FrameRateMode[]
    {
        FrameRateMode.FPS_60,
        FrameRateMode.FPS_120,
        FrameRateMode.FPS_144,
        FrameRateMode.Unlimited,
        FrameRateMode.VSync,
        FrameRateMode.FPS_30
    };

    private static FrameRateMode _currentMode = FrameRateMode.FPS_120;
    public static FrameRateMode CurrentMode => _currentMode;

    public static event Action<FrameRateMode> OnFrameRateModeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnStartup()
    {
        LoadSettings();
    }

    /// <summary>
    /// 保存されている設定を読み込んで適用する。未設定の場合はデフォルト(120FPS)を適用。
    /// </summary>
    public static void LoadSettings()
    {
        int savedFps = PlayerPrefs.GetInt(PREF_KEY_TARGET_FPS, (int)FrameRateMode.FPS_120);
        int savedVSync = PlayerPrefs.GetInt(PREF_KEY_VSYNC, 0);

        if (savedVSync > 0)
        {
            SetMode(FrameRateMode.VSync, saveToPrefs: false);
        }
        else
        {
            SetMode((FrameRateMode)savedFps, saveToPrefs: false);
        }
    }

    /// <summary>
    /// フレームレートモードを設定し適用する。
    /// </summary>
    /// <param name="mode">設定するフレームレートモード</param>
    /// <param name="saveToPrefs">PlayerPrefsに保存するかどうか</param>
    public static void SetMode(FrameRateMode mode, bool saveToPrefs = true)
    {
        _currentMode = mode;

        if (mode == FrameRateMode.VSync)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1; // VSync時は-1推奨
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = (int)mode;
        }

        if (saveToPrefs)
        {
            if (mode == FrameRateMode.VSync)
            {
                PlayerPrefs.SetInt(PREF_KEY_VSYNC, 1);
            }
            else
            {
                PlayerPrefs.SetInt(PREF_KEY_VSYNC, 0);
                PlayerPrefs.SetInt(PREF_KEY_TARGET_FPS, (int)mode);
            }
            PlayerPrefs.Save();
        }

        Debug.Log($"[FrameRateManager] フレームレート設定を適用: Mode={mode}, targetFrameRate={Application.targetFrameRate}, vSyncCount={QualitySettings.vSyncCount}");
        OnFrameRateModeChanged?.Invoke(mode);
    }

    /// <summary>
    /// 次のフレームレートモードへ循環切り替え（デバッグやキーボードショートカット用）
    /// </summary>
    public static FrameRateMode ToggleNextMode()
    {
        int currentIndex = Array.IndexOf(AvailableModes, _currentMode);
        int nextIndex = (currentIndex + 1) % AvailableModes.Length;
        SetMode(AvailableModes[nextIndex]);
        return _currentMode;
    }

    /// <summary>
    /// シーン遷移などで上書きされた場合に現在の設定を再適用する
    /// </summary>
    public static void RefreshCurrentSettings()
    {
        SetMode(_currentMode, saveToPrefs: false);
    }
}
