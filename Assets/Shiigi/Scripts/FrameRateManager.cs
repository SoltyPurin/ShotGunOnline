using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体のフレームレートおよび垂直同期(VSync)を管理する。
/// </summary>
public static class FrameRateManager
{
    private const int DefaultTargetFps = 120;

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

    private static readonly int[] s_selectableFpsArray =
    {
        30,
        60,
        90,
        120,
        144,
        240,
        -1
    };

    private static readonly FrameRateMode[] s_availableModesArray =
    {
        FrameRateMode.FPS_60,
        FrameRateMode.FPS_120,
        FrameRateMode.FPS_144,
        FrameRateMode.Unlimited,
        FrameRateMode.VSync,
        FrameRateMode.FPS_30
    };

    /// <summary>
    /// オプション画面で選択可能なFPS一覧 (-1は無制限)
    /// </summary>
    public static IReadOnlyList<int> SelectableFpsList => s_selectableFpsArray;

    public static IReadOnlyList<FrameRateMode> AvailableModes => s_availableModesArray;

    private static IFrameRateSettingsRepository _repository = new PlayerPrefsFrameRateRepository();
    private static int _targetFps = DefaultTargetFps;
    private static bool _isVSyncEnabled;
    private static bool _isInitialized;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _repository = new PlayerPrefsFrameRateRepository();
        _targetFps = DefaultTargetFps;
        _isVSyncEnabled = false;
        _isInitialized = false;
        OnFrameRateModeChanged = null;
        OnSettingsChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnStartup()
    {
        LoadSettings();
    }

    /// <summary>
    /// 設定保存先リポジトリを差し替える（テストや将来のクラウド設定保存用）
    /// </summary>
    public static void SetRepository(IFrameRateSettingsRepository repository, bool reloadImmediately = true)
    {
        _repository = repository ?? new PlayerPrefsFrameRateRepository();
        if (reloadImmediately)
        {
            LoadSettings();
        }
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
        var (loadedFps, loadedVSync) = _repository.Load(DefaultTargetFps, defaultVSync: false);
        _targetFps = NormalizeTargetFps(loadedFps);
        _isVSyncEnabled = loadedVSync;
        ApplyCurrentSettings(saveToRepository: false, notifyListeners: true);
    }

    /// <summary>
    /// 目標FPSを設定する
    /// </summary>
    public static void SetTargetFps(int fps, bool saveToPrefs = true)
    {
        EnsureInitialized();
        int normalized = NormalizeTargetFps(fps);
        if (_targetFps == normalized) return;

        _targetFps = normalized;
        ApplyCurrentSettings(saveToPrefs, notifyListeners: true);
    }

    /// <summary>
    /// 次のFPS選択肢へ進める（右ボタン用）
    /// </summary>
    public static int NextTargetFps()
    {
        EnsureInitialized();
        int idx = Array.IndexOf(s_selectableFpsArray, _targetFps);
        if (idx < 0) idx = 0;
        idx = (idx + 1) % s_selectableFpsArray.Length;
        SetTargetFps(s_selectableFpsArray[idx]);
        return _targetFps;
    }

    /// <summary>
    /// 前のFPS選択肢へ戻す（左ボタン用）
    /// </summary>
    public static int PrevTargetFps()
    {
        EnsureInitialized();
        int idx = Array.IndexOf(s_selectableFpsArray, _targetFps);
        if (idx < 0) idx = 0;
        idx = (idx - 1 + s_selectableFpsArray.Length) % s_selectableFpsArray.Length;
        SetTargetFps(s_selectableFpsArray[idx]);
        return _targetFps;
    }

    /// <summary>
    /// 垂直同期(VSync)のON/OFFを設定する
    /// </summary>
    public static void SetVSync(bool enabled, bool saveToPrefs = true)
    {
        EnsureInitialized();
        bool changed = _isVSyncEnabled != enabled;
        _isVSyncEnabled = enabled;
        ApplyCurrentSettings(saveToPrefs && changed, notifyListeners: changed);
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
        bool nextVSync = mode == FrameRateMode.VSync;
        int nextFps = nextVSync ? _targetFps : NormalizeTargetFps((int)mode);
        bool changed = (_isVSyncEnabled != nextVSync) || (_targetFps != nextFps);

        _isVSyncEnabled = nextVSync;
        _targetFps = nextFps;

        ApplyCurrentSettings(saveToPrefs && changed, notifyListeners: changed);
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
        int currentIndex = Array.IndexOf(s_availableModesArray, CurrentMode);
        int nextIndex = (currentIndex + 1) % s_availableModesArray.Length;
        SetMode(s_availableModesArray[nextIndex]);
        return CurrentMode;
    }

    /// <summary>
    /// シーン遷移などで上書きされた場合に現在の設定を再適用する
    /// </summary>
    public static void RefreshCurrentSettings()
    {
        EnsureInitialized();
        ApplyToUnityEngine();
    }

    private static int NormalizeTargetFps(int fps)
    {
        return Array.IndexOf(s_selectableFpsArray, fps) >= 0 ? fps : DefaultTargetFps;
    }

    private static void ApplyToUnityEngine()
    {
        int desiredVSync = _isVSyncEnabled ? 1 : 0;
        int desiredTargetFps = _isVSyncEnabled ? -1 : _targetFps;

        if (QualitySettings.vSyncCount != desiredVSync)
        {
            QualitySettings.vSyncCount = desiredVSync;
        }
        if (Application.targetFrameRate != desiredTargetFps)
        {
            Application.targetFrameRate = desiredTargetFps;
        }
    }

    private static void ApplyCurrentSettings(bool saveToRepository, bool notifyListeners)
    {
        ApplyToUnityEngine();

        if (saveToRepository)
        {
            _repository.Save(_targetFps, _isVSyncEnabled);
        }

        if (notifyListeners)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[FrameRateManager] 設定適用: TargetFPS={GetTargetFpsDisplayString()}, VSync={GetVSyncDisplayString()} (actual targetFrameRate={Application.targetFrameRate}, vSyncCount={QualitySettings.vSyncCount})");
#endif
            OnFrameRateModeChanged?.Invoke(CurrentMode);
            OnSettingsChanged?.Invoke();
        }
    }
}
