using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class MultiplayerSessionManager : MonoBehaviour
{
    public static MultiplayerSessionManager Instance { get; private set; }

    private ISession _currentSession;
    public ISession CurrentSession => _currentSession;
    public event Action OnSessionChanged;

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
}
