#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// 開発ビルド (Development Build) および Unity エディタ実行時に、
/// ホストとの Ping (RTT)、パケットロス率、FPS、接続ロール等を画面に表示するデバッグオーバーレイ。
/// リリースビルド（製品版）では自動的に完全に無効化・破棄されます。
/// </summary>
[DisallowMultipleComponent]
public class NetworkDebugOverlay : MonoBehaviour
{
    public enum DisplayMode
    {
        Full,       // 詳細表示
        Mini,       // 簡易1行表示
        Hidden      // 非表示
    }

    public enum ScreenPosition
    {
        TopRight,
        TopLeft,
        BottomRight,
        BottomLeft
    }

    private const string MsgPing = "NetDebug_Ping";
    private const string MsgPong = "NetDebug_Pong";
    private const int MaxHistoryCount = 20; // パケットロスの測定対象（過去20回分 = 約10秒間）
    private const float PingInterval = 0.5f; // Ping送信間隔（秒）
    private const float PacketTimeout = 1.0f; // タイムアウト判定秒数

    private struct SentPacketInfo
    {
        public int SequenceId;
        public float SendTime;
        public float RttMs;
        public bool Received;
        public bool TimedOut;
    }

    public static NetworkDebugOverlay Instance { get; private set; }

    [Header("表示設定")]
    [Tooltip("開発ビルドまたはエディタ実行時のみ動作するか (falseにするとリリースでも動作)")]
    [SerializeField] private bool _onlyInDevelopmentBuild = true;

    [Tooltip("初期の表示モード")]
    [SerializeField] private DisplayMode _displayMode = DisplayMode.Full;

    [Tooltip("画面上の表示位置")]
    [SerializeField] private ScreenPosition _position = ScreenPosition.TopRight;

    [Tooltip("表示切り替えショートカットキー (Legacy Input使用時)")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F3;

    [Tooltip("起動時に自動で常駐オブジェクトを生成するか")]
    [SerializeField] private bool _dontDestroyOnLoad = true;

    // パケットロス & Ping 計測データ
    private int _nextSequenceId = 1;
    private float _lastPingSendTime = 0f;
    private readonly List<SentPacketInfo> _packetHistory = new(MaxHistoryCount);

    // 統計キャッシュ
    private float _appRttMs = 0f;
    private float _appRttMin = 0f;
    private float _appRttMax = 0f;
    private float _appRttAvg = 0f;
    private float _jitterMs = 0f;
    private float _packetLossPercent = 0f;
    private int _lostPacketsCount = 0;
    private int _evaluatedPacketsCount = 0;

    // FPS 計測用
    private float _fps = 0f;
    private float _fpsTimer = 0f;
    private int _fpsFrames = 0;

    // UTP 内部メトリクス取得（リフレクションキャッシュ）
    private FieldInfo _packetLossCacheField;
    private FieldInfo _packetLossValueField;
    private bool _reflectionInitialized;

    // GUI スタイルキャッシュ
    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _titleStyle;
    private bool _stylesInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        // 開発ビルドまたはエディタでなければ自動生成しない
        if (!Debug.isDebugBuild && !Application.isEditor)
        {
            return;
        }

        if (Instance == null && FindAnyObjectByType<NetworkDebugOverlay>() == null)
        {
            var go = new GameObject("[NetworkDebugOverlay]");
            var overlay = go.AddComponent<NetworkDebugOverlay>();
            if (overlay._dontDestroyOnLoad)
            {
                DontDestroyOnLoad(go);
            }
        }
    }

    private void Awake()
    {
        // 開発ビルド限定チェック
        if (_onlyInDevelopmentBuild && !Debug.isDebugBuild && !Application.isEditor)
        {
            Destroy(gameObject);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_dontDestroyOnLoad && transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        InitUtpReflection();
    }

    private void OnEnable()
    {
        RegisterMessageHandlers();
    }

    private void OnDisable()
    {
        UnregisterMessageHandlers();
    }

    private void Update()
    {
        UpdateFps();
        HandleInput();

        var netManager = NetworkManager.Singleton;
        if (netManager == null || !netManager.IsListening)
        {
            return;
        }

        // メッセージハンドラが未登録なら登録を試みる（NetworkManager起動遅延対応）
        EnsureHandlersRegistered();

        // クライアント側からホストへのPing送信 & パケットロス監視
        if (netManager.IsClient && !netManager.IsHost)
        {
            UpdatePingAndPacketLoss();
        }
    }

    private void HandleInput()
    {
        bool keyPressed = false;

#if ENABLE_INPUT_SYSTEM
        try
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
            {
                keyPressed = true;
            }
        }
        catch
        {
            // Input System 未初期化時の安全策
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (!keyPressed && Input.GetKeyDown(_toggleKey))
        {
            keyPressed = true;
        }
#endif

        if (keyPressed)
        {
            CycleDisplayMode();
        }
    }

    public void CycleDisplayMode()
    {
        _displayMode = _displayMode switch
        {
            DisplayMode.Full => DisplayMode.Mini,
            DisplayMode.Mini => DisplayMode.Hidden,
            _ => DisplayMode.Full
        };
    }

    public void SetDisplayMode(DisplayMode mode)
    {
        _displayMode = mode;
    }

    private void UpdateFps()
    {
        _fpsFrames++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 0.5f)
        {
            _fps = _fpsFrames / _fpsTimer;
            _fpsFrames = 0;
            _fpsTimer = 0f;
        }
    }

    #region Ping & パケットロス計測ロジック

    private bool _handlersRegistered = false;

    private void EnsureHandlersRegistered()
    {
        if (!_handlersRegistered && NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
        {
            RegisterMessageHandlers();
        }
    }

    private void RegisterMessageHandlers()
    {
        var netManager = NetworkManager.Singleton;
        if (netManager == null || netManager.CustomMessagingManager == null || _handlersRegistered) return;

        try
        {
            netManager.CustomMessagingManager.RegisterNamedMessageHandler(MsgPing, OnReceivePingOnServer);
            netManager.CustomMessagingManager.RegisterNamedMessageHandler(MsgPong, OnReceivePongOnClient);
            _handlersRegistered = true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[NetworkDebugOverlay] メッセージハンドラ登録待機中: {ex.Message}");
        }
    }

    private void UnregisterMessageHandlers()
    {
        var netManager = NetworkManager.Singleton;
        if (netManager == null || netManager.CustomMessagingManager == null || !_handlersRegistered) return;

        try
        {
            netManager.CustomMessagingManager.UnregisterNamedMessageHandler(MsgPing);
            netManager.CustomMessagingManager.UnregisterNamedMessageHandler(MsgPong);
        }
        catch
        {
            // 終了時などの例外は無視
        }
        finally
        {
            _handlersRegistered = false;
        }
    }

    /// <summary>
    /// ホスト側: クライアントからのPingを受信し、即座にPongを返信
    /// </summary>
    private void OnReceivePingOnServer(ulong senderClientId, FastBufferReader reader)
    {
        if (!reader.TryBeginRead(sizeof(int) + sizeof(float))) return;

        reader.ReadValueSafe(out int sequenceId);
        reader.ReadValueSafe(out float clientSendTime);

        // 送信元クライアントへオウム返し (Unreliableで軽量送信)
        using var writer = new FastBufferWriter(sizeof(int) + sizeof(float), Allocator.Temp);
        writer.WriteValueSafe(sequenceId);
        writer.WriteValueSafe(clientSendTime);

        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
            MsgPong,
            senderClientId,
            writer,
            NetworkDelivery.Unreliable
        );
    }

    /// <summary>
    /// クライアント側: ホストからのPongを受信し、往復時間を記録
    /// </summary>
    private void OnReceivePongOnClient(ulong senderClientId, FastBufferReader reader)
    {
        if (!reader.TryBeginRead(sizeof(int) + sizeof(float))) return;

        reader.ReadValueSafe(out int sequenceId);
        reader.ReadValueSafe(out float clientSendTime);

        float now = Time.realtimeSinceStartup;
        float rtt = (now - clientSendTime) * 1000f; // ms

        for (int i = 0; i < _packetHistory.Count; i++)
        {
            var p = _packetHistory[i];
            if (p.SequenceId == sequenceId && !p.Received)
            {
                p.Received = true;
                p.RttMs = rtt;
                _packetHistory[i] = p;
                break;
            }
        }

        RecalculateStats();
    }

    private void UpdatePingAndPacketLoss()
    {
        float now = Time.realtimeSinceStartup;

        // 定期的なPing送信
        if (now - _lastPingSendTime >= PingInterval)
        {
            _lastPingSendTime = now;
            SendPingToHost(now);
        }

        // タイムアウトチェック
        bool statsDirty = false;
        for (int i = 0; i < _packetHistory.Count; i++)
        {
            var p = _packetHistory[i];
            if (!p.Received && !p.TimedOut && (now - p.SendTime) > PacketTimeout)
            {
                p.TimedOut = true;
                _packetHistory[i] = p;
                statsDirty = true;
            }
        }

        if (statsDirty)
        {
            RecalculateStats();
        }
    }

    private void SendPingToHost(float sendTime)
    {
        var netManager = NetworkManager.Singleton;
        if (netManager == null || !netManager.IsConnectedClient || netManager.CustomMessagingManager == null) return;

        int seq = _nextSequenceId++;

        // 履歴追加 (最大件数を超えたら古いものを削除)
        if (_packetHistory.Count >= MaxHistoryCount)
        {
            _packetHistory.RemoveAt(0);
        }

        _packetHistory.Add(new SentPacketInfo
        {
            SequenceId = seq,
            SendTime = sendTime,
            RttMs = 0f,
            Received = false,
            TimedOut = false
        });

        using var writer = new FastBufferWriter(sizeof(int) + sizeof(float), Allocator.Temp);
        writer.WriteValueSafe(seq);
        writer.WriteValueSafe(sendTime);

        netManager.CustomMessagingManager.SendNamedMessage(
            MsgPing,
            NetworkManager.ServerClientId,
            writer,
            NetworkDelivery.Unreliable
        );
    }

    private void RecalculateStats()
    {
        if (_packetHistory.Count == 0) return;

        int totalEvaluated = 0;
        int lost = 0;
        float rttSum = 0f;
        int receivedCount = 0;
        float minRtt = float.MaxValue;
        float maxRtt = float.MinValue;
        float lastRtt = _appRttMs;

        for (int i = 0; i < _packetHistory.Count; i++)
        {
            var p = _packetHistory[i];
            if (p.Received)
            {
                totalEvaluated++;
                receivedCount++;
                rttSum += p.RttMs;
                if (p.RttMs < minRtt) minRtt = p.RttMs;
                if (p.RttMs > maxRtt) maxRtt = p.RttMs;
                lastRtt = p.RttMs;
            }
            else if (p.TimedOut)
            {
                totalEvaluated++;
                lost++;
            }
        }

        _evaluatedPacketsCount = totalEvaluated;
        _lostPacketsCount = lost;
        _packetLossPercent = totalEvaluated > 0 ? ((float)lost / totalEvaluated) * 100f : 0f;

        if (receivedCount > 0)
        {
            float avg = rttSum / receivedCount;
            _appRttMs = lastRtt;
            _appRttAvg = avg;
            _appRttMin = minRtt;
            _appRttMax = maxRtt;
            _jitterMs = Mathf.Abs(lastRtt - avg);
        }
    }

    #endregion

    #region UTP リフレクション (内部パケットロス値取得)

    private void InitUtpReflection()
    {
        try
        {
            var transportType = typeof(UnityTransport);
            _packetLossCacheField = transportType.GetField("m_PacketLossCache", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_packetLossCacheField != null)
            {
                var cacheType = _packetLossCacheField.FieldType;
                _packetLossValueField = cacheType.GetField("PacketLoss", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            _reflectionInitialized = true;
        }
        catch
        {
            _reflectionInitialized = false;
        }
    }

    private float? GetUtpPacketLossPercent()
    {
        if (!_reflectionInitialized || _packetLossCacheField == null || _packetLossValueField == null) return null;

        var netManager = NetworkManager.Singleton;
        if (netManager == null) return null;

        var utp = netManager.NetworkConfig?.NetworkTransport as UnityTransport;
        if (utp == null) return null;

        try
        {
            var cache = _packetLossCacheField.GetValue(utp);
            if (cache != null)
            {
                var val = _packetLossValueField.GetValue(cache);
                if (val is float f)
                {
                    return f * 100f; // 0.05 -> 5.0%
                }
            }
        }
        catch
        {
            // 例外発生時は無視
        }

        return null;
    }

    #endregion

    #region OnGUI 描画

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        // 背景ボックススタイル (半透明の黒)
        _boxStyle = new GUIStyle(GUI.skin.box);
        var bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.1f, 0.88f));
        bgTex.Apply();
        _boxStyle.normal.background = bgTex;
        _boxStyle.padding = new RectOffset(10, 10, 8, 8);

        // ラベルスタイル
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            richText = true,
            alignment = TextAnchor.MiddleLeft
        };
        _labelStyle.normal.textColor = Color.white;

        // タイトルスタイル
        _titleStyle = new GUIStyle(_labelStyle)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        // ボタンスタイル
        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
        };

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (_displayMode == DisplayMode.Hidden) return;

        InitStyles();

        // 画面スケーリング（高DPIや4Kでも見やすく）
        float scale = Mathf.Max(1.0f, Screen.dpi / 160f);
        if (Screen.dpi <= 0) scale = Mathf.Clamp(Screen.height / 1080f, 1.0f, 2.0f);

        var netManager = NetworkManager.Singleton;
        bool isListening = netManager != null && netManager.IsListening;
        bool isHost = netManager != null && netManager.IsHost;
        bool isServer = netManager != null && netManager.IsServer && !isHost;
        bool isClient = netManager != null && netManager.IsClient && !isHost;

        // トランスポート層 RTT (NGO標準API)
        ulong transportRtt = 0;
        if (isListening && netManager.NetworkConfig?.NetworkTransport != null)
        {
            transportRtt = netManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
        }

        float displayRtt = isClient ? (_appRttMs > 0 ? _appRttMs : transportRtt) : 0;
        float utpLoss = GetUtpPacketLossPercent() ?? 0f;
        float displayLoss = isClient ? Mathf.Max(_packetLossPercent, utpLoss) : 0f;

        if (_displayMode == DisplayMode.Mini)
        {
            DrawMiniOverlay(isListening, isHost, isServer, isClient, displayRtt, displayLoss, scale);
        }
        else if (_displayMode == DisplayMode.Full)
        {
            DrawFullOverlay(netManager, isListening, isHost, isServer, isClient, transportRtt, displayRtt, displayLoss, scale);
        }
    }

    private void DrawMiniOverlay(bool isListening, bool isHost, bool isServer, bool isClient, float rtt, float loss, float scale)
    {
        float width = 340f * scale;
        float height = 30f * scale;
        var rect = CalculateRect(width, height);

        GUILayout.BeginArea(rect, _boxStyle);
        GUILayout.BeginHorizontal();

        string statusText = !isListening ? "<color=#888888>Offline</color>" :
                            isHost ? "<color=#4CAF50>Host</color>" :
                            isServer ? "<color=#2196F3>Server</color>" : "<color=#00E5FF>Client</color>";

        string pingColor = GetPingColorTag(rtt);
        string lossColor = GetLossColorTag(loss);

        string text = isHost ? $"{statusText} | <color=#AAAAAA>0ms</color> | <b>{_fps:F0}</b> FPS" :
                      isClient ? $"{statusText} | Ping: <color={pingColor}><b>{rtt:F0}ms</b></color> | Loss: <color={lossColor}><b>{loss:F1}%</b></color> | <b>{_fps:F0}</b> FPS" :
                      $"{statusText} | <b>{_fps:F0}</b> FPS";

        GUILayout.Label(text, _labelStyle);

        if (GUILayout.Button("Full [F3]", _buttonStyle, GUILayout.Width(65 * scale), GUILayout.Height(18 * scale)))
        {
            CycleDisplayMode();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawFullOverlay(NetworkManager netManager, bool isListening, bool isHost, bool isServer, bool isClient, ulong transportRtt, float appRtt, float loss, float scale)
    {
        float width = 300f * scale;
        float height = isHost ? 260f * scale : 230f * scale;
        var rect = CalculateRect(width, height);

        GUILayout.BeginArea(rect, _boxStyle);

        // ヘッダー (タイトル & 切り替えボタン)
        GUILayout.BeginHorizontal();
        GUILayout.Label("🌐 <b>Network Debug</b>", _titleStyle);
        if (GUILayout.Button("Mini [F3]", _buttonStyle, GUILayout.Width(65 * scale), GUILayout.Height(20 * scale)))
        {
            CycleDisplayMode();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4 * scale);

        // ロール & FPS
        string roleStr = !isListening ? "<color=#888888>● Disconnected</color>" :
                         isHost ? "<color=#4CAF50>● Host (Server+Client)</color>" :
                         isServer ? "<color=#2196F3>● Dedicated Server</color>" : "<color=#00E5FF>● Client</color>";

        GUILayout.Label($"Role: {roleStr}", _labelStyle);
        GUILayout.Label($"FPS:  <b>{_fps:F1}</b> ({1000f / Mathf.Max(1f, _fps):F1} ms)", _labelStyle);

        if (!isListening)
        {
            GUILayout.Label("<color=#888888>NetworkManager is not listening.</color>", _labelStyle);
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label($"Local Client ID: <b>{netManager.LocalClientId}</b>", _labelStyle);

        // クライアント時のホスト通信情報
        if (isClient)
        {
            GUILayout.Space(4 * scale);
            string pingColor = GetPingColorTag(appRtt);
            string lossColor = GetLossColorTag(loss);

            GUILayout.Label($"Host Ping (RTT): <color={pingColor}><b>{appRtt:F1} ms</b></color> (UTP: {transportRtt}ms)", _labelStyle);
            GUILayout.Label($"  Min: {(_appRttMin > 9999 ? 0 : _appRttMin):F0}ms / Max: {_appRttMax:F0}ms / Jitter: ±{_jitterMs:F1}ms", _labelStyle);
            GUILayout.Label($"Packet Loss: <color={lossColor}><b>{loss:F1} %</b></color> ({_lostPacketsCount}/{_evaluatedPacketsCount} lost)", _labelStyle);

            var utpLossVal = GetUtpPacketLossPercent();
            if (utpLossVal.HasValue)
            {
                GUILayout.Label($"  (UTP Transport Loss: {utpLossVal.Value:F1}%)", _labelStyle);
            }
        }
        // ホスト時のクライアント接続一覧
        else if (isHost)
        {
            GUILayout.Space(4 * scale);
            var clients = netManager.ConnectedClientsList;
            GUILayout.Label($"Connected Clients: <b>{clients.Count}</b>", _labelStyle);

            int displayed = 0;
            foreach (var client in clients)
            {
                if (client.ClientId == netManager.LocalClientId) continue; // ホスト自身はスキップ

                ulong clientRtt = netManager.NetworkConfig?.NetworkTransport != null
                    ? netManager.NetworkConfig.NetworkTransport.GetCurrentRtt(client.ClientId)
                    : 0;

                string clientPingColor = GetPingColorTag(clientRtt);
                GUILayout.Label($"  Client [{client.ClientId}]: <color={clientPingColor}><b>{clientRtt} ms</b></color>", _labelStyle);

                displayed++;
                if (displayed >= 4)
                {
                    GUILayout.Label($"  ... and {clients.Count - 1 - displayed} more", _labelStyle);
                    break;
                }
            }

            if (displayed == 0)
            {
                GUILayout.Label("  <color=#888888>No other clients connected.</color>", _labelStyle);
            }
        }

        GUILayout.EndArea();
    }

    private Rect CalculateRect(float width, float height)
    {
        const float margin = 12f;
        return _position switch
        {
            ScreenPosition.TopRight => new Rect(Screen.width - width - margin, margin, width, height),
            ScreenPosition.TopLeft => new Rect(margin, margin, width, height),
            ScreenPosition.BottomRight => new Rect(Screen.width - width - margin, Screen.height - height - margin, width, height),
            ScreenPosition.BottomLeft => new Rect(margin, Screen.height - height - margin, width, height),
            _ => new Rect(Screen.width - width - margin, margin, width, height)
        };
    }

    private string GetPingColorTag(float pingMs)
    {
        if (pingMs <= 50f) return "#4CAF50"; // 緑 (良好)
        if (pingMs <= 100f) return "#FFEB3B"; // 黄 (普通)
        if (pingMs <= 180f) return "#FF9800"; // 橙 (やや重い)
        return "#F44336";                    // 赤 (警告)
    }

    private string GetLossColorTag(float lossPercent)
    {
        if (lossPercent <= 0.01f) return "#4CAF50"; // 緑 (ロスなし)
        if (lossPercent <= 2.0f) return "#FFEB3B";  // 黄 (わずかにロス)
        if (lossPercent <= 5.0f) return "#FF9800";  // 橙 (要注意)
        return "#F44336";                          // 赤 (深刻なパケットロス)
    }

    #endregion
}
#else
// リリースビルド用の空実装。
// コードサイズやパフォーマンスへの影響をなくす
using UnityEngine;

public class NetworkDebugOverlay : MonoBehaviour
{
    private void Awake()
    {
        Destroy(gameObject);
    }
}
#endif
