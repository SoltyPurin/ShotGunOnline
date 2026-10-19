using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// マルチプレイセッション関連機能のエントリポイント（ファサード）クラス。
/// 認証初期化・ルーム管理・ハートビート監視・シーン遷移の各単一責務クラスを統合・調整します。
/// </summary>
public class MultiplayerSessionManager : MonoBehaviour, ILobbySessionService
{
    private const string GameSceneName = "MP_GameScene";
    private const float HeartbeatInterval = 2f;
    private const float HeartbeatTimeout = 10f;

    public static MultiplayerSessionManager Instance { get; private set; }

    private IUnityServicesInitializer _servicesInitializer;
    private ILobbySessionService _sessionService;
    private SessionHeartbeatMonitor _heartbeatMonitor;
    private NetworkSceneTransitionHandler _sceneTransitionHandler;

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
        _sessionService = new LobbySessionService(
            _servicesInitializer,
            onSessionConnected: SubscribeNetworkEvents,
            onSessionLeaving: HandleSessionLeaving);
        _heartbeatMonitor = new SessionHeartbeatMonitor(
            () => _sessionService.CurrentSession,
            HeartbeatInterval,
            HeartbeatTimeout);
    }

    private async void Start()
    {
        EnsureSubsystemsInitialized();
        await _servicesInitializer.InitializeAsync();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnsubscribeNetworkEvents();
        }
    }

    private void Update()
    {
        _heartbeatMonitor?.Tick(Time.unscaledTime, NetworkManager.Singleton);
    }

    private void SubscribeNetworkEvents()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null) return;

        _sceneTransitionHandler?.SubscribeEvents(networkManager);
        _heartbeatMonitor?.SubscribeMessaging(networkManager.CustomMessagingManager);
    }

    private void UnsubscribeNetworkEvents()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null) return;

        _sceneTransitionHandler?.UnsubscribeEvents(networkManager);
        _heartbeatMonitor?.UnsubscribeMessaging(networkManager.CustomMessagingManager);
    }

    private void HandleSessionLeaving()
    {
        UnsubscribeNetworkEvents();
        _heartbeatMonitor?.Reset();
    }

    /// <summary>
    /// ホストとして新しいルームを作成する
    /// </summary>
    public UniTask<bool> CreateRoomSessionAsync(string roomName, int maxPlayers)
    {
        EnsureSubsystemsInitialized();
        return _sessionService.CreateRoomSessionAsync(roomName, maxPlayers);
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
    /// ホストがゲームを開始する
    /// </summary>
    public void StartMultiplayGame()
    {
        EnsureSubsystemsInitialized();
        _sceneTransitionHandler.StartMultiplayGame(NetworkManager.Singleton);
    }
}
