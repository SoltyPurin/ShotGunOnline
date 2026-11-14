using System;
using UnityEngine;

/// <summary>
/// ゲーム全体のフレームレートおよび垂直同期(VSync)を一括管理するマネージャー。
/// PlayerPrefsに保存し、ゲーム起動時やシーン遷移時にも自動で設定を維持します。
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

    /// <summary>
    /// オプション画面で選択可能なFPS一覧 (-1は無制限)
    /// </summary>
    public static readonly int[] SelectableFpsList = new int[]
    {
        30,
        60,
        90,
        120,
        144,
        240,
        -1
    };

    public static readonly FrameRateMode[] AvailableModes = new FrameRateMode[]
    {
        FrameRateMode.FPS_60,
        FrameRateMode.FPS_120,
        FrameRateMode.FPS_144,
        FrameRateMode.Unlimited,
        FrameRateMode.VSync,
        FrameRateMode.FPS_30
    };

    private static int _targetFps = 120;
    private static bool _isVSyncEnabled = false;
    private static bool _isInitialized = false;

    /// <summary>
    /// 現在選択されている目標FPS (-1は無制限)
    /// </summary>
    public static int TargetFps
    {
        get
        {
            EnsureInitialized();
            return _targetFps;
        }
    }

    /// <summary>
    /// 垂直同期(VSync)が有効かどうか
    /// </summary>
    public static bool IsVSyncEnabled
    {
        get
        {
            EnsureInitialized();
            return _isVSyncEnabled;
        }
    }

    /// <summary>
    /// 現在の動作モード
    /// </summary>
    public static FrameRateMode CurrentMode
    {
        get
        {
            EnsureInitialized();
            if (_isVSyncEnabled) return FrameRateMode.VSync;
            return Enum.IsDefined(typeof(FrameRateMode), _targetFps)
                ? (FrameRateMode)_targetFps
                : FrameRateMode.FPS_120;
        }
    }

    public static event Action<FrameRateMode> OnFrameRateModeChanged;
    public static event Action OnSettingsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnStartup()
    {
        LoadSettings();
    }

    private static void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            LoadSettings();
        }
    }

    /// <summary>
    /// 保存されている設定を読み込んで適用する。未設定の場合はデフォルト(120FPS, VSync OFF)を適用。
    /// </summary>
    public static void LoadSettings()
    {
        _isInitialized = true;
        _targetFps = PlayerPrefs.GetInt(PREF_KEY_TARGET_FPS, 120);
        if (Array.IndexOf(SelectableFpsList, _targetFps) < 0)
        {
            _targetFps = 120;
        }

        _isVSyncEnabled = PlayerPrefs.GetInt(PREF_KEY_VSYNC, 0) > 0;
        ApplyCurrentSettings(saveToPrefs: false);
    }

    /// <summary>
    /// 目標FPSを設定する
    /// </summary>
    public static void SetTargetFps(int fps, bool saveToPrefs = true)
    {
        EnsureInitialized();
        if (Array.IndexOf(SelectableFpsList, fps) < 0)
        {
            fps = 120;
        }

        _targetFps = fps;
        ApplyCurrentSettings(saveToPrefs);
    }

    /// <summary>
    /// 次のFPS選択肢へ進める（右ボタン用）
    /// </summary>
    public static int NextTargetFps()
    {
        EnsureInitialized();
        int idx = Array.IndexOf(SelectableFpsList, _targetFps);
        if (idx < 0) idx = 0;
        idx = (idx + 1) % SelectableFpsList.Length;
        SetTargetFps(SelectableFpsList[idx]);
        return _targetFps;
    }

    /// <summary>
    /// 前のFPS選択肢へ戻す（左ボタン用）
    /// </summary>
    public static int PrevTargetFps()
    {
        EnsureInitialized();
        int idx = Array.IndexOf(SelectableFpsList, _targetFps);
        if (idx < 0) idx = 0;
        idx = (idx - 1 + SelectableFpsList.Length) % SelectableFpsList.Length;
        SetTargetFps(SelectableFpsList[idx]);
        return _targetFps;
    }

    /// <summary>
    /// 垂直同期(VSync)のON/OFFを設定する
    /// </summary>
    public static void SetVSync(bool enabled, bool saveToPrefs = true)
    {
        EnsureInitialized();
        _isVSyncEnabled = enabled;
        ApplyCurrentSettings(saveToPrefs);
    }

    /// <summary>
    /// 垂直同期(VSync)のON/OFFをトグル切り替えする
    /// </summary>
    public static bool ToggleVSync()
    {
        EnsureInitialized();
        SetVSync(!_isVSyncEnabled);
        return _isVSyncEnabled;
    }

    /// <summary>
    /// フレームレートモードを一括設定する
    /// </summary>
    public static void SetMode(FrameRateMode mode, bool saveToPrefs = true)
    {
        EnsureInitialized();
        if (mode == FrameRateMode.VSync)
        {
            _isVSyncEnabled = true;
        }
        else
        {
            _isVSyncEnabled = false;
            _targetFps = (int)mode;
        }

        ApplyCurrentSettings(saveToPrefs);
    }

    private static void ApplyCurrentSettings(bool saveToPrefs)
    {
        if (_isVSyncEnabled)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetFps;
        }

        if (saveToPrefs)
        {
            PlayerPrefs.SetInt(PREF_KEY_TARGET_FPS, _targetFps);
            PlayerPrefs.SetInt(PREF_KEY_VSYNC, _isVSyncEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        Debug.Log($"[FrameRateManager] 設定適用: TargetFPS={GetTargetFpsDisplayString()}, VSync={(_isVSyncEnabled ? "ON" : "OFF")} (actual targetFrameRate={Application.targetFrameRate}, vSyncCount={QualitySettings.vSyncCount})");
        OnFrameRateModeChanged?.Invoke(CurrentMode);
        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// UI表示用のFPSテキストを取得する (例: "60", "120", "MAX")
    /// </summary>
    public static string GetTargetFpsDisplayString()
    {
        EnsureInitialized();
        return _targetFps < 0 ? "MAX" : _targetFps.ToString();
    }

    /// <summary>
    /// UI表示用のVSyncテキストを取得する ("ON" / "OFF")
    /// </summary>
    public static string GetVSyncDisplayString()
    {
        EnsureInitialized();
        return _isVSyncEnabled ? "ON" : "OFF";
    }

    /// <summary>
    /// 次のフレームレートモードへ循環切り替え（デバッグショートカット用）
    /// </summary>
    public static FrameRateMode ToggleNextMode()
    {
        int currentIndex = Array.IndexOf(AvailableModes, CurrentMode);
        int nextIndex = (currentIndex + 1) % AvailableModes.Length;
        SetMode(AvailableModes[nextIndex]);
        return CurrentMode;
    }

    /// <summary>
    /// シーン遷移などで上書きされた場合に現在の設定を再適用する
    /// </summary>
    public static void RefreshCurrentSettings()
    {
        EnsureInitialized();
        ApplyCurrentSettings(saveToPrefs: false);
    }
}
