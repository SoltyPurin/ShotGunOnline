using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// メインスレッドのフリーズを別スレッドで検知し、ログをテキスト出力する診断コンポーネント。
/// </summary>
public class FreezeWatchdogLogger : MonoBehaviour
{
    /// <summary>
    /// コードの各重要ポイントで通過チェックポイントを記録できるメソッド（本番ビルドでは空スタブ）
    /// </summary>
    public static void Checkpoint(string tag)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _lastCheckpoint = tag;
        LogDirect($"[Checkpoint] {tag}");
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static FreezeWatchdogLogger _instance;
    private static string _realtimeLogPath;
    private static string _freezeDumpPath;
    private static StreamWriter _logWriter;
    private static readonly object _fileLock = new object();

    // 直近のログリングバッファ
    private static readonly Queue<string> _recentLogs = new Queue<string>(MaxRecentLogs);
    private const int MaxRecentLogs = 60;

    // ウォッチドッグ用スレッド
    private static Thread _watchdogThread;
    private static volatile bool _isRunning;
    private static long _lastHeartbeatTicks;
    private const double FreezeThresholdSeconds = 1.5;
    private static volatile bool _freezeDumped;

    // メインスレッドのプリミティブ状態スナップショット（毎フレームの文字列アロケーションを回避）
    private static volatile string _currentSceneName = "Unknown";
    private static volatile int _currentFrame;
    private static volatile float _currentTimeScale = 1.0f;
    private static volatile string _lastCheckpoint = "Initialized";

    private static volatile bool _netHasSingleton;
    private static volatile bool _netIsListening;
    private static volatile bool _netIsServer;
    private static volatile bool _netIsClient;
    private static volatile int _netConnectedCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        ShutdownWatchdog();
        _instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance != null) return;

#if UNITY_EDITOR
        string logDir = Directory.GetCurrentDirectory();
#else
        string logDir = Application.persistentDataPath;
#endif
        _realtimeLogPath = Path.Combine(logDir, "EditorRealtimeLog.txt");
        _freezeDumpPath = Path.Combine(logDir, "FreezeDump.txt");

        try
        {
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

        _freezeDumped = false;
        _currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        Application.logMessageReceivedThreaded -= OnLogReceived;
        Application.logMessageReceivedThreaded += OnLogReceived;

        GameObject go = new GameObject("[FreezeWatchdogLogger]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<FreezeWatchdogLogger>();

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

    private static void OnActiveSceneChanged(Scene previous, Scene next)
    {
        _currentSceneName = string.IsNullOrEmpty(next.name) ? "Unknown" : next.name;
    }

    private static void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string entry = $"[{timestamp}][{type}] {condition}";

        lock (_fileLock)
        {
            if (_recentLogs.Count >= MaxRecentLogs)
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
                catch
                {
                    // ログストリーム競合・破棄時の例外は無視
                }
            }
        }
    }

    private static void LogDirect(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string line = $"[{timestamp}][TRACE] {message}";
        lock (_fileLock)
        {
            try
            {
                _logWriter?.WriteLine(line);
            }
            catch
            {
                // ストリーム破棄・I/O例外時は無視
            }
        }
    }

    private void Update()
    {
        Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);

        _currentFrame = Time.frameCount;
        _currentTimeScale = Time.timeScale;

        var netMgr = NetworkManager.Singleton;
        if (netMgr != null)
        {
            _netHasSingleton = true;
            _netIsListening = netMgr.IsListening;
            _netIsServer = netMgr.IsServer;
            _netIsClient = netMgr.IsClient;
            _netConnectedCount = netMgr.IsListening && netMgr.ConnectedClientsIds != null
                ? netMgr.ConnectedClientsIds.Count
                : 0;
        }
        else
        {
            _netHasSingleton = false;
            _netIsListening = false;
            _netIsServer = false;
            _netIsClient = false;
            _netConnectedCount = 0;
        }

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
            if (!_isRunning) break;

            long lastTicks = Interlocked.Read(ref _lastHeartbeatTicks);
            double elapsedSeconds = (DateTime.UtcNow.Ticks - lastTicks) / (double)TimeSpan.TicksPerSecond;

            if (elapsedSeconds >= FreezeThresholdSeconds && !_freezeDumped)
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
            var sb = new StringBuilder(2048);
            sb.AppendLine("================================================================================");
            sb.AppendLine($"【FREEZE DETECTED】メインスレッドの停止を検知しました！ (停止時間: {frozenDuration:F2}秒)");
            sb.AppendLine($"検知時刻: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"アクティブシーン: {_currentSceneName}");
            sb.AppendLine($"フレーム数: {_currentFrame}");
            sb.AppendLine($"Time.timeScale: {_currentTimeScale}");
            sb.AppendLine($"最終チェックポイント: {_lastCheckpoint}");
            sb.AppendLine("================================================================================");

            if (_netHasSingleton)
            {
                sb.AppendLine($"[NetworkManager] IsListening: {_netIsListening}, IsServer: {_netIsServer}, IsClient: {_netIsClient}");
                sb.AppendLine($"[NetworkManager] ConnectedClients: {_netConnectedCount}");
            }
            else
            {
                sb.AppendLine("[NetworkManager] Singleton is NULL");
            }

            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine($"【直前に出力されたログ（最新 {MaxRecentLogs} 件）】");
            lock (_fileLock)
            {
                foreach (var log in _recentLogs)
                {
                    sb.AppendLine(log);
                }
            }
            sb.AppendLine("================================================================================");

            File.WriteAllText(_freezeDumpPath, sb.ToString(), Encoding.UTF8);
            LogDirect($"★★★ 【FREEZE DUMP GENERATED】停止時間: {frozenDuration:F2}秒 (詳細は FreezeDump.txt を参照) ★★★");
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(_freezeDumpPath, $"FreezeDump 書き込み例外: {ex}", Encoding.UTF8);
            }
            catch
            {
                // 二重例外は無視
            }
        }
    }

    private static void ShutdownWatchdog()
    {
        _isRunning = false;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        Application.logMessageReceivedThreaded -= OnLogReceived;

        var thread = _watchdogThread;
        _watchdogThread = null;
        if (thread != null && thread.IsAlive && thread != Thread.CurrentThread)
        {
            try
            {
                thread.Join(500);
            }
            catch
            {
                // スレッド終了待機例外は無視
            }
        }

        lock (_fileLock)
        {
            _recentLogs.Clear();
            _freezeDumped = false;
            if (_logWriter != null)
            {
                try
                {
                    _logWriter.Flush();
                    _logWriter.Close();
                }
                catch
                {
                    // クローズ時例外は無視
                }
                finally
                {
                    _logWriter = null;
                }
            }
        }
    }

    private void OnDestroy()
    {
        ShutdownWatchdog();
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        ShutdownWatchdog();
    }
#endif
}
