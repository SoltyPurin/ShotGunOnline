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
    private const string GameStartingMessageName = "GameStarting";

    public static MultiplayerSessionManager Instance { get; private set; }

    private ISession _currentSession;
    private bool _gameStartingHandlerRegistered;
    private NetworkManager _networkManagerForHandler;
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

    private void RegisterGameStartingMessage()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogWarning("[MSM] NetworkManager がまだ利用できないため、開始通知を登録できません。");
            return;
        }

        if (_gameStartingHandlerRegistered)
            return;

        if (networkManager.CustomMessagingManager == null)
        {
            if (_networkManagerForHandler != networkManager)
            {
                if (_networkManagerForHandler != null)
                    _networkManagerForHandler.OnClientConnectedCallback -= OnNetworkClientConnected;

                _networkManagerForHandler = networkManager;
                _networkManagerForHandler.OnClientConnectedCallback += OnNetworkClientConnected;
            }

            Debug.Log("[MSM] CustomMessagingManager の初期化待ちです。");
            return;
        }

        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
            GameStartingMessageName,
            OnGameStartingMessageReceived);
        _gameStartingHandlerRegistered = true;
        networkManager.OnClientConnectedCallback -= OnNetworkClientConnected;
        _networkManagerForHandler = null;
        Debug.Log("[MSM] ゲーム開始通知ハンドラーを登録しました。");
    }

    private void OnNetworkClientConnected(ulong clientId)
    {
        RegisterGameStartingMessage();
    }

    private void OnGameStartingMessageReceived(ulong senderClientId, FastBufferReader reader)
    {
        Debug.Log($"[MSM] ゲーム開始通知を受信しました。送信元: {senderClientId}");
        OnGameStarting?.Invoke();
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
            // オプション作成
            var options = new SessionOptions
            {
                Name = roomName,
                MaxPlayers = maxPlayers,
                IsPrivate = false
            };

            _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            _currentSession.Changed += HandleSessionChanged;

            // ホストとしてゲーム開始
            RegisterGameStartingMessage();
            NetworkManager.Singleton.StartHost();
            RegisterGameStartingMessage();
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

            RegisterGameStartingMessage();
            NetworkManager.Singleton.StartClient();
            RegisterGameStartingMessage();
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

    public void StartMultiplayGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("ゲームを開始");

            using (var writer = new FastBufferWriter(1, Allocator.Temp))
            {
                Debug.Log("[MSM] クライアントへゲーム開始通知を送信します。");
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                    GameStartingMessageName,
                    writer,
                    NetworkDelivery.ReliableSequenced);
            }

            StartCoroutine(LoadGameSceneAfterNotification());
        }
    }

    private IEnumerator LoadGameSceneAfterNotification()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MP_Honpen", LoadSceneMode.Single);
        }
    }
}
