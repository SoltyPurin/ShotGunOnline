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
            Debug.Log("Unity Services初期化中...");
            await UnityServices.InitializeAsync();

            Debug.Log("Authentication (匿名サインイン)...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"サインイン成功: プレイヤーID = {AuthenticationService.Instance.PlayerId}");
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
    public async UniTask<bool> CreateRoomSessionAsync(string roomName, int maxPlayers = 4)
    {
        if (!IsInitialized)
        {
            Debug.LogError("Servicesが初期化されていません");
            return false;
        }

        try
        {
            Debug.Log($"ルーム作成開始: {roomName} (最大 {maxPlayers} 人)");

            // オプション作成
            var options = new SessionOptions
            {
                Name = roomName,
                MaxPlayers = maxPlayers,
                IsPrivate = false
            };

            _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
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
            Debug.Log("ルーム検索中...");

            // 検索条件作成
            var queryOptions = new QuerySessionsOptions
            {
                // TODO: 検索条件がある場合はここに追加（例えばルームの空き状況など）
            };

            QuerySessionsResults queryResponse = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            Debug.Log($"ルーム検索完了: {queryResponse.Sessions.Count}");
            return (List<ISessionInfo>)queryResponse.Sessions;
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
            Debug.Log($"ルームに参加中: {targetRoom.Name} (ID: {targetRoom.Id})");
            _currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(targetRoom.Id);

            Debug.Log($"ルームに参加成功: {_currentSession.Name}");
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
            Debug.Log("ルームから退出中...");

            await _currentSession.LeaveAsync();
            _currentSession = null;

            Debug.Log("ルームから正常に退出しました");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルーム退出時にエラーが発生しました: {ex.Message}");
        }
    }
}
