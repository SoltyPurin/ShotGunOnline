using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity Editor / Standalone のフリーズ状況をリアルタイムでテキストファイルに強制記録する診断クラス
/// 自動起動し、メインスレッド停止（フリーズ）を別スレッドで検知してtxtに出力します。
/// </summary>
public class FreezeWatchdogLogger : MonoBehaviour
{
    private static FreezeWatchdogLogger _instance;
    private static string _realtimeLogPath;
    private static string _freezeDumpPath;
    private static StreamWriter _logWriter;
    private static readonly object _fileLock = new object();

    // 直近のログリングバッファ
    private static readonly Queue<string> _recentLogs = new Queue<string>();
    private const int MAX_RECENT_LOGS = 60;

    // ウォッチドッグ用スレッド
    private static Thread _watchdogThread;
    private static volatile bool _isRunning = false;
    private static long _lastHeartbeatTicks = 0;
    private static readonly double FREEZE_THRESHOLD_SECONDS = 1.5;
    private static bool _freezeDumped = false;

    // メインスレッドの情報
    private static string _currentSceneName = "Unknown";
    private static int _currentFrame = 0;
    private static float _currentTimeScale = 1.0f;
    private static string _lastCheckpoint = "Initialized";
    private static volatile string _networkStatusSummary = "[NetworkManager] Singleton is NULL";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        return;
#endif
        if (_instance != null) return;

        string projectRoot = Directory.GetCurrentDirectory();
        _realtimeLogPath = Path.Combine(projectRoot, "EditorRealtimeLog.txt");
        _freezeDumpPath = Path.Combine(projectRoot, "FreezeDump.txt");

        try
        {
            // 起動時に既存ログをリセット
            File.WriteAllText(_realtimeLogPath, $"=== Session Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===\n", Encoding.UTF8);
            File.WriteAllText(_freezeDumpPath, $"=== Freeze Watchdog Initialized at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===\n", Encoding.UTF8);

            _logWriter = new StreamWriter(new FileStream(_realtimeLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8)
            {
                AutoFlush = true
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FreezeWatchdog] ログファイル作成失敗: {ex.Message}");
        }

        // ログコールバック登録（マルチスレッド対応・バッファリングなし即時書き込み）
        Application.logMessageReceivedThreaded += OnLogReceived;

        // 常駐GameObject生成
        GameObject go = new GameObject("[FreezeWatchdogLogger]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<FreezeWatchdogLogger>();

        // ウォッチドッグ監視スレッド開始
        _isRunning = true;
        _lastHeartbeatTicks = DateTime.UtcNow.Ticks;
        _watchdogThread = new Thread(WatchdogLoop)
        {
            IsBackground = true,
            Name = "FreezeWatchdogThread"
        };
        _watchdogThread.Start();

        Debug.Log($"[FreezeWatchdog] 監視開始: ログ出力先 => {_realtimeLogPath}");
    }

    private static void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string entry = $"[{timestamp}][{type}] {condition}";

        lock (_fileLock)
        {
            if (_recentLogs.Count >= MAX_RECENT_LOGS)
            {
                _recentLogs.Dequeue();
            }
            _recentLogs.Enqueue(entry);

            if (_logWriter != null)
            {
                try
                {
                    _logWriter.WriteLine(entry);
                    if (type == LogType.Exception || type == LogType.Error || !string.IsNullOrEmpty(stackTrace))
                    {
                        _logWriter.WriteLine(stackTrace);
                    }
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// コードの各重要ポイントで通過チェックポイントを記録できるメソッド
    /// </summary>
    public static void Checkpoint(string tag)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _lastCheckpoint = tag;
        LogDirect($"[Checkpoint] {tag}");
#endif
    }

    private static void LogDirect(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string line = $"[{timestamp}][TRACE] {message}";
        lock (_fileLock)
        {
            _logWriter?.WriteLine(line);
        }
    }

    private void Update()
    {
        // メインスレッド生存確認（毎フレーム更新）
        Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);

        _currentFrame = Time.frameCount;
        _currentTimeScale = Time.timeScale;
        _currentSceneName = SceneManager.GetActiveScene().name;

        var netMgr = NetworkManager.Singleton;
        if (netMgr != null)
        {
            _networkStatusSummary =
                $"[NetworkManager] IsListening: {netMgr.IsListening}, IsServer: {netMgr.IsServer}, IsClient: {netMgr.IsClient}\n" +
                $"[NetworkManager] ConnectedClients: {netMgr.ConnectedClientsList.Count}";
        }
        else
        {
            _networkStatusSummary = "[NetworkManager] Singleton is NULL";
        }

        // 一度フリーズダンプを出力したが復帰した場合
        if (_freezeDumped)
        {
            _freezeDumped = false;
            LogDirect($"[FreezeWatchdog] ★★★ メインスレッドが復帰しました (Frame: {_currentFrame}) ★★★");
        }
    }

    private void LateUpdate()
    {
        Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
    }

    private static void WatchdogLoop()
    {
        while (_isRunning)
        {
            Thread.Sleep(200);

            long lastTicks = Interlocked.Read(ref _lastHeartbeatTicks);
            double elapsedSeconds = (DateTime.UtcNow.Ticks - lastTicks) / (double)TimeSpan.TicksPerSecond;

            if (elapsedSeconds >= FREEZE_THRESHOLD_SECONDS && !_freezeDumped)
            {
                _freezeDumped = true;
                WriteFreezeDump(elapsedSeconds);
            }
        }
    }

    private static void WriteFreezeDump(double frozenDuration)
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"【FREEZE DETECTED】メインスレッドの停止を検知しました！ (停止時間: {frozenDuration:F2}秒)");
            sb.AppendLine($"検知時刻: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"アクティブシーン: {_currentSceneName}");
            sb.AppendLine($"フレーム数: {_currentFrame}");
            sb.AppendLine($"Time.timeScale: {_currentTimeScale}");
            sb.AppendLine($"最終チェックポイント: {_lastCheckpoint}");
            sb.AppendLine("================================================================================");

            // ネットワーク状態（メインスレッドでキャッシュした値を出力）
            sb.AppendLine(_networkStatusSummary);

            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("【直前に出力されたログ（最新 30 件）】");
            lock (_fileLock)
            {
                foreach (var log in _recentLogs)
                {
                    sb.AppendLine(log);
                }
            }
            sb.AppendLine("================================================================================");

            File.WriteAllText(_freezeDumpPath, sb.ToString(), Encoding.UTF8);

            // リアルタイムログ側にも書き込み
            LogDirect($"★★★ 【FREEZE DUMP GENERATED】停止時間: {frozenDuration:F2}秒 (詳細は FreezeDump.txt を参照) ★★★");
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(_freezeDumpPath, $"FreezeDump 書き込み例外: {ex}", Encoding.UTF8);
            }
            catch { }
        }
    }

    private void OnDestroy()
    {
        _isRunning = false;
        Application.logMessageReceivedThreaded -= OnLogReceived;
        if (_logWriter != null)
        {
            _logWriter.Flush();
            _logWriter.Close();
            _logWriter = null;
        }
    }

    private void OnApplicationQuit()
    {
        _isRunning = false;
        Application.logMessageReceivedThreaded -= OnLogReceived;
        if (_logWriter != null)
        {
            _logWriter.Flush();
            _logWriter.Close();
            _logWriter = null;
        }
    }
}
