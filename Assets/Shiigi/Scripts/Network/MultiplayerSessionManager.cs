using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerSessionManager : MonoBehaviour
{
    private const string GameSceneName = "MP_Honpen";
    private const string GameStartingMessageName = "GameStarting";

    public static MultiplayerSessionManager Instance { get; private set; }

    private ISession _currentSession;
    public ISession CurrentSession => _currentSession;
    public event Action OnSessionChanged;
    public event Action OnGameStarting;

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        // シングルトンセットアップ
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        try
        {
            await InitializeMultiplayerServicesAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"UnityServicesの初期化に失敗: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();
    }

    private void SubscribeNetworkEvents()
    {
        if (NetworkManager.Singleton == null) return;

        // 接続完了時にも再登録を保証
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // NGOシーンイベントの監視（フォールバック用）
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
        }

        // CustomMessageの監視（即時性用）
        if (NetworkManager.Singleton.CustomMessagingManager != null)
        {
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(GameStartingMessageName);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(GameStartingMessageName, OnGameStartingMessageReceived);
        }
    }

    private void UnsubscribeNetworkEvents()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }

        if (NetworkManager.Singleton.CustomMessagingManager != null)
        {
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(GameStartingMessageName);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        SubscribeNetworkEvents();
    }

    private void OnGameStartingMessageReceived(ulong senderClientId, FastBufferReader reader)
    {
        Debug.Log($"[MSM] ホストからのゲーム開始通知を受信しました (Sender: {senderClientId})");
        OnGameStarting?.Invoke();
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        // ホストがゲームシーンの読み込みを開始した瞬間、全クライアントにイベントが届く
        if (sceneEvent.SceneEventType == SceneEventType.Load && sceneEvent.SceneName == GameSceneName)
        {
            Debug.Log($"[MSM] ゲームシーン読み込み開始を検知しました: {sceneEvent.SceneName}");
            OnGameStarting?.Invoke();
        }
    }

    /// <summary>
    /// マルチプレイ用のUnityServicesの初期化メソッド
    /// </summary>
    /// <returns></returns>
    private async UniTask InitializeMultiplayerServicesAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unity Servicesの初期化に失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// ホストとして新しいルームを作成する
    /// </summary>
    /// <param name="roomName"></param>
    /// <param name="maxPlayers"></param>
    /// <returns></returns>
    public async UniTask<bool> CreateRoomSessionAsync(string roomName, int maxPlayers)
    {
        if (!IsInitialized)
        {
            Debug.LogError("Servicesが初期化されていません");
            return false;
        }

        try
        {
            // オプション作成（Relayを有効化）
            var options = new SessionOptions
            {
                Name = roomName,
                MaxPlayers = maxPlayers,
                IsPrivate = false
            }.WithRelayNetwork();

            _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            _currentSession.Changed += HandleSessionChanged;

            // ホストとしてゲーム開始
            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
            }
            SubscribeNetworkEvents();
            Debug.Log("[MSM]: ホストとして接続");

            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"ルーム作成失敗: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 公開されているルームを検索
    /// </summary>
    /// <returns></returns>
    public async UniTask<List<ISessionInfo>> SearchRoomSessionsAsync()
    {
        if (!IsInitialized)
        {
            Debug.LogError("Servicesが初期化されていません");
            return new List<ISessionInfo>();
        }

        try
        {
            // 検索条件作成
            var queryOptions = new QuerySessionsOptions
            {
                // TODO: 検索条件がある場合はここに追加（例えばルームの空き状況など）
            };

            QuerySessionsResults queryResponse = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            return new List<ISessionInfo>(queryResponse.Sessions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルーム検索失敗: {ex.Message}");
            return new List<ISessionInfo>();
        }
    }

    /// <summary>
    /// 指定したセッションに参加する
    /// </summary>
    /// <param name="targetRoom"></param>
    /// <returns></returns>
    public async UniTask<bool> JoinRoomSessionAsync(ISessionInfo targetRoom)
    {
        if (!IsInitialized)
        {
            Debug.Log("Servicesが初期化されていません");
            return false;
        }

        try
        {
            _currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(targetRoom.Id);
            _currentSession.Changed += HandleSessionChanged;

            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartClient();
            }
            SubscribeNetworkEvents();
            Debug.Log("[MSM]: クライアントとして接続");

            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"ルームに参加失敗: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 現在参加しているルームから退出する
    /// </summary>
    /// <returns></returns>
    public async UniTask LeaveRoomSessionAsync()
    {
        if (_currentSession == null) return;

        try
        {
            UnsubscribeNetworkEvents();
            _currentSession.Changed -= HandleSessionChanged;
            await _currentSession.LeaveAsync();
            _currentSession = null;
            
            // 接続解除
            NetworkManager.Singleton?.Shutdown();
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルーム退出時にエラーが発生しました: {ex.Message}");
        }
    }

    private void HandleSessionChanged()
    {
        OnSessionChanged?.Invoke();
    }

    /// <summary>
    /// 準備完了ステータスを更新する
    /// </summary>
    public async UniTask SetReadyStatusAsync(bool isReady)
    {
        if (_currentSession == null || _currentSession.CurrentPlayer == null)
        {
            Debug.LogError("セッションまたはプレイヤーが存在しません");
            return;
        }

        try
        {
            var value = isReady ? "true" : "false";
            var properties = new Dictionary<string, PlayerProperty>
            {
                { "IsReady", new PlayerProperty(value, VisibilityPropertyOptions.Member) }
            };

            _currentSession.CurrentPlayer.SetProperties(properties);
            await _currentSession.SaveCurrentPlayerDataAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"準備状態の更新に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// ホストがゲームを開始する
    /// </summary>
    public void StartMultiplayGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[MSM] ホストがゲームを開始: 即時通知を送信し、シーン遷移を実行します");

            // ゲーム開始を通知(クライアントを接続中ステータスに更新させる)
            if (NetworkManager.Singleton.CustomMessagingManager != null)
            {
                using (var writer = new FastBufferWriter(0, Allocator.Temp))
                {
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                        GameStartingMessageName,
                        writer,
                        NetworkDelivery.ReliableSequenced);
                }
            }

            NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }
    }
}
