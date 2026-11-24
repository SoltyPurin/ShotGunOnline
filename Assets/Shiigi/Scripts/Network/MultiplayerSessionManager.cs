using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// マルチプレイセッション関連機能のエントリポイント（ファサード）クラス。
/// 認証初期化・ルーム管理・ハートビート監視・シーン遷移・切断エラーハンドリングの各単一責務クラスを統合・調整します。
/// </summary>
public class MultiplayerSessionManager : MonoBehaviour, ILobbySessionService
{
    private const string GameSceneName = "MP_GameScene";
    private const float HeartbeatInterval = 2f;
    private const float HeartbeatTimeout = 10f;

    [Header("通信エラー表示設定 (未設定時はロビーのConfirmDialogUIを自動複製)")]
    [SerializeField] private ConfirmDialogUI _errorDialogPrefab;

    public static MultiplayerSessionManager Instance { get; private set; }

    private IUnityServicesInitializer _servicesInitializer;
    private ILobbySessionService _sessionService;
    private SessionHeartbeatMonitor _heartbeatMonitor;
    private NetworkSceneTransitionHandler _sceneTransitionHandler;
    private SessionDisconnectHandler _disconnectHandler;
    private bool _isDisconnecting;
    private UniTask? _disconnectTask;

    public ISession CurrentSession => _sessionService?.CurrentSession;
    public bool IsInitialized => _servicesInitializer != null && _servicesInitializer.IsInitialized;

    public event Action OnSessionChanged
    {
        add
        {
            EnsureSubsystemsInitialized();
            _sessionService.OnSessionChanged += value;
        }
        remove
        {
            if (_sessionService != null)
            {
                _sessionService.OnSessionChanged -= value;
            }
        }
    }

    public event Action OnGameStarting
    {
        add
        {
            EnsureSubsystemsInitialized();
            _sceneTransitionHandler.OnGameStarting += value;
        }
        remove
        {
            if (_sceneTransitionHandler != null)
            {
                _sceneTransitionHandler.OnGameStarting -= value;
            }
        }
    }

    private void Awake()
    {
        // シングルトンセットアップ
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSubsystemsInitialized();
            PlayerProfileStore.OnProfileChanged += HandleLocalProfileChanged;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void EnsureSubsystemsInitialized()
    {
        if (_servicesInitializer != null) return;

        _servicesInitializer = new UnityServicesInitializer();
        _sceneTransitionHandler = new NetworkSceneTransitionHandler(GameSceneName);
        _disconnectHandler = new SessionDisconnectHandler(
            transform,
            DisconnectAndCleanupAsync,
            _errorDialogPrefab);
        _sessionService = new LobbySessionService(
            _servicesInitializer,
            onSessionConnected: SubscribeNetworkEvents,
            onSessionLeaving: HandleSessionLeaving,
            onUnexpectedDisconnect: () => _disconnectHandler.NotifyUnexpectedDisconnect());
        _heartbeatMonitor = new SessionHeartbeatMonitor(
            () => _sessionService.CurrentSession,
            HeartbeatInterval,
            HeartbeatTimeout,
            onHostTimeout: () => _disconnectHandler.NotifyUnexpectedDisconnect());
    }

    private async void Start()
    {
        EnsureSubsystemsInitialized();
        _disconnectHandler?.TryCacheDialogFromScene();
        await _servicesInitializer.InitializeAsync();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            PlayerProfileStore.OnProfileChanged -= HandleLocalProfileChanged;
            UnsubscribeNetworkEvents();
            Instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        UnsubscribeNetworkEvents();
        _disconnectHandler?.NotifyApplicationQuitting();
    }

    private void Update()
    {
        _heartbeatMonitor?.Tick(Time.unscaledTime, NetworkManager.Singleton);
    }

    /// <summary>
    /// ルームおよびRelay(NetworkManager)接続が存在する場合に切断し、ネットワーク関連オブジェクトをクリーンアップする。
    /// 既にクリーンアップ中の場合は、進行中の処理が完了するまで待機してから戻る。
    /// </summary>
    public UniTask DisconnectAndCleanupAsync()
    {
        // 多重呼び出し時に即座に return すると、呼び出し側がクリーンアップ完了前にシーン遷移等を行ってしまうため進行中タスクを共有する
        if (_isDisconnecting && _disconnectTask.HasValue)
        {
            return _disconnectTask.Value;
        }

        var hasActiveSession = CurrentSession != null;
        var hasNetworkManager = NetworkManager.Singleton != null;
        if (!hasActiveSession && !hasNetworkManager) return UniTask.CompletedTask;

        _isDisconnecting = true;
        var task = DisconnectAndCleanupCoreAsync(hasActiveSession).Preserve();
        _disconnectTask = task;
        return task;
    }

    private async UniTask DisconnectAndCleanupCoreAsync(bool hasActiveSession)
    {
        try
        {
            EnsureSubsystemsInitialized();
            UnsubscribeNetworkEvents();

            if (hasActiveSession)
            {
                Debug.Log("[MSM]: ルームセッションから退出します。");
                await _sessionService.LeaveRoomSessionAsync();
            }

            // タイトル画面等では不要になったNetworkManagerを破棄し、次回ロビー入室時に新規生成されるようにする
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
                {
                    Debug.Log("[MSM]: NetworkManager(Relay)をシャットダウンします。");
                    NetworkManager.Singleton.Shutdown();
                    // NGOの内部シャットダウン完了を待ってから破棄する
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }

                if (NetworkManager.Singleton != null)
                {
                    Destroy(NetworkManager.Singleton.gameObject);
                }
            }

            Debug.Log("[MSM]: ルームおよびRelayの切断とクリーンアップが完了しました。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MSM]: 切断クリーンアップ処理中にエラーが発生しました: {ex.Message}");
        }
        finally
        {
            _isDisconnecting = false;
            _disconnectTask = null;
        }
    }

    private void SubscribeNetworkEvents()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null) return;

        _sceneTransitionHandler?.SubscribeEvents(networkManager);
        _heartbeatMonitor?.SubscribeMessaging(networkManager.CustomMessagingManager);
        _disconnectHandler?.SubscribeEvents(networkManager);
    }

    private void UnsubscribeNetworkEvents()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null) return;

        _sceneTransitionHandler?.UnsubscribeEvents(networkManager);
        _heartbeatMonitor?.UnsubscribeMessaging(networkManager.CustomMessagingManager);
        _disconnectHandler?.UnsubscribeEvents(networkManager);
    }

    private void HandleSessionLeaving()
    {
        UnsubscribeNetworkEvents();
        _heartbeatMonitor?.Reset();
    }

    /// <summary>
    /// ホストとして新しいルームを作成する
    /// </summary>
    public UniTask<bool> CreateRoomSessionAsync(string roomName, int maxPlayers, RelayProtocol protocol = RelayProtocol.DTLS)
    {
        EnsureSubsystemsInitialized();
        return _sessionService.CreateRoomSessionAsync(roomName, maxPlayers, protocol);
    }

    /// <summary>
    /// 公開されているルームを検索
    /// </summary>
    public UniTask<List<ISessionInfo>> SearchRoomSessionsAsync()
    {
        EnsureSubsystemsInitialized();
        return _sessionService.SearchRoomSessionsAsync();
    }

    /// <summary>
    /// 指定したセッションに参加する
    /// </summary>
    public UniTask<bool> JoinRoomSessionAsync(ISessionInfo targetRoom)
    {
        EnsureSubsystemsInitialized();
        return _sessionService.JoinRoomSessionAsync(targetRoom);
    }

    /// <summary>
    /// 現在参加しているルームから退出する
    /// </summary>
    public UniTask LeaveRoomSessionAsync()
    {
        EnsureSubsystemsInitialized();
        return _sessionService.LeaveRoomSessionAsync();
    }

    /// <summary>
    /// 準備完了ステータスを更新する
    /// </summary>
    public UniTask SetReadyStatusAsync(bool isReady)
    {
        EnsureSubsystemsInitialized();
        return _sessionService.SetReadyStatusAsync(isReady);
    }

    /// <summary>
    /// 現在のセッションにローカルプレイヤー名を反映する
    /// </summary>
    public UniTask UpdatePlayerNameAsync(string playerName)
    {
        EnsureSubsystemsInitialized();
        return _sessionService.UpdatePlayerNameAsync(playerName);
    }

    private void HandleLocalProfileChanged(PlayerProfile profile)
    {
        if (profile == null || CurrentSession == null) return;
        UpdatePlayerNameAsync(profile.PlayerName).Forget();
    }

    /// <summary>
    /// セッションのゲーム状態（待機中 / 試合中）を更新する
    /// </summary>
    public UniTask UpdateSessionGameStatusAsync(bool isInGame)
    {
        EnsureSubsystemsInitialized();
        return _sessionService.UpdateSessionGameStatusAsync(isInGame);
    }

    /// <summary>
    /// ホストがゲームを開始する
    /// </summary>
    public void StartMultiplayGame()
    {
        EnsureSubsystemsInitialized();
        _sessionService.UpdateSessionGameStatusAsync(true).Forget();
        _sceneTransitionHandler.StartMultiplayGame(NetworkManager.Singleton);
    }
}
